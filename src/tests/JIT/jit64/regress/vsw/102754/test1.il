// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.






.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern legacy library mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .hash = (34 49 DD 73 C6 4A 4D D1 3C F6 93 9D DA 59 9C EF   
           01 2A 9C E3 )                                     
  
}
.assembly legacy library 'test1_102754'
{


  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.custom instance void [mscorlib]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       
.corflags 0x00000001    



.namespace hello
{
  .class public auto ansi beforefieldinit Class1
         extends [mscorlib]System.Object
  {
    .method public hidebysig static int32 Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  2
      .locals (int64* V_0,
               int64 V_1,
               int32 V_2)
                // V_1 = (long) 0
      IL_0000:  ldc.i4.0
      IL_0001:  conv.i8
      IL_0002:  stloc.1

                // V_0 = &V_1
      IL_0003:  ldloca.s   V_1
      IL_0005:  stloc.0

                // (Endianness assumption)
                // (Also note that + is not scaled to sizeof(int64))
                // *(unaligned int*)(V_0 + (int)1) = (int)0x1111
      IL_0006:  ldloc.0
      IL_0007:  ldc.i4     0x1
      IL_000c:  conv.i

      IL_000d:  add
      IL_000e:  ldc.i4   0x1111
      IL_0010:  conv.i4

                unaligned. 1
      IL_0011:  stind.i4

                // Console.Writeline(V_1)
      IL_0012:  ldloc.1
      IL_0013:  call       void [System.Console]System.Console::WriteLine(int64)

                // V_2 = (int)V_1 - 0x11109C  // 0x111100 - 0x11109C --> 0x60 == 100
      IL_0018:  ldloc.1
      IL_0019:  conv.i4
      IL_001a:  ldc.i4     0x11109C
      IL_001f:  sub
      IL_0020:  stloc.2

      IL_0021:  br.s       IL_0023

                // return V_2
      IL_0023:  ldloc.2
      IL_0024:  ret
    } 

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } 

  } 

} 
