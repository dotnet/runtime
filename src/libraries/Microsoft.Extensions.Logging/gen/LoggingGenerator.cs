// © Microsoft Corporation. All rights reserved.

namespace Microsoft.Extensions.Logging.Generators
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;
    using System.Collections.Generic;
    using System.Text;

    [Generator]
    public partial class LoggingGenerator : ISourceGenerator
    {
        // The maximum arity of the LogStateHolder-family of types. Beyond this number, parameters are just kepts in an array (which implies an allocation
        // for the array and boxing of all logging method arguments.
        const int MaxStateHolderArity = 6;

        /// <inheritdoc />
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        /// <inheritdoc />
        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
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
            var result = e.Emit(p.GetLogClasses(receiver.ClassDeclarations), context.CancellationToken);

            context.AddSource(nameof(LoggingGenerator), SourceText.From(result, Encoding.UTF8));
        }

        private sealed class SyntaxReceiver : ISyntaxReceiver
        {
            public List<ClassDeclarationSyntax> ClassDeclarations { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // Any class
                var classSyntax = syntaxNode as ClassDeclarationSyntax;
                if (classSyntax != null)
                {
                    ClassDeclarations.Add(classSyntax);
                }
            }
        }
    }
}
