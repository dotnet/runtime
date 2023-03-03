.assembly extern mscorlib
{
}
.assembly 'library'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module library.dll


.namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies
{
  .class public auto ansi beforefieldinit AssemblyWithUnusedAttributeOnReturnParameterDefinition
  	extends [mscorlib]System.Object
  {
  
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    }
    
    .method public hidebysig 
           instance default int32 Method (int32 arg)  cil managed 
    {
    .param [0]
    	.custom instance void class Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies.AssemblyWithUnusedAttributeOnReturnParameterDefinition/FooAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .maxstack 8
    IL_0000:  ldarg.1 
    IL_0001:  ret 
    }

    .method public hidebysig 
           instance default int32 MethodWithoutParameters ()  cil managed 
    {
    .param [0]
    	.custom instance void class Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies.AssemblyWithUnusedAttributeOnReturnParameterDefinition/FooAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .maxstack 8
    IL_0000:  ldc.i4.s 3 
    IL_0001:  ret 
    }

    .method public hidebysig 
           instance default int32 MethodWithoutParametersNonNestedAttribute ()  cil managed 
    {
    .param [0]
    	.custom instance void class Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies.BarAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .maxstack 8
    IL_0000:  ldc.i4.s 5
    IL_0001:  ret 
    }

  .class nested private auto ansi beforefieldinit FooAttribute
    extends [mscorlib]System.Attribute
  {

    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
    .maxstack 8
    IL_0000:  ldarg.0 
    IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
    IL_0006:  ret 
    }

  }
  }

  .class private auto ansi beforefieldinit BarAttribute
    extends [mscorlib]System.Attribute
  {

    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
    .maxstack 8
    IL_0000:  ldarg.0 
    IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
    IL_0006:  ret 
    }

  }
}

