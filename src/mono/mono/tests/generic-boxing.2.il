.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly extern 'generic-unboxing.2'
{
  .ver 0:0:0:0
}
.assembly 'generic-boxing.2'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'generic-boxing.2.dll' // GUID = {9C29868F-7AD1-40CC-BBDC-DF985AB656FC}


.namespace BoxTest
{
  .class public auto ansi beforefieldinit Boxer`1<T>
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
    } // end of method Boxer`1::.ctor

    // method line 2
    .method public hidebysig 
           instance default object boxNullable (valuetype [mscorlib]System.Nullable`1<valuetype ['generic-unboxing.2']UnboxTest.UnboxerStruct`1<!T>> us)  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 15 (0xf)
	.maxstack 8
	IL_0000:  ldarg.1 
	//	IL_0004:  call instance !0 valuetype [mscorlib]System.Nullable`1<valuetype ['generic-unboxing.2']UnboxTest.UnboxerStruct`1<!T>>::get_Value()
	IL_0009:  box valuetype [mscorlib]System.Nullable`1<valuetype ['generic-unboxing.2']UnboxTest.UnboxerStruct`1<!T>>
	IL_000e:  ret 
    } // end of method Boxer`1::boxNullable

  } // end of class BoxTest.Boxer`1
}

