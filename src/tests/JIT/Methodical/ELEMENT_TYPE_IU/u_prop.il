// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'u_prop'// as "qsort"
{ }
.assembly extern xunit.core {}
.namespace Test_u_prop
{
  .class public auto ansi BB extends [mscorlib]System.Object
  {
    .method public hidebysig static int32 Main() il managed
    {
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.maxstack  8
		.locals (class Test_u_prop.AA)
		
		newobj instance void Test_u_prop.AA::.ctor()
		stloc.0
		ldloc.0
		ldc.i4.0
		call instance void Test_u_prop.AA::set_I(native unsigned int)
		ldloc.0
		call instance native unsigned int Test_u_prop.AA::get_I()
		ldstr "passed"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldc.i4 0x64
		add
		ret
    } // end of method 'AA::Main'

    .method public hidebysig specialname rtspecialname instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      ldarg.0
      call       instance void [mscorlib]System.Object::.ctor()
      ret
    } // end of method 'AA::.ctor'
  }
  .class private auto ansi AA extends [mscorlib]System.Object
  {
	.field private native unsigned int m_i
	
	.method public native unsigned int get_I() il managed
	{
		.maxstack  8
		ldarg.0
		ldfld native unsigned int Test_u_prop.AA::m_i
		ret
	}
	
	.method public void set_I(native unsigned int) il managed
	{
		.maxstack  8
		ldarg.0
		ldarg.1
		stfld native unsigned int Test_u_prop.AA::m_i
		ret
	}
	
    .method public hidebysig specialname rtspecialname instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      ldarg.0
      call       instance void [mscorlib]System.Object::.ctor()
      ldarg.0
      ldc.i4.0
      stfld		native unsigned int Test_u_prop.AA::m_i
      ret
    } // end of method 'AA::.ctor'

  } // end of class 'AA'

} // end of namespace 'Test_u_prop'

//*********** DISASSEMBLY COMPLETE ***********************
