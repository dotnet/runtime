.assembly extern mscorlib
{
  .ver 1:0:5000:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'event'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module event.exe // GUID = {7A082A08-017B-4BB9-BDD9-105E86FCC135}


  .class public auto ansi beforefieldinit T
  	extends [mscorlib]System.Object
  {
    .field  private static  class T/Do MyDo

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
    } // end of method T::.ctor

    // method line 2
    .method public static  hidebysig  specialname 
           default void add_MyDo (class T/Do 'value')  cil managed synchronized 
    {
        // Method begins at RVA 0x20f4
	// Code size 22 (0x16)
	.maxstack 8
	IL_0000:  ldsfld class T/Do T::MyDo
	IL_0005:  ldarg.0 
	IL_0006:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	IL_000b:  castclass T/Do
	IL_0010:  stsfld class T/Do T::MyDo
	IL_0015:  ret 
    } // end of method T::add_MyDo

    // method line 3
    .method public static  hidebysig  specialname 
           default void remove_MyDo (class T/Do 'value')  cil managed synchronized 
    {
        // Method begins at RVA 0x210c
	// Code size 22 (0x16)
	.maxstack 8
	IL_0000:  ldsfld class T/Do T::MyDo
	IL_0005:  ldarg.0 
	IL_0006:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	IL_000b:  castclass T/Do
	IL_0010:  stsfld class T/Do T::MyDo
	IL_0015:  ret 
    } // end of method T::remove_MyDo

    .method public static  hidebysig  specialname 
           default void other1_MyDo (class T/Do 'value')  cil managed synchronized 
    {
        // Method begins at RVA 0x210c
	// Code size 22 (0x16)
	.maxstack 8
	IL_0015:  ret 
    } // end of method T::other1_MyDo

    .method private static  hidebysig  specialname 
           default void other2_MyDo (class T/Do 'value')  cil managed synchronized 
    {
        // Method begins at RVA 0x210c
	// Code size 22 (0x16)
	.maxstack 8
	IL_0015:  ret 
    } // end of method T::other2_MyDo

    // method line 4
    .method public static  hidebysig 
           default void Main ()  cil managed 
    {
        // Method begins at RVA 0x2124
	.entrypoint
	// Code size 1 (0x1)
	.maxstack 8
	IL_0000:  ret 
    } // end of method T::Main

	.event T/Do MyDo
	{
		.addon default void T::add_MyDo (class T/Do 'value') 
		.removeon default void T::remove_MyDo (class T/Do 'value') 
		.other default void T::other1_MyDo (class T/Do 'value') 
		.other default void T::other2_MyDo (class T/Do 'value') 
	}
  .class nested public auto ansi sealed Do
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 5
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Do::.ctor

    // method line 6
    .method public virtual  hidebysig  newslot 
           instance default void Invoke ()  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Do::Invoke

    // method line 7
    .method public virtual  hidebysig  newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Do::BeginInvoke

    // method line 8
    .method public virtual  hidebysig  newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Do::EndInvoke

  } // end of class Do

  } // end of class T

