.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly extern 'null-blob-null-blob-assm'
{
  .ver 0:0:0:0
}
.assembly 'null-blob-ref'
{
  .ver  0:0:0:0
}
.module 'null-blob-ref.dll'


  .class public auto ansi beforefieldinit C2
  	extends [mscorlib]System.Object
  {

    // method line 1
    .method public hidebysig
           instance default void M1 (class ['null-blob-null-blob-assm']C1 c1)  cil managed
    {
        // Method begins at RVA 0x2050
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  nop
	IL_0001:  ret
    } // end of method C2::M1

    // method line 2
    .method public hidebysig specialname rtspecialname
           instance default void '.ctor' ()  cil managed
    {
        // Method begins at RVA 0x2053
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  nop
	IL_0007:  ret
    } // end of method C2::.ctor

  } // end of class C2

