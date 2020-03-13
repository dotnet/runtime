.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'gtest-116'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'gtest-116.exe' // GUID = {ADBACCC6-7EC7-4C8F-A829-4E8B7BCFF277}


.namespace Slow
{
  .class interface public auto ansi abstract ITest
  {

    // method line 1
    .method public virtual  hidebysig  newslot  abstract 
           instance default void DoNothing<class T> ()  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method ITest::DoNothing

  } // end of class Slow.ITest
}

.namespace Slow
{
  .class public auto ansi beforefieldinit Test
  	extends [mscorlib]System.Object
  	implements Slow.ITest  {

    // method line 2
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Test::.ctor

    // method line 3
    .method public final  virtual  hidebysig  newslot 
           instance default void DoNothing<class T> ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 8 (0x8)
	.maxstack 2
	.locals init (
		!!T	V_0)
	IL_0000:  ldnull 
	IL_0001:  unbox.any !!0
	IL_0006:  stloc.0 
	IL_0007:  ret 
    } // end of method Test::DoNothing

  } // end of class Slow.Test
}

.namespace Slow
{
  .class private auto ansi beforefieldinit Program
  	extends [mscorlib]System.Object
  {

    // method line 4
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2108
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Program::.ctor

    // method line 5
    .method private static  hidebysig 
           default void Main (string[] args)  cil managed 
    {
        // Method begins at RVA 0x2110
	.entrypoint
	// Code size 201 (0xc9)
	.maxstack 22
	.locals init (
		int32	V_0,
		class Slow.Test	V_1,
		valuetype [mscorlib]System.DateTime	V_2,
		int32	V_3,
		valuetype [mscorlib]System.DateTime	V_4,
		valuetype [mscorlib]System.TimeSpan	V_5,
		class Slow.ITest	V_6,
		int32	V_7)
	IL_0000:  newobj instance void class Slow.Test::'.ctor'()
	IL_0005:  stloc.1 
	IL_0006:  call valuetype [mscorlib]System.DateTime valuetype [mscorlib]System.DateTime::get_Now()
	IL_000b:  stloc.2 
	IL_000c:  ldstr "Calling Test.DoNothing<Program>() on an object reference...  "
	IL_0011:  call void class [mscorlib]System.Console::Write(string)
	IL_0016:  ldc.i4.0 
	IL_0017:  stloc.3 
	IL_0018:  br IL_0027

	IL_001d:  ldloc.1 
	IL_001e:  callvirt instance void class Slow.Test::DoNothing<class Slow.Program> ()
	IL_0023:  ldloc.3 
	IL_0024:  ldc.i4.1 
	IL_0025:  add 
	IL_0026:  stloc.3 
	IL_0027:  ldloc.3 
	IL_0028:  ldc.i4 10000
	IL_002d:  blt IL_001d

	IL_0032:  call valuetype [mscorlib]System.DateTime valuetype [mscorlib]System.DateTime::get_Now()
	IL_0037:  stloc.s 4
	IL_0039:  ldloc.s 4
	IL_003b:  ldloc.2 
	IL_003c:  call valuetype [mscorlib]System.TimeSpan valuetype [mscorlib]System.DateTime::op_Subtraction(valuetype [mscorlib]System.DateTime, valuetype [mscorlib]System.DateTime)
	IL_0041:  stloc.s 5
	IL_0043:  ldstr "Took "
	IL_0048:  ldloca.s 5
	IL_004a:  call instance float64 valuetype [mscorlib]System.TimeSpan::get_TotalMilliseconds()
	IL_004f:  box [mscorlib]System.Double
	IL_0054:  ldstr " ms."
	IL_0059:  call string string::Concat(object, object, object)
	IL_005e:  call void class [mscorlib]System.Console::WriteLine(string)
	IL_0063:  ldloc.1 
	IL_0064:  stloc.s 6
	IL_0066:  call valuetype [mscorlib]System.DateTime valuetype [mscorlib]System.DateTime::get_Now()
	IL_006b:  stloc.2 
	IL_006c:  ldstr "Calling Test.DoNothing<Program>() on an interface reference...  "
	IL_0071:  call void class [mscorlib]System.Console::Write(string)
	IL_0076:  ldc.i4.0 
	IL_0077:  stloc.s 7
	IL_0079:  br IL_008b

	IL_007e:  ldloc.s 6
	IL_0080:  callvirt instance void class Slow.ITest::DoNothing<class Slow.Program> ()
	IL_0085:  ldloc.s 7
	IL_0087:  ldc.i4.1 
	IL_0088:  add 
	IL_0089:  stloc.s 7
	IL_008b:  ldloc.s 7
	IL_008d:  ldc.i4 10000
	IL_0092:  blt IL_007e

	IL_0097:  call valuetype [mscorlib]System.DateTime valuetype [mscorlib]System.DateTime::get_Now()
	IL_009c:  stloc.s 4
	IL_009e:  ldloc.s 4
	IL_00a0:  ldloc.2 
	IL_00a1:  call valuetype [mscorlib]System.TimeSpan valuetype [mscorlib]System.DateTime::op_Subtraction(valuetype [mscorlib]System.DateTime, valuetype [mscorlib]System.DateTime)
	IL_00a6:  stloc.s 5
	IL_00a8:  ldstr "Took "
	IL_00ad:  ldloca.s 5
	IL_00af:  call instance float64 valuetype [mscorlib]System.TimeSpan::get_TotalMilliseconds()
	IL_00b4:  box [mscorlib]System.Double
	IL_00b9:  ldstr " ms."
	IL_00be:  call string string::Concat(object, object, object)
	IL_00c3:  call void class [mscorlib]System.Console::WriteLine(string)
	IL_00c8:  ret 
    } // end of method Program::Main

  } // end of class Slow.Program
}

