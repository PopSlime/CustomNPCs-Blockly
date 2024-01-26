using CnpcBlockly.Generator.Resources;
using System;
using System.Collections.Generic;
using System.IO;

namespace CnpcBlockly.Generator.Parser {
	public class JavaPackage(DirectoryInfo path, string name) {
		readonly DirectoryInfo _path = path.Exists ? path : throw new ArgumentException(SR.Error_DirectoryNotFound, nameof(path));

		public string Name => name;
		public override string ToString() => Name;

		readonly List<JavaPackage> m_subpackages = [];
		public ICollection<JavaPackage> GetSubpackages() => m_subpackages.AsReadOnly();

		readonly List<JavaType> m_types = [];
		public ICollection<JavaType> GetTypes() => m_types.AsReadOnly();

		public void Collect() {
			m_subpackages.Clear();
			m_types.Clear();
			foreach (var fsinfo in _path.EnumerateFileSystemInfos()) {
				if (fsinfo.Name.Contains('-', StringComparison.Ordinal)) continue;
				if (fsinfo is FileInfo finfo && finfo.Extension.Equals(".html", StringComparison.OrdinalIgnoreCase)) {
					var t = new JavaType(finfo, name);
					m_types.Add(t);
				}
				else if (fsinfo is DirectoryInfo dinfo) {
					var sp = new JavaPackage(dinfo, $"{name}/{dinfo.Name}");
					sp.Collect();
					m_subpackages.Add(sp);
				}
			}
		}
		public static JavaPackage Collect(DirectoryInfo path, string name) {
			var package = new JavaPackage(path, name);
			package.Collect();
			return package;
		}
	}
}
