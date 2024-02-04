// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace Microsoft.Extensions.Logging.Generators
{
    public partial class LoggerMessageGenerator
    {
        internal sealed class Parser
        {
            internal const string LoggerMessageAttribute = "Microsoft.Extensions.Logging.LoggerMessageAttribute";

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
                INamedTypeSymbol? loggerMessageAttribute = _compilation.GetBestTypeByMetadataName(LoggerMessageAttribute);
                if (loggerMessageAttribute == null)
                {
                    // nothing to do if this type isn't available
                    return Array.Empty<LoggerClass>();
                }

                INamedTypeSymbol? loggerSymbol = _compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Logging.ILogger");
                if (loggerSymbol == null)
                {
                    // nothing to do if this type isn't available
                    return Array.Empty<LoggerClass>();
                }

                INamedTypeSymbol? logLevelSymbol = _compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Logging.LogLevel");
                if (logLevelSymbol == null)
                {
                    // nothing to do if this type isn't available
                    return Array.Empty<LoggerClass>();
                }

                INamedTypeSymbol? exceptionSymbol = _compilation.GetBestTypeByMetadataName("System.Exception");
                if (exceptionSymbol == null)
                {
                    Diag(DiagnosticDescriptors.MissingRequiredType, null, "System.Exception");
                    return Array.Empty<LoggerClass>();
                }

                INamedTypeSymbol enumerableSymbol = _compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
                INamedTypeSymbol stringSymbol = _compilation.GetSpecialType(SpecialType.System_String);

                var results = new List<LoggerClass>();
                var eventIds = new HashSet<int>();
                var eventNames = new HashSet<string>();

                // we enumerate by syntax tree, to minimize the need to instantiate semantic models (since they're expensive)
                foreach (IGrouping<SyntaxTree, ClassDeclarationSyntax> group in classes.GroupBy(x => x.SyntaxTree))
                {
                    SyntaxTree syntaxTree = group.Key;
                    SemanticModel sm = _compilation.GetSemanticModel(syntaxTree);

                    foreach (ClassDeclarationSyntax classDec in group)
                    {
                        // stop if we're asked to
                        _cancellationToken.ThrowIfCancellationRequested();

                        LoggerClass? lc = null;
                        string nspace = string.Empty;
                        string? loggerField = null;
                        bool multipleLoggerFields = false;

                        // events ids and names should be unique in a class
                        eventIds.Clear();
                        eventNames.Clear();

                        foreach (MemberDeclarationSyntax member in classDec.Members)
                        {
                            var method = member as MethodDeclarationSyntax;
                            if (method == null)
                            {
                                // we only care about methods
                                continue;
                            }

                            IMethodSymbol logMethodSymbol = sm.GetDeclaredSymbol(method, _cancellationToken)!;
                            Debug.Assert(logMethodSymbol != null, "log method is present.");
                            (int eventId, int? level, string message, string? eventName, bool skipEnabledCheck) = (-1, null, string.Empty, null, false);
                            bool suppliedEventId = false;

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

                                    bool hasMisconfiguredInput = false;
                                    ImmutableArray<AttributeData> boundAttributes = logMethodSymbol.GetAttributes();

                                    if (boundAttributes.Length == 0)
                                    {
                                        continue;
                                    }

                                    foreach (AttributeData attributeData in boundAttributes)
                                    {
                                        if (!SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, loggerMessageAttribute))
                                        {
                                            continue;
                                        }

                                        // supports: [LoggerMessage(0, LogLevel.Warning, "custom message")]
                                        // supports: [LoggerMessage(eventId: 0, level: LogLevel.Warning, message: "custom message")]
                                        if (attributeData.ConstructorArguments.Any())
                                        {
                                            foreach (TypedConstant typedConstant in attributeData.ConstructorArguments)
                                            {
                                                if (typedConstant.Kind == TypedConstantKind.Error)
                                                {
                                                    hasMisconfiguredInput = true;
                                                    break; // if a compilation error was found, no need to keep evaluating other args
                                                }
                                            }

                                            ImmutableArray<TypedConstant> items = attributeData.ConstructorArguments;

                                            switch (items.Length)
                                            {
                                                case 1:
                                                    // LoggerMessageAttribute(LogLevel level)
                                                    // LoggerMessageAttribute(string message)
                                                    if (items[0].Type.SpecialType == SpecialType.System_String)
                                                    {
                                                        message = (string)GetItem(items[0]);
                                                        level = null;
                                                    }
                                                    else
                                                    {
                                                        message = string.Empty;
                                                        level = items[0].IsNull ? null : (int?)GetItem(items[0]);
                                                    }
                                                    break;

                                                case 2:
                                                    // LoggerMessageAttribute(LogLevel level, string message)
                                                    level = items[0].IsNull ? null : (int?)GetItem(items[0]);
                                                    message = items[1].IsNull ? string.Empty : (string)GetItem(items[1]);
                                                    break;

                                                case 3:
                                                    // LoggerMessageAttribute(int eventId, LogLevel level, string message)
                                                    if (!items[0].IsNull)
                                                    {
                                                        suppliedEventId = true;
                                                        eventId = (int)GetItem(items[0]);
                                                    }
                                                    level = items[1].IsNull ? null : (int?)GetItem(items[1]);
                                                    message = items[2].IsNull ? string.Empty : (string)GetItem(items[2]);
                                                    break;

                                                default:
                                                    Debug.Assert(false, "Unexpected number of arguments in attribute constructor.");
                                                    break;
                                            }
                                        }

                                        // argument syntax takes parameters. e.g. EventId = 0
                                        // supports: e.g. [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "custom message")]
                                        if (attributeData.NamedArguments.Any())
                                        {
                                            foreach (KeyValuePair<string, TypedConstant> namedArgument in attributeData.NamedArguments)
                                            {
                                                TypedConstant typedConstant = namedArgument.Value;
                                                if (typedConstant.Kind == TypedConstantKind.Error)
                                                {
                                                    hasMisconfiguredInput = true;
                                                    break; // if a compilation error was found, no need to keep evaluating other args
                                                }
                                                else
                                                {
                                                    TypedConstant value = namedArgument.Value;
                                                    switch (namedArgument.Key)
                                                    {
                                                        case "EventId":
                                                            eventId = (int)GetItem(value);
                                                            suppliedEventId = true;
                                                            break;
                                                        case "Level":
                                                            level = value.IsNull ? null : (int?)GetItem(value);
                                                            break;
                                                        case "SkipEnabledCheck":
                                                            skipEnabledCheck = (bool)GetItem(value);
                                                            break;
                                                        case "EventName":
                                                            eventName = (string?)GetItem(value);
                                                            break;
                                                        case "Message":
                                                            message = value.IsNull ? string.Empty : (string)GetItem(value);
                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (hasMisconfiguredInput)
                                    {
                                        // skip further generator execution and let compiler generate the errors
                                        break;
                                    }

                                    if (!suppliedEventId)
                                    {
                                        eventId = GetNonRandomizedHashCode(string.IsNullOrWhiteSpace(eventName) ? logMethodSymbol.Name : eventName);
                                    }

                                    var lm = new LoggerMethod
                                    {
                                        Name = logMethodSymbol.Name,
                                        Level = level,
                                        Message = message,
                                        EventId = eventId,
                                        EventName = eventName,
                                        IsExtensionMethod = logMethodSymbol.IsExtensionMethod,
                                        Modifiers = method.Modifiers.ToString(),
                                        SkipEnabledCheck = skipEnabledCheck
                                    };

                                    bool keepMethod = true;   // whether or not we want to keep the method definition or if it's got errors making it so we should discard it instead

                                    bool success = ExtractTemplates(message, lm.TemplateMap, lm.TemplateList);
                                    if (!success)
                                    {
                                        Diag(DiagnosticDescriptors.MalformedFormatStrings, method.Identifier.GetLocation(), method.Identifier.ToString());
                                        keepMethod = false;
                                    }

                                    if (lm.Name[0] == '_')
                                    {
                                        // can't have logging method names that start with _ since that can lead to conflicting symbol names
                                        // because the generated symbols start with _
                                        Diag(DiagnosticDescriptors.InvalidLoggingMethodName, method.Identifier.GetLocation());
                                        keepMethod = false;
                                    }

                                    if (!logMethodSymbol.ReturnsVoid)
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
                                        if (mod.IsKind(SyntaxKind.PartialKeyword))
                                        {
                                            isPartial = true;
                                        }
                                        else if (mod.IsKind(SyntaxKind.StaticKeyword))
                                        {
                                            isStatic = true;
                                        }
                                    }

                                    if (!isPartial)
                                    {
                                        Diag(DiagnosticDescriptors.LoggingMethodMustBePartial, method.GetLocation());
                                        keepMethod = false;
                                    }

                                    CSharpSyntaxNode? methodBody = method.Body as CSharpSyntaxNode ?? method.ExpressionBody;
                                    if (methodBody != null)
                                    {
                                        Diag(DiagnosticDescriptors.LoggingMethodHasBody, methodBody.GetLocation());
                                        keepMethod = false;
                                    }

                                    // ensure there are no duplicate event ids.
                                    // We don't check Id duplication for the auto-generated event id.
                                    if (suppliedEventId && !eventIds.Add(lm.EventId))
                                    {
                                        Diag(DiagnosticDescriptors.ShouldntReuseEventIds, ma.GetLocation(), lm.EventId, classDec.Identifier.Text);
                                    }

                                    // ensure there are no duplicate event names.
                                    if (lm.EventName != null && !eventNames.Add(lm.EventName))
                                    {
                                        Diag(DiagnosticDescriptors.ShouldntReuseEventNames, ma.GetLocation(), lm.EventName, classDec.Identifier.Text);
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
                                    foreach (IParameterSymbol paramSymbol in logMethodSymbol.Parameters)
                                    {
                                        string paramName = paramSymbol.Name;
                                        bool needsAtSign = false;
                                        if (paramSymbol.DeclaringSyntaxReferences.Length > 0)
                                        {
                                            ParameterSyntax paramSyntax = paramSymbol.DeclaringSyntaxReferences[0].GetSyntax(_cancellationToken) as ParameterSyntax;
                                            if (paramSyntax != null && !string.IsNullOrEmpty(paramSyntax.Identifier.Text))
                                            {
                                                needsAtSign = paramSyntax.Identifier.Text[0] == '@';
                                            }
                                        }
                                        if (string.IsNullOrWhiteSpace(paramName))
                                        {
                                            // semantic problem, just bail quietly
                                            keepMethod = false;
                                            break;
                                        }

                                        ITypeSymbol paramTypeSymbol = paramSymbol.Type;
                                        if (paramTypeSymbol is IErrorTypeSymbol)
                                        {
                                            // semantic problem, just bail quietly
                                            keepMethod = false;
                                            break;
                                        }

                                        string? qualifier = null;
                                        if (paramSymbol.RefKind == RefKind.In)
                                        {
                                            qualifier = "in";
                                        }
                                        else if (paramSymbol.RefKind == RefKind.Ref)
                                        {
                                            qualifier = "ref";
                                        }
                                        else if (paramSymbol.RefKind == RefKind.Out)
                                        {
                                            Diag(DiagnosticDescriptors.InvalidLoggingMethodParameterOut, paramSymbol.Locations[0], paramName);
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
                                            Qualifier = qualifier,
                                            CodeName = needsAtSign ? "@" + paramName : paramName,
                                            IsLogger = !foundLogger && IsBaseOrIdentity(paramTypeSymbol, loggerSymbol),
                                            IsException = !foundException && IsBaseOrIdentity(paramTypeSymbol, exceptionSymbol),
                                            IsLogLevel = !foundLogLevel && IsBaseOrIdentity(paramTypeSymbol, logLevelSymbol),
                                            IsEnumerable = IsBaseOrIdentity(paramTypeSymbol, enumerableSymbol) && !IsBaseOrIdentity(paramTypeSymbol, stringSymbol),
                                        };

                                        foundLogger |= lp.IsLogger;
                                        foundException |= lp.IsException;
                                        foundLogLevel |= lp.IsLogLevel;

                                        bool forceAsTemplateParams = false;
                                        if (lp.IsLogger && lm.TemplateMap.ContainsKey(paramName))
                                        {
                                            Diag(DiagnosticDescriptors.ShouldntMentionLoggerInMessage, paramSymbol.Locations[0], paramName);
                                            forceAsTemplateParams = true;
                                        }
                                        else if (lp.IsException && lm.TemplateMap.ContainsKey(paramName))
                                        {
                                            Diag(DiagnosticDescriptors.ShouldntMentionExceptionInMessage, paramSymbol.Locations[0], paramName);
                                            forceAsTemplateParams = true;
                                        }
                                        else if (lp.IsLogLevel && lm.TemplateMap.ContainsKey(paramName))
                                        {
                                            Diag(DiagnosticDescriptors.ShouldntMentionLogLevelInMessage, paramSymbol.Locations[0], paramName);
                                            forceAsTemplateParams = true;
                                        }
                                        else if (lp.IsLogLevel && level != null && !lm.TemplateMap.ContainsKey(paramName) && !lm.TemplateMap.ContainsKey(lp.CodeName))
                                        {
                                            Diag(DiagnosticDescriptors.ArgumentHasNoCorrespondingTemplate, paramSymbol.Locations[0], paramName);
                                        }
                                        else if (lp.IsTemplateParameter && !lm.TemplateMap.ContainsKey(paramName) && !lm.TemplateMap.ContainsKey($"@{paramName}") && !lm.TemplateMap.ContainsKey(lp.CodeName))
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
                                        if (lp.IsTemplateParameter || forceAsTemplateParams)
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
                                                if (t.Key.Equals(p.Name, StringComparison.OrdinalIgnoreCase) ||
                                                    t.Key.Equals(p.CodeName, StringComparison.OrdinalIgnoreCase) ||
                                                    t.Key[0] == '@' && t.Key.Substring(1).Equals(p.CodeName, StringComparison.OrdinalIgnoreCase))
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
                                        SyntaxNode? potentialNamespaceParent = classDec.Parent;
                                        while (potentialNamespaceParent != null &&
                                               potentialNamespaceParent is not NamespaceDeclarationSyntax
#if ROSLYN4_0_OR_GREATER
                                               && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax
#endif
                                               )
                                        {
                                            potentialNamespaceParent = potentialNamespaceParent.Parent;
                                        }

#if ROSLYN4_0_OR_GREATER
                                        if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
#else
                                            if (potentialNamespaceParent is NamespaceDeclarationSyntax namespaceParent)
#endif
                                        {
                                            nspace = namespaceParent.Name.ToString();
                                            while (true)
                                            {
                                                namespaceParent = namespaceParent.Parent as NamespaceDeclarationSyntax;
                                                if (namespaceParent == null)
                                                {
                                                    break;
                                                }

                                                nspace = $"{namespaceParent.Name}.{nspace}";
                                            }
                                        }
                                    }

                                    if (keepMethod)
                                    {
                                        lc ??= new LoggerClass
                                        {
                                            Keyword = classDec.Keyword.ValueText,
                                            Namespace = nspace,
                                            Name = classDec.Identifier.ToString() + classDec.TypeParameterList,
                                            ParentClass = null,
                                        };

                                        LoggerClass currentLoggerClass = lc;
                                        var parentLoggerClass = (classDec.Parent as TypeDeclarationSyntax);

                                        static bool IsAllowedKind(SyntaxKind kind) =>
                                            kind == SyntaxKind.ClassDeclaration ||
                                            kind == SyntaxKind.StructDeclaration ||
                                            kind == SyntaxKind.RecordDeclaration;

                                        while (parentLoggerClass != null && IsAllowedKind(parentLoggerClass.Kind()))
                                        {
                                            currentLoggerClass.ParentClass = new LoggerClass
                                            {
                                                Keyword = parentLoggerClass.Keyword.ValueText,
                                                Namespace = nspace,
                                                Name = parentLoggerClass.Identifier.ToString() + parentLoggerClass.TypeParameterList,
                                                ParentClass = null,
                                            };

                                            currentLoggerClass = currentLoggerClass.ParentClass;
                                            parentLoggerClass = (parentLoggerClass.Parent as TypeDeclarationSyntax);
                                        }

                                        lc.Methods.Add(lm);
                                    }
                                }
                            }
                        }

                        if (lc != null)
                        {
                            //once we've collected all methods for the given class, check for overloads
                            //and provide unique names for logger methods
                            var methods = new Dictionary<string, int>(lc.Methods.Count);
                            foreach (LoggerMethod lm in lc.Methods)
                            {
                                if (methods.TryGetValue(lm.Name, out int currentCount))
                                {
                                    lm.UniqueName = $"{lm.Name}{currentCount}";
                                    methods[lm.Name] = currentCount + 1;
                                }
                                else
                                {
                                    lm.UniqueName = lm.Name;
                                    methods[lm.Name] = 1; //start from 1
                                }
                            }
                            results.Add(lc);
                        }
                    }
                }

                if (results.Count > 0 && _compilation is CSharpCompilation { LanguageVersion : LanguageVersion version and < LanguageVersion.CSharp8 })
                {
                    // we only support C# 8.0 and above
                    Diag(DiagnosticDescriptors.LoggingUnsupportedLanguageVersion, null, version.ToDisplayString(), LanguageVersion.CSharp8.ToDisplayString());
                    return Array.Empty<LoggerClass>();
                }

                return results;
            }

            private (string? loggerField, bool multipleLoggerFields) FindLoggerField(SemanticModel sm, TypeDeclarationSyntax classDec, ITypeSymbol loggerSymbol)
            {
                string? loggerField = null;

                INamedTypeSymbol? classType = sm.GetDeclaredSymbol(classDec, _cancellationToken);

                bool onMostDerivedType = true;

                while (classType is { SpecialType: not SpecialType.System_Object })
                {
                    foreach (IFieldSymbol fs in classType.GetMembers().OfType<IFieldSymbol>())
                    {
                        if (!onMostDerivedType && fs.DeclaredAccessibility == Accessibility.Private)
                        {
                            continue;
                        }
                        if (IsBaseOrIdentity(fs.Type, loggerSymbol))
                        {
                            if (loggerField == null)
                            {
                                loggerField = fs.Name;
                            }
                            else
                            {
                                return (null, true);
                            }
                        }
                    }

                    onMostDerivedType = false;
                    classType = classType.BaseType;
                }

                return (loggerField, false);
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
            /// <returns>A value indicating whether the extraction was successful.</returns>
            private static bool ExtractTemplates(string? message, Dictionary<string, string> templateMap, List<string> templateList)
            {
                if (string.IsNullOrEmpty(message))
                {
                    return true;
                }

                int scanIndex = 0;
                int endIndex = message.Length;

                bool success = true;
                while (scanIndex < endIndex)
                {
                    int openBraceIndex = FindBraceIndex(message, '{', scanIndex, endIndex);

                    if (openBraceIndex == -2) // found '}' instead of '{'
                    {
                        success = false;
                        break;
                    }
                    else if (openBraceIndex == -1) // scanned the string and didn't find any remaining '{' or '}'
                    {
                        break;
                    }

                    int closeBraceIndex = FindBraceIndex(message, '}', openBraceIndex + 1, endIndex);

                    if (closeBraceIndex <= -1) // unclosed '{'
                    {
                        success = false;
                        break;
                    }

                    // Format item syntax : { index[,alignment][ :formatString] }.
                    int formatDelimiterIndex = FindIndexOfAny(message, _formatDelimiters, openBraceIndex, closeBraceIndex);
                    string templateName = message.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1);

                    if (string.IsNullOrWhiteSpace(templateName)) // braces with no named argument, such as {} and { }
                    {
                        success = false;
                        break;
                    }

                    templateMap[templateName] = templateName;
                    templateList.Add(templateName);

                    scanIndex = closeBraceIndex + 1;
                }

                return success;
            }

            /// <summary>
            /// Searches for the next brace index in the message.
            /// </summary>
            /// <remarks> The search skips any sequences of {{ or }}.</remarks>
            /// <example>{{prefix{{{Argument}}}suffix}}</example>
            /// <returns>The zero-based index position of the first occurrence of the searched brace; -1 if the searched brace was not found; -2 if the wrong brace was found.</returns>
            private static int FindBraceIndex(string message, char searchedBrace, int startIndex, int endIndex)
            {
                Debug.Assert(searchedBrace is '{' or '}');

                int braceIndex = -1;
                int scanIndex = startIndex;

                while (scanIndex < endIndex)
                {
                    char current = message[scanIndex];

                    if (current is '{' or '}')
                    {
                        char currentBrace = current;

                        int scanIndexBeforeSkip = scanIndex;
                        while (current == currentBrace && ++scanIndex < endIndex)
                        {
                            current = message[scanIndex];
                        }

                        int bracesCount = scanIndex - scanIndexBeforeSkip;
                        if (bracesCount % 2 != 0) // if it is an even number of braces, just skip them, otherwise, we found an unescaped brace
                        {
                            if (currentBrace == searchedBrace)
                            {
                                if (currentBrace == '{')
                                {
                                    braceIndex = scanIndex - 1; // For '{' pick the last occurrence.
                                }
                                else
                                {
                                    braceIndex = scanIndexBeforeSkip; // For '}' pick the first occurrence.
                                }
                            }
                            else
                            {
                                braceIndex = -2; // wrong brace found
                            }

                            break;
                        }
                    }
                    else
                    {
                        scanIndex++;
                    }
                }

                return braceIndex;
            }

            private static int FindIndexOfAny(string message, char[] chars, int startIndex, int endIndex)
            {
                int findIndex = message.IndexOfAny(chars, startIndex, endIndex - startIndex);
                return findIndex == -1 ? endIndex : findIndex;
            }

            private static object GetItem(TypedConstant arg) => arg.Kind == TypedConstantKind.Array ? arg.Values : arg.Value;
        }

        /// <summary>
        /// A logger class holding a bunch of logger methods.
        /// </summary>
        internal sealed class LoggerClass
        {
            public readonly List<LoggerMethod> Methods = new();
            public string Keyword = string.Empty;
            public string Namespace = string.Empty;
            public string Name = string.Empty;
            public LoggerClass? ParentClass;
        }

        /// <summary>
        /// A logger method in a logger class.
        /// </summary>
        internal sealed class LoggerMethod
        {
            public readonly List<LoggerParameter> AllParameters = new();
            public readonly List<LoggerParameter> TemplateParameters = new();
            public readonly Dictionary<string, string> TemplateMap = new(StringComparer.OrdinalIgnoreCase);
            public readonly List<string> TemplateList = new();
            public string Name = string.Empty;
            public string UniqueName = string.Empty;
            public string Message = string.Empty;
            public int? Level;
            public int EventId;
            public string? EventName;
            public bool IsExtensionMethod;
            public string Modifiers = string.Empty;
            public string LoggerField = string.Empty;
            public bool SkipEnabledCheck;
        }

        /// <summary>
        /// A single parameter to a logger method.
        /// </summary>
        internal sealed class LoggerParameter
        {
            public string Name = string.Empty;
            public string Type = string.Empty;
            public string CodeName = string.Empty;
            public string? Qualifier;
            public bool IsLogger;
            public bool IsException;
            public bool IsLogLevel;
            public bool IsEnumerable;
            // A parameter flagged as IsTemplateParameter is not going to be taken care of specially as an argument to ILogger.Log
            // but instead is supposed to be taken as a parameter for the template.
            public bool IsTemplateParameter => !IsLogger && !IsException && !IsLogLevel;
        }

        /// <summary>
        /// Returns a non-randomized hash code for the given string.
        /// We always return a positive value.
        /// </summary>
        internal static int GetNonRandomizedHashCode(string s)
        {
            uint result = 2166136261u;
            foreach (char c in s)
            {
                result = (c ^ result) * 16777619;
            }
            return Math.Abs((int)result);
        }
    }
}
