using CnpcBlockly.Generator.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CnpcBlockly.Generator {
	public partial class BlockGenerator {
		string[]? _cancelableEventBlocks;
		void GenerateEvent(JavaType type, IEnumerable<string> inheritedBlocks) {
			var fields = type.GetFields().Where(m => m.IsValid);
			var typeKey = GetTypeKey(type);
			var key = $"CNPC_T_{typeKey}".ToUpperInvariant();
			_toolboxWriter.Write($"{{'kind':'category','name':'%{{BKY_{key}}}','contents':[");
			_msgWriter.Write($"'{key}':'{type.Name}',");
			GenerateEventHook(type, typeKey);
			if (type.Description == null || !type.Description.Contains("not cancelable", StringComparison.InvariantCultureIgnoreCase)) {
				foreach (var block in _cancelableEventBlocks!) AddBlockToToolbox(block);
			}
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
			_blocksWriter.Write("'type':'input_statement',");
			_blocksWriter.Write("'name':'statement',");
			_blocksWriter.Write("},");
			_generatorWriter.Write($"'{key}':function(b,g){{");
			var code = $"`function {match.Groups[1].Value}(event) {{\\n${{g.statementToCode(b, 'statement')}}}}`";
			_blocksWriter.Write("],");
			_generatorWriter.Write($"return {code};");
			_blocksWriter.Write("'colour':60,");
			_blocksWriter.Write("},");
			_generatorWriter.Write("},");

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
			_blocksWriter.Write($"'output':[{GetInheritanceChain(field.Type)}],");
			_generatorWriter.Write($"return [`event.{field.Name}`,Order.MEMBER];");
			_blocksWriter.Write("'colour':30,");
			_blocksWriter.Write("},");
			_generatorWriter.Write("},");

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
			_blocksWriter.Write("'type':'input_value',");
			_blocksWriter.Write("'name':'value',");
			_blocksWriter.Write($"'check':'{field.Type.FullName}',");
			_blocksWriter.Write("},");
			_generatorWriter.Write("const $value=g.valueToCode(b,'value',Order.ASSIGNMENT);");
			_blocksWriter.Write("],");
			_generatorWriter.Write($"return `event.{field.Name} = ${{$value}};\\n`;");
			_blocksWriter.Write("'previousStatement':null,");
			_blocksWriter.Write("'nextStatement':null,");
			_blocksWriter.Write("'colour':0,");
			_blocksWriter.Write("},");
			_generatorWriter.Write("},");

			AddBlockToToolbox(key);
		}
	}
}
