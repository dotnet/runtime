// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using ILLink.RoslynAnalyzer.TrimAnalysis;
using ILLink.Shared;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public class DynamicallyAccessedMembersAnalyzer : DiagnosticAnalyzer
	{
		internal const string DynamicallyAccessedMembers = nameof (DynamicallyAccessedMembers);
		internal const string DynamicallyAccessedMembersAttribute = nameof (DynamicallyAccessedMembersAttribute);

		static ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnostics ()
		{
			var diagDescriptorsArrayBuilder = ImmutableArray.CreateBuilder<DiagnosticDescriptor> (26);
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresUnreferencedCode));
			AddRange (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter, DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter);
			AddRange (DiagnosticId.MethodParameterCannotBeStaticallyDetermined, DiagnosticId.TypePassedToGenericParameterCannotBeStaticallyDetermined);
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersFieldAccessedViaReflection));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.UnrecognizedTypeInRuntimeHelpersRunClassConstructor));

			return diagDescriptorsArrayBuilder.ToImmutable ();

			void AddRange (DiagnosticId first, DiagnosticId last)
			{
				Debug.Assert ((int) first < (int) last);

				for (int i = (int) first;
					i <= (int) last; i++) {
					diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor ((DiagnosticId) i));
				}
			}
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => GetSupportedDiagnostics ();

		public override void Initialize (AnalysisContext context)
		{
			if (!System.Diagnostics.Debugger.IsAttached)
				context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.RegisterOperationBlockAction (context => {
				if (context.OwningSymbol.IsInRequiresUnreferencedCodeAttributeScope ())
					return;

				foreach (var operationBlock in context.OperationBlocks) {
					ControlFlowGraph cfg = context.GetControlFlowGraph (operationBlock);
					TrimDataFlowAnalysis trimDataFlowAnalysis = new (context, cfg);

					foreach (var diagnostic in trimDataFlowAnalysis.ComputeTrimAnalysisPatterns ().CollectDiagnostics ()) {
						context.ReportDiagnostic (diagnostic);
					}
				}
			});
			context.RegisterSyntaxNodeAction (context => {
				ProcessGenericParameters (context);
			}, SyntaxKind.GenericName);
		}

		static void ProcessGenericParameters (SyntaxNodeAnalysisContext context)
		{
			// RUC on the containing symbol normally silences warnings, but when examining a generic base type,
			// the containing symbol is the declared derived type. RUC on the derived type does not silence
			// warnings about base type arguments.
			if (context.ContainingSymbol is not null
				&& context.ContainingSymbol is not INamedTypeSymbol
				&& context.ContainingSymbol.IsInRequiresUnreferencedCodeAttributeScope ())
				return;

			ImmutableArray<ITypeParameterSymbol> typeParams = default;
			ImmutableArray<ITypeSymbol> typeArgs = default;
			var symbol = context.SemanticModel.GetSymbolInfo (context.Node).Symbol;
			switch (symbol) {
			case INamedTypeSymbol type:
				// INamedTypeSymbol inside nameof, commonly used to access the string value of a variable, type, or a memeber,
				// can generate diagnostics warnings, which can be noisy and unhelpful. 
				// Walking the node heirarchy to check if INamedTypeSymbol is inside a nameof to not generate diagnostics
				var parentNode = context.Node;
				while (parentNode != null) {
					if (parentNode is InvocationExpressionSyntax invocationExpression && invocationExpression.Expression is IdentifierNameSyntax ident1) {
						if (ident1.Identifier.ValueText.Equals ("nameof"))
							return;
					}
					parentNode = parentNode.Parent;
				}
				typeParams = type.TypeParameters;
				typeArgs = type.TypeArguments;
				break;
			case IMethodSymbol targetMethod:
				typeParams = targetMethod.TypeParameters;
				typeArgs = targetMethod.TypeArguments;
				break;
			}

			if (typeParams != null) {
				Debug.Assert (typeParams.Length == typeArgs.Length);

				for (int i = 0; i < typeParams.Length; i++) {
					// Syntax like typeof (Foo<>) will have an ErrorType as the type argument.
					// These uninstantiated generics should not produce warnings.
					if (typeArgs[i].Kind == SymbolKind.ErrorType)
						continue;
					var sourceValue = GetTypeValueNodeFromGenericArgument (context, typeArgs[i]);
					var targetValue = new GenericParameterValue (typeParams[i]);
					foreach (var diagnostic in GetDynamicallyAccessedMembersDiagnostics (sourceValue, targetValue, context.Node.GetLocation ()))
						context.ReportDiagnostic (diagnostic);
				}
			}
		}

		static SingleValue GetTypeValueNodeFromGenericArgument (SyntaxNodeAnalysisContext context, ITypeSymbol type)
		{
			return type.Kind switch {
				SymbolKind.TypeParameter => new GenericParameterValue ((ITypeParameterSymbol) type),
				// Technically this should be a new value node type as it's not a System.Type instance representation, but just the generic parameter
				// That said we only use it to perform the dynamically accessed members checks and for that purpose treating it as System.Type is perfectly valid.
				SymbolKind.NamedType => new SystemTypeValue (new TypeProxy ((INamedTypeSymbol) type)),
				SymbolKind.ErrorType => UnknownValue.Instance,
				SymbolKind.ArrayType => new SystemTypeValue (new TypeProxy (context.Compilation.GetTypeByMetadataName ("System.Array")!)),
				// What about things like PointerType and so on. Linker treats these as "named types" since it can resolve them to concrete type
				_ => throw new NotImplementedException ()
			};
		}

		static IEnumerable<Diagnostic> GetDynamicallyAccessedMembersDiagnostics (SingleValue sourceValue, SingleValue targetValue, Location location)
		{
			// The target should always be an annotated value, but the visitor design currently prevents
			// declaring this in the type system.
			if (targetValue is not ValueWithDynamicallyAccessedMembers targetWithDynamicallyAccessedMembers)
				throw new NotImplementedException ();

			var diagnosticContext = new DiagnosticContext (location);
			var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction (diagnosticContext, new ReflectionAccessAnalyzer ());
			requireDynamicallyAccessedMembersAction.Invoke (sourceValue, targetWithDynamicallyAccessedMembers);

			return diagnosticContext.Diagnostics;
		}
	}
}
