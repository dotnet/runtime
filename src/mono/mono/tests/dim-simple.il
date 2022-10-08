.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly simple
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module simple.exe
// MVID: {0B8FCFD0-673A-4DEB-90CC-B96749DE09C8}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x01A80000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface private abstract auto ansi IBlah
{
  .method public hidebysig newslot virtual 
          instance int32  Blah(int32 c) cil managed
  {
    // Code size       39 (0x27)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBlah.Blah"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldarg.0
    IL_000e:  call       instance int32 IBlah::Blah_Private_GetA()
    IL_0013:  add
    IL_0014:  ldarg.0
    IL_0015:  call       instance int32 IBlah::Blah_Internal_GetB()
    IL_001a:  add
    IL_001b:  ldarg.0
    IL_001c:  call       instance int32 IBlah::Blah_Protected_GetC()
    IL_0021:  add
    IL_0022:  stloc.0
    IL_0023:  br.s       IL_0025

    IL_0025:  ldloc.0
    IL_0026:  ret 
  } // end of method IBlah::Blah

  .method private hidebysig instance int32 
          Blah_Private_GetA() cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBlah.Blah_Private_GetA"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldc.i4.1
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method IBlah_Impl::Blah_Private_GetA

  .method assembly hidebysig instance int32 
          Blah_Internal_GetB() cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBlah.Blah_Internal_GetB"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldc.i4.2
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method IBlah_Impl::Blah_Internal_GetB

  .method family hidebysig instance int32 
          Blah_Protected_GetC() cil managed
  {
    // Code size       18 (0x12)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBlah.Blah_Protected_GetC"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldc.i4.3
    IL_000d:  stloc.0
    IL_000e:  br.s       IL_0010

    IL_0010:  ldloc.0
    IL_0011:  ret
  } // end of method IBlah_Impl::Blah_Protected_GetC
} // end of class IBlah

.class interface private abstract auto ansi IFoo
{
  .method public hidebysig newslot virtual 
          instance int32  Foo(int32 a) cil managed
  {
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IFoo.Foo"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.1
    IL_000e:  add
    IL_000f:  stloc.0
    IL_0010:  br.s       IL_0012

    IL_0012:  ldloc.0
    IL_0013:  ret
  } // end of method IFoo::Foo

} // end of class IFoo

.class interface private abstract auto ansi IBar
{
  .method public hidebysig newslot virtual 
          instance int32  Bar(int32 b) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBar.Bar"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   10
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method IBar::Bar

} // end of class IBar

.class private auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
       implements IBlah
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Base::.ctor

} // end of class Base

.class private auto ansi beforefieldinit FooBar
       extends Base
       implements IFoo,
                  IBar
{
  .method public hidebysig instance int32 
          CallBlahProtected() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  call       instance int32 IBlah::Blah_Protected_GetC()
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method FooBar::CallBlahProtected

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Base::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method FooBar::.ctor

} // end of class FooBar

.class private auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       158 (0x9e)
    .maxstack  2
    .locals init (class FooBar V_0,
             class IFoo V_1,
             class IBar V_2,
             class IBlah V_3,
             int32 V_4)
    IL_0000:  nop
    IL_0001:  newobj     instance void FooBar::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  stloc.1
    IL_0009:  ldloc.0
    IL_000a:  stloc.2
    IL_000b:  ldloc.0
    IL_000c:  stloc.3
    IL_000d:  ldstr      "Calling IFoo.Foo on FooBar - expecting default met"
    + "hod on IFoo.Foo. "
    IL_0012:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0017:  nop
    IL_0018:  ldloc.1
    IL_0019:  ldc.i4.s   10
    IL_001b:  callvirt   instance int32 IFoo::Foo(int32)
    IL_0020:  ldc.i4.s   11
    IL_0022:  ceq
    IL_0024:  ldstr      "Calling IFoo.Foo on FooBar"
    IL_0029:  call       void Test::Assert(bool,
                                           string)
    IL_002e:  nop
    IL_002f:  ldstr      "Calling IBar.Bar on FooBar - expecting default met"
    + "hod on IBar.Bar. "
    IL_0034:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0039:  nop
    IL_003a:  ldloc.2
    IL_003b:  ldc.i4.s   10
    IL_003d:  callvirt   instance int32 IBar::Bar(int32)
    IL_0042:  ldc.i4.s   20
    IL_0044:  ceq
    IL_0046:  ldstr      "Calling IBar.Bar on FooBar"
    IL_004b:  call       void Test::Assert(bool,
                                           string)
    IL_0050:  nop
    IL_0051:  ldstr      "Calling IBlah.Blah on FooBar - expecting default m"
    + "ethod on IBlah.Blah from Base. "
    IL_0056:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_005b:  nop
    IL_005c:  ldloc.3
    IL_005d:  ldc.i4.s   10
    IL_005f:  callvirt   instance int32 IBlah::Blah(int32)
    IL_0064:  ldc.i4.s   16
    IL_0066:  ceq
    IL_0068:  ldstr      "Calling IBlah.Blah on FooBar"
    IL_006d:  call       void Test::Assert(bool,
                                           string)
    IL_0072:  nop
    IL_0073:  ldstr      "Calling FooBar.CallBlahProtected - expecting prote"
    + "cted methods on interface can be called"
    IL_0078:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_007d:  nop
    IL_007e:  ldloc.0
    IL_007f:  callvirt   instance int32 FooBar::CallBlahProtected()
    IL_0084:  ldc.i4.3
    IL_0085:  ceq
    IL_0087:  ldstr      "Calling FooBar.CallBlahProtected"
    IL_008c:  call       void Test::Assert(bool,
                                           string)
    IL_0091:  nop
    IL_0092:  call       int32 Test::Ret()
    IL_0097:  stloc.s    V_4
    IL_0099:  br.s       IL_009b

    IL_009b:  ldloc.s    V_4
    IL_009d:  ret
  } // end of method Program::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Program::.ctor

} // end of class Program

.class private auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .field private static bool Pass
  .method public hidebysig static int32  Ret() cil managed
  {
    // Code size       19 (0x13)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldsfld     bool Test::Pass
    IL_0006:  brtrue.s   IL_000c

    IL_0008:  ldc.i4.s   101
    IL_000a:  br.s       IL_000e

    IL_000c:  ldc.i4.s   0
    IL_000e:  stloc.0
    IL_000f:  br.s       IL_0011

    IL_0011:  ldloc.0
    IL_0012:  ret
  } // end of method Test::Ret

  .method public hidebysig static void  Assert(bool cond,
                                               string msg) cil managed
  {
    // Code size       47 (0x2f)
    .maxstack  2
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  ldloc.0
    IL_0004:  brfalse.s  IL_0015

    IL_0006:  nop
    IL_0007:  ldstr      "PASS"
    IL_000c:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0011:  nop
    IL_0012:  nop
    IL_0013:  br.s       IL_002e

    IL_0015:  nop
    IL_0016:  ldstr      "FAIL: "
    IL_001b:  ldarg.1
    IL_001c:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0021:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0026:  nop
    IL_0027:  ldc.i4.0
    IL_0028:  stsfld     bool Test::Pass
    IL_002d:  nop
    IL_002e:  ret
  } // end of method Test::Assert

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Test::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  stsfld     bool Test::Pass
    IL_0006:  ret
  } // end of method Test::.cctor

} // end of class Test


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file simple.res
