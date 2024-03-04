// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared;
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
				&& IsCheckForDisabledFeature (method))
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

			if (_context.IsOptimizationEnabled (CodeOptimizations.SubstituteFeatureChecks, method)
				&& IsCheckForDisabledFeature (method)) {
				value = false;
				return true;
			}

			return false;
		}

		internal bool IsCheckForDisabledFeature (MethodDefinition method)
		{
			if (!method.IsStatic)
				return false;

			if (method.ReturnType.MetadataType != MetadataType.Boolean)
				return false;

			if (FindProperty (method) is not PropertyDefinition property)
				return false;

			if (property.SetMethod != null)
				return false;

			HashSet<TypeDefinition> featureSet = new ();
			foreach (var featureCheckAttribute in _context.CustomAttributes.GetCustomAttributes (property, "System.Diagnostics.CodeAnalysis", "FeatureCheckAttribute")) {
				if (featureCheckAttribute.ConstructorArguments is not [CustomAttributeArgument { Value: TypeReference featureType }])
					continue;

				if (_context.TryResolve (featureType) is not TypeDefinition featureTypeDef)
					continue;

				if (IsFeatureDisabled (featureTypeDef))
					return true;
			}

			return false;

			bool IsFeatureDisabled (TypeDefinition featureType) {
				if (!featureSet.Add (featureType))
					return false;

				if (featureType.FullName == "System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute")
					return true;

				foreach (var featureSwitchDefinitionAttribute in _context.CustomAttributes.GetCustomAttributes (featureType, "System.Diagnostics.CodeAnalysis", "FeatureSwitchDefinitionAttribute")) {
					if (featureSwitchDefinitionAttribute.ConstructorArguments is not [CustomAttributeArgument { Value: string switchName }])
						continue;

					if (_context.FeatureSettings.TryGetValue (switchName, out bool featureSetting) && !featureSetting)
						return true;

					return false;
				}

				return false;
			}

			static PropertyDefinition? FindProperty (MethodDefinition method) {
				if (!method.IsGetter)
					return null;

				foreach (var property in method.DeclaringType.Properties) {
					if (property.GetMethod == method)
						return property;
				}

				return null;
			}
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
