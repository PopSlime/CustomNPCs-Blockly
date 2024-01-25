using System;
using System.Collections.Generic;

namespace CnpcBlockly.Generator.Parser {
	public class Domain {
		readonly Dictionary<string, JavaPackage> m_packages = [];
		public IDictionary<string, JavaPackage> GetPackages() => m_packages.AsReadOnly();

		readonly Dictionary<string, IType> m_types = [];
		public IDictionary<string, IType> GetTypes() => m_types.AsReadOnly();
		public IType? GetType(string name) => m_types.GetValueOrDefault(name);

		public bool Inject(string name, IType type) => m_types.TryAdd(name, type);
		public void Flatten(JavaPackage package) {
			ArgumentNullException.ThrowIfNull(package);
			m_packages.Add(package.Name, package);
			foreach (var type in package.GetTypes()) m_types.Add(type.FullName, type);
			foreach (var sp in package.GetSubpackages()) Flatten(sp);
		}

		public void ParseTypes() {
			foreach (var type in m_types.Values) type.Parse(this);
		}
	}
}
