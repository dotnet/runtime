// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .ver 2:0:0:0
}
.assembly 'dev10_865840'
{
  .custom instance void [mscorlib]System.Security.SecurityCriticalAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       
.corflags 0x00000001    



.class private sequential ansi sealed beforefieldinit DifRepro.MyInt
       extends [mscorlib]System.ValueType
{
  .field public native int i
} 

.class private auto ansi beforefieldinit DifRepro.Program
       extends [mscorlib]System.Object
{
  .field private static class [mscorlib]System.Security.Permissions.SecurityPermission _unmanagedCodePermission
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  8
    IL_0000:  call       void DifRepro.Program::Test()
    IL_0005:  ldc.i4     0x64
    IL_000a:  ret
  } 

  .method private hidebysig static void  Helper(valuetype DifRepro.MyInt& i) cil managed
  {
    .maxstack  8
    IL_0000:  ret
  } 

  .method public hidebysig static reqsecobj 
          void  Test() cil managed
  {
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  call       void DifRepro.Program::Helper(valuetype DifRepro.MyInt&)
    IL_0006:  ldsfld     class [mscorlib]System.Security.Permissions.SecurityPermission DifRepro.Program::_unmanagedCodePermission
    IL_000b:  brtrue.s   IL_0018

    IL_000d:  ldc.i4.2
    IL_000e:  newobj     instance void [mscorlib]System.Security.Permissions.SecurityPermission::.ctor(valuetype [mscorlib]System.Security.Permissions.SecurityPermissionFlag)
    IL_0013:  stsfld     class [mscorlib]System.Security.Permissions.SecurityPermission DifRepro.Program::_unmanagedCodePermission
    IL_0018:  ldsfld     class [mscorlib]System.Security.Permissions.SecurityPermission DifRepro.Program::_unmanagedCodePermission
    IL_001d:  callvirt   instance void [mscorlib]System.Security.CodeAccessPermission::Demand()
    IL_0022:  ret
  } 

} 
