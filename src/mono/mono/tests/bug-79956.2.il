.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'ConsoleApplication1'
{
  .custom instance void class [mscorlib]System.Reflection.AssemblyFileVersionAttribute::.ctor(string) =  (01 00 07 31 2E 30 2E 30 2E 30 00 00 ) // ...1.0.0.0..

  .custom instance void class [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) =  (
		01 00 24 36 32 66 38 34 30 35 36 2D 39 62 35 34   // ..$62f84056-9b54
		2D 34 34 61 38 2D 62 63 30 63 2D 30 36 62 39 34   // -44a8-bc0c-06b94
		65 64 37 61 39 63 61 00 00                      ) // ed7a9ca..

  .custom instance void class [mscorlib]System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool) =  (01 00 00 00 00 ) // .....

  .custom instance void class [mscorlib]System.Reflection.AssemblyTrademarkAttribute::.ctor(string) =  (01 00 00 00 00 ) // .....

  .custom instance void class [mscorlib]System.Reflection.AssemblyCopyrightAttribute::.ctor(string) =  (
		01 00 12 43 6F 70 79 72 69 67 68 74 20 C2 A9 20   // ...Copyright .. 
		20 32 30 30 36 00 00                            ) //  2006..

  .custom instance void class [mscorlib]System.Reflection.AssemblyProductAttribute::.ctor(string) =  (
		01 00 13 43 6F 6E 73 6F 6C 65 41 70 70 6C 69 63   // ...ConsoleApplic
		61 74 69 6F 6E 31 00 00                         ) // ation1..

  .custom instance void class [mscorlib]System.Reflection.AssemblyCompanyAttribute::.ctor(string) =  (01 00 00 00 00 ) // .....

  .custom instance void class [mscorlib]System.Reflection.AssemblyConfigurationAttribute::.ctor(string) =  (01 00 00 00 00 ) // .....

  .custom instance void class [mscorlib]System.Reflection.AssemblyDescriptionAttribute::.ctor(string) =  (01 00 00 00 00 ) // .....

  .custom instance void class [mscorlib]System.Reflection.AssemblyTitleAttribute::.ctor(string) =  (
		01 00 13 43 6F 6E 73 6F 6C 65 41 70 70 6C 69 63   // ...ConsoleApplic
		61 74 69 6F 6E 31 00 00                         ) // ation1..

  .custom instance void class [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) =  (01 00 02 00 00 00 00 00 ) // ........

  .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) =  (01 00 08 00 00 00 00 00 ) // ........

  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  1:0:0:0
}
.module ConsoleApplication1.exe // GUID = {25CBD5B1-A241-43CC-8D15-0925BECF7395}


.namespace ConsoleApplication1
{
  .class private auto ansi beforefieldinit Parent`2<T,U>
  	extends [mscorlib]System.Object
  {
    .field  family  valuetype ConsoleApplication1.Parent`2/TestEnum<!0,!1> te

    // method line 1
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x2050
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::.ctor()
	IL_0006:  ret 
    } // end of method Parent`2::.ctor

  .class nested family auto ansi sealed TestEnum<T,U>
  	extends [mscorlib]System.Enum
  {
    .field  public specialname  rtspecialname  int32 value__
    .field public static  literal  valuetype ConsoleApplication1.Parent`2/TestEnum<!0,!1> one = int32(0x00000000)
    .field public static  literal  valuetype ConsoleApplication1.Parent`2/TestEnum<!0,!1> two = int32(0x00000001)
    .field public static  literal  valuetype ConsoleApplication1.Parent`2/TestEnum<!0,!1> three = int32(0x00000002)

  } // end of class TestEnum

  } // end of class ConsoleApplication1.Parent`2
}

.namespace ConsoleApplication1
{
  .class private auto ansi beforefieldinit Child
  	extends class ConsoleApplication1.Parent`2<int32,int32>
  {

    // method line 2
    .method public hidebysig 
           instance default void DoIt ()  cil managed 
    {
        // Method begins at RVA 0x2058
	// Code size 29 (0x1d)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.0 
	IL_0002:  stfld valuetype ConsoleApplication1.Parent`2/TestEnum<!0,!1> class ConsoleApplication1.Parent`2<int32,int32>::te
	IL_0007:  ldarg.0 
	IL_0008:  ldfld valuetype ConsoleApplication1.Parent`2/TestEnum<!0,!1> class ConsoleApplication1.Parent`2<int32,int32>::te
	IL_000d:  box valuetype ConsoleApplication1.Parent`2/TestEnum<int32,int32>
	IL_0012:  callvirt instance string object::ToString()
	IL_0017:  call void class [mscorlib]System.Console::WriteLine(string)
	IL_001c:  ret 
    } // end of method Child::DoIt

    // method line 3
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x2076
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class ConsoleApplication1.Parent`2<int32, int32>::.ctor()
	IL_0006:  ret 
    } // end of method Child::.ctor

  } // end of class ConsoleApplication1.Child
}

.namespace ConsoleApplication1
{
  .class private auto ansi beforefieldinit Program
  	extends [mscorlib]System.Object
  {

    // method line 4
    .method private static  hidebysig 
           default void Main (string[] args)  cil managed 
    {
        // Method begins at RVA 0x2080
	.entrypoint
	// Code size 13 (0xd)
	.maxstack 1
	.locals init (
		class ConsoleApplication1.Child	V_0)
	IL_0000:  newobj instance void class ConsoleApplication1.Child::.ctor()
	IL_0005:  stloc.0 
	IL_0006:  ldloc.0 
	IL_0007:  callvirt instance void class ConsoleApplication1.Child::DoIt()
	IL_000c:  ret 
    } // end of method Program::Main

    // method line 5
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x2099
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::.ctor()
	IL_0006:  ret 
    } // end of method Program::.ctor

  } // end of class ConsoleApplication1.Program
}

