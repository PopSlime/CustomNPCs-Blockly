using CnpcBlockly.Generator.Parser;
using CnpcBlockly.Generator.Resources;
using System;
using System.IO;
using System.Linq;

namespace CnpcBlockly.Generator {
	public class BlockGenerator : IDisposable {
		readonly Domain _domain;
		readonly StreamWriter _blocksWriter;
		readonly StreamWriter _generatorWriter;
		readonly StreamWriter _toolboxWriter;
		readonly StreamWriter _msgWriter;

		bool _isDisposed;

		public BlockGenerator(Domain domain, DirectoryInfo dir) {
			ArgumentNullException.ThrowIfNull(domain);
			ArgumentNullException.ThrowIfNull(dir);

			_domain = domain;
			_blocksWriter = new(new FileStream(Path.Combine(dir.FullName, "blocks.g.js"), FileMode.Create, FileAccess.Write), Shared.Encoding);
			_generatorWriter = new(new FileStream(Path.Combine(dir.FullName, "generator.g.js"), FileMode.Create, FileAccess.Write), Shared.Encoding);
			_toolboxWriter = new(new FileStream(Path.Combine(dir.FullName, "toolbox.g.js"), FileMode.Create, FileAccess.Write), Shared.Encoding);
			_msgWriter = new(new FileStream(Path.Combine(dir.FullName, "msg.g.js"), FileMode.Create, FileAccess.Write), Shared.Encoding);
		}

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
			foreach (var package in _domain.GetPackages().Values) {
				if (!package.Name.Contains('/', StringComparison.Ordinal))
					GeneratePackage(package);
			}
			_blocksWriter.Write("]);");
			_generatorWriter.Write("};");
			_toolboxWriter.Write("]}]};");
			_msgWriter.Write("};");
		}

		static void GenerateLicense(TextWriter writer) => writer.WriteLine(Snippets.License);

		void GeneratePackage(JavaPackage package) {
			var types = package.GetTypes();
			var key = $"CNPC_P_{package.Name.Replace("/", "_1", StringComparison.Ordinal)}".ToUpperInvariant();
			if (types.Count > 0) {
				_toolboxWriter.Write($"{{'kind':'category','name':'%{{BKY_{key}}}','contents':[");
				_msgWriter.Write($"'{key}':'{package.Name}',");
			}
			foreach (var type in types) GenerateType(type);
			foreach (var sp in package.GetSubpackages()) GeneratePackage(sp);
			if (types.Count > 0)
				_toolboxWriter.Write("]},");
		}

		void GenerateType(IType type) {
			if (type is JavaType jtype) {
				var fields = jtype.GetFields();
				var methods = jtype.GetMethods();
				if (fields.Count == 0 && methods.Count == 0) return;
				var key = $"CNPC_T_{type.FullName.Replace("/", "_1", StringComparison.Ordinal).Replace("$", "_2", StringComparison.Ordinal)}".ToUpperInvariant();
				_toolboxWriter.Write($"{{'kind':'category','name':'%{{BKY_{key}}}','contents':[");
				_msgWriter.Write($"'{key}':'{type.Name}',");
				foreach (var field in fields) GenerateField(jtype, key, field);
				foreach (var method in methods) GenerateMethod(jtype, key, method);
				_toolboxWriter.Write("]},");
			}
		}

		void GenerateField(JavaType type, string typeKey, JavaField field) {
			var key = $"CNPC_F_{typeKey}_3{field.Name}".ToUpperInvariant();
			_msgWriter.Write($"'{key}':'%1.{field.Name}',");

			_blocksWriter.Write("{");
			_blocksWriter.Write($"'type':'{key}',");
			_blocksWriter.Write($"'message0':'%{{BKY_{key}}}',");
			_blocksWriter.Write("'args0':[");
			_generatorWriter.Write($"'{key}':function(b,g){{");
			GenerateThisArgument(type);
			_blocksWriter.Write("],");
			_blocksWriter.Write($"'output':'{field.Type.FullName}',");
			_generatorWriter.Write($"return [`${{$this}}.{field.Name}`,Order.MEMBER];");
			_blocksWriter.Write($"'colour':30,");
			_blocksWriter.Write("},");
			_generatorWriter.Write($"}},");

			AddBlockToToolbox(key);
		}

		void GenerateMethod(JavaType type, string typeKey, JavaMethod method) {
			var key = $"CNPC_M_{typeKey}_3{method.Name}_4{string.Join("_5", method.Parameters.Select(p => p.Type.FullName.Replace("/", "_1", StringComparison.Ordinal).Replace("$", "_2", StringComparison.Ordinal)))}".ToUpperInvariant();
			_msgWriter.Write($"'{key}':'%1.{method.Name}({string.Join(", ", method.Parameters.Select((p, i) => $"{p.Name} = %{i + 2}"))})',");

			bool getFlag = method.Name.StartsWith("get", StringComparison.Ordinal) && method.ReturnType != null && method.Parameters.Count == 0;
			bool setFlag = method.Name.StartsWith("set", StringComparison.Ordinal) && method.ReturnType == null && method.Parameters.Count == 1;

			_blocksWriter.Write("{");
			_blocksWriter.Write($"'type':'{key}',");
			_blocksWriter.Write($"'message0':'%{{BKY_{key}}}',");
			_blocksWriter.Write("'args0':[");
			_generatorWriter.Write($"'{key}':function(b,g){{");
			GenerateThisArgument(type);
			foreach (var param in method.Parameters) {
				_blocksWriter.Write("{");
				_blocksWriter.Write($"'type':'input_value',");
				_blocksWriter.Write($"'name':'{param.Name}',");
				_blocksWriter.Write($"'check':'{param.Type.FullName}',");
				_blocksWriter.Write("},");

				_generatorWriter.Write($"const _{param.Name}=g.valueToCode(b,'{param.Name}',Order.COMMA);");
			}
			var code = $"`${{$this}}.{method.Name}({string.Join(',', method.Parameters.Select(p => $"${{_{p.Name}}}"))})`";
			_blocksWriter.Write("],");
			if (method.ReturnType != null) {
				_blocksWriter.Write($"'output':'{method.ReturnType.FullName}',");
				if (getFlag)
					_blocksWriter.Write($"'colour':180,");
				else
					_blocksWriter.Write($"'colour':120,");
				_generatorWriter.Write($"return [{code},Order.FUNCTION_CALL];");
			}
			else {
				_blocksWriter.Write($"'previousStatement':null,");
				_blocksWriter.Write($"'nextStatement':null,");
				if (setFlag)
					_blocksWriter.Write($"'colour':90,");
				else
					_blocksWriter.Write($"'colour':150,");
				_generatorWriter.Write($"return {code};");
			}
			_blocksWriter.Write("},");
			_generatorWriter.Write($"}},");

			AddBlockToToolbox(key);
		}

		void GenerateThisArgument(JavaType type) {
			_blocksWriter.Write("{");
			_blocksWriter.Write($"'type':'input_value',");
			_blocksWriter.Write($"'name':'this',");
			_blocksWriter.Write($"'check':'{type.FullName}',");
			_blocksWriter.Write("},");
			_generatorWriter.Write($"const $this=g.valueToCode(b,'this',Order.MEMBER);");
		}

		void AddBlockToToolbox(string key) {
			_toolboxWriter.Write("{");
			_toolboxWriter.Write("'kind':'block',");
			_toolboxWriter.Write($"'type':'{key}',");
			_toolboxWriter.Write("},");
		}
	}
}
