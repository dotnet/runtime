// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'bug'
{


  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.module 'bug.exe'
// MVID: {90803FD0-8E18-44C9-A242-98463ADD450D}
.namespace JitTest
{
  .class private value sealed Test extends [mscorlib]System.ValueType
  {
    .method private hidebysig instance int32 DoStuff(method instance int32 *()) il managed
    {
			// Code size       1 (0x1)
			.maxstack  8
			ldarg.0
			ldarg.1
			tail. calli		instance int32()
			ret
    } // end of method 'Test::DoStuff'
    .method private hidebysig instance method instance int32 *() RetStuff() il managed
    {
			// Code size       1 (0x1)
			.maxstack  8
			ldftn 		instance int32 JitTest.Test::DummyMethod()
			ret
    } // end of method 'Test::DoStuff'
    .method private hidebysig instance int32 DummyMethod() il managed
    {
			// Code size       1 (0x1)
			.maxstack  8
			ldarg.0
			ldobj		JitTest.Test
			box		JitTest.Test
			callvirt instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
			callvirt instance class [mscorlib]System.String [mscorlib]System.Type::ToString()
			call  void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
			ldc.i4.0
			ret
    } // end of method 'Test::DoStuff'

    .method private hidebysig static int32
            TestMain() il managed
    {
			.maxstack  10
			.locals (int32 V_0, value class JitTest.Test v)
			ldloca v
			initobj		value class JitTest.Test

			ldloca v
			ldftn 		instance int32 JitTest.Test::DummyMethod()
			ldftn		instance int32 JitTest.Test::DoStuff(method instance int32 *())
			calli		instance int32 (method instance int32 *())
			pop

			ldloca v
			ldloca v
			ldftn		instance method instance int32 *() JitTest.Test::RetStuff()
			calli		instance method instance int32 *()()
			tail. calli		instance int32 ()

			ret
    } // end of method 'Test::TestMain'

    .method private hidebysig static int32
            Main() il managed
    {
			.entrypoint
			.maxstack  4
			ldftn 		int32 JitTest.Test::TestMain()
			calli		int32 ()

			ldc.i4		0x64
			add

			ret
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      	ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'JitTest'
//*********** DISASSEMBLY COMPLETE ***********************
