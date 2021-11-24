// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b'// as "b"
{


  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
.module 'b.exe'
// MVID: {0573B9C2-3980-4A83-9B01-889CCC1D6FBC}
.namespace Test
{
  .class private auto ansi App extends [mscorlib]System.Object
  {
    .method private hidebysig static int32 Main() il managed
    {
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.maxstack  6
		ldc.i4.0
		switch ()
		ldc.i4 100
		ret
	} // end of method 'App::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
		// Code size       7 (0x7)
		.maxstack  8
		IL_0000:  ldarg.0
		IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
		IL_0006:  ret
    } // end of method 'App::.ctor'

  } // end of class 'App'

} // end of namespace 'Test'

//*********** DISASSEMBLY COMPLETE ***********************
