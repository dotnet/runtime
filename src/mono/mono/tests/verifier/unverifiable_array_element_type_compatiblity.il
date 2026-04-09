.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly extern System
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'test-32'
{
  .hash algorithm 0x00000000
  .ver  0:0:0:0
}
.module 'test-32' // GUID = {6434201F-9537-4C39-8064-7F35FE5E39DD}


  .class public auto ansi beforefieldinit Blah
  	extends [mscorlib]System.Object
  {

    // method line 1
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2050
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Blah::.ctor

    // method line 2
    .method public static  hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x2058
	.entrypoint
	// Code size 52 (0x34)
	.maxstack 4
	.locals init (
		//object V_0)
		int32[][]	V_0) 
	IL_0008:  ldc.i4.6 
	IL_0009:  newobj instance void int32[,][]::'.ctor'(int32)
	IL_000e:  stloc.0
	
	
	
	ldc.i4.0
	IL_0033:  ret 
    } // end of method Blah::Main

  } // end of class Blah

