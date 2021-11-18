// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib{}
.assembly test_17{}
.assembly extern xunit.core {}
.module test_17.exe

.class interface public abstract auto ansi ITrees
{
  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Collections.IEnumerator 
          GetEnumerator() cil managed{} 

}

.class public auto ansi beforefieldinit Garden
       extends [mscorlib]System.Object
       implements ITrees
{
  .method private hidebysig newslot virtual final 
          instance class [mscorlib]System.Collections.IEnumerator 
          ITrees.GetEnumerator() cil managed
  {
    .override ITrees::GetEnumerator
	ldnull
	ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
	ldarg.0
	call       instance void [mscorlib]System.Object::.ctor()
	ret
  }
}

.class public auto ansi beforefieldinit MainClass
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

	ldtoken    Garden
	call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	ldtoken    ITrees
	call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	callvirt   instance valuetype [mscorlib]System.Reflection.InterfaceMapping [mscorlib]System.Type::GetInterfaceMap(class [mscorlib]System.Type)
	pop
	ldc.i4 100
	ret
  }

}
