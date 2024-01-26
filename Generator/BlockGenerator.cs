using CnpcBlockly.Generator.Parser;
using CnpcBlockly.Generator.Resources;
using System;
using System.Collections.Generic;
using System.IO;

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

		static string GetTypeKey(IType type) => type.FullName.Replace("/", "_1", StringComparison.Ordinal).Replace("$", "_2", StringComparison.Ordinal);

		static string GetInheritanceChain(IType type) {
			if (type is JavaType jtype && jtype.BaseType is IType baseType)
				return $"'{type.FullName}',{GetInheritanceChain(baseType)}";
			return $"'{type.FullName}'";
		}

		readonly List<string> _blocks = [];
		void AddBlockToToolbox(string key) {
			_toolboxWriter.Write("{");
			_toolboxWriter.Write("'kind':'block',");
			_toolboxWriter.Write($"'type':'{key}',");
			_toolboxWriter.Write("},");
			_blocks.Add(key);
		}
	}
}
