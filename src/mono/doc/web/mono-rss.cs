//
// Generates the Mono RSS feed
//
// Miguel de Icaza
//
using System;
using System.IO;
using System.Xml;
using System.Text;
using RSS;

class X {
	static RSS.RSS rss;
	static Channel c;
	static int item_count;
	static int line;
	
	static void PopulateRSS (StreamReader input)
	{
		string s;
		
		while ((s = input.ReadLine ()) != null){
			line++;
			if (s.StartsWith ("@item "))
				break;
		}

		if (s == null || !s.StartsWith ("@item ")){
			Console.WriteLine ("Could not find beginning of text to RSS");
			return;
		}

		Item i = null;
		string description = "";
		do {
			if (s.StartsWith ("@item ")){
				if (item_count++ > 25)
					break;

				if (i != null){
					i.Description = description;
					description = "";
				}
				
				string title = s.Substring (6);
				string link = "http://www.go-mono.com/index.html#";
				foreach (char ch in title){
					if (ch != ' ')
						link += ch;
				}
				
				i = c.NewItem ();
				i.Title = title;
				i.Link = link;
			} else {
				description += "\n" + (s == "\n" ? "<p>" : s);
			}
			line++;
		} while ((s = input.ReadLine ()) != null);

		if (i != null){
			i.Description = description;
		}
	}
	
	static void MakeRSS (string input, string output)
	{
		rss = new RSS.RSS ();
		c = rss.NewChannel ("Mono Project News", "http://www.go-mono.com");
		
		c.Title = "Mono Project News";
		c.Link = "http://www.go-mono.com";
		c.Description =
		"News from the Mono project: a portable implementation of the .NET Framework";
		c.WebMaster = "webmaster@go-mono.com";
		c.ManagingEditor = "miguel@ximian.com";
		string t = File.GetLastWriteTime (input).ToString ("r");
		c.PubDate = t;
		c.LastBuildDate = t;

		using (FileStream fs = new FileStream (input, FileMode.Open)){
			using (StreamReader input_stream = new StreamReader (fs)){
				try {
					PopulateRSS (input_stream);
				} catch {
					Console.WriteLine ("{0} failure while loading: {1}", line, input);
					throw;
				}
			}
		}
		
		rss.XmlDocument.Save (output);
	}
	
	static int Main (string [] args)
	{
		switch (args.Length){
		case 0:
			MakeRSS ("index", "index.rss");
			break;
		case 2:
			MakeRSS (args [0], args [1]);
			break;
			
		default:
			Console.WriteLine ("Usage is: mono-rss [input output.rss]");
			return 1;
		}

		return 0;
	}
}
