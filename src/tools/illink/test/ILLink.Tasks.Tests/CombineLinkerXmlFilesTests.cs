// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Xunit;

namespace ILLink.Tasks.Tests
{
	public class CombineLinkerXmlFilesTests
	{
		[Fact]
		public void TestCombineLinkerXmlFiles ()
		{
			CreateLinkerXml (
				new XElement ("linker",
					new XElement ("assembly",
						new XAttribute ("fullname", "assembly1"),
						new XElement ("type",
							new XAttribute ("fullname", "Namespace1.Type1"),
							new XAttribute ("required", "false")))),
				"doc1.xml");

			CreateLinkerXml (
				new XElement ("linker",
					new XElement ("assembly",
						new XAttribute ("fullname", "assembly2"),
						new XElement ("type",
							new XAttribute ("fullname", "*"),
							new XAttribute ("required", "true")))),
				"doc2.xml");

			var xmlFiles = new ITaskItem[] {
				new TaskItem ("doc1.xml"),
				new TaskItem ("doc2.xml"),
			};

			var combiner = new CombineLinkerXmlFiles () {
				LinkerXmlFiles = xmlFiles,
				CombinedLinkerXmlFile = "combined_output.xml"
			};

			Assert.True (combiner.Execute ());

			XDocument combined = XDocument.Load ("combined_output.xml");

			string expectedXml = new XElement ("linker",
					new XElement ("assembly",
						new XAttribute ("fullname", "assembly1"),
						new XElement ("type",
							new XAttribute ("fullname", "Namespace1.Type1"),
							new XAttribute ("required", "false"))),
					new XElement ("assembly",
						new XAttribute ("fullname", "assembly2"),
						new XElement ("type",
							new XAttribute ("fullname", "*"),
							new XAttribute ("required", "true")))).ToString ();
			Assert.Equal (expectedXml, combined.Root.ToString ());
		}

		private static void CreateLinkerXml (XElement root, string path)
		{
			var doc = new XDocument ();
			doc.Add (root);
			doc.Save (path);
		}
	}
}
