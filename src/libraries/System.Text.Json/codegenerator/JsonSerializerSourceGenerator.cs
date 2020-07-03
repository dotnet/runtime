using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace System.Text.Json.CodeGenerator
{
    // TODO(@kevinwkt): Base JsonSerializerSourceGenerator. This class will invoke CodeGenerator within Execute
    // to generate wanted output code for JsonSerializers.
    [Generator]
    public class JsonSerializerSourceGenerator : ISourceGenerator
    {
        public void Execute(SourceGeneratorContext context)
        {
            // TODO(@kevinwkt): Foreach type found, call code generator.
            // Temporary simple HelloWorld class.
            StringBuilder sourceBuilder = new StringBuilder(@"
using System;
namespace HelloWorldGenerated
{
    public static class HelloWorld
    {
        public static string SayHello() 
        {
            return ""Hello"";
");

            sourceBuilder.Append(@"
        }
    }
}");

            context.AddSource("helloWorldGenerated", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        public void Initialize(InitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new JsonSerializableSyntaxReceiver());
        }

        // TODO(@kevinwkt): Search types with attribute JsonSerializable.
        // Temporary function for now that reads all types.
        internal class JsonSerializableSyntaxReceiver : ISyntaxReceiver
        {
            public List<TypeDeclarationSyntax> GeneratorInputTypes = new List<TypeDeclarationSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // TODO(@kevinwkt): For now get all the type decl in all syntax tree.
                if (syntaxNode is TypeDeclarationSyntax tds)
                {
                    GeneratorInputTypes.Add(tds);
                }
            }
        }

    }
}
