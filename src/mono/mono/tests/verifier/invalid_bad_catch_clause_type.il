.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly 'test-901'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module 'test-901.exe'
// MVID: {66ECA143-8B2A-47EB-A840-23ED00E2033E}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00620000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Application
       extends [mscorlib]System.Object
{
  .class auto ansi sealed nested private beforefieldinit '<foo>c__async0`1'<.ctor ([mscorlib]System.Exception, [mscorlib]System.Runtime.Serialization.ISerializable, [mscorlib]System.Runtime.InteropServices._Exception) TException>
         extends [mscorlib]System.ValueType
         implements [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field assembly valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder $builder
    .field assembly int32 $PC
    .method public hidebysig newslot virtual final 
            instance void  MoveNext() cil managed
    {
      // Code size       94 (0x5e)
      .maxstack  3
      .locals init (class [mscorlib]System.Exception V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldfld      int32 valuetype Application/'<foo>c__async0`1'<!TException>::$PC
      IL_0006:  ldarg.0
      IL_0007:  ldc.i4.m1
      IL_0008:  stfld      int32 valuetype Application/'<foo>c__async0`1'<!TException>::$PC
      IL_000d:  brtrue     IL_005d

      .try
      {
        IL_0012:  call       !!0 [mscorlib]System.Activator::CreateInstance<!TException>()
        IL_0017:  box        !TException
        IL_001c:  throw

      }  // end .try
      catch !!0
      {
        IL_001d:  pop
        IL_001e:  ldstr      "caught in correct block"
        IL_0023:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0028:  leave      IL_0052

      }  // end handler
      catch [mscorlib]System.Exception 
      {
        IL_002d:  stloc.0
        IL_002e:  ldstr      "caught in generic block"
        IL_0033:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0038:  ldstr      "type: "
        IL_003d:  ldloc.0
        IL_003e:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Exception::GetType()
        IL_0043:  call       string [mscorlib]System.String::Concat(object,
                                                                    object)
        IL_0048:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_004d:  leave      IL_0052

      }  // end handler
      IL_0052:  ldarg.0
      IL_0053:  ldflda     valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder valuetype Application/'<foo>c__async0`1'<!TException>::$builder
      IL_0058:  call       instance void [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::SetResult()
      IL_005d:  ret
    } // end of method '<foo>c__async0`1'::MoveNext

    .method public hidebysig newslot virtual final 
            instance void  SetStateMachine(class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine stateMachine) cil managed
    {
      .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
      // Code size       13 (0xd)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldflda     valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder valuetype Application/'<foo>c__async0`1'<!TException>::$builder
      IL_0006:  ldarg.1
      IL_0007:  call       instance void [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::SetStateMachine(class [mscorlib]System.Runtime.CompilerServices.IAsyncStateMachine)
      IL_000c:  ret
    } // end of method '<foo>c__async0`1'::SetStateMachine

  } // end of class '<foo>c__async0`1'

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Application::.ctor

  .method private hidebysig static class [mscorlib]System.Threading.Tasks.Task 
          foo<.ctor ([mscorlib]System.Exception) TException>() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.AsyncStateMachineAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 1C 41 70 70 6C 69 63 61 74 69 6F 6E 2B 3C   // ...Application+<
                                                                                                                                       66 6F 6F 3E 63 5F 5F 61 73 79 6E 63 30 60 31 00   // foo>c__async0`1.
                                                                                                                                       00 ) 
    .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       33 (0x21)
    .maxstack  3
    .locals init (valuetype Application/'<foo>c__async0`1'<!!TException> V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  call       valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::Create()
    IL_0007:  stfld      valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder valuetype Application/'<foo>c__async0`1'<!!TException>::$builder
    IL_000c:  ldloca.s   V_0
    IL_000e:  ldflda     valuetype [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder valuetype Application/'<foo>c__async0`1'<!!TException>::$builder
    IL_0013:  dup
    IL_0014:  ldloca.s   V_0
    IL_0016:  call       instance void [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::Start<valuetype Application/'<foo>c__async0`1'<!!0>>(!!0&)
    IL_001b:  call       instance class [mscorlib]System.Threading.Tasks.Task [mscorlib]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::get_Task()
    IL_0020:  ret
  } // end of method Application::foo

  .method private hidebysig static void  Main(string[] args) cil managed
  {
    .entrypoint
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  call       class [mscorlib]System.Threading.Tasks.Task Application::foo<class [mscorlib]System.InvalidOperationException>()
    IL_0005:  callvirt   instance void [mscorlib]System.Threading.Tasks.Task::Wait()
    IL_000a:  ret
  } // end of method Application::Main

} // end of class Application


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
