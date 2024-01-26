namespace CnpcBlockly.Generator.Parser {
	public class InjectedType(string fullName) : IType {
		public string Name => fullName[(fullName.LastIndexOf('/') + 1)..];

		public string FullName => fullName;

		public bool IsValid => true;

		public void Parse(Domain domain) { }

		public override string ToString() => $"! {FullName}";
	}
}
