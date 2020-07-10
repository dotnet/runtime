using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace Mono.Linker
{
	public class WarningSuppressionWriter
	{
		private readonly LinkContext _context;
		private readonly Dictionary<AssemblyNameDefinition, HashSet<(int, IMemberDefinition)>> _warnings;

		public WarningSuppressionWriter (LinkContext context)
		{
			_context = context;
			_warnings = new Dictionary<AssemblyNameDefinition, HashSet<(int, IMemberDefinition)>> ();
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
				using (var sw = new StreamWriter (Path.Combine (_context.OutputDirectory, $"{assemblyName.Name}.WarningSuppressions.cs"))) {
					StringBuilder sb = new StringBuilder ("using System.Diagnostics.CodeAnalysis;").AppendLine ().AppendLine ();
					List<(int Code, IMemberDefinition Member)> listOfWarnings = _warnings[assemblyName].ToList ();
					listOfWarnings.Sort ((a, b) => {
						string lhs = a.Member is MethodReference lhsMethod ? lhsMethod.GetDisplayName () : a.Member.FullName;
						string rhs = b.Member is MethodReference rhsMethod ? rhsMethod.GetDisplayName () : b.Member.FullName;
						if (lhs == rhs)
							return a.Code.CompareTo (b.Code);

						return string.CompareOrdinal (lhs, rhs);
					});

					foreach (var warning in listOfWarnings) {
						int warningCode = warning.Code;
						IMemberDefinition warningOrigin = warning.Member;
						sb.Append ("[assembly: UnconditionalSuppressMessage (\"");
						sb.Append (Constants.ILLink);
						sb.Append ("\", \"IL");
						sb.Append (warningCode).Append ("\", Scope = \"");
						switch (warningOrigin.MetadataToken.TokenType) {
						case TokenType.TypeDef:
							sb.Append ("type\", Target = \"");
							break;
						case TokenType.Method:
						case TokenType.Property:
						case TokenType.Field:
						case TokenType.Event:
							sb.Append ("member\", Target = \"");
							break;
						default:
							break;
						}

						DocumentationSignatureGenerator.Instance.VisitMember (warningOrigin, sb);
						sb.AppendLine ("\")]");
					}

					sw.Write (sb.ToString ());
				}
			}
		}
	}
}
