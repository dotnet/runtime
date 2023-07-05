.assembly extern mscorlib
{
}
.assembly 'library'
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module 'library.dll'


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib
       extends [mscorlib]System.Object
{
  .class interface abstract auto ansi nested public IFoo
  {
    .method public hidebysig newslot abstract virtual 
            instance void  Foo() cil managed
    {
    } // end of method IFoo::Foo

  } // end of class IFoo

  .class interface abstract auto ansi nested public IBar
  {
    .method public hidebysig newslot abstract virtual 
            instance void  Bar() cil managed
    {
    } // end of method IBar::Bar

  } // end of class IBar

  .class auto ansi nested private beforefieldinit A
         extends [mscorlib]System.Object
         implements Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib/IBar,
                    Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib/IFoo
  {
    .method public hidebysig newslot virtual final 
            instance void  Foo() cil managed
    {
      // Code size       1 (0x1)
      .maxstack  8
      IL_0000:  ret
    } // end of method A::Foo

    .method public hidebysig newslot virtual final 
            instance void  Bar() cil managed
    {
      // Code size       1 (0x1)
      .maxstack  8
      IL_0000:  ret
    } // end of method A::Bar

  } // end of class A

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }

}