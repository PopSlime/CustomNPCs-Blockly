using CnpcBlockly.Generator.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CnpcBlockly.Generator.Parser {
	public partial class JavaType(FileInfo file, string packageName) : IType {
		readonly FileInfo _file = file.Exists ? file : throw new ArgumentException(SR.Error_FileNotFound, nameof(file));
		public string Name { get; private set; } = file.Name[..file.Name.LastIndexOf('.')].Replace('.', '$');

		public string FullName => $"{packageName}/{Name}";
		public override string ToString() => FullName;

		public bool IsValid => true;

		public IType? BaseType { get; private set; }

		public string? Description { get; private set; }

		readonly Dictionary<string, IType> m_typeParameters = [];
		public IDictionary<string, IType> GetTypeParameters() => m_typeParameters.AsReadOnly();

		readonly List<JavaField> m_fields = [];
		public ICollection<JavaField> GetFields() => m_fields.AsReadOnly();

		readonly List<JavaMethod> m_methods = [];
		public ICollection<JavaMethod> GetMethods() => m_methods.AsReadOnly();

		static readonly XName A = XName.Get("a");
		static readonly XName CLASS = XName.Get("class");
		static readonly XName H3 = XName.Get("h3");
		static readonly XName I = XName.Get("i");
		static readonly XName ID = XName.Get("id");
		static readonly XName TITLE = XName.Get("title");
		static readonly XName UL = XName.Get("ul");

		[GeneratedRegex(@"<(br|hr|input|link|meta|wbr)(.*?)/?>")]
		private static partial Regex SolidusTags();
		public void Parse(Domain domain) {
			ArgumentNullException.ThrowIfNull(domain);
			using var stream = _file.OpenRead();
			using var reader = new StreamReader(stream, Shared.Encoding);
			var src = SolidusTags().Replace(reader.ReadToEnd(), @"<$1 $2 />").Replace("&nbsp;", " ", StringComparison.Ordinal);
			var doc = XDocument.Parse(src);
			var cdel = doc.Descendants().FirstOrDefault(e => e.Attribute(ID)?.Value == "class-description") ?? throw new JavaApiFormatException();

			var tsel = cdel.Elements().FirstOrDefault(e => e.Attribute(CLASS)?.Value == "type-signature")
				?? throw new JavaApiFormatException(SR.Error_InvalidType);
			var tnel = tsel.Descendants().FirstOrDefault(e => e.Attribute(CLASS)?.Value == "element-name type-name-label")
				?? throw new JavaApiFormatException(SR.Error_InvalidType);
			ParseTypeName(domain, tnel);

			var eiel = tsel.Descendants().FirstOrDefault(e => e.Attribute(CLASS)?.Value == "extends-implements");
			if (eiel != null) {
				ParseBaseType(domain, eiel);
			}
			BaseType ??= domain.GetType("java/lang/Object") ?? throw new InvalidOperationException(SR.Error_MissingBaseType);

			var bel = cdel.Elements().FirstOrDefault(e => e.Attribute(CLASS)?.Value == "block");
			if (bel != null) Description = bel.Value;

			var fdel = doc.Descendants().FirstOrDefault(e => e.Attribute(ID)?.Value == "field-detail");
			if (fdel != null) {
				foreach (var fel in fdel.Descendants(UL).First().Elements()) {
					ParseField(domain, fel);
				}
			}

			var mdel = doc.Descendants().FirstOrDefault(e => e.Attribute(ID)?.Value == "method-detail");
			if (mdel != null) {
				foreach (var mel in mdel.Descendants(UL).First().Elements()) {
					ParseMethod(domain, mel);
				}
			}
		}

		void ParseTypeName(Domain domain, XElement element) {
			var splitter = new FlowSplitter(element);
			splitter.GetIdentifier();
			if (splitter.GetDelimiter() == '<') {
				while (true) {
					var paramName = splitter.GetIdentifier() ?? throw new JavaApiFormatException();
					var sep = splitter.GetDelimiter();
					if (sep == ' ') {
						if (splitter.GetIdentifier() != "extends") throw new JavaApiFormatException();
						if (splitter.GetDelimiter() != ' ') throw new JavaApiFormatException();
						m_typeParameters.Add(paramName, ParseTypeReference(domain, splitter.GetJavaType() ?? throw new JavaApiFormatException()) ?? throw new JavaApiFormatException());
						sep = splitter.GetDelimiter();
					}
					else {
						m_typeParameters.Add(paramName, domain.GetType("java/lang/Object") ?? throw new InvalidOperationException(SR.Error_MissingBaseType));
					}
					switch (sep) {
						case '>': return;
						case ',': break;
						default: throw new JavaApiFormatException();
					}
				}
			}
		}

		void ParseBaseType(Domain domain, XElement element) {
			var splitter = new FlowSplitter(element);
			splitter.GetDelimiter();
			var kw = splitter.GetIdentifier();
			if (kw != "extends") return;
			if (splitter.GetDelimiter() != ' ') throw new JavaApiFormatException();
			BaseType = ParseTypeReference(domain, splitter.GetJavaType() ?? throw new JavaApiFormatException());
		}

		static T ParseTags<T>(T member, XElement el) where T : JavaMember {
			foreach (var t in el.Descendants(I).Select(i => i.Attribute(CLASS)?.Value)) {
				if (t == null) continue;
				member.Tags.Add(t);
			}
			return member;
		}

		void ParseField(Domain domain, XElement fel) {
			var name = fel.Descendants(H3).First().Value;
			var sigel = fel.Descendants().First(e => e.Attribute(CLASS)?.Value == "member-signature");
			ParseModifiers(sigel, out var isStatic, out var isFinal);
			var returnType = ParseTypeReference(domain, sigel.Descendants().First(e => e.Attribute(CLASS)?.Value == "return-type")) ?? throw new JavaApiFormatException();
			m_fields.Add(ParseTags<JavaField>(new(name, isStatic, isFinal, returnType), fel));
		}

		void ParseMethod(Domain domain, XElement mel) {
			var name = mel.Descendants(H3).First().Value;
			var sigel = mel.Descendants().First(e => e.Attribute(CLASS)?.Value == "member-signature");
			ParseModifiers(sigel, out var isStatic, out var isFinal);
			var returnType = ParseTypeReference(domain, sigel.Descendants().First(e => e.Attribute(CLASS)?.Value == "return-type"));
			var parameters = new List<JavaParameter>();
			var pel = sigel.Descendants().FirstOrDefault(e => e.Attribute(CLASS)?.Value == "parameters");
			if (pel != null) {
				var splitter = new FlowSplitter(pel);
				if (splitter.GetDelimiter() != '(') throw new JavaApiFormatException();
				while (true) {
					while (splitter.GetDelimiter(peek: true) == '@')
						splitter.GetJavaType(); // TODO annotations
					var type = ParseTypeReference(domain, splitter.GetJavaType() ?? throw new JavaApiFormatException()) ?? throw new JavaApiFormatException();
					var varargs = false;
					char? sep = splitter.GetDelimiter();
					switch (sep) {
						case ' ':
							break;
						case '.':
							if (splitter.GetDelimiter() != '.') throw new JavaApiFormatException();
							if (splitter.GetDelimiter() != '.') throw new JavaApiFormatException();
							varargs = true;
							break;
						default:
							throw new JavaApiFormatException();
					}
					parameters.Add(new(splitter.GetIdentifier() ?? throw new JavaApiFormatException(), type, varargs));
					switch (splitter.GetDelimiter()) {
						case ',': break;
						case ')': goto break_while;
						default: throw new JavaApiFormatException();
					}
				}
			}
		break_while:
			m_methods.Add(ParseTags<JavaMethod>(new(name, isStatic, isFinal, returnType, parameters), mel));
		}

		static void ParseModifiers(XElement sigel, out bool isStatic, out bool isFinal) {
			var model = sigel.Descendants().FirstOrDefault(e => e.Attribute(CLASS)?.Value == "modifiers");
			isStatic = false;
			isFinal = false;
			if (model == null) return;
			var mods = model.Value.Split(' ');
			isStatic = mods.Contains("static");
			isFinal = mods.Contains("final");
		}

		IType? ParseTypeReference(Domain domain, XElement element) {
			var name = element.Value;
			if (name.EndsWith("[]", StringComparison.Ordinal)) {
				Trace.TraceWarning(SR.Warning_ArrayType.FormatSR(name));
				return domain.GetType("Array") ?? throw new InvalidOperationException(SR.Error_MissingBaseType);
			}
			var references = element.Elements(A).ToArray();
			if (references.Length > 1)
				Trace.TraceWarning(SR.Warning_GenericType.FormatSR(name));
			var reference = references.Length > 0 ? references[0] : null;
			if (reference == null) {
				if (name == "void") return null;
				return ParseTypeReferenceFromText(domain, name);
			}
			else {
				return ParseTypeReferenceFromElement(domain, reference);
			}
		}

		static IType ParseTypeReferenceFromText(Domain domain, string name) {
			var type = domain.GetType(name);
			if (type == null) {
				Trace.TraceWarning(SR.Warning_UnknownType.FormatSR(name));
				return new UnknownType(name);
			}
			return type;
		}

		IType ParseTypeReferenceFromElement(Domain domain, XElement reference) {
			var attr = reference.Attribute(TITLE) ?? throw new JavaApiFormatException(SR.Error_InvalidTypeReference);
			var refInfo = attr.Value.Split(" in ");
			var refType = refInfo[0];
			var package = refInfo[1].Replace('.', '/');
			var typeName = reference.Value.Replace('.', '$');
			if (refType == "type parameter")
				return m_typeParameters[typeName];
			var fullName = $"{package}/{typeName}";
			var type = domain.GetType(fullName);
			if (type == null) {
				Trace.TraceWarning(SR.Warning_UnreferencedType.FormatSR(fullName));
				return new UnreferencedType(typeName, package);
			}
			return type;
		}

		struct FlowSplitter(XElement element) {
			readonly IEnumerator<XNode> _enumerator = element.Nodes().GetEnumerator();
			bool _isText;
			string _current;
			int _index;

			bool Next(bool textOnly = false) {
				if (_isText && _index < _current.Length)
					return true;
				if (textOnly && _enumerator.Current.NextNode is not XText)
					return false;
				if (!_enumerator.MoveNext())
					return false;
				var node = _enumerator.Current;
				if (node is XText text) {
					_current = text.Value;
					_index = 0;
					_isText = true;
				}
				else {
					_isText = false;
				}
				return true;
			}
			public string? GetIdentifier() {
				if (!Next()) return null;
				if (!_isText) throw new JavaApiFormatException();
				var start = _index;
				bool flag = false;
				for (; _index < _current.Length; _index++) {
					var c = _current[_index];
					if (!(flag ? IsJavaLetterOrDigit(c) : IsJavaLetter(c)))
						break;
					flag = true;
				}
				return _current[start.._index];
			}
			static bool IsJavaLetter(char c) => char.IsAsciiLetter(c) || c == '_' || c == '$';
			static bool IsJavaLetterOrDigit(char c) => IsJavaLetter(c) || char.IsAsciiDigit(c);
			public char? GetDelimiter(bool peek = false, bool end = false) {
				if (!Next(peek)) return null;
				if (!_isText) throw new JavaApiFormatException();
				char result = ' ';
				bool flag = false;
				for (; _index < _current.Length; _index++) {
					var c = _current[_index];
					if (IsJavaLetterOrDigit(c)) {
						if (!flag)
							return null;
						break;
					}
					if (!char.IsWhiteSpace(c)) {
						if (result != ' ')
							break;
						result = c;
						if (end) {
							_index++;
							break;
						}
					}
					flag = true;
				}
				return result;
			}
			static readonly XName TYPE = XName.Get("type");
			public XElement? GetJavaType() {
				if (!Next()) return null;
				XElement result = new(TYPE);
			loop_unknown_type:
				if (_isText)
					result.Add(GetIdentifier());
				else
					result.Add(_enumerator.Current);
				if (!Next()) return result;
				if (!_isText) throw new JavaApiFormatException();
				var start = _index;
				var sep = GetDelimiter();
				if (sep == '<') {
					while (true) {
						var sep2 = GetDelimiter(peek: true, end: true);
						if (sep2 == '?') {
							sep2 = GetDelimiter();
							if (sep2 != ' ') goto next;
							if (GetIdentifier() != "extends") throw new JavaApiFormatException();
							if (GetDelimiter() != ' ') throw new JavaApiFormatException();
						}
						var type = GetJavaType() ?? throw new JavaApiFormatException();
						foreach (var node in type.Nodes()) {
							result.Add(node);
						}
						sep2 = GetDelimiter(end: true);
					next:
						switch (sep2) {
							case ',': GetDelimiter(peek: true); break;
							case '>': goto break_while;
							default: throw new JavaApiFormatException();
						}
					}
				break_while:
					start = _index;
					sep = GetDelimiter();
				}
				else if (sep == '.' && _index < _current.Length && _current[_index] != '.') {
					result.Add(".");
					goto loop_unknown_type;
				}
				while (sep == '[') {
					if (GetDelimiter(end: true) != ']') throw new JavaApiFormatException();
					result.Add("[]");
					start = _index;
					sep = GetDelimiter();
				}
				_index = start;
				return result;
			}
		}
	}
}
