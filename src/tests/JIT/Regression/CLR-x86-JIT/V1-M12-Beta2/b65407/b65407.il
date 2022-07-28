// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b65407'
{
}
.assembly extern xunit.core {}
// MVID: {EAE7CF60-60E9-49D2-84F9-F450B7E26DE4}

.file alignment 512

// Image base: 0x03110000
.class private auto ansi beforefieldinit sample
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       62 (0x3e)
    .maxstack  1
    .locals init ([0] int64 i,
                  [1] object o,
                  [2] object[] _Vb_t_array_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  box        [mscorlib]System.Boolean
    IL_0007:  call       object [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::GetObjectValue(object)
    IL_000c:  stloc.1
    IL_000d:  ldloc.1
    IL_000e:  unbox      [mscorlib]System.Boolean
    IL_0013:  ldobj      [mscorlib]System.Boolean
    IL_0018:  conv.i4
    IL_0019:  conv.i8
    IL_001a:  stloc.0
    IL_001b:  ldstr      "bool to int32"
    IL_0020:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0025:  ldloc.1
    IL_0026:  unbox      [mscorlib]System.Boolean
    IL_002b:  ldobj      [mscorlib]System.Boolean
    IL_0030:  conv.i8
    IL_0031:  stloc.0
    IL_0032:  ldstr      "bool to int64"
    IL_0037:  call       void [System.Console]System.Console::WriteLine(string)
    IL_003c:  nop
    
    		  ldc.i4 100
    IL_003d:  ret
  } // end of method sample::Main

  .method public hidebysig specialname rtspecialname 
          instance void .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method sample::.ctor

} // end of class sample

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file sample.res
