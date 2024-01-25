using CnpcBlockly.Generator.Resources;
using System;

namespace CnpcBlockly.Generator.Parser {
	public class JavaApiFormatException : Exception {
		public JavaApiFormatException() : base(SR.Error_ApiFormat) { }
		public JavaApiFormatException(string message) : base(message) { }
		public JavaApiFormatException(string message, Exception innerException) : base(message, innerException) { }
	}
}
