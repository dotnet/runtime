// © Microsoft Corporation. All rights reserved.

[assembly: System.Resources.NeutralResourcesLanguage("en-us")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Microsoft.Extensions.Logging.Generators.Tests")]

namespace Microsoft.Extensions.Logging.Generators
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    public partial class LoggingGenerator
    {
        internal class Parser
        {
            private const string DiagnosticCategory = "LoggingGenerator";

#pragma warning disable RS2008 // Enable analyzer release tracking

            private static readonly DiagnosticDescriptor ErrorInvalidMethodName = new(
                id: "LG0000",
                title: Resources.ErrorInvalidMethodNameTitle,
                messageFormat: Resources.ErrorInvalidMethodNameMessage,
                category: DiagnosticCategory,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            private static readonly DiagnosticDescriptor ErrorInvalidMessage = new(
                id: "LG0001",
                title: Resources.ErrorInvalidMessageTitle,
                messageFormat: Resources.ErrorInvalidMessageMessage,
                category: DiagnosticCategory,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            private static readonly DiagnosticDescriptor ErrorInvalidParameterName = new(
                id: "LG0002",
                title: Resources.ErrorInvalidParameterNameTitle,
                messageFormat: Resources.ErrorInvalidParameterNameMessage,
                category: DiagnosticCategory,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            private static readonly DiagnosticDescriptor ErrorNestedType = new(
                id: "LG0003",
                title: Resources.ErrorNestedTypeTitle,
                messageFormat: Resources.ErrorNestedTypeMessage,
                category: DiagnosticCategory,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            private static readonly DiagnosticDescriptor ErrorMissingRequiredType = new(
                id: "LG0004",
                title: Resources.ErrorMissingRequiredTypeTitle,
                messageFormat: Resources.ErrorMissingRequiredTypeMessage,
                category: DiagnosticCategory,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            private static readonly DiagnosticDescriptor ErrorEventIdReuse = new(
                id: "LG0005",
                title: Resources.ErrorEventIdReuseTitle,
                messageFormat: Resources.ErrorEventIdReuseMessage,
                category: DiagnosticCategory,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            private static readonly DiagnosticDescriptor ErrorInvalidMethodReturnType = new(
                id: "LG0006",
                title: Resources.ErrorInvalidMethodReturnTypeTitle,
                messageFormat: Resources.ErrorInvalidMethodReturnTypeMessage,
                category: DiagnosticCategory,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            private static readonly DiagnosticDescriptor ErrorFirstArgMustBeILogger = new(
                id: "LG0007",
                title: Resources.ErrorFirstArgMustBeILoggerTitle,
                messageFormat: Resources.ErrorFirstArgMustBeILoggerMessage,
                category: DiagnosticCategory,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            private static readonly DiagnosticDescriptor ErrorNotStaticMethod = new(
                id: "LG0008",
                title: Resources.ErrorNotStaticMethodTitle,
                messageFormat: Resources.ErrorNotStaticMethodMessage,
                category: DiagnosticCategory,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            private static readonly DiagnosticDescriptor ErrorNotPartialMethod = new(
                id: "LG0009",
                title: Resources.ErrorNotPartialMethodTitle,
                messageFormat: Resources.ErrorNotPartialMethodMessage,
                category: DiagnosticCategory,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            private static readonly DiagnosticDescriptor ErrorMethodIsGeneric = new(
                id: "LG0010",
                title: Resources.ErrorMethodIsGenericTitle,
                messageFormat: Resources.ErrorMethodIsGenericMessage,
                category: DiagnosticCategory,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            private readonly CancellationToken _cancellationToken;
            private readonly Compilation _compilation;
            private readonly Action<Diagnostic> _reportDiagnostic;
            private readonly Dictionary<SyntaxTree, SemanticModel> _semanticModels = new();

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
                var results = new List<LoggerClass>();

                var exSymbol = _compilation.GetTypeByMetadataName("System.Exception");
                if (exSymbol == null)
                {
                    Diag(ErrorMissingRequiredType, null, "System.Exception");
                    return results;
                }

                var loggerMessageAttribute = _compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.LoggerMessageAttribute");
                if (loggerMessageAttribute is null)
                {
                    Diag(ErrorMissingRequiredType, null, "Microsoft.Extensions.Logging.LoggerMessageAttribute");
                    return results;
                }

                var loggerSymbol = _compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.ILogger");
                if (loggerSymbol == null)
                {
                    Diag(ErrorMissingRequiredType, null, "Microsoft.Extensions.Logging.ILogger");
                    return results;
                }

                var ids = new HashSet<string>();
                foreach (var classDef in classes)
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        // be nice and stop if we're asked to
                        return results;
                    }

                    LoggerClass? lc = null;
                    string nspace = string.Empty;

                    ids.Clear();
                    foreach (var method in classDef.Members.Where(m => m.IsKind(SyntaxKind.MethodDeclaration)).OfType<MethodDeclarationSyntax>())
                    {
                        foreach (var mal in method.AttributeLists)
                        {
                            foreach (var ma in mal.Attributes)
                            {
                                var sm = GetSemanticModel(ma.SyntaxTree);
                                var maSymbolInfo = sm.GetSymbolInfo(ma, _cancellationToken);
                                var maSymbol = (maSymbolInfo.Symbol as IMethodSymbol)!;

                                var methodSymbol = (sm.GetDeclaredSymbol(method, _cancellationToken) as IMethodSymbol)!;

                                if (loggerMessageAttribute.Equals(maSymbol.ContainingType, SymbolEqualityComparer.Default))
                                {
                                    var arg = ma.ArgumentList!.Arguments[0];
                                    var eventId = sm.GetConstantValue(arg.Expression, _cancellationToken).ToString();

                                    arg = ma.ArgumentList.Arguments[1];
                                    var level = (int)sm.GetConstantValue(arg.Expression, _cancellationToken).Value!;

                                    arg = ma.ArgumentList.Arguments[2];
                                    var message = sm.GetConstantValue(arg.Expression, _cancellationToken).ToString();

                                    string eventName = string.Empty;
                                    if (ma.ArgumentList.Arguments.Count > 3)
                                    {
                                        arg = ma.ArgumentList.Arguments[3];
                                        eventName = sm.GetConstantValue(arg.Expression, _cancellationToken).ToString();
                                    }

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

                                    bool keep = true;
                                    if (lm.Name.StartsWith("__", StringComparison.Ordinal))
                                    {
                                        // can't have logging method names that start with __ since that can lead to conflicting symbol names
                                        // because the generated symbols start with __
                                        Diag(ErrorInvalidMethodName, method.Identifier.GetLocation());
                                    }

                                    if (GetSemanticModel(method.ReturnType.SyntaxTree).GetTypeInfo(method.ReturnType!).Type!.SpecialType != SpecialType.System_Void)
                                    {
                                        Diag(ErrorInvalidMethodReturnType, method.ReturnType.GetLocation());
                                        keep = false;
                                    }

                                    if (method.Arity > 0)
                                    {
                                        Diag(ErrorMethodIsGeneric, method.Identifier.GetLocation());
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
                                        Diag(ErrorNotStaticMethod, method.GetLocation());
                                        keep = false;
                                    }

                                    if (!isPartial)
                                    {
                                        Diag(ErrorNotPartialMethod, method.GetLocation());
                                        keep = false;
                                    }

                                    // ensure there are no duplicate ids.
                                    if (ids.Contains(lm.EventId))
                                    {
                                        Diag(ErrorEventIdReuse, ma.ArgumentList!.Arguments[0].GetLocation(), lm.EventId);
                                    }
                              
                                    
                                    else
                                    {
                                        ids.Add(lm.EventId);
                                    }

                                    if (string.IsNullOrWhiteSpace(lm.Message))
                                    {
                                        Diag(ErrorInvalidMessage, ma.GetLocation(), method.Identifier.ToString());
                                    }

                                    bool first = true;
                                    foreach (var p in method.ParameterList.Parameters)
                                    {
//                                        var parameterSyntax = p.SyntaxTree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().First();
                                        var typeName = GetSemanticModel(p.SyntaxTree).GetDeclaredSymbol(p)!.ToDisplayString();
                                        var pSymbol = GetSemanticModel(p.SyntaxTree).GetTypeInfo(p.Type!).Type!;

                                        if (first)
                                        {
                                            // skip the ILogger
                                            first = false;

                                            if (!IsBaseOrIdentity(pSymbol, loggerSymbol))
                                            {
                                                Diag(ErrorFirstArgMustBeILogger, p.Identifier.GetLocation());
                                                keep = false;
                                            }

                                            lm.LoggerType = typeName;
                                            continue;
                                        }

                                        var lp = new LoggerParameter
                                        {
                                            Name = p.Identifier.ToString(),
                                            Type = typeName,
                                            IsExceptionType = IsBaseOrIdentity(pSymbol, exSymbol),
                                        };
                                                    
                                        lm.Parameters.Add(lp);

                                        if (lp.Name.StartsWith("__", StringComparison.Ordinal))
                                        {
                                            // can't have logging method parameter names that start with  __ since that can lead to conflicting symbol names
                                            // because all generated symbols start with __
                                            Diag(ErrorInvalidParameterName, p.Identifier.GetLocation());
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
                                                Diag(ErrorNestedType, classDef.Identifier.GetLocation());
                                                keep = false;
                                            }
                                        }
                                        else
                                        {
                                            nspace = ns.Name.ToString();
                                            for (; ; )
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
                                                Name = classDef.Identifier.ToString(),
                                                Constraints = classDef.ConstraintClauses.ToString(),
                                            };

                                            if (classDef.TypeParameterList != null)
                                            {
                                                lc.Name += classDef.TypeParameterList.ToString();
                                            }
                                        }

                                        lc.Methods.Add(lm);
                                    }

                                    break;
                                }
                            }
                        }
                    }

                    if (lc != null)
                    {
                        results.Add(lc);
                    }
                }

                return results;
            }

            private void Diag(DiagnosticDescriptor desc, Location? location, params object?[]? messageArgs)
            {
                _reportDiagnostic(Diagnostic.Create(desc, location, messageArgs));
            }

            // Workaround for https://github.com/dotnet/roslyn/pull/49330
            private SemanticModel GetSemanticModel(SyntaxTree syntaxTree)
            {
                if (!_semanticModels.TryGetValue(syntaxTree, out var semanticModel))
                {
                    semanticModel = _compilation.GetSemanticModel(syntaxTree);
                    _semanticModels[syntaxTree] = semanticModel;
                }

                return semanticModel;
            }

            private bool IsBaseOrIdentity(ITypeSymbol source, ITypeSymbol dest)
            {
                var conversion = _compilation.ClassifyConversion(source, dest);
                return conversion.IsIdentity || (conversion.IsReference && conversion.IsImplicit);
            }

            /// <summary>
            /// Does the string contain templates?
            /// </summary>
            private static bool HasTemplates(string message)
            {
                int start = message.IndexOf('{');
                if (start < 0)
                {
                    return false;
                }

                return message.IndexOf('}', start) > 0;
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
            public List<LoggerMethod> Methods = new();
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
            public List<LoggerParameter> Parameters = new();
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
