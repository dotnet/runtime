.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'ConsoleApplication1'
{
  .custom instance void class [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) =  (01 00 02 00 00 00 00 00 ) // ........

  .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) =  (01 00 08 00 00 00 00 00 ) // ........

  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module ConsoleApplication1.exe // GUID = {372A8B83-2D34-44EC-A004-B57731C07D69}


  .class public auto ansi beforefieldinit TempClass`1<T>
  	extends [mscorlib]System.Object
  {

    // method line 1
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x2050
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::.ctor()
	IL_0006:  ldarg.0 
	IL_0007:  ldc.i4.0 
	IL_0008:  call instance void class TempClass`1<!0>::Method1(valuetype TempClass`1/TestEnum<!0>)
	IL_000d:  ret 
    } // end of method TempClass`1::.ctor

    // method line 2
    .method private hidebysig 
           instance default void Method1 (valuetype TempClass`1/TestEnum<!T> increaseDimension)  cil managed 
    {
        // Method begins at RVA 0x205f
	// Code size 1 (0x1)
	.maxstack 8
	IL_0000:  ret 
    } // end of method TempClass`1::Method1

  .class nested private auto ansi sealed TestEnum<T>
  	extends [mscorlib]System.Enum
  {
    .field  public specialname  rtspecialname  int32 value__
    .field public static  literal  valuetype TempClass`1/TestEnum<!0> One = int32(0x00000000)

  } // end of class TestEnum

  } // end of class TempClass`1

  .class public auto ansi beforefieldinit Program
  	extends [mscorlib]System.Object
  {

    // method line 3
    .method public static  hidebysig 
           default void Main (string[] args)  cil managed 
    {
        .custom instance void class [mscorlib]System.STAThreadAttribute::.ctor() =  (01 00 00 00 ) // ....

        // Method begins at RVA 0x2061
	.entrypoint
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  newobj instance void class TempClass`1<class Program>::.ctor()
	IL_0005:  pop 
	IL_0006:  ret 
    } // end of method Program::Main

    // method line 4
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x2069
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::.ctor()
	IL_0006:  ret 
    } // end of method Program::.ctor

  } // end of class Program

