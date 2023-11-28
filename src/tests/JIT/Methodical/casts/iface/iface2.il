// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// iface2.il
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME { }
.assembly extern xunit.core {}
.namespace JitTest_iface2_il
{
  .class interface private abstract auto ansi Iface1
  {
    .method public hidebysig newslot virtual abstract 
            instance int32 Method1a() cil managed { }
  }
  .class interface private abstract auto ansi Iface2
         implements JitTest_iface2_il.Iface1
  {
    .method public hidebysig newslot virtual abstract 
            instance int32 Method2a() cil managed { }
  }
  .class interface private abstract auto ansi Iface3
         implements JitTest_iface2_il.Iface1
  {
    .method public hidebysig newslot virtual abstract 
            instance int32 Method3a() cil managed { }
  }
  .class private auto ansi beforefieldinit BaseClass
         extends [mscorlib]System.Object
         implements JitTest_iface2_il.Iface2,
                    JitTest_iface2_il.Iface1
  {
    .method public hidebysig newslot final virtual 
            instance int32 Method1a() cil managed
    {
      .maxstack  1
      ldc.i4.1
      ret
    }
    .method public hidebysig newslot final virtual 
            instance int32 Method2a() cil managed
    {
      .maxstack  1
      ldc.i4.s   10
      ret
    }
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      .maxstack  8
      ldarg.0
      call       instance void [mscorlib]System.Object::.ctor()
      ret
    }
  }
  .class public auto ansi beforefieldinit CoClass
         extends JitTest_iface2_il.BaseClass
         implements JitTest_iface2_il.Iface3,
                    JitTest_iface2_il.Iface1
  {
    .method public hidebysig newslot final virtual 
            instance int32 Method3a() cil managed
    {
      .maxstack  1
      ldc.i4.s   100
      ret
    }
    .method private hidebysig static int32 
            Static0(class JitTest_iface2_il.BaseClass co) cil managed
    {
      .maxstack  2
      .locals (int32)
      ldc.i4.0
      ldarg.0
      castclass  JitTest_iface2_il.Iface1
      callvirt   instance int32 JitTest_iface2_il.Iface1::Method1a()
      add
      
      ldarg.0
      castclass  JitTest_iface2_il.Iface1
      callvirt   instance int32 JitTest_iface2_il.Iface1::Method1a()
      add

      ldarg.0
      castclass  JitTest_iface2_il.Iface2
      callvirt   instance int32 JitTest_iface2_il.Iface2::Method2a()
      add
      
      ldarg.0
      castclass  JitTest_iface2_il.Iface2
      callvirt   instance int32 JitTest_iface2_il.Iface2::Method2a()
      add

      ldarg.0
      isinst     JitTest_iface2_il.Iface3
      brfalse.s  IL_004c

      ldarg.0
      castclass  JitTest_iface2_il.Iface3
      callvirt   instance int32 JitTest_iface2_il.Iface3::Method3a()
      add
      
      ldarg.0
      isinst     JitTest_iface2_il.Iface3
      callvirt   instance int32 JitTest_iface2_il.Iface3::Method3a()
      add
      
      br.s       IL_0071
      
   IL_004c:
      stloc.0
      .try
      {
        ldarg.0
        castclass  JitTest_iface2_il.Iface3
        callvirt   instance int32 JitTest_iface2_il.Iface3::Method3a()
        ldarg.0
        isinst     JitTest_iface2_il.Iface3
        callvirt   instance int32 JitTest_iface2_il.Iface3::Method3a()
        add
        ldloc.0
        add
        stloc.0
        leave.s    IL_0070
      }
      catch [mscorlib]System.Object 
      {
        pop
        ldloc.0
        ldc.i4     0x3e8
        add
        stloc.0
        leave.s    IL_0070
      }
   IL_0070:
      ldloc.0
   IL_0071:
      ret
    }
    .method private hidebysig static int32 
            Static1(class JitTest_iface2_il.Iface1 i) cil managed
    {
      .maxstack  2
      ldarg.0
      castclass  JitTest_iface2_il.CoClass
      call       int32 JitTest_iface2_il.CoClass::Static0(class JitTest_iface2_il.BaseClass)
      ldarg.0
      isinst     JitTest_iface2_il.CoClass
      call       int32 JitTest_iface2_il.CoClass::Static0(class JitTest_iface2_il.BaseClass)
      add
      ret
    }
    .method private hidebysig static int32 
            Static2(class JitTest_iface2_il.Iface2 i) cil managed
    {
      .maxstack  2
      ldarg.0
      castclass  JitTest_iface2_il.CoClass
      call       int32 JitTest_iface2_il.CoClass::Static0(class JitTest_iface2_il.BaseClass)
      ldarg.0
      isinst     JitTest_iface2_il.CoClass
      call       int32 JitTest_iface2_il.CoClass::Static0(class JitTest_iface2_il.BaseClass)
      add
      ret
    }
    .method private hidebysig static int32 
            Static3(class JitTest_iface2_il.Iface3 i) cil managed
    {
      .maxstack  2
      ldarg.0
      castclass  JitTest_iface2_il.CoClass
      call       int32 JitTest_iface2_il.CoClass::Static0(class JitTest_iface2_il.BaseClass)
      ldarg.0
      isinst     JitTest_iface2_il.CoClass
      call       int32 JitTest_iface2_il.CoClass::Static0(class JitTest_iface2_il.BaseClass)
      add
      ret
    }
    .method private hidebysig static int32 
            Static4(class JitTest_iface2_il.Iface1 i) cil managed
    {
      .maxstack  2
      ldarg.0
      castclass  JitTest_iface2_il.BaseClass
      call       int32 JitTest_iface2_il.CoClass::Static0(class JitTest_iface2_il.BaseClass)
      ldarg.0
      isinst     JitTest_iface2_il.BaseClass
      call       int32 JitTest_iface2_il.CoClass::Static0(class JitTest_iface2_il.BaseClass)
      add
      ret
    }
    .method private hidebysig static int32 
            Static5(class JitTest_iface2_il.Iface2 i) cil managed
    {
      .maxstack  2
      ldarg.0
      castclass  JitTest_iface2_il.BaseClass
      call       int32 JitTest_iface2_il.CoClass::Static0(class JitTest_iface2_il.BaseClass)
      ldarg.0
      isinst     JitTest_iface2_il.BaseClass
      call       int32 JitTest_iface2_il.CoClass::Static0(class JitTest_iface2_il.BaseClass)
      add
      ret
    }
    .method public hidebysig static int32
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  2
      .locals ([0] class JitTest_iface2_il.CoClass co,
               [1] class JitTest_iface2_il.BaseClass bs,
               [2] int32 _Vb_t_$00000003$00000000)
      IL_0000:  newobj     instance void JitTest_iface2_il.CoClass::.ctor()
      IL_0005:  stloc.0
      IL_0006:  ldloc.0
      IL_0007:  call       int32 JitTest_iface2_il.CoClass::Static1(class JitTest_iface2_il.Iface1)
      IL_000c:  ldc.i4     0x1bc
      IL_0011:  beq.s      IL_0025
      IL_0013:  ldstr      "Test 1 failed."
      IL_0018:  call       void [System.Console]System.Console::WriteLine(string)
      IL_001d:  ldc.i4.s   101
      IL_001f:  ret
      IL_0025:  ldloc.0
      IL_0026:  call       int32 JitTest_iface2_il.CoClass::Static2(class JitTest_iface2_il.Iface2)
      IL_002b:  ldc.i4     0x1bc
      IL_0030:  beq.s      IL_0041
      IL_0032:  ldstr      "Test 2 failed."
      IL_0037:  call       void [System.Console]System.Console::WriteLine(string)
      IL_003c:  ldc.i4.s   102
      IL_003e:  ret
      IL_0041:  ldloc.0
      IL_0042:  call       int32 JitTest_iface2_il.CoClass::Static3(class JitTest_iface2_il.Iface3)
      IL_0047:  ldc.i4     0x1bc
      IL_004c:  beq.s      IL_005d
      IL_004e:  ldstr      "Test 3 failed."
      IL_0053:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0058:  ldc.i4.s   103
      IL_005a:  ret
      IL_005d:  newobj     instance void JitTest_iface2_il.BaseClass::.ctor()
      IL_0062:  stloc.1
      IL_0063:  ldloc.1
      IL_0064:  call       int32 JitTest_iface2_il.CoClass::Static4(class JitTest_iface2_il.Iface1)
      IL_0069:  ldc.i4     0x7fc
      IL_006e:  beq.s      IL_007f
      IL_0070:  ldstr      "Test 4 failed."
      IL_0075:  call       void [System.Console]System.Console::WriteLine(string)
      IL_007a:  ldc.i4.s   104
      IL_007c:  ret
      IL_007f:  ldloc.1
      IL_0080:  call       int32 JitTest_iface2_il.CoClass::Static5(class JitTest_iface2_il.Iface2)
      IL_0085:  ldc.i4     0x7fc
      IL_008a:  beq.s      IL_009b
      IL_008c:  ldstr      "Test 5 failed."
      IL_0091:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0096:  ldc.i4.s   105
      IL_0098:  ret
      IL_009b:  ldstr      "Passed"
      IL_00a0:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00a5:  ldc.i4 100
      IL_00aa:  ret
    }
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void JitTest_iface2_il.BaseClass::.ctor()
      IL_0006:  ret
    }
  }
}
