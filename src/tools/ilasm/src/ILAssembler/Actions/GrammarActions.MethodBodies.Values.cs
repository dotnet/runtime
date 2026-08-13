// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    private readonly Stack<ParameterDirectiveFrame> _parameterDirectiveFrames = new();
    private readonly Stack<ExceptionBlockFrame> _exceptionBlockFrames = new();
    private readonly Stack<ExceptionClauseListFrame> _exceptionClauseListFrames = new();
    private readonly Stack<CatchClauseFrame> _catchClauseFrames = new();

    private sealed class ParameterDirectiveFrame
    {
        public ParameterDirectiveFrame(CILParser.ParameterDeclContext owner)
        {
            Owner = owner;
        }

        public CILParser.ParameterDeclContext Owner { get; }

        public List<CILParser.CustomAttrDeclContext> CustomAttributes { get; } = new();
    }

    private abstract record ExceptionRangeValue
    {
        public static ExceptionRangeValue Invalid { get; } = new InvalidExceptionRangeValue();
    }

    private sealed record InvalidExceptionRangeValue : ExceptionRangeValue;

    private sealed record ScopeExceptionRangeValue(CILParser.ScopeBlockContext Scope) : ExceptionRangeValue;

    private sealed record LabelExceptionRangeValue(string Start, string End) : ExceptionRangeValue;

    private sealed record OffsetExceptionRangeValue(int Start, int End) : ExceptionRangeValue;

    private abstract record ExceptionFilterValue
    {
        public static ExceptionFilterValue Invalid { get; } = new InvalidExceptionFilterValue();
    }

    private sealed record InvalidExceptionFilterValue : ExceptionFilterValue;

    private sealed record ScopeExceptionFilterValue(CILParser.ScopeBlockContext Scope) : ExceptionFilterValue;

    private sealed record LabelExceptionFilterValue(string Label) : ExceptionFilterValue;

    private sealed record OffsetExceptionFilterValue(int Offset) : ExceptionFilterValue;

    private sealed record CatchTypeValue(EntityRegistry.TypeEntity? Type, bool IsValid)
    {
        public static CatchTypeValue Invalid { get; } = new(null, IsValid: false);
    }

    private abstract record ExceptionClauseValue(ExceptionRangeValue Handler)
    {
        public static ExceptionClauseValue Invalid { get; } =
            new InvalidExceptionClauseValue(ExceptionRangeValue.Invalid);
    }

    private sealed record InvalidExceptionClauseValue(ExceptionRangeValue Handler)
        : ExceptionClauseValue(Handler);

    private sealed record CatchExceptionClauseValue(
        CatchTypeValue CatchType,
        ExceptionRangeValue Handler)
        : ExceptionClauseValue(Handler);

    private sealed record FilterExceptionClauseValue(
        ExceptionFilterValue Filter,
        ExceptionRangeValue Handler)
        : ExceptionClauseValue(Handler);

    private sealed record FinallyExceptionClauseValue(ExceptionRangeValue Handler)
        : ExceptionClauseValue(Handler);

    private sealed record FaultExceptionClauseValue(ExceptionRangeValue Handler)
        : ExceptionClauseValue(Handler);

    private sealed record ExceptionBlockFrame(
        CILParser.SehBlockContext Owner,
        int InitialSyntaxErrorCount);

    private sealed class ExceptionClauseListFrame
    {
        public ExceptionClauseListFrame(CILParser.SehClausesContext owner)
        {
            Owner = owner;
        }

        public CILParser.SehClausesContext Owner { get; }

        public ImmutableArray<ExceptionClauseValue>.Builder Clauses { get; } =
            ImmutableArray.CreateBuilder<ExceptionClauseValue>();
    }

    private sealed record CatchClauseFrame(
        CILParser.CatchClauseContext Owner,
        int InitialSyntaxErrorCount);

    private static ExceptionRangeValue GetExceptionRangeValue(object? value)
        => value as ExceptionRangeValue ?? ExceptionRangeValue.Invalid;

    private static ExceptionFilterValue GetExceptionFilterValue(object? value)
        => value as ExceptionFilterValue ?? ExceptionFilterValue.Invalid;

    private static CatchTypeValue GetCatchTypeValue(object? value)
        => value as CatchTypeValue ?? CatchTypeValue.Invalid;

    private static ExceptionClauseValue GetExceptionClauseValue(object? value)
        => value as ExceptionClauseValue ?? ExceptionClauseValue.Invalid;

    private static ImmutableArray<ExceptionClauseValue> GetExceptionClausesValue(object? value)
        => value is ImmutableArray<ExceptionClauseValue> clauses
            ? clauses
            : ImmutableArray<ExceptionClauseValue>.Empty;
}
