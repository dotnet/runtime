// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Xml;
using ILLink.Shared;
using Mono.Cecil;
using Mono.Linker.Steps;

namespace Mono.Linker
{
	public static class EmbeddedXmlInfo
	{
		static EmbeddedResource? GetEmbeddedXml (AssemblyDefinition assembly, Func<Resource, bool> predicate)
		{
			return assembly.Modules
				.SelectMany (mod => mod.Resources)
				.Where (res => res.ResourceType == ResourceType.Embedded)
				.Where (res => res.Name.EndsWith (".xml", StringComparison.OrdinalIgnoreCase))
				.Where (res => predicate (res))
				.SingleOrDefault () as EmbeddedResource;
		}

		public static void ProcessDescriptors (AssemblyDefinition assembly, LinkContext context)
		{
			if (context.Annotations.GetAction (assembly) == AssemblyAction.Skip)
				return;

			var rsc = GetEmbeddedXml (assembly, res => ShouldProcessRootDescriptorResource (assembly, context, res.Name));
			if (rsc == null)
				return;

			DescriptorMarker? marker = null;
			try {
				context.LogMessage ($"Processing embedded linker descriptor '{rsc.Name}' from '{assembly.Name}'.");
				marker = GetExternalResolveStep (context, rsc, assembly);
			} catch (XmlException ex) {
				/* This could happen if some broken XML file is embedded. */
				context.LogError (null, DiagnosticId.XmlException, rsc.Name, ex.ToString ());
			}

			marker?.Mark ();
		}

		public static SubstitutionInfo? ProcessSubstitutions (AssemblyDefinition assembly, LinkContext context)
		{
			if (context.Annotations.GetAction (assembly) == AssemblyAction.Skip)
				return null;

			var rsc = GetEmbeddedXml (assembly, res => res.Name.Equals ("ILLink.Substitutions.xml", StringComparison.OrdinalIgnoreCase));
			if (rsc == null)
				return null;

			BodySubstitutionParser? parser = null;
			try {
				context.LogMessage ($"Processing embedded substitution descriptor '{rsc.Name}' from '{assembly.Name}'.");
				parser = GetExternalSubstitutionParser (context, rsc, assembly);
			} catch (XmlException ex) {
				context.LogError (null, DiagnosticId.XmlException, rsc.Name, ex.ToString ());
			}

			if (parser == null)
				return null;

			var substitutionInfo = new SubstitutionInfo ();
			parser.Parse (substitutionInfo);
			return substitutionInfo;
		}

		public static AttributeInfo? ProcessAttributes (AssemblyDefinition assembly, LinkContext context)
		{
			if (context.Annotations.GetAction (assembly) == AssemblyAction.Skip)
				return null;

			var rsc = GetEmbeddedXml (assembly, res => res.Name.Equals ("ILLink.LinkAttributes.xml", StringComparison.OrdinalIgnoreCase));
			if (rsc == null)
				return null;

			LinkAttributesParser? parser = null;
			try {
				context.LogMessage ($"Processing embedded '{rsc.Name}' from '{assembly.Name}'.");
				parser = GetExternalLinkAttributesParser (context, rsc, assembly);
			} catch (XmlException ex) {
				context.LogError (null, DiagnosticId.XmlException, rsc.Name, ex.ToString ());
			}

			if (parser == null)
				return null;

			var attributeInfo = new AttributeInfo ();
			parser.Parse (attributeInfo);
			return attributeInfo;
		}

		static string GetAssemblyName (string descriptor)
		{
			int pos = descriptor.LastIndexOf ('.');
			if (pos == -1)
				return descriptor;

			return descriptor.Substring (0, pos);
		}

		static bool ShouldProcessRootDescriptorResource (AssemblyDefinition assembly, LinkContext context, string resourceName)
		{
			if (resourceName.Equals ("ILLink.Descriptors.xml", StringComparison.OrdinalIgnoreCase))
				return true;

			if (GetAssemblyName (resourceName) != assembly.Name.Name)
				return false;

			switch (context.Annotations.GetAction (assembly)) {
			case AssemblyAction.Link:
			case AssemblyAction.AddBypassNGen:
			case AssemblyAction.AddBypassNGenUsed:
			case AssemblyAction.Copy:
				return true;
			default:
				return false;
			}
		}

		static DescriptorMarker GetExternalResolveStep (LinkContext context, EmbeddedResource resource, AssemblyDefinition assembly)
		{
			return new DescriptorMarker (context, resource.GetResourceStream (), resource, assembly, "resource " + resource.Name + " in " + assembly.FullName);
		}

		static BodySubstitutionParser GetExternalSubstitutionParser (LinkContext context, EmbeddedResource resource, AssemblyDefinition assembly)
		{
			return new BodySubstitutionParser (context, resource.GetResourceStream (), resource, assembly, "resource " + resource.Name + " in " + assembly.FullName);
		}

		static LinkAttributesParser GetExternalLinkAttributesParser (LinkContext context, EmbeddedResource resource, AssemblyDefinition assembly)
		{
			return new LinkAttributesParser (context, resource.GetResourceStream (), resource, assembly, "resource " + resource.Name + " in " + assembly.FullName);
		}
	}
}
