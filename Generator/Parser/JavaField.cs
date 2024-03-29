namespace CnpcBlockly.Generator.Parser {
	public record JavaField(string Name, bool IsStatic, bool IsFinal, IType Type) : JavaMember(Name, IsStatic, IsFinal) {
		public override bool IsValid => Type.IsValid;

		public override string ToString() => $"{Type} {Name}";
	}
}
