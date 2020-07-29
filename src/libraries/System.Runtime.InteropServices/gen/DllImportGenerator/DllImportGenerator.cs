using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Interop
{
    [Generator]
    public class DllImportGenerator : ISourceGenerator
    {
        private const string GeneratedDllImport = nameof(GeneratedDllImport);
        private const string GeneratedDllImportAttribute = nameof(GeneratedDllImportAttribute);
        private static readonly string GeneratedDllImportAttributeSource = $@"
#nullable enable
namespace System.Runtime.InteropServices
{{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class {nameof(GeneratedDllImportAttribute)} : Attribute
    {{
        public bool BestFitMapping;
        public CallingConvention CallingConvention;
        public CharSet CharSet;
        public string? EntryPoint;
        public bool ExactSpelling;
        public bool PreserveSig;
        public bool SetLastError;
        public bool ThrowOnUnmappableChar;

        public {nameof(GeneratedDllImportAttribute)}(string dllName)
        {{
            this.Value = dllName;
        }}

        public string Value {{ get; private set; }}
    }}
}}
";

        public void Execute(SourceGeneratorContext context)
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

            context.AddSource(nameof(GeneratedDllImportAttributeSource), SourceText.From(GeneratedDllImportAttributeSource, Encoding.UTF8));

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

                // Create the stub details.
                var dllImportStub = DllImportStub.Create(methodSymbolInfo, context.CancellationToken);

                // Report any diagnostics from the stub genertion step.
                foreach (var diag in dllImportStub.Diagnostics)
                {
                    context.ReportDiagnostic(diag);
                }

                // Process the attributes on the method.
                AttributeSyntax dllImportAttr;
                var additionalAttrs = this.ProcessAttributes(methodSymbolInfo.Name, methodSyntax.AttributeLists, out dllImportAttr);

                PrintGeneratedSource(generatedDllImports, methodSyntax, ref dllImportStub, dllImportAttr, additionalAttrs);
            }

            Debug.WriteLine(generatedDllImports.ToString()); // [TODO] Find some way to emit this for debugging - logs?
            context.AddSource("DllImportGenerator.g.cs", SourceText.From(generatedDllImports.ToString(), Encoding.UTF8));
        }

        private void PrintGeneratedSource(
            StringBuilder builder,
            MethodDeclarationSyntax userDeclaredMethod,
            ref DllImportStub stub,
            AttributeSyntax dllImportAttr,
            IEnumerable<string> additionalAttrDecls)
        {
            const string SingleDepth = "    ";
            var currentIndent = string.Empty;

            // Declare namespace
            if (!(stub.StubTypeNamespace is null))
            {
                builder.AppendLine($@"namespace {stub.StubTypeNamespace}
{{");
                currentIndent += SingleDepth;
            }

            // Print type declarations
            var typeIndentStack = new Stack<string>();
            foreach (var typeDecl in stub.StubContainingTypesDecl)
            {
                builder.AppendLine($@"{currentIndent}{typeDecl}
{currentIndent}{{");

                typeIndentStack.Push(currentIndent);
                currentIndent += SingleDepth;
            }

            // Begin declare function
            builder.Append(
$@"{currentIndent}{userDeclaredMethod.Modifiers} {stub.StubReturnType} {userDeclaredMethod.Identifier}(");

            char delim = ' ';
            foreach (var param in stub.StubParameters)
            {
                builder.Append($"{delim}{param.Type} {param.Name}");
                delim = ',';
            }

            // End declare function
            builder.AppendLine(
$@")
{currentIndent}{{");

            // Insert lines into function
            foreach (var line in stub.StubCode)
            {
                builder.AppendLine($@"{currentIndent}{SingleDepth}{line}");
            }

            builder.AppendLine(
$@"{ currentIndent}}}

{currentIndent}[{dllImportAttr}]");

            // Create the DllImport declaration.
            builder.Append($"{currentIndent}extern private static {stub.DllImportReturnType} {stub.DllImportMethodName}");
            if (!stub.DllImportParameters.Any())
            {
                builder.AppendLine("();");
            }
            else
            {
                delim = '(';
                foreach (var paramPair in stub.DllImportParameters)
                {
                    builder.Append($"{delim}{paramPair.Type} {paramPair.Name}");
                    delim = ',';
                }
                builder.AppendLine(");");
            }

            // Print closing type declarations
            while (typeIndentStack.Count > 0)
            {
                builder.AppendLine($@"{typeIndentStack.Pop()}}}");
            }

            // Close namespace
            if (!(stub.StubTypeNamespace is null))
            {
                builder.AppendLine("}");
            }
        }

        public void Initialize(InitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
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

        private IEnumerable<string> ProcessAttributes(
            string methodName,
            SyntaxList<AttributeListSyntax> attributes,
            out AttributeSyntax dllImportAttr)
        {
            dllImportAttr = default;

            var retainedAttrs = new List<string>();

            // Process all attributes
            foreach (AttributeListSyntax listSyntax in attributes)
            {
                foreach (AttributeSyntax attrSyntax in listSyntax.Attributes)
                {
                    // Retain the attribute if not GeneratedDllImport.
                    if (!IsGeneratedDllImportAttribute(attrSyntax))
                    {
                        retainedAttrs.Add(attrSyntax.ToString());
                        continue;
                    }

                    // Determine if the attribute has the EntryPoint property set.
                    bool hasEntryPoint = false;
                    if (!(attrSyntax.ArgumentList is null))
                    {
                        foreach (var arg in attrSyntax.ArgumentList.Arguments)
                        {
                            if (arg.NameEquals is null)
                            {
                                continue;
                            }

                            hasEntryPoint = nameof(DllImportAttribute.EntryPoint).Equals(arg.NameEquals.Name.ToString());
                            if (hasEntryPoint)
                            {
                                break;
                            }
                        }
                    }

                    // Don't retain the GeneratedDllImport attribute.
                    // However, we use its settings for the real DllImport.
                    AttributeSyntax newAttr = attrSyntax;
                    if (!hasEntryPoint)
                    {
                        // If the EntryPoint property is not set, we will compute and
                        // add it based on existing semantics (i.e. method name).
                        //
                        // N.B. The export discovery logic is identical regardless of where
                        // the name is defined (i.e. method name vs EntryPoint property).
                        var entryPointName = SyntaxFactory.NameEquals(nameof(DllImportAttribute.EntryPoint));

                        // The name of the method is the entry point name to use.
                        var entryPointValue = SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(methodName));

                        var entryPointProp = SyntaxFactory.AttributeArgument(entryPointName, null, entryPointValue);

                        // Add the new property to the existing attribute thus creating a new attribute.
                        newAttr = attrSyntax.AddArgumentListArguments(entryPointProp);
                    }

                    // Replace the name of the attribute
                    NameSyntax dllImportName = SyntaxFactory.ParseName(typeof(DllImportAttribute).FullName);
                    dllImportAttr = newAttr.WithName(dllImportName);
                }
            }

            return retainedAttrs;
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
