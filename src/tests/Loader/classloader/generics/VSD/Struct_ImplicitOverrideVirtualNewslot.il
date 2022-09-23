// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
// this test is the equivalent of Class_ImplicitOverrideVirtualNewslot.cs for structs.
// The reason we are using IL is because C# doesn't allow struct to have virtual methods with 'virtual' keyword.
// Methods that implement interface methods are by default virtual (which translates to 'virtual newslot final' in IL.
// But if we use 'virtual' keyword in C# it translates to 'virtual newslot' in  IL
// and struct virtual methods can't be overridden.


.assembly extern mscorlib {}
.assembly Struct_ImplicitOverrideVirtualNewslot {}


.class public auto ansi beforefieldinit A`1<T>
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A`1::.ctor

} // end of class A`1

.class public sequential ansi sealed beforefieldinit S`1<T>
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} // end of class S`1

.class interface public abstract auto ansi I
{
  .method public hidebysig newslot abstract virtual
          instance int32  method1() cil managed
  {
  } // end of method I::method1

  .method public hidebysig newslot abstract virtual
          instance int32  method2<T>() cil managed
  {
  } // end of method I::method2

} // end of class I

.class interface public abstract auto ansi IGen`1<T>
{
  .method public hidebysig newslot abstract virtual
          instance int32  method1() cil managed
  {
  } // end of method IGen`1::method1

  .method public hidebysig newslot abstract virtual
          instance int32  method2<M>() cil managed
  {
  } // end of method IGen`1::method2

} // end of class IGen`1

.class public sequential ansi sealed beforefieldinit C1
       extends [mscorlib]System.ValueType
       implements I
{
  .pack 0
  .size 1
  .method public hidebysig newslot virtual
          instance int32  method1() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C1::method1

  .method public hidebysig newslot virtual
          instance int32  method2<T>() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.2
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C1::method2

} // end of class C1

.class public sequential ansi sealed beforefieldinit C2`1<T>
       extends [mscorlib]System.ValueType
       implements I
{
  .pack 0
  .size 1
  .method public hidebysig newslot virtual
          instance int32  method1() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.3
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C2`1::method1

  .method public hidebysig newslot virtual
          instance int32  method2<U>() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.4
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C2`1::method2

} // end of class C2`1

.class public sequential ansi sealed beforefieldinit C3Int
       extends [mscorlib]System.ValueType
       implements class IGen`1<int32>
{
  .pack 0
  .size 1
  .method public hidebysig newslot virtual
          instance int32  method1() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.5
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C3Int::method1

  .method public hidebysig newslot virtual
          instance int32  method2<U>() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.6
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C3Int::method2

} // end of class C3Int

.class public sequential ansi sealed beforefieldinit C3String
       extends [mscorlib]System.ValueType
       implements class IGen`1<string>
{
  .pack 0
  .size 1
  .method public hidebysig newslot virtual
          instance int32  method1() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.5
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C3String::method1

  .method public hidebysig newslot virtual
          instance int32  method2<U>() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.6
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C3String::method2

} // end of class C3String

.class public sequential ansi sealed beforefieldinit C3Object
       extends [mscorlib]System.ValueType
       implements class IGen`1<object>
{
  .pack 0
  .size 1
  .method public hidebysig newslot virtual
          instance int32  method1() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.5
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C3Object::method1

  .method public hidebysig newslot virtual
          instance int32  method2<U>() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.6
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C3Object::method2

} // end of class C3Object

.class public sequential ansi sealed beforefieldinit C4`1<T>
       extends [mscorlib]System.ValueType
       implements class IGen`1<!T>
{
  .pack 0
  .size 1
  .method public hidebysig newslot virtual
          instance int32  method1() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.7
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C4`1::method1

  .method public hidebysig newslot virtual
          instance int32  method2<U>() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.8
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C4`1::method2

} // end of class C4`1

.class public auto ansi beforefieldinit Test_Struct_ImplicitOverrideVirtualNewslot
       extends [mscorlib]System.Object
{
  .field public static bool pass
  .method public hidebysig static void  TestNonGenInterface_NonGenType() cil managed
  {
    // Code size       88 (0x58)
    .maxstack  2
    .locals init (class I V_0,
             valuetype C1 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_1
    IL_0003:  initobj    C1
    IL_0009:  ldloc.1
    IL_000a:  box        C1
    IL_000f:  stloc.0
    IL_0010:  ldloc.0
    IL_0011:  callvirt   instance int32 I::method2<int32>()
    IL_0016:  ldc.i4.2
    IL_0017:  bne.un.s   IL_003f

    IL_0019:  ldloc.0
    IL_001a:  callvirt   instance int32 I::method2<string>()
    IL_001f:  ldc.i4.2
    IL_0020:  bne.un.s   IL_003f

    IL_0022:  ldloc.0
    IL_0023:  callvirt   instance int32 I::method2<object>()
    IL_0028:  ldc.i4.2
    IL_0029:  bne.un.s   IL_003f

    IL_002b:  ldloc.0
    IL_002c:  callvirt   instance int32 I::method2<class A`1<int32>>()
    IL_0031:  ldc.i4.2
    IL_0032:  bne.un.s   IL_003f

    IL_0034:  ldloc.0
    IL_0035:  callvirt   instance int32 I::method2<valuetype S`1<object>>()
    IL_003a:  ldc.i4.2
    IL_003b:  ceq
    IL_003d:  br.s       IL_0040

    IL_003f:  ldc.i4.0
    IL_0040:  stloc.2
    IL_0041:  ldloc.2
    IL_0042:  brtrue.s   IL_0057

    IL_0044:  nop
    IL_0045:  ldstr      "Failed at TestNonGenInterface_NonGenType: generic "
    + "method"
    IL_004a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_004f:  nop
    IL_0050:  ldc.i4.0
    IL_0051:  stsfld     bool Test_Struct_ImplicitOverrideVirtualNewslot::pass
    IL_0056:  nop
    IL_0057:  ret
  } // end of method Test::TestNonGenInterface_NonGenType

  .method public hidebysig static void  TestNonGenInterface_GenType() cil managed
  {
    // Code size       270 (0x10e)
    .maxstack  2
    .locals init (class I V_0,
             class I V_1,
             class I V_2,
             valuetype C2`1<int32> V_3,
             valuetype C2`1<object> V_4,
             valuetype C2`1<string> V_5,
             bool V_6)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_3
    IL_0003:  initobj    valuetype C2`1<int32>
    IL_0009:  ldloc.3
    IL_000a:  box        valuetype C2`1<int32>
    IL_000f:  stloc.0
    IL_0010:  ldloca.s   V_4
    IL_0012:  initobj    valuetype C2`1<object>
    IL_0018:  ldloc.s    V_4
    IL_001a:  box        valuetype C2`1<object>
    IL_001f:  stloc.1
    IL_0020:  ldloca.s   V_5
    IL_0022:  initobj    valuetype C2`1<string>
    IL_0028:  ldloc.s    V_5
    IL_002a:  box        valuetype C2`1<string>
    IL_002f:  stloc.2
    IL_0030:  ldloc.0
    IL_0031:  callvirt   instance int32 I::method1()
    IL_0036:  ldc.i4.3
    IL_0037:  bne.un.s   IL_004d

    IL_0039:  ldloc.2
    IL_003a:  callvirt   instance int32 I::method1()
    IL_003f:  ldc.i4.3
    IL_0040:  bne.un.s   IL_004d

    IL_0042:  ldloc.1
    IL_0043:  callvirt   instance int32 I::method1()
    IL_0048:  ldc.i4.3
    IL_0049:  ceq
    IL_004b:  br.s       IL_004e

    IL_004d:  ldc.i4.0
    IL_004e:  stloc.s    V_6
    IL_0050:  ldloc.s    V_6
    IL_0052:  brtrue.s   IL_0067

    IL_0054:  nop
    IL_0055:  ldstr      "Failed at TestNonGenInterface_GenType: non generic"
    + " method"
    IL_005a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_005f:  nop
    IL_0060:  ldc.i4.0
    IL_0061:  stsfld     bool Test_Struct_ImplicitOverrideVirtualNewslot::pass
    IL_0066:  nop
    IL_0067:  ldloc.0
    IL_0068:  callvirt   instance int32 I::method2<int32>()
    IL_006d:  ldc.i4.4
    IL_006e:  bne.un     IL_00f3

    IL_0073:  ldloc.0
    IL_0074:  callvirt   instance int32 I::method2<object>()
    IL_0079:  ldc.i4.4
    IL_007a:  bne.un.s   IL_00f3

    IL_007c:  ldloc.0
    IL_007d:  callvirt   instance int32 I::method2<string>()
    IL_0082:  ldc.i4.4
    IL_0083:  bne.un.s   IL_00f3

    IL_0085:  ldloc.0
    IL_0086:  callvirt   instance int32 I::method2<class A`1<int32>>()
    IL_008b:  ldc.i4.4
    IL_008c:  bne.un.s   IL_00f3

    IL_008e:  ldloc.0
    IL_008f:  callvirt   instance int32 I::method2<valuetype S`1<string>>()
    IL_0094:  ldc.i4.4
    IL_0095:  bne.un.s   IL_00f3

    IL_0097:  ldloc.2
    IL_0098:  callvirt   instance int32 I::method2<int32>()
    IL_009d:  ldc.i4.4
    IL_009e:  bne.un.s   IL_00f3

    IL_00a0:  ldloc.2
    IL_00a1:  callvirt   instance int32 I::method2<object>()
    IL_00a6:  ldc.i4.4
    IL_00a7:  bne.un.s   IL_00f3

    IL_00a9:  ldloc.2
    IL_00aa:  callvirt   instance int32 I::method2<string>()
    IL_00af:  ldc.i4.4
    IL_00b0:  bne.un.s   IL_00f3

    IL_00b2:  ldloc.2
    IL_00b3:  callvirt   instance int32 I::method2<class A`1<int32>>()
    IL_00b8:  ldc.i4.4
    IL_00b9:  bne.un.s   IL_00f3

    IL_00bb:  ldloc.2
    IL_00bc:  callvirt   instance int32 I::method2<valuetype S`1<string>>()
    IL_00c1:  ldc.i4.4
    IL_00c2:  bne.un.s   IL_00f3

    IL_00c4:  ldloc.1
    IL_00c5:  callvirt   instance int32 I::method2<int32>()
    IL_00ca:  ldc.i4.4
    IL_00cb:  bne.un.s   IL_00f3

    IL_00cd:  ldloc.1
    IL_00ce:  callvirt   instance int32 I::method2<object>()
    IL_00d3:  ldc.i4.4
    IL_00d4:  bne.un.s   IL_00f3

    IL_00d6:  ldloc.1
    IL_00d7:  callvirt   instance int32 I::method2<string>()
    IL_00dc:  ldc.i4.4
    IL_00dd:  bne.un.s   IL_00f3

    IL_00df:  ldloc.1
    IL_00e0:  callvirt   instance int32 I::method2<class A`1<int32>>()
    IL_00e5:  ldc.i4.4
    IL_00e6:  bne.un.s   IL_00f3

    IL_00e8:  ldloc.1
    IL_00e9:  callvirt   instance int32 I::method2<valuetype S`1<string>>()
    IL_00ee:  ldc.i4.4
    IL_00ef:  ceq
    IL_00f1:  br.s       IL_00f4

    IL_00f3:  ldc.i4.0
    IL_00f4:  stloc.s    V_6
    IL_00f6:  ldloc.s    V_6
    IL_00f8:  brtrue.s   IL_010d

    IL_00fa:  nop
    IL_00fb:  ldstr      "Failed at TestNonGenInterface_GenType: generic met"
    + "hod"
    IL_0100:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0105:  nop
    IL_0106:  ldc.i4.0
    IL_0107:  stsfld     bool Test_Struct_ImplicitOverrideVirtualNewslot::pass
    IL_010c:  nop
    IL_010d:  ret
  } // end of method Test::TestNonGenInterface_GenType

  .method public hidebysig static void  TestGenInterface_NonGenType() cil managed
  {
    // Code size       270 (0x10e)
    .maxstack  2
    .locals init (class IGen`1<int32> V_0,
             class IGen`1<object> V_1,
             class IGen`1<string> V_2,
             valuetype C3Int V_3,
             valuetype C3Object V_4,
             valuetype C3String V_5,
             bool V_6)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_3
    IL_0003:  initobj    C3Int
    IL_0009:  ldloc.3
    IL_000a:  box        C3Int
    IL_000f:  stloc.0
    IL_0010:  ldloca.s   V_4
    IL_0012:  initobj    C3Object
    IL_0018:  ldloc.s    V_4
    IL_001a:  box        C3Object
    IL_001f:  stloc.1
    IL_0020:  ldloca.s   V_5
    IL_0022:  initobj    C3String
    IL_0028:  ldloc.s    V_5
    IL_002a:  box        C3String
    IL_002f:  stloc.2
    IL_0030:  ldloc.0
    IL_0031:  callvirt   instance int32 class IGen`1<int32>::method1()
    IL_0036:  ldc.i4.5
    IL_0037:  bne.un.s   IL_004d

    IL_0039:  ldloc.1
    IL_003a:  callvirt   instance int32 class IGen`1<object>::method1()
    IL_003f:  ldc.i4.5
    IL_0040:  bne.un.s   IL_004d

    IL_0042:  ldloc.2
    IL_0043:  callvirt   instance int32 class IGen`1<string>::method1()
    IL_0048:  ldc.i4.5
    IL_0049:  ceq
    IL_004b:  br.s       IL_004e

    IL_004d:  ldc.i4.0
    IL_004e:  stloc.s    V_6
    IL_0050:  ldloc.s    V_6
    IL_0052:  brtrue.s   IL_0067

    IL_0054:  nop
    IL_0055:  ldstr      "Failed at TestGenInterface_NonGenType: non generic"
    + " method"
    IL_005a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_005f:  nop
    IL_0060:  ldc.i4.0
    IL_0061:  stsfld     bool Test_Struct_ImplicitOverrideVirtualNewslot::pass
    IL_0066:  nop
    IL_0067:  ldloc.0
    IL_0068:  callvirt   instance int32 class IGen`1<int32>::method2<int32>()
    IL_006d:  ldc.i4.6
    IL_006e:  bne.un     IL_00f3

    IL_0073:  ldloc.0
    IL_0074:  callvirt   instance int32 class IGen`1<int32>::method2<object>()
    IL_0079:  ldc.i4.6
    IL_007a:  bne.un.s   IL_00f3

    IL_007c:  ldloc.0
    IL_007d:  callvirt   instance int32 class IGen`1<int32>::method2<string>()
    IL_0082:  ldc.i4.6
    IL_0083:  bne.un.s   IL_00f3

    IL_0085:  ldloc.0
    IL_0086:  callvirt   instance int32 class IGen`1<int32>::method2<class A`1<int32>>()
    IL_008b:  ldc.i4.6
    IL_008c:  bne.un.s   IL_00f3

    IL_008e:  ldloc.0
    IL_008f:  callvirt   instance int32 class IGen`1<int32>::method2<valuetype S`1<string>>()
    IL_0094:  ldc.i4.6
    IL_0095:  bne.un.s   IL_00f3

    IL_0097:  ldloc.1
    IL_0098:  callvirt   instance int32 class IGen`1<object>::method2<int32>()
    IL_009d:  ldc.i4.6
    IL_009e:  bne.un.s   IL_00f3

    IL_00a0:  ldloc.1
    IL_00a1:  callvirt   instance int32 class IGen`1<object>::method2<object>()
    IL_00a6:  ldc.i4.6
    IL_00a7:  bne.un.s   IL_00f3

    IL_00a9:  ldloc.1
    IL_00aa:  callvirt   instance int32 class IGen`1<object>::method2<string>()
    IL_00af:  ldc.i4.6
    IL_00b0:  bne.un.s   IL_00f3

    IL_00b2:  ldloc.1
    IL_00b3:  callvirt   instance int32 class IGen`1<object>::method2<class A`1<int32>>()
    IL_00b8:  ldc.i4.6
    IL_00b9:  bne.un.s   IL_00f3

    IL_00bb:  ldloc.1
    IL_00bc:  callvirt   instance int32 class IGen`1<object>::method2<valuetype S`1<string>>()
    IL_00c1:  ldc.i4.6
    IL_00c2:  bne.un.s   IL_00f3

    IL_00c4:  ldloc.2
    IL_00c5:  callvirt   instance int32 class IGen`1<string>::method2<int32>()
    IL_00ca:  ldc.i4.6
    IL_00cb:  bne.un.s   IL_00f3

    IL_00cd:  ldloc.2
    IL_00ce:  callvirt   instance int32 class IGen`1<string>::method2<object>()
    IL_00d3:  ldc.i4.6
    IL_00d4:  bne.un.s   IL_00f3

    IL_00d6:  ldloc.2
    IL_00d7:  callvirt   instance int32 class IGen`1<string>::method2<string>()
    IL_00dc:  ldc.i4.6
    IL_00dd:  bne.un.s   IL_00f3

    IL_00df:  ldloc.2
    IL_00e0:  callvirt   instance int32 class IGen`1<string>::method2<class A`1<int32>>()
    IL_00e5:  ldc.i4.6
    IL_00e6:  bne.un.s   IL_00f3

    IL_00e8:  ldloc.2
    IL_00e9:  callvirt   instance int32 class IGen`1<string>::method2<valuetype S`1<string>>()
    IL_00ee:  ldc.i4.6
    IL_00ef:  ceq
    IL_00f1:  br.s       IL_00f4

    IL_00f3:  ldc.i4.0
    IL_00f4:  stloc.s    V_6
    IL_00f6:  ldloc.s    V_6
    IL_00f8:  brtrue.s   IL_010d

    IL_00fa:  nop
    IL_00fb:  ldstr      "Failed at TestGenInterface_NonGenType: generic met"
    + "hod"
    IL_0100:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0105:  nop
    IL_0106:  ldc.i4.0
    IL_0107:  stsfld     bool Test_Struct_ImplicitOverrideVirtualNewslot::pass
    IL_010c:  nop
    IL_010d:  ret
  } // end of method Test::TestGenInterface_NonGenType

  .method public hidebysig static void  TestGenInterface_GenType() cil managed
  {
    // Code size       270 (0x10e)
    .maxstack  2
    .locals init (class IGen`1<int32> V_0,
             class IGen`1<object> V_1,
             class IGen`1<string> V_2,
             valuetype C4`1<int32> V_3,
             valuetype C4`1<object> V_4,
             valuetype C4`1<string> V_5,
             bool V_6)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_3
    IL_0003:  initobj    valuetype C4`1<int32>
    IL_0009:  ldloc.3
    IL_000a:  box        valuetype C4`1<int32>
    IL_000f:  stloc.0
    IL_0010:  ldloca.s   V_4
    IL_0012:  initobj    valuetype C4`1<object>
    IL_0018:  ldloc.s    V_4
    IL_001a:  box        valuetype C4`1<object>
    IL_001f:  stloc.1
    IL_0020:  ldloca.s   V_5
    IL_0022:  initobj    valuetype C4`1<string>
    IL_0028:  ldloc.s    V_5
    IL_002a:  box        valuetype C4`1<string>
    IL_002f:  stloc.2
    IL_0030:  ldloc.0
    IL_0031:  callvirt   instance int32 class IGen`1<int32>::method1()
    IL_0036:  ldc.i4.7
    IL_0037:  bne.un.s   IL_004d

    IL_0039:  ldloc.1
    IL_003a:  callvirt   instance int32 class IGen`1<object>::method1()
    IL_003f:  ldc.i4.7
    IL_0040:  bne.un.s   IL_004d

    IL_0042:  ldloc.2
    IL_0043:  callvirt   instance int32 class IGen`1<string>::method1()
    IL_0048:  ldc.i4.7
    IL_0049:  ceq
    IL_004b:  br.s       IL_004e

    IL_004d:  ldc.i4.0
    IL_004e:  stloc.s    V_6
    IL_0050:  ldloc.s    V_6
    IL_0052:  brtrue.s   IL_0067

    IL_0054:  nop
    IL_0055:  ldstr      "Failed at TestGenInterface_GenType: non generic me"
    + "thod"
    IL_005a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_005f:  nop
    IL_0060:  ldc.i4.0
    IL_0061:  stsfld     bool Test_Struct_ImplicitOverrideVirtualNewslot::pass
    IL_0066:  nop
    IL_0067:  ldloc.0
    IL_0068:  callvirt   instance int32 class IGen`1<int32>::method2<int32>()
    IL_006d:  ldc.i4.8
    IL_006e:  bne.un     IL_00f3

    IL_0073:  ldloc.0
    IL_0074:  callvirt   instance int32 class IGen`1<int32>::method2<object>()
    IL_0079:  ldc.i4.8
    IL_007a:  bne.un.s   IL_00f3

    IL_007c:  ldloc.0
    IL_007d:  callvirt   instance int32 class IGen`1<int32>::method2<string>()
    IL_0082:  ldc.i4.8
    IL_0083:  bne.un.s   IL_00f3

    IL_0085:  ldloc.0
    IL_0086:  callvirt   instance int32 class IGen`1<int32>::method2<class A`1<int32>>()
    IL_008b:  ldc.i4.8
    IL_008c:  bne.un.s   IL_00f3

    IL_008e:  ldloc.0
    IL_008f:  callvirt   instance int32 class IGen`1<int32>::method2<valuetype S`1<string>>()
    IL_0094:  ldc.i4.8
    IL_0095:  bne.un.s   IL_00f3

    IL_0097:  ldloc.1
    IL_0098:  callvirt   instance int32 class IGen`1<object>::method2<int32>()
    IL_009d:  ldc.i4.8
    IL_009e:  bne.un.s   IL_00f3

    IL_00a0:  ldloc.1
    IL_00a1:  callvirt   instance int32 class IGen`1<object>::method2<object>()
    IL_00a6:  ldc.i4.8
    IL_00a7:  bne.un.s   IL_00f3

    IL_00a9:  ldloc.1
    IL_00aa:  callvirt   instance int32 class IGen`1<object>::method2<string>()
    IL_00af:  ldc.i4.8
    IL_00b0:  bne.un.s   IL_00f3

    IL_00b2:  ldloc.1
    IL_00b3:  callvirt   instance int32 class IGen`1<object>::method2<class A`1<int32>>()
    IL_00b8:  ldc.i4.8
    IL_00b9:  bne.un.s   IL_00f3

    IL_00bb:  ldloc.1
    IL_00bc:  callvirt   instance int32 class IGen`1<object>::method2<valuetype S`1<string>>()
    IL_00c1:  ldc.i4.8
    IL_00c2:  bne.un.s   IL_00f3

    IL_00c4:  ldloc.2
    IL_00c5:  callvirt   instance int32 class IGen`1<string>::method2<int32>()
    IL_00ca:  ldc.i4.8
    IL_00cb:  bne.un.s   IL_00f3

    IL_00cd:  ldloc.2
    IL_00ce:  callvirt   instance int32 class IGen`1<string>::method2<object>()
    IL_00d3:  ldc.i4.8
    IL_00d4:  bne.un.s   IL_00f3

    IL_00d6:  ldloc.2
    IL_00d7:  callvirt   instance int32 class IGen`1<string>::method2<string>()
    IL_00dc:  ldc.i4.8
    IL_00dd:  bne.un.s   IL_00f3

    IL_00df:  ldloc.2
    IL_00e0:  callvirt   instance int32 class IGen`1<string>::method2<class A`1<int32>>()
    IL_00e5:  ldc.i4.8
    IL_00e6:  bne.un.s   IL_00f3

    IL_00e8:  ldloc.2
    IL_00e9:  callvirt   instance int32 class IGen`1<string>::method2<valuetype S`1<string>>()
    IL_00ee:  ldc.i4.8
    IL_00ef:  ceq
    IL_00f1:  br.s       IL_00f4

    IL_00f3:  ldc.i4.0
    IL_00f4:  stloc.s    V_6
    IL_00f6:  ldloc.s    V_6
    IL_00f8:  brtrue.s   IL_010d

    IL_00fa:  nop
    IL_00fb:  ldstr      "Failed at TestGenInterface_GenType: generic method"
    IL_0100:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0105:  nop
    IL_0106:  ldc.i4.0
    IL_0107:  stsfld     bool Test_Struct_ImplicitOverrideVirtualNewslot::pass
    IL_010c:  nop
    IL_010d:  ret
  } // end of method Test::TestGenInterface_GenType

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       73 (0x49)
    .maxstack  2
    .locals init (int32 V_0,
             bool V_1)
    IL_0000:  nop
    IL_0001:  call       void Test_Struct_ImplicitOverrideVirtualNewslot::TestNonGenInterface_NonGenType()
    IL_0006:  nop
    IL_0007:  call       void Test_Struct_ImplicitOverrideVirtualNewslot::TestNonGenInterface_GenType()
    IL_000c:  nop
    IL_000d:  call       void Test_Struct_ImplicitOverrideVirtualNewslot::TestGenInterface_NonGenType()
    IL_0012:  nop
    IL_0013:  call       void Test_Struct_ImplicitOverrideVirtualNewslot::TestGenInterface_GenType()
    IL_0018:  nop
    IL_0019:  ldsfld     bool Test_Struct_ImplicitOverrideVirtualNewslot::pass
    IL_001e:  ldc.i4.0
    IL_001f:  ceq
    IL_0021:  stloc.1
    IL_0022:  ldloc.1
    IL_0023:  brtrue.s   IL_0036

    IL_0025:  nop
    IL_0026:  ldstr      "PASS"
    IL_002b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0030:  nop
    IL_0031:  ldc.i4.s   100
    IL_0033:  stloc.0
    IL_0034:  br.s       IL_0047

    IL_0036:  nop
    IL_0037:  ldstr      "FAIL"
    IL_003c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0041:  nop
    IL_0042:  ldc.i4.s   101
    IL_0044:  stloc.0
    IL_0045:  br.s       IL_0047

    IL_0047:  ldloc.0
    IL_0048:  ret
  } // end of method Test::Main

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

  .method private hidebysig specialname rtspecialname static
          void  .cctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  stsfld     bool Test_Struct_ImplicitOverrideVirtualNewslot::pass
    IL_0006:  ret
  } // end of method Test::.cctor

} // end of class Test
