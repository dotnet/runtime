// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME
{
}
.assembly extern xunit.core {}
.namespace JitTest_implicit_arg_coercion
{
  .class public auto ansi beforefieldinit TestClass
         extends [mscorlib]System.Object
  {
    .field private static native int s_data
    .field private static native uint s_udata

    .method public hidebysig static native int TakeNativeIntArg(native int) cil managed
    {
        ldarg.0
        ret
    }

    .method public hidebysig static native uint TakeNativeUIntArg(native uint) cil managed
    {
        ldarg.0
        ret
    }

    .method public hidebysig static int32 TakeU2Arg(uint16) cil managed
    {
        ldarg.0
        ret
    }

    .method public hidebysig static int32 TakeU1Arg(uint8) cil managed
    {
        ldarg.0
        ret
    }

    .method public hidebysig static int32
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.SkipOnMonoAttribute::.ctor(string, valuetype [Microsoft.DotNet.XUnitExtensions]Xunit.TestPlatforms) = {
          string('Handling of implicit arg coercion rules III.1.6 differs between Mono and CoreCLR, and this tests the CoreCLR behavior, and what we've put in the augments')
          int32(0xFFFFFFFF) // Any
      }

      .entrypoint
      // Code size       36 (0x24)
      .maxstack  2
      ldc.i4.m1
      stsfld native int JitTest_implicit_arg_coercion.TestClass::s_data
      ldc.i4.m1
      stsfld native uint JitTest_implicit_arg_coercion.TestClass::s_udata

      ldsfld native int JitTest_implicit_arg_coercion.TestClass::s_data
      conv.u8
      box        [mscorlib]System.UInt64
      call       void [System.Console]System.Console::WriteLine(object)

      ldsfld native uint JitTest_implicit_arg_coercion.TestClass::s_udata
      conv.u8
      box        [mscorlib]System.UInt64
      call       void [System.Console]System.Console::WriteLine(object)

      ldc.i4.m1
      conv.i
      box        [mscorlib]System.UInt64
      call       void [System.Console]System.Console::WriteLine(object)

      ldc.i4.m1
      conv.u
      box        [mscorlib]System.UInt64
      call       void [System.Console]System.Console::WriteLine(object)

      ldc.i4.m1
      conv.i
      ldsfld native int JitTest_implicit_arg_coercion.TestClass::s_data
      beq IL_PassNativeInt
      ldstr      "Failed conversion of i4 into i for stsfld"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s 101
      ret

      IL_PassNativeInt:
      ldc.i4.m1
      conv.i // The published ECMA 335 spec specifies that we should use the implicit argument coercion rules in section III.
      ldsfld native uint JitTest_implicit_arg_coercion.TestClass::s_udata
      beq IL_PassNativeUInt
      ldstr      "Failed conversion of i4 into u for stsfld"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s 102
      ret
      IL_PassNativeUInt:

      ldc.i4.m1
      call native int [ASSEMBLY_NAME]JitTest_implicit_arg_coercion.TestClass::TakeNativeIntArg(native int)
      ldc.i4.m1
      conv.i
      beq IL_PassNativeIntCall
      ldstr      "Failed conversion of i4 into i for argument call"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s 103
      ret
      IL_PassNativeIntCall:

      ldc.i4.m1
      call native uint [ASSEMBLY_NAME]JitTest_implicit_arg_coercion.TestClass::TakeNativeUIntArg(native uint)
      ldc.i4.m1
      conv.i
      beq IL_PassNativeUIntCall
      ldstr      "Failed conversion of i4 into u for argument call"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s 104
      ret
      IL_PassNativeUIntCall:

      ldc.i4.m1
      call int32 [ASSEMBLY_NAME]JitTest_implicit_arg_coercion.TestClass::TakeU2Arg(uint16)
      ldc.i4 0xFFFF
      beq IL_PassU2Call
      ldstr      "Failed conversion of i4 into u2 for argument call"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s 105
      ret
      IL_PassU2Call:

      ldc.i4.m1
      call int32 [ASSEMBLY_NAME]JitTest_implicit_arg_coercion.TestClass::TakeU1Arg(uint8)
      ldc.i4 0xFF
      beq IL_PassU1Call
      ldstr      "Failed conversion of i4 into u1 for argument call"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s 105
      ret
      IL_PassU1Call:

      ldstr      "Passed => 100"
      IL_001c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0021:  ldc.i4.s   100
      IL_0023:  ret
    } // end of method TestClass::Main

    .method public hidebysig specialname rtspecialname
            instance void .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method TestClass::.ctor

  } // end of class TestClass

} // end of namespace JitTest_implicit_arg_coercion

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file ldnull.res