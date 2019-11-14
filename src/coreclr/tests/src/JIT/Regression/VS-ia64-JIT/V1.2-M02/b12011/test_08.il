// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib{}
.assembly attrib014_2{}
.module attrib014_2.exe

.class private auto ansi import beforefieldinit ShellApplication
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24  00 00 )
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() runtime managed internalcall {}

}

.class public auto ansi beforefieldinit MainClass extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .entrypoint

    .try
    {
        newobj     instance void ShellApplication::.ctor()
	pop
        leave.s    IL_0030

    }
    catch [mscorlib]System.Exception 
    {
	pop
        leave.s    IL_0030
    }

    IL_0030:  ldc.i4 100	ret
  }
}