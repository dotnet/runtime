.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'null_dele'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module null_dele.exe // GUID = {2CEA1DD9-6D58-496F-B44B-D78A7BB13197}


  .class public auto ansi sealed Foo
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 1
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object A_1, native int A_2)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Foo::.ctor

    // method line 2
    .method public virtual hidebysig newslot 
           instance default void Invoke ()  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Foo::Invoke

    // method line 3
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Foo::BeginInvoke

    // method line 4
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult A_1)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Foo::EndInvoke

  } // end of class Foo


  .class public auto ansi sealed Foo2
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 1
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object A_1, native int A_2)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Foo::.ctor

    // method line 2
    .method public virtual hidebysig newslot 
           instance default void Invoke (Program P_0)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Foo::Invoke

    // method line 3
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Foo::BeginInvoke

    // method line 4
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult A_1)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Foo::EndInvoke

  } // end of class Foo


  .class private auto ansi beforefieldinit Program
  	extends [mscorlib]System.Object
  {

    // method line 5
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Program::.ctor

    // method line 6
    .method public hidebysig 
           instance default void Test ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 1 (0x1)
	.maxstack 8
	IL_0000:  ret 
    } // end of method Program::Test

    // method line 7
    .method private static hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x20f8
		.entrypoint
		// Code size 109 (0x6d)
		.maxstack 3
		.locals init (class Program	V_0)

		ldnull 
		stloc.0

		.try {
			ldloc.0 
			ldftn instance void class Program::Test()
			newobj instance void class Foo::'.ctor'(object, native int)
			leave ERR_1
		}
		catch class [mscorlib]System.ArgumentException { // 2
			pop 
			leave NEXT_1
		}

NEXT_1:
		newobj instance void class Program::'.ctor'()
		stloc.0 
		.try {
			ldloc.0 
			ldftn instance void class Program::Test()
			newobj instance void class Foo::'.ctor'(object, native int)
			leave NEXT_2
		}
		catch class [mscorlib]System.ArgumentException { // 2
			pop 
			leave ERR_2
		}

NEXT_2:

		.try {
			ldnull
			ldftn instance void class Program::Test()
			newobj instance void class Foo2::'.ctor'(object, native int)
			leave NEXT_3
		}
		catch class [mscorlib]System.ArgumentException { // 2
			pop 
			leave ERR_3
		}

NEXT_3:
		.try {
			ldnull
			ldftn instance void class Program::Test()
			newobj instance void class Foo::'.ctor'(object, native int)
			leave ERR_4
		}
		catch class [mscorlib]System.ArgumentException { // 2
			pop 
			leave OK
		}

OK:
		ldc.i4.0
		ret
ERR_1:
		ldc.i4.1
		ret
ERR_2:
		ldc.i4.2
		ret
ERR_3:
		ldc.i4.3
		ret
ERR_4:
		ldc.i4.4
		ret

	}
	


}

