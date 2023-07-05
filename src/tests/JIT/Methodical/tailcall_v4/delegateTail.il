// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern mscorlib { auto }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly 'delegateTail'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       
.corflags 0x00000001    



.namespace Test_delegateTail_il
{
.class public auto ansi sealed beforefieldinit Closure
       extends [mscorlib]System.Object
{
  .field public initonly object[] Constants
  .field public initonly object[] Locals
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object[] constants,
                               object[] locals) cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      object[] Test_delegateTail_il.Closure::Constants
    IL_000d:  ldarg.0
    IL_000e:  ldarg.2
    IL_000f:  stfld      object[] Test_delegateTail_il.Closure::Locals
    IL_0014:  ret
  } 

  .method public hidebysig instance object 
          FImpl(object o1,
                object o2,
                object o3,
                object o4,
                object o5,
                object o6,
                object o7,
                object o8,
                object o9) cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      object[] Test_delegateTail_il.Closure::Constants
    IL_0006:  ldc.i4.0
    IL_0007:  ldelem.ref
    IL_0008:  castclass  string[]
    IL_000d:  ret
  } 

  .method public hidebysig instance object 
          GImpl(int32 i1,
                int32 i2,
                int32 i3,
                int32 i4,
                int32 i5,
                int32 i6,
                int32 i7,
                int32 i8,
                int32 i9) cil managed
  {
    .maxstack  10
    IL_0000:  ldarg.0
    IL_0001:  ldfld      object[] Test_delegateTail_il.Closure::Constants
    IL_0006:  ldc.i4.0
    IL_0007:  ldelem.ref
    IL_0008:  castclass  class [System.Runtime]System.Func`10<object,object,object,object,object,object,object,object,object,object>
    IL_000d:  ldarg.1
    IL_000e:  box        [mscorlib]System.Int32
    IL_0013:  ldarg.2
    IL_0014:  box        [mscorlib]System.Int32
    IL_0019:  ldarg.3
    IL_001a:  box        [mscorlib]System.Int32
    IL_001f:  ldarg.s    i4
    IL_0021:  box        [mscorlib]System.Int32
    IL_0026:  ldarg.s    i5
    IL_0028:  box        [mscorlib]System.Int32
    IL_002d:  ldarg.s    i6
    IL_002f:  box        [mscorlib]System.Int32
    IL_0034:  ldarg.s    i7
    IL_0036:  box        [mscorlib]System.Int32
    IL_003b:  ldarg.s    i8
    IL_003d:  box        [mscorlib]System.Int32
    IL_0042:  ldarg.s    i9
    IL_0044:  box        [mscorlib]System.Int32
    IL_0049:  tail.
              callvirt   instance !9 class [System.Runtime]System.Func`10<object,object,object,object,object,object,object,object,object,object>::Invoke(!0,
                                                                                                                                                      !1,
                                                                                                                                                      !2,
                                                                                                                                                      !3,
                                                                                                                                                      !4,
                                                                                                                                                      !5,
                                                                                                                                                      !6,
                                                                                                                                                      !7,
                                                                                                                                                      !8)
    IL_004f:  ret
  } 

} 

.class public abstract auto ansi sealed beforefieldinit Repro
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  10
    .locals init (class [System.Runtime]System.Func`10<object,object,object,object,object,object,object,object,object,object> V_0,
             class [System.Runtime]System.Func`10<int32,int32,int32,int32,int32,int32,int32,int32,int32,object> V_1,
             object[] V_2,
             string[] V_3,
             object[] V_4)
    IL_0000:  ldc.i4.1
    IL_0001:  newarr     [mscorlib]System.Object
    IL_0006:  stloc.2
    IL_0007:  ldloc.2
    IL_0008:  ldc.i4.0
    IL_0009:  ldc.i4.1
    IL_000a:  newarr     [mscorlib]System.String
    IL_000f:  stloc.3
    IL_0010:  ldloc.3
    IL_0011:  ldc.i4.0
    IL_0012:  ldstr      "a"
    IL_0017:  stelem.ref
    IL_0018:  ldloc.3
    IL_0019:  stelem.ref
    IL_001a:  ldloc.2
    IL_001b:  ldnull
    IL_001c:  newobj     instance void Test_delegateTail_il.Closure::.ctor(object[],
                                                      object[])
    IL_0021:  ldftn      instance object Test_delegateTail_il.Closure::FImpl(object,
                                                        object,
                                                        object,
                                                        object,
                                                        object,
                                                        object,
                                                        object,
                                                        object,
                                                        object)
    IL_0027:  newobj     instance void class [System.Runtime]System.Func`10<object,object,object,object,object,object,object,object,object,object>::.ctor(object,
                                                                                                                                                       native int)
    IL_002c:  stloc.0
    IL_002d:  ldc.i4.1
    IL_002e:  newarr     [mscorlib]System.Object
    IL_0033:  stloc.s    V_4
    IL_0035:  ldloc.s    V_4
    IL_0037:  ldc.i4.0
    IL_0038:  ldloc.0
    IL_0039:  stelem.ref
    IL_003a:  ldloc.s    V_4
    IL_003c:  ldnull
    IL_003d:  newobj     instance void Test_delegateTail_il.Closure::.ctor(object[],
                                                      object[])
    IL_0042:  ldftn      instance object Test_delegateTail_il.Closure::GImpl(int32,
                                                        int32,
                                                        int32,
                                                        int32,
                                                        int32,
                                                        int32,
                                                        int32,
                                                        int32,
                                                        int32)
    IL_0048:  newobj     instance void class [System.Runtime]System.Func`10<int32,int32,int32,int32,int32,int32,int32,int32,int32,object>::.ctor(object,
                                                                                                                                              native int)
    IL_004d:  stloc.1
    IL_004e:  ldloc.1
    IL_004f:  ldc.i4.1
    IL_0050:  ldc.i4.2
    IL_0051:  ldc.i4.3
    IL_0052:  ldc.i4.4
    IL_0053:  ldc.i4.5
    IL_0054:  ldc.i4.6
    IL_0055:  ldc.i4.7
    IL_0056:  ldc.i4.8
    IL_0057:  ldc.i4.s   9
    IL_0059:  callvirt   instance !9 class [System.Runtime]System.Func`10<int32,int32,int32,int32,int32,int32,int32,int32,int32,object>::Invoke(!0,
                                                                                                                                             !1,
                                                                                                                                             !2,
                                                                                                                                             !3,
                                                                                                                                             !4,
                                                                                                                                             !5,
                                                                                                                                             !6,
                                                                                                                                             !7,
                                                                                                                                             !8)
    IL_005e:  pop
ldc.i4 100
    IL_005f:  ret
  } 

} 
}
