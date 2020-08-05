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
		private readonly LinkContext _context;
		private readonly Dictionary<AssemblyNameDefinition, HashSet<(int Code, IMemberDefinition Member)>> _warnings;
		private readonly FileOutputKind _fileOutputKind;

		public WarningSuppressionWriter (LinkContext context,
			FileOutputKind fileOutputKind = FileOutputKind.CSharp)
		{
			_context = context;
			_warnings = new Dictionary<AssemblyNameDefinition, HashSet<(int, IMemberDefinition)>> ();
			_fileOutputKind = fileOutputKind;
		}

		public void AddWarning (int code, IMemberDefinition memberDefinition)
		{
			var assemblyName = _context.Suppressions.GetModuleFromProvider (memberDefinition).Assembly.Name;
			if (!_warnings.TryGetValue (assemblyName, out var warnings)) {
				warnings = new HashSet<(int, IMemberDefinition)> ();
				_warnings.Add (assemblyName, warnings);
			}

			warnings.Add ((code, memberDefinition));
		}

		public void OutputSuppressions ()
		{
			foreach (var assemblyName in _warnings.Keys) {
				if (_fileOutputKind == FileOutputKind.Xml) {
					OutputSuppressionsXmlFormat (assemblyName);
				} else {
					OutputSuppressionsCSharpFormat (assemblyName);
				}
			}
		}

		void OutputSuppressionsXmlFormat (AssemblyNameDefinition assemblyName)
		{
			var xmlTree =
				new XElement ("linker",
					new XElement ("assembly", new XAttribute ("fullname", assemblyName.FullName)));

			foreach (var warning in GetListOfWarnings (assemblyName)) {
				xmlTree.Element ("assembly").Add (
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
			using (var xw = XmlWriter.Create (Path.Combine (_context.OutputDirectory, $"{assemblyName.Name}.WarningSuppressions.xml"),
				new XmlWriterSettings { Indent = true })) {
				xdoc.Save (xw);
			}
		}

		void OutputSuppressionsCSharpFormat (AssemblyNameDefinition assemblyName)
		{
			using (var sw = new StreamWriter (Path.Combine (_context.OutputDirectory, $"{assemblyName.Name}.WarningSuppressions.cs"))) {
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
				DocumentationSignatureGenerator.Instance.VisitMember (warning.Member, sb);
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
