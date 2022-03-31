// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Text;
using JavaScript.MarshalerGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace System.Runtime.InteropServices.JavaScript
{
    [Generator]
    internal class JsImportGenerator : IIncrementalGenerator
    {
        private const string AttributeFullName = "System.Runtime.InteropServices.JavaScript.JSImportAttribute";
        private const string Category = "JsImport";
        private const string Prefix = "JsImport";
#pragma warning disable RS2008 //TODO remove this
        public static DiagnosticDescriptor RequirePartialDD = new DiagnosticDescriptor(Prefix + "001", "JSImportAttribute requires partial method", "JSImportAttribute requires partial method", Category, DiagnosticSeverity.Error, true);
        public static DiagnosticDescriptor RequireStaticDD = new DiagnosticDescriptor(Prefix + "002", "JSImportAttribute requires static method", "JSImportAttribute requires static method", Category, DiagnosticSeverity.Error, true);
        public static void Debug(SourceProductionContext context, string message)
        {
            var dd = new DiagnosticDescriptor(Prefix + "000", message, message, Category, DiagnosticSeverity.Warning, true);
            context.ReportDiagnostic(Diagnostic.Create(dd, Location.None));
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<JSImportMethodGenerator> methodDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (s, _) => IsMethodDeclarationWithAnyAttribute(s),
                    static (ctx, _) => GetMethodDeclarationsWithMarshalerAttribute(ctx)
                )
                .Where(static m => m is not null);

            IncrementalValueProvider<(Compilation, ImmutableArray<JSImportMethodGenerator>)> compilationAndClasses = context.CompilationProvider.Combine(methodDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private static bool IsMethodDeclarationWithAnyAttribute(SyntaxNode node)
            => node is MethodDeclarationSyntax m && m.AttributeLists.Count > 0;

        private static JSImportMethodGenerator GetMethodDeclarationsWithMarshalerAttribute(GeneratorSyntaxContext context)
        {
            var methodSyntax = (MethodDeclarationSyntax)context.Node;

            foreach (AttributeListSyntax attributeListSyntax in methodSyntax.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    IMethodSymbol attributeSymbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol as IMethodSymbol;
                    if (attributeSymbol != null)
                    {
                        string fullName = attributeSymbol.ContainingType.ToDisplayString();
                        if (fullName == AttributeFullName)
                        {
                            IMethodSymbol methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodSyntax);
                            var attributeData = methodSymbol.GetAttributes();
                            AttributeData jsImportData = attributeData.Where(d => d.AttributeClass.ToDisplayString() == AttributeFullName).Single();


                            var methodGenrator = new JSImportMethodGenerator(methodSyntax, attributeSyntax, methodSymbol, attributeSymbol, jsImportData);

                            return methodGenrator;
                        }
                    }
                }
            }

            return null;
        }

        private static void Execute(Compilation compilation, ImmutableArray<JSImportMethodGenerator> methods, SourceProductionContext context)
        {
            if (methods.IsDefaultOrEmpty)
                return;

            var fileText = new StringBuilder();
            foreach (JSImportMethodGenerator method in methods)
            {
                if (!method.MethodSymbol.IsPartialDefinition)
                {
                    context.ReportDiagnostic(Diagnostic.Create(RequirePartialDD, method.MethodSyntax.GetLocation()));
                    continue;
                }
                if (!method.MethodSymbol.IsStatic)
                {
                    context.ReportDiagnostic(Diagnostic.Create(RequireStaticDD, method.MethodSyntax.GetLocation()));
                    continue;
                }
                try
                {
                    method.SelectMarshalers(compilation);

                    string code = method.GenerateWrapper();
                    // this is just for debug
                        fileText.AppendLine("/* " + method.MethodName + "  " + DateTime.Now.ToString("o"));
                        fileText.Append(method.prolog.ToString());
                        fileText.AppendLine("*/\n");
                    fileText.AppendLine(code);
                }
                catch (Exception ex)
                {
                    // this is just for debug
                    fileText.AppendLine("/* " + method.MethodName + "  " + DateTime.Now.ToString("o"));
                    fileText.AppendLine(method.MethodSyntax.ToString());
                    fileText.Append(method.prolog.ToString());
                    fileText.AppendLine(ex.ToString());
                    fileText.AppendLine("*/");
                }
            }
            context.AddSource("JsImport.g.cs", fileText.ToString());
        }
    }
}
