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
                DllImportStub.GeneratedDllImportData dllImportData;
                AttributeSyntax dllImportAttr = this.ProcessGeneratedDllImportAttribute(methodSymbolInfo, generatedDllImportAttr, context.AnalyzerConfigOptions.GlobalOptions.GenerateForwarders(), out dllImportData);
                Debug.Assert((dllImportAttr is not null) && (dllImportData is not null));

                if (dllImportData!.IsUserDefined.HasFlag(DllImportStub.DllImportMember.BestFitMapping))
                {
                    generatorDiagnostics.ReportConfigurationNotSupported(generatedDllImportAttr, nameof(DllImportStub.GeneratedDllImportData.BestFitMapping));
                }

                if (dllImportData!.IsUserDefined.HasFlag(DllImportStub.DllImportMember.ThrowOnUnmappableChar))
                {
                    generatorDiagnostics.ReportConfigurationNotSupported(generatedDllImportAttr, nameof(DllImportStub.GeneratedDllImportData.ThrowOnUnmappableChar));
                }

                if (lcidConversionAttr != null)
                {
                    // Using LCIDConversion with GeneratedDllImport is not supported
                    generatorDiagnostics.ReportConfigurationNotSupported(lcidConversionAttr, nameof(TypeNames.LCIDConversionAttribute));
                }

                // Create the stub.
                var dllImportStub = DllImportStub.Create(methodSymbolInfo, dllImportData!, env, generatorDiagnostics, context.CancellationToken);

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

        private AttributeSyntax ProcessGeneratedDllImportAttribute(
            IMethodSymbol method,
            AttributeData attrData,
            bool generateForwarders,
            out DllImportStub.GeneratedDllImportData dllImportData)
        {
            dllImportData = new DllImportStub.GeneratedDllImportData();

            // Found the GeneratedDllImport, but it has an error so report the error.
            // This is most likely an issue with targeting an incorrect TFM.
            if (attrData.AttributeClass?.TypeKind is null or TypeKind.Error)
            {
                // [TODO] Report GeneratedDllImport has an error - corrupt metadata?
                throw new InvalidProgramException();
            }

            var newAttributeArgs = new List<AttributeArgumentSyntax>();

            // Populate the DllImport data from the GeneratedDllImportAttribute attribute.
            dllImportData.ModuleName = attrData.ConstructorArguments[0].Value!.ToString();

            newAttributeArgs.Add(SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(dllImportData.ModuleName))));

            // All other data on attribute is defined as NamedArguments.
            foreach (var namedArg in attrData.NamedArguments)
            {
                ExpressionSyntax? expSyntaxMaybe = null;
                switch (namedArg.Key)
                {
                    default:
                        Debug.Fail($"An unknown member was found on {GeneratedDllImport}");
                        continue;
                    case nameof(DllImportStub.GeneratedDllImportData.BestFitMapping):
                        dllImportData.BestFitMapping = (bool)namedArg.Value.Value!;
                        expSyntaxMaybe = CreateBoolExpressionSyntax(dllImportData.BestFitMapping);
                        dllImportData.IsUserDefined |= DllImportStub.DllImportMember.BestFitMapping;
                        break;
                    case nameof(DllImportStub.GeneratedDllImportData.CallingConvention):
                        dllImportData.CallingConvention = (CallingConvention)namedArg.Value.Value!;
                        expSyntaxMaybe = CreateEnumExpressionSyntax(dllImportData.CallingConvention);
                        dllImportData.IsUserDefined |= DllImportStub.DllImportMember.CallingConvention;
                        break;
                    case nameof(DllImportStub.GeneratedDllImportData.CharSet):
                        dllImportData.CharSet = (CharSet)namedArg.Value.Value!;
                        expSyntaxMaybe = CreateEnumExpressionSyntax(dllImportData.CharSet);
                        dllImportData.IsUserDefined |= DllImportStub.DllImportMember.CharSet;
                        break;
                    case nameof(DllImportStub.GeneratedDllImportData.EntryPoint):
                        dllImportData.EntryPoint = (string)namedArg.Value.Value!;
                        expSyntaxMaybe = CreateStringExpressionSyntax(dllImportData.EntryPoint!);
                        dllImportData.IsUserDefined |= DllImportStub.DllImportMember.EntryPoint;
                        break;
                    case nameof(DllImportStub.GeneratedDllImportData.ExactSpelling):
                        dllImportData.ExactSpelling = (bool)namedArg.Value.Value!;
                        expSyntaxMaybe = CreateBoolExpressionSyntax(dllImportData.ExactSpelling);
                        dllImportData.IsUserDefined |= DllImportStub.DllImportMember.ExactSpelling;
                        break;
                    case nameof(DllImportStub.GeneratedDllImportData.PreserveSig):
                        dllImportData.PreserveSig = (bool)namedArg.Value.Value!;
                        expSyntaxMaybe = CreateBoolExpressionSyntax(dllImportData.PreserveSig);
                        dllImportData.IsUserDefined |= DllImportStub.DllImportMember.PreserveSig;
                        break;
                    case nameof(DllImportStub.GeneratedDllImportData.SetLastError):
                        dllImportData.SetLastError = (bool)namedArg.Value.Value!;
                        expSyntaxMaybe = CreateBoolExpressionSyntax(dllImportData.SetLastError);
                        dllImportData.IsUserDefined |= DllImportStub.DllImportMember.SetLastError;
                        break;
                    case nameof(DllImportStub.GeneratedDllImportData.ThrowOnUnmappableChar):
                        dllImportData.ThrowOnUnmappableChar = (bool)namedArg.Value.Value!;
                        expSyntaxMaybe = CreateBoolExpressionSyntax(dllImportData.ThrowOnUnmappableChar);
                        dllImportData.IsUserDefined |= DllImportStub.DllImportMember.ThrowOnUnmappableChar;
                        break;
                }

                Debug.Assert(expSyntaxMaybe is not null);

                // If we're generating a forwarder stub, then all parameters on the GenerateDllImport attribute
                // must also be added to the generated DllImport attribute.
                if (generateForwarders || PassThroughToDllImportAttribute(namedArg.Key))
                {
                    // Defer the name equals syntax till we know the value means something. If we created
                    // an expression we know the key value was valid.
                    NameEqualsSyntax nameSyntax = SyntaxFactory.NameEquals(namedArg.Key);
                    newAttributeArgs.Add(SyntaxFactory.AttributeArgument(nameSyntax, null, expSyntaxMaybe!));
                }
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

            static bool PassThroughToDllImportAttribute(string argName)
            {
                // Certain fields on DllImport will prevent inlining. Their functionality should be handled by the
                // generated source, so the generated DllImport declaration should not include these fields.
                return argName switch
                {
                    // https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.preservesig
                    // If PreserveSig=false (default is true), the P/Invoke stub checks/converts a returned HRESULT to an exception.
                    nameof(DllImportStub.GeneratedDllImportData.PreserveSig) => false,
                    // https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.setlasterror
                    // If SetLastError=true (default is false), the P/Invoke stub gets/caches the last error after invoking the native function.
                    nameof(DllImportStub.GeneratedDllImportData.SetLastError) => false,
                    _ => true
                };
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
