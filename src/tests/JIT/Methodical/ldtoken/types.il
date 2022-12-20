// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Runtime.Extensions { auto }
.assembly extern System.Console { auto }
.assembly extern mscorlib { auto }

.assembly types { }
.assembly extern xunit.core {}
.namespace JitTest_types_ldtoken_il
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .field private static int32 s_exitCode;

    .method private hidebysig static void
            test_token(object boxed_vt,
                       valuetype [mscorlib]System.RuntimeTypeHandle vt) cil managed
    {
      .maxstack  8
      ldarg.0
      callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      ldarg.1
      call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(
      													valuetype [mscorlib]System.RuntimeTypeHandle)
      beq.s      IL_EXIT

      ldstr      "Test failed on "
      ldarg.1
      call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(
      													valuetype [mscorlib]System.RuntimeTypeHandle)
      callvirt   instance string [mscorlib]System.Type::ToString()
      call       string [mscorlib]System.String::Concat(string, string)
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s   101
      stsfld     int32 JitTest_types_ldtoken_il.Test::s_exitCode
      IL_EXIT:
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
      ldc.i4     100
      stsfld     int32 JitTest_types_ldtoken_il.Test::s_exitCode
      ldc.i4.1
      box        [mscorlib]System.Byte
      ldtoken    [mscorlib]System.Byte
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4.1
      box        [mscorlib]System.SByte
      ldtoken    [mscorlib]System.SByte
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4.1
      box        [mscorlib]System.Int16
      ldtoken    [mscorlib]System.Int16
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4.1
      box        [mscorlib]System.UInt16
      ldtoken    [mscorlib]System.UInt16
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4.1
      box        [mscorlib]System.Int32
      ldtoken    [mscorlib]System.Int32
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4.1
      box        [mscorlib]System.UInt32
      ldtoken    [mscorlib]System.UInt32
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i8 1
      box        [mscorlib]System.Int64
      ldtoken    [mscorlib]System.Int64
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i8 1
      box        [mscorlib]System.UInt64
      ldtoken    [mscorlib]System.UInt64
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.r8 1
      box        [mscorlib]System.Single
      ldtoken    [mscorlib]System.Single
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.r4 1
      box        [mscorlib]System.Double
      ldtoken    [mscorlib]System.Double
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4.1
      box        [mscorlib]System.IntPtr
      ldtoken    [mscorlib]System.IntPtr
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4.1
      box        [mscorlib]System.UIntPtr
      ldtoken    [mscorlib]System.UIntPtr
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4.1
      box        [mscorlib]System.Boolean
      ldtoken    [mscorlib]System.Boolean
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4.1
      box        [mscorlib]System.Char
      ldtoken    [mscorlib]System.Char
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldstr      "moo"
      ldtoken    [mscorlib]System.String
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      newobj     instance void [mscorlib]System.Object::.ctor()
      ldtoken    [mscorlib]System.Object
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4 10
      newarr     int8
      ldtoken    int8[]
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4 10
      newarr     uint8
      ldtoken    uint8[]
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4 10
      newarr     int16
      ldtoken    int16[]
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4 10
      newarr     uint16
      ldtoken    uint16[]
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4 10
      newarr     int32
      ldtoken    int32[]
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4 10
      newarr     uint32
      ldtoken    uint32[]
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4 10
      newarr     int64
      ldtoken    int64[]
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4 10
      newarr     uint64
      ldtoken    uint64[]
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4 10
      newarr     native int
      ldtoken    native int[]
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4 10
      newarr     valuetype [mscorlib]System.UIntPtr
      ldtoken    valuetype [mscorlib]System.UIntPtr[]
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4 10
      newarr     bool
      ldtoken    bool[]
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4 10
      newarr     char
      ldtoken    char[]
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4 10
      newarr     object
      ldtoken    object[]
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldc.i4 10
      newarr     string
      ldtoken    string[]
      call       void JitTest_types_ldtoken_il.Test::test_token(object, valuetype [mscorlib]System.RuntimeTypeHandle)

      ldstr      "Passed"
      call       void [System.Console]System.Console::WriteLine(string)
      ldsfld     int32 JitTest_types_ldtoken_il.Test::s_exitCode
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
