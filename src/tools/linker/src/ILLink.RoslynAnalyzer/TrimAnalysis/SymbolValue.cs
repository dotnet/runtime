// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public record SymbolValue : ValueWithDynamicallyAccessedMembers
	{
		public readonly ISymbol Source;
		public readonly bool IsMethodReturn;

		public SymbolValue (IMethodSymbol method, bool isMethodReturn) => (Source, IsMethodReturn) = (method, isMethodReturn);

		public SymbolValue (IParameterSymbol parameter) => Source = parameter;

		public SymbolValue (IFieldSymbol field) => Source = field;

		public SymbolValue (INamedTypeSymbol type) => Source = type;

		// This ctor isn't used for dataflow - it's really just a wrapper
		// for annotations on type arguments/parameters which are type-checked
		// by the analyzer (outside of the dataflow analysis).
		public SymbolValue (ITypeSymbol typeArgument) => Source = typeArgument;

		public SymbolValue (ITypeParameterSymbol typeParameter) => Source = typeParameter;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes {
			get {
				if (IsMethodReturn) {
					var method = (IMethodSymbol) Source;
					var returnDamt = method.GetDynamicallyAccessedMemberTypesOnReturnType ();
					// Is this a property getter?
					// If there are conflicts between the getter and the property annotation,
					// the getter annotation wins. (But DAMT.None is ignored)
					if (method.MethodKind is MethodKind.PropertyGet && returnDamt == DynamicallyAccessedMemberTypes.None) {
						var property = (IPropertySymbol) method.AssociatedSymbol!;
						Debug.Assert (property != null);
						returnDamt = property!.GetDynamicallyAccessedMemberTypes ();
					}

					return returnDamt;
				}

				var damt = Source.GetDynamicallyAccessedMemberTypes ();

				// Is this a property setter parameter?
				if (Source.Kind == SymbolKind.Parameter) {
					var parameterMethod = (IMethodSymbol) Source.ContainingSymbol;
					Debug.Assert (parameterMethod != null);
					// If there are conflicts between the setter and the property annotation,
					// the setter annotation wins. (But DAMT.None is ignored)
					if (parameterMethod!.MethodKind == MethodKind.PropertySet && damt == DynamicallyAccessedMemberTypes.None) {
						var property = (IPropertySymbol) parameterMethod.AssociatedSymbol!;
						Debug.Assert (property != null);
						damt = property!.GetDynamicallyAccessedMemberTypes ();
					}
				}

				return damt;
			}
		}

		public override string ToString ()
		{
			StringBuilder sb = new ();
			switch (Source) {
			case IMethodSymbol method:
				if (IsMethodReturn)
					sb.Append (method.Name);
				else
					sb.Append ("'this'");
				break;
			case IParameterSymbol param:
				sb.Append (param.Name);
				break;
			case IFieldSymbol field:
				sb.Append (field.Name);
				break;
			case INamedTypeSymbol:
				sb.Append ("type 'this'");
				break;
			default:
				throw new NotImplementedException (Source.GetType ().ToString ());
			}
			var damtStr = Annotations.GetMemberTypesString (DynamicallyAccessedMemberTypes);
			var memberTypesStr = damtStr.Split ('.')[1].TrimEnd ('\'');
			sb.Append ("[").Append (memberTypesStr).Append ("]");
			return sb.ToString ();
		}
	}
}