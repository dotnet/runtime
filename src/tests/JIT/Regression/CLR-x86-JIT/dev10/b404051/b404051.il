// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
}

.assembly b404051
{
}
.assembly extern xunit.core
{
    .publickeytoken = (
        8d 05 b1 bb 7a 6f db 6c
     )
     .ver 2:4:2:0
}


.class private auto ansi beforefieldinit BadHandles
       extends [mscorlib]System.Object
{
  .field private static object Pass
  .method private hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  8
    IL_0000:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_0005:  stsfld     object BadHandles::Pass
    IL_000a:  ldtoken    field object BadHandles::Pass
    IL_000f:  call       class [mscorlib]System.Reflection.FieldInfo 
                             [mscorlib]System.Reflection.FieldInfo::GetFieldFromHandle(
                                 valuetype [mscorlib]System.RuntimeFieldHandle)
    IL_0014:  call       void [System.Console]System.Console::WriteLine(object)
              ldc.i4.s   100
    IL_0019:  ret
  } 

} 
