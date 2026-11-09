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
        Out.Write($"AssertLog key {key}, value '{AssertLog.GetString(value) ?? "(null)"}'");
        AssertLog.Unlock();
    }

    public void dmpCallLog(uint key, uint value)
    {
        Out.Write($"CallLog {key} '{CallLog.GetString(value) ?? "(null)"}'");
        CallLog.Unlock();
    }

    public void dmpSetBoundaries(uint key, in Agnostic_SetBoundaries value)
    {
        const int OffsetMappingSize = 12; // ICorDebugInfo::OffsetMapping: nativeOffset, ilOffset, source
        ReadOnlySpan<byte> om = SetBoundaries.GetBuffer(value.pMap_offset);
        Out.Write($"SetBoundaries key 0, value ftn-{value.ftn:X16} cMap-{value.cMap} {value.pMap_offset}{{");
        for (uint i = 0; i < value.cMap; i++)
        {
            if (i != 0)
                Out.Write(", ");
            int at = (int)i * OffsetMappingSize;
            Out.Write($"{U32(om, at + 4)} {U32(om, at)} {U32(om, at + 8)}");
        }
        Out.Write("}");
        SetBoundaries.Unlock();
    }

    public void dmpSetVars(uint key, in Agnostic_SetVars value)
    {
        // ICorDebugInfo::NativeVarInfo: startOffset, endOffset, callReturnValueILOffset, varNumber, then a 16-byte VarLoc.
        const int NativeVarInfoSize = 32;
        ReadOnlySpan<byte> om = SetVars.GetBuffer(value.vars_offset);
        Out.Write($"SetVars key {key}, value ftn-{value.ftn:X16} cVars-{value.cVars} {value.vars_offset}{{");
        for (uint i = 0; i < value.cVars; i++)
        {
            if (i != 0)
                Out.Write(", ");
            int at = (int)i * NativeVarInfoSize;
            Out.Write($"so-{U32(om, at)} eo-{U32(om, at + 4)} var-{U32(om, at + 12)}");
        }
        Out.Write("}");
        SetVars.Unlock();
    }

    public void dmpSetPatchpointInfo(uint key, in Agnostic_SetPatchpointInfo value)
    {
        SetPatchpointInfo.GetBuffer(value.index);
        Out.Write($"SetPatchpointInfo key {key}, index {value.index}{{");
        Out.Write("}");
        SetPatchpointInfo.Unlock();
    }

    // The buffer's offset is printed in place of its address, which changes from run to run.
    public void dmpAllocGCInfo(uint key, in Agnostic_AllocGCInfo value)
    {
        ReadOnlySpan<byte> buff = AllocGCInfo.GetBuffer(value.retval_offset);
        Out.Write("AllocGCInfo key 0, ");
        Out.Write($"sz-{value.size} {(value.retval_offset == uint.MaxValue ? "(nil)" : $"0x{value.retval_offset:x}")}{{ ");
        for (ulong i = 0; i < value.size; i++)
            Out.Write($"{buff[(int)i]:X2} ");
        Out.Write("}");
        AllocGCInfo.Unlock();
    }

    public void dmpReportInliningDecision(uint key, in Agnostic_ReportInliningDecision value)
    {
        Out.Write($"ReportInliningDecision key {key}, value inliner-{value.inlinerHnd:X16} inlinee-{value.inlineeHnd:X16} " +
            $"res-{value.inlineResult} reason-'{ReportInliningDecision.GetString(value.reason_offset) ?? "(null)"}'");
        ReportInliningDecision.Unlock();
    }

    public void dmpReportTailCallDecision(uint key, in Agnostic_ReportTailCallDecision value)
    {
        Out.Write($"ReportTailCallDecision key-{key}, value cr-{value.callerHnd:X16} ce-{value.calleeHnd:X16} tail-{value.tailCallResult} " +
            $"call-{value.tailCallResult} -{ReportTailCallDecision.GetString(value.reason_index) ?? "(null)"}");
        ReportTailCallDecision.Unlock();
    }

    public void dmpProcessName(uint key, uint value)
    {
        Out.Write($"ProcessName key {key}, value '{ProcessName.GetString(value) ?? "(null)"}'");
        ProcessName.Unlock();
    }

    public void dmpAllocMem(uint key, in Agnostic_AllocMemDetails value)
        => Out.Write($"AllocMem key 0, value hotCodeSize-{value.hotCodeSize} coldCodeSize-{value.coldCodeSize} roDataSize-{value.roDataSize} " +
            $"xcptnsCount-{value.xcptnsCount} hotCodeBlock_offset-{value.hotCodeBlock_offset} coldCodeBlock_offset-{value.coldCodeBlock_offset} " +
            $"roDataBlock_offset-{value.roDataBlock_offset} hotCodeBlock-{value.hotCodeBlock:X16} coldCodeBlock-{value.coldCodeBlock:X16} " +
            $"roDataBlock-{value.roDataBlock:X16}");

    public void dmpCompileMethod(uint key, in Agnostic_CompileMethodResults value)
        => Out.Write($"CompileMethod key {key}, value nativeEntry-{value.nativeEntry:X16} nativeSizeOfCode-{value.nativeSizeOfCode} " +
            $"CorJitResult-{value.CorJitResult}");

    public void dmpMessageLog(uint key, uint value)
        => Out.Write("MessageLog NYI");

    public void dmpSetEHcount(uint key, uint value)
        => Out.Write($"SetEHcount key {key}, value {value}");

    public void dmpSetEHinfo(uint key, in Agnostic_CORINFO_EH_CLAUSE value)
    {
        Out.Write($"SetEHinfo key {key}, value flg-{value.Flags} to-{value.TryOffset} tl-{value.TryLength} ho-{value.HandlerOffset} " +
            $"hl-{value.HandlerLength}");
        if (value.Flags == CORINFO_EH_CLAUSE_FILTER)
        {
            Out.Write($" fo-{value.ClassToken}"); // FilterOffset
        }
        else if (value.Flags == CORINFO_EH_CLAUSE_NONE)
        {
            Out.Write($" cls-{value.ClassToken:X8}");
        }
        // else, no need to print for finally/fault handlers
    }

    public void dmpSetMethodAttribs(ulong key, uint value)
    {
        Out.Write($"SetMethodAttribs key ftn-{key:X16}, value attr-{value:X8}");
    }

    public void dmpReportFatalError(uint key, uint value)
        => Out.Write($"ReportFatalError key Count-{key}, value result-{value:X8}");

    public void dmpRecordRelocation(uint key, in Agnostic_RecordRelocation value)
        => Out.Write($"RecordRelocation key {key}, value loc-{value.location:X16} tgt-{value.target:X16} " +
            $"fRelocType-{value.fRelocType}({relocationTypeToString(value.fRelocType)}) addlDelta:{(int)value.addlDelta}");

    public void dmpReserveUnwindInfo(uint key, in Agnostic_ReserveUnwindInfo value)
        => Out.Write($"ReserveUnwindInfo key {key}, value isFun-{value.isFunclet} isCold-{value.isColdCode} usz-{value.unwindSize}");

    public void dmpAllocUnwindInfo(uint key, in Agnostic_AllocUnwindInfo value)
        => Out.Write($"AllocUnwindInfo key {key}, value pHot-{value.pHotCode:X16} pCold-{value.pColdCode:X16} startOff-{value.startOffset} " +
            $"endOff-{value.endOffset} unwindSz-{value.unwindSize} blki-{value.pUnwindBlock_index} funcKind-{value.funcKind}");

    public void dmpRecordCallSiteWithSignature(uint key, in Agnostic_RecordCallSite value)
        => Out.Write($"RecordCallSite key {key}, " +
            $"callSig-{SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(value.callSig, RecordCallSiteWithSignature, CrSigInstHandleMap)} " +
            $"ftn-{value.methodHandle:X16}");

    public void dmpRecordCallSiteWithoutSignature(uint key, ulong methodHandle)
        => Out.Write($"RecordCallSite without call signature key {key}, ftn-{methodHandle:X16}");

    public void dmpCrSigInstHandleMap(uint key, ulong value)
        => Out.Write($"CrSigInstHandleMap key {key}, value {value:X16}");
}
