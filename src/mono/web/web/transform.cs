using System;
using System.Xml;
using System.Xml.Xsl;

namespace Transform
{
	class Transform
	{
		public static void Main (string [] rgstrArgs)
		{
			XmlDocument xml = new XmlDocument ();
			xml.Load (rgstrArgs [0]);

			XslTransform xsl = new XslTransform ();
			xsl.Load (rgstrArgs [1]);

			XmlTextWriter xtw = new XmlTextWriter (Console.Out);
			xtw.Formatting = Formatting.Indented;
			XmlWriter w = new Mono.Xml.Ext.XhtmlWriter (xtw);
			xsl.Transform (xml, null, w);
			w.Close ();
		}
	}
}
