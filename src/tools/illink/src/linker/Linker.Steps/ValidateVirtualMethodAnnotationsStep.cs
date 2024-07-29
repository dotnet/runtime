// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using ILLink.Shared;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class ValidateVirtualMethodAnnotationsStep : BaseStep
	{
		protected override void Process ()
		{
			var annotations = Context.Annotations;
			foreach (var method in annotations.VirtualMethodsWithAnnotationsToValidate) {
				var baseOverrideInformations = annotations.GetBaseMethods (method);
				if (baseOverrideInformations != null) {
					foreach (var baseOv in baseOverrideInformations) {
						annotations.FlowAnnotations.ValidateMethodAnnotationsAreSame (baseOv);
						ValidateMethodRequiresUnreferencedCodeAreSame (baseOv);
					}
				}

				var overrides = annotations.GetOverrides (method);
				if (overrides != null) {
					foreach (var overrideInformation in overrides) {
						// Skip validation for cases where both base and override are in the list, we will validate the edge
						// when validating the override from the list.
						// This avoids validating the edge twice (it would produce the same warning twice)
						if (annotations.VirtualMethodsWithAnnotationsToValidate.Contains (overrideInformation.Override))
							continue;

						annotations.FlowAnnotations.ValidateMethodAnnotationsAreSame (overrideInformation);
						ValidateMethodRequiresUnreferencedCodeAreSame (overrideInformation);
					}
				}
			}
		}

		void ValidateMethodRequiresUnreferencedCodeAreSame (OverrideInformation ov)
		{
			var method = ov.Override;
			var baseMethod = ov.Base;
			var annotations = Context.Annotations;
			bool methodSatisfies = annotations.IsInRequiresUnreferencedCodeScope (method, out _);
			bool baseRequires = annotations.DoesMethodRequireUnreferencedCode (baseMethod, out _);
			if ((baseRequires && !methodSatisfies) || (!baseRequires && annotations.DoesMethodRequireUnreferencedCode (method, out _))) {
				string message = MessageFormat.FormatRequiresAttributeMismatch (
					methodSatisfies,
					baseMethod.DeclaringType.IsInterface,
					nameof (RequiresUnreferencedCodeAttribute),
					method.GetDisplayName (),
					baseMethod.GetDisplayName ());
				IMemberDefinition origin = (ov.IsOverrideOfInterfaceMember && ov.InterfaceImplementor.Implementor != method.DeclaringType)
					? ov.InterfaceImplementor.Implementor
					: method;
				Context.LogWarning (origin, DiagnosticId.RequiresUnreferencedCodeAttributeMismatch, message);
			}
		}
	}
}
