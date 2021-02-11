//LDTOKEN test for byref types
//it should print "System.Int32&" and return 0 

.assembly extern mscorlib
{
  .ver 1:0:5000:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'token'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module token.exe // GUID = {8DBC8A7B-2036-4640-B76C-DCEA4F743D35}


  .class public auto ansi beforefieldinit TestClass
  	extends [mscorlib]System.Object
  {

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
    } // end of method TestClass::.ctor

    // method line 2
    .method public static  hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	.entrypoint
	// Code size 36 (0x24)
	.maxstack 6
	.locals init (
		class [mscorlib]System.Type	V_0)
	IL_0000:  ldtoken [mscorlib]System.Int32&
	IL_0005:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_000a:  stloc.0 
	IL_000b:  ldloc.0 
	IL_000c:  call void class [mscorlib]System.Console::WriteLine(object)
	IL_0011:  ldloc.0 
	IL_0012:  callvirt instance bool class [mscorlib]System.Type::get_IsByRef()
	IL_0017:  brfalse IL_0022

	IL_001c:  ldc.i4.0 
	IL_001d:  br IL_0023

	IL_0022:  ldc.i4.1 
	IL_0023:  ret 
    } // end of method TestClass::Main

  } // end of class TestClass

