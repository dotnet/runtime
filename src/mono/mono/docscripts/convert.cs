using HtmlAgilityPack;

class Convert {
	
	static void Main (string [] args)
	{
		HtmlDocument doc = new HtmlDocument();
		doc.Load(args [0]);
		doc.OptionOutputAsXml = true;
		doc.Save(args [1]);
	}
}
