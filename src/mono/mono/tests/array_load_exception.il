.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly extern TestDriver
{
  .ver 0:0:0:0
}

.assembly 'test'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module test.exe // GUID = {36476DF0-2B03-45C8-B512-E12B67C7C094}


  .class interface private auto ansi abstract IFoo
  {

    // method line 1
    .method public virtual  hidebysig  newslot  abstract 
           instance default int32 A ()  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method IFoo::A

  } // end of class IFoo


  .class public auto ansi sealed Dele
  	extends [mscorlib]System.MulticastDelegate implements IFoo
  {

    // method line 5
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Dele::.ctor

    // method line 6
    .method public virtual  hidebysig  newslot 
           instance default void Invoke ()  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Dele::Invoke

    // method line 7
    .method public virtual  hidebysig  newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Dele::BeginInvoke

    // method line 8
    .method public virtual  hidebysig  newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Dele::EndInvoke

  } // end of class Dele


  .class private auto ansi beforefieldinit Foo
  	extends [mscorlib]System.Object
  	implements IFoo  {

    // method line 2
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Foo::.ctor


    // method line 4
    .method public hidebysig 
           instance default int32 SSS ()  cil managed 
    {
        // Method begins at RVA 0x20f8
	// Code size 3 (0x3)
	.maxstack 8
	IL_0000:  ldc.i4.s 0x14
	IL_0002:  ret 
    } // end of method Foo::SSS

  } // end of class Foo

  .class public sequential ansi sealed beforefieldinit FooStruct extends [mscorlib]System.ValueType implements IFoo  {
	.field public int32 blarg 

  } // end of class FooStruct

  .class public auto ansi beforefieldinit Tests
  	extends [mscorlib]System.Object
  {
    .method private static  hidebysig 
           default void array_ldelema (class Foo& f)  cil managed 
    {
		.maxstack 8
		ldarg.0 
		ldind.ref 
		callvirt instance int32 class Foo::SSS()
		pop 
		ret 
    }

    .method private static  hidebysig 
           default void array_ldelema (class Foo[] arr)  cil managed 
    {
		.maxstack 8
		ldarg.0 
		ldc.i4.0 
		ldelema Foo
		call void class Tests::array_ldelema([out] class Foo&)
		ret 
    }

    .method public static hidebysig default int32 test_0_ldelema_type_check ()  cil managed 
    {
		.maxstack 2
		.try { 
		  	ldnull 
		  	call void class Tests::array_ldelema(class Foo[])
		  	leave BAD
		}
		catch class [mscorlib]System.TypeLoadException { // 0
		  	call void class [mscorlib]System.Console::WriteLine(object)
		  	leave GOOD
		}
	BAD:
	  	ldc.i4.1
		ret 
	GOOD:
		ldc.i4.0
		ret
    }

    .method public static  hidebysig default void delegate_ctor ()  cil managed 
	{
		.maxstack 4
		.locals init (object	V_0)
		ldnull 
		ldftn void class Tests::delegate_ctor()
		newobj instance void class Dele::'.ctor'(object, native int)
		stloc.0
		ret 
    }


    // method line 3
    .method private static  hidebysig default int32 test_0_delegate_ctor ()  cil managed 
    {
		.maxstack 2
		.try { 
		  	ldnull 
		  	call void class Tests::delegate_ctor()
		  	leave BAD
		}
		catch class [mscorlib]System.TypeLoadException { // 0
		  	call void class [mscorlib]System.Console::WriteLine(object)
		  	leave GOOD
		}
	BAD:
	  	ldc.i4.1
		ret 
	GOOD:
		ldc.i4.0
		ret
    }

    .method private static  hidebysig default void generic_box<(class IFoo) T> ()  cil managed 
    {
		.maxstack 3
		.locals init (!!T	V_0, !!T	V_1)
		ldloca.s 1
		initobj !!0
		ldloc.1 
		stloc.0 
		ldloca.s 0
		constrained. !!0
		callvirt instance int32 class IFoo::A()
		pop 
		ret 
    }

    .method private static  hidebysig default int32 test_0_generic_box ()  cil managed 
    {
		.maxstack 2
		.try { 
		  	ldnull 
		  	call void class Tests::generic_box<valuetype FooStruct> ()
		  	leave BAD
		}
		catch class [mscorlib]System.TypeLoadException { // 0
		  	call void class [mscorlib]System.Console::WriteLine(object)
		  	leave GOOD
		}
	BAD:
	  	ldc.i4.1
		ret 
	GOOD:
		ldc.i4.0
		ret
    }

    .method private static  hidebysig 
           default int32 Main ()  cil managed 
    {
		.entrypoint

		ldtoken Tests
		call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call int32 class [TestDriver]TestDriver::RunTests(class [mscorlib]System.Type)
		ret 

	}
}

