// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This test makes sure that ByRef types are properly unified by the reflection stack
// and that a type constructed by reflection is equivalent to a type constructed through
// the LDTOKEN instruction.
//

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
    .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
    .ver 4:0:0:0
}
.assembly byrefs
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
}

.class public auto ansi beforefieldinit Test_byrefs
       extends [mscorlib]System.Object
{
    .method public hidebysig static int32  Main() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
        .maxstack  2
	
        call       bool Test_byrefs::LdTokenEqualsMakeByRef()
        brfalse    Failed
        
        call       bool Test_byrefs::MakeByRefEqualsLdToken()
        brfalse    Failed
	
        ldc.i4     100
        ldstr      "ByRefs look good"
        br.s       Done
    Failed:
        ldc.i4     666
        ldstr      "ByRefs are broken"
    Done:
        call       void class [System.Console]System.Console::WriteLine(string)
        ret
    }
  
    .method private hidebysig static bool LdTokenEqualsMakeByRef() cil managed
    {
        .maxstack  2
  
        ldtoken    valuetype MyType1&
        call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	
        ldtoken    valuetype MyType1
        call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        callvirt   instance class [mscorlib]System.Type [mscorlib]System.Type::MakeByRefType()
	
        ceq
	
        ret
    }
  
    .method private hidebysig static bool MakeByRefEqualsLdToken() cil managed
    {
        .maxstack  2
  	
        ldtoken    valuetype MyType2
        call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        callvirt   instance class [mscorlib]System.Type [mscorlib]System.Type::MakeByRefType()
        
        ldtoken    valuetype MyType2&
        call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	
        ceq
	
        ret
    }
}

.class private auto ansi sealed beforefieldinit MyType1
       extends [mscorlib]System.ValueType
{
}

.class private auto ansi sealed beforefieldinit MyType2
       extends [mscorlib]System.ValueType
{
}
