// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

// This isn't a shipping analyzer, so we don't need release tracking
#pragma warning disable RS2008

#nullable enable

namespace IntrinsicsInSystemPrivateCoreLib
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [CLSCompliant(false)]
    public class IntrinsicsInSystemPrivateCoreLibAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "IntrinsicsInSystemPrivateCoreLib";

        private const string Title = "System.Private.CoreLib ReadyToRun Intrinsics";
        private const string MessageFormat = "Intrinsics from class '{0}' used without the protection of an explicit if statement checking the correct IsSupported flag or CompExactlyDependsOn";
        private const string Description = "ReadyToRun Intrinsic Safety For System.Private.CoreLib.";
        private const string Category = "IntrinsicsCorrectness";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public const string DiagnosticIdHelper = "IntrinsicsInSystemPrivateCoreLibHelper";
        private const string MessageHelperFormat = "Helper '{0}' used without the protection of an explicit if statement checking the correct IsSupported flag or CompExactlyDependsOn";
        private static readonly DiagnosticDescriptor RuleHelper = new DiagnosticDescriptor(DiagnosticIdHelper, Title, MessageHelperFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public const string DiagnosticIdConditionParsing = "IntrinsicsInSystemPrivateCoreLibConditionParsing";
        private const string MessageNonParseableConditionFormat = "Unable to parse condition to determine if intrinsics are correctly used";
        private static readonly DiagnosticDescriptor RuleCantParse = new DiagnosticDescriptor(DiagnosticIdConditionParsing, Title, MessageNonParseableConditionFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public const string DiagnosticIdAttributeNotSpecificEnough = "IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough";
        private const string MessageAttributeNotSpecificEnoughFormat = "CompExactlyDependsOn({0}) attribute found which relates to this IsSupported check, but is not specific enough. Suppress this error if this function has an appropriate if condition so that if the meaning of the function is invariant regardless of the result of the call to IsSupported.";
        private static readonly DiagnosticDescriptor RuleAttributeNotSpecificEnough = new DiagnosticDescriptor(DiagnosticIdAttributeNotSpecificEnough, Title, MessageAttributeNotSpecificEnoughFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule, RuleHelper, RuleCantParse, RuleAttributeNotSpecificEnough); } }

        private static INamespaceSymbol GetNamespace(IAssemblySymbol assembly, params string[] namespaceNames)
        {
            INamespaceSymbol outerNamespace = assembly.GlobalNamespace;

            string newFullNamespaceName = "";
            INamespaceSymbol? foundNamespace = null;

            foreach (var namespaceName in namespaceNames)
            {
                if (newFullNamespaceName == "")
                    newFullNamespaceName = namespaceName;
                else
                    newFullNamespaceName = newFullNamespaceName + "." + namespaceName;

                foundNamespace = null;

                foreach (var innerNamespace in outerNamespace.GetNamespaceMembers())
                {
                    if (innerNamespace.Name == namespaceName)
                    {
                        foundNamespace = innerNamespace;
                        break;
                    }
                }

                if (foundNamespace == null)
                {
                    throw new Exception($"Not able to find {newFullNamespaceName} namespace");
                }

                outerNamespace = foundNamespace;
            }

            return foundNamespace!;
        }

        private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
        {
            foreach (var typeSymbol in type.GetTypeMembers())
            {
                yield return typeSymbol;
                foreach (var nestedTypeSymbol in GetNestedTypes(typeSymbol))
                {
                    yield return nestedTypeSymbol;
                }
            }
        }

        private static IEnumerable<INamedTypeSymbol> GetSubtypes(INamespaceSymbol namespaceSymbol)
        {
            foreach (var typeSymbol in namespaceSymbol.GetTypeMembers())
            {
                yield return typeSymbol;
                foreach (var nestedTypeSymbol in GetNestedTypes(typeSymbol))
                {
                    yield return nestedTypeSymbol;
                }
            }

            foreach (var namespaceMember in namespaceSymbol.GetNamespaceMembers())
            {
                foreach (var typeSymbol in GetSubtypes(namespaceMember))
                {
                    yield return typeSymbol;
                }
            }
        }

        private sealed class IntrinsicsAnalyzerOnLoadData
        {
            public IntrinsicsAnalyzerOnLoadData(HashSet<INamedTypeSymbol> namedTypesToBeProtected,
                                                INamedTypeSymbol? bypassReadyToRunAttribute,
                                                INamedTypeSymbol? compExactlyDependsOn)
            {
                NamedTypesToBeProtected = namedTypesToBeProtected;
                BypassReadyToRunAttribute = bypassReadyToRunAttribute;
                CompExactlyDependsOn = compExactlyDependsOn;
            }
            public readonly HashSet<INamedTypeSymbol> NamedTypesToBeProtected;
            public readonly INamedTypeSymbol? BypassReadyToRunAttribute;
            public readonly INamedTypeSymbol? CompExactlyDependsOn;
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(context =>
            {
                HashSet<INamedTypeSymbol> namedTypesToBeProtected = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                INamespaceSymbol systemRuntimeIntrinsicsNamespace = GetNamespace(context.Compilation.Assembly, "System", "Runtime", "Intrinsics");
                INamedTypeSymbol? bypassReadyToRunAttribute = context.Compilation.Assembly.GetTypeByMetadataName("System.Runtime.BypassReadyToRunAttribute");
                INamedTypeSymbol? compExactlyDependsOn = context.Compilation.Assembly.GetTypeByMetadataName("System.Runtime.CompilerServices.CompExactlyDependsOnAttribute");

                IntrinsicsAnalyzerOnLoadData onLoadData = new IntrinsicsAnalyzerOnLoadData(namedTypesToBeProtected, bypassReadyToRunAttribute, compExactlyDependsOn);

                // Find all types in the System.Runtime.Intrinsics namespace that have an IsSupported property that are NOT
                // directly in the System.Runtime.Intrinsics namespace
                foreach (var architectureSpecificNamespace in systemRuntimeIntrinsicsNamespace.GetNamespaceMembers())
                {
                    foreach (var typeSymbol in GetSubtypes(architectureSpecificNamespace))
                    {
                        foreach (var member in typeSymbol.GetMembers())
                        {
                            if (member.Kind == SymbolKind.Property)
                            {
                                if (member.Name == "IsSupported")
                                {
                                    namedTypesToBeProtected.Add(typeSymbol);
                                }
                            }
                        }
                    }
                }

                context.RegisterSymbolStartAction(context =>
                {
                    var methodSymbol = (IMethodSymbol)context.Symbol;

                    foreach (var attributeData in methodSymbol.GetAttributes())
                    {
                        if (bypassReadyToRunAttribute != null)
                        {
                            if (attributeData.AttributeClass.Equals(bypassReadyToRunAttribute, SymbolEqualityComparer.Default))
                            {
                                // This method isn't involved in ReadyToRun, and so doesn't need analysis
                                return;
                            }
                        }
                    }

                    context.RegisterOperationAction(context =>
                    {
                        AnalyzeOperation(context.Operation, methodSymbol, context, onLoadData);
                    },
                    OperationKind.Invocation, OperationKind.PropertyReference);
                }, SymbolKind.Method);
            });
        }

        private static ISymbol? GetOperationSymbol(IOperation operation)
            => operation switch
            {
                IInvocationOperation iOperation => iOperation.TargetMethod,
                IMemberReferenceOperation mOperation => mOperation.Member,
                _ => null,
            };

        private static INamedTypeSymbol? GetIsSupportedTypeSymbol(SemanticModel model, MemberAccessExpressionSyntax memberAccessExpression)
        {
            if (memberAccessExpression.Name is IdentifierNameSyntax identifierName && identifierName.Identifier.Text == "IsSupported")
            {
                var symbolInfo = model.GetSymbolInfo(memberAccessExpression);
                return symbolInfo.Symbol.ContainingSymbol as INamedTypeSymbol;
            }
            else
            {
                return null;
            }
        }

        private static INamedTypeSymbol? GetIsSupportedTypeSymbol(SemanticModel model, IdentifierNameSyntax identifierName)
        {
            var symbolInfo = model.GetSymbolInfo(identifierName);

            if (identifierName.Identifier.Text == "IsSupported")
                return symbolInfo.Symbol.ContainingSymbol as INamedTypeSymbol;
            else
                return null;
        }

        private static INamedTypeSymbol[] GatherAndConditions(SemanticModel model, ExpressionSyntax expressionToDecompose)
        {
            if (expressionToDecompose is ParenthesizedExpressionSyntax parenthesizedExpression)
            {
                return GatherAndConditions(model, parenthesizedExpression.Expression);
            }

            if (expressionToDecompose is MemberAccessExpressionSyntax memberAccessExpression)
            {
                var isSupportedType = GetIsSupportedTypeSymbol(model, memberAccessExpression);
                if (isSupportedType == null)
                {
                    return Array.Empty<INamedTypeSymbol>();
                }
                else
                    return new INamedTypeSymbol[] { isSupportedType };
            }
            else if (expressionToDecompose is IdentifierNameSyntax identifier)
            {
                var isSupportedType = GetIsSupportedTypeSymbol(model, identifier);
                if (isSupportedType == null)
                {
                    return Array.Empty<INamedTypeSymbol>();
                }
                else
                    return new INamedTypeSymbol[] { isSupportedType };
            }
            else if (expressionToDecompose is BinaryExpressionSyntax binaryExpression)
            {
                if (binaryExpression.OperatorToken is SyntaxToken operatorToken && operatorToken.ValueText == "&&")
                {
                    var decomposedLeft = GatherAndConditions(model, binaryExpression.Left);
                    var decomposedRight = GatherAndConditions(model, binaryExpression.Right);
                    int arrayLen = decomposedLeft.Length + decomposedRight.Length;

                    if (arrayLen != 0)
                    {
                        var retVal = new INamedTypeSymbol[decomposedLeft.Length + decomposedRight.Length];
                        Array.Copy(decomposedLeft, retVal, decomposedLeft.Length);
                        Array.Copy(decomposedRight, 0, retVal, decomposedLeft.Length, decomposedRight.Length);
                        return retVal;
                    }
                    else
                    {
                        return Array.Empty<INamedTypeSymbol>();
                    }
                }
            }

            return Array.Empty<INamedTypeSymbol>();
        }

        private static INamedTypeSymbol[][] DecomposePropertySymbolForIsSupportedGroups_Property(OperationAnalysisContext context, SemanticModel model, ExpressionSyntax expressionToDecompose)
        {
            var symbolInfo = model.GetSymbolInfo(expressionToDecompose);
            if (symbolInfo.Symbol.Kind != SymbolKind.Property)
            {
                return Array.Empty<INamedTypeSymbol[]>();
            }

            if (symbolInfo.Symbol.Name == "IsSupported")
            {
                var typeSymbol = symbolInfo.Symbol.ContainingSymbol as INamedTypeSymbol;
                if (typeSymbol != null)
                {
                    return new INamedTypeSymbol[][] { new INamedTypeSymbol[] { typeSymbol } };
                }
            }

            var propertyDefiningSyntax = symbolInfo.Symbol.DeclaringSyntaxReferences[0].GetSyntax();
            if (propertyDefiningSyntax != null)
            {
                if (propertyDefiningSyntax is PropertyDeclarationSyntax propertyDeclaration
                    && propertyDeclaration.ExpressionBody is ArrowExpressionClauseSyntax arrowExpression)
                {
                    return DecomposeConditionForIsSupportedGroups(context, model, arrowExpression.Expression);
                }
            }

            return Array.Empty<INamedTypeSymbol[]>();
        }

        private static INamedTypeSymbol[][] DecomposeConditionForIsSupportedGroups(OperationAnalysisContext context, SemanticModel model, ExpressionSyntax expressionToDecompose)
        {
            if (expressionToDecompose is ParenthesizedExpressionSyntax parenthesizedExpression)
            {
                return DecomposeConditionForIsSupportedGroups(context, model, parenthesizedExpression.Expression);
            }
            if (expressionToDecompose is MemberAccessExpressionSyntax || expressionToDecompose is IdentifierNameSyntax)
            {
                return DecomposePropertySymbolForIsSupportedGroups_Property(context, model, expressionToDecompose);
            }
            else if (expressionToDecompose is BinaryExpressionSyntax binaryExpression)
            {
                var decomposedLeft = DecomposeConditionForIsSupportedGroups(context, model, binaryExpression.Left);
                var decomposedRight = DecomposeConditionForIsSupportedGroups(context, model, binaryExpression.Right);
                if (binaryExpression.OperatorToken is SyntaxToken operatorToken && operatorToken.ValueText == "&&")
                {
                    if (decomposedLeft.Length == 0)
                        return decomposedRight;
                    else if (decomposedRight.Length == 0)
                        return decomposedLeft;

                    if ((decomposedLeft.Length > 1) || (decomposedRight.Length > 1))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(RuleCantParse, expressionToDecompose.GetLocation()));
                    }

                    return new INamedTypeSymbol[][] { GatherAndConditions(model, binaryExpression) };
                }
                else if (binaryExpression.OperatorToken is SyntaxToken operatorToken2 && operatorToken2.ValueText == "||")
                {
                    if (decomposedLeft.Length == 0 || decomposedRight.Length == 0)
                    {
                        if (decomposedLeft.Length != 0 || decomposedRight.Length != 0)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(RuleCantParse, expressionToDecompose.GetLocation()));
                        }
                        return Array.Empty<INamedTypeSymbol[]>();
                    }
                    var retVal = new INamedTypeSymbol[decomposedLeft.Length + decomposedRight.Length][];
                    Array.Copy(decomposedLeft, retVal, decomposedLeft.Length);
                    Array.Copy(decomposedRight, 0, retVal, decomposedLeft.Length, decomposedRight.Length);
                    return retVal;
                }
                else
                {
                    if (decomposedLeft.Length != 0 || decomposedRight.Length != 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(RuleCantParse, expressionToDecompose.GetLocation()));
                    }
                }
            }
            else if (expressionToDecompose is PrefixUnaryExpressionSyntax prefixUnaryExpression)
            {
                var decomposedOperand = DecomposeConditionForIsSupportedGroups(context, model, prefixUnaryExpression.Operand);

                if (decomposedOperand.Length != 0)
                    context.ReportDiagnostic(Diagnostic.Create(RuleCantParse, expressionToDecompose.GetLocation()));
            }
            else if (expressionToDecompose is ConditionalExpressionSyntax conditionalExpressionSyntax)
            {
                var decomposedTrue = DecomposeConditionForIsSupportedGroups(context, model, conditionalExpressionSyntax.WhenTrue);
                var decomposedFalse = DecomposeConditionForIsSupportedGroups(context, model, conditionalExpressionSyntax.WhenFalse);
                if (decomposedTrue.Length != 0 || decomposedFalse.Length != 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(RuleCantParse, expressionToDecompose.GetLocation()));
                }
            }
            return Array.Empty<INamedTypeSymbol[]>();
        }

        private static IEnumerable<INamedTypeSymbol> GetCompExactlyDependsOnUseList(ISymbol symbol, IntrinsicsAnalyzerOnLoadData onLoadData)
        {
            var compExactlyDependsOn = onLoadData.CompExactlyDependsOn;
            if (compExactlyDependsOn != null)
            {
                foreach (var attributeData in symbol.GetAttributes())
                {
                    if (attributeData.AttributeClass.Equals(compExactlyDependsOn, SymbolEqualityComparer.Default))
                    {
                        if (attributeData.ConstructorArguments[0].Value is INamedTypeSymbol attributeTypeSymbol)
                        {
                            yield return attributeTypeSymbol;
                        }
                    }
                }
            }
        }

        private static bool ConditionAllowsSymbol(ISymbol symbolOfInvokeTarget, INamedTypeSymbol namedTypeThatIsSafeToUse, IntrinsicsAnalyzerOnLoadData onLoadData)
        {
            HashSet<INamedTypeSymbol> examinedSymbols = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            Stack<INamedTypeSymbol> symbolsToExamine = new Stack<INamedTypeSymbol>();
            symbolsToExamine.Push(namedTypeThatIsSafeToUse);

            while (symbolsToExamine.Count > 0)
            {
                INamedTypeSymbol symbol = symbolsToExamine.Pop();
                if (symbolOfInvokeTarget.ContainingSymbol.Equals(symbol, SymbolEqualityComparer.Default))
                    return true;

                foreach (var helperForType in GetCompExactlyDependsOnUseList(symbolOfInvokeTarget, onLoadData))
                {
                    if (helperForType.Equals(symbol, SymbolEqualityComparer.Default))
                        return true;
                }

                examinedSymbols.Add(symbol);
                if (symbol.ContainingType != null && !examinedSymbols.Contains(symbol.ContainingType))
                    symbolsToExamine.Push(symbol.ContainingType);
                if (symbol.BaseType != null && !examinedSymbols.Contains(symbol.BaseType))
                    symbolsToExamine.Push(symbol.BaseType);
            }

            return false;
        }

        private static bool TypeSymbolAllowsTypeSymbol(INamedTypeSymbol namedTypeToCheckForSupport, INamedTypeSymbol namedTypeThatProvidesSupport)
        {
            HashSet<INamedTypeSymbol> examinedSymbols = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            Stack<INamedTypeSymbol> symbolsToExamine = new Stack<INamedTypeSymbol>();
            symbolsToExamine.Push(namedTypeThatProvidesSupport);

            while (symbolsToExamine.Count > 0)
            {
                INamedTypeSymbol symbol = symbolsToExamine.Pop();
                if (namedTypeToCheckForSupport.Equals(symbol, SymbolEqualityComparer.Default))
                    return true;

                examinedSymbols.Add(symbol);
                if (symbol.ContainingType != null && !examinedSymbols.Contains(symbol.ContainingType))
                    symbolsToExamine.Push(symbol.ContainingType);
                if (symbol.BaseType != null && !examinedSymbols.Contains(symbol.BaseType))
                    symbolsToExamine.Push(symbol.BaseType);
            }

            return false;
        }

        private static INamespaceSymbol? SymbolToNamespaceSymbol(ISymbol symbol)
        {
            return symbol.ContainingNamespace;
        }
        private static void AnalyzeOperation(IOperation operation, IMethodSymbol methodSymbol, OperationAnalysisContext context, IntrinsicsAnalyzerOnLoadData onLoadData)
        {
            var symbol = GetOperationSymbol(operation);

            if (symbol == null || symbol is ITypeSymbol type && type.SpecialType != SpecialType.None)
            {
                return;
            }

            bool methodNeedsProtectionWithIsSupported = false;
#pragma warning disable RS1024 // The hashset is constructed with the correct comparer
            if (onLoadData.NamedTypesToBeProtected.Contains(symbol.ContainingSymbol))
            {
                methodNeedsProtectionWithIsSupported = true;
            }
#pragma warning restore RS1024

            // A method on an intrinsic type can call other methods on the intrinsic type safely, as well as methods on the type that contains the method
            if (methodNeedsProtectionWithIsSupported &&
                (methodSymbol.ContainingType.Equals(symbol.ContainingSymbol, SymbolEqualityComparer.Default)
                || (methodSymbol.ContainingType.ContainingType != null && methodSymbol.ContainingType.ContainingType.Equals(symbol.ContainingType, SymbolEqualityComparer.Default))))
            {
                return; // Intrinsic functions on their containing type can call themselves
            }

            if (!methodNeedsProtectionWithIsSupported)
            {
                if (GetCompExactlyDependsOnUseList(symbol, onLoadData).Any())
                    methodNeedsProtectionWithIsSupported = true;
            }

            if (!methodNeedsProtectionWithIsSupported)
            {
                return;
            }

            var compExactlyDependsOn = onLoadData.CompExactlyDependsOn;

            ISymbol? symbolThatMightHaveCompExactlyDependsOnAttribute = methodSymbol;
            IOperation operationSearch = operation;
            while (operationSearch != null)
            {
                if (operationSearch.Kind == OperationKind.AnonymousFunction)
                {
                    symbolThatMightHaveCompExactlyDependsOnAttribute = null;
                    break;
                }
                if (operationSearch.Kind == OperationKind.LocalFunction)
                {
                    // Assign symbolThatMightHaveCompExactlyDependsOnAttribute to the symbol for the LocalFunction
                    ILocalFunctionOperation localFunctionOperation = (ILocalFunctionOperation)operationSearch;
                    symbolThatMightHaveCompExactlyDependsOnAttribute = localFunctionOperation.Symbol;
                    break;
                }

                operationSearch = operationSearch.Parent;
            }

            if (symbol is IPropertySymbol propertySymbol)
            {
                if (propertySymbol.Name == "IsSupported")
                {
                    ISymbol? attributeExplicitlyAllowsRelatedSymbol = null;
                    ISymbol? attributeExplicitlyAllowsExactSymbol = null;
                    if ((compExactlyDependsOn != null) && symbolThatMightHaveCompExactlyDependsOnAttribute != null)
                    {
                        foreach (var attributeData in symbolThatMightHaveCompExactlyDependsOnAttribute.GetAttributes())
                        {
                            if (attributeData.AttributeClass.Equals(compExactlyDependsOn, SymbolEqualityComparer.Default))
                            {
                                if (attributeData.ConstructorArguments[0].Value is INamedTypeSymbol attributeTypeSymbol)
                                {
                                    var namespaceAttributeTypeSymbol = SymbolToNamespaceSymbol(attributeTypeSymbol);
                                    var namespaceSymbol = SymbolToNamespaceSymbol(symbol);
                                    if ((namespaceAttributeTypeSymbol != null) && (namespaceSymbol != null))
                                    {
                                        if (namespaceAttributeTypeSymbol.Equals(namespaceSymbol, SymbolEqualityComparer.Default))
                                        {
                                            if (ConditionAllowsSymbol(symbol, attributeTypeSymbol, onLoadData))
                                            {
                                                attributeExplicitlyAllowsExactSymbol = attributeTypeSymbol;
                                            }
                                            else
                                            {
                                                attributeExplicitlyAllowsRelatedSymbol = attributeTypeSymbol;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if ((attributeExplicitlyAllowsRelatedSymbol != null) && (attributeExplicitlyAllowsExactSymbol == null))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(RuleAttributeNotSpecificEnough, operation.Syntax.GetLocation(), attributeExplicitlyAllowsRelatedSymbol.ToDisplayString()));
                    }

                    return;
                }
            }

            if (symbolThatMightHaveCompExactlyDependsOnAttribute != null)
            {
                foreach (var attributeTypeSymbol in GetCompExactlyDependsOnUseList(symbolThatMightHaveCompExactlyDependsOnAttribute, onLoadData))
                {
                    if (ConditionAllowsSymbol(symbol, attributeTypeSymbol, onLoadData))
                    {
                        // This attribute indicates that this method will only be compiled into a ReadyToRun image if the behavior
                        // of the associated IsSupported method is defined to a constant value during ReadyToRun compilation that cannot change at runtime
                        return;
                    }
                }
            }

            var ancestorNodes = operation.Syntax.AncestorsAndSelf(true);
            SyntaxNode? previousNode = null;
            HashSet<INamedTypeSymbol> notTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            foreach (var ancestorNode in ancestorNodes)
            {
                if (previousNode != null)
                {
                    if (ancestorNode is LocalFunctionStatementSyntax)
                    {
                        // Local functions are not the same ECMA 335 function as the outer function, so don't continue searching for an if statement.
                        break;
                    }
                    if (ancestorNode is LambdaExpressionSyntax)
                    {
                        // Lambda functions are not the same ECMA 335 function as the outer function, so don't continue searching for an if statement.
                        break;
                    }
                    if (ancestorNode is IfStatementSyntax ifStatement)
                    {
                        if (HandleConditionalCase(ifStatement.Condition, ifStatement.Statement, ifStatement.Else))
                            return;
                    }
                    if (ancestorNode is ConditionalExpressionSyntax conditionalExpression)
                    {
                        if (HandleConditionalCase(conditionalExpression.Condition, conditionalExpression.WhenTrue, conditionalExpression.WhenFalse))
                            return;
                    }

                    // Returns true to indicate the wrapping method should return
                    bool HandleConditionalCase(ExpressionSyntax condition, SyntaxNode? syntaxOnPositiveCondition, SyntaxNode? syntaxOnNegativeCondition)
                    {
                        if (previousNode == syntaxOnPositiveCondition)
                        {
                            var decomposedCondition = DecomposeConditionForIsSupportedGroups(context, operation.SemanticModel, condition);

                            if (decomposedCondition.Length == 0)
                                return false;

                            // Ensure every symbol found in the condition is only in 1 OR clause
                            HashSet<ISymbol> foundSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
                            foreach (var andClause in decomposedCondition)
                            {
                                foreach (var symbolInOrClause in andClause)
                                {
                                    if (!foundSymbols.Add(symbolInOrClause))
                                    {
                                        context.ReportDiagnostic(Diagnostic.Create(RuleCantParse, operation.Syntax.GetLocation()));
                                        return true;
                                    }
                                }
                            }

                            // Determine which sets of conditions have been excluded
                            List<int> includedClauses = new List<int>();
                            for (int andClauseIndex = 0; andClauseIndex < decomposedCondition.Length; andClauseIndex++)
                            {
                                bool foundMatchInAndClause = false;
                                foreach (var symbolInAndClause in decomposedCondition[andClauseIndex])
                                {
                                    foreach (var notType in notTypes)
                                    {
                                        if (TypeSymbolAllowsTypeSymbol(notType, symbolInAndClause))
                                        {
                                            foundMatchInAndClause = true;
                                            break;
                                        }
                                    }
                                    if (foundMatchInAndClause)
                                        break;
                                }

                                if (!foundMatchInAndClause)
                                {
                                    includedClauses.Add(andClauseIndex);
                                }
                            }

                            // Each one of these clauses must be supported by the function being called
                            // or there is a lack of safety

                            foreach (var clauseIndex in includedClauses)
                            {
                                bool clauseAllowsSymbol = false;

                                var andClause = decomposedCondition[clauseIndex];
                                foreach (var symbolFromCondition in andClause)
                                {
                                    if (ConditionAllowsSymbol(symbol, symbolFromCondition, onLoadData))
                                    {
                                        // There is a good IsSupported check with a positive check for the IsSupported call involved. Do not report.
                                        clauseAllowsSymbol = true;
                                    }
                                }

                                if (!clauseAllowsSymbol)
                                    return false;
                            }

                            return true;
                        }
                        else if (previousNode == syntaxOnNegativeCondition)
                        {
                            var decomposedCondition = DecomposeConditionForIsSupportedGroups(context, operation.SemanticModel, condition);
                            if (decomposedCondition.Length == 1)
                            {
                                foreach (var symbolFromCondition in decomposedCondition[0])
                                {
                                    notTypes.Add(symbolFromCondition);
                                }
                            }
                        }

                        return false;
                    }
                }
                previousNode = ancestorNode;
            }

            if (onLoadData.NamedTypesToBeProtected.Contains(symbol.ContainingType))
                context.ReportDiagnostic(Diagnostic.Create(Rule, operation.Syntax.GetLocation(), symbol.ContainingSymbol.ToDisplayString()));
            else
                context.ReportDiagnostic(Diagnostic.Create(RuleHelper, operation.Syntax.GetLocation(), symbol.ToDisplayString()));
        }
    }
}
