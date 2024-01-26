using System.Collections.Generic;
using System.Linq;

namespace CnpcBlockly.Generator.Parser {
	public record JavaMethod(string Name, bool IsStatic, bool IsFinal, IType? ReturnType, IList<JavaParameter> Parameters) : JavaMember(Name, IsStatic, IsFinal) {
		public override bool IsValid => (ReturnType?.IsValid ?? true) && Parameters.All(p => p.Type.IsValid);

		public override string ToString() => $"{ReturnType?.ToString() ?? "void"} {Name}({string.Join(", ", Parameters)})";
	}
}
