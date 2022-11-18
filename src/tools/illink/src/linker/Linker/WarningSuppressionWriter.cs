// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Mono.Cecil;

namespace Mono.Linker
{
	public class WarningSuppressionWriter
	{
		private readonly Dictionary<AssemblyNameDefinition, HashSet<(int Code, IMemberDefinition Member)>> _warnings;
		private readonly FileOutputKind _fileOutputKind;
		readonly LinkContext _context;

		public WarningSuppressionWriter (LinkContext context, FileOutputKind fileOutputKind = FileOutputKind.CSharp)
		{
			_warnings = new Dictionary<AssemblyNameDefinition, HashSet<(int, IMemberDefinition)>> ();
			_fileOutputKind = fileOutputKind;
			_context = context;
		}

		public bool IsEmpty => _warnings.Count == 0;

		public void AddWarning (int code, ICustomAttributeProvider provider)
		{
			// We don't have a targeted suppression mechanism for
			// warnings from assembly-level attributes.
			if (provider is not IMemberDefinition memberDefinition)
				return;

			if (UnconditionalSuppressMessageAttributeState.GetModuleFromProvider (provider) is not ModuleDefinition module)
				return;

			var assemblyName = module.Assembly.Name;
			if (!_warnings.TryGetValue (assemblyName, out var warnings)) {
				warnings = new HashSet<(int, IMemberDefinition)> ();
				_warnings.Add (assemblyName, warnings);
			}

			warnings.Add ((code, memberDefinition));
		}

		public void OutputSuppressions (string directory)
		{
			foreach (var assemblyName in _warnings.Keys) {
				if (_fileOutputKind == FileOutputKind.Xml) {
					OutputSuppressionsXmlFormat (assemblyName, directory);
				} else {
					OutputSuppressionsCSharpFormat (assemblyName, directory);
				}
			}
		}

		void OutputSuppressionsXmlFormat (AssemblyNameDefinition assemblyName, string directory)
		{
			var xmlTree = new XElement ("linker");
			var xmlAssembly = new XElement ("assembly", new XAttribute ("fullname", assemblyName.FullName));
			xmlTree.Add (xmlAssembly);

			foreach (var warning in GetListOfWarnings (assemblyName)) {
				xmlAssembly.Add (
					new XElement ("attribute",
						new XAttribute ("fullname", "System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessageAttribute"),
						new XElement ("argument", Constants.ILLink),
						new XElement ("argument", $"IL{warning.Code}"),
						new XElement ("property", new XAttribute ("name", UnconditionalSuppressMessageAttributeState.ScopeProperty),
							GetWarningSuppressionScopeString (warning.MemberDocumentationSignature)),
						new XElement ("property", new XAttribute ("name", UnconditionalSuppressMessageAttributeState.TargetProperty),
							warning.MemberDocumentationSignature)));
			}

			XDocument xdoc = new XDocument (xmlTree);
			using (var xw = XmlWriter.Create (Path.Combine (directory, $"{assemblyName.Name}.WarningSuppressions.xml"),
				new XmlWriterSettings { Indent = true })) {
				xdoc.Save (xw);
			}
		}

		void OutputSuppressionsCSharpFormat (AssemblyNameDefinition assemblyName, string directory)
		{
			using (var sw = new StreamWriter (Path.Combine (directory, $"{assemblyName.Name}.WarningSuppressions.cs"))) {
				StringBuilder sb = new StringBuilder ("using System.Diagnostics.CodeAnalysis;").AppendLine ().AppendLine ();
				foreach (var warning in GetListOfWarnings (assemblyName)) {
					sb.Append ("[assembly: UnconditionalSuppressMessage (\"")
						.Append (Constants.ILLink)
						.Append ("\", \"IL").Append (warning.Code)
						.Append ("\", Scope = \"").Append (GetWarningSuppressionScopeString (warning.MemberDocumentationSignature))
						.Append ("\", Target = \"").Append (warning.MemberDocumentationSignature)
						.AppendLine ("\")]");
				}

				sw.Write (sb.ToString ());
			}
		}

		List<(int Code, string MemberDocumentationSignature)> GetListOfWarnings (AssemblyNameDefinition assemblyName)
		{
			List<(int Code, string MemberDocumentationSignature)> listOfWarnings = new List<(int Code, string MemberDocumentationSignature)> ();
			StringBuilder sb = new StringBuilder ();
			foreach (var warning in _warnings[assemblyName].ToList ()) {
				DocumentationSignatureGenerator.VisitMember (warning.Member, sb, _context);
				listOfWarnings.Add ((warning.Code, sb.ToString ()));
				sb.Clear ();
			}

			listOfWarnings.Sort ();
			return listOfWarnings;
		}

		static string GetWarningSuppressionScopeString (string memberDocumentationSignature)
		{
			if (memberDocumentationSignature.StartsWith (DocumentationSignatureGenerator.TypePrefix))
				return "type";

			return "member";
		}

		public enum FileOutputKind
		{
			CSharp,
			Xml
		};
	}
}
