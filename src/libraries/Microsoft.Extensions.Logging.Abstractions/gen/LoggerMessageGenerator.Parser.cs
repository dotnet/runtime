// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
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

                INamedTypeSymbol loggerMessageAttribute = _compilation.GetTypeByMetadataName(LoggerMessageAttribute);
                if (loggerMessageAttribute == null)
                {
                    // nothing to do if this type isn't available
                    return Array.Empty<LoggerClass>();
                }

                INamedTypeSymbol loggerSymbol = _compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.ILogger");
                if (loggerSymbol == null)
                {
                    // nothing to do if this type isn't available
                    return Array.Empty<LoggerClass>();
                }

                INamedTypeSymbol logLevelSymbol = _compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.LogLevel");
                if (logLevelSymbol == null)
                {
                    // nothing to do if this type isn't available
                    return Array.Empty<LoggerClass>();
                }

                INamedTypeSymbol exceptionSymbol = _compilation.GetTypeByMetadataName("System.Exception");
                if (exceptionSymbol == null)
                {
                    Diag(DiagnosticDescriptors.MissingRequiredType, null, "System.Exception");
                    return Array.Empty<LoggerClass>();
                }

                INamedTypeSymbol enumerableSymbol = _compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
                INamedTypeSymbol stringSymbol = _compilation.GetSpecialType(SpecialType.System_String);

                var results = new List<LoggerClass>();
                var ids = new HashSet<int>();

                // we enumerate by syntax tree, to minimize the need to instantiate semantic models (since they're expensive)
                foreach (var group in classes.GroupBy(x => x.SyntaxTree))
                {
                    SemanticModel? sm = null;
                    foreach (ClassDeclarationSyntax classDec in group)
                    {
                        // stop if we're asked to
                        _cancellationToken.ThrowIfCancellationRequested();

                        LoggerClass? lc = null;
                        string nspace = string.Empty;
                        string? loggerField = null;
                        bool multipleLoggerFields = false;

                        ids.Clear();
                        foreach (var member in classDec.Members)
                        {
                            var method = member as MethodDeclarationSyntax;
                            if (method == null)
                            {
                                // we only care about methods
                                continue;
                            }

                            sm ??= _compilation.GetSemanticModel(classDec.SyntaxTree);

                            foreach (AttributeListSyntax mal in method.AttributeLists)
                            {
                                foreach (AttributeSyntax ma in mal.Attributes)
                                {
                                    IMethodSymbol attrCtorSymbol = sm.GetSymbolInfo(ma, _cancellationToken).Symbol as IMethodSymbol;
                                    if (attrCtorSymbol == null || !loggerMessageAttribute.Equals(attrCtorSymbol.ContainingType, SymbolEqualityComparer.Default))
                                    {
                                        // badly formed attribute definition, or not the right attribute
                                        continue;
                                    }

                                    (int eventId, int? level, string message, string? eventName) = ExtractAttributeValues(ma.ArgumentList!, sm);

                                    IMethodSymbol? methodSymbol = sm.GetDeclaredSymbol(method, _cancellationToken);
                                    if (methodSymbol != null)
                                    {
                                        var lm = new LoggerMethod
                                        {
                                            Name = methodSymbol.Name,
                                            Level = level,
                                            Message = message,
                                            EventId = eventId,
                                            EventName = eventName,
                                            IsExtensionMethod = methodSymbol.IsExtensionMethod,
                                            Modifiers = method.Modifiers.ToString(),
                                        };

                                        ExtractTemplates(message, lm.TemplateMap, lm.TemplateList);

                                        bool keepMethod = true;   // whether or not we want to keep the method definition or if it's got errors making it so we should discard it instead
                                        if (lm.Name[0] == '_')
                                        {
                                            // can't have logging method names that start with _ since that can lead to conflicting symbol names
                                            // because the generated symbols start with _
                                            Diag(DiagnosticDescriptors.InvalidLoggingMethodName, method.Identifier.GetLocation());
                                            keepMethod = false;
                                        }

                                        if (!methodSymbol.ReturnsVoid)
                                        {
                                            // logging methods must return void
                                            Diag(DiagnosticDescriptors.LoggingMethodMustReturnVoid, method.ReturnType.GetLocation());
                                            keepMethod = false;
                                        }

                                        if (method.Arity > 0)
                                        {
                                            // we don't currently support generic methods
                                            Diag(DiagnosticDescriptors.LoggingMethodIsGeneric, method.Identifier.GetLocation());
                                            keepMethod = false;
                                        }

                                        bool isStatic = false;
                                        bool isPartial = false;
                                        foreach (SyntaxToken mod in method.Modifiers)
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

                                        if (!isPartial)
                                        {
                                            Diag(DiagnosticDescriptors.LoggingMethodMustBePartial, method.GetLocation());
                                            keepMethod = false;
                                        }

                                        if (method.Body != null)
                                        {
                                            Diag(DiagnosticDescriptors.LoggingMethodHasBody, method.Body.GetLocation());
                                            keepMethod = false;
                                        }

                                        // ensure there are no duplicate ids.
                                        if (ids.Contains(lm.EventId))
                                        {
                                            Diag(DiagnosticDescriptors.ShouldntReuseEventIds, ma.GetLocation(), lm.EventId, classDec.Identifier.Text);
                                        }
                                        else
                                        {
                                            _ = ids.Add(lm.EventId);
                                        }

                                        string msg = lm.Message;
                                        if (msg.StartsWith("INFORMATION:", StringComparison.OrdinalIgnoreCase)
                                            || msg.StartsWith("INFO:", StringComparison.OrdinalIgnoreCase)
                                            || msg.StartsWith("WARNING:", StringComparison.OrdinalIgnoreCase)
                                            || msg.StartsWith("WARN:", StringComparison.OrdinalIgnoreCase)
                                            || msg.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase)
                                            || msg.StartsWith("ERR:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            Diag(DiagnosticDescriptors.RedundantQualifierInMessage, ma.GetLocation(), method.Identifier.ToString());
                                        }

                                        bool foundLogger = false;
                                        bool foundException = false;
                                        bool foundLogLevel = level != null;
                                        foreach (IParameterSymbol paramSymbol in methodSymbol.Parameters)
                                        {
                                            string paramName = paramSymbol.Name;
                                            if (string.IsNullOrWhiteSpace(paramName))
                                            {
                                                // semantic problem, just bail quietly
                                                keepMethod = false;
                                                break;
                                            }

                                            ITypeSymbol paramTypeSymbol = paramSymbol!.Type;
                                            if (paramTypeSymbol is IErrorTypeSymbol)
                                            {
                                                // semantic problem, just bail quietly
                                                keepMethod = false;
                                                break;
                                            }

                                            string typeName = paramTypeSymbol.ToDisplayString(
                                                SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                                                    SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));

                                            var lp = new LoggerParameter
                                            {
                                                Name = paramName,
                                                Type = typeName,
                                                IsLogger = !foundLogger && IsBaseOrIdentity(paramTypeSymbol!, loggerSymbol),
                                                IsException = !foundException && IsBaseOrIdentity(paramTypeSymbol!, exceptionSymbol),
                                                IsLogLevel = !foundLogLevel && IsBaseOrIdentity(paramTypeSymbol!, logLevelSymbol),
                                                IsEnumerable = IsBaseOrIdentity(paramTypeSymbol!, enumerableSymbol) && !IsBaseOrIdentity(paramTypeSymbol!, stringSymbol),
                                            };

                                            foundLogger |= lp.IsLogger;
                                            foundException |= lp.IsException;
                                            foundLogLevel |= lp.IsLogLevel;

                                            if (lp.IsLogger && lm.TemplateMap.ContainsKey(paramName))
                                            {
                                                Diag(DiagnosticDescriptors.ShouldntMentionLoggerInMessage, paramSymbol.Locations[0], paramName);
                                            }
                                            else if (lp.IsException && lm.TemplateMap.ContainsKey(paramName))
                                            {
                                                Diag(DiagnosticDescriptors.ShouldntMentionExceptionInMessage, paramSymbol.Locations[0], paramName);
                                            }
                                            else if (lp.IsLogLevel && lm.TemplateMap.ContainsKey(paramName))
                                            {
                                                Diag(DiagnosticDescriptors.ShouldntMentionLogLevelInMessage, paramSymbol.Locations[0], paramName);
                                            }
                                            else if (lp.IsLogLevel && level != null && !lm.TemplateMap.ContainsKey(paramName))
                                            {
                                                Diag(DiagnosticDescriptors.ArgumentHasNoCorrespondingTemplate, paramSymbol.Locations[0], paramName);
                                            }
                                            else if (lp.IsTemplateParameter && !lm.TemplateMap.ContainsKey(paramName))
                                            {
                                                Diag(DiagnosticDescriptors.ArgumentHasNoCorrespondingTemplate, paramSymbol.Locations[0], paramName);
                                            }

                                            if (paramName[0] == '_')
                                            {
                                                // can't have logging method parameter names that start with _ since that can lead to conflicting symbol names
                                                // because all generated symbols start with _
                                                Diag(DiagnosticDescriptors.InvalidLoggingMethodParameterName, paramSymbol.Locations[0]);
                                            }

                                            lm.AllParameters.Add(lp);
                                            if (lp.IsTemplateParameter)
                                            {
                                                lm.TemplateParameters.Add(lp);
                                            }
                                        }

                                        if (keepMethod)
                                        {
                                            if (isStatic && !foundLogger)
                                            {
                                                Diag(DiagnosticDescriptors.MissingLoggerArgument, method.GetLocation(), lm.Name);
                                                keepMethod = false;
                                            }
                                            else if (!isStatic && foundLogger)
                                            {
                                                Diag(DiagnosticDescriptors.LoggingMethodShouldBeStatic, method.GetLocation());
                                            }
                                            else if (!isStatic && !foundLogger)
                                            {
                                                if (loggerField == null)
                                                {
                                                    (loggerField, multipleLoggerFields) = FindLoggerField(sm, classDec, loggerSymbol);
                                                }

                                                if (multipleLoggerFields)
                                                {
                                                    Diag(DiagnosticDescriptors.MultipleLoggerFields, method.GetLocation(), classDec.Identifier.Text);
                                                    keepMethod = false;
                                                }
                                                else if (loggerField == null)
                                                {
                                                    Diag(DiagnosticDescriptors.MissingLoggerField, method.GetLocation(), classDec.Identifier.Text);
                                                    keepMethod = false;
                                                }
                                                else
                                                {
                                                    lm.LoggerField = loggerField;
                                                }
                                            }

                                            if (level == null && !foundLogLevel)
                                            {
                                                Diag(DiagnosticDescriptors.MissingLogLevel, method.GetLocation());
                                                keepMethod = false;
                                            }

                                            foreach (KeyValuePair<string, string> t in lm.TemplateMap)
                                            {
                                                bool found = false;
                                                foreach (LoggerParameter p in lm.AllParameters)
                                                {
                                                    if (t.Key.Equals(p.Name, StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        found = true;
                                                        break;
                                                    }
                                                }

                                                if (!found)
                                                {
                                                    Diag(DiagnosticDescriptors.TemplateHasNoCorrespondingArgument, ma.GetLocation(), t.Key);
                                                }
                                            }
                                        }

                                        if (lc == null)
                                        {
                                            // determine the namespace the class is declared in, if any
                                            var ns = classDec.Parent as NamespaceDeclarationSyntax;
                                            if (ns == null)
                                            {
                                                if (classDec.Parent is not CompilationUnitSyntax)
                                                {
                                                    // since this generator doesn't know how to generate a nested type...
                                                    Diag(DiagnosticDescriptors.LoggingMethodInNestedType, classDec.Identifier.GetLocation());
                                                    keepMethod = false;
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

                                        if (keepMethod)
                                        {
                                            lc ??= new LoggerClass
                                            {
                                                Namespace = nspace,
                                                Name = classDec.Identifier.ToString() + classDec.TypeParameterList,
                                                Constraints = classDec.ConstraintClauses.ToString(),
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

            private (string? loggerField, bool multipleLoggerFields) FindLoggerField(SemanticModel sm, TypeDeclarationSyntax classDec, ITypeSymbol loggerSymbol)
            {
                string? loggerField = null;

                foreach (MemberDeclarationSyntax m in classDec.Members)
                {
                    if (m is FieldDeclarationSyntax fds)
                    {
                        foreach (VariableDeclaratorSyntax v in fds.Declaration.Variables)
                        {
                            var fs = sm.GetDeclaredSymbol(v, _cancellationToken) as IFieldSymbol;
                            if (fs != null)
                            {
                                if (IsBaseOrIdentity(fs.Type, loggerSymbol))
                                {
                                    if (loggerField == null)
                                    {
                                        loggerField = v.Identifier.Text;
                                    }
                                    else
                                    {
                                        return (null, true);
                                    }
                                }
                            }
                        }
                    }
                }

                return (loggerField, false);
            }

            private (int eventId, int? level, string message, string? eventName) ExtractAttributeValues(AttributeArgumentListSyntax args, SemanticModel sm)
            {
                int eventId = 0;
                int? level = null;
                string? eventName = null;
                string message = string.Empty;
                foreach (AttributeArgumentSyntax a in args.Arguments)
                {
                    // argument syntax takes parameters. e.g. EventId = 0
                    Debug.Assert(a.NameEquals != null);
                    switch (a.NameEquals.Name.ToString())
                    {
                        case "EventId":
                            eventId = (int)sm.GetConstantValue(a.Expression, _cancellationToken).Value!;
                            break;
                        case "EventName":
                            eventName = sm.GetConstantValue(a.Expression, _cancellationToken).ToString();
                            break;
                        case "Level":
                            level = (int)sm.GetConstantValue(a.Expression, _cancellationToken).Value!;
                            break;
                        case "Message":
                            message = sm.GetConstantValue(a.Expression, _cancellationToken).ToString();
                            break;
                    }
                }
                return (eventId, level, message, eventName);
            }

            private void Diag(DiagnosticDescriptor desc, Location? location, params object?[]? messageArgs)
            {
                _reportDiagnostic(Diagnostic.Create(desc, location, messageArgs));
            }

            private bool IsBaseOrIdentity(ITypeSymbol source, ITypeSymbol dest)
            {
                Conversion conversion = _compilation.ClassifyConversion(source, dest);
                return conversion.IsIdentity || (conversion.IsReference && conversion.IsImplicit);
            }

            private static readonly char[] _formatDelimiters = { ',', ':' };

            /// <summary>
            /// Finds the template arguments contained in the message string.
            /// </summary>
            private static void ExtractTemplates(string? message, IDictionary<string, string> templateMap, ICollection<string> templateList)
            {
                if (string.IsNullOrEmpty(message))
                {
                    return;
                }

                int scanIndex = 0;
                int endIndex = message!.Length;

                while (scanIndex < endIndex)
                {
                    int openBraceIndex = FindBraceIndex(message, '{', scanIndex, endIndex);
                    int closeBraceIndex = FindBraceIndex(message, '}', openBraceIndex, endIndex);

                    if (closeBraceIndex == endIndex)
                    {
                        scanIndex = endIndex;
                    }
                    else
                    {
                        // Format item syntax : { index[,alignment][ :formatString] }.
                        int formatDelimiterIndex = FindIndexOfAny(message, _formatDelimiters, openBraceIndex, closeBraceIndex);

                        string templateName = message.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1);
                        templateMap[templateName] = templateName;
                        templateList.Add(templateName);
                        scanIndex = closeBraceIndex + 1;
                    }
                }
            }

            private static int FindBraceIndex(string message, char brace, int startIndex, int endIndex)
            {
                // Example: {{prefix{{{Argument}}}suffix}}.
                int braceIndex = endIndex;
                int scanIndex = startIndex;
                int braceOccurrenceCount = 0;

                while (scanIndex < endIndex)
                {
                    if (braceOccurrenceCount > 0 && message[scanIndex] != brace)
                    {
                        if (braceOccurrenceCount % 2 == 0)
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
                int findIndex = message.IndexOfAny(chars, startIndex, endIndex - startIndex);
                return findIndex == -1 ? endIndex : findIndex;
            }

            private string GetStringExpression(SemanticModel sm, SyntaxNode expr)
            {
                Optional<object?> optional = sm.GetConstantValue(expr, _cancellationToken);
                if (optional.HasValue)
                {
                    object o = optional.Value;
                    if (o != null)
                    {
                        return o.ToString();
                    }
                }

                return string.Empty;
            }
        }

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
            public readonly List<LoggerParameter> AllParameters = new ();
            public readonly List<LoggerParameter> TemplateParameters = new ();
            public readonly Dictionary<string, string> TemplateMap = new (StringComparer.OrdinalIgnoreCase);
            public readonly List<string> TemplateList = new ();
            public string Name = string.Empty;
            public string Message = string.Empty;
            public int? Level;
            public int EventId;
            public string? EventName;
            public bool IsExtensionMethod;
            public string Modifiers = string.Empty;
            public string LoggerField = string.Empty;
        }

        /// <summary>
        /// A single parameter to a logger method.
        /// </summary>
        internal class LoggerParameter
        {
            public string Name = string.Empty;
            public string Type = string.Empty;
            public bool IsLogger;
            public bool IsException;
            public bool IsLogLevel;
            public bool IsEnumerable;
            // A parameter flagged as IsTemplateParameter is not going to be taken care of specially as an argument to ILogger.Log
            // but instead is supposed to be taken as a parameter for the template.
            public bool IsTemplateParameter => !IsLogger && !IsException && !IsLogLevel;
        }
    }
}
