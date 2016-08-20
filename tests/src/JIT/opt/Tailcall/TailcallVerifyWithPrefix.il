// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



// Metadata version: v2.0.50727
.module extern kernel32

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern System.Linq
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                     
  .ver 4:0:0:0
}
.assembly TailcallVerifyWithPrefix
{
  .custom instance void [mscorlib]System.Reflection.AssemblyDescriptionAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.AssemblyConfigurationAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.AssemblyCompanyAttribute::.ctor(string) = ( 01 00 09 4D 69 63 72 6F 73 6F 66 74 00 00 )       // ...Microsoft..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .custom instance void [mscorlib]System.Reflection.AssemblyCopyrightAttribute::.ctor(string) = ( 01 00 1B 43 6F 70 79 72 69 67 68 74 20 C2 A9 20   // ...Copyright .. 
                                                                                                  4D 69 63 72 6F 73 6F 66 74 20 32 30 30 38 00 00 ) // Microsoft 2008..
  .custom instance void [mscorlib]System.Reflection.AssemblyTitleAttribute::.ctor(string) = ( 01 00 0E 54 61 69 6C 63 61 6C 6C 56 65 72 69 66   // ...TailcallVerif
                                                                                              79 00 00 )                                        // y..
  .custom instance void [mscorlib]System.Reflection.AssemblyTrademarkAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 66 36 39 30 66 65 61 66 2D 38 65 36 34   // ..$f690feaf-8e64
                                                                                                  2D 34 33 34 31 2D 39 38 32 66 2D 35 32 39 62 65   // -4341-982f-529be
                                                                                                  32 35 63 64 30 64 34 00 00 )                      // 25cd0d4..

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 02 00 00 00 00 00 ) 

  .custom instance void [mscorlib]System.Reflection.AssemblyProductAttribute::.ctor(string) = ( 01 00 0E 54 61 69 6C 63 61 6C 6C 56 65 72 69 66   // ...TailcallVerif
                                                                                                79 00 00 )                                        // y..
  .custom instance void [mscorlib]System.Reflection.AssemblyFileVersionAttribute::.ctor(string) = ( 01 00 07 31 2E 30 2E 30 2E 30 00 00 )             // ...1.0.0.0..
    .hash algorithm 0x00008004
  .ver 1:0:0:0
}
.module TailcallVerifyWithPrefix.exe
// MVID: {D1242658-CA16-4D11-A740-6635F112F4B5}
.custom instance void [mscorlib]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00F50000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit TailcallVerify.Condition18
       extends [mscorlib]System.Object
{
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .field private static int32 Result
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       154 (0x9a)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition18.Test1 - Caller(declarative s"
    + "ecurity): Arguments: None - ReturnType: 3 byte struct; Callee: Argument"
    + "s: None - ReturnType: 3 byte struct"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition18::Result
    .try
    {
      IL_0011:  call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition18::Caller1()
      IL_0016:  pop
      IL_0017:  leave.s    IL_0070

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0019:  stloc.0
      IL_001a:  ldloc.0
      IL_001b:  isinst     [mscorlib]System.DivideByZeroException
      IL_0020:  brtrue.s   IL_002b

      IL_0022:  ldc.i4.s   101
      IL_0024:  stsfld     int32 TailcallVerify.Condition18::Result
      IL_0029:  rethrow
      IL_002b:  ldloc.0
      IL_002c:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0031:  ldstr      "Caller"
      IL_0036:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003b:  ldc.i4.m1
      IL_003c:  beq.s   IL_006e

      IL_003e:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0043:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0048:  ldstr      "------------------------------------------------"
      IL_004d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0052:  call       void [System.Console]System.Console::WriteLine()
      IL_0057:  ldloc.0
      IL_0058:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0062:  call       void [System.Console]System.Console::WriteLine()
      IL_0067:  ldc.i4.s   101
      IL_0069:  stsfld     int32 TailcallVerify.Condition18::Result
      IL_006e:  leave.s    IL_0070

    }  // end handler
    IL_0070:  ldstr      "Execution finished - Test "
    IL_0075:  ldsfld     int32 TailcallVerify.Condition18::Result
    IL_007a:  ldc.i4.s   100
    IL_007c:  beq.s      IL_0085

    IL_007e:  ldstr      "FAILED"
    IL_0083:  br.s       IL_008a

    IL_0085:  ldstr      "PASSED"
    IL_008a:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0094:  ldsfld     int32 TailcallVerify.Condition18::Result
    IL_0099:  ret
  } // end of method Condition18::Test1

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          Caller1() cil managed
  {
    // Code size       52 (0x34)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition18::Result
    IL_002e:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition18::Callee1()
    IL_0033:  ret
  } // end of method Condition18::Caller1

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          Callee1() cil managed noinlining
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes v)
    IL_0000:  ldloca.s   v
    IL_0002:  initobj    TailcallVerify.ValueType3Bytes
    IL_0008:  ldloca.s   v
    IL_000a:  ldc.i4.1
    IL_000b:  volatile.
    IL_000d:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition18::zero
    IL_0012:  div
    IL_0013:  conv.i2
    IL_0014:  stfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method Condition18::Callee1

  .method public hidebysig static int32  Test2() cil managed nooptimization
  {
    // Code size       154 (0x9a)
    .maxstack  3
    .locals init ([0] class [System.Console]System.Exception e)
    IL_0000:  ldstr      "Executing Condition18.Test2 - Caller(imperative se"
    + "curity): Arguments: None - ReturnType: 3 byte struct; Callee: Arguments"
    + ": None - ReturnType: 3 byte struct"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition18::Result
    .try
    {
      IL_0011:  call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition18::Caller2()
      IL_0016:  pop
      IL_0017:  leave.s    IL_0070

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0019:  stloc.0
      IL_001a:  ldloc.0
      IL_001b:  isinst     [mscorlib]System.DivideByZeroException
      IL_0020:  brtrue.s   IL_002b

      IL_0022:  ldc.i4.s   101
      IL_0024:  stsfld     int32 TailcallVerify.Condition18::Result
      IL_0029:  rethrow
      IL_002b:  ldloc.0
      IL_002c:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0031:  ldstr      "Caller"
      IL_0036:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003b:  ldc.i4.m1
      IL_003c:  bne.un.s   IL_006e

      IL_003e:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0043:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0048:  ldstr      "------------------------------------------------"
      IL_004d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0052:  call       void [System.Console]System.Console::WriteLine()
      IL_0057:  ldloc.0
      IL_0058:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0062:  call       void [System.Console]System.Console::WriteLine()
      IL_0067:  ldc.i4.s   101
      IL_0069:  stsfld     int32 TailcallVerify.Condition18::Result
      IL_006e:  leave.s    IL_0070

    }  // end handler
    IL_0070:  ldstr      "Execution finished - Test "
    IL_0075:  ldsfld     int32 TailcallVerify.Condition18::Result
    IL_007a:  ldc.i4.s   100
    IL_007c:  beq.s      IL_0085

    IL_007e:  ldstr      "FAILED"
    IL_0083:  br.s       IL_008a

    IL_0085:  ldstr      "PASSED"
    IL_008a:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0094:  ldsfld     int32 TailcallVerify.Condition18::Result
    IL_0099:  ret
  } // end of method Condition18::Test2

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          Caller2() cil managed
  {
    // Code size       65 (0x41)
    .maxstack  2
    .locals init ([0] class [mscorlib]System.Security.Permissions.FileIOPermission io)
    IL_0000:  ldc.i4.1
    IL_0001:  newobj     instance void [mscorlib]System.Security.Permissions.FileIOPermission::.ctor(valuetype [mscorlib]System.Security.Permissions.PermissionState)
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  callvirt   instance void [mscorlib]System.Security.CodeAccessPermission::Demand()
    IL_000d:  ldc.i4.0
    IL_000e:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0013:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_0018:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_001d:  ldstr      "Caller"
    IL_0022:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_0027:  ldc.i4.m1
    IL_0028:  bne.un.s   IL_003b

    IL_002a:  ldstr      "Failed, Method was inlined..."
    IL_002f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0034:  ldc.i4.s   101
    IL_0036:  stsfld     int32 TailcallVerify.Condition18::Result
    IL_003b:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition18::Callee2()
    IL_0040:  ret
  } // end of method Condition18::Caller2

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          Callee2() cil managed noinlining
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes v)
    IL_0000:  ldloca.s   v
    IL_0002:  initobj    TailcallVerify.ValueType3Bytes
    IL_0008:  ldloca.s   v
    IL_000a:  ldc.i4.1
    IL_000b:  volatile.
    IL_000d:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition18::zero
    IL_0012:  div
    IL_0013:  conv.i2
    IL_0014:  stfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method Condition18::Callee2

  .method public hidebysig static int32  Test3() cil managed nooptimization
  {
    // Code size       154 (0x9a)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition18.Test3 - Caller(imperative se"
    + "curity): Arguments: None - ReturnType: 3 byte struct; Callee: Arguments"
    + ": None - ReturnType: 3 byte struct"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition18::Result
    .try
    {
      IL_0011:  call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition18::Caller3()
      IL_0016:  pop
      IL_0017:  leave.s    IL_0070

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0019:  stloc.0
      IL_001a:  ldloc.0
      IL_001b:  isinst     [mscorlib]System.DivideByZeroException
      IL_0020:  brtrue.s   IL_002b

      IL_0022:  ldc.i4.s   101
      IL_0024:  stsfld     int32 TailcallVerify.Condition18::Result
      IL_0029:  rethrow
      IL_002b:  ldloc.0
      IL_002c:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0031:  ldstr      "Caller"
      IL_0036:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003b:  ldc.i4.m1
      IL_003c:  bne.un.s   IL_006e

      IL_003e:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0043:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0048:  ldstr      "------------------------------------------------"
      IL_004d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0052:  call       void [System.Console]System.Console::WriteLine()
      IL_0057:  ldloc.0
      IL_0058:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0062:  call       void [System.Console]System.Console::WriteLine()
      IL_0067:  ldc.i4.s   101
      IL_0069:  stsfld     int32 TailcallVerify.Condition18::Result
      IL_006e:  leave.s    IL_0070

    }  // end handler
    IL_0070:  ldstr      "Execution finished - Test "
    IL_0075:  ldsfld     int32 TailcallVerify.Condition18::Result
    IL_007a:  ldc.i4.s   100
    IL_007c:  beq.s      IL_0085

    IL_007e:  ldstr      "FAILED"
    IL_0083:  br.s       IL_008a

    IL_0085:  ldstr      "PASSED"
    IL_008a:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0094:  ldsfld     int32 TailcallVerify.Condition18::Result
    IL_0099:  ret
  } // end of method Condition18::Test3

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          Caller3() cil managed
  {
    // Code size       65 (0x41)
    .maxstack  2
    .locals init ([0] class [mscorlib]System.Security.Permissions.FileIOPermission io)
    IL_0000:  ldc.i4.1
    IL_0001:  newobj     instance void [mscorlib]System.Security.Permissions.FileIOPermission::.ctor(valuetype [mscorlib]System.Security.Permissions.PermissionState)
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  callvirt   instance void [mscorlib]System.Security.CodeAccessPermission::Demand()
    IL_000d:  ldc.i4.0
    IL_000e:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0013:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_0018:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_001d:  ldstr      "Caller"
    IL_0022:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_0027:  ldc.i4.m1
    IL_0028:  bne.un.s   IL_003b

    IL_002a:  ldstr      "Failed, Method was inlined..."
    IL_002f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0034:  ldc.i4.s   101
    IL_0036:  stsfld     int32 TailcallVerify.Condition18::Result
    IL_003b:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition18::Callee3()
    IL_0040:  ret
  } // end of method Condition18::Caller3

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          Callee3() cil managed noinlining
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes v)
    IL_0000:  ldloca.s   v
    IL_0002:  initobj    TailcallVerify.ValueType3Bytes
    IL_0008:  ldloca.s   v
    IL_000a:  ldc.i4.1
    IL_000b:  volatile.
    IL_000d:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition18::zero
    IL_0012:  div
    IL_0013:  conv.i2
    IL_0014:  stfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method Condition18::Callee3

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Condition18::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  volatile.
    IL_0003:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition18::zero
    IL_0008:  ldc.i4.s   100
    IL_000a:  stsfld     int32 TailcallVerify.Condition18::Result
    IL_000f:  ret
  } // end of method Condition18::.cctor

} // end of class TailcallVerify.Condition18

.class private sequential ansi sealed beforefieldinit TailcallVerify.Unsafe
       extends [mscorlib]System.ValueType
{
  .class sequential ansi sealed nested public beforefieldinit '<data>e__FixedBuffer0'
         extends [mscorlib]System.ValueType
  {
    .pack 0
    .size 3
    .custom instance void [mscorlib]System.Runtime.CompilerServices.UnsafeValueTypeAttribute::.ctor() = ( 01 00 00 00 ) 
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public uint8 FixedElementField
  } // end of class '<data>e__FixedBuffer0'

  .field public valuetype TailcallVerify.Unsafe/'<data>e__FixedBuffer0' data
  .custom instance void [mscorlib]System.Runtime.CompilerServices.FixedBufferAttribute::.ctor(class [mscorlib]System.Type,
                                                                                              int32) = ( 01 00 58 53 79 73 74 65 6D 2E 42 79 74 65 2C 20   // ..XSystem.Byte, 
                                                                                                         6D 73 63 6F 72 6C 69 62 2C 20 56 65 72 73 69 6F   // mscorlib, Versio
                                                                                                         6E 3D 32 2E 30 2E 30 2E 30 2C 20 43 75 6C 74 75   // n=2.0.0.0, Cultu
                                                                                                         72 65 3D 6E 65 75 74 72 61 6C 2C 20 50 75 62 6C   // re=neutral, Publ
                                                                                                         69 63 4B 65 79 54 6F 6B 65 6E 3D 62 37 37 61 35   // icKeyToken=b77a5
                                                                                                         63 35 36 31 39 33 34 65 30 38 39 03 00 00 00 00   // c561934e089.....
                                                                                                         00 ) 
} // end of class TailcallVerify.Unsafe

.class private auto ansi beforefieldinit TailcallVerify.Condition17
       extends [mscorlib]System.Object
{
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .field private static int32 Result
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       162 (0xa2)
    .maxstack  3
    .locals init ([0] valuetype TailcallVerify.Unsafe u,
             [1] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition17.Test1 - Caller: Arguments: N"
    + "one - ReturnType: 3 byte struct; Callee: Arguments: None - ReturnType: "
    + "void"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition17::Result
    .try
    {
      IL_0011:  ldloca.s   u
      IL_0013:  initobj    TailcallVerify.Unsafe
      IL_0019:  ldloc.0
      IL_001a:  call       void TailcallVerify.Condition17::Caller1(valuetype TailcallVerify.Unsafe)
      IL_001f:  leave.s    IL_0078

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0021:  stloc.1
      IL_0022:  ldloc.1
      IL_0023:  isinst     [mscorlib]System.DivideByZeroException
      IL_0028:  brtrue.s   IL_0033

      IL_002a:  ldc.i4.s   101
      IL_002c:  stsfld     int32 TailcallVerify.Condition17::Result
      IL_0031:  rethrow
      IL_0033:  ldloc.1
      IL_0034:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0039:  ldstr      "Caller"
      IL_003e:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_0043:  ldc.i4.m1
      IL_0044:  bne.un.s   IL_0076

      IL_0046:  ldstr      "FAILED: DID NOT find the word 'Caller' in the stac"
      + "ktrace."
      IL_004b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0050:  ldstr      "------------------------------------------------"
      IL_0055:  call       void [System.Console]System.Console::WriteLine(string)
      IL_005a:  call       void [System.Console]System.Console::WriteLine()
      IL_005f:  ldloc.1
      IL_0060:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0065:  call       void [System.Console]System.Console::WriteLine(string)
      IL_006a:  call       void [System.Console]System.Console::WriteLine()
      IL_006f:  ldc.i4.s   101
      IL_0071:  stsfld     int32 TailcallVerify.Condition17::Result
      IL_0076:  leave.s    IL_0078

    }  // end handler
    IL_0078:  ldstr      "Execution finished - Test "
    IL_007d:  ldsfld     int32 TailcallVerify.Condition17::Result
    IL_0082:  ldc.i4.s   100
    IL_0084:  beq.s      IL_008d

    IL_0086:  ldstr      "FAILED"
    IL_008b:  br.s       IL_0092

    IL_008d:  ldstr      "PASSED"
    IL_0092:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0097:  call       void [System.Console]System.Console::WriteLine(string)
    IL_009c:  ldsfld     int32 TailcallVerify.Condition17::Result
    IL_00a1:  ret
  } // end of method Condition17::Test1

  .method private hidebysig static void  Caller1(valuetype TailcallVerify.Unsafe u) cil managed
  {
    // Code size       74 (0x4a)
    .maxstack  2
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition17::Result
    IL_002e:  ldarga.s   u
    IL_0030:  ldflda     valuetype TailcallVerify.Unsafe/'<data>e__FixedBuffer0' TailcallVerify.Unsafe::data
    IL_0035:  ldflda     uint8 TailcallVerify.Unsafe/'<data>e__FixedBuffer0'::FixedElementField
    IL_003a:  conv.u
    IL_003b:  volatile.
    IL_003d:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition17::zero
    IL_0042:  conv.u1
    IL_0043:  stind.i1
    IL_0044:  tail. call       void TailcallVerify.Condition17::Callee1()
    IL_0049:  ret
  } // end of method Condition17::Caller1

  .method private hidebysig static void  Callee1() cil managed noinlining
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition17::zero
    IL_0008:  div
    IL_0009:  pop
    IL_000a:  ret
  } // end of method Condition17::Callee1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Condition17::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  volatile.
    IL_0003:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition17::zero
    IL_0008:  ldc.i4.s   100
    IL_000a:  stsfld     int32 TailcallVerify.Condition17::Result
    IL_000f:  ret
  } // end of method Condition17::.cctor

} // end of class TailcallVerify.Condition17

.class private auto ansi beforefieldinit TailcallVerify.Condition16
       extends [mscorlib]System.Object
{
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .field private static int32 Result
  .field public object syncObj
  .field private static class [mscorlib]System.Threading.ParameterizedThreadStart 'CS$<>9__CachedAnonymousMethodDelegate1'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .field private static class [mscorlib]System.Threading.ParameterizedThreadStart 'CS$<>9__CachedAnonymousMethodDelegate3'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       211 (0xd3)
    .maxstack  3
    .locals init ([0] class TailcallVerify.Condition16 con,
             [1] class [mscorlib]System.Exception e,
             [2] class [mscorlib]System.Threading.Thread t)
    IL_0000:  ldstr      "Executing Condition16.Test1 - Caller: Arguments: N"
    + "one - ReturnType: void; Callee: Arguments: None - ReturnType: void"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition16::Result
    IL_0011:  newobj     instance void TailcallVerify.Condition16::.ctor()
    IL_0016:  stloc.0
    .try
    {
      IL_0017:  ldloc.0
      IL_0018:  callvirt   instance void TailcallVerify.Condition16::Caller1()
      IL_001d:  leave      IL_00a9

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0022:  stloc.1
      IL_0023:  ldloc.1
      IL_0024:  isinst     [mscorlib]System.DivideByZeroException
      IL_0029:  brtrue.s   IL_0034

      IL_002b:  ldc.i4.s   101
      IL_002d:  stsfld     int32 TailcallVerify.Condition16::Result
      IL_0032:  rethrow
      IL_0034:  ldloc.1
      IL_0035:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_003a:  ldstr      "Caller"
      IL_003f:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_0044:  ldc.i4.m1
      IL_0045:  bne.un.s   IL_0077

      IL_0047:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0051:  ldstr      "------------------------------------------------"
      IL_0056:  call       void [System.Console]System.Console::WriteLine(string)
      IL_005b:  call       void [System.Console]System.Console::WriteLine()
      IL_0060:  ldloc.1
      IL_0061:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0066:  call       void [System.Console]System.Console::WriteLine(string)
      IL_006b:  call       void [System.Console]System.Console::WriteLine()
      IL_0070:  ldc.i4.s   101
      IL_0072:  stsfld     int32 TailcallVerify.Condition16::Result
      IL_0077:  ldsfld     class [mscorlib]System.Threading.ParameterizedThreadStart TailcallVerify.Condition16::'CS$<>9__CachedAnonymousMethodDelegate1'
      IL_007c:  brtrue.s   IL_008f

      IL_007e:  ldnull
      IL_007f:  ldftn      void TailcallVerify.Condition16::'<Test1>b__0'(object)
      IL_0085:  newobj     instance void [mscorlib]System.Threading.ParameterizedThreadStart::.ctor(object,
                                                                                                    native int)
      IL_008a:  stsfld     class [mscorlib]System.Threading.ParameterizedThreadStart TailcallVerify.Condition16::'CS$<>9__CachedAnonymousMethodDelegate1'
      IL_008f:  ldsfld     class [mscorlib]System.Threading.ParameterizedThreadStart TailcallVerify.Condition16::'CS$<>9__CachedAnonymousMethodDelegate1'
      IL_0094:  newobj     instance void [mscorlib]System.Threading.Thread::.ctor(class [mscorlib]System.Threading.ParameterizedThreadStart)
      IL_0099:  stloc.2
      IL_009a:  ldloc.2
      IL_009b:  ldloc.0
      IL_009c:  callvirt   instance void [mscorlib]System.Threading.Thread::Start(object)
      IL_00a1:  ldloc.2
      IL_00a2:  callvirt   instance void [mscorlib]System.Threading.Thread::Join()
      IL_00a7:  leave.s    IL_00a9

    }  // end handler
    IL_00a9:  ldstr      "Execution finished - Test "
    IL_00ae:  ldsfld     int32 TailcallVerify.Condition16::Result
    IL_00b3:  ldc.i4.s   100
    IL_00b5:  beq.s      IL_00be

    IL_00b7:  ldstr      "FAILED"
    IL_00bc:  br.s       IL_00c3

    IL_00be:  ldstr      "PASSED"
    IL_00c3:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_00c8:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00cd:  ldsfld     int32 TailcallVerify.Condition16::Result
    IL_00d2:  ret
  } // end of method Condition16::Test1

  .method private hidebysig instance void 
          Caller1() cil managed synchronized
  {
    // Code size       53 (0x35)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition16::Result
    IL_002e:  ldarg.0
    IL_002f:  tail. call       instance void TailcallVerify.Condition16::Callee1()
    IL_0034:  ret
  } // end of method Condition16::Caller1

  .method private hidebysig instance void 
          Callee1() cil managed noinlining
  {
    // Code size       59 (0x3b)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Threading.Thread t)
    IL_0000:  ldsfld     class [mscorlib]System.Threading.ParameterizedThreadStart TailcallVerify.Condition16::'CS$<>9__CachedAnonymousMethodDelegate3'
    IL_0005:  brtrue.s   IL_0018

    IL_0007:  ldnull
    IL_0008:  ldftn      void TailcallVerify.Condition16::'<Callee1>b__2'(object)
    IL_000e:  newobj     instance void [mscorlib]System.Threading.ParameterizedThreadStart::.ctor(object,
                                                                                                  native int)
    IL_0013:  stsfld     class [mscorlib]System.Threading.ParameterizedThreadStart TailcallVerify.Condition16::'CS$<>9__CachedAnonymousMethodDelegate3'
    IL_0018:  ldsfld     class [mscorlib]System.Threading.ParameterizedThreadStart TailcallVerify.Condition16::'CS$<>9__CachedAnonymousMethodDelegate3'
    IL_001d:  newobj     instance void [mscorlib]System.Threading.Thread::.ctor(class [mscorlib]System.Threading.ParameterizedThreadStart)
    IL_0022:  stloc.0
    IL_0023:  ldloc.0
    IL_0024:  ldarg.0
    IL_0025:  callvirt   instance void [mscorlib]System.Threading.Thread::Start(object)
    IL_002a:  ldloc.0
    IL_002b:  callvirt   instance void [mscorlib]System.Threading.Thread::Join()
    IL_0030:  ldc.i4.1
    IL_0031:  volatile.
    IL_0033:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition16::zero
    IL_0038:  div
    IL_0039:  pop
    IL_003a:  ret
  } // end of method Condition16::Callee1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_0006:  stfld      object TailcallVerify.Condition16::syncObj
    IL_000b:  ldarg.0
    IL_000c:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0011:  ret
  } // end of method Condition16::.ctor

  .method private hidebysig static void  '<Test1>b__0'(object o) cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       48 (0x30)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4     0x1f4
    IL_0006:  call       bool [mscorlib]System.Threading.Monitor::TryEnter(object,
                                                                           int32)
    IL_000b:  brfalse.s  IL_001e

    IL_000d:  ldstr      "GOOD: got the lock again..."
    IL_0012:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0017:  ldarg.0
    IL_0018:  call       void [mscorlib]System.Threading.Monitor::Exit(object)
    IL_001d:  ret

    IL_001e:  ldstr      "BAD: Failed to get the lock"
    IL_0023:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0028:  ldc.i4.s   101
    IL_002a:  stsfld     int32 TailcallVerify.Condition16::Result
    IL_002f:  ret
  } // end of method Condition16::'<Test1>b__0'

  .method private hidebysig static void  '<Callee1>b__2'(object o) cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       42 (0x2a)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4     0x1f4
    IL_0006:  call       bool [mscorlib]System.Threading.Monitor::TryEnter(object,
                                                                           int32)
    IL_000b:  brfalse.s  IL_001f

    IL_000d:  ldstr      "BAD: got the lock again..."
    IL_0012:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0017:  ldc.i4.s   101
    IL_0019:  stsfld     int32 TailcallVerify.Condition16::Result
    IL_001e:  ret

    IL_001f:  ldstr      "GOOD: Failed to get the lock"
    IL_0024:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0029:  ret
  } // end of method Condition16::'<Callee1>b__2'

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  volatile.
    IL_0003:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition16::zero
    IL_0008:  ldc.i4.s   100
    IL_000a:  stsfld     int32 TailcallVerify.Condition16::Result
    IL_000f:  ret
  } // end of method Condition16::.cctor

} // end of class TailcallVerify.Condition16

.class private auto ansi beforefieldinit TailcallVerify.Condition13
       extends [mscorlib]System.Object
{
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .field private static int32 Result
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       154 (0x9a)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition13.Test1 - Caller: Arguments: N"
    + "one - ReturnType: 3 byte struct; Callee: Arguments: None - ReturnType: "
    + "3 byte struct"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition13::Result
    .try
    {
      IL_0011:  call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition13::Caller1()
      IL_0016:  pop
      IL_0017:  leave.s    IL_0070

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0019:  stloc.0
      IL_001a:  ldloc.0
      IL_001b:  isinst     [mscorlib]System.DivideByZeroException
      IL_0020:  brtrue.s   IL_002b

      IL_0022:  ldc.i4.s   101
      IL_0024:  stsfld     int32 TailcallVerify.Condition13::Result
      IL_0029:  rethrow
      IL_002b:  ldloc.0
      IL_002c:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0031:  ldstr      "Caller"
      IL_0036:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003b:  ldc.i4.m1
      IL_003c:  beq.s      IL_006e

      IL_003e:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0043:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0048:  ldstr      "------------------------------------------------"
      IL_004d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0052:  call       void [System.Console]System.Console::WriteLine()
      IL_0057:  ldloc.0
      IL_0058:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0062:  call       void [System.Console]System.Console::WriteLine()
      IL_0067:  ldc.i4.s   101
      IL_0069:  stsfld     int32 TailcallVerify.Condition13::Result
      IL_006e:  leave.s    IL_0070

    }  // end handler
    IL_0070:  ldstr      "Execution finished - Test "
    IL_0075:  ldsfld     int32 TailcallVerify.Condition13::Result
    IL_007a:  ldc.i4.s   100
    IL_007c:  beq.s      IL_0085

    IL_007e:  ldstr      "FAILED"
    IL_0083:  br.s       IL_008a

    IL_0085:  ldstr      "PASSED"
    IL_008a:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0094:  ldsfld     int32 TailcallVerify.Condition13::Result
    IL_0099:  ret
  } // end of method Condition13::Test1

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          Caller1() cil managed nooptimization
  {
    // Code size       52 (0x34)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition13::Result
    IL_002e:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition13::Callee1()
    IL_0033:  ret
  } // end of method Condition13::Caller1

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          Callee1() cil managed noinlining
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes v)
    IL_0000:  ldloca.s   v
    IL_0002:  initobj    TailcallVerify.ValueType3Bytes
    IL_0008:  ldloca.s   v
    IL_000a:  ldc.i4.1
    IL_000b:  volatile.
    IL_000d:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition13::zero
    IL_0012:  div
    IL_0013:  conv.i2
    IL_0014:  stfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method Condition13::Callee1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Condition13::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  volatile.
    IL_0003:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition13::zero
    IL_0008:  ldc.i4.s   100
    IL_000a:  stsfld     int32 TailcallVerify.Condition13::Result
    IL_000f:  ret
  } // end of method Condition13::.cctor

} // end of class TailcallVerify.Condition13

.class private auto ansi beforefieldinit TailcallVerify.Condition12
       extends [mscorlib]System.Object
{
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .field private static int32 Result
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       142 (0x8e)
    .maxstack  6
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition12.Test1 - Caller: Arguments: i"
    + "nt,int,int - ReturnType: void; Callee: Arguments: string,string,string "
    + "- ReturnType: void"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition12::Result
    .try
    {
      IL_0011:  ldc.i4.1
      IL_0012:  ldc.i4.2
      IL_0013:  ldc.i4.3
      IL_0014:  ldc.i4.4
      IL_0015:  ldc.i4.5
      IL_0016:  ldc.i4.6
      IL_0017:  call       void TailcallVerify.Condition12::Caller1(int32,
                                                                    int32,
                                                                    int32,
                                                                    int32,
                                                                    int32,
                                                                    int32)
      IL_001c:  leave.s    IL_0064

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_001e:  stloc.0
      IL_001f:  ldloc.0
      IL_0020:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0025:  ldstr      "Caller"
      IL_002a:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_002f:  ldc.i4.m1
      IL_0030:  beq.s      IL_0062

      IL_0032:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0037:  call       void [System.Console]System.Console::WriteLine(string)
      IL_003c:  ldstr      "------------------------------------------------"
      IL_0041:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0046:  call       void [System.Console]System.Console::WriteLine()
      IL_004b:  ldloc.0
      IL_004c:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0051:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0056:  call       void [System.Console]System.Console::WriteLine()
      IL_005b:  ldc.i4.s   101
      IL_005d:  stsfld     int32 TailcallVerify.Condition12::Result
      IL_0062:  leave.s    IL_0064

    }  // end handler
    IL_0064:  ldstr      "Execution finished - Test "
    IL_0069:  ldsfld     int32 TailcallVerify.Condition12::Result
    IL_006e:  ldc.i4.s   100
    IL_0070:  beq.s      IL_0079

    IL_0072:  ldstr      "FAILED"
    IL_0077:  br.s       IL_007e

    IL_0079:  ldstr      "PASSED"
    IL_007e:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0083:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0088:  ldsfld     int32 TailcallVerify.Condition12::Result
    IL_008d:  ret
  } // end of method Condition12::Test1

  .method private hidebysig static void  Caller1(int32 i1,
                                                 int32 i2,
                                                 int32 i3,
                                                 int32 i4,
                                                 int32 i5,
                                                 int32 i6) cil managed
  {
    // Code size       82 (0x52)
    .maxstack  6
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition12::Result
    IL_002e:  ldstr      "1"
    IL_0033:  ldstr      "2"
    IL_0038:  ldstr      "3"
    IL_003d:  ldstr      "2"
    IL_0042:  ldstr      "3"
    IL_0047:  ldstr      "2"
    IL_004c:  tail. call       void TailcallVerify.Condition12::Callee1(string,
                                                                  string,
                                                                  string,
                                                                  string,
                                                                  string,
                                                                  string)
    IL_0051:  ret
  } // end of method Condition12::Caller1

  .method private hidebysig static void  Callee1(string s1,
                                                 string s2,
                                                 string s3,
                                                 string s4,
                                                 string s5,
                                                 string s6) cil managed noinlining
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldarg.2
    IL_0001:  callvirt   instance int32 [mscorlib]System.String::get_Length()
    IL_0006:  volatile.
    IL_0008:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition12::zero
    IL_000d:  div
    IL_000e:  pop
    IL_000f:  ret
  } // end of method Condition12::Callee1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Condition12::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  volatile.
    IL_0003:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition12::zero
    IL_0008:  ldc.i4.s   100
    IL_000a:  stsfld     int32 TailcallVerify.Condition12::Result
    IL_000f:  ret
  } // end of method Condition12::.cctor

} // end of class TailcallVerify.Condition12

.class interface private abstract auto ansi TailcallVerify.ICondition9
{
  .method public hidebysig newslot abstract virtual 
          instance void  Callee1() cil managed
  {
  } // end of method ICondition9::Callee1

} // end of class TailcallVerify.ICondition9

.class private auto ansi beforefieldinit TailcallVerify.Condition9
       extends [mscorlib]System.Object
       implements TailcallVerify.ICondition9
{
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .field private static int32 Result
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       153 (0x99)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition9.Test1 - Caller: Arguments: No"
    + "ne - ReturnType: void; Callee(Interface Member): Arguments: None - Retu"
    + "rnType: void"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition9::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition9::Caller1()
      IL_0016:  leave.s    IL_006f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  isinst     [mscorlib]System.DivideByZeroException
      IL_001f:  brtrue.s   IL_002a

      IL_0021:  ldc.i4.s   101
      IL_0023:  stsfld     int32 TailcallVerify.Condition9::Result
      IL_0028:  rethrow
      IL_002a:  ldloc.0
      IL_002b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0030:  ldstr      "Caller"
      IL_0035:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003a:  ldc.i4.m1
      IL_003b:  beq.s      IL_006d

      IL_003d:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0042:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0047:  ldstr      "------------------------------------------------"
      IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0051:  call       void [System.Console]System.Console::WriteLine()
      IL_0056:  ldloc.0
      IL_0057:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0061:  call       void [System.Console]System.Console::WriteLine()
      IL_0066:  ldc.i4.s   101
      IL_0068:  stsfld     int32 TailcallVerify.Condition9::Result
      IL_006d:  leave.s    IL_006f

    }  // end handler
    IL_006f:  ldstr      "Execution finished - Test "
    IL_0074:  ldsfld     int32 TailcallVerify.Condition9::Result
    IL_0079:  ldc.i4.s   100
    IL_007b:  beq.s      IL_0084

    IL_007d:  ldstr      "FAILED"
    IL_0082:  br.s       IL_0089

    IL_0084:  ldstr      "PASSED"
    IL_0089:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0093:  ldsfld     int32 TailcallVerify.Condition9::Result
    IL_0098:  ret
  } // end of method Condition9::Test1

  .method private hidebysig static void  Caller1() cil managed
  {
    // Code size       59 (0x3b)
    .maxstack  2
    .locals init ([0] class TailcallVerify.ICondition9 con)
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition9::Result
    IL_002e:  newobj     instance void TailcallVerify.Condition9::.ctor()
    IL_0033:  stloc.0
    IL_0034:  ldloc.0
    IL_0035:  tail. callvirt   instance void TailcallVerify.ICondition9::Callee1()
    IL_003a:  ret
  } // end of method Condition9::Caller1

  .method public hidebysig newslot virtual final 
          instance void  Callee1() cil managed noinlining
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition9::zero
    IL_0008:  div
    IL_0009:  pop
    IL_000a:  ret
  } // end of method Condition9::Callee1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Condition9::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  volatile.
    IL_0003:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition9::zero
    IL_0008:  ldc.i4.s   100
    IL_000a:  stsfld     int32 TailcallVerify.Condition9::Result
    IL_000f:  ret
  } // end of method Condition9::.cctor

} // end of class TailcallVerify.Condition9

.class private auto ansi beforefieldinit TailcallVerify.Condition11
       extends [mscorlib]System.Object
{
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .field private static int32 Result
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       153 (0x99)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition11.Test1 - Caller: Arguments: N"
    + "one - ReturnType: void; Callee(virtual): Arguments: None - ReturnType: "
    + "void"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition11::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition11::Caller1()
      IL_0016:  leave.s    IL_006f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  isinst     [mscorlib]System.DivideByZeroException
      IL_001f:  brtrue.s   IL_002a

      IL_0021:  ldc.i4.s   101
      IL_0023:  stsfld     int32 TailcallVerify.Condition11::Result
      IL_0028:  rethrow
      IL_002a:  ldloc.0
      IL_002b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0030:  ldstr      "Caller"
      IL_0035:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003a:  ldc.i4.m1
      IL_003b:  beq.s      IL_006d

      IL_003d:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0042:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0047:  ldstr      "------------------------------------------------"
      IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0051:  call       void [System.Console]System.Console::WriteLine()
      IL_0056:  ldloc.0
      IL_0057:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0061:  call       void [System.Console]System.Console::WriteLine()
      IL_0066:  ldc.i4.s   101
      IL_0068:  stsfld     int32 TailcallVerify.Condition11::Result
      IL_006d:  leave.s    IL_006f

    }  // end handler
    IL_006f:  ldstr      "Execution finished - Test "
    IL_0074:  ldsfld     int32 TailcallVerify.Condition11::Result
    IL_0079:  ldc.i4.s   100
    IL_007b:  beq.s      IL_0084

    IL_007d:  ldstr      "FAILED"
    IL_0082:  br.s       IL_0089

    IL_0084:  ldstr      "PASSED"
    IL_0089:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0093:  ldsfld     int32 TailcallVerify.Condition11::Result
    IL_0098:  ret
  } // end of method Condition11::Test1

  .method private hidebysig static void  Caller1() cil managed
  {
    // Code size       59 (0x3b)
    .maxstack  2
    .locals init ([0] class TailcallVerify.Condition11 con)
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition11::Result
    IL_002e:  newobj     instance void TailcallVerify.Condition11::.ctor()
    IL_0033:  stloc.0
    IL_0034:  ldloc.0
    IL_0035:  tail. callvirt   instance void TailcallVerify.Condition11::Callee1()
    IL_003a:  ret
  } // end of method Condition11::Caller1

  .method public hidebysig newslot virtual 
          instance void  Callee1() cil managed noinlining
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition11::zero
    IL_0008:  div
    IL_0009:  pop
    IL_000a:  ret
  } // end of method Condition11::Callee1

  .method public hidebysig static int32  Test2() cil managed nooptimization
  {
    // Code size       160 (0xa0)
    .maxstack  3
    .locals init ([0] class TailcallVerify.Condition11 con,
             [1] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition11.Test2  - Caller: Arguments: "
    + "None - ReturnType: void; Callee (virtual call): Arguments: 0 - ReturnTy"
    + "pe: Int32"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition11::Result
    .try
    {
      IL_0011:  newobj     instance void TailcallVerify.Condition11::.ctor()
      IL_0016:  stloc.0
      IL_0017:  ldloc.0
      IL_0018:  callvirt   instance void TailcallVerify.Condition11::Caller2()
      IL_001d:  leave.s    IL_0076

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_001f:  stloc.1
      IL_0020:  ldloc.1
      IL_0021:  isinst     [mscorlib]System.DivideByZeroException
      IL_0026:  brtrue.s   IL_0031

      IL_0028:  ldc.i4.s   101
      IL_002a:  stsfld     int32 TailcallVerify.Condition11::Result
      IL_002f:  rethrow
      IL_0031:  ldloc.1
      IL_0032:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0037:  ldstr      "Caller"
      IL_003c:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_0041:  ldc.i4.m1
      IL_0042:  beq.s      IL_0074

      IL_0044:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0049:  call       void [System.Console]System.Console::WriteLine(string)
      IL_004e:  ldstr      "------------------------------------------------"
      IL_0053:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0058:  call       void [System.Console]System.Console::WriteLine()
      IL_005d:  ldloc.1
      IL_005e:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0063:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0068:  call       void [System.Console]System.Console::WriteLine()
      IL_006d:  ldc.i4.s   101
      IL_006f:  stsfld     int32 TailcallVerify.Condition11::Result
      IL_0074:  leave.s    IL_0076

    }  // end handler
    IL_0076:  ldstr      "Execution finished - Test "
    IL_007b:  ldsfld     int32 TailcallVerify.Condition11::Result
    IL_0080:  ldc.i4.s   100
    IL_0082:  beq.s      IL_008b

    IL_0084:  ldstr      "FAILED"
    IL_0089:  br.s       IL_0090

    IL_008b:  ldstr      "PASSED"
    IL_0090:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0095:  call       void [System.Console]System.Console::WriteLine(string)
    IL_009a:  ldsfld     int32 TailcallVerify.Condition11::Result
    IL_009f:  ret
  } // end of method Condition11::Test2

  .method private hidebysig instance void 
          Caller2() cil managed
  {
    // Code size       54 (0x36)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition11::Result
    IL_002e:  ldarg.0
    IL_002f:  tail. callvirt   instance int32 TailcallVerify.Condition11::Callee2()
    IL_0034:  pop
    IL_0035:  ret
  } // end of method Condition11::Caller2

  .method family hidebysig newslot virtual 
          instance int32  Callee2() cil managed noinlining
  {
    // Code size       12 (0xc)
    .maxstack  2
    .locals init ([0] int32 i)
    IL_0000:  ldc.i4.1
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition11::zero
    IL_0008:  div
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method Condition11::Callee2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Condition11::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  volatile.
    IL_0003:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition11::zero
    IL_0008:  ldc.i4.s   100
    IL_000a:  stsfld     int32 TailcallVerify.Condition11::Result
    IL_000f:  ret
  } // end of method Condition11::.cctor

} // end of class TailcallVerify.Condition11

.class private auto ansi beforefieldinit TailcallVerify.MyCompare
       extends [mscorlib]System.Object
       implements class [mscorlib]System.Collections.Generic.IComparer`1<string>
{
  .method public hidebysig newslot virtual final 
          instance int32  Compare(string x,
                                  string y) cil managed
  {
    // Code size       108 (0x6c)
    .maxstack  3
    .locals init ([0] int32 a,
             [1] int32 b)
    IL_0000:  ldarg.1
    IL_0001:  ldstr      "Condition"
    IL_0006:  ldstr      ""
    IL_000b:  callvirt   instance string [mscorlib]System.String::Replace(string,
                                                                          string)
    IL_0010:  ldstr      "."
    IL_0015:  ldstr      ""
    IL_001a:  callvirt   instance string [mscorlib]System.String::Replace(string,
                                                                          string)
    IL_001f:  ldstr      "Test"
    IL_0024:  ldstr      ""
    IL_0029:  callvirt   instance string [mscorlib]System.String::Replace(string,
                                                                          string)
    IL_002e:  call       int32 [mscorlib]System.Convert::ToInt32(string)
    IL_0033:  stloc.0
    IL_0034:  ldarg.2
    IL_0035:  ldstr      "Condition"
    IL_003a:  ldstr      ""
    IL_003f:  callvirt   instance string [mscorlib]System.String::Replace(string,
                                                                          string)
    IL_0044:  ldstr      "."
    IL_0049:  ldstr      ""
    IL_004e:  callvirt   instance string [mscorlib]System.String::Replace(string,
                                                                          string)
    IL_0053:  ldstr      "Test"
    IL_0058:  ldstr      ""
    IL_005d:  callvirt   instance string [mscorlib]System.String::Replace(string,
                                                                          string)
    IL_0062:  call       int32 [mscorlib]System.Convert::ToInt32(string)
    IL_0067:  stloc.1
    IL_0068:  ldloc.0
    IL_0069:  ldloc.1
    IL_006a:  sub
    IL_006b:  ret
  } // end of method MyCompare::Compare

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MyCompare::.ctor

} // end of class TailcallVerify.MyCompare

.class private auto ansi beforefieldinit TailcallVerify.Program
       extends [mscorlib]System.Object
{
  .field private static int32 result
  .method private hidebysig static void  PrintOutRunTestsFile() cil managed
  {
    // Code size       948 (0x3b4)
    .maxstack  4
    .locals init ([0] class [mscorlib]System.Collections.Generic.List`1<string> exludeCollection,
             [1] class [mscorlib]System.Collections.Generic.List`1<string> foo,
             [2] class [mscorlib]System.Type t,
             [3] class [mscorlib]System.Reflection.MethodInfo m,
             [4] class [mscorlib]System.Text.StringBuilder sb,
             [5] string s,
             [6] string V_6,
             [7] string 'rem',
             [8] class [mscorlib]System.Collections.Generic.List`1<string> '<>g__initLocal0',
             [9] class [mscorlib]System.Type[] CS$6$0000,
             [10] int32 CS$7$0001,
             [11] class [mscorlib]System.Reflection.MethodInfo[] CS$6$0002,
             [12] int32 CS$7$0003,
             [13] valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<string> CS$5$0004,
             [14] valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<string> CS$5$0005)
    IL_0000:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
    IL_0005:  stloc.s    '<>g__initLocal0'
    IL_0007:  ldloc.s    '<>g__initLocal0'
    IL_0009:  ldstr      "Condition1.Test1"
    IL_000e:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_0013:  ldloc.s    '<>g__initLocal0'
    IL_0015:  ldstr      "Condition1.Test2"
    IL_001a:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_001f:  ldloc.s    '<>g__initLocal0'
    IL_0021:  ldstr      "Condition1.Test3"
    IL_0026:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_002b:  ldloc.s    '<>g__initLocal0'
    IL_002d:  ldstr      "Condition1.Test4"
    IL_0032:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_0037:  ldloc.s    '<>g__initLocal0'
    IL_0039:  ldstr      "Condition5.Test1"
    IL_003e:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_0043:  ldloc.s    '<>g__initLocal0'
    IL_0045:  ldstr      "Condition5.Test4"
    IL_004a:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_004f:  ldloc.s    '<>g__initLocal0'
    IL_0051:  ldstr      "Condition5.Test5"
    IL_0056:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_005b:  ldloc.s    '<>g__initLocal0'
    IL_005d:  ldstr      "Condition5.Test6"
    IL_0062:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_0067:  ldloc.s    '<>g__initLocal0'
    IL_0069:  ldstr      "Condition5.Test7"
    IL_006e:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_0073:  ldloc.s    '<>g__initLocal0'
    IL_0075:  ldstr      "Condition6.Test1"
    IL_007a:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_007f:  ldloc.s    '<>g__initLocal0'
    IL_0081:  ldstr      "Condition6.Test2"
    IL_0086:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_008b:  ldloc.s    '<>g__initLocal0'
    IL_008d:  ldstr      "Condition6.Test3"
    IL_0092:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_0097:  ldloc.s    '<>g__initLocal0'
    IL_0099:  ldstr      "Condition6.Test4"
    IL_009e:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_00a3:  ldloc.s    '<>g__initLocal0'
    IL_00a5:  ldstr      "Condition6.Test5"
    IL_00aa:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_00af:  ldloc.s    '<>g__initLocal0'
    IL_00b1:  ldstr      "Condition7.Test1"
    IL_00b6:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_00bb:  ldloc.s    '<>g__initLocal0'
    IL_00bd:  ldstr      "Condition7.Test2"
    IL_00c2:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_00c7:  ldloc.s    '<>g__initLocal0'
    IL_00c9:  ldstr      "Condition7.Test3"
    IL_00ce:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_00d3:  ldloc.s    '<>g__initLocal0'
    IL_00d5:  ldstr      "Condition7.Test4"
    IL_00da:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_00df:  ldloc.s    '<>g__initLocal0'
    IL_00e1:  ldstr      "Condition8.Test2"
    IL_00e6:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_00eb:  ldloc.s    '<>g__initLocal0'
    IL_00ed:  ldstr      "Condition8.Test3"
    IL_00f2:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_00f7:  ldloc.s    '<>g__initLocal0'
    IL_00f9:  ldstr      "Condition9.Test1"
    IL_00fe:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_0103:  ldloc.s    '<>g__initLocal0'
    IL_0105:  ldstr      "Condition11.Test1"
    IL_010a:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_010f:  ldloc.s    '<>g__initLocal0'
    IL_0111:  ldstr      "Condition13.Test1"
    IL_0116:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_011b:  ldloc.s    '<>g__initLocal0'
    IL_011d:  ldstr      "Condition21.Test1"
    IL_0122:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_0127:  ldloc.s    '<>g__initLocal0'
    IL_0129:  ldstr      "Condition21.Test2"
    IL_012e:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_0133:  ldloc.s    '<>g__initLocal0'
    IL_0135:  ldstr      "Condition21.Test3"
    IL_013a:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_013f:  ldloc.s    '<>g__initLocal0'
    IL_0141:  ldstr      "Condition21.Test4"
    IL_0146:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_014b:  ldloc.s    '<>g__initLocal0'
    IL_014d:  ldstr      "Condition21.Test5"
    IL_0152:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_0157:  ldloc.s    '<>g__initLocal0'
    IL_0159:  ldstr      "Condition21.Test6"
    IL_015e:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_0163:  ldloc.s    '<>g__initLocal0'
    IL_0165:  ldstr      "Condition21.Test7"
    IL_016a:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_016f:  ldloc.s    '<>g__initLocal0'
    IL_0171:  stloc.0
    IL_0172:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
    IL_0177:  stloc.1
    IL_0178:  call       class [mscorlib]System.Reflection.Assembly [mscorlib]System.Reflection.Assembly::GetExecutingAssembly()
    IL_017d:  callvirt   instance class [mscorlib]System.Type[] [mscorlib]System.Reflection.Assembly::GetTypes()
    IL_0182:  stloc.s    CS$6$0000
    IL_0184:  ldc.i4.0
    IL_0185:  stloc.s    CS$7$0001
    IL_0187:  br.s       IL_01f6

    IL_0189:  ldloc.s    CS$6$0000
    IL_018b:  ldloc.s    CS$7$0001
    IL_018d:  ldelem.ref
    IL_018e:  stloc.2
    IL_018f:  ldloc.2
    IL_0190:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0195:  ldstr      "Condi"
    IL_019a:  callvirt   instance bool [mscorlib]System.String::StartsWith(string)
    IL_019f:  brfalse.s  IL_01f0

    IL_01a1:  ldloc.2
    IL_01a2:  callvirt   instance class [mscorlib]System.Reflection.MethodInfo[] [mscorlib]System.Type::GetMethods()
    IL_01a7:  stloc.s    CS$6$0002
    IL_01a9:  ldc.i4.0
    IL_01aa:  stloc.s    CS$7$0003
    IL_01ac:  br.s       IL_01e8

    IL_01ae:  ldloc.s    CS$6$0002
    IL_01b0:  ldloc.s    CS$7$0003
    IL_01b2:  ldelem.ref
    IL_01b3:  stloc.3
    IL_01b4:  ldloc.3
    IL_01b5:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_01ba:  ldstr      "Test"
    IL_01bf:  callvirt   instance bool [mscorlib]System.String::StartsWith(string)
    IL_01c4:  brfalse.s  IL_01e2

    IL_01c6:  ldloc.1
    IL_01c7:  ldloc.2
    IL_01c8:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_01cd:  ldstr      "."
    IL_01d2:  ldloc.3
    IL_01d3:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_01d8:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_01dd:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_01e2:  ldloc.s    CS$7$0003
    IL_01e4:  ldc.i4.1
    IL_01e5:  add
    IL_01e6:  stloc.s    CS$7$0003
    IL_01e8:  ldloc.s    CS$7$0003
    IL_01ea:  ldloc.s    CS$6$0002
    IL_01ec:  ldlen
    IL_01ed:  conv.i4
    IL_01ee:  blt.s      IL_01ae

    IL_01f0:  ldloc.s    CS$7$0001
    IL_01f2:  ldc.i4.1
    IL_01f3:  add
    IL_01f4:  stloc.s    CS$7$0001
    IL_01f6:  ldloc.s    CS$7$0001
    IL_01f8:  ldloc.s    CS$6$0000
    IL_01fa:  ldlen
    IL_01fb:  conv.i4
    IL_01fc:  blt.s      IL_0189

    IL_01fe:  ldloc.1
    IL_01ff:  ldstr      "Condition22.Test1"
    IL_0204:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_0209:  ldloc.1
    IL_020a:  ldstr      "Condition22.Test2"
    IL_020f:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_0214:  ldloc.1
    IL_0215:  ldstr      "Condition22.Test3"
    IL_021a:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_021f:  ldloc.1
    IL_0220:  ldstr      "Condition22.Test4"
    IL_0225:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_022a:  ldloc.1
    IL_022b:  ldstr      "Condition23.Test1"
    IL_0230:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_0235:  ldloc.1
    IL_0236:  newobj     instance void TailcallVerify.MyCompare::.ctor()
    IL_023b:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Sort(class [mscorlib]System.Collections.Generic.IComparer`1<!0>)
    IL_0240:  newobj     instance void [mscorlib]System.Text.StringBuilder::.ctor()
    IL_0245:  stloc.s    sb
    IL_0247:  ldloc.s    sb
    IL_0249:  ldstr      "del result.txt"
    IL_024e:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
    IL_0253:  pop
    IL_0254:  ldloc.s    sb
    IL_0256:  ldstr      "echo Make sure BVT_ROOT is set or Condition23.Test"
    + "1 will fail!!  >> result.txt"
    IL_025b:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
    IL_0260:  pop
    IL_0261:  ldloc.s    sb
    IL_0263:  ldstr      "echo TailcallVerifyWithPrefix.exe results >> resul"
    + "t.txt"
    IL_0268:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
    IL_026d:  pop
    IL_026e:  ldloc.s    sb
    IL_0270:  ldstr      "echo -------------  >> result.txt"
    IL_0275:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
    IL_027a:  pop
    IL_027b:  ldloc.1
    IL_027c:  callvirt   instance valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<!0> class [mscorlib]System.Collections.Generic.List`1<string>::GetEnumerator()
    IL_0281:  stloc.s    CS$5$0004
    .try
    {
      IL_0283:  br.s       IL_02c6

      IL_0285:  ldloca.s   CS$5$0004
      IL_0287:  call       instance !0 valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<string>::get_Current()
      IL_028c:  stloc.s    s
      IL_028e:  ldloc.s    s
      IL_0290:  ldstr      "Condition23"
      IL_0295:  callvirt   instance bool [mscorlib]System.String::Contains(string)
      IL_029a:  brfalse.s  IL_02b2

      IL_029c:  ldloc.s    sb
      IL_029e:  ldstr      "%BVT_ROOT%\\tools\\PartialTrustRunner\\PTRunn"
      + "er.exe TailcallVerifyWithPrefix.exe {0} & call echo {0}:%%ERRORLEVEL%% "
      + ">> result.txt"
      IL_02a3:  ldloc.s    s
      IL_02a5:  call       string [mscorlib]System.String::Format(string,
                                                                  object)
      IL_02aa:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
      IL_02af:  pop
      IL_02b0:  br.s       IL_02c6

      IL_02b2:  ldloc.s    sb
      IL_02b4:  ldstr      "TailcallVerifyWithPrefix.exe {0} & call echo {0}:%"
      + "%ERRORLEVEL%% >> result.txt"
      IL_02b9:  ldloc.s    s
      IL_02bb:  call       string [mscorlib]System.String::Format(string,
                                                                  object)
      IL_02c0:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
      IL_02c5:  pop
      IL_02c6:  ldloca.s   CS$5$0004
      IL_02c8:  call       instance bool valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<string>::MoveNext()
      IL_02cd:  brtrue.s   IL_0285

      IL_02cf:  leave.s    IL_02df

    }  // end .try
    finally
    {
      IL_02d1:  ldloca.s   CS$5$0004
      IL_02d3:  constrained. valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<string>
      IL_02d9:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
      IL_02de:  endfinally
    }  // end handler
    IL_02df:  ldloc.s    sb
    IL_02e1:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine()
    IL_02e6:  pop
    IL_02e7:  ldloc.s    sb
    IL_02e9:  ldstr      "echo TailcallVerifyWithPrefix.exe results >> result."
    + "txt"
    IL_02ee:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
    IL_02f3:  pop
    IL_02f4:  ldloc.s    sb
    IL_02f6:  ldstr      "echo -------------  >> result.txt"
    IL_02fb:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
    IL_0300:  pop
    IL_0301:  ldloc.1
    IL_0302:  callvirt   instance valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<!0> class [mscorlib]System.Collections.Generic.List`1<string>::GetEnumerator()
    IL_0307:  stloc.s    CS$5$0005
    .try
    {
      IL_0309:  br.s       IL_0368

      IL_030b:  ldloca.s   CS$5$0005
      IL_030d:  call       instance !0 valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<string>::get_Current()
      IL_0312:  stloc.s    V_6
      IL_0314:  ldloc.0
      IL_0315:  ldloc.s    V_6
      IL_0317:  callvirt   instance bool class [mscorlib]System.Collections.Generic.List`1<string>::Contains(!0)
      IL_031c:  brtrue.s   IL_0325

      IL_031e:  ldstr      ""
      IL_0323:  br.s       IL_032a

      IL_0325:  ldstr      "rem"
      IL_032a:  stloc.s    'rem'
      IL_032c:  ldloc.s    V_6
      IL_032e:  ldstr      "Condition23"
      IL_0333:  callvirt   instance bool [mscorlib]System.String::Contains(string)
      IL_0338:  brfalse.s  IL_0352

      IL_033a:  ldloc.s    sb
      IL_033c:  ldstr      "{1} %BVT_ROOT%\\tools\\PartialTrustRunner\\PT"
      + "Runner.exe TailcallVerifyWithPrefix.exe {0} & call echo {0}:%%ERRORLEVEL%"
      + "% >> result.txt"
      IL_0341:  ldloc.s    V_6
      IL_0343:  ldloc.s    'rem'
      IL_0345:  call       string [mscorlib]System.String::Format(string,
                                                                  object,
                                                                  object)
      IL_034a:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
      IL_034f:  pop
      IL_0350:  br.s       IL_0368

      IL_0352:  ldloc.s    sb
      IL_0354:  ldstr      "{1} TailcallVerifyWithPrefix.exe {0} & call echo {0}"
      + ":%%ERRORLEVEL%% >> result.txt"
      IL_0359:  ldloc.s    V_6
      IL_035b:  ldloc.s    'rem'
      IL_035d:  call       string [mscorlib]System.String::Format(string,
                                                                  object,
                                                                  object)
      IL_0362:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
      IL_0367:  pop
      IL_0368:  ldloca.s   CS$5$0005
      IL_036a:  call       instance bool valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<string>::MoveNext()
      IL_036f:  brtrue.s   IL_030b

      IL_0371:  leave.s    IL_0381

    }  // end .try
    finally
    {
      IL_0373:  ldloca.s   CS$5$0005
      IL_0375:  constrained. valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<string>
      IL_037b:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
      IL_0380:  endfinally
    }  // end handler
    IL_0381:  ldloc.s    sb
    IL_0383:  ldstr      "start notepad.exe result.txt"
    IL_0388:  callvirt   instance class [mscorlib]System.Text.StringBuilder [mscorlib]System.Text.StringBuilder::AppendLine(string)
    IL_038d:  pop
    IL_038e:  ldstr      "RunTests.bat written to your current folder!!"
    IL_0393:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0398:  ldstr      "Make sure BVT_ROOT is set or Condition23.Test1 wil"
    + "l fail!!"
    IL_039d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_03a2:  ldstr      "RunTests.bat"
    IL_03a7:  ldloc.s    sb
    IL_03a9:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_03ae:  call       void [mscorlib]System.IO.File::WriteAllText(string,
                                                                     string)
    IL_03b3:  ret
  } // end of method Program::PrintOutRunTestsFile

  .method public hidebysig specialname static 
          int32  get_Result() cil managed
  {
    // Code size       6 (0x6)
    .maxstack  8
    IL_0000:  ldsfld     int32 TailcallVerify.Program::result
    IL_0005:  ret
  } // end of method Program::get_Result

  .method public hidebysig specialname static 
          void  set_Result(int32 'value') cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldsfld     int32 TailcallVerify.Program::result
    IL_0005:  ldc.i4.s   100
    IL_0007:  bne.un.s   IL_000f

    IL_0009:  ldarg.0
    IL_000a:  stsfld     int32 TailcallVerify.Program::result
    IL_000f:  ret
  } // end of method Program::set_Result

  .method private hidebysig static int32 
          Run(string arg) cil managed
  {
    // Code size       1964 (0x7ac)
    .maxstack  3
    .locals init ([0] string RunAll,
             [1] string runTestCaseId)
    IL_0000:  ldstr      "/\?"
    IL_0005:  stloc.0
    IL_0006:  ldarg.0
    IL_0011:  stloc.1
    IL_0012:  ldloc.1
    IL_0013:  ldstr      "/create"
    IL_0018:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_001d:  brfalse.s  IL_0027

    IL_001f:  call       void TailcallVerify.Program::PrintOutRunTestsFile()
    IL_0024:  ldc.i4.s   100
    IL_0026:  ret

    IL_0027:  ldloc.1
    IL_0028:  ldstr      "/\?"
    IL_002d:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0032:  brfalse.s  IL_003c

    IL_0034:  call       void TailcallVerify.Program::Usage()
    IL_0039:  ldc.i4.s   100
    IL_003b:  ret

    IL_003c:  ldloc.1
    IL_003d:  ldstr      "Condition1.Test1"
    IL_0042:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0047:  brtrue.s   IL_0052

    IL_0049:  ldloc.1
    IL_004a:  ldloc.0
    IL_004b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0050:  brfalse.s  IL_005c

    IL_0052:  call       int32 TailcallVerify.Condition1::Test1()
    IL_0057:  call       void TailcallVerify.Program::set_Result(int32)
    IL_005c:  ldloc.1
    IL_005d:  ldstr      "Condition1.Test2"
    IL_0062:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0067:  brtrue.s   IL_0072

    IL_0069:  ldloc.1
    IL_006a:  ldloc.0
    IL_006b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0070:  brfalse.s  IL_007c

    IL_0072:  call       int32 TailcallVerify.Condition1::Test2()
    IL_0077:  call       void TailcallVerify.Program::set_Result(int32)
    IL_007c:  ldloc.1
    IL_007d:  ldstr      "Condition1.Test3"
    IL_0082:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0087:  brtrue.s   IL_0092

    IL_0089:  ldloc.1
    IL_008a:  ldloc.0
    IL_008b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0090:  brfalse.s  IL_009c

    IL_0092:  call       int32 TailcallVerify.Condition1::Test3()
    IL_0097:  call       void TailcallVerify.Program::set_Result(int32)
    IL_009c:  ldloc.1
    IL_009d:  ldstr      "Condition1.Test4"
    IL_00a2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_00a7:  brtrue.s   IL_00b2

    IL_00a9:  ldloc.1
    IL_00aa:  ldloc.0
    IL_00ab:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_00b0:  brfalse.s  IL_00bc

    IL_00b2:  call       int32 TailcallVerify.Condition1::Test4()
    IL_00b7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_00bc:  ldloc.1
    IL_00bd:  ldstr      "Condition2.Test1"
    IL_00c2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_00c7:  brtrue.s   IL_00d2

    IL_00c9:  ldloc.1
    IL_00ca:  ldloc.0
    IL_00cb:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_00d0:  brfalse.s  IL_00dc

    IL_00d2:  call       int32 TailcallVerify.Condition2::Test1()
    IL_00d7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_00dc:  ldloc.1
    IL_00dd:  ldstr      "Condition2.Test2"
    IL_00e2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_00e7:  brtrue.s   IL_00f2

    IL_00e9:  ldloc.1
    IL_00ea:  ldloc.0
    IL_00eb:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_00f0:  brfalse.s  IL_00fc

    IL_00f2:  call       int32 TailcallVerify.Condition2::Test2()
    IL_00f7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_00fc:  ldloc.1
    IL_00fd:  ldstr      "Condition3.Test1"
    IL_0102:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0107:  brtrue.s   IL_0112

    IL_0109:  ldloc.1
    IL_010a:  ldloc.0
    IL_010b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0110:  brfalse.s  IL_011c

    IL_0112:  call       int32 TailcallVerify.Condition3::Test1()
    IL_0117:  call       void TailcallVerify.Program::set_Result(int32)
    IL_011c:  ldloc.1
    IL_011d:  ldstr      "Condition3.Test2"
    IL_0122:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0127:  brtrue.s   IL_0132

    IL_0129:  ldloc.1
    IL_012a:  ldloc.0
    IL_012b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0130:  brfalse.s  IL_013c

    IL_0132:  call       int32 TailcallVerify.Condition3::Test2()
    IL_0137:  call       void TailcallVerify.Program::set_Result(int32)
    IL_013c:  ldloc.1
    IL_013d:  ldstr      "Condition4.Test1"
    IL_0142:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0147:  brtrue.s   IL_0152

    IL_0149:  ldloc.1
    IL_014a:  ldloc.0
    IL_014b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0150:  brfalse.s  IL_015c

    IL_0152:  call       int32 TailcallVerify.Condition4::Test1()
    IL_0157:  call       void TailcallVerify.Program::set_Result(int32)
    IL_015c:  ldloc.1
    IL_015d:  ldstr      "Condition4.Test2"
    IL_0162:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0167:  brtrue.s   IL_0172

    IL_0169:  ldloc.1
    IL_016a:  ldloc.0
    IL_016b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0170:  brfalse.s  IL_017c

    IL_0172:  call       int32 TailcallVerify.Condition4::Test2()
    IL_0177:  call       void TailcallVerify.Program::set_Result(int32)
    IL_017c:  ldloc.1
    IL_017d:  ldstr      "Condition5.Test1"
    IL_0182:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0187:  brtrue.s   IL_0192

    IL_0189:  ldloc.1
    IL_018a:  ldloc.0
    IL_018b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0190:  brfalse.s  IL_019c

    IL_0192:  call       int32 TailcallVerify.Condition5::Test1()
    IL_0197:  call       void TailcallVerify.Program::set_Result(int32)
    IL_019c:  ldloc.1
    IL_019d:  ldstr      "Condition5.Test2"
    IL_01a2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_01a7:  brtrue.s   IL_01b2

    IL_01a9:  ldloc.1
    IL_01aa:  ldloc.0
    IL_01ab:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_01b0:  brfalse.s  IL_01bc

    IL_01b2:  call       int32 TailcallVerify.Condition5::Test2()
    IL_01b7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_01bc:  ldloc.1
    IL_01bd:  ldstr      "Condition5.Test3"
    IL_01c2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_01c7:  brtrue.s   IL_01d2

    IL_01c9:  ldloc.1
    IL_01ca:  ldloc.0
    IL_01cb:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_01d0:  brfalse.s  IL_01dc

    IL_01d2:  call       int32 TailcallVerify.Condition5::Test3()
    IL_01d7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_01dc:  ldloc.1
    IL_01dd:  ldstr      "Condition5.Test4"
    IL_01e2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_01e7:  brtrue.s   IL_01f2

    IL_01e9:  ldloc.1
    IL_01ea:  ldloc.0
    IL_01eb:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_01f0:  brfalse.s  IL_01fc

    IL_01f2:  call       int32 TailcallVerify.Condition5::Test4()
    IL_01f7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_01fc:  ldloc.1
    IL_01fd:  ldstr      "Condition5.Test5"
    IL_0202:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0207:  brtrue.s   IL_0212

    IL_0209:  ldloc.1
    IL_020a:  ldloc.0
    IL_020b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0210:  brfalse.s  IL_021c

    IL_0212:  call       int32 TailcallVerify.Condition5::Test5()
    IL_0217:  call       void TailcallVerify.Program::set_Result(int32)
    IL_021c:  ldloc.1
    IL_021d:  ldstr      "Condition5.Test6"
    IL_0222:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0227:  brtrue.s   IL_0232

    IL_0229:  ldloc.1
    IL_022a:  ldloc.0
    IL_022b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0230:  brfalse.s  IL_023c

    IL_0232:  call       int32 TailcallVerify.Condition5::Test6()
    IL_0237:  call       void TailcallVerify.Program::set_Result(int32)
    IL_023c:  ldloc.1
    IL_023d:  ldstr      "Condition5.Test7"
    IL_0242:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0247:  brtrue.s   IL_0252

    IL_0249:  ldloc.1
    IL_024a:  ldloc.0
    IL_024b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0250:  brfalse.s  IL_025c

    IL_0252:  call       int32 TailcallVerify.Condition5::Test7()
    IL_0257:  call       void TailcallVerify.Program::set_Result(int32)
    IL_025c:  ldloc.1
    IL_025d:  ldstr      "Condition5.Test8"
    IL_0262:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0267:  brtrue.s   IL_0272

    IL_0269:  ldloc.1
    IL_026a:  ldloc.0
    IL_026b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0270:  brfalse.s  IL_027c

    IL_0272:  call       int32 TailcallVerify.Condition5::Test8()
    IL_0277:  call       void TailcallVerify.Program::set_Result(int32)
    IL_027c:  ldloc.1
    IL_027d:  ldstr      "Condition5.Test9"
    IL_0282:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0287:  brtrue.s   IL_0292

    IL_0289:  ldloc.1
    IL_028a:  ldloc.0
    IL_028b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0290:  brfalse.s  IL_029c

    IL_0292:  call       int32 TailcallVerify.Condition5::Test9()
    IL_0297:  call       void TailcallVerify.Program::set_Result(int32)
    IL_029c:  ldloc.1
    IL_029d:  ldstr      "Condition6.Test1"
    IL_02a2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_02a7:  brtrue.s   IL_02b2

    IL_02a9:  ldloc.1
    IL_02aa:  ldloc.0
    IL_02ab:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_02b0:  brfalse.s  IL_02bc

    IL_02b2:  call       int32 TailcallVerify.Condition6::Test1()
    IL_02b7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_02bc:  ldloc.1
    IL_02bd:  ldstr      "Condition6.Test2"
    IL_02c2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_02c7:  brtrue.s   IL_02d2

    IL_02c9:  ldloc.1
    IL_02ca:  ldloc.0
    IL_02cb:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_02d0:  brfalse.s  IL_02dc

    IL_02d2:  call       int32 TailcallVerify.Condition6::Test2()
    IL_02d7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_02dc:  ldloc.1
    IL_02dd:  ldstr      "Condition6.Test3"
    IL_02e2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_02e7:  brtrue.s   IL_02f2

    IL_02e9:  ldloc.1
    IL_02ea:  ldloc.0
    IL_02eb:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_02f0:  brfalse.s  IL_02fc

    IL_02f2:  call       int32 TailcallVerify.Condition6::Test3()
    IL_02f7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_02fc:  ldloc.1
    IL_02fd:  ldstr      "Condition6.Test4"
    IL_0302:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0307:  brtrue.s   IL_0312

    IL_0309:  ldloc.1
    IL_030a:  ldloc.0
    IL_030b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0310:  brfalse.s  IL_031c

    IL_0312:  call       int32 TailcallVerify.Condition6::Test4()
    IL_0317:  call       void TailcallVerify.Program::set_Result(int32)
    IL_031c:  ldloc.1
    IL_031d:  ldstr      "Condition6.Test5"
    IL_0322:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0327:  brtrue.s   IL_0332

    IL_0329:  ldloc.1
    IL_032a:  ldloc.0
    IL_032b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0330:  brfalse.s  IL_033c

    IL_0332:  call       int32 TailcallVerify.Condition6::Test5()
    IL_0337:  call       void TailcallVerify.Program::set_Result(int32)
    IL_033c:  ldloc.1
    IL_033d:  ldstr      "Condition7.Test1"
    IL_0342:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0347:  brtrue.s   IL_0352

    IL_0349:  ldloc.1
    IL_034a:  ldloc.0
    IL_034b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0350:  brfalse.s  IL_035c

    IL_0352:  call       int32 TailcallVerify.Condition7::Test1()
    IL_0357:  call       void TailcallVerify.Program::set_Result(int32)
    IL_035c:  ldloc.1
    IL_035d:  ldstr      "Condition7.Test2"
    IL_0362:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0367:  brtrue.s   IL_0372

    IL_0369:  ldloc.1
    IL_036a:  ldloc.0
    IL_036b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0370:  brfalse.s  IL_037c

    IL_0372:  call       int32 TailcallVerify.Condition7::Test2()
    IL_0377:  call       void TailcallVerify.Program::set_Result(int32)
    IL_037c:  ldloc.1
    IL_037d:  ldstr      "Condition7.Test3"
    IL_0382:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0387:  brtrue.s   IL_0392

    IL_0389:  ldloc.1
    IL_038a:  ldloc.0
    IL_038b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0390:  brfalse.s  IL_039c

    IL_0392:  call       int32 TailcallVerify.Condition7::Test3()
    IL_0397:  call       void TailcallVerify.Program::set_Result(int32)
    IL_039c:  ldloc.1
    IL_039d:  ldstr      "Condition7.Test4"
    IL_03a2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_03a7:  brtrue.s   IL_03b2

    IL_03a9:  ldloc.1
    IL_03aa:  ldloc.0
    IL_03ab:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_03b0:  brfalse.s  IL_03bc

    IL_03b2:  call       int32 TailcallVerify.Condition7::Test4()
    IL_03b7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_03bc:  ldloc.1
    IL_03bd:  ldstr      "Condition8.Test1"
    IL_03c2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_03c7:  brtrue.s   IL_03d2

    IL_03c9:  ldloc.1
    IL_03ca:  ldloc.0
    IL_03cb:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_03d0:  brfalse.s  IL_03dc

    IL_03d2:  call       int32 TailcallVerify.Condition8::Test1()
    IL_03d7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_03dc:  ldloc.1
    IL_03dd:  ldstr      "Condition8.Test2"
    IL_03e2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_03e7:  brtrue.s   IL_03f2

    IL_03e9:  ldloc.1
    IL_03ea:  ldloc.0
    IL_03eb:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_03f0:  brfalse.s  IL_03fc

    IL_03f2:  call       int32 TailcallVerify.Condition8::Test2()
    IL_03f7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_03fc:  ldloc.1
    IL_03fd:  ldstr      "Condition8.Test3"
    IL_0402:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0407:  brtrue.s   IL_0412

    IL_0409:  ldloc.1
    IL_040a:  ldloc.0
    IL_040b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0410:  brfalse.s  IL_041c

    IL_0412:  call       int32 TailcallVerify.Condition8::Test3()
    IL_0417:  call       void TailcallVerify.Program::set_Result(int32)
    IL_041c:  ldloc.1
    IL_041d:  ldstr      "Condition9.Test1"
    IL_0422:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0427:  brtrue.s   IL_0432

    IL_0429:  ldloc.1
    IL_042a:  ldloc.0
    IL_042b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0430:  brfalse.s  IL_043c

    IL_0432:  call       int32 TailcallVerify.Condition9::Test1()
    IL_0437:  call       void TailcallVerify.Program::set_Result(int32)
    IL_043c:  ldloc.1
    IL_043d:  ldstr      "Condition10.Test1"
    IL_0442:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0447:  brtrue.s   IL_0452

    IL_0449:  ldloc.1
    IL_044a:  ldloc.0
    IL_044b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0450:  brfalse.s  IL_045c

    IL_0452:  call       int32 TailcallVerify.Condition10::Test1()
    IL_0457:  call       void TailcallVerify.Program::set_Result(int32)
    IL_045c:  ldloc.1
    IL_045d:  ldstr      "Condition10.Test2"
    IL_0462:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0467:  brtrue.s   IL_0472

    IL_0469:  ldloc.1
    IL_046a:  ldloc.0
    IL_046b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0470:  brfalse.s  IL_047c

    IL_0472:  call       int32 TailcallVerify.Condition10::Test2()
    IL_0477:  call       void TailcallVerify.Program::set_Result(int32)
    IL_047c:  ldloc.1
    IL_047d:  ldstr      "Condition10.Test3"
    IL_0482:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0487:  brtrue.s   IL_0492

    IL_0489:  ldloc.1
    IL_048a:  ldloc.0
    IL_048b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0490:  brfalse.s  IL_049c

    IL_0492:  call       int32 TailcallVerify.Condition10::Test3()
    IL_0497:  call       void TailcallVerify.Program::set_Result(int32)
    IL_049c:  ldloc.1
    IL_049d:  ldstr      "Condition10.Test4"
    IL_04a2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_04a7:  brtrue.s   IL_04b2

    IL_04a9:  ldloc.1
    IL_04aa:  ldloc.0
    IL_04ab:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_04b0:  brfalse.s  IL_04bc

    IL_04b2:  call       int32 TailcallVerify.Condition10::Test4()
    IL_04b7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_04bc:  ldloc.1
    IL_04bd:  ldstr      "Condition10.Test5"
    IL_04c2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_04c7:  brtrue.s   IL_04d2

    IL_04c9:  ldloc.1
    IL_04ca:  ldloc.0
    IL_04cb:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_04d0:  brfalse.s  IL_04dc

    IL_04d2:  call       int32 TailcallVerify.Condition10::Test5()
    IL_04d7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_04dc:  ldloc.1
    IL_04dd:  ldstr      "Condition11.Test1"
    IL_04e2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_04e7:  brtrue.s   IL_04f2

    IL_04e9:  ldloc.1
    IL_04ea:  ldloc.0
    IL_04eb:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_04f0:  brfalse.s  IL_04fc

    IL_04f2:  call       int32 TailcallVerify.Condition11::Test1()
    IL_04f7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_04fc:  ldloc.1
    IL_04fd:  ldstr      "Condition12.Test1"
    IL_0502:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0507:  brtrue.s   IL_0512

    IL_0509:  ldloc.1
    IL_050a:  ldloc.0
    IL_050b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0510:  brfalse.s  IL_051c

    IL_0512:  call       int32 TailcallVerify.Condition12::Test1()
    IL_0517:  call       void TailcallVerify.Program::set_Result(int32)
    IL_051c:  ldloc.1
    IL_051d:  ldstr      "Condition13.Test1"
    IL_0522:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0527:  brtrue.s   IL_0532

    IL_0529:  ldloc.1
    IL_052a:  ldloc.0
    IL_052b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0530:  brfalse.s  IL_053c

    IL_0532:  call       int32 TailcallVerify.Condition13::Test1()
    IL_0537:  call       void TailcallVerify.Program::set_Result(int32)
    IL_053c:  ldloc.1
    IL_053d:  ldstr      "Condition16.Test1"
    IL_0542:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0547:  brtrue.s   IL_0552

    IL_0549:  ldloc.1
    IL_054a:  ldloc.0
    IL_054b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0550:  brfalse.s  IL_055c

    IL_0552:  call       int32 TailcallVerify.Condition16::Test1()
    IL_0557:  call       void TailcallVerify.Program::set_Result(int32)
    IL_055c:  ldloc.1
    IL_055d:  ldstr      "Condition17.Test1"
    IL_0562:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0567:  brtrue.s   IL_0572

    IL_0569:  ldloc.1
    IL_056a:  ldloc.0
    IL_056b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0570:  brfalse.s  IL_057c

    IL_0572:  call       int32 TailcallVerify.Condition17::Test1()
    IL_0577:  call       void TailcallVerify.Program::set_Result(int32)
    IL_057c:  ldloc.1
    IL_057d:  ldstr      "Condition18.Test1"
    IL_0582:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0587:  brtrue.s   IL_0592

    IL_0589:  ldloc.1
    IL_058a:  ldloc.0
    IL_058b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0590:  brfalse.s  IL_059c

    IL_0592:  call       int32 TailcallVerify.Condition18::Test1()
    IL_0597:  call       void TailcallVerify.Program::set_Result(int32)
    IL_059c:  ldloc.1
    IL_059d:  ldstr      "Condition18.Test2"
    IL_05a2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_05a7:  brtrue.s   IL_05b2

    IL_05a9:  ldloc.1
    IL_05aa:  ldloc.0
    IL_05ab:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_05b0:  brfalse.s  IL_05bc

    IL_05b2:  call       int32 TailcallVerify.Condition18::Test2()
    IL_05b7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_05bc:  ldloc.1
    IL_05bd:  ldstr      "Condition19.Test1"
    IL_05c2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_05c7:  brtrue.s   IL_05d2

    IL_05c9:  ldloc.1
    IL_05ca:  ldloc.0
    IL_05cb:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_05d0:  brfalse.s  IL_05dc

    IL_05d2:  call       int32 TailcallVerify.Condition19::Test1()
    IL_05d7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_05dc:  ldloc.1
    IL_05dd:  ldstr      "Condition19.Test2"
    IL_05e2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_05e7:  brtrue.s   IL_05f2

    IL_05e9:  ldloc.1
    IL_05ea:  ldloc.0
    IL_05eb:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_05f0:  brfalse.s  IL_05fc

    IL_05f2:  call       int32 TailcallVerify.Condition19::Test2()
    IL_05f7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_05fc:  ldloc.1
    IL_05fd:  ldstr      "Condition20.Test1"
    IL_0602:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0607:  brtrue.s   IL_0612

    IL_0609:  ldloc.1
    IL_060a:  ldloc.0
    IL_060b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0610:  brfalse.s  IL_061c

    IL_0612:  call       int32 TailcallVerify.Condition20::Test1()
    IL_0617:  call       void TailcallVerify.Program::set_Result(int32)
    IL_061c:  ldloc.1
    IL_061d:  ldstr      "Condition21.Test1"
    IL_0622:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0627:  brtrue.s   IL_0632

    IL_0629:  ldloc.1
    IL_062a:  ldloc.0
    IL_062b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0630:  brfalse.s  IL_063c

    IL_0632:  call       int32 TailcallVerify.Condition21::Test1()
    IL_0637:  call       void TailcallVerify.Program::set_Result(int32)
    IL_063c:  ldloc.1
    IL_063d:  ldstr      "Condition21.Test2"
    IL_0642:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0647:  brtrue.s   IL_0652

    IL_0649:  ldloc.1
    IL_064a:  ldloc.0
    IL_064b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0650:  brfalse.s  IL_065c

    IL_0652:  call       int32 TailcallVerify.Condition21::Test2()
    IL_0657:  call       void TailcallVerify.Program::set_Result(int32)
    IL_065c:  ldloc.1
    IL_065d:  ldstr      "Condition21.Test3"
    IL_0662:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0667:  brtrue.s   IL_0672

    IL_0669:  ldloc.1
    IL_066a:  ldloc.0
    IL_066b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0670:  brfalse.s  IL_067c

    IL_0672:  call       int32 TailcallVerify.Condition21::Test3()
    IL_0677:  call       void TailcallVerify.Program::set_Result(int32)
    IL_067c:  ldloc.1
    IL_067d:  ldstr      "Condition21.Test4"
    IL_0682:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0687:  brtrue.s   IL_0692

    IL_0689:  ldloc.1
    IL_068a:  ldloc.0
    IL_068b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0690:  brfalse.s  IL_069c

    IL_0692:  call       int32 TailcallVerify.Condition21::Test4()
    IL_0697:  call       void TailcallVerify.Program::set_Result(int32)
    IL_069c:  ldloc.1
    IL_069d:  ldstr      "Condition21.Test5"
    IL_06a2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_06a7:  brtrue.s   IL_06b2

    IL_06a9:  ldloc.1
    IL_06aa:  ldloc.0
    IL_06ab:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_06b0:  brfalse.s  IL_06bc

    IL_06b2:  call       int32 TailcallVerify.Condition21::Test5()
    IL_06b7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_06bc:  ldloc.1
    IL_06bd:  ldstr      "Condition21.Test6"
    IL_06c2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_06c7:  brtrue.s   IL_06d2

    IL_06c9:  ldloc.1
    IL_06ca:  ldloc.0
    IL_06cb:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_06d0:  brfalse.s  IL_06dc

    IL_06d2:  call       int32 TailcallVerify.Condition21::Test6()
    IL_06d7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_06dc:  ldloc.1
    IL_06dd:  ldstr      "Condition21.Test7"
    IL_06e2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_06e7:  brtrue.s   IL_06f2

    IL_06e9:  ldloc.1
    IL_06ea:  ldloc.0
    IL_06eb:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_06f0:  brfalse.s  IL_06fc

    IL_06f2:  call       int32 TailcallVerify.Condition21::Test7()
    IL_06f7:  call       void TailcallVerify.Program::set_Result(int32)
    IL_06fc:  ldloc.1
    IL_06fd:  ldstr      "Condition22.Test1"
    IL_0702:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0707:  brtrue.s   IL_0712

    IL_0709:  ldloc.1
    IL_070a:  ldloc.0
    IL_070b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0710:  brfalse.s  IL_071c

    IL_0712:  call       int32 TailcallVerify.Condition22::Test1()
    IL_0717:  call       void TailcallVerify.Program::set_Result(int32)
    IL_071c:  ldloc.1
    IL_071d:  ldstr      "Condition22.Test2"
    IL_0722:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0727:  brtrue.s   IL_0732

    IL_0729:  ldloc.1
    IL_072a:  ldloc.0
    IL_072b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0730:  brfalse.s  IL_073c

    IL_0732:  call       int32 TailcallVerify.Condition22::Test2()
    IL_0737:  call       void TailcallVerify.Program::set_Result(int32)
    IL_073c:  ldloc.1
    IL_073d:  ldstr      "Condition22.Test3"
    IL_0742:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0747:  brtrue.s   IL_0752

    IL_0749:  ldloc.1
    IL_074a:  ldloc.0
    IL_074b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0750:  brfalse.s  IL_075c

    IL_0752:  call       int32 TailcallVerify.Condition22::Test3()
    IL_0757:  call       void TailcallVerify.Program::set_Result(int32)
    IL_075c:  ldloc.1
    IL_075d:  ldstr      "Condition22.Test4"
    IL_0762:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0767:  brtrue.s   IL_0772

    IL_0769:  ldloc.1
    IL_076a:  ldloc.0
    IL_076b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0770:  brfalse.s  IL_077c

    IL_0772:  call       int32 TailcallVerify.Condition22::Test4()
    IL_0777:  call       void TailcallVerify.Program::set_Result(int32)
    IL_077c:  ldloc.1
    IL_077d:  ldstr      "Condition23.Test1"
    IL_0782:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0787:  brtrue.s   IL_0792

    IL_0789:  ldloc.1
    IL_078a:  ldloc.0
    IL_078b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0790:  brfalse.s  IL_07a6

    IL_0792:  ldstr      "Make sure to run this with Tools\\PartialTr"
    + "ustRunner\\PTRunner.exe or it will fail!!"
    IL_0797:  call       void [System.Console]System.Console::WriteLine(string)
    IL_079c:  call       int32 TailcallVerify.Condition23::Test1()
    IL_07a1:  call       void TailcallVerify.Program::set_Result(int32)
    IL_07a6:  call       int32 TailcallVerify.Program::get_Result()
    IL_07ab:  ret
  } // end of method Program::Run

  .method private hidebysig static int32 
          Main() cil managed
  {
    .entrypoint
    .maxstack  1

    ldstr      "Condition1.Test1"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition1.Test2"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition1.Test3"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition2.Test1"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition2.Test2"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition3.Test1"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition3.Test2"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition4.Test1"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition4.Test2"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition5.Test1"
    call       int32 TailcallVerify.Program::Run(string)      
    pop	
    ldstr      "Condition5.Test2"
    call       int32 TailcallVerify.Program::Run(string)     
    pop	
    ldstr      "Condition5.Test3"
    call       int32 TailcallVerify.Program::Run(string)      
    pop	
    ldstr      "Condition5.Test4"
    call       int32 TailcallVerify.Program::Run(string)    
    pop	
    ldstr      "Condition5.Test5"
    call       int32 TailcallVerify.Program::Run(string)    
    pop	
    ldstr      "Condition5.Test6"
    call       int32 TailcallVerify.Program::Run(string)      
    pop		
    ldstr      "Condition5.Test7"
    call       int32 TailcallVerify.Program::Run(string)      
    pop	
    ldstr      "Condition5.Test8"
    call       int32 TailcallVerify.Program::Run(string)      
    pop	
    ldstr      "Condition5.Test9"
    call       int32 TailcallVerify.Program::Run(string)    
    pop	
    ldstr      "Condition6.Test1"
    call       int32 TailcallVerify.Program::Run(string)      
    pop	
    ldstr      "Condition6.Test2"
    call       int32 TailcallVerify.Program::Run(string)      
    pop	 
    ldstr      "Condition6.Test3"
    call       int32 TailcallVerify.Program::Run(string)      
    pop	
    ldstr      "Condition6.Test4"
    call       int32 TailcallVerify.Program::Run(string)      
    pop	
    ldstr      "Condition6.Test5"
    call       int32 TailcallVerify.Program::Run(string)      
    pop	
    ldstr      "Condition7.Test1"
    call       int32 TailcallVerify.Program::Run(string)    
    pop	
    ldstr      "Condition7.Test2"
    call       int32 TailcallVerify.Program::Run(string)    
    pop	
    ldstr      "Condition7.Test3"
    call       int32 TailcallVerify.Program::Run(string)      
    pop	
    ldstr      "Condition7.Test4"
    call       int32 TailcallVerify.Program::Run(string)      
    pop	

    // Condition8 tests use varargs calling conventions.
    // The lines below should be uncommented when/if CoreCLR starts
    // supporting varargs.
    //ldstr      "Condition8.Test1"
    //call       int32 TailcallVerify.Program::Run(string)
    //pop
    //ldstr      "Condition8.Test2"
    //call       int32 TailcallVerify.Program::Run(string)
    //pop
    //ldstr      "Condition8.Test3"
    //call       int32 TailcallVerify.Program::Run(string)
    //pop

    ldstr      "Condition9.Test1"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition10.Test1"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition10.Test2"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition10.Test3"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition10.Test4"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition10.Test5"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition11.Test1"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition11.Test2"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition12.Test1"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition13.Test1"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition16.Test1"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition17.Test1"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition17.Test4"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition18.Test1"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition18.Test3"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition19.Test1"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition19.Test2"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition20.Test1"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition21.Test1"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition21.Test2"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition21.Test3"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition21.Test6"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition21.Test7"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition22.Test2"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition22.Test4"
    call       int32 TailcallVerify.Program::Run(string)
    ret    
  } // end of method Program::Main
    
  .method private hidebysig static void  Usage() cil managed
  {
    // Code size       71 (0x47)
    .maxstack  1
    IL_0000:  ldstr      "TailcallVerifyWithPrefix.exe ConditionX.TestX | /\? |"
    + " /create"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldstr      "------------------ Available Switches ------------"
    + "--------"
    IL_000f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0014:  ldstr      "No Arguments: Runs all contained testcases (better"
    + " use the /create switch)"
    IL_0019:  call       void [System.Console]System.Console::WriteLine(string)
    IL_001e:  ldstr      "ConditionX.TestX: Runs one of the contained testca"
    + "ses, e.g. Condtion1.Test1"
    IL_0023:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0028:  ldstr      "/\?: This little help..."
    IL_002d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0032:  ldstr      "/Create: Creates a little RunTests.bat file in the"
    + " same folder which runs all testcases "
    IL_0037:  call       void [System.Console]System.Console::WriteLine(string)
    IL_003c:  ldstr      "and opens a result.txt file containing the ERRORLE"
    + "VEL for each."
    IL_0041:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0046:  ret
  } // end of method Program::Usage

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Program::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldc.i4.s   100
    IL_0002:  stsfld     int32 TailcallVerify.Program::result
    IL_0007:  ret
  } // end of method Program::.cctor

  .property int32 Result()
  {
    .get int32 TailcallVerify.Program::get_Result()
    .set void TailcallVerify.Program::set_Result(int32)
  } // end of property Program::Result
} // end of class TailcallVerify.Program

.class private auto ansi beforefieldinit TailcallVerify.Condition8
       extends [mscorlib]System.Object
{
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .field private static int32 Result
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       153 (0x99)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition8.Test1 - Caller: Arguments: No"
    + "ne - ReturnType: void; Callee: Arguments: varargs - ReturnType: void"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition8::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition8::Caller1()
      IL_0016:  leave.s    IL_006f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  isinst     [mscorlib]System.DivideByZeroException
      IL_001f:  brtrue.s   IL_002a

      IL_0021:  ldc.i4.s   101
      IL_0023:  stsfld     int32 TailcallVerify.Condition8::Result
      IL_0028:  rethrow
      IL_002a:  ldloc.0
      IL_002b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0030:  ldstr      "Caller"
      IL_0035:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003a:  ldc.i4.m1
      IL_003b:  beq.s      IL_006d

      IL_003d:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0042:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0047:  ldstr      "------------------------------------------------"
      IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0051:  call       void [System.Console]System.Console::WriteLine()
      IL_0056:  ldloc.0
      IL_0057:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0061:  call       void [System.Console]System.Console::WriteLine()
      IL_0066:  ldc.i4.s   101
      IL_0068:  stsfld     int32 TailcallVerify.Condition8::Result
      IL_006d:  leave.s    IL_006f

    }  // end handler
    IL_006f:  ldstr      "Execution finished - Test "
    IL_0074:  ldsfld     int32 TailcallVerify.Condition8::Result
    IL_0079:  ldc.i4.s   100
    IL_007b:  beq.s      IL_0084

    IL_007d:  ldstr      "FAILED"
    IL_0082:  br.s       IL_0089

    IL_0084:  ldstr      "PASSED"
    IL_0089:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0093:  ldsfld     int32 TailcallVerify.Condition8::Result
    IL_0098:  ret
  } // end of method Condition8::Test1

  .method private hidebysig static void  Caller1() cil managed
  {
    // Code size       55 (0x37)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition8::Result
    IL_002e:  ldc.i4.0
    IL_002f:  ldc.i4.1
    IL_0030:  ldc.i4.2
    IL_0031:  tail. call       vararg void TailcallVerify.Condition8::Callee1(...,
                                                                        int32,
                                                                        int32,
                                                                        int32)
    IL_0036:  ret
  } // end of method Condition8::Caller1

  .method private hidebysig static vararg void 
          Callee1() cil managed noinlining
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition8::zero
    IL_0008:  div
    IL_0009:  pop
    IL_000a:  ret
  } // end of method Condition8::Callee1

  .method public hidebysig static int32  Test2() cil managed nooptimization
  {
    // Code size       156 (0x9c)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition8.Test2 - Caller: Arguments: va"
    + "rargs - ReturnType: void; Callee: Arguments: None - ReturnType: void"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition8::Result
    .try
    {
      IL_0011:  ldc.i4.0
      IL_0012:  ldc.i4.1
      IL_0013:  ldc.i4.2
      IL_0014:  call       vararg void TailcallVerify.Condition8::Caller2(...,
                                                                          int32,
                                                                          int32,
                                                                          int32)
      IL_0019:  leave.s    IL_0072

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_001b:  stloc.0
      IL_001c:  ldloc.0
      IL_001d:  isinst     [mscorlib]System.DivideByZeroException
      IL_0022:  brtrue.s   IL_002d

      IL_0024:  ldc.i4.s   101
      IL_0026:  stsfld     int32 TailcallVerify.Condition8::Result
      IL_002b:  rethrow
      IL_002d:  ldloc.0
      IL_002e:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0033:  ldstr      "Caller"
      IL_0038:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003d:  ldc.i4.m1
      IL_003e:  beq.s      IL_0070

      IL_0040:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0045:  call       void [System.Console]System.Console::WriteLine(string)
      IL_004a:  ldstr      "------------------------------------------------"
      IL_004f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0054:  call       void [System.Console]System.Console::WriteLine()
      IL_0059:  ldloc.0
      IL_005a:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0064:  call       void [System.Console]System.Console::WriteLine()
      IL_0069:  ldc.i4.s   101
      IL_006b:  stsfld     int32 TailcallVerify.Condition8::Result
      IL_0070:  leave.s    IL_0072

    }  // end handler
    IL_0072:  ldstr      "Execution finished - Test "
    IL_0077:  ldsfld     int32 TailcallVerify.Condition8::Result
    IL_007c:  ldc.i4.s   100
    IL_007e:  beq.s      IL_0087

    IL_0080:  ldstr      "FAILED"
    IL_0085:  br.s       IL_008c

    IL_0087:  ldstr      "PASSED"
    IL_008c:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0091:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0096:  ldsfld     int32 TailcallVerify.Condition8::Result
    IL_009b:  ret
  } // end of method Condition8::Test2

  .method private hidebysig static vararg void 
          Caller2() cil managed
  {
    // Code size       52 (0x34)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition8::Result
    IL_002e:  tail. call       void TailcallVerify.Condition8::Callee2()
    IL_0033:  ret
  } // end of method Condition8::Caller2

  .method private hidebysig static void  Callee2() cil managed noinlining
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition8::zero
    IL_0008:  div
    IL_0009:  pop
    IL_000a:  ret
  } // end of method Condition8::Callee2

  .method public hidebysig static int32  Test3() cil managed nooptimization
  {
    // Code size       156 (0x9c)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition8.Test3 - Caller: Arguments: va"
    + "rarg - ReturnType: void; Callee: Arguments: vararg - ReturnType: void"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition8::Result
    .try
    {
      IL_0011:  ldc.i4.0
      IL_0012:  ldc.i4.1
      IL_0013:  ldc.i4.2
      IL_0014:  call       vararg void TailcallVerify.Condition8::Caller3(...,
                                                                          int32,
                                                                          int32,
                                                                          int32)
      IL_0019:  leave.s    IL_0072

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_001b:  stloc.0
      IL_001c:  ldloc.0
      IL_001d:  isinst     [mscorlib]System.DivideByZeroException
      IL_0022:  brtrue.s   IL_002d

      IL_0024:  ldc.i4.s   101
      IL_0026:  stsfld     int32 TailcallVerify.Condition8::Result
      IL_002b:  rethrow
      IL_002d:  ldloc.0
      IL_002e:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0033:  ldstr      "Caller"
      IL_0038:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003d:  ldc.i4.m1
      IL_003e:  beq.s      IL_0070

      IL_0040:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0045:  call       void [System.Console]System.Console::WriteLine(string)
      IL_004a:  ldstr      "------------------------------------------------"
      IL_004f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0054:  call       void [System.Console]System.Console::WriteLine()
      IL_0059:  ldloc.0
      IL_005a:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0064:  call       void [System.Console]System.Console::WriteLine()
      IL_0069:  ldc.i4.s   101
      IL_006b:  stsfld     int32 TailcallVerify.Condition8::Result
      IL_0070:  leave.s    IL_0072

    }  // end handler
    IL_0072:  ldstr      "Execution finished - Test "
    IL_0077:  ldsfld     int32 TailcallVerify.Condition8::Result
    IL_007c:  ldc.i4.s   100
    IL_007e:  beq.s      IL_0087

    IL_0080:  ldstr      "FAILED"
    IL_0085:  br.s       IL_008c

    IL_0087:  ldstr      "PASSED"
    IL_008c:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0091:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0096:  ldsfld     int32 TailcallVerify.Condition8::Result
    IL_009b:  ret
  } // end of method Condition8::Test3

  .method private hidebysig static vararg void 
          Caller3() cil managed
  {
    // Code size       55 (0x37)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition8::Result
    IL_002e:  ldc.i4.0
    IL_002f:  ldc.i4.1
    IL_0030:  ldc.i4.2
    IL_0031:  tail. call       vararg void TailcallVerify.Condition8::Callee3(...,
                                                                        int32,
                                                                        int32,
                                                                        int32)
    IL_0036:  ret
  } // end of method Condition8::Caller3

  .method private hidebysig static vararg void 
          Callee3() cil managed noinlining
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition8::zero
    IL_0008:  div
    IL_0009:  pop
    IL_000a:  ret
  } // end of method Condition8::Callee3

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Condition8::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  volatile.
    IL_0003:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition8::zero
    IL_0008:  ldc.i4.s   100
    IL_000a:  stsfld     int32 TailcallVerify.Condition8::Result
    IL_000f:  ret
  } // end of method Condition8::.cctor

} // end of class TailcallVerify.Condition8

.class private auto ansi beforefieldinit TailcallVerify.Condition21
       extends [mscorlib]System.Object
{
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .field private static int32 Result
  .field private static class [mscorlib]System.Collections.Generic.List`1<int64> evenList
  .field private static class [mscorlib]System.Collections.Generic.List`1<int64> oddList
  .field assembly class [mscorlib]System.Collections.Generic.List`1<int64> threadList
  .field private static int32 counterE4
  .field private static int32 counterO4
  .field private int32 counterE5
  .field private int32 counterO5
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       368 (0x170)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e,
             [1] int64 l,
             [2] int64 V_2,
             [3] valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64> CS$5$0000,
             [4] valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64> CS$5$0001)
    IL_0000:  ldstr      "Executing Condition21.Test1 - Caller: Arguments: N"
    + "one - ReturnType: void; CalleeIsOdd1: Arguments: int, int, int, int, 3 "
    + "byte struct - ReturnType: 3 byte struct; CalleeIsEven1: Arguments: int,"
    + " int, int, int, 3 byte struct - ReturnType: 3 byte struct;"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_0011:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::evenList
    IL_0016:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<int64>::Clear()
    IL_001b:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_0020:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<int64>::Clear()
    .try
    {
      IL_0025:  call       void TailcallVerify.Condition21::Caller1()
      IL_002a:  leave.s    IL_0083

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_002c:  stloc.0
      IL_002d:  ldloc.0
      IL_002e:  isinst     [mscorlib]System.DivideByZeroException
      IL_0033:  brtrue.s   IL_003e

      IL_0035:  ldc.i4.s   101
      IL_0037:  stsfld     int32 TailcallVerify.Condition21::Result
      IL_003c:  rethrow
      IL_003e:  ldloc.0
      IL_003f:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0044:  ldstr      "Caller"
      IL_0049:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_004e:  ldc.i4.m1
      IL_004f:  beq.s      IL_0081

      IL_0051:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0056:  call       void [System.Console]System.Console::WriteLine(string)
      IL_005b:  ldstr      "------------------------------------------------"
      IL_0060:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0065:  call       void [System.Console]System.Console::WriteLine()
      IL_006a:  ldloc.0
      IL_006b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  call       void [System.Console]System.Console::WriteLine()
      IL_007a:  ldc.i4.s   101
      IL_007c:  stsfld     int32 TailcallVerify.Condition21::Result
      IL_0081:  leave.s    IL_0083

    }  // end handler
    IL_0083:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::evenList
    IL_0088:  call       class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0> [System.Linq]System.Linq.Enumerable::Distinct<int64>(class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_008d:  call       int32 [System.Linq]System.Linq.Enumerable::Count<int64>(class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_0092:  ldc.i4.1
    IL_0093:  beq.s      IL_00e4

    IL_0095:  ldstr      "FAILED: The local on the stack for CalleeIsEven3 s"
    + "hifted. (should have always the same address)"
    IL_009a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_009f:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::evenList
    IL_00a4:  callvirt   instance valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<!0> class [mscorlib]System.Collections.Generic.List`1<int64>::GetEnumerator()
    IL_00a9:  stloc.3
    .try
    {
      IL_00aa:  br.s       IL_00c4

      IL_00ac:  ldloca.s   CS$5$0000
      IL_00ae:  call       instance !0 valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64>::get_Current()
      IL_00b3:  stloc.1
      IL_00b4:  ldstr      "0x{0:x8}"
      IL_00b9:  ldloc.1
      IL_00ba:  box        [mscorlib]System.Int64
      IL_00bf:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_00c4:  ldloca.s   CS$5$0000
      IL_00c6:  call       instance bool valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64>::MoveNext()
      IL_00cb:  brtrue.s   IL_00ac

      IL_00cd:  leave.s    IL_00dd

    }  // end .try
    finally
    {
      IL_00cf:  ldloca.s   CS$5$0000
      IL_00d1:  constrained. valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64>
      IL_00d7:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
      IL_00dc:  endfinally
    }  // end handler
    IL_00dd:  ldc.i4.s   101
    IL_00df:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_00e4:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_00e9:  call       class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0> [System.Linq]System.Linq.Enumerable::Distinct<int64>(class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_00ee:  call       int32 [System.Linq]System.Linq.Enumerable::Count<int64>(class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_00f3:  ldc.i4.1
    IL_00f4:  beq.s      IL_0146

    IL_00f6:  ldstr      "FAILED: The local on the stack for CalleeIsOdd3 sh"
    + "ifted. (should have always the same address)"
    IL_00fb:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0100:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_0105:  callvirt   instance valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<!0> class [mscorlib]System.Collections.Generic.List`1<int64>::GetEnumerator()
    IL_010a:  stloc.s    CS$5$0001
    .try
    {
      IL_010c:  br.s       IL_0126

      IL_010e:  ldloca.s   CS$5$0001
      IL_0110:  call       instance !0 valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64>::get_Current()
      IL_0115:  stloc.2
      IL_0116:  ldstr      "0x{0:x8}"
      IL_011b:  ldloc.2
      IL_011c:  box        [mscorlib]System.Int64
      IL_0121:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_0126:  ldloca.s   CS$5$0001
      IL_0128:  call       instance bool valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64>::MoveNext()
      IL_012d:  brtrue.s   IL_010e

      IL_012f:  leave.s    IL_013f

    }  // end .try
    finally
    {
      IL_0131:  ldloca.s   CS$5$0001
      IL_0133:  constrained. valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64>
      IL_0139:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
      IL_013e:  endfinally
    }  // end handler
    IL_013f:  ldc.i4.s   101
    IL_0141:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_0146:  ldstr      "Execution finished - Test "
    IL_014b:  ldsfld     int32 TailcallVerify.Condition21::Result
    IL_0150:  ldc.i4.s   100
    IL_0152:  beq.s      IL_015b

    IL_0154:  ldstr      "FAILED"
    IL_0159:  br.s       IL_0160

    IL_015b:  ldstr      "PASSED"
    IL_0160:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0165:  call       void [System.Console]System.Console::WriteLine(string)
    IL_016a:  ldsfld     int32 TailcallVerify.Condition21::Result
    IL_016f:  ret
  } // end of method Condition21::Test1

  .method private hidebysig static void  Caller1() cil managed
  {
    // Code size       96 (0x60)
    .maxstack  6
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes v,
             [1] valuetype TailcallVerify.ValueType3Bytes '<>g__initLocal0',
             [2] valuetype TailcallVerify.ValueType3Bytes CS$0$0000)
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "FAILED: Caller3 was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_002e:  ldloca.s   CS$0$0000
    IL_0030:  initobj    TailcallVerify.ValueType3Bytes
    IL_0036:  ldloc.2
    IL_0037:  stloc.1
    IL_0038:  ldloca.s   '<>g__initLocal0'
    IL_003a:  ldc.i4     0xff
    IL_003f:  stfld      uint8 TailcallVerify.ValueType3Bytes::i1
    IL_0044:  ldloca.s   '<>g__initLocal0'
    IL_0046:  ldc.i4     0xffff8000
    IL_004b:  stfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_0050:  ldloc.1
    IL_0051:  stloc.0
    IL_0052:  ldc.i4.s   20
    IL_0054:  ldc.i4.1
    IL_0055:  ldc.i4.2
    IL_0056:  ldc.i4.3
    IL_0057:  ldc.i4.4
    IL_0058:  ldloc.0
    IL_0059:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CalleeIsOdd1(int32,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           valuetype TailcallVerify.ValueType3Bytes)
    IL_005e:  pop
    IL_005f:  ret
  } // end of method Condition21::Caller1

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          CalleeIsOdd1(int32 i,
                       int32 i2,
                       int32 i3,
                       int32 i4,
                       int32 i5,
                       valuetype TailcallVerify.ValueType3Bytes v3) cil managed
  {
    // Code size       171 (0xab)
    .maxstack  6
    .locals init ([0] int32 data,
             [1] int32* pData,
             [2] native int p,
             [3] valuetype TailcallVerify.ValueType3Bytes CS$0$0000)
    IL_0000:  ldloca.s   data
    IL_0002:  conv.u
    IL_0003:  stloc.1
    IL_0004:  ldloca.s   p
    IL_0006:  ldloc.1
    IL_0007:  call       instance void [mscorlib]System.IntPtr::.ctor(void*)
    IL_000c:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_0011:  ldloca.s   p
    IL_0013:  call       instance int64 [mscorlib]System.IntPtr::ToInt64()
    IL_0018:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<int64>::Add(!0)
    IL_001d:  ldarg.1
    IL_001e:  ldc.i4.1
    IL_001f:  bne.un.s   IL_004a

    IL_0021:  ldarg.2
    IL_0022:  ldc.i4.2
    IL_0023:  bne.un.s   IL_004a

    IL_0025:  ldarg.3
    IL_0026:  ldc.i4.3
    IL_0027:  bne.un.s   IL_004a

    IL_0029:  ldarg.s    i5
    IL_002b:  ldc.i4.4
    IL_002c:  bne.un.s   IL_004a

    IL_002e:  ldarga.s   v3
    IL_0030:  ldfld      uint8 TailcallVerify.ValueType3Bytes::i1
    IL_0035:  ldc.i4     0xff
    IL_003a:  bne.un.s   IL_004a

    IL_003c:  ldarga.s   v3
    IL_003e:  ldfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_0043:  ldc.i4     0xffff8000
    IL_0048:  beq.s      IL_005b

    IL_004a:  ldstr      "FAILED: Passed in arguments are wrong..."
    IL_004f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0054:  ldc.i4.s   101
    IL_0056:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_005b:  ldc.i4.0
    IL_005c:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0061:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_0066:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_006b:  ldstr      "Callee"
    IL_0070:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_0075:  ldc.i4.m1
    IL_0076:  bne.un.s   IL_0089

    IL_0078:  ldstr      "FAILED: CalleeIsOdd3 was inlined..."
    IL_007d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0082:  ldc.i4.s   101
    IL_0084:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_0089:  ldarg.0
    IL_008a:  brtrue.s   IL_009b

    IL_008c:  ldc.i4.1
    IL_008d:  ldarg.0
    IL_008e:  div
    IL_008f:  starg.s    i
    IL_0091:  ldloca.s   CS$0$0000
    IL_0093:  initobj    TailcallVerify.ValueType3Bytes
    IL_0099:  ldloc.3
    IL_009a:  ret

    IL_009b:  ldarg.0
    IL_009c:  ldc.i4.1
    IL_009d:  sub
    IL_009e:  ldarg.1
    IL_009f:  ldarg.2
    IL_00a0:  ldarg.3
    IL_00a1:  ldarg.s    i5
    IL_00a3:  ldarg.s    v3
    IL_00a5:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CalleeIsEven1(int32,
                                                                                                            int32,
                                                                                                            int32,
                                                                                                            int32,
                                                                                                            int32,
                                                                                                            valuetype TailcallVerify.ValueType3Bytes)
    IL_00aa:  ret
  } // end of method Condition21::CalleeIsOdd1

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          CalleeIsEven1(int32 i,
                        int32 i2,
                        int32 i3,
                        int32 i4,
                        int32 i5,
                        valuetype TailcallVerify.ValueType3Bytes v3) cil managed
  {
    // Code size       171 (0xab)
    .maxstack  6
    .locals init ([0] int32 data,
             [1] int32* pData,
             [2] native int p,
             [3] valuetype TailcallVerify.ValueType3Bytes CS$0$0000)
    IL_0000:  ldloca.s   data
    IL_0002:  conv.u
    IL_0003:  stloc.1
    IL_0004:  ldloca.s   p
    IL_0006:  ldloc.1
    IL_0007:  call       instance void [mscorlib]System.IntPtr::.ctor(void*)
    IL_000c:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::evenList
    IL_0011:  ldloca.s   p
    IL_0013:  call       instance int64 [mscorlib]System.IntPtr::ToInt64()
    IL_0018:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<int64>::Add(!0)
    IL_001d:  ldarg.1
    IL_001e:  ldc.i4.1
    IL_001f:  bne.un.s   IL_004a

    IL_0021:  ldarg.2
    IL_0022:  ldc.i4.2
    IL_0023:  bne.un.s   IL_004a

    IL_0025:  ldarg.3
    IL_0026:  ldc.i4.3
    IL_0027:  bne.un.s   IL_004a

    IL_0029:  ldarg.s    i5
    IL_002b:  ldc.i4.4
    IL_002c:  bne.un.s   IL_004a

    IL_002e:  ldarga.s   v3
    IL_0030:  ldfld      uint8 TailcallVerify.ValueType3Bytes::i1
    IL_0035:  ldc.i4     0xff
    IL_003a:  bne.un.s   IL_004a

    IL_003c:  ldarga.s   v3
    IL_003e:  ldfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_0043:  ldc.i4     0xffff8000
    IL_0048:  beq.s      IL_005b

    IL_004a:  ldstr      "FAILED: Passed in arguments are wrong..."
    IL_004f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0054:  ldc.i4.s   101
    IL_0056:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_005b:  ldc.i4.0
    IL_005c:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0061:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_0066:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_006b:  ldstr      "Callee"
    IL_0070:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_0075:  ldc.i4.m1
    IL_0076:  bne.un.s   IL_0089

    IL_0078:  ldstr      "FAILED: CalleeIsEven3 was inlined..."
    IL_007d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0082:  ldc.i4.s   101
    IL_0084:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_0089:  ldarg.0
    IL_008a:  brtrue.s   IL_009b

    IL_008c:  ldc.i4.1
    IL_008d:  ldarg.0
    IL_008e:  div
    IL_008f:  starg.s    i
    IL_0091:  ldloca.s   CS$0$0000
    IL_0093:  initobj    TailcallVerify.ValueType3Bytes
    IL_0099:  ldloc.3
    IL_009a:  ret

    IL_009b:  ldarg.0
    IL_009c:  ldc.i4.1
    IL_009d:  sub
    IL_009e:  ldarg.1
    IL_009f:  ldarg.2
    IL_00a0:  ldarg.3
    IL_00a1:  ldarg.s    i5
    IL_00a3:  ldarg.s    v3
    IL_00a5:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CalleeIsOdd1(int32,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           valuetype TailcallVerify.ValueType3Bytes)
    IL_00aa:  ret
  } // end of method Condition21::CalleeIsEven1

  .method public hidebysig static int32  Test2() cil managed nooptimization
  {
    // Code size       368 (0x170)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e,
             [1] int64 l,
             [2] int64 V_2,
             [3] valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64> CS$5$0000,
             [4] valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64> CS$5$0001)
    IL_0000:  ldstr      "Executing Condition21.Test2 - Caller: Arguments: N"
    + "one - ReturnType: void; CalleeIsOdd3: Arguments: int, int, int, int, 3 "
    + "byte struct - ReturnType: 3 byte struct; CalleeIsEven3: Arguments: int,"
    + " int, int, int, 3 byte struct - ReturnType: 3 byte struct;"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_0011:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::evenList
    IL_0016:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<int64>::Clear()
    IL_001b:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_0020:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<int64>::Clear()
    .try
    {
      IL_0025:  call       void TailcallVerify.Condition21::Caller2()
      IL_002a:  leave.s    IL_0083

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_002c:  stloc.0
      IL_002d:  ldloc.0
      IL_002e:  isinst     [mscorlib]System.DivideByZeroException
      IL_0033:  brtrue.s   IL_003e

      IL_0035:  ldc.i4.s   101
      IL_0037:  stsfld     int32 TailcallVerify.Condition21::Result
      IL_003c:  rethrow
      IL_003e:  ldloc.0
      IL_003f:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0044:  ldstr      "Caller"
      IL_0049:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_004e:  ldc.i4.m1
      IL_004f:  beq.s      IL_0081

      IL_0051:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0056:  call       void [System.Console]System.Console::WriteLine(string)
      IL_005b:  ldstr      "------------------------------------------------"
      IL_0060:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0065:  call       void [System.Console]System.Console::WriteLine()
      IL_006a:  ldloc.0
      IL_006b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  call       void [System.Console]System.Console::WriteLine()
      IL_007a:  ldc.i4.s   101
      IL_007c:  stsfld     int32 TailcallVerify.Condition21::Result
      IL_0081:  leave.s    IL_0083

    }  // end handler
    IL_0083:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::evenList
    IL_0088:  call       class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0> [System.Linq]System.Linq.Enumerable::Distinct<int64>(class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_008d:  call       int32 [System.Linq]System.Linq.Enumerable::Count<int64>(class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_0092:  ldc.i4.2
    IL_0093:  beq.s      IL_00e4

    IL_0095:  ldstr      "FAILED: The local on the stack for CalleeIsEven3 s"
    + "hifted. (should have always the same address except for the first one.)"
    IL_009a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_009f:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::evenList
    IL_00a4:  callvirt   instance valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<!0> class [mscorlib]System.Collections.Generic.List`1<int64>::GetEnumerator()
    IL_00a9:  stloc.3
    .try
    {
      IL_00aa:  br.s       IL_00c4

      IL_00ac:  ldloca.s   CS$5$0000
      IL_00ae:  call       instance !0 valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64>::get_Current()
      IL_00b3:  stloc.1
      IL_00b4:  ldstr      "0x{0:x8}"
      IL_00b9:  ldloc.1
      IL_00ba:  box        [mscorlib]System.Int64
      IL_00bf:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_00c4:  ldloca.s   CS$5$0000
      IL_00c6:  call       instance bool valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64>::MoveNext()
      IL_00cb:  brtrue.s   IL_00ac

      IL_00cd:  leave.s    IL_00dd

    }  // end .try
    finally
    {
      IL_00cf:  ldloca.s   CS$5$0000
      IL_00d1:  constrained. valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64>
      IL_00d7:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
      IL_00dc:  endfinally
    }  // end handler
    IL_00dd:  ldc.i4.s   101
    IL_00df:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_00e4:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_00e9:  call       class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0> [System.Linq]System.Linq.Enumerable::Distinct<int64>(class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_00ee:  call       int32 [System.Linq]System.Linq.Enumerable::Count<int64>(class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_00f3:  ldc.i4.1
    IL_00f4:  beq.s      IL_0146

    IL_00f6:  ldstr      "FAILED: The local on the stack for CalleeIsOdd3 sh"
    + "ifted. (should have always the same address)"
    IL_00fb:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0100:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_0105:  callvirt   instance valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<!0> class [mscorlib]System.Collections.Generic.List`1<int64>::GetEnumerator()
    IL_010a:  stloc.s    CS$5$0001
    .try
    {
      IL_010c:  br.s       IL_0126

      IL_010e:  ldloca.s   CS$5$0001
      IL_0110:  call       instance !0 valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64>::get_Current()
      IL_0115:  stloc.2
      IL_0116:  ldstr      "0x{0:x8}"
      IL_011b:  ldloc.2
      IL_011c:  box        [mscorlib]System.Int64
      IL_0121:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_0126:  ldloca.s   CS$5$0001
      IL_0128:  call       instance bool valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64>::MoveNext()
      IL_012d:  brtrue.s   IL_010e

      IL_012f:  leave.s    IL_013f

    }  // end .try
    finally
    {
      IL_0131:  ldloca.s   CS$5$0001
      IL_0133:  constrained. valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64>
      IL_0139:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
      IL_013e:  endfinally
    }  // end handler
    IL_013f:  ldc.i4.s   101
    IL_0141:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_0146:  ldstr      "Execution finished - Test "
    IL_014b:  ldsfld     int32 TailcallVerify.Condition21::Result
    IL_0150:  ldc.i4.s   100
    IL_0152:  beq.s      IL_015b

    IL_0154:  ldstr      "FAILED"
    IL_0159:  br.s       IL_0160

    IL_015b:  ldstr      "PASSED"
    IL_0160:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0165:  call       void [System.Console]System.Console::WriteLine(string)
    IL_016a:  ldsfld     int32 TailcallVerify.Condition21::Result
    IL_016f:  ret
  } // end of method Condition21::Test2

  .method private hidebysig static void  Caller2() cil managed
  {
    // Code size       66 (0x42)
    .maxstack  4
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes CS$0$0000)
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "FAILED: Caller3 was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_002e:  ldc.i4.s   20
    IL_0030:  ldc.i4.1
    IL_0031:  ldc.i4.2
    IL_0032:  ldloca.s   CS$0$0000
    IL_0034:  initobj    TailcallVerify.ValueType3Bytes
    IL_003a:  ldloc.0
    IL_003b:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CalleeIsEven2(int32,
                                                                                                            int32,
                                                                                                            int32,
                                                                                                            valuetype TailcallVerify.ValueType3Bytes)
    IL_0040:  pop
    IL_0041:  ret
  } // end of method Condition21::Caller2

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          CalleeIsOdd2(int32 i,
                       int32 i2,
                       int32 i3,
                       int32 i4,
                       int32 i5,
                       valuetype TailcallVerify.ValueType3Bytes v3,
                       int32 i6,
                       int32 i7,
                       int32 i8,
                       int32 i9,
                       int32 i10,
                       int32 i11,
                       int32 i12) cil managed
  {
    // Code size       116 (0x74)
    .maxstack  4
    .locals init ([0] int32 data,
             [1] int32* pData,
             [2] native int p,
             [3] valuetype TailcallVerify.ValueType3Bytes CS$0$0000)
    IL_0000:  ldloca.s   data
    IL_0002:  conv.u
    IL_0003:  stloc.1
    IL_0004:  ldloca.s   p
    IL_0006:  ldloc.1
    IL_0007:  call       instance void [mscorlib]System.IntPtr::.ctor(void*)
    IL_000c:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_0011:  ldloca.s   p
    IL_0013:  call       instance int64 [mscorlib]System.IntPtr::ToInt64()
    IL_0018:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<int64>::Add(!0)
    IL_001d:  ldc.i4.0
    IL_001e:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0023:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_0028:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_002d:  ldstr      "Callee"
    IL_0032:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_0037:  ldc.i4.m1
    IL_0038:  bne.un.s   IL_004b

    IL_003a:  ldstr      "FAILED: CalleeIsOdd3 was inlined..."
    IL_003f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0044:  ldc.i4.s   101
    IL_0046:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_004b:  ldarg.0
    IL_004c:  brtrue.s   IL_0067

    IL_004e:  ldstr      "IsOdd"
    IL_0053:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0058:  ldc.i4.1
    IL_0059:  ldarg.0
    IL_005a:  div
    IL_005b:  starg.s    i
    IL_005d:  ldloca.s   CS$0$0000
    IL_005f:  initobj    TailcallVerify.ValueType3Bytes
    IL_0065:  ldloc.3
    IL_0066:  ret

    IL_0067:  ldarg.0
    IL_0068:  ldc.i4.1
    IL_0069:  sub
    IL_006a:  ldarg.1
    IL_006b:  ldarg.2
    IL_006c:  ldarg.s    v3
    IL_006e:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CalleeIsEven2(int32,
                                                                                                            int32,
                                                                                                            int32,
                                                                                                            valuetype TailcallVerify.ValueType3Bytes)
    IL_0073:  ret
  } // end of method Condition21::CalleeIsOdd2

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          CalleeIsEven2(int32 i,
                        int32 i2,
                        int32 i3,
                        valuetype TailcallVerify.ValueType3Bytes v3) cil managed
  {
    // Code size       124 (0x7c)
    .maxstack  13
    .locals init ([0] int32 data,
             [1] int32* pData,
             [2] native int p,
             [3] valuetype TailcallVerify.ValueType3Bytes CS$0$0000)
    IL_0000:  ldloca.s   data
    IL_0002:  conv.u
    IL_0003:  stloc.1
    IL_0004:  ldloca.s   p
    IL_0006:  ldloc.1
    IL_0007:  call       instance void [mscorlib]System.IntPtr::.ctor(void*)
    IL_000c:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::evenList
    IL_0011:  ldloca.s   p
    IL_0013:  call       instance int64 [mscorlib]System.IntPtr::ToInt64()
    IL_0018:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<int64>::Add(!0)
    IL_001d:  ldc.i4.0
    IL_001e:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0023:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_0028:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_002d:  ldstr      "Callee"
    IL_0032:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_0037:  ldc.i4.m1
    IL_0038:  bne.un.s   IL_004b

    IL_003a:  ldstr      "FAILED: CalleeIsEven3 was inlined..."
    IL_003f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0044:  ldc.i4.s   101
    IL_0046:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_004b:  ldarg.0
    IL_004c:  brtrue.s   IL_0067

    IL_004e:  ldstr      "IsEven"
    IL_0053:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0058:  ldc.i4.1
    IL_0059:  ldarg.0
    IL_005a:  div
    IL_005b:  starg.s    i
    IL_005d:  ldloca.s   CS$0$0000
    IL_005f:  initobj    TailcallVerify.ValueType3Bytes
    IL_0065:  ldloc.3
    IL_0066:  ret

    IL_0067:  ldarg.0
    IL_0068:  ldc.i4.1
    IL_0069:  sub
    IL_006a:  ldarg.1
    IL_006b:  ldarg.2
    IL_006c:  ldarg.2
    IL_006d:  ldarg.1
    IL_006e:  ldarg.3
    IL_006f:  ldarg.2
    IL_0070:  ldarg.1
    IL_0071:  ldarg.0
    IL_0072:  ldc.i4.3
    IL_0073:  ldc.i4.4
    IL_0074:  ldc.i4.5
    IL_0075:  ldc.i4.6
    IL_0076:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CalleeIsOdd2(int32,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           valuetype TailcallVerify.ValueType3Bytes,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           int32)
    IL_007b:  ret
  } // end of method Condition21::CalleeIsEven2

  .method public hidebysig static int32  Test3() cil managed nooptimization
  {
    // Code size       394 (0x18a)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Threading.Thread t1,
             [1] class [mscorlib]System.Exception e,
             [2] int64 l,
             [3] int64 V_3,
             [4] valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64> CS$5$0000,
             [5] valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64> CS$5$0001)
    IL_0000:  ldstr      "Executing Condition21.Test3 - Caller: Arguments: N"
    + "one - ReturnType: void; CalleeIsOdd3: Arguments: int, int, int, int, 3 "
    + "byte struct - ReturnType: 3 byte struct; CalleeIsEven3: Arguments: int,"
    + " int, int, int, 3 byte struct - ReturnType: 3 byte struct;"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_0011:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::evenList
    IL_0016:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<int64>::Clear()
    IL_001b:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_0020:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<int64>::Clear()
    .try
    {
      IL_0025:  ldnull
      IL_0026:  ldftn      void TailcallVerify.Condition21::Caller3()
      IL_002c:  newobj     instance void [mscorlib]System.Threading.ThreadStart::.ctor(object,
                                                                                       native int)
      IL_0031:  newobj     instance void [mscorlib]System.Threading.Thread::.ctor(class [mscorlib]System.Threading.ThreadStart)
      IL_0036:  stloc.0
      IL_0037:  ldloc.0
      IL_0038:  callvirt   instance void [mscorlib]System.Threading.Thread::Start()
      IL_003d:  ldloc.0
      IL_003e:  callvirt   instance void [mscorlib]System.Threading.Thread::Join()
      IL_0043:  leave.s    IL_009c

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0045:  stloc.1
      IL_0046:  ldloc.1
      IL_0047:  isinst     [mscorlib]System.DivideByZeroException
      IL_004c:  brtrue.s   IL_0057

      IL_004e:  ldc.i4.s   101
      IL_0050:  stsfld     int32 TailcallVerify.Condition21::Result
      IL_0055:  rethrow
      IL_0057:  ldloc.1
      IL_0058:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005d:  ldstr      "Caller"
      IL_0062:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_0067:  ldc.i4.m1
      IL_0068:  beq.s      IL_009a

      IL_006a:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_006f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0074:  ldstr      "------------------------------------------------"
      IL_0079:  call       void [System.Console]System.Console::WriteLine(string)
      IL_007e:  call       void [System.Console]System.Console::WriteLine()
      IL_0083:  ldloc.1
      IL_0084:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0089:  call       void [System.Console]System.Console::WriteLine(string)
      IL_008e:  call       void [System.Console]System.Console::WriteLine()
      IL_0093:  ldc.i4.s   101
      IL_0095:  stsfld     int32 TailcallVerify.Condition21::Result
      IL_009a:  leave.s    IL_009c

    }  // end handler
    IL_009c:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::evenList
    IL_00a1:  call       class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0> [System.Linq]System.Linq.Enumerable::Distinct<int64>(class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_00a6:  call       int32 [System.Linq]System.Linq.Enumerable::Count<int64>(class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_00ab:  ldc.i4.1
    IL_00ac:  beq.s      IL_00fe

    IL_00ae:  ldstr      "FAILED: The local on the stack for CalleeIsEven3 s"
    + "hifted. (should have always the same address)"
    IL_00b3:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00b8:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::evenList
    IL_00bd:  callvirt   instance valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<!0> class [mscorlib]System.Collections.Generic.List`1<int64>::GetEnumerator()
    IL_00c2:  stloc.s    CS$5$0000
    .try
    {
      IL_00c4:  br.s       IL_00de

      IL_00c6:  ldloca.s   CS$5$0000
      IL_00c8:  call       instance !0 valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64>::get_Current()
      IL_00cd:  stloc.2
      IL_00ce:  ldstr      "0x{0:x8}"
      IL_00d3:  ldloc.2
      IL_00d4:  box        [mscorlib]System.Int64
      IL_00d9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_00de:  ldloca.s   CS$5$0000
      IL_00e0:  call       instance bool valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64>::MoveNext()
      IL_00e5:  brtrue.s   IL_00c6

      IL_00e7:  leave.s    IL_00f7

    }  // end .try
    finally
    {
      IL_00e9:  ldloca.s   CS$5$0000
      IL_00eb:  constrained. valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64>
      IL_00f1:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
      IL_00f6:  endfinally
    }  // end handler
    IL_00f7:  ldc.i4.s   101
    IL_00f9:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_00fe:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_0103:  call       class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0> [System.Linq]System.Linq.Enumerable::Distinct<int64>(class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_0108:  call       int32 [System.Linq]System.Linq.Enumerable::Count<int64>(class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_010d:  ldc.i4.1
    IL_010e:  beq.s      IL_0160

    IL_0110:  ldstr      "FAILED: The local on the stack for CalleeIsOdd3 sh"
    + "ifted. (should have always the same address)"
    IL_0115:  call       void [System.Console]System.Console::WriteLine(string)
    IL_011a:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_011f:  callvirt   instance valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<!0> class [mscorlib]System.Collections.Generic.List`1<int64>::GetEnumerator()
    IL_0124:  stloc.s    CS$5$0001
    .try
    {
      IL_0126:  br.s       IL_0140

      IL_0128:  ldloca.s   CS$5$0001
      IL_012a:  call       instance !0 valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64>::get_Current()
      IL_012f:  stloc.3
      IL_0130:  ldstr      "0x{0:x8}"
      IL_0135:  ldloc.3
      IL_0136:  box        [mscorlib]System.Int64
      IL_013b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_0140:  ldloca.s   CS$5$0001
      IL_0142:  call       instance bool valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64>::MoveNext()
      IL_0147:  brtrue.s   IL_0128

      IL_0149:  leave.s    IL_0159

    }  // end .try
    finally
    {
      IL_014b:  ldloca.s   CS$5$0001
      IL_014d:  constrained. valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<int64>
      IL_0153:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
      IL_0158:  endfinally
    }  // end handler
    IL_0159:  ldc.i4.s   101
    IL_015b:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_0160:  ldstr      "Execution finished - Test "
    IL_0165:  ldsfld     int32 TailcallVerify.Condition21::Result
    IL_016a:  ldc.i4.s   100
    IL_016c:  beq.s      IL_0175

    IL_016e:  ldstr      "FAILED"
    IL_0173:  br.s       IL_017a

    IL_0175:  ldstr      "PASSED"
    IL_017a:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_017f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0184:  ldsfld     int32 TailcallVerify.Condition21::Result
    IL_0189:  ret
  } // end of method Condition21::Test3

  .method private hidebysig static void  Caller3() cil managed
  {
    // Code size       73 (0x49)
    .maxstack  6
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes CS$0$0000)
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "FAILED: Caller3 was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition21::Result
    .try
    {
      IL_002e:  ldc.i4.s   20
      IL_0030:  ldc.i4.1
      IL_0031:  ldc.i4.2
      IL_0032:  ldc.i4.3
      IL_0033:  ldc.i4.4
      IL_0034:  ldloca.s   CS$0$0000
      IL_0036:  initobj    TailcallVerify.ValueType3Bytes
      IL_003c:  ldloc.0
      IL_003d:  call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CallerIsOdd3(int32,
                                                                                                             int32,
                                                                                                             int32,
                                                                                                             int32,
                                                                                                             int32,
                                                                                                             valuetype TailcallVerify.ValueType3Bytes)
      IL_0042:  pop
      IL_0043:  leave.s    IL_0048

    }  // end .try
    catch [mscorlib]System.DivideByZeroException 
    {
      IL_0045:  pop
      IL_0046:  leave.s    IL_0048

    }  // end handler
    IL_0048:  ret
  } // end of method Condition21::Caller3

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          CallerIsOdd3(int32 i,
                       int32 i2,
                       int32 i3,
                       int32 i4,
                       int32 i5,
                       valuetype TailcallVerify.ValueType3Bytes v3) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  ldarg.3
    IL_0004:  ldarg.s    i5
    IL_0006:  ldarg.s    v3
    IL_0008:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CalleeIsOdd3(int32,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           valuetype TailcallVerify.ValueType3Bytes)
    IL_000d:  ret
  } // end of method Condition21::CallerIsOdd3

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          CalleeIsOdd3(int32 i,
                       int32 i2,
                       int32 i3,
                       int32 i4,
                       int32 i5,
                       valuetype TailcallVerify.ValueType3Bytes v3) cil managed
  {
    // Code size       109 (0x6d)
    .maxstack  6
    .locals init ([0] int32 data,
             [1] int32* pData,
             [2] native int p,
             [3] valuetype TailcallVerify.ValueType3Bytes CS$0$0000)
    IL_0000:  ldloca.s   data
    IL_0002:  conv.u
    IL_0003:  stloc.1
    IL_0004:  ldloca.s   p
    IL_0006:  ldloc.1
    IL_0007:  call       instance void [mscorlib]System.IntPtr::.ctor(void*)
    IL_000c:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_0011:  ldloca.s   p
    IL_0013:  call       instance int64 [mscorlib]System.IntPtr::ToInt64()
    IL_0018:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<int64>::Add(!0)
    IL_001d:  ldc.i4.0
    IL_001e:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0023:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_0028:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_002d:  ldstr      "Callee"
    IL_0032:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_0037:  ldc.i4.m1
    IL_0038:  bne.un.s   IL_004b

    IL_003a:  ldstr      "FAILED: CalleeIsOdd3 was inlined..."
    IL_003f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0044:  ldc.i4.s   101
    IL_0046:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_004b:  ldarg.0
    IL_004c:  brtrue.s   IL_005d

    IL_004e:  ldc.i4.1
    IL_004f:  ldarg.0
    IL_0050:  div
    IL_0051:  starg.s    i
    IL_0053:  ldloca.s   CS$0$0000
    IL_0055:  initobj    TailcallVerify.ValueType3Bytes
    IL_005b:  ldloc.3
    IL_005c:  ret

    IL_005d:  ldarg.0
    IL_005e:  ldc.i4.1
    IL_005f:  sub
    IL_0060:  ldarg.1
    IL_0061:  ldarg.2
    IL_0062:  ldarg.3
    IL_0063:  ldarg.s    i5
    IL_0065:  ldarg.s    v3
    IL_0067:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CalleeIsEven3(int32,
                                                                                                            int32,
                                                                                                            int32,
                                                                                                            int32,
                                                                                                            int32,
                                                                                                            valuetype TailcallVerify.ValueType3Bytes)
    IL_006c:  ret
  } // end of method Condition21::CalleeIsOdd3

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          CalleeIsEven3(int32 i,
                        int32 i2,
                        int32 i3,
                        int32 i4,
                        int32 i5,
                        valuetype TailcallVerify.ValueType3Bytes v3) cil managed
  {
    // Code size       109 (0x6d)
    .maxstack  6
    .locals init ([0] int32 data,
             [1] int32* pData,
             [2] native int p,
             [3] valuetype TailcallVerify.ValueType3Bytes CS$0$0000)
    IL_0000:  ldloca.s   data
    IL_0002:  conv.u
    IL_0003:  stloc.1
    IL_0004:  ldloca.s   p
    IL_0006:  ldloc.1
    IL_0007:  call       instance void [mscorlib]System.IntPtr::.ctor(void*)
    IL_000c:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::evenList
    IL_0011:  ldloca.s   p
    IL_0013:  call       instance int64 [mscorlib]System.IntPtr::ToInt64()
    IL_0018:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<int64>::Add(!0)
    IL_001d:  ldc.i4.0
    IL_001e:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0023:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_0028:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_002d:  ldstr      "Callee"
    IL_0032:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_0037:  ldc.i4.m1
    IL_0038:  bne.un.s   IL_004b

    IL_003a:  ldstr      "FAILED: CalleeIsEven3 was inlined..."
    IL_003f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0044:  ldc.i4.s   101
    IL_0046:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_004b:  ldarg.0
    IL_004c:  brtrue.s   IL_005d

    IL_004e:  ldc.i4.1
    IL_004f:  ldarg.0
    IL_0050:  div
    IL_0051:  starg.s    i
    IL_0053:  ldloca.s   CS$0$0000
    IL_0055:  initobj    TailcallVerify.ValueType3Bytes
    IL_005b:  ldloc.3
    IL_005c:  ret

    IL_005d:  ldarg.0
    IL_005e:  ldc.i4.1
    IL_005f:  sub
    IL_0060:  ldarg.1
    IL_0061:  ldarg.2
    IL_0062:  ldarg.3
    IL_0063:  ldarg.s    i5
    IL_0065:  ldarg.s    v3
    IL_0067:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CalleeIsOdd3(int32,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           int32,
                                                                                                           valuetype TailcallVerify.ValueType3Bytes)
    IL_006c:  ret
  } // end of method Condition21::CalleeIsEven3

  .method public hidebysig static int32  Test4() cil managed nooptimization
  {
    // Code size       269 (0x10d)
    .maxstack  4
    .locals init ([0] int32 i,
             [1] int32 V_1)
    IL_0000:  ldstr      "Executing Condition21.Test4 - Verifying helper fra"
    + "me location on the stack."
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_0011:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_0016:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<int64>::Clear()
    .try
    {
      IL_001b:  call       void TailcallVerify.Condition21::Caller4()
      IL_0020:  leave.s    IL_0025

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0022:  pop
      IL_0023:  leave.s    IL_0025

    }  // end handler
    IL_0025:  ldc.i4.0
    IL_0026:  stloc.0
    IL_0027:  br.s       IL_006a

    IL_0029:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_002e:  ldloc.0
    IL_002f:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<int64>::get_Item(int32)
    IL_0034:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_0039:  ldloc.0
    IL_003a:  ldc.i4.1
    IL_003b:  add
    IL_003c:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<int64>::get_Item(int32)
    IL_0041:  bne.un.s   IL_005f

    IL_0043:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_0048:  ldloc.0
    IL_0049:  ldc.i4.1
    IL_004a:  add
    IL_004b:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<int64>::get_Item(int32)
    IL_0050:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_0055:  ldloc.0
    IL_0056:  ldc.i4.2
    IL_0057:  add
    IL_0058:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<int64>::get_Item(int32)
    IL_005d:  beq.s      IL_0066

    IL_005f:  ldc.i4.s   101
    IL_0061:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_0066:  ldloc.0
    IL_0067:  ldc.i4.3
    IL_0068:  add
    IL_0069:  stloc.0
    IL_006a:  ldloc.0
    IL_006b:  ldc.i4.2
    IL_006c:  add
    IL_006d:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_0072:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<int64>::get_Count()
    IL_0077:  blt.s      IL_0029

    IL_0079:  ldsfld     int32 TailcallVerify.Condition21::Result
    IL_007e:  ldc.i4.s   100
    IL_0080:  beq.s      IL_00e3

    IL_0082:  ldstr      "The following shows a list of stack addresses of a"
    + " local on the stack."
    IL_0087:  call       void [System.Console]System.Console::WriteLine(string)
    IL_008c:  ldstr      "You should see the same address three times before"
    + " it decreases and repeats again."
    IL_0091:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0096:  ldstr      "This happens because the call chain looks like thi"
    + "s E->E->E->E->O->O->O->E->E->E->E->O->O->O"
    IL_009b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00a0:  ldstr      "where E is a method where tailcalls are not allowe"
    + "d (it's synchronized) and O is a"
    IL_00a5:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00aa:  ldstr      "method that gets tailcall'ed."
    IL_00af:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00b4:  ldc.i4.0
    IL_00b5:  stloc.1
    IL_00b6:  br.s       IL_00d6

    IL_00b8:  ldstr      "0x{0:x8}"
    IL_00bd:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_00c2:  ldloc.1
    IL_00c3:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<int64>::get_Item(int32)
    IL_00c8:  box        [mscorlib]System.Int64
    IL_00cd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_00d2:  ldloc.1
    IL_00d3:  ldc.i4.1
    IL_00d4:  add
    IL_00d5:  stloc.1
    IL_00d6:  ldloc.1
    IL_00d7:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_00dc:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<int64>::get_Count()
    IL_00e1:  blt.s      IL_00b8

    IL_00e3:  ldstr      "Execution finished - Test "
    IL_00e8:  ldsfld     int32 TailcallVerify.Condition21::Result
    IL_00ed:  ldc.i4.s   100
    IL_00ef:  beq.s      IL_00f8

    IL_00f1:  ldstr      "FAILED"
    IL_00f6:  br.s       IL_00fd

    IL_00f8:  ldstr      "PASSED"
    IL_00fd:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0102:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0107:  ldsfld     int32 TailcallVerify.Condition21::Result
    IL_010c:  ret
  } // end of method Condition21::Test4

  .method private hidebysig static void  Caller4() cil managed
  {
    // Code size       71 (0x47)
    .maxstack  6
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes CS$0$0000)
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "FAILED: Caller4 was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_002e:  ldc.i4     0xc8
    IL_0033:  ldc.i4.1
    IL_0034:  ldc.i4.2
    IL_0035:  ldc.i4.3
    IL_0036:  ldc.i4.4
    IL_0037:  ldloca.s   CS$0$0000
    IL_0039:  initobj    TailcallVerify.ValueType3Bytes
    IL_003f:  ldloc.0
    IL_0040:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CalleeE4(int32,
                                                                                                       int32,
                                                                                                       int32,
                                                                                                       int32,
                                                                                                       int32,
                                                                                                       valuetype TailcallVerify.ValueType3Bytes)
    IL_0045:  pop
    IL_0046:  ret
  } // end of method Condition21::Caller4

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          CalleeE4(int32 i,
                   int32 i2,
                   int32 i3,
                   int32 i4,
                   int32 i5,
                   valuetype TailcallVerify.ValueType3Bytes v3) cil managed synchronized noinlining
  {
    // Code size       128 (0x80)
    .maxstack  6
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes CS$0$0000)
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Callee"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "FAILED: CalleeE4 was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_002e:  ldarg.0
    IL_002f:  brtrue.s   IL_0046

    IL_0031:  ldarg.0
    IL_0032:  volatile.
    IL_0034:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition21::zero
    IL_0039:  div
    IL_003a:  starg.s    i
    IL_003c:  ldloca.s   CS$0$0000
    IL_003e:  initobj    TailcallVerify.ValueType3Bytes
    IL_0044:  ldloc.0
    IL_0045:  ret

    IL_0046:  ldsfld     int32 TailcallVerify.Condition21::counterE4
    IL_004b:  ldc.i4.0
    IL_004c:  ble.s      IL_006a

    IL_004e:  ldsfld     int32 TailcallVerify.Condition21::counterE4
    IL_0053:  ldc.i4.1
    IL_0054:  sub
    IL_0055:  stsfld     int32 TailcallVerify.Condition21::counterE4
    IL_005a:  ldarg.0
    IL_005b:  ldc.i4.1
    IL_005c:  sub
    IL_005d:  ldarg.1
    IL_005e:  ldarg.2
    IL_005f:  ldarg.3
    IL_0060:  ldarg.s    i5
    IL_0062:  ldarg.s    v3
    IL_0064:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CalleeE4(int32,
                                                                                                       int32,
                                                                                                       int32,
                                                                                                       int32,
                                                                                                       int32,
                                                                                                       valuetype TailcallVerify.ValueType3Bytes)
    IL_0069:  ret

    IL_006a:  ldc.i4.3
    IL_006b:  stsfld     int32 TailcallVerify.Condition21::counterE4
    IL_0070:  ldarg.0
    IL_0071:  ldc.i4.1
    IL_0072:  sub
    IL_0073:  ldarg.1
    IL_0074:  ldarg.2
    IL_0075:  ldarg.3
    IL_0076:  ldarg.s    i5
    IL_0078:  ldarg.s    v3
    IL_007a:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CalleeO4(int32,
                                                                                                       int32,
                                                                                                       int32,
                                                                                                       int32,
                                                                                                       int32,
                                                                                                       valuetype TailcallVerify.ValueType3Bytes)
    IL_007f:  ret
  } // end of method Condition21::CalleeE4

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          CalleeO4(int32 i,
                   int32 i2,
                   int32 i3,
                   int32 i4,
                   int32 i5,
                   valuetype TailcallVerify.ValueType3Bytes v3) cil managed
  {
    // Code size       157 (0x9d)
    .maxstack  6
    .locals init ([0] int32 data,
             [1] int32* pData,
             [2] native int p,
             [3] valuetype TailcallVerify.ValueType3Bytes CS$0$0000)
    IL_0000:  ldloca.s   data
    IL_0002:  conv.u
    IL_0003:  stloc.1
    IL_0004:  ldloca.s   p
    IL_0006:  ldloc.1
    IL_0007:  call       instance void [mscorlib]System.IntPtr::.ctor(void*)
    IL_000c:  ldsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_0011:  ldloca.s   p
    IL_0013:  call       instance int64 [mscorlib]System.IntPtr::ToInt64()
    IL_0018:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<int64>::Add(!0)
    IL_001d:  ldc.i4.0
    IL_001e:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0023:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_0028:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_002d:  ldstr      "Callee"
    IL_0032:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_0037:  ldc.i4.m1
    IL_0038:  bne.un.s   IL_004b

    IL_003a:  ldstr      "FAILED: CalleeO4 was inlined..."
    IL_003f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0044:  ldc.i4.s   101
    IL_0046:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_004b:  ldarg.0
    IL_004c:  brtrue.s   IL_0063

    IL_004e:  ldarg.0
    IL_004f:  volatile.
    IL_0051:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition21::zero
    IL_0056:  div
    IL_0057:  starg.s    i
    IL_0059:  ldloca.s   CS$0$0000
    IL_005b:  initobj    TailcallVerify.ValueType3Bytes
    IL_0061:  ldloc.3
    IL_0062:  ret

    IL_0063:  ldsfld     int32 TailcallVerify.Condition21::counterO4
    IL_0068:  ldc.i4.0
    IL_0069:  ble.s      IL_0087

    IL_006b:  ldsfld     int32 TailcallVerify.Condition21::counterO4
    IL_0070:  ldc.i4.1
    IL_0071:  sub
    IL_0072:  stsfld     int32 TailcallVerify.Condition21::counterO4
    IL_0077:  ldarg.0
    IL_0078:  ldc.i4.1
    IL_0079:  sub
    IL_007a:  ldarg.1
    IL_007b:  ldarg.2
    IL_007c:  ldarg.3
    IL_007d:  ldarg.s    i5
    IL_007f:  ldarg.s    v3
    IL_0081:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CalleeO4(int32,
                                                                                                       int32,
                                                                                                       int32,
                                                                                                       int32,
                                                                                                       int32,
                                                                                                       valuetype TailcallVerify.ValueType3Bytes)
    IL_0086:  ret

    IL_0087:  ldc.i4.2
    IL_0088:  stsfld     int32 TailcallVerify.Condition21::counterO4
    IL_008d:  ldarg.0
    IL_008e:  ldc.i4.1
    IL_008f:  sub
    IL_0090:  ldarg.1
    IL_0091:  ldarg.2
    IL_0092:  ldarg.3
    IL_0093:  ldarg.s    i5
    IL_0095:  ldarg.s    v3
    IL_0097:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CalleeE4(int32,
                                                                                                       int32,
                                                                                                       int32,
                                                                                                       int32,
                                                                                                       int32,
                                                                                                       valuetype TailcallVerify.ValueType3Bytes)
    IL_009c:  ret
  } // end of method Condition21::CalleeO4

  .method public hidebysig static int32  Test5() cil managed nooptimization
  {
    // Code size       387 (0x183)
    .maxstack  3
    .locals init ([0] class TailcallVerify.Condition21 con1,
             [1] class TailcallVerify.Condition21 con2,
             [2] class [mscorlib]System.Threading.Thread t1,
             [3] class [mscorlib]System.Threading.Thread t2,
             [4] class [mscorlib]System.Exception ex,
             [5] int32 i,
             [6] int32 V_6)
    IL_0000:  ldstr      "Executing Condition21.Test5 - Verifying helper fra"
    + "me location on the stack while using multiple threads."
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_0011:  newobj     instance void TailcallVerify.Condition21::.ctor()
    IL_0016:  stloc.0
    IL_0017:  newobj     instance void TailcallVerify.Condition21::.ctor()
    IL_001c:  stloc.1
    IL_001d:  ldloc.0
    IL_001e:  ldftn      instance void TailcallVerify.Condition21::Caller5()
    IL_0024:  newobj     instance void [mscorlib]System.Threading.ThreadStart::.ctor(object,
                                                                                     native int)
    IL_0029:  newobj     instance void [mscorlib]System.Threading.Thread::.ctor(class [mscorlib]System.Threading.ThreadStart)
    IL_002e:  stloc.2
    IL_002f:  ldloc.1
    IL_0030:  ldftn      instance void TailcallVerify.Condition21::Caller5()
    IL_0036:  newobj     instance void [mscorlib]System.Threading.ThreadStart::.ctor(object,
                                                                                     native int)
    IL_003b:  newobj     instance void [mscorlib]System.Threading.Thread::.ctor(class [mscorlib]System.Threading.ThreadStart)
    IL_0040:  stloc.3
    .try
    {
      IL_0041:  ldloc.2
      IL_0042:  callvirt   instance void [mscorlib]System.Threading.Thread::Start()
      IL_0047:  ldloc.3
      IL_0048:  callvirt   instance void [mscorlib]System.Threading.Thread::Start()
      IL_004d:  ldloc.2
      IL_004e:  callvirt   instance void [mscorlib]System.Threading.Thread::Join()
      IL_0053:  ldloc.3
      IL_0054:  callvirt   instance void [mscorlib]System.Threading.Thread::Join()
      IL_0059:  leave.s    IL_007c

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_005b:  stloc.s    ex
      IL_005d:  ldstr      "FAILED: This exception was NOT expected: "
      IL_0062:  ldloc.s    ex
      IL_0064:  callvirt   instance string [mscorlib]System.Exception::get_Message()
      IL_0069:  call       string [mscorlib]System.String::Concat(string,
                                                                  string)
      IL_006e:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0073:  ldc.i4.s   101
      IL_0075:  stsfld     int32 TailcallVerify.Condition21::Result
      IL_007a:  leave.s    IL_007c

    }  // end handler
    IL_007c:  ldloc.0
    IL_007d:  callvirt   instance void TailcallVerify.Condition21::VerifyThreadList()
    IL_0082:  ldsfld     int32 TailcallVerify.Condition21::Result
    IL_0087:  ldc.i4.s   100
    IL_0089:  bne.un.s   IL_0091

    IL_008b:  ldloc.1
    IL_008c:  callvirt   instance void TailcallVerify.Condition21::VerifyThreadList()
    IL_0091:  ldsfld     int32 TailcallVerify.Condition21::Result
    IL_0096:  ldc.i4.s   100
    IL_0098:  beq        IL_0159

    IL_009d:  ldstr      "The following shows a list of stack addresses of a"
    + " local on the stack."
    IL_00a2:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00a7:  ldstr      "You should see the same address three times before"
    + " it decreases and repeats again."
    IL_00ac:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00b1:  ldstr      "This happens because the call chain looks like thi"
    + "s E->E->E->E->O->O->O->E->E->E->E->O->O->O"
    IL_00b6:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00bb:  ldstr      "where E is a method where tailcalls are not allowe"
    + "d (it's synchronized) and O is a"
    IL_00c0:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00c5:  ldstr      "method that gets tailcall'ed."
    IL_00ca:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00cf:  call       void [System.Console]System.Console::WriteLine()
    IL_00d4:  ldstr      "------ Stack addresses for thread 1. ---------"
    IL_00d9:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00de:  ldc.i4.0
    IL_00df:  stloc.s    i
    IL_00e1:  br.s       IL_0105

    IL_00e3:  ldstr      "0x{0:x8}"
    IL_00e8:  ldloc.0
    IL_00e9:  ldfld      class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::threadList
    IL_00ee:  ldloc.s    i
    IL_00f0:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<int64>::get_Item(int32)
    IL_00f5:  box        [mscorlib]System.Int64
    IL_00fa:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_00ff:  ldloc.s    i
    IL_0101:  ldc.i4.1
    IL_0102:  add
    IL_0103:  stloc.s    i
    IL_0105:  ldloc.s    i
    IL_0107:  ldloc.0
    IL_0108:  ldfld      class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::threadList
    IL_010d:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<int64>::get_Count()
    IL_0112:  blt.s      IL_00e3

    IL_0114:  call       void [System.Console]System.Console::WriteLine()
    IL_0119:  ldstr      "------ Stack addresses for thread 2. ---------"
    IL_011e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0123:  ldc.i4.0
    IL_0124:  stloc.s    V_6
    IL_0126:  br.s       IL_014a

    IL_0128:  ldstr      "0x{0:x8}"
    IL_012d:  ldloc.1
    IL_012e:  ldfld      class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::threadList
    IL_0133:  ldloc.s    V_6
    IL_0135:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<int64>::get_Item(int32)
    IL_013a:  box        [mscorlib]System.Int64
    IL_013f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0144:  ldloc.s    V_6
    IL_0146:  ldc.i4.1
    IL_0147:  add
    IL_0148:  stloc.s    V_6
    IL_014a:  ldloc.s    V_6
    IL_014c:  ldloc.1
    IL_014d:  ldfld      class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::threadList
    IL_0152:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<int64>::get_Count()
    IL_0157:  blt.s      IL_0128

    IL_0159:  ldstr      "Execution finished - Test "
    IL_015e:  ldsfld     int32 TailcallVerify.Condition21::Result
    IL_0163:  ldc.i4.s   100
    IL_0165:  beq.s      IL_016e

    IL_0167:  ldstr      "FAILED"
    IL_016c:  br.s       IL_0173

    IL_016e:  ldstr      "PASSED"
    IL_0173:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0178:  call       void [System.Console]System.Console::WriteLine(string)
    IL_017d:  ldsfld     int32 TailcallVerify.Condition21::Result
    IL_0182:  ret
  } // end of method Condition21::Test5

  .method assembly hidebysig instance void 
          VerifyThreadList() cil managed
  {
    // Code size       90 (0x5a)
    .maxstack  4
    .locals init ([0] int32 i)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0049

    IL_0004:  ldarg.0
    IL_0005:  ldfld      class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::threadList
    IL_000a:  ldloc.0
    IL_000b:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<int64>::get_Item(int32)
    IL_0010:  ldarg.0
    IL_0011:  ldfld      class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::threadList
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  add
    IL_0019:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<int64>::get_Item(int32)
    IL_001e:  bne.un.s   IL_003e

    IL_0020:  ldarg.0
    IL_0021:  ldfld      class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::threadList
    IL_0026:  ldloc.0
    IL_0027:  ldc.i4.1
    IL_0028:  add
    IL_0029:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<int64>::get_Item(int32)
    IL_002e:  ldarg.0
    IL_002f:  ldfld      class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::threadList
    IL_0034:  ldloc.0
    IL_0035:  ldc.i4.2
    IL_0036:  add
    IL_0037:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<int64>::get_Item(int32)
    IL_003c:  beq.s      IL_0045

    IL_003e:  ldc.i4.s   101
    IL_0040:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_0045:  ldloc.0
    IL_0046:  ldc.i4.3
    IL_0047:  add
    IL_0048:  stloc.0
    IL_0049:  ldloc.0
    IL_004a:  ldc.i4.2
    IL_004b:  add
    IL_004c:  ldarg.0
    IL_004d:  ldfld      class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::threadList
    IL_0052:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<int64>::get_Count()
    IL_0057:  blt.s      IL_0004

    IL_0059:  ret
  } // end of method Condition21::VerifyThreadList

  .method private hidebysig instance void 
          Caller5() cil managed
  {
    // Code size       74 (0x4a)
    .maxstack  7
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes CS$0$0000)
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "FAILED: Caller5 was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition21::Result
    .try
    {
      IL_002e:  ldarg.0
      IL_002f:  ldc.i4.s   50
      IL_0031:  ldc.i4.1
      IL_0032:  ldc.i4.2
      IL_0033:  ldc.i4.3
      IL_0034:  ldc.i4.4
      IL_0035:  ldloca.s   CS$0$0000
      IL_0037:  initobj    TailcallVerify.ValueType3Bytes
      IL_003d:  ldloc.0
      IL_003e:  call       instance valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CallerE5(int32,
                                                                                                                  int32,
                                                                                                                  int32,
                                                                                                                  int32,
                                                                                                                  int32,
                                                                                                                  valuetype TailcallVerify.ValueType3Bytes)
      IL_0043:  pop
      IL_0044:  leave.s    IL_0049

    }  // end .try
    catch [mscorlib]System.DivideByZeroException 
    {
      IL_0046:  pop
      IL_0047:  leave.s    IL_0049

    }  // end handler
    IL_0049:  ret
  } // end of method Condition21::Caller5

  .method private hidebysig instance valuetype TailcallVerify.ValueType3Bytes 
          CallerE5(int32 i,
                   int32 i2,
                   int32 i3,
                   int32 i4,
                   int32 i5,
                   valuetype TailcallVerify.ValueType3Bytes v3) cil managed noinlining
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  ldarg.3
    IL_0004:  ldarg.s    i4
    IL_0006:  ldarg.s    i5
    IL_0008:  ldarg.s    v3
    IL_000a:  tail. call       instance valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CalleeE5(int32,
                                                                                                                int32,
                                                                                                                int32,
                                                                                                                int32,
                                                                                                                int32,
                                                                                                                valuetype TailcallVerify.ValueType3Bytes)
    IL_000f:  ret
  } // end of method Condition21::CallerE5

  .method private hidebysig instance valuetype TailcallVerify.ValueType3Bytes 
          CalleeE5(int32 i,
                   int32 i2,
                   int32 i3,
                   int32 i4,
                   int32 i5,
                   valuetype TailcallVerify.ValueType3Bytes v3) cil managed synchronized
  {
    // Code size       147 (0x93)
    .maxstack  7
    .locals init ([0] int32 data,
             [1] int32* pData,
             [2] valuetype TailcallVerify.ValueType3Bytes CS$0$0000)
    IL_0000:  ldloca.s   data
    IL_0002:  conv.u
    IL_0003:  stloc.1
    IL_0004:  ldloc.1
    IL_0005:  newobj     instance void [mscorlib]System.IntPtr::.ctor(void*)
    IL_000a:  pop
    IL_000b:  ldc.i4.0
    IL_000c:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0011:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_0016:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_001b:  ldstr      "Callee"
    IL_0020:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_0025:  ldc.i4.m1
    IL_0026:  bne.un.s   IL_0039

    IL_0028:  ldstr      "FAILED: CalleeE5 was inlined..."
    IL_002d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0032:  ldc.i4.s   101
    IL_0034:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_0039:  ldarg.1
    IL_003a:  brtrue.s   IL_0051

    IL_003c:  ldarg.1
    IL_003d:  volatile.
    IL_003f:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition21::zero
    IL_0044:  div
    IL_0045:  starg.s    i
    IL_0047:  ldloca.s   CS$0$0000
    IL_0049:  initobj    TailcallVerify.ValueType3Bytes
    IL_004f:  ldloc.2
    IL_0050:  ret

    IL_0051:  ldarg.0
    IL_0052:  ldfld      int32 TailcallVerify.Condition21::counterE5
    IL_0057:  ldc.i4.0
    IL_0058:  ble.s      IL_007a

    IL_005a:  ldarg.0
    IL_005b:  dup
    IL_005c:  ldfld      int32 TailcallVerify.Condition21::counterE5
    IL_0061:  ldc.i4.1
    IL_0062:  sub
    IL_0063:  stfld      int32 TailcallVerify.Condition21::counterE5
    IL_0068:  ldarg.0
    IL_0069:  ldarg.1
    IL_006a:  ldc.i4.1
    IL_006b:  sub
    IL_006c:  ldarg.2
    IL_006d:  ldarg.3
    IL_006e:  ldarg.s    i4
    IL_0070:  ldarg.s    i5
    IL_0072:  ldarg.s    v3
    IL_0074:  tail. call       instance valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CalleeE5(int32,
                                                                                                                int32,
                                                                                                                int32,
                                                                                                                int32,
                                                                                                                int32,
                                                                                                                valuetype TailcallVerify.ValueType3Bytes)
    IL_0079:  ret

    IL_007a:  ldarg.0
    IL_007b:  ldc.i4.3
    IL_007c:  stfld      int32 TailcallVerify.Condition21::counterE5
    IL_0081:  ldarg.0
    IL_0082:  ldarg.1
    IL_0083:  ldc.i4.1
    IL_0084:  sub
    IL_0085:  ldarg.2
    IL_0086:  ldarg.3
    IL_0087:  ldarg.s    i4
    IL_0089:  ldarg.s    i5
    IL_008b:  ldarg.s    v3
    IL_008d:  tail. call       instance valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CalleeO5(int32,
                                                                                                                int32,
                                                                                                                int32,
                                                                                                                int32,
                                                                                                                int32,
                                                                                                                valuetype TailcallVerify.ValueType3Bytes)
    IL_0092:  ret
  } // end of method Condition21::CalleeE5

  .method private hidebysig instance valuetype TailcallVerify.ValueType3Bytes 
          CalleeO5(int32 i,
                   int32 i2,
                   int32 i3,
                   int32 i4,
                   int32 i5,
                   valuetype TailcallVerify.ValueType3Bytes v3) cil managed
  {
    // Code size       166 (0xa6)
    .maxstack  7
    .locals init ([0] int32 data,
             [1] int32* pData,
             [2] native int p,
             [3] valuetype TailcallVerify.ValueType3Bytes CS$0$0000)
    IL_0000:  ldloca.s   data
    IL_0002:  conv.u
    IL_0003:  stloc.1
    IL_0004:  ldloca.s   p
    IL_0006:  ldloc.1
    IL_0007:  call       instance void [mscorlib]System.IntPtr::.ctor(void*)
    IL_000c:  ldarg.0
    IL_000d:  ldfld      class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::threadList
    IL_0012:  ldloca.s   p
    IL_0014:  call       instance int64 [mscorlib]System.IntPtr::ToInt64()
    IL_0019:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<int64>::Add(!0)
    IL_001e:  ldc.i4.0
    IL_001f:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0024:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_0029:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_002e:  ldstr      "Callee"
    IL_0033:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_0038:  ldc.i4.m1
    IL_0039:  bne.un.s   IL_004c

    IL_003b:  ldstr      "FAILED: CalleeO5 was inlined..."
    IL_0040:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0045:  ldc.i4.s   101
    IL_0047:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_004c:  ldarg.1
    IL_004d:  brtrue.s   IL_0064

    IL_004f:  ldarg.1
    IL_0050:  volatile.
    IL_0052:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition21::zero
    IL_0057:  div
    IL_0058:  starg.s    i
    IL_005a:  ldloca.s   CS$0$0000
    IL_005c:  initobj    TailcallVerify.ValueType3Bytes
    IL_0062:  ldloc.3
    IL_0063:  ret

    IL_0064:  ldarg.0
    IL_0065:  ldfld      int32 TailcallVerify.Condition21::counterO5
    IL_006a:  ldc.i4.0
    IL_006b:  ble.s      IL_008d

    IL_006d:  ldarg.0
    IL_006e:  dup
    IL_006f:  ldfld      int32 TailcallVerify.Condition21::counterO5
    IL_0074:  ldc.i4.1
    IL_0075:  sub
    IL_0076:  stfld      int32 TailcallVerify.Condition21::counterO5
    IL_007b:  ldarg.0
    IL_007c:  ldarg.1
    IL_007d:  ldc.i4.1
    IL_007e:  sub
    IL_007f:  ldarg.2
    IL_0080:  ldarg.3
    IL_0081:  ldarg.s    i4
    IL_0083:  ldarg.s    i5
    IL_0085:  ldarg.s    v3
    IL_0087:  tail. call       instance valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CalleeO5(int32,
                                                                                                                int32,
                                                                                                                int32,
                                                                                                                int32,
                                                                                                                int32,
                                                                                                                valuetype TailcallVerify.ValueType3Bytes)
    IL_008c:  ret

    IL_008d:  ldarg.0
    IL_008e:  ldc.i4.2
    IL_008f:  stfld      int32 TailcallVerify.Condition21::counterO5
    IL_0094:  ldarg.0
    IL_0095:  ldarg.1
    IL_0096:  ldc.i4.1
    IL_0097:  sub
    IL_0098:  ldarg.2
    IL_0099:  ldarg.3
    IL_009a:  ldarg.s    i4
    IL_009c:  ldarg.s    i5
    IL_009e:  ldarg.s    v3
    IL_00a0:  tail. call       instance valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition21::CalleeE5(int32,
                                                                                                                int32,
                                                                                                                int32,
                                                                                                                int32,
                                                                                                                int32,
                                                                                                                valuetype TailcallVerify.ValueType3Bytes)
    IL_00a5:  ret
  } // end of method Condition21::CalleeO5

  .method public hidebysig static int32  Test6() cil managed nooptimization
  {
    // Code size       153 (0x99)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition21.Test6  - Caller: Arguments: "
    + "None - ReturnType: void; Callee: Arguments: None - ReturnType: void"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition21::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition21::Caller6()
      IL_0016:  leave.s    IL_006f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  isinst     [mscorlib]System.DivideByZeroException
      IL_001f:  brtrue.s   IL_002a

      IL_0021:  ldc.i4.s   101
      IL_0023:  stsfld     int32 TailcallVerify.Condition21::Result
      IL_0028:  rethrow
      IL_002a:  ldloc.0
      IL_002b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0030:  ldstr      "Caller"
      IL_0035:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003a:  ldc.i4.m1
      IL_003b:  beq.s      IL_006d

      IL_003d:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0042:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0047:  ldstr      "------------------------------------------------"
      IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0051:  call       void [System.Console]System.Console::WriteLine()
      IL_0056:  ldloc.0
      IL_0057:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0061:  call       void [System.Console]System.Console::WriteLine()
      IL_0066:  ldc.i4.s   101
      IL_0068:  stsfld     int32 TailcallVerify.Condition21::Result
      IL_006d:  leave.s    IL_006f

    }  // end handler
    IL_006f:  ldstr      "Execution finished - Test "
    IL_0074:  ldsfld     int32 TailcallVerify.Condition21::Result
    IL_0079:  ldc.i4.s   100
    IL_007b:  beq.s      IL_0084

    IL_007d:  ldstr      "FAILED"
    IL_0082:  br.s       IL_0089

    IL_0084:  ldstr      "PASSED"
    IL_0089:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0093:  ldsfld     int32 TailcallVerify.Condition21::Result
    IL_0098:  ret
  } // end of method Condition21::Test6

  .method private hidebysig static void  Caller6() cil managed
  {
    // Code size       52 (0x34)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_002e:  ldftn        void TailcallVerify.Condition21::Callee6()
    tail. calli       void()
    IL_0033:  ret
  } // end of method Condition21::Caller6

  .method private hidebysig static void  Callee6() cil managed noinlining
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition21::zero
    IL_0008:  div
    IL_0009:  pop
    IL_000a:  ret
  } // end of method Condition21::Callee6

  .method public hidebysig static int32  Test7() cil managed nooptimization
  {
    // Code size       155 (0x9b)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition21.Test7  - Caller: Arguments: "
    + "Int32 - ReturnType: Int32; Callee: Arguments: Int32 - ReturnType: Int32"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition21::Result
    .try
    {
      IL_0011:  ldc.i4.0
      IL_0012:  call       int32 TailcallVerify.Condition21::Caller7(int32)
      IL_0017:  pop
      IL_0018:  leave.s    IL_0071

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_001a:  stloc.0
      IL_001b:  ldloc.0
      IL_001c:  isinst     [mscorlib]System.DivideByZeroException
      IL_0021:  brtrue.s   IL_002c

      IL_0023:  ldc.i4.s   101
      IL_0025:  stsfld     int32 TailcallVerify.Condition21::Result
      IL_002a:  rethrow
      IL_002c:  ldloc.0
      IL_002d:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0032:  ldstr      "Caller"
      IL_0037:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003c:  ldc.i4.m1
      IL_003d:  beq.s      IL_006f

      IL_003f:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0044:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0049:  ldstr      "------------------------------------------------"
      IL_004e:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0053:  call       void [System.Console]System.Console::WriteLine()
      IL_0058:  ldloc.0
      IL_0059:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005e:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0063:  call       void [System.Console]System.Console::WriteLine()
      IL_0068:  ldc.i4.s   101
      IL_006a:  stsfld     int32 TailcallVerify.Condition21::Result
      IL_006f:  leave.s    IL_0071

    }  // end handler
    IL_0071:  ldstr      "Execution finished - Test "
    IL_0076:  ldsfld     int32 TailcallVerify.Condition21::Result
    IL_007b:  ldc.i4.s   100
    IL_007d:  beq.s      IL_0086

    IL_007f:  ldstr      "FAILED"
    IL_0084:  br.s       IL_008b

    IL_0086:  ldstr      "PASSED"
    IL_008b:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0090:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0095:  ldsfld     int32 TailcallVerify.Condition21::Result
    IL_009a:  ret
  } // end of method Condition21::Test7

  .method private hidebysig static int32 
          Caller7(int32 i1) cil managed
  {
    // Code size       53 (0x35)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_002e:  ldarg.0
    IL_002f:  ldftn        int32 TailcallVerify.Condition21::Callee7(int32)
    tail. calli       int32(int32)
    IL_0034:  ret
  } // end of method Condition21::Caller7

  .method private hidebysig static int32 
          Callee7(int32 i1) cil managed noinlining
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition21::zero
    IL_0008:  div
    IL_0009:  pop
    IL_000a:  ldarg.0
    IL_000b:  ret
  } // end of method Condition21::Callee7

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       32 (0x20)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<int64>::.ctor()
    IL_0006:  stfld      class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::threadList
    IL_000b:  ldarg.0
    IL_000c:  ldc.i4.3
    IL_000d:  stfld      int32 TailcallVerify.Condition21::counterE5
    IL_0012:  ldarg.0
    IL_0013:  ldc.i4.2
    IL_0014:  stfld      int32 TailcallVerify.Condition21::counterO5
    IL_0019:  ldarg.0
    IL_001a:  call       instance void [mscorlib]System.Object::.ctor()
    IL_001f:  ret
  } // end of method Condition21::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       48 (0x30)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  volatile.
    IL_0003:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition21::zero
    IL_0008:  ldc.i4.s   100
    IL_000a:  stsfld     int32 TailcallVerify.Condition21::Result
    IL_000f:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<int64>::.ctor()
    IL_0014:  stsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::evenList
    IL_0019:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<int64>::.ctor()
    IL_001e:  stsfld     class [mscorlib]System.Collections.Generic.List`1<int64> TailcallVerify.Condition21::oddList
    IL_0023:  ldc.i4.3
    IL_0024:  stsfld     int32 TailcallVerify.Condition21::counterE4
    IL_0029:  ldc.i4.2
    IL_002a:  stsfld     int32 TailcallVerify.Condition21::counterO4
    IL_002f:  ret
  } // end of method Condition21::.cctor

} // end of class TailcallVerify.Condition21

.class private auto ansi beforefieldinit TailcallVerify.Condition10
       extends [mscorlib]System.Object
{
  .class auto ansi nested private beforefieldinit Foo1`2<V,K>
         extends [mscorlib]System.Object
  {
    .field private int32 zero
    .method public hidebysig instance void 
            Test1() cil managed nooptimization
    {
      // Code size       91 (0x5b)
      .maxstack  2
      .locals init ([0] class [mscorlib]System.Exception e)
      .try
      {
        IL_0000:  ldarg.0
        IL_0001:  call       instance void class TailcallVerify.Condition10/Foo1`2<!V,!K>::Caller1()
        IL_0006:  leave.s    IL_005a

      }  // end .try
      catch [mscorlib]System.Exception 
      {
        IL_0008:  stloc.0
        IL_0009:  ldloc.0
        IL_000a:  isinst     [mscorlib]System.DivideByZeroException
        IL_000f:  brtrue.s   IL_001a

        IL_0011:  ldc.i4.s   101
        IL_0013:  stsfld     int32 TailcallVerify.Condition10::Result
        IL_0018:  rethrow
        IL_001a:  ldloc.0
        IL_001b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
        IL_0020:  ldstr      "Caller"
        IL_0025:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
        IL_002a:  ldc.i4.m1
        IL_002b:  beq.s      IL_0058

        IL_002d:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
        IL_0032:  call       void [System.Console]System.Console::WriteLine(string)
        IL_0037:  ldstr      "------------------------------------------------"
        IL_003c:  call       void [System.Console]System.Console::WriteLine(string)
        IL_0041:  call       void [System.Console]System.Console::WriteLine()
        IL_0046:  ldloc.0
        IL_0047:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
        IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
        IL_0051:  call       void [System.Console]System.Console::WriteLine()
        IL_0056:  rethrow
        IL_0058:  leave.s    IL_005a

      }  // end handler
      IL_005a:  ret
    } // end of method Foo1`2::Test1

    .method public hidebysig instance void 
            Test1Recursive() cil managed nooptimization
    {
      // Code size       91 (0x5b)
      .maxstack  2
      .locals init ([0] class [mscorlib]System.Exception e)
      .try
      {
        IL_0000:  ldarg.0
        IL_0001:  call       instance void class TailcallVerify.Condition10/Foo1`2<!V,!K>::Caller1Recursive()
        IL_0006:  leave.s    IL_005a

      }  // end .try
      catch [mscorlib]System.Exception 
      {
        IL_0008:  stloc.0
        IL_0009:  ldloc.0
        IL_000a:  isinst     [mscorlib]System.DivideByZeroException
        IL_000f:  brtrue.s   IL_001a

        IL_0011:  ldc.i4.s   101
        IL_0013:  stsfld     int32 TailcallVerify.Condition10::Result
        IL_0018:  rethrow
        IL_001a:  ldloc.0
        IL_001b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
        IL_0020:  ldstr      "Caller"
        IL_0025:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
        IL_002a:  ldc.i4.m1
        IL_002b:  beq.s      IL_0058

        IL_002d:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
        IL_0032:  call       void [System.Console]System.Console::WriteLine(string)
        IL_0037:  ldstr      "------------------------------------------------"
        IL_003c:  call       void [System.Console]System.Console::WriteLine(string)
        IL_0041:  call       void [System.Console]System.Console::WriteLine()
        IL_0046:  ldloc.0
        IL_0047:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
        IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
        IL_0051:  call       void [System.Console]System.Console::WriteLine()
        IL_0056:  rethrow
        IL_0058:  leave.s    IL_005a

      }  // end handler
      IL_005a:  ret
    } // end of method Foo1`2::Test1Recursive

    .method public hidebysig instance void 
            Caller1() cil managed
    {
      // Code size       53 (0x35)
      .maxstack  8
      IL_0000:  ldc.i4.0
      IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
      IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
      IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
      IL_0010:  ldstr      "Caller"
      IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_001a:  ldc.i4.m1
      IL_001b:  bne.un.s   IL_002e

      IL_001d:  ldstr      "Failed, Method was inlined..."
      IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0027:  ldc.i4.s   101
      IL_0029:  stsfld     int32 TailcallVerify.Condition10::Result
      IL_002e:  ldarg.0
      IL_002f:  call       instance void class TailcallVerify.Condition10/Foo1`2<!V,!K>::Callee1()
      IL_0034:  ret
    } // end of method Foo1`2::Caller1

    .method public hidebysig instance void 
            Caller1Recursive() cil managed
    {
      // Code size       55 (0x37)
      .maxstack  8
      IL_0000:  ldc.i4.0
      IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
      IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
      IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
      IL_0010:  ldstr      "Caller"
      IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_001a:  ldc.i4.m1
      IL_001b:  bne.un.s   IL_002e

      IL_001d:  ldstr      "Failed, Method was inlined..."
      IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0027:  ldc.i4.s   101
      IL_0029:  stsfld     int32 TailcallVerify.Condition10::Result
      IL_002e:  ldarg.0
      IL_002f:  ldc.i4.s   20
      IL_0031:  call       instance void class TailcallVerify.Condition10/Foo1`2<!V,!K>::Callee1Recursive(int32)
      IL_0036:  ret
    } // end of method Foo1`2::Caller1Recursive

    .method private hidebysig instance void 
            Callee1() cil managed noinlining
    {
      // Code size       10 (0xa)
      .maxstack  8
      IL_0000:  ldc.i4.1
      IL_0001:  ldarg.0
      IL_0002:  ldfld      int32 class TailcallVerify.Condition10/Foo1`2<!V,!K>::zero
      IL_0007:  div
      IL_0008:  pop
      IL_0009:  ret
    } // end of method Foo1`2::Callee1

    .method private hidebysig instance void 
            Callee1Recursive(int32 i) cil managed
    {
      // Code size       93 (0x5d)
      .maxstack  3
      .locals init ([0] string stackTrace,
               [1] int32 count,
               [2] int32 idx)
      IL_0000:  newobj     instance void [mscorlib]System.Diagnostics.StackTrace::.ctor()
      IL_0005:  callvirt   instance string [mscorlib]System.Object::ToString()
      IL_000a:  stloc.0
      IL_000b:  ldc.i4.0
      IL_000c:  stloc.1
      IL_000d:  ldc.i4.0
      IL_000e:  stloc.2
      IL_000f:  br.s       IL_0019

      IL_0011:  ldloc.2
      IL_0012:  ldc.i4.1
      IL_0013:  add
      IL_0014:  stloc.2
      IL_0015:  ldloc.1
      IL_0016:  ldc.i4.1
      IL_0017:  add
      IL_0018:  stloc.1
      IL_0019:  ldloc.0
      IL_001a:  ldstr      "Callee"
      IL_001f:  ldloc.2
      IL_0020:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string,
                                                                           int32)
      IL_0025:  dup
      IL_0026:  stloc.2
      IL_0027:  ldc.i4.m1
      IL_0028:  bne.un.s   IL_0011

      IL_002a:  ldloc.1
      IL_002b:  ldc.i4.1
      IL_002c:  beq.s      IL_0045

      IL_002e:  ldstr      "FAILED: Callee1Recursive was not tailcall'ed..."
      IL_0033:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0038:  ldloc.0
      IL_0039:  call       void [System.Console]System.Console::WriteLine(string)
      IL_003e:  ldc.i4.s   101
      IL_0040:  stsfld     int32 TailcallVerify.Condition10::Result
      IL_0045:  ldarg.1
      IL_0046:  brtrue.s   IL_0053

      IL_0048:  ldarg.1
      IL_0049:  ldarg.0
      IL_004a:  ldfld      int32 class TailcallVerify.Condition10/Foo1`2<!V,!K>::zero
      IL_004f:  div
      IL_0050:  starg.s    i
      IL_0052:  ret

      IL_0053:  ldarg.0
      IL_0054:  ldarg.1
      IL_0055:  ldc.i4.1
      IL_0056:  sub
      IL_0057:  call       instance void class TailcallVerify.Condition10/Foo1`2<!V,!K>::Callee1Recursive(int32)
      IL_005c:  ret
    } // end of method Foo1`2::Callee1Recursive

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Foo1`2::.ctor

  } // end of class Foo1`2

  .class auto ansi nested private beforefieldinit Foo2`2<V,K>
         extends [mscorlib]System.Object
  {
    .field private int32 zero
    .method public hidebysig instance void 
            Test1() cil managed nooptimization
    {
      // Code size       109 (0x6d)
      .maxstack  3
      .locals init ([0] !V arg1,
               [1] !K arg2,
               [2] class [mscorlib]System.Exception e)
      .try
      {
        IL_0000:  ldloca.s   arg1
        IL_0002:  initobj    !V
        IL_0008:  ldloca.s   arg2
        IL_000a:  initobj    !K
        IL_0010:  ldarg.0
        IL_0011:  ldloc.0
        IL_0012:  ldloc.1
        IL_0013:  call       instance void class TailcallVerify.Condition10/Foo2`2<!V,!K>::Caller1(!0,
                                                                                                   !1)
        IL_0018:  leave.s    IL_006c

      }  // end .try
      catch [mscorlib]System.Exception 
      {
        IL_001a:  stloc.2
        IL_001b:  ldloc.2
        IL_001c:  isinst     [mscorlib]System.DivideByZeroException
        IL_0021:  brtrue.s   IL_002c

        IL_0023:  ldc.i4.s   101
        IL_0025:  stsfld     int32 TailcallVerify.Condition10::Result
        IL_002a:  rethrow
        IL_002c:  ldloc.2
        IL_002d:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
        IL_0032:  ldstr      "Caller"
        IL_0037:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
        IL_003c:  ldc.i4.m1
        IL_003d:  beq.s      IL_006a

        IL_003f:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
        IL_0044:  call       void [System.Console]System.Console::WriteLine(string)
        IL_0049:  ldstr      "------------------------------------------------"
        IL_004e:  call       void [System.Console]System.Console::WriteLine(string)
        IL_0053:  call       void [System.Console]System.Console::WriteLine()
        IL_0058:  ldloc.2
        IL_0059:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
        IL_005e:  call       void [System.Console]System.Console::WriteLine(string)
        IL_0063:  call       void [System.Console]System.Console::WriteLine()
        IL_0068:  rethrow
        IL_006a:  leave.s    IL_006c

      }  // end handler
      IL_006c:  ret
    } // end of method Foo2`2::Test1

    .method public hidebysig instance void 
            Caller1(!V arg1,
                    !K arg2) cil managed
    {
      // Code size       54 (0x36)
      .maxstack  8
      IL_0000:  ldc.i4.0
      IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
      IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
      IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
      IL_0010:  ldstr      "Caller"
      IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_001a:  ldc.i4.m1
      IL_001b:  bne.un.s   IL_002e

      IL_001d:  ldstr      "Failed, Method was inlined..."
      IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0027:  ldc.i4.s   101
      IL_0029:  stsfld     int32 TailcallVerify.Condition10::Result
      IL_002e:  ldarg.0
      IL_002f:  ldarg.1
      IL_0030:  call       instance void class TailcallVerify.Condition10/Foo2`2<!V,!K>::Callee1(!0)
      IL_0035:  ret
    } // end of method Foo2`2::Caller1

    .method private hidebysig instance void 
            Callee1(!V arg1) cil managed noinlining
    {
      // Code size       10 (0xa)
      .maxstack  8
      IL_0000:  ldc.i4.1
      IL_0001:  ldarg.0
      IL_0002:  ldfld      int32 class TailcallVerify.Condition10/Foo2`2<!V,!K>::zero
      IL_0007:  div
      IL_0008:  pop
      IL_0009:  ret
    } // end of method Foo2`2::Callee1

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Foo2`2::.ctor

  } // end of class Foo2`2

  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .field private static int32 Result
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       153 (0x99)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition10.Test1 - Caller<string>: Argu"
    + "ments: None - ReturnType: void; Callee: Arguments: None - ReturnType: v"
    + "oid"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition10::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition10::Caller1<string>()
      IL_0016:  leave.s    IL_006f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  isinst     [mscorlib]System.DivideByZeroException
      IL_001f:  brtrue.s   IL_002a

      IL_0021:  ldc.i4.s   101
      IL_0023:  stsfld     int32 TailcallVerify.Condition10::Result
      IL_0028:  rethrow
      IL_002a:  ldloc.0
      IL_002b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0030:  ldstr      "Caller"
      IL_0035:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003a:  ldc.i4.m1
      IL_003b:  beq.s      IL_006d

      IL_003d:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0042:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0047:  ldstr      "------------------------------------------------"
      IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0051:  call       void [System.Console]System.Console::WriteLine()
      IL_0056:  ldloc.0
      IL_0057:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0061:  call       void [System.Console]System.Console::WriteLine()
      IL_0066:  ldc.i4.s   101
      IL_0068:  stsfld     int32 TailcallVerify.Condition10::Result
      IL_006d:  leave.s    IL_006f

    }  // end handler
    IL_006f:  ldstr      "Execution finished - Test "
    IL_0074:  ldsfld     int32 TailcallVerify.Condition10::Result
    IL_0079:  ldc.i4.s   100
    IL_007b:  beq.s      IL_0084

    IL_007d:  ldstr      "FAILED"
    IL_0082:  br.s       IL_0089

    IL_0084:  ldstr      "PASSED"
    IL_0089:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0093:  ldsfld     int32 TailcallVerify.Condition10::Result
    IL_0098:  ret
  } // end of method Condition10::Test1

  .method private hidebysig static void  Caller1<T>() cil managed
  {
    // Code size       52 (0x34)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition10::Result
    IL_002e:  tail. call       void TailcallVerify.Condition10::Callee1()
    IL_0033:  ret
  } // end of method Condition10::Caller1

  .method private hidebysig static void  Callee1() cil managed noinlining
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition10::zero
    IL_0008:  div
    IL_0009:  pop
    IL_000a:  ret
  } // end of method Condition10::Callee1

  .method public hidebysig static int32  Test2() cil managed nooptimization
  {
    // Code size       153 (0x99)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition10.Test2 - Caller: Arguments: N"
    + "one - ReturnType: void; Callee<string>: Arguments: None - ReturnType: v"
    + "oid"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition10::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition10::Caller2()
      IL_0016:  leave.s    IL_006f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  isinst     [mscorlib]System.DivideByZeroException
      IL_001f:  brtrue.s   IL_002a

      IL_0021:  ldc.i4.s   101
      IL_0023:  stsfld     int32 TailcallVerify.Condition10::Result
      IL_0028:  rethrow
      IL_002a:  ldloc.0
      IL_002b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0030:  ldstr      "Caller"
      IL_0035:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003a:  ldc.i4.m1
      IL_003b:  beq.s      IL_006d

      IL_003d:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0042:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0047:  ldstr      "------------------------------------------------"
      IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0051:  call       void [System.Console]System.Console::WriteLine()
      IL_0056:  ldloc.0
      IL_0057:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0061:  call       void [System.Console]System.Console::WriteLine()
      IL_0066:  ldc.i4.s   101
      IL_0068:  stsfld     int32 TailcallVerify.Condition10::Result
      IL_006d:  leave.s    IL_006f

    }  // end handler
    IL_006f:  ldstr      "Execution finished - Test "
    IL_0074:  ldsfld     int32 TailcallVerify.Condition10::Result
    IL_0079:  ldc.i4.s   100
    IL_007b:  beq.s      IL_0084

    IL_007d:  ldstr      "FAILED"
    IL_0082:  br.s       IL_0089

    IL_0084:  ldstr      "PASSED"
    IL_0089:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0093:  ldsfld     int32 TailcallVerify.Condition10::Result
    IL_0098:  ret
  } // end of method Condition10::Test2

  .method private hidebysig static void  Caller2() cil managed
  {
    // Code size       52 (0x34)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition10::Result
    IL_002e:  call       void TailcallVerify.Condition10::Callee2<string>()
    IL_0033:  ret
  } // end of method Condition10::Caller2

  .method private hidebysig static void  Callee2<T>() cil managed noinlining
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition10::zero
    IL_0008:  div
    IL_0009:  pop
    IL_000a:  ret
  } // end of method Condition10::Callee2

  .method public hidebysig static int32  Test3() cil managed nooptimization
  {
    // Code size       170 (0xaa)
    .maxstack  3
    .locals init ([0] class TailcallVerify.Condition10/Foo1`2<string,int32> foo1,
             [1] class TailcallVerify.Condition10/Foo1`2<string,int32> foo2,
             [2] class TailcallVerify.Condition10/Foo1`2<string,int16> foo3,
             [3] class TailcallVerify.Condition10/Foo1`2<string,valuetype TailcallVerify.ValueType3Bytes> foo4,
             [4] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition10.Test3 - class Foo1<string, i"
    + "nt>: Caller: Arguments: None - ReturnType: void; Callee: Arguments: Non"
    + "e - ReturnType: void"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldstr      "                              class Foo1<string, i"
    + "nt>: Caller: Arguments: None - ReturnType: void; Callee: Arguments: Non"
    + "e - ReturnType: void"
    IL_000f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0014:  ldstr      "                              class Foo1<string, s"
    + "hort>: Caller: Arguments: None - ReturnType: void; Callee: Arguments: N"
    + "one - ReturnType: void"
    IL_0019:  call       void [System.Console]System.Console::WriteLine(string)
    IL_001e:  ldstr      "                              class Foo1<string, V"
    + "alueType3Bytes>: Caller: Arguments: None - ReturnType: void; Callee: Ar"
    + "guments: None - ReturnType: void"
    IL_0023:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0028:  ldc.i4.s   100
    IL_002a:  stsfld     int32 TailcallVerify.Condition10::Result
    .try
    {
      IL_002f:  newobj     instance void class TailcallVerify.Condition10/Foo1`2<string,int32>::.ctor()
      IL_0034:  stloc.0
      IL_0035:  newobj     instance void class TailcallVerify.Condition10/Foo1`2<string,int32>::.ctor()
      IL_003a:  stloc.1
      IL_003b:  newobj     instance void class TailcallVerify.Condition10/Foo1`2<string,int16>::.ctor()
      IL_0040:  stloc.2
      IL_0041:  newobj     instance void class TailcallVerify.Condition10/Foo1`2<string,valuetype TailcallVerify.ValueType3Bytes>::.ctor()
      IL_0046:  stloc.3
      IL_0047:  ldloc.0
      IL_0048:  callvirt   instance void class TailcallVerify.Condition10/Foo1`2<string,int32>::Test1()
      IL_004d:  ldloc.1
      IL_004e:  callvirt   instance void class TailcallVerify.Condition10/Foo1`2<string,int32>::Test1()
      IL_0053:  ldloc.2
      IL_0054:  callvirt   instance void class TailcallVerify.Condition10/Foo1`2<string,int16>::Test1()
      IL_0059:  ldloc.3
      IL_005a:  callvirt   instance void class TailcallVerify.Condition10/Foo1`2<string,valuetype TailcallVerify.ValueType3Bytes>::Test1()
      IL_005f:  leave.s    IL_0080

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0061:  stloc.s    e
      IL_0063:  ldloc.s    e
      IL_0065:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_006a:  ldstr      "Caller"
      IL_006f:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_0074:  ldc.i4.m1
      IL_0075:  beq.s      IL_007e

      IL_0077:  ldc.i4.s   101
      IL_0079:  stsfld     int32 TailcallVerify.Condition10::Result
      IL_007e:  leave.s    IL_0080

    }  // end handler
    IL_0080:  ldstr      "Execution finished - Test "
    IL_0085:  ldsfld     int32 TailcallVerify.Condition10::Result
    IL_008a:  ldc.i4.s   100
    IL_008c:  beq.s      IL_0095

    IL_008e:  ldstr      "FAILED"
    IL_0093:  br.s       IL_009a

    IL_0095:  ldstr      "PASSED"
    IL_009a:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_009f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00a4:  ldsfld     int32 TailcallVerify.Condition10::Result
    IL_00a9:  ret
  } // end of method Condition10::Test3

  .method public hidebysig static int32  Test4() cil managed nooptimization
  {
    // Code size       161 (0xa1)
    .maxstack  3
    .locals init ([0] class TailcallVerify.Condition10/Foo2`2<string,int32> foo1,
             [1] class TailcallVerify.Condition10/Foo2`2<string,int32> foo2,
             [2] class TailcallVerify.Condition10/Foo2`2<string,int16> foo3,
             [3] class TailcallVerify.Condition10/Foo2`2<string,valuetype TailcallVerify.ValueType3Bytes> foo4,
             [4] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition10.Test4 - class Foo2<string, i"
    + "nt>: Caller: Arguments: string, int - ReturnType: void; Callee: Argumen"
    + "ts: string - ReturnType: void"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldstr      "                              class Foo2<string, i"
    + "nt>: Caller: Arguments: string, int - ReturnType: void; Callee: Argumen"
    + "ts: string - ReturnType: void"
    IL_000f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0014:  ldstr      "                              class Foo2<string, s"
    + "hort>: Caller: Arguments: string, short - ReturnType: void; Callee: Arg"
    + "uments: string - ReturnType: void"
    IL_0019:  call       void [System.Console]System.Console::WriteLine(string)
    IL_001e:  ldstr      "                              class Foo2<string, V"
    + "alueType3Bytes>: Caller: Arguments: string, ValueType3Bytes - ReturnTyp"
    + "e: void; Callee: Arguments: string - ReturnType: void"
    IL_0023:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0028:  ldc.i4.s   100
    IL_002a:  stsfld     int32 TailcallVerify.Condition10::Result
    .try
    {
      IL_002f:  newobj     instance void class TailcallVerify.Condition10/Foo2`2<string,int32>::.ctor()
      IL_0034:  stloc.0
      IL_0035:  newobj     instance void class TailcallVerify.Condition10/Foo2`2<string,int32>::.ctor()
      IL_003a:  stloc.1
      IL_003b:  newobj     instance void class TailcallVerify.Condition10/Foo2`2<string,int16>::.ctor()
      IL_0040:  stloc.2
      IL_0041:  newobj     instance void class TailcallVerify.Condition10/Foo2`2<string,valuetype TailcallVerify.ValueType3Bytes>::.ctor()
      IL_0046:  stloc.3
      IL_0047:  ldloc.0
      IL_0048:  callvirt   instance void class TailcallVerify.Condition10/Foo2`2<string,int32>::Test1()
      IL_004d:  ldloc.1
      IL_004e:  callvirt   instance void class TailcallVerify.Condition10/Foo2`2<string,int32>::Test1()
      IL_0053:  ldloc.2
      IL_0054:  callvirt   instance void class TailcallVerify.Condition10/Foo2`2<string,int16>::Test1()
      IL_0059:  ldloc.3
      IL_005a:  callvirt   instance void class TailcallVerify.Condition10/Foo2`2<string,valuetype TailcallVerify.ValueType3Bytes>::Test1()
      IL_005f:  leave.s    IL_0077

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0061:  stloc.s    e
      IL_0063:  ldc.i4.s   101
      IL_0065:  stsfld     int32 TailcallVerify.Condition10::Result
      IL_006a:  ldloc.s    e
      IL_006c:  isinst     [mscorlib]System.DivideByZeroException
      IL_0071:  brtrue.s   IL_0075

      IL_0073:  rethrow
      IL_0075:  leave.s    IL_0077

    }  // end handler
    IL_0077:  ldstr      "Execution finished - Test "
    IL_007c:  ldsfld     int32 TailcallVerify.Condition10::Result
    IL_0081:  ldc.i4.s   100
    IL_0083:  beq.s      IL_008c

    IL_0085:  ldstr      "FAILED"
    IL_008a:  br.s       IL_0091

    IL_008c:  ldstr      "PASSED"
    IL_0091:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0096:  call       void [System.Console]System.Console::WriteLine(string)
    IL_009b:  ldsfld     int32 TailcallVerify.Condition10::Result
    IL_00a0:  ret
  } // end of method Condition10::Test4

  .method public hidebysig static int32  Test5() cil managed nooptimization
  {
    // Code size       200 (0xc8)
    .maxstack  3
    .locals init ([0] class TailcallVerify.Condition10/Foo1`2<string,int32> foo1,
             [1] class TailcallVerify.Condition10/Foo1`2<string,int32> foo2,
             [2] class TailcallVerify.Condition10/Foo1`2<string,int16> foo3,
             [3] class TailcallVerify.Condition10/Foo1`2<string,valuetype TailcallVerify.ValueType3Bytes> foo4,
             [4] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition10.Test5 - Caller: Arguments: N"
    + "one - ReturnType: void; Callee<string>: Arguments: None - ReturnType: v"
    + "oid"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition10::Result
    .try
    {
      IL_0011:  newobj     instance void class TailcallVerify.Condition10/Foo1`2<string,int32>::.ctor()
      IL_0016:  stloc.0
      IL_0017:  newobj     instance void class TailcallVerify.Condition10/Foo1`2<string,int32>::.ctor()
      IL_001c:  stloc.1
      IL_001d:  newobj     instance void class TailcallVerify.Condition10/Foo1`2<string,int16>::.ctor()
      IL_0022:  stloc.2
      IL_0023:  newobj     instance void class TailcallVerify.Condition10/Foo1`2<string,valuetype TailcallVerify.ValueType3Bytes>::.ctor()
      IL_0028:  stloc.3
      IL_0029:  ldloc.0
      IL_002a:  callvirt   instance void class TailcallVerify.Condition10/Foo1`2<string,int32>::Test1Recursive()
      IL_002f:  ldloc.1
      IL_0030:  callvirt   instance void class TailcallVerify.Condition10/Foo1`2<string,int32>::Test1Recursive()
      IL_0035:  ldloc.2
      IL_0036:  callvirt   instance void class TailcallVerify.Condition10/Foo1`2<string,int16>::Test1Recursive()
      IL_003b:  ldloc.3
      IL_003c:  callvirt   instance void class TailcallVerify.Condition10/Foo1`2<string,valuetype TailcallVerify.ValueType3Bytes>::Test1Recursive()
      IL_0041:  leave.s    IL_009e

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0043:  stloc.s    e
      IL_0045:  ldloc.s    e
      IL_0047:  isinst     [mscorlib]System.DivideByZeroException
      IL_004c:  brtrue.s   IL_0057

      IL_004e:  ldc.i4.s   101
      IL_0050:  stsfld     int32 TailcallVerify.Condition10::Result
      IL_0055:  rethrow
      IL_0057:  ldloc.s    e
      IL_0059:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005e:  ldstr      "Caller"
      IL_0063:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_0068:  ldc.i4.m1
      IL_0069:  beq.s      IL_009c

      IL_006b:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0075:  ldstr      "------------------------------------------------"
      IL_007a:  call       void [System.Console]System.Console::WriteLine(string)
      IL_007f:  call       void [System.Console]System.Console::WriteLine()
      IL_0084:  ldloc.s    e
      IL_0086:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_008b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0090:  call       void [System.Console]System.Console::WriteLine()
      IL_0095:  ldc.i4.s   101
      IL_0097:  stsfld     int32 TailcallVerify.Condition10::Result
      IL_009c:  leave.s    IL_009e

    }  // end handler
    IL_009e:  ldstr      "Execution finished - Test "
    IL_00a3:  ldsfld     int32 TailcallVerify.Condition10::Result
    IL_00a8:  ldc.i4.s   100
    IL_00aa:  beq.s      IL_00b3

    IL_00ac:  ldstr      "FAILED"
    IL_00b1:  br.s       IL_00b8

    IL_00b3:  ldstr      "PASSED"
    IL_00b8:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_00bd:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00c2:  ldsfld     int32 TailcallVerify.Condition10::Result
    IL_00c7:  ret
  } // end of method Condition10::Test5

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Condition10::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  volatile.
    IL_0003:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition10::zero
    IL_0008:  ldc.i4.s   100
    IL_000a:  stsfld     int32 TailcallVerify.Condition10::Result
    IL_000f:  ret
  } // end of method Condition10::.cctor

} // end of class TailcallVerify.Condition10

.class private auto ansi beforefieldinit TailcallVerify.Condition7
       extends [mscorlib]System.Object
{
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .field private static int32 Result
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       153 (0x99)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition7.Test1 - Caller: Arguments: No"
    + "ne - ReturnType: void; Callee: Arguments: 10 x Int32 - ReturnType: void"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition7::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition7::Caller1()
      IL_0016:  leave.s    IL_006f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  isinst     [mscorlib]System.DivideByZeroException
      IL_001f:  brtrue.s   IL_002a

      IL_0021:  ldc.i4.s   101
      IL_0023:  stsfld     int32 TailcallVerify.Condition7::Result
      IL_0028:  rethrow
      IL_002a:  ldloc.0
      IL_002b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0030:  ldstr      "Caller"
      IL_0035:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003a:  ldc.i4.m1
      IL_003b:  beq.s      IL_006d

      IL_003d:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0042:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0047:  ldstr      "------------------------------------------------"
      IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0051:  call       void [System.Console]System.Console::WriteLine()
      IL_0056:  ldloc.0
      IL_0057:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0061:  call       void [System.Console]System.Console::WriteLine()
      IL_0066:  ldc.i4.s   101
      IL_0068:  stsfld     int32 TailcallVerify.Condition7::Result
      IL_006d:  leave.s    IL_006f

    }  // end handler
    IL_006f:  ldstr      "Execution finished - Test "
    IL_0074:  ldsfld     int32 TailcallVerify.Condition7::Result
    IL_0079:  ldc.i4.s   100
    IL_007b:  beq.s      IL_0084

    IL_007d:  ldstr      "FAILED"
    IL_0082:  br.s       IL_0089

    IL_0084:  ldstr      "PASSED"
    IL_0089:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0093:  ldsfld     int32 TailcallVerify.Condition7::Result
    IL_0098:  ret
  } // end of method Condition7::Test1

  .method private hidebysig static void  Caller1() cil managed
  {
    // Code size       63 (0x3f)
    .maxstack  10
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition7::Result
    IL_002e:  ldc.i4.0
    IL_002f:  ldc.i4.1
    IL_0030:  ldc.i4.2
    IL_0031:  ldc.i4.3
    IL_0032:  ldc.i4.4
    IL_0033:  ldc.i4.5
    IL_0034:  ldc.i4.6
    IL_0035:  ldc.i4.7
    IL_0036:  ldc.i4.8
    IL_0037:  ldc.i4.s   9
    IL_0039:  tail. call       void TailcallVerify.Condition7::Callee1(int32,
                                                                 int32,
                                                                 int32,
                                                                 int32,
                                                                 int32,
                                                                 int32,
                                                                 int32,
                                                                 int32,
                                                                 int32,
                                                                 int32)
    IL_003e:  ret
  } // end of method Condition7::Caller1

  .method private hidebysig static void  Callee1(int32 i1,
                                                 int32 i2,
                                                 int32 i3,
                                                 int32 i4,
                                                 int32 i5,
                                                 int32 i6,
                                                 int32 i7,
                                                 int32 i8,
                                                 int32 i9,
                                                 int32 i10) cil managed noinlining
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition7::zero
    IL_0008:  div
    IL_0009:  pop
    IL_000a:  ret
  } // end of method Condition7::Callee1

  .method public hidebysig static int32  Test2() cil managed nooptimization
  {
    // Code size       153 (0x99)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition7.Test2 - Caller: Arguments: No"
    + "ne - ReturnType: void; Callee: Arguments: i32,i64,mb3,i16,u8,f64,f32,mb"
    + "5 - ReturnType: void"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition7::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition7::Caller2()
      IL_0016:  leave.s    IL_006f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  isinst     [mscorlib]System.DivideByZeroException
      IL_001f:  brtrue.s   IL_002a

      IL_0021:  ldc.i4.s   101
      IL_0023:  stsfld     int32 TailcallVerify.Condition7::Result
      IL_0028:  rethrow
      IL_002a:  ldloc.0
      IL_002b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0030:  ldstr      "Caller"
      IL_0035:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003a:  ldc.i4.m1
      IL_003b:  beq.s      IL_006d

      IL_003d:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0042:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0047:  ldstr      "------------------------------------------------"
      IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0051:  call       void [System.Console]System.Console::WriteLine()
      IL_0056:  ldloc.0
      IL_0057:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0061:  call       void [System.Console]System.Console::WriteLine()
      IL_0066:  ldc.i4.s   101
      IL_0068:  stsfld     int32 TailcallVerify.Condition7::Result
      IL_006d:  leave.s    IL_006f

    }  // end handler
    IL_006f:  ldstr      "Execution finished - Test "
    IL_0074:  ldsfld     int32 TailcallVerify.Condition7::Result
    IL_0079:  ldc.i4.s   100
    IL_007b:  beq.s      IL_0084

    IL_007d:  ldstr      "FAILED"
    IL_0082:  br.s       IL_0089

    IL_0084:  ldstr      "PASSED"
    IL_0089:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0093:  ldsfld     int32 TailcallVerify.Condition7::Result
    IL_0098:  ret
  } // end of method Condition7::Test2

  .method private hidebysig static void  Caller2() cil managed
  {
    // Code size       174 (0xae)
    .maxstack  8
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes v3,
             [1] valuetype TailcallVerify.ValueType5Bytes v5,
             [2] valuetype TailcallVerify.ValueType3Bytes '<>g__initLocal0',
             [3] valuetype TailcallVerify.ValueType5Bytes '<>g__initLocal1',
             [4] valuetype TailcallVerify.ValueType3Bytes CS$0$0000,
             [5] valuetype TailcallVerify.ValueType5Bytes CS$0$0001)
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition7::Result
    IL_002e:  ldloca.s   CS$0$0000
    IL_0030:  initobj    TailcallVerify.ValueType3Bytes
    IL_0036:  ldloc.s    CS$0$0000
    IL_0038:  stloc.2
    IL_0039:  ldloca.s   '<>g__initLocal0'
    IL_003b:  ldc.i4.0
    IL_003c:  stfld      uint8 TailcallVerify.ValueType3Bytes::i1
    IL_0041:  ldloca.s   '<>g__initLocal0'
    IL_0043:  ldc.i4     0x7fff
    IL_0048:  stfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_004d:  ldloc.2
    IL_004e:  stloc.0
    IL_004f:  ldloca.s   CS$0$0001
    IL_0051:  initobj    TailcallVerify.ValueType5Bytes
    IL_0057:  ldloc.s    CS$0$0001
    IL_0059:  stloc.3
    IL_005a:  ldloca.s   '<>g__initLocal1'
    IL_005c:  ldc.i4     0xff
    IL_0061:  stfld      uint8 TailcallVerify.ValueType5Bytes::i1
    IL_0066:  ldloca.s   '<>g__initLocal1'
    IL_0068:  ldc.i4     0xffff8000
    IL_006d:  stfld      int16 TailcallVerify.ValueType5Bytes::i2
    IL_0072:  ldloca.s   '<>g__initLocal1'
    IL_0074:  ldc.i4     0x7fff
    IL_0079:  stfld      int16 TailcallVerify.ValueType5Bytes::i3
    IL_007e:  ldloc.3
    IL_007f:  stloc.1
    IL_0080:  ldc.i4     0x80000000
    IL_0085:  ldc.i8     0x7fffffffffffffff
    IL_008e:  ldloc.0
    IL_008f:  ldc.i4     0xffff8000
    IL_0094:  ldc.i4     0xff
    IL_0099:  ldc.r8     -1.7976931348623157e+308
    IL_00a2:  ldc.r4     3.4028235e+038
    IL_00a7:  ldloc.1
    IL_00a8:  tail. call       void TailcallVerify.Condition7::Callee2(int32,
                                                                 int64,
                                                                 valuetype TailcallVerify.ValueType3Bytes,
                                                                 int16,
                                                                 uint8,
                                                                 float64,
                                                                 float32,
                                                                 valuetype TailcallVerify.ValueType5Bytes)
    IL_00ad:  ret
  } // end of method Condition7::Caller2

  .method private hidebysig static void  Callee2(int32 i1,
                                                 int64 i2,
                                                 valuetype TailcallVerify.ValueType3Bytes v3,
                                                 int16 i4,
                                                 uint8 b5,
                                                 float64 d6,
                                                 float32 f7,
                                                 valuetype TailcallVerify.ValueType5Bytes v8) cil managed noinlining
  {
    // Code size       308 (0x134)
    .maxstack  4
    .locals init ([0] object[] CS$0$0000)
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4     0x80000000
    IL_0006:  bne.un.s   IL_007f

    IL_0008:  ldarg.1
    IL_0009:  ldc.i8     0x7fffffffffffffff
    IL_0012:  bne.un.s   IL_007f

    IL_0014:  ldarga.s   v3
    IL_0016:  ldfld      uint8 TailcallVerify.ValueType3Bytes::i1
    IL_001b:  brtrue.s   IL_007f

    IL_001d:  ldarga.s   v3
    IL_001f:  ldfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_0024:  ldc.i4     0x7fff
    IL_0029:  bne.un.s   IL_007f

    IL_002b:  ldarg.3
    IL_002c:  ldc.i4     0xffff8000
    IL_0031:  bne.un.s   IL_007f

    IL_0033:  ldarg.s    b5
    IL_0035:  ldc.i4     0xff
    IL_003a:  bne.un.s   IL_007f

    IL_003c:  ldarg.s    d6
    IL_003e:  ldc.r8     -1.7976931348623157e+308
    IL_0047:  bne.un.s   IL_007f

    IL_0049:  ldarg.s    f7
    IL_004b:  ldc.r4     3.4028235e+038
    IL_0050:  bne.un.s   IL_007f

    IL_0052:  ldarga.s   v8
    IL_0054:  ldfld      uint8 TailcallVerify.ValueType5Bytes::i1
    IL_0059:  ldc.i4     0xff
    IL_005e:  bne.un.s   IL_007f

    IL_0060:  ldarga.s   v8
    IL_0062:  ldfld      int16 TailcallVerify.ValueType5Bytes::i2
    IL_0067:  ldc.i4     0xffff8000
    IL_006c:  bne.un.s   IL_007f

    IL_006e:  ldarga.s   v8
    IL_0070:  ldfld      int16 TailcallVerify.ValueType5Bytes::i3
    IL_0075:  ldc.i4     0x7fff
    IL_007a:  beq        IL_0129

    IL_007f:  ldc.i4.s   101
    IL_0081:  stsfld     int32 TailcallVerify.Condition7::Result
    IL_0086:  ldstr      "FAILED: Passed in arguments are invalid."
    IL_008b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0090:  ldstr      "i1:{0} != Int32.MinValue || i2:{1} != Int64.MaxVal"
    + "ue || v3.i1:{2} != byte.MinValue || v3.i2:{3} != short.MaxValue || i4:{"
    + "4} != Int16.MinValue || b5:{5} != byte.MaxValue || d6:{6} != double.Min"
    + "Value || f7:{7} != float.MaxValue || v8.i1:{8} != byte.MaxValue || v8.i"
    + "2:{9} != short.MinValue || v8.i3:{10} != short.MaxValue"
    IL_0095:  ldc.i4.s   11
    IL_0097:  newarr     [mscorlib]System.Object
    IL_009c:  stloc.0
    IL_009d:  ldloc.0
    IL_009e:  ldc.i4.0
    IL_009f:  ldarg.0
    IL_00a0:  box        [mscorlib]System.Int32
    IL_00a5:  stelem.ref
    IL_00a6:  ldloc.0
    IL_00a7:  ldc.i4.1
    IL_00a8:  ldarg.1
    IL_00a9:  box        [mscorlib]System.Int64
    IL_00ae:  stelem.ref
    IL_00af:  ldloc.0
    IL_00b0:  ldc.i4.2
    IL_00b1:  ldarga.s   v3
    IL_00b3:  ldfld      uint8 TailcallVerify.ValueType3Bytes::i1
    IL_00b8:  box        [mscorlib]System.Byte
    IL_00bd:  stelem.ref
    IL_00be:  ldloc.0
    IL_00bf:  ldc.i4.3
    IL_00c0:  ldarga.s   v3
    IL_00c2:  ldfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_00c7:  box        [mscorlib]System.Int16
    IL_00cc:  stelem.ref
    IL_00cd:  ldloc.0
    IL_00ce:  ldc.i4.4
    IL_00cf:  ldarg.3
    IL_00d0:  box        [mscorlib]System.Int16
    IL_00d5:  stelem.ref
    IL_00d6:  ldloc.0
    IL_00d7:  ldc.i4.5
    IL_00d8:  ldarg.s    b5
    IL_00da:  box        [mscorlib]System.Byte
    IL_00df:  stelem.ref
    IL_00e0:  ldloc.0
    IL_00e1:  ldc.i4.6
    IL_00e2:  ldarg.s    d6
    IL_00e4:  box        [mscorlib]System.Double
    IL_00e9:  stelem.ref
    IL_00ea:  ldloc.0
    IL_00eb:  ldc.i4.7
    IL_00ec:  ldarg.s    f7
    IL_00ee:  box        [mscorlib]System.Single
    IL_00f3:  stelem.ref
    IL_00f4:  ldloc.0
    IL_00f5:  ldc.i4.8
    IL_00f6:  ldarga.s   v8
    IL_00f8:  ldfld      uint8 TailcallVerify.ValueType5Bytes::i1
    IL_00fd:  box        [mscorlib]System.Byte
    IL_0102:  stelem.ref
    IL_0103:  ldloc.0
    IL_0104:  ldc.i4.s   9
    IL_0106:  ldarga.s   v8
    IL_0108:  ldfld      int16 TailcallVerify.ValueType5Bytes::i2
    IL_010d:  box        [mscorlib]System.Int16
    IL_0112:  stelem.ref
    IL_0113:  ldloc.0
    IL_0114:  ldc.i4.s   10
    IL_0116:  ldarga.s   v8
    IL_0118:  ldfld      int16 TailcallVerify.ValueType5Bytes::i3
    IL_011d:  box        [mscorlib]System.Int16
    IL_0122:  stelem.ref
    IL_0123:  ldloc.0
    IL_0124:  call       void [System.Console]System.Console::Write(string)
              callvirt   instance string [mscorlib]System.Object::ToString()
              call       void [System.Console]System.Console::WriteLine(string)

    IL_0129:  ldc.i4.1
    IL_012a:  volatile.
    IL_012c:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition7::zero
    IL_0131:  div
    IL_0132:  pop
    IL_0133:  ret
  } // end of method Condition7::Callee2

  .method public hidebysig static int32  Test3() cil managed nooptimization
  {
    // Code size       193 (0xc1)
    .maxstack  6
    .locals init ([0] class [mscorlib]System.Exception e,
             [1] valuetype TailcallVerify.ValueType3Bytes CS$0$0000,
             [2] valuetype TailcallVerify.ValueType5Bytes CS$0$0001,
             [3] valuetype TailcallVerify.ValueTypeSingleInt64 CS$0$0002,
             [4] valuetype TailcallVerify.ValueType3Bytes CS$0$0003,
             [5] valuetype TailcallVerify.ValueType5Bytes CS$0$0004,
             [6] valuetype TailcallVerify.ValueType3Bytes CS$0$0005)
    IL_0000:  ldstr      "Executing Condition7.Test3 - Caller: Arguments: mb"
    + "3,mb5,mb8,mb3,mb5,mb3 - ReturnType: void; Callee: Arguments: mb3,mb5,mb"
    + "8,mb3,mb5,mb3 - ReturnType: void"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition7::Result
    .try
    {
      IL_0011:  ldloca.s   CS$0$0000
      IL_0013:  initobj    TailcallVerify.ValueType3Bytes
      IL_0019:  ldloc.1
      IL_001a:  ldloca.s   CS$0$0001
      IL_001c:  initobj    TailcallVerify.ValueType5Bytes
      IL_0022:  ldloc.2
      IL_0023:  ldloca.s   CS$0$0002
      IL_0025:  initobj    TailcallVerify.ValueTypeSingleInt64
      IL_002b:  ldloc.3
      IL_002c:  ldloca.s   CS$0$0003
      IL_002e:  initobj    TailcallVerify.ValueType3Bytes
      IL_0034:  ldloc.s    CS$0$0003
      IL_0036:  ldloca.s   CS$0$0004
      IL_0038:  initobj    TailcallVerify.ValueType5Bytes
      IL_003e:  ldloc.s    CS$0$0004
      IL_0040:  ldloca.s   CS$0$0005
      IL_0042:  initobj    TailcallVerify.ValueType3Bytes
      IL_0048:  ldloc.s    CS$0$0005
      IL_004a:  call       void TailcallVerify.Condition7::Caller3(valuetype TailcallVerify.ValueType3Bytes,
                                                                   valuetype TailcallVerify.ValueType5Bytes,
                                                                   valuetype TailcallVerify.ValueTypeSingleInt64,
                                                                   valuetype TailcallVerify.ValueType3Bytes,
                                                                   valuetype TailcallVerify.ValueType5Bytes,
                                                                   valuetype TailcallVerify.ValueType3Bytes)
      IL_004f:  leave.s    IL_0097

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0051:  stloc.0
      IL_0052:  ldloc.0
      IL_0053:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0058:  ldstr      "Caller"
      IL_005d:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_0062:  ldc.i4.m1
      IL_0063:  beq.s      IL_0095

      IL_0065:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_006a:  call       void [System.Console]System.Console::WriteLine(string)
      IL_006f:  ldstr      "------------------------------------------------"
      IL_0074:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0079:  call       void [System.Console]System.Console::WriteLine()
      IL_007e:  ldloc.0
      IL_007f:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0084:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0089:  call       void [System.Console]System.Console::WriteLine()
      IL_008e:  ldc.i4.s   101
      IL_0090:  stsfld     int32 TailcallVerify.Condition7::Result
      IL_0095:  leave.s    IL_0097

    }  // end handler
    IL_0097:  ldstr      "Execution finished - Test "
    IL_009c:  ldsfld     int32 TailcallVerify.Condition7::Result
    IL_00a1:  ldc.i4.s   100
    IL_00a3:  beq.s      IL_00ac

    IL_00a5:  ldstr      "FAILED"
    IL_00aa:  br.s       IL_00b1

    IL_00ac:  ldstr      "PASSED"
    IL_00b1:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_00b6:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00bb:  ldsfld     int32 TailcallVerify.Condition7::Result
    IL_00c0:  ret
  } // end of method Condition7::Test3

  .method private hidebysig static void  Caller3(valuetype TailcallVerify.ValueType3Bytes v1,
                                                 valuetype TailcallVerify.ValueType5Bytes v2,
                                                 valuetype TailcallVerify.ValueTypeSingleInt64 v3,
                                                 valuetype TailcallVerify.ValueType3Bytes v4,
                                                 valuetype TailcallVerify.ValueType5Bytes v5,
                                                 valuetype TailcallVerify.ValueType3Bytes v6) cil managed
  {
    // Code size       60 (0x3c)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition7::Result
    IL_002e:  ldarg.0
    IL_002f:  ldarg.1
    IL_0030:  ldarg.2
    IL_0031:  ldarg.3
    IL_0032:  ldarg.s    v5
    IL_0034:  ldarg.s    v6
    IL_0036:  tail. call       void TailcallVerify.Condition7::Callee3(valuetype TailcallVerify.ValueType3Bytes,
                                                                 valuetype TailcallVerify.ValueType5Bytes,
                                                                 valuetype TailcallVerify.ValueTypeSingleInt64,
                                                                 valuetype TailcallVerify.ValueType3Bytes,
                                                                 valuetype TailcallVerify.ValueType5Bytes,
                                                                 valuetype TailcallVerify.ValueType3Bytes)
    IL_003b:  ret
  } // end of method Condition7::Caller3

  .method private hidebysig static void  Callee3(valuetype TailcallVerify.ValueType3Bytes v1,
                                                 valuetype TailcallVerify.ValueType5Bytes v2,
                                                 valuetype TailcallVerify.ValueTypeSingleInt64 v3,
                                                 valuetype TailcallVerify.ValueType3Bytes v4,
                                                 valuetype TailcallVerify.ValueType5Bytes v5,
                                                 valuetype TailcallVerify.ValueType3Bytes v6) cil managed noinlining
  {
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldarga.s   v1
    IL_0002:  ldc.i4.1
    IL_0003:  volatile.
    IL_0005:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition7::zero
    IL_000a:  div
    IL_000b:  conv.i2
    IL_000c:  stfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_0011:  ret
  } // end of method Condition7::Callee3

  .method public hidebysig static int32  Test4() cil managed nooptimization
  {
    // Code size       193 (0xc1)
    .maxstack  6
    .locals init ([0] class [mscorlib]System.Exception e,
             [1] valuetype TailcallVerify.ValueType3Bytes CS$0$0000,
             [2] valuetype TailcallVerify.ValueType5Bytes CS$0$0001,
             [3] valuetype TailcallVerify.ValueTypeSingleInt64 CS$0$0002,
             [4] valuetype TailcallVerify.ValueType3Bytes CS$0$0003,
             [5] valuetype TailcallVerify.ValueType5Bytes CS$0$0004,
             [6] valuetype TailcallVerify.ValueType3Bytes CS$0$0005)
    IL_0000:  ldstr      "Executing Condition7.Test4 - Caller: Arguments: mb"
    + "3,mb5,mb8,mb3,mb5,mb3 - ReturnType: void; Callee: Arguments: mb3,mb5,mb"
    + "8,mb3,mb5,mb3 - ReturnType: void"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition7::Result
    .try
    {
      IL_0011:  ldloca.s   CS$0$0000
      IL_0013:  initobj    TailcallVerify.ValueType3Bytes
      IL_0019:  ldloc.1
      IL_001a:  ldloca.s   CS$0$0001
      IL_001c:  initobj    TailcallVerify.ValueType5Bytes
      IL_0022:  ldloc.2
      IL_0023:  ldloca.s   CS$0$0002
      IL_0025:  initobj    TailcallVerify.ValueTypeSingleInt64
      IL_002b:  ldloc.3
      IL_002c:  ldloca.s   CS$0$0003
      IL_002e:  initobj    TailcallVerify.ValueType3Bytes
      IL_0034:  ldloc.s    CS$0$0003
      IL_0036:  ldloca.s   CS$0$0004
      IL_0038:  initobj    TailcallVerify.ValueType5Bytes
      IL_003e:  ldloc.s    CS$0$0004
      IL_0040:  ldloca.s   CS$0$0005
      IL_0042:  initobj    TailcallVerify.ValueType3Bytes
      IL_0048:  ldloc.s    CS$0$0005
      IL_004a:  call       void TailcallVerify.Condition7::Caller4(valuetype TailcallVerify.ValueType3Bytes,
                                                                   valuetype TailcallVerify.ValueType5Bytes,
                                                                   valuetype TailcallVerify.ValueTypeSingleInt64,
                                                                   valuetype TailcallVerify.ValueType3Bytes,
                                                                   valuetype TailcallVerify.ValueType5Bytes,
                                                                   valuetype TailcallVerify.ValueType3Bytes)
      IL_004f:  leave.s    IL_0097

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0051:  stloc.0
      IL_0052:  ldloc.0
      IL_0053:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0058:  ldstr      "Caller"
      IL_005d:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_0062:  ldc.i4.m1
      IL_0063:  beq.s      IL_0095

      IL_0065:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_006a:  call       void [System.Console]System.Console::WriteLine(string)
      IL_006f:  ldstr      "------------------------------------------------"
      IL_0074:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0079:  call       void [System.Console]System.Console::WriteLine()
      IL_007e:  ldloc.0
      IL_007f:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0084:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0089:  call       void [System.Console]System.Console::WriteLine()
      IL_008e:  ldc.i4.s   101
      IL_0090:  stsfld     int32 TailcallVerify.Condition7::Result
      IL_0095:  leave.s    IL_0097

    }  // end handler
    IL_0097:  ldstr      "Execution finished - Test "
    IL_009c:  ldsfld     int32 TailcallVerify.Condition7::Result
    IL_00a1:  ldc.i4.s   100
    IL_00a3:  beq.s      IL_00ac

    IL_00a5:  ldstr      "FAILED"
    IL_00aa:  br.s       IL_00b1

    IL_00ac:  ldstr      "PASSED"
    IL_00b1:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_00b6:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00bb:  ldsfld     int32 TailcallVerify.Condition7::Result
    IL_00c0:  ret
  } // end of method Condition7::Test4

  .method private hidebysig static void  Caller4(valuetype TailcallVerify.ValueType3Bytes v1,
                                                 valuetype TailcallVerify.ValueType5Bytes v2,
                                                 valuetype TailcallVerify.ValueTypeSingleInt64 v3,
                                                 valuetype TailcallVerify.ValueType3Bytes v4,
                                                 valuetype TailcallVerify.ValueType5Bytes v5,
                                                 valuetype TailcallVerify.ValueType3Bytes v6) cil managed
  {
    // Code size       81 (0x51)
    .maxstack  7
    .locals init ([0] int32[] a)
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition7::Result
    IL_002e:  ldc.i4.5
    IL_002f:  newarr     [mscorlib]System.Int32
    IL_0034:  dup
    IL_0035:  ldtoken    field valuetype '<PrivateImplementationDetails>{D1242658-CA16-4D11-A740-6635F112F4B5}'/'__StaticArrayInitTypeSize=20' '<PrivateImplementationDetails>{D1242658-CA16-4D11-A740-6635F112F4B5}'::'$$method0x6000076-1'
    IL_003a:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                        valuetype [mscorlib]System.RuntimeFieldHandle)
    IL_003f:  stloc.0
    IL_0040:  ldarg.0
    IL_0041:  ldarg.1
    IL_0042:  ldarg.2
    IL_0043:  ldarg.3
    IL_0044:  ldarg.s    v5
    IL_0046:  ldarg.s    v6
    IL_0048:  ldloc.0
    IL_0049:  ldlen
    IL_004a:  conv.i4
    IL_004b:  tail. call       void TailcallVerify.Condition7::Callee4(valuetype TailcallVerify.ValueType3Bytes,
                                                                 valuetype TailcallVerify.ValueType5Bytes,
                                                                 valuetype TailcallVerify.ValueTypeSingleInt64,
                                                                 valuetype TailcallVerify.ValueType3Bytes,
                                                                 valuetype TailcallVerify.ValueType5Bytes,
                                                                 valuetype TailcallVerify.ValueType3Bytes,
                                                                 int32)
    IL_0050:  ret
  } // end of method Condition7::Caller4

  .method private hidebysig static void  Callee4(valuetype TailcallVerify.ValueType3Bytes v1,
                                                 valuetype TailcallVerify.ValueType5Bytes v2,
                                                 valuetype TailcallVerify.ValueTypeSingleInt64 v3,
                                                 valuetype TailcallVerify.ValueType3Bytes v4,
                                                 valuetype TailcallVerify.ValueType5Bytes v5,
                                                 valuetype TailcallVerify.ValueType3Bytes v6,
                                                 int32 i7) cil managed noinlining
  {
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldarga.s   v1
    IL_0002:  ldc.i4.1
    IL_0003:  volatile.
    IL_0005:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition7::zero
    IL_000a:  div
    IL_000b:  conv.i2
    IL_000c:  stfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_0011:  ret
  } // end of method Condition7::Callee4

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Condition7::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  volatile.
    IL_0003:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition7::zero
    IL_0008:  ldc.i4.s   100
    IL_000a:  stsfld     int32 TailcallVerify.Condition7::Result
    IL_000f:  ret
  } // end of method Condition7::.cctor

} // end of class TailcallVerify.Condition7

.class private auto ansi beforefieldinit TailcallVerify.Condition20
       extends [mscorlib]System.Object
{
  .field private static int32 Result
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       158 (0x9e)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition20.Test1 - Caller: Arguments: i"
    + "nt - ReturnType: void; Callee: the same as Caller (called again)"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition20::Result
    .try
    {
      IL_0011:  ldc.i4     0xc8
      IL_0016:  call       void TailcallVerify.Condition20::Caller1(int32)
      IL_001b:  leave.s    IL_0074

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_001d:  stloc.0
      IL_001e:  ldloc.0
      IL_001f:  isinst     [mscorlib]System.DivideByZeroException
      IL_0024:  brtrue.s   IL_002f

      IL_0026:  ldc.i4.s   101
      IL_0028:  stsfld     int32 TailcallVerify.Condition20::Result
      IL_002d:  rethrow
      IL_002f:  ldloc.0
      IL_0030:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0035:  ldstr      "Caller"
      IL_003a:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003f:  ldc.i4.m1
      IL_0040:  bne.un.s   IL_0072

      IL_0042:  ldstr      "FAILED: Did not find the word 'Caller' in the stac"
      + "ktrace."
      IL_0047:  call       void [System.Console]System.Console::WriteLine(string)
      IL_004c:  ldstr      "------------------------------------------------"
      IL_0051:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0056:  call       void [System.Console]System.Console::WriteLine()
      IL_005b:  ldloc.0
      IL_005c:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0061:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0066:  call       void [System.Console]System.Console::WriteLine()
      IL_006b:  ldc.i4.s   101
      IL_006d:  stsfld     int32 TailcallVerify.Condition20::Result
      IL_0072:  leave.s    IL_0074

    }  // end handler
    IL_0074:  ldstr      "Execution finished - Test "
    IL_0079:  ldsfld     int32 TailcallVerify.Condition20::Result
    IL_007e:  ldc.i4.s   100
    IL_0080:  beq.s      IL_0089

    IL_0082:  ldstr      "FAILED"
    IL_0087:  br.s       IL_008e

    IL_0089:  ldstr      "PASSED"
    IL_008e:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0093:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0098:  ldsfld     int32 TailcallVerify.Condition20::Result
    IL_009d:  ret
  } // end of method Condition20::Test1

  .method private hidebysig static void  Caller1(int32 i) cil managed noinlining
  {
    // Code size       60 (0x3c)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition20::Result
    IL_002e:  ldc.i4.s   10
    IL_0030:  ldarg.0
    IL_0031:  div
    IL_0032:  pop
    IL_0033:  ldarg.0
    IL_0034:  ldc.i4.1
    IL_0035:  sub
    IL_0036:  call       void TailcallVerify.Condition20::Caller1(int32)
    IL_003b:  ret
  } // end of method Condition20::Caller1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Condition20::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldc.i4.s   100
    IL_0002:  stsfld     int32 TailcallVerify.Condition20::Result
    IL_0007:  ret
  } // end of method Condition20::.cctor

} // end of class TailcallVerify.Condition20

.class private auto ansi beforefieldinit TailcallVerify.Condition6
       extends [mscorlib]System.Object
{
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .field private static int32 Result
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       136 (0x88)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition6.Test1 - Caller: Arguments: No"
    + "ne - ReturnType: void; Callee: Arguments: 3 byte struct - ReturnType: v"
    + "oid"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition6::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition6::Caller1()
      IL_0016:  leave.s    IL_005e

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_001f:  ldstr      "Caller"
      IL_0024:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_0029:  ldc.i4.m1
      IL_002a:  beq.s      IL_005c

      IL_002c:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0031:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0036:  ldstr      "------------------------------------------------"
      IL_003b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0040:  call       void [System.Console]System.Console::WriteLine()
      IL_0045:  ldloc.0
      IL_0046:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_004b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0050:  call       void [System.Console]System.Console::WriteLine()
      IL_0055:  ldc.i4.s   101
      IL_0057:  stsfld     int32 TailcallVerify.Condition6::Result
      IL_005c:  leave.s    IL_005e

    }  // end handler
    IL_005e:  ldstr      "Execution finished - Test "
    IL_0063:  ldsfld     int32 TailcallVerify.Condition6::Result
    IL_0068:  ldc.i4.s   100
    IL_006a:  beq.s      IL_0073

    IL_006c:  ldstr      "FAILED"
    IL_0071:  br.s       IL_0078

    IL_0073:  ldstr      "PASSED"
    IL_0078:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_007d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0082:  ldsfld     int32 TailcallVerify.Condition6::Result
    IL_0087:  ret
  } // end of method Condition6::Test1

  .method private hidebysig static void  Caller1() cil managed
  {
    // Code size       70 (0x46)
    .maxstack  2
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes CS$0$0000,
             [1] valuetype TailcallVerify.ValueType5Bytes CS$0$0001)
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition6::Result
    IL_002e:  ldloca.s   CS$0$0000
    IL_0030:  initobj    TailcallVerify.ValueType3Bytes
    IL_0036:  ldloc.0
    IL_0037:  ldloca.s   CS$0$0001
    IL_0039:  initobj    TailcallVerify.ValueType5Bytes
    IL_003f:  ldloc.1
    IL_0040:  tail. call       void TailcallVerify.Condition6::Callee1(valuetype TailcallVerify.ValueType3Bytes,
                                                                 valuetype TailcallVerify.ValueType5Bytes)
    IL_0045:  ret
  } // end of method Condition6::Caller1

  .method private hidebysig static void  Callee1(valuetype TailcallVerify.ValueType3Bytes v,
                                                 valuetype TailcallVerify.ValueType5Bytes v5) cil managed noinlining
  {
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldarga.s   v
    IL_0002:  ldc.i4.1
    IL_0003:  volatile.
    IL_0005:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition6::zero
    IL_000a:  div
    IL_000b:  conv.i2
    IL_000c:  stfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_0011:  ret
  } // end of method Condition6::Callee1

  .method public hidebysig static int32  Test2() cil managed nooptimization
  {
    // Code size       136 (0x88)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition6.Test2 - Caller: Arguments: No"
    + "ne - ReturnType: void; Callee: Arguments: 3 byte struct - ReturnType: v"
    + "oid"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition6::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition6::Caller2()
      IL_0016:  leave.s    IL_005e

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_001f:  ldstr      "Caller"
      IL_0024:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_0029:  ldc.i4.m1
      IL_002a:  beq.s      IL_005c

      IL_002c:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0031:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0036:  ldstr      "------------------------------------------------"
      IL_003b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0040:  call       void [System.Console]System.Console::WriteLine()
      IL_0045:  ldloc.0
      IL_0046:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_004b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0050:  call       void [System.Console]System.Console::WriteLine()
      IL_0055:  ldc.i4.s   101
      IL_0057:  stsfld     int32 TailcallVerify.Condition6::Result
      IL_005c:  leave.s    IL_005e

    }  // end handler
    IL_005e:  ldstr      "Execution finished - Test "
    IL_0063:  ldsfld     int32 TailcallVerify.Condition6::Result
    IL_0068:  ldc.i4.s   100
    IL_006a:  beq.s      IL_0073

    IL_006c:  ldstr      "FAILED"
    IL_0071:  br.s       IL_0078

    IL_0073:  ldstr      "PASSED"
    IL_0078:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_007d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0082:  ldsfld     int32 TailcallVerify.Condition6::Result
    IL_0087:  ret
  } // end of method Condition6::Test2

  .method private hidebysig static void  Caller2() cil managed
  {
    // Code size       79 (0x4f)
    .maxstack  3
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes CS$0$0000,
             [1] valuetype TailcallVerify.ValueType5Bytes CS$0$0001,
             [2] valuetype TailcallVerify.ValueTypeSingleInt64 CS$0$0002)
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition6::Result
    IL_002e:  ldloca.s   CS$0$0000
    IL_0030:  initobj    TailcallVerify.ValueType3Bytes
    IL_0036:  ldloc.0
    IL_0037:  ldloca.s   CS$0$0001
    IL_0039:  initobj    TailcallVerify.ValueType5Bytes
    IL_003f:  ldloc.1
    IL_0040:  ldloca.s   CS$0$0002
    IL_0042:  initobj    TailcallVerify.ValueTypeSingleInt64
    IL_0048:  ldloc.2
    IL_0049:  tail. call       void TailcallVerify.Condition6::Callee2(valuetype TailcallVerify.ValueType3Bytes,
                                                                 valuetype TailcallVerify.ValueType5Bytes,
                                                                 valuetype TailcallVerify.ValueTypeSingleInt64)
    IL_004e:  ret
  } // end of method Condition6::Caller2

  .method private hidebysig static void  Callee2(valuetype TailcallVerify.ValueType3Bytes v1,
                                                 valuetype TailcallVerify.ValueType5Bytes v2,
                                                 valuetype TailcallVerify.ValueTypeSingleInt64 v3) cil managed noinlining
  {
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldarga.s   v1
    IL_0002:  ldc.i4.1
    IL_0003:  volatile.
    IL_0005:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition6::zero
    IL_000a:  div
    IL_000b:  conv.i2
    IL_000c:  stfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_0011:  ret
  } // end of method Condition6::Callee2

  .method public hidebysig static int32  Test3() cil managed nooptimization
  {
    // Code size       136 (0x88)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition6.Test3 - Caller: Arguments: No"
    + "ne - ReturnType: void; Callee: Arguments: 3 byte struct - ReturnType: v"
    + "oid"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition6::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition6::Caller3()
      IL_0016:  leave.s    IL_005e

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_001f:  ldstr      "Caller"
      IL_0024:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_0029:  ldc.i4.m1
      IL_002a:  beq.s      IL_005c

      IL_002c:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0031:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0036:  ldstr      "------------------------------------------------"
      IL_003b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0040:  call       void [System.Console]System.Console::WriteLine()
      IL_0045:  ldloc.0
      IL_0046:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_004b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0050:  call       void [System.Console]System.Console::WriteLine()
      IL_0055:  ldc.i4.s   101
      IL_0057:  stsfld     int32 TailcallVerify.Condition6::Result
      IL_005c:  leave.s    IL_005e

    }  // end handler
    IL_005e:  ldstr      "Execution finished - Test "
    IL_0063:  ldsfld     int32 TailcallVerify.Condition6::Result
    IL_0068:  ldc.i4.s   100
    IL_006a:  beq.s      IL_0073

    IL_006c:  ldstr      "FAILED"
    IL_0071:  br.s       IL_0078

    IL_0073:  ldstr      "PASSED"
    IL_0078:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_007d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0082:  ldsfld     int32 TailcallVerify.Condition6::Result
    IL_0087:  ret
  } // end of method Condition6::Test3

  .method private hidebysig static void  Caller3() cil managed
  {
    // Code size       88 (0x58)
    .maxstack  4
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes CS$0$0000,
             [1] valuetype TailcallVerify.ValueType5Bytes CS$0$0001,
             [2] valuetype TailcallVerify.ValueTypeSingleInt64 CS$0$0002,
             [3] valuetype TailcallVerify.ValueType3Bytes CS$0$0003)
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition6::Result
    IL_002e:  ldloca.s   CS$0$0000
    IL_0030:  initobj    TailcallVerify.ValueType3Bytes
    IL_0036:  ldloc.0
    IL_0037:  ldloca.s   CS$0$0001
    IL_0039:  initobj    TailcallVerify.ValueType5Bytes
    IL_003f:  ldloc.1
    IL_0040:  ldloca.s   CS$0$0002
    IL_0042:  initobj    TailcallVerify.ValueTypeSingleInt64
    IL_0048:  ldloc.2
    IL_0049:  ldloca.s   CS$0$0003
    IL_004b:  initobj    TailcallVerify.ValueType3Bytes
    IL_0051:  ldloc.3
    IL_0052:  tail. call       void TailcallVerify.Condition6::Callee3(valuetype TailcallVerify.ValueType3Bytes,
                                                                 valuetype TailcallVerify.ValueType5Bytes,
                                                                 valuetype TailcallVerify.ValueTypeSingleInt64,
                                                                 valuetype TailcallVerify.ValueType3Bytes)
    IL_0057:  ret
  } // end of method Condition6::Caller3

  .method private hidebysig static void  Callee3(valuetype TailcallVerify.ValueType3Bytes v1,
                                                 valuetype TailcallVerify.ValueType5Bytes v2,
                                                 valuetype TailcallVerify.ValueTypeSingleInt64 v3,
                                                 valuetype TailcallVerify.ValueType3Bytes v4) cil managed noinlining
  {
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldarga.s   v1
    IL_0002:  ldc.i4.1
    IL_0003:  volatile.
    IL_0005:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition6::zero
    IL_000a:  div
    IL_000b:  conv.i2
    IL_000c:  stfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_0011:  ret
  } // end of method Condition6::Callee3

  .method public hidebysig static int32  Test4() cil managed nooptimization
  {
    // Code size       136 (0x88)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition6.Test4 - Caller: Arguments: No"
    + "ne - ReturnType: void; Callee: Arguments: 3 byte struct - ReturnType: v"
    + "oid"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition6::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition6::Caller4()
      IL_0016:  leave.s    IL_005e

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_001f:  ldstr      "Caller"
      IL_0024:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_0029:  ldc.i4.m1
      IL_002a:  beq.s      IL_005c

      IL_002c:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0031:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0036:  ldstr      "------------------------------------------------"
      IL_003b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0040:  call       void [System.Console]System.Console::WriteLine()
      IL_0045:  ldloc.0
      IL_0046:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_004b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0050:  call       void [System.Console]System.Console::WriteLine()
      IL_0055:  ldc.i4.s   101
      IL_0057:  stsfld     int32 TailcallVerify.Condition6::Result
      IL_005c:  leave.s    IL_005e

    }  // end handler
    IL_005e:  ldstr      "Execution finished - Test "
    IL_0063:  ldsfld     int32 TailcallVerify.Condition6::Result
    IL_0068:  ldc.i4.s   100
    IL_006a:  beq.s      IL_0073

    IL_006c:  ldstr      "FAILED"
    IL_0071:  br.s       IL_0078

    IL_0073:  ldstr      "PASSED"
    IL_0078:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_007d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0082:  ldsfld     int32 TailcallVerify.Condition6::Result
    IL_0087:  ret
  } // end of method Condition6::Test4

  .method private hidebysig static void  Caller4() cil managed
  {
    // Code size       98 (0x62)
    .maxstack  5
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes CS$0$0000,
             [1] valuetype TailcallVerify.ValueType5Bytes CS$0$0001,
             [2] valuetype TailcallVerify.ValueTypeSingleInt64 CS$0$0002,
             [3] valuetype TailcallVerify.ValueType3Bytes CS$0$0003,
             [4] valuetype TailcallVerify.ValueType5Bytes CS$0$0004)
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition6::Result
    IL_002e:  ldloca.s   CS$0$0000
    IL_0030:  initobj    TailcallVerify.ValueType3Bytes
    IL_0036:  ldloc.0
    IL_0037:  ldloca.s   CS$0$0001
    IL_0039:  initobj    TailcallVerify.ValueType5Bytes
    IL_003f:  ldloc.1
    IL_0040:  ldloca.s   CS$0$0002
    IL_0042:  initobj    TailcallVerify.ValueTypeSingleInt64
    IL_0048:  ldloc.2
    IL_0049:  ldloca.s   CS$0$0003
    IL_004b:  initobj    TailcallVerify.ValueType3Bytes
    IL_0051:  ldloc.3
    IL_0052:  ldloca.s   CS$0$0004
    IL_0054:  initobj    TailcallVerify.ValueType5Bytes
    IL_005a:  ldloc.s    CS$0$0004
    IL_005c:  tail. call       void TailcallVerify.Condition6::Callee4(valuetype TailcallVerify.ValueType3Bytes,
                                                                 valuetype TailcallVerify.ValueType5Bytes,
                                                                 valuetype TailcallVerify.ValueTypeSingleInt64,
                                                                 valuetype TailcallVerify.ValueType3Bytes,
                                                                 valuetype TailcallVerify.ValueType5Bytes)
    IL_0061:  ret
  } // end of method Condition6::Caller4

  .method private hidebysig static void  Callee4(valuetype TailcallVerify.ValueType3Bytes v1,
                                                 valuetype TailcallVerify.ValueType5Bytes v2,
                                                 valuetype TailcallVerify.ValueTypeSingleInt64 v3,
                                                 valuetype TailcallVerify.ValueType3Bytes v4,
                                                 valuetype TailcallVerify.ValueType5Bytes v5) cil managed noinlining
  {
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldarga.s   v1
    IL_0002:  ldc.i4.1
    IL_0003:  volatile.
    IL_0005:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition6::zero
    IL_000a:  div
    IL_000b:  conv.i2
    IL_000c:  stfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_0011:  ret
  } // end of method Condition6::Callee4

  .method public hidebysig static int32  Test5() cil managed nooptimization
  {
    // Code size       136 (0x88)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition6.Test5 - Caller: Arguments: No"
    + "ne - ReturnType: void; Callee: Arguments: 3 byte struct - ReturnType: v"
    + "oid"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition6::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition6::Caller5()
      IL_0016:  leave.s    IL_005e

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_001f:  ldstr      "Caller"
      IL_0024:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_0029:  ldc.i4.m1
      IL_002a:  beq.s      IL_005c

      IL_002c:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0031:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0036:  ldstr      "------------------------------------------------"
      IL_003b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0040:  call       void [System.Console]System.Console::WriteLine()
      IL_0045:  ldloc.0
      IL_0046:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_004b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0050:  call       void [System.Console]System.Console::WriteLine()
      IL_0055:  ldc.i4.s   101
      IL_0057:  stsfld     int32 TailcallVerify.Condition6::Result
      IL_005c:  leave.s    IL_005e

    }  // end handler
    IL_005e:  ldstr      "Execution finished - Test "
    IL_0063:  ldsfld     int32 TailcallVerify.Condition6::Result
    IL_0068:  ldc.i4.s   100
    IL_006a:  beq.s      IL_0073

    IL_006c:  ldstr      "FAILED"
    IL_0071:  br.s       IL_0078

    IL_0073:  ldstr      "PASSED"
    IL_0078:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_007d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0082:  ldsfld     int32 TailcallVerify.Condition6::Result
    IL_0087:  ret
  } // end of method Condition6::Test5

  .method private hidebysig static void  Caller5() cil managed
  {
    // Code size       108 (0x6c)
    .maxstack  6
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes CS$0$0000,
             [1] valuetype TailcallVerify.ValueType5Bytes CS$0$0001,
             [2] valuetype TailcallVerify.ValueTypeSingleInt64 CS$0$0002,
             [3] valuetype TailcallVerify.ValueType3Bytes CS$0$0003,
             [4] valuetype TailcallVerify.ValueType5Bytes CS$0$0004,
             [5] valuetype TailcallVerify.ValueType3Bytes CS$0$0005)
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition6::Result
    IL_002e:  ldloca.s   CS$0$0000
    IL_0030:  initobj    TailcallVerify.ValueType3Bytes
    IL_0036:  ldloc.0
    IL_0037:  ldloca.s   CS$0$0001
    IL_0039:  initobj    TailcallVerify.ValueType5Bytes
    IL_003f:  ldloc.1
    IL_0040:  ldloca.s   CS$0$0002
    IL_0042:  initobj    TailcallVerify.ValueTypeSingleInt64
    IL_0048:  ldloc.2
    IL_0049:  ldloca.s   CS$0$0003
    IL_004b:  initobj    TailcallVerify.ValueType3Bytes
    IL_0051:  ldloc.3
    IL_0052:  ldloca.s   CS$0$0004
    IL_0054:  initobj    TailcallVerify.ValueType5Bytes
    IL_005a:  ldloc.s    CS$0$0004
    IL_005c:  ldloca.s   CS$0$0005
    IL_005e:  initobj    TailcallVerify.ValueType3Bytes
    IL_0064:  ldloc.s    CS$0$0005
    IL_0066:  tail. call       void TailcallVerify.Condition6::Callee5(valuetype TailcallVerify.ValueType3Bytes,
                                                                 valuetype TailcallVerify.ValueType5Bytes,
                                                                 valuetype TailcallVerify.ValueTypeSingleInt64,
                                                                 valuetype TailcallVerify.ValueType3Bytes,
                                                                 valuetype TailcallVerify.ValueType5Bytes,
                                                                 valuetype TailcallVerify.ValueType3Bytes)
    IL_006b:  ret
  } // end of method Condition6::Caller5

  .method private hidebysig static void  Callee5(valuetype TailcallVerify.ValueType3Bytes v1,
                                                 valuetype TailcallVerify.ValueType5Bytes v2,
                                                 valuetype TailcallVerify.ValueTypeSingleInt64 v3,
                                                 valuetype TailcallVerify.ValueType3Bytes v4,
                                                 valuetype TailcallVerify.ValueType5Bytes v5,
                                                 valuetype TailcallVerify.ValueType3Bytes v6) cil managed noinlining
  {
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldarga.s   v1
    IL_0002:  ldc.i4.1
    IL_0003:  volatile.
    IL_0005:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition6::zero
    IL_000a:  div
    IL_000b:  conv.i2
    IL_000c:  stfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_0011:  ret
  } // end of method Condition6::Callee5

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Condition6::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  volatile.
    IL_0003:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition6::zero
    IL_0008:  ldc.i4.s   100
    IL_000a:  stsfld     int32 TailcallVerify.Condition6::Result
    IL_000f:  ret
  } // end of method Condition6::.cctor

} // end of class TailcallVerify.Condition6

.class private auto ansi beforefieldinit TailcallVerify.Condition5
       extends [mscorlib]System.Object
{
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) dummy
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .field private static int32 Result
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       104 (0x68)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition5.Test1 - There is a 'conv.i2' "
    + "IL instruction between the tail. call to the Callee and the ret IL instructio"
    + "n which is not allowed. Expecting to catch an InvalidProgramException i"
    + "nternally if tail. prefix is present."
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition5::Result
    .try
    {
      IL_0011:  call       int16 TailcallVerify.Condition5::Caller1()
      IL_0016:  pop
      IL_0017:  leave.s    IL_003e

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0019:  stloc.0
      IL_001a:  ldloc.0
      IL_001b:  isinst     [mscorlib]System.InvalidProgramException
      IL_0020:  brtrue.s   IL_002a

      IL_0022:  ldloc.0
      IL_0023:  isinst     [mscorlib]System.DivideByZeroException
      IL_0028:  brfalse.s  IL_0033

      IL_002a:  ldc.i4.s   100
      IL_002c:  stsfld     int32 TailcallVerify.Condition5::Result
      IL_0031:  br.s       IL_003c

      IL_0033:  ldc.i4.s   101
      IL_0035:  stsfld     int32 TailcallVerify.Condition5::Result
      IL_003a:  rethrow
      IL_003c:  leave.s    IL_003e

    }  // end handler
    IL_003e:  ldstr      "Execution finished - Test "
    IL_0043:  ldsfld     int32 TailcallVerify.Condition5::Result
    IL_0048:  ldc.i4.s   100
    IL_004a:  beq.s      IL_0053

    IL_004c:  ldstr      "FAILED"
    IL_0051:  br.s       IL_0058

    IL_0053:  ldstr      "PASSED"
    IL_0058:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_005d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0062:  ldsfld     int32 TailcallVerify.Condition5::Result
    IL_0067:  ret
  } // end of method Condition5::Test1

  .method private hidebysig static int16 
          Caller1() cil managed
  {
    // Code size       53 (0x35)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition5::Result
    IL_002e:  tail. call       int32 TailcallVerify.Condition5::Callee1()
    IL_0033:  conv.i2
    IL_0034:  ret
  } // end of method Condition5::Caller1

  .method private hidebysig static int32 
          Callee1() cil managed noinlining
  {
    // Code size       12 (0xc)
    .maxstack  2
    .locals init ([0] int32 i)
    IL_0000:  ldc.i4.1
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition5::zero
    IL_0008:  div
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method Condition5::Callee1

  .method public hidebysig static int32  Test2() cil managed nooptimization
  {
    // Code size       154 (0x9a)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition5.Test2 - Caller: Arguments: No"
    + "ne - ReturnType: byte; Callee: Arguments: None - ReturnType: byte"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition5::Result
    .try
    {
      IL_0011:  call       uint8 TailcallVerify.Condition5::Caller2()
      IL_0016:  pop
      IL_0017:  leave.s    IL_0070

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0019:  stloc.0
      IL_001a:  ldloc.0
      IL_001b:  isinst     [mscorlib]System.DivideByZeroException
      IL_0020:  brtrue.s   IL_002b

      IL_0022:  ldc.i4.s   101
      IL_0024:  stsfld     int32 TailcallVerify.Condition5::Result
      IL_0029:  rethrow
      IL_002b:  ldloc.0
      IL_002c:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0031:  ldstr      "Caller"
      IL_0036:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003b:  ldc.i4.m1
      IL_003c:  beq.s      IL_006e

      IL_003e:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0043:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0048:  ldstr      "------------------------------------------------"
      IL_004d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0052:  call       void [System.Console]System.Console::WriteLine()
      IL_0057:  ldloc.0
      IL_0058:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0062:  call       void [System.Console]System.Console::WriteLine()
      IL_0067:  ldc.i4.s   101
      IL_0069:  stsfld     int32 TailcallVerify.Condition5::Result
      IL_006e:  leave.s    IL_0070

    }  // end handler
    IL_0070:  ldstr      "Execution finished - Test "
    IL_0075:  ldsfld     int32 TailcallVerify.Condition5::Result
    IL_007a:  ldc.i4.s   100
    IL_007c:  beq.s      IL_0085

    IL_007e:  ldstr      "FAILED"
    IL_0083:  br.s       IL_008a

    IL_0085:  ldstr      "PASSED"
    IL_008a:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0094:  ldsfld     int32 TailcallVerify.Condition5::Result
    IL_0099:  ret
  } // end of method Condition5::Test2

  .method private hidebysig static uint8 
          Caller2() cil managed
  {
    // Code size       52 (0x34)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition5::Result
    IL_002e:  tail. call       uint8 TailcallVerify.Condition5::Callee2()
    IL_0033:  ret
  } // end of method Condition5::Caller2

  .method private hidebysig static uint8 
          Callee2() cil managed noinlining
  {
    // Code size       17 (0x11)
    .maxstack  8
    IL_0000:  volatile.
    IL_0002:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition5::dummy
    IL_0007:  volatile.
    IL_0009:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition5::zero
    IL_000e:  div
    IL_000f:  conv.u1
    IL_0010:  ret
  } // end of method Condition5::Callee2

  .method public hidebysig static int32  Test3() cil managed nooptimization
  {
    // Code size       154 (0x9a)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition5.Test3 - Caller: Arguments: No"
    + "ne - ReturnType: Int32; Callee: Arguments: None - ReturnType: Int32"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition5::Result
    .try
    {
      IL_0011:  call       int32 TailcallVerify.Condition5::Caller3()
      IL_0016:  pop
      IL_0017:  leave.s    IL_0070

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0019:  stloc.0
      IL_001a:  ldloc.0
      IL_001b:  isinst     [mscorlib]System.DivideByZeroException
      IL_0020:  brtrue.s   IL_002b

      IL_0022:  ldc.i4.s   101
      IL_0024:  stsfld     int32 TailcallVerify.Condition5::Result
      IL_0029:  rethrow
      IL_002b:  ldloc.0
      IL_002c:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0031:  ldstr      "Caller"
      IL_0036:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003b:  ldc.i4.m1
      IL_003c:  beq.s      IL_006e

      IL_003e:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0043:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0048:  ldstr      "------------------------------------------------"
      IL_004d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0052:  call       void [System.Console]System.Console::WriteLine()
      IL_0057:  ldloc.0
      IL_0058:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0062:  call       void [System.Console]System.Console::WriteLine()
      IL_0067:  ldc.i4.s   101
      IL_0069:  stsfld     int32 TailcallVerify.Condition5::Result
      IL_006e:  leave.s    IL_0070

    }  // end handler
    IL_0070:  ldstr      "Execution finished - Test "
    IL_0075:  ldsfld     int32 TailcallVerify.Condition5::Result
    IL_007a:  ldc.i4.s   100
    IL_007c:  beq.s      IL_0085

    IL_007e:  ldstr      "FAILED"
    IL_0083:  br.s       IL_008a

    IL_0085:  ldstr      "PASSED"
    IL_008a:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0094:  ldsfld     int32 TailcallVerify.Condition5::Result
    IL_0099:  ret
  } // end of method Condition5::Test3

  .method private hidebysig static int32 
          Caller3() cil managed
  {
    // Code size       52 (0x34)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition5::Result
    IL_002e:  tail. call       int32 TailcallVerify.Condition5::Callee3()
    IL_0033:  ret
  } // end of method Condition5::Caller3

  .method private hidebysig static int32 
          Callee3() cil managed noinlining
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  volatile.
    IL_0002:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition5::dummy
    IL_0007:  volatile.
    IL_0009:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition5::zero
    IL_000e:  div
    IL_000f:  ret
  } // end of method Condition5::Callee3

  .method public hidebysig static int32  Test4() cil managed nooptimization
  {
    // Code size       154 (0x9a)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition5.Test4 - Caller: Arguments: No"
    + "ne - ReturnType: UInt32; Callee: Arguments: None - ReturnType: Int32"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition5::Result
    .try
    {
      IL_0011:  call       uint32 TailcallVerify.Condition5::Caller4()
      IL_0016:  pop
      IL_0017:  leave.s    IL_0070

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0019:  stloc.0
      IL_001a:  ldloc.0
      IL_001b:  isinst     [mscorlib]System.DivideByZeroException
      IL_0020:  brtrue.s   IL_002b

      IL_0022:  ldc.i4.s   101
      IL_0024:  stsfld     int32 TailcallVerify.Condition5::Result
      IL_0029:  rethrow
      IL_002b:  ldloc.0
      IL_002c:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0031:  ldstr      "Caller"
      IL_0036:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003b:  ldc.i4.m1
      IL_003c:  beq.s      IL_006e

      IL_003e:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0043:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0048:  ldstr      "------------------------------------------------"
      IL_004d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0052:  call       void [System.Console]System.Console::WriteLine()
      IL_0057:  ldloc.0
      IL_0058:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0062:  call       void [System.Console]System.Console::WriteLine()
      IL_0067:  ldc.i4.s   101
      IL_0069:  stsfld     int32 TailcallVerify.Condition5::Result
      IL_006e:  leave.s    IL_0070

    }  // end handler
    IL_0070:  ldstr      "Execution finished - Test "
    IL_0075:  ldsfld     int32 TailcallVerify.Condition5::Result
    IL_007a:  ldc.i4.s   100
    IL_007c:  beq.s      IL_0085

    IL_007e:  ldstr      "FAILED"
    IL_0083:  br.s       IL_008a

    IL_0085:  ldstr      "PASSED"
    IL_008a:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0094:  ldsfld     int32 TailcallVerify.Condition5::Result
    IL_0099:  ret
  } // end of method Condition5::Test4

  .method private hidebysig static uint32 
          Caller4() cil managed
  {
    // Code size       52 (0x34)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition5::Result
    IL_002e:  tail. call       int32 TailcallVerify.Condition5::Callee4()
    IL_0033:  ret
  } // end of method Condition5::Caller4

  .method private hidebysig static int32 
          Callee4() cil managed noinlining
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  volatile.
    IL_0002:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition5::dummy
    IL_0007:  volatile.
    IL_0009:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition5::zero
    IL_000e:  div
    IL_000f:  ret
  } // end of method Condition5::Callee4

  .method public hidebysig static int32  Test5() cil managed nooptimization
  {
    // Code size       154 (0x9a)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition5.Test5 - Caller: Arguments: No"
    + "ne - ReturnType: Int32; Callee: Arguments: None - ReturnType: UInt32"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition5::Result
    .try
    {
      IL_0011:  call       int32 TailcallVerify.Condition5::Caller5()
      IL_0016:  pop
      IL_0017:  leave.s    IL_0070

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0019:  stloc.0
      IL_001a:  ldloc.0
      IL_001b:  isinst     [mscorlib]System.DivideByZeroException
      IL_0020:  brtrue.s   IL_002b

      IL_0022:  ldc.i4.s   101
      IL_0024:  stsfld     int32 TailcallVerify.Condition5::Result
      IL_0029:  rethrow
      IL_002b:  ldloc.0
      IL_002c:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0031:  ldstr      "Caller"
      IL_0036:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003b:  ldc.i4.m1
      IL_003c:  beq.s      IL_006e

      IL_003e:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0043:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0048:  ldstr      "------------------------------------------------"
      IL_004d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0052:  call       void [System.Console]System.Console::WriteLine()
      IL_0057:  ldloc.0
      IL_0058:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0062:  call       void [System.Console]System.Console::WriteLine()
      IL_0067:  ldc.i4.s   101
      IL_0069:  stsfld     int32 TailcallVerify.Condition5::Result
      IL_006e:  leave.s    IL_0070

    }  // end handler
    IL_0070:  ldstr      "Execution finished - Test "
    IL_0075:  ldsfld     int32 TailcallVerify.Condition5::Result
    IL_007a:  ldc.i4.s   100
    IL_007c:  beq.s      IL_0085

    IL_007e:  ldstr      "FAILED"
    IL_0083:  br.s       IL_008a

    IL_0085:  ldstr      "PASSED"
    IL_008a:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0094:  ldsfld     int32 TailcallVerify.Condition5::Result
    IL_0099:  ret
  } // end of method Condition5::Test5

  .method private hidebysig static int32 
          Caller5() cil managed
  {
    // Code size       52 (0x34)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition5::Result
    IL_002e:  tail. call       uint32 TailcallVerify.Condition5::Callee5()
    IL_0033:  ret
  } // end of method Condition5::Caller5

  .method private hidebysig static uint32 
          Callee5() cil managed noinlining
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  volatile.
    IL_0002:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition5::dummy
    IL_0007:  volatile.
    IL_0009:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition5::zero
    IL_000e:  div
    IL_000f:  ret
  } // end of method Condition5::Callee5

  .method public hidebysig static int32  Test6() cil managed nooptimization
  {
    // Code size       154 (0x9a)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition5.Test6 - Caller: Arguments: No"
    + "ne - ReturnType: Int64; Callee: Arguments: None - ReturnType: UInt64"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition5::Result
    .try
    {
      IL_0011:  call       int64 TailcallVerify.Condition5::Caller6()
      IL_0016:  pop
      IL_0017:  leave.s    IL_0070

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0019:  stloc.0
      IL_001a:  ldloc.0
      IL_001b:  isinst     [mscorlib]System.DivideByZeroException
      IL_0020:  brtrue.s   IL_002b

      IL_0022:  ldc.i4.s   101
      IL_0024:  stsfld     int32 TailcallVerify.Condition5::Result
      IL_0029:  rethrow
      IL_002b:  ldloc.0
      IL_002c:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0031:  ldstr      "Caller"
      IL_0036:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003b:  ldc.i4.m1
      IL_003c:  beq.s      IL_006e

      IL_003e:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0043:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0048:  ldstr      "------------------------------------------------"
      IL_004d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0052:  call       void [System.Console]System.Console::WriteLine()
      IL_0057:  ldloc.0
      IL_0058:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0062:  call       void [System.Console]System.Console::WriteLine()
      IL_0067:  ldc.i4.s   101
      IL_0069:  stsfld     int32 TailcallVerify.Condition5::Result
      IL_006e:  leave.s    IL_0070

    }  // end handler
    IL_0070:  ldstr      "Execution finished - Test "
    IL_0075:  ldsfld     int32 TailcallVerify.Condition5::Result
    IL_007a:  ldc.i4.s   100
    IL_007c:  beq.s      IL_0085

    IL_007e:  ldstr      "FAILED"
    IL_0083:  br.s       IL_008a

    IL_0085:  ldstr      "PASSED"
    IL_008a:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0094:  ldsfld     int32 TailcallVerify.Condition5::Result
    IL_0099:  ret
  } // end of method Condition5::Test6

  .method private hidebysig static int64 
          Caller6() cil managed
  {
    // Code size       52 (0x34)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition5::Result
    IL_002e:  tail. call       uint64 TailcallVerify.Condition5::Callee6()
    IL_0033:  ret
  } // end of method Condition5::Caller6

  .method private hidebysig static uint64 
          Callee6() cil managed noinlining
  {
    // Code size       17 (0x11)
    .maxstack  8
    IL_0000:  volatile.
    IL_0002:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition5::dummy
    IL_0007:  volatile.
    IL_0009:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition5::zero
    IL_000e:  div
    IL_000f:  conv.i8
    IL_0010:  ret
  } // end of method Condition5::Callee6

  .method public hidebysig static int32  Test7() cil managed nooptimization
  {
    // Code size       154 (0x9a)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition5.Test7 - Caller: Arguments: No"
    + "ne - ReturnType: UInt64; Callee: Arguments: None - ReturnType: Int64"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition5::Result
    .try
    {
      IL_0011:  call       int64 TailcallVerify.Condition5::Caller7()
      IL_0016:  pop
      IL_0017:  leave.s    IL_0070

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0019:  stloc.0
      IL_001a:  ldloc.0
      IL_001b:  isinst     [mscorlib]System.DivideByZeroException
      IL_0020:  brtrue.s   IL_002b

      IL_0022:  ldc.i4.s   101
      IL_0024:  stsfld     int32 TailcallVerify.Condition5::Result
      IL_0029:  rethrow
      IL_002b:  ldloc.0
      IL_002c:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0031:  ldstr      "Caller"
      IL_0036:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003b:  ldc.i4.m1
      IL_003c:  beq.s      IL_006e

      IL_003e:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0043:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0048:  ldstr      "------------------------------------------------"
      IL_004d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0052:  call       void [System.Console]System.Console::WriteLine()
      IL_0057:  ldloc.0
      IL_0058:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0062:  call       void [System.Console]System.Console::WriteLine()
      IL_0067:  ldc.i4.s   101
      IL_0069:  stsfld     int32 TailcallVerify.Condition5::Result
      IL_006e:  leave.s    IL_0070

    }  // end handler
    IL_0070:  ldstr      "Execution finished - Test "
    IL_0075:  ldsfld     int32 TailcallVerify.Condition5::Result
    IL_007a:  ldc.i4.s   100
    IL_007c:  beq.s      IL_0085

    IL_007e:  ldstr      "FAILED"
    IL_0083:  br.s       IL_008a

    IL_0085:  ldstr      "PASSED"
    IL_008a:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0094:  ldsfld     int32 TailcallVerify.Condition5::Result
    IL_0099:  ret
  } // end of method Condition5::Test7

  .method private hidebysig static int64 
          Caller7() cil managed
  {
    // Code size       52 (0x34)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition5::Result
    IL_002e:  tail. call       uint64 TailcallVerify.Condition5::Callee7()
    IL_0033:  ret
  } // end of method Condition5::Caller7

  .method private hidebysig static uint64 
          Callee7() cil managed noinlining
  {
    // Code size       17 (0x11)
    .maxstack  8
    IL_0000:  volatile.
    IL_0002:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition5::dummy
    IL_0007:  volatile.
    IL_0009:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition5::zero
    IL_000e:  div
    IL_000f:  conv.i8
    IL_0010:  ret
  } // end of method Condition5::Callee7

  .method public hidebysig static int32  Test8() cil managed nooptimization
  {
    // Code size       104 (0x68)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition5.Test8 - Caller: Arguments: No"
    + "ne - ReturnType: UInt64; Callee: Arguments: None - ReturnType: UInt32 -"
    + " Expecting a InvalidProgramException"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition5::Result
    .try
    {
      IL_0011:  call       int64 TailcallVerify.Condition5::Caller8()
      IL_0016:  pop
      IL_0017:  leave.s    IL_003e

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0019:  stloc.0
      IL_001a:  ldloc.0
      IL_001b:  isinst     [mscorlib]System.InvalidProgramException
      IL_0020:  brtrue.s   IL_002a

      IL_0022:  ldloc.0
      IL_0023:  isinst     [mscorlib]System.DivideByZeroException
      IL_0028:  brfalse.s  IL_0033

      IL_002a:  ldc.i4.s   100
      IL_002c:  stsfld     int32 TailcallVerify.Condition5::Result
      IL_0031:  br.s       IL_003c

      IL_0033:  ldc.i4.s   101
      IL_0035:  stsfld     int32 TailcallVerify.Condition5::Result
      IL_003a:  rethrow
      IL_003c:  leave.s    IL_003e

    }  // end handler
    IL_003e:  ldstr      "Execution finished - Test "
    IL_0043:  ldsfld     int32 TailcallVerify.Condition5::Result
    IL_0048:  ldc.i4.s   100
    IL_004a:  beq.s      IL_0053

    IL_004c:  ldstr      "FAILED"
    IL_0051:  br.s       IL_0058

    IL_0053:  ldstr      "PASSED"
    IL_0058:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_005d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0062:  ldsfld     int32 TailcallVerify.Condition5::Result
    IL_0067:  ret
  } // end of method Condition5::Test8

  .method private hidebysig static int64 
          Caller8() cil managed
  {
    // Code size       57 (0x39)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition5::Result
    IL_002e:  tail. call       valuetype TailcallVerify.ValueTypeSingleInt64 TailcallVerify.Condition5::Callee8()
    IL_0033:  ldfld      int64 TailcallVerify.ValueTypeSingleInt64::i1
    IL_0038:  ret
  } // end of method Condition5::Caller8

  .method private hidebysig static valuetype TailcallVerify.ValueTypeSingleInt64 
          Callee8() cil managed noinlining
  {
    // Code size       33 (0x21)
    .maxstack  3
    .locals init ([0] valuetype TailcallVerify.ValueTypeSingleInt64 v)
    IL_0000:  ldloca.s   v
    IL_0002:  initobj    TailcallVerify.ValueTypeSingleInt64
    IL_0008:  ldloca.s   v
    IL_000a:  volatile.
    IL_000c:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition5::dummy
    IL_0011:  volatile.
    IL_0013:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition5::zero
    IL_0018:  div
    IL_0019:  conv.i8
    IL_001a:  stfld      int64 TailcallVerify.ValueTypeSingleInt64::i1
    IL_001f:  ldloc.0
    IL_0020:  ret
  } // end of method Condition5::Callee8

  .method public hidebysig static int32  Test9() cil managed nooptimization
  {
    // Code size       104 (0x68)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition5.Test9 - Caller: Arguments: No"
    + "ne - ReturnType: UInt32; Callee: Arguments: None - ReturnType: UInt64 -"
    + " Expecting a InvalidProgramException"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition5::Result
    .try
    {
      IL_0011:  call       uint32 TailcallVerify.Condition5::Caller9()
      IL_0016:  pop
      IL_0017:  leave.s    IL_003e

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0019:  stloc.0
      IL_001a:  ldloc.0
      IL_001b:  isinst     [mscorlib]System.InvalidProgramException
      IL_0020:  brtrue.s   IL_002a

      IL_0022:  ldloc.0
      IL_0023:  isinst     [mscorlib]System.DivideByZeroException
      IL_0028:  brfalse.s  IL_0033

      IL_002a:  ldc.i4.s   100
      IL_002c:  stsfld     int32 TailcallVerify.Condition5::Result
      IL_0031:  br.s       IL_003c

      IL_0033:  ldc.i4.s   101
      IL_0035:  stsfld     int32 TailcallVerify.Condition5::Result
      IL_003a:  rethrow
      IL_003c:  leave.s    IL_003e

    }  // end handler
    IL_003e:  ldstr      "Execution finished - Test "
    IL_0043:  ldsfld     int32 TailcallVerify.Condition5::Result
    IL_0048:  ldc.i4.s   100
    IL_004a:  beq.s      IL_0053

    IL_004c:  ldstr      "FAILED"
    IL_0051:  br.s       IL_0058

    IL_0053:  ldstr      "PASSED"
    IL_0058:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_005d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0062:  ldsfld     int32 TailcallVerify.Condition5::Result
    IL_0067:  ret
  } // end of method Condition5::Test9

  .method private hidebysig static uint32 
          Caller9() cil managed
  {
    // Code size       53 (0x35)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition5::Result
    IL_002e:  tail. call       uint64 TailcallVerify.Condition5::Callee9()
    IL_0033:  conv.u4
    IL_0034:  ret
  } // end of method Condition5::Caller9

  .method private hidebysig static uint64 
          Callee9() cil managed noinlining
  {
    // Code size       17 (0x11)
    .maxstack  8
    IL_0000:  volatile.
    IL_0002:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition5::dummy
    IL_0007:  volatile.
    IL_0009:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition5::zero
    IL_000e:  div
    IL_000f:  conv.i8
    IL_0010:  ret
  } // end of method Condition5::Callee9

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Condition5::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  volatile.
    IL_0003:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition5::dummy
    IL_0008:  ldc.i4.0
    IL_0009:  volatile.
    IL_000b:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition5::zero
    IL_0010:  ldc.i4.s   100
    IL_0012:  stsfld     int32 TailcallVerify.Condition5::Result
    IL_0017:  ret
  } // end of method Condition5::.cctor

} // end of class TailcallVerify.Condition5

.class private auto ansi beforefieldinit TailcallVerify.Condition4
       extends [mscorlib]System.Object
{
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .field public static int32 Result
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       154 (0x9a)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition4.Test1 - Caller: Arguments: No"
    + "ne - ReturnType: 3 byte struct; Callee: Arguments: None - ReturnType: 3"
    + " byte struct"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition4::Result
    .try
    {
      IL_0011:  call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition4::Caller1()
      IL_0016:  pop
      IL_0017:  leave.s    IL_0070

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0019:  stloc.0
      IL_001a:  ldloc.0
      IL_001b:  isinst     [mscorlib]System.DivideByZeroException
      IL_0020:  brtrue.s   IL_002b

      IL_0022:  ldc.i4.s   101
      IL_0024:  stsfld     int32 TailcallVerify.Condition4::Result
      IL_0029:  rethrow
      IL_002b:  ldloc.0
      IL_002c:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0031:  ldstr      "Caller"
      IL_0036:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003b:  ldc.i4.m1
      IL_003c:  beq.s      IL_006e

      IL_003e:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0043:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0048:  ldstr      "------------------------------------------------"
      IL_004d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0052:  call       void [System.Console]System.Console::WriteLine()
      IL_0057:  ldloc.0
      IL_0058:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0062:  call       void [System.Console]System.Console::WriteLine()
      IL_0067:  ldc.i4.s   101
      IL_0069:  stsfld     int32 TailcallVerify.Condition4::Result
      IL_006e:  leave.s    IL_0070

    }  // end handler
    IL_0070:  ldstr      "Execution finished - Test "
    IL_0075:  ldsfld     int32 TailcallVerify.Condition4::Result
    IL_007a:  ldc.i4.s   100
    IL_007c:  beq.s      IL_0085

    IL_007e:  ldstr      "FAILED"
    IL_0083:  br.s       IL_008a

    IL_0085:  ldstr      "PASSED"
    IL_008a:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0094:  ldsfld     int32 TailcallVerify.Condition4::Result
    IL_0099:  ret
  } // end of method Condition4::Test1

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          Caller1() cil managed
  {
    // Code size       52 (0x34)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition4::Result
    IL_002e:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition4::Callee1()
    IL_0033:  ret
  } // end of method Condition4::Caller1

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          Callee1() cil managed noinlining
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes v)
    IL_0000:  ldloca.s   v
    IL_0002:  initobj    TailcallVerify.ValueType3Bytes
    IL_0008:  ldloca.s   v
    IL_000a:  ldc.i4.1
    IL_000b:  volatile.
    IL_000d:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition4::zero
    IL_0012:  div
    IL_0013:  conv.i2
    IL_0014:  stfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method Condition4::Callee1

  .method public hidebysig static int32  Test2() cil managed nooptimization
  {
    // Code size       172 (0xac)
    .maxstack  3
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes v,
             [1] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition4.Test2 - Caller: Arguments: No"
    + "ne - ReturnType: 3 byte struct; Callee: Arguments: None - ReturnType: 3"
    + " byte struct [Verifying the field values in the return type struct]"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition4::Result
    .try
    {
      IL_0011:  call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition4::Caller2()
      IL_0016:  stloc.0
      IL_0017:  ldloca.s   v
      IL_0019:  ldfld      uint8 TailcallVerify.ValueType3Bytes::i1
      IL_001e:  brtrue.s   IL_002e

      IL_0020:  ldloca.s   v
      IL_0022:  ldfld      int16 TailcallVerify.ValueType3Bytes::i2
      IL_0027:  ldc.i4     0x7fff
      IL_002c:  beq.s      IL_0061

      IL_002e:  ldc.i4.s   101
      IL_0030:  stsfld     int32 TailcallVerify.Condition4::Result
      IL_0035:  ldstr      "FAILED: The fields in the return type struct have "
      + "the wrong values."
      IL_003a:  call       void [System.Console]System.Console::WriteLine(string)
      IL_003f:  ldstr      "v.i1: != byte.MinValue || v.i2: != short.M"
      + "axValue"
                call       void [System.Console]System.Console::WriteLine(string)
      IL_0044:  ldloca.s   v
      IL_0046:  ldfld      uint8 TailcallVerify.ValueType3Bytes::i1
      IL_004b:  box        [mscorlib]System.Byte
                callvirt   instance string [mscorlib]System.Object::ToString()
                call       void [System.Console]System.Console::WriteLine(string)
      IL_0050:  ldloca.s   v
      IL_0052:  ldfld      int16 TailcallVerify.ValueType3Bytes::i2
      IL_0057:  box        [mscorlib]System.Int16
                callvirt   instance string [mscorlib]System.Object::ToString()
      IL_005c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0061:  leave.s    IL_0082

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0063:  stloc.1
      IL_0064:  ldc.i4.s   101
      IL_0066:  stsfld     int32 TailcallVerify.Condition4::Result
      IL_006b:  ldstr      "FAILED: Unexpected exception: "
      IL_0070:  ldloc.1
      IL_0071:  callvirt   instance string [mscorlib]System.Object::ToString()
      IL_0076:  call       string [mscorlib]System.String::Concat(string,
                                                                  string)
      IL_007b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0080:  leave.s    IL_0082

    }  // end handler
    IL_0082:  ldstr      "Execution finished - Test "
    IL_0087:  ldsfld     int32 TailcallVerify.Condition4::Result
    IL_008c:  ldc.i4.s   100
    IL_008e:  beq.s      IL_0097

    IL_0090:  ldstr      "FAILED"
    IL_0095:  br.s       IL_009c

    IL_0097:  ldstr      "PASSED"
    IL_009c:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_00a1:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00a6:  ldsfld     int32 TailcallVerify.Condition4::Result
    IL_00ab:  ret
  } // end of method Condition4::Test2

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          Caller2() cil managed
  {
    // Code size       52 (0x34)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition4::Result
    IL_002e:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition4::Callee2()
    IL_0033:  ret
  } // end of method Condition4::Caller2

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          Callee2() cil managed noinlining
  {
    // Code size       30 (0x1e)
    .maxstack  2
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes v)
    IL_0000:  ldloca.s   v
    IL_0002:  initobj    TailcallVerify.ValueType3Bytes
    IL_0008:  ldloca.s   v
    IL_000a:  ldc.i4.0
    IL_000b:  stfld      uint8 TailcallVerify.ValueType3Bytes::i1
    IL_0010:  ldloca.s   v
    IL_0012:  ldc.i4     0x7fff
    IL_0017:  stfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_001c:  ldloc.0
    IL_001d:  ret
  } // end of method Condition4::Callee2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Condition4::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  volatile.
    IL_0003:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition4::zero
    IL_0008:  ldc.i4.s   100
    IL_000a:  stsfld     int32 TailcallVerify.Condition4::Result
    IL_000f:  ret
  } // end of method Condition4::.cctor

} // end of class TailcallVerify.Condition4

.class private auto ansi beforefieldinit TailcallVerify.Condition3
       extends [mscorlib]System.Object
{
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) dummy
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .field private static int32 Result
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       154 (0x9a)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition3.Test1 - Caller: Arguments: No"
    + "ne - ReturnType: Int32; Callee: Arguments: None - ReturnType: Int32"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition3::Result
    .try
    {
      IL_0011:  call       int32 TailcallVerify.Condition3::Caller1()
      IL_0016:  pop
      IL_0017:  leave.s    IL_0070

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0019:  stloc.0
      IL_001a:  ldloc.0
      IL_001b:  isinst     [mscorlib]System.DivideByZeroException
      IL_0020:  brtrue.s   IL_002b

      IL_0022:  ldc.i4.s   101
      IL_0024:  stsfld     int32 TailcallVerify.Condition3::Result
      IL_0029:  rethrow
      IL_002b:  ldloc.0
      IL_002c:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0031:  ldstr      "Caller"
      IL_0036:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003b:  ldc.i4.m1
      IL_003c:  beq.s      IL_006e

      IL_003e:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0043:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0048:  ldstr      "------------------------------------------------"
      IL_004d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0052:  call       void [System.Console]System.Console::WriteLine()
      IL_0057:  ldloc.0
      IL_0058:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0062:  call       void [System.Console]System.Console::WriteLine()
      IL_0067:  ldc.i4.s   101
      IL_0069:  stsfld     int32 TailcallVerify.Condition3::Result
      IL_006e:  leave.s    IL_0070

    }  // end handler
    IL_0070:  ldstr      "Execution finished - Test "
    IL_0075:  ldsfld     int32 TailcallVerify.Condition3::Result
    IL_007a:  ldc.i4.s   100
    IL_007c:  beq.s      IL_0085

    IL_007e:  ldstr      "FAILED"
    IL_0083:  br.s       IL_008a

    IL_0085:  ldstr      "PASSED"
    IL_008a:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0094:  ldsfld     int32 TailcallVerify.Condition3::Result
    IL_0099:  ret
  } // end of method Condition3::Test1

  .method private hidebysig static int32 
          Caller1() cil managed
  {
    // Code size       52 (0x34)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition3::Result
    IL_002e:  tail. call       int32 TailcallVerify.Condition3::Callee1()
    IL_0033:  ret
  } // end of method Condition3::Caller1

  .method private hidebysig static int32 
          Callee1() cil managed noinlining
  {
    // Code size       12 (0xc)
    .maxstack  2
    .locals init ([0] int32 i)
    IL_0000:  ldc.i4.1
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition3::zero
    IL_0008:  div
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method Condition3::Callee1

  .method public hidebysig static int32  Test2() cil managed nooptimization
  {
    // Code size       154 (0x9a)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition3.Test2 - Caller: Arguments: No"
    + "ne - ReturnType: void; Callee: Arguments: None - ReturnType: string"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition3::Result
    .try
    {
      IL_0011:  call       string TailcallVerify.Condition3::Caller2()
      IL_0016:  pop
      IL_0017:  leave.s    IL_0070

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0019:  stloc.0
      IL_001a:  ldloc.0
      IL_001b:  isinst     [mscorlib]System.DivideByZeroException
      IL_0020:  brtrue.s   IL_002b

      IL_0022:  ldc.i4.s   101
      IL_0024:  stsfld     int32 TailcallVerify.Condition3::Result
      IL_0029:  rethrow
      IL_002b:  ldloc.0
      IL_002c:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0031:  ldstr      "Caller"
      IL_0036:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003b:  ldc.i4.m1
      IL_003c:  beq.s      IL_006e

      IL_003e:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0043:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0048:  ldstr      "------------------------------------------------"
      IL_004d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0052:  call       void [System.Console]System.Console::WriteLine()
      IL_0057:  ldloc.0
      IL_0058:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0062:  call       void [System.Console]System.Console::WriteLine()
      IL_0067:  ldc.i4.s   101
      IL_0069:  stsfld     int32 TailcallVerify.Condition3::Result
      IL_006e:  leave.s    IL_0070

    }  // end handler
    IL_0070:  ldstr      "Execution finished - Test "
    IL_0075:  ldsfld     int32 TailcallVerify.Condition3::Result
    IL_007a:  ldc.i4.s   100
    IL_007c:  beq.s      IL_0085

    IL_007e:  ldstr      "FAILED"
    IL_0083:  br.s       IL_008a

    IL_0085:  ldstr      "PASSED"
    IL_008a:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0094:  ldsfld     int32 TailcallVerify.Condition3::Result
    IL_0099:  ret
  } // end of method Condition3::Test2

  .method private hidebysig static string 
          Caller2() cil managed
  {
    // Code size       52 (0x34)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition3::Result
    IL_002e:  tail. call       string TailcallVerify.Condition3::Callee2()
    IL_0033:  ret
  } // end of method Condition3::Caller2

  .method private hidebysig static string 
          Callee2() cil managed noinlining
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  volatile.
    IL_0002:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition3::dummy
    IL_0007:  volatile.
    IL_0009:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition3::zero
    IL_000e:  div
    IL_000f:  conv.i2
    IL_0010:  volatile.
    IL_0012:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition3::dummy
    IL_0017:  ldstr      "foo"
    IL_001c:  ret
  } // end of method Condition3::Callee2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Condition3::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  volatile.
    IL_0003:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition3::dummy
    IL_0008:  ldc.i4.0
    IL_0009:  volatile.
    IL_000b:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition3::zero
    IL_0010:  ldc.i4.s   100
    IL_0012:  stsfld     int32 TailcallVerify.Condition3::Result
    IL_0017:  ret
  } // end of method Condition3::.cctor

} // end of class TailcallVerify.Condition3

.class private auto ansi beforefieldinit TailcallVerify.Condition2
       extends [mscorlib]System.Object
{
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) dummy
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .field private static int32 Result
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       153 (0x99)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition2.Test1  - Caller: Arguments: N"
    + "one - ReturnType: void; Callee: Arguments: 0 - ReturnType: Int32"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition2::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition2::Caller1()
      IL_0016:  leave.s    IL_006f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  isinst     [mscorlib]System.DivideByZeroException
      IL_001f:  brtrue.s   IL_002a

      IL_0021:  ldc.i4.s   101
      IL_0023:  stsfld     int32 TailcallVerify.Condition2::Result
      IL_0028:  rethrow
      IL_002a:  ldloc.0
      IL_002b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0030:  ldstr      "Caller"
      IL_0035:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003a:  ldc.i4.m1
      IL_003b:  beq.s      IL_006d

      IL_003d:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0042:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0047:  ldstr      "------------------------------------------------"
      IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0051:  call       void [System.Console]System.Console::WriteLine()
      IL_0056:  ldloc.0
      IL_0057:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0061:  call       void [System.Console]System.Console::WriteLine()
      IL_0066:  ldc.i4.s   101
      IL_0068:  stsfld     int32 TailcallVerify.Condition2::Result
      IL_006d:  leave.s    IL_006f

    }  // end handler
    IL_006f:  ldstr      "Execution finished - Test "
    IL_0074:  ldsfld     int32 TailcallVerify.Condition2::Result
    IL_0079:  ldc.i4.s   100
    IL_007b:  beq.s      IL_0084

    IL_007d:  ldstr      "FAILED"
    IL_0082:  br.s       IL_0089

    IL_0084:  ldstr      "PASSED"
    IL_0089:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0093:  ldsfld     int32 TailcallVerify.Condition2::Result
    IL_0098:  ret
  } // end of method Condition2::Test1

  .method private hidebysig static void  Caller1() cil managed
  {
    // Code size       53 (0x35)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition2::Result
    IL_002e:  tail. call       int32 TailcallVerify.Condition2::Callee1()
    IL_0033:  pop
    IL_0034:  ret
  } // end of method Condition2::Caller1

  .method private hidebysig static int32 
          Callee1() cil managed noinlining
  {
    // Code size       12 (0xc)
    .maxstack  2
    .locals init ([0] int32 i)
    IL_0000:  ldc.i4.1
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition2::zero
    IL_0008:  div
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method Condition2::Callee1

  .method public hidebysig static int32  Test2() cil managed nooptimization
  {
    // Code size       153 (0x99)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition2.Test2  - Caller: Arguments: N"
    + "one - ReturnType: void; Callee: Arguments: Int32 - ReturnType: string"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition2::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition2::Caller2()
      IL_0016:  leave.s    IL_006f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  isinst     [mscorlib]System.DivideByZeroException
      IL_001f:  brtrue.s   IL_002a

      IL_0021:  ldc.i4.s   101
      IL_0023:  stsfld     int32 TailcallVerify.Condition2::Result
      IL_0028:  rethrow
      IL_002a:  ldloc.0
      IL_002b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0030:  ldstr      "Caller"
      IL_0035:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003a:  ldc.i4.m1
      IL_003b:  beq.s      IL_006d

      IL_003d:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0042:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0047:  ldstr      "------------------------------------------------"
      IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0051:  call       void [System.Console]System.Console::WriteLine()
      IL_0056:  ldloc.0
      IL_0057:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0061:  call       void [System.Console]System.Console::WriteLine()
      IL_0066:  ldc.i4.s   101
      IL_0068:  stsfld     int32 TailcallVerify.Condition2::Result
      IL_006d:  leave.s    IL_006f

    }  // end handler
    IL_006f:  ldstr      "Execution finished - Test "
    IL_0074:  ldsfld     int32 TailcallVerify.Condition2::Result
    IL_0079:  ldc.i4.s   100
    IL_007b:  beq.s      IL_0084

    IL_007d:  ldstr      "FAILED"
    IL_0082:  br.s       IL_0089

    IL_0084:  ldstr      "PASSED"
    IL_0089:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0093:  ldsfld     int32 TailcallVerify.Condition2::Result
    IL_0098:  ret
  } // end of method Condition2::Test2

  .method private hidebysig static void  Caller2() cil managed
  {
    // Code size       54 (0x36)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition2::Result
    IL_002e:  ldc.i4.0
    IL_002f:  tail. call       string TailcallVerify.Condition2::Callee2(int32)
    IL_0034:  pop
    IL_0035:  ret
  } // end of method Condition2::Caller2

  .method private hidebysig static string 
          Callee2(int32 i) cil managed noinlining
  {
    // Code size       23 (0x17)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition2::zero
    IL_0008:  div
    IL_0009:  conv.i2
    IL_000a:  volatile.
    IL_000c:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition2::dummy
    IL_0011:  ldstr      "foo"
    IL_0016:  ret
  } // end of method Condition2::Callee2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Condition2::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  volatile.
    IL_0003:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition2::dummy
    IL_0008:  ldc.i4.0
    IL_0009:  volatile.
    IL_000b:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition2::zero
    IL_0010:  ldc.i4.s   100
    IL_0012:  stsfld     int32 TailcallVerify.Condition2::Result
    IL_0017:  ret
  } // end of method Condition2::.cctor

} // end of class TailcallVerify.Condition2

.class private auto ansi beforefieldinit TailcallVerify.Condition1
       extends [mscorlib]System.Object
{
  .field private static int32 Result
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       153 (0x99)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition1.Test1 - Caller: Arguments: No"
    + "ne - ReturnType: void; Callee: Arguments: None - ReturnType: 3 byte str"
    + "uct"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition1::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition1::Caller1()
      IL_0016:  leave.s    IL_006f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  isinst     [mscorlib]System.DivideByZeroException
      IL_001f:  brtrue.s   IL_002a

      IL_0021:  ldc.i4.s   101
      IL_0023:  stsfld     int32 TailcallVerify.Condition1::Result
      IL_0028:  rethrow
      IL_002a:  ldloc.0
      IL_002b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0030:  ldstr      "Caller"
      IL_0035:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003a:  ldc.i4.m1
      IL_003b:  beq.s      IL_006d

      IL_003d:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0042:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0047:  ldstr      "------------------------------------------------"
      IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0051:  call       void [System.Console]System.Console::WriteLine()
      IL_0056:  ldloc.0
      IL_0057:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0061:  call       void [System.Console]System.Console::WriteLine()
      IL_0066:  ldc.i4.s   101
      IL_0068:  stsfld     int32 TailcallVerify.Condition1::Result
      IL_006d:  leave.s    IL_006f

    }  // end handler
    IL_006f:  ldstr      "Execution finished - Test "
    IL_0074:  ldsfld     int32 TailcallVerify.Condition1::Result
    IL_0079:  ldc.i4.s   100
    IL_007b:  beq.s      IL_0084

    IL_007d:  ldstr      "FAILED"
    IL_0082:  br.s       IL_0089

    IL_0084:  ldstr      "PASSED"
    IL_0089:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0093:  ldsfld     int32 TailcallVerify.Condition1::Result
    IL_0098:  ret
  } // end of method Condition1::Test1

  .method private hidebysig static void  Caller1() cil managed
  {
    // Code size       53 (0x35)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition1::Result
    IL_002e:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition1::Callee1()
    IL_0033:  pop
    IL_0034:  ret
  } // end of method Condition1::Caller1

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          Callee1() cil managed noinlining
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes temp)
    IL_0000:  ldloca.s   temp
    IL_0002:  initobj    TailcallVerify.ValueType3Bytes
    IL_0008:  ldloca.s   temp
    IL_000a:  ldc.i4.1
    IL_000b:  volatile.
    IL_000d:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition1::zero
    IL_0012:  div
    IL_0013:  conv.i2
    IL_0014:  stfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method Condition1::Callee1

  .method public hidebysig static int32  Test2() cil managed nooptimization
  {
    // Code size       153 (0x99)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition1.Test2 - Caller: Arguments: No"
    + "ne - ReturnType: void; Callee: Arguments: Int32 - ReturnType: 3 byte st"
    + "ruct"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition1::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition1::Caller2()
      IL_0016:  leave.s    IL_006f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  isinst     [mscorlib]System.DivideByZeroException
      IL_001f:  brtrue.s   IL_002a

      IL_0021:  ldc.i4.s   101
      IL_0023:  stsfld     int32 TailcallVerify.Condition1::Result
      IL_0028:  rethrow
      IL_002a:  ldloc.0
      IL_002b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0030:  ldstr      "Caller"
      IL_0035:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003a:  ldc.i4.m1
      IL_003b:  beq.s      IL_006d

      IL_003d:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0042:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0047:  ldstr      "------------------------------------------------"
      IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0051:  call       void [System.Console]System.Console::WriteLine()
      IL_0056:  ldloc.0
      IL_0057:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0061:  call       void [System.Console]System.Console::WriteLine()
      IL_0066:  ldc.i4.s   101
      IL_0068:  stsfld     int32 TailcallVerify.Condition1::Result
      IL_006d:  leave.s    IL_006f

    }  // end handler
    IL_006f:  ldstr      "Execution finished - Test "
    IL_0074:  ldsfld     int32 TailcallVerify.Condition1::Result
    IL_0079:  ldc.i4.s   100
    IL_007b:  beq.s      IL_0084

    IL_007d:  ldstr      "FAILED"
    IL_0082:  br.s       IL_0089

    IL_0084:  ldstr      "PASSED"
    IL_0089:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0093:  ldsfld     int32 TailcallVerify.Condition1::Result
    IL_0098:  ret
  } // end of method Condition1::Test2

  .method private hidebysig static void  Caller2() cil managed
  {
    // Code size       54 (0x36)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition1::Result
    IL_002e:  ldc.i4.0
    IL_002f:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition1::Callee2(int32)
    IL_0034:  pop
    IL_0035:  ret
  } // end of method Condition1::Caller2

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          Callee2(int32 i) cil managed noinlining
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes temp)
    IL_0000:  ldloca.s   temp
    IL_0002:  initobj    TailcallVerify.ValueType3Bytes
    IL_0008:  ldloca.s   temp
    IL_000a:  ldarg.0
    IL_000b:  volatile.
    IL_000d:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition1::zero
    IL_0012:  div
    IL_0013:  conv.i2
    IL_0014:  stfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method Condition1::Callee2

  .method public hidebysig static int32  Test3() cil managed nooptimization
  {
    // Code size       153 (0x99)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition1.Test3 - Caller: Arguments: No"
    + "ne - ReturnType: void; Callee: Arguments: 8 x int - ReturnType: 3 byte "
    + "struct"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition1::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition1::Caller3()
      IL_0016:  leave.s    IL_006f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  isinst     [mscorlib]System.DivideByZeroException
      IL_001f:  brtrue.s   IL_002a

      IL_0021:  ldc.i4.s   101
      IL_0023:  stsfld     int32 TailcallVerify.Condition1::Result
      IL_0028:  rethrow
      IL_002a:  ldloc.0
      IL_002b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0030:  ldstr      "Caller"
      IL_0035:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003a:  ldc.i4.m1
      IL_003b:  beq.s      IL_006d

      IL_003d:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0042:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0047:  ldstr      "------------------------------------------------"
      IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0051:  call       void [System.Console]System.Console::WriteLine()
      IL_0056:  ldloc.0
      IL_0057:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0061:  call       void [System.Console]System.Console::WriteLine()
      IL_0066:  ldc.i4.s   101
      IL_0068:  stsfld     int32 TailcallVerify.Condition1::Result
      IL_006d:  leave.s    IL_006f

    }  // end handler
    IL_006f:  ldstr      "Execution finished - Test "
    IL_0074:  ldsfld     int32 TailcallVerify.Condition1::Result
    IL_0079:  ldc.i4.s   100
    IL_007b:  beq.s      IL_0084

    IL_007d:  ldstr      "FAILED"
    IL_0082:  br.s       IL_0089

    IL_0084:  ldstr      "PASSED"
    IL_0089:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0093:  ldsfld     int32 TailcallVerify.Condition1::Result
    IL_0098:  ret
  } // end of method Condition1::Test3

  .method private hidebysig static void  Caller3() cil managed
  {
    // Code size       93 (0x5d)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition1::Result
    IL_002e:  ldc.i4     0x80000000
    IL_0033:  ldc.i4     0x7fffffff
    IL_0038:  ldc.i4     0x80000000
    IL_003d:  ldc.i4     0x7fffffff
    IL_0042:  ldc.i4     0x80000000
    IL_0047:  ldc.i4     0x7fffffff
    IL_004c:  ldc.i4     0x80000000
    IL_0051:  ldc.i4     0x7fffffff
    IL_0056:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition1::Callee3(int32,
                                                                                                     int32,
                                                                                                     int32,
                                                                                                     int32,
                                                                                                     int32,
                                                                                                     int32,
                                                                                                     int32,
                                                                                                     int32)
    IL_005b:  pop
    IL_005c:  ret
  } // end of method Condition1::Caller3

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          Callee3(int32 i1,
                  int32 i2,
                  int32 i3,
                  int32 i4,
                  int32 i5,
                  int32 i6,
                  int32 i7,
                  int32 i8) cil managed noinlining
  {
    // Code size       105 (0x69)
    .maxstack  2
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes temp)
    IL_0000:  ldloca.s   temp
    IL_0002:  initobj    TailcallVerify.ValueType3Bytes
    IL_0008:  ldarg.0
    IL_0009:  ldc.i4     0x80000000
    IL_000e:  bne.un.s   IL_004c

    IL_0010:  ldarg.1
    IL_0011:  ldc.i4     0x7fffffff
    IL_0016:  bne.un.s   IL_004c

    IL_0018:  ldarg.2
    IL_0019:  ldc.i4     0x80000000
    IL_001e:  bne.un.s   IL_004c

    IL_0020:  ldarg.3
    IL_0021:  ldc.i4     0x7fffffff
    IL_0026:  bne.un.s   IL_004c

    IL_0028:  ldarg.s    i5
    IL_002a:  ldc.i4     0x80000000
    IL_002f:  bne.un.s   IL_004c

    IL_0031:  ldarg.s    i6
    IL_0033:  ldc.i4     0x7fffffff
    IL_0038:  bne.un.s   IL_004c

    IL_003a:  ldarg.s    i7
    IL_003c:  ldc.i4     0x80000000
    IL_0041:  bne.un.s   IL_004c

    IL_0043:  ldarg.s    i8
    IL_0045:  ldc.i4     0x7fffffff
    IL_004a:  beq.s      IL_005d

    IL_004c:  ldstr      "FAILED: Passed in arguments are invalid."
    IL_0051:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0056:  ldc.i4.s   101
    IL_0058:  stsfld     int32 TailcallVerify.Condition1::Result
    IL_005d:  ldc.i4.1
    IL_005e:  volatile.
    IL_0060:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition1::zero
    IL_0065:  div
    IL_0066:  pop
    IL_0067:  ldloc.0
    IL_0068:  ret
  } // end of method Condition1::Callee3

  .method public hidebysig static int32  Test4() cil managed nooptimization
  {
    // Code size       153 (0x99)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition1.Test4 - Caller: Arguments: No"
    + "ne - ReturnType: void; Callee: Arguments: i32,i64,mb3,i16,u8,f64,f32,mb"
    + "5 - ReturnType: 3 byte struct"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition1::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition1::Caller4()
      IL_0016:  leave.s    IL_006f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  isinst     [mscorlib]System.DivideByZeroException
      IL_001f:  brtrue.s   IL_002a

      IL_0021:  ldc.i4.s   101
      IL_0023:  stsfld     int32 TailcallVerify.Condition1::Result
      IL_0028:  rethrow
      IL_002a:  ldloc.0
      IL_002b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0030:  ldstr      "Caller"
      IL_0035:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003a:  ldc.i4.m1
      IL_003b:  beq.s      IL_006d

      IL_003d:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0042:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0047:  ldstr      "------------------------------------------------"
      IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0051:  call       void [System.Console]System.Console::WriteLine()
      IL_0056:  ldloc.0
      IL_0057:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0061:  call       void [System.Console]System.Console::WriteLine()
      IL_0066:  ldc.i4.s   101
      IL_0068:  stsfld     int32 TailcallVerify.Condition1::Result
      IL_006d:  leave.s    IL_006f

    }  // end handler
    IL_006f:  ldstr      "Execution finished - Test "
    IL_0074:  ldsfld     int32 TailcallVerify.Condition1::Result
    IL_0079:  ldc.i4.s   100
    IL_007b:  beq.s      IL_0084

    IL_007d:  ldstr      "FAILED"
    IL_0082:  br.s       IL_0089

    IL_0084:  ldstr      "PASSED"
    IL_0089:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0093:  ldsfld     int32 TailcallVerify.Condition1::Result
    IL_0098:  ret
  } // end of method Condition1::Test4

  .method private hidebysig static void  Caller4() cil managed
  {
    // Code size       175 (0xaf)
    .maxstack  8
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes v3,
             [1] valuetype TailcallVerify.ValueType5Bytes v5,
             [2] valuetype TailcallVerify.ValueType3Bytes '<>g__initLocal0',
             [3] valuetype TailcallVerify.ValueType5Bytes '<>g__initLocal1',
             [4] valuetype TailcallVerify.ValueType3Bytes CS$0$0000,
             [5] valuetype TailcallVerify.ValueType5Bytes CS$0$0001)
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition1::Result
    IL_002e:  ldloca.s   CS$0$0000
    IL_0030:  initobj    TailcallVerify.ValueType3Bytes
    IL_0036:  ldloc.s    CS$0$0000
    IL_0038:  stloc.2
    IL_0039:  ldloca.s   '<>g__initLocal0'
    IL_003b:  ldc.i4.0
    IL_003c:  stfld      uint8 TailcallVerify.ValueType3Bytes::i1
    IL_0041:  ldloca.s   '<>g__initLocal0'
    IL_0043:  ldc.i4     0x7fff
    IL_0048:  stfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_004d:  ldloc.2
    IL_004e:  stloc.0
    IL_004f:  ldloca.s   CS$0$0001
    IL_0051:  initobj    TailcallVerify.ValueType5Bytes
    IL_0057:  ldloc.s    CS$0$0001
    IL_0059:  stloc.3
    IL_005a:  ldloca.s   '<>g__initLocal1'
    IL_005c:  ldc.i4     0xff
    IL_0061:  stfld      uint8 TailcallVerify.ValueType5Bytes::i1
    IL_0066:  ldloca.s   '<>g__initLocal1'
    IL_0068:  ldc.i4     0xffff8000
    IL_006d:  stfld      int16 TailcallVerify.ValueType5Bytes::i2
    IL_0072:  ldloca.s   '<>g__initLocal1'
    IL_0074:  ldc.i4     0x7fff
    IL_0079:  stfld      int16 TailcallVerify.ValueType5Bytes::i3
    IL_007e:  ldloc.3
    IL_007f:  stloc.1
    IL_0080:  ldc.i4     0x80000000
    IL_0085:  ldc.i8     0x7fffffffffffffff
    IL_008e:  ldloc.0
    IL_008f:  ldc.i4     0xffff8000
    IL_0094:  ldc.i4     0xff
    IL_0099:  ldc.r8     -1.7976931348623157e+308
    IL_00a2:  ldc.r4     3.4028235e+038
    IL_00a7:  ldloc.1
    IL_00a8:  tail. call       valuetype TailcallVerify.ValueType3Bytes TailcallVerify.Condition1::Callee4(int32,
                                                                                                     int64,
                                                                                                     valuetype TailcallVerify.ValueType3Bytes,
                                                                                                     int16,
                                                                                                     uint8,
                                                                                                     float64,
                                                                                                     float32,
                                                                                                     valuetype TailcallVerify.ValueType5Bytes)
    IL_00ad:  pop
    IL_00ae:  ret
  } // end of method Condition1::Caller4

  .method private hidebysig static valuetype TailcallVerify.ValueType3Bytes 
          Callee4(int32 i1,
                  int64 i2,
                  valuetype TailcallVerify.ValueType3Bytes v3,
                  int16 i4,
                  uint8 b5,
                  float64 d6,
                  float32 f7,
                  valuetype TailcallVerify.ValueType5Bytes v8) cil managed noinlining
  {
    // Code size       317 (0x13d)
    .maxstack  4
    .locals init ([0] valuetype TailcallVerify.ValueType3Bytes temp,
             [1] object[] CS$0$0000)
    IL_0000:  ldloca.s   temp
    IL_0002:  initobj    TailcallVerify.ValueType3Bytes
    IL_0008:  ldarg.0
    IL_0009:  ldc.i4     0x80000000
    IL_000e:  bne.un.s   IL_0087

    IL_0010:  ldarg.1
    IL_0011:  ldc.i8     0x7fffffffffffffff
    IL_001a:  bne.un.s   IL_0087

    IL_001c:  ldarga.s   v3
    IL_001e:  ldfld      uint8 TailcallVerify.ValueType3Bytes::i1
    IL_0023:  brtrue.s   IL_0087

    IL_0025:  ldarga.s   v3
    IL_0027:  ldfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_002c:  ldc.i4     0x7fff
    IL_0031:  bne.un.s   IL_0087

    IL_0033:  ldarg.3
    IL_0034:  ldc.i4     0xffff8000
    IL_0039:  bne.un.s   IL_0087

    IL_003b:  ldarg.s    b5
    IL_003d:  ldc.i4     0xff
    IL_0042:  bne.un.s   IL_0087

    IL_0044:  ldarg.s    d6
    IL_0046:  ldc.r8     -1.7976931348623157e+308
    IL_004f:  bne.un.s   IL_0087

    IL_0051:  ldarg.s    f7
    IL_0053:  ldc.r4     3.4028235e+038
    IL_0058:  bne.un.s   IL_0087

    IL_005a:  ldarga.s   v8
    IL_005c:  ldfld      uint8 TailcallVerify.ValueType5Bytes::i1
    IL_0061:  ldc.i4     0xff
    IL_0066:  bne.un.s   IL_0087

    IL_0068:  ldarga.s   v8
    IL_006a:  ldfld      int16 TailcallVerify.ValueType5Bytes::i2
    IL_006f:  ldc.i4     0xffff8000
    IL_0074:  bne.un.s   IL_0087

    IL_0076:  ldarga.s   v8
    IL_0078:  ldfld      int16 TailcallVerify.ValueType5Bytes::i3
    IL_007d:  ldc.i4     0x7fff
    IL_0082:  beq        IL_0131

    IL_0087:  ldstr      "FAILED: Passed in arguments are invalid."
    IL_008c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0091:  ldstr      "i1:{0} != Int32.MinValue || i2:{1} != Int64.MaxVal"
    + "ue || v3.i1:{2} != byte.MinValue || v3.i2:{3} != short.MaxValue || i4:{"
    + "4} != Int16.MinValue || b5:{5} != byte.MaxValue || d6:{6} != double.Min"
    + "Value || f7:{7} != float.MaxValue || v8.i1:{8} != byte.MaxValue || v8.i"
    + "2:{9} != short.MinValue || v8.i3:{10} != short.MaxValue"
    IL_0096:  ldc.i4.s   11
    IL_0098:  newarr     [mscorlib]System.Object
    IL_009d:  stloc.1
    IL_009e:  ldloc.1
    IL_009f:  ldc.i4.0
    IL_00a0:  ldarg.0
    IL_00a1:  box        [mscorlib]System.Int32
    IL_00a6:  stelem.ref
    IL_00a7:  ldloc.1
    IL_00a8:  ldc.i4.1
    IL_00a9:  ldarg.1
    IL_00aa:  box        [mscorlib]System.Int64
    IL_00af:  stelem.ref
    IL_00b0:  ldloc.1
    IL_00b1:  ldc.i4.2
    IL_00b2:  ldarga.s   v3
    IL_00b4:  ldfld      uint8 TailcallVerify.ValueType3Bytes::i1
    IL_00b9:  box        [mscorlib]System.Byte
    IL_00be:  stelem.ref
    IL_00bf:  ldloc.1
    IL_00c0:  ldc.i4.3
    IL_00c1:  ldarga.s   v3
    IL_00c3:  ldfld      int16 TailcallVerify.ValueType3Bytes::i2
    IL_00c8:  box        [mscorlib]System.Int16
    IL_00cd:  stelem.ref
    IL_00ce:  ldloc.1
    IL_00cf:  ldc.i4.4
    IL_00d0:  ldarg.3
    IL_00d1:  box        [mscorlib]System.Int16
    IL_00d6:  stelem.ref
    IL_00d7:  ldloc.1
    IL_00d8:  ldc.i4.5
    IL_00d9:  ldarg.s    b5
    IL_00db:  box        [mscorlib]System.Byte
    IL_00e0:  stelem.ref
    IL_00e1:  ldloc.1
    IL_00e2:  ldc.i4.6
    IL_00e3:  ldarg.s    d6
    IL_00e5:  box        [mscorlib]System.Double
    IL_00ea:  stelem.ref
    IL_00eb:  ldloc.1
    IL_00ec:  ldc.i4.7
    IL_00ed:  ldarg.s    f7
    IL_00ef:  box        [mscorlib]System.Single
    IL_00f4:  stelem.ref
    IL_00f5:  ldloc.1
    IL_00f6:  ldc.i4.8
    IL_00f7:  ldarga.s   v8
    IL_00f9:  ldfld      uint8 TailcallVerify.ValueType5Bytes::i1
    IL_00fe:  box        [mscorlib]System.Byte
    IL_0103:  stelem.ref
    IL_0104:  ldloc.1
    IL_0105:  ldc.i4.s   9
    IL_0107:  ldarga.s   v8
    IL_0109:  ldfld      int16 TailcallVerify.ValueType5Bytes::i2
    IL_010e:  box        [mscorlib]System.Int16
    IL_0113:  stelem.ref
    IL_0114:  ldloc.1
    IL_0115:  ldc.i4.s   10
    IL_0117:  ldarga.s   v8
    IL_0119:  ldfld      int16 TailcallVerify.ValueType5Bytes::i3
    IL_011e:  box        [mscorlib]System.Int16
    IL_0123:  stelem.ref
    IL_0124:  ldloc.1
    IL_0125:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object[])
    IL_012a:  ldc.i4.s   101
    IL_012c:  stsfld     int32 TailcallVerify.Condition1::Result
    IL_0131:  ldc.i4.1
    IL_0132:  volatile.
    IL_0134:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition1::zero
    IL_0139:  div
    IL_013a:  pop
    IL_013b:  ldloc.0
    IL_013c:  ret
  } // end of method Condition1::Callee4

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Condition1::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldc.i4.s   100
    IL_0002:  stsfld     int32 TailcallVerify.Condition1::Result
    IL_0007:  ldc.i4.0
    IL_0008:  volatile.
    IL_000a:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition1::zero
    IL_000f:  ret
  } // end of method Condition1::.cctor

} // end of class TailcallVerify.Condition1

.class private sequential ansi sealed beforefieldinit TailcallVerify.ValueType3Bytes
       extends [mscorlib]System.ValueType
{
  .pack 1
  .size 3
  .field public uint8 i1
  .field public int16 i2
} // end of class TailcallVerify.ValueType3Bytes

.class private sequential ansi sealed beforefieldinit TailcallVerify.ValueType5Bytes
       extends [mscorlib]System.ValueType
{
  .pack 1
  .size 5
  .field public uint8 i1
  .field public int16 i2
  .field public int16 i3
} // end of class TailcallVerify.ValueType5Bytes

.class private sequential ansi sealed beforefieldinit TailcallVerify.ValueTypeSingleInt64
       extends [mscorlib]System.ValueType
{
  .pack 1
  .size 8
  .field public int64 i1
} // end of class TailcallVerify.ValueTypeSingleInt64

.class private auto ansi beforefieldinit TailcallVerify.Win32Interop
       extends [mscorlib]System.Object
{
  .method public hidebysig static pinvokeimpl("kernel32" as "Sleep" winapi) 
          void  Callee(int32 duration) cil managed preservesig
  {
  }
  .method public hidebysig static pinvokeimpl("kernel32" lasterr winapi) 
          void  Sleep(int32 duration) cil managed preservesig
  {
  }
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Win32Interop::.ctor

} // end of class TailcallVerify.Win32Interop

.class private auto ansi beforefieldinit TailcallVerify.Condition19
       extends [mscorlib]System.Object
{
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .field private static int32 Result
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       153 (0x99)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition19.Test1 - Caller: Arguments: N"
    + "one - ReturnType: void; Callee (doing a pinvoke call): Arguments: None "
    + "- ReturnType: void"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition19::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition19::Caller1()
      IL_0016:  leave.s    IL_006f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  isinst     [mscorlib]System.DivideByZeroException
      IL_001f:  brtrue.s   IL_002a

      IL_0021:  ldc.i4.s   101
      IL_0023:  stsfld     int32 TailcallVerify.Condition19::Result
      IL_0028:  rethrow
      IL_002a:  ldloc.0
      IL_002b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0030:  ldstr      "Caller"
      IL_0035:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003a:  ldc.i4.m1
      IL_003b:  beq.s      IL_006d

      IL_003d:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0042:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0047:  ldstr      "------------------------------------------------"
      IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0051:  call       void [System.Console]System.Console::WriteLine()
      IL_0056:  ldloc.0
      IL_0057:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0061:  call       void [System.Console]System.Console::WriteLine()
      IL_0066:  ldc.i4.s   101
      IL_0068:  stsfld     int32 TailcallVerify.Condition19::Result
      IL_006d:  leave.s    IL_006f

    }  // end handler
    IL_006f:  ldstr      "Execution finished - Test "
    IL_0074:  ldsfld     int32 TailcallVerify.Condition19::Result
    IL_0079:  ldc.i4.s   100
    IL_007b:  beq.s      IL_0084

    IL_007d:  ldstr      "FAILED"
    IL_0082:  br.s       IL_0089

    IL_0084:  ldstr      "PASSED"
    IL_0089:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0093:  ldsfld     int32 TailcallVerify.Condition19::Result
    IL_0098:  ret
  } // end of method Condition19::Test1

  .method private hidebysig static void  Caller1() cil managed
  {
    // Code size       53 (0x35)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition19::Result
    IL_002e:  ldc.i4.1
    IL_002f:  tail. call       void TailcallVerify.Condition19::Callee1(int32)
    IL_0034:  ret
  } // end of method Condition19::Caller1

  .method private hidebysig static void  Callee1(int32 i) cil managed noinlining
  {
    // Code size       43 (0x2b)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  call       void TailcallVerify.Win32Interop::Sleep(int32)
    IL_0006:  ldstr      "result: "
    IL_000b:  call       int32 [mscorlib]System.Runtime.InteropServices.Marshal::GetLastWin32Error()
    IL_0010:  box        [mscorlib]System.Int32
    IL_0015:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_001a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_001f:  ldc.i4.1
    IL_0020:  volatile.
    IL_0022:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition19::zero
    IL_0027:  div
    IL_0028:  starg.s    i
    IL_002a:  ret
  } // end of method Condition19::Callee1

  .method public hidebysig static int32  Test2() cil managed nooptimization
  {
    // Code size       153 (0x99)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition19.Test2 - Caller: Arguments: N"
    + "one - ReturnType: void; Callee (IS A pinvoke call): Arguments: None - R"
    + "eturnType: void"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition19::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition19::Caller2()
      IL_0016:  leave.s    IL_006f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  isinst     [mscorlib]System.DivideByZeroException
      IL_001f:  brtrue.s   IL_002a

      IL_0021:  ldc.i4.s   101
      IL_0023:  stsfld     int32 TailcallVerify.Condition19::Result
      IL_0028:  rethrow
      IL_002a:  ldloc.0
      IL_002b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0030:  ldstr      "Caller"
      IL_0035:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003a:  ldc.i4.m1
      IL_003b:  beq.s      IL_006d

      IL_003d:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0042:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0047:  ldstr      "------------------------------------------------"
      IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0051:  call       void [System.Console]System.Console::WriteLine()
      IL_0056:  ldloc.0
      IL_0057:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0061:  call       void [System.Console]System.Console::WriteLine()
      IL_0066:  ldc.i4.s   101
      IL_0068:  stsfld     int32 TailcallVerify.Condition19::Result
      IL_006d:  leave.s    IL_006f

    }  // end handler
    IL_006f:  ldstr      "Execution finished - Test "
    IL_0074:  ldsfld     int32 TailcallVerify.Condition19::Result
    IL_0079:  ldc.i4.s   100
    IL_007b:  beq.s      IL_0084

    IL_007d:  ldstr      "FAILED"
    IL_0082:  br.s       IL_0089

    IL_0084:  ldstr      "PASSED"
    IL_0089:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0093:  ldsfld     int32 TailcallVerify.Condition19::Result
    IL_0098:  ret
  } // end of method Condition19::Test2

  .method private hidebysig static void  Caller2() cil managed
  {
    // Code size       78 (0x4e)
    .maxstack  2
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition19::Result
    IL_002e:  ldstr      "result: "
    IL_0033:  call       int32 [mscorlib]System.Runtime.InteropServices.Marshal::GetLastWin32Error()
    IL_0038:  box        [mscorlib]System.Int32
    IL_003d:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_0042:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0047:  ldc.i4.0
    IL_0048:  tail. call       void TailcallVerify.Win32Interop::Callee(int32)
    IL_004d:  ret
  } // end of method Condition19::Caller2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Condition19::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  volatile.
    IL_0003:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition19::zero
    IL_0008:  ldc.i4.s   100
    IL_000a:  stsfld     int32 TailcallVerify.Condition19::Result
    IL_000f:  ret
  } // end of method Condition19::.cctor

} // end of class TailcallVerify.Condition19

.class private auto ansi '<PrivateImplementationDetails>{D1242658-CA16-4D11-A740-6635F112F4B5}'
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .class explicit ansi sealed nested private '__StaticArrayInitTypeSize=20'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 20
  } // end of class '__StaticArrayInitTypeSize=20'

  .field static assembly valuetype '<PrivateImplementationDetails>{D1242658-CA16-4D11-A740-6635F112F4B5}'/'__StaticArrayInitTypeSize=20' '$$method0x6000076-1' at I_00005C00
} // end of class '<PrivateImplementationDetails>{D1242658-CA16-4D11-A740-6635F112F4B5}'


// =============================================================

.data cil I_00005C00 = bytearray (
                 01 00 00 00 02 00 00 00 03 00 00 00 04 00 00 00
                 05 00 00 00) 
// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file TailcallVerifyWithPrefix.res
