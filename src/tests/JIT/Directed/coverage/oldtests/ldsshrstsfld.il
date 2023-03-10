// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ldsshrstsfld {}
.class public auto ansi beforefieldinit ldsshrstsfld
       extends [mscorlib]System.Object
{
  .field private static char c
  .method private hidebysig static void  f1() cil managed
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldsfld     char ldsshrstsfld::c
    IL_0005:  ldc.i4 0
    IL_0006:  shr
    IL_0007:  //conv.u2
    IL_0008:  stsfld     char ldsshrstsfld::c
    IL_000d:  ldsfld     char ldsshrstsfld::c
    IL_0012:  call       void [System.Console]System.Console::WriteLine(char)
    IL_0017:  ret
  }

    .method private hidebysig static void  f2() cil managed
  {
    // Code size       50 (0x32)
    .maxstack  3
    .locals init (class [mscorlib]System.Random V_0)
    IL_0000:  newobj     instance void [mscorlib]System.Random::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  ldc.i4.s   32
    IL_0009:  ldc.i4.s   64
    IL_000b:  callvirt   instance int32 [mscorlib]System.Random::Next(int32,
                                                                      int32)
    IL_0010:  call       char [mscorlib]System.Convert::ToChar(int32)
    IL_0015:  stsfld     char ldsshrstsfld::c
    IL_001a:  ldsfld     char ldsshrstsfld::c
    IL_001f:  ldc.i4 0
    IL_0020:  shr
    IL_0021:  //conv.u2
    IL_0022:  stsfld     char ldsshrstsfld::c
    IL_0027:  ldsfld     char ldsshrstsfld::c
    IL_002c:  call       void [System.Console]System.Console::WriteLine(char)
    IL_0031:  ret
  }

   .method public hidebysig static int32 Main() cil managed
  {
  	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
  	    01 00 00 00
  	)
  	.entrypoint
  	call void ldsshrstsfld::f1()
  	call void ldsshrstsfld::f2()
  	ldc.i4 100
  	ret
  }

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldc.i4.s   99
    IL_0002:  stsfld     char ldsshrstsfld::c
    IL_0007:  ret
  } // end of method ldsshrstsfld::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method ldsshrstsfld::.ctor

}
