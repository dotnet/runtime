// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.





.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
}
.assembly ConsoleApplication2
{


  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 39 35 33 34 45 45 36 46 2D 36 43 43 35   
                                                                                                  2D 34 36 42 37 2D 39 32 30 32 2D 36 42 35 46 44   
                                                                                                  34 43 37 38 34 45 34 00 00 )                      
  .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = ( 01 00 01 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.AssemblyTrademarkAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.AssemblyCopyrightAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.AssemblyProductAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.AssemblyCompanyAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.AssemblyDescriptionAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.AssemblyTitleAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 1:0:583:22480
}
.module ConsoleApplication2.exe
.imagebase 0x11000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       
.corflags 0x00000001    


.namespace ConsoleApplication2
{
  .class private auto ansi sealed Module1
         extends [mscorlib]System.Object
  {
    .method public static int32  Main() cil managed
    {
      .entrypoint
      .maxstack  2
      .locals init (bool V_0,
               int16 V_1,
               object V_2)
      IL_0000:  nop
      IL_0001:  ldc.i4.s   16
      IL_0003:  stloc.1
      IL_0004:  ldloc.1
      IL_0005:  box        [mscorlib]System.Int16
      IL_000a:  stloc.2
      IL_000b:  ldloc.2
      IL_000c:  unbox      [mscorlib]System.Int16
      IL_0011:  ldobj      [mscorlib]System.Int16
      IL_0016:  ldc.i4.0
      IL_0017:  cgt.un
      IL_0019:  stloc.0
      IL_001a:  ldloc.0
      IL_001b:  brfalse.s  IL_0028

      IL_001d:  ldstr      "test"
      IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0027:  nop
      IL_0028:  nop
      IL_0029:  nop
                ldc.i4     100
      IL_002a:  ret
    } 

  } 

} 
