using System.Collections.Generic;

namespace CnpcBlockly.Generator.Parser {
	public record JavaMethod(string Name, bool IsStatic, bool IsFinal, IType? ReturnType, IList<JavaParameter> Parameters) : JavaMember(Name, IsStatic, IsFinal) {
		public override string ToString() => $"{ReturnType?.ToString() ?? "void"} {Name}({string.Join(", ", Parameters)})";
	}
}
