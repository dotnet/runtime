.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'generic-unboxing.2'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'generic-unboxing.2.dll' // GUID = {0285A5AB-E59B-4ED1-848C-3BB9C270CB7D}

.namespace UnboxTest
{
  .class public sequential ansi sealed beforefieldinit UnboxerStruct`1<T>
  	extends [mscorlib]System.ValueType
  {
    .field  public  int32 'field'

  } // end of class UnboxTest.UnboxerStruct`1
}

.namespace UnboxTest
{
  .class public auto ansi beforefieldinit Unboxer`1<T>
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
    } // end of method Unboxer`1::.ctor

    // method line 2
    .method public hidebysig 
           instance default valuetype [mscorlib]System.Nullable`1<valuetype UnboxTest.UnboxerStruct`1<!T>> 'unboxNullable' (object obj)  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.1 
	//	IL_0001:  unbox.any valuetype [mscorlib]System.Nullable`1<valuetype UnboxerStruct`1<!0>>
	IL_0001:  unbox valuetype [mscorlib]System.Nullable`1<valuetype UnboxTest.UnboxerStruct`1<!0>>
	IL_0002:  ldobj valuetype [mscorlib]System.Nullable`1<valuetype UnboxTest.UnboxerStruct`1<!0>>
	IL_0006:  ret 
    } // end of method Unboxer`1::unboxNullable

    // method line 2
    .method public hidebysig 
           instance default valuetype UnboxTest.UnboxerStruct`1<!T> 'unbox' (object obj)  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  unbox valuetype UnboxTest.UnboxerStruct`1<!0>
	IL_0002:  ldobj valuetype UnboxTest.UnboxerStruct`1<!0>
	IL_0006:  ret 
    } // end of method Unboxer`1::unbox

  } // end of class Unboxer`1

}
