// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ldtokena.il
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ldtokena
{
  
  
}
.namespace JitTest
{
  .class private auto ansi beforefieldinit TestClass
         extends [mscorlib]System.Object
  {
    .field family class JitTest.TestClass m_fld
    .field public static class JitTest.TestClass s_fld
    .method public hidebysig static class JitTest.TestClass 
            StaticMethod(class JitTest.TestClass inst) cil managed
    {
      .maxstack  1
      .locals (class JitTest.TestClass V_0)
      IL_0000:  newobj     instance void JitTest.TestClass::.ctor()
      IL_0005:  stloc.0
      IL_0006:  br.s       IL_0008
      IL_0008:  ldloc.0
      IL_0009:  ret
    }
    .method private hidebysig instance class JitTest.TestClass 
            InstanceMethod(class JitTest.TestClass inst) cil managed
    {
      .maxstack  1
      .locals (class JitTest.TestClass V_0)
      IL_0000:  ldarg.0
      IL_0001:  stloc.0
      IL_0002:  br.s       IL_0004
      IL_0004:  ldloc.0
      IL_0005:  ret
    }
    .method assembly hidebysig newslot virtual 
            instance class JitTest.TestClass 
            VirtualMethod(class JitTest.TestClass inst) cil managed
    {
      .maxstack  1
      .locals (class JitTest.TestClass V_0)
      IL_0000:  ldarg.0
      IL_0001:  stloc.0
      IL_0002:  br.s       IL_0004
      IL_0004:  ldloc.0
      IL_0005:  ret
    }
    .method private hidebysig static int32 Main() cil managed
    {
      .entrypoint
      .maxstack  8
      .locals (int32 V_0,
      		valuetype [mscorlib]System.RuntimeFieldHandle,
      		valuetype [mscorlib]System.RuntimeMethodHandle,
      		valuetype [mscorlib]System.RuntimeTypeHandle)
      ldc.i4.0
      stloc.0

      ldtoken field class JitTest.TestClass JitTest.TestClass::m_fld
      stloc.1

      ldloca 0
      ldind.i4
      brfalse br1
      ldloca 1
      br mg1
br1:
      ldloca 1
mg1:
      ldobj [mscorlib]System.RuntimeFieldHandle
      pop

      ldc.i4 100
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
