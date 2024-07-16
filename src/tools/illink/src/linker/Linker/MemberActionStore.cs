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
		private readonly Dictionary<MethodDefinition, bool> _featureCheckValues;
		readonly LinkContext _context;

		public MemberActionStore (LinkContext context)
		{
			PrimarySubstitutionInfo = new SubstitutionInfo ();
			_embeddedXmlInfos = new Dictionary<AssemblyDefinition, SubstitutionInfo?> ();
			_featureCheckValues = new Dictionary<MethodDefinition, bool> ();
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

			if (TryGetFeatureCheckValue (method, out _))
				return MethodAction.ConvertToStub;

			return MethodAction.Nothing;
		}

		public bool TryGetMethodStubValue (MethodDefinition method, out object? value)
		{
			if (PrimarySubstitutionInfo.MethodStubValues.TryGetValue (method, out value))
				return true;

			if (TryGetSubstitutionInfo (method, out var embeddedXml)
				&& embeddedXml.MethodStubValues.TryGetValue (method, out value))
				return true;

			if (TryGetFeatureCheckValue (method, out bool bValue)) {
				value = bValue ? 1 : 0;
				return true;
			}

			return false;
		}

		internal bool TryGetFeatureCheckValue (MethodDefinition method, out bool value)
		{
			if (_featureCheckValues.TryGetValue (method, out value))
				return true;

			value = false;

			if (!method.IsStatic)
				return false;

			if (method.ReturnType.MetadataType != MetadataType.Boolean)
				return false;

			if (FindProperty (method) is not PropertyDefinition property)
				return false;

			if (property.SetMethod != null)
				return false;

			foreach (var featureSwitchDefinitionAttribute in _context.CustomAttributes.GetCustomAttributes (property, "System.Diagnostics.CodeAnalysis", "FeatureSwitchDefinitionAttribute")) {
				if (featureSwitchDefinitionAttribute.ConstructorArguments is not [CustomAttributeArgument { Value: string switchName }])
					continue;

				// If there's a FeatureSwitchDefinition, don't continue looking for FeatureGuard.
				// We don't want to infer feature switch settings from FeatureGuard.
				if (_context.FeatureSettings.TryGetValue (switchName, out value)) {
					_featureCheckValues[method] = value;
					return true;
				}
				return false;
			}

			if (!_context.IsOptimizationEnabled (CodeOptimizations.SubstituteFeatureGuards, method))
				return false;

			foreach (var featureGuardAttribute in _context.CustomAttributes.GetCustomAttributes (property, "System.Diagnostics.CodeAnalysis", "FeatureGuardAttribute")) {
				if (featureGuardAttribute.ConstructorArguments is not [CustomAttributeArgument { Value: TypeReference featureType }])
					continue;

				if (featureType.Namespace == "System.Diagnostics.CodeAnalysis") {
					switch (featureType.Name) {
					case "RequiresUnreferencedCodeAttribute":
						_featureCheckValues[method] = value;
						return true;
					case "RequiresDynamicCodeAttribute":
						if (_context.FeatureSettings.TryGetValue (
								"System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported",
								out bool isDynamicCodeSupported)
							&& !isDynamicCodeSupported) {
							_featureCheckValues[method] = value;
							return true;
							}
						break;
					}
				}
			}

			return false;

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
