namespace CnpcBlockly.Generator.Parser {
	public interface IType {
		string Name { get; }
		string FullName { get; }
		bool IsValid { get; }
		void Parse(Domain domain);
	}
}
