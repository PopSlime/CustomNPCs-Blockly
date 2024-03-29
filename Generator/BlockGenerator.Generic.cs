using CnpcBlockly.Generator.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CnpcBlockly.Generator {
	public partial class BlockGenerator {
		void GeneratePackage(JavaPackage package) {
			IEnumerable<JavaType> ttypes = package.GetTypes().OrderBy(t => t.Name);
			if (RootEventType != null)
				ttypes = ttypes.Where(t => !IsAssignableTo(t, RootEventType));
			var types = ttypes.ToArray();
			var key = $"CNPC_P_{package.Name.Replace("/", "_1", StringComparison.Ordinal)}".ToUpperInvariant();
			if (types.Length > 0) {
				_toolboxWriter.Write($"{{'kind':'category','toolboxitemid':'{key}','name':'%{{BKY_{key}}}','contents':[");
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
			var fields = jtype.GetFields().Where(m => m.IsValid).ToArray();
			var methods = jtype.GetMethods().Where(m => m.IsValid).ToArray();
			if (fields.Length == 0 && methods.Length == 0) return;
			var typeKey = GetTypeKey(type);
			var key = $"CNPC_T_{typeKey}".ToUpperInvariant();
			_toolboxWriter.Write($"{{'kind':'category','toolboxitemid':'{key}','name':'%{{BKY_{key}}}','contents':[");
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
			_blocksWriter.Write($"'output':[{GetInheritanceChain(field.Type)}],");
			_generatorWriter.Write($"return [`{GenerateThisReference(type, typeKey, field)}.{field.Name}`,Order.MEMBER];");
			_blocksWriter.Write("'colour':30,");
			_blocksWriter.Write("},");
			_generatorWriter.Write("},");

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
			_blocksWriter.Write("'type':'input_value',");
			_blocksWriter.Write("'name':'value',");
			_blocksWriter.Write($"'check':'{field.Type.FullName}',");
			_blocksWriter.Write("},");
			_generatorWriter.Write("const $value=g.valueToCode(b,'value',Order.ASSIGNMENT);");
			_blocksWriter.Write("],");
			_generatorWriter.Write($"return `{GenerateThisReference(type, typeKey, field)}.{field.Name} = ${{$value}};\\n`;");
			_blocksWriter.Write("'previousStatement':null,");
			_blocksWriter.Write("'nextStatement':null,");
			_blocksWriter.Write("'colour':0,");
			_blocksWriter.Write("},");
			_generatorWriter.Write("},");

			AddBlockToToolbox(key);
		}

		[GeneratedRegex(@"^[a-z]*")]
		private static partial Regex MethodPrefix();
		void GenerateMethod(JavaType type, string typeKey, JavaMethod method, JavaMethod? singletonMethod = null) {
			var isStaticOrSingleton = method.IsStatic || singletonMethod != null;

			var sigKey = $"{typeKey}_3{method.Name}_4{string.Join("_5", method.Parameters.Select(p => GetTypeKey(p.Type)))}".ToUpperInvariant();
			var key = $"CNPC_M_{sigKey}";
			var msg = isStaticOrSingleton
				? $"{method.Name}({string.Join(", ", method.Parameters.Select((p, i) => $"{p.Name} = %{i + 1}"))})"
				: $"%1.{method.Name}({string.Join(", ", method.Parameters.Select((p, i) => $"{p.Name} = %{i + 2}"))})";
			_msgWriter.Write($"'{key}':'{msg}',");

			var prefix = MethodPrefix().Match(method.Name).Value;
			bool chainingFlag = method.Tags.Contains("method-chaining") && method.ReturnType != null;
			bool getFlag = (prefix is "get" or "in" or "is" or "has" or "can") && method.ReturnType != null && method.Parameters.Count == 0;
			bool setFlag = (prefix is "set") && (method.ReturnType == null || chainingFlag) && method.Parameters.Count == 1;
			bool mutatingFlag = method.Tags.Contains("method-mutating") && method.ReturnType != null;

			if (mutatingFlag) {
				_msgWriter.Write($"'CNPC_MR_{sigKey}':'output -> ',");
			}

			_blocksWriter.Write("{");
			_blocksWriter.Write($"'type':'{key}',");
			_blocksWriter.Write($"'message0':'%{{BKY_{key}}}',");
			_blocksWriter.Write("'args0':[");
			_generatorWriter.Write($"'{key}':function(b,g){{");
			if (!isStaticOrSingleton) GenerateThisArgument(type);
			foreach (var param in method.Parameters) {
				_blocksWriter.Write("{");
				_blocksWriter.Write("'type':'input_value',");
				_blocksWriter.Write($"'name':'{param.Name}',");
				_blocksWriter.Write($"'check':'{param.Type.FullName}',");
				_blocksWriter.Write("'align':'RIGHT',");
				_blocksWriter.Write("},");

				_generatorWriter.Write($"const _{param.Name}=g.valueToCode(b,'{param.Name}',Order.COMMA);");
			}
			var code = $"{GenerateThisReference(type, typeKey, method, singletonMethod)}.{method.Name}({string.Join(", ", method.Parameters.Select(p => $"${{_{p.Name}}}"))})";
			_blocksWriter.Write("],");
			if (method.ReturnType != null && !chainingFlag)
				_blocksWriter.Write($"'output':[{GetInheritanceChain(method.ReturnType!)}],");
			if (method.ReturnType != null && !mutatingFlag && !chainingFlag) {
				_generatorWriter.Write($"return [`{code}`,Order.FUNCTION_CALL];");
				if (getFlag)
					_blocksWriter.Write("'colour':180,");
				else
					_blocksWriter.Write("'colour':120,");
			}
			else {
				_blocksWriter.Write("'previousStatement':null,");
				_blocksWriter.Write("'nextStatement':null,");
				if (setFlag)
					_blocksWriter.Write("'colour':90,");
				else
					_blocksWriter.Write("'colour':150,");
				if (mutatingFlag) {
					_generatorWriter.Write("const _return=b.getFieldValue('return');");
					code = "${_return?`${g.getVariableName(_return)} = `:''}" + code;
					_blocksWriter.Write("'extensions':['INIT_MUTATOR_METHOD_MUTATING','MIXIN_MUTATOR_METHOD_MUTATING'],");
					_blocksWriter.Write("'mutator':'MUTATOR_METHOD_MUTATING',");
				}
				_generatorWriter.Write($"return `{code};\\n`;");
			}
			_blocksWriter.Write("},");
			_generatorWriter.Write("},");

			AddBlockToToolbox(key);
		}

		void GenerateThisArgument(JavaType type) {
			_blocksWriter.Write("{");
			_blocksWriter.Write("'type':'input_value',");
			_blocksWriter.Write("'name':'this',");
			_blocksWriter.Write($"'check':'{type.FullName}',");
			_blocksWriter.Write("'align':'RIGHT',");
			_blocksWriter.Write("},");
			_generatorWriter.Write("const $this=g.valueToCode(b,'this',Order.MEMBER);");
		}

		static string GenerateThisReference(JavaType type, string typeKey, JavaMember member, JavaMethod? singletonMethod = null) => member.IsStatic
			? GenerateTypeReference(type, typeKey)
			: singletonMethod != null
			? GenerateTypeReference(type, typeKey, "I", $".{singletonMethod.Name}()")
			: "${$this}";

		static string GenerateTypeReference(JavaType type, string typeKey, string refPrefix = "T", string refSuffix = "") => $"${{g.provideFunction_('CNPC_{refPrefix}_{typeKey}', `var ${{g.FUNCTION_NAME_PLACEHOLDER_}} = Java.type('{type.FullName.Replace('/', '.').Replace('$', '.')}'){refSuffix};`)}}";
	}
}
