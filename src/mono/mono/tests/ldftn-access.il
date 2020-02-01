.assembly extern mscorlib
{
  .ver 1:0:5000:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'test-del-access'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'test-del-access.exe' // GUID = {3B188D52-4BD2-41E1-AD35-3CCAA38D8683}


  .class private auto ansi beforefieldinit A
  	extends [mscorlib]System.Object
  {

    // method line 1
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::.ctor()
	IL_0006:  ret 
    } // end of method A::.ctor

    // method line 2
    .method private static  hidebysig 
           default void test ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 1 (0x1)
	.maxstack 8
	IL_0000:  ret 
    } // end of method A::test

  } // end of class A

  .class private auto ansi beforefieldinit B
  	extends [mscorlib]System.Object
  {

    // method line 3
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x20f8
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::.ctor()
	IL_0006:  ret 
    } // end of method B::.ctor

    .method private static  hidebysig 
           default void testdel ()  cil managed 
    {
	.locals init (
		class B/MyDel	V_0)
	  IL_0000:  ldnull 
	  IL_0001:  ldftn void class A::test()
	  IL_0007:  newobj instance void class B/MyDel::.ctor(object, native int)
	  IL_000c:  stloc.0 
	  IL_000d:  ldloc.0 
	  IL_000e:  callvirt instance void class B/MyDel::Invoke()
	  IL_0028:  ret 
    }

    // method line 4
    .method private static  hidebysig 
           default int32 test ()  cil managed 
    {
        // Method begins at RVA 0x2100
	.entrypoint
	// Code size 41 (0x29)
	.maxstack 4
	.locals init (
		class B/MyDel	V_0,
		int32	V_1)
	.try { // 0
	  IL_000e:  call void B::testdel()
	  IL_0013:  leave IL_0025

	} // end .try 0
	catch [mscorlib]System.MethodAccessException { // 0
	  IL_0018:  pop 
	  IL_0019:  ldc.i4.0 
	  IL_001a:  stloc.1 
	  IL_001b:  leave IL_0027

	  IL_0020:  leave IL_0025

	} // end handler 0
	IL_0025:  ldc.i4.1 
	IL_0026:  ret 
	IL_0027:  ldloc.1 
	IL_0028:  ret 
    } // end of method B::Main

  .class nested private auto ansi sealed MyDel
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 5
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method MyDel::.ctor

    // method line 6
    .method public virtual  hidebysig  newslot 
           instance default void Invoke ()  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method MyDel::Invoke

    // method line 7
    .method public virtual  hidebysig  newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method MyDel::BeginInvoke

    // method line 8
    .method public virtual  hidebysig  newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method MyDel::EndInvoke

  } // end of class MyDel

  } // end of class B

