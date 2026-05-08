// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        /// <summary>
        /// Suppresses false-positive diagnostics emitted by the linker
        /// when analyzing binding invocations that we have intercepted.
        /// Workaround for https://github.com/dotnet/roslyn/issues/68669.
        /// </summary>
        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        public sealed class Suppressor : DiagnosticSuppressor
        {
            private const string Justification = "The target method has been intercepted by a generated static variant.";

            /// <summary>
            /// Suppression descriptor for IL2026: Members attributed with RequiresUnreferencedCode may break when trimming.
            /// </summary>
            private static readonly SuppressionDescriptor RUCDiagnostic = new(id: "SYSLIBSUPPRESS0002", suppressedDiagnosticId: "IL2026", Justification);

            /// <summary>
            /// Suppression descriptor for IL3050: Avoid calling members annotated with 'RequiresDynamicCodeAttribute' when publishing as native AOT.
            /// </summary>
            private static readonly SuppressionDescriptor RDCDiagnostic = new(id: "SYSLIBSUPPRESS0003", suppressedDiagnosticId: "IL3050", Justification);

            public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(RUCDiagnostic, RDCDiagnostic);

            public override void ReportSuppressions(SuppressionAnalysisContext context)
            {
                // Lazily built set of locations that were actually intercepted by the generator.
                HashSet<string>? interceptedLocationKeys = null;

                foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
                {
                    string diagnosticId = diagnostic.Id;

                    if (diagnosticId != RDCDiagnostic.SuppressedDiagnosticId && diagnosticId != RUCDiagnostic.SuppressedDiagnosticId)
                    {
                        continue;
                    }

                    Location location = diagnostic.AdditionalLocations.Count > 0
                        ? diagnostic.AdditionalLocations[0]
                        : diagnostic.Location;

                    // The trim analyzer changed from warning on the InvocationExpression to the MemberAccessExpression in https://github.com/dotnet/runtime/pull/110086
                    // In other words, the warning location went from from `{|Method1(arg1, arg2)|}` to `{|Method1|}(arg1, arg2)`
                    // To account for this, we need to check if the location is an InvocationExpression or a child of an InvocationExpression.
                    // Use getInnermostNodeForTie to handle the case where the binding call is an
                    // argument to another method (e.g. Some.Method(config.Get<T>())). In that case,
                    // ArgumentSyntax and the inner InvocationExpressionSyntax can share the same span.
                    if (location.SourceTree is not SyntaxTree sourceTree)
                    {
                        continue;
                    }

                    SyntaxNode syntaxNode = sourceTree.GetRoot(context.CancellationToken).FindNode(location.SourceSpan, getInnermostNodeForTie: true);
                    if ((syntaxNode as InvocationExpressionSyntax ?? syntaxNode.Parent as InvocationExpressionSyntax) is not InvocationExpressionSyntax invocation)
                    {
                        continue;
                    }

                    if (!BinderInvocation.IsCandidateSyntaxNode(invocation))
                    {
                        continue;
                    }

                    SemanticModel semanticModel = context.GetSemanticModel(sourceTree);
                    if (semanticModel.GetOperation(invocation, context.CancellationToken) is not IInvocationOperation operation ||
                        !BinderInvocation.IsBindingOperation(operation))
                    {
                        continue;
                    }

                    // Only suppress if the generator actually intercepted this call site.
                    // The generator may skip interception for unsupported types (https://github.com/dotnet/runtime/issues/96643).
                    interceptedLocationKeys ??= CollectInterceptedLocationKeys(context.Compilation, context.CancellationToken);

                    string? locationKey = GetInvocationLocationKey(invocation, semanticModel, context.CancellationToken);
                    if (locationKey is null || !interceptedLocationKeys.Contains(locationKey))
                    {
                        continue;
                    }

                    SuppressionDescriptor targetSuppression = diagnosticId == RUCDiagnostic.SuppressedDiagnosticId
                            ? RUCDiagnostic
                            : RDCDiagnostic;
                    context.ReportSuppression(Suppression.Create(targetSuppression, diagnostic));
                }
            }

            /// <summary>
            /// Scans the generated source trees for [InterceptsLocation] attributes and collects
            /// all intercepted locations. For v0, locations are (filePath, line, column) tuples.
            /// For v1, locations are the encoded data strings from the attribute.
            /// </summary>
            private static HashSet<string> CollectInterceptedLocationKeys(Compilation compilation, CancellationToken cancellationToken)
            {
                var keys = new HashSet<string>();

                foreach (SyntaxTree tree in compilation.SyntaxTrees)
                {
                    if (!tree.FilePath.EndsWith("BindingExtensions.g.cs", System.StringComparison.Ordinal))
                    {
                        continue;
                    }

                    SyntaxNode root = tree.GetRoot(cancellationToken);
                    foreach (AttributeSyntax attr in root.DescendantNodes().OfType<AttributeSyntax>())
                    {
                        // Matching the name like this is somewhat brittle, but it's okay as long as we match what the generator emits.
                        if (attr.Name.ToString() != "InterceptsLocation")
                        {
                            continue;
                        }

                        AttributeArgumentListSyntax? argList = attr.ArgumentList;
                        if (argList is null)
                        {
                            continue;
                        }

                        SeparatedSyntaxList<AttributeArgumentSyntax> args = argList.Arguments;

                        if (InterceptorVersion == 0)
                        {
                            // v0 format: [InterceptsLocation("filePath", line, column)]
                            if (args.Count == 3 &&
                                args[0].Expression is LiteralExpressionSyntax filePathLiteral &&
                                filePathLiteral.IsKind(SyntaxKind.StringLiteralExpression) &&
                                args[1].Expression is LiteralExpressionSyntax lineLiteral &&
                                lineLiteral.IsKind(SyntaxKind.NumericLiteralExpression) &&
                                args[2].Expression is LiteralExpressionSyntax colLiteral &&
                                colLiteral.IsKind(SyntaxKind.NumericLiteralExpression))
                            {
                                keys.Add(MakeV0Key(filePathLiteral.Token.ValueText, (int)lineLiteral.Token.Value!, (int)colLiteral.Token.Value!));
                            }
                        }
                        else
                        {
                            // v1 format: [InterceptsLocation(version, "data")]
                            if (args.Count == 2 &&
                                args[1].Expression is LiteralExpressionSyntax dataLiteral &&
                                dataLiteral.IsKind(SyntaxKind.StringLiteralExpression))
                            {
                                keys.Add(MakeV1Key(dataLiteral.Token.ValueText));
                            }
                        }
                    }
                }

                return keys;
            }

            private static string MakeV0Key(string filePath, int line, int column) => $"{filePath}|{line}|{column}";

            private static string MakeV1Key(string data) => data;

            private static string? GetInvocationLocationKey(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                {
                    return null;
                }

                if (InterceptorVersion == 0)
                {
                    SyntaxTree syntaxTree = memberAccess.SyntaxTree;
                    TextSpan nameSpan = memberAccess.Name.Span;
                    FileLinePositionSpan lineSpan = syntaxTree.GetLineSpan(nameSpan, cancellationToken);

                    string filePath;
                    SourceReferenceResolver? resolver = semanticModel.Compilation.Options.SourceReferenceResolver;
                    filePath = resolver?.NormalizePath(syntaxTree.FilePath, baseFilePath: null) ?? syntaxTree.FilePath;

                    return MakeV0Key(filePath, lineSpan.StartLinePosition.Line + 1, lineSpan.StartLinePosition.Character + 1);
                }
                else
                {
                    object? interceptableLocation = GetInterceptableLocationFunc?.Invoke(semanticModel, invocation, cancellationToken);
                    if (interceptableLocation is null)
                    {
                        return null;
                    }

                    string data = (string)InterceptableLocationDataGetter!.Invoke(interceptableLocation, parameters: null)!;

                    return MakeV1Key(data);
                }
            }
        }
    }
}
