.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'dd'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module dd.exe // GUID = {7278218D-519A-4CA2-B185-7B5A5A3F17E7}


  .class public auto ansi beforefieldinit Foo`1<(class [mscorlib]System.BrokenIComparable`1<!T>) T>
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
    } // end of method Foo`1::.ctor

    // method line 2
    .method public hidebysig 
           instance default string Test ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldnull 
	IL_0001:  ret 
    } // end of method Foo`1::Test

  } // end of class Foo`1

  .class public auto ansi beforefieldinit Foo
  	extends [mscorlib]System.Object
  {

    // method line 3
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20f8
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Foo::.ctor

    // method line 4
    .method public hidebysig 
           instance default string Test<(class [mscorlib]System.BrokenIComparable`1<!!T>) T> ()  cil managed 
    {
        // Method begins at RVA 0x2100
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldnull 
	IL_0001:  ret 
    } // end of method Foo::Test

  } // end of class Foo

  .class public auto ansi abstract sealed beforefieldinit Driver
  	extends [mscorlib]System.Object
  {

    // method line 5
    .method private static  hidebysig 
           default void UseBrokenMethod ()  cil managed 
    {
        // Method begins at RVA 0x2104
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  newobj instance void class Foo::'.ctor'()
	IL_0005:  callvirt instance string class Foo::Test<int32> ()
	IL_000a:  pop 
	IL_000b:  ret 
    } // end of method Driver::UseBrokenMethod

    // method line 6
    .method private static  hidebysig 
           default void UseBrokenType ()  cil managed 
    {
        // Method begins at RVA 0x2114
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  newobj instance void class Foo`1<int32>::'.ctor'()
	IL_0005:  callvirt instance string class Foo`1<int32>::Test()
	IL_000a:  pop 
	IL_000b:  ret 
    } // end of method Driver::UseBrokenType

    // method line 7
    .method private static  hidebysig 
           default void TestMethod ()  cil managed 
    {
        // Method begins at RVA 0x2124
	// Code size 47 (0x2f)
	.maxstack 10
	.locals init (
		class [mscorlib]System.Type	V_0,
		class [mscorlib]System.Reflection.MethodInfo	V_1,
		class [mscorlib]System.Type	V_2,
		class [mscorlib]System.Type	V_3)
	IL_0000:  ldtoken Foo
	IL_0005:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_000a:  stloc.0 
	IL_000b:  ldloc.0 
	IL_000c:  ldstr "Test"
	IL_0011:  callvirt instance class [mscorlib]System.Reflection.MethodInfo class [mscorlib]System.Type::GetMethod(string)
	IL_0016:  stloc.1 
	IL_0017:  ldloc.1 
	IL_0018:  callvirt instance class [mscorlib]System.Type[] class [mscorlib]System.Reflection.MethodInfo::GetGenericArguments()
	IL_001d:  ldc.i4.0 
	IL_001e:  ldelem.ref 
	IL_001f:  stloc.2 
	IL_0020:  ldloc.2 
	IL_0021:  callvirt instance class [mscorlib]System.Type[] class [mscorlib]System.Type::GetGenericParameterConstraints()
	IL_0026:  ldc.i4.0 
	IL_0027:  ldelem.ref 
	IL_0028:  stloc.3 
	IL_0029:  call void class Driver::UseBrokenMethod()
	IL_002e:  ret 
    } // end of method Driver::TestMethod

    // method line 8
    .method private static  hidebysig 
           default void TestClass ()  cil managed 
    {
        // Method begins at RVA 0x2160
	// Code size 35 (0x23)
	.maxstack 8
	.locals init (
		class [mscorlib]System.Type	V_0,
		class [mscorlib]System.Type	V_1,
		class [mscorlib]System.Type	V_2)
	IL_0000:  ldtoken Foo`1
	IL_0005:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_000a:  stloc.0 
	IL_000b:  ldloc.0 
	IL_000c:  callvirt instance class [mscorlib]System.Type[] class [mscorlib]System.Type::GetGenericArguments()
	IL_0011:  ldc.i4.0 
	IL_0012:  ldelem.ref 
	IL_0013:  stloc.1 
	IL_0014:  ldloc.1 
	IL_0015:  callvirt instance class [mscorlib]System.Type[] class [mscorlib]System.Type::GetGenericParameterConstraints()
	IL_001a:  ldc.i4.0 
	IL_001b:  ldelem.ref 
	IL_001c:  stloc.2 
	IL_001d:  call void class Driver::UseBrokenType()
	IL_0022:  ret 
    } // end of method Driver::TestClass

    // method line 9
    .method public static  hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x2190
	.entrypoint
	// Code size 50 (0x32)
	.maxstack 4
	.locals init (
		int32	V_0)
	.try { // 1
	  IL_0000:  call void class Driver::TestMethod()
	  IL_0005:  ldc.i4.1 
	  IL_0006:  stloc.0 
	  IL_0007:  leave IL_0030

	  IL_000c:  leave IL_0017

	} // end .try 1
	catch class [mscorlib]System.TypeLoadException { // 1
	  IL_0011:  pop 
	  IL_0012:  leave IL_0017

	} // end handler 1
	.try { // 0
	  IL_0017:  call void class Driver::TestClass()
	  IL_001c:  ldc.i4.2 
	  IL_001d:  stloc.0 
	  IL_001e:  leave IL_0030

	  IL_0023:  leave IL_002e

	} // end .try 0
	catch class [mscorlib]System.TypeLoadException { // 0
	  IL_0028:  pop 
	  IL_0029:  leave IL_002e

	} // end handler 0
	IL_002e:  ldc.i4.0 
	IL_002f:  ret 
	IL_0030:  ldloc.0 
	IL_0031:  ret 
    } // end of method Driver::Main

  } // end of class Driver

