// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.




.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .ver 4:0:0:0
}
.assembly 'dependentLifetimes'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
}
.assembly extern xunit.core {}


.class public auto ansi beforefieldinit BugRepro
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  2
    .locals init (object V_0,
             object & V_1)
              ldc.i4.5
              box        int32
              stloc.0

              ldloca.s   V_0
              stloc.1

              ldloca.s   V_1
              conv.u
              conv.u8
              call       void [System.Console]System.Console::WriteLine(uint64)

              ldloc.1
              ldind.ref
              dup
              callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
              call       void [System.Console]System.Console::WriteLine(object)
              call       void [System.Console]System.Console::WriteLine(object)

              call       void [mscorlib]System.GC::Collect()

              ldloc.1
              ldind.ref
              dup
              callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
              call       void [System.Console]System.Console::WriteLine(object)
              call       void [System.Console]System.Console::WriteLine(object)

              ldc.i4 100
              ret
  } 

} 
