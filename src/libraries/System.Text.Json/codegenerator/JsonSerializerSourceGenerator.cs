using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace System.Text.Json.CodeGenerator
{
    [Generator]
    public class JsonSerializerSourceGenerator : ISourceGenerator
    {
        public void Execute(SourceGeneratorContext context)
        {
            // TODO(@kevinwkt): Foreach type found, call code generator.
            //Console.WriteLine(">???");
        }

        public void Initialize(InitializationContext context)
        {
            //context.RegisterForSyntaxNotifications(() => new JsonSerializableSyntaxReceiver());
        }

        //internal class JsonSerializableSyntaxReceiver : ISyntaxReceiver
        //{
        //    public List<TypeDeclarationSyntax> GeneratorInputTypes = new List<TypeDeclarationSyntax>();

        //    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        //    {
        //        // TODO(@kevinwkt): For now get all the type decl in all syntax tree.
        //        if (syntaxNode is TypeDeclarationSyntax tds)
        //        {
        //            GeneratorInputTypes.Add(tds);
        //        }
        //    }
        //}

    }
}
