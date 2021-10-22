// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XUnitWrapperGenerator
{
    [Generator]
    public sealed class XUnitWrapperGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterImplementationSourceOutput(
                context.SyntaxProvider.CreateSyntaxProvider(
                    static (node, ct) =>
                        node.IsKind(SyntaxKind.MethodDeclaration)
                            && node is MethodDeclarationSyntax method
                            && method.AttributeLists.Count > 0,
                    static (context, ct) => (IMethodSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node)!)
                    .Where(method => method.IsStatic && method.Parameters.IsEmpty && method.GetAttributes().Any(attr => attr.AttributeClass!.ToDisplayString() == "Xunit.FactAttribute"))
                    .Select((method, ct) => $"{method.ContainingType.ToDisplayString()}.{method.Name}")
                    .Collect(),
                static (context, methods) =>
                {
                    // For simplicity, we'll use top-level statements for the generated Main method.
                    StringBuilder builder = new();
                    builder.AppendLine("try {");
                    builder.Append(string.Join("\n", methods.Select(m => $"{m}();")));
                    builder.AppendLine("} catch(System.Exception) { return 101; }");
                    builder.AppendLine("return 100;");
                    context.AddSource("Main.g.cs", builder.ToString());
                });
        }
    }
}
