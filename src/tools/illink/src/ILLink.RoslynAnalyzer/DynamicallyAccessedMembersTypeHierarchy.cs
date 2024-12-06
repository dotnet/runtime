// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using ILLink.RoslynAnalyzer.TrimAnalysis;

namespace ILLink.RoslynAnalyzer
{
	sealed class DynamicallyAccessedMembersTypeHierarchy
	{
		public static void ApplyDynamicallyAccessedMembersToTypeHierarchy (Location typeLocation, INamedTypeSymbol type, Action<Diagnostic> reportDiagnostic)
		{
			var annotation = FlowAnnotations.GetTypeAnnotation (type);

			// We need to apply annotations to this type, and its base/interface types (recursively)
			// But the annotations on base/interfaces may already be applied so we don't need to apply those
			// again (and should avoid doing so as it would produce extra warnings).
			var reflectionAccessAnalyzer = new ReflectionAccessAnalyzer (reportDiagnostic, type);
			if (type.BaseType is INamedTypeSymbol baseType) {
				var baseAnnotation = FlowAnnotations.GetTypeAnnotation (baseType);
				var annotationToApplyToBase = Annotations.GetMissingMemberTypes (annotation, baseAnnotation);

				// Apply any annotations that didn't exist on the base type to the base type.
				// This may produce redundant warnings when the annotation is DAMT.All or DAMT.PublicConstructors and the base already has a
				// subset of those annotations.
				reflectionAccessAnalyzer.GetReflectionAccessDiagnostics (typeLocation, baseType, annotationToApplyToBase, declaredOnly: false);
			}

			// Most of the DynamicallyAccessedMemberTypes don't select members on interfaces. We only need to apply
			// annotations to interfaces separately if dealing with DAMT.All or DAMT.Interfaces.
			if (annotation.HasFlag (DynamicallyAccessedMemberTypes.Interfaces))
			{
				var annotationToApplyToInterfaces = annotation == DynamicallyAccessedMemberTypes.All ? annotation : DynamicallyAccessedMemberTypes.Interfaces;
				foreach (var iface in type.AllInterfaces) {
					if (FlowAnnotations.GetTypeAnnotation (iface).HasFlag (annotationToApplyToInterfaces))
						continue;

					// Apply All or Interfaces to the interface type.
					// DAMT.All may produce redundant warnings from implementing types, when the interface type already had some annotations.
					reflectionAccessAnalyzer.GetReflectionAccessDiagnostics (typeLocation, iface, annotationToApplyToInterfaces, declaredOnly: false);
				}
			}

			// The annotations this type inherited from its base types or interfaces should not produce
			// warnings on the respective base/interface members, since those are already covered by applying
			// the annotations to those types. So we only need to handle the members directly declared on this type.
			reflectionAccessAnalyzer.GetReflectionAccessDiagnostics (typeLocation, type, annotation, declaredOnly: true);
		}
	}
}
