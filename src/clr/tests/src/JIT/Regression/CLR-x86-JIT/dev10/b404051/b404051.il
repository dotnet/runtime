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

.assembly b404051
{
}


.class private auto ansi beforefieldinit BadHandles
       extends [mscorlib]System.Object
{
  .field private static object Pass
  .method private hidebysig static int32 Main() cil managed
  {
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
