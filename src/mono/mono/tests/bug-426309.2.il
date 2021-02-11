.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'csBug'
{
  .custom instance void class [mscorlib]System.Reflection.AssemblyTitleAttribute::'.ctor'(string) =  (01 00 05 63 73 42 75 67 00 00 ) // ...csBug..

  .custom instance void class [mscorlib]System.Reflection.AssemblyDescriptionAttribute::'.ctor'(string) =  (01 00 00 00 00 ) // .....

  .custom instance void class [mscorlib]System.Reflection.AssemblyConfigurationAttribute::'.ctor'(string) =  (01 00 00 00 00 ) // .....

  .custom instance void class [mscorlib]System.Reflection.AssemblyCompanyAttribute::'.ctor'(string) =  (01 00 00 00 00 ) // .....

  .custom instance void class [mscorlib]System.Reflection.AssemblyProductAttribute::'.ctor'(string) =  (01 00 05 63 73 42 75 67 00 00 ) // ...csBug..

  .custom instance void class [mscorlib]System.Reflection.AssemblyCopyrightAttribute::'.ctor'(string) =  (
		01 00 12 43 6F 70 79 72 69 67 68 74 20 C2 A9 20   // ...Copyright .. 
		20 32 30 30 38 00 00                            ) //  2008..

  .custom instance void class [mscorlib]System.Reflection.AssemblyTrademarkAttribute::'.ctor'(string) =  (01 00 00 00 00 ) // .....

  .custom instance void class [mscorlib]System.Runtime.InteropServices.ComVisibleAttribute::'.ctor'(bool) =  (01 00 00 00 00 ) // .....

  .custom instance void class [mscorlib]System.Runtime.InteropServices.GuidAttribute::'.ctor'(string) =  (
		01 00 24 37 30 33 38 34 32 37 34 2D 62 31 62 32   // ..$70384274-b1b2
		2D 34 38 62 33 2D 61 34 61 35 2D 39 33 35 65 63   // -48b3-a4a5-935ec
		30 39 37 33 32 37 32 00 00                      ) // 0973272..

  .custom instance void class [mscorlib]System.Reflection.AssemblyFileVersionAttribute::'.ctor'(string) =  (01 00 07 31 2E 30 2E 30 2E 30 00 00 ) // ...1.0.0.0..

  .custom instance void class [mscorlib]System.Diagnostics.DebuggableAttribute::'.ctor'(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) =  (01 00 07 01 00 00 00 00 ) // ........

  .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::'.ctor'(int32) =  (01 00 08 00 00 00 00 00 ) // ........

  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  1:0:0:0
}
.module csBug.exe // GUID = {503A131C-031D-480A-9BEF-B6280AC26811}


.namespace csBug
{
  .class interface public auto ansi abstract IDataCollection
  {

    // method line 1
    .method public virtual  hidebysig  newslot  abstract 
           instance default void Dummy ()  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method IDataCollection::Dummy

  } // end of class csBug.IDataCollection
}

.namespace csBug
{
  .class public auto ansi beforefieldinit CDataCollection`1<T>
  	extends [mscorlib]System.Object
  	implements csBug.IDataCollection  {

    // method line 2
    .method public final  virtual  hidebysig  newslot 
           instance default void Dummy ()  cil managed 
    {
        // Method begins at RVA 0x2050
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  nop 
	IL_0001:  ret 
    } // end of method CDataCollection`1::Dummy

    // method line 3
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2053
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method CDataCollection`1::.ctor

  } // end of class csBug.CDataCollection`1
}

.namespace csBug
{
  .class interface public auto ansi abstract IDataSelector
  {

    // method line 4
    .method public virtual  hidebysig  newslot  abstract 
           instance default class csBug.IDataCollection Execute<T> ()  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method IDataSelector::Execute

  } // end of class csBug.IDataSelector
}

.namespace csBug
{
  .class public auto ansi beforefieldinit CDataSelector
  	extends [mscorlib]System.Object
  	implements csBug.IDataSelector  {

    // method line 5
    .method public final  virtual  hidebysig  newslot 
           instance default class csBug.IDataCollection Execute<T> ()  cil managed 
    {
        // Method begins at RVA 0x205c
	// Code size 11 (0xb)
	.maxstack 1
	.locals init (
		class csBug.IDataCollection	V_0)
	IL_0000:  nop 
	IL_0001:  newobj instance void class csBug.CDataCollection`1<!!T>::'.ctor'()
	IL_0006:  stloc.0 
	IL_0007:  br.s IL_0009

	IL_0009:  ldloc.0 
	IL_000a:  ret 
    } // end of method CDataSelector::Execute

    // method line 6
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2073
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method CDataSelector::.ctor

  } // end of class csBug.CDataSelector
}

.namespace csBug
{
  .class public auto ansi beforefieldinit CSelect
  	extends [mscorlib]System.Object
  {

    // method line 7
    .method public static  hidebysig 
           default class csBug.IDataSelector CreateSelector ()  cil managed 
    {
        // Method begins at RVA 0x207c
	// Code size 11 (0xb)
	.maxstack 1
	.locals init (
		class csBug.IDataSelector	V_0)
	IL_0000:  nop 
	IL_0001:  newobj instance void class csBug.CDataSelector::'.ctor'()
	IL_0006:  stloc.0 
	IL_0007:  br.s IL_0009

	IL_0009:  ldloc.0 
	IL_000a:  ret 
    } // end of method CSelect::CreateSelector

    // method line 8
    .method public static  hidebysig 
           default class csBug.IDataCollection SelectAll<T> ()  cil managed 
    {
        // Method begins at RVA 0x2094
	// Code size 16 (0x10)
	.maxstack 1
	.locals init (
		class csBug.IDataCollection	V_0)
	IL_0000:  nop 
	IL_0001:  call class csBug.IDataSelector class csBug.CSelect::CreateSelector()
	IL_0006:  callvirt instance class csBug.IDataCollection class csBug.IDataSelector::Execute<!!0> ()
	IL_000b:  stloc.0 
	IL_000c:  br.s IL_000e

	IL_000e:  ldloc.0 
	IL_000f:  ret 
    } // end of method CSelect::SelectAll

    // method line 9
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20b0
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method CSelect::.ctor

  } // end of class csBug.CSelect
}

.namespace csBug
{
  .class private auto ansi beforefieldinit Program
  	extends [mscorlib]System.Object
  {

    // method line 10
    .method private static  hidebysig 
           default void Main (string[] args)  cil managed 
    {
        // Method begins at RVA 0x20b8
	.entrypoint
	// Code size 15 (0xf)
	.maxstack 1
	.locals init (
		class csBug.IDataCollection	V_0)
	IL_0000:  nop 
	IL_0001:  call class csBug.IDataCollection class csBug.CSelect::SelectAll<object> ()
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  callvirt instance void class csBug.IDataCollection::Dummy()
	IL_000d:  nop 
	IL_000e:  ret 
    } // end of method Program::Main

    // method line 11
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20d3
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Program::.ctor

  } // end of class csBug.Program
}

