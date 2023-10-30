// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	internal readonly partial struct DiagnosticContext
	{
		public List<Diagnostic> Diagnostics { get; } = new ();

		readonly Location? Location { get; init; }

		public DiagnosticContext (Location location)
		{
			Location = location;
		}

		public static DiagnosticContext CreateDisabled () => new () { Location = null };

		public Diagnostic CreateDiagnostic (DiagnosticId id, params string[] args)
		{
			return Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (id), Location, args);
		}

		public void AddDiagnostic (Diagnostic diagnostic)
		{
			if (Location == null)
				return;

			Diagnostics.Add (diagnostic);
		}

		public partial void AddDiagnostic (DiagnosticId id, params string[] args)
		{
			if (Location == null)
				return;

			Diagnostics.Add (CreateDiagnostic (id, args));
		}

		public partial void AddDiagnostic (DiagnosticId id, ValueWithDynamicallyAccessedMembers actualValue, ValueWithDynamicallyAccessedMembers expectedAnnotationsValue, params string[] args)
		{
			if (Location == null)
				return;

			Diagnostics.Add (CreateDiagnostic (id, actualValue, expectedAnnotationsValue, args));
		}

		private Diagnostic CreateDiagnostic (DiagnosticId id, ValueWithDynamicallyAccessedMembers actualValue, ValueWithDynamicallyAccessedMembers expectedAnnotationsValue, params string[] args)
		{
			Debug.Assert (Location != null);

			if (actualValue is NullableValueWithDynamicallyAccessedMembers nv)
				actualValue = nv.UnderlyingTypeValue;

			ISymbol symbol = actualValue switch {
				FieldValue field => field.FieldSymbol,
				MethodParameterValue maybeThisParameter when maybeThisParameter.Parameter.IsImplicitThis => maybeThisParameter.MethodSymbol,
				MethodParameterValue methodParameter => methodParameter.Parameter.ParameterSymbol!,
				MethodReturnValue mrv => mrv.MethodSymbol,
				GenericParameterValue gpv => gpv.GenericParameter.TypeParameterSymbol,
				_ => throw new InvalidOperationException ()
			};

			Location[]? sourceLocation;
			Dictionary<string, string?>? DAMArgument = new Dictionary<string, string?> ();

			// not supporting merging differing attributes, check to make sure symbol has no other attributes
			if (symbol.DeclaringSyntaxReferences.Length == 0
					|| (actualValue is not MethodReturnValue
						&& symbol.TryGetAttribute (DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute, out var _))
					|| (actualValue is MethodReturnValue
						&& symbol is IMethodSymbol method
						&& method.TryGetReturnAttribute (DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute, out var _))) {
				sourceLocation = null;
				DAMArgument = null;
			} else {
				Location symbolLocation;
				symbolLocation = symbol.DeclaringSyntaxReferences[0].GetSyntax ().GetLocation ();
				DAMArgument.Add ("attributeArgument", expectedAnnotationsValue.DynamicallyAccessedMemberTypes.ToString ());
				sourceLocation = new Location[] { symbolLocation };
			}

			return Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (id), Location, sourceLocation, DAMArgument?.ToImmutableDictionary (), args);
		}
	}
}
