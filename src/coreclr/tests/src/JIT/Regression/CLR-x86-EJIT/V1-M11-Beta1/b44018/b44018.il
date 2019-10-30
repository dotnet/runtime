// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'c'// as "c"
{ }
.module 'c.exe'
.namespace JitTest
{
  .class private auto ansi Test
         extends [mscorlib]System.Object
  {
		.method private hidebysig static int32 Main() il managed
		{
			.entrypoint
			.maxstack  30
			.locals (int32 V_0)
			ldc.i4.0
			stloc.0
			br begin
		begin:
			ldloc.0
			brtrue do2
			ldc.i4 100
			ret
		unreachable:
			ldc.i8 0
			br do1
		do1:
			pop
			ldloc.0
			brfalse do2
			br begin
		do2:
			ldc.i8 1
			br do1
		}

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'
  } // end of class 'Test'
} // end of namespace 'JitTest'
