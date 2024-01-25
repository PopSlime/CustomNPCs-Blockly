namespace CnpcBlockly.Generator.Parser {
	public record JavaField(string Name, IType Type) {
		public override string ToString() => $"{Type} {Name}";
	}
}
