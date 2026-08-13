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
    private readonly Stack<ScopeFrame> _scopeStack = new();
    private RuleContext? _methodOwner;

    internal void BeginMethod(CILParser.MethodHeadContext context)
    {
        ClearPendingCustomAttributeOwners();
        ResetMethodBodyState();
        _currentMethod = new CurrentMethodContext(VisitMethodHead(context).Value);
        _methodOwner = context.Parent;
    }

    private void EndMethod()
    {
        Debug.Assert(
            _scopeStack.Count == 0 &&
            _parameterDirectiveFrames.Count == 0 &&
            _exceptionBlockFrames.Count == 0 &&
            _exceptionClauseListFrames.Count == 0 &&
            _catchClauseFrames.Count == 0 &&
            _semanticRootFrames.Count == 0,
            "Method-body frames must be released by their owning rules.");

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
        _parameterDirectiveFrames.Clear();
        _exceptionBlockFrames.Clear();
        _exceptionClauseListFrames.Clear();
        _catchClauseFrames.Clear();
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

    internal void BeginExceptionBlock(CILParser.SehBlockContext context)
        => _exceptionBlockFrames.Push(new(context, _syntaxErrorCount));

    internal void EndExceptionBlock(CILParser.SehBlockContext context)
    {
        Debug.Assert(_exceptionBlockFrames.Count > 0);
        if (_exceptionBlockFrames.Count == 0 ||
            !ReferenceEquals(_exceptionBlockFrames.Peek().Owner, context))
        {
            return;
        }

        ExceptionBlockFrame frame = _exceptionBlockFrames.Pop();
        if (_currentMethod is null ||
            frame.InitialSyntaxErrorCount != _syntaxErrorCount ||
            context.exception is not null ||
            context.tryRange is null ||
            context.clauses is null)
        {
            return;
        }

        ExceptionRangeValue tryRangeValue = GetExceptionRangeValue(context.tryRange.Value);
        (LabelHandle Start, LabelHandle End)? tryRange = ResolveExceptionRange(tryRangeValue);
        if (tryRange is null)
        {
            return;
        }

        foreach (ExceptionClauseValue clause in GetExceptionClausesValue(context.clauses.Value))
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

    internal void BeginExceptionClauses(CILParser.SehClausesContext context)
        => _exceptionClauseListFrames.Push(new(context));

    internal void AddExceptionClause(CILParser.SehClausesContext context, object? clause)
    {
        Debug.Assert(_exceptionClauseListFrames.Count > 0);
        if (_exceptionClauseListFrames.Count == 0)
        {
            return;
        }

        ExceptionClauseListFrame frame = _exceptionClauseListFrames.Peek();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        if (ReferenceEquals(frame.Owner, context))
        {
            frame.Clauses.Add(GetExceptionClauseValue(clause));
        }
    }

    internal object EndExceptionClauses(CILParser.SehClausesContext context)
    {
        Debug.Assert(_exceptionClauseListFrames.Count > 0);
        if (_exceptionClauseListFrames.Count == 0 ||
            !ReferenceEquals(_exceptionClauseListFrames.Peek().Owner, context))
        {
            return ImmutableArray<ExceptionClauseValue>.Empty;
        }

        return _exceptionClauseListFrames.Pop().Clauses.ToImmutable();
    }

    internal void BeginCatchClause(CILParser.CatchClauseContext context)
        => _catchClauseFrames.Push(new(context, _syntaxErrorCount));

    internal object EndCatchClause(CILParser.CatchClauseContext context)
    {
        Debug.Assert(_catchClauseFrames.Count > 0);
        if (_catchClauseFrames.Count == 0 ||
            !ReferenceEquals(_catchClauseFrames.Peek().Owner, context))
        {
            return CatchTypeValue.Invalid;
        }

        CatchClauseFrame frame = _catchClauseFrames.Pop();
        if (_currentMethod is null ||
            frame.InitialSyntaxErrorCount != _syntaxErrorCount ||
            context.exception is not null ||
            context.catchType is null ||
            context.catchType.HasSyntaxError)
        {
            return CatchTypeValue.Invalid;
        }

        EntityRegistry.TypeEntity catchType =
            ResolveTypeSpecification(GetTypeSpecificationValue(context.catchType.Value));
        return new CatchTypeValue(catchType, IsValid: true);
    }

    internal object CreateScopeExceptionRange(CILParser.ScopeBlockContext scope)
        => new ScopeExceptionRangeValue(scope);

    internal object CreateLabelExceptionRange(IToken start, IToken end)
        => new LabelExceptionRangeValue(ParseIdentifier(start), ParseIdentifier(end));

    internal object CreateOffsetExceptionRange(IToken start, IToken end)
        => new OffsetExceptionRangeValue(ParseInt32(start), ParseInt32(end));

    internal object CreateScopeFilter(CILParser.ScopeBlockContext scope)
        => new ScopeExceptionFilterValue(scope);

    internal object CreateLabelFilter(IToken label)
        => new LabelExceptionFilterValue(ParseIdentifier(label));

    internal object CreateOffsetFilter(IToken offset)
        => new OffsetExceptionFilterValue(ParseInt32(offset));

    internal object CreateCatchExceptionClause(object? catchType, object? handler)
        => new CatchExceptionClauseValue(
            GetCatchTypeValue(catchType),
            GetExceptionRangeValue(handler));

    internal object CreateFilterExceptionClause(object? filter, object? handler)
        => new FilterExceptionClauseValue(
            GetExceptionFilterValue(filter),
            GetExceptionRangeValue(handler));

    internal object CreateFinallyExceptionClause(object? handler)
        => new FinallyExceptionClauseValue(GetExceptionRangeValue(handler));

    internal object CreateFaultExceptionClause(object? handler)
        => new FaultExceptionClauseValue(GetExceptionRangeValue(handler));

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
