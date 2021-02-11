.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'repro'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module repro.exe // GUID = {598DED0A-DD7D-47FF-B2BA-8C50F6437129}


  .class private auto ansi beforefieldinit Program
  	extends [mscorlib]System.Object
  {
    .field public static  literal  int32 A = int32(0x0000000a)
    .field public static  int32 B

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
    } // end of method Program::.ctor

    // method line 2
    .method private static  hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	.entrypoint
	// Code size 8 (0x8)
	.maxstack 1
	.locals init (
		int32	V_0)
	IL_0000:  ldsfld int32 Program::A
	IL_0005:  stloc.0 
	IL_0006:  ldloc.0 
	IL_0007:  ret 
    } // end of method Program::Main

  } // end of class Program

