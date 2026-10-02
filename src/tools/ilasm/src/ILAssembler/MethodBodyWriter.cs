// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace ILAssembler;

internal sealed class MethodBodyWriter
{
    private readonly List<int?> _labels = new();
    private readonly List<BranchFixup> _branches = new();
    private readonly List<Diagnostic> _diagnostics = new();

    internal readonly record struct Label(int Id);

    private readonly record struct BranchFixup(Blob Operand, Label Target, int EndOffset, ILOpCode OpCode, Location Location);

    private readonly record struct ResolvedExceptionRegion(ExceptionRegionKind Kind, int TryOffset, int TryLength,
        int HandlerOffset, int HandlerLength, int CatchTokenOrOffset);

    public BlobBuilder CodeBuilder { get; } = new();

    public int Offset => CodeBuilder.Count;

    public Label DefineLabel()
    {
        _labels.Add(null);
        return new Label(_labels.Count);
    }

    public void MarkLabel(Label label) => MarkLabel(label, Offset);

    public void MarkLabel(Label label, int offset) => _labels[label.Id - 1] = offset;

    private int GetLabelOffset(Label label)
    {
        int? offset = _labels[label.Id - 1];
        Debug.Assert(offset.HasValue);
        return offset.Value;
    }

    public void OpCode(ILOpCode code)
    {
        if ((ushort)code <= byte.MaxValue)
        {
            CodeBuilder.WriteByte((byte)code);
        }
        else
        {
            CodeBuilder.WriteUInt16BE((ushort)code);
        }
    }

    public void Token(EntityHandle handle) => CodeBuilder.WriteInt32(MetadataTokens.GetToken(handle));

    public void LoadString(UserStringHandle handle)
    {
        OpCode(ILOpCode.Ldstr);
        CodeBuilder.WriteInt32(MetadataTokens.GetToken(handle));
    }

    public void LoadConstantI4(ILOpCode code, int value, bool optimize)
    {
        if (optimize)
        {
            if (value is >= -1 and <= 8)
            {
                OpCode((ILOpCode)((int)ILOpCode.Ldc_i4_m1 + value + 1));
                return;
            }

            if (value is >= sbyte.MinValue and <= sbyte.MaxValue)
            {
                code = ILOpCode.Ldc_i4_s;
            }
        }

        OpCode(code);
        if (code == ILOpCode.Ldc_i4_s)
        {
            CodeBuilder.WriteByte(unchecked((byte)value));
        }
        else
        {
            CodeBuilder.WriteInt32(value);
        }
    }

    public void LoadConstantI8(long value)
    {
        OpCode(ILOpCode.Ldc_i8);
        CodeBuilder.WriteInt64(value);
    }

    public void LoadConstantR4(float value)
    {
        OpCode(ILOpCode.Ldc_r4);
        CodeBuilder.WriteSingle(value);
    }

    public void LoadConstantR8(double value)
    {
        OpCode(ILOpCode.Ldc_r8);
        CodeBuilder.WriteDouble(value);
    }

    public void Variable(ILOpCode code, int index, bool optimize)
    {
        if (optimize)
        {
            if ((uint)index < 4)
            {
                ILOpCode macro = code switch
                {
                    ILOpCode.Ldarg or ILOpCode.Ldarg_s => (ILOpCode)((int)ILOpCode.Ldarg_0 + index),
                    ILOpCode.Ldloc or ILOpCode.Ldloc_s => (ILOpCode)((int)ILOpCode.Ldloc_0 + index),
                    ILOpCode.Stloc or ILOpCode.Stloc_s => (ILOpCode)((int)ILOpCode.Stloc_0 + index),
                    _ => code,
                };
                if (macro != code)
                {
                    OpCode(macro);
                    return;
                }
            }

            if ((uint)index <= byte.MaxValue)
            {
                code = code switch
                {
                    ILOpCode.Ldarg => ILOpCode.Ldarg_s,
                    ILOpCode.Ldarga => ILOpCode.Ldarga_s,
                    ILOpCode.Starg => ILOpCode.Starg_s,
                    ILOpCode.Ldloc => ILOpCode.Ldloc_s,
                    ILOpCode.Ldloca => ILOpCode.Ldloca_s,
                    ILOpCode.Stloc => ILOpCode.Stloc_s,
                    _ => code,
                };
            }
        }

        OpCode(code);
        if (code is ILOpCode.Ldarg_s or ILOpCode.Ldarga_s or ILOpCode.Starg_s
            or ILOpCode.Ldloc_s or ILOpCode.Ldloca_s or ILOpCode.Stloc_s)
        {
            CodeBuilder.WriteByte(unchecked((byte)index));
        }
        else
        {
            CodeBuilder.WriteUInt16(unchecked((ushort)index));
        }
    }

    public void Branch(ILOpCode code, Label target, bool optimize, Location location)
    {
        if (optimize && _labels[target.Id - 1] is int targetOffset)
        {
            long distance = (long)targetOffset - Offset;
            // Native ilasm uses the worst-case long-instruction size when shortening a known target.
            if (distance - 5 >= sbyte.MinValue && distance - 2 <= sbyte.MaxValue)
            {
                code = code.GetShortBranch();
            }
        }

        OpCode(code);
        Blob operand = CodeBuilder.ReserveBytes(code.GetBranchOperandSize());
        _branches.Add(new BranchFixup(operand, target, Offset, code, location));
    }

    public void Branch(ILOpCode code, int distance, bool optimize, Location location)
    {
        if (optimize && distance is >= sbyte.MinValue and <= sbyte.MaxValue)
        {
            code = code.GetShortBranch();
        }

        OpCode(code);
        var writer = new BlobWriter(CodeBuilder.ReserveBytes(code.GetBranchOperandSize()));
        WriteBranchOperand(ref writer, code, distance, location);
    }

    public void Switch(IReadOnlyList<(Label Label, int? Offset)> targets, Location location)
    {
        int endOffset = checked(Offset + 1 + sizeof(int) + targets.Count * sizeof(int));
        OpCode(ILOpCode.Switch);
        CodeBuilder.WriteInt32(targets.Count);
        foreach (var target in targets)
        {
            if (target.Offset is int distance)
            {
                CodeBuilder.WriteInt32(distance);
            }
            else
            {
                _branches.Add(new BranchFixup(CodeBuilder.ReserveBytes(sizeof(int)), target.Label,
                    endOffset, ILOpCode.Switch, location));
            }
        }
    }

    private void WriteBranchOperand(ref BlobWriter writer, ILOpCode code, int distance, Location location)
    {
        if (writer.Length == 1)
        {
            if (distance is < sbyte.MinValue or > sbyte.MaxValue)
            {
                _diagnostics.Add(new Diagnostic(DiagnosticIds.BranchOffsetOutOfRange, DiagnosticSeverity.Error,
                    string.Format(DiagnosticMessageTemplates.BranchOffsetOutOfRange, code, distance), location));
            }

            writer.WriteByte(unchecked((byte)distance));
        }
        else
        {
            writer.WriteInt32(distance);
        }
    }

    public IReadOnlyList<Diagnostic> Complete(IReadOnlyList<EntityRegistry.ExceptionRegion> exceptionRegions)
    {
        foreach (var branch in _branches)
        {
            int distance = checked(GetLabelOffset(branch.Target) - branch.EndOffset);
            var writer = new BlobWriter(branch.Operand);
            WriteBranchOperand(ref writer, branch.OpCode, distance, branch.Location);
        }

        foreach (var region in exceptionRegions)
        {
            int tryStart = GetLabelOffset(region.TryStart);
            int tryEnd = GetLabelOffset(region.TryEnd);
            int handlerStart = GetLabelOffset(region.HandlerStart);
            int handlerEnd = GetLabelOffset(region.HandlerEnd);
            bool invalid = tryStart < 0 || tryEnd < tryStart || tryEnd > Offset
                || handlerStart < 0 || handlerEnd < handlerStart || handlerEnd > Offset;
            if (region is EntityRegistry.ExceptionRegion.FilterRegion filter)
            {
                int filterStart = GetLabelOffset(filter.FilterStart);
                if (filterStart < 0 || filterStart > Offset)
                {
                    _diagnostics.Add(new Diagnostic(DiagnosticIds.InvalidExceptionRegion, DiagnosticSeverity.Error,
                        string.Format(DiagnosticMessageTemplates.InvalidFilterOffset, filterStart, Offset), region.Location));
                }
            }

            if (invalid)
            {
                _diagnostics.Add(new Diagnostic(DiagnosticIds.InvalidExceptionRegion, DiagnosticSeverity.Error,
                    string.Format(DiagnosticMessageTemplates.InvalidExceptionRegion,
                        tryStart, tryEnd, handlerStart, handlerEnd, Offset), region.Location));
            }
        }

        return _diagnostics;
    }

    public int WriteTo(MethodBodyStreamEncoder bodyStream, int maxStack, StandaloneSignatureHandle localsSignature,
        MethodBodyAttributes attributes, IReadOnlyList<EntityRegistry.ExceptionRegion> exceptionRegions, bool hasDynamicStackAllocation)
    {
        var regions = new List<ResolvedExceptionRegion>(exceptionRegions.Count);
        bool small = ExceptionRegionEncoder.IsSmallRegionCount(exceptionRegions.Count);
        foreach (var region in exceptionRegions)
        {
            int tryStart = GetLabelOffset(region.TryStart);
            int tryLength = unchecked(GetLabelOffset(region.TryEnd) - tryStart);
            int handlerStart = GetLabelOffset(region.HandlerStart);
            int handlerLength = unchecked(GetLabelOffset(region.HandlerEnd) - handlerStart);
            var (kind, tokenOrOffset) = region switch
            {
                EntityRegistry.ExceptionRegion.CatchRegion clause => (ExceptionRegionKind.Catch, MetadataTokens.GetToken(clause.CatchType.Handle)),
                EntityRegistry.ExceptionRegion.FilterRegion clause => (ExceptionRegionKind.Filter, GetLabelOffset(clause.FilterStart)),
                EntityRegistry.ExceptionRegion.FinallyRegion => (ExceptionRegionKind.Finally, 0),
                EntityRegistry.ExceptionRegion.FaultRegion => (ExceptionRegionKind.Fault, 0),
                _ => throw new UnreachableException(),
            };
            small &= ExceptionRegionEncoder.IsSmallExceptionRegion(tryStart, tryLength)
                && ExceptionRegionEncoder.IsSmallExceptionRegion(handlerStart, handlerLength);
            regions.Add(new ResolvedExceptionRegion(kind, tryStart, tryLength, handlerStart, handlerLength, tokenOrOffset));
        }

        MethodBodyStreamEncoder.MethodBody body = bodyStream.AddMethodBody(
            CodeBuilder.Count, maxStack, regions.Count, small, localsSignature, attributes, hasDynamicStackAllocation);
        var instructions = new BlobWriter(body.Instructions);
        CodeBuilder.WriteContentTo(ref instructions);

        // ECMA-335 II.25.4.5: write clause fields directly so /ERROR can preserve malformed offsets and lengths.
        BlobBuilder eh = body.ExceptionRegions.Builder;
        foreach (var region in regions)
        {
            if (small)
            {
                eh.WriteUInt16((ushort)region.Kind);
                eh.WriteUInt16((ushort)region.TryOffset);
                eh.WriteByte((byte)region.TryLength);
                eh.WriteUInt16((ushort)region.HandlerOffset);
                eh.WriteByte((byte)region.HandlerLength);
            }
            else
            {
                eh.WriteInt32((int)region.Kind);
                eh.WriteInt32(region.TryOffset);
                eh.WriteInt32(region.TryLength);
                eh.WriteInt32(region.HandlerOffset);
                eh.WriteInt32(region.HandlerLength);
            }

            eh.WriteInt32(region.CatchTokenOrOffset);
        }

        return body.Offset;
    }
}
