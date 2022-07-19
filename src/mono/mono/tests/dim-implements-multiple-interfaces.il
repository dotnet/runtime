.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'dim-implements-multiple-interfaces'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::'.ctor'(int32) =  (01 00 08 00 00 00 00 00 ) // ........

  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .custom instance void class [mscorlib]System.Diagnostics.DebuggableAttribute::'.ctor'(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) =  (01 00 07 01 00 00 00 00 ) // ........

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'dim-implements-multiple-interfaces.exe' // GUID = {07D2CB79-FB07-4F86-A98B-DDB6DB7760DE}


.namespace Application
{
  .class interface public auto ansi abstract I1
  {

    // method line 1
    .method public virtual hidebysig newslot 
           instance default void M1 ()  cil managed 
    {
        // Method begins at RVA 0x2050
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  nop 
	IL_0001:  ldstr "Shoud not be called"
	IL_0006:  newobj instance void class [mscorlib]System.Exception::'.ctor'(string)
	IL_000b:  throw 
    } // end of method I1::M1

  } // end of class Application.I1
}

.namespace Application
{
  .class interface public auto ansi abstract I2
  	implements Application.I1  {

    // method line 2
    .method public final virtual hidebysig 
           instance default void Application.I1.M1 ()  cil managed 
    {
        // Method begins at RVA 0x2050
	.override class Application.I1::M1
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  nop 
	IL_0001:  ldstr "Shoud not be called"
	IL_0006:  newobj instance void class [mscorlib]System.Exception::'.ctor'(string)
	IL_000b:  throw 
    } // end of method I2::Application.I1.M1

  } // end of class Application.I2
}

.namespace Application
{
  .class interface public auto ansi abstract I3
  	implements Application.I1  {

    // method line 3
    .method public final virtual hidebysig 
           instance default void Application.I1.M1 ()  cil managed 
    {
        // Method begins at RVA 0x2050
	.override class Application.I1::M1
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  nop 
	IL_0001:  ldstr "Shoud not be called"
	IL_0006:  newobj instance void class [mscorlib]System.Exception::'.ctor'(string)
	IL_000b:  throw 
    } // end of method I3::Application.I1.M1

  } // end of class Application.I3
}

.namespace Application
{
  .class interface public auto ansi abstract I4
  	implements Application.I1  {

    // method line 4
    .method public final virtual hidebysig 
           instance default void Application.I1.M1 ()  cil managed 
    {
        // Method begins at RVA 0x2050
	.override class Application.I1::M1
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  nop 
	IL_0001:  ldstr "Shoud not be called"
	IL_0006:  newobj instance void class [mscorlib]System.Exception::'.ctor'(string)
	IL_000b:  throw 
    } // end of method I4::Application.I1.M1

  } // end of class Application.I4
}

.namespace Application
{
  .class private auto ansi beforefieldinit Test10
  	extends [mscorlib]System.Object
  	implements Application.I1, Application.I2, Application.I3, Application.I4  {

    // method line 5
    .method public final virtual hidebysig newslot 
           instance default void M1 ()  cil managed 
    {
        // Method begins at RVA 0x205d
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  nop 
	IL_0001:  ldstr "Test10.M1"
	IL_0006:  call void class [mscorlib]System.Console::WriteLine(string)
	IL_000b:  nop 
	IL_000c:  ret 
    } // end of method Test10::M1

    // method line 6
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x206b
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  nop 
	IL_0007:  ret 
    } // end of method Test10::.ctor

  } // end of class Application.Test10
}

.namespace Application
{
  .class private auto ansi beforefieldinit MainClass
  	extends [mscorlib]System.Object
  {

    // method line 7
    .method public static hidebysig 
           default void Main (string[] args)  cil managed 
    {
        // Method begins at RVA 0x2074
	.entrypoint
	// Code size 15 (0xf)
	.maxstack 1
	.locals init (
		class Application.I1	V_0)
	IL_0000:  nop 
	IL_0001:  newobj instance void class Application.Test10::'.ctor'()
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  callvirt instance void class Application.I1::M1()
	IL_000d:  nop 
	IL_000e:  ret 
    } // end of method MainClass::Main

    // method line 8
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x206b
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  nop 
	IL_0007:  ret 
    } // end of method MainClass::.ctor

  } // end of class Application.MainClass
}

