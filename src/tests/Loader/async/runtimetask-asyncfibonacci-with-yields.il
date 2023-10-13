// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//  .NET IL Disassembler.  Version 9.0.0-dev



// Metadata version: v4.0.30319
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 9:0:0:0
}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 9:0:0:0
}
.assembly 'runtimetask-asyncfibonacci-with-yields'
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                   63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [System.Runtime]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module 'runtimetask-asyncfibonacci-with-yields.dll'
// MVID: {ca42840a-c801-47a9-8471-ea986ae2c60d}
.custom instance void [System.Runtime]System.Runtime.CompilerServices.RefSafetyRulesAttribute::.ctor(int32) = ( 01 00 0B 00 00 00 00 00 ) 
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x000001E00BDE0000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit Program
       extends [System.Runtime]System.Object
{
  .class auto ansi sealed nested private beforefieldinit '<<Main>$>d__0'
         extends [System.Runtime]System.Object
         implements [System.Runtime]System.Runtime.CompilerServices.IAsyncStateMachine
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public int32 '<>1__state'
    .field public valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> '<>t__builder'
    .field public string[] args
    .field private int32 '<i>5__1'
    .field private class [System.Runtime]System.Diagnostics.Stopwatch '<sw>5__2'
    .field private uint32 '<result>5__3'
    .field private uint32 '<>s__4'
    .field private valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<uint32> '<>u__1'
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  ret
    } // end of method '<<Main>$>d__0'::.ctor

    .method private hidebysig newslot virtual final 
            instance void  MoveNext() cil managed
    {
      .override [System.Runtime]System.Runtime.CompilerServices.IAsyncStateMachine::MoveNext
      // Code size       334 (0x14e)
      .maxstack  3
      .locals init (int32 V_0,
               int32 V_1,
               valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<uint32> V_2,
               class Program/'<<Main>$>d__0' V_3,
               valuetype [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_4,
               int32 V_5,
               bool V_6,
               class [System.Runtime]System.Exception V_7)
      IL_0000:  ldarg.0
      IL_0001:  ldfld      int32 Program/'<<Main>$>d__0'::'<>1__state'
      IL_0006:  stloc.0
      .try
      {
        IL_0007:  ldloc.0
        IL_0008:  brfalse.s  IL_000c

        IL_000a:  br.s       IL_000e

        IL_000c:  br.s       IL_0072

        IL_000e:  ldarg.0
        IL_000f:  ldc.i4.0
        IL_0010:  stfld      int32 Program/'<<Main>$>d__0'::'<i>5__1'
        IL_0015:  br         IL_0106

        IL_001a:  nop
        IL_001b:  ldarg.0
        IL_001c:  newobj     instance void [System.Runtime]System.Diagnostics.Stopwatch::.ctor()
        IL_0021:  stfld      class [System.Runtime]System.Diagnostics.Stopwatch Program/'<<Main>$>d__0'::'<sw>5__2'
        IL_0026:  ldarg.0
        IL_0027:  ldfld      class [System.Runtime]System.Diagnostics.Stopwatch Program/'<<Main>$>d__0'::'<sw>5__2'
        IL_002c:  callvirt   instance void [System.Runtime]System.Diagnostics.Stopwatch::Start()
        IL_0031:  nop
        IL_0032:  ldc.i4     0x5f5e100
        IL_0037:  call       class [System.Runtime]System.Threading.Tasks.Task`1<uint32> Program::'<<Main>$>g__A|0_0'(uint32)
        IL_003c:  callvirt   instance valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<!0> class [System.Runtime]System.Threading.Tasks.Task`1<uint32>::GetAwaiter()
        IL_0041:  stloc.2
        IL_0042:  ldloca.s   V_2
        IL_0044:  call       instance bool valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<uint32>::get_IsCompleted()
        IL_0049:  brtrue.s   IL_008e

        IL_004b:  ldarg.0
        IL_004c:  ldc.i4.0
        IL_004d:  dup
        IL_004e:  stloc.0
        IL_004f:  stfld      int32 Program/'<<Main>$>d__0'::'<>1__state'
        IL_0054:  ldarg.0
        IL_0055:  ldloc.2
        IL_0056:  stfld      valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<uint32> Program/'<<Main>$>d__0'::'<>u__1'
        IL_005b:  ldarg.0
        IL_005c:  stloc.3
        IL_005d:  ldarg.0
        IL_005e:  ldflda     valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> Program/'<<Main>$>d__0'::'<>t__builder'
        IL_0063:  ldloca.s   V_2
        IL_0065:  ldloca.s   V_3
        IL_0067:  call       instance void valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::AwaitUnsafeOnCompleted<valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<uint32>,class Program/'<<Main>$>d__0'>(!!0&,
                                                                                                                                                                                                                                                                            !!1&)
        IL_006c:  nop
        IL_006d:  leave      IL_014d

        IL_0072:  ldarg.0
        IL_0073:  ldfld      valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<uint32> Program/'<<Main>$>d__0'::'<>u__1'
        IL_0078:  stloc.2
        IL_0079:  ldarg.0
        IL_007a:  ldflda     valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<uint32> Program/'<<Main>$>d__0'::'<>u__1'
        IL_007f:  initobj    valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<uint32>
        IL_0085:  ldarg.0
        IL_0086:  ldc.i4.m1
        IL_0087:  dup
        IL_0088:  stloc.0
        IL_0089:  stfld      int32 Program/'<<Main>$>d__0'::'<>1__state'
        IL_008e:  ldarg.0
        IL_008f:  ldloca.s   V_2
        IL_0091:  call       instance !0 valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<uint32>::GetResult()
        IL_0096:  stfld      uint32 Program/'<<Main>$>d__0'::'<>s__4'
        IL_009b:  ldarg.0
        IL_009c:  ldarg.0
        IL_009d:  ldfld      uint32 Program/'<<Main>$>d__0'::'<>s__4'
        IL_00a2:  stfld      uint32 Program/'<<Main>$>d__0'::'<result>5__3'
        IL_00a7:  ldc.i4.s   11
        IL_00a9:  ldc.i4.2
        IL_00aa:  newobj     instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::.ctor(int32,
                                                                                                                                   int32)
        IL_00af:  stloc.s    V_4
        IL_00b1:  ldloca.s   V_4
        IL_00b3:  ldarg.0
        IL_00b4:  ldfld      class [System.Runtime]System.Diagnostics.Stopwatch Program/'<<Main>$>d__0'::'<sw>5__2'
        IL_00b9:  callvirt   instance int64 [System.Runtime]System.Diagnostics.Stopwatch::get_ElapsedMilliseconds()
        IL_00be:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int64>(!!0)
        IL_00c3:  nop
        IL_00c4:  ldloca.s   V_4
        IL_00c6:  ldstr      " ms result="
        IL_00cb:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
        IL_00d0:  nop
        IL_00d1:  ldloca.s   V_4
        IL_00d3:  ldarg.0
        IL_00d4:  ldfld      uint32 Program/'<<Main>$>d__0'::'<result>5__3'
        IL_00d9:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<uint32>(!!0)
        IL_00de:  nop
        IL_00df:  ldloca.s   V_4
        IL_00e1:  call       instance string [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::ToStringAndClear()
        IL_00e6:  call       void [System.Console]System.Console::WriteLine(string)
        IL_00eb:  nop
        IL_00ec:  nop
        IL_00ed:  ldarg.0
        IL_00ee:  ldnull
        IL_00ef:  stfld      class [System.Runtime]System.Diagnostics.Stopwatch Program/'<<Main>$>d__0'::'<sw>5__2'
        IL_00f4:  ldarg.0
        IL_00f5:  ldfld      int32 Program/'<<Main>$>d__0'::'<i>5__1'
        IL_00fa:  stloc.s    V_5
        IL_00fc:  ldarg.0
        IL_00fd:  ldloc.s    V_5
        IL_00ff:  ldc.i4.1
        IL_0100:  add
        IL_0101:  stfld      int32 Program/'<<Main>$>d__0'::'<i>5__1'
        IL_0106:  ldarg.0
        IL_0107:  ldfld      int32 Program/'<<Main>$>d__0'::'<i>5__1'
        IL_010c:  ldc.i4.s   10
        IL_010e:  clt
        IL_0110:  stloc.s    V_6
        IL_0112:  ldloc.s    V_6
        IL_0114:  brtrue     IL_001a

        IL_0119:  ldc.i4.s   100
        IL_011b:  stloc.1
        IL_011c:  leave.s    IL_0138

      }  // end .try
      catch [System.Runtime]System.Exception 
      {
        IL_011e:  stloc.s    V_7
        IL_0120:  ldarg.0
        IL_0121:  ldc.i4.s   -2
        IL_0123:  stfld      int32 Program/'<<Main>$>d__0'::'<>1__state'
        IL_0128:  ldarg.0
        IL_0129:  ldflda     valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> Program/'<<Main>$>d__0'::'<>t__builder'
        IL_012e:  ldloc.s    V_7
        IL_0130:  call       instance void valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::SetException(class [System.Runtime]System.Exception)
        IL_0135:  nop
        IL_0136:  leave.s    IL_014d

      }  // end handler
      IL_0138:  ldarg.0
      IL_0139:  ldc.i4.s   -2
      IL_013b:  stfld      int32 Program/'<<Main>$>d__0'::'<>1__state'
      IL_0140:  ldarg.0
      IL_0141:  ldflda     valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> Program/'<<Main>$>d__0'::'<>t__builder'
      IL_0146:  ldloc.1
      IL_0147:  call       instance void valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::SetResult(!0)
      IL_014c:  nop
      IL_014d:  ret
    } // end of method '<<Main>$>d__0'::MoveNext

    .method private hidebysig newslot virtual final 
            instance void  SetStateMachine(class [System.Runtime]System.Runtime.CompilerServices.IAsyncStateMachine stateMachine) cil managed
    {
      .custom instance void [System.Runtime]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
      .override [System.Runtime]System.Runtime.CompilerServices.IAsyncStateMachine::SetStateMachine
      // Code size       1 (0x1)
      .maxstack  8
      IL_0000:  ret
    } // end of method '<<Main>$>d__0'::SetStateMachine

  } // end of class '<<Main>$>d__0'

  .method private hidebysig static class [System.Runtime]System.Threading.Tasks.Task`1<int32> 
          '<Main>$'(string[] args) cil managed
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.AsyncStateMachineAttribute::.ctor(class [System.Runtime]System.Type) = ( 01 00 15 50 72 6F 67 72 61 6D 2B 3C 3C 4D 61 69   // ...Program+<<Mai
                                                                                                                                                   6E 3E 24 3E 64 5F 5F 30 00 00 )                   // n>$>d__0..
    .custom instance void [System.Runtime]System.Diagnostics.DebuggerStepThroughAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       57 (0x39)
    .maxstack  2
    .locals init (class Program/'<<Main>$>d__0' V_0)
    IL_0000:  newobj     instance void Program/'<<Main>$>d__0'::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  call       valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::Create()
    IL_000c:  stfld      valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> Program/'<<Main>$>d__0'::'<>t__builder'
    IL_0011:  ldloc.0
    IL_0012:  ldarg.0
    IL_0013:  stfld      string[] Program/'<<Main>$>d__0'::args
    IL_0018:  ldloc.0
    IL_0019:  ldc.i4.m1
    IL_001a:  stfld      int32 Program/'<<Main>$>d__0'::'<>1__state'
    IL_001f:  ldloc.0
    IL_0020:  ldflda     valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> Program/'<<Main>$>d__0'::'<>t__builder'
    IL_0025:  ldloca.s   V_0
    IL_0027:  call       instance void valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::Start<class Program/'<<Main>$>d__0'>(!!0&)
    IL_002c:  nop
    IL_002d:  ldloc.0
    IL_002e:  ldflda     valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> Program/'<<Main>$>d__0'::'<>t__builder'
    IL_0033:  call       instance class [System.Runtime]System.Threading.Tasks.Task`1<!0> valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::get_Task()
    IL_0038:  ret
  } // end of method Program::'<Main>$'

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Program::.ctor

  .method private hidebysig specialname static 
          int32  '<Main>'(string[] args) cil managed
  {
    .entrypoint
    .custom instance void [System.Runtime]System.Diagnostics.DebuggerStepThroughAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       20 (0x14)
    .maxstack  1
    .locals init (valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<int32> V_0)
    IL_0000:  ldarg.0
    IL_0001:  call       class [System.Runtime]System.Threading.Tasks.Task`1<int32> Program::'<Main>$'(string[])
    IL_0006:  callvirt   instance valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<!0> class [System.Runtime]System.Threading.Tasks.Task`1<int32>::GetAwaiter()
    IL_000b:  stloc.0
    IL_000c:  ldloca.s   V_0
    IL_000e:  call       instance !0 valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<int32>::GetResult()
    IL_0013:  ret
  } // end of method Program::'<Main>'

  .method assembly hidebysig static uint32 modopt([System.Runtime]System.Threading.Tasks.Task`1)
          '<<Main>$>g__A|0_0'(uint32 n) cil managed
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       32 (0x20)
    .maxstack  2
    .locals init (uint32 V_0,
             uint32 V_1,
             bool V_2,
             uint32 V_3)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  ldc.i4.0
    IL_0004:  stloc.1
    IL_0005:  br.s       IL_0012

    IL_0007:  ldloc.0
    IL_0008:  call       uint32 modopt([System.Runtime]System.Threading.Tasks.Task`1) Program::'<<Main>$>g__B|0_1'(uint32)
    IL_000d:  stloc.0
    IL_000e:  ldloc.1
    IL_000f:  ldc.i4.1
    IL_0010:  add
    IL_0011:  stloc.1
    IL_0012:  ldloc.1
    IL_0013:  ldarg.0
    IL_0014:  clt.un
    IL_0016:  stloc.2
    IL_0017:  ldloc.2
    IL_0018:  brtrue.s   IL_0007

    IL_001a:  ldloc.0
    IL_001b:  stloc.3
    IL_001c:  br.s       IL_001e

    IL_001e:  ldloc.3
    IL_001f:  ret
  } // end of method Program::'<<Main>$>g__A|0_0'

  .method assembly hidebysig static uint32 modopt([System.Runtime]System.Threading.Tasks.Task`1)
          '<<Main>$>g__B|0_1'(uint32 n) cil managed
  {
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  // Code size       89 (0x59)
  .maxstack  2
  .locals init (uint32 V_0,
           bool V_1,
           bool V_2,
           bool V_3,
           uint32 V_4,
           valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable V_5)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  ldc.i4     0x773593ed
  IL_0009:  mul
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  ldc.i4     0x3e8
  IL_0011:  clt.un
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  brfalse.s  IL_001d
  IL_0017:  call       valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable [System.Runtime]System.Threading.Tasks.Task::Yield()
            stloc 5
            ldloca 5
            call       instance valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable::GetAwaiter()
  IL_001c:  call       void [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::UnsafeAwaitAwaiterFromRuntimeAsync<valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter>(!!0)
  IL_001d:  ldloc.0
  IL_001e:  ldc.i4     0x773593ed
  IL_0023:  mul
  IL_0024:  stloc.0
  IL_0025:  ldloc.0
  IL_0026:  ldc.i4     0x3e8
  IL_002b:  clt.un
  IL_002d:  stloc.2
  IL_002e:  ldloc.2
  IL_002f:  brfalse.s  IL_0037
  IL_0031:  call       valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable [System.Runtime]System.Threading.Tasks.Task::Yield()
            stloc 5
            ldloca 5
            call       instance valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable::GetAwaiter()
  IL_0036:  call       void [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::UnsafeAwaitAwaiterFromRuntimeAsync<valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter>(!!0)
  IL_0037:  ldloc.0
  IL_0038:  ldc.i4     0x773593ed
  IL_003d:  mul
  IL_003e:  stloc.0
  IL_003f:  ldloc.0
  IL_0040:  ldc.i4     0x3e8
  IL_0045:  clt.un
  IL_0047:  stloc.3
  IL_0048:  ldloc.3
  IL_0049:  brfalse.s  IL_0051
  IL_004b:  call       valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable [System.Runtime]System.Threading.Tasks.Task::Yield()
            stloc 5
            ldloca 5
            call       instance valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable::GetAwaiter()
  IL_0050:  call       void [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::UnsafeAwaitAwaiterFromRuntimeAsync<valuetype [System.Runtime]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter>(!!0)
  IL_0051:  ldloc.0
  IL_0052:  stloc.s    V_4
  IL_0054:  br.s       IL_0056
  IL_0056:  ldloc.s    V_4
  IL_0058:  ret
  } // end of method Program::'<<Main>$>g__B|0_1'

} // end of class Program


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
