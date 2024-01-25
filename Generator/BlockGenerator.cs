using CnpcBlockly.Generator.Parser;
using CnpcBlockly.Generator.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CnpcBlockly.Generator {
	public partial class BlockGenerator : IDisposable {
		readonly Domain _domain;
		readonly StreamWriter _blocksWriter;
		readonly StreamWriter _generatorWriter;
		readonly StreamWriter _toolboxWriter;
		readonly StreamWriter _msgWriter;

		public JavaType? RootEventType { get; set; }

		public BlockGenerator(Domain domain, DirectoryInfo dir) {
			ArgumentNullException.ThrowIfNull(domain);
			ArgumentNullException.ThrowIfNull(dir);

			_domain = domain;
			_blocksWriter = new(new FileStream(Path.Combine(dir.FullName, "blocks.g.js"), FileMode.Create, FileAccess.Write), Shared.Encoding);
			_generatorWriter = new(new FileStream(Path.Combine(dir.FullName, "generator.g.js"), FileMode.Create, FileAccess.Write), Shared.Encoding);
			_toolboxWriter = new(new FileStream(Path.Combine(dir.FullName, "toolbox.g.js"), FileMode.Create, FileAccess.Write), Shared.Encoding);
			_msgWriter = new(new FileStream(Path.Combine(dir.FullName, "msg.g.js"), FileMode.Create, FileAccess.Write), Shared.Encoding);
		}

		bool _isDisposed;
		protected virtual void Dispose(bool disposing) {
			if (_isDisposed) return;
			if (disposing) {
				_blocksWriter.Dispose();
				_generatorWriter.Dispose();
				_toolboxWriter.Dispose();
				_msgWriter.Dispose();
			}
			_isDisposed = true;
		}
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		ICollection<IType>? _types;
		public void Generate() {
			GenerateLicense(_blocksWriter);
			GenerateLicense(_toolboxWriter);
			GenerateLicense(_msgWriter);
			_blocksWriter.Write("import * as Blockly from 'blockly/core';export const blocks=Blockly.common.createBlockDefinitionsFromJsonArray([");
			_generatorWriter.Write("import {Order} from 'blockly/javascript';export const forBlock={");
			_toolboxWriter.Write("export const toolbox={'kind':'categoryToolbox','contents':[");
			_toolboxWriter.Write(Snippets.ToolboxBuiltin);
			_toolboxWriter.Write("{'kind':'category','name':'%{BKY_CNPC}','contents':[");
			_msgWriter.Write("export const msg={'CNPC':'Custom NPCs',");
			_types = _domain.GetTypes().Values;
			foreach (var package in _domain.GetPackages().Values) {
				if (!package.Name.Contains('/', StringComparison.Ordinal))
					GeneratePackage(package);
			}
			if (RootEventType != null) GenerateEvent(RootEventType, Enumerable.Empty<string>());
			_blocksWriter.Write("]);");
			_generatorWriter.Write("};");
			_toolboxWriter.Write("]}]};");
			_msgWriter.Write("};");
		}

		static void GenerateLicense(TextWriter writer) => writer.WriteLine(Snippets.License);

		#region Generic types
		void GeneratePackage(JavaPackage package) {
			IEnumerable<JavaType> ttypes = package.GetTypes().OrderBy(t => t.Name);
			if (RootEventType != null)
				ttypes = ttypes.Where(t => !IsAssignableTo(t, RootEventType));
			var types = ttypes.ToArray();
			var key = $"CNPC_P_{package.Name.Replace("/", "_1", StringComparison.Ordinal)}".ToUpperInvariant();
			if (types.Length > 0) {
				_toolboxWriter.Write($"{{'kind':'category','name':'%{{BKY_{key}}}','contents':[");
				_msgWriter.Write($"'{key}':'{package.Name}',");
			}
			foreach (var type in types) GenerateType(type);
			foreach (var sp in package.GetSubpackages()) GeneratePackage(sp);
			if (types.Length > 0)
				_toolboxWriter.Write("]},");
		}

		static bool IsAssignableTo(JavaType src, JavaType dest) => src == dest || (src.BaseType is JavaType baseType && IsAssignableTo(baseType, dest));

		void GenerateType(IType type) {
			if (type is not JavaType jtype) return;
			var fields = jtype.GetFields();
			var methods = jtype.GetMethods();
			if (fields.Count == 0 && methods.Count == 0) return;
			var typeKey = GetTypeKey(type);
			var key = $"CNPC_T_{typeKey}".ToUpperInvariant();
			_toolboxWriter.Write($"{{'kind':'category','name':'%{{BKY_{key}}}','contents':[");
			_msgWriter.Write($"'{key}':'{type.Name}',");
			foreach (var field in fields) GenerateField(jtype, typeKey, field);
			var singletonMethod = methods.Where(m => m.ReturnType == type && m.IsStatic && m.Parameters.Count == 0).SingleOrDefault();
			foreach (var method in methods) GenerateMethod(jtype, typeKey, method, singletonMethod);
			_toolboxWriter.Write("]},");
		}

		void GenerateField(JavaType jtype, string key, JavaField field) {
			GenerateFieldGet(jtype, key, field);
			if (!field.IsFinal) GenerateFieldSet(jtype, key, field);
		}

		void GenerateFieldGet(JavaType type, string typeKey, JavaField field) {
			var key = $"CNPC_FG_{typeKey}_3{field.Name}".ToUpperInvariant();
			_msgWriter.Write(field.IsStatic ? $"'{key}':'{field.Name}'," : $"'{key}':'%1.{field.Name}',");

			_blocksWriter.Write("{");
			_blocksWriter.Write($"'type':'{key}',");
			_blocksWriter.Write($"'message0':'%{{BKY_{key}}}',");
			_blocksWriter.Write("'args0':[");
			_generatorWriter.Write($"'{key}':function(b,g){{");
			if (!field.IsStatic) GenerateThisArgument(type);
			_blocksWriter.Write("],");
			_blocksWriter.Write($"'output':'{field.Type.FullName}',");
			_generatorWriter.Write($"return [`{GenerateThisReference(type, typeKey, field)}.{field.Name}`,Order.MEMBER];");
			_blocksWriter.Write($"'colour':30,");
			_blocksWriter.Write("},");
			_generatorWriter.Write($"}},");

			AddBlockToToolbox(key);
		}

		void GenerateFieldSet(JavaType type, string typeKey, JavaField field) {
			var key = $"CNPC_FS_{typeKey}_3{field.Name}".ToUpperInvariant();
			_msgWriter.Write(field.IsStatic ? $"'{key}':'{field.Name} = %1'," : $"'{key}':'%1.{field.Name} = %2',");

			_blocksWriter.Write("{");
			_blocksWriter.Write($"'type':'{key}',");
			_blocksWriter.Write($"'message0':'%{{BKY_{key}}}',");
			_blocksWriter.Write("'args0':[");
			_generatorWriter.Write($"'{key}':function(b,g){{");
			if (!field.IsStatic) GenerateThisArgument(type);
			_blocksWriter.Write("{");
			_blocksWriter.Write($"'type':'input_value',");
			_blocksWriter.Write($"'name':'value',");
			_blocksWriter.Write($"'check':'{field.Type.FullName}',");
			_blocksWriter.Write("},");
			_generatorWriter.Write($"const $value=g.valueToCode(b,'value',Order.ASSIGNMENT);");
			_blocksWriter.Write("],");
			_generatorWriter.Write($"return `{GenerateThisReference(type, typeKey, field)}.{field.Name} = ${{$value}};\\n`;");
			_blocksWriter.Write($"'previousStatement':null,");
			_blocksWriter.Write($"'nextStatement':null,");
			_blocksWriter.Write($"'colour':0,");
			_blocksWriter.Write("},");
			_generatorWriter.Write($"}},");

			AddBlockToToolbox(key);
		}

		void GenerateMethod(JavaType type, string typeKey, JavaMethod method, JavaMethod? singletonMethod = null) {
			var isStaticOrSingleton = method.IsStatic || singletonMethod != null;

			var key = $"CNPC_M_{typeKey}_3{method.Name}_4{string.Join("_5", method.Parameters.Select(p => GetTypeKey(p.Type)))}".ToUpperInvariant();
			_msgWriter.Write(isStaticOrSingleton
				? $"'{key}':'{method.Name}({string.Join(", ", method.Parameters.Select((p, i) => $"{p.Name} = %{i + 1}"))})',"
				: $"'{key}':'%1.{method.Name}({string.Join(", ", method.Parameters.Select((p, i) => $"{p.Name} = %{i + 2}"))})',"
			);

			bool getFlag = method.Name.StartsWith("get", StringComparison.Ordinal) && method.ReturnType != null && method.Parameters.Count == 0;
			bool setFlag = method.Name.StartsWith("set", StringComparison.Ordinal) && method.ReturnType == null && method.Parameters.Count == 1;

			_blocksWriter.Write("{");
			_blocksWriter.Write($"'type':'{key}',");
			_blocksWriter.Write($"'message0':'%{{BKY_{key}}}',");
			_blocksWriter.Write("'args0':[");
			_generatorWriter.Write($"'{key}':function(b,g){{");
			if (!isStaticOrSingleton) GenerateThisArgument(type);
			foreach (var param in method.Parameters) {
				_blocksWriter.Write("{");
				_blocksWriter.Write($"'type':'input_value',");
				_blocksWriter.Write($"'name':'{param.Name}',");
				_blocksWriter.Write($"'check':'{param.Type.FullName}',");
				_blocksWriter.Write("},");

				_generatorWriter.Write($"const _{param.Name}=g.valueToCode(b,'{param.Name}',Order.COMMA);");
			}
			var code = $"{GenerateThisReference(type, typeKey, method, singletonMethod)}.{method.Name}({string.Join(',', method.Parameters.Select(p => $"${{_{p.Name}}}"))})";
			_blocksWriter.Write("],");
			if (method.ReturnType != null) {
				_blocksWriter.Write($"'output':'{method.ReturnType.FullName}',");
				_generatorWriter.Write($"return [`{code}`,Order.FUNCTION_CALL];");
				if (getFlag)
					_blocksWriter.Write($"'colour':180,");
				else
					_blocksWriter.Write($"'colour':120,");
			}
			else {
				_generatorWriter.Write($"return `{code};\\n`;");
				_blocksWriter.Write($"'previousStatement':null,");
				_blocksWriter.Write($"'nextStatement':null,");
				if (setFlag)
					_blocksWriter.Write($"'colour':90,");
				else
					_blocksWriter.Write($"'colour':150,");
			}
			_blocksWriter.Write("},");
			_generatorWriter.Write($"}},");

			AddBlockToToolbox(key);
		}

		static string GetTypeKey(IType type) => type.FullName.Replace("/", "_1", StringComparison.Ordinal).Replace("$", "_2", StringComparison.Ordinal);

		void GenerateThisArgument(JavaType type) {
			_blocksWriter.Write("{");
			_blocksWriter.Write($"'type':'input_value',");
			_blocksWriter.Write($"'name':'this',");
			_blocksWriter.Write($"'check':'{type.FullName}',");
			_blocksWriter.Write("},");
			_generatorWriter.Write($"const $this=g.valueToCode(b,'this',Order.MEMBER);");
		}

		static string GenerateThisReference(JavaType type, string typeKey, JavaMember member, JavaMethod? singletonMethod = null) => member.IsStatic
			? $"${{g.provideFunction_('CNPC_T_{typeKey}', `var ${{g.FUNCTION_NAME_PLACEHOLDER_}} = Java.type('{type.FullName.Replace('/', '.').Replace('$', '.')}');`)}}"
			: singletonMethod != null
			? $"${{g.provideFunction_('CNPC_I_{typeKey}', `var ${{g.FUNCTION_NAME_PLACEHOLDER_}} = Java.type('{type.FullName.Replace('/', '.').Replace('$', '.')}').{singletonMethod.Name}();`)}}"
			: "${$this}";

		readonly List<string> _blocks = [];
		void AddBlockToToolbox(string key) {
			_toolboxWriter.Write("{");
			_toolboxWriter.Write("'kind':'block',");
			_toolboxWriter.Write($"'type':'{key}',");
			_toolboxWriter.Write("},");
			_blocks.Add(key);
		}
		#endregion

		#region Event types
		void GenerateEvent(JavaType type, IEnumerable<string> inheritedBlocks) {
			var fields = type.GetFields();
			var methods = type.GetMethods();
			var typeKey = GetTypeKey(type);
			var key = $"CNPC_T_{typeKey}".ToUpperInvariant();
			_toolboxWriter.Write($"{{'kind':'category','name':'%{{BKY_{key}}}','contents':[");
			_msgWriter.Write($"'{key}':'{type.Name}',");
			GenerateEventHook(type, typeKey);
			_blocks.Clear();
			foreach (var field in fields) GenerateEventField(type, typeKey, field);
			var iblocks = _blocks.ToArray().Concat(inheritedBlocks);
			foreach (var block in inheritedBlocks) AddBlockToToolbox(block);
			foreach (var subtype in _types!.OfType<JavaType>().Where(t => t.BaseType == type).OrderBy(t => t.Name)) GenerateEvent(subtype, iblocks);
			_toolboxWriter.Write("]},");
		}

		[GeneratedRegex(@"Hook function name: (\w*)")]
		private static partial Regex HookFunctionName();
		void GenerateEventHook(JavaType type, string typeKey) {
			var match = HookFunctionName().Match(type.Description ?? "");
			if (!match.Success) return;

			var key = $"CNPC_E_{typeKey}".ToUpperInvariant();
			_msgWriter.Write($"'{key}':'event {type.Name}\\n%1',");

			_blocksWriter.Write("{");
			_blocksWriter.Write($"'type':'{key}',");
			_blocksWriter.Write($"'message0':'%{{BKY_{key}}}',");
			_blocksWriter.Write("'args0':[");
			_blocksWriter.Write("{");
			_blocksWriter.Write($"'type':'input_statement',");
			_blocksWriter.Write($"'name':'statement',");
			_blocksWriter.Write("},");
			_generatorWriter.Write($"'{key}':function(b,g){{");
			var code = $"`function {match.Groups[1].Value}(event) {{\\n${{g.statementToCode(b, 'statement')}}}}`";
			_blocksWriter.Write("],");
			_generatorWriter.Write($"return {code};");
			_blocksWriter.Write($"'colour':60,");
			_blocksWriter.Write("},");
			_generatorWriter.Write($"}},");

			AddBlockToToolbox(key);
		}

		void GenerateEventField(JavaType jtype, string key, JavaField field) {
			GenerateEventFieldGet(jtype, key, field);
			if (!field.IsFinal) GenerateEventFieldSet(jtype, key, field);
		}

		void GenerateEventFieldGet(JavaType type, string typeKey, JavaField field) {
			if (field.IsStatic) {
				GenerateFieldGet(type, typeKey, field);
				return;
			}

			var key = $"CNPC_FG_{typeKey}_3{field.Name}".ToUpperInvariant();
			_msgWriter.Write($"'{key}':'event.{field.Name}',");

			_blocksWriter.Write("{");
			_blocksWriter.Write($"'type':'{key}',");
			_blocksWriter.Write($"'message0':'%{{BKY_{key}}}',");
			_generatorWriter.Write($"'{key}':function(b,g){{");
			_blocksWriter.Write($"'output':'{field.Type.FullName}',");
			_generatorWriter.Write($"return [`event.{field.Name}`,Order.MEMBER];");
			_blocksWriter.Write($"'colour':30,");
			_blocksWriter.Write("},");
			_generatorWriter.Write($"}},");

			AddBlockToToolbox(key);
		}

		void GenerateEventFieldSet(JavaType type, string typeKey, JavaField field) {
			if (field.IsStatic) {
				GenerateFieldSet(type, typeKey, field);
				return;
			}

			var key = $"CNPC_FS_{typeKey}_3{field.Name}".ToUpperInvariant();
			_msgWriter.Write($"'{key}':'event.{field.Name} = %1',");

			_blocksWriter.Write("{");
			_blocksWriter.Write($"'type':'{key}',");
			_blocksWriter.Write($"'message0':'%{{BKY_{key}}}',");
			_blocksWriter.Write("'args0':[");
			_generatorWriter.Write($"'{key}':function(b,g){{");
			_blocksWriter.Write("{");
			_blocksWriter.Write($"'type':'input_value',");
			_blocksWriter.Write($"'name':'value',");
			_blocksWriter.Write($"'check':'{field.Type.FullName}',");
			_blocksWriter.Write("},");
			_generatorWriter.Write($"const $value=g.valueToCode(b,'value',Order.ASSIGNMENT);");
			_blocksWriter.Write("],");
			_generatorWriter.Write($"return `event.{field.Name} = ${{$value}};\\n`;");
			_blocksWriter.Write($"'previousStatement':null,");
			_blocksWriter.Write($"'nextStatement':null,");
			_blocksWriter.Write($"'colour':0,");
			_blocksWriter.Write("},");
			_generatorWriter.Write($"}},");

			AddBlockToToolbox(key);
		}
		#endregion
	}
}
