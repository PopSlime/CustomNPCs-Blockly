using System.Text;
using System.Xml;

namespace CnpcBlockly.Generator {
	internal static class Shared {
		public static Encoding Encoding = new UTF8Encoding(false, true);
		public static readonly XmlReaderSettings XmlSettings = new() {
			DtdProcessing = DtdProcessing.Ignore,
		};
	}
}
