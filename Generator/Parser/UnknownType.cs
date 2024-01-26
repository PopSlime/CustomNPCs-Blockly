namespace CnpcBlockly.Generator.Parser {
	public record UnknownType(string Name) : IType {
		public string FullName => Name;

		public bool IsValid => false;

		public void Parse(Domain domain) { }

		public override string ToString() => $"* {FullName}";
	}
}
