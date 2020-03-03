.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'sre'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module modulename // GUID = {30125A6A-77AE-4084-B2EA-4B9F0F64A1B9}


  .class public auto ansi Sample<TFirst>
  	extends [mscorlib]System.Object
  {
    .field  public  class Sample/'nested'<!0> 'field'

    // method line 1
    .method public specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Sample::.ctor

    // method line 2
    .method public 
           instance default void test (class Sample<!TFirst> A_1)  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  castclass class Sample/'nested'
	IL_0006:  pop 
	IL_0007:  ret 
    } // end of method Sample::test

  .class nested private auto ansi 'nested'<TFirst>
  	extends [mscorlib]System.Object
  {

    // method line 3
    .method public specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2100
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method nested::.ctor

  } // end of class nested

  } // end of class Sample

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 8
	newobj instance void class Sample<int32>::.ctor()
	ldnull
	call instance void class Sample<int32>::test(class Sample<!0>)
	ldc.i4.0
	ret
}
