// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
// This test is the same as ExplicitOverrideVirtualNewslotFinal.cs
// The difference is that here the virtual methods are 'virtual newslot' as opposed to
// 'virtual newslot final'
// C# doesn't allow generating explicitly overriding methods with 'virtual' keyword (which translates to 
// 'virtual newslot'


.assembly extern mscorlib {}

.assembly Class_ExplicitOverrideVirtualNewslot {}


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

.class public auto ansi beforefieldinit C1
       extends [mscorlib]System.Object
       implements I
{
  .method private hidebysig newslot virtual 
          instance int32  I.method1() cil managed
  {
    .override I::method1
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C1::I.method1

  .method private hidebysig newslot virtual 
          instance int32  I.method2<T>() cil managed
  {
    .override I::method2
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.2
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C1::I.method2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C1::.ctor

} // end of class C1

.class public auto ansi beforefieldinit C2`1<T>
       extends [mscorlib]System.Object
       implements I
{
  .method private hidebysig newslot virtual 
          instance int32  I.method1() cil managed
  {
    .override I::method1
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.3
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C2`1::I.method1

  .method private hidebysig newslot virtual 
          instance int32  I.method2<U>() cil managed
  {
    .override I::method2
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.4
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C2`1::I.method2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C2`1::.ctor

} // end of class C2`1

.class public auto ansi beforefieldinit C3Int
       extends [mscorlib]System.Object
       implements class IGen`1<int32>
{
  .method private hidebysig newslot virtual 
          instance int32  'IGen<System.Int32>.method1'() cil managed
  {
    .override  method instance int32 class IGen`1<int32>::method1()
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.5
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C3Int::'IGen<System.Int32>.method1'

  .method private hidebysig newslot virtual 
          instance int32  'IGen<System.Int32>.method2'<U>() cil managed
  {
    .override  method instance int32 class IGen`1<int32>::method2<[1]>()
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.6
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C3Int::'IGen<System.Int32>.method2'

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C3Int::.ctor

} // end of class C3Int

.class public auto ansi beforefieldinit C3String
       extends [mscorlib]System.Object
       implements class IGen`1<string>
{
  .method private hidebysig newslot virtual 
          instance int32  'IGen<System.String>.method1'() cil managed
  {
    .override  method instance int32 class IGen`1<string>::method1()
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.5
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C3String::'IGen<System.String>.method1'

  .method private hidebysig newslot virtual 
          instance int32  'IGen<System.String>.method2'<U>() cil managed
  {
    .override  method instance int32 class IGen`1<string>::method2<[1]>()
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.6
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C3String::'IGen<System.String>.method2'

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C3String::.ctor

} // end of class C3String

.class public auto ansi beforefieldinit C3Object
       extends [mscorlib]System.Object
       implements class IGen`1<object>
{
  .method private hidebysig newslot virtual 
          instance int32  'IGen<System.Object>.method1'() cil managed
  {
    .override  method instance int32 class IGen`1<object>::method1()
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.5
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C3Object::'IGen<System.Object>.method1'

  .method private hidebysig newslot virtual 
          instance int32  'IGen<System.Object>.method2'<U>() cil managed
  {
    .override  method instance int32 class IGen`1<object>::method2<[1]>()
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.6
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C3Object::'IGen<System.Object>.method2'

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C3Object::.ctor

} // end of class C3Object

.class public auto ansi beforefieldinit C4`1<T>
       extends [mscorlib]System.Object
       implements class IGen`1<!T>
{
  .method private hidebysig newslot virtual 
          instance int32  'IGen<T>.method1'() cil managed
  {
    .override  method instance int32 class IGen`1<!T>::method1()
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.7
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C4`1::'IGen<T>.method1'

  .method private hidebysig newslot virtual 
          instance int32  'IGen<T>.method2'<U>() cil managed
  {
    .override  method instance int32 class IGen`1<!T>::method2<[1]>()
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.8
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C4`1::'IGen<T>.method2'

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C4`1::.ctor

} // end of class C4`1

.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .field public static bool pass
  .method public hidebysig static void  TestNonGenInterface_NonGenType() cil managed
  {
    // Code size       79 (0x4f)
    .maxstack  2
    .locals init (class I V_0,
             bool V_1)
    IL_0000:  nop
    IL_0001:  newobj     instance void C1::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  callvirt   instance int32 I::method2<int32>()
    IL_000d:  ldc.i4.2
    IL_000e:  bne.un.s   IL_0036

    IL_0010:  ldloc.0
    IL_0011:  callvirt   instance int32 I::method2<string>()
    IL_0016:  ldc.i4.2
    IL_0017:  bne.un.s   IL_0036

    IL_0019:  ldloc.0
    IL_001a:  callvirt   instance int32 I::method2<object>()
    IL_001f:  ldc.i4.2
    IL_0020:  bne.un.s   IL_0036

    IL_0022:  ldloc.0
    IL_0023:  callvirt   instance int32 I::method2<class A`1<int32>>()
    IL_0028:  ldc.i4.2
    IL_0029:  bne.un.s   IL_0036

    IL_002b:  ldloc.0
    IL_002c:  callvirt   instance int32 I::method2<valuetype S`1<object>>()
    IL_0031:  ldc.i4.2
    IL_0032:  ceq
    IL_0034:  br.s       IL_0037

    IL_0036:  ldc.i4.0
    IL_0037:  stloc.1
    IL_0038:  ldloc.1
    IL_0039:  brtrue.s   IL_004e

    IL_003b:  nop
    IL_003c:  ldstr      "Failed at TestNonGenInterface_NonGenType: generic "
    + "method"
    IL_0041:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0046:  nop
    IL_0047:  ldc.i4.0
    IL_0048:  stsfld     bool Test::pass
    IL_004d:  nop
    IL_004e:  ret
  } // end of method Test::TestNonGenInterface_NonGenType

  .method public hidebysig static void  TestNonGenInterface_GenType() cil managed
  {
    // Code size       237 (0xed)
    .maxstack  2
    .locals init (class I V_0,
             class I V_1,
             class I V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  newobj     instance void class C2`1<int32>::.ctor()
    IL_0006:  stloc.0
    IL_0007:  newobj     instance void class C2`1<object>::.ctor()
    IL_000c:  stloc.1
    IL_000d:  newobj     instance void class C2`1<string>::.ctor()
    IL_0012:  stloc.2
    IL_0013:  ldloc.0
    IL_0014:  callvirt   instance int32 I::method1()
    IL_0019:  ldc.i4.3
    IL_001a:  bne.un.s   IL_0030

    IL_001c:  ldloc.2
    IL_001d:  callvirt   instance int32 I::method1()
    IL_0022:  ldc.i4.3
    IL_0023:  bne.un.s   IL_0030

    IL_0025:  ldloc.1
    IL_0026:  callvirt   instance int32 I::method1()
    IL_002b:  ldc.i4.3
    IL_002c:  ceq
    IL_002e:  br.s       IL_0031

    IL_0030:  ldc.i4.0
    IL_0031:  stloc.3
    IL_0032:  ldloc.3
    IL_0033:  brtrue.s   IL_0048

    IL_0035:  nop
    IL_0036:  ldstr      "Failed at TestNonGenInterface_GenType: non generic"
    + " method"
    IL_003b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0040:  nop
    IL_0041:  ldc.i4.0
    IL_0042:  stsfld     bool Test::pass
    IL_0047:  nop
    IL_0048:  ldloc.0
    IL_0049:  callvirt   instance int32 I::method2<int32>()
    IL_004e:  ldc.i4.4
    IL_004f:  bne.un     IL_00d4

    IL_0054:  ldloc.0
    IL_0055:  callvirt   instance int32 I::method2<object>()
    IL_005a:  ldc.i4.4
    IL_005b:  bne.un.s   IL_00d4

    IL_005d:  ldloc.0
    IL_005e:  callvirt   instance int32 I::method2<string>()
    IL_0063:  ldc.i4.4
    IL_0064:  bne.un.s   IL_00d4

    IL_0066:  ldloc.0
    IL_0067:  callvirt   instance int32 I::method2<class A`1<int32>>()
    IL_006c:  ldc.i4.4
    IL_006d:  bne.un.s   IL_00d4

    IL_006f:  ldloc.0
    IL_0070:  callvirt   instance int32 I::method2<valuetype S`1<string>>()
    IL_0075:  ldc.i4.4
    IL_0076:  bne.un.s   IL_00d4

    IL_0078:  ldloc.2
    IL_0079:  callvirt   instance int32 I::method2<int32>()
    IL_007e:  ldc.i4.4
    IL_007f:  bne.un.s   IL_00d4

    IL_0081:  ldloc.2
    IL_0082:  callvirt   instance int32 I::method2<object>()
    IL_0087:  ldc.i4.4
    IL_0088:  bne.un.s   IL_00d4

    IL_008a:  ldloc.2
    IL_008b:  callvirt   instance int32 I::method2<string>()
    IL_0090:  ldc.i4.4
    IL_0091:  bne.un.s   IL_00d4

    IL_0093:  ldloc.2
    IL_0094:  callvirt   instance int32 I::method2<class A`1<int32>>()
    IL_0099:  ldc.i4.4
    IL_009a:  bne.un.s   IL_00d4

    IL_009c:  ldloc.2
    IL_009d:  callvirt   instance int32 I::method2<valuetype S`1<string>>()
    IL_00a2:  ldc.i4.4
    IL_00a3:  bne.un.s   IL_00d4

    IL_00a5:  ldloc.1
    IL_00a6:  callvirt   instance int32 I::method2<int32>()
    IL_00ab:  ldc.i4.4
    IL_00ac:  bne.un.s   IL_00d4

    IL_00ae:  ldloc.1
    IL_00af:  callvirt   instance int32 I::method2<object>()
    IL_00b4:  ldc.i4.4
    IL_00b5:  bne.un.s   IL_00d4

    IL_00b7:  ldloc.1
    IL_00b8:  callvirt   instance int32 I::method2<string>()
    IL_00bd:  ldc.i4.4
    IL_00be:  bne.un.s   IL_00d4

    IL_00c0:  ldloc.1
    IL_00c1:  callvirt   instance int32 I::method2<class A`1<int32>>()
    IL_00c6:  ldc.i4.4
    IL_00c7:  bne.un.s   IL_00d4

    IL_00c9:  ldloc.1
    IL_00ca:  callvirt   instance int32 I::method2<valuetype S`1<string>>()
    IL_00cf:  ldc.i4.4
    IL_00d0:  ceq
    IL_00d2:  br.s       IL_00d5

    IL_00d4:  ldc.i4.0
    IL_00d5:  stloc.3
    IL_00d6:  ldloc.3
    IL_00d7:  brtrue.s   IL_00ec

    IL_00d9:  nop
    IL_00da:  ldstr      "Failed at TestNonGenInterface_GenType: generic met"
    + "hod"
    IL_00df:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00e4:  nop
    IL_00e5:  ldc.i4.0
    IL_00e6:  stsfld     bool Test::pass
    IL_00eb:  nop
    IL_00ec:  ret
  } // end of method Test::TestNonGenInterface_GenType

  .method public hidebysig static void  TestGenInterface_NonGenType() cil managed
  {
    // Code size       237 (0xed)
    .maxstack  2
    .locals init (class IGen`1<int32> V_0,
             class IGen`1<object> V_1,
             class IGen`1<string> V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  newobj     instance void C3Int::.ctor()
    IL_0006:  stloc.0
    IL_0007:  newobj     instance void C3Object::.ctor()
    IL_000c:  stloc.1
    IL_000d:  newobj     instance void C3String::.ctor()
    IL_0012:  stloc.2
    IL_0013:  ldloc.0
    IL_0014:  callvirt   instance int32 class IGen`1<int32>::method1()
    IL_0019:  ldc.i4.5
    IL_001a:  bne.un.s   IL_0030

    IL_001c:  ldloc.1
    IL_001d:  callvirt   instance int32 class IGen`1<object>::method1()
    IL_0022:  ldc.i4.5
    IL_0023:  bne.un.s   IL_0030

    IL_0025:  ldloc.2
    IL_0026:  callvirt   instance int32 class IGen`1<string>::method1()
    IL_002b:  ldc.i4.5
    IL_002c:  ceq
    IL_002e:  br.s       IL_0031

    IL_0030:  ldc.i4.0
    IL_0031:  stloc.3
    IL_0032:  ldloc.3
    IL_0033:  brtrue.s   IL_0048

    IL_0035:  nop
    IL_0036:  ldstr      "Failed at TestGenInterface_NonGenType: non generic"
    + " method"
    IL_003b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0040:  nop
    IL_0041:  ldc.i4.0
    IL_0042:  stsfld     bool Test::pass
    IL_0047:  nop
    IL_0048:  ldloc.0
    IL_0049:  callvirt   instance int32 class IGen`1<int32>::method2<int32>()
    IL_004e:  ldc.i4.6
    IL_004f:  bne.un     IL_00d4

    IL_0054:  ldloc.0
    IL_0055:  callvirt   instance int32 class IGen`1<int32>::method2<object>()
    IL_005a:  ldc.i4.6
    IL_005b:  bne.un.s   IL_00d4

    IL_005d:  ldloc.0
    IL_005e:  callvirt   instance int32 class IGen`1<int32>::method2<string>()
    IL_0063:  ldc.i4.6
    IL_0064:  bne.un.s   IL_00d4

    IL_0066:  ldloc.0
    IL_0067:  callvirt   instance int32 class IGen`1<int32>::method2<class A`1<int32>>()
    IL_006c:  ldc.i4.6
    IL_006d:  bne.un.s   IL_00d4

    IL_006f:  ldloc.0
    IL_0070:  callvirt   instance int32 class IGen`1<int32>::method2<valuetype S`1<string>>()
    IL_0075:  ldc.i4.6
    IL_0076:  bne.un.s   IL_00d4

    IL_0078:  ldloc.1
    IL_0079:  callvirt   instance int32 class IGen`1<object>::method2<int32>()
    IL_007e:  ldc.i4.6
    IL_007f:  bne.un.s   IL_00d4

    IL_0081:  ldloc.1
    IL_0082:  callvirt   instance int32 class IGen`1<object>::method2<object>()
    IL_0087:  ldc.i4.6
    IL_0088:  bne.un.s   IL_00d4

    IL_008a:  ldloc.1
    IL_008b:  callvirt   instance int32 class IGen`1<object>::method2<string>()
    IL_0090:  ldc.i4.6
    IL_0091:  bne.un.s   IL_00d4

    IL_0093:  ldloc.1
    IL_0094:  callvirt   instance int32 class IGen`1<object>::method2<class A`1<int32>>()
    IL_0099:  ldc.i4.6
    IL_009a:  bne.un.s   IL_00d4

    IL_009c:  ldloc.1
    IL_009d:  callvirt   instance int32 class IGen`1<object>::method2<valuetype S`1<string>>()
    IL_00a2:  ldc.i4.6
    IL_00a3:  bne.un.s   IL_00d4

    IL_00a5:  ldloc.2
    IL_00a6:  callvirt   instance int32 class IGen`1<string>::method2<int32>()
    IL_00ab:  ldc.i4.6
    IL_00ac:  bne.un.s   IL_00d4

    IL_00ae:  ldloc.2
    IL_00af:  callvirt   instance int32 class IGen`1<string>::method2<object>()
    IL_00b4:  ldc.i4.6
    IL_00b5:  bne.un.s   IL_00d4

    IL_00b7:  ldloc.2
    IL_00b8:  callvirt   instance int32 class IGen`1<string>::method2<string>()
    IL_00bd:  ldc.i4.6
    IL_00be:  bne.un.s   IL_00d4

    IL_00c0:  ldloc.2
    IL_00c1:  callvirt   instance int32 class IGen`1<string>::method2<class A`1<int32>>()
    IL_00c6:  ldc.i4.6
    IL_00c7:  bne.un.s   IL_00d4

    IL_00c9:  ldloc.2
    IL_00ca:  callvirt   instance int32 class IGen`1<string>::method2<valuetype S`1<string>>()
    IL_00cf:  ldc.i4.6
    IL_00d0:  ceq
    IL_00d2:  br.s       IL_00d5

    IL_00d4:  ldc.i4.0
    IL_00d5:  stloc.3
    IL_00d6:  ldloc.3
    IL_00d7:  brtrue.s   IL_00ec

    IL_00d9:  nop
    IL_00da:  ldstr      "Failed at TestGenInterface_NonGenType: generic met"
    + "hod"
    IL_00df:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00e4:  nop
    IL_00e5:  ldc.i4.0
    IL_00e6:  stsfld     bool Test::pass
    IL_00eb:  nop
    IL_00ec:  ret
  } // end of method Test::TestGenInterface_NonGenType

  .method public hidebysig static void  TestGenInterface_GenType() cil managed
  {
    // Code size       237 (0xed)
    .maxstack  2
    .locals init (class IGen`1<int32> V_0,
             class IGen`1<object> V_1,
             class IGen`1<string> V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  newobj     instance void class C4`1<int32>::.ctor()
    IL_0006:  stloc.0
    IL_0007:  newobj     instance void class C4`1<object>::.ctor()
    IL_000c:  stloc.1
    IL_000d:  newobj     instance void class C4`1<string>::.ctor()
    IL_0012:  stloc.2
    IL_0013:  ldloc.0
    IL_0014:  callvirt   instance int32 class IGen`1<int32>::method1()
    IL_0019:  ldc.i4.7
    IL_001a:  bne.un.s   IL_0030

    IL_001c:  ldloc.1
    IL_001d:  callvirt   instance int32 class IGen`1<object>::method1()
    IL_0022:  ldc.i4.7
    IL_0023:  bne.un.s   IL_0030

    IL_0025:  ldloc.2
    IL_0026:  callvirt   instance int32 class IGen`1<string>::method1()
    IL_002b:  ldc.i4.7
    IL_002c:  ceq
    IL_002e:  br.s       IL_0031

    IL_0030:  ldc.i4.0
    IL_0031:  stloc.3
    IL_0032:  ldloc.3
    IL_0033:  brtrue.s   IL_0048

    IL_0035:  nop
    IL_0036:  ldstr      "Failed at TestGenInterface_GenType: non generic me"
    + "thod"
    IL_003b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0040:  nop
    IL_0041:  ldc.i4.0
    IL_0042:  stsfld     bool Test::pass
    IL_0047:  nop
    IL_0048:  ldloc.0
    IL_0049:  callvirt   instance int32 class IGen`1<int32>::method2<int32>()
    IL_004e:  ldc.i4.8
    IL_004f:  bne.un     IL_00d4

    IL_0054:  ldloc.0
    IL_0055:  callvirt   instance int32 class IGen`1<int32>::method2<object>()
    IL_005a:  ldc.i4.8
    IL_005b:  bne.un.s   IL_00d4

    IL_005d:  ldloc.0
    IL_005e:  callvirt   instance int32 class IGen`1<int32>::method2<string>()
    IL_0063:  ldc.i4.8
    IL_0064:  bne.un.s   IL_00d4

    IL_0066:  ldloc.0
    IL_0067:  callvirt   instance int32 class IGen`1<int32>::method2<class A`1<int32>>()
    IL_006c:  ldc.i4.8
    IL_006d:  bne.un.s   IL_00d4

    IL_006f:  ldloc.0
    IL_0070:  callvirt   instance int32 class IGen`1<int32>::method2<valuetype S`1<string>>()
    IL_0075:  ldc.i4.8
    IL_0076:  bne.un.s   IL_00d4

    IL_0078:  ldloc.1
    IL_0079:  callvirt   instance int32 class IGen`1<object>::method2<int32>()
    IL_007e:  ldc.i4.8
    IL_007f:  bne.un.s   IL_00d4

    IL_0081:  ldloc.1
    IL_0082:  callvirt   instance int32 class IGen`1<object>::method2<object>()
    IL_0087:  ldc.i4.8
    IL_0088:  bne.un.s   IL_00d4

    IL_008a:  ldloc.1
    IL_008b:  callvirt   instance int32 class IGen`1<object>::method2<string>()
    IL_0090:  ldc.i4.8
    IL_0091:  bne.un.s   IL_00d4

    IL_0093:  ldloc.1
    IL_0094:  callvirt   instance int32 class IGen`1<object>::method2<class A`1<int32>>()
    IL_0099:  ldc.i4.8
    IL_009a:  bne.un.s   IL_00d4

    IL_009c:  ldloc.1
    IL_009d:  callvirt   instance int32 class IGen`1<object>::method2<valuetype S`1<string>>()
    IL_00a2:  ldc.i4.8
    IL_00a3:  bne.un.s   IL_00d4

    IL_00a5:  ldloc.2
    IL_00a6:  callvirt   instance int32 class IGen`1<string>::method2<int32>()
    IL_00ab:  ldc.i4.8
    IL_00ac:  bne.un.s   IL_00d4

    IL_00ae:  ldloc.2
    IL_00af:  callvirt   instance int32 class IGen`1<string>::method2<object>()
    IL_00b4:  ldc.i4.8
    IL_00b5:  bne.un.s   IL_00d4

    IL_00b7:  ldloc.2
    IL_00b8:  callvirt   instance int32 class IGen`1<string>::method2<string>()
    IL_00bd:  ldc.i4.8
    IL_00be:  bne.un.s   IL_00d4

    IL_00c0:  ldloc.2
    IL_00c1:  callvirt   instance int32 class IGen`1<string>::method2<class A`1<int32>>()
    IL_00c6:  ldc.i4.8
    IL_00c7:  bne.un.s   IL_00d4

    IL_00c9:  ldloc.2
    IL_00ca:  callvirt   instance int32 class IGen`1<string>::method2<valuetype S`1<string>>()
    IL_00cf:  ldc.i4.8
    IL_00d0:  ceq
    IL_00d2:  br.s       IL_00d5

    IL_00d4:  ldc.i4.0
    IL_00d5:  stloc.3
    IL_00d6:  ldloc.3
    IL_00d7:  brtrue.s   IL_00ec

    IL_00d9:  nop
    IL_00da:  ldstr      "Failed at TestGenInterface_GenType: generic method"
    IL_00df:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00e4:  nop
    IL_00e5:  ldc.i4.0
    IL_00e6:  stsfld     bool Test::pass
    IL_00eb:  nop
    IL_00ec:  ret
  } // end of method Test::TestGenInterface_GenType

  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       73 (0x49)
    .maxstack  2
    .locals init (int32 V_0,
             bool V_1)
    IL_0000:  nop
    IL_0001:  call       void Test::TestNonGenInterface_NonGenType()
    IL_0006:  nop
    IL_0007:  call       void Test::TestNonGenInterface_GenType()
    IL_000c:  nop
    IL_000d:  call       void Test::TestGenInterface_NonGenType()
    IL_0012:  nop
    IL_0013:  call       void Test::TestGenInterface_GenType()
    IL_0018:  nop
    IL_0019:  ldsfld     bool Test::pass
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
    IL_0001:  stsfld     bool Test::pass
    IL_0006:  ret
  } // end of method Test::.cctor

} // end of class Test


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file ExplicitOverrideVirtualNewslot.res
