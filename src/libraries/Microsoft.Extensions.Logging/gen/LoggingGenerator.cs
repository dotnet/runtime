// © Microsoft Corporation. All rights reserved.

namespace Microsoft.Extensions.Logging.Generators
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;

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
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
            {
                // nothing to do yet
                return;
            }

            var p = new Parser(context);

            var types = new StringBuilder();
            foreach (var lc in p.GetLogClasses(receiver.ClassDeclarations))
            {
                types.Append(GenType(lc));
            }

            var final = $@"
using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

{types}";

            context.AddSource(nameof(LoggingGenerator), SourceText.From(final, Encoding.UTF8));
        }

        private static string GenType(LoggerClass lc)
        {
            var methods = new StringBuilder();
            foreach (var lm in lc.Methods)
            {
                if (lm.Parameters.Count > 0)
                {
                    methods.Append(GenStruct(lm));
                }
                methods.Append(GenExtensionLogMethod(lm));
            }

            var namespaceStart = string.Empty;
            var namespaceEnd = string.Empty;

            if (lc.Namespace != null)
            {
                namespaceStart = $"namespace {lc.Namespace}\n{{\n";
                namespaceEnd = "}\n";
            }

            return $@"
{namespaceStart}
    partial class {lc.Name}
    {{
        {methods}
    }}
{namespaceEnd}
";
        }

        private static string GenStruct(LoggerMethod lm)
        {
            var constructor = $@"
            public __{lm.Name}State({GenParameters(lm)})
            {{
{GenFieldAssignments(lm)}
            }}
";

            var format = $@"
            public override string ToString() => $""{lm.Message}"";
";

            var del = string.Empty;
            if (lm.MessageHasTemplates)
            {
                del = $"            public static readonly Func<__{lm.Name}State, Exception?, string> Format = (s, _) => s.ToString();";
            }

            return $@"
        private readonly struct __{lm.Name}State : IReadOnlyList<KeyValuePair<string, object>>
        {{
{GenFields(lm)}
{constructor}
{format}
{del}

            public int Count => {lm.Parameters.Count};

            public KeyValuePair<string, object> this[int index]
            {{
                get
                {{
                    switch (index)
                    {{
{GenCases(lm)}
                        default:
                            throw new ArgumentOutOfRangeException(nameof(index));
                    }}
                }}
            }}

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {{
{GenEnumerator(lm)}
            }}

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }}
";
        }

        private static string GenExtensionLogMethod(LoggerMethod lm)
        {
            string exceptionArg = "null";
            foreach (var p in lm.Parameters)
            {
                if (p.IsExceptionType)
                {
                    exceptionArg = p.Name;
                    break;
                }
            }

            var loggerArg = "ILogger logger";

            var ctorCall = $"new __{lm.Name}State({ GenArguments(lm)})";
            if (lm.Parameters.Count == 0)
            {
                // when no parameters, use the common struct
                ctorCall = "new Microsoft.Extensions.Logging.LogStateHolder()";
            }

            var formatCall = $"(s, _) => \"{ lm.Message}\"";
            if (lm.MessageHasTemplates)
            {
                if (lm.Parameters.Count == 0)
                {
                    formatCall = "Microsoft.Extensions.Logging.LogStateHolder.Format";
                }
                else
                {
                    formatCall = $"__{lm.Name}State.Format";
                }
            }

            var eventName = $"nameof({lm.Name})";
            if (lm.EventName != null)
            {
                eventName = $"\"{lm.EventName}\"";
            }

            var eventIdCall = $"new EventId({lm.EventId}, {eventName})";

            return $@"
        public static partial void {lm.Name}({loggerArg}{(lm.Parameters.Count > 0 ? ", " : string.Empty)}{GenParameters(lm)})
        {{
            if (logger.IsEnabled((LogLevel){lm.Level}))
            {{
                logger.Log((LogLevel){lm.Level}, {eventIdCall}, {ctorCall}, {exceptionArg}, {formatCall});
            }}
        }}
";
        }

        private static string GenParameters(LoggerMethod lm)
        {
            if (lm.Parameters.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var p in lm.Parameters)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(p.Type);
                sb.Append(' ');
                sb.Append(p.Name);
            }

            return sb.ToString();
        }

        private static string GenArguments(LoggerMethod lm)
        {
            if (lm.Parameters.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var p in lm.Parameters)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(p.Name);
            }

            return sb.ToString();
        }

        private static string GenFields(LoggerMethod lm)
        {
            var sb = new StringBuilder();
            foreach (var p in lm.Parameters)
            {
                sb.Append($"            private readonly {p.Type} {p.Name};\n");
            }

            return sb.ToString();
        }

        private static string GenFieldAssignments(LoggerMethod lm)
        {
            var sb = new StringBuilder();
            foreach (var p in lm.Parameters)
            {
                sb.Append($"                this.{p.Name} = {p.Name};\n");
            }

            return sb.ToString();
        }

        private static string GenEnumerator(LoggerMethod lm)
        {
            var sb = new StringBuilder();
            int index = 0;
            foreach (var p in lm.Parameters)
            {
                sb.Append($"                yield return this[{index}];\n");
            }

            return sb.ToString();
        }

        private static string GenCases(LoggerMethod lm)
        {
            var sb = new StringBuilder();
            var index = 0;
            foreach (var p in lm.Parameters)
            {
                sb.Append($"                        case {index++}:\n");
                sb.Append($"                            return new KeyValuePair<string, object>(nameof({p.Name}), {p.Name});\n");
            }

            return sb.ToString();
        }

        private sealed class SyntaxReceiver : ISyntaxReceiver
        {
            public List<ClassDeclarationSyntax> ClassDeclarations { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // Any partial class
                if (syntaxNode is ClassDeclarationSyntax { Modifiers: { Count: > 0 } modifiers } classSyntax && modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    ClassDeclarations.Add(classSyntax);
                }
            }
        }
    }
}
