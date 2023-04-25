// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private const string Title = "System.Private.CoreLib ReadyToRun Intrinsics";
        private const string MessageFormat = "Intrinsics from class '{0}' used without the protection of an explicit if statement checking the correct IsSupported flag";
        private const string Description = "ReadyToRun Intrinsic Safety For System.Private.CoreLib.";
        private const string Category = "IntrinsicsCorrectness";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public const string DiagnosticIdConditionParsing = "IntrinsicsInSystemPrivateCoreLibConditionParsing";
        private const string MessageNonParseableConditionFormat = "Unable to parse condition to determine if intrinsics are correctly used";
        private static readonly DiagnosticDescriptor RuleCantParse = new DiagnosticDescriptor(DiagnosticIdConditionParsing, Title, MessageNonParseableConditionFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule, RuleCantParse); } }

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
            public IntrinsicsAnalyzerOnLoadData(List<INamedTypeSymbol> namedTypesToBeProtected,
                                                INamedTypeSymbol? bypassReadyToRunAttribute,
                                                INamedTypeSymbol? bypassReadyToRunForIntrinsicsHelperUse)
            {
                NamedTypesToBeProtected = namedTypesToBeProtected;
                BypassReadyToRunAttribute = bypassReadyToRunAttribute;
                BypassReadyToRunForIntrinsicsHelperUse = bypassReadyToRunForIntrinsicsHelperUse;
            }
            public readonly List<INamedTypeSymbol> NamedTypesToBeProtected;
            public readonly INamedTypeSymbol? BypassReadyToRunAttribute;
            public readonly INamedTypeSymbol? BypassReadyToRunForIntrinsicsHelperUse;
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(context =>
            {
                List<INamedTypeSymbol> namedTypesToBeProtected = new List<INamedTypeSymbol>();
                INamespaceSymbol systemRuntimeIntrinsicsNamespace = GetNamespace(context.Compilation.Assembly, "System", "Runtime", "Intrinsics");
                INamedTypeSymbol? bypassReadyToRunAttribute = context.Compilation.Assembly.GetTypeByMetadataName("System.Runtime.BypassReadyToRunAttribute");
                INamedTypeSymbol? bypassReadyToRunForIntrinsicsHelperUse = context.Compilation.Assembly.GetTypeByMetadataName("System.Runtime.BypassReadyToRunForIntrinsicsHelperUseAttribute");

                IntrinsicsAnalyzerOnLoadData onLoadData = new IntrinsicsAnalyzerOnLoadData(namedTypesToBeProtected, bypassReadyToRunAttribute, bypassReadyToRunForIntrinsicsHelperUse);

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
                    OperationKind.Invocation);
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

        private static INamedTypeSymbol[][] DecomposeConditionForIsSupportedGroups(SemanticModel model, ExpressionSyntax expressionToDecompose)
        {
            if (expressionToDecompose is ParenthesizedExpressionSyntax parenthesizedExpression)
            {
                return DecomposeConditionForIsSupportedGroups(model, parenthesizedExpression.Expression);
            }
            if (expressionToDecompose is MemberAccessExpressionSyntax memberAccessExpression)
            {
                var isSupportedType = GetIsSupportedTypeSymbol(model, memberAccessExpression);
                if (isSupportedType == null)
                {
                    return Array.Empty<INamedTypeSymbol[]>();
                }
                else
                {
                    return new INamedTypeSymbol[][] { new INamedTypeSymbol[] { isSupportedType } };
                }
            }
            else if (expressionToDecompose is BinaryExpressionSyntax binaryExpression)
            {
                var decomposedLeft = DecomposeConditionForIsSupportedGroups(model, binaryExpression.Left);
                var decomposedRight = DecomposeConditionForIsSupportedGroups(model, binaryExpression.Right);
                if (binaryExpression.OperatorToken is SyntaxToken operatorToken && operatorToken.ValueText == "&&")
                {
                    return new INamedTypeSymbol[][] { GatherAndConditions(model, binaryExpression) };
                }
                else if (binaryExpression.OperatorToken is SyntaxToken operatorToken2 && operatorToken2.ValueText == "||")
                {
                    if (decomposedLeft.Length == 0 || decomposedRight.Length == 0)
                    {
                        return Array.Empty<INamedTypeSymbol[]>();
                    }
                    var retVal = new INamedTypeSymbol[decomposedLeft.Length + decomposedRight.Length][];
                    Array.Copy(decomposedLeft, retVal, decomposedLeft.Length);
                    Array.Copy(decomposedRight, 0, retVal, decomposedLeft.Length, decomposedRight.Length);
                    return retVal;
                }
            }
            return Array.Empty<INamedTypeSymbol[]>();
        }

        private static bool ConditionAllowsSymbol(ISymbol symbolOfInvokeTarget, INamedTypeSymbol namedTypeThatIsSafeToUse)
        {
            HashSet<INamedTypeSymbol> examinedSymbols = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            Stack<INamedTypeSymbol> symbolsToExamine = new Stack<INamedTypeSymbol>();
            symbolsToExamine.Push(namedTypeThatIsSafeToUse);

            while (symbolsToExamine.Count > 0)
            {
                INamedTypeSymbol symbol = symbolsToExamine.Pop();
                if (symbolOfInvokeTarget.ContainingSymbol.Equals(symbol, SymbolEqualityComparer.Default))
                    return true;

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

        private static void AnalyzeOperation(IOperation operation, IMethodSymbol methodSymbol, OperationAnalysisContext context, IntrinsicsAnalyzerOnLoadData onLoadData)
        {
            var symbol = GetOperationSymbol(operation);

            if (symbol == null || symbol is ITypeSymbol type && type.SpecialType != SpecialType.None)
            {
                return;
            }

            if (methodSymbol.ContainingType.Equals(symbol.ContainingSymbol, SymbolEqualityComparer.Default))
            {
                return; // Intrinsic functions on their containing type can call themselves
            }

            bool methodNeedsProtectionWithIsSupported = false;
            foreach (var search in onLoadData.NamedTypesToBeProtected)
            {
                if (search.Equals(symbol.ContainingSymbol, SymbolEqualityComparer.Default))
                {
                    methodNeedsProtectionWithIsSupported = true;
                }
            }

            if (!methodNeedsProtectionWithIsSupported)
                return;

            if (symbol is IPropertySymbol propertySymbol)
            {
                if (propertySymbol.Name == "IsSupported")
                {
                    return;
                }
            }

            ISymbol? symbolThatMightHaveIntrinsicsHelperAttribute = methodSymbol;
            IOperation operationSearch = operation;
            while (operationSearch != null)
            {
                if (operationSearch.Kind == OperationKind.AnonymousFunction)
                {
                    symbolThatMightHaveIntrinsicsHelperAttribute = null;
                    break;
                }
                if (operationSearch.Kind == OperationKind.LocalFunction)
                {
                    // Assign symbolThatMightHaveIntrinsicsHelperAttribute to the symbol for the LocalFunction
                    ILocalFunctionOperation localFunctionOperation = (ILocalFunctionOperation)operationSearch;
                    symbolThatMightHaveIntrinsicsHelperAttribute = localFunctionOperation.Symbol;
                    break;
                }

                operationSearch = operationSearch.Parent;
            }

            var bypassReadyToRunForIntrinsicsHelperUse = onLoadData.BypassReadyToRunForIntrinsicsHelperUse;
            if ((bypassReadyToRunForIntrinsicsHelperUse != null) && symbolThatMightHaveIntrinsicsHelperAttribute != null)
            {
                foreach (var attributeData in symbolThatMightHaveIntrinsicsHelperAttribute.GetAttributes())
                {
                    if (attributeData.AttributeClass.Equals(bypassReadyToRunForIntrinsicsHelperUse, SymbolEqualityComparer.Default))
                    {
                        if (attributeData.ConstructorArguments[0].Value is INamedTypeSymbol attributeTypeSymbol && ConditionAllowsSymbol(symbol, attributeTypeSymbol))
                        {
                            // This attribute indicates that this method will only be compiled into a ReadyToRun image if the behavior
                            // of the associated IsSupported method is defined to a constant value during ReadyToRun compilation that cannot change at runtime
                            return;
                        }
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
                            var decomposedCondition = DecomposeConditionForIsSupportedGroups(operation.SemanticModel, condition);
                            if (decomposedCondition.Length == 1)
                            {
                                foreach (var symbolFromCondition in decomposedCondition[0])
                                {
                                    if (ConditionAllowsSymbol(symbol, symbolFromCondition))
                                    {
                                        // There is a good IsSupported check with a positive check for the IsSupported call involved. Do not report.
                                        return true;
                                    }
                                }
                            }
                            else if (decomposedCondition.Length > 1)
                            {
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

                                // Check to see if all of the OR conditions other than 1 have been eliminated via if statements excluding those symbols
                                int indexNotExcluded = -1;
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
                                        if (indexNotExcluded == -1)
                                        {
                                            indexNotExcluded = andClauseIndex;
                                        }
                                        else
                                        {
                                            // Multiple And clause groups not excluded. We didn't find a unique one.
                                            indexNotExcluded = -1;
                                            break;
                                        }
                                    }
                                }

                                if (indexNotExcluded != -1)
                                {
                                    var andClause = decomposedCondition[indexNotExcluded];
                                    foreach (var symbolFromCondition in andClause)
                                    {
                                        if (ConditionAllowsSymbol(symbol, symbolFromCondition))
                                        {
                                            // There is a good IsSupported check with a positive check for the IsSupported call involved. Do not report.
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                        else if (previousNode == syntaxOnNegativeCondition)
                        {
                            var decomposedCondition = DecomposeConditionForIsSupportedGroups(operation.SemanticModel, condition);
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

            context.ReportDiagnostic(Diagnostic.Create(Rule, operation.Syntax.GetLocation(), symbol.ContainingSymbol.ToDisplayString()));
        }
    }
}
