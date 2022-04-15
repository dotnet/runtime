// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'ftn_t'
{

  //  .custom instance void ['mscorlib']System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
// MVID: {90803FD0-8E18-44C9-A242-98463ADD450D}
.namespace JitTest_ftn_t_fptr_il
{
  .class public auto ansi Test
         extends ['mscorlib']System.Object
  {
    .method private hidebysig static void DoStuff(method void *()) il managed
    {
      // Code size       1 (0x1)
      	.maxstack  8
      	ldarg.0
      	tail. calli	void()
      	ret
    } // end of method 'Test::DoStuff'
    .method private hidebysig static method void *() RetStuff() il managed
    {
      // Code size       1 (0x1)
      .maxstack  8
      ldftn 		void JitTest_ftn_t_fptr_il.Test::DummyMethod()
      ret
    } // end of method 'Test::DoStuff'
    .method private hidebysig static void DummyMethod() il managed
    {
      // Code size       1 (0x1)
      .maxstack  8
      ldstr "Looks good."
      call  void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      ret
    } // end of method 'Test::DoStuff'

    .method private hidebysig static void TestMain() il managed
    {
		.maxstack  4
		.locals (int32 V_0)
		ldftn 		void JitTest_ftn_t_fptr_il.Test::DummyMethod()
		ldftn		void JitTest_ftn_t_fptr_il.Test::DoStuff(method void *())
		calli		void (method void *())
		ldftn		method void *() JitTest_ftn_t_fptr_il.Test::RetStuff()
		calli		method void *()()
		tail. calli void()
		ret
    } // end of method 'Test::TestMain'

    .method public hidebysig static int32 Main() il managed
    {
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.maxstack  4
		ldftn 		void JitTest_ftn_t_fptr_il.Test::TestMain()
		calli		void ()
		ldc.i4 0x64
		ret
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'JitTest_ftn_t_fptr_il'
//*********** DISASSEMBLY COMPLETE ***********************
