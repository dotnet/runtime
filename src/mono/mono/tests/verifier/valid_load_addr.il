.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'bla'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module bla.exe // GUID = {8936712E-615B-4184-B427-9B50704538E2}

.method static public void ByRefParam (int32&)
{
	.maxstack 8
	.locals init (int32 &V_0)
	ret
}

.method static public void Blue (int32)
{
	.maxstack 8
	.locals init (int32& V_0)

	ldarga 0
	call void ByRefParam(int32&)
	ret
}


.method static public void ZZZ ()
{
	.maxstack 8
	.locals init (int32 V_0)

	ldloc.0
	call void Blue(int32)
	ldloca 0
	call void ByRefParam(int32&)
	ret
}

  .class public auto ansi beforefieldinit Example
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
    } // end of method Example::.ctor

    // method line 2
    .method public static  hidebysig 
           default int32 Main ()  cil managed 
    {
	.entrypoint
		.maxstack 2
		call void ZZZ ()
		ldc.i4.0
		ret 
    }

  } // end of class Example

