// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    private readonly Dictionary<CILParser.ScopeBlockContext, (int Start, int End)> _scopeRanges = new();
    private readonly Stack<ScopeFrame> _scopeStack = new();
    private readonly Dictionary<CILParser.CatchClauseContext, EntityRegistry.TypeEntity> _catchTypes = new();
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
        _methodOwner = null;
        if (_currentMethod is not null)
        {
            if (_currentMethod.AllLocals.Count > 0)
            {
                BlobBuilder localsSignature = new();
                BlobEncoder encoder = new(localsSignature);
                LocalVariablesEncoder localsEncoder = encoder.LocalVariableSignature(_currentMethod.AllLocals.Count);
                foreach (SignatureArg local in _currentMethod.AllLocals)
                {
                    local.SignatureBlob.WriteContentTo(localsEncoder.AddVariable().Builder);
                }

                _currentMethod.Definition.LocalsSignature = _entityRegistry.GetOrCreateStandaloneSignature(localsSignature);
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
        _catchTypes.Clear();
    }

    internal void OnMethodDeclaration(CILParser.MethodDeclContext context)
    {
        // A method header that failed to parse never opened a method, so its body items have
        // nothing to attach to.
        if (_currentMethod is null)
        {
            return;
        }

        if (TryProcessInstruction(context) ||
            context.scopeBlock() is not null ||
            context.sehBlock() is not null)
        {
            return;
        }

        VisitMethodDecl(context);
    }

    internal void BeginScope(CILParser.ScopeBlockContext context)
    {
        if (_currentMethod is null)
        {
            return;
        }

        _scopeStack.Push(new ScopeFrame(context, CurrentMethodBodyOffset, _currentMethod.LocalsScopes.Count));
    }

    /// <summary>
    /// Closes the lexical scope opened for <paramref name="context"/>, if one was opened.
    /// </summary>
    /// <remarks>
    /// This runs from the <c>scopeBlock</c> rule's <c>finally</c> block. It is keyed on the owning
    /// context so that it is idempotent and so that an unopened scope (for example when the opening
    /// brace failed to match) cannot pop a frame belonging to an enclosing scope.
    /// </remarks>
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

    /// <summary>
    /// Resolves the type of a <c>catch</c> clause before its handler body is parsed.
    /// </summary>
    /// <remarks>
    /// Native ilasm resolves the caught type as soon as it is parsed, so the type reference it
    /// creates precedes any reference created by the handler body. Resolving the type when the
    /// exception regions are recorded instead would reorder the emitted <c>TypeRef</c> rows.
    /// </remarks>
    internal void OnCatchClause(CILParser.CatchClauseContext context)
    {
        if (_currentMethod is null)
        {
            return;
        }

        _catchTypes[context] = VisitTypeSpec(context.typeSpec()).Value;
    }

    internal void EndExceptionBlock(CILParser.SehBlockContext context)
    {
        if (_currentMethod is null || context.sehClauses() is not CILParser.SehClausesContext clauses)
        {
            return;
        }

        (LabelHandle TryStart, LabelHandle TryEnd) tryRange = GetTryRange(context.tryBlock());

        foreach (CILParser.SehClauseContext clause in clauses.sehClause())
        {
            (LabelHandle HandlerStart, LabelHandle HandlerEnd) handlerRange = GetHandlerRange(clause.handlerBlock());
            if (clause.finallyClause() is not null)
            {
                AddExceptionRegion(new EntityRegistry.ExceptionRegion.FinallyRegion(
                    tryRange.TryStart,
                    tryRange.TryEnd,
                    handlerRange.HandlerStart,
                    handlerRange.HandlerEnd));
            }
            else if (clause.faultClause() is not null)
            {
                AddExceptionRegion(new EntityRegistry.ExceptionRegion.FaultRegion(
                    tryRange.TryStart,
                    tryRange.TryEnd,
                    handlerRange.HandlerStart,
                    handlerRange.HandlerEnd));
            }
            else if (clause.catchClause() is CILParser.CatchClauseContext catchClause)
            {
                if (_catchTypes.Remove(catchClause, out EntityRegistry.TypeEntity? catchType))
                {
                    AddExceptionRegion(new EntityRegistry.ExceptionRegion.CatchRegion(
                        tryRange.TryStart,
                        tryRange.TryEnd,
                        handlerRange.HandlerStart,
                        handlerRange.HandlerEnd,
                        catchType));
                }
            }
            else if (clause.filterClause() is CILParser.FilterClauseContext filterClause)
            {
                AddExceptionRegion(new EntityRegistry.ExceptionRegion.FilterRegion(
                    tryRange.TryStart,
                    tryRange.TryEnd,
                    handlerRange.HandlerStart,
                    handlerRange.HandlerEnd,
                    GetFilterStart(filterClause)));
            }
        }
    }

    private void AddExceptionRegion(EntityRegistry.ExceptionRegion region)
    {
        Debug.Assert(_currentMethod is not null);
        _currentMethod.Definition.ExceptionRegions.Add(region);
    }

    private int CurrentMethodBodyOffset => _currentMethod?.Definition.MethodBody.Offset ?? 0;

    private LabelHandle GetOrCreateMethodLabel(CILParser.IdContext context)
    {
        Debug.Assert(_currentMethod is not null);
        string name = VisitId(context).Value;
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

    private (LabelHandle Start, LabelHandle End) GetTryRange(CILParser.TryBlockContext? context)
        => context?.scopeBlock() is CILParser.ScopeBlockContext scope
            ? GetScopeRange(scope)
            : GetExplicitRange(context?.id(), context?.int32());

    private (LabelHandle Start, LabelHandle End) GetHandlerRange(CILParser.HandlerBlockContext? context)
        => context?.scopeBlock() is CILParser.ScopeBlockContext scope
            ? GetScopeRange(scope)
            : GetExplicitRange(context?.id(), context?.int32());

    private LabelHandle GetFilterStart(CILParser.FilterClauseContext context)
    {
        if (context.scopeBlock() is CILParser.ScopeBlockContext scope)
        {
            return GetScopeRange(scope).Start;
        }

        if (context.id() is CILParser.IdContext id)
        {
            return GetOrCreateMethodLabel(id);
        }

        return context.int32() is CILParser.Int32Context offset
            ? DefineMethodLabelAtOffset(VisitInt32(offset).Value)
            : DefineMethodLabelAtOffset(CurrentMethodBodyOffset);
    }

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

    /// <summary>
    /// Resolves an explicit label or offset range, falling back to an empty range at the current
    /// offset when error recovery left the range unmatched.
    /// </summary>
    private (LabelHandle Start, LabelHandle End) GetExplicitRange(
        CILParser.IdContext[]? ids,
        CILParser.Int32Context[]? offsets)
    {
        if (ids is { Length: 2 })
        {
            return (GetOrCreateMethodLabel(ids[0]), GetOrCreateMethodLabel(ids[1]));
        }

        if (offsets is { Length: 2 })
        {
            return (
                DefineMethodLabelAtOffset(VisitInt32(offsets[0]).Value),
                DefineMethodLabelAtOffset(VisitInt32(offsets[1]).Value));
        }

        int currentOffset = CurrentMethodBodyOffset;
        return (
            DefineMethodLabelAtOffset(currentOffset),
            DefineMethodLabelAtOffset(currentOffset));
    }

    private readonly record struct ScopeFrame(CILParser.ScopeBlockContext Context, int Start, int LocalsScopeCount);

    public GrammarResult VisitMethodDecl(CILParser.MethodDeclContext context)
    {
        Debug.Assert(_currentMethod is not null);
        var currentMethod = _currentMethod!;

        if (context.EMITBYTE() is not null)
        {
            currentMethod.Definition.MethodBody.CodeBuilder.WriteByte((byte)VisitInt32(context.GetChild<CILParser.Int32Context>(0)).Value);
        }
        else if (context.ENTRYPOINT() is not null)
        {
            _entityRegistry.EntryPoint = currentMethod.Definition;
        }
        else if (context.ZEROINIT() is not null)
        {
            currentMethod.Definition.BodyAttributes = MethodBodyAttributes.InitLocals;
        }
        else if (context.MAXSTACK() is not null)
        {
            currentMethod.Definition.MaxStack = VisitInt32(context.GetChild<CILParser.Int32Context>(0)).Value;
        }
        else if (context.LOCALS() is not null)
        {
            if (context.ChildCount == 3)
            {
                // init keyword specified
                currentMethod.Definition.BodyAttributes = MethodBodyAttributes.InitLocals;
            }
            Dictionary<string, int> localsScope;
            if (currentMethod.LocalsScopes.Count != 0)
            {
                localsScope = currentMethod.LocalsScopes[currentMethod.LocalsScopes.Count - 1];
            }
            else
            {
                localsScope = new();
                currentMethod.LocalsScopes.Add(localsScope);
            }
            var newLocals = VisitSigArgs(context.sigArgs()).Value;
            foreach (var loc in newLocals)
            {
                // BREAK-COMPAT: We don't allow specifying a local's slot via the [in], [out], or [opt] parameter attributes, or the custom int override.
                // This only worked in ilasm due to how ilasm reused fields.
                // We're only going to support allowing this tool to determine the slot numbers.
                // This blocks two different locals in two different scopes from resuing the same slot
                // but that is a very rare scenario (even using more than one .locals block in a method in IL is quite rare)

                // If the local is named, add it to our name-lookup dictionary.
                // Otherwise, it will only be accessible via its index.
                if (loc.Name is not null)
                {
                    localsScope.TryAdd(loc.Name, currentMethod.AllLocals.Count);
                }
                currentMethod.AllLocals.Add(loc);
            }
        }
        else if (context.labelDecl() is CILParser.LabelDeclContext labelDecl)
        {
            var labelId = labelDecl.id();
            string labelName = VisitId(labelId).Value;
            currentMethod.DeclaredLabels.Add(labelName);
            if (!currentMethod.Labels.TryGetValue(labelName, out var label))
            {
                label = currentMethod.Definition.MethodBody.DefineLabel();
                currentMethod.Labels[labelName] = label;
            }
            currentMethod.Definition.MethodBody.MarkLabel(label);
        }
        else if (context.EXPORT() is not null)
        {
            // .export [ordinal] or .export [ordinal] as alias
            int ordinal = VisitInt32(context.int32()[0]).Value;
            string? alias = context.id() is { } aliasId ? VisitId(aliasId).Value : null;

            currentMethod.Definition.ExportOrdinal = ordinal;
            currentMethod.Definition.ExportAlias = alias;
        }
        else if (context.VTENTRY() is not null)
        {
            // .vtentry vtableIndex : slotIndex
            int vtableEntry = VisitInt32(context.int32()[0]).Value;
            int vtableSlot = VisitInt32(context.int32()[1]).Value;

            currentMethod.Definition.VTableEntry = vtableEntry;
            currentMethod.Definition.VTableSlot = vtableSlot;
        }
        else if (context.OVERRIDE() is not null)
        {
            EntityRegistry.TypeDefinitionEntity? currentType = _currentTypeDefinition.PeekOrDefault();
            if (currentType is null)
            {
                return GrammarResult.SentinelValue.Result;
            }

            BlobBuilder signature = currentMethod.Definition.MethodSignature!;
            if (context.callConv() is {} callConv)
            {
                // We have an explicitly specified signature, so we need to parse it.
                signature = new();
                var callConvByte = VisitCallConv(callConv).Value;
                var arity = VisitGenArity(context.genArity()).Value;
                if (arity > 0)
                {
                    callConvByte |= (byte)SignatureAttributes.Generic;
                }
                signature.WriteByte(callConvByte);
                if (arity > 0)
                {
                    signature.WriteCompressedInteger(arity);
                }
                var args = VisitSigArgs(context.sigArgs()).Value;
                signature.WriteCompressedInteger(args.Length);
                VisitType(context.type()).Value.WriteContentTo(signature);
                foreach (var arg in args)
                {
                    arg.SignatureBlob.WriteContentTo(signature);
                }
            }

            var ownerType = VisitTypeSpec(context.typeSpec()).Value;
            var methodName = VisitMethodName(context.methodName()).Value;
            var methodRef = _entityRegistry.CreateLazilyRecordedMemberReference(ownerType, methodName, signature);
            currentType.MethodImplementations.Add(EntityRegistry.CreateUnrecordedMethodImplementation(currentMethod.Definition, methodRef));
        }
        else if (context.PARAM() is not null)
        {
            // BREAK-COMPAT: We require attributes on parameters, generic parameters, and constraints
            // to be specified directly after the .param directive, not at any point later in the method.
            // This matches the IL outputted by ILDASM, ILSpy, and other tools in the ecosystem.
            // Attributes not specified directly after the .param directive are applied to the method itself.
            var customAttrDeclarations = context.customAttrDecl();
            if (context.TYPE() is not null)
            {
                // Type parameters
                EntityRegistry.GenericParameterEntity? param = null;
                if (context.int32() is { Length: > 0 } int32)
                {
                    int index = VisitInt32(int32[0]).Value;
                    if (index < 0 || index >= currentMethod.Definition.GenericParameters.Count)
                    {
                        ReportError(DiagnosticIds.GenericParameterIndexOutOfRange,
                            string.Format(DiagnosticMessageTemplates.GenericParameterIndexOutOfRange, index),
                            context);
                        return GrammarResult.SentinelValue.Result;
                    }
                    param = currentMethod.Definition.GenericParameters[index];
                }
                else
                {
                    string name = VisitDottedName(context.dottedName()).Value;
                    foreach (var genericParam in currentMethod.Definition.GenericParameters)
                    {
                        if (genericParam.Name == name)
                        {
                            param = genericParam;
                            break;
                        }
                    }
                    if (param is null)
                    {
                        ReportError(DiagnosticIds.UnknownGenericParameter,
                            string.Format(DiagnosticMessageTemplates.UnknownGenericParameter, name),
                            context);
                        return GrammarResult.SentinelValue.Result;
                    }
                }
                foreach (var attr in customAttrDeclarations ?? Array.Empty<CILParser.CustomAttrDeclContext>())
                {
                    var customAttrDecl = VisitCustomAttrDecl(attr).Value;
                    customAttrDecl?.Owner = param;
                }
            }
            else if (context.CONSTRAINT() is not null)
            {
                // constraints
                EntityRegistry.GenericParameterEntity? param = null;
                if (context.int32() is { Length: > 0 } int32)
                {
                    int index = VisitInt32(int32[0]).Value;
                    if (index < 0 || index >= currentMethod.Definition.GenericParameters.Count)
                    {
                        ReportError(DiagnosticIds.GenericParameterIndexOutOfRange,
                            string.Format(DiagnosticMessageTemplates.GenericParameterIndexOutOfRange, index),
                            context);
                        return GrammarResult.SentinelValue.Result;
                    }
                    param = currentMethod.Definition.GenericParameters[index];
                }
                else
                {
                    string name = VisitDottedName(context.dottedName()).Value;
                    foreach (var genericParam in currentMethod.Definition.GenericParameters)
                    {
                        if (genericParam.Name == name)
                        {
                            param = genericParam;
                            break;
                        }
                    }
                    if (param is null)
                    {
                        ReportError(DiagnosticIds.UnknownGenericParameter,
                            string.Format(DiagnosticMessageTemplates.UnknownGenericParameter, name),
                            context);
                        return GrammarResult.SentinelValue.Result;
                    }
                }
                EntityRegistry.GenericParameterConstraintEntity? constraint = null;
                var baseType = VisitTypeSpec(context.typeSpec()).Value;
                foreach (var constraintEntity in param.Constraints)
                {
                    if (constraintEntity.BaseType == baseType)
                    {
                        constraint = constraintEntity;
                        break;
                    }
                }
                if (constraint is null)
                {
                    constraint = EntityRegistry.CreateGenericConstraint(baseType);
                    constraint.Owner = param;
                    param.Constraints.Add(constraint);
                    currentMethod.Definition.GenericParameterConstraints.Add(constraint);
                }
                foreach (var attr in customAttrDeclarations ?? Array.Empty<CILParser.CustomAttrDeclContext>())
                {
                    var customAttrDecl = VisitCustomAttrDecl(attr).Value;
                    customAttrDecl?.Owner = constraint;
                }
            }
            else
            {
                // Adding attributes to parameters.
                int index = VisitInt32(context.int32()[0]).Value;
                if (index < 0 || index >= currentMethod.Definition.Parameters.Count)
                {
                    ReportError(DiagnosticIds.ParameterIndexOutOfRange,
                        string.Format(DiagnosticMessageTemplates.ParameterIndexOutOfRange, index),
                        context);
                    return GrammarResult.SentinelValue.Result;
                }

                // Handle initOpt to get the Constant table entry if a constant value is provided.
                var constantValue = VisitInitOpt(context.initOpt()).Value;
                var param = currentMethod.Definition.Parameters[index];
                if (constantValue is not NoConstantSentinel)
                {
                    param.ConstantValue = constantValue;
                    param.HasConstant = true;
                }
                foreach (var attr in customAttrDeclarations ?? Array.Empty<CILParser.CustomAttrDeclContext>())
                {
                    var customAttrDecl = VisitCustomAttrDecl(attr).Value;
                    if (customAttrDecl is not null)
                    {
                        customAttrDecl.Owner = param;
                        param.HasCustomAttributes = true;
                    }
                }
            }
        }
        else if (context.secDecl() is {} secDecl)
        {
            var declarativeSecurity = VisitSecDecl(secDecl).Value;
            declarativeSecurity?.Parent = currentMethod.Definition;
        }
        else if (context.customDescrInMethodBody() is {} customDescrInMethod)
        {
            var customAttr = VisitCustomDescrInMethodBody(customDescrInMethod).Value;
            if (customAttr is not null)
            {
                customAttr.Owner = currentMethod.Definition;
            }
        }
        else if (context.GetChild(0) is CILParser.InstrContext instr)
        {
            _ = VisitInstr(instr);
        }
        else
        {
            // Handle other methodDecl alternatives
            var child = context.children[0];
            _ = child.Accept(this);
        }
        return GrammarResult.SentinelValue.Result;
    }

#pragma warning disable CA1822 // Mark members as static
        GrammarResult ICILVisitor<GrammarResult>.VisitCatchClause(CILParser.CatchClauseContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        public GrammarResult VisitFaultClause(CILParser.FaultClauseContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

        GrammarResult ICILVisitor<GrammarResult>.VisitFilterClause(CILParser.FilterClauseContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        public GrammarResult VisitFinallyClause(CILParser.FinallyClauseContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

        GrammarResult ICILVisitor<GrammarResult>.VisitHandlerBlock(CILParser.HandlerBlockContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitImplAttr(ILAssembler.CILParser.ImplAttrContext context) => VisitImplAttr(context);
        public GrammarResult.Flag<MethodImplAttributes> VisitImplAttr(CILParser.ImplAttrContext context)
        {
            if (context.int32() is CILParser.Int32Context int32)
            {
                return new((MethodImplAttributes)VisitInt32(int32).Value, ShouldAppend: false);
            }
            string attribute = context.GetText();
            return attribute switch
            {
                "native" => new(MethodImplAttributes.Native, MethodImplAttributes.CodeTypeMask),
                "cil" or "il" => new(MethodImplAttributes.IL, MethodImplAttributes.CodeTypeMask),
                "optil" => new(MethodImplAttributes.OPTIL, MethodImplAttributes.CodeTypeMask),
                "managed" => new(MethodImplAttributes.Managed, MethodImplAttributes.ManagedMask),
                "unmanaged" => new(MethodImplAttributes.Unmanaged, MethodImplAttributes.ManagedMask),
                "forwardref" => new(MethodImplAttributes.ForwardRef),
                "preservesig" => new(MethodImplAttributes.PreserveSig),
                "runtime" => new(MethodImplAttributes.Runtime, MethodImplAttributes.CodeTypeMask),
                "internalcall" => new(MethodImplAttributes.InternalCall),
                "synchronized" => new(MethodImplAttributes.Synchronized),
                "noinlining" => new(MethodImplAttributes.NoInlining),
                "aggressiveinlining" => new(MethodImplAttributes.AggressiveInlining),
                "nooptimization" => new(MethodImplAttributes.NoOptimization),
                "aggressiveoptimization" => new(MethodImplAttributes.AggressiveOptimization),
                "async" => new(MethodImplAttributes.Async),
                _ => throw new UnreachableException(),
            };
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitImplClause(CILParser.ImplClauseContext context) => VisitImplClause(context);
        public GrammarResult.Sequence<EntityRegistry.InterfaceImplementationEntity> VisitImplClause(CILParser.ImplClauseContext context) => context.implList() is {} implList ? VisitImplList(implList) : new(ImmutableArray<EntityRegistry.InterfaceImplementationEntity>.Empty);

        GrammarResult ICILVisitor<GrammarResult>.VisitImplList(CILParser.ImplListContext context) => VisitImplList(context);
        public GrammarResult.Sequence<EntityRegistry.InterfaceImplementationEntity> VisitImplList(CILParser.ImplListContext context)
        {
            var builder = ImmutableArray.CreateBuilder<EntityRegistry.InterfaceImplementationEntity>();
            foreach (var impl in context.typeSpec())
            {
                builder.Add(EntityRegistry.CreateUnrecordedInterfaceImplementation(_currentTypeDefinition.PeekOrDefault()!, VisitTypeSpec(impl).Value));
            }
            return new(builder.ToImmutable());
        }

        private void ValidateLabelReferences()
        {
            if (_currentMethod is null)
            {
                return;
            }

            // Report errors for any labels that were referenced but never declared
            foreach (var undefinedLabel in _currentMethod.UndefinedLabelReferences)
            {
                string labelName = undefinedLabel.Key;
                ParserRuleContext context = undefinedLabel.Value;

                // Only report if the label was never declared
                if (!_currentMethod.DeclaredLabels.Contains(labelName))
                {
                    ReportError(DiagnosticIds.LabelNotFound,
                        string.Format(DiagnosticMessageTemplates.LabelNotFound, labelName),
                        context);
                }
            }
        }

        public GrammarResult VisitLabels(CILParser.LabelsContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

        GrammarResult ICILVisitor<GrammarResult>.VisitLabelDecl(CILParser.LabelDeclContext context) => VisitLabelDecl(context);
        public GrammarResult VisitLabelDecl(CILParser.LabelDeclContext context)
        {
            var labelId = context.id();
            string labelName = VisitId(labelId).Value;
            _currentMethod!.DeclaredLabels.Add(labelName);
            if (!_currentMethod!.Labels.TryGetValue(labelName, out var label))
            {
                label = _currentMethod.Definition.MethodBody.DefineLabel();
                _currentMethod.Labels[labelName] = label;
            }
            _currentMethod.Definition.MethodBody.MarkLabel(label);
            return GrammarResult.SentinelValue.Result;
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitMethAttr(CILParser.MethAttrContext context) => VisitMethAttr(context);
        public GrammarResult.Flag<MethodAttributes> VisitMethAttr(CILParser.MethAttrContext context)
        {
            if (context.int32() is CILParser.Int32Context int32)
            {
                return new((MethodAttributes)VisitInt32(int32).Value, ShouldAppend: false);
            }
            string attribute = context.GetText();
            return attribute switch
            {
                "static" => new(MethodAttributes.Static),
                "public" => new(MethodAttributes.Public, MethodAttributes.MemberAccessMask),
                "private" => new(MethodAttributes.Private, MethodAttributes.MemberAccessMask),
                "family" => new(MethodAttributes.Family, MethodAttributes.MemberAccessMask),
                "final" => new(MethodAttributes.Final),
                "specialname" => new(MethodAttributes.SpecialName),
                "virtual" => new(MethodAttributes.Virtual),
                "strict" => new(MethodAttributes.CheckAccessOnOverride),
                "abstract" => new(MethodAttributes.Abstract),
                "assembly" => new(MethodAttributes.Assembly, MethodAttributes.MemberAccessMask),
                "famandassem" => new(MethodAttributes.FamANDAssem, MethodAttributes.MemberAccessMask),
                "famorassem" => new(MethodAttributes.FamORAssem, MethodAttributes.MemberAccessMask),
                "privatescope" => new(MethodAttributes.PrivateScope, MethodAttributes.MemberAccessMask),
                "hidebysig" => new(MethodAttributes.HideBySig),
                "newslot" => new(MethodAttributes.NewSlot),
                "rtspecialname" => new(MethodAttributes.RTSpecialName),
                "unmanagedexp" => new(MethodAttributes.UnmanagedExport),
                "reqsecobj" => new(MethodAttributes.RequireSecObject),
                _ => throw new UnreachableException(),
            };
        }


        public GrammarResult VisitMethodDecls(CILParser.MethodDeclsContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitMethodHead(CILParser.MethodHeadContext context) => VisitMethodHead(context);
        public GrammarResult.Literal<EntityRegistry.MethodDefinitionEntity> VisitMethodHead(CILParser.MethodHeadContext context)
        {
            string name = VisitMethodName(context.methodName()).Value;
            var containingType = _currentTypeDefinition.PeekOrDefault() ?? _entityRegistry.ModuleType;
            var methodDefinition = EntityRegistry.CreateUnrecordedMethodDefinition(containingType, name);

            BlobBuilder methodSignature = new();
            byte sigHeader = VisitCallConv(context.callConv()).Value;

            // Two-pass generic parameter processing for method params:
            // Pass 1: Register all parameter names (without resolving constraints)
            _currentMethod = new(methodDefinition);
            var typarContexts = context.typarsClause()?.typars()?.typar() ?? Array.Empty<CILParser.TyparContext>();
            for (int i = 0; i < typarContexts.Length; i++)
            {
                var attributes = VisitTyparAttribs(typarContexts[i].typarAttribs()).Value;
                var param = EntityRegistry.CreateGenericParameter(attributes, VisitDottedName(typarContexts[i].dottedName()).Value);
                param.Owner = methodDefinition;
                param.Index = i;
                methodDefinition.GenericParameters.Add(param);
            }
            if (typarContexts.Length != 0)
            {
                sigHeader |= (byte)SignatureAttributes.Generic;
            }
            methodDefinition.MethodAttributes = context.methAttr().Aggregate((MethodAttributes)0, (acc, attr) => acc | VisitMethAttr(attr));

            // COMPAT: Native ilasm implicitly adds RTSpecialName + SpecialName for .ctor/.cctor methods
            if (name is ".ctor" or ".cctor")
            {
                methodDefinition.MethodAttributes |= MethodAttributes.RTSpecialName | MethodAttributes.SpecialName;
            }
            // COMPAT: Native ilasm implicitly adds SpecialName when RTSpecialName is set
            else if (methodDefinition.MethodAttributes.HasFlag(MethodAttributes.RTSpecialName))
            {
                methodDefinition.MethodAttributes |= MethodAttributes.SpecialName;
            }

            if (methodDefinition.MethodAttributes.HasFlag(MethodAttributes.Abstract) && !methodDefinition.ContainingType.Attributes.HasFlag(TypeAttributes.Abstract))
            {
                ReportWarning(DiagnosticIds.AbstractMethodNotInAbstractType,
                    string.Format(DiagnosticMessageTemplates.AbstractMethodNotInAbstractType, methodDefinition.Name),
                    context);
            }

            (EntityRegistry.ModuleReferenceEntity Module, string? EntryPoint, MethodImportAttributes Attributes)? pInvokeInformation = null;
            foreach (var pInvokeInfo in context.pinvImpl())
            {
                var (moduleName, entryPoint, attributes) = VisitPinvImpl(pInvokeInfo).Value;
                if (moduleName is null)
                {
                    ReportError(DiagnosticIds.InvalidPInvokeSignature,
                        DiagnosticMessageTemplates.InvalidPInvokeSignature,
                        pInvokeInfo);
                    continue;
                }
                pInvokeInformation = (_entityRegistry.GetOrCreateModuleReference(moduleName, _ => { }), entryPoint ?? name, attributes);
            }
            methodDefinition.MethodImportInformation = pInvokeInformation;

            SignatureHeader parsedHeader = new(sigHeader);
            if (methodDefinition.MethodAttributes.HasFlag(MethodAttributes.Static) && (parsedHeader.IsInstance || parsedHeader.HasExplicitThis))
            {
                // Error on static + instance.
            }
            // COMPAT: Native ilasm auto-adds instance calling convention for non-static methods in class context
            if (!methodDefinition.MethodAttributes.HasFlag(MethodAttributes.Static)
                && !parsedHeader.IsInstance
                && _currentTypeDefinition.Count > 0)
            {
                sigHeader |= (byte)SignatureAttributes.Instance;
                parsedHeader = new(sigHeader);
            }
            if (parsedHeader.HasExplicitThis && !parsedHeader.IsInstance)
            {
                // Warn on explicit-this + non-instance
                parsedHeader = new(sigHeader |= (byte)SignatureAttributes.Instance);
            }
            methodSignature.WriteByte(sigHeader);
            if (typarContexts.Length != 0)
            {
                methodSignature.WriteCompressedInteger(typarContexts.Length);
            }
            // Pass 2: Resolve constraints (now all params are registered)
            for (int i = 0; i < typarContexts.Length; i++)
            {
                var param = methodDefinition.GenericParameters[i];
                foreach (var constraint in VisitTyBound(typarContexts[i].tyBound()).Value)
                {
                    constraint.Owner = param;
                    param.Constraints.Add(constraint);
                    methodDefinition.GenericParameterConstraints.Add(constraint);
                }
            }

            var args = VisitSigArgs(context.sigArgs()).Value;
            methodSignature.WriteCompressedInteger(args.Length);

            SignatureArg returnValue = new(VisitParamAttr(context.paramAttr()).Value, VisitType(context.type()).Value, VisitMarshalClause(context.marshalClause()).Value, null);

            returnValue.SignatureBlob.WriteContentTo(methodSignature);
            methodDefinition.Parameters.Add(EntityRegistry.CreateParameter(returnValue.Attributes, returnValue.Name, returnValue.MarshallingDescriptor, 0));
            for (int i = 0; i < args.Length; i++)
            {
                SignatureArg? arg = args[i];
                arg.SignatureBlob.WriteContentTo(methodSignature);
                // COMPAT: Native ilasm auto-generates A_N names for unnamed parameters
                string? paramName = arg.Name ?? $"A_{i}";
                methodDefinition.Parameters.Add(EntityRegistry.CreateParameter(arg.Attributes, paramName, arg.MarshallingDescriptor, i + 1));
            }
            // We've parsed all signature information. We can reset the current method now (the caller will handle setting/unsetting it for the method body).
            _currentMethod = null;
            methodDefinition.SignatureHeader = parsedHeader;
            methodDefinition.MethodSignature = methodSignature;

            methodDefinition.ImplementationAttributes = context.implAttr().Aggregate((MethodImplAttributes)0, (acc, attr) => acc | VisitImplAttr(attr));
            if (!EntityRegistry.TryAddMethodDefinitionToContainingType(methodDefinition))
            {
                ReportError(DiagnosticIds.DuplicateMethod,
                    DiagnosticMessageTemplates.DuplicateMethod,
                    context);
            }

            return new(methodDefinition);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitMethodName(CILParser.MethodNameContext context) => VisitMethodName(context);
        public GrammarResult.String VisitMethodName(CILParser.MethodNameContext context)
        {
            // Error recovery in a malformed method header can leave the name unmatched.
            if (context.ChildCount == 0)
            {
                return new(string.Empty);
            }

            IParseTree child = context.GetChild(0);
            return child switch
            {
                ITerminalNode terminal => new(terminal.Symbol.Text),
                CILParser.DottedNameContext dottedName => VisitDottedName(dottedName),
                _ => new(context.GetText()),
            };
        }

        public GrammarResult VisitScopeBlock(CILParser.ScopeBlockContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        public GrammarResult VisitSehBlock(CILParser.SehBlockContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitSehClause(CILParser.SehClauseContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitSehClauses(CILParser.SehClausesContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitTryBlock(CILParser.TryBlockContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

#pragma warning restore CA1822 // Mark members as static
}
