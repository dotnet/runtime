// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly Runtime_32059 {}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit Runtime_32059 extends [System.Runtime]System.Object
{

.method public hidebysig static int32 Main() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint

   ldc.i4.0
   newarr [System.Runtime]System.String
   call int32 Runtime_32059::MainX(string[])
   ret
}

.method public hidebysig static int32 MainX(string[] args) cil managed noinlining
{
  .locals init (valuetype [System.Runtime]System.DateTime V_0)
  .maxstack 12
  ldarg.0
  ldnull
  ldnull
  ldnull
  ldnull
  ldnull
  ldnull
  ldnull
  ldloc.0
  ldc.i4.0
  ldnull
  newobj     instance void Runtime_32059::.ctor(string[],
                                           string,
                                           string,
                                           string,
                                           string,
                                           string,
                                           string,
                                           string,
                                           valuetype [System.Runtime]System.DateTime,
                                           int32,
                                           string)
  tail.
  call       instance int32 Runtime_32059::F()
  ret
}

.method public specialname rtspecialname instance void .ctor(string[] o,
                             string h,
                             string g,
                             string f,
                             string e,
                             string d,
                             string c,
                             string b,
                             valuetype [System.Runtime]System.DateTime a,
                             int32 pc,
                             string current) cil managed  noinlining
{
  ldarg.0
  call instance void [System.Runtime]System.Object::.ctor()
  ret
} 

.method public hidebysig instance int32 F() cil managed noinlining
{
  ldc.i4 100
  ret
}
}





