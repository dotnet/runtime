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

  .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::'.ctor'(int32) =  (01 00 08 00 00 00 00 00 ) // ........

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module test.exe // GUID = {E2A189E0-A26B-4210-99AE-84ECC4B299A5}


  .class private auto ansi beforefieldinit Test
  	extends [mscorlib]System.Object
  {

    // method line 1
    .method private static  hidebysig 
           default void Main ()  cil managed 
    {
        // Method begins at RVA 0x2050
	.entrypoint
	// Code size 31 (0x1f)
	.maxstack 2
	.locals init (valuetype [mscorlib]System.Nullable`1<float64>	V_0)
		//ldloca.s 0
		//initobj valuetype [mscorlib]System.Nullable`1<float64>
		ldloc.0
		ldloc.0
		call bool class Test::AreEqual(valuetype [mscorlib]System.Nullable`1<float64>, valuetype [mscorlib]System.Nullable`1<float64>) 
		pop
		ret 
    }

    // method line 2
    .method private static  hidebysig 
           default bool AreEqual (valuetype [mscorlib]System.Nullable`1<float64> A_1, valuetype [mscorlib]System.Nullable`1<float64> A_2)  cil managed 
    {
        // Method begins at RVA 0x207c
	// Code size 67 (0x43)
	.maxstack 4
	.locals init (
		bool	V_0,
		valuetype [mscorlib]System.Nullable`1<float64>	V_1,
		valuetype [mscorlib]System.Nullable`1<float64>	V_2)
		ldarg 0
		stloc.1
		ldc.i4.0
		ret 
    }
}

