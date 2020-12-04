// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// The test exposes the issue when:
//
//   1) methods Callee and Caller have the following structure:
//
//      S Callee() { produce and return an instance of S }
//
//      S Caller() { return Callee(); }
//
//   2) S is a value type with non-GC fields and has such size that it
//      must be passed via return buffer;
//
//   3) call to Callee is transformed to tail call via helper:
//
//      S result;
//      DispatchTailCalls(&IL_STUB_CallTailCallTarget, (IntPtr)&result, _AddressOfReturnAddress());
//      return result;
//
//   4) Caller is invoked via Reflection.
//
// During morph phase the JIT discovers that both Caller and Callee have the return buffer arguments and
// falsely assumes that the value can be directly passed from Caller to Callee.
// Here is a problem: DispatchTailCalls helper expects the RetValue argument (in this case, return buffer argument)
// to always point to the stack. However, during a reflection call the return buffer for S value type can be placed on the GC heap (see condition 2 above).
// As a consequence, when GC is called at DispatchTailCalls the object corresponding to the return value buffer can be moved,
// but the pointers passed to DispatchTailCalls will not be updated that leads to Callee using the non-updated location when writing its return value.
//
// Note: you will find below that Caller uses localloc - this is done in order to prevent a call to Caller to be transformed into fast tail call.
// Note: COMPlus_GCStress=3 or COMPlus_GCStress=C are required in order to reliably expose this issue.

.assembly extern System.Runtime
{
}

.assembly Runtime_39581
{
}

.class private sequential ansi sealed beforefieldinit int32x8
       extends [System.Runtime]System.ValueType
{
  .field public int32 a
  .field public int32 b
  .field public int32 c
  .field public int32 d
  .field public int32 e
  .field public int32 f
  .field public int32 g
  .field public int32 h
}

.class private auto ansi beforefieldinit Runtime_39581.Program
       extends [System.Runtime]System.Object
{
  .method private hidebysig static valuetype int32x8 Callee(uint8* arg0) cil managed noinlining
  {
    .maxstack  2
    .locals init (valuetype int32x8 V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    int32x8
    IL_0008:  ldloca.s   V_0
    IL_000a:  ldc.i4.0
    IL_000b:  stfld      int32 int32x8::a
    IL_0010:  ldloca.s   V_0
    IL_0012:  ldc.i4.1
    IL_0013:  stfld      int32 int32x8::b
    IL_0018:  ldloca.s   V_0
    IL_001a:  ldc.i4.2
    IL_001b:  stfld      int32 int32x8::c
    IL_0020:  ldloca.s   V_0
    IL_0022:  ldc.i4.3
    IL_0023:  stfld      int32 int32x8::d
    IL_0028:  ldloca.s   V_0
    IL_002a:  ldc.i4.4
    IL_002b:  stfld      int32 int32x8::e
    IL_0030:  ldloca.s   V_0
    IL_0032:  ldc.i4.5
    IL_0033:  stfld      int32 int32x8::f
    IL_0038:  ldloca.s   V_0
    IL_003a:  ldc.i4.6
    IL_003b:  stfld      int32 int32x8::g
    IL_0040:  ldloca.s   V_0
    IL_0042:  ldc.i4.7
    IL_0043:  stfld      int32 int32x8::h
    IL_0048:  ldloc.0
    IL_0049:  ret
  }

  .method public hidebysig static valuetype int32x8 Caller(int32 arg0) cil managed noinlining
  {
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  conv.u
    IL_0002:  localloc
    IL_0004:  tail.
    IL_0006:  call       valuetype int32x8 Runtime_39581.Program::Callee(uint8*)
    IL_000b:  ret
  }

  .method private hidebysig static int32 Main(string[] args) cil managed
  {
    .entrypoint
    .maxstack  6
    .locals init (valuetype int32x8 V_0)
    IL_0000:  ldtoken    Runtime_39581.Program
    IL_0005:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_000a:  ldstr      "Caller"
    IL_000f:  ldc.i4.1
    IL_0010:  newarr     [System.Runtime]System.Type
    IL_0015:  dup
    IL_0016:  ldc.i4.0
    IL_0017:  ldtoken    [System.Runtime]System.Int32
    IL_001c:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0021:  stelem.ref
    IL_0022:  call       instance class [System.Runtime]System.Reflection.MethodInfo [System.Runtime]System.Type::GetMethod(string, class [System.Runtime]System.Type[])
    IL_0027:  ldnull
    IL_0028:  ldc.i4.1
    IL_0029:  newarr     [System.Runtime]System.Object
    IL_002e:  dup
    IL_002f:  ldc.i4.0
    IL_0030:  ldc.i4.1
    IL_0031:  box        [System.Runtime]System.Int32
    IL_0036:  stelem.ref
    IL_0037:  callvirt   instance object [System.Runtime]System.Reflection.MethodBase::Invoke(object, object[])
    IL_003c:  unbox.any  int32x8
    IL_0041:  stloc.0
    IL_0042:  ldloc.0
    IL_0043:  ldfld      int32 int32x8::a
    IL_0048:  brtrue.s   IL_008c

    IL_004a:  ldloc.0
    IL_004b:  ldfld      int32 int32x8::b
    IL_0050:  ldc.i4.1
    IL_0051:  bne.un.s   IL_008c

    IL_0053:  ldloc.0
    IL_0054:  ldfld      int32 int32x8::c
    IL_0059:  ldc.i4.2
    IL_005a:  bne.un.s   IL_008c

    IL_005c:  ldloc.0
    IL_005d:  ldfld      int32 int32x8::d
    IL_0062:  ldc.i4.3
    IL_0063:  bne.un.s   IL_008c

    IL_0065:  ldloc.0
    IL_0066:  ldfld      int32 int32x8::e
    IL_006b:  ldc.i4.4
    IL_006c:  bne.un.s   IL_008c

    IL_006e:  ldloc.0
    IL_006f:  ldfld      int32 int32x8::f
    IL_0074:  ldc.i4.5
    IL_0075:  bne.un.s   IL_008c

    IL_0077:  ldloc.0
    IL_0078:  ldfld      int32 int32x8::g
    IL_007d:  ldc.i4.6
    IL_007e:  bne.un.s   IL_008c

    IL_0080:  ldloc.0
    IL_0081:  ldfld      int32 int32x8::h
    IL_0086:  ldc.i4.7
    IL_0087:  bne.un.s   IL_008c

    IL_0089:  ldc.i4.s   100
    IL_008b:  ret

    IL_008c:  ldc.i4.0
    IL_008d:  ret
  }
}
