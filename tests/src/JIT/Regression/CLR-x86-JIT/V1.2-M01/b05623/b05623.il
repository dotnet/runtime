// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern Microsoft.VisualC
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .hash = (EC 47 1C 7B E2 10 BB EE 15 E2 3B 4E FB 55 44 9D   // .G.{......;N.UD.
           E2 D0 4D B7 )                                     // ..M.
  .ver 7:0:3300:0
}
.assembly t
{

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 00 01 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module t.exe
// MVID: {B0E04E8C-E35E-406B-87D5-0DB207C0D0AC}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001
// Image base: 0x02fe0000

// =============== CLASS MEMBERS DECLARATION ===================

.method public static char modopt([Microsoft.VisualC]Microsoft.VisualC.IsConstModifier)& modopt([mscorlib]System.Runtime.CompilerServices.CallConvCdecl) 
        PtrToStringChars(string modopt([Microsoft.VisualC]Microsoft.VisualC.IsConstModifier) s) cil managed
{
  .vtentry 3 : 1
  // Code size       24 (0x18)
  .maxstack  2
  .locals (unsigned int8 modopt([Microsoft.VisualC]Microsoft.VisualC.IsConstModifier)& pinned modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) V_0,
           unsigned int32 V_1,
           string modopt([Microsoft.VisualC]Microsoft.VisualC.IsConstModifier) pinned modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) V_2)
  IL_0000:  ldnull
  IL_0001:  stloc.2
  IL_0002:  ldarg.0
  IL_0003:  stloc.2
  IL_0004:  ldarg.0
  IL_0005:  conv.i4
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0014

  IL_000a:  call       int32 [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::get_OffsetToStringData()
  IL_000f:  stloc.1
  IL_0010:  ldloc.0
  IL_0011:  ldloc.1
  IL_0012:  add
  IL_0013:  stloc.0
  IL_0014:  ldloc.0
  IL_0015:  br.s       IL_0017

  IL_0017:  ret
} // end of global method PtrToStringChars


.class private auto ansi beforefieldinit T
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main(string[] args) cil managed
  {
    .entrypoint
    // Code size       9 (0x9)
    .maxstack  1
	ldc.i4 100
    IL_0008:  ret
  } // end of method T::Main


    .method family static void  StringToPtrFast(string pstrSrc,
                                                int32 offset,
                                                char* pwchDest,
                                                int32 modopt([Microsoft.VisualC]Microsoft.VisualC.IsLongModifier) cwchSize) cil managed
    {
      // Code size       64 (0x40)
      .maxstack  3
      .locals (char modopt([Microsoft.VisualC]Microsoft.VisualC.IsConstModifier)& pinned modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) V_0,
               int32 V_1)
      IL_0000:  ldc.i4.0
      IL_0001:  ldarg.3
      IL_0002:  bge.s      IL_003f

      IL_0004:  ldarg.0
      IL_0005:  call       instance int32 [mscorlib]System.String::get_Length()
      IL_000a:  ldarg.1
      IL_000b:  ldarg.3
      IL_000c:  add
      IL_000d:  blt.s      IL_0039

      IL_000f:  ldarg.0
      IL_0010:  call       char modopt([Microsoft.VisualC]Microsoft.VisualC.IsConstModifier)& modopt([mscorlib]System.Runtime.CompilerServices.CallConvCdecl) PtrToStringChars(string modopt([Microsoft.VisualC]Microsoft.VisualC.IsConstModifier))
      IL_0015:  stloc.0
      IL_0016:  ldloc.0
      IL_0017:  ldarg.1
      IL_0018:  ldc.i4.2
      IL_0019:  mul
      IL_001a:  add
      IL_001b:  stloc.0
      IL_001c:  ldc.i4.0
      IL_001d:  stloc.1
      IL_001e:  ldloc.1
      IL_001f:  ldarg.3
      IL_0020:  bge.s      IL_0035

      IL_0022:  ldarg.2
      IL_0023:  ldloc.0
      IL_0024:  ldind.u2
      IL_0025:  stind.i2
      IL_0026:  ldarg.2
      IL_0027:  ldc.i4.2
      IL_0028:  add
      IL_0029:  starg.s    pwchDest
      IL_002b:  ldloc.0
      IL_002c:  ldc.i4.2
      IL_002d:  add
      IL_002e:  stloc.0
      IL_002f:  ldloc.1
      IL_0030:  ldc.i4.1
      IL_0031:  add
      IL_0032:  stloc.1
      IL_0033:  br.s       IL_001e

      IL_0035:  ldc.i4.0
      IL_0036:  stloc.0
      IL_0037:  br.s       IL_003f

      IL_0039:  newobj     instance void [mscorlib]System.IndexOutOfRangeException::.ctor()
      IL_003e:  throw

      IL_003f:  ret
    } // end of method CXVariantBase::StringToPtrFast



} // end of class T



// =============================================================


//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file repro.res
