// © Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

                var loggerMessageAttribute = _compilation.GetTypeByMetadataName(LoggerMessageAttribute);
                if (loggerMessageAttribute is null)
                {
                    Diag(DiagDescriptors.ErrorMissingRequiredType, null, LoggerMessageAttribute);
                    return results;
                }

                var loggerSymbol = _compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.ILogger");
                if (loggerSymbol == null)
                {
                    Diag(DiagDescriptors.ErrorMissingRequiredType, null, "Microsoft.Extensions.Logging.ILogger");
                    return results;
                }

                var ids = new HashSet<string>();

                // we enumerate by syntax tree, to minimize the need to instantiate semantic models (since they're expensive)
                foreach (var group in classes.GroupBy(x => x.SyntaxTree))
                {
                    SemanticModel? sm = null;
                    foreach (var classDef in group)
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            // be nice and stop if we're asked to
                            return results;
                        }

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
                                    if (sm == null)
                                    {
                                        // need a semantic model for this tree
                                        sm = _compilation.GetSemanticModel(classDef.SyntaxTree);
                                    }

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
                                        var lm = new LoggerMethod
                                        {
                                            Name = method.Identifier.ToString(),
                                            Level = level,
                                            Message = message,
                                            EventId = eventId,
                                            EventName = eventName,
                                            MessageHasTemplates = HasTemplates(message),
                                            IsExtensionMethod = methodSymbol.IsExtensionMethod,
                                            Modifiers = method.Modifiers.ToString(),
                                        };

                                        bool keep = true;   // whether or not we want to keep the method definition or if it's got errors making it worth discarding instead
                                        if (lm.Name.StartsWith("_", StringComparison.Ordinal))
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

                                                lm.LoggerType = typeName;
                                                lm.LoggerName = paramName;
                                                continue;
                                            }

                                            var lp = new LoggerParameter
                                            {
                                                Name = paramName,
                                                Type = typeName,
                                                IsExceptionType = IsBaseOrIdentity(paramSymbol!, exceptionSymbol),
                                            };

                                            lm.Parameters.Add(lp);

                                            if (lp.Name.StartsWith("_", StringComparison.Ordinal))
                                            {
                                                // can't have logging method parameter names that start with _ since that can lead to conflicting symbol names
                                                // because all generated symbols start with _
                                                Diag(DiagDescriptors.ErrorInvalidParameterName, p.Identifier.GetLocation());
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
                                            if (lc == null)
                                            {
                                                lc = new LoggerClass
                                                {
                                                    Namespace = nspace,
                                                    Name = classDef.Identifier.ToString() + classDef.TypeParameterList ?? string.Empty,
                                                    Constraints = classDef.ConstraintClauses.ToString(),
                                                };
                                            }

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

            /// <summary>
            /// Checks if a string contain templates.
            /// </summary>
            private static bool HasTemplates(string message)
            {
                int start = message.IndexOf('{');
                if (start < 0)
                {
                    return false;
                }

#pragma warning disable S2692 // "IndexOf" checks should not be for positive numbers
                return message.IndexOf('}', start) > 0;
#pragma warning restore S2692 // "IndexOf" checks should not be for positive numbers
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
        }

#pragma warning disable SA1401 // Fields should be private

        /// <summary>
        /// A logger class holding a bunch of logger methods.
        /// </summary>
        internal class LoggerClass
        {
            public string Namespace = string.Empty;
            public string Name = string.Empty;
            public string Constraints = string.Empty;
            public List<LoggerMethod> Methods = new ();
        }

        /// <summary>
        /// A logger method in a logger class.
        /// </summary>
        internal class LoggerMethod
        {
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
            public List<LoggerParameter> Parameters = new ();
        }

        /// <summary>
        /// A single parameter to a logger method.
        /// </summary>
        internal class LoggerParameter
        {
            public string Name = string.Empty;
            public string Type = string.Empty;
            public bool IsExceptionType;
        }
    }
}
