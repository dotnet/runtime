// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly repro_32
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
}

.class private auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32  Main() cil managed
  {
		.entrypoint
  		.maxstack  2

		ldtoken    string&
		call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call	   bool Test::Bug(object)
                brtrue.s   Failed
                ldc.i4     100
                ldstr      "Test SUCCESS"
                br.s       Done
	Failed:
                ldc.i4     666
                ldstr      "Test FAIL"
	Done:
		call       void class [System.Console]System.Console::WriteLine(string)
		ret
  } // end of method Test::Main

  .method private hidebysig static bool Bug(object 'o') cil managed
  {
		.maxstack  2

		ldtoken    object&
		call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		ldarg.0
                callvirt   instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
		ceq	   
		ret
  } // end of method Test::Main
}
