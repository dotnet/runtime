// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker
{
	public class CustomAttributeSource
	{
		public AttributeInfo PrimaryAttributeInfo { get; }
		private readonly Dictionary<AssemblyDefinition, AttributeInfo> _embeddedXmlInfos;
		readonly LinkContext _context;

		public CustomAttributeSource (LinkContext context)
		{
			PrimaryAttributeInfo = new AttributeInfo ();
			_embeddedXmlInfos = new Dictionary<AssemblyDefinition, AttributeInfo> ();
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

		public bool TryGetEmbeddedXmlInfo (ICustomAttributeProvider provider, out AttributeInfo xmlInfo)
		{
			var assembly = GetAssemblyFromCustomAttributeProvider (provider);

			if (!_embeddedXmlInfos.TryGetValue (assembly, out xmlInfo)) {
				xmlInfo = EmbeddedXmlInfo.ProcessAttributes (assembly, _context);
				_embeddedXmlInfos.Add (assembly, xmlInfo);
			}

			return xmlInfo != null;
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
