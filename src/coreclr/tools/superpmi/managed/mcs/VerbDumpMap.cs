// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Internal.JitInterface;

using static SuperPmi.SpmiRecordsHelper;

namespace SuperPmi.Mcs;

// Signature printing shared by -dumpMap and -ildump.
internal static unsafe class SigDumper
{
    private static TextWriter Out => Logger.Out;

    public static void DumpPrimToConsoleBare(MethodContext mc, CorInfoType prim, CORINFO_CLASS_STRUCT_* classHandle)
    {
        switch (prim)
        {
            case CorInfoType.CORINFO_TYPE_VOID: Out.Write("void"); return;
            case CorInfoType.CORINFO_TYPE_BOOL: Out.Write("bool"); return;
            case CorInfoType.CORINFO_TYPE_CHAR: Out.Write("char"); return;
            case CorInfoType.CORINFO_TYPE_BYTE: Out.Write("int8"); return;
            case CorInfoType.CORINFO_TYPE_UBYTE: Out.Write("unsigned int8"); return;
            case CorInfoType.CORINFO_TYPE_SHORT: Out.Write("int16"); return;
            case CorInfoType.CORINFO_TYPE_USHORT: Out.Write("unsigned int16"); return;
            case CorInfoType.CORINFO_TYPE_INT: Out.Write("int32"); return;
            case CorInfoType.CORINFO_TYPE_UINT: Out.Write("unsigned int32"); return;
            case CorInfoType.CORINFO_TYPE_LONG: Out.Write("int64"); return;
            case CorInfoType.CORINFO_TYPE_ULONG: Out.Write("unsigned int64"); return;
            case CorInfoType.CORINFO_TYPE_NATIVEINT: Out.Write("native int"); return;
            case CorInfoType.CORINFO_TYPE_NATIVEUINT: Out.Write("native unsigned int"); return;
            case CorInfoType.CORINFO_TYPE_FLOAT: Out.Write("float32"); return;
            case CorInfoType.CORINFO_TYPE_DOUBLE: Out.Write("float64"); return;
            case CorInfoType.CORINFO_TYPE_PTR: Out.Write("ptr"); return;
            case CorInfoType.CORINFO_TYPE_BYREF: Out.Write("byref"); return;
            case CorInfoType.CORINFO_TYPE_VALUECLASS:
            case CorInfoType.CORINFO_TYPE_CLASS:
                uint arrayRank = mc.repGetArrayRank(classHandle);
                if (arrayRank > 0)
                {
                    CORINFO_CLASS_STRUCT_* childCls;
                    CorInfoType childType = mc.repGetChildType(classHandle, &childCls);
                    DumpPrimToConsoleBare(mc, childType, childCls);
                    Out.Write('[');
                    for (uint i = 1; i < arrayRank; i++)
                        Out.Write(',');
                    Out.Write(']');
                }
                else
                {
                    Out.Write(prim == CorInfoType.CORINFO_TYPE_VALUECLASS ? "valueclass " : "class ");
                    Out.Write(PrintClassName(mc, classHandle));
                }
                return;
            default:
                Logger.LogWarning($"unknown type in PrimToString(0x{(int)prim:x})");
                return;
        }
    }

    public static void DumpSigToConsoleBare(MethodContext mc, CORINFO_SIG_INFO sig)
    {
        CORINFO_ARG_LIST_STRUCT_* currentItem = sig.args;
        for (int i = 0; i < sig.numArgs; i++)
        {
            CORINFO_CLASS_STRUCT_* dl;
            CorInfoTypeWithMod type = mc.repGetArgType(&sig, currentItem, &dl, out _);
            CorInfoType cit = TypeUtils.strip(type);
            if (cit == CorInfoType.CORINFO_TYPE_CLASS)
                dl = mc.repGetArgClass(&sig, currentItem, out _);
            if ((type & CorInfoTypeWithMod.CORINFO_TYPE_MOD_PINNED) == CorInfoTypeWithMod.CORINFO_TYPE_MOD_PINNED)
                Out.Write("pinned ");
            DumpPrimToConsoleBare(mc, cit, dl);
            currentItem = mc.repGetArgNext(currentItem);
            if (i + 1 < sig.numArgs)
                Out.Write(", ");
        }
    }

    public static void DumpAttributeToConsoleBare(uint attribute)
    {
        var flags = (CorInfoFlag)attribute;
        if ((flags & CorInfoFlag.CORINFO_FLG_STATIC) == CorInfoFlag.CORINFO_FLG_STATIC)
            Out.Write("static ");
        else if ((flags & CorInfoFlag.CORINFO_FLG_DONT_INLINE) == CorInfoFlag.CORINFO_FLG_DONT_INLINE)
            Out.Write("$dontInline  ");
        else if ((flags & CorInfoFlag.CORINFO_FLG_CONSTRUCTOR) == CorInfoFlag.CORINFO_FLG_CONSTRUCTOR)
            Out.Write("$constructor ");
    }

    // A name printed into a fixed 256-byte buffer, truncated to fit.
    public static string PrintName(MethodContext mc, string mapName, LightWeightMap<ulong, Agnostic_PrintResult> map, ulong handle)
    {
        byte* buffer = stackalloc byte[256];
        int length = (int)mc.repPrint(mapName, map, handle, buffer, 256, null);
        int nul = new ReadOnlySpan<byte>(buffer, length).IndexOf((byte)0);
        return Encoding.UTF8.GetString(buffer, nul >= 0 ? nul : length);
    }

    public static string PrintClassName(MethodContext mc, CORINFO_CLASS_STRUCT_* cls) => PrintName(mc, "PrintClassName", mc.PrintClassName, CastHandle(cls));

    public static string PrintMethodName(MethodContext mc, CORINFO_METHOD_STRUCT_* meth) => PrintName(mc, "PrintMethodName", mc.PrintMethodName, CastHandle(meth));
}

internal static unsafe class VerbDumpMap
{
    // CSV: index, method name, full signature, jit flags, os.
    public static int DoWork(string nameOfInput)
    {
        using var mci = new MethodContextIterator();
        if (!mci.Initialize(nameOfInput))
            return -1;

        TextWriter output = Logger.Out;
        output.Write("index,method name,full signature,jit flags,os\n");

        while (mci.MoveNext())
            DumpMap(output, mci.MethodContextNumber, mci.Current);

        return mci.Destroy() ? 0 : -1;
    }

    private static void DumpMap(TextWriter output, int index, MethodContext mc)
    {
        CORINFO_METHOD_INFO cmi = mc.repCompileMethod(out _, out CORINFO_OS os);

        string methodName = SigDumper.PrintMethodName(mc, cmi.ftn);
        string className = SigDumper.PrintClassName(mc, mc.repGetMethodClass(cmi.ftn));

        output.Write($"{index},{className}:{methodName},");

        output.Write('"');
        SigDumper.DumpAttributeToConsoleBare(mc.repGetMethodAttribs(cmi.ftn));
        SigDumper.DumpPrimToConsoleBare(mc, cmi.args.retType, cmi.args.retTypeClass);
        output.Write($" {methodName}");

        // Class and method generic parameters, if there are any.
        CORINFO_SIG_INFO sig;
        mc.repGetMethodSig(cmi.ftn, &sig, null);
        WriteInstantiation(output, mc, sig.sigInst.classInstCount, sig.sigInst.classInst);
        WriteInstantiation(output, mc, sig.sigInst.methInstCount, sig.sigInst.methInst);

        output.Write('(');
        SigDumper.DumpSigToConsoleBare(mc, cmi.args);
        output.Write(")\"");

        output.Write($", {SpmiDumpHelper.DumpJitFlags(ExtraJitFlags.GetFlagsWithPgo(mc))}, {(int)os}\n");
    }

    private static void WriteInstantiation(TextWriter output, MethodContext mc, uint count, CORINFO_CLASS_STRUCT_** handles)
    {
        for (uint i = 0; i < count; i++)
        {
            output.Write(i == 0 ? "[" : "");
            output.Write(i > 0 ? ", " : "");
            output.Write(SigDumper.PrintClassName(mc, handles[i]));
            output.Write(i == count - 1 ? "]" : "");
        }
    }
}

internal static class VerbJitFlags
{
    public static int DoWork(string nameOfInput)
    {
        using var mci = new MethodContextIterator();
        if (!mci.Initialize(nameOfInput))
            return -1;

        // Kept sorted the way LightWeightMap sorts its keys: by memcmp of the little-endian bytes.
        var flagMap = new LightWeightMap<ulong, uint>();
        uint mcCount = 0;

        while (mci.MoveNext())
        {
            ulong rawFlags = ExtraJitFlags.GetFlagsWithPgo(mci.Current);

            int index = flagMap.GetIndex(rawFlags);
            if (index == -1)
                flagMap.Add(rawFlags, 1);
            else
                flagMap.Update(index, flagMap.GetItem(index) + 1);

            mcCount++;
        }

        if (!mci.Destroy())
            return -1;

        TextWriter output = Logger.Out;
        output.Write($"\nGrouped Flag Appearances ({mcCount} contexts)\n\n");
        output.Write($"{"bits",-16} {"count",8} {"percent",8}  parsed\n");

        var appearancesPerBit = new uint[64];
        ReadOnlySpan<ulong> keys = flagMap.GetRawKeys();
        ReadOnlySpan<uint> items = flagMap.GetRawItems();
        for (int i = 0; i < keys.Length; i++)
        {
            ulong flags = keys[i];
            uint appearances = items[i];
            output.Write($"{flags:x16} {appearances,8} {100.0 * appearances / mcCount,7:F2}% {SpmiDumpHelper.DumpJitFlags(flags)}\n");

            for (int bit = 0; bit < 64; bit++)
            {
                if ((flags & (1UL << bit)) != 0)
                    appearancesPerBit[bit] += appearances;
            }
        }

        output.Write("\nIndividual Flag Appearances\n\n");
        for (int bit = 0; bit < 64; bit++)
        {
            uint perBit = appearancesPerBit[bit];
            if (perBit > 0)
                output.Write($"{perBit,8} {100.0 * perBit / mcCount,7:F2}% {SpmiDumpHelper.DumpJitFlags(1UL << bit)}\n");
        }

        return 0;
    }
}
