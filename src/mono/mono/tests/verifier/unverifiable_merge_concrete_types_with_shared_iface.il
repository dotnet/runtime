.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'test-997'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'test-997.exe' // GUID = {AAE37F83-0848-454C-B66A-0AEEA55FFB63}


  .class private auto ansi beforefieldinit C
  	extends [mscorlib]System.Object
  {
    .field  private  bool multiDomain

    // method line 1
    .method public static hidebysig 
           default void Main ()  cil managed 
    {
        // Method begins at RVA 0x2050
	.entrypoint
	// Code size 1 (0x1)
	.maxstack 8
	IL_0000:  ret 
    } // end of method C::Main

    // method line 2
    .method private hidebysig 
           instance default class IA Create (bool arg)  cil managed 
    {
        // Method begins at RVA 0x2054
	// Code size 24 (0x18)
	.maxstack 1
	.locals init (
		class IA	V_0)
	IL_0000:  ldarg.1 
	IL_0001:  brfalse IL_0010

	IL_0006:  newobj instance void class B2::'.ctor'()
	IL_000b:  br IL_0015

	IL_0010:  newobj instance void class B1::'.ctor'()
	IL_0015:  stloc.0 
	IL_0016:  ldloc.0 
	IL_0017:  ret 
    } // end of method C::Create

    // method line 3
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2078
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method C::.ctor

  } // end of class C

  .class interface private auto ansi abstract IA
  {

  } // end of class IA

  .class private auto ansi beforefieldinit B2
  	extends [mscorlib]System.Object
  	implements IA  {

    // method line 4
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2080
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method B2::.ctor

  } // end of class B2

  .class private auto ansi beforefieldinit B1
  	extends [mscorlib]System.Object
  	implements IA  {

    // method line 5
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2088
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method B1::.ctor

  } // end of class B1

  .class private auto ansi beforefieldinit B
  	extends [mscorlib]System.Object
  	implements IA  {

    // method line 6
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2090
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method B::.ctor

  } // end of class B

