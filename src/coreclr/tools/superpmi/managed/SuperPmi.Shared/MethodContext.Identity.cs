// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using Internal.JitInterface;
using Internal.Pgo;

using static SuperPmi.SpmiRecordsHelper;

namespace SuperPmi;

public sealed unsafe partial class MethodContext
{
    private const int METHOD_IDENTITY_INFO_SIZE = 0x10000;
    // Reserved for everything in the identity except the method name.
    private const int METHOD_IDENTITY_INFO_NON_NAME_RESERVE = 0x400;

    // CORINFO_InstructionSetFlags::FlagsFieldCount in inc/corinfoinstructionset.h.
    public const int InstructionFlagsFieldCount = 2;

    public uint methodSize;

    public CORINFO_METHOD_INFO repCompileMethod(out uint flags, out CORINFO_OS os)
    {
        CORINFO_METHOD_INFO info;
        uint f;
        CORINFO_OS o;
        repCompileMethod(&info, &f, &o);
        flags = f;
        os = o;
        return info;
    }

    // The JIT flags word, followed by the instruction set flags.
    public (ulong JitFlags, ulong[] InstructionSetFlags) repGetJitFlags()
    {
        ulong* raw = stackalloc ulong[1 + InstructionFlagsFieldCount];
        new Span<ulong>(raw, 1 + InstructionFlagsFieldCount).Clear();
        repGetJitFlags(ref *((CORJIT_FLAGS*)raw), (uint)(sizeof(ulong) * (1 + InstructionFlagsFieldCount)));
        return (raw[0], new ReadOnlySpan<ulong>(raw + 1, InstructionFlagsFieldCount).ToArray());
    }

    // The text that identifies a method: its signature, calling convention, options, JIT and ISA flags,
    // and hashes of its IL and root method PGO counters.
    public byte[] dumpMethodIdentityInfoToBuffer(bool ignoreMethodName = false, CORINFO_METHOD_INFO* optInfo = null, uint optFlags = 0)
    {
        CORINFO_METHOD_INFO info = optInfo != null ? *optInfo : repCompileMethod(out _, out _);
        (ulong jitFlags, ulong[] isaFlags) = repGetJitFlags();

        var buff = new ByteBuilder();

        // Huge method signatures are truncated.
        buff.Append(CallUtils.GetMethodFullName(this, info.ftn, info.args, ignoreMethodName));
        buff.Append(" -- "u8);
        buff.Truncate(METHOD_IDENTITY_INFO_SIZE - METHOD_IDENTITY_INFO_NON_NAME_RESERVE - 1);

        buff.Append($"CallingConvention: {(int)info.args.callConv}, CorInfoOptions: {(int)info.options}, CorInfoRegionKind: {(int)info.regionKind}, JitFlags {jitFlags:x16}, ISA Flags");
        for (int i = InstructionFlagsFieldCount - 1; i >= 0; i--)
            buff.Append($" {isaFlags[i]:X16}");

        buff.Append($" ILCode Hash: {Hash.HashBuffer(new ReadOnlySpan<byte>(info.ILCode, (int)info.ILCodeSize))}");

        if (GetPgoInstrumentationResults != null && GetPgoInstrumentationResults.GetIndex(CastHandle(info.ftn)) != -1)
            AppendPgoCountersHash(buff, info.ftn);

        return buff.ToArray();
    }

    private void AppendPgoCountersHash(ByteBuilder buff, CORINFO_METHOD_STRUCT_* ftn)
    {
        PgoInstrumentationSchema* schema = null;
        uint schemaCount = 0;
        byte* schemaData;
        PgoSource pgoSource = default;
        bool dynamicPgo = false;
        if (repGetPgoInstrumentationResults(ftn, ref schema, ref schemaCount, &schemaData, ref pgoSource, ref dynamicPgo) < 0)
            return;

        ulong minOffset = ulong.MaxValue;
        ulong maxOffset = 0;
        ulong totalCount = 0;
        for (uint i = 0; i < schemaCount; i++)
        {
            ulong start = (ulong)schema[i].Offset;
            ulong end;
            switch (schema[i].InstrumentationKind)
            {
                case PgoInstrumentationKind.BasicBlockIntCount:
                case PgoInstrumentationKind.EdgeIntCount:
                    totalCount += BinaryPrimitives.ReadUInt32LittleEndian(new ReadOnlySpan<byte>(schemaData + start, sizeof(uint)));
                    end = start + 4;
                    break;
                case PgoInstrumentationKind.BasicBlockLongCount:
                case PgoInstrumentationKind.EdgeLongCount:
                    totalCount += BinaryPrimitives.ReadUInt64LittleEndian(new ReadOnlySpan<byte>(schemaData + start, sizeof(ulong)));
                    end = start + 8;
                    break;
                default:
                    continue;
            }

            minOffset = Math.Min(minOffset, start);
            maxOffset = Math.Max(maxOffset, end);
        }

        if (minOffset < maxOffset)
        {
            string pgoHash = Hash.HashBuffer(new ReadOnlySpan<byte>(schemaData + minOffset, (int)(maxOffset - minOffset)));
            buff.Append($" Pgo Counters {schemaCount}, Count {totalCount}, Hash: {pgoHash}");
        }
    }

    public string dumpMethodHashToBuffer(bool ignoreMethodName = false, CORINFO_METHOD_INFO* optInfo = null, uint optFlags = 0)
        => Hash.HashBuffer(dumpMethodIdentityInfoToBuffer(ignoreMethodName, optInfo, optFlags));

    public string dumpMethodHashToBuffer() => dumpMethodHashToBuffer(false, null, 0);

    // Dumps every map of the method context and its compile result. With 'simple', the header skips the
    // method name, which is useful when debugging the name lookups themselves.
    public void dumpToConsole(int mcNumber, bool simple)
    {
        Logger.Out.Write("*****************************************");
        if (mcNumber != -1)
            Logger.Out.Write($" method context #{mcNumber}");
        if (!simple)
            Logger.Out.Write(" " + System.Text.Encoding.UTF8.GetString(dumpMethodIdentityInfoToBuffer()));
        Logger.Out.Write("\n");

        dumpMaps();
        cr.dumpMaps();
    }

    // Permissive equality for the legacy duplicate removal: may treat too many method contexts as equal.
    public bool Equal(MethodContext other)
    {
        CORINFO_METHOD_INFO otherInfo = other.repCompileMethod(out uint otherFlags, out CORINFO_OS otherOs);
        CORINFO_METHOD_INFO ourInfo = repCompileMethod(out uint ourFlags, out CORINFO_OS ourOs);

        if (otherInfo.ILCodeSize != ourInfo.ILCodeSize ||
            otherInfo.args.numArgs != ourInfo.args.numArgs ||
            otherInfo.args.retType != ourInfo.args.retType ||
            otherInfo.locals.numArgs != ourInfo.locals.numArgs ||
            otherInfo.EHcount != ourInfo.EHcount ||
            otherInfo.options != ourInfo.options ||
            !new ReadOnlySpan<byte>(otherInfo.ILCode, (int)otherInfo.ILCodeSize).SequenceEqual(new ReadOnlySpan<byte>(ourInfo.ILCode, (int)ourInfo.ILCodeSize)) ||
            otherInfo.maxStack != ourInfo.maxStack ||
            otherInfo.regionKind != ourInfo.regionKind ||
            otherInfo.args.callConv != ourInfo.args.callConv ||
            otherInfo.args.cbSig != ourInfo.args.cbSig ||
            otherInfo.args.flags != ourInfo.args.flags ||
            otherInfo.locals.cbSig != ourInfo.locals.cbSig ||
            otherFlags != ourFlags ||
            otherOs != ourOs)
        {
            return false;
        }

        return SoftMapsEqual(_maps, other._maps) && SoftMapsEqual(cr.Maps, other.cr.Maps);
    }

    private static bool SoftMapsEqual(LightWeightMapBuffer[] ours, LightWeightMapBuffer[] theirs)
    {
        for (int i = 0; i < ours.Length; i++)
        {
            if (ours[i] == null ? theirs[i] != null : theirs[i] == null || ours[i].GetCount() != theirs[i].GetCount())
                return false;
        }
        return true;
    }
}
