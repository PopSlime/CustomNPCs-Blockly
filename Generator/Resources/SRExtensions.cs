namespace CnpcBlockly.Generator.Resources {
	internal static class SRExtensions {
		public static string FormatSR(this string format, params object[] args) => string.Format(SR.Culture, format, args);
	}
}
