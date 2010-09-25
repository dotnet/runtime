.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'generic-array-exc.2'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'generic-array-exc.2.exe' // GUID = {770295FB-70A6-4755-9969-EFE664D15786}


  .class public auto ansi beforefieldinit ClassA
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
    } // end of method ClassA::.ctor

  } // end of class ClassA

  .class public auto ansi beforefieldinit GenExc`1<T>
  	extends [mscorlib]System.Exception
  {

    // method line 2
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.Exception::.ctor()
	IL_0006:  ret 
    } // end of method GenExc`1::.ctor

  } // end of class GenExc`1

  .class public auto ansi beforefieldinit NonGen
  	extends [mscorlib]System.Object
  {

    // method line 3
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x20fc
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::.ctor()
	IL_0006:  ret 
    } // end of method NonGen::.ctor

    // method line 4
    .method public static  hidebysig 
           default void doThrow ()  cil managed 
    {
        // Method begins at RVA 0x2104
	// Code size 6 (0x6)
	.maxstack 8
	IL_0000:  ldc.i4.3 
	IL_0001:  newarr ClassA
	IL_0005:  throw 
    } // end of method NonGen::doThrow

    // method line 5
    .method public static  hidebysig 
           default object newArr ()  cil managed 
    {
        // Method begins at RVA 0x210c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldc.i4.3 
	IL_0001:  newarr ClassA
	IL_0006:  ret 
    } // end of method NonGen::newArr

  } // end of class NonGen

  .class public auto ansi beforefieldinit Gen`1<T>
  	extends [mscorlib]System.Object
  {

    // method line 6
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x2114
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::.ctor()
	IL_0006:  ret 
    } // end of method Gen`1::.ctor

    // method line 7
    .method public hidebysig 
           instance default void except ()  cil managed 
    {
        // Method begins at RVA 0x211c
	// Code size 17 (0x11)
	.maxstack 1
	.try { // 0
	  IL_0000:  call void class NonGen::doThrow()
	  IL_0005:  leave IL_0010

	} // end .try 0
	catch !0[] { // 0
	  IL_000a:  pop 
	  IL_000b:  leave IL_0010

	} // end handler 0
	IL_0010:  ret 
    } // end of method Gen`1::except

  } // end of class Gen`1

  .class public auto ansi beforefieldinit main
  	extends [mscorlib]System.Object
  {

    // method line 8
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x2158
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::.ctor()
	IL_0006:  ret 
    } // end of method main::.ctor

    // method line 9
    .method public static  hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x2160
	.entrypoint
	// Code size 44 (0x2c)
	.maxstack 4
	.locals init (
		class Gen`1<class ClassA>	V_0,
		int32	V_1)
	IL_0000:  newobj instance void class Gen`1<class ClassA>::.ctor()
	IL_0005:  stloc.0 
	.try { // 0
	  IL_0006:  ldloc.0 
	  IL_0007:  callvirt instance void class Gen`1<class ClassA>::except()
	  IL_000c:  leave IL_0028

	} // end .try 0
	catch class ClassA[] { // 0
	  IL_0011:  pop 
	  IL_0012:  ldstr "exception not caught"
	  IL_0017:  call void class [mscorlib]System.Console::WriteLine(string)
	  IL_001c:  ldc.i4.1 
	  IL_001d:  stloc.1 
	  IL_001e:  leave IL_002a

	  IL_0023:  leave IL_0028

	} // end handler 0
	IL_0028:  ldc.i4.0 
	IL_0029:  ret 
	IL_002a:  ldloc.1 
	IL_002b:  ret 
    } // end of method main::Main

  } // end of class main

