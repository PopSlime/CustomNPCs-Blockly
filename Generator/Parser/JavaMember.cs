namespace CnpcBlockly.Generator.Parser {
	public abstract record JavaMember(string Name, bool IsStatic, bool IsFinal) {
		public abstract bool IsValid { get; }
	}
}
