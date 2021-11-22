// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}


.assembly extern mscorlib
{
    .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
    .ver 4:0:0:0
}

.assembly UnalignedDouble1
{
    .hash algorithm 0x00008004
    .ver 4:0:30319:16507
}
.assembly extern xunit.core {}

.module UnalignedDouble1.exe
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    // ILONLY


// =============== CLASS MEMBERS DECLARATION ===================

.class public abstract auto ansi sealed beforefieldinit UnalignedDouble1.PtrAccess
       extends [mscorlib]System.Object
{
    .method public hidebysig static float64 
            LoadUnalignedDouble(float64* unalignedPtr) cil managed noinlining
    {
        // Code size       3 (0x3)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  unaligned.    1
                  ldind.r8
        IL_0002:  ret
    }

    .method public hidebysig static void  StoreUnalignedDouble(float64* unalignedPtr,
                                                               float64 newValue) cil managed noinlining
    {
        // Code size       4 (0x4)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  ldarg.1
        IL_0002:  unaligned.    1
                  stind.r8
        IL_0003:  ret
    }

}

.class public explicit ansi sealed beforefieldinit UnalignedDouble1.UnalignedDoubleHolder
       extends [mscorlib]System.ValueType
{
    .field [0] public uint8 LeadingByte
    .field [1] public float64 UnalignedDouble
    .method public hidebysig static valuetype UnalignedDouble1.UnalignedDoubleHolder 
            GetDefaultInstance() cil managed
    {
        // Code size       43 (0x2b)
        .maxstack  2
        .locals init ([0] valuetype UnalignedDouble1.UnalignedDoubleHolder holder,
                 [1] float64* unalignedPtr)
        IL_0000:  ldloca.s   holder
        IL_0002:  initobj    UnalignedDouble1.UnalignedDoubleHolder
        IL_0008:  ldloca.s   holder
        IL_000a:  ldc.i4.s   51
        IL_000c:  stfld      uint8 UnalignedDouble1.UnalignedDoubleHolder::LeadingByte
        IL_0011:  ldloca.s   holder
        IL_0013:  ldflda     float64 UnalignedDouble1.UnalignedDoubleHolder::UnalignedDouble
        IL_0018:  conv.u
        IL_0019:  stloc.1
        IL_001a:  ldloc.1
        IL_001b:  ldc.r8     123.456
        IL_0024:  call       void UnalignedDouble1.PtrAccess::StoreUnalignedDouble(float64*,
                                                                                   float64)
        IL_0029:  ldloc.0
        IL_002a:  ret
    }

}

.class private abstract auto ansi sealed beforefieldinit UnalignedDouble1.App
       extends [mscorlib]System.Object
{
    .method private hidebysig static void  DereferenceAndDisplayUnalignedPointer(float64* unalignedPtr) cil managed noinlining
    {
        // Code size       24 (0x18)
        .maxstack  2
        .locals init ([0] float64 valueToDisplay)
        IL_0000:  ldarg.0
        IL_0001:  call       float64 UnalignedDouble1.PtrAccess::LoadUnalignedDouble(float64*)
        IL_0006:  stloc.0
        IL_0007:  ldstr      "VALUE: ({0})"
        IL_000c:  ldloc.0
        IL_000d:  box        [mscorlib]System.Double
        IL_0012:  call       void [System.Console]System.Console::WriteLine(string,
                                                                      object)
        IL_0017:  ret
    }

    .method private hidebysig static void  UseUnalignedDouble() cil managed noinlining
    {
        // Code size       22 (0x16)
        .maxstack  1
        .locals init ([0] valuetype UnalignedDouble1.UnalignedDoubleHolder holder,
                 [1] float64* unalignedPtr)
        IL_0000:  call       valuetype UnalignedDouble1.UnalignedDoubleHolder UnalignedDouble1.UnalignedDoubleHolder::GetDefaultInstance()
        IL_0005:  stloc.0
        IL_0006:  ldloca.s   holder
        IL_0008:  ldflda     float64 UnalignedDouble1.UnalignedDoubleHolder::UnalignedDouble
        IL_000d:  conv.u
        IL_000e:  stloc.1
        IL_000f:  ldloc.1
        IL_0010:  call       void UnalignedDouble1.App::DereferenceAndDisplayUnalignedPointer(float64*)
        IL_0015:  ret
    }

    .method private hidebysig static int32  Main() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
        // Code size       6 (0x6)
        .maxstack  8
        IL_0000:  call       void UnalignedDouble1.App::UseUnalignedDouble()
        IL_0006:  ldc.i4 100
        IL_0008:  ret
    }

}

