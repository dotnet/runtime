.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'NestedTypes_04'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::'.ctor'(int32) =  (01 00 08 00 00 00 00 00 ) // ........

  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .custom instance void class [mscorlib]System.Diagnostics.DebuggableAttribute::'.ctor'(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) =  (01 00 07 01 00 00 00 00 ) // ........

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module NestedTypes_04.exe // GUID = {E85D011D-AA7F-4680-92B5-99EEA4960461}


  .class interface public auto ansi abstract I1
  {

  .class interface nested family auto ansi abstract T1
  {

    // method line 3
    .method public virtual hidebysig newslot abstract 
           instance default void M1 ()  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method T1::M1

  } // end of class T1

  .class nested family auto ansi beforefieldinit T2
  	extends [mscorlib]System.Object
  {

    // method line 4
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2096
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  nop 
	IL_0007:  ret 
    } // end of method T2::.ctor

  } // end of class T2

  .class nested family sequential ansi sealed beforefieldinit T3
  	extends [mscorlib]System.ValueType
  {
    .pack 0
    .size 1

  } // end of class T3

  .class nested family auto ansi sealed T4
  	extends [mscorlib]System.Enum
  {
    .field  public specialname rtspecialname  int32 value__
    .field public static literal  valuetype I1/T4 B = int32(0x00000000)

  } // end of class T4

  } // end of class I1

  .class private auto ansi beforefieldinit Test1
  	extends [mscorlib]System.Object
  	implements I1  {

    // method line 1
    .method private static hidebysig 
           default void Main ()  cil managed 
    {
        // Method begins at RVA 0x2050
	.entrypoint
	// Code size 58 (0x3a)
	.maxstack 1
	.locals init (
		class I1/T1	V_0,
		valuetype I1/T3	V_1)
	IL_0000:  nop 
	IL_0001:  newobj instance void class Test1/Test2::'.ctor'()
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  callvirt instance void class I1/T1::M1()
	IL_000d:  nop 
	IL_000e:  newobj instance void class I1/T2::'.ctor'()
	IL_0013:  call void class [mscorlib]System.Console::WriteLine(object)
	IL_0018:  nop 
	IL_0019:  ldloca.s 1
	IL_001b:  initobj I1/T3
	IL_0021:  ldloc.1 
	IL_0022:  box I1/T3
	IL_0027:  call void class [mscorlib]System.Console::WriteLine(object)
	IL_002c:  nop 
	IL_002d:  ldc.i4.0 
	IL_002e:  box I1/T4
	IL_0033:  call void class [mscorlib]System.Console::WriteLine(object)
	IL_0038:  nop 
	IL_0039:  ret 
    } // end of method Test1::Main

    // method line 2
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2096
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  nop 
	IL_0007:  ret 
    } // end of method Test1::.ctor

  .class nested private auto ansi beforefieldinit Test2
  	extends [mscorlib]System.Object
  	implements I1/T1  {

    // method line 5
    .method public final virtual hidebysig newslot 
           instance default void M1 ()  cil managed 
    {
        // Method begins at RVA 0x209f
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  nop 
	IL_0001:  ldstr "M1"
	IL_0006:  call void class [mscorlib]System.Console::WriteLine(string)
	IL_000b:  nop 
	IL_000c:  ret 
    } // end of method Test2::M1

    // method line 6
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2096
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  nop 
	IL_0007:  ret 
    } // end of method Test2::.ctor

  } // end of class Test2

  } // end of class Test1

