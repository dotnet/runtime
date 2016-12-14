using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Build.Utilities; // Task
using Microsoft.Build.Framework; // MessageImportance
using Microsoft.NET.Build.Tasks; // LockFileCache
using NuGet.ProjectModel; // LockFileTargetLibrary
using NuGet.Frameworks; // NuGetFramework.Parse(targetframework)

namespace ILLink.Tasks
{
	public class CreateRootDescriptorFile : Task
	{
		/// <summary>
		///   Assembly names (without path or extension) to
		///   include in the generated root file.
		/// </summary>
		[Required]
		public ITaskItem[] AssemblyNames { get; set; }

		/// <summary>
		///   The path to the file to generate.
		/// </summary>
		[Required]
		public ITaskItem RootDescriptorFilePath { get; set; }

		public override bool Execute()
		{
			var roots = new XElement("linker");
			foreach (var assemblyItem in AssemblyNames) {
				var assemblyName = assemblyItem.ItemSpec;
				roots.Add(new XElement("assembly",
						new XAttribute("fullname", assemblyName),
						new XElement("type",
							new XAttribute("fullname", "*"),
							new XAttribute("required", "true"))));
			}

			var xdoc = new XDocument(roots);

			XmlWriterSettings xws = new XmlWriterSettings();
			xws.Indent = true;
			xws.OmitXmlDeclaration = true;

			using (XmlWriter xw = XmlWriter.Create(RootDescriptorFilePath.ItemSpec, xws)) {
				xdoc.Save(xw);
			}

			return true;
		}
	}
}
