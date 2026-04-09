.assembly extern mscorlib
{
  .ver 1:0:5000:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'bug-324535-il'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'bug-324535-il.dll' // GUID = {60452EB2-5BE7-4A6D-A5BB-CCC19EA7F8EF}


  .class public auto ansi beforefieldinit Generator
  	extends [mscorlib]System.Object
  {
    .field  private static  int32[] fld

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
    } // end of method Generator::.ctor

    // method line 2
    .method public static  hidebysig 
           default object fieldOverflow ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 20 (0x14)
	.maxstack 4
	.locals init (
		int32[]	V_0)
	IL_0000:  ldc.i4.5
	IL_0001:  newarr [mscorlib]System.Int32
	IL_0006:  dup 
	IL_0007:  ldtoken field valuetype '<PrivateImplementationDetails>'/$ArrayType$16 '<PrivateImplementationDetails>'::'$$field-0'
	IL_000c:  call void class [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype [mscorlib]System.RuntimeFieldHandle)
	IL_0011:  stloc.0 
	IL_0012:  ldloc.0 
	IL_0013:  ret 
    } // end of method Generator::fieldOverflow

    // method line 3
    .method public static  hidebysig 
           default object referenceArray ()  cil managed 
    {
        // Method begins at RVA 0x2114
	// Code size 2 (0x2)
	.maxstack 8
	IL_0016:  ldc.i4.2
	IL_0017:  newarr [mscorlib]System.Object
			dup
			ldtoken field valuetype '<PrivateImplementationDetails>'/$ArrayType$16 '<PrivateImplementationDetails>'::'$$field-0'
			call void class [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype [mscorlib]System.RuntimeFieldHandle)
	IL_0001:  ret 
    } // end of method Generator::referenceArray

    // method line 4
    .method public static  hidebysig 
           default object nonRVAField ()  cil managed 
    {
        // Method begins at RVA 0x2118
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldc.i4.1
	IL_0001:  newarr [mscorlib]System.Int32
	IL_0006:  dup 
	IL_0007:  ldtoken field int32[] Generator::fld
	IL_000c:  call void class [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype [mscorlib]System.RuntimeFieldHandle)
	IL_0013:  ret 
    } // end of method Generator::nonRVAField

  } // end of class Generator

  .class private auto ansi '<PrivateImplementationDetails>'
  	extends [mscorlib]System.Object
  {
    .field  assembly static  valuetype '<PrivateImplementationDetails>'/$ArrayType$16 '$$field-0' at D_0000211c

    // method line 5
    .method public specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x212c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::.ctor()
	IL_0006:  ret 
    } // end of method <PrivateImplementationDetails>::.ctor

  .class nested private explicit ansi sealed $ArrayType$16
  	extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 16

  } // end of class $ArrayType$16

  } // end of class <PrivateImplementationDetails>

.data D_0000211c = bytearray (
	 01 00 00 00 02 00 00 00 03 00 00 00 04 00 00 00) // size: 16
