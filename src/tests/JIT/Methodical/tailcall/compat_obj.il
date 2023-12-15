// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME { }
.assembly extern xunit.core {}
.namespace JitTest_compat_obj
{
  .class public auto ansi beforefieldinit BaseClass
         extends [mscorlib]System.Object
  {
    .method public hidebysig  static 
            class JitTest_compat_obj.BaseClass  caller(bool which) il managed
    {
      // Code size       18 (0x12)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  brfalse.s  IL_000a

      IL_0003:  newobj     instance void JitTest_compat_obj.BaseClass::.ctor()
      IL_0008:  br.s       IL_0011

      IL_000a:  tail.
      IL_000c:  call       class JitTest_compat_obj.DerivedClass JitTest_compat_obj.DerivedClass::callee()
      IL_0011:  ret
    } // end of method BaseClass::caller

    .method public hidebysig  specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method BaseClass::.ctor

  } // end of class BaseClass

  .class public auto ansi beforefieldinit DerivedClass
         extends JitTest_compat_obj.BaseClass
  {
    .method public hidebysig  static 
            class JitTest_compat_obj.DerivedClass callee() il managed
    {
      // Code size       6 (0x6)
      .maxstack  8
      IL_0000:  newobj     instance void JitTest_compat_obj.DerivedClass::.ctor()
      IL_0005:  ret
    } // end of method DerivedClass::callee

    .method public hidebysig  static 
            int32 Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       39 (0x27)
      .maxstack  1
      .locals (int32 V_0)
      IL_0000:  ldc.i4.0
      IL_0001:  call       class JitTest_compat_obj.BaseClass JitTest_compat_obj.BaseClass::caller(bool)
      IL_0006:  call       void [System.Console]System.Console::WriteLine(class System.Object)
      IL_000b:  ldc.i4.1
      IL_000c:  call       class JitTest_compat_obj.BaseClass JitTest_compat_obj.BaseClass::caller(bool)
      IL_0011:  call       void [System.Console]System.Console::WriteLine(class System.Object)
      IL_0016:  ldstr      "passed"
      IL_001b:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0020:  ldc.i4.s   100
      IL_0022:  stloc.0
      IL_0023:  br.s       IL_0025

      IL_0025:  ldloc.0
      IL_0026:  ret
    } // end of method DerivedClass::Main

    .method public hidebysig  specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void JitTest_compat_obj.BaseClass::.ctor()
      IL_0006:  ret
    } // end of method DerivedClass::.ctor

  } // end of class DerivedClass

} // end of namespace JitTest_compat_obj

//*********** DISASSEMBLY COMPLETE ***********************
