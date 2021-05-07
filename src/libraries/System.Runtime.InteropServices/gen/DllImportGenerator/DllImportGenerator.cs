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

        private static readonly Version MinimumSupportedFrameworkVersion = new Version(5, 0);

        public void Execute(GeneratorExecutionContext context)
        {
            var synRec = context.SyntaxReceiver as SyntaxReceiver;
            if (synRec is null || !synRec.Methods.Any())
            {
                return;
            }

            // Get the symbol for GeneratedDllImportAttribute. If it doesn't exist in the compilation, the generator has nothing to do.
            INamedTypeSymbol? generatedDllImportAttrType = context.Compilation.GetTypeByMetadataName(TypeNames.GeneratedDllImportAttribute);
            if (generatedDllImportAttrType == null)
                return;

            INamedTypeSymbol? lcidConversionAttrType = context.Compilation.GetTypeByMetadataName(TypeNames.LCIDConversionAttribute);

            // Fire the start/stop pair for source generation
            using var _ = Diagnostics.Events.SourceGenerationStartStop(synRec.Methods.Count);

            // Store a mapping between SyntaxTree and SemanticModel.
            // SemanticModels cache results and since we could be looking at
            // method declarations in the same SyntaxTree we want to benefit from
            // this caching.
            var syntaxToModel = new Dictionary<SyntaxTree, SemanticModel>();

            var generatorDiagnostics = new GeneratorDiagnostics(context);

            Version targetFrameworkVersion;
            bool isSupported = IsSupportedTargetFramework(context.Compilation, out targetFrameworkVersion);
            if (!isSupported)
            {
                // We don't return early here, letting the source generation continue.
                // This allows a user to copy generated source and use it as a starting point
                // for manual marshalling if desired.
                generatorDiagnostics.ReportTargetFrameworkNotSupported(MinimumSupportedFrameworkVersion);
            }

            var env = new StubEnvironment(context.Compilation, isSupported, targetFrameworkVersion, context.AnalyzerConfigOptions.GlobalOptions);
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
                var methodSymbolInfo = sm.GetDeclaredSymbol(methodSyntax, context.CancellationToken)!;

                // Get any attributes of interest on the method
                AttributeData? generatedDllImportAttr = null;
                AttributeData? lcidConversionAttr = null;
                foreach (var attr in methodSymbolInfo.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, generatedDllImportAttrType))
                    {
                        generatedDllImportAttr = attr;
                    }
                    else if (lcidConversionAttrType != null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, lcidConversionAttrType))
                    {
                        lcidConversionAttr = attr;
                    }
                }

                if (generatedDllImportAttr == null)
                    continue;

                // Process the GeneratedDllImport attribute
                DllImportStub.GeneratedDllImportData stubDllImportData = this.ProcessGeneratedDllImportAttribute(generatedDllImportAttr);
                Debug.Assert(stubDllImportData is not null);
                AttributeSyntax dllImportAttr = this.CreateDllImportAttributeForTarget(stubDllImportData!, env.Options.GenerateForwarders(), methodSymbolInfo.Name);

                if (stubDllImportData!.IsUserDefined.HasFlag(DllImportStub.DllImportMember.BestFitMapping))
                {
                    generatorDiagnostics.ReportConfigurationNotSupported(generatedDllImportAttr, nameof(DllImportStub.GeneratedDllImportData.BestFitMapping));
                }

                if (stubDllImportData!.IsUserDefined.HasFlag(DllImportStub.DllImportMember.ThrowOnUnmappableChar))
                {
                    generatorDiagnostics.ReportConfigurationNotSupported(generatedDllImportAttr, nameof(DllImportStub.GeneratedDllImportData.ThrowOnUnmappableChar));
                }

                if (lcidConversionAttr != null)
                {
                    // Using LCIDConversion with GeneratedDllImport is not supported
                    generatorDiagnostics.ReportConfigurationNotSupported(lcidConversionAttr, nameof(TypeNames.LCIDConversionAttribute));
                }

                // Create the stub.
                var dllImportStub = DllImportStub.Create(methodSymbolInfo, stubDllImportData!, env, generatorDiagnostics, context.CancellationToken);

                PrintGeneratedSource(generatedDllImports, methodSyntax, dllImportStub, dllImportAttr!);
            }

            Debug.WriteLine(generatedDllImports.ToString()); // [TODO] Find some way to emit this for debugging - logs?
            context.AddSource("DllImportGenerator.g.cs", SourceText.From(generatedDllImports.ToString(), Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        private SyntaxTokenList StripTriviaFromModifiers(SyntaxTokenList tokenList)
        {
            SyntaxToken[] strippedTokens = new SyntaxToken[tokenList.Count];
            for (int i = 0; i < tokenList.Count; i++)
            {
                strippedTokens[i] = tokenList[i].WithoutTrivia();
            }
            return new SyntaxTokenList(strippedTokens);
        }

        private TypeDeclarationSyntax CreateTypeDeclarationWithoutTrivia(TypeDeclarationSyntax typeDeclaration)
        {
            return TypeDeclaration(
                typeDeclaration.Kind(),
                typeDeclaration.Identifier)
                .WithTypeParameterList(typeDeclaration.TypeParameterList)
                .WithModifiers(typeDeclaration.Modifiers);
        }

        private void PrintGeneratedSource(
            StringBuilder builder,
            MethodDeclarationSyntax userDeclaredMethod,
            DllImportStub stub,
            AttributeSyntax dllImportAttr)
        {
            // Create stub function
            var stubMethod = MethodDeclaration(stub.StubReturnType, userDeclaredMethod.Identifier)
                .AddAttributeLists(stub.AdditionalAttributes)
                .WithModifiers(StripTriviaFromModifiers(userDeclaredMethod.Modifiers))
                .WithParameterList(ParameterList(SeparatedList(stub.StubParameters)))
                .WithBody(stub.StubCode);

            // Create the DllImport declaration.
            var dllImport = stub.DllImportDeclaration.AddAttributeLists(
                AttributeList(
                    SingletonSeparatedList(dllImportAttr)));

            // Stub should have at least one containing type
            Debug.Assert(stub.StubContainingTypes.Any());

            // Add stub function and DllImport declaration to the first (innermost) containing
            MemberDeclarationSyntax containingType = CreateTypeDeclarationWithoutTrivia(stub.StubContainingTypes.First())
                .AddMembers(stubMethod, dllImport);

            // Add type to the remaining containing types (skipping the first which was handled above)
            foreach (var typeDecl in stub.StubContainingTypes.Skip(1))
            {
                containingType = CreateTypeDeclarationWithoutTrivia(typeDecl)
                    .WithMembers(SingletonList(containingType));
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

        private static bool IsSupportedTargetFramework(Compilation compilation, out Version version)
        {
            IAssemblySymbol systemAssembly = compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly;
            version = systemAssembly.Identity.Version;

            return systemAssembly.Identity.Name switch
            {
                // .NET Framework
                "mscorlib" => false,
                // .NET Standard
                "netstandard" => false,
                // .NET Core (when version < 5.0) or .NET
                "System.Runtime" or "System.Private.CoreLib" => version >= MinimumSupportedFrameworkVersion,
                _ => false,
            };
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

        private DllImportStub.GeneratedDllImportData ProcessGeneratedDllImportAttribute(AttributeData attrData)
        {
            var stubDllImportData = new DllImportStub.GeneratedDllImportData();

            // Found the GeneratedDllImport, but it has an error so report the error.
            // This is most likely an issue with targeting an incorrect TFM.
            if (attrData.AttributeClass?.TypeKind is null or TypeKind.Error)
            {
                // [TODO] Report GeneratedDllImport has an error - corrupt metadata?
                throw new InvalidProgramException();
            }

            // Populate the DllImport data from the GeneratedDllImportAttribute attribute.
            stubDllImportData.ModuleName = attrData.ConstructorArguments[0].Value!.ToString();

            // All other data on attribute is defined as NamedArguments.
            foreach (var namedArg in attrData.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    default:
                        Debug.Fail($"An unknown member was found on {GeneratedDllImport}");
                        continue;
                    case nameof(DllImportStub.GeneratedDllImportData.BestFitMapping):
                        stubDllImportData.BestFitMapping = (bool)namedArg.Value.Value!;
                        stubDllImportData.IsUserDefined |= DllImportStub.DllImportMember.BestFitMapping;
                        break;
                    case nameof(DllImportStub.GeneratedDllImportData.CallingConvention):
                        stubDllImportData.CallingConvention = (CallingConvention)namedArg.Value.Value!;
                        stubDllImportData.IsUserDefined |= DllImportStub.DllImportMember.CallingConvention;
                        break;
                    case nameof(DllImportStub.GeneratedDllImportData.CharSet):
                        stubDllImportData.CharSet = (CharSet)namedArg.Value.Value!;
                        stubDllImportData.IsUserDefined |= DllImportStub.DllImportMember.CharSet;
                        break;
                    case nameof(DllImportStub.GeneratedDllImportData.EntryPoint):
                        stubDllImportData.EntryPoint = (string)namedArg.Value.Value!;
                        stubDllImportData.IsUserDefined |= DllImportStub.DllImportMember.EntryPoint;
                        break;
                    case nameof(DllImportStub.GeneratedDllImportData.ExactSpelling):
                        stubDllImportData.ExactSpelling = (bool)namedArg.Value.Value!;
                        stubDllImportData.IsUserDefined |= DllImportStub.DllImportMember.ExactSpelling;
                        break;
                    case nameof(DllImportStub.GeneratedDllImportData.PreserveSig):
                        stubDllImportData.PreserveSig = (bool)namedArg.Value.Value!;
                        stubDllImportData.IsUserDefined |= DllImportStub.DllImportMember.PreserveSig;
                        break;
                    case nameof(DllImportStub.GeneratedDllImportData.SetLastError):
                        stubDllImportData.SetLastError = (bool)namedArg.Value.Value!;
                        stubDllImportData.IsUserDefined |= DllImportStub.DllImportMember.SetLastError;
                        break;
                    case nameof(DllImportStub.GeneratedDllImportData.ThrowOnUnmappableChar):
                        stubDllImportData.ThrowOnUnmappableChar = (bool)namedArg.Value.Value!;
                        stubDllImportData.IsUserDefined |= DllImportStub.DllImportMember.ThrowOnUnmappableChar;
                        break;
                }
            }

            return stubDllImportData;
        }

        private AttributeSyntax CreateDllImportAttributeForTarget(DllImportStub.GeneratedDllImportData stubDllImportData, bool generateForwarders, string originalMethodName)
        {
            DllImportStub.GeneratedDllImportData targetDllImportData = 
                GetTargetDllImportDataFromStubData(stubDllImportData, generateForwarders, originalMethodName);

            var newAttributeArgs = new List<AttributeArgumentSyntax>
            {
                AttributeArgument(LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    Literal(targetDllImportData.ModuleName))),
                AttributeArgument(
                    NameEquals(nameof(DllImportAttribute.EntryPoint)),
                    null,
                    CreateStringExpressionSyntax(targetDllImportData.EntryPoint))
            };

            if (targetDllImportData.IsUserDefined.HasFlag(DllImportStub.DllImportMember.BestFitMapping))
            {
                var name = NameEquals(nameof(DllImportAttribute.BestFitMapping));
                var value = CreateBoolExpressionSyntax(targetDllImportData.BestFitMapping);
                newAttributeArgs.Add(AttributeArgument(name, null, value));
            }
            if (targetDllImportData.IsUserDefined.HasFlag(DllImportStub.DllImportMember.CallingConvention))
            {
                var name = NameEquals(nameof(DllImportAttribute.CallingConvention));
                var value = CreateEnumExpressionSyntax(targetDllImportData.CallingConvention);
                newAttributeArgs.Add(AttributeArgument(name, null, value));
            }
            if (targetDllImportData.IsUserDefined.HasFlag(DllImportStub.DllImportMember.CharSet))
            {
                var name = NameEquals(nameof(DllImportAttribute.CharSet));
                var value = CreateEnumExpressionSyntax(targetDllImportData.CharSet);
                newAttributeArgs.Add(AttributeArgument(name, null, value));
            }
            if (targetDllImportData.IsUserDefined.HasFlag(DllImportStub.DllImportMember.ExactSpelling))
            {
                var name = NameEquals(nameof(DllImportAttribute.ExactSpelling));
                var value = CreateBoolExpressionSyntax(targetDllImportData.ExactSpelling);
                newAttributeArgs.Add(AttributeArgument(name, null, value));
            }
            if (targetDllImportData.IsUserDefined.HasFlag(DllImportStub.DllImportMember.PreserveSig))
            {
                var name = NameEquals(nameof(DllImportAttribute.PreserveSig));
                var value = CreateBoolExpressionSyntax(targetDllImportData.PreserveSig);
                newAttributeArgs.Add(AttributeArgument(name, null, value));
            }
            if (targetDllImportData.IsUserDefined.HasFlag(DllImportStub.DllImportMember.SetLastError))
            {
                var name = NameEquals(nameof(DllImportAttribute.SetLastError));
                var value = CreateBoolExpressionSyntax(targetDllImportData.SetLastError);
                newAttributeArgs.Add(AttributeArgument(name, null, value));
            }
            if (targetDllImportData.IsUserDefined.HasFlag(DllImportStub.DllImportMember.ThrowOnUnmappableChar))
            {
                var name = NameEquals(nameof(DllImportAttribute.ThrowOnUnmappableChar));
                var value = CreateBoolExpressionSyntax(targetDllImportData.ThrowOnUnmappableChar);
                newAttributeArgs.Add(AttributeArgument(name, null, value));
            }

            // Create new attribute
            return Attribute(
                ParseName(typeof(DllImportAttribute).FullName),
                AttributeArgumentList(SeparatedList(newAttributeArgs)));

            static ExpressionSyntax CreateBoolExpressionSyntax(bool trueOrFalse)
            {
                return LiteralExpression(
                    trueOrFalse
                        ? SyntaxKind.TrueLiteralExpression
                        : SyntaxKind.FalseLiteralExpression);
            }

            static ExpressionSyntax CreateStringExpressionSyntax(string str)
            {
                return LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    Literal(str));
            }

            static ExpressionSyntax CreateEnumExpressionSyntax<T>(T value) where T : Enum
            {
                return MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(typeof(T).FullName),
                    IdentifierName(value.ToString()));
            }

            static DllImportStub.GeneratedDllImportData GetTargetDllImportDataFromStubData(DllImportStub.GeneratedDllImportData stubDllImportData, bool generateForwarders, string originalMethodName)
            {
                DllImportStub.DllImportMember membersToForward = DllImportStub.DllImportMember.All
                                   // https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.preservesig
                                   // If PreserveSig=false (default is true), the P/Invoke stub checks/converts a returned HRESULT to an exception.
                                   & ~DllImportStub.DllImportMember.PreserveSig
                                   // https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.setlasterror
                                   // If SetLastError=true (default is false), the P/Invoke stub gets/caches the last error after invoking the native function.
                                   & ~DllImportStub.DllImportMember.SetLastError;
                if (generateForwarders)
                {
                    membersToForward = DllImportStub.DllImportMember.All;
                }

                var targetDllImportData = new DllImportStub.GeneratedDllImportData
                {
                    CharSet = stubDllImportData.CharSet,
                    BestFitMapping = stubDllImportData.BestFitMapping,
                    CallingConvention = stubDllImportData.CallingConvention,
                    EntryPoint = stubDllImportData.EntryPoint,
                    ModuleName = stubDllImportData.ModuleName,
                    ExactSpelling = stubDllImportData.ExactSpelling,
                    SetLastError = stubDllImportData.SetLastError,
                    PreserveSig = stubDllImportData.PreserveSig,
                    ThrowOnUnmappableChar = stubDllImportData.ThrowOnUnmappableChar,
                    IsUserDefined = stubDllImportData.IsUserDefined & membersToForward
                };

                // If the EntryPoint property is not set, we will compute and
                // add it based on existing semantics (i.e. method name).
                //
                // N.B. The export discovery logic is identical regardless of where
                // the name is defined (i.e. method name vs EntryPoint property).
                if (!targetDllImportData.IsUserDefined.HasFlag(DllImportStub.DllImportMember.EntryPoint))
                {
                    targetDllImportData.EntryPoint = originalMethodName;
                }

                return targetDllImportData;
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
