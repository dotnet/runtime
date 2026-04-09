.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'bug-0'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'bug-0.exe' // GUID = {5680CF40-DE38-48DF-A437-2A55C9E7DA2E}


  .class interface private auto ansi abstract IBar`1<T>
  {

    // method line 1
    .method public virtual  hidebysig  newslot  abstract  specialname 
           instance default void add_Handler (class Handler`1<!T> 'value')  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method IBar`1::add_Handler

    // method line 2
    .method public virtual  hidebysig  newslot  abstract  specialname 
           instance default void remove_Handler (class Handler`1<!T> 'value')  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method IBar`1::remove_Handler

	.event class Handler`1<!0> Handler
	{
		.addon instance default void IBar`1::add_Handler (class Handler`1<!0> 'value') 
		.removeon instance default void IBar`1::remove_Handler (class Handler`1<!0> 'value') 
	}
  } // end of class IBar`1

  .class private auto ansi beforefieldinit Foo`1<T>
  	extends [mscorlib]System.Object
  {
    .field  private  class IBar`1<!0> proxy
    .field  private  class IBar`1<!0> real
    .field  private  class Handler`1<!0> proxyHandler
    .field  private  class Handler`1<!0> 'handler'

    // method line 3
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Foo`1::.ctor

    // method line 4
    .method private hidebysig  specialname 
           instance default void add_handler (class Handler`1<!T> 'value')  cil managed synchronized 
    {
        // Method begins at RVA 0x20f4
	// Code size 24 (0x18)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.0 
	IL_0002:  ldfld class Handler`1<!0> class Foo`1<!0>::'handler'
	IL_0007:  ldarg.1 
	IL_0008:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	IL_000d:  castclass class Handler`1<!0>
	IL_0012:  stfld class Handler`1<!0> class Foo`1<!0>::'handler'
	IL_0017:  ret 
    } // end of method Foo`1::add_handler

    // method line 5
    .method private hidebysig  specialname 
           instance default void remove_handler (class Handler`1<!T> 'value')  cil managed synchronized 
    {
        // Method begins at RVA 0x2110
	// Code size 24 (0x18)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.0 
	IL_0002:  ldfld class Handler`1<!0> class Foo`1<!0>::'handler'
	IL_0007:  ldarg.1 
	IL_0008:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	IL_000d:  castclass class Handler`1<!0>
	IL_0012:  stfld class Handler`1<!0> class Foo`1<!0>::'handler'
	IL_0017:  ret 
    } // end of method Foo`1::remove_handler

    // method line 6
    .method public hidebysig  specialname 
           instance default void add_Handler (class Handler`1<!T> 'value')  cil managed 
    {
        // Method begins at RVA 0x212c
	// Code size 64 (0x40)
	.maxstack 4
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class Handler`1<!0> class Foo`1<!0>::'handler'
	IL_0006:  brtrue IL_0028

	IL_000b:  ldarg.0 
	IL_000c:  ldfld class Handler`1<!0> class Foo`1<!0>::proxyHandler
	IL_0011:  brtrue IL_0028

	IL_0016:  ldarg.0 
	IL_0017:  ldarg.0 
	IL_0018:  ldftn instance void class Foo`1<!T>::'<Foo`1>m__0'<!!0> (object)
	IL_001e:  newobj instance void class Handler`1<!T>::'.ctor'(object, native int)
	IL_0023:  stfld class Handler`1<!0> class Foo`1<!0>::proxyHandler
	IL_0028:  ldarg.0 
	IL_0029:  dup 
	IL_002a:  ldfld class Handler`1<!0> class Foo`1<!0>::'handler'
	IL_002f:  ldarg.1 
	IL_0030:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	IL_0035:  castclass class Handler`1<!0>
	IL_003a:  stfld class Handler`1<!0> class Foo`1<!0>::'handler'
	IL_003f:  ret 
    } // end of method Foo`1::add_Handler

    // method line 7
    .method public hidebysig  specialname 
           instance default void remove_Handler (class Handler`1<!T> 'value')  cil managed 
    {
        // Method begins at RVA 0x2178
	// Code size 24 (0x18)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  dup 
	IL_0002:  ldfld class Handler`1<!0> class Foo`1<!0>::'handler'
	IL_0007:  ldarg.1 
	IL_0008:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	IL_000d:  castclass class Handler`1<!0>
	IL_0012:  stfld class Handler`1<!0> class Foo`1<!0>::'handler'
	IL_0017:  ret 
    } // end of method Foo`1::remove_Handler

    // method line 8
    .method private hidebysig 
           instance default void '<Foo`1>m__0'<T> (object s)  cil managed 
    {
        .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::'.ctor'() =  (01 00 00 00 ) // ....

        // Method begins at RVA 0x2194
	// Code size 18 (0x12)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class Handler`1<!0> class Foo`1<!0>::'handler'
	IL_0006:  ldarg.0 
	IL_0007:  ldfld class IBar`1<!0> class Foo`1<!0>::proxy
	IL_000c:  callvirt instance void class Handler`1<!T>::Invoke(object)
	IL_0011:  ret 
    } // end of method Foo`1::<Foo`1>m__0

	.event class Handler`1<!0> 'handler'
	{
		.addon instance default void Foo`1::add_handler (class Handler`1<!0> 'value') 
		.removeon instance default void Foo`1::remove_handler (class Handler`1<!0> 'value') 
	}
	.event class Handler`1<!0> Handler
	{
		.addon instance default void Foo`1::add_Handler (class Handler`1<!0> 'value') 
		.removeon instance default void Foo`1::remove_Handler (class Handler`1<!0> 'value') 
	}
  } // end of class Foo`1

  .class private auto ansi beforefieldinit Program
  	extends [mscorlib]System.Object
  {

    // method line 9
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x21a8
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Program::.ctor

    // method line 10
    .method private static  hidebysig 
           default void Main ()  cil managed 
    {
        // Method begins at RVA 0x21b0
	.entrypoint
	// Code size 14 (0xe)
	.maxstack 3
	.locals init (
		class Foo`1<int32>	V_0)
	IL_0000:  newobj instance void class Foo`1<int32>::'.ctor'()
	IL_0005:  stloc.0 
	IL_0006:  ldloc.0 
	IL_0007:  ldnull 
	IL_0008:  callvirt instance void class Foo`1<int32>::add_Handler(class Handler`1<!0>)
	IL_000d:  ret 
    } // end of method Program::Main

  } // end of class Program

  .class private auto ansi sealed Handler`1<T>
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 11
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Handler`1::.ctor

    // method line 12
    .method public virtual  hidebysig  newslot 
           instance default void Invoke (object sender)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Handler`1::Invoke

    // method line 13
    .method public virtual  hidebysig  newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (object sender, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Handler`1::BeginInvoke

    // method line 14
    .method public virtual  hidebysig  newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method Handler`1::EndInvoke

  } // end of class Handler`1

