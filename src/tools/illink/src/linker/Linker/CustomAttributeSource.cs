// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

namespace Mono.Linker
{
	public class CustomAttributeSource
	{
		public AttributeInfo PrimaryAttributeInfo { get; }
		private readonly Dictionary<AssemblyDefinition, AttributeInfo?> _embeddedXmlInfos;
		readonly LinkContext _context;

		public CustomAttributeSource (LinkContext context)
		{
			PrimaryAttributeInfo = new AttributeInfo ();
			_embeddedXmlInfos = new Dictionary<AssemblyDefinition, AttributeInfo?> ();
			_context = context;
		}

		public static AssemblyDefinition GetAssemblyFromCustomAttributeProvider (ICustomAttributeProvider provider)
		{
			return provider switch {
				MemberReference mr => mr.Module.Assembly,
				AssemblyDefinition ad => ad,
				ModuleDefinition md => md.Assembly,
				InterfaceImplementation ii => ii.InterfaceType.Module.Assembly,
				GenericParameterConstraint gpc => gpc.ConstraintType.Module.Assembly,
				ParameterDefinition pd => pd.ParameterType.Module.Assembly,
				MethodReturnType mrt => mrt.ReturnType.Module.Assembly,
				_ => throw new NotImplementedException (provider.GetType ().ToString ()),
			};
		}

		public bool TryGetEmbeddedXmlInfo (ICustomAttributeProvider provider, [NotNullWhen (true)] out AttributeInfo? xmlInfo)
		{
			AssemblyDefinition assembly = GetAssemblyFromCustomAttributeProvider (provider);

			if (!_embeddedXmlInfos.TryGetValue (assembly, out xmlInfo)) {
				// Add an empty record - this prevents reentrancy
				// If the embedded XML itself generates warnings, trying to log such warning
				// may ask for attributes (suppressions) and thus we could end up in this very place again
				// So first add a dummy record and once processed we will replace it with the real data
				_embeddedXmlInfos.Add (assembly, new AttributeInfo ());
				xmlInfo = _context.EmbeddedXmlInfo.ProcessAttributes (assembly, _context);
				_embeddedXmlInfos[assembly] = xmlInfo;
			}

			return xmlInfo != null;
		}

		public IEnumerable<CustomAttribute> GetCustomAttributes (ICustomAttributeProvider provider, string attributeNamespace, string attributeName)
		{
			foreach (var attr in GetCustomAttributes (provider)) {
				if (attr.AttributeType.Namespace == attributeNamespace && attr.AttributeType.Name == attributeName)
					yield return attr;
			}
		}

		public IEnumerable<CustomAttribute> GetCustomAttributes (ICustomAttributeProvider provider)
		{
			if (provider.HasCustomAttributes) {
				foreach (var customAttribute in provider.CustomAttributes)
					yield return customAttribute;
			}

			if (PrimaryAttributeInfo.CustomAttributes.TryGetValue (provider, out var annotations)) {
				foreach (var customAttribute in annotations)
					yield return customAttribute;
			}

			if (!TryGetEmbeddedXmlInfo (provider, out var embeddedXml))
				yield break;

			if (embeddedXml.CustomAttributes.TryGetValue (provider, out annotations)) {
				foreach (var customAttribute in annotations)
					yield return customAttribute;
			}
		}

		public bool TryGetCustomAttributeOrigin (ICustomAttributeProvider provider, CustomAttribute customAttribute, out MessageOrigin origin)
		{
			if (PrimaryAttributeInfo.CustomAttributesOrigins.TryGetValue (customAttribute, out origin))
				return true;

			if (!TryGetEmbeddedXmlInfo (provider, out var embeddedXml))
				return false;

			return embeddedXml.CustomAttributesOrigins.TryGetValue (customAttribute, out origin);
		}

		public bool HasAny (ICustomAttributeProvider provider)
		{
			if (provider.HasCustomAttributes)
				return true;

			if (PrimaryAttributeInfo.CustomAttributes.ContainsKey (provider))
				return true;

			if (!TryGetEmbeddedXmlInfo (provider, out var embeddedXml))
				return false;

			return embeddedXml.CustomAttributes.ContainsKey (provider);
		}
	}
}
