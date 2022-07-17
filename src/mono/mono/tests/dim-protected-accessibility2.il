.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'ProtectedAccessibility_07'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::'.ctor'(int32) =  (01 00 08 00 00 00 00 00 ) // ........

  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .custom instance void class [mscorlib]System.Diagnostics.DebuggableAttribute::'.ctor'(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) =  (01 00 07 01 00 00 00 00 ) // ........

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module ProtectedAccessibility_07.exe // GUID = {4DA4F172-A1C5-4ACE-B38C-B0705C3B61F5}


  .class interface private auto ansi abstract I1`1<T>
  {

    // method line 1
    .method family static hidebysig 
           default void M1 ()  cil managed 
    {
        // Method begins at RVA 0x2050
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  nop 
	IL_0001:  ldstr "M1"
	IL_0006:  call void class [mscorlib]System.Console::WriteLine(string)
	IL_000b:  nop 
	IL_000c:  ret 
    } // end of method I1`1::M1

  } // end of class I1`1

  .class private auto ansi beforefieldinit I3`1<T>
  	extends [mscorlib]System.Object
  	implements class I1`1<!0>  {

    // method line 2
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x205e
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  nop 
	IL_0007:  ret 
    } // end of method I3`1::.ctor

  } // end of class I3`1

  .class private auto ansi beforefieldinit I2`1<T>
  	extends class I3`1<!0>
  {

    // method line 3
    .method public static hidebysig 
           default void Test ()  cil managed 
    {
        // Method begins at RVA 0x2067
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  nop 
	IL_0001:  call void class I1`1<int32>::M1()
	IL_0006:  nop 
	IL_0007:  ret 
    } // end of method I2`1::Test

    // method line 4
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2070
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class I3`1<!T>::'.ctor'()
	IL_0006:  nop 
	IL_0007:  ret 
    } // end of method I2`1::.ctor

  } // end of class I2`1

  .class private auto ansi beforefieldinit A
  	extends [mscorlib]System.Object
  {

    // method line 5
    .method private static hidebysig 
           default void Main ()  cil managed 
    {
        // Method begins at RVA 0x2079
	.entrypoint
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  nop 
	IL_0001:  call void class I2`1<unsigned int8>::Test()
	IL_0006:  nop 
	IL_0007:  ret 
    } // end of method A::Main

    // method line 6
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x205e
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  nop 
	IL_0007:  ret 
    } // end of method A::.ctor

  } // end of class A

