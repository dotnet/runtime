//
// C# implementation of a handful of shell steps
// this is used to automate the buidl in Windows
//
using System;
using System.Text;
using System.IO;

class Prepare {

	static void Main (string [] args)
	{
		string bdir = args.Length == 0 ? "../../../mcs/class" : args [0];
			
		using (var xps = new StreamReader (bdir + "/System.XML/System.Xml.XPath/Parser.jay")){
			using (var xpp = new StreamWriter (bdir + "/System.XML/Mono.Xml.Xsl/PatternParser.jay")){

				xpp.Write (xps.ReadToEnd ().Replace ("%start Expr", "%start Pattern"));
			}
		}
	}
	
}