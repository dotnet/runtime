// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using Internal.Pgo;
using static SuperPmi.DumpStrings;

namespace SuperPmi;

// Dumps each kind of recorded data, as mcs -dump prints it.
public sealed partial class MethodContext
{
    private static TextWriter Out => Logger.Out;

    private static uint U32(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset));

    private static ulong U64(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(offset));

    public void dmpGetClassAssemblyName(ulong key, uint value)
    {
        string assemblyName = GetClassAssemblyName.GetString(value);
        Out.Write($"GetClassAssemblyName cls-{key:X16}, value-{value} '{assemblyName ?? "(null)"}'");
        GetClassAssemblyName.Unlock();
    }

    public void dmpGetVars(ulong key, in Agnostic_GetVars value)
    {
        const int ILVarInfoSize = 12; // ICorDebugInfo::ILVarInfo: startOffset, endOffset, varNumber
        ReadOnlySpan<byte> vars = GetVars.GetBuffer(value.vars_offset);
        Out.Write($"GetVars key ftn-{key:X16}, value cVars-{value.cVars} extendOthers-{value.extendOthers} (");
        for (uint i = 0; i < value.cVars; i++)
        {
            int at = (int)i * ILVarInfoSize;
            Out.Write($"({i} {U32(vars, at)} {U32(vars, at + 4)} {U32(vars, at + 8)})");
        }
        Out.Write(")");
        GetVars.Unlock();
    }

    public void dmpGetBoundaries(ulong key, in Agnostic_GetBoundaries value)
    {
        Out.Write($"GetBoundaries key ftn-{key:X16}, value cnt-{value.cILOffsets} imp-{value.implicitBoundaries}{{");
        ReadOnlySpan<byte> bnd = GetBoundaries.GetBuffer(value.pILOffset_offset);
        for (uint i = 0; i < value.cILOffsets; i++)
        {
            Out.Write($"{U32(bnd, (int)i * 4)}");
            // Every element is followed by a comma.
            Out.Write(",");
        }
        GetBoundaries.Unlock();
        Out.Write("}");
    }

    public void dmpGetMethodNameFromMetadata(Agnostic_CORINFO_METHODNAME_TOKENin key, Agnostic_CORINFO_METHODNAME_TOKENout value)
    {
        string methodName = GetMethodNameFromMetadata.GetString(value.methodName);
        string className = GetMethodNameFromMetadata.GetString(value.className);
        string namespaceName = GetMethodNameFromMetadata.GetString(value.namespaceName);
        ReadOnlySpan<byte> enclosingClassNameIndices = GetMethodNameFromMetadata.GetBuffer(value.enclosingClassNames);
        Out.Write($"GetMethodNameFromMetadata key - ftn-{key.ftn:X16} classNonNull-{key.className} namespaceNonNull-{key.namespaceName} " +
            $"maxEnclosingClassNames-{key.maxEnclosingClassNames}, value meth-'{methodName ?? "(null)"}', class-'{className ?? "(null)"}', " +
            $"namespace-'{namespaceName ?? "(null)"}'");

        for (uint i = 0; i < key.maxEnclosingClassNames; i++)
        {
            string enclosingClassName = GetMethodNameFromMetadata.GetString(U32(enclosingClassNameIndices, (int)i * 4));
            if (enclosingClassName != null)
                Out.Write($" enclosingClass{i}-'{enclosingClassName}'");
        }

        GetMethodNameFromMetadata.Unlock();
    }

    public void dmpGetJitFlags(uint key, DD value)
    {
        ReadOnlySpan<byte> jitflags = GetJitFlags.GetBuffer(value.A);
        ulong flags = U64(jitflags, 0);
        Out.Write($"GetJitFlags key {key} sizeInBytes-{value.B} jitFlags-{flags:X16} instructionSetFlags-");

        var isaFlags = new ulong[InstructionFlagsFieldCount];
        for (int i = 0; i < isaFlags.Length; i++)
            isaFlags[i] = U64(jitflags, 8 * (i + 1));
        for (int i = isaFlags.Length - 1; i >= 0; i--)
            Out.Write($"{isaFlags[i]:X16}");

        Out.Write(" (");
        for (uint flag = 0; flag < 64; flag++)
        {
            if ((flags & (1UL << (int)flag)) != 0)
                Out.Write($" {JitFlagNames.CorJitFlagToString(flag)}");
        }
        for (uint isa = 0; isa < 64 * isaFlags.Length; isa++)
        {
            if ((isaFlags[isa / 64] & (1UL << (int)(isa % 64))) != 0)
                Out.Write($" {JitFlagNames.InstructionSetToString(isa)}");
        }
        Out.Write(" )");
        GetJitFlags.Unlock();
    }

    public void dmpGetFieldInfo(in Agnostic_GetFieldInfo key, in Agnostic_CORINFO_FIELD_INFO value)
    {
        Out.Write($"GetFieldInfo key ch-{key.callerHandle:X16} flg-{key.flags:X8} " +
            $"rt{{{SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.ResolvedToken)}}}");

        Out.Write($", value fa-{value.fieldAccessor} fflg-{value.fieldFlags:X8} hlp-{value.helper} off-{value.offset} " +
            $"fT-{value.fieldType}({toString_CorInfoType(value.fieldType)}) sT-{value.structType:X16} aa-{value.accessAllowed} " +
            $"hnum-{value.accessCalloutHelper.helperNum} na-{value.accessCalloutHelper.numArgs} {{");

        for (int i = 0; i < 4; i++)
        {
            Agnostic_CORINFO_HELPER_ARG arg = value.accessCalloutHelper.args[i];
            string kind = arg.argType switch
            {
                1 => "fld",
                2 => "mth",
                3 => "cls",
                4 => "mod",
                5 => "const",
                _ => null,
            };
            if (kind == null)
                Out.Write($"{{{i}: illegal}}");
            else
                Out.Write($"{{{i}: {kind}-{arg.constant:X16}}}");
        }
        Out.Write($" fl {SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(value.fieldLookup)}}}");
    }

    public void dmpGetClassGClayout(ulong key, in Agnostic_GetClassGClayout value)
    {
        Out.Write($"GetClassGCLayout key {key:X16}, value len {value.len} cnt {value.valCount} {{");
        if (value.gcPtrs_Index != uint.MaxValue)
        {
            ReadOnlySpan<byte> ptr = GetClassGClayout.GetBuffer(value.gcPtrs_Index);
            for (uint i = 0; i < value.len; i++)
            {
                Out.Write($"0x{ptr[(int)i]:x2}");
                if (i + 1 < value.len)
                    Out.Write(",");
            }
            GetClassGClayout.Unlock();
        }
        Out.Write("}");
    }

    public void dmpCanAccessClass(in Agnostic_CanAccessClassIn key, in Agnostic_CanAccessClassOut value)
    {
        Out.Write($"CanAccessClass key rt{{{SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.ResolvedToken)}}}, callerHandle " +
            $"{key.callerHandle:X16}\n");
        Out.Write($", value hnum-{value.AccessHelper.helperNum} na-{value.AccessHelper.numArgs} {{");
        for (int i = 0; i < 4; i++)
            Out.Write($"{{{value.AccessHelper.args[i].constant:X16} {value.AccessHelper.args[i].argType}}}");
        Out.Write($"}} res-{value.result}");
    }

    public void dmpGetTypeLayout(ulong key, in Agnostic_GetTypeLayoutResult value)
    {
        Out.Write($"GetTypeLayout key type-{key:X16} value result={(int)value.result} numNodes={(int)value.numNodes}");
        if (value.numNodes > 0)
        {
            ReadOnlySpan<Agnostic_CORINFO_TYPE_LAYOUT_NODE> nodes =
                MemoryMarshal.Cast<byte, Agnostic_CORINFO_TYPE_LAYOUT_NODE>(GetTypeLayout.GetBuffer(value.nodesBuffer));
            ulong index = 0;
            dmpTypeLayoutTree(nodes, value.numNodes, ref index, 1);
        }
    }

    // Nodes are printed one after another on the same line. For partial layouts the field
    // counts can describe more nodes than were recorded, which aren't printed.
    public void dmpTypeLayoutTree(ReadOnlySpan<Agnostic_CORINFO_TYPE_LAYOUT_NODE> nodes, ulong maxNodes, ref ulong index, ulong indent)
    {
        if (index >= (ulong)nodes.Length)
            return;

        for (ulong i = 0; i < indent; i++)
            Out.Write("  ");

        Agnostic_CORINFO_TYPE_LAYOUT_NODE node = nodes[(int)index];
        Out.Write($"{index}: parent {node.parent} offset {node.offset} size {node.size} type {toString_CorInfoType(node.type)} numFields " +
            $"{node.numFields} hasSignificantPadding {(node.hasSignificantPadding != 0 ? "yes" : "no")} simdTypeHnd {node.simdTypeHnd:X16} " +
            $"diagFieldHnd {node.diagFieldHnd:X16}");

        index++;
        for (ulong i = 0; i < node.numFields; i++)
        {
            if (i >= maxNodes)
                break;
            dmpTypeLayoutTree(nodes, maxNodes, ref index, indent + 1);
        }
    }

    private const int PgoSchemaSize = 24; // Agnostic_PgoInstrumentationSchema

    private static Agnostic_PgoInstrumentationSchema PgoSchemaAt(ReadOnlySpan<byte> schema, uint i)
        => MemoryMarshal.Read<Agnostic_PgoInstrumentationSchema>(schema.Slice((int)i * PgoSchemaSize));

    public void dmpAllocPgoInstrumentationBySchema(ulong key, in Agnostic_AllocPgoInstrumentationBySchema value)
    {
        Out.Write($"AllocPgoInstrumentationBySchema key ftn-{key:X16}, value res-{value.result:X8} addr-{value.instrumentationDataAddress:X16} " +
            $"cnt-{value.countSchemaItems} schema{{\n");

        if (value.countSchemaItems > 0)
        {
            ReadOnlySpan<byte> pBuf = AllocPgoInstrumentationBySchema.GetBuffer(value.schema_index);
            Out.Write("\n");
            for (uint i = 0; i < value.countSchemaItems; i++)
            {
                Agnostic_PgoInstrumentationSchema s = PgoSchemaAt(pBuf, i);
                Out.Write($" {i}-{{Offset {s.Offset:X16} ILOffset {s.ILOffset} Kind {s.InstrumentationKind}(0x{s.InstrumentationKind:x}) Count " +
                    $"{s.Count} Other {s.Other}}}\n");
            }
            AllocPgoInstrumentationBySchema.Unlock();
        }
        Out.Write("}");
    }

    public void dmpGetPgoInstrumentationResults(ulong key, in Agnostic_GetPgoInstrumentationResults value)
    {
        Out.Write($"GetPgoInstrumentationResults key ftn-{key:X16}, value res-{value.result:X8} schemaCnt-{value.countSchemaItems} " +
            $"profileBufSize-{value.dataByteCount} source-{value.pgoSource} dynamic-{value.dynamicPgo} schema{{");

        if (value.countSchemaItems > 0)
        {
            ReadOnlySpan<byte> pBuf = GetPgoInstrumentationResults.GetBuffer(value.schema_index);
            ReadOnlySpan<byte> data = GetPgoInstrumentationResults.GetBuffer(value.data_index);

            Out.Write("\n");
            for (uint i = 0; i < value.countSchemaItems; i++)
            {
                Agnostic_PgoInstrumentationSchema s = PgoSchemaAt(pBuf, i);
                Out.Write($" {i}-{{Offset {s.Offset:X16} ILOffset {s.ILOffset} Kind {s.InstrumentationKind}(0x{s.InstrumentationKind:x}) Count " +
                    $"{s.Count} Other {s.Other} Data ");

                int at = (int)s.Offset;
                switch ((PgoInstrumentationKind)s.InstrumentationKind)
                {
                    case PgoInstrumentationKind.BasicBlockIntCount:
                        Out.Write($"B {U32(data, at)}");
                        break;
                    case PgoInstrumentationKind.BasicBlockLongCount:
                        Out.Write($"B {U64(data, at)}");
                        break;
                    case PgoInstrumentationKind.EdgeIntCount:
                        Out.Write($"E {U32(data, at)}");
                        break;
                    case PgoInstrumentationKind.EdgeLongCount:
                        Out.Write($"E {U64(data, at)}");
                        break;
                    case PgoInstrumentationKind.HandleHistogramIntCount:
                        Out.Write($"T {U32(data, at)}");
                        break;
                    case PgoInstrumentationKind.HandleHistogramLongCount:
                        Out.Write($"T {U64(data, at)}");
                        break;
                    case PgoInstrumentationKind.HandleHistogramTypes:
                    case PgoInstrumentationKind.HandleHistogramMethods:
                        for (uint j = 0; j < s.Count; j++)
                            Out.Write($"[{j}] {ReadPointer(data, at + (int)j * IntPtr.Size):X16} ");
                        break;
                    case PgoInstrumentationKind.ValueHistogramIntCount:
                        Out.Write($"V {U32(data, at)}");
                        break;
                    case PgoInstrumentationKind.ValueHistogramLongCount:
                        Out.Write($"V {U64(data, at)}");
                        break;
                    case PgoInstrumentationKind.ValueHistogram:
                        for (uint j = 0; j < s.Count; j++)
                            Out.Write($"[{j}] {U32(data, at + (int)j * sizeof(uint)):X8} ");
                        break;
                    case PgoInstrumentationKind.GetLikelyClass:
                    case PgoInstrumentationKind.GetLikelyMethod:
                        // (N)umber, (L)ikelihood, (H)andle
                        Out.Write($"N {s.Other >> 8} L {s.Other & 0xFF} H {ReadPointer(data, at):X16}");
                        break;
                    default:
                        Out.Write("?");
                        break;
                }

                Out.Write("}\n");
            }
            GetPgoInstrumentationResults.Unlock();
        }
        Out.Write($"}} data_index-{value.data_index}");
    }

    // Instrumentation data holds handles in the collecting host's pointer size.
    private static ulong ReadPointer(ReadOnlySpan<byte> data, int offset)
        => IntPtr.Size == 8 ? U64(data, offset) : U32(data, offset);

    public void dmpGetSystemVAmd64PassStructInRegisterDescriptor(ulong key, in Agnostic_GetSystemVAmd64PassStructInRegisterDescriptor value)
    {
        Out.Write($"GetSystemVAmd64PassStructInRegisterDescriptor key structHnd-{key:X16}, value passInReg-{value.passedInRegisters} " +
            $"8bCount-{value.eightByteCount}");
        for (int i = 0; i < 2; i++)
        {
            Out.Write($" 8bClass[{i}]-{value.eightByteClassifications[i]} 8bSz[{i}]-{value.eightByteSizes[i]} " +
                $"8bOff[{i}]-{value.eightByteOffsets[i]}");
        }
        Out.Write($" result {value.result}");
    }

    public void dmpGetSwiftLowering(ulong key, in Agnostic_GetSwiftLowering value)
    {
        Out.Write($"GetSwiftLowering key structHnd-{key:X16}, value byReference-{value.byReference} numLoweredElements-{value.numLoweredElements}");
        if (value.byReference == 0)
        {
            for (int i = 0; i < value.numLoweredElements; i++)
                Out.Write($" [{i}] {value.loweredElements[i]}");
        }
    }

    public void dmpGetFpStructLowering(ulong key, in Agnostic_GetFpStructLowering value)
    {
        Out.Write($"GetFpStructLowering key structHnd-{key:X16}, value byIntegerCallConv-{value.byIntegerCallConv} " +
            $"numLoweredElements-{value.numLoweredElements}");
        if (value.byIntegerCallConv == 0)
        {
            for (int i = 0; i < value.numLoweredElements; i++)
                Out.Write($" [{i}] {value.loweredElements[i]}");
        }
    }

    public void dmpGetClassNameFromMetadata(DLD key, DD value)
    {
        string className = GetClassNameFromMetadata.GetString(value.A);
        string namespaceName = GetClassNameFromMetadata.GetString(value.B);
        Out.Write($"GetClassNameFromMetadata key - classNonNull-{key.A} namespaceNonNull-{key.B}, value class-'{className ?? "(null)"}', " +
            $"namespace-'{namespaceName ?? "(null)"}'");
        GetClassNameFromMetadata.Unlock();
    }

    public void dmpPrint(string name, LightWeightMapBuffer buffer, ulong key, in Agnostic_PrintResult value)
    {
        Out.Write($"{name} key hnd-{key:X16}, stringBufferSize-{value.stringBufferSize}, requiredBufferSize-{value.requiredBufferSize}");
        buffer.Unlock();
    }

    public void dmpCheckMethodModifier(in Agnostic_CheckMethodModifier key, uint value)
    {
        Out.Write($"CheckMethodModifier key, ftn-{key.hMethod:X16} mod-'{CheckMethodModifier.GetString(key.modifier) ?? "(null)"}' " +
            $"opt-{key.fOptional}, value res-{value}");
        CheckMethodModifier.Unlock();
    }

    public void dmpGetIntConfigValue(in Agnostic_ConfigIntInfo key, uint value)
    {
        string name = GetIntConfigValue.GetString(key.nameIndex);
        Out.Write($"GetIntConfigValue name {name ?? "(null)"}, default value {(int)key.defaultValue}, value {(int)value}");
        GetIntConfigValue.Unlock();
    }

    public void dmpGetStringConfigValue(uint nameIndex, uint resultIndex)
    {
        string name = GetStringConfigValue.GetString(nameIndex);
        string result = GetStringConfigValue.GetString(resultIndex);
        Out.Write($"GetStringConfigValue name {name ?? "(null)"}, result {result ?? "(null)"}");
        GetStringConfigValue.Unlock();
    }

    public void dmpCompileMethod(uint key, in Agnostic_CompileMethod value)
        => Out.Write($"CompileMethod key {key}, value ftn-{value.info.ftn:X16} scp-{value.info.scope:X16} ilo-{value.info.ILCode_offset} " +
            $"ils-{value.info.ILCodeSize} ms-{value.info.maxStack} ehc-{value.info.EHcount} opt-{value.info.options} rk-{value.info.regionKind} " +
            $"args-{SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(value.info.args, CompileMethod, SigInstHandleMap)} " +
            $"locals-{SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(value.info.locals, CompileMethod, SigInstHandleMap)} flg-{value.flags:X8} " +
            $"os-{value.os}");

    public void dmpGetMethodClass(ulong key, ulong value)
        => Out.Write($"GetMethodClass key {key:X16}, value {value:X16}");

    public void dmpGetClassAttribs(ulong key, uint value)
        => Out.Write($"GetClassAttribs key {key:X16}, value {value:X8} ({SpmiDumpHelper.DumpCorInfoFlag(value)})");

    public void dmpIsIntrinsic(ulong key, uint value)
        => Out.Write($"IsIntrinsic key ftn-{key:X16}, value res-{value}");

    public void dmpNotifyMethodInfoUsage(ulong key, uint value)
        => Out.Write($"NotifyMethodInfoUsage key ftn-{key:X16}, value res-{value}");

    public void dmpNotifyInstructionSetUsage(DD key, uint value)
        => Out.Write($"NotifyInstructionSetUsage key isa-{key.A}, supported-{key.B & 1}, preserve-negative-dependency-{(key.B >> 1) & 1}, " +
            $"res-{value}");

    public void dmpGetMethodAttribs(ulong key, uint value)
        => Out.Write($"GetMethodAttribs key {key:X16}, value {value:X8} ({SpmiDumpHelper.DumpCorInfoFlag(value)})");

    public void dmpInitClass(in Agnostic_InitClass key, uint value)
        => Out.Write($"InitClass key fld-{key.field:X16} meth-{key.method:X16} con-{key.context:X16}, value res-{value}");

    public void dmpCanInline(DLDL key, in Agnostic_CanInline value)
        => Out.Write($"CanInline key - callerHnd-{key.A:X16} calleeHnd-{key.B:X16}, value result-{value.result} " +
            $"exceptionCode-{value.exceptionCode:X8}");

    public void dmpResolveToken(in Agnostic_CORINFO_RESOLVED_TOKENin key, in ResolveTokenValue value)
        => Out.Write($"ResolveToken key: {SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKENin(key)}, value: " +
            $"{SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKENout(value.tokenOut)} excp-{value.exceptionCode:X8}");

    public void dmpGetCallInfo(in Agnostic_GetCallInfo key, in Agnostic_CORINFO_CALL_INFO value)
        => Out.Write($"GetCallInfo key rt{{{SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.ResolvedToken)}}} " +
            $"crt{{{SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.ConstrainedResolvedToken)}}} ch-{key.callerHandle:X16} " +
            $"flg-{key.flags:X8}, value mth-{value.hMethod:X16}, mf-{value.methodFlags:X8} ({SpmiDumpHelper.DumpCorInfoFlag(value.methodFlags)}) " +
            $"cf-{value.classFlags:X8} ({SpmiDumpHelper.DumpCorInfoFlag(value.classFlags)}) " +
            $"sig-{SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(value.sig, GetCallInfo, SigInstHandleMap)} aa-{value.accessAllowed} " +
            $"cch{{hn-{value.callsiteCalloutHelper.helperNum} na-{value.callsiteCalloutHelper.numArgs} (TODO: dump callsiteCalloutHelper.args)}} " +
            $"tt-{value.thisTransform} k-{value.kind} ({toString_CORINFO_CALL_KIND(value.kind)}) nic-{value.nullInstanceCheck} " +
            $"({((value.nullInstanceCheck != 0) ? "true" : "false")}) ch-{value.contextHandle:X16} ecnrl-{value.exactContextNeedsRuntimeLookup} " +
            $"({((value.exactContextNeedsRuntimeLookup != 0) ? "true" : "false")}) " +
            $"stubLookup{{{SpmiDumpHelper.DumpAgnostic_CORINFO_LOOKUP(value.stubLookup)}}} ipl{{at-{value.instParamLookup.accessType:X8} " +
            $"({toString_InfoAccessType(value.instParamLookup.accessType)}) hnd-{value.instParamLookup.handle:X16}}} excp-{value.exceptionCode:X8}");

    public void dmpExpandRawHandleIntrinsic(in Agnostic_ExpandRawHandleIntrinsic key, in Agnostic_CORINFO_GENERICHANDLE_RESULT result)
        => Out.Write($"ExpandRawHandleIntrinsic key: {SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKENin(key.ResolvedToken)}, value " +
            $"{SpmiDumpHelper.DumpAgnostic_CORINFO_LOOKUP(result.lookup)} cth-{result.compileTimeHandle:x16} ht-{result.handleType}");

    public void dmpIsIntrinsicType(ulong key, uint value)
        => Out.Write($"IsIntrinsicType key mth-{key:X16}, value intr-{value}");

    public void dmpAsCorInfoType(ulong key, uint value)
        => Out.Write($"AsCorInfoType key cls-{key:X16}, value cit-{value}({toString_CorInfoType(value)})");

    public void dmpIsValueClass(ulong key, uint value)
        => Out.Write($"IsValueClass key cls-{key:X16}, value res-{value}");

    public void dmpIsByRefLike(ulong key, uint value)
        => Out.Write($"IsByRefLike key cls-{key:X16}, value res-{value}");

    public void dmpIsSharedInst(ulong key, uint value)
        => Out.Write($"IsSharedInst key cls-{key:X16}, value res-{value}");

    public void dmpGetClassSize(ulong key, uint val)
        => Out.Write($"GetClassSize key {key:X16}, value {val}");

    public void dmpGetHeapClassSize(ulong key, uint val)
        => Out.Write($"GetHeapClassSize key {key:X16}, value {val}");

    public void dmpCanAllocateOnStack(ulong key, uint val)
        => Out.Write($"CanAllocateOnStack key {key:X16}, value {val}");

    public void dmpGetClassNumInstanceFields(ulong key, uint value)
        => Out.Write($"GetClassNumInstanceFields key cls-{key:X16}, value res-{value}");

    public void dmpGetNewArrHelper(ulong key, uint value)
        => Out.Write($"GetNewArrHelper key cls-{key:X16}, value res-{value}");

    public void dmpGetSharedCCtorHelper(ulong key, uint value)
        => Out.Write($"GetSharedCCtorHelper key cls-{key:X16}, value res-{value}");

    public void dmpGetTypeForBox(ulong key, ulong value)
        => Out.Write($"GetTypeForBox key cls-{key:X16}, value res-{value:X16}");

    public void dmpGetBoxHelper(ulong key, uint value)
        => Out.Write($"GetBoxHelper key cls-{key:X16}, value res-{value}");

    public void dmpGetBuiltinClass(uint key, ulong value)
        => Out.Write($"GetBuiltinClass key cls-{key:X8}, value cls-{value:X16}");

    public void dmpGetTypeForPrimitiveValueClass(ulong key, uint value)
        => Out.Write($"GetTypeForPrimitiveValueClass key cls-{key:X16}, value cit-{value}({toString_CorInfoType(value)})");

    public void dmpGetTypeForPrimitiveNumericClass(ulong key, uint value)
        => Out.Write($"GetTypeForPrimitiveNumericClass key cls-{key:X16}, value cit-{value}({toString_CorInfoType(value)})");

    public void dmpGetParentType(ulong key, ulong value)
        => Out.Write($"GetParentType key cls-{key:X16}, value cls-{value:X16}");

    public void dmpIsSDArray(ulong key, uint value)
        => Out.Write($"IsSDArray key cls-{key:X16}, value res-{value}");

    public void dmpGetFieldClass(ulong key, ulong value)
        => Out.Write($"GetFieldClass key {key:X16}, value {value:X16}");

    public void dmpGetFieldOffset(ulong key, uint value)
        => Out.Write($"GetFieldOffset key FLD-{key:X16}, value {value:X8}");

    public void dmpGetUnBoxHelper(ulong key, uint value)
        => Out.Write($"GetUnBoxHelper key cls-{key:X16}, value res-{value}");

    public void dmpGetRuntimeTypePointer(ulong key, ulong value)
        => Out.Write($"GetRuntimeTypePointer key cls-{key:X16}, value res-{value:X16}");

    public void dmpIsObjectImmutable(ulong key, uint value)
        => Out.Write($"IsObjectImmutable key obj-{key:X16}, value res-{value}");

    public void dmpGetStringChar(DLD key, DD value)
        => Out.Write($"IsObjectImmutable key obj-{key.A:X16}, index-{key.B}, value res-{value.A}, val-{value.B}");

    public void dmpGetObjectType(ulong key, ulong value)
        => Out.Write($"GetObjectType key obj-{key:X16}, value res-{value:X16}");

    public void dmpGetReadyToRunHelper(GetReadyToRunHelper_TOKENin key, GetReadyToRunHelper_TOKENout value)
    {
        Out.Write($"GetReadyToRunHelper key: tk{{{SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.ResolvedToken)}}} id-{key.id}");
        Out.Write($", value: lk{{ {SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(value.Lookup)} }} {value.result}");
    }

    public void dmpGetReadyToRunDelegateCtorHelper(GetReadyToRunDelegateCtorHelper_TOKENIn key, Agnostic_CORINFO_LOOKUP value)
    {
        Out.Write($"GetReadyToRunDelegateCtorHelper key: method tk{{{SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.TargetMethod)}}} " +
            $"type-{key.delegateType:X16} constraint-{key.targetConstraint:X8}");
        Out.Write($", value: {SpmiDumpHelper.DumpAgnostic_CORINFO_LOOKUP(value)}");
    }

    public void dmpGetHelperFtn(uint key, Agnostic_GetHelperFtn value)
        => Out.Write($"GetHelperFtn key ftn-{key} nativeEntrypoint {{{SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(value.helperLookup)}}}, " +
            $"helperMethod-{value.helperMethod:X16}");

    public void dmpGetJustMyCodeHandle(ulong key, DLDL value)
        => Out.Write($"GetJustMyCodeHandle key ftn-{key:X16}, value pp-{value.A:X16}, res-{value.B:X16}");

    public void dmpGetFunctionEntryPoint(DLD key, DLD value)
        => Out.Write($"GetFunctionEntryPoint key ftn-{key.A:X16} af-{key.B:X8}, value add-{value.A:X16} at-{value.B}");

    public void dmpConstructStringLiteral(DLD key, DLD value)
        => Out.Write($"ConstructStringLiteral key mod-{key.A:X16} tok-{key.B:X8}, value pp-{value.A:X16} iat-{value.B}");

    public void dmpConvertPInvokeCalliToCall(DLD key, DLDL value)
        => Out.Write($"ConvertPInvokeCalliToCall key mod-{key.A:X16} tok-{key.B:X8}, value cls-{value.A:X16} meth-{value.B:X16}");

    public void dmpEmptyStringLiteral(uint key, DLD value)
        => Out.Write($"EmptyStringLiteral key {key}, value pVal-{value.A:X16} res-{value.B}");

    public void dmpGetArgType(in Agnostic_GetArgType_Key key, in Agnostic_GetArgType_Value value)
    {
        Out.Write($"GetArgType key flg-{key.flags:X8} na-{key.numArgs} " +
            $"{SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INST_Element("", "cc", "ci", key.sigInst_classInstCount, key.sigInst_classInst_Index, SigInstHandleMap)} " +
            $"{SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INST_Element("", "mc", "mi", key.sigInst_methInstCount, key.sigInst_methInst_Index, SigInstHandleMap)} " +
            $"msig-{key.methodSignature:X16} scp-{key.scope:X16} arg-{key.args:X16}");
        Out.Write($", value result(cit)-{value.result}({toString_CorInfoTypeWithMod(value.result)}) vcType-{value.vcTypeRet:X16} " +
            $"excp-{value.exceptionCode:X8}");
    }

    public void dmpGetExactClasses(DLD key, in Agnostic_GetExactClassesResult value)
        => Out.Write($"GetExactClasses key baseType-{key.A:X16}, maxExactCls-{key.B}, value numClasses-{value.numClasses}");

    public void dmpGetArgNext(ulong key, ulong value)
        => Out.Write($"GetArgNext key {key:X16}, value {value:X16}");

    public void dmpGetMethodSig(DLDL key, in Agnostic_CORINFO_SIG_INFO value)
        => Out.Write($"GetMethodSig key ftn-{key.A:X16} prt-{key.B:X16}, " +
            $"value-{SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(value, GetMethodSig, SigInstHandleMap)}");

    public void dmpGetArgClass(in Agnostic_GetArgClass_Key key, in Agnostic_GetArgClass_Value value)
    {
        Out.Write("GetArgClass key " +
            $"{SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INST_Element("", "cc", "ci", key.sigInst_classInstCount, key.sigInst_classInst_Index, SigInstHandleMap)} " +
            $"{SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INST_Element("", "mc", "mi", key.sigInst_methInstCount, key.sigInst_methInst_Index, SigInstHandleMap)} " +
            $"msig-{key.methodSignature:X16} scp-{key.scope:X16} args-{key.args:X16}");
        Out.Write($", value {value.result:X16} excp-{value.exceptionCode:X8}");
    }

    public void dmpGetHFAType(ulong key, uint value)
        => Out.Write($"GetHFAType key {key:X16}, value {value} ");

    public void dmpGetMethodInfo(DLDL key, in Agnostic_GetMethodInfo value)
    {
        if (value.result != 0)
        {
            Out.Write($"GetMethodInfo key ftn-{key.A:X16} ctx-{key.B:X16}, value res-{value.result} ftn-{value.info.ftn:X16} " +
                $"scp-{value.info.scope:X16} ilo-{value.info.ILCode_offset} ils-{value.info.ILCodeSize} ms-{value.info.maxStack} " +
                $"ehc-{value.info.EHcount} opt-{value.info.options:X8} rk-{value.info.regionKind} " +
                $"args-{SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(value.info.args, GetMethodInfo, SigInstHandleMap)} " +
                $"locals-{SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(value.info.locals, GetMethodInfo, SigInstHandleMap)} " +
                $"excp-{value.exceptionCode:X8}");
        }
        else
        {
            Out.Write($"GetMethodInfo key ftn-{key.A:X16} ctx-{key.B:X16}, value res-{value.result} excp-{value.exceptionCode:X8}");
        }
    }

    public void dmpHaveSameMethodDefinition(in DLDL key, uint value)
        => Out.Write($"HaveSameMethodDefinition key methHnd1-{key.A:X16}, methHnd2-{key.B:X16}, value {value}");

    public void dmpGetTypeDefinition(ulong key, ulong value)
        => Out.Write($"GetTypeDefinition key cls-{key:X16}, value cls-{value:X16}");

    public void dmpGetNewHelper(ulong key, in DDD value)
        => Out.Write($"GetNewHelper key {key:X16}, hasSideEffects-{value.A}, value res-{value.B}, exceptionCode-{value.C:X8}");

    public void dmpEmbedGenericHandle(in Agnostic_EmbedGenericHandle key, in Agnostic_CORINFO_GENERICHANDLE_RESULT value)
        => Out.Write($"EmbedGenericHandle key rt{{{SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.ResolvedToken)}}} " +
            $"emb-{key.fEmbedParent}, value {SpmiDumpHelper.DumpAgnostic_CORINFO_LOOKUP(value.lookup)} cth-{value.compileTimeHandle:X16} " +
            $"ht-{value.handleType}");

    public void dmpGetEHinfo(DLD key, in Agnostic_CORINFO_EH_CLAUSE value)
        => Out.Write($"GetEHinfo key ftn-{key.A:X16} ehn-{key.B}, value flg-{value.Flags} to-{value.TryOffset} tl-{value.TryLength} " +
            $"ho-{value.HandlerOffset} hl-{value.HandlerLength} ct-{value.ClassToken}");

    public void dmpGetMethodVTableOffset(ulong key, DDD value)
        => Out.Write($"GetMethodVTableOffset key ftn-{key:X16}, value offi-{value.A}, offa-{value.B}. offr-{(int)value.C}");

    public void dmpResolveVirtualMethod(in Agnostic_ResolveVirtualMethodKey key, in Agnostic_ResolveVirtualMethodResult result)
    {
        Out.Write($"ResolveVirtualMethod key virtMethod-{key.virtualMethod:X16}, objClass-{key.objClass:X16}, context-{key.context:X16} " +
            $"pResolvedTokenVirtualMethodNonNull-{key.pResolvedTokenVirtualMethodNonNull:X8} " +
            $"pResolvedTokenVirtualMethod{{{((key.pResolvedTokenVirtualMethodNonNull != 0) ? SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.pResolvedTokenVirtualMethod) : "???")}}}");
        Out.Write($", value returnValue-{((result.returnValue != 0) ? "true" : "false")}, devirtMethod-{result.devirtualizedMethod:X16}, " +
            $"tokenLookupContext-{result.tokenLookupContext:X16}, detail-{(int)result.detail}, " +
            $"tokDvMeth{{{((result.returnValue != 0) ? SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(result.resolvedTokenDevirtualizedMethod) : "???")}}}, " +
            $"tokDvUnboxMeth{{{((result.returnValue != 0) ? SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(result.resolvedTokenDevirtualizedUnboxedMethod) : "???")}}}, " +
            $"instParamLookup{{{SpmiDumpHelper.DumpAgnostic_CORINFO_LOOKUP(result.instParamLookup)}}}");
    }

    public void dmpGetAsyncOtherVariant(ulong key, DLD value)
        => Out.Write($"GetAsyncOtherVariant ftn-{key:X16}, result-{value.A:X16}, variantIsThunk-{value.B}");

    public void dmpGetDefaultComparerClass(ulong key, ulong value)
        => Out.Write($"GetDefaultComparerClass key cls-{key:X16}, value cls-{value:X16}");

    public void dmpGetDefaultEqualityComparerClass(ulong key, ulong value)
        => Out.Write($"GetDefaultEqualityComparerClass key cls-{key:X16}, value cls-{value:X16}");

    public void dmpGetSZArrayHelperEnumeratorClass(ulong key, ulong value)
        => Out.Write($"GetSZArrayHelperEnumeratorClass key cls-{key:X16}, value cls-{value:X16}");

    public void dmpGetTokenTypeAsHandle(in GetTokenTypeAsHandleValue key, ulong value)
        => Out.Write($"GetTokenTypeAsHandle key ftn-{key.hMethod:X16} fld-{key.hField:X16}, value cls-{value:X16}");

    public void dmpGetThreadLocalFieldInfo(DLD key, uint value)
        => Out.Write($"GetThreadLocalFieldInfo key hnd-{key.A:X16},gctype-{(int)key.B} result-{value}");

    public void dmpGetThreadLocalStaticBlocksInfo(uint key, in Agnostic_GetThreadLocalStaticBlocksInfo value)
        => Out.Write($"GetThreadLocalStaticBlocksInfo key {key}, tlsIndex-{SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(value.tlsIndex)}, , " +
            $"tlsGetAddrFtnPtr-{value.tlsGetAddrFtnPtr:X16}, tlsIndexObject - {value.tlsIndexObject:X16}, threadVarsSection - " +
            $"{value.threadVarsSection:X16}, offsetOfThreadLocalStoragePointer-{value.offsetOfThreadLocalStoragePointer}, " +
            $"offsetOfMaxThreadStaticBlocks-{value.offsetOfMaxThreadStaticBlocks}, offsetOfThreadStaticBlocks-{value.offsetOfThreadStaticBlocks}, " +
            $"offsetOfBaseOfThreadLocalData-{value.offsetOfBaseOfThreadLocalData}");

    public void dmpGetThreadLocalStaticInfo_NativeAOT(ulong key, in Agnostic_GetThreadStaticInfo_NativeAOT value)
        => Out.Write($"GetThreadLocalStaticInfo_NativeAOT key {key:X16}, " +
            $"tlsRootObject-{SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(value.tlsRootObject)}, " +
            $"tlsIndexObject-{SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(value.tlsIndexObject)},  " +
            $"offsetOfThreadLocalStoragePointer-{value.offsetOfThreadLocalStoragePointer}, " +
            $"threadStaticBaseSlow-{SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(value.threadStaticBaseSlow)}, " +
            $"tlsGetAddrFtnPtr-{SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(value.tlsGetAddrFtnPtr)}");

    public void dmpEmbedMethodHandle(ulong key, DLDL value)
        => Out.Write($"EmbedMethodHandle key ftn-{key:X16}, value pp-{value.A:X16} res-{value.B:X16}");

    public void dmpGetStaticFieldContent(DLDDD key, DD value)
    {
        Out.Write($"GetStaticFieldContent key fld-{key.A:X16} bufSize-{key.B}, ignoremovable-{key.C}, valOffset-{key.D} result-{value.A}");
        GetStaticFieldContent.Unlock();
    }

    public void dmpGetObjectContent(DLDD key, DD value)
    {
        Out.Write($"GetObjectContent key fld-{key.A:X16} bufSize-{key.B}, valOffset-{key.C} result-{value.A}");
        GetObjectContent.Unlock();
    }

    public void dmpGetStaticFieldCurrentClass(DLD key, in Agnostic_GetStaticFieldCurrentClass value)
        => Out.Write($"GetStaticFieldCurrentClass key fld-{key.A:X16}, value clsHnd-{value.classHandle:X16} isSpeculative-{value.isSpeculative}");

    public void dmpGetClassAlignmentRequirement(DLD key, uint value)
        => Out.Write($"GetClassAlignmentRequirement key {key.A:X16} {key.B}, value {value}");

    public void dmpGetCastingHelper(in Agnostic_GetCastingHelper key, uint value)
        => Out.Write($"GetCastingHelper key cls-{key.hClass:X16}, thw-{key.fThrowing}, value res-{value}");

    public void dmpEmbedModuleHandle(ulong key, DLDL value)
        => Out.Write($"EmbedModuleHandle key mod-{key:X16}, value pp-{value.A:X16} res-{value.B:X16}");

    public void dmpEmbedClassHandle(ulong key, DLDL value)
        => Out.Write($"EmbedClassHandle key cls-{key:X16}, value pp-{value.A:X16} res-{value.B:X16}");

    public void dmpPInvokeMarshalingRequired(in MethodOrSigInfoValue key, uint value)
        => Out.Write($"PInvokeMarshalingRequired key mth-{key.method:X16} scp-{key.scope:X16} " +
            $"sig-{SpmiDumpHelper.DumpPSig(key.pSig_Index, key.cbSig, PInvokeMarshalingRequired)}, value res-{value}");

    public void dmpGetUnmanagedCallConv(in MethodOrSigInfoValue key, DD value)
        => Out.Write($"GetUnmanagedCallConv key mth-{key.method:X16} scp-{key.scope:X16} " +
            $"sig-{SpmiDumpHelper.DumpPSig(key.pSig_Index, key.cbSig, GetUnmanagedCallConv)}, value res-{value.A},{value.B}");

    public void dmpFindSig(in Agnostic_FindSig key, in Agnostic_CORINFO_SIG_INFO value)
    {
        Out.Write($"FindSig key module-{key.module:X16} sigTOK-{key.sigTOK:X8} context-{key.context:X16}");
        Out.Write($", value-{SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(value, FindSig, SigInstHandleMap)}");
    }

    public void dmpGetEEInfo(uint key, in Agnostic_CORINFO_EE_INFO value)
        => Out.Write($"GetEEInfo key {key}, value icfi{{sz-{value.inlinedCallFrameInfo.size} " +
            $"sz-witharg-{value.inlinedCallFrameInfo.sizeWithSecretStubArg} ofl-{value.inlinedCallFrameInfo.offsetOfFrameLink} " +
            $"ocsp-{value.inlinedCallFrameInfo.offsetOfCallSiteSP} ocsfp-{value.inlinedCallFrameInfo.offsetOfCalleeSavedFP} " +
            $"oct-{value.inlinedCallFrameInfo.offsetOfCallTarget} ora-{value.inlinedCallFrameInfo.offsetOfReturnAddress} " +
            $"ossa-{value.inlinedCallFrameInfo.offsetOfSecretStubArg} osap-{value.inlinedCallFrameInfo.offsetOfSPAfterProlog}}} " +
            $"otf-{value.offsetOfThreadFrame} ogcs-{value.offsetOfGCState} odi-{value.offsetOfDelegateInstance} " +
            $"odft-{value.offsetOfDelegateFirstTarget} srpf-{value.sizeOfReversePInvokeFrame} osps-{value.osPageSize} " +
            $"muono-{value.maxUncheckedOffsetForNullObject} tabi-{value.targetAbi} osType-{value.osType}");

    public void dmpGetAsyncInfo(uint key, in Agnostic_CORINFO_ASYNC_INFO value)
        => Out.Write($"GetAsyncInfo key {key} value contClsHnd-{value.continuationClsHnd:X16} contNextFldHnd-{value.continuationNextFldHnd:X16} " +
            $"contResumeInfoFldHnd-{value.continuationResumeInfoFldHnd:X16} contStateFldHnd-{value.continuationStateFldHnd:X16} " +
            $"contFlagsFldHnd-{value.continuationFlagsFldHnd:X16}");

    public void dmpGetWasmWellKnownGlobals(uint key, in Agnostic_CORINFO_WASM_WELLKNOWN_GLOBALS value)
        => Out.Write($"GetWasmWellKnownGlobals key {key} value stackPointer-{value.stackPointer:X16} imageBase-{value.imageBase:X16} " +
            $"tableBase-{value.tableBase:X16} asyncContinuation-{value.asyncContinuation:X16}");

    public void dmpGetAwaitReturnCall(ulong key, in Agnostic_GetAwaitReturnCallResult value)
        => Out.Write($"GetAwaitReturnCall key {key:X16} value methodHnd-{value.methodHnd:X16} contextHandle-{value.contextHandle:X16} instArg " +
            $"{SpmiDumpHelper.DumpAgnostic_CORINFO_LOOKUP(value.instArg)}");

    public void dmpGetAwaitAwaiterInContinuationCall(in Agnostic_GetAwaitAwaiterInContinuationCall key, in Agnostic_GetAwaitReturnCallResult value)
        => Out.Write($"GetAwaitAwaiterInContinuationCall caller-{key.callerHnd:X16} " +
            $"rt{{{SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.ResolvedToken)}}} unsafe-{key.isUnsafe} methodHnd-{value.methodHnd:X16} " +
            $"contextHandle-{value.contextHandle:X16} instArg {SpmiDumpHelper.DumpAgnostic_CORINFO_LOOKUP(value.instArg)}");

    public void dmpGetGSCookie(uint key, DLDL value)
        => Out.Write($"GetGSCookie key 0, value pCookieVal-{value.A:X16} ppCookieVal-{value.B:X16}");

    public void dmpGetOSRInfo(uint key, in Agnostic_GetOSRInfo value)
    {
        // todo - dump patchpoint info?
        Out.Write($"GetOSRInfo key {key}, value patchpointInfo-{value.index} {{...}} iloffset-{value.ilOffset}\n");
    }

    public void dmpGetClassModuleIdForStatics(ulong key, in Agnostic_GetClassModuleIdForStatics value)
        => Out.Write($"GetClassModuleIdForStatics key cls-{key:X16}, value mod-{value.Module:X16} pp-{value.pIndirection:X16} " +
            $"res-{value.result:X16}");

    public void dmpGetIsClassInitedFlagAddress(ulong key, in Agnostic_GetIsClassInitedFlagAddress value)
        => Out.Write($"GetIsClassInitedFlagAddress key hnd-{key:X16}, value addr-{value.addr.handle:X16}, result-{value.result}");

    public void dmpGetStaticBaseAddress(DLD key, in Agnostic_GetStaticBaseAddress value)
        => Out.Write($"GetStaticBaseAddress key hnd-{key.A:X16}, value addr-{value.addr.handle:X16}, result-{value.result}");

    public void dmpGetThreadTLSIndex(uint key, DLD value)
        => Out.Write($"GetThreadTLSIndex key 0, value ppIndirection-{value.A:X16} result-{value.B:X8}");

    public void dmpGetAddrOfCaptureThreadGlobal(uint key, DLDL value)
        => Out.Write($"GetAddrOfCaptureThreadGlobal key {key}, value ppi-{value.A:X16} res-{value.B:X16}");

    public void dmpGetClassStaticDynamicInfo(ulong key, DLD value)
        => Out.Write($"GetClassStaticDynamicInfo key cls-{key:X16}, value pp-{value.A:X16} res-{value.B}");

    public void dmpGetClassThreadStaticDynamicInfo(ulong key, DLD value)
        => Out.Write($"GetClassThreadStaticDynamicInfo key cls-{key:X16}, value pp-{value.A:X16} res-{value.B}");

    public void dmpGetLocationOfThisType(ulong key, in Agnostic_CORINFO_LOOKUP_KIND value)
        => Out.Write($"GetLocationOfThisType key ftn-{key:X16}, value {SpmiDumpHelper.DumpAgnostic_CORINFO_LOOKUP_KIND(value)}");

    public void dmpGetDelegateCtor(in Agnostic_GetDelegateCtorIn key, in Agnostic_GetDelegateCtorOut value)
        => Out.Write($"GetDelegateCtor key ftn-{key.methHnd:X16} cls-{key.clsHnd:X16} tftn-{key.targetMethodHnd:X16}, value " +
            $"pm-{value.CtorData.pMethod:X16} a3-{value.CtorData.pArg3:X16} a4-{value.CtorData.pArg4:X16} a5-{value.CtorData.pArg5:X16} " +
            $"res-{value.result:X16}");

    public void dmpGetFunctionFixedEntryPoint(ulong key, in Agnostic_CORINFO_CONST_LOOKUP value)
        => Out.Write($"GetFunctionFixedEntryPoint key ftn-{key:X16}, value {SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(value)}");

    public void dmpGetFieldInClass(DLD key, ulong value)
        => Out.Write($"GetFieldInClass key cls-{key.A:X16} ind-{key.B}, value {value:X16}");

    public void dmpGetFieldType(DLDL key, DLD value)
        => Out.Write($"GetFieldType key fld-{key.A:X16} cls-{key.B:X16}, value ch-{value.A:X16} cit-{value.B}({toString_CorInfoType(value.B)})");

    public void dmpSatisfiesMethodConstraints(DLDL key, uint value)
        => Out.Write($"SatisfiesMethodConstraints key cls-{key.A:X16} ftn-{key.B:X16}, value res-{value}");

    public void dmpGetStringLiteral(DLDDD key, DD value)
    {
        Out.Write($"GetStringLiteral key mod-{key.A:X16} tok-{key.B:X8}, bufSize-{key.C}, startIndex-{key.D}, len-{value.A}");
        GetStringLiteral.Unlock();
    }

    public void dmpCanCast(DLDL key, uint value)
        => Out.Write($"CanCast key chd-{key.A:X16} par-{key.B:X16}, value res-{value}");

    public void dmpGetChildType(ulong key, DLD value)
        => Out.Write($"GetChildType key cls-{key:X16}, value clsr-{value.A:X16} cit-{value.B}({toString_CorInfoType(value.B)})");

    public void dmpGetArrayInitializationData(DLD key, ulong value)
        => Out.Write($"GetArrayInitializationData key field-{key.A:X16} size-{key.B:X8}, value result-{value:X16}");

    public void dmpGetAddressOfPInvokeTarget(ulong key, DLD value)
        => Out.Write($"GetAddressOfPInvokeTarget key ftn-{key:X16}, value addr-{value.A:X16} at-{value.B}");

    public void dmpGetMethodHash(ulong key, uint value)
        => Out.Write($"GetMethodHash key {key:X16}, value {value}");

    public void dmpCanTailCall(in Agnostic_CanTailCall key, uint value)
        => Out.Write($"CanTailCall key clr-{key.callerHnd:X16} dcle-{key.declaredCalleeHnd:X16} ecle-{key.exactCalleeHnd:X16} " +
            $"pfx-{key.fIsTailPrefix}, value res-{value}");

    public void dmpIsDelegateCreationAllowed(DLDL key, uint value)
        => Out.Write($"IsDelegateCreationAllowed key delegateHnd-{key.A:X16} calleeHnd-{key.B:X16} result-{value:X8}");

    public void dmpFindCallSiteSig(in Agnostic_FindCallSiteSig key, in Agnostic_CORINFO_SIG_INFO value)
    {
        Out.Write($"dmpFindCallSiteSig key module-{key.module:X16} methTok-{key.methTok:X8} context-{key.context:X16}");
        Out.Write($", value-{SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(value, FindCallSiteSig, SigInstHandleMap)}");
    }

    public void dmpGetVarArgsHandle(in GetVarArgsHandleValue key, DLDL value)
    {
        Out.Write($"GetVarArgsHandle key sig-{SpmiDumpHelper.DumpPSig(key.pSig_Index, key.cbSig, GetVarArgsHandle)} scope-{key.scope:X16} " +
            $"token-{key.token:X8} methHnd-{key.methHnd:X16}");
        Out.Write($", value ppIndirection-{value.A:X16} result-{value.B:X16}");
    }

    public void dmpGetFieldThreadLocalStoreID(ulong key, DLD value)
        => Out.Write($"GetFieldThreadLocalStoreID key field-{key:X16}, value ppIndirection-{value.A:X16} result-{value.B:X8}");

    public void dmpIsMoreSpecificType(DLDL key, uint value)
        => Out.Write($"IsMoreSpecificType key cls1-{key.A:X16} cls2-{key.B:X16}, value {value}");

    public void dmpIsExactType(ulong key, uint value)
        => Out.Write($"IsExactType key cls-{key:X16}, value res-{value}");

    public void dmpIsGenericType(ulong key, uint value)
        => Out.Write($"IsGenericType key cls-{key:X16}, value res-{(int)value}");

    public void dmpIsNullableType(ulong key, uint value)
        => Out.Write($"IsNullableType key cls-{key:X16}, value res-{(int)value}");

    public void dmpIsEnum(ulong key, DLD value)
        => Out.Write($"IsEnum key cls-{key:X16}, value underlyingType-{value.A:X16} result-{value.B}");

    public void dmpGetCookieForInterpreterCalliSig(in GetCookieForInterpreterCalliSigValue key, DLDL value)
        => Out.Write("GetCookieForInterpreterCalliSig NYI");

    public void dmpErrorList(uint key, uint value)
        => Out.Write("ErrorList NYI");

    public void dmpGetProfilingHandle(uint key, in Agnostic_GetProfilingHandle value)
        => Out.Write($"GetProfilingHandle key {key}, value bHookFtn-{value.bHookFunction} profHnd-{value.ProfilerHandle:X16} " +
            $"bIndHnd-{value.bIndirectedHandles}");

    public void dmpEmbedFieldHandle(ulong key, DLDL value)
        => Out.Write("EmbedFieldHandle NYI");

    public void dmpCompareTypesForCast(DLDL key, uint value)
        => Out.Write($"CompareTypesForCast key fromClass={key.A:X16}, toClass={key.B:x16}, result={(int)value}");

    public void dmpCompareTypesForEquality(DLDL key, uint value)
        => Out.Write($"CompareTypesForEquality key cls1={key.A:X16}, cls2={key.B:x16}, result={(int)value}");

    public void dmpGetWasmLowering(ulong key, uint value)
        => Out.Write($"GetWasmLowering key structHnd-{key:X16}, value {(int)value} ");

    public void dmpGetRelocTypeHint(ulong key, uint value)
        => Out.Write($"GetRelocTypeHint key tgt-{key:X16}, value hint-{value}");

    public void dmpGetAddressAlignment(ulong key, uint value)
        => Out.Write($"GetAddressAlignment key addr-{key:X16}, value align-{value}");

    public void dmpGetExpectedTargetArchitecture(uint key, uint result)
        => Out.Write($"GetExpectedTargetArchitecture key {key}, res {result}");

    public void dmpDoesFieldBelongToClass(DLDL key, uint value)
        => Out.Write($"DoesFieldBelongToClass key fld={key.A:X16}, cls={key.B:x16}, result={(int)value}");

    public void dmpGetTypeInstantiationArgument(DLD key, ulong value)
    {
        Out.Write($"GetTypeInstantiationArgument key - classNonNull-{key.A}, index-{key.B}, value NonNull-{value}");
        GetTypeInstantiationArgument.Unlock();
    }

    public void dmpGetMethodInstantiationArgument(DLD key, ulong value)
    {
        Out.Write($"GetMethodInstantiationArgument key - methodNonNull-{key.A}, index-{key.B}, value NonNull-{value}");
        GetMethodInstantiationArgument.Unlock();
    }

    public void dmpPrintObjectDescription(ulong key, in Agnostic_PrintResult value)
    {
        dmpPrint("PrintObjectDescription", PrintObjectDescription, key, value);
    }

    public void dmpPrintClassName(ulong key, in Agnostic_PrintResult value)
    {
        dmpPrint("PrintClassName", PrintClassName, key, value);
    }

    public void dmpPrintFieldName(ulong key, in Agnostic_PrintResult value)
    {
        dmpPrint("PrintFieldName", PrintFieldName, key, value);
    }

    public void dmpPrintMethodName(ulong key, in Agnostic_PrintResult value)
    {
        dmpPrint("PrintMethodName", PrintMethodName, key, value);
    }

    public void dmpGetTailCallHelpers(in Agnostic_GetTailCallHelpers key, in Agnostic_CORINFO_TAILCALL_HELPERS value)
    {
        Out.Write($"GetTailCallHelpers key callToken-{SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.callToken)} " +
            $"sig-{SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(key.sig, GetTailCallHelpers, SigInstHandleMap)} flg-{key.flags:X8}");
        Out.Write($", value result-{((value.result != 0) ? "true" : "false")} flg-{value.flags:X8} hStoreArgs-{value.hStoreArgs:X16} " +
            $"hCallTarget-{value.hCallTarget:X16} hDispatcher-{value.hDispatcher:X16}");
    }

    public void dmpGetAsyncResumptionStub(uint key, in DLDL value)
        => Out.Write($"GetAsyncResumptionStub key-{key}, hnd-{value.A:X16}, entrypoint-{value.B:X16}");

    public void dmpGetContinuationType(in Agnostic_GetContinuationTypeIn key, ulong value)
        => Out.Write($"GetContinuationType dataSize-{key.dataSize:X16}, objRefs-{key.objRefs}, handle-{value:X16}");

    public void dmpUpdateEntryPointForTailCall(in Agnostic_CORINFO_CONST_LOOKUP origEntryPoint, in Agnostic_CORINFO_CONST_LOOKUP newEntryPoint)
        => Out.Write($"UpdateEntryPointForTailcall orig={SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(origEntryPoint)} " +
            $"new={SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(newEntryPoint)}");

    public void dmpGetMethodDefFromMethod(ulong key, uint value)
        => Out.Write($"GetMethodDefFromMethod key ftn-{key:X16}, value res-{value}");

    public void dmpGetArrayRank(ulong key, uint value)
        => Out.Write($"GetArrayRank key {key:X16}, value {value}");

    public void dmpGetArrayIntrinsicID(ulong key, uint value)
        => Out.Write($"GetArrayIntrinsicID key {key:X16}, value {value}");

    public void dmpIsFieldStatic(ulong key, uint value)
        => Out.Write($"IsFieldStatic key {key:X16}, value {value}");

    public void dmpGetArrayOrStringLength(ulong key, uint value)
        => Out.Write($"GetArrayOrStringLength key {key:X16}, value {value}");

    public void dmpGetSpecialCopyHelper(ulong key, ulong value)
        => Out.Write($"getSpecialCopyHelper key {key:X16}, value {value:X16}");

    public void dmpGetWasmTypeSymbol(in Agnostic_GetWasmTypeSymbol key, ulong value)
        => Out.Write($"getWasmTypeSymbol types {key.types}, typesSize {key.typesSize}, value {value:X16}");

    public void dmpSigInstHandleMap(uint key, ulong value)
        => Out.Write($"SigInstHandleMap key {key}, value {value:X16}");

    public void dmpCanValueClassInstancePointerEscape(ulong key, uint value)
        => Out.Write($"CanValueClassInstancePointerEscape key ftn-{key:X16}, value res-{value}");
}
