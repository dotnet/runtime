// © Microsoft Corporation. All rights reserved.

namespace Microsoft.Extensions.Logging.Generators
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Net.Mime;
    using System.Text;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;

    [Generator]
    public partial class LoggingGenerator : ISourceGenerator
    {
        const int MaxStaeHolderArity = 6;

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

            context.AddSource(nameof(LoggingGenerator), SourceText.From(types.ToString(), Encoding.UTF8));
        }

        private static string GenType(LoggerClass lc)
        {
            var methods = new StringBuilder();
            foreach (var lm in lc.Methods)
            {
                methods.Append(GenFormatFunc(lm));
                methods.Append(GenNameArray(lm));
                methods.Append(GenLogMethod(lm));
            }

            if (string.IsNullOrWhiteSpace(lc.Namespace))
            {
                return $@"
                    partial class {lc.Name}
                    {{
                        {methods}
                    }}
                ";
            }

            return $@"
                namespace {lc.Namespace}
                {{
                    partial class {lc.Name}
                    {{
                        {methods}
                    }}
                }}
                ";
        }

        private static string GenFormatFunc(LoggerMethod lm)
        {
            string typeName;
            var sb = new StringBuilder();

            if (lm.Parameters.Count == 0 || !lm.MessageHasTemplates)
            {
                return string.Empty;
            }
            else if (lm.Parameters.Count == 1)
            {
                typeName = $"global::Microsoft.Extensions.Logging.LogStateHolder<{lm.Parameters[0].Type}>";
                sb.Append("var ");
                sb.Append(lm.Parameters[0].Name);
                sb.Append(" = __holder.Value;\n");
            }
            else if (lm.Parameters.Count > MaxStaeHolderArity)
            {
                typeName = $"global::System.Collections.Generic.KeyValuePair<string, object?>[]";
                var index = 0;
                foreach (var p in lm.Parameters)
                {
                    sb.Append("var ");
                    sb.Append(p.Name);
                    sb.AppendFormat(CultureInfo.InvariantCulture, " = __holder[{0}];\n", index++);
                }
            }
            else
            {
                sb.Append("global::Microsoft.Extensions.Logging.LogStateHolder<");

                foreach (var p in lm.Parameters)
                {
                    if (p != lm.Parameters[0])
                    {
                        sb.Append(", ");
                    }

                    sb.Append(p.Type);
                }
                sb.Append('>');
                typeName = sb.ToString();

                sb.Clear();
                var index = 1;
                foreach (var p in lm.Parameters)
                {
                    sb.Append("var ");
                    sb.Append(p.Name);
                    sb.AppendFormat(CultureInfo.InvariantCulture, " = __holder.Value{0};\n", index++);
                }
            }

            return $@"private static readonly global::System.Func<{typeName}, global::System.Exception?, string> __{lm.Name}FormatFunc = (__holder, _) =>
                {{
                    {sb}
                    return $""{lm.Message}"";
                }};
                ";
        }

        private static string GenNameArray(LoggerMethod lm)
        {
            if (lm.Parameters.Count is < 2 or > MaxStaeHolderArity)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.Append($"private static readonly string[] __{lm.Name}Names = new []{{");
            foreach (var p in lm.Parameters)
            {
                sb.Append($"\"{p.Name}\",");
            }

            sb.Append("};");
            return sb.ToString();
        }

        private static string GenLogMethod(LoggerMethod lm)
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

            var loggerArg = $"{lm.LoggerType} __logger";

            var formatCall = $"(_, _) => \"{ lm.Message}\"";
            if (lm.MessageHasTemplates)
            {
                if (lm.Parameters.Count == 0)
                {
                    formatCall = "Microsoft.Extensions.Logging.LogStateHolder.Format";
                }
                else
                {
                    formatCall = $"__{lm.Name}FormatFunc";
                }
            }

            var eventName = $"nameof({lm.Name})";
            if (lm.EventName != null)
            {
                eventName = $"\"{lm.EventName}\"";
            }

            var eventIdCall = $"new global::Microsoft.Extensions.Logging.EventId({lm.EventId}, {eventName})";

            return $@"
                {lm.Modifier} static partial void {lm.Name}({loggerArg}{(lm.Parameters.Count > 0 ? ", " : string.Empty)}{GenParameters(lm)})
                {{
                    if (__logger.IsEnabled((global::Microsoft.Extensions.Logging.LogLevel){lm.Level}))
                    {{
                        __logger.Log(
                            (global::Microsoft.Extensions.Logging.LogLevel){lm.Level},
                            {eventIdCall},
                            {GenHolder(lm)},
                            {exceptionArg},
                            {formatCall});
                    }}
                }}
        ";
        }

        private static string GenParameters(LoggerMethod lm)
        {
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

        private static string GenHolder(LoggerMethod lm)
        {
            if (lm.Parameters.Count == 0)
            {
                return "new global::Microsoft.Extensions.Logging.LogStateHolder()";
            }
            else if (lm.Parameters.Count == 1)
            {
                return $"new global::Microsoft.Extensions.Logging.LogStateHolder<{lm.Parameters[0].Type}>(nameof({lm.Parameters[0].Name}), {lm.Parameters[0].Name})";
            }
            else if (lm.Parameters.Count > MaxStaeHolderArity)
            {
                var sb = new StringBuilder("new []{");
                foreach (var p in lm.Parameters)
                {
                    sb.Append($"new global::System.Collections.Generic.KeyValuePair<string, object?>(\"{p.Name}\", {p.Name}), ");
                }

                sb.Append('}');
                return sb.ToString();
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var p in lm.Parameters)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(p.Type);
                }
                var tp = sb.ToString();

                sb.Clear();
                sb.Append($"new global::Microsoft.Extensions.Logging.LogStateHolder<{tp}>(__{lm.Name}Names, ");
                foreach (var p in lm.Parameters)
                {
                    if (p != lm.Parameters[0])
                    {
                        sb.Append(", ");
                    }

                    sb.Append(p.Name);
                }

                sb.Append(')');
                return sb.ToString();
            }
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
