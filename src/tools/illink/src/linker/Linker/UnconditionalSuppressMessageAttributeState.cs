// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ILLink.Shared;
using Mono.Cecil;

namespace Mono.Linker
{
	public class UnconditionalSuppressMessageAttributeState
	{
		internal const string ScopeProperty = "Scope";
		internal const string TargetProperty = "Target";
		internal const string MessageIdProperty = "MessageId";

		public class Suppression
		{
			public SuppressMessageInfo SuppressMessageInfo { get; }
			public bool Used { get; set; } = false;
			public CustomAttribute OriginAttribute { get; }
			public ICustomAttributeProvider Provider { get; }

			public Suppression (SuppressMessageInfo suppressMessageInfo, CustomAttribute originAttribute, ICustomAttributeProvider provider)
			{
				SuppressMessageInfo = suppressMessageInfo;
				OriginAttribute = originAttribute;
				Provider = provider;
			}
		}

		readonly LinkContext _context;
		readonly Dictionary<ICustomAttributeProvider, Dictionary<int, Suppression>> _suppressions;
		HashSet<AssemblyDefinition> InitializedAssemblies { get; }

		public UnconditionalSuppressMessageAttributeState (LinkContext context)
		{
			_context = context;
			_suppressions = new Dictionary<ICustomAttributeProvider, Dictionary<int, Suppression>> ();
			InitializedAssemblies = new HashSet<AssemblyDefinition> ();
		}

		void AddSuppression (Suppression suppression)
		{
			var used = false;
			if (!_suppressions.TryGetValue (suppression.Provider, out var suppressions)) {
				suppressions = new Dictionary<int, Suppression> ();
				_suppressions.Add (suppression.Provider, suppressions);
			} else if (suppressions.TryGetValue (suppression.SuppressMessageInfo.Id, out Suppression? value)) {
				used = value.Used;
				string? elementName = suppression.Provider is MemberReference memberRef ? memberRef.GetDisplayName () : suppression.Provider.ToString ();
				_context.LogMessage ($"Element '{elementName}' has more than one unconditional suppression. Note that only the last one is used.");
			}

			suppression.Used = used;
			suppressions[suppression.SuppressMessageInfo.Id] = suppression;
		}

		public bool IsSuppressed (int id, MessageOrigin warningOrigin, out SuppressMessageInfo info)
		{
			// Check for suppressions on both the suppression context as well as the original member
			// (if they're different). This is to correctly handle compiler generated code
			// which needs to use suppressions from both the compiler generated scope
			// as well as the original user defined method.
			info = default;

			ICustomAttributeProvider? provider = warningOrigin.Provider;
			if (provider == null)
				return false;

			if (IsSuppressed (id, provider, out info))
				return true;

			if (provider is not IMemberDefinition member)
				return false;

			MethodDefinition? owningMethod;
			while (_context.CompilerGeneratedState.TryGetOwningMethodForCompilerGeneratedMember (member, out owningMethod)) {
				Debug.Assert (owningMethod != member);
				if (IsSuppressed (id, owningMethod, out info))
					return true;
				member = owningMethod;
			}

			return false;
		}

		public void GatherSuppressions (ICustomAttributeProvider provider)
		{
			TryGetSuppressionsForProvider (provider, out _);
		}

		public IEnumerable<Suppression> GetUnusedSuppressions ()
		{
			foreach (var (provider, suppressions) in _suppressions) {
				foreach (var (_, suppression) in suppressions) {
					if (!suppression.Used)
						yield return suppression;
				}
			}
		}

		bool IsSuppressed (int id, ICustomAttributeProvider warningOrigin, out SuppressMessageInfo info)
		{
			info = default;

			if (warningOrigin is IMemberDefinition warningOriginMember) {
				while (warningOriginMember != null) {
					if (IsSuppressedOnElement (id, warningOriginMember, out info))
						return true;

					if (warningOriginMember is MethodDefinition method) {
						if (method.TryGetProperty (out var property)) {
							Debug.Assert (property.DeclaringType == warningOriginMember.DeclaringType);
							warningOriginMember = property;
							continue;
						} else if (method.TryGetEvent (out var @event)) {
							Debug.Assert (@event.DeclaringType == warningOriginMember.DeclaringType);
							warningOriginMember = @event;
							continue;
						}
					}

					warningOriginMember = warningOriginMember.DeclaringType;
				}
			}

			ModuleDefinition? module = GetModuleFromProvider (warningOrigin);
			if (module == null)
				return false;

			// Check if there's an assembly or module level suppression.
			if (IsSuppressedOnElement (id, module, out info) ||
				IsSuppressedOnElement (id, module.Assembly, out info))
				return true;

			return false;
		}

		bool IsSuppressedOnElement (int id, ICustomAttributeProvider? provider, out SuppressMessageInfo info)
		{
			info = default;

			if (TryGetSuppressionsForProvider (provider, out var suppressions)) {
				if (suppressions != null && suppressions.TryGetValue (id, out var suppression)) {
					suppression.Used = true;
					info = suppression.SuppressMessageInfo;
					return true;
				}
			}

			return false;
		}

		bool TryGetSuppressionsForProvider (ICustomAttributeProvider? provider, out Dictionary<int, Suppression>? suppressions)
		{
			suppressions = null;
			if (provider == null)
				return false;

			if (_suppressions.TryGetValue (provider, out suppressions))
				return true;


			// Populate the cache with suppressions for this member. We need to look for suppressions on the
			// member itself, and on the assembly/module.

			var membersToScan = new HashSet<ICustomAttributeProvider> { { provider } };

			// Gather assembly-level suppressions if we haven't already. To ensure that we always cache
			// complete information for a member, we will also scan for attributes on any other members
			// targeted by the assembly-level suppressions.
			if (GetModuleFromProvider (provider) is ModuleDefinition module) {
				var assembly = module.Assembly;
				if (InitializedAssemblies.Add (assembly)) {
					foreach (var suppression in DecodeAssemblyAndModuleSuppressions (module)) {
						AddSuppression (suppression);
						membersToScan.Add (suppression.Provider);
					}
				}
			}

			// Populate the cache for this member, and for any members that were targeted by assembly-level
			// suppressions to make sure the cached info is complete.
			foreach (var member in membersToScan) {
				if (member is ModuleDefinition or AssemblyDefinition)
					continue;
				foreach (var suppression in DecodeSuppressions (member))
					AddSuppression (suppression);
			}

			return _suppressions.TryGetValue (provider, out suppressions);
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
				!int.TryParse (warningId.AsSpan (2, 4), out info.Id)) {
				return false;
			}

			if (warningId.Length > 6 && warningId[6] != ':')
				return false;

			if (attribute.HasProperties) {
				foreach (var p in attribute.Properties) {
					switch (p.Name) {
					case ScopeProperty when p.Argument.Value is string scope:
						info.Scope = scope;
						break;
					case TargetProperty when p.Argument.Value is string target:
						info.Target = target;
						break;
					case MessageIdProperty when p.Argument.Value is string messageId:
						info.MessageId = messageId;
						break;
					}
				}
			}

			return true;
		}

		public static ModuleDefinition? GetModuleFromProvider (ICustomAttributeProvider provider)
		{
			switch (provider.MetadataToken.TokenType) {
			case TokenType.Module:
				return provider as ModuleDefinition;
			case TokenType.Assembly:
				return ((AssemblyDefinition) provider).MainModule;
			case TokenType.TypeDef:
				return ((TypeDefinition) provider).Module;
			case TokenType.Method:
			case TokenType.Property:
			case TokenType.Field:
			case TokenType.Event:
				return ((IMemberDefinition) provider).DeclaringType.Module;
			default:
				return null;
			}
		}

		IEnumerable<Suppression> DecodeSuppressions (ICustomAttributeProvider provider)
		{
			Debug.Assert (provider is not ModuleDefinition or AssemblyDefinition);

			if (!_context.CustomAttributes.HasAny (provider))
				yield break;

			foreach (var ca in _context.CustomAttributes.GetCustomAttributes (provider)) {
				if (!TypeRefHasUnconditionalSuppressions (ca.Constructor.DeclaringType))
					continue;

				if (!TryDecodeSuppressMessageAttributeData (ca, out var info))
					continue;

				yield return new Suppression (info, originAttribute: ca, provider);
			}
		}

		IEnumerable<Suppression> DecodeAssemblyAndModuleSuppressions (ModuleDefinition module)
		{
			AssemblyDefinition assembly = module.Assembly;
			foreach (var suppression in DecodeGlobalSuppressions (module, assembly))
				yield return suppression;

			foreach (var _module in assembly.Modules) {
				foreach (var suppression in DecodeGlobalSuppressions (_module, _module))
					yield return suppression;
			}
		}

		IEnumerable<Suppression> DecodeGlobalSuppressions (ModuleDefinition module, ICustomAttributeProvider provider)
		{
			var attributes = _context.CustomAttributes.GetCustomAttributes (provider).
					Where (a => TypeRefHasUnconditionalSuppressions (a.AttributeType));
			foreach (var instance in attributes) {
				SuppressMessageInfo info;
				if (!TryDecodeSuppressMessageAttributeData (instance, out info))
					continue;

				var scope = info.Scope?.ToLower ();
				if (info.Target == null && (scope == "module" || scope == null)) {
					yield return new Suppression (info, originAttribute: instance, provider);
					continue;
				}

				switch (scope) {
				case "module":
					yield return new Suppression (info, originAttribute: instance, provider);
					break;

				case "type":
				case "member":
					if (info.Target == null)
						break;

					foreach (var result in DocumentationSignatureParser.GetMembersForDocumentationSignature (info.Target, module, _context))
						yield return new Suppression (info, originAttribute: instance, result);

					break;
				default:
					_context.LogWarning (_context.GetAssemblyLocation (module.Assembly), DiagnosticId.InvalidScopeInUnconditionalSuppressMessage, info.Scope ?? "", module.Name, info.Target ?? "");
					break;
				}
			}
		}

		static bool TypeRefHasUnconditionalSuppressions (TypeReference typeRef)
		{
			return typeRef.Name == "UnconditionalSuppressMessageAttribute" &&
				typeRef.Namespace == "System.Diagnostics.CodeAnalysis";
		}

		public MessageOrigin GetSuppressionOrigin (Suppression suppression)
		{
			if (_context.CustomAttributes.TryGetCustomAttributeOrigin (suppression.Provider, suppression.OriginAttribute, out MessageOrigin origin))
				return origin;
			if (suppression.Provider is ModuleDefinition module)
				return new MessageOrigin (module.Assembly);
			return new MessageOrigin (suppression.Provider);
		}
	}
}
