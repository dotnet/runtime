// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using ILLink.RoslynAnalyzer;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace ILLink.CodeFix
{
	[ExportCodeFixProvider (LanguageNames.CSharp, Name = nameof (DynamicallyAccessedMembersCodeFixProvider)), Shared]
	public sealed class DynamicallyAccessedMembersCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
	{
		private static ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnostics ()
		{
			// Commented-out are currently unsupported Generic Parameter DiagnosticIDs
			var diagDescriptorsArrayBuilder = ImmutableArray.CreateBuilder<DiagnosticDescriptor> ();
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsField));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter));
			// diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsGenericParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsField));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter));
			// diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsGenericParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsMethodReturnType));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsField));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter));
			// diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsGenericParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsMethodReturnType));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsField));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsThisParameter));
			// diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsGenericParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsMethodReturnType));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsField));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsThisParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodParameterBetweenOverrides));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodReturnValueBetweenOverrides));
			// diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnImplicitThisBetweenOverrides));
			// diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnGenericParameterBetweenOverrides));

			return diagDescriptorsArrayBuilder.ToImmutable ();
		}

		public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => GetSupportedDiagnostics ();

		public sealed override ImmutableArray<string> FixableDiagnosticIds => SupportedDiagnostics.Select (dd => dd.Id).ToImmutableArray ();

		private static LocalizableString CodeFixTitle => new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersCodeFixTitle), Resources.ResourceManager, typeof (Resources));

		private static string FullyQualifiedAttributeName => DynamicallyAccessedMembersAnalyzer.FullyQualifiedDynamicallyAccessedMembersAttribute;

		private static readonly string[] AttributeOnReturn = {
			DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter.AsString (),
			DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType.AsString() ,
			DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsField.AsString (),
			DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter.AsString () ,
			DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodReturnValueBetweenOverrides.AsString ()
		};

		private static readonly string[] AttributeOnGeneric = {
			DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsParameter.AsString(),
			DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsMethodReturnType.AsString () ,
			DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsField.AsString(),
			DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsThisParameter.AsString(),
			DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter.AsString(),
			DiagnosticId.DynamicallyAccessedMembersMismatchOnGenericParameterBetweenOverrides.AsString()
		};

		public sealed override FixAllProvider GetFixAllProvider ()
		{
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override async Task RegisterCodeFixesAsync (CodeFixContext context)
		{
			var document = context.Document;
			var diagnostic = context.Diagnostics[0];
			var codeFixTitle = CodeFixTitle.ToString ();

			if (await document.GetSyntaxRootAsync (context.CancellationToken).ConfigureAwait (false) is not { } root)
				return;
			if (diagnostic.AdditionalLocations.Count == 0)
				return;
			if (root.FindNode (diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true) is not SyntaxNode targetNode)
				return;
			if (diagnostic.Properties["attributeArgument"] is not string stringArgs || stringArgs.Contains (","))
				return;

			context.RegisterCodeFix (CodeAction.Create (
				title: CodeFixTitle.ToString (),
				createChangedDocument: ct => AddAttributeAsync (
					document,
					targetNode,
					stringArgs,
					addAsReturnAttribute: AttributeOnReturn.Contains (diagnostic.Id),
					addGenericParameterAttribute: AttributeOnGeneric.Contains (diagnostic.Id),
					ct),
				equivalenceKey: codeFixTitle), diagnostic);
		}

		private static async Task<Document> AddAttributeAsync (
			Document document,
			SyntaxNode targetNode,
			string stringArguments,
			bool addAsReturnAttribute,
			bool addGenericParameterAttribute,
			CancellationToken cancellationToken)
		{
			if (await document.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false) is not { } model)
				return document;
			if (model.Compilation.GetBestTypeByMetadataName (FullyQualifiedAttributeName) is not { } attributeSymbol)
				return document;

			var editor = await DocumentEditor.CreateAsync (document, cancellationToken).ConfigureAwait (false);
			var generator = editor.Generator;
			var attributeArguments = new[] { generator.AttributeArgument (generator.MemberAccessExpression (generator.DottedName ("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes"), stringArguments)) };
			var attribute = generator.Attribute (
				generator.TypeExpression (attributeSymbol), attributeArguments)
				.WithAdditionalAnnotations (Simplifier.Annotation, Simplifier.AddImportsAnnotation);

			if (addAsReturnAttribute) {
				// don't use AddReturnAttribute because it's the same as AddAttribute https://github.com/dotnet/roslyn/pull/63084
				editor.ReplaceNode (targetNode, (d, g) => g.AddReturnAttributes (d, new[] { attribute }));
			} else if (addGenericParameterAttribute) {
				// AddReturnAttributes currently doesn't support adding attributes to type arguments https://github.com/dotnet/roslyn/pull/63292
				var newNode = (TypeParameterSyntax) targetNode;
				var nodeWithAttribute = newNode.AddAttributeLists ((AttributeListSyntax) attribute);
				editor.ReplaceNode (targetNode, nodeWithAttribute);
			} else {
				editor.AddAttribute (targetNode, attribute);
			}
			return editor.GetChangedDocument ();
		}
	}
}
