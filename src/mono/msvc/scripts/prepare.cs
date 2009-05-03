//
// C# implementation of a handful of shell steps
// this is used to automate the buidl in Windows
//
using System;
using System.Text;
using System.IO;

class Prepare {
	delegate void filt (StreamReader sr, StreamWriter sw);
	
	static void Filter (string inpath, string outpath, filt filter)
	{
		using (var ins = new StreamReader (inpath)){
			using (var outs = new StreamWriter (outpath)){
				filter (ins, outs);
			}
		}
	}
	
	static void Main (string [] args)
	{
		string bdir = args.Length == 0 ? "../../../mcs" : args [0];

		if (!Directory.Exists (Path.Combine(bdir, "class"))){
			Console.Error.WriteLine ("The directory {0} does not contain class at {1}", Path.GetFullPath (bdir), Environment.CurrentDirectory);
			Environment.Exit (1);
		}

		switch (args [1]){
		case "xml":
			Filter (bdir + "/class/System.XML/System.Xml.XPath/Parser.jay",
				bdir + "/class/System.XML/Mono.Xml.Xsl/PatternParser.jay",
				(i, o) => o.Write (i.ReadToEnd ().Replace ("%start Expr", "%start Pattern")));
			break;

		case "core":
			Filter (bdir + "/build/common/Consts.cs.in",
				bdir + "/build/common/Consts.cs",
				(i, o) => o.Write (i.ReadToEnd ().Replace ("@MONO_VERSION@", "2.5.0")));
			break;
			
		default:
			Console.Error.WriteLine ("Unknonw option to prepare.exe {0}", args [1]);
			Environment.Exit (1);
			break;
		}
	}
	
}
