// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics.Hashing;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using SourceGenerators;

namespace Microsoft.Extensions.Logging.Generators
{
    public partial class LoggerMessageGenerator
    {
        internal sealed class Parser
        {
            internal const string LoggerMessageAttribute = "Microsoft.Extensions.Logging.LoggerMessageAttribute";

            private readonly CancellationToken _cancellationToken;
            private readonly INamedTypeSymbol _loggerMessageAttribute;
            private readonly INamedTypeSymbol _loggerSymbol;
            private readonly INamedTypeSymbol _logLevelSymbol;
            private readonly INamedTypeSymbol _exceptionSymbol;
            private readonly INamedTypeSymbol _enumerableSymbol;
            private readonly INamedTypeSymbol _stringSymbol;
            private readonly Action<Diagnostic>? _reportDiagnostic;

            public List<DiagnosticInfo> Diagnostics { get; } = new();

            public Parser(
                INamedTypeSymbol loggerMessageAttribute,
                INamedTypeSymbol loggerSymbol,
                INamedTypeSymbol logLevelSymbol,
                INamedTypeSymbol exceptionSymbol,
                INamedTypeSymbol enumerableSymbol,
                INamedTypeSymbol stringSymbol,
                Action<Diagnostic>? reportDiagnostic,
                CancellationToken cancellationToken)
            {
                _loggerMessageAttribute = loggerMessageAttribute;
                _loggerSymbol = loggerSymbol;
                _logLevelSymbol = logLevelSymbol;
                _exceptionSymbol = exceptionSymbol;
                _enumerableSymbol = enumerableSymbol;
                _stringSymbol = stringSymbol;
                _cancellationToken = cancellationToken;
                _reportDiagnostic = reportDiagnostic;
            }

            /// <summary>
            /// Gets the set of logging classes containing methods to output.
            /// </summary>
            public IReadOnlyList<LoggerClass> GetLogClasses(IEnumerable<ClassDeclarationSyntax> classes, SemanticModel semanticModel)
            {
                var results = new List<LoggerClass>();
                var eventIds = new HashSet<int>();
                var eventNames = new HashSet<string>();

                // we enumerate by syntax tree, to minimize the need to instantiate semantic models (since they're expensive)
                foreach (IGrouping<SyntaxTree, ClassDeclarationSyntax> group in classes.GroupBy(x => x.SyntaxTree))
                {
                    SyntaxTree syntaxTree = group.Key;
                    SemanticModel sm = semanticModel.Compilation.GetSemanticModel(syntaxTree);

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
                                    if (attrCtorSymbol == null || !_loggerMessageAttribute.Equals(attrCtorSymbol.ContainingType, SymbolEqualityComparer.Default))
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
                                        if (!SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, _loggerMessageAttribute))
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
                                                    Debug.Fail("Unexpected number of arguments in attribute constructor.");
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
#if ROSLYN4_8_OR_GREATER
                                        else if (paramSymbol.RefKind == RefKind.RefReadOnlyParameter)
#else
                                        else if (paramSymbol.RefKind == (RefKind)4) // RefKind.RefReadOnlyParameter, added in Roslyn 4.8
#endif
                                        {
                                            qualifier = "ref readonly";
                                        }

                                        string typeName = paramTypeSymbol.ToDisplayString(
                                            SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                                                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));

                                        if (paramSymbol.IsParams)
                                        {
                                            Diag(DiagnosticDescriptors.InvalidLoggingMethodParameterParams, paramSymbol.Locations[0], paramName);
                                            keepMethod = false;
                                            break;
                                        }

                                        var lp = new LoggerParameter
                                        {
                                            Name = paramName,
                                            Type = typeName,
                                            Qualifier = qualifier,
                                            CodeName = needsAtSign ? "@" + paramName : paramName,
                                            IsLogger = !foundLogger && IsBaseOrIdentity(paramTypeSymbol, _loggerSymbol, sm.Compilation),
                                            IsException = !foundException && IsBaseOrIdentity(paramTypeSymbol, _exceptionSymbol, sm.Compilation),
                                            IsLogLevel = !foundLogLevel && IsBaseOrIdentity(paramTypeSymbol, _logLevelSymbol, sm.Compilation),
                                            IsEnumerable = IsBaseOrIdentity(paramTypeSymbol, _enumerableSymbol, sm.Compilation) && !IsBaseOrIdentity(paramTypeSymbol, _stringSymbol, sm.Compilation),
                                        };
#if ROSLYN4_4_OR_GREATER
                                        lp.IsScoped = paramSymbol.ScopedKind != ScopedKind.None;
#endif

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
                                                (loggerField, multipleLoggerFields) = FindLoggerField(sm, classDec, _loggerSymbol);
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
                                            Name = GenerateClassName(classDec),
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
                                                Name = GenerateClassName(parentLoggerClass),
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

                if (results.Count > 0 && semanticModel.Compilation is CSharpCompilation { LanguageVersion : LanguageVersion version and < LanguageVersion.CSharp8 })
                {
                    // we only support C# 8.0 and above
                    Diag(DiagnosticDescriptors.LoggingUnsupportedLanguageVersion, null, version.ToDisplayString(), LanguageVersion.CSharp8.ToDisplayString());
                    return Array.Empty<LoggerClass>();
                }

                results.Sort((lhs, rhs) =>
                {
                    int c = StringComparer.Ordinal.Compare(lhs.Namespace, rhs.Namespace);
                    return c != 0 ? c : StringComparer.Ordinal.Compare(lhs.Name, rhs.Name);
                });
                return results;
            }

            private static string GenerateClassName(TypeDeclarationSyntax typeDeclaration)
            {
                if (typeDeclaration.TypeParameterList != null &&
                    typeDeclaration.TypeParameterList.Parameters.Count != 0)
                {
                    // The source generator produces a partial class that the compiler merges with the original
                    // class definition in the user code. If the user applies attributes to the generic types
                    // of the class, it is necessary to remove these attribute annotations from the generated
                    // code. Failure to do so may result in a compilation error (CS0579: Duplicate attribute).
                    for (int i = 0; i < typeDeclaration.TypeParameterList.Parameters.Count; i++)
                    {
                        TypeParameterSyntax parameter = typeDeclaration.TypeParameterList.Parameters[i];

                        if (parameter.AttributeLists.Count > 0)
                        {
                            typeDeclaration = typeDeclaration.ReplaceNode(parameter, parameter.WithAttributeLists([]));
                        }
                    }
                }

                return typeDeclaration.Identifier.ToString() + typeDeclaration.TypeParameterList;
            }

            private (string? loggerField, bool multipleLoggerFields) FindLoggerField(SemanticModel sm, TypeDeclarationSyntax classDec, ITypeSymbol loggerSymbol)
            {
                string? loggerField = null;

                INamedTypeSymbol? classType = sm.GetDeclaredSymbol(classDec, _cancellationToken);

                INamedTypeSymbol? currentClassType = classType;
                bool onMostDerivedType = true;

                // We keep track of the names of all non-logger fields, since they prevent referring to logger
                // primary constructor parameters with the same name. Example:
                // partial class C(ILogger logger)
                // {
                //     private readonly object logger = logger;
                //
                //     [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = ""M1"")]
                //     public partial void M1(); // The ILogger primary constructor parameter cannot be used here.
                // }
                HashSet<string> shadowedNames = new(StringComparer.Ordinal);

                while (currentClassType is { SpecialType: not SpecialType.System_Object })
                {
                    foreach (IFieldSymbol fs in currentClassType.GetMembers().OfType<IFieldSymbol>())
                    {
                        if (!onMostDerivedType && fs.DeclaredAccessibility == Accessibility.Private)
                        {
                            continue;
                        }
                        if (!fs.CanBeReferencedByName)
                        {
                            continue;
                        }
                        if (IsBaseOrIdentity(fs.Type, loggerSymbol, sm.Compilation))
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
                        else
                        {
                            shadowedNames.Add(fs.Name);
                        }
                    }

                    onMostDerivedType = false;
                    currentClassType = currentClassType.BaseType;
                }

                // We prioritize fields over primary constructor parameters and avoid warnings if both exist.
                if (loggerField is not null)
                {
                    return (loggerField, false);
                }

                IEnumerable<IMethodSymbol> primaryConstructors = classType.InstanceConstructors
                    .Where(ic => ic.DeclaringSyntaxReferences
                        .Any(ds => ds.GetSyntax() is ClassDeclarationSyntax));

                foreach (IMethodSymbol primaryConstructor in primaryConstructors)
                {
                    foreach (IParameterSymbol parameter in primaryConstructor.Parameters)
                    {
                        if (IsBaseOrIdentity(parameter.Type, loggerSymbol, sm.Compilation))
                        {
                            if (shadowedNames.Contains(parameter.Name))
                            {
                                // Accessible fields always shadow primary constructor parameters,
                                // so we can't use the primary constructor parameter,
                                // even if the field is not a valid logger.
                                Diag(DiagnosticDescriptors.PrimaryConstructorParameterLoggerHidden, parameter.Locations[0], classDec.Identifier.Text);

                                continue;
                            }

                            if (loggerField == null)
                            {
                                loggerField = parameter.Name;
                            }
                            else
                            {
                                return (null, true);
                            }
                        }
                    }
                }

                return (loggerField, false);
            }

            private void Diag(DiagnosticDescriptor desc, Location? location, params object?[]? messageArgs)
            {
                // Report immediately if callback is provided (preserves pragma suppression with original locations)
                _reportDiagnostic?.Invoke(Diagnostic.Create(desc, location, messageArgs));

                // Also collect for scenarios that need the diagnostics list; in Roslyn 4.0+ incremental generators,
                // this list is exposed via parser.Diagnostics (as ImmutableEquatableArray<DiagnosticInfo>) and reported in Execute.
                Diagnostics.Add(DiagnosticInfo.Create(desc, location, messageArgs));
            }

            private static bool IsBaseOrIdentity(ITypeSymbol source, ITypeSymbol dest, Compilation compilation)
            {
                Conversion conversion = compilation.ClassifyConversion(source, dest);
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

            public LoggerClassSpec ToSpec() => new LoggerClassSpec
            {
                Methods = Methods.Select(m => m.ToSpec()).ToImmutableEquatableArray(),
                Keyword = Keyword,
                Namespace = Namespace,
                Name = Name,
                ParentClass = ParentClass?.ToSpec()
            };
        }

        /// <summary>
        /// Immutable specification of a logger class for incremental caching.
        /// </summary>
        internal sealed record LoggerClassSpec : IEquatable<LoggerClassSpec>
        {
            public required ImmutableEquatableArray<LoggerMethodSpec> Methods { get; init; }
            public required string Keyword { get; init; }
            public required string Namespace { get; init; }
            public required string Name { get; init; }
            public required LoggerClassSpec? ParentClass { get; init; }

            public bool Equals(LoggerClassSpec? other)
            {
                if (other is null) return false;
                if (ReferenceEquals(this, other)) return true;
                return Methods.Equals(other.Methods) &&
                       Keyword == other.Keyword &&
                       Namespace == other.Namespace &&
                       Name == other.Name &&
                       Equals(ParentClass, other.ParentClass);
            }

            public override int GetHashCode()
            {
                int hash = Methods.GetHashCode();
                hash = HashHelpers.Combine(hash, Keyword.GetHashCode());
                hash = HashHelpers.Combine(hash, Namespace.GetHashCode());
                hash = HashHelpers.Combine(hash, Name.GetHashCode());
                hash = HashHelpers.Combine(hash, ParentClass?.GetHashCode() ?? 0);
                return hash;
            }
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

            public LoggerMethodSpec ToSpec() => new LoggerMethodSpec
            {
                AllParameters = AllParameters.Select(p => p.ToSpec()).ToImmutableEquatableArray(),
                TemplateParameters = TemplateParameters.Select(p => p.ToSpec()).ToImmutableEquatableArray(),
                TemplateMap = TemplateMap.Select(kvp => new KeyValuePairEquatable<string, string>(kvp.Key, kvp.Value)).ToImmutableEquatableArray(),
                TemplateList = TemplateList.ToImmutableEquatableArray(),
                Name = Name,
                UniqueName = UniqueName,
                Message = Message,
                Level = Level,
                EventId = EventId,
                EventName = EventName,
                IsExtensionMethod = IsExtensionMethod,
                Modifiers = Modifiers,
                LoggerField = LoggerField,
                SkipEnabledCheck = SkipEnabledCheck
            };
        }

        /// <summary>
        /// Immutable specification of a logger method for incremental caching.
        /// </summary>
        internal sealed record LoggerMethodSpec : IEquatable<LoggerMethodSpec>
        {
            public required ImmutableEquatableArray<LoggerParameterSpec> AllParameters { get; init; }
            public required ImmutableEquatableArray<LoggerParameterSpec> TemplateParameters { get; init; }
            public required ImmutableEquatableArray<KeyValuePairEquatable<string, string>> TemplateMap { get; init; }
            public required ImmutableEquatableArray<string> TemplateList { get; init; }
            public required string Name { get; init; }
            public required string UniqueName { get; init; }
            public required string Message { get; init; }
            public required int? Level { get; init; }
            public required int EventId { get; init; }
            public required string? EventName { get; init; }
            public required bool IsExtensionMethod { get; init; }
            public required string Modifiers { get; init; }
            public required string LoggerField { get; init; }
            public required bool SkipEnabledCheck { get; init; }

            public bool Equals(LoggerMethodSpec? other)
            {
                if (other is null) return false;
                if (ReferenceEquals(this, other)) return true;
                return AllParameters.Equals(other.AllParameters) &&
                       TemplateParameters.Equals(other.TemplateParameters) &&
                       TemplateMap.Equals(other.TemplateMap) &&
                       TemplateList.Equals(other.TemplateList) &&
                       Name == other.Name &&
                       UniqueName == other.UniqueName &&
                       Message == other.Message &&
                       Level == other.Level &&
                       EventId == other.EventId &&
                       EventName == other.EventName &&
                       IsExtensionMethod == other.IsExtensionMethod &&
                       Modifiers == other.Modifiers &&
                       LoggerField == other.LoggerField &&
                       SkipEnabledCheck == other.SkipEnabledCheck;
            }

            public override int GetHashCode()
            {
                int hash = AllParameters.GetHashCode();
                hash = HashHelpers.Combine(hash, TemplateParameters.GetHashCode());
                hash = HashHelpers.Combine(hash, TemplateMap.GetHashCode());
                hash = HashHelpers.Combine(hash, TemplateList.GetHashCode());
                hash = HashHelpers.Combine(hash, Name.GetHashCode());
                hash = HashHelpers.Combine(hash, UniqueName.GetHashCode());
                hash = HashHelpers.Combine(hash, Message.GetHashCode());
                hash = HashHelpers.Combine(hash, Level?.GetHashCode() ?? 0);
                hash = HashHelpers.Combine(hash, EventId.GetHashCode());
                hash = HashHelpers.Combine(hash, EventName?.GetHashCode() ?? 0);
                hash = HashHelpers.Combine(hash, IsExtensionMethod.GetHashCode());
                hash = HashHelpers.Combine(hash, Modifiers.GetHashCode());
                hash = HashHelpers.Combine(hash, LoggerField.GetHashCode());
                hash = HashHelpers.Combine(hash, SkipEnabledCheck.GetHashCode());
                return hash;
            }
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
#pragma warning disable CS0649 // Field is never assigned to in builds without ROSLYN4_4_OR_GREATER
            public bool IsScoped;
#pragma warning restore CS0649
            // A parameter flagged as IsTemplateParameter is not going to be taken care of specially as an argument to ILogger.Log
            // but instead is supposed to be taken as a parameter for the template.
            public bool IsTemplateParameter => !IsLogger && !IsException && !IsLogLevel;

            public LoggerParameterSpec ToSpec() => new LoggerParameterSpec
            {
                Name = Name,
                Type = Type,
                CodeName = CodeName,
                Qualifier = Qualifier,
                IsLogger = IsLogger,
                IsException = IsException,
                IsLogLevel = IsLogLevel,
                IsEnumerable = IsEnumerable,
                IsScoped = IsScoped
            };
        }

        /// <summary>
        /// Immutable specification of a logger parameter for incremental caching.
        /// </summary>
        internal sealed record LoggerParameterSpec : IEquatable<LoggerParameterSpec>
        {
            public required string Name { get; init; }
            public required string Type { get; init; }
            public required string CodeName { get; init; }
            public required string? Qualifier { get; init; }
            public required bool IsLogger { get; init; }
            public required bool IsException { get; init; }
            public required bool IsLogLevel { get; init; }
            public required bool IsEnumerable { get; init; }
            public required bool IsScoped { get; init; }

            // A parameter flagged as IsTemplateParameter is not going to be taken care of specially as an argument to ILogger.Log
            // but instead is supposed to be taken as a parameter for the template.
            public bool IsTemplateParameter => !IsLogger && !IsException && !IsLogLevel;

            public bool Equals(LoggerParameterSpec? other)
            {
                if (other is null) return false;
                if (ReferenceEquals(this, other)) return true;
                return Name == other.Name &&
                       Type == other.Type &&
                       CodeName == other.CodeName &&
                       Qualifier == other.Qualifier &&
                       IsLogger == other.IsLogger &&
                       IsException == other.IsException &&
                       IsLogLevel == other.IsLogLevel &&
                       IsEnumerable == other.IsEnumerable &&
                       IsScoped == other.IsScoped;
            }

            public override int GetHashCode()
            {
                int hash = Name.GetHashCode();
                hash = HashHelpers.Combine(hash, Type.GetHashCode());
                hash = HashHelpers.Combine(hash, CodeName.GetHashCode());
                hash = HashHelpers.Combine(hash, Qualifier?.GetHashCode() ?? 0);
                hash = HashHelpers.Combine(hash, IsLogger.GetHashCode());
                hash = HashHelpers.Combine(hash, IsException.GetHashCode());
                hash = HashHelpers.Combine(hash, IsLogLevel.GetHashCode());
                hash = HashHelpers.Combine(hash, IsEnumerable.GetHashCode());
                hash = HashHelpers.Combine(hash, IsScoped.GetHashCode());
                return hash;
            }
        }

        /// <summary>
        /// Equatable KeyValuePair wrapper for use in ImmutableEquatableArray.
        /// </summary>
        internal readonly record struct KeyValuePairEquatable<TKey, TValue>(TKey Key, TValue Value) : IEquatable<KeyValuePairEquatable<TKey, TValue>>
            where TKey : IEquatable<TKey>
            where TValue : IEquatable<TValue>
        {
            public bool Equals(KeyValuePairEquatable<TKey, TValue> other)
            {
                return Key.Equals(other.Key) && Value.Equals(other.Value);
            }

            public override int GetHashCode()
            {
                return HashHelpers.Combine(Key.GetHashCode(), Value.GetHashCode());
            }
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

            int ret = (int)result;
            return ret == int.MinValue ? 0 : Math.Abs(ret); // Ensure the result is non-negative
        }
    }
}
