using CnpcBlockly.Generator.Parser;
using CnpcBlockly.Generator.Resources;
using System;
using System.Diagnostics;
using System.IO;

namespace CnpcBlockly.Generator {
	internal sealed class Program {
		static void Main(string[] args) {
			Trace.Listeners.Add(new ConsoleTraceListener());
			string apiPath;
			if (args.Length > 0) {
				apiPath = args[0];
			}
			else {
				Console.Write(SR.Prompt_InputApiPath);
				apiPath = Console.ReadLine() ?? string.Empty;
			}
			var package = JavaPackage.Collect(new(Path.Combine(apiPath, "output", "noppes")), "noppes");
			var domain = new Domain();
			InjectBuiltinTypes(domain);
			domain.Flatten(package);
			domain.ParseTypes();
			using var generator = new BlockGenerator(domain, new(Environment.CurrentDirectory));
			generator.RootEventType = domain.GetType("noppes/npcs/api/event/CustomNPCsEvent") as JavaType;
			generator.Generate();
		}

		static void InjectBuiltinTypes(Domain domain) {
			var number = new InjectedType("Number");
			domain.Inject("byte", number);
			domain.Inject("short", number);
			domain.Inject("int", number);
			domain.Inject("long", number);
			domain.Inject("float", number);
			domain.Inject("double", number);
			domain.Inject("boolean", new InjectedType("Boolean"));

			var array = new InjectedType("Array");
			domain.Inject("Array", array);
			domain.Inject("java/util/List", array);
			domain.Inject("java/lang/String", new InjectedType("String"));
			domain.Inject("java/lang/Object", new InjectedType("Object"));
		}
	}
}
