//
// ByMaintainer.cs
//
// Author:
//    Sean MacIsaac (sean@ximian.com)
//
// (C) Ximian, Inc.   http://www.ximian.com
//

using System;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;

namespace Mono.StatusReporter {
	public class ByMaintainer {
		static int Main (string[] args) {
			XslTransform xslt = new XslTransform ();
			xslt.Load ("ByMaintainer.xsl");
			//StreamWriter sw = new StreamWriter ("bm/index");

			XPathDocument doc = new XPathDocument ("class.xml");

			XmlDocument maintainers = new XmlDocument();

			maintainers.Load ("maintainers.xml");

			XmlNodeList people = maintainers.GetElementsByTagName("person");
			foreach (XmlNode node in people) {
				string email = node.Attributes.GetNamedItem("email").Value;
				string name = node.Attributes.GetNamedItem("name").Value;

				//sw.WriteLine ("<li><a href=\"" + email + ".html\">" + email + "</a>");

				XmlWriter writer = new XmlTextWriter ("src/" + email, null);

				XsltArgumentList xslArg = new XsltArgumentList ();
				xslArg.AddParam ("email", "", email);
				xslArg.AddParam ("name", "", name);

				xslt.Transform (doc, xslArg, writer);

				writer.Close ();
			}

			//sw.Close ();

			return 0;
		}
	}
}
