//
// RSS.cs: Some utility classes to generate RSS feeds
//
// (C) 2002 Miguel de Icaza (miguel@gnu.org)
//
//
using System;
using System.Xml;
using System.IO;

namespace RSS {

	public class Item {
		XmlDocument doc;
		XmlNode item;
		XmlText title, link, description, pubdate;
		
		public Item (XmlDocument doc, XmlNode item)
		{
			this.doc = doc;
			this.item = item;
		}

		XmlText MakeTextElement (string name)
		{
			XmlNode node = doc.CreateElement (name);
			XmlText text = doc.CreateTextNode ("");
			
			item.AppendChild (node);
			node.AppendChild (text);
			
			return text;
		}

		public string Title {
			get {
				if (title == null)
					return null;

				return title.Value;
			}

			set {
				if (title == null)
					title = MakeTextElement ("title");
				title.Value = value;
			}
		}

		public string Link {
			get {
				if (link == null)
					return null;

				return link.Value;
			}

			set {
				if (link == null)
					link = MakeTextElement ("link");
				link.Value = value;
			}
		}

		public string Description {
			get {
				if (description == null)
					return null;

				return description.Value;
			}

			set {
				if (description == null)
					description = MakeTextElement ("description");
				description.Value = value;
			}
		}
		
		public string PubDate {
			get {
				if (pubdate == null)
					return null;

				return pubdate.Value;
			}

			set {
				if (pubdate == null)
					pubdate = MakeTextElement ("pubDate");
				pubdate.Value = value;
			}
		}
	}
	
	public class Channel {
		XmlDocument doc;
		XmlNode channel;
		XmlText title, link, description, language, pubDate, lastBuildDate;
		XmlText managingEditor, webMaster;
		
		XmlText MakeTextElement (string name)
		{
			XmlNode node = doc.CreateElement (name);
			XmlText text = doc.CreateTextNode ("");
			
			channel.AppendChild (node);
			node.AppendChild (text);
			
			return text;
		}
		
		public Channel (XmlDocument doc, XmlNode node)
		{
			this.channel = node;
			this.doc = doc;
			
			title = MakeTextElement ("title");
			link = MakeTextElement ("link");
			description = MakeTextElement ("description");
		}

		public Item NewItem ()
		{
			XmlNode node = doc.CreateElement ("item");
			Item item;

			channel.AppendChild (node);
			item = new Item (doc, node);

			return item;
		}
		
		public string Title {
			get {
				return title.Value;
			}
			
			set {
				title.Value = value;
			}
		}
		
		public string Link {
			get {
				return link.Value;
			}

			set {
				link.Value = value;
			}
		}

		public string Description {
			get {
				return description.Value;
			}

			set {
				description.Value = value;
			}
		}

#region Optional Values
		public string ManagingEditor {
			get {
				if (managingEditor == null)
					return null;
			
				return managingEditor.Value;
			}

			set {
				if (managingEditor == null)
					managingEditor = MakeTextElement ("managingEditor");

				managingEditor.Value = value;
			}
		}

		public string WebMaster {
			get {
				if (webMaster == null)
					return null;
			
				return webMaster.Value;
			}

			set {
				if (webMaster == null)
					webMaster = MakeTextElement ("webMaster");
				webMaster.Value = value;
			}
		}

		public string PubDate {
			get {
				if (pubDate == null)
					return null;

				return pubDate.Value;
			}

			set {
				if (pubDate == null)
					pubDate = MakeTextElement ("pubDate");
				pubDate.Value = value;
			}
		}

		public string LastBuildDate {
			get {
				if (lastBuildDate == null)
					return null;

				return lastBuildDate.Value;
			}

			set {
				if (lastBuildDate == null)
					lastBuildDate = MakeTextElement ("lastBuildDate");
				lastBuildDate.Value = value;
			}
		}

		public string Language {
			get {
				if (language == null)
					return null;

				return language.Value;
			}

			set {
				if (language == null)
					language = MakeTextElement ("language");
				language.Value = value;
			}
		}
#endregion
	}
 
	class RSS {
		XmlDocument doc;
		XmlNode rss;
		
		const string rss_base =
		"<?xml version=\"1.0\"?> <rss version=\"0.92\"></rss>";
		
		public RSS ()
		{
			doc = new XmlDocument ();
			
			doc.LoadXml (rss_base);
			rss = doc.DocumentElement;
		}
		
		public Channel NewChannel (string title, string url)
		{
			XmlNode node = doc.CreateElement ("channel");
			Channel c;
			
			rss.AppendChild (node);
			c = new Channel (doc, node);
			
			return c;
		}
		
		public XmlDocument XmlDocument {
			get {
				return doc;
			}
		}
	}
}


