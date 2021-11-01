// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

namespace Mono.Linker
{
	public class MemberActionStore
	{
		public SubstitutionInfo PrimarySubstitutionInfo { get; }
		private readonly Dictionary<AssemblyDefinition, SubstitutionInfo?> _embeddedXmlInfos;
		readonly LinkContext _context;

		public MemberActionStore (LinkContext context)
		{
			PrimarySubstitutionInfo = new SubstitutionInfo ();
			_embeddedXmlInfos = new Dictionary<AssemblyDefinition, SubstitutionInfo?> ();
			_context = context;
		}

		public bool TryGetSubstitutionInfo (MemberReference member, [NotNullWhen (true)] out SubstitutionInfo? xmlInfo)
		{
			var assembly = member.Module.Assembly;
			if (!_embeddedXmlInfos.TryGetValue (assembly, out xmlInfo)) {
				xmlInfo = EmbeddedXmlInfo.ProcessSubstitutions (assembly, _context);
				_embeddedXmlInfos.Add (assembly, xmlInfo);
			}

			return xmlInfo != null;
		}

		public MethodAction GetAction (MethodDefinition method)
		{
			if (PrimarySubstitutionInfo.MethodActions.TryGetValue (method, out MethodAction action))
				return action;

			if (TryGetSubstitutionInfo (method, out var embeddedXml)) {
				if (embeddedXml.MethodActions.TryGetValue (method, out action))
					return action;
			}

			return MethodAction.Nothing;
		}

		public bool TryGetMethodStubValue (MethodDefinition method, out object? value)
		{
			if (PrimarySubstitutionInfo.MethodStubValues.TryGetValue (method, out value))
				return true;

			if (!TryGetSubstitutionInfo (method, out var embeddedXml))
				return false;

			return embeddedXml.MethodStubValues.TryGetValue (method, out value);
		}

		public bool TryGetFieldUserValue (FieldDefinition field, out object? value)
		{
			if (PrimarySubstitutionInfo.FieldValues.TryGetValue (field, out value))
				return true;

			if (!TryGetSubstitutionInfo (field, out var embeddedXml))
				return false;

			return embeddedXml.FieldValues.TryGetValue (field, out value);
		}

		public bool HasSubstitutedInit (FieldDefinition field)
		{
			if (PrimarySubstitutionInfo.FieldInit.Contains (field))
				return true;

			if (!TryGetSubstitutionInfo (field, out var embeddedXml))
				return false;

			return embeddedXml.FieldInit.Contains (field);
		}
	}
}