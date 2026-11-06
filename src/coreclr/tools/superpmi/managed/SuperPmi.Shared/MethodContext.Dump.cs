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
        CFormat.Printf(Out, "GetClassAssemblyName cls-%016llX, value-%u '%s'", key, value, assemblyName);
        GetClassAssemblyName.Unlock();
    }

    public void dmpGetVars(ulong key, in Agnostic_GetVars value)
    {
        const int ILVarInfoSize = 12; // ICorDebugInfo::ILVarInfo: startOffset, endOffset, varNumber
        ReadOnlySpan<byte> vars = GetVars.GetBuffer(value.vars_offset);
        CFormat.Printf(Out, "GetVars key ftn-%016llX, value cVars-%u extendOthers-%u (", key, value.cVars, value.extendOthers);
        for (uint i = 0; i < value.cVars; i++)
        {
            int at = (int)i * ILVarInfoSize;
            CFormat.Printf(Out, "(%u %u %u %u)", i, U32(vars, at), U32(vars, at + 4), U32(vars, at + 8));
        }
        Out.Write(")");
        GetVars.Unlock();
    }

    public void dmpGetBoundaries(ulong key, in Agnostic_GetBoundaries value)
    {
        CFormat.Printf(Out, "GetBoundaries key ftn-%016llX, value cnt-%u imp-%u{", key, value.cILOffsets, value.implicitBoundaries);
        ReadOnlySpan<byte> bnd = GetBoundaries.GetBuffer(value.pILOffset_offset);
        for (uint i = 0; i < value.cILOffsets; i++)
        {
            CFormat.Printf(Out, "%u", U32(bnd, (int)i * 4));
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
        CFormat.Printf(Out, "GetMethodNameFromMetadata key - ftn-%016llX classNonNull-%u namespaceNonNull-%u maxEnclosingClassNames-%u, value meth-'%s', class-'%s', namespace-'%s'",
            key.ftn, key.className, key.namespaceName, key.maxEnclosingClassNames, methodName, className, namespaceName);

        for (uint i = 0; i < key.maxEnclosingClassNames; i++)
        {
            string enclosingClassName = GetMethodNameFromMetadata.GetString(U32(enclosingClassNameIndices, (int)i * 4));
            if (enclosingClassName != null)
                CFormat.Printf(Out, " enclosingClass%u-'%s'", i, enclosingClassName);
        }

        GetMethodNameFromMetadata.Unlock();
    }

    public void dmpGetJitFlags(uint key, DD value)
    {
        ReadOnlySpan<byte> jitflags = GetJitFlags.GetBuffer(value.A);
        ulong flags = U64(jitflags, 0);
        CFormat.Printf(Out, "GetJitFlags key %u sizeInBytes-%u jitFlags-%016llX instructionSetFlags-", key, value.B, flags);

        var isaFlags = new ulong[InstructionFlagsFieldCount];
        for (int i = 0; i < isaFlags.Length; i++)
            isaFlags[i] = U64(jitflags, 8 * (i + 1));
        for (int i = isaFlags.Length - 1; i >= 0; i--)
            CFormat.Printf(Out, "%016llX", isaFlags[i]);

        Out.Write(" (");
        for (uint flag = 0; flag < 64; flag++)
        {
            if ((flags & (1UL << (int)flag)) != 0)
                CFormat.Printf(Out, " %s", JitFlagNames.CorJitFlagToString(flag));
        }
        for (uint isa = 0; isa < 64 * isaFlags.Length; isa++)
        {
            if ((isaFlags[isa / 64] & (1UL << (int)(isa % 64))) != 0)
                CFormat.Printf(Out, " %s", JitFlagNames.InstructionSetToString(isa));
        }
        Out.Write(" )");
        GetJitFlags.Unlock();
    }

    public void dmpGetFieldInfo(in Agnostic_GetFieldInfo key, in Agnostic_CORINFO_FIELD_INFO value)
    {
        CFormat.Printf(Out, "GetFieldInfo key ch-%016llX flg-%08X rt{%s}", key.callerHandle, key.flags,
            SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.ResolvedToken));

        CFormat.Printf(Out, ", value fa-%u fflg-%08X hlp-%u off-%u fT-%u(%s) sT-%016llX aa-%u hnum-%u na-%u {", value.fieldAccessor,
            value.fieldFlags, value.helper, value.offset, value.fieldType, toString_CorInfoType(value.fieldType),
            value.structType, value.accessAllowed, value.accessCalloutHelper.helperNum, value.accessCalloutHelper.numArgs);

        for (int i = 0; i < 4; i++)
        {
            Agnostic_CORINFO_HELPER_ARG arg = value.accessCalloutHelper.args[i];
            string format = arg.argType switch
            {
                1 => "{%u: fld-%016llX}",
                2 => "{%u: mth-%016llX}",
                3 => "{%u: cls-%016llX}",
                4 => "{%u: mod-%016llX}",
                5 => "{%u: const-%016llX}",
                _ => null,
            };
            if (format == null)
                CFormat.Printf(Out, "{%u: illegal}", i);
            else
                CFormat.Printf(Out, format, i, arg.constant);
        }
        CFormat.Printf(Out, " fl %s}", SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(value.fieldLookup));
    }

    public void dmpGetClassGClayout(ulong key, in Agnostic_GetClassGClayout value)
    {
        CFormat.Printf(Out, "GetClassGCLayout key %016llX, value len %u cnt %u {", key, value.len, value.valCount);
        if (value.gcPtrs_Index != uint.MaxValue)
        {
            ReadOnlySpan<byte> ptr = GetClassGClayout.GetBuffer(value.gcPtrs_Index);
            for (uint i = 0; i < value.len; i++)
            {
                CFormat.Printf(Out, "0x%02x", ptr[(int)i]);
                if (i + 1 < value.len)
                    Out.Write(",");
            }
            GetClassGClayout.Unlock();
        }
        Out.Write("}");
    }

    public void dmpCanAccessClass(in Agnostic_CanAccessClassIn key, in Agnostic_CanAccessClassOut value)
    {
        CFormat.Printf(Out, "CanAccessClass key rt{%s}, callerHandle %016llX\n",
            SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.ResolvedToken), key.callerHandle);
        CFormat.Printf(Out, ", value hnum-%u na-%u {", value.AccessHelper.helperNum, value.AccessHelper.numArgs);
        for (int i = 0; i < 4; i++)
            CFormat.Printf(Out, "{%016llX %u}", value.AccessHelper.args[i].constant, value.AccessHelper.args[i].argType);
        CFormat.Printf(Out, "} res-%u", value.result);
    }

    public void dmpGetTypeLayout(ulong key, in Agnostic_GetTypeLayoutResult value)
    {
        CFormat.Printf(Out, "GetTypeLayout key type-%016llX value result=%d numNodes=%d", key, value.result, value.numNodes);
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
        CFormat.Printf(Out, "%zu: parent %u offset %u size %u type %s numFields %u hasSignificantPadding %s simdTypeHnd %016llX diagFieldHnd %016llX",
            index, node.parent, node.offset, node.size, toString_CorInfoType(node.type), node.numFields,
            node.hasSignificantPadding != 0 ? "yes" : "no", node.simdTypeHnd, node.diagFieldHnd);

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
        CFormat.Printf(Out, "AllocPgoInstrumentationBySchema key ftn-%016llX, value res-%08X addr-%016llX cnt-%u schema{\n",
            key, value.result, value.instrumentationDataAddress, value.countSchemaItems);

        if (value.countSchemaItems > 0)
        {
            ReadOnlySpan<byte> pBuf = AllocPgoInstrumentationBySchema.GetBuffer(value.schema_index);
            Out.Write("\n");
            for (uint i = 0; i < value.countSchemaItems; i++)
            {
                Agnostic_PgoInstrumentationSchema s = PgoSchemaAt(pBuf, i);
                CFormat.Printf(Out, " %u-{Offset %016llX ILOffset %u Kind %u(0x%x) Count %u Other %u}\n",
                    i, s.Offset, s.ILOffset, s.InstrumentationKind, s.InstrumentationKind, s.Count, s.Other);
            }
            AllocPgoInstrumentationBySchema.Unlock();
        }
        Out.Write("}");
    }

    public void dmpGetPgoInstrumentationResults(ulong key, in Agnostic_GetPgoInstrumentationResults value)
    {
        CFormat.Printf(Out, "GetPgoInstrumentationResults key ftn-%016llX, value res-%08X schemaCnt-%u profileBufSize-%u source-%u dynamic-%u schema{",
            key, value.result, value.countSchemaItems, value.dataByteCount, value.pgoSource, value.dynamicPgo);

        if (value.countSchemaItems > 0)
        {
            ReadOnlySpan<byte> pBuf = GetPgoInstrumentationResults.GetBuffer(value.schema_index);
            ReadOnlySpan<byte> data = GetPgoInstrumentationResults.GetBuffer(value.data_index);

            Out.Write("\n");
            for (uint i = 0; i < value.countSchemaItems; i++)
            {
                Agnostic_PgoInstrumentationSchema s = PgoSchemaAt(pBuf, i);
                CFormat.Printf(Out, " %u-{Offset %016llX ILOffset %u Kind %u(0x%x) Count %u Other %u Data ",
                    i, s.Offset, s.ILOffset, s.InstrumentationKind, s.InstrumentationKind, s.Count, s.Other);

                int at = (int)s.Offset;
                switch ((PgoInstrumentationKind)s.InstrumentationKind)
                {
                    case PgoInstrumentationKind.BasicBlockIntCount:
                        CFormat.Printf(Out, "B %u", U32(data, at));
                        break;
                    case PgoInstrumentationKind.BasicBlockLongCount:
                        CFormat.Printf(Out, "B %llu", U64(data, at));
                        break;
                    case PgoInstrumentationKind.EdgeIntCount:
                        CFormat.Printf(Out, "E %u", U32(data, at));
                        break;
                    case PgoInstrumentationKind.EdgeLongCount:
                        CFormat.Printf(Out, "E %llu", U64(data, at));
                        break;
                    case PgoInstrumentationKind.HandleHistogramIntCount:
                        CFormat.Printf(Out, "T %u", U32(data, at));
                        break;
                    case PgoInstrumentationKind.HandleHistogramLongCount:
                        CFormat.Printf(Out, "T %llu", U64(data, at));
                        break;
                    case PgoInstrumentationKind.HandleHistogramTypes:
                    case PgoInstrumentationKind.HandleHistogramMethods:
                        for (uint j = 0; j < s.Count; j++)
                            CFormat.Printf(Out, "[%u] %016llX ", j, ReadPointer(data, at + (int)j * IntPtr.Size));
                        break;
                    case PgoInstrumentationKind.ValueHistogramIntCount:
                        CFormat.Printf(Out, "V %u", U32(data, at));
                        break;
                    case PgoInstrumentationKind.ValueHistogramLongCount:
                        CFormat.Printf(Out, "V %llu", U64(data, at));
                        break;
                    case PgoInstrumentationKind.ValueHistogram:
                        for (uint j = 0; j < s.Count; j++)
                            CFormat.Printf(Out, "[%u] %08X ", j, U32(data, at + (int)j * sizeof(uint)));
                        break;
                    case PgoInstrumentationKind.GetLikelyClass:
                    case PgoInstrumentationKind.GetLikelyMethod:
                        // (N)umber, (L)ikelihood, (H)andle
                        CFormat.Printf(Out, "N %u L %u H %016llX", s.Other >> 8, s.Other & 0xFF, ReadPointer(data, at));
                        break;
                    default:
                        Out.Write("?");
                        break;
                }

                Out.Write("}\n");
            }
            GetPgoInstrumentationResults.Unlock();
        }
        CFormat.Printf(Out, "} data_index-%u", value.data_index);
    }

    // Instrumentation data holds handles in the collecting host's pointer size.
    private static ulong ReadPointer(ReadOnlySpan<byte> data, int offset)
        => IntPtr.Size == 8 ? U64(data, offset) : U32(data, offset);

    public void dmpGetSystemVAmd64PassStructInRegisterDescriptor(ulong key, in Agnostic_GetSystemVAmd64PassStructInRegisterDescriptor value)
    {
        CFormat.Printf(Out, "GetSystemVAmd64PassStructInRegisterDescriptor key structHnd-%016llX, value passInReg-%u 8bCount-%u", key,
            value.passedInRegisters, value.eightByteCount);
        for (int i = 0; i < 2; i++)
        {
            CFormat.Printf(Out, " 8bClass[%u]-%u 8bSz[%u]-%u 8bOff[%u]-%u", i, value.eightByteClassifications[i], i,
                value.eightByteSizes[i], i, value.eightByteOffsets[i]);
        }
        CFormat.Printf(Out, " result %u", value.result);
    }

    public void dmpGetSwiftLowering(ulong key, in Agnostic_GetSwiftLowering value)
    {
        CFormat.Printf(Out, "GetSwiftLowering key structHnd-%016llX, value byReference-%u numLoweredElements-%u", key,
            value.byReference, value.numLoweredElements);
        if (value.byReference == 0)
        {
            for (int i = 0; i < value.numLoweredElements; i++)
                CFormat.Printf(Out, " [%zu] %u", i, value.loweredElements[i]);
        }
    }

    public void dmpGetFpStructLowering(ulong key, in Agnostic_GetFpStructLowering value)
    {
        CFormat.Printf(Out, "GetFpStructLowering key structHnd-%016llX, value byIntegerCallConv-%u numLoweredElements-%u", key,
            value.byIntegerCallConv, value.numLoweredElements);
        if (value.byIntegerCallConv == 0)
        {
            for (int i = 0; i < value.numLoweredElements; i++)
                CFormat.Printf(Out, " [%zu] %u", i, value.loweredElements[i]);
        }
    }

    public void dmpGetClassNameFromMetadata(DLD key, DD value)
    {
        string className = GetClassNameFromMetadata.GetString(value.A);
        string namespaceName = GetClassNameFromMetadata.GetString(value.B);
        CFormat.Printf(Out, "GetClassNameFromMetadata key - classNonNull-%llu namespaceNonNull-%u, value class-'%s', namespace-'%s'",
            key.A, key.B, className, namespaceName);
        GetClassNameFromMetadata.Unlock();
    }

    public void dmpPrint(string name, LightWeightMapBuffer buffer, ulong key, in Agnostic_PrintResult value)
    {
        CFormat.Printf(Out, "%s key hnd-%016llX, stringBufferSize-%u, requiredBufferSize-%u", name, key, value.stringBufferSize, value.requiredBufferSize);
        buffer.Unlock();
    }

    public void dmpCheckMethodModifier(in Agnostic_CheckMethodModifier key, uint value)
    {
        CFormat.Printf(Out, "CheckMethodModifier key, ftn-%016llX mod-'%s' opt-%u, value res-%u", key.hMethod,
            CheckMethodModifier.GetString(key.modifier), key.fOptional, value);
        CheckMethodModifier.Unlock();
    }

    public void dmpGetIntConfigValue(in Agnostic_ConfigIntInfo key, uint value)
    {
        string name = GetIntConfigValue.GetString(key.nameIndex);
        CFormat.Printf(Out, "GetIntConfigValue name %s, default value %d, value %d", name, key.defaultValue, value);
        GetIntConfigValue.Unlock();
    }

    public void dmpGetStringConfigValue(uint nameIndex, uint resultIndex)
    {
        string name = GetStringConfigValue.GetString(nameIndex);
        string result = GetStringConfigValue.GetString(resultIndex);
        CFormat.Printf(Out, "GetStringConfigValue name %s, result %s", name, result);
        GetStringConfigValue.Unlock();
    }

    public void dmpCompileMethod(uint key, in Agnostic_CompileMethod value)
        => CFormat.Printf(Out, "CompileMethod key %u, value ftn-%016llX scp-%016llX ilo-%u ils-%u ms-%u ehc-%u opt-%u rk-%u args-%s locals-%s flg-%08X os-%u",
            key, value.info.ftn, value.info.scope, value.info.ILCode_offset, value.info.ILCodeSize, value.info.maxStack,
            value.info.EHcount, value.info.options, value.info.regionKind,
            SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(value.info.args, CompileMethod, SigInstHandleMap),
            SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(value.info.locals, CompileMethod, SigInstHandleMap),
            value.flags, value.os);

    public void dmpGetMethodClass(ulong key, ulong value)
        => CFormat.Printf(Out, "GetMethodClass key %016llX, value %016llX", key, value);

    public void dmpGetClassAttribs(ulong key, uint value)
        => CFormat.Printf(Out, "GetClassAttribs key %016llX, value %08X (%s)", key, value, SpmiDumpHelper.DumpCorInfoFlag(value));

    public void dmpIsIntrinsic(ulong key, uint value)
        => CFormat.Printf(Out, "IsIntrinsic key ftn-%016llX, value res-%u", key, value);

    public void dmpNotifyMethodInfoUsage(ulong key, uint value)
        => CFormat.Printf(Out, "NotifyMethodInfoUsage key ftn-%016llX, value res-%u", key, value);

    public void dmpNotifyInstructionSetUsage(DD key, uint value)
        => CFormat.Printf(Out, "NotifyInstructionSetUsage key isa-%u, supported-%u, preserve-negative-dependency-%u, res-%u",
            key.A, key.B & 1, (key.B >> 1) & 1, value);

    public void dmpGetMethodAttribs(ulong key, uint value)
        => CFormat.Printf(Out, "GetMethodAttribs key %016llX, value %08X (%s)", key, value, SpmiDumpHelper.DumpCorInfoFlag(value));

    public void dmpInitClass(in Agnostic_InitClass key, uint value)
        => CFormat.Printf(Out, "InitClass key fld-%016llX meth-%016llX con-%016llX, value res-%u", key.field, key.method,
            key.context, value);

    public void dmpCanInline(DLDL key, in Agnostic_CanInline value)
        => CFormat.Printf(Out, "CanInline key - callerHnd-%016llX calleeHnd-%016llX, value result-%u exceptionCode-%08X",
            key.A, key.B, value.result, value.exceptionCode);

    public void dmpResolveToken(in Agnostic_CORINFO_RESOLVED_TOKENin key, in ResolveTokenValue value)
        => CFormat.Printf(Out, "ResolveToken key: %s, value: %s excp-%08X",
            SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKENin(key),
            SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKENout(value.tokenOut),
            value.exceptionCode);

    public void dmpGetCallInfo(in Agnostic_GetCallInfo key, in Agnostic_CORINFO_CALL_INFO value)
        => CFormat.Printf(Out, "GetCallInfo key rt{%s} crt{%s} ch-%016llX flg-%08X, value mth-%016llX, mf-%08X (%s) cf-%08X (%s) sig-%s aa-%u cch{hn-%u na-%u (TODO: dump callsiteCalloutHelper.args)} tt-%u k-%u (%s) nic-%u (%s) ch-%016llX ecnrl-%u (%s) stubLookup{%s} ipl{at-%08X (%s) hnd-%016llX} excp-%08X",
            // input
            SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.ResolvedToken),
            SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.ConstrainedResolvedToken),
            key.callerHandle,
            key.flags,
            // output
            value.hMethod,
            value.methodFlags,
            SpmiDumpHelper.DumpCorInfoFlag(value.methodFlags),
            value.classFlags,
            SpmiDumpHelper.DumpCorInfoFlag(value.classFlags),
            SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(value.sig, GetCallInfo, SigInstHandleMap),
            value.accessAllowed,
            value.callsiteCalloutHelper.helperNum,
            value.callsiteCalloutHelper.numArgs,
            value.thisTransform,
            value.kind,
            toString_CORINFO_CALL_KIND(value.kind),
            value.nullInstanceCheck,
            (value.nullInstanceCheck != 0) ? "true" : "false",
            value.contextHandle,
            value.exactContextNeedsRuntimeLookup,
            (value.exactContextNeedsRuntimeLookup != 0) ? "true" : "false",
            SpmiDumpHelper.DumpAgnostic_CORINFO_LOOKUP(value.stubLookup),
            value.instParamLookup.accessType,
            toString_InfoAccessType(value.instParamLookup.accessType),
            value.instParamLookup.handle,
            value.exceptionCode);

    public void dmpExpandRawHandleIntrinsic(in Agnostic_ExpandRawHandleIntrinsic key, in Agnostic_CORINFO_GENERICHANDLE_RESULT result)
        => CFormat.Printf(Out, "ExpandRawHandleIntrinsic key: %s, value %s cth-%016llx ht-%u",
            SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKENin(key.ResolvedToken),
            SpmiDumpHelper.DumpAgnostic_CORINFO_LOOKUP(result.lookup),
            result.compileTimeHandle,
            result.handleType);

    public void dmpIsIntrinsicType(ulong key, uint value)
        => CFormat.Printf(Out, "IsIntrinsicType key mth-%016llX, value intr-%u", key, value);

    public void dmpAsCorInfoType(ulong key, uint value)
        => CFormat.Printf(Out, "AsCorInfoType key cls-%016llX, value cit-%u(%s)", key, value, toString_CorInfoType(value));

    public void dmpIsValueClass(ulong key, uint value)
        => CFormat.Printf(Out, "IsValueClass key cls-%016llX, value res-%u", key, value);

    public void dmpIsByRefLike(ulong key, uint value)
        => CFormat.Printf(Out, "IsByRefLike key cls-%016llX, value res-%u", key, value);

    public void dmpIsSharedInst(ulong key, uint value)
        => CFormat.Printf(Out, "IsSharedInst key cls-%016llX, value res-%u", key, value);

    public void dmpGetClassSize(ulong key, uint val)
        => CFormat.Printf(Out, "GetClassSize key %016llX, value %u", key, val);

    public void dmpGetHeapClassSize(ulong key, uint val)
        => CFormat.Printf(Out, "GetHeapClassSize key %016llX, value %u", key, val);

    public void dmpCanAllocateOnStack(ulong key, uint val)
        => CFormat.Printf(Out, "CanAllocateOnStack key %016llX, value %u", key, val);

    public void dmpGetClassNumInstanceFields(ulong key, uint value)
        => CFormat.Printf(Out, "GetClassNumInstanceFields key cls-%016llX, value res-%u", key, value);

    public void dmpGetNewArrHelper(ulong key, uint value)
        => CFormat.Printf(Out, "GetNewArrHelper key cls-%016llX, value res-%u", key, value);

    public void dmpGetSharedCCtorHelper(ulong key, uint value)
        => CFormat.Printf(Out, "GetSharedCCtorHelper key cls-%016llX, value res-%u", key, value);

    public void dmpGetTypeForBox(ulong key, ulong value)
        => CFormat.Printf(Out, "GetTypeForBox key cls-%016llX, value res-%016llX", key, value);

    public void dmpGetBoxHelper(ulong key, uint value)
        => CFormat.Printf(Out, "GetBoxHelper key cls-%016llX, value res-%u", key, value);

    public void dmpGetBuiltinClass(uint key, ulong value)
        => CFormat.Printf(Out, "GetBuiltinClass key cls-%08X, value cls-%016llX", key, value);

    public void dmpGetTypeForPrimitiveValueClass(ulong key, uint value)
        => CFormat.Printf(Out, "GetTypeForPrimitiveValueClass key cls-%016llX, value cit-%u(%s)", key, value, toString_CorInfoType(value));

    public void dmpGetTypeForPrimitiveNumericClass(ulong key, uint value)
        => CFormat.Printf(Out, "GetTypeForPrimitiveNumericClass key cls-%016llX, value cit-%u(%s)", key, value,
            toString_CorInfoType(value));

    public void dmpGetParentType(ulong key, ulong value)
        => CFormat.Printf(Out, "GetParentType key cls-%016llX, value cls-%016llX", key, value);

    public void dmpIsSDArray(ulong key, uint value)
        => CFormat.Printf(Out, "IsSDArray key cls-%016llX, value res-%u", key, value);

    public void dmpGetFieldClass(ulong key, ulong value)
        => CFormat.Printf(Out, "GetFieldClass key %016llX, value %016llX", key, value);

    public void dmpGetFieldOffset(ulong key, uint value)
        => CFormat.Printf(Out, "GetFieldOffset key FLD-%016llX, value %08X", key, value);

    public void dmpGetUnBoxHelper(ulong key, uint value)
        => CFormat.Printf(Out, "GetUnBoxHelper key cls-%016llX, value res-%u", key, value);

    public void dmpGetRuntimeTypePointer(ulong key, ulong value)
        => CFormat.Printf(Out, "GetRuntimeTypePointer key cls-%016llX, value res-%016llX", key, value);

    public void dmpIsObjectImmutable(ulong key, uint value)
        => CFormat.Printf(Out, "IsObjectImmutable key obj-%016llX, value res-%u", key, value);

    public void dmpGetStringChar(DLD key, DD value)
        => CFormat.Printf(Out, "IsObjectImmutable key obj-%016llX, index-%u, value res-%u, val-%u", key.A, key.B, value.A, value.B);

    public void dmpGetObjectType(ulong key, ulong value)
        => CFormat.Printf(Out, "GetObjectType key obj-%016llX, value res-%016llX", key, value);

    public void dmpGetReadyToRunHelper(GetReadyToRunHelper_TOKENin key, GetReadyToRunHelper_TOKENout value)
    {
        CFormat.Printf(Out, "GetReadyToRunHelper key: tk{%s} id-%u",
               SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.ResolvedToken), key.id);
        CFormat.Printf(Out, ", value: lk{ %s } %u", SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(value.Lookup),
               value.result);
    }

    public void dmpGetReadyToRunDelegateCtorHelper(GetReadyToRunDelegateCtorHelper_TOKENIn key, Agnostic_CORINFO_LOOKUP value)
    {
        CFormat.Printf(Out, "GetReadyToRunDelegateCtorHelper key: method tk{%s} type-%016llX constraint-%08X",
               SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.TargetMethod), key.delegateType, key.targetConstraint);
        CFormat.Printf(Out, ", value: %s", SpmiDumpHelper.DumpAgnostic_CORINFO_LOOKUP(value));
    }

    public void dmpGetHelperFtn(uint key, Agnostic_GetHelperFtn value)
        => CFormat.Printf(Out, "GetHelperFtn key ftn-%u nativeEntrypoint {%s}, helperMethod-%016llX", key,
            SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(value.helperLookup),
            value.helperMethod);

    public void dmpGetJustMyCodeHandle(ulong key, DLDL value)
        => CFormat.Printf(Out, "GetJustMyCodeHandle key ftn-%016llX, value pp-%016llX, res-%016llX", key, value.A, value.B);

    public void dmpGetFunctionEntryPoint(DLD key, DLD value)
        => CFormat.Printf(Out, "GetFunctionEntryPoint key ftn-%016llX af-%08X, value add-%016llX at-%u", key.A, key.B, value.A, value.B);

    public void dmpConstructStringLiteral(DLD key, DLD value)
        => CFormat.Printf(Out, "ConstructStringLiteral key mod-%016llX tok-%08X, value pp-%016llX iat-%u", key.A, key.B, value.A, value.B);

    public void dmpConvertPInvokeCalliToCall(DLD key, DLDL value)
        => CFormat.Printf(Out, "ConvertPInvokeCalliToCall key mod-%016llX tok-%08X, value cls-%016llX meth-%016llX",
            key.A, key.B, value.A, value.B);

    public void dmpEmptyStringLiteral(uint key, DLD value)
        => CFormat.Printf(Out, "EmptyStringLiteral key %u, value pVal-%016llX res-%u", key, value.A, value.B);

    public void dmpGetArgType(in Agnostic_GetArgType_Key key, in Agnostic_GetArgType_Value value)
    {
        CFormat.Printf(Out, "GetArgType key flg-%08X na-%u %s %s msig-%016llX scp-%016llX arg-%016llX",
            key.flags, key.numArgs,
            SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INST_Element("", "cc", "ci", key.sigInst_classInstCount, key.sigInst_classInst_Index, SigInstHandleMap),
            SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INST_Element("", "mc", "mi", key.sigInst_methInstCount, key.sigInst_methInst_Index, SigInstHandleMap),
            key.methodSignature, key.scope, key.args);
        CFormat.Printf(Out, ", value result(cit)-%u(%s) vcType-%016llX excp-%08X", value.result, toString_CorInfoTypeWithMod(value.result), value.vcTypeRet, value.exceptionCode);
    }

    public void dmpGetExactClasses(DLD key, in Agnostic_GetExactClassesResult value)
        => CFormat.Printf(Out, "GetExactClasses key baseType-%016llX, maxExactCls-%u, value numClasses-%d",
            key.A, key.B, value.numClasses);

    public void dmpGetArgNext(ulong key, ulong value)
        => CFormat.Printf(Out, "GetArgNext key %016llX, value %016llX", key, value);

    public void dmpGetMethodSig(DLDL key, in Agnostic_CORINFO_SIG_INFO value)
        => CFormat.Printf(Out, "GetMethodSig key ftn-%016llX prt-%016llX, value-%s", key.A, key.B,
            SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(value, GetMethodSig, SigInstHandleMap));

    public void dmpGetArgClass(in Agnostic_GetArgClass_Key key, in Agnostic_GetArgClass_Value value)
    {
        CFormat.Printf(Out, "GetArgClass key %s %s msig-%016llX scp-%016llX args-%016llX",
            SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INST_Element("", "cc", "ci", key.sigInst_classInstCount, key.sigInst_classInst_Index, SigInstHandleMap),
            SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INST_Element("", "mc", "mi", key.sigInst_methInstCount, key.sigInst_methInst_Index, SigInstHandleMap),
            key.methodSignature, key.scope, key.args);
        CFormat.Printf(Out, ", value %016llX excp-%08X", value.result, value.exceptionCode);
    }

    public void dmpGetHFAType(ulong key, uint value)
        => CFormat.Printf(Out, "GetHFAType key %016llX, value %u ", key, value);

    public void dmpGetMethodInfo(DLDL key, in Agnostic_GetMethodInfo value)
    {
        if (value.result != 0)
        {
            CFormat.Printf(Out, "GetMethodInfo key ftn-%016llX ctx-%016llX, value res-%u ftn-%016llX scp-%016llX ilo-%u ils-%u ms-%u ehc-%u opt-%08X rk-%u args-%s locals-%s excp-%08X",
                key.A, key.B, value.result, value.info.ftn, value.info.scope, value.info.ILCode_offset, value.info.ILCodeSize,
                value.info.maxStack, value.info.EHcount, value.info.options, value.info.regionKind,
                SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(value.info.args, GetMethodInfo, SigInstHandleMap),
                SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(value.info.locals, GetMethodInfo, SigInstHandleMap),
                value.exceptionCode);
        }
        else
        {
            CFormat.Printf(Out, "GetMethodInfo key ftn-%016llX ctx-%016llX, value res-%u excp-%08X",
                key.A, key.B, value.result, value.exceptionCode);
        }
    }

    public void dmpHaveSameMethodDefinition(in DLDL key, uint value)
        => CFormat.Printf(Out, "HaveSameMethodDefinition key methHnd1-%016llX, methHnd2-%016llX, value %u", key.A, key.B, value);

    public void dmpGetTypeDefinition(ulong key, ulong value)
        => CFormat.Printf(Out, "GetTypeDefinition key cls-%016llX, value cls-%016llX", key, value);

    public void dmpGetNewHelper(ulong key, in DDD value)
        => CFormat.Printf(Out, "GetNewHelper key %016llX, hasSideEffects-%u, value res-%u, exceptionCode-%08X", key, value.A, value.B, value.C);

    public void dmpEmbedGenericHandle(in Agnostic_EmbedGenericHandle key, in Agnostic_CORINFO_GENERICHANDLE_RESULT value)
        => CFormat.Printf(Out, "EmbedGenericHandle key rt{%s} emb-%u, value %s cth-%016llX ht-%u",
            SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.ResolvedToken),
            key.fEmbedParent,
            SpmiDumpHelper.DumpAgnostic_CORINFO_LOOKUP(value.lookup),
            value.compileTimeHandle,
            value.handleType);

    public void dmpGetEHinfo(DLD key, in Agnostic_CORINFO_EH_CLAUSE value)
        => CFormat.Printf(Out, "GetEHinfo key ftn-%016llX ehn-%u, value flg-%u to-%u tl-%u ho-%u hl-%u ct-%u", key.A, key.B, value.Flags,
            value.TryOffset, value.TryLength, value.HandlerOffset, value.HandlerLength, value.ClassToken);

    public void dmpGetMethodVTableOffset(ulong key, DDD value)
        => CFormat.Printf(Out, "GetMethodVTableOffset key ftn-%016llX, value offi-%u, offa-%u. offr-%d", key, value.A, value.B, value.C);

    public void dmpResolveVirtualMethod(in Agnostic_ResolveVirtualMethodKey key, in Agnostic_ResolveVirtualMethodResult result)
    {
        CFormat.Printf(Out, "ResolveVirtualMethod key virtMethod-%016llX, objClass-%016llX, context-%016llX pResolvedTokenVirtualMethodNonNull-%08X pResolvedTokenVirtualMethod{%s}",
            key.virtualMethod,
            key.objClass,
            key.context,
            key.pResolvedTokenVirtualMethodNonNull,
            (key.pResolvedTokenVirtualMethodNonNull != 0) ? SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.pResolvedTokenVirtualMethod) : "???");
        CFormat.Printf(Out, ", value returnValue-%s, devirtMethod-%016llX, tokenLookupContext-%016llX, detail-%d, tokDvMeth{%s}, tokDvUnboxMeth{%s}, instParamLookup{%s}",
            (result.returnValue != 0) ? "true" : "false",
            result.devirtualizedMethod,
            result.tokenLookupContext,
            result.detail,
            (result.returnValue != 0) ? SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(result.resolvedTokenDevirtualizedMethod) : "???",
            (result.returnValue != 0) ? SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(result.resolvedTokenDevirtualizedUnboxedMethod) : "???",
            SpmiDumpHelper.DumpAgnostic_CORINFO_LOOKUP(result.instParamLookup));
    }

    public void dmpGetAsyncOtherVariant(ulong key, DLD value)
        => CFormat.Printf(Out, "GetAsyncOtherVariant ftn-%016llX, result-%016llX, variantIsThunk-%u", key, value.A, value.B);

    public void dmpGetDefaultComparerClass(ulong key, ulong value)
        => CFormat.Printf(Out, "GetDefaultComparerClass key cls-%016llX, value cls-%016llX", key, value);

    public void dmpGetDefaultEqualityComparerClass(ulong key, ulong value)
        => CFormat.Printf(Out, "GetDefaultEqualityComparerClass key cls-%016llX, value cls-%016llX", key, value);

    public void dmpGetSZArrayHelperEnumeratorClass(ulong key, ulong value)
        => CFormat.Printf(Out, "GetSZArrayHelperEnumeratorClass key cls-%016llX, value cls-%016llX", key, value);

    public void dmpGetTokenTypeAsHandle(in GetTokenTypeAsHandleValue key, ulong value)
        => CFormat.Printf(Out, "GetTokenTypeAsHandle key ftn-%016llX fld-%016llX, value cls-%016llX", key.hMethod, key.hField, value);

    public void dmpGetThreadLocalFieldInfo(DLD key, uint value)
        => CFormat.Printf(Out, "GetThreadLocalFieldInfo key hnd-%016llX,gctype-%d result-%u", key.A, key.B, value);

    public void dmpGetThreadLocalStaticBlocksInfo(uint key, in Agnostic_GetThreadLocalStaticBlocksInfo value)
        => CFormat.Printf(Out, "GetThreadLocalStaticBlocksInfo key %u, tlsIndex-%s, , tlsGetAddrFtnPtr-%016llX, tlsIndexObject - %016llX, threadVarsSection - %016llX, offsetOfThreadLocalStoragePointer-%u, offsetOfMaxThreadStaticBlocks-%u, offsetOfThreadStaticBlocks-%u, offsetOfBaseOfThreadLocalData-%u",
            key, SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(value.tlsIndex), value.tlsGetAddrFtnPtr,
            value.tlsIndexObject, value.threadVarsSection, value.offsetOfThreadLocalStoragePointer,
            value.offsetOfMaxThreadStaticBlocks, value.offsetOfThreadStaticBlocks, value.offsetOfBaseOfThreadLocalData);

    public void dmpGetThreadLocalStaticInfo_NativeAOT(ulong key, in Agnostic_GetThreadStaticInfo_NativeAOT value)
        => CFormat.Printf(Out, "GetThreadLocalStaticInfo_NativeAOT key %016llX, tlsRootObject-%s, tlsIndexObject-%s,  offsetOfThreadLocalStoragePointer-%u, threadStaticBaseSlow-%s, tlsGetAddrFtnPtr-%s",
            key, SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(value.tlsRootObject),
            SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(value.tlsIndexObject),
            value.offsetOfThreadLocalStoragePointer,
            SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(value.threadStaticBaseSlow),
            SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(value.tlsGetAddrFtnPtr));

    public void dmpEmbedMethodHandle(ulong key, DLDL value)
        => CFormat.Printf(Out, "EmbedMethodHandle key ftn-%016llX, value pp-%016llX res-%016llX", key, value.A, value.B);

    public void dmpGetStaticFieldContent(DLDDD key, DD value)
    {
        CFormat.Printf(Out, "GetStaticFieldContent key fld-%016llX bufSize-%u, ignoremovable-%u, valOffset-%u result-%u", key.A, key.B, key.C, key.D, value.A);
        GetStaticFieldContent.Unlock();
    }

    public void dmpGetObjectContent(DLDD key, DD value)
    {
        CFormat.Printf(Out, "GetObjectContent key fld-%016llX bufSize-%u, valOffset-%u result-%u", key.A, key.B, key.C, value.A);
        GetObjectContent.Unlock();
    }

    public void dmpGetStaticFieldCurrentClass(DLD key, in Agnostic_GetStaticFieldCurrentClass value)
        => CFormat.Printf(Out, "GetStaticFieldCurrentClass key fld-%016llX, value clsHnd-%016llX isSpeculative-%u", key.A, value.classHandle,
            value.isSpeculative);

    public void dmpGetClassAlignmentRequirement(DLD key, uint value)
        => CFormat.Printf(Out, "GetClassAlignmentRequirement key %016llX %u, value %u", key.A, key.B, value);

    public void dmpGetCastingHelper(in Agnostic_GetCastingHelper key, uint value)
        => CFormat.Printf(Out, "GetCastingHelper key cls-%016llX, thw-%u, value res-%u", key.hClass, key.fThrowing, value);

    public void dmpEmbedModuleHandle(ulong key, DLDL value)
        => CFormat.Printf(Out, "EmbedModuleHandle key mod-%016llX, value pp-%016llX res-%016llX", key, value.A, value.B);

    public void dmpEmbedClassHandle(ulong key, DLDL value)
        => CFormat.Printf(Out, "EmbedClassHandle key cls-%016llX, value pp-%016llX res-%016llX", key, value.A, value.B);

    public void dmpPInvokeMarshalingRequired(in MethodOrSigInfoValue key, uint value)
        => CFormat.Printf(Out, "PInvokeMarshalingRequired key mth-%016llX scp-%016llX sig-%s, value res-%u",
            key.method, key.scope,
            SpmiDumpHelper.DumpPSig(key.pSig_Index, key.cbSig, PInvokeMarshalingRequired),
            value);

    public void dmpGetUnmanagedCallConv(in MethodOrSigInfoValue key, DD value)
        => CFormat.Printf(Out, "GetUnmanagedCallConv key mth-%016llX scp-%016llX sig-%s, value res-%u,%u",
            key.method, key.scope,
            SpmiDumpHelper.DumpPSig(key.pSig_Index, key.cbSig, GetUnmanagedCallConv),
            value.A, value.B);

    public void dmpFindSig(in Agnostic_FindSig key, in Agnostic_CORINFO_SIG_INFO value)
    {
        CFormat.Printf(Out, "FindSig key module-%016llX sigTOK-%08X context-%016llX", key.module, key.sigTOK, key.context);
        CFormat.Printf(Out, ", value-%s",
            SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(value, FindSig, SigInstHandleMap));
    }

    public void dmpGetEEInfo(uint key, in Agnostic_CORINFO_EE_INFO value)
        => CFormat.Printf(Out, "GetEEInfo key %u, value icfi{sz-%u sz-witharg-%u ofl-%u ocsp-%u ocsfp-%u oct-%u ora-%u ossa-%u osap-%u} otf-%u ogcs-%u odi-%u odft-%u srpf-%u osps-%u muono-%u tabi-%u osType-%u",
            key, value.inlinedCallFrameInfo.size, value.inlinedCallFrameInfo.sizeWithSecretStubArg,
            value.inlinedCallFrameInfo.offsetOfFrameLink,
            value.inlinedCallFrameInfo.offsetOfCallSiteSP, value.inlinedCallFrameInfo.offsetOfCalleeSavedFP,
            value.inlinedCallFrameInfo.offsetOfCallTarget, value.inlinedCallFrameInfo.offsetOfReturnAddress,
            value.inlinedCallFrameInfo.offsetOfSecretStubArg, value.inlinedCallFrameInfo.offsetOfSPAfterProlog,
            value.offsetOfThreadFrame, value.offsetOfGCState, value.offsetOfDelegateInstance,
            value.offsetOfDelegateFirstTarget,
            value.sizeOfReversePInvokeFrame, value.osPageSize, value.maxUncheckedOffsetForNullObject, value.targetAbi,
            value.osType);

    public void dmpGetAsyncInfo(uint key, in Agnostic_CORINFO_ASYNC_INFO value)
        => CFormat.Printf(Out, "GetAsyncInfo key %u value contClsHnd-%016llX contNextFldHnd-%016llX contResumeInfoFldHnd-%016llX contStateFldHnd-%016llX contFlagsFldHnd-%016llX",
            key, value.continuationClsHnd, value.continuationNextFldHnd, value.continuationResumeInfoFldHnd,
            value.continuationStateFldHnd, value.continuationFlagsFldHnd);

    public void dmpGetWasmWellKnownGlobals(uint key, in Agnostic_CORINFO_WASM_WELLKNOWN_GLOBALS value)
        => CFormat.Printf(Out, "GetWasmWellKnownGlobals key %u value stackPointer-%016llX imageBase-%016llX tableBase-%016llX asyncContinuation-%016llX",
            key, value.stackPointer, value.imageBase, value.tableBase, value.asyncContinuation);

    public void dmpGetAwaitReturnCall(ulong key, in Agnostic_GetAwaitReturnCallResult value)
        => CFormat.Printf(Out, "GetAwaitReturnCall key %016llX value methodHnd-%016llX contextHandle-%016llX instArg %s",
            key,
            value.methodHnd,
            value.contextHandle,
            SpmiDumpHelper.DumpAgnostic_CORINFO_LOOKUP(value.instArg));

    public void dmpGetAwaitAwaiterInContinuationCall(in Agnostic_GetAwaitAwaiterInContinuationCall key, in Agnostic_GetAwaitReturnCallResult value)
        => CFormat.Printf(Out, "GetAwaitAwaiterInContinuationCall caller-%016llX rt{%s} unsafe-%u methodHnd-%016llX contextHandle-%016llX instArg %s",
            key.callerHnd,
            SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.ResolvedToken),
            key.isUnsafe,
            value.methodHnd,
            value.contextHandle,
            SpmiDumpHelper.DumpAgnostic_CORINFO_LOOKUP(value.instArg));

    public void dmpGetGSCookie(uint key, DLDL value)
        => CFormat.Printf(Out, "GetGSCookie key 0, value pCookieVal-%016llX ppCookieVal-%016llX", value.A, value.B);

    public void dmpGetOSRInfo(uint key, in Agnostic_GetOSRInfo value)
    {
        // todo - dump patchpoint info?
        CFormat.Printf(Out, "GetOSRInfo key %u, value patchpointInfo-%u {...} iloffset-%u\n",
            key, value.index, value.ilOffset);
    }

    public void dmpGetClassModuleIdForStatics(ulong key, in Agnostic_GetClassModuleIdForStatics value)
        => CFormat.Printf(Out, "GetClassModuleIdForStatics key cls-%016llX, value mod-%016llX pp-%016llX res-%016llX", key, value.Module,
            value.pIndirection, value.result);

    public void dmpGetIsClassInitedFlagAddress(ulong key, in Agnostic_GetIsClassInitedFlagAddress value)
        => CFormat.Printf(Out, "GetIsClassInitedFlagAddress key hnd-%016llX, value addr-%016llX, result-%u", key, value.addr.handle, value.result);

    public void dmpGetStaticBaseAddress(DLD key, in Agnostic_GetStaticBaseAddress value)
        => CFormat.Printf(Out, "GetStaticBaseAddress key hnd-%016llX, value addr-%016llX, result-%u", key.A, value.addr.handle, value.result);

    public void dmpGetThreadTLSIndex(uint key, DLD value)
        => CFormat.Printf(Out, "GetThreadTLSIndex key 0, value ppIndirection-%016llX result-%08X", value.A, value.B);

    public void dmpGetAddrOfCaptureThreadGlobal(uint key, DLDL value)
        => CFormat.Printf(Out, "GetAddrOfCaptureThreadGlobal key %u, value ppi-%016llX res-%016llX", key, value.A, value.B);

    public void dmpGetClassStaticDynamicInfo(ulong key, DLD value)
        => CFormat.Printf(Out, "GetClassStaticDynamicInfo key cls-%016llX, value pp-%016llX res-%u", key, value.A, value.B);

    public void dmpGetClassThreadStaticDynamicInfo(ulong key, DLD value)
        => CFormat.Printf(Out, "GetClassThreadStaticDynamicInfo key cls-%016llX, value pp-%016llX res-%u", key, value.A, value.B);

    public void dmpGetLocationOfThisType(ulong key, in Agnostic_CORINFO_LOOKUP_KIND value)
        => CFormat.Printf(Out, "GetLocationOfThisType key ftn-%016llX, value %s", key,
            SpmiDumpHelper.DumpAgnostic_CORINFO_LOOKUP_KIND(value));

    public void dmpGetDelegateCtor(in Agnostic_GetDelegateCtorIn key, in Agnostic_GetDelegateCtorOut value)
        => CFormat.Printf(Out, "GetDelegateCtor key ftn-%016llX cls-%016llX tftn-%016llX, value pm-%016llX a3-%016llX a4-%016llX a5-%016llX res-%016llX",
            key.methHnd, key.clsHnd, key.targetMethodHnd, value.CtorData.pMethod, value.CtorData.pArg3,
            value.CtorData.pArg4, value.CtorData.pArg5, value.result);

    public void dmpGetFunctionFixedEntryPoint(ulong key, in Agnostic_CORINFO_CONST_LOOKUP value)
        => CFormat.Printf(Out, "GetFunctionFixedEntryPoint key ftn-%016llX, value %s", key,
            SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(value));

    public void dmpGetFieldInClass(DLD key, ulong value)
        => CFormat.Printf(Out, "GetFieldInClass key cls-%016llX ind-%u, value %016llX", key.A, key.B, value);

    public void dmpGetFieldType(DLDL key, DLD value)
        => CFormat.Printf(Out, "GetFieldType key fld-%016llX cls-%016llX, value ch-%016llX cit-%u(%s)", key.A, key.B, value.A, value.B,
            toString_CorInfoType(value.B));

    public void dmpSatisfiesMethodConstraints(DLDL key, uint value)
        => CFormat.Printf(Out, "SatisfiesMethodConstraints key cls-%016llX ftn-%016llX, value res-%u", key.A, key.B, value);

    public void dmpGetStringLiteral(DLDDD key, DD value)
    {
        CFormat.Printf(Out, "GetStringLiteral key mod-%016llX tok-%08X, bufSize-%u, startIndex-%u, len-%u", key.A, key.B, key.C, key.D, value.A);
        GetStringLiteral.Unlock();
    }

    public void dmpCanCast(DLDL key, uint value)
        => CFormat.Printf(Out, "CanCast key chd-%016llX par-%016llX, value res-%u", key.A, key.B, value);

    public void dmpGetChildType(ulong key, DLD value)
        => CFormat.Printf(Out, "GetChildType key cls-%016llX, value clsr-%016llX cit-%u(%s)", key, value.A, value.B,
            toString_CorInfoType(value.B));

    public void dmpGetArrayInitializationData(DLD key, ulong value)
        => CFormat.Printf(Out, "GetArrayInitializationData key field-%016llX size-%08X, value result-%016llX", key.A, key.B, value);

    public void dmpGetAddressOfPInvokeTarget(ulong key, DLD value)
        => CFormat.Printf(Out, "GetAddressOfPInvokeTarget key ftn-%016llX, value addr-%016llX at-%u", key, value.A, value.B);

    public void dmpGetMethodHash(ulong key, uint value)
        => CFormat.Printf(Out, "GetMethodHash key %016llX, value %u", key, value);

    public void dmpCanTailCall(in Agnostic_CanTailCall key, uint value)
        => CFormat.Printf(Out, "CanTailCall key clr-%016llX dcle-%016llX ecle-%016llX pfx-%u, value res-%u", key.callerHnd,
            key.declaredCalleeHnd, key.exactCalleeHnd, key.fIsTailPrefix, value);

    public void dmpIsDelegateCreationAllowed(DLDL key, uint value)
        => CFormat.Printf(Out, "IsDelegateCreationAllowed key delegateHnd-%016llX calleeHnd-%016llX result-%08X", key.A, key.B, value);

    public void dmpFindCallSiteSig(in Agnostic_FindCallSiteSig key, in Agnostic_CORINFO_SIG_INFO value)
    {
        CFormat.Printf(Out, "dmpFindCallSiteSig key module-%016llX methTok-%08X context-%016llX", key.module, key.methTok, key.context);
        CFormat.Printf(Out, ", value-%s",
            SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(value, FindCallSiteSig, SigInstHandleMap));
    }

    public void dmpGetVarArgsHandle(in GetVarArgsHandleValue key, DLDL value)
    {
        CFormat.Printf(Out, "GetVarArgsHandle key sig-%s scope-%016llX token-%08X methHnd-%016llX",
            SpmiDumpHelper.DumpPSig(key.pSig_Index, key.cbSig, GetVarArgsHandle),
            key.scope, key.token, key.methHnd);
        CFormat.Printf(Out, ", value ppIndirection-%016llX result-%016llX", value.A, value.B);
    }

    public void dmpGetFieldThreadLocalStoreID(ulong key, DLD value)
        => CFormat.Printf(Out, "GetFieldThreadLocalStoreID key field-%016llX, value ppIndirection-%016llX result-%08X", key, value.A,
            value.B);

    public void dmpIsMoreSpecificType(DLDL key, uint value)
        => CFormat.Printf(Out, "IsMoreSpecificType key cls1-%016llX cls2-%016llX, value %u", key.A, key.B, value);

    public void dmpIsExactType(ulong key, uint value)
        => CFormat.Printf(Out, "IsExactType key cls-%016llX, value res-%u", key, value);

    public void dmpIsGenericType(ulong key, uint value)
        => CFormat.Printf(Out, "IsGenericType key cls-%016llX, value res-%d", key, value);

    public void dmpIsNullableType(ulong key, uint value)
        => CFormat.Printf(Out, "IsNullableType key cls-%016llX, value res-%d", key, value);

    public void dmpIsEnum(ulong key, DLD value)
        => CFormat.Printf(Out, "IsEnum key cls-%016llX, value underlyingType-%016llX result-%u", key, value.A, value.B);

    public void dmpGetCookieForInterpreterCalliSig(in GetCookieForInterpreterCalliSigValue key, DLDL value)
        => CFormat.Printf(Out, "GetCookieForInterpreterCalliSig NYI");

    public void dmpErrorList(uint key, uint value)
        => CFormat.Printf(Out, "ErrorList NYI");

    public void dmpGetProfilingHandle(uint key, in Agnostic_GetProfilingHandle value)
        => CFormat.Printf(Out, "GetProfilingHandle key %u, value bHookFtn-%u profHnd-%016llX bIndHnd-%u", key, value.bHookFunction,
            value.ProfilerHandle, value.bIndirectedHandles);

    public void dmpEmbedFieldHandle(ulong key, DLDL value)
        => CFormat.Printf(Out, "EmbedFieldHandle NYI");

    public void dmpCompareTypesForCast(DLDL key, uint value)
        => CFormat.Printf(Out, "CompareTypesForCast key fromClass=%016llX, toClass=%016llx, result=%d", key.A, key.B, value);

    public void dmpCompareTypesForEquality(DLDL key, uint value)
        => CFormat.Printf(Out, "CompareTypesForEquality key cls1=%016llX, cls2=%016llx, result=%d", key.A, key.B, value);

    public void dmpGetWasmLowering(ulong key, uint value)
        => CFormat.Printf(Out, "GetWasmLowering key structHnd-%016llX, value %d ", key, value);

    public void dmpGetRelocTypeHint(ulong key, uint value)
        => CFormat.Printf(Out, "GetRelocTypeHint key tgt-%016llX, value hint-%u", key, value);

    public void dmpGetAddressAlignment(ulong key, uint value)
        => CFormat.Printf(Out, "GetAddressAlignment key addr-%016llX, value align-%u", key, value);

    public void dmpGetExpectedTargetArchitecture(uint key, uint result)
        => CFormat.Printf(Out, "GetExpectedTargetArchitecture key %u, res %u", key, result);

    public void dmpDoesFieldBelongToClass(DLDL key, uint value)
        => CFormat.Printf(Out, "DoesFieldBelongToClass key fld=%016llX, cls=%016llx, result=%d", key.A, key.B, value);

    public void dmpGetTypeInstantiationArgument(DLD key, ulong value)
    {
        CFormat.Printf(Out, "GetTypeInstantiationArgument key - classNonNull-%llu, index-%u, value NonNull-%llu", key.A, key.B, value);
        GetTypeInstantiationArgument.Unlock();
    }

    public void dmpGetMethodInstantiationArgument(DLD key, ulong value)
    {
        CFormat.Printf(Out, "GetMethodInstantiationArgument key - methodNonNull-%llu, index-%u, value NonNull-%llu", key.A, key.B, value);
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
        CFormat.Printf(Out, "GetTailCallHelpers key callToken-%s sig-%s flg-%08X",
            SpmiDumpHelper.DumpAgnostic_CORINFO_RESOLVED_TOKEN(key.callToken),
            SpmiDumpHelper.DumpAgnostic_CORINFO_SIG_INFO(key.sig, GetTailCallHelpers, SigInstHandleMap),
            key.flags);
        CFormat.Printf(Out, ", value result-%s flg-%08X hStoreArgs-%016llX hCallTarget-%016llX hDispatcher-%016llX",
            (value.result != 0) ? "true" : "false",
            value.flags,
            value.hStoreArgs,
            value.hCallTarget,
            value.hDispatcher);
    }

    public void dmpGetAsyncResumptionStub(uint key, in DLDL value)
        => CFormat.Printf(Out, "GetAsyncResumptionStub key-%u, hnd-%016llX, entrypoint-%016llX", key, value.A, value.B);

    public void dmpGetContinuationType(in Agnostic_GetContinuationTypeIn key, ulong value)
        => CFormat.Printf(Out, "GetContinuationType dataSize-%016llX, objRefs-%u, handle-%016llX",
            key.dataSize, key.objRefs, value);

    public void dmpUpdateEntryPointForTailCall(in Agnostic_CORINFO_CONST_LOOKUP origEntryPoint, in Agnostic_CORINFO_CONST_LOOKUP newEntryPoint)
        => CFormat.Printf(Out, "UpdateEntryPointForTailcall orig=%s new=%s",
            SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(origEntryPoint),
            SpmiDumpHelper.DumpAgnostic_CORINFO_CONST_LOOKUP(newEntryPoint));

    public void dmpGetMethodDefFromMethod(ulong key, uint value)
        => CFormat.Printf(Out, "GetMethodDefFromMethod key ftn-%016llX, value res-%u", key, value);

    public void dmpGetArrayRank(ulong key, uint value)
        => CFormat.Printf(Out, "GetArrayRank key %016llX, value %u", key, value);

    public void dmpGetArrayIntrinsicID(ulong key, uint value)
        => CFormat.Printf(Out, "GetArrayIntrinsicID key %016llX, value %u", key, value);

    public void dmpIsFieldStatic(ulong key, uint value)
        => CFormat.Printf(Out, "IsFieldStatic key %016llX, value %u", key, value);

    public void dmpGetArrayOrStringLength(ulong key, uint value)
        => CFormat.Printf(Out, "GetArrayOrStringLength key %016llX, value %u", key, value);

    public void dmpGetSpecialCopyHelper(ulong key, ulong value)
        => CFormat.Printf(Out, "getSpecialCopyHelper key %016llX, value %016llX", key, value);

    public void dmpGetWasmTypeSymbol(in Agnostic_GetWasmTypeSymbol key, ulong value)
        => CFormat.Printf(Out, "getWasmTypeSymbol types %u, typesSize %u, value %016llX", key.types, key.typesSize, value);

    public void dmpSigInstHandleMap(uint key, ulong value)
        => CFormat.Printf(Out, "SigInstHandleMap key %u, value %016llX", key, value);

    public void dmpCanValueClassInstancePointerEscape(ulong key, uint value)
        => CFormat.Printf(Out, "CanValueClassInstancePointerEscape key ftn-%016llX, value res-%u", key, value);
}
