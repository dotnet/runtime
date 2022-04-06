// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "simpletimer.h"
#include "methodcontext.h"
#include "methodcontextiterator.h"
#include "verbildump.h"
#include "spmiutil.h"

void DumpPrimToConsoleBare(MethodContext* mc, CorInfoType prim, DWORDLONG classHandle)
{
    switch (prim)
    {
        case CORINFO_TYPE_VOID:
            printf("void");
            return;
        case CORINFO_TYPE_BOOL:
            printf("bool");
            return;
        case CORINFO_TYPE_CHAR:
            printf("char");
            return;
        case CORINFO_TYPE_BYTE:
            printf("int8");
            return;
        case CORINFO_TYPE_UBYTE:
            printf("unsigned int8");
            return;
        case CORINFO_TYPE_SHORT:
            printf("int16");
            return;
        case CORINFO_TYPE_USHORT:
            printf("unsigned int16");
            return;
        case CORINFO_TYPE_INT:
            printf("int32");
            return;
        case CORINFO_TYPE_UINT:
            printf("unsigned int32");
            return;
        case CORINFO_TYPE_LONG:
            printf("int64");
            return;
        case CORINFO_TYPE_ULONG:
            printf("unsigned int64");
            return;
        case CORINFO_TYPE_NATIVEINT:
            printf("native int");
            return;
        case CORINFO_TYPE_NATIVEUINT:
            printf("native unsigned int");
            return;
        case CORINFO_TYPE_FLOAT:
            printf("float32");
            return;
        case CORINFO_TYPE_DOUBLE:
            printf("float64");
            return;
        //  case CORINFO_TYPE_STRING: printf("string"); return;
        case CORINFO_TYPE_PTR:
            printf("ptr");
            return;
        case CORINFO_TYPE_BYREF:
            printf("byref");
            return;
        case CORINFO_TYPE_VALUECLASS:
            printf("valueclass %s", mc->repGetClassName((CORINFO_CLASS_HANDLE)classHandle));
            return;
        case CORINFO_TYPE_CLASS:
            printf("class %s", mc->repGetClassName((CORINFO_CLASS_HANDLE)classHandle));
            return;
        case CORINFO_TYPE_REFANY:
            printf("refany");
            return;
        case CORINFO_TYPE_VAR:
            printf("var");
            return;
        default:
            LogWarning("unknown type in PrimToString(0x%x)", prim);
            __debugbreak();
            return;
    }
}
void DumpSigToConsoleBare(MethodContext* mc, CORINFO_SIG_INFO* pSig)
{
    CORINFO_ARG_LIST_HANDLE currentItem = pSig->args;
    DWORD                   exceptionCode;

    for (int i = 0; i < (int)pSig->numArgs; i++)
    {
        DWORDLONG          dl;
        CorInfoTypeWithMod type = mc->repGetArgType(pSig, currentItem, (CORINFO_CLASS_HANDLE*)&dl, &exceptionCode);
        CorInfoType        cit  = strip(type);
        if (cit == CORINFO_TYPE_CLASS)
            dl = CastHandle(mc->repGetArgClass(pSig, currentItem, &exceptionCode));
        if ((type & CORINFO_TYPE_MOD_PINNED) == CORINFO_TYPE_MOD_PINNED)
            printf("pinned ");
        DumpPrimToConsoleBare(mc, cit, dl);
        currentItem = mc->repGetArgNext(currentItem);
        if (i + 1 < (int)pSig->numArgs)
            printf(", ");
    }
}

void DumpILToConsoleBare(unsigned char* ilCode, int len)
{
    int i, j, k;
    for (i = 0; i < len; i++)
    {
        printf("IL_%04x: ", i);
        switch (ilCode[i])
        {
            case 0x00:
                printf("nop");
                continue;
            case 0x01:
                printf("break");
                continue;
            case 0x02:
                printf("ldarg.0");
                continue;
            case 0x03:
                printf("ldarg.1");
                continue;
            case 0x04:
                printf("ldarg.2");
                continue;
            case 0x05:
                printf("ldarg.3");
                continue;
            case 0x06:
                printf("ldloc.0");
                continue;
            case 0x07:
                printf("ldloc.1");
                continue;
            case 0x08:
                printf("ldloc.2");
                continue;
            case 0x09:
                printf("ldloc.3");
                continue;
            case 0x0a:
                printf("stloc.0");
                continue;
            case 0x0b:
                printf("stloc.1");
                continue;
            case 0x0c:
                printf("stloc.2");
                continue;
            case 0x0d:
                printf("stloc.3");
                continue;
            case 0x0e: // ldarg.s X
                printf("ldarg.s 0x%02x", ilCode[i + 1]);
                i += 1;
                continue;
            case 0x0f: // ldarga.s X
                printf("ldarga.s 0x%02x", ilCode[i + 1]);
                i += 1;
                continue;
            case 0x10: // starg.s X
                printf("starg.s 0x%02x", ilCode[i + 1]);
                i += 1;
                continue;
            case 0x11: // ldloc.s X
                printf("ldloc.s 0x%02x", ilCode[i + 1]);
                i += 1;
                continue;
            case 0x12: // ldloca.s X
                printf("ldloca.s 0x%02x", ilCode[i + 1]);
                i += 1;
                continue;
            case 0x13: // stloc.s X
                printf("stloc.s 0x%02x", ilCode[i + 1]);
                i += 1;
                continue;
            case 0x14:
                printf("ldnull");
                continue;
            case 0x15:
                printf("ldc.i4.m1");
                continue;
            case 0x16:
                printf("ldc.i4.0");
                continue;
            case 0x17:
                printf("ldc.i4.1");
                continue;
            case 0x18:
                printf("ldc.i4.2");
                continue;
            case 0x19:
                printf("ldc.i4.3");
                continue;
            case 0x1a:
                printf("ldc.i4.4");
                continue;
            case 0x1b:
                printf("ldc.i4.5");
                continue;
            case 0x1c:
                printf("ldc.i4.6");
                continue;
            case 0x1d:
                printf("ldc.i4.7");
                continue;
            case 0x1e:
                printf("ldc.i4.8");
                continue;
            case 0x1f: // ldc.i4.s X
                printf("ldc.i4.s 0x%02x", ilCode[i + 1]);
                i += 1;
                continue;
            case 0x20: // ldc.i4 XXXX
                printf("ldc.i4 0x%02x%02x%02x%02x", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x21: // ldc.i8 XXXXXXXX
                printf("ldc.i8 0x%02x%02x%02x%02x%02x%02x%02x%02x", ilCode[i + 8], ilCode[i + 7], ilCode[i + 6],
                       ilCode[i + 5], ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 8;
                continue;
            case 0x22: // ldc.r4 XXXX
                printf("ldc.r4 float32(0x%02x%02x%02x%02x)", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2],
                       ilCode[i + 1]);
                i += 4;
                continue;
            case 0x23: // ldc.r8 XXXXXXXX
                printf("ldc.r8 float64(0x%02x%02x%02x%02x%02x%02x%02x%02x)", ilCode[i + 8], ilCode[i + 7],
                       ilCode[i + 6], ilCode[i + 5], ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 8;
                continue;
            case 0x25:
                printf("dup");
                continue;
            case 0x26:
                printf("pop");
                continue;
            case 0x27: // JMP <T>
                printf("jmp <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x28: // call <T>
                printf("call <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x29: // calli <T>
                printf("calli <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x2a:
                printf("ret");
                continue;
            case 0x2b: // br.s X
                printf("br.s IL_%04x", i + 2 + ilCode[i + 1]);
                ;
                i += 1;
                continue;
            case 0x2c: // brfalse.s X
                printf("brfalse.s IL_%04x", i + 2 + ilCode[i + 1]);
                ;
                i += 1;
                continue;
            case 0x2d: // brtrue.s X
                printf("brtrue.s IL_%04x", i + 2 + ilCode[i + 1]);
                ;
                i += 1;
                continue;
            case 0x2e: // beq.s X
                printf("beq.s IL_%04x", i + 2 + ilCode[i + 1]);
                ;
                i += 1;
                continue;
            case 0x2f: // bgt.s X
                printf("bgt.s IL_%04x", i + 2 + ilCode[i + 1]);
                ;
                i += 1;
                continue;
            case 0x30: // bgt.s X
                printf("bgt.s IL_%04x", i + 2 + ilCode[i + 1]);
                ;
                i += 1;
                continue;
            case 0x31: // ble.s X
                printf("ble.s IL_%04x", i + 2 + ilCode[i + 1]);
                ;
                i += 1;
                continue;
            case 0x32: // blt.s X
                printf("blt.s IL_%04x", i + 2 + ilCode[i + 1]);
                ;
                i += 1;
                continue;
            case 0x33: // bne.un.s X
                printf("bne.un.s IL_%04x", i + 2 + ilCode[i + 1]);
                ;
                i += 1;
                continue;
            case 0x34: // bge.un.s X
                printf("bge.un.s IL_%04x", i + 2 + ilCode[i + 1]);
                ;
                i += 1;
                continue;
            case 0x35: // bgt.un.s X
                printf("bgt.un.s IL_%04x", i + 2 + ilCode[i + 1]);
                ;
                i += 1;
                continue;
            case 0x36: // ble.un.s X
                printf("ble.un.s IL_%04x", i + 2 + ilCode[i + 1]);
                ;
                i += 1;
                continue;
            case 0x37: // blt.un.s X
                printf("blt.un.s IL_%04x", i + 2 + ilCode[i + 1]);
                ;
                i += 1;
                continue;
            case 0x38: // br XXXX
                printf("br IL_%04x",
                       i + 5 + (ilCode[i + 4] << 24 | ilCode[i + 3] << 16 | ilCode[i + 2] << 8 | ilCode[i + 1]));
                i += 4;
                continue;
            case 0x39: // brfalse XXXX
                printf("brfalse IL_%04x",
                       i + 5 + (ilCode[i + 4] << 24 | ilCode[i + 3] << 16 | ilCode[i + 2] << 8 | ilCode[i + 1]));
                i += 4;
                continue;
            case 0x3a: // brtrue XXXX
                printf("brtrue IL_%04x",
                       i + 5 + (ilCode[i + 4] << 24 | ilCode[i + 3] << 16 | ilCode[i + 2] << 8 | ilCode[i + 1]));
                i += 4;
                continue;
            case 0x3b: // beq XXXX
                printf("beq IL_%04x",
                       i + 5 + (ilCode[i + 4] << 24 | ilCode[i + 3] << 16 | ilCode[i + 2] << 8 | ilCode[i + 1]));
                i += 4;
                continue;
            case 0x3c: // bgt XXXX
                printf("bgt IL_%04x",
                       i + 5 + (ilCode[i + 4] << 24 | ilCode[i + 3] << 16 | ilCode[i + 2] << 8 | ilCode[i + 1]));
                i += 4;
                continue;
            case 0x3d: // bgt XXXX
                printf("bgt IL_%04x",
                       i + 5 + (ilCode[i + 4] << 24 | ilCode[i + 3] << 16 | ilCode[i + 2] << 8 | ilCode[i + 1]));
                i += 4;
                continue;
            case 0x3e: // ble XXXX
                printf("ble IL_%04x",
                       i + 5 + (ilCode[i + 4] << 24 | ilCode[i + 3] << 16 | ilCode[i + 2] << 8 | ilCode[i + 1]));
                i += 4;
                continue;
            case 0x3f: // blt XXXX
                printf("blt IL_%04x",
                       i + 5 + (ilCode[i + 4] << 24 | ilCode[i + 3] << 16 | ilCode[i + 2] << 8 | ilCode[i + 1]));
                i += 4;
                continue;
            case 0x40: // bne.un XXXX
                printf("bne.un IL_%04x",
                       i + 5 + (ilCode[i + 4] << 24 | ilCode[i + 3] << 16 | ilCode[i + 2] << 8 | ilCode[i + 1]));
                i += 4;
                continue;
            case 0x41: // bge.un XXXX
                printf("bge.un IL_%04x",
                       i + 5 + (ilCode[i + 4] << 24 | ilCode[i + 3] << 16 | ilCode[i + 2] << 8 | ilCode[i + 1]));
                i += 4;
                continue;
            case 0x42: // bgt.un XXXX
                printf("bgt.un IL_%04x",
                       i + 5 + (ilCode[i + 4] << 24 | ilCode[i + 3] << 16 | ilCode[i + 2] << 8 | ilCode[i + 1]));
                i += 4;
                continue;
            case 0x43: // ble.un XXXX
                printf("ble.un IL_%04x",
                       i + 5 + (ilCode[i + 4] << 24 | ilCode[i + 3] << 16 | ilCode[i + 2] << 8 | ilCode[i + 1]));
                i += 4;
                continue;
            case 0x44: // blt.un XXXX
                printf("blt.un IL_%04x",
                       i + 5 + (ilCode[i + 4] << 24 | ilCode[i + 3] << 16 | ilCode[i + 2] << 8 | ilCode[i + 1]));
                i += 4;
                continue;
            case 0x45: // switch NNNN NNNN*XXXX
                printf("switch (0x%02x%02x%02x%02x)", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                k = (ilCode[i + 4] << 24) | (ilCode[i + 3] << 16) | (ilCode[i + 2] << 8) | (ilCode[i + 1] << 0);
                i += 4;
                for (j = 0; j < k; j++)
                {
                    printf(" <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                    i += 4;
                }
                continue;
            case 0x46:
                printf("ldind.i1");
                continue;
            case 0x47:
                printf("ldind.u1");
                continue;
            case 0x48:
                printf("ldind.i2");
                continue;
            case 0x49:
                printf("ldind.u2");
                continue;
            case 0x4a:
                printf("ldind.i4");
                continue;
            case 0x4b:
                printf("ldind.u4");
                continue;
            case 0x4c:
                printf("ldind.i8");
                continue;
            case 0x4d:
                printf("ldind.u8");
                continue;
            case 0x4e:
                printf("ldind.r4");
                continue;
            case 0x4f:
                printf("ldind.r8");
                continue;
            case 0x50:
                printf("ldind.ref");
                continue;
            case 0x51:
                printf("stind.ref");
                continue;
            case 0x52:
                printf("stind.i1");
                continue;
            case 0x53:
                printf("stind.i2");
                continue;
            case 0x54:
                printf("stind.i4");
                continue;
            case 0x55:
                printf("stind.i8");
                continue;
            case 0x56:
                printf("stind.r4");
                continue;
            case 0x57:
                printf("stind.r8");
                continue;
            case 0x58:
                printf("add");
                continue;
            case 0x59:
                printf("sub");
                continue;
            case 0x5a:
                printf("mul");
                continue;
            case 0x5b:
                printf("div");
                continue;
            case 0x5c:
                printf("div.un");
                continue;
            case 0x5d:
                printf("rem");
                continue;
            case 0x5e:
                printf("rem.un");
                continue;
            case 0x5f:
                printf("and");
                continue;
            case 0x60:
                printf("or");
                continue;
            case 0x61:
                printf("xor");
                continue;
            case 0x62:
                printf("shl");
                continue;
            case 0x63:
                printf("shr");
                continue;
            case 0x64:
                printf("shr.un");
                continue;
            case 0x65:
                printf("neg");
                continue;
            case 0x66:
                printf("not");
                continue;
            case 0x67:
                printf("conv.i1");
                continue;
            case 0x68:
                printf("conv.i2");
                continue;
            case 0x69:
                printf("conv.i4");
                continue;
            case 0x6a:
                printf("conv.i8");
                continue;
            case 0x6b:
                printf("conv.r4");
                continue;
            case 0x6c:
                printf("conv.r8");
                continue;
            case 0x6d:
                printf("conv.u4");
                continue;
            case 0x6e:
                printf("conv.u8");
                continue;
            case 0x6f: // callvirt <T>
                printf("callvirt <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x70: // cpobj <T>
                printf("cpobj <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x71: // ldobj <T>
                printf("ldobj <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x72: // ldstr <T>
                printf("ldstr <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x73: // newobj <T>
                printf("newobj <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x74: // castclass <T>
                printf("castclass <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x75: // isinst <T>
                printf("isinst <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x76:
                printf("conv.r.un");
                continue;
            case 0x79: // unbox <T>
                printf("unbox <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x7a:
                printf("throw");
                continue;
            case 0x7b: // ldfld <T>
                printf("ldfld <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x7c: // ldflda <T>
                printf("ldflda <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x7d: // stfld <T>
                printf("stfld <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x7e: // ldsfld <T>
                printf("ldsfld <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x7f: // ldsflda <T>
                printf("ldsflda <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x80: // stsfld <T>
                printf("stsfld <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x81: // stobj <T>
                printf("stobj <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x82:
                printf("conv.ovf.i1.un");
                continue;
            case 0x83:
                printf("conv.ovf.i2.un");
                continue;
            case 0x84:
                printf("conv.ovf.i4.un");
                continue;
            case 0x85:
                printf("conv.ovf.i8.un");
                continue;
            case 0x86:
                printf("conv.ovf.u1.un");
                continue;
            case 0x87:
                printf("conv.ovf.u2.un");
                continue;
            case 0x88:
                printf("conv.ovf.u4.un");
                continue;
            case 0x89:
                printf("conv.ovf.u8.un");
                continue;
            case 0x8a:
                printf("conv.ovf.i.un");
                continue;
            case 0x8b:
                printf("conv.ovf.u.un");
                continue;
            case 0x8c: // box <T>
                printf("box <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x8d: // newarr <T>
                printf("newarr <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x8e:
                printf("ldlen");
                continue;
            case 0x8f:
                printf("ldelema <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0x90:
                printf("ldelem.i1");
                continue;
            case 0x91:
                printf("ldelem.u1");
                continue;
            case 0x92:
                printf("ldelem.i2");
                continue;
            case 0x93:
                printf("ldelem.u2");
                continue;
            case 0x94:
                printf("ldelem.i4");
                continue;
            case 0x95:
                printf("ldelem.u4");
                continue;
            case 0x96:
                printf("ldelem.i8");
                continue;
            case 0x97:
                printf("ldelem.i");
                continue;
            case 0x98:
                printf("ldelem.r4");
                continue;
            case 0x99:
                printf("ldelem.r8");
                continue;
            case 0x9a:
                printf("ldelem.ref");
                continue;
            case 0x9b:
                printf("stelem.i");
                continue;
            case 0x9c:
                printf("stelem.i1");
                continue;
            case 0x9d:
                printf("stelem.i2");
                continue;
            case 0x9e:
                printf("stelem.i4");
                continue;
            case 0x9f:
                printf("stelem.i8");
                continue;
            case 0xa0:
                printf("stelem.r4");
                continue;
            case 0xa1:
                printf("stelem.r8");
                continue;
            case 0xa2:
                printf("stelem.ref");
                continue;
            case 0xa3:
                printf("stelem <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0xa4:
                printf("stelem <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0xa5:
                printf("unbox.any <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0xb3:
                printf("conv.ovf.i1");
                continue;
            case 0xb4:
                printf("conv.ovf.u1");
                continue;
            case 0xb5:
                printf("conv.ovf.i2");
                continue;
            case 0xb6:
                printf("conv.ovf.u2");
                continue;
            case 0xb7:
                printf("conv.ovf.i4");
                continue;
            case 0xb8:
                printf("conv.ovf.u4");
                continue;
            case 0xb9:
                printf("conv.ovf.i8");
                continue;
            case 0xba:
                printf("conv.ovf.u8");
                continue;
            case 0xc2: // refanyval <T>
                printf("refanyval <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0xc3:
                printf("ckfinite");
                continue;
            case 0xc6: // mkrefany <T>
                printf("mkrefany <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0xd0: // ldtoken <T>
                printf("ldtoken <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0xd1:
                printf("conv.u2");
                continue;
            case 0xd2:
                printf("conv.u1");
                continue;
            case 0xd3:
                printf("conv.i");
                continue;
            case 0xd4:
                printf("conv.ovf.i");
                continue;
            case 0xd5:
                printf("conv.ovf.u");
                continue;
            case 0xd6:
                printf("add.ovf");
                continue;
            case 0xd7:
                printf("add.ovf.un");
                continue;
            case 0xd8:
                printf("mul.ovf");
                continue;
            case 0xd9:
                printf("mul.ovf.un");
                continue;
            case 0xda:
                printf("sub.ovf");
                continue;
            case 0xdb:
                printf("sub.ovf.un");
                continue;
            case 0xdc:
                printf("endfinally");
                continue;
            case 0xdd: // leave XXXX
                printf("leave 0x%02x%02x%02x%02x", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2], ilCode[i + 1]);
                i += 4;
                continue;
            case 0xde: // leave.s X
                printf("leave 0x%02x", ilCode[i + 1]);
                i += 1;
                continue;
            case 0xdf:
                printf("stind.i");
                continue;
            case 0xe0:
                printf("conv.u");
                continue;
            case 0xfe:
                i++;
                switch (ilCode[i])
                {
                    case 0x00:
                        printf("arglist");
                        continue;
                    case 0x01:
                        printf("ceq");
                        continue;
                    case 0x02:
                        printf("cgt");
                        continue;
                    case 0x03:
                        printf("cgt.un");
                        continue;
                    case 0x04:
                        printf("clt");
                        continue;
                    case 0x05:
                        printf("clt.un");
                        continue;
                    case 0x06: // ldftn <T>
                        printf("ldftn <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2],
                               ilCode[i + 1]);
                        i += 4;
                        continue;
                    case 0x07: // ldvirtftn <T>
                        printf("ldvirtftn <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2],
                               ilCode[i + 1]);
                        i += 4;
                        continue;
                    case 0x09: // ldarg XX
                        printf("ldarg 0x%02x%02x", ilCode[i + 2], ilCode[i + 1]);
                        i += 2;
                        continue;
                    case 0x0a: // ldarga XX
                        printf("ldarga 0x%02x%02x", ilCode[i + 2], ilCode[i + 1]);
                        i += 2;
                        continue;
                    case 0x0b: // starg XX
                        printf("starg 0x%02x%02x", ilCode[i + 2], ilCode[i + 1]);
                        i += 2;
                        continue;
                    case 0x0c: // ldloc XX
                        printf("ldloc 0x%02x%02x", ilCode[i + 2], ilCode[i + 1]);
                        i += 2;
                        continue;
                    case 0x0d: // ldloca XX
                        printf("ldloca 0x%02x%02x", ilCode[i + 2], ilCode[i + 1]);
                        i += 2;
                        continue;
                    case 0x0e: // stloc XX
                        printf("stloc 0x%02x%02x", ilCode[i + 2], ilCode[i + 1]);
                        i += 2;
                        continue;
                    case 0x0f:
                        printf("localloc");
                        continue;
                    case 0x11:
                        printf("endfilter");
                        continue;
                    case 0x12: // unaligned X
                        printf("unaligned. 0x%02x", ilCode[i + 1]);
                        i += 1;
                        continue;
                    case 0x13:
                        printf("volatile.");
                        continue;
                    case 0x14:
                        printf("tail.");
                        continue;
                    case 0x15: // initobj <T>
                        printf("initobj <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2],
                               ilCode[i + 1]);
                        i += 4;
                        continue;
                    case 0x16: // incomplete?
                        printf("constrained. <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2],
                               ilCode[i + 1]);
                        i += 4;
                        continue;
                    case 0x17:
                        printf("cpblk");
                        continue;
                    case 0x18:
                        printf("initblk");
                        continue;
                    case 0x19:
                        printf("no.");
                        continue; // incomplete?
                    case 0x1a:
                        printf("rethrow");
                        continue;
                    case 0x1c: // sizeof <T>
                        printf("sizeof <0x%02x%02x%02x%02x>", ilCode[i + 4], ilCode[i + 3], ilCode[i + 2],
                               ilCode[i + 1]);
                        i += 4;
                        continue;
                    case 0x1d:
                        printf("refanytype");
                        continue;
                    default:
                        LogError("unknown ilCode 0xfe%2x at offset %d in MethodGen::PrettyPrint", ilCode[i], i);
                        break;
                }
                break;
            default:
                LogError("unknown ilCode 0x%02x at offset %d in MethodGen::PrettyPrint", ilCode[i], i);
                break;
        }
        printf("\n");
    }
}
char* DumpAttributeToConsoleBare(DWORD attribute)
{
    const char* s_static      = "static";
    const char* s_dontInline  = "$dontInline ";
    const char* s_constructor = "$constructor";

#define ifPrint(s, t)                                                                                                  \
    else if ((s & attribute) == s)                                                                                     \
    {                                                                                                                  \
        printf(t);                                                                                                     \
        printf(" ");                                                                                                   \
    }

    if (0)
        ;
    ifPrint(CORINFO_FLG_STATIC, s_static)
    ifPrint(CORINFO_FLG_DONT_INLINE, s_dontInline)
    ifPrint(CORINFO_FLG_CONSTRUCTOR, s_constructor)
    return nullptr;

#undef ifPrint
}

void DumpIL(MethodContext* mc)
{
    CORINFO_METHOD_INFO cmi;
    unsigned int        flags = 0;
    CORINFO_OS          os    = CORINFO_WINNT;

    mc->repCompileMethod(&cmi, &flags, &os);

    const char* moduleName = nullptr;
    const char* methodName = mc->repGetMethodName(cmi.ftn, &moduleName);
    const char* className  = mc->repGetClassName(mc->repGetMethodClass(cmi.ftn));

    printf("// ProcessName - '%s'\n", mc->cr->repProcessName());
    printf(".assembly extern mscorlib{}\n");
    printf(".assembly %s{}\n", moduleName);
    printf(".class %s\n", className);
    printf("{\n");
    printf("   .method ");
    DumpAttributeToConsoleBare(mc->repGetMethodAttribs(cmi.ftn));
    DumpPrimToConsoleBare(mc, cmi.args.retType, CastHandle(cmi.args.retTypeClass));
    printf(" %s(", methodName);
    DumpSigToConsoleBare(mc, &cmi.args);
    printf(")\n");
    printf("   {\n");
    printf("      .maxstack  %u\n", cmi.maxStack);
    printf("      .locals%s(", (((cmi.options & CORINFO_OPT_INIT_LOCALS) == CORINFO_OPT_INIT_LOCALS) ? " init " : " "));
    DumpSigToConsoleBare(mc, &cmi.locals);
    printf(")\n");
    DumpILToConsoleBare(cmi.ILCode, cmi.ILCodeSize);
    printf("   }\n");
    printf("}\n");
}

int verbILDump::DoWork(const char* nameOfInput, int indexCount, const int* indexes)
{
    LogVerbose("// Reading from '%s' dumping raw IL for MC Indexes to console", nameOfInput);

    MethodContextIterator mci(indexCount, indexes);
    if (!mci.Initialize(nameOfInput))
        return -1;

    int dumpedCount = 0;

    while (mci.MoveNext())
    {
        MethodContext* mc = mci.Current();
        DumpIL(mc);
        dumpedCount++;
    }

    LogInfo("// Dumped %d", dumpedCount);

    if (!mci.Destroy())
        return -1;

    return 0;
}
