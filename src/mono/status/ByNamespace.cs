//
// ByNamespace.cs
//
// Author:
//    Sean MacIsaac (sean@ximian.com)
//
// (C) Ximian, Inc.   http://www.ximian.com
//

using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;

namespace Mono.StatusReporter {
	public class ByMaintainer {
		static int Main (string[] args) {
			XslTransform xslt = new XslTransform ();
			xslt.Load ("ByNamespace.xsl");
			//StreamWriter sw = new StreamWriter ("bn/index");

			XPathDocument doc = new XPathDocument ("class.xml");

			XmlDocument classxml = new XmlDocument ();
			classxml.Load ("class.xml");
			ArrayList nsList = new ArrayList ();

			XmlNodeList classes = classxml.GetElementsByTagName ("class");
			foreach (XmlNode node in classes) {
				string name = node.Attributes.GetNamedItem ("name").Value;
				string ns = name.Substring(0, name.LastIndexOf ("."));
				if (!nsList.Contains (ns)) nsList.Add (ns);
			}

			foreach (string str in nsList) {
				//sw.WriteLine ("<li><a href=\"" + str + ".html\">" + str + "</a>");

				XmlWriter writer = new XmlTextWriter ("src/" + str, null);

				XsltArgumentList xslArg = new XsltArgumentList ();
				xslArg.AddParam ("ns", "", str);

				xslt.Transform (doc, xslArg, writer);

				writer.Close ();
			}

			//sw.Close ();

			return 0;
		}
	}
}
