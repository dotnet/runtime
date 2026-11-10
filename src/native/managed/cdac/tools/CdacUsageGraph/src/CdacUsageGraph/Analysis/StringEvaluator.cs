// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace CdacUsageGraph.Analysis;

/// <summary>Evaluates statically-known string values from cDAC operation trees.</summary>
internal sealed class StringEvaluator(CSharpCompilation compilation)
{
    public IEnumerable<string> Evaluate(IOperation operation)
    {
        operation = OperationInspector.Unwrap(operation);
        if (operation.ConstantValue is { HasValue: true, Value: string constant })
        {
            yield return constant;
            yield break;
        }

        switch (operation)
        {
            case INameOfOperation nameOf when nameOf.ConstantValue.Value is string name:
                yield return name;
                break;
            case IFieldReferenceOperation { Field.HasConstantValue: true, Field.ConstantValue: string fieldConstant }:
                yield return fieldConstant;
                break;
            case ILocalReferenceOperation local:
                foreach (SyntaxReference reference in local.Local.DeclaringSyntaxReferences)
                {
                    if (reference.GetSyntax() is not VariableDeclaratorSyntax
                        {
                            Initializer.Value: { } initializer,
                        })
                    {
                        continue;
                    }

                    SemanticModel model = compilation.GetSemanticModel(initializer.SyntaxTree);
                    if (model.GetOperation(initializer) is not IOperation initializerOperation)
                        continue;
                    foreach (string localValue in Evaluate(initializerOperation))
                        yield return localValue;
                }
                break;
            case IInvocationOperation
                {
                    TargetMethod.Name: nameof(ToString),
                    TargetMethod.Parameters.Length: 0,
                    Instance.Type.TypeKind: TypeKind.Enum,
                } enumToString:
                yield return $"<{enumToString.Instance!.Type!.Name}>";
                break;
            case IBinaryOperation { OperatorKind: BinaryOperatorKind.Add } binary:
                foreach (string left in Evaluate(binary.LeftOperand))
                {
                    foreach (string right in Evaluate(binary.RightOperand))
                        yield return left + right;
                }
                break;
            case IConditionalOperation conditional:
                IEnumerable<string> values = Evaluate(conditional.WhenTrue);
                if (conditional.WhenFalse is not null)
                    values = values.Concat(Evaluate(conditional.WhenFalse));
                foreach (string conditionalValue in values.Distinct(StringComparer.Ordinal))
                {
                    yield return conditionalValue;
                }
                break;
            case ISwitchExpressionOperation switchExpression:
                foreach (string switchValue in switchExpression.Arms
                    .Where(arm => arm.Value is not null)
                    .SelectMany(arm => Evaluate(arm.Value!))
                    .Distinct(StringComparer.Ordinal))
                {
                    yield return switchValue;
                }
                break;
        }
    }
}
