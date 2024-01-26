namespace CnpcBlockly.Generator.Parser {
	public record UnreferencedType(string Name, string PackageName) : IType {
		public string FullName => $"{PackageName}/{Name}";

		public bool IsValid => false;

		public void Parse(Domain domain) { }

		public override string ToString() => $"? {FullName}";
	}
}
