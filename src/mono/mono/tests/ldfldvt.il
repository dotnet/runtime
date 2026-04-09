// C# source. Compiling this code with mcs didn't produce the necessary output
//
// public struct Struct1 {
// 	public int f1;
// 	public Struct2 f2;
// 	public int f3;
// }
// 
// public struct Struct2 {
// 	public int f1;
// 	public int f2;
// }
// 
// public class MyArray {
// 
// 	private Struct1[] array;
// 
// 	public MyArray ()
// 	{
// 		array = new Struct1 [2];
// 
// 		array [0].f1 = 1;
// 		array [0].f2.f1 = 2;
// 		array [0].f2.f2 = 3;
// 		array [0].f3 = 4;
// 
// 		array [1].f1 = 5;
// 		array [1].f2.f1 = 6;
// 		array [1].f2.f2 = 7;
// 		array [1].f3 = 8;
// 	}
// 
// 	public Struct1 this [int index]
// 	{
// 		get {
// 			return array [index];
// 		} set {
// 			array [index] = value;
// 		}
// 	}
// }
// 
// public class Program {
// 	public static int Check (Struct2 s1, Struct2 s2)
// 	{
// 		if (s1.f1 == 2 && s1.f2 == 3 && s2.f1 == 6 && s2.f2 == 7)
// 			return 0;
// 		return 1;
// 	}
// 
// 	public static int Check (MyArray array)
// 	{
// 		return Check (array [0].f2, array [1].f2);
// 	}
// 
// 	public static int Main (string[] args)
// 	{
// 		MyArray array = new MyArray ();
// 		return Check (array);
// 	}
// }

.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'VT'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::'.ctor'(int32) =  (01 00 08 00 00 00 00 00 ) // ........

  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .custom instance void class [mscorlib]System.Diagnostics.DebuggableAttribute::'.ctor'(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) =  (01 00 07 01 00 00 00 00 ) // ........

  .custom instance void class [mscorlib]System.Reflection.AssemblyTitleAttribute::'.ctor'(string) =  (01 00 02 56 54 00 00 ) // ...VT..

  .custom instance void class [mscorlib]System.Reflection.AssemblyDescriptionAttribute::'.ctor'(string) =  (01 00 00 00 00 ) // .....

  .custom instance void class [mscorlib]System.Reflection.AssemblyConfigurationAttribute::'.ctor'(string) =  (01 00 00 00 00 ) // .....

  .custom instance void class [mscorlib]System.Reflection.AssemblyCompanyAttribute::'.ctor'(string) =  (01 00 00 00 00 ) // .....

  .custom instance void class [mscorlib]System.Reflection.AssemblyProductAttribute::'.ctor'(string) =  (01 00 02 56 54 00 00 ) // ...VT..

  .custom instance void class [mscorlib]System.Reflection.AssemblyCopyrightAttribute::'.ctor'(string) =  (
		01 00 12 43 6F 70 79 72 69 67 68 74 20 C2 A9 20   // ...Copyright .. 
		20 32 30 31 38 00 00                            ) //  2018..

  .custom instance void class [mscorlib]System.Reflection.AssemblyTrademarkAttribute::'.ctor'(string) =  (01 00 00 00 00 ) // .....

  .custom instance void class [mscorlib]System.Runtime.InteropServices.ComVisibleAttribute::'.ctor'(bool) =  (01 00 00 00 00 ) // .....

  .custom instance void class [mscorlib]System.Runtime.InteropServices.GuidAttribute::'.ctor'(string) =  (
		01 00 24 33 65 32 39 35 61 63 31 2D 65 61 34 32   // ..$3e295ac1-ea42
		2D 34 64 30 38 2D 38 62 61 32 2D 65 37 66 62 61   // -4d08-8ba2-e7fba
		66 37 31 30 61 38 63 00 00                      ) // f710a8c..

  .custom instance void class [mscorlib]System.Reflection.AssemblyFileVersionAttribute::'.ctor'(string) =  (01 00 07 31 2E 30 2E 30 2E 30 00 00 ) // ...1.0.0.0..

  .custom instance void class [mscorlib]System.Runtime.Versioning.TargetFrameworkAttribute::'.ctor'(string) =  (
		01 00 1C 2E 4E 45 54 46 72 61 6D 65 77 6F 72 6B   // ....NETFramework
		2C 56 65 72 73 69 6F 6E 3D 76 34 2E 36 2E 31 01   // ,Version=v4.6.1.
		00 54 0E 14 46 72 61 6D 65 77 6F 72 6B 44 69 73   // .T..FrameworkDis
		70 6C 61 79 4E 61 6D 65 14 2E 4E 45 54 20 46 72   // playName..NET Fr
		61 6D 65 77 6F 72 6B 20 34 2E 36 2E 31          ) // amework 4.6.1

  .hash algorithm 0x00008004
  .ver  1:0:0:0
}
.module VT.exe // GUID = {EC2F1E5D-A3E0-493A-8462-E24A6A70FF38}


  .class public sequential ansi sealed beforefieldinit Struct1
  	extends [mscorlib]System.ValueType
  {
    .field  public  int32 f1
    .field  public  valuetype Struct2 f2
    .field  public  int32 f3

  } // end of class Struct1

  .class public sequential ansi sealed beforefieldinit Struct2
  	extends [mscorlib]System.ValueType
  {
    .field  public  int32 f1
    .field  public  int32 f2

  } // end of class Struct2

  .class public auto ansi beforefieldinit MyArray
  	extends [mscorlib]System.Object
  {
    .custom instance void class [mscorlib]System.Reflection.DefaultMemberAttribute::'.ctor'(string) =  (01 00 04 49 74 65 6D 00 00 ) // ...Item..

    .field  private  valuetype Struct1[] 'array'

    // method line 1
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2050
	// Code size 185 (0xb9)
	.maxstack 2
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  nop 
	IL_0007:  nop 
	IL_0008:  ldarg.0 
	IL_0009:  ldc.i4.2 
	IL_000a:  newarr Struct1
	IL_000f:  stfld valuetype Struct1[] MyArray::'array'
	IL_0014:  ldarg.0 
	IL_0015:  ldfld valuetype Struct1[] MyArray::'array'
	IL_001a:  ldc.i4.0 
	IL_001b:  ldelema Struct1
	IL_0020:  ldc.i4.1 
	IL_0021:  stfld int32 Struct1::f1
	IL_0026:  ldarg.0 
	IL_0027:  ldfld valuetype Struct1[] MyArray::'array'
	IL_002c:  ldc.i4.0 
	IL_002d:  ldelema Struct1
	IL_0032:  ldflda valuetype Struct2 Struct1::f2
	IL_0037:  ldc.i4.2 
	IL_0038:  stfld int32 Struct2::f1
	IL_003d:  ldarg.0 
	IL_003e:  ldfld valuetype Struct1[] MyArray::'array'
	IL_0043:  ldc.i4.0 
	IL_0044:  ldelema Struct1
	IL_0049:  ldflda valuetype Struct2 Struct1::f2
	IL_004e:  ldc.i4.3 
	IL_004f:  stfld int32 Struct2::f2
	IL_0054:  ldarg.0 
	IL_0055:  ldfld valuetype Struct1[] MyArray::'array'
	IL_005a:  ldc.i4.0 
	IL_005b:  ldelema Struct1
	IL_0060:  ldc.i4.4 
	IL_0061:  stfld int32 Struct1::f3
	IL_0066:  ldarg.0 
	IL_0067:  ldfld valuetype Struct1[] MyArray::'array'
	IL_006c:  ldc.i4.1 
	IL_006d:  ldelema Struct1
	IL_0072:  ldc.i4.5 
	IL_0073:  stfld int32 Struct1::f1
	IL_0078:  ldarg.0 
	IL_0079:  ldfld valuetype Struct1[] MyArray::'array'
	IL_007e:  ldc.i4.1 
	IL_007f:  ldelema Struct1
	IL_0084:  ldflda valuetype Struct2 Struct1::f2
	IL_0089:  ldc.i4.6 
	IL_008a:  stfld int32 Struct2::f1
	IL_008f:  ldarg.0 
	IL_0090:  ldfld valuetype Struct1[] MyArray::'array'
	IL_0095:  ldc.i4.1 
	IL_0096:  ldelema Struct1
	IL_009b:  ldflda valuetype Struct2 Struct1::f2
	IL_00a0:  ldc.i4.7 
	IL_00a1:  stfld int32 Struct2::f2
	IL_00a6:  ldarg.0 
	IL_00a7:  ldfld valuetype Struct1[] MyArray::'array'
	IL_00ac:  ldc.i4.1 
	IL_00ad:  ldelema Struct1
	IL_00b2:  ldc.i4.8 
	IL_00b3:  stfld int32 Struct1::f3
	IL_00b8:  ret 
    } // end of method MyArray::.ctor

    // method line 2
    .method public hidebysig specialname 
           instance default valuetype Struct1 get_Item (int32 index)  cil managed 
    {
        // Method begins at RVA 0x2118
	// Code size 18 (0x12)
	.maxstack 2
	.locals init (
		valuetype Struct1	V_0)
	IL_0000:  nop 
	IL_0001:  ldarg.0 
	IL_0002:  ldfld valuetype Struct1[] MyArray::'array'
	IL_0007:  ldarg.1 
	IL_0008:  ldelem Struct1
	IL_000d:  stloc.0 
	IL_000e:  br.s IL_0010

	IL_0010:  ldloc.0 
	IL_0011:  ret 
    } // end of method MyArray::get_Item

    // method line 3
    .method public hidebysig specialname 
           instance default void set_Item (int32 index, valuetype Struct1 'value')  cil managed 
    {
        // Method begins at RVA 0x2136
	// Code size 15 (0xf)
	.maxstack 8
	IL_0000:  nop 
	IL_0001:  ldarg.0 
	IL_0002:  ldfld valuetype Struct1[] MyArray::'array'
	IL_0007:  ldarg.1 
	IL_0008:  ldarg.2 
	IL_0009:  stelem Struct1
	IL_000e:  ret 
    } // end of method MyArray::set_Item

	.property instance valuetype Struct1 Item (int32)
	{
		.get instance default valuetype Struct1 MyArray::get_Item (int32 index) 
		.set instance default void MyArray::set_Item (int32 index, valuetype Struct1 'value') 
	}
  } // end of class MyArray

  .class public auto ansi beforefieldinit Program
  	extends [mscorlib]System.Object
  {

    // method line 4
    .method public static hidebysig 
           default int32 Check (valuetype Struct2 s1, valuetype Struct2 s2)  cil managed 
    {
        // Method begins at RVA 0x2148
	// Code size 54 (0x36)
	.maxstack 2
	.locals init (
		bool	V_0,
		int32	V_1)
	IL_0000:  nop 
	IL_0001:  ldarg.0 
	IL_0002:  ldfld int32 Struct2::f1
	IL_0007:  ldc.i4.2 
	IL_0008:  bne.un.s IL_0027

	IL_000a:  ldarg.0 
	IL_000b:  ldfld int32 Struct2::f2
	IL_0010:  ldc.i4.3 
	IL_0011:  bne.un.s IL_0027

	IL_0013:  ldarg.1 
	IL_0014:  ldfld int32 Struct2::f1
	IL_0019:  ldc.i4.6 
	IL_001a:  bne.un.s IL_0027

	IL_001c:  ldarg.1 
	IL_001d:  ldfld int32 Struct2::f2
	IL_0022:  ldc.i4.7 
	IL_0023:  ceq 
	IL_0025:  br.s IL_0028

	IL_0027:  ldc.i4.0 
	IL_0028:  stloc.0 
	IL_0029:  ldloc.0 
	IL_002a:  brfalse.s IL_0030

	IL_002c:  ldc.i4.0 
	IL_002d:  stloc.1 
	IL_002e:  br.s IL_0034

	IL_0030:  ldc.i4.1 
	IL_0031:  stloc.1 
	IL_0032:  br.s IL_0034

	IL_0034:  ldloc.1 
	IL_0035:  ret 
    } // end of method Program::Check

    // method line 5
    .method public static hidebysig 
           default int32 Check (class MyArray 'array')  cil managed 
    {
        // Method begins at RVA 0x218c
	// Code size 35 (0x23)
	.maxstack 3
	.locals init (
		int32	V_0)
	IL_0000:  nop 
	IL_0001:  ldarg.0 
	IL_0002:  ldc.i4.0 
	IL_0003:  callvirt instance valuetype Struct1 class MyArray::get_Item(int32)
	IL_0008:  ldfld valuetype Struct2 Struct1::f2
	IL_000d:  ldarg.0 
	IL_000e:  ldc.i4.1 
	IL_000f:  callvirt instance valuetype Struct1 class MyArray::get_Item(int32)
	IL_0014:  ldfld valuetype Struct2 Struct1::f2
	IL_0019:  call int32 class Program::Check(valuetype Struct2, valuetype Struct2)
	IL_001e:  stloc.0 
	IL_001f:  br.s IL_0021

	IL_0021:  ldloc.0 
	IL_0022:  ret 
    } // end of method Program::Check

    // method line 6
    .method public static hidebysig 
           default int32 Main (string[] args)  cil managed 
    {
        // Method begins at RVA 0x21bc
	.entrypoint
	// Code size 18 (0x12)
	.maxstack 1
	.locals init (
		class MyArray	V_0,
		int32	V_1)
	IL_0000:  nop 
	IL_0001:  newobj instance void class MyArray::'.ctor'()
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  call int32 class Program::Check(class MyArray)
	IL_000d:  stloc.1 
	IL_000e:  br.s IL_0010

	IL_0010:  ldloc.1 
	IL_0011:  ret 
    } // end of method Program::Main

    // method line 7
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x21da
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  nop 
	IL_0007:  ret 
    } // end of method Program::.ctor

  } // end of class Program

