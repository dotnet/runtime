// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes =>
			IsMethodReturn
				? ((IMethodSymbol) Source).GetDynamicallyAccessedMemberTypesOnReturnType ()
				: Source.GetDynamicallyAccessedMemberTypes ();

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