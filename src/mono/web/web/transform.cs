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

			xsl.Transform (xml, null, Console.Out);
		}
	}
}
