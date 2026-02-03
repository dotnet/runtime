// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib{}
.assembly 'b12425'{}
.assembly extern xunit.core {}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary { .ver 0:0:0:0 }

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
    .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {
        string('https://github.com/dotnet/runtime/issues/89157')
        type([TestLibrary]TestLibrary.Utilities)
        string[1] ('IsNativeAot')
    }

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
