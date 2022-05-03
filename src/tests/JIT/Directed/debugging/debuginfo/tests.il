
// Metadata version: v4.0.30319
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 7:0:0:0
}
.assembly extern attribute
{
  .ver 0:0:0:0
}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 7:0:0:0
}
#ifdef DEBUG
.module tests_d.dll
.assembly tests_d
#else
.module tests_r.dll
.assembly tests_r
#endif
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                   63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {D4625784-B5B9-4F12-8B8C-0423DDB744AA}
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY

.class public auto ansi beforefieldinit DebugInfoMethods
  extends [System.Runtime]System.Object
{
  .method public hidebysig static void  TestUninlined() cil managed
  {
    .custom instance void [attribute]ExpectedILMappings::.ctor() = {
      property int32[] Debug = int32[2]( 0 6 )
      property int32[] Opts = int32[2]( 0 6 )
    }
    .maxstack  8
    IL_0000:  call       int32 DebugInfoMethods::NeverInlined()
    IL_0005:  pop
    IL_0006:  call       int32 DebugInfoMethods::NeverInlined()
    IL_000b:  pop
    IL_000c:  ret
  }

  // fwdSub moves second call under the return tree
  //
  .method public hidebysig static int32 TestLateFailingInlineCandidate() cil managed
  {
    .custom instance void [attribute]ExpectedILMappings::.ctor() = {
      property int32[] Debug = int32[2]( 0 6 )
      property int32[] Opts = int32[2]( 0 0xE )
    }
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  call       int32 DebugInfoMethods::NeverInlined()
    IL_0005:  pop
    IL_0006:  call       int32 DebugInfoMethods::NotInlinedByAnalysis()
    IL_000b:  ldc.i4.5
    IL_000c:  add
    IL_000d:  stloc.0
    IL_000e:  ldloc.0
    IL_000f:  ret
  }

  .method public hidebysig static int32 TestSucceedingInlineCandidate() cil managed
  {
    // We still expect a mapping where the call was inlined.
    .custom instance void [attribute]ExpectedILMappings::.ctor() = {
      property int32[] Debug = int32[2]( 0 6 )
      property int32[] Opts = int32[2]( 0 6 )
    }
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  call       int32 DebugInfoMethods::NeverInlined()
    IL_0005:  pop
    IL_0006:  call       int32 DebugInfoMethods::AggressivelyInlined()
    IL_000b:  ldc.i4.5
    IL_000c:  add
    IL_000d:  stloc.0
    IL_000e:  ldloc.0
    IL_000f:  ret
  }

  .method public hidebysig static int32  TestControlFlow(int32 arg) cil managed
  {
    .custom instance void [attribute]ExpectedILMappings::.ctor() = {
      // Note: We really would want to validate that we are generating mappings for every call instruction in debug,
      // as this is used for the managed-ret-val feature, but the debugger filters out these mappings and does not
      // report them in the ETW event. We should probably change this, those mappings should be useful in any case.
      property int32[] Debug = int32[10]( 0x0 0x6 0xe 0x12 0x1a 0x1c 0x24 0x28 0x2c 0x34 ) 
      property int32[] Opts = int32[5]( 0x0 0x6 0x12 0x1c 0x2c )
    }
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  ldarg.0
    IL_0003:  ldc.i4.0
    IL_0004:  ble.s      IL_001c

    IL_0006:  ldloc.0
    IL_0007:  call       int32 DebugInfoMethods::NeverInlined()
    IL_000c:  add
    IL_000d:  stloc.0
    IL_000e:  ldloc.0
    IL_000f:  ldc.i4.1
    IL_0010:  add
    IL_0011:  stloc.0
    IL_0012:  ldloc.0
    IL_0013:  call       int32 DebugInfoMethods::NotInlinedByAnalysis()
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  br.s       IL_0034

    IL_001c:  ldloc.0
    IL_001d:  call       int32 DebugInfoMethods::NeverInlined()
    IL_0022:  add
    IL_0023:  stloc.0
    IL_0024:  ldloc.0
    IL_0025:  ldc.i4.1
    IL_0026:  add
    IL_0027:  stloc.0
    IL_0028:  ldloc.0
    IL_0029:  ldc.i4.3
    IL_002a:  add
    IL_002b:  stloc.0
    IL_002c:  ldloc.0
    IL_002d:  call       int32 DebugInfoMethods::NotInlinedByAnalysis()
    IL_0032:  add
    IL_0033:  stloc.0
    IL_0034:  ldloc.0
    IL_0035:  ret
  }


  .method private hidebysig static int32 NeverInlined() cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldc.i4.5
    IL_0001:  ret
  }

  .method private hidebysig static int32 NotInlinedByAnalysis() cil managed
  {
    .maxstack  5
    .locals init (int32 V_0,
             int32 V_1,
             int32 V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_006f

    IL_0004:  ldc.i4.0
    IL_0005:  stloc.1
    IL_0006:  br.s       IL_0062

    IL_0008:  ldc.i4.0
    IL_0009:  stloc.2
    IL_000a:  br.s       IL_0055

    IL_000c:  ldc.i4.0
    IL_000d:  stloc.3
    IL_000e:  br.s       IL_0048

    IL_0010:  ldstr      "{0} {1} {2} {3}"
    IL_0015:  ldc.i4.4
    IL_0016:  newarr     [System.Runtime]System.Object
    IL_001b:  dup
    IL_001c:  ldc.i4.0
    IL_001d:  ldloc.0
    IL_001e:  box        [System.Runtime]System.Int32
    IL_0023:  stelem.ref
    IL_0024:  dup
    IL_0025:  ldc.i4.1
    IL_0026:  ldloc.1
    IL_0027:  box        [System.Runtime]System.Int32
    IL_002c:  stelem.ref
    IL_002d:  dup
    IL_002e:  ldc.i4.2
    IL_002f:  ldloc.2
    IL_0030:  box        [System.Runtime]System.Int32
    IL_0035:  stelem.ref
    IL_0036:  dup
    IL_0037:  ldc.i4.3
    IL_0038:  ldloc.3
    IL_0039:  box        [System.Runtime]System.Int32
    IL_003e:  stelem.ref
    IL_003f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                        object[])
    IL_0044:  ldloc.3
    IL_0045:  ldc.i4.1
    IL_0046:  add
    IL_0047:  stloc.3
    IL_0048:  ldloc.3
    IL_0049:  call       int32 [System.Runtime]System.Environment::get_TickCount()
    IL_004e:  neg
    IL_004f:  blt.s      IL_0010

    IL_0051:  ldloc.2
    IL_0052:  ldc.i4.1
    IL_0053:  add
    IL_0054:  stloc.2
    IL_0055:  ldloc.2
    IL_0056:  call       int32 [System.Runtime]System.Environment::get_TickCount()
    IL_005b:  neg
    IL_005c:  blt.s      IL_000c

    IL_005e:  ldloc.1
    IL_005f:  ldc.i4.1
    IL_0060:  add
    IL_0061:  stloc.1
    IL_0062:  ldloc.1
    IL_0063:  call       int32 [System.Runtime]System.Environment::get_TickCount()
    IL_0068:  neg
    IL_0069:  blt.s      IL_0008

    IL_006b:  ldloc.0
    IL_006c:  ldc.i4.1
    IL_006d:  add
    IL_006e:  stloc.0
    IL_006f:  ldloc.0
    IL_0070:  call       int32 [System.Runtime]System.Environment::get_TickCount()
    IL_0075:  neg
    IL_0076:  blt.s      IL_0004

    IL_0078:  ldc.i4.5
    IL_0079:  ret
  }

  .method private hidebysig static int32 AggressivelyInlined() cil managed aggressiveinlining
  {
    call       int32 DebugInfoMethods::NeverInlined()
    pop
    call       int32 DebugInfoMethods::NeverInlined()
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } 

}