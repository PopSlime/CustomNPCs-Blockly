using System.Collections.Generic;

namespace CnpcBlockly.Generator.Parser {
	public abstract record JavaMember(string Name, bool IsStatic, bool IsFinal) {
		public abstract bool IsValid { get; }
		public ISet<string> Tags { get; } = new HashSet<string>();
	}
}
