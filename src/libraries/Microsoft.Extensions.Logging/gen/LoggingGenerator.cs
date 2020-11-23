// © Microsoft Corporation. All rights reserved.

namespace Microsoft.Extensions.Logging.Generators
{
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;

    [Generator]
    public partial class LoggingGenerator : ISourceGenerator
    {
        // The maximum arity of the LogStateHolder-family of types. Beyond this number, parameters are just kepts in an array (which implies an allocation
        // for the array and boxing of all logging method arguments.
        const int MaxStaeHolderArity = 6;

        private readonly Stack<StringBuilder> _builders = new();

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

            var sb = GetStringBuilder();
            try
            {
                foreach (var lc in p.GetLogClasses(receiver.ClassDeclarations))
                {
                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        // stop any additional work
                        return;
                    }

                    sb.Append(GenType(lc));
                }

                context.AddSource(nameof(LoggingGenerator), SourceText.From(sb.ToString(), Encoding.UTF8));
            }
            finally
            {
                ReturnStringBuilder(sb);
            }
        }

        private string GenType(LoggerClass lc)
        {
            var sb = GetStringBuilder();
            try
            {
                foreach (var lm in lc.Methods)
                {
                    sb.Append(GenFormatFunc(lm));
                    sb.Append(GenNameArray(lm));
                    sb.Append(GenLogMethod(lm));
                }

                if (string.IsNullOrWhiteSpace(lc.Namespace))
                {
                    return $@"
                    partial class {lc.Name}
                    {{
                        {sb}
                    }}
                ";
                }

                return $@"
                namespace {lc.Namespace}
                {{
                    partial class {lc.Name}
                    {{
                        {sb}
                    }}
                }}
                ";
            }
            finally
            {
                ReturnStringBuilder(sb);
            }
        }

        private string GenFormatFunc(LoggerMethod lm)
        {
            if (lm.Parameters.Count == 0 || !lm.MessageHasTemplates)
            {
                return string.Empty;
            }

            string typeName;
            var sb = GetStringBuilder();
            try
            {

                if (lm.Parameters.Count == 1)
                {
                    typeName = $"global::Microsoft.Extensions.Logging.LogStateHolder<{lm.Parameters[0].Type}>";
                    sb.Append($"var {lm.Parameters[0].Name} = __holder.Value;\n");
                }
                else if (lm.Parameters.Count > MaxStaeHolderArity)
                {
                    typeName = "global::System.Collections.Generic.KeyValuePair<string, object?>[]";
                    var index = 0;
                    foreach (var p in lm.Parameters)
                    {
                        sb.Append($"var {p.Name} = __holder[{index++}].Value;\n");
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
                        sb.Append($"var {p.Name} = __holder.Value{index++};\n");
                    }
                }

                return $@"private static readonly global::System.Func<{typeName}, global::System.Exception?, string> __{lm.Name}FormatFunc = (__holder, _) =>
                {{
                    {sb}
                    return $""{EscapeMessageString(lm.Message)}"";
                }};
                ";
            }
            finally
            {
                ReturnStringBuilder(sb);
            }
        }

        private string GenNameArray(LoggerMethod lm)
        {
            if (lm.Parameters.Count is < 2 or > MaxStaeHolderArity)
            {
                return string.Empty;
            }

            var sb = GetStringBuilder();
            try
            {
                sb.Append($"private static readonly string[] __{lm.Name}Names = new []{{");
                foreach (var p in lm.Parameters)
                {
                    sb.Append($"\"{p.Name}\", ");
                }

                sb.Append("};");
                return sb.ToString();
            }
            finally
            {
                ReturnStringBuilder(sb);
            }
        }

        private string GenLogMethod(LoggerMethod lm)
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

            string formatCall;
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
            else
            {
                formatCall = $"(_, _) => \"{EscapeMessageString(lm.Message)}\"";
            }

            string eventIdCall;
            if (lm.EventName != null)
            {
                eventIdCall = $"new global::Microsoft.Extensions.Logging.EventId({lm.EventId}, \"{lm.EventName}\")";
            }
            else
            {
                eventIdCall = $"new global::Microsoft.Extensions.Logging.EventId({lm.EventId}, nameof({lm.Name}))";
            }

            return $@"
                {lm.Modifier} static partial void {lm.Name}({lm.LoggerType} __logger{(lm.Parameters.Count > 0 ? ", " : string.Empty)}{GenParameters(lm)})
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

        private static string EscapeMessageString(string message)
        {
            return message
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\"", "\\\"");
        }

        private string GenParameters(LoggerMethod lm)
        {
            var sb = GetStringBuilder();
            try
            {
                foreach (var p in lm.Parameters)
                {
                    if (p != lm.Parameters[0])
                    {
                        sb.Append(", ");
                    }

                    sb.Append($"{p.Type} {p.Name}");
                }

                return sb.ToString();
            }
            finally
            {
                ReturnStringBuilder(sb);
            }
        }

        private string GenHolder(LoggerMethod lm)
        {
            if (lm.Parameters.Count == 0)
            {
                return "new global::Microsoft.Extensions.Logging.LogStateHolder()";
            }

            if (lm.Parameters.Count == 1)
            {
                return $"new global::Microsoft.Extensions.Logging.LogStateHolder<{lm.Parameters[0].Type}>(nameof({lm.Parameters[0].Name}), {lm.Parameters[0].Name})";
            }

            var sb = GetStringBuilder();
            try
            {
                if (lm.Parameters.Count > MaxStaeHolderArity)
                {
                    sb.Append("new []{");
                    foreach (var p in lm.Parameters)
                    {
                        sb.Append($"new global::System.Collections.Generic.KeyValuePair<string, object?>(\"{p.Name}\", {p.Name}), ");
                    }

                    sb.Append('}');
                }
                else
                {
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
                }
                return sb.ToString();
            }
            finally
            {
                ReturnStringBuilder(sb);
            }
        }

        // our own cheezy object pool since we can't use the .NET core version
        private StringBuilder GetStringBuilder()
        {
            if (_builders.Count == 0)
            {
                return new StringBuilder(1024);
            }

            var b = _builders.Pop();
            b.Clear();
            return b;
        }

        private void ReturnStringBuilder(StringBuilder sb)
        {
            _builders.Push(sb);
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
