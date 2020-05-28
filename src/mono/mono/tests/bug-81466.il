.assembly extern 'bug-81466-lib'
{
  .ver 0:0:0:0
}
.assembly extern mscorlib
{
  .ver 1:0:5000:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'bug-81466'
{
  .custom instance void class [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool, bool) =  (01 00 01 01 00 00 ) // ......

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'bug-81466.exe'


.namespace ImplSpace
{
  .class private auto ansi beforefieldinit TestImpl
  	extends ['bug-81466-lib']MainSpace.CD
  {

    // method line 1
    .method public virtual  hidebysig 
           instance default bool Test ()  cil managed 
    {
        // Method begins at RVA 0x2050
	// Code size 21 (0x15)
	.maxstack 1
	.locals init (
		bool	V_0)
	IL_0000:  ldstr "TestImpl.Test()"
	IL_0005:  call void class [mscorlib]System.Console::WriteLine(string)
	IL_000a:  ldarg.0 
	IL_000b:  call instance bool class ['bug-81466-lib']MainSpace.CD::Test()
	IL_0010:  stloc.0 
	IL_0011:  br.s IL_0013

	IL_0013:  ldloc.0 
	IL_0014:  ret 
    } // end of method TestImpl::Test

    // method line 2
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x2074
	// Code size 7 (0x7)
	.maxstack 1
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class ['bug-81466-lib']MainSpace.CD::.ctor()
	IL_0006:  ret 
    } // end of method TestImpl::.ctor

  } // end of class ImplSpace.TestImpl
}

.namespace ImplSpace
{
  .class public auto ansi beforefieldinit TestClass
  	extends [mscorlib]System.Object
  {

    // method line 3
    .method public static  hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x2088
	.entrypoint
	// Code size 14 (0xe)
	.maxstack 1
	.locals init (
		class ImplSpace.TestImpl	V_0)
	IL_0000:  newobj instance void class ImplSpace.TestImpl::.ctor()
	IL_0005:  stloc.0 
	IL_0006:  ldloc.0 
	IL_0007:  callvirt instance bool class ['bug-81466-lib']MainSpace.AC::Test()
	IL_000c:  pop 
	IL_000d:  ldc.i4.0 
	IL_XXXX:  ret 
    } // end of method TestClass::Main

    // method line 4
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x20a4
	// Code size 7 (0x7)
	.maxstack 1
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::.ctor()
	IL_0006:  ret 
    } // end of method TestClass::.ctor

  } // end of class ImplSpace.TestClass
}

