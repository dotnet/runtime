// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}




/*

This test verifies that InitializeArray works on array of enums both with MinOpts on and off.

Template used to create this program

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

enum Color
{
   Red,
   Green,
   Blue
};
   
class My {

    static int[] r = new int[] { 1, 2, 3, 2, 1 };

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static Color[] WithoutMinOpts()
    {
        // Replaced with call to InitializeArray from cctor
        return new Color[1];
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining|MethodImplOptions.NoOptimization)]
    static Color[] WithMinOpts()
    {
        // Replaced with call to InitializeArray from cctor
        return new Color[1];
    }    

    static int Main()
    {
        if (!((IStructuralEquatable)WithoutMinOpts()).Equals(WithMinOpts(), EqualityComparer<Color>.Default))
        {
            Console.WriteLine("FAILED");
            return 1;
        }

        Console.WriteLine("PASSED");
        return 100;
    }
}

*/

// Metadata version: v4.0.x86chk
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly initializearray_enum
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
// MVID: {88A311DA-3D49-42AC-8FA7-6DB1266177BC}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00260000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi sealed Color
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Color Red = int32(0x00000000)
  .field public static literal valuetype Color Green = int32(0x00000001)
  .field public static literal valuetype Color Blue = int32(0x00000002)
} // end of class Color

.class public auto ansi beforefieldinit My
       extends [mscorlib]System.Object
{
  .method private hidebysig static valuetype Color[] 
          WithoutMinOpts() cil managed noinlining
  {
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldc.i4.5
    IL_0001:  newarr     Color
    IL_0006:  dup
    IL_0007:  ldtoken    field valuetype '<PrivateImplementationDetails>{088B7B6E-A29C-421F-A591-353A450BF543}'/'__StaticArrayInitTypeSize=20' '<PrivateImplementationDetails>{088B7B6E-A29C-421F-A591-353A450BF543}'::'$$method0x6000005-1'
    IL_000c:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                        valuetype [mscorlib]System.RuntimeFieldHandle)
    IL_0011:  ret
  } // end of method My::WithoutMinOpts

  .method private hidebysig static valuetype Color[] 
          WithMinOpts() cil managed noinlining nooptimization
  {
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldc.i4.5
    IL_0001:  newarr     Color
    IL_0006:  dup
    IL_0007:  ldtoken    field valuetype '<PrivateImplementationDetails>{088B7B6E-A29C-421F-A591-353A450BF543}'/'__StaticArrayInitTypeSize=20' '<PrivateImplementationDetails>{088B7B6E-A29C-421F-A591-353A450BF543}'::'$$method0x6000005-1'
    IL_000c:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                        valuetype [mscorlib]System.RuntimeFieldHandle)
    IL_0011:  ret
  } // end of method My::WithMinOpts

  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       47 (0x2f)
    .maxstack  8
    IL_0000:  call       valuetype Color[] My::WithoutMinOpts()
    IL_0005:  call       valuetype Color[] My::WithMinOpts()
    IL_000a:  call       class [mscorlib]System.Collections.Generic.EqualityComparer`1<!0> class [mscorlib]System.Collections.Generic.EqualityComparer`1<valuetype Color>::get_Default()
    IL_000f:  callvirt   instance bool [mscorlib]System.Collections.IStructuralEquatable::Equals(object,
                                                                                                 class [mscorlib]System.Collections.IEqualityComparer)
    IL_0014:  brtrue.s   IL_0022

    IL_0016:  ldstr      "FAILED"
    IL_001b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0020:  ldc.i4.1
    IL_0021:  ret

    IL_0022:  ldstr      "PASSED"
    IL_0027:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002c:  ldc.i4.s   100
    IL_002e:  ret
  } // end of method My::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method My::.ctor

} // end of class My

.class private auto ansi '<PrivateImplementationDetails>{088B7B6E-A29C-421F-A591-353A450BF543}'
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .class explicit ansi sealed nested private '__StaticArrayInitTypeSize=20'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 20
  } // end of class '__StaticArrayInitTypeSize=20'

  .field static assembly valuetype '<PrivateImplementationDetails>{088B7B6E-A29C-421F-A591-353A450BF543}'/'__StaticArrayInitTypeSize=20' '$$method0x6000005-1' at I_00002050
} // end of class '<PrivateImplementationDetails>{088B7B6E-A29C-421F-A591-353A450BF543}'


// =============================================================

.data cil I_00002050 = bytearray (
                 01 00 00 00 02 00 00 00 03 00 00 00 02 00 00 00
                 01 00 00 00) 
// *********** DISASSEMBLY COMPLETE ***********************
