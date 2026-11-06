// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using static SuperPmi.DumpStrings;

namespace SuperPmi;

// Dumps each kind of recorded data, as mcs -dump prints it.
public sealed partial class CompileResult
{
    private static TextWriter Out => Logger.Out;

    private static uint U32(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset));

    public void dmpAssertLog(uint key, uint value)
    {
        CFormat.Printf(Out, "AssertLog key %u, value '%s'", key, AssertLog.GetString(value));
        AssertLog.Unlock();
    }

    public void dmpCallLog(uint key, uint value)
    {
        CFormat.Printf(Out, "CallLog %u '%s'", key, CallLog.GetString(value));
        CallLog.Unlock();
    }

    public void dmpSetBoundaries(uint key, in Agnostic_SetBoundaries value)
    {
        const int OffsetMappingSize = 12; // ICorDebugInfo::OffsetMapping: nativeOffset, ilOffset, source
        ReadOnlySpan<byte> om = SetBoundaries.GetBuffer(value.pMap_offset);
        CFormat.Printf(Out, "SetBoundaries key 0, value ftn-%016llX cMap-%u %u{", value.ftn, value.cMap, value.pMap_offset);
        for (uint i = 0; i < value.cMap; i++)
        {
            if (i != 0)
                Out.Write(", ");
            int at = (int)i * OffsetMappingSize;
            CFormat.Printf(Out, "%u %u %u", U32(om, at + 4), U32(om, at), U32(om, at + 8));
        }
        Out.Write("}");
        SetBoundaries.Unlock();
    }

    public void dmpSetVars(uint key, in Agnostic_SetVars value)
    {
        // ICorDebugInfo::NativeVarInfo: startOffset, endOffset, callReturnValueILOffset, varNumber, then a 16-byte VarLoc.
        const int NativeVarInfoSize = 32;
        ReadOnlySpan<byte> om = SetVars.GetBuffer(value.vars_offset);
        CFormat.Printf(Out, "SetVars key %u, value ftn-%016llX cVars-%u %u{", key, value.ftn, value.cVars, value.vars_offset);
        for (uint i = 0; i < value.cVars; i++)
        {
            if (i != 0)
                Out.Write(", ");
            int at = (int)i * NativeVarInfoSize;
            CFormat.Printf(Out, "so-%u eo-%u var-%u", U32(om, at), U32(om, at + 4), U32(om, at + 12));
        }
        Out.Write("}");
        SetVars.Unlock();
    }

    public void dmpSetPatchpointInfo(uint key, in Agnostic_SetPatchpointInfo value)
    {
        SetPatchpointInfo.GetBuffer(value.index);
        CFormat.Printf(Out, "SetPatchpointInfo key %u, index %u{", key, value.index);
        Out.Write("}");
        SetPatchpointInfo.Unlock();
    }

    // The buffer's offset is printed in place of its address, which changes from run to run.
    public void dmpAllocGCInfo(uint key, in Agnostic_AllocGCInfo value)
    {
        ReadOnlySpan<byte> buff = AllocGCInfo.GetBuffer(value.retval_offset);
        Out.Write("AllocGCInfo key 0, ");
        CFormat.Printf(Out, "sz-%llu %s{ ", value.size, value.retval_offset == uint.MaxValue ? "(nil)" : CFormat.Sprintf("0x%x", value.retval_offset));
        for (ulong i = 0; i < value.size; i++)
            CFormat.Printf(Out, "%02X ", buff[(int)i]);
        Out.Write("}");
        AllocGCInfo.Unlock();
    }

    public void dmpReportInliningDecision(uint key, in Agnostic_ReportInliningDecision value)
    {
        CFormat.Printf(Out, "ReportInliningDecision key %u, value inliner-%016llX inlinee-%016llX res-%u reason-'%s'", key,
            value.inlinerHnd, value.inlineeHnd, value.inlineResult, ReportInliningDecision.GetString(value.reason_offset));
        ReportInliningDecision.Unlock();
    }

    public void dmpReportTailCallDecision(uint key, in Agnostic_ReportTailCallDecision value)
    {
        CFormat.Printf(Out, "ReportTailCallDecision key-%u, value cr-%016llX ce-%016llX tail-%u call-%u -%s", key, value.callerHnd,
            value.calleeHnd, value.tailCallResult, value.tailCallResult, ReportTailCallDecision.GetString(value.reason_index));
        ReportTailCallDecision.Unlock();
    }

    public void dmpProcessName(uint key, uint value)
    {
        CFormat.Printf(Out, "ProcessName key %u, value '%s'", key, ProcessName.GetString(value));
        ProcessName.Unlock();
    }

    public void dmpAllocMem(uint key, in Agnostic_AllocMemDetails value)
        => CFormat.Printf(Out, "AllocMem key 0, value hotCodeSize-%u coldCodeSize-%u roDataSize-%u xcptnsCount-%u hotCodeBlock_offset-%u coldCodeBlock_offset-%u roDataBlock_offset-%u hotCodeBlock-%016llX coldCodeBlock-%016llX roDataBlock-%016llX",
            value.hotCodeSize, value.coldCodeSize, value.roDataSize, value.xcptnsCount,
            value.hotCodeBlock_offset, value.coldCodeBlock_offset, value.roDataBlock_offset, value.hotCodeBlock,
            value.coldCodeBlock, value.roDataBlock);

    public void dmpCompileMethod(uint key, in Agnostic_CompileMethodResults value)
        => CFormat.Printf(Out, "CompileMethod key %u, value nativeEntry-%016llX nativeSizeOfCode-%u CorJitResult-%u", key,
            value.nativeEntry, value.nativeSizeOfCode, value.CorJitResult);

    public void dmpMessageLog(uint key, uint value)
        => CFormat.Printf(Out, "MessageLog NYI");

    public void dmpSetEHcount(uint key, uint value)
        => CFormat.Printf(Out, "SetEHcount key %u, value %u", key, value);

    public void dmpSetEHinfo(uint key, in Agnostic_CORINFO_EH_CLAUSE value)
    {
        CFormat.Printf(Out, "SetEHinfo key %u, value flg-%u to-%u tl-%u ho-%u hl-%u", key, value.Flags, value.TryOffset, value.TryLength,
               value.HandlerOffset, value.HandlerLength);
        if (value.Flags == CORINFO_EH_CLAUSE_FILTER)
        {
            CFormat.Printf(Out, " fo-%u", value.ClassToken); // FilterOffset
        }
        else if (value.Flags == CORINFO_EH_CLAUSE_NONE)
        {
            CFormat.Printf(Out, " cls-%08X", value.ClassToken);
        }
        // else, no need to print for finally/fault handlers
    }

    public void dmpSetMethodAttribs(ulong key, uint value)
    {
        CFormat.Printf(Out, "SetMethodAttribs key ftn-%016llX, value attr-%08X", key, value);
    }

    public void dmpReportFatalError(uint key, uint value)
        => CFormat.Printf(Out, "ReportFatalError key Count-%u, value result-%08X", key, value);

    public void dmpRecordRelocation(uint key, in Agnostic_RecordRelocation value)
        => CFormat.Printf(Out, "RecordRelocation key %u, value loc-%016llX tgt-%016llX fRelocType-%u(%s) addlDelta:%d", key,
            value.location, value.target, value.fRelocType, relocationTypeToString(value.fRelocType),
            (int)value.addlDelta);

    public void dmpReserveUnwindInfo(uint key, in Agnostic_ReserveUnwindInfo value)
        => CFormat.Printf(Out, "ReserveUnwindInfo key %u, value isFun-%u isCold-%u usz-%u", key, value.isFunclet, value.isColdCode,
            value.unwindSize);

    public void dmpAllocUnwindInfo(uint key, in Agnostic_AllocUnwindInfo value)
        => CFormat.Printf(Out, "AllocUnwindInfo key %u, value pHot-%016llX pCold-%016llX startOff-%u endOff-%u unwindSz-%u blki-%u funcKind-%u",
            key, value.pHotCode, value.pColdCode, value.startOffset, value.endOffset, value.unwindSize,
            value.pUnwindBlock_index, value.funcKind);

    public void dmpRecordCallSiteWithSignature(uint key, in Agnostic_RecordCallSite value)
        => CFormat.Printf(Out, "RecordCallSite key %u, callSig-%s ftn-%016llX",
            key,
            SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(value.callSig, RecordCallSiteWithSignature, CrSigInstHandleMap),
            value.methodHandle);

    public void dmpRecordCallSiteWithoutSignature(uint key, ulong methodHandle)
        => CFormat.Printf(Out, "RecordCallSite without call signature key %u, ftn-%016llX", key, methodHandle);

    public void dmpCrSigInstHandleMap(uint key, ulong value)
        => CFormat.Printf(Out, "CrSigInstHandleMap key %u, value %016llX", key, value);
}
