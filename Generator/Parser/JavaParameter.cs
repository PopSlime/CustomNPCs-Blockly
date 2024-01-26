namespace CnpcBlockly.Generator.Parser {
	public record JavaParameter(string Name, IType Type, bool IsVarargs) {
		public override string ToString() => $"{Type}{(IsVarargs ? "..." : "")} {Name}";
	}
}
