// © Microsoft Corporation. All rights reserved.

using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Extensions.Logging.Generators
{
    [Generator]
    public partial class LoggingGenerator : ISourceGenerator
    {
        /// <inheritdoc />
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        /// <inheritdoc />
        public void Execute(GeneratorExecutionContext context)
        {
            var receiver = context.SyntaxReceiver as SyntaxReceiver;
            if (receiver == null || receiver.ClassDeclarations.Count == 0)
            {
                // nothing to do yet
                return;
            }

            var pascalCaseArguments = false;
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("PascalCaseArguments", out var value))
            {
                pascalCaseArguments = ((value.ToUpperInvariant() == "TRUE") || (value.ToUpperInvariant() == "YES"));
            }

            var p = new Parser(context.Compilation, context.ReportDiagnostic, context.CancellationToken);
            var e = new Emitter(pascalCaseArguments);
            var logClasses = p.GetLogClasses(receiver.ClassDeclarations);
            var result = e.Emit(logClasses, context.CancellationToken);

            context.AddSource(nameof(LoggingGenerator), SourceText.From(result, Encoding.UTF8));
        }

        private sealed class SyntaxReceiver : ISyntaxReceiver
        {
            public List<ClassDeclarationSyntax> ClassDeclarations { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                var classSyntax = syntaxNode as ClassDeclarationSyntax;
                if (classSyntax != null)
                {
                    ClassDeclarations.Add(classSyntax);
                }
            }
        }
    }
}
