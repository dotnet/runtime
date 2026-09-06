// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    private readonly Dictionary<CILParser.ScopeBlockContext, (int Start, int End)> _scopeRanges = new();
    // Method scopes nest while IL and local declarations are emitted, so their live offsets and
    // local-scope depth must be restored when each lexical scope exits.
    private readonly Stack<ScopeFrame> _scopeStack = new();
    private RuleContext? _methodOwner;

    private void EndMethod()
    {
        Debug.Assert(
            _scopeStack.Count == 0,
            "Lexical scope state must be released by its owning rule.");

        _methodOwner = null;
        if (_currentMethod is not null)
        {
            if (_currentMethod.AllLocals.Count > 0)
            {
                BlobBuilder localsSignature = new();
                BlobEncoder encoder = new(localsSignature);
                LocalVariablesEncoder localsEncoder =
                    encoder.LocalVariableSignature(_currentMethod.AllLocals.Count);
                foreach (SignatureArg local in _currentMethod.AllLocals)
                {
                    local.SignatureBlob.WriteContentTo(localsEncoder.AddVariable().Builder);
                }

                _currentMethod.Definition.LocalsSignature =
                    _entityRegistry.GetOrCreateStandaloneSignature(localsSignature);
            }

            ValidateLabelReferences();
            _currentMethod = null;
        }

        ResetMethodBodyState();
        ClearPendingCustomAttributeOwners();
    }

    private void ResetMethodBodyState()
    {
        _scopeRanges.Clear();
        _scopeStack.Clear();
    }

    internal void BeginScope(CILParser.ScopeBlockContext context)
    {
        if (_currentMethod is not null)
        {
            _scopeStack.Push(
                new ScopeFrame(context, CurrentMethodBodyOffset, _currentMethod.LocalsScopes.Count));
        }
    }

    internal void EndScope(CILParser.ScopeBlockContext context)
    {
        if (_scopeStack.Count == 0 || !ReferenceEquals(_scopeStack.Peek().Context, context))
        {
            return;
        }

        ScopeFrame frame = _scopeStack.Pop();
        if (_currentMethod is not null && frame.LocalsScopeCount < _currentMethod.LocalsScopes.Count)
        {
            _currentMethod.LocalsScopes.RemoveRange(
                frame.LocalsScopeCount,
                _currentMethod.LocalsScopes.Count - frame.LocalsScopeCount);
        }

        _scopeRanges[context] = (frame.Start, CurrentMethodBodyOffset);
    }

    internal void EndExceptionBlock(
        CILParser.SehBlockContext context,
        int initialSyntaxErrorCount)
    {
        if (_currentMethod is null ||
            HasSyntaxErrorsSince(initialSyntaxErrorCount) ||
            context.exception is not null ||
            context.tryRange is null ||
            context.clauses is null)
        {
            return;
        }

        ExceptionRangeValue tryRangeValue = context.tryRange.Value;
        (LabelHandle Start, LabelHandle End)? tryRange = ResolveExceptionRange(tryRangeValue);
        if (tryRange is null)
        {
            return;
        }

        foreach (ExceptionClauseValue clause in context.clauses.Value)
        {
            (LabelHandle Start, LabelHandle End)? handlerRange = ResolveExceptionRange(clause.Handler);
            if (handlerRange is null)
            {
                continue;
            }

            switch (clause)
            {
                case FinallyExceptionClauseValue:
                    AddExceptionRegion(new EntityRegistry.ExceptionRegion.FinallyRegion(
                        tryRange.Value.Start,
                        tryRange.Value.End,
                        handlerRange.Value.Start,
                        handlerRange.Value.End));
                    break;
                case FaultExceptionClauseValue:
                    AddExceptionRegion(new EntityRegistry.ExceptionRegion.FaultRegion(
                        tryRange.Value.Start,
                        tryRange.Value.End,
                        handlerRange.Value.Start,
                        handlerRange.Value.End));
                    break;
                case CatchExceptionClauseValue { CatchType: { IsValid: true, Type: not null } catchType }:
                    AddExceptionRegion(new EntityRegistry.ExceptionRegion.CatchRegion(
                        tryRange.Value.Start,
                        tryRange.Value.End,
                        handlerRange.Value.Start,
                        handlerRange.Value.End,
                        catchType.Type));
                    break;
                case FilterExceptionClauseValue filterClause
                    when ResolveExceptionFilter(filterClause.Filter) is LabelHandle filterStart:
                    AddExceptionRegion(new EntityRegistry.ExceptionRegion.FilterRegion(
                        tryRange.Value.Start,
                        tryRange.Value.End,
                        handlerRange.Value.Start,
                        handlerRange.Value.End,
                        filterStart));
                    break;
            }
        }
    }

    internal CatchTypeValue EndCatchClause(
        CILParser.CatchClauseContext context,
        int initialSyntaxErrorCount)
    {
        if (_currentMethod is null ||
            HasSyntaxErrorsSince(initialSyntaxErrorCount) ||
            context.exception is not null ||
            context.catchType is null ||
            context.catchType.HasSyntaxError)
        {
            return CatchTypeValue.Invalid;
        }

        EntityRegistry.TypeEntity catchType =
            ResolveTypeSpecification(context.catchType.Value);
        return new CatchTypeValue(catchType, isValid: true);
    }

    internal ExceptionRangeValue CreateScopeExceptionRange(CILParser.ScopeBlockContext scope)
        => new ScopeExceptionRangeValue(scope);

    internal ExceptionRangeValue CreateLabelExceptionRange(IToken start, IToken end)
        => new LabelExceptionRangeValue(ParseIdentifier(start), ParseIdentifier(end));

    internal ExceptionRangeValue CreateOffsetExceptionRange(IToken start, IToken end)
        => new OffsetExceptionRangeValue(ParseInt32(start), ParseInt32(end));

    internal ExceptionFilterValue CreateScopeFilter(CILParser.ScopeBlockContext scope)
        => new ScopeExceptionFilterValue(scope);

    internal ExceptionFilterValue CreateLabelFilter(IToken label)
        => new LabelExceptionFilterValue(ParseIdentifier(label));

    internal ExceptionFilterValue CreateOffsetFilter(IToken offset)
        => new OffsetExceptionFilterValue(ParseInt32(offset));

    internal ExceptionClauseValue CreateCatchExceptionClause(
        CatchTypeValue catchType,
        ExceptionRangeValue handler)
        => new CatchExceptionClauseValue(
            catchType,
            handler);

    internal ExceptionClauseValue CreateFilterExceptionClause(
        ExceptionFilterValue filter,
        ExceptionRangeValue handler)
        => new FilterExceptionClauseValue(
            filter,
            handler);

    internal ExceptionClauseValue CreateFinallyExceptionClause(ExceptionRangeValue handler)
        => new FinallyExceptionClauseValue(handler);

    internal ExceptionClauseValue CreateFaultExceptionClause(ExceptionRangeValue handler)
        => new FaultExceptionClauseValue(handler);

    private void AddExceptionRegion(EntityRegistry.ExceptionRegion region)
    {
        Debug.Assert(_currentMethod is not null);
        _currentMethod.Definition.ExceptionRegions.Add(region);
    }

    private (LabelHandle Start, LabelHandle End)? ResolveExceptionRange(ExceptionRangeValue range)
        => range switch
        {
            ScopeExceptionRangeValue scope => GetScopeRange(scope.Scope),
            LabelExceptionRangeValue labels => (
                GetOrCreateMethodLabel(labels.Start),
                GetOrCreateMethodLabel(labels.End)),
            OffsetExceptionRangeValue offsets => (
                DefineMethodLabelAtOffset(offsets.Start),
                DefineMethodLabelAtOffset(offsets.End)),
            _ => null,
        };

    private LabelHandle? ResolveExceptionFilter(ExceptionFilterValue filter)
        => filter switch
        {
            ScopeExceptionFilterValue scope => GetScopeRange(scope.Scope).Start,
            LabelExceptionFilterValue label => GetOrCreateMethodLabel(label.Label),
            OffsetExceptionFilterValue offset => DefineMethodLabelAtOffset(offset.Offset),
            _ => null,
        };

    private (LabelHandle Start, LabelHandle End) GetScopeRange(CILParser.ScopeBlockContext context)
    {
        if (!_scopeRanges.TryGetValue(context, out (int Start, int End) range))
        {
            int offset = CurrentMethodBodyOffset;
            range = (offset, offset);
        }

        return (
            DefineMethodLabelAtOffset(range.Start),
            DefineMethodLabelAtOffset(range.End));
    }

    private LabelHandle GetOrCreateMethodLabel(string name)
    {
        Debug.Assert(_currentMethod is not null);
        if (!_currentMethod.Labels.TryGetValue(name, out LabelHandle label))
        {
            label = _currentMethod.Definition.MethodBody.DefineLabel();
            _currentMethod.Labels[name] = label;
        }

        return label;
    }

    private LabelHandle DefineMethodLabelAtOffset(int offset)
    {
        Debug.Assert(_currentMethod is not null);
        LabelHandle label = _currentMethod.Definition.MethodBody.DefineLabel();
        _currentMethod.Definition.MethodBody.MarkLabel(label, offset);
        return label;
    }

    private int CurrentMethodBodyOffset => _currentMethod?.Definition.MethodBody.Offset ?? 0;

    private readonly record struct ScopeFrame(
        CILParser.ScopeBlockContext Context,
        int Start,
        int LocalsScopeCount);
}
#pragma warning restore CA1822
