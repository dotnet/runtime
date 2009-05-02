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

		Filter (bdir + "/class/System.XML/System.Xml.XPath/Parser.jay",
			bdir + "/class/System.XML/Mono.Xml.Xsl/PatternParser.jay",
			(i, o) => o.Write (i.ReadToEnd ().Replace ("%start Expr", "%start Pattern")));

		Filter (bdir + "/build/common/Consts.cs.in",
			bdir + "/build/common/Consts.cs",
			(i, o) => o.Write (i.ReadToEnd ().Replace ("@MONO_VERSION@", "Mono-VSBuild")));
	}
	
}
