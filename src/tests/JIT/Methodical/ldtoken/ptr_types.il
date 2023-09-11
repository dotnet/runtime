// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ptr_types.il
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME { }
.assembly extern xunit.core {}
.namespace JitTest_ptr_types_ldtoken_il
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method private hidebysig static void 
    		test_token(valuetype [mscorlib]System.RuntimeTypeHandle vt) cil managed
    {
      .maxstack  8
      ldarg.0
      call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(
      													valuetype [mscorlib]System.RuntimeTypeHandle)
      callvirt   instance string [mscorlib]System.Type::ToString()
      call       void [System.Console]System.Console::WriteLine(string)
      ret
    }
    .method public hidebysig static int32
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  8
      ldtoken    int8*
      call       void JitTest_ptr_types_ldtoken_il.Test::test_token(valuetype [mscorlib]System.RuntimeTypeHandle)

      ldtoken    unsigned int8*
      call       void JitTest_ptr_types_ldtoken_il.Test::test_token(valuetype [mscorlib]System.RuntimeTypeHandle)
      
      ldtoken    int16*
      call       void JitTest_ptr_types_ldtoken_il.Test::test_token(valuetype [mscorlib]System.RuntimeTypeHandle)
      
      ldtoken    unsigned int16*
      call       void JitTest_ptr_types_ldtoken_il.Test::test_token(valuetype [mscorlib]System.RuntimeTypeHandle)
      
      ldtoken    int32*
      call       void JitTest_ptr_types_ldtoken_il.Test::test_token(valuetype [mscorlib]System.RuntimeTypeHandle)
      
      ldtoken    unsigned int32*
      call       void JitTest_ptr_types_ldtoken_il.Test::test_token(valuetype [mscorlib]System.RuntimeTypeHandle)
      
      ldtoken    int64*
      call       void JitTest_ptr_types_ldtoken_il.Test::test_token(valuetype [mscorlib]System.RuntimeTypeHandle)
      
      ldtoken    unsigned int64*
      call       void JitTest_ptr_types_ldtoken_il.Test::test_token(valuetype [mscorlib]System.RuntimeTypeHandle)
      
      ldtoken    native int*
      call       void JitTest_ptr_types_ldtoken_il.Test::test_token(valuetype [mscorlib]System.RuntimeTypeHandle)
      
      ldtoken    native unsigned int*
      call       void JitTest_ptr_types_ldtoken_il.Test::test_token(valuetype [mscorlib]System.RuntimeTypeHandle)
      
      ldtoken    float32*
      call       void JitTest_ptr_types_ldtoken_il.Test::test_token(valuetype [mscorlib]System.RuntimeTypeHandle)
      
      ldtoken    float64*
      call       void JitTest_ptr_types_ldtoken_il.Test::test_token(valuetype [mscorlib]System.RuntimeTypeHandle)
      
      ldstr      "Passed"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s   100
      ret
    }
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    }
  }
}
