namespace CnpcBlockly.Generator.Parser {
	public interface IType {
		string Name { get; }
		string FullName { get; }
		void Parse(Domain domain);
	}
}
