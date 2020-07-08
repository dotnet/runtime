// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern System.Runtime { auto }
.assembly extern mscorlib { auto }


.assembly ActivateStructDefCtor
{
}

// =============== CLASS MEMBERS DECLARATION ===================

.class private abstract auto ansi sealed beforefieldinit Program
       extends [System.Runtime]System.Object
{
  .class sequential ansi sealed nested private beforefieldinit Foo
         extends [System.Runtime]System.ValueType
  {
    .field public int32 x

    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.1
      IL_0002:  stfld      int32 Program/Foo::x
      IL_0007:  ret
    } // end of method Foo::.ctor
  } // end of class Foo

  .field private static int32 Pass
  .field private static int32 Fail

  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       89 (0x59)
    .maxstack  2
    .locals init ([0] int32 testIndex,
             [1] int32 i,
             [2] valuetype Program/Foo foo)
    IL_0000:  ldc.i4.m1
    IL_0001:  stloc.0
    IL_0002:  ldc.i4.0
    IL_0003:  stloc.1
    IL_0004:  br.s       IL_004f

    IL_0006:  ldloc.0
    IL_0007:  ldc.i4.1
    IL_0008:  add
    IL_0009:  stloc.0
    IL_000a:  call       !!0 [System.Runtime]System.Activator::CreateInstance<valuetype Program/Foo>()
    IL_000f:  stloc.2
    IL_0010:  ldloc.2
    IL_0011:  ldfld      int32 Program/Foo::x
    IL_0016:  ldc.i4.1
    IL_0017:  beq.s      IL_0021

    IL_0019:  ldsfld     int32 Program::Fail
    IL_001e:  ldloc.0
    IL_001f:  add
    IL_0020:  ret

    IL_0021:  ldloc.0
    IL_0022:  ldc.i4.1
    IL_0023:  add
    IL_0024:  stloc.0
    IL_0025:  ldtoken    Program/Foo
    IL_002a:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_002f:  call       object [System.Runtime]System.Activator::CreateInstance(class [System.Runtime]System.Type)
    IL_0034:  unbox.any  Program/Foo
    IL_0039:  stloc.2
    IL_003a:  ldloc.2
    IL_003b:  ldfld      int32 Program/Foo::x
    IL_0040:  ldc.i4.1
    IL_0041:  beq.s      IL_004b

    IL_0043:  ldsfld     int32 Program::Fail
    IL_0048:  ldloc.0
    IL_0049:  add
    IL_004a:  ret

    IL_004b:  ldloc.1
    IL_004c:  ldc.i4.1
    IL_004d:  add
    IL_004e:  stloc.1
    IL_004f:  ldloc.1
    IL_0050:  ldc.i4.2
    IL_0051:  blt.s      IL_0006

    IL_0053:  ldsfld     int32 Program::Pass
    IL_0058:  ret
  } // end of method Program::Main

  .method private hidebysig specialname rtspecialname static
          void  .cctor() cil managed
  {
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  ldc.i4.s   100
    IL_0002:  stsfld     int32 Program::Pass
    IL_0007:  ldc.i4.s   101
    IL_0009:  stsfld     int32 Program::Fail
    IL_000e:  ret
  } // end of method Program::.cctor
} // end of class Program
