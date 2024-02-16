// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

namespace Mono.Linker
{
	public class MemberActionStore
	{
		public SubstitutionInfo PrimarySubstitutionInfo { get; }
		private readonly Dictionary<AssemblyDefinition, SubstitutionInfo?> _embeddedXmlInfos;
		private readonly Dictionary<PropertyDefinition, bool> _constantFeatureChecks;
		readonly LinkContext _context;

		public MemberActionStore (LinkContext context)
		{
			PrimarySubstitutionInfo = new SubstitutionInfo ();
			_embeddedXmlInfos = new Dictionary<AssemblyDefinition, SubstitutionInfo?> ();
			_constantFeatureChecks = new Dictionary<PropertyDefinition, bool> ();
			_context = context;
		}

		private bool TryGetSubstitutionInfo (MemberReference member, [NotNullWhen (true)] out SubstitutionInfo? xmlInfo)
		{
			var assembly = member.Module.Assembly;
			if (!_embeddedXmlInfos.TryGetValue (assembly, out xmlInfo)) {
				xmlInfo = _context.EmbeddedXmlInfo.ProcessSubstitutions (assembly, _context);
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

			if (_context.IsOptimizationEnabled (CodeOptimizations.SubstituteFeatureChecks, method)
				&& IsGuardForDisabledFeature (method))
				return MethodAction.ConvertToStub;

			return MethodAction.Nothing;
		}

		public bool TryGetMethodStubValue (MethodDefinition method, out object? value)
		{
			if (PrimarySubstitutionInfo.MethodStubValues.TryGetValue (method, out value))
				return true;

			if (TryGetSubstitutionInfo (method, out var embeddedXml)) {
				return embeddedXml.MethodStubValues.TryGetValue (method, out value);
			}

			// If there are no substitutions for the method, look for FeatureCheckAttribute on the property,
			// and substitute 'false' for guards for features that are known to be disabled.
			if (_context.IsOptimizationEnabled (CodeOptimizations.SubstituteFeatureChecks, method)
				&& IsGuardForDisabledFeature (method)) {
				value = false;
				return true;
			}

			return false;
		}

		internal bool IsGuardForDisabledFeature (MethodDefinition method)
		{
			if (!method.IsGetter && !method.IsSetter)
				return false;

			// Look for a property with a getter that matches the method.
			foreach (var property in method.DeclaringType.Properties) {
				if (property.GetMethod != method && property.SetMethod != method)
					continue;

				// Include non-static, non-bool properties with setters as those will get validated inside GetLinkerAttributes.
				foreach (var featureCheckAttribute in _context.Annotations.GetLinkerAttributes<FeatureCheckAttribute> (property)) {
					// When trimming, we assume that a feature check for RequiresUnreferencedCodeAttribute returns false.
					var requiresAttributeType = featureCheckAttribute.FeatureType;
					if (requiresAttributeType.Name == "RequiresUnreferencedCodeAttribute" && requiresAttributeType.Namespace == "System.Diagnostics.CodeAnalysis")
						return true;
				}
			}

			return false;
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
