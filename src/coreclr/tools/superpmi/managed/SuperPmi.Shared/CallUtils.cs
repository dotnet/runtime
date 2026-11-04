// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Internal.JitInterface;

using static SuperPmi.SpmiRecordsHelper;

namespace SuperPmi;

public static class TypeUtils
{
    // Naming follows JITtype2varType in src/coreclr/jit/ee_il_dll.hpp.
    public static string GetCorInfoTypeName(CorInfoType type) => type switch
    {
        CorInfoType.CORINFO_TYPE_VOID => "void",
        CorInfoType.CORINFO_TYPE_BOOL => "bool",
        CorInfoType.CORINFO_TYPE_CHAR => "char",
        CorInfoType.CORINFO_TYPE_BYTE => "byte",
        CorInfoType.CORINFO_TYPE_UBYTE => "ubyte",
        CorInfoType.CORINFO_TYPE_SHORT => "short",
        CorInfoType.CORINFO_TYPE_USHORT => "ushort",
        CorInfoType.CORINFO_TYPE_INT => "int",
        CorInfoType.CORINFO_TYPE_UINT => "uint",
        CorInfoType.CORINFO_TYPE_LONG => "long",
        CorInfoType.CORINFO_TYPE_ULONG => "ulong",
        CorInfoType.CORINFO_TYPE_FLOAT => "float",
        CorInfoType.CORINFO_TYPE_DOUBLE => "double",
        CorInfoType.CORINFO_TYPE_BYREF => "byref",
        CorInfoType.CORINFO_TYPE_VALUECLASS => "struct",
        CorInfoType.CORINFO_TYPE_CLASS => "ref",
        // Emulates the JIT's TYP_I_IMPL.
        CorInfoType.CORINFO_TYPE_NATIVEINT or CorInfoType.CORINFO_TYPE_NATIVEUINT => SpmiUtil.IsSpmiTarget64Bit() ? "long" : "int",
        // Pointer-sized, without regard to GC-ness.
        CorInfoType.CORINFO_TYPE_PTR => "ptr",
        _ => UnknownType(type),
    };

    private static string UnknownType(CorInfoType type)
    {
        ErrorHandling.LogException(ExceptionCodes.TYPEUTILS, $"Unknown type passed into GetCorInfoTypeName (0x{(int)type:x})");
        return "UNKNOWN";
    }

    public static CorInfoType strip(CorInfoTypeWithMod val) => (CorInfoType)(val & CorInfoTypeWithMod.CORINFO_TYPE_MASK);
}

public static unsafe class CallUtils
{
    public const uint CORINFO_HELP_UNDEF = 0;

    // Helpers are marked by odd handle values.
    public static uint GetHelperNum(CORINFO_METHOD_STRUCT_* method)
        => (CastHandle(method) & 1) == 0 ? CORINFO_HELP_UNDEF : (uint)(CastHandle(method) >> 2);

    public static string GetHelperName(uint helperNum)
        => helperNum < (uint)CorInfoHelpFunc.CORINFO_HELP_COUNT ? Enum.GetName((CorInfoHelpFunc)helperNum) : $"CORINFO_HELP_{helperNum}";

    // Formats "Class:Method(argtypes):rettype[:this]", as in src/coreclr/jit/eeinterface.cpp. With
    // 'ignoreMethodName', the class and method names are placeholders that are the same for all methods.
    public static byte[] GetMethodFullName(MethodContext mc, CORINFO_METHOD_STRUCT_* hnd, CORINFO_SIG_INFO sig, bool ignoreMethodName = false)
    {
        uint helperNum = GetHelperNum(hnd);
        if (helperNum != CORINFO_HELP_UNDEF)
            return Encoding.ASCII.GetBytes(GetHelperName(helperNum));

        var name = new ByteBuilder();
        if (ignoreMethodName)
        {
            name.Append("CLASS:METHOD");
        }
        else
        {
            name.Append(getClassName(mc, mc.repGetMethodClass(hnd)));
            name.Append(":"u8);
            name.Append(getMethodName(mc, hnd));
        }

        name.Append("("u8);
        CORINFO_ARG_LIST_STRUCT_* argList = sig.args;
        for (uint i = 0; i < sig.numArgs; i++)
        {
            CORINFO_CLASS_STRUCT_* dummyCls;
            CorInfoType type = TypeUtils.strip(mc.repGetArgType(&sig, argList, &dummyCls, out _));
            name.Append(TypeUtils.GetCorInfoTypeName(type));

            argList = mc.repGetArgNext(argList);
            if (i + 1 < sig.numArgs)
                name.Append(","u8);
        }
        name.Append(")"u8);

        if (sig.retType != CorInfoType.CORINFO_TYPE_VOID)
            name.Append(":"u8).Append(TypeUtils.GetCorInfoTypeName(sig.retType));

        // Explicit 'this' is already the first element of the argument list.
        if ((sig.callConv & (CorInfoCallConv.CORINFO_CALLCONV_HASTHIS | CorInfoCallConv.CORINFO_CALLCONV_EXPLICITTHIS)) == CorInfoCallConv.CORINFO_CALLCONV_HASTHIS)
            name.Append(":this"u8);

        return name.ToArray();
    }

    public static byte[] getMethodName(MethodContext mc, CORINFO_METHOD_STRUCT_* methHnd)
        => GetFromPrinter((buffer, size, required) => mc.repPrintMethodName(methHnd, buffer, size, required));

    public static byte[] getClassName(MethodContext mc, CORINFO_CLASS_STRUCT_* clsHnd)
        => GetFromPrinter((buffer, size, required) => mc.repPrintClassName(clsHnd, buffer, size, required));

    private delegate nuint Printer(byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize);

    // Prints into a 256-byte buffer first, and asks for the required size only if that wasn't enough.
    private static byte[] GetFromPrinter(Printer print)
    {
        byte* buffer = stackalloc byte[256];
        nuint requiredBufferSize;
        print(buffer, 256, &requiredBufferSize);
        if (requiredBufferSize <= 256)
            return MemoryMarshal.CreateReadOnlySpanFromNullTerminated(buffer).ToArray();

        byte[] large = new byte[requiredBufferSize];
        fixed (byte* p = large)
        {
            print(p, requiredBufferSize, null);
            return MemoryMarshal.CreateReadOnlySpanFromNullTerminated(p).ToArray();
        }
    }
}
