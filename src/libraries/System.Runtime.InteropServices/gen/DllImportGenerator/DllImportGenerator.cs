using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    [Generator]
    public class DllImportGenerator : ISourceGenerator
    {
        private const string GeneratedDllImport = nameof(GeneratedDllImport);
        private const string GeneratedDllImportAttribute = nameof(GeneratedDllImportAttribute);

        public void Execute(GeneratorExecutionContext context)
        {
            var synRec = context.SyntaxReceiver as SyntaxReceiver;
            if (synRec is null)
            {
                return;
            }

            // Store a mapping between SyntaxTree and SemanticModel.
            // SemanticModels cache results and since we could be looking at
            // method declarations in the same SyntaxTree we want to benefit from
            // this caching.
            var syntaxToModel = new Dictionary<SyntaxTree, SemanticModel>();

            var generatorDiagnostics = new GeneratorDiagnostics(context);

            var generatedDllImports = new StringBuilder();
            foreach (SyntaxReference synRef in synRec.Methods)
            {
                var methodSyntax = (MethodDeclarationSyntax)synRef.GetSyntax(context.CancellationToken);

                // Get the model for the method.
                if (!syntaxToModel.TryGetValue(methodSyntax.SyntaxTree, out SemanticModel sm))
                {
                    sm = context.Compilation.GetSemanticModel(methodSyntax.SyntaxTree, ignoreAccessibility: true);
                    syntaxToModel.Add(methodSyntax.SyntaxTree, sm);
                }

                // Process the method syntax and get its SymbolInfo.
                var methodSymbolInfo = sm.GetDeclaredSymbol(methodSyntax, context.CancellationToken);

                // Process the attributes on the method.
                DllImportStub.GeneratedDllImportData dllImportData;
                AttributeSyntax dllImportAttr = this.ProcessAttributes(methodSymbolInfo, context.CancellationToken, out dllImportData);
                Debug.Assert(!(dllImportAttr is null) && !(dllImportData is null));

                // Create the stub.
                var dllImportStub = DllImportStub.Create(methodSymbolInfo, dllImportData, context.Compilation, generatorDiagnostics, context.CancellationToken);

                PrintGeneratedSource(generatedDllImports, methodSyntax, dllImportStub, dllImportAttr);
            }

            Debug.WriteLine(generatedDllImports.ToString()); // [TODO] Find some way to emit this for debugging - logs?
            context.AddSource("DllImportGenerator.g.cs", SourceText.From(generatedDllImports.ToString(), Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        private void PrintGeneratedSource(
            StringBuilder builder,
            MethodDeclarationSyntax userDeclaredMethod,
            DllImportStub stub,
            AttributeSyntax dllImportAttr)
        {
            // Create stub function
            var stubMethod = MethodDeclaration(stub.StubReturnType, userDeclaredMethod.Identifier)
                .WithModifiers(userDeclaredMethod.Modifiers)
                .WithParameterList(ParameterList(SeparatedList(stub.StubParameters)))
                .WithBody(stub.StubCode);

            // Create the DllImport declaration.
            // [TODO] Don't include PreserveSig=false once that is handled by the generated stub
            var dllImport = stub.DllImportDeclaration.AddAttributeLists(
                AttributeList(
                    SingletonSeparatedList<AttributeSyntax>(dllImportAttr)));

            // Stub should have at least one containing type
            Debug.Assert(stub.StubContainingTypes.Any());

            // Add stub function and DllImport declaration to the first (innermost) containing
            MemberDeclarationSyntax containingType = stub.StubContainingTypes.First()
                .AddMembers(stubMethod, dllImport);

            // Add type to the remaining containing types (skipping the first which was handled above)
            foreach (var typeDecl in stub.StubContainingTypes.Skip(1))
            {
                containingType = typeDecl.WithMembers(
                    SingletonList<MemberDeclarationSyntax>(containingType));
            }

            MemberDeclarationSyntax toPrint = containingType;

            // Add type to the containing namespace
            if (!(stub.StubTypeNamespace is null))
            {
                toPrint = NamespaceDeclaration(IdentifierName(stub.StubTypeNamespace))
                    .AddMembers(toPrint);
            }

            builder.AppendLine(toPrint.NormalizeWhitespace().ToString());
        }

        private static bool IsGeneratedDllImportAttribute(AttributeSyntax attrSyntaxMaybe)
        {
            var attrName = attrSyntaxMaybe.Name.ToString();

            if (attrName.Length == GeneratedDllImport.Length)
            {
                return attrName.Equals(GeneratedDllImport);
            }
            else if (attrName.Length == GeneratedDllImportAttribute.Length)
            {
                return attrName.Equals(GeneratedDllImportAttribute);
            }

            // Handle the case where the user defines an attribute with
            // the same name but adds a prefix.
            const string PrefixedGeneratedDllImport = "." + GeneratedDllImport;
            const string PrefixedGeneratedDllImportAttribute = "." + GeneratedDllImportAttribute;
            return attrName.EndsWith(PrefixedGeneratedDllImport)
                || attrName.EndsWith(PrefixedGeneratedDllImportAttribute);
        }

        private AttributeSyntax ProcessAttributes(
            IMethodSymbol method,
            CancellationToken cancelToken,
            out DllImportStub.GeneratedDllImportData dllImportData)
        {
            dllImportData = new DllImportStub.GeneratedDllImportData();

            // Process all attributes
            foreach (AttributeData attrData in method.GetAttributes())
            {
                if (attrData.ApplicationSyntaxReference is null)
                {
                    continue;
                }

                var attrSyntax = (AttributeSyntax)attrData.ApplicationSyntaxReference.GetSyntax(cancelToken);

                // Skip the attribute if not GeneratedDllImport.
                if (!IsGeneratedDllImportAttribute(attrSyntax))
                {
                    continue;
                }

                // Found the GeneratedDllImport, but it has an error so report the error.
                // This is most likely an issue with targeting an incorrect TFM.
                if (attrData.AttributeClass.TypeKind == TypeKind.Error)
                {
                    // [TODO] Report GeneratedDllImport has an error - corrupt metadata?
                    throw new InvalidProgramException();
                }

                var newAttributeArgs = new List<AttributeArgumentSyntax>();

                // Populate the DllImport data from the GeneratedDllImportAttribute attribute.
                dllImportData.ModuleName = attrData.ConstructorArguments[0].Value.ToString();

                newAttributeArgs.Add(SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(dllImportData.ModuleName))));

                // All other data on attribute is defined as NamedArguments.
                foreach (var namedArg in attrData.NamedArguments)
                {
                    ExpressionSyntax expSyntaxMaybe = null;
                    switch (namedArg.Key)
                    {
                        default:
                            Debug.Fail($"An unknown member was found on {GeneratedDllImport}");
                            continue;
                        case nameof(DllImportStub.GeneratedDllImportData.BestFitMapping):
                            dllImportData.BestFitMapping = (bool)namedArg.Value.Value;
                            expSyntaxMaybe = CreateBoolExpressionSyntax(dllImportData.BestFitMapping);
                            dllImportData.IsUserDefined |= DllImportStub.DllImportMember.BestFitMapping;
                            break;
                        case nameof(DllImportStub.GeneratedDllImportData.CallingConvention):
                            dllImportData.CallingConvention = (CallingConvention)namedArg.Value.Value;
                            expSyntaxMaybe = CreateEnumExpressionSyntax(dllImportData.CallingConvention);
                            dllImportData.IsUserDefined |= DllImportStub.DllImportMember.CallingConvention;
                            break;
                        case nameof(DllImportStub.GeneratedDllImportData.CharSet):
                            dllImportData.CharSet = (CharSet)namedArg.Value.Value;
                            expSyntaxMaybe = CreateEnumExpressionSyntax(dllImportData.CharSet);
                            dllImportData.IsUserDefined |= DllImportStub.DllImportMember.CharSet;
                            break;
                        case nameof(DllImportStub.GeneratedDllImportData.EntryPoint):
                            dllImportData.EntryPoint = (string)namedArg.Value.Value;
                            expSyntaxMaybe = CreateStringExpressionSyntax(dllImportData.EntryPoint);
                            dllImportData.IsUserDefined |= DllImportStub.DllImportMember.EntryPoint;
                            break;
                        case nameof(DllImportStub.GeneratedDllImportData.ExactSpelling):
                            dllImportData.ExactSpelling = (bool)namedArg.Value.Value;
                            expSyntaxMaybe = CreateBoolExpressionSyntax(dllImportData.ExactSpelling);
                            dllImportData.IsUserDefined |= DllImportStub.DllImportMember.ExactSpelling;
                            break;
                        case nameof(DllImportStub.GeneratedDllImportData.PreserveSig):
                            dllImportData.PreserveSig = (bool)namedArg.Value.Value;
                            expSyntaxMaybe = CreateBoolExpressionSyntax(dllImportData.PreserveSig);
                            dllImportData.IsUserDefined |= DllImportStub.DllImportMember.PreserveSig;
                            break;
                        case nameof(DllImportStub.GeneratedDllImportData.SetLastError):
                            dllImportData.SetLastError = (bool)namedArg.Value.Value;
                            expSyntaxMaybe = CreateBoolExpressionSyntax(dllImportData.SetLastError);
                            dllImportData.IsUserDefined |= DllImportStub.DllImportMember.SetLastError;
                            break;
                        case nameof(DllImportStub.GeneratedDllImportData.ThrowOnUnmappableChar):
                            dllImportData.ThrowOnUnmappableChar = (bool)namedArg.Value.Value;
                            expSyntaxMaybe = CreateBoolExpressionSyntax(dllImportData.ThrowOnUnmappableChar);
                            dllImportData.IsUserDefined |= DllImportStub.DllImportMember.ThrowOnUnmappableChar;
                            break;
                    }

                    Debug.Assert(!(expSyntaxMaybe is null));

                    // Defer the name equals syntax till we know the value means something. If we created
                    // an expression we know the key value was valid.
                    NameEqualsSyntax nameSyntax = SyntaxFactory.NameEquals(namedArg.Key);
                    newAttributeArgs.Add(SyntaxFactory.AttributeArgument(nameSyntax, null, expSyntaxMaybe));
                }

                // If the EntryPoint property is not set, we will compute and
                // add it based on existing semantics (i.e. method name).
                //
                // N.B. The export discovery logic is identical regardless of where
                // the name is defined (i.e. method name vs EntryPoint property).
                if (!dllImportData.IsUserDefined.HasFlag(DllImportStub.DllImportMember.EntryPoint))
                {
                    var entryPointName = SyntaxFactory.NameEquals(nameof(DllImportAttribute.EntryPoint));

                    // The name of the method is the entry point name to use.
                    var entryPointValue = CreateStringExpressionSyntax(method.Name);
                    newAttributeArgs.Add(SyntaxFactory.AttributeArgument(entryPointName, null, entryPointValue));
                }

                // Create new attribute
                return SyntaxFactory.Attribute(
                    SyntaxFactory.ParseName(typeof(DllImportAttribute).FullName),
                    SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(newAttributeArgs)));
            }

            // [TODO] Report the missing GeneratedDllImportAttribute
            throw new NotSupportedException();

            static ExpressionSyntax CreateBoolExpressionSyntax(bool trueOrFalse)
            {
                return SyntaxFactory.LiteralExpression(
                    trueOrFalse
                        ? SyntaxKind.TrueLiteralExpression
                        : SyntaxKind.FalseLiteralExpression);
            }

            static ExpressionSyntax CreateStringExpressionSyntax(string str)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(str));
            }

            static ExpressionSyntax CreateEnumExpressionSyntax<T>(T value) where T : Enum
            {
                return SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(typeof(T).FullName),
                    SyntaxFactory.IdentifierName(value.ToString()));
            }
        }

        private class SyntaxReceiver : ISyntaxReceiver
        {
            public ICollection<SyntaxReference> Methods { get; } = new List<SyntaxReference>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // We only support C# method declarations.
                if (syntaxNode.Language != LanguageNames.CSharp
                    || !syntaxNode.IsKind(SyntaxKind.MethodDeclaration))
                {
                    return;
                }

                var methodSyntax = (MethodDeclarationSyntax)syntaxNode;

                // Verify the method has no generic types or defined implementation
                // and is marked static and partial.
                if (!(methodSyntax.TypeParameterList is null)
                    || !(methodSyntax.Body is null)
                    || !methodSyntax.Modifiers.Any(SyntaxKind.StaticKeyword)
                    || !methodSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return;
                }

                // Check if the method is marked with the GeneratedDllImport attribute.
                foreach (AttributeListSyntax listSyntax in methodSyntax.AttributeLists)
                {
                    foreach (AttributeSyntax attrSyntax in listSyntax.Attributes)
                    {
                        if (IsGeneratedDllImportAttribute(attrSyntax))
                        {
                            this.Methods.Add(syntaxNode.GetReference());
                            return;
                        }
                    }
                }
            }
        }
    }
}
