// © Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Extensions.Logging.Generators
{
    public partial class LoggerMessageGenerator
    {
        internal class Parser
        {
            private readonly CancellationToken _cancellationToken;
            private readonly Compilation _compilation;
            private readonly Action<Diagnostic> _reportDiagnostic;

            public Parser(Compilation compilation, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
            {
                _compilation = compilation;
                _cancellationToken = cancellationToken;
                _reportDiagnostic = reportDiagnostic;
            }

            /// <summary>
            /// Gets the set of logging classes containing methods to output.
            /// </summary>
            public IReadOnlyList<LoggerClass> GetLogClasses(IEnumerable<ClassDeclarationSyntax> classes)
            {
                const string LoggerMessageAttribute = "Microsoft.Extensions.Logging.LoggerMessageAttribute";
                const int LoggerMessageAttrEventIdArg = 0;
                const int LoggerMessageAttrLevelArg = 1;
                const int LoggerMessageAttrMessageArg = 2;
                const int LoggerMessageAttrEventNameArg = 3;

                var results = new List<LoggerClass>();

                var exceptionSymbol = _compilation.GetTypeByMetadataName("System.Exception");
                if (exceptionSymbol == null)
                {
                    Diag(DiagDescriptors.ErrorMissingRequiredType, null, "System.Exception");
                    return results;
                }

                var dateTimeSymbol = _compilation.GetTypeByMetadataName("System.DateTime");
                if (dateTimeSymbol == null)
                {
                    Diag(DiagDescriptors.ErrorMissingRequiredType, null, "System.DateTime");
                    return results;
                }

                var loggerMessageAttribute = _compilation.GetTypeByMetadataName(LoggerMessageAttribute);
                if (loggerMessageAttribute is null)
                {
                    // nothing to do if this type isn't available
                    return results;
                }

                var loggerSymbol = _compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.ILogger");
                if (loggerSymbol == null)
                {
                    // nothing to do if this type isn't available
                    return results;
                }

                var ids = new HashSet<string>();

                // we enumerate by syntax tree, to minimize the need to instantiate semantic models (since they're expensive)
                foreach (var group in classes.GroupBy(x => x.SyntaxTree))
                {
                    SemanticModel? sm = null;
                    foreach (var classDef in group)
                    {
                        // stop if we're asked to
                        _cancellationToken.ThrowIfCancellationRequested();

                        LoggerClass? lc = null;
                        string nspace = string.Empty;

                        ids.Clear();
                        foreach (var member in classDef.Members)
                        {
                            var method = member as MethodDeclarationSyntax;
                            if (method == null)
                            {
                                // we only care about methods
                                continue;
                            }

                            foreach (var mal in method.AttributeLists)
                            {
                                foreach (var ma in mal.Attributes)
                                {
                                    sm ??= _compilation.GetSemanticModel(classDef.SyntaxTree);

                                    var mattrSymbol = sm.GetSymbolInfo(ma, _cancellationToken).Symbol as IMethodSymbol;
                                    if (mattrSymbol == null || !loggerMessageAttribute.Equals(mattrSymbol.ContainingType, SymbolEqualityComparer.Default))
                                    {
                                        // badly formed attribute definition, or not the right attribute
                                        continue;
                                    }

                                    var args = ma.ArgumentList!.Arguments;

                                    var eventId = sm.GetConstantValue(args[LoggerMessageAttrEventIdArg].Expression, _cancellationToken).ToString();
                                    var level = (int)sm.GetConstantValue(args[LoggerMessageAttrLevelArg].Expression, _cancellationToken).Value!;
                                    var message = sm.GetConstantValue(args[LoggerMessageAttrMessageArg].Expression, _cancellationToken).ToString();

                                    string eventName = string.Empty;
                                    if (args.Count > LoggerMessageAttrEventNameArg)
                                    {
                                        eventName = sm.GetConstantValue(args[LoggerMessageAttrEventNameArg].Expression, _cancellationToken).ToString();
                                    }

                                    var methodSymbol = sm.GetDeclaredSymbol(method, _cancellationToken);
                                    if (methodSymbol != null)
                                    {
                                        List<string>? templates = null;
                                        if (!string.IsNullOrWhiteSpace(message))
                                        {
                                            templates = ExtractTemplateArgs(message);
                                        }

                                        var lm = new LoggerMethod
                                        {
                                            Name = method.Identifier.ToString(),
                                            Level = level,
                                            Message = message,
                                            EventId = eventId,
                                            EventName = eventName,
                                            MessageHasTemplates = templates != null && templates.Count > 0,
                                            IsExtensionMethod = methodSymbol.IsExtensionMethod,
                                            Modifiers = method.Modifiers.ToString(),
                                        };

                                        bool keep = true;   // whether or not we want to keep the method definition or if it's got errors making it worth discarding instead
                                        if (lm.Name[0] == '_')
                                        {
                                            // can't have logging method names that start with _ since that can lead to conflicting symbol names
                                            // because the generated symbols start with _
                                            Diag(DiagDescriptors.ErrorInvalidMethodName, method.Identifier.GetLocation());
                                        }

                                        if (sm.GetTypeInfo(method.ReturnType!).Type!.SpecialType != SpecialType.System_Void)
                                        {
                                            // logging methods must return void
                                            Diag(DiagDescriptors.ErrorInvalidMethodReturnType, method.ReturnType.GetLocation());
                                            keep = false;
                                        }

                                        if (method.Arity > 0)
                                        {
                                            // we don't currently support generic methods
                                            Diag(DiagDescriptors.ErrorMethodIsGeneric, method.Identifier.GetLocation());
                                            keep = false;
                                        }

                                        bool isStatic = false;
                                        bool isPartial = false;
                                        foreach (var mod in method.Modifiers)
                                        {
                                            switch (mod.Text)
                                            {
                                                case "partial":
                                                    isPartial = true;
                                                    break;

                                                case "static":
                                                    isStatic = true;
                                                    break;
                                            }
                                        }

                                        if (!isStatic)
                                        {
                                            Diag(DiagDescriptors.ErrorNotStaticMethod, method.GetLocation());
                                            keep = false;
                                        }

                                        if (!isPartial)
                                        {
                                            Diag(DiagDescriptors.ErrorNotPartialMethod, method.GetLocation());
                                            keep = false;
                                        }

                                        if (method.Body != null)
                                        {
                                            Diag(DiagDescriptors.ErrorMethodHasBody, method.Body.GetLocation());
                                            keep = false;
                                        }

                                        // ensure there are no duplicate ids.
                                        if (ids.Contains(lm.EventId))
                                        {
                                            Diag(DiagDescriptors.ErrorEventIdReuse, args[0].GetLocation(), lm.EventId);
                                        }
                                        else
                                        {
                                            _ = ids.Add(lm.EventId);
                                        }

                                        if (string.IsNullOrWhiteSpace(lm.Message))
                                        {
                                            Diag(DiagDescriptors.ErrorInvalidMessage, ma.GetLocation(), method.Identifier.ToString());
                                        }
                                        else
                                        {
                                            var msg = lm.Message;
#pragma warning disable S1067 // Expressions should not be too complex
                                            if (msg.StartsWith("INFORMATION:", StringComparison.OrdinalIgnoreCase)
                                                || msg.StartsWith("INFO:", StringComparison.OrdinalIgnoreCase)
                                                || msg.StartsWith("WARNING:", StringComparison.OrdinalIgnoreCase)
                                                || msg.StartsWith("WARN:", StringComparison.OrdinalIgnoreCase)
                                                || msg.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase)
                                                || msg.StartsWith("ERR:", StringComparison.OrdinalIgnoreCase))
#pragma warning restore S1067 // Expressions should not be too complex
                                            {
                                                Diag(DiagDescriptors.RedundantQualifierInMessage, ma.GetLocation(), method.Identifier.ToString());
                                            }
                                        }

                                        bool foundException = false;
                                        foreach (var p in method.ParameterList.Parameters)
                                        {
                                            var paramName = p.Identifier.ToString();
                                            if (string.IsNullOrWhiteSpace(paramName))
                                            {
                                                // semantic problem, just bail quietly
                                                keep = false;
                                                break;
                                            }

                                            var paramSymbol = sm.GetTypeInfo(p.Type!).Type;
                                            if (paramSymbol is IErrorTypeSymbol)
                                            {
                                                // semantic problem, just bail quietly
                                                keep = false;
                                                break;
                                            }

                                            var declaredType = sm.GetDeclaredSymbol(p);
                                            var typeName = declaredType!.ToDisplayString();

                                            // skip the ILogger parameter
                                            if (p == method.ParameterList.Parameters[0])
                                            {
                                                if (!IsBaseOrIdentity(paramSymbol!, loggerSymbol))
                                                {
                                                    Diag(DiagDescriptors.ErrorFirstArgMustBeILogger, p.Identifier.GetLocation());
                                                    keep = false;
                                                }
                                                else
                                                {
                                                    lm.LoggerType = typeName;
                                                    lm.LoggerName = paramName;
                                                    continue;
                                                }
                                            }

                                            var lp = new LoggerParameter
                                            {
                                                Name = paramName,
                                                Type = typeName,
                                                IsException = !foundException && IsBaseOrIdentity(paramSymbol!, exceptionSymbol),
                                            };

                                            if (lp.IsException)
                                            {
                                                foundException = true;
                                            }

                                            if (IsBaseOrIdentity(paramSymbol!, dateTimeSymbol))
                                            {
                                                Diag(DiagDescriptors.PassingDateTime, p.Identifier.GetLocation());
                                            }

                                            if (lp.IsException && templates != null)
                                            {
                                                foreach (var t in templates)
                                                {
                                                    if (t == paramName)
                                                    {
                                                        Diag(DiagDescriptors.DontMentionExceptionInMessage, p.Identifier.GetLocation(), paramName);
                                                    }
                                                }
                                            }

                                            if (lp.Name[0] == '_')
                                            {
                                                // can't have logging method parameter names that start with _ since that can lead to conflicting symbol names
                                                // because all generated symbols start with _
                                                Diag(DiagDescriptors.ErrorInvalidParameterName, p.Identifier.GetLocation());
                                            }

                                            if (!lp.IsException)
                                            {
                                                bool found = false;
                                                if (templates != null)
                                                {
                                                    foreach (var t in templates)
                                                    {
                                                        if (t.Equals(lp.Name, StringComparison.Ordinal))
                                                        {
                                                            found = true;
                                                            break;
                                                        }
                                                    }
                                                }

                                                if (!found)
                                                {
                                                    Diag(DiagDescriptors.ArgumentHasNoCorrespondingTemplate, p.Identifier.GetLocation(), lp.Name);
                                                }
                                            }

                                            lm.Parameters.Add(lp);
                                        }

                                        if (templates != null)
                                        {
                                            foreach (var t in templates)
                                            {
                                                bool found = false;
                                                foreach (var p in lm.Parameters)
                                                {
                                                    if (t.Equals(p.Name, StringComparison.Ordinal))
                                                    {
                                                        found = true;
                                                        break;
                                                    }
                                                }

                                                if (!found)
                                                {
                                                    Diag(DiagDescriptors.TemplateHasNoCorrespondingArgument, ma.GetLocation(), t);
                                                }
                                            }
                                        }

                                        if (lc == null)
                                        {
                                            // determine the namespace the class is declared in, if any
                                            var ns = classDef.Parent as NamespaceDeclarationSyntax;
                                            if (ns == null)
                                            {
                                                if (classDef.Parent is not CompilationUnitSyntax)
                                                {
                                                    // since this generator doesn't know how to generate a nested type...
                                                    Diag(DiagDescriptors.ErrorNestedType, classDef.Identifier.GetLocation());
                                                    keep = false;
                                                }
                                            }
                                            else
                                            {
                                                nspace = ns.Name.ToString();
                                                while (true)
                                                {
                                                    ns = ns.Parent as NamespaceDeclarationSyntax;
                                                    if (ns == null)
                                                    {
                                                        break;
                                                    }

                                                    nspace = $"{ns.Name}.{nspace}";
                                                }
                                            }
                                        }

                                        if (keep)
                                        {
                                            lc ??= new LoggerClass
                                            {
                                                Namespace = nspace,
                                                Name = classDef.Identifier.ToString() + classDef.TypeParameterList,
                                                Constraints = classDef.ConstraintClauses.ToString(),
                                            };

                                            lc.Methods.Add(lm);
                                        }
                                    }
                                }
                            }
                        }

                        if (lc != null)
                        {
                            results.Add(lc);
                        }
                    }
                }

                return results;
            }

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
            private void Diag(DiagnosticDescriptor desc, Location? location, params object?[]? messageArgs)
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly
            {
                _reportDiagnostic(Diagnostic.Create(desc, location, messageArgs));
            }

            private bool IsBaseOrIdentity(ITypeSymbol source, ITypeSymbol dest)
            {
                var conversion = _compilation.ClassifyConversion(source, dest);
                return conversion.IsIdentity || (conversion.IsReference && conversion.IsImplicit);
            }

            private static readonly char[] _formatDelimiters = { ',', ':' };

            /// <summary>
            /// Finds the template arguments contained in the message string.
            /// </summary>
            private static List<string> ExtractTemplateArgs(string message)
            {
                var args = new List<string>();
                var scanIndex = 0;
                var endIndex = message.Length;

                while (scanIndex < endIndex)
                {
                    var openBraceIndex = FindBraceIndex(message, '{', scanIndex, endIndex);
                    var closeBraceIndex = FindBraceIndex(message, '}', openBraceIndex, endIndex);

                    if (closeBraceIndex == endIndex)
                    {
                        scanIndex = endIndex;
                    }
                    else
                    {
                        // Format item syntax : { index[,alignment][ :formatString] }.
                        var formatDelimiterIndex = FindIndexOfAny(message, _formatDelimiters, openBraceIndex, closeBraceIndex);

                        args.Add(message.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1));
                        scanIndex = closeBraceIndex + 1;
                    }
                }

                return args;
            }

            private static int FindBraceIndex(string message, char brace, int startIndex, int endIndex)
            {
                // Example: {{prefix{{{Argument}}}suffix}}.
                var braceIndex = endIndex;
                var scanIndex = startIndex;
                var braceOccurrenceCount = 0;

                while (scanIndex < endIndex)
                {
                    if (braceOccurrenceCount > 0 && message[scanIndex] != brace)
                    {
#pragma warning disable S109 // Magic numbers should not be used
                        if (braceOccurrenceCount % 2 == 0)
#pragma warning restore S109 // Magic numbers should not be used
                        {
                            // Even number of '{' or '}' found. Proceed search with next occurrence of '{' or '}'.
                            braceOccurrenceCount = 0;
                            braceIndex = endIndex;
                        }
                        else
                        {
                            // An unescaped '{' or '}' found.
                            break;
                        }
                    }
                    else if (message[scanIndex] == brace)
                    {
                        if (brace == '}')
                        {
                            if (braceOccurrenceCount == 0)
                            {
                                // For '}' pick the first occurrence.
                                braceIndex = scanIndex;
                            }
                        }
                        else
                        {
                            // For '{' pick the last occurrence.
                            braceIndex = scanIndex;
                        }

                        braceOccurrenceCount++;
                    }

                    scanIndex++;
                }

                return braceIndex;
            }

            private static int FindIndexOfAny(string message, char[] chars, int startIndex, int endIndex)
            {
                var findIndex = message.IndexOfAny(chars, startIndex, endIndex - startIndex);
                return findIndex == -1 ? endIndex : findIndex;
            }
        }

#pragma warning disable SA1401 // Fields should be private

        /// <summary>
        /// A logger class holding a bunch of logger methods.
        /// </summary>
        internal class LoggerClass
        {
            public readonly List<LoggerMethod> Methods = new ();
            public string Namespace = string.Empty;
            public string Name = string.Empty;
            public string Constraints = string.Empty;
        }

        /// <summary>
        /// A logger method in a logger class.
        /// </summary>
        internal class LoggerMethod
        {
            public readonly List<LoggerParameter> Parameters = new ();
            public string Name = string.Empty;
            public string Message = string.Empty;
            public int Level;
            public string EventId = string.Empty;
            public string EventName = string.Empty;
            public bool MessageHasTemplates;
            public bool IsExtensionMethod;
            public string Modifiers = string.Empty;
            public string LoggerType = string.Empty;
            public string LoggerName = string.Empty;
        }

        /// <summary>
        /// A single parameter to a logger method.
        /// </summary>
        internal class LoggerParameter
        {
            public string Name = string.Empty;
            public string Type = string.Empty;
            public bool IsException;
        }
    }
}
