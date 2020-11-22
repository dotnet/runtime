// © Microsoft Corporation. All rights reserved.

namespace Microsoft.Extensions.Logging.Generators
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
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
                methods.Append(GenStruct(lm));
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

        private static string GenStruct(LoggerMethod lm)
        {
            if (lm.Parameters.Count == 0)
            {
                // we don't need a custom struct if there aren't any parameters
                return string.Empty;
            }

            return $@"
                private readonly struct __{lm.Name}State : global::System.Collections.Generic.IReadOnlyList<global::System.Collections.Generic.KeyValuePair<string, object?>>
                {{
                    {GenHolderField(lm)}

                    public __{lm.Name}State({GenParameters(lm)})
                    {{
                        {GenHolderFieldAssignment(lm)}
                    }}

                    {GenFormatFunc(lm)}

                    public override string ToString()
                    {{
                        {GenToString(lm)}
                    }}

                    public int Count => {lm.Parameters.Count};
                    public global::System.Collections.Generic.KeyValuePair<string, object?> this[int index] => __holder[index];
                    public global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<string, object?>> GetEnumerator() => (global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<string, object?>>)__holder.GetEnumerator();
                    System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
                }}
            ";
        }

        private static string GenHolderField(LoggerMethod lm)
        {
            if (lm.Parameters.Count > MaxStaeHolderArity)
            {
                return "private readonly global::System.Collections.Generic.KeyValuePair<string, object?>[] __holder;";
            }

            var sb = new StringBuilder();
            foreach (var p in lm.Parameters)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(p.Type);
            }
 
            return $"private readonly global::Microsoft.Extensions.Logging.LogStateHolder<{sb}> __holder;";
        }

        private static string GenHolderFieldAssignment(LoggerMethod lm)
        {
            if (lm.Parameters.Count == 1)
            {
                return $"__holder = new(nameof({lm.Parameters[0].Name}), {lm.Parameters[0].Name});";
            }

            var sb = new StringBuilder();

            if (lm.Parameters.Count > MaxStaeHolderArity)
            {
                sb = new StringBuilder("__holder = new []{");

                foreach (var p in lm.Parameters)
                {
                    sb.Append($"new global::System.Collections.Generic.KeyValuePair<string, object?>(\"{p.Name}\", {p.Name}), ");
                }

                sb.Append("};");
            }
            else
            {
                sb.Append("__holder = new(new []{");
                bool first = true;
                foreach (var p in lm.Parameters)
                {
                    if (!first)
                    {
                        sb.Append(", ");
                    }
                    first = false;
                    sb.Append("nameof(");
                    sb.Append(p.Name);
                    sb.Append(')');
                }

                sb.Append("}, ");
                first = true;
                foreach (var p in lm.Parameters)
                {
                    if (!first)
                    {
                        sb.Append(", ");
                    }
                    first = false;

                    sb.Append(p.Name);
                }

                sb.Append(");");
            };

            return sb.ToString();
        }

        private static string GenFormatFunc(LoggerMethod lm)
        {
            if (lm.MessageHasTemplates)
            {
                return $"public static readonly global::System.Func<__{lm.Name}State, global::System.Exception?, string> Format = (s, _) => s.ToString();";
            }

            return string.Empty;
        }

        private static string GenToString(LoggerMethod lm)
        {
            var sb = new StringBuilder();
            if (lm.Parameters.Count == 1)
            {
                sb.Append("var ");
                sb.Append(lm.Parameters[0].Name);
                sb.Append(" = __holder.Value;\n");
            }
            else if (lm.Parameters.Count > MaxStaeHolderArity)
            {
                var index = 0;
                foreach (var p in lm.Parameters)
                {
                    sb.Append("var ");
                    sb.Append(p.Name);
                    sb.AppendFormat(CultureInfo.InvariantCulture, " = __holder[0];\n", index++);
                }
            }
            else 
            {
                var index = 1;
                foreach (var p in lm.Parameters)
                {
                    sb.Append("var ");
                    sb.Append(p.Name);
                    sb.AppendFormat(CultureInfo.InvariantCulture, " = __holder.Value{0};\n", index++);
                }
            }

            return $@"
                {sb}
                return $""{lm.Message}"";
            ";
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

            var eventIdCall = $"new global::Microsoft.Extensions.Logging.EventId({lm.EventId}, {eventName})";

            return $@"
                {lm.Modifier} static partial void {lm.Name}({loggerArg}{(lm.Parameters.Count > 0 ? ", " : string.Empty)}{GenParameters(lm)})
                {{
                    if (__logger.IsEnabled((global::Microsoft.Extensions.Logging.LogLevel){lm.Level}))
                    {{
                        __logger.Log((global::Microsoft.Extensions.Logging.LogLevel){lm.Level}, {eventIdCall}, {ctorCall}, {exceptionArg}, {formatCall});
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
