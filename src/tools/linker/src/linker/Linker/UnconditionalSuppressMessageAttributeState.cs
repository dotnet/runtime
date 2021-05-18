using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Mono.Linker
{
	public class UnconditionalSuppressMessageAttributeState
	{
		internal const string ScopeProperty = "Scope";
		internal const string TargetProperty = "Target";
		internal const string MessageIdProperty = "MessageId";

		readonly LinkContext _context;
		readonly Dictionary<ICustomAttributeProvider, Dictionary<int, SuppressMessageInfo>> _suppressions;
		HashSet<AssemblyDefinition> InitializedAssemblies { get; }

		public UnconditionalSuppressMessageAttributeState (LinkContext context)
		{
			_context = context;
			_suppressions = new Dictionary<ICustomAttributeProvider, Dictionary<int, SuppressMessageInfo>> ();
			InitializedAssemblies = new HashSet<AssemblyDefinition> ();
		}

		public void AddSuppression (CustomAttribute ca, ICustomAttributeProvider provider)
		{
			SuppressMessageInfo info;
			if (!TryDecodeSuppressMessageAttributeData (ca, out info))
				return;

			AddSuppression (info, provider);
		}

		void AddSuppression (SuppressMessageInfo info, ICustomAttributeProvider provider)
		{
			if (!_suppressions.TryGetValue (provider, out var suppressions)) {
				suppressions = new Dictionary<int, SuppressMessageInfo> ();
				_suppressions.Add (provider, suppressions);
			}

			if (suppressions.ContainsKey (info.Id))
				_context.LogMessage ($"Element {provider} has more than one unconditional suppression. Note that only the last one is used.");

			suppressions[info.Id] = info;
		}

		public bool IsSuppressed (int id, MessageOrigin warningOrigin, out SuppressMessageInfo info)
		{
			info = default;
			if (warningOrigin.MemberDefinition == null)
				return false;

			IMemberDefinition elementContainingWarning = warningOrigin.MemberDefinition;
			ModuleDefinition module = GetModuleFromProvider (warningOrigin.MemberDefinition);
			DecodeModuleLevelAndGlobalSuppressMessageAttributes (module);
			while (elementContainingWarning != null) {
				if (IsSuppressed (id, elementContainingWarning, out info))
					return true;

				elementContainingWarning = elementContainingWarning.DeclaringType;
			}

			// Check if there's an assembly or module level suppression.
			if (IsSuppressed (id, module, out info) ||
				IsSuppressed (id, module.Assembly, out info))
				return true;

			return false;
		}

		bool IsSuppressed (int id, ICustomAttributeProvider provider, out SuppressMessageInfo info)
		{
			info = default;
			if (provider == null)
				return false;

			return _suppressions.TryGetValue (provider, out var suppressions) &&
				suppressions.TryGetValue (id, out info);
		}

		static bool TryDecodeSuppressMessageAttributeData (CustomAttribute attribute, out SuppressMessageInfo info)
		{
			info = default;

			// We need at least the Category and Id to decode the warning to suppress.
			// The only UnconditionalSuppressMessageAttribute constructor requires those two parameters.
			if (attribute.ConstructorArguments.Count < 2) {
				return false;
			}

			// Ignore the category parameter because it does not identify the warning
			// and category information can be obtained from warnings themselves.
			// We only support warnings with code pattern IL####.
			if (!(attribute.ConstructorArguments[1].Value is string warningId) ||
				warningId.Length < 6 ||
				!warningId.StartsWith ("IL") ||
				!int.TryParse (warningId.Substring (2, 4), out info.Id)) {
				return false;
			}

			if (warningId.Length > 6 && warningId[6] != ':')
				return false;

			if (attribute.HasProperties) {
				foreach (var p in attribute.Properties) {
					switch (p.Name) {
					case ScopeProperty:
						info.Scope = (p.Argument.Value as string)?.ToLower ();
						break;
					case TargetProperty:
						info.Target = p.Argument.Value as string;
						break;
					case MessageIdProperty:
						info.MessageId = p.Argument.Value as string;
						break;
					}
				}
			}

			return true;
		}

		public static ModuleDefinition GetModuleFromProvider (ICustomAttributeProvider provider)
		{
			switch (provider.MetadataToken.TokenType) {
			case TokenType.Module:
				return provider as ModuleDefinition;
			case TokenType.Assembly:
				return (provider as AssemblyDefinition).MainModule;
			case TokenType.TypeDef:
				return (provider as TypeDefinition).Module;
			case TokenType.Method:
			case TokenType.Property:
			case TokenType.Field:
			case TokenType.Event:
				return (provider as IMemberDefinition).DeclaringType.Module;
			default:
				return null;
			}
		}

		void DecodeModuleLevelAndGlobalSuppressMessageAttributes (ModuleDefinition module)
		{
			AssemblyDefinition assembly = module.Assembly;
			if (InitializedAssemblies.Add (assembly)) {
				LookForModuleLevelAndGlobalSuppressions (module, assembly);
				foreach (var _module in assembly.Modules)
					LookForModuleLevelAndGlobalSuppressions (_module, _module);
			}
		}

		public void LookForModuleLevelAndGlobalSuppressions (ModuleDefinition module, ICustomAttributeProvider provider)
		{
			var attributes = _context.CustomAttributes.GetCustomAttributes (provider).
					Where (a => TypeRefHasUnconditionalSuppressions (a.AttributeType));
			foreach (var instance in attributes) {
				SuppressMessageInfo info;
				if (!TryDecodeSuppressMessageAttributeData (instance, out info))
					continue;

				if (info.Target == null && (info.Scope == "module" || info.Scope == null)) {
					AddSuppression (info, provider);
					continue;
				}

				if (info.Target == null || info.Scope == null)
					continue;

				switch (info.Scope) {
				case "type":
				case "member":
					foreach (var result in DocumentationSignatureParser.GetMembersForDocumentationSignature (info.Target, module))
						AddSuppression (info, result);

					break;
				default:
					_context.LogMessage ($"Scope `{info.Scope}` used in `UnconditionalSuppressMessage` is currently not supported.");
					break;
				}
			}
		}

		public static bool TypeRefHasUnconditionalSuppressions (TypeReference typeRef)
		{
			return typeRef.Name == "UnconditionalSuppressMessageAttribute" &&
				typeRef.Namespace == "System.Diagnostics.CodeAnalysis";
		}
	}
}
