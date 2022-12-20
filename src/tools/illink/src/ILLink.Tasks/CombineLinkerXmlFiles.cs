// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ILLink.Tasks
{
	/// <summary>
	/// Combines multiple linker xml files into a single xml file.
	/// </summary>
	public class CombineLinkerXmlFiles : Task
	{
		/// <summary>
		/// The individual linker xml files that will be combined into one.
		/// </summary>
		[Required]
		public ITaskItem[] LinkerXmlFiles { get; set; }

		/// <summary>
		/// The path to the file to generate.
		/// </summary>
		[Required]
		public string CombinedLinkerXmlFile { get; set; }

		public override bool Execute ()
		{
			var combined = new XElement ("linker");
			foreach (var linkerXmlFile in LinkerXmlFiles) {
				XDocument subFile = XDocument.Load (linkerXmlFile.ItemSpec);

				foreach (var element in subFile.Root.Elements ()) {
					combined.Add (element);
				}
			}

			var xdoc = new XDocument (combined);

			XmlWriterSettings xws = new XmlWriterSettings {
				Indent = true,
				OmitXmlDeclaration = true
			};

			using (XmlWriter xw = XmlWriter.Create (CombinedLinkerXmlFile, xws)) {
				xdoc.Save (xw);
			}

			return true;
		}
	}
}
