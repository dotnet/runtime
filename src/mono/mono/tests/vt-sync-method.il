.assembly extern mscorlib
{
  .ver 1:0:5000:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'vt-sync'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'vt-sync.exe' // GUID = {92777FC4-E766-42CE-B02F-DFA9AF516D3E}


  .class private auto ansi beforefieldinit T
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
    } // end of method T::.ctor

    // method line 2
    .method public static  hidebysig 
           default int32 test ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 18 (0x12)
	.maxstack 3
	.locals init (
		valuetype Test	V_0,
		int32	V_1)
	IL_0000:  ldloca.s 0
	IL_0002:  initobj Test
	IL_0008:  ldloca.s 0
	IL_000a:  call instance int32 valuetype Test::test()
	IL_000f:  stloc.1 
	IL_0010:  ldloc.1 
	IL_0011:  ret 
    } // end of method T::test

    // method line 3
    .method public static  hidebysig 
           default int32 Main (string[] args)  cil managed 
    {
        // Method begins at RVA 0x2114
	.entrypoint
	// Code size 28 (0x1c)
	.maxstack 3
	.locals init (
		int32	V_0)
	.try { // 0
	  IL_0000:  call int32 class T::test()
	  IL_0005:  pop 
	  IL_0006:  leave IL_0018

	} // end .try 0
	catch [mscorlib]System.TypeLoadException { // 0
	  IL_000b:  pop 
	  IL_000c:  ldc.i4.0 
	  IL_000d:  stloc.0 
	  IL_000e:  leave IL_001a

	  IL_0013:  leave IL_0018

	} // end handler 0
	IL_0018:  ldc.i4.1 
	IL_0019:  ret 
	IL_001a:  ldloc.0 
	IL_001b:  ret 
    } // end of method T::Main

  } // end of class T

  .class private sequential ansi sealed beforefieldinit Test
  	extends [mscorlib]System.ValueType
  {
    .field  private  unsigned int8 $PRIVATE$

    // method line 4
    .method public hidebysig 
           instance default int32 test ()  cil managed synchronized 
    {
        // Method begins at RVA 0x2158
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldc.i4.4 
	IL_0001:  ret 
    } // end of method Test::test

  } // end of class Test

