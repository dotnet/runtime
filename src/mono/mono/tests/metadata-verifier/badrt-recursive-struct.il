.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'test'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module test.exe // GUID = {732E02C2-108F-4B9D-BF92-FF55756E2922}


  .class private sequential ansi sealed beforefieldinit Bar
  	extends [mscorlib]System.ValueType
  {
    .field  private  int32 x

  } // end of class Bar

  .class private sequential ansi sealed beforefieldinit Foo
  	extends [mscorlib]System.ValueType
  {
    .field  private  valuetype Foo x

  } // end of class Foo

  .class private sequential ansi sealed beforefieldinit Mutual0
  	extends [mscorlib]System.ValueType
  {
    .field  private  valuetype Mutual1 x

  } // end of class Mutual0

  .class private sequential ansi sealed beforefieldinit Mutual1
  	extends [mscorlib]System.ValueType
  {
    .field  private  valuetype Mutual0 x

  } // end of class Mutual1

  .class public auto ansi beforefieldinit Driver
  	extends [mscorlib]System.Object
  {

    // method line 1
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Driver::.ctor

    // method line 2
    .method private static  hidebysig 
           default int32 Test0 ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 29 (0x1d)
	.maxstack 2
	.locals init (
		valuetype Foo	V_0,
		object	V_1)
	IL_0000:  ldloca.s 0
	IL_0002:  initobj Foo
	IL_0008:  ldloc.0 
	IL_0009:  box Foo
	IL_000e:  stloc.1 
	IL_000f:  ldloc.1 
	IL_0010:  brfalse IL_001b

	IL_0015:  ldc.i4.1 
	IL_0016:  br IL_001c

	IL_001b:  ldc.i4.0 
	IL_001c:  ret 
    } // end of method Driver::Test0

    // method line 3
    .method private static  hidebysig 
           default int32 Test1 ()  cil managed 
    {
        // Method begins at RVA 0x2120
	// Code size 29 (0x1d)
	.maxstack 2
	.locals init (
		valuetype Mutual0	V_0,
		object	V_1)
	IL_0000:  ldloca.s 0
	IL_0002:  initobj Mutual0
	IL_0008:  ldloc.0 
	IL_0009:  box Mutual0
	IL_000e:  stloc.1 
	IL_000f:  ldloc.1 
	IL_0010:  brfalse IL_001b

	IL_0015:  ldc.i4.1 
	IL_0016:  br IL_001c

	IL_001b:  ldc.i4.0 
	IL_001c:  ret 
    } // end of method Driver::Test1

    // method line 4
    .method private static  hidebysig 
           default int32 Test2 ()  cil managed 
    {
        // Method begins at RVA 0x214c
	// Code size 29 (0x1d)
	.maxstack 2
	.locals init (
		valuetype Mutual1	V_0,
		object	V_1)
	IL_0000:  ldloca.s 0
	IL_0002:  initobj Mutual1
	IL_0008:  ldloc.0 
	IL_0009:  box Mutual1
	IL_000e:  stloc.1 
	IL_000f:  ldloc.1 
	IL_0010:  brfalse IL_001b

	IL_0015:  ldc.i4.1 
	IL_0016:  br IL_001c

	IL_001b:  ldc.i4.0 
	IL_001c:  ret 
    } // end of method Driver::Test2

    // method line 5
    .method private static  hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x2178
	.entrypoint
	// Code size 76 (0x4c)
	.maxstack 5
	.locals init (
		int32	V_0)
	.try { // 2
	  IL_0000:  call int32 class Driver::Test0()
	  IL_0005:  pop 
	  IL_0006:  ldc.i4.1 
	  IL_0007:  stloc.0 
	  IL_0008:  leave IL_004a

	  IL_000d:  leave IL_0018

	} // end .try 2
	catch class [mscorlib]System.TypeLoadException { // 2
	  IL_0012:  pop 
	  IL_0013:  leave IL_0018

	} // end handler 2
	.try { // 1
	  IL_0018:  call int32 class Driver::Test1()
	  IL_001d:  pop 
	  IL_001e:  ldc.i4.2 
	  IL_001f:  stloc.0 
	  IL_0020:  leave IL_004a

	  IL_0025:  leave IL_0030

	} // end .try 1
	catch class [mscorlib]System.TypeLoadException { // 1
	  IL_002a:  pop 
	  IL_002b:  leave IL_0030

	} // end handler 1
	.try { // 0
	  IL_0030:  call int32 class Driver::Test2()
	  IL_0035:  pop 
	  IL_0036:  ldc.i4.3 
	  IL_0037:  stloc.0 
	  IL_0038:  leave IL_004a

	  IL_003d:  leave IL_0048

	} // end .try 0
	catch class [mscorlib]System.TypeLoadException { // 0
	  IL_0042:  pop 
	  IL_0043:  leave IL_0048

	} // end handler 0
	IL_0048:  ldc.i4.0 
	IL_0049:  ret 
	IL_004a:  ldloc.0 
	IL_004b:  ret 
    } // end of method Driver::Main

  } // end of class Driver

