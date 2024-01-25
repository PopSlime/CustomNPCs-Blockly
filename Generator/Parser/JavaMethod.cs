using System.Collections.Generic;

namespace CnpcBlockly.Generator.Parser {
	public record JavaMethod(string Name, IType? ReturnType, IList<JavaParameter> Parameters) {
		public override string ToString() => $"{ReturnType?.ToString() ?? "void"} {Name}({string.Join(", ", Parameters)})";
	}
}
