// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



// Metadata version: v2.0.50727

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Runtime.Extensions
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:2:1:0
}
.assembly extern System.Threading.Thread
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
             [2] class [System.Threading.Thread]System.Threading.Thread t)
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
      IL_0094:  newobj     instance void [System.Threading.Thread]System.Threading.Thread::.ctor(class [mscorlib]System.Threading.ParameterizedThreadStart)
      IL_0099:  stloc.2
      IL_009a:  ldloc.2
      IL_009b:  ldloc.0
      IL_009c:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Start(object)
      IL_00a1:  ldloc.2
      IL_00a2:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Join()
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
    .locals init ([0] class [System.Threading.Thread]System.Threading.Thread t)
    IL_0000:  ldsfld     class [mscorlib]System.Threading.ParameterizedThreadStart TailcallVerify.Condition16::'CS$<>9__CachedAnonymousMethodDelegate3'
    IL_0005:  brtrue.s   IL_0018

    IL_0007:  ldnull
    IL_0008:  ldftn      void TailcallVerify.Condition16::'<Callee1>b__2'(object)
    IL_000e:  newobj     instance void [mscorlib]System.Threading.ParameterizedThreadStart::.ctor(object,
                                                                                                  native int)
    IL_0013:  stsfld     class [mscorlib]System.Threading.ParameterizedThreadStart TailcallVerify.Condition16::'CS$<>9__CachedAnonymousMethodDelegate3'
    IL_0018:  ldsfld     class [mscorlib]System.Threading.ParameterizedThreadStart TailcallVerify.Condition16::'CS$<>9__CachedAnonymousMethodDelegate3'
    IL_001d:  newobj     instance void [System.Threading.Thread]System.Threading.Thread::.ctor(class [mscorlib]System.Threading.ParameterizedThreadStart)
    IL_0022:  stloc.0
    IL_0023:  ldloc.0
    IL_0024:  ldarg.0
    IL_0025:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Start(object)
    IL_002a:  ldloc.0
    IL_002b:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Join()
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

.class public auto ansi beforefieldinit TailcallVerify.Program
       extends [mscorlib]System.Object
{
  .field private static int32 result
  .method private hidebysig static void  PrintOutRunTestsFile() cil managed
  {
    // Code size       948 (0x3b4)
    .maxstack  4
    .locals init ([0] class [mscorlib]System.Collections.Generic.List`1<string> excludeCollection,
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
    IL_00f7:  ldloc.s    '<>g__initLocal0'
    IL_00f9:  ldstr      "Condition9.Test1"
    IL_00fe:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_0103:  ldloc.s    '<>g__initLocal0'
    IL_0105:  ldstr      "Condition11.Test1"
    IL_010a:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_010f:  ldloc.s    '<>g__initLocal0'
    IL_0111:  ldstr      "Condition13.Test1"
    IL_0116:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
    IL_0133:  ldloc.s    '<>g__initLocal0'
    IL_0135:  ldstr      "Condition21.Test3"
    IL_013a:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
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
    IL_0032:  brfalse.s  IL_00fc

    IL_0034:  call       void TailcallVerify.Program::Usage()
    IL_0039:  ldc.i4.s   100
    IL_003b:  ret

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
    IL_0290:  brfalse.s  IL_041c

    IL_0292:  call       int32 TailcallVerify.Condition5::Test9()
    IL_0297:  call       void TailcallVerify.Program::set_Result(int32)
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
    IL_0590:  brfalse.s  IL_05bc

    IL_0592:  call       int32 TailcallVerify.Condition18::Test1()
    IL_0597:  call       void TailcallVerify.Program::set_Result(int32)
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
    IL_0610:  brfalse.s  IL_065c

    IL_0612:  call       int32 TailcallVerify.Condition20::Test1()
    IL_0617:  call       void TailcallVerify.Program::set_Result(int32)
    IL_065c:  ldloc.1
    IL_065d:  ldstr      "Condition21.Test3"
    IL_0662:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0667:  brtrue.s   IL_0672

    IL_0669:  ldloc.1
    IL_066a:  ldloc.0
    IL_066b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0670:  brfalse.s  IL_06bc

    IL_0672:  call       int32 TailcallVerify.Condition21::Test3()
    IL_0677:  call       void TailcallVerify.Program::set_Result(int32)
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
    IL_06f0:  brfalse.s  IL_075c

    IL_06f2:  call       int32 TailcallVerify.Condition21::Test7()
    IL_06f7:  call       void TailcallVerify.Program::set_Result(int32)
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

  .method public hidebysig static int32
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  1

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
    ldstr      "Condition21.Test3"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition21.Test6"
    call       int32 TailcallVerify.Program::Run(string)
    pop
    ldstr      "Condition21.Test7"
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

  .method public hidebysig static int32  Test3() cil managed nooptimization
  {
    // Code size       394 (0x18a)
    .maxstack  3
    .locals init ([0] class [System.Threading.Thread]System.Threading.Thread t1,
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
      IL_002c:  newobj     instance void [System.Threading.Thread]System.Threading.ThreadStart::.ctor(object,
                                                                                       native int)
      IL_0031:  newobj     instance void [System.Threading.Thread]System.Threading.Thread::.ctor(class [System.Threading.Thread]System.Threading.ThreadStart)
      IL_0036:  stloc.0
      IL_0037:  ldloc.0
      IL_0038:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Start()
      IL_003d:  ldloc.0
      IL_003e:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Join()
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
      // This method depends on JIT compiler doing automatic tail call, which isn't supported by Ready-to-Run.
      .custom instance void System.Runtime.BypassReadyToRunAttribute::.ctor() = ( 01 00 00 00 )

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
      // This method depends on JIT compiler doing automatic tail call, which isn't supported by Ready-to-Run.
      .custom instance void System.Runtime.BypassReadyToRunAttribute::.ctor() = ( 01 00 00 00 )

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
                tail.
      IL_0032:  call       instance void class TailcallVerify.Condition10/Foo1`2<!V,!K>::Callee1Recursive(int32)
      IL_0037:  ret
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
      // This method depends on JIT compiler doing automatic tail call, which isn't supported by Ready-to-Run.
      .custom instance void System.Runtime.BypassReadyToRunAttribute::.ctor() = ( 01 00 00 00 )

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
                tail.
      IL_0058:  call       instance void class TailcallVerify.Condition10/Foo1`2<!V,!K>::Callee1Recursive(int32)
      IL_005d:  ret
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
      // This method depends on JIT compiler doing automatic tail call, which isn't supported by Ready-to-Run.
      .custom instance void System.Runtime.BypassReadyToRunAttribute::.ctor() = ( 01 00 00 00 )

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
    // This method depends on JIT compiler doing automatic tail call, which isn't supported by Ready-to-Run.
    .custom instance void System.Runtime.BypassReadyToRunAttribute::.ctor() = ( 01 00 00 00 )

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

.class private auto ansi beforefieldinit TailcallVerify.UnixInterop
       extends [System.Runtime]System.Object
{
  .method public hidebysig static pinvokeimpl("libc" as "printf" winapi)
          void  Callee(string format) cil managed preservesig
  {
  }
  .method public hidebysig static pinvokeimpl("libc" winapi)
          void  printf(string format) cil managed preservesig
  {
  }
  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method TailcallVerify.UnixInterop::.ctor

} // end of class TailcallVerify.UnixInterop

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
    IL_0000:  call       class [System.Runtime.Extensions]System.OperatingSystem [System.Runtime.Extensions]System.Environment::get_OSVersion()
    IL_0005:  callvirt   instance valuetype [System.Runtime.Extensions]System.PlatformID [System.Runtime.Extensions]System.OperatingSystem::get_Platform()
    IL_000a:  ldc.i4.2
    IL_000b:  bne.un.s   IL_UNIX

    IL_000d:  ldc.i4.0
    IL_000e:  call       void TailcallVerify.Win32Interop::Sleep(int32)
              ldstr      "result: "
              call       int32 [mscorlib]System.Runtime.InteropServices.Marshal::GetLastWin32Error()
              box        [mscorlib]System.Int32
              call       string [mscorlib]System.String::Concat(object,
                                                                object)
              call       void [System.Console]System.Console::WriteLine(string)
              br.s       IL_AFTER

    IL_UNIX:  ldstr      "Hello\n"
              call       void TailcallVerify.UnixInterop::printf(string)
    IL_AFTER: ldc.i4.1
              volatile.
              ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition19::zero
              div
              starg.s    i
              ret
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

              call       class [System.Runtime.Extensions]System.OperatingSystem [System.Runtime.Extensions]System.Environment::get_OSVersion()
              callvirt   instance valuetype [System.Runtime.Extensions]System.PlatformID [System.Runtime.Extensions]System.OperatingSystem::get_Platform()
              ldc.i4.2
              bne.un.s   IL_UNIX

              ldc.i4.0
              tail. call       void TailcallVerify.Win32Interop::Callee(int32)
              ret

    IL_UNIX:  ldstr      "Hello\n"
              tail. call       void TailcallVerify.UnixInterop::Callee(string)
              ret
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

.class private auto ansi sealed beforefieldinit System.Runtime.BypassReadyToRunAttribute
       extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  ret
  } // end of method BypassReadyToRunAttribute::.ctor

} // end of class System.Runtime.BypassReadyToRunAttribute


// =============================================================

.data cil I_00005C00 = bytearray (
                 01 00 00 00 02 00 00 00 03 00 00 00 04 00 00 00
                 05 00 00 00)
// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file TailcallVerifyWithPrefix.res
