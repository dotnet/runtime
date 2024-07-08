// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console { auto }
.assembly extern xunit.core {}
.assembly extern mscorlib { auto }

// model revision #2
// To compile: ilasm nesting7.il /err

.assembly nesting7{}

// Nesting module: PureManaged
// Nesting location: MainModule

// enclosing type
// kind: Interface
// flag: abstract
// visibility: private
// generics: Nongeneric
// depends on nested: StaticField

.class interface abstract private EnclType
{
  // nested type
  // kind: Delegate
  // flag: sealed
  // visibility: assembly
  // generics: Nongeneric
  // depends on enclosing: None

  .class sealed nested assembly NestedType extends [mscorlib]System.MulticastDelegate
  {

    // nested member visibility: assembly

    .method assembly hidebysig specialname rtspecialname
            instance void  .ctor(object 'object',
                                 native int 'method') runtime managed
    {}

    .method assembly hidebysig newslot virtual
            instance void  Invoke(int32 i) runtime managed
    {}

    .method assembly hidebysig newslot virtual
            instance class [mscorlib]System.IAsyncResult
            BeginInvoke(int32 i,
                        class [mscorlib]System.AsyncCallback callback,
                        object 'object') runtime managed
    {}

    .method assembly hidebysig newslot virtual
            instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
    {}
  }

  // enclosing depends on nested: StaticField
  // enclosing member visibility: assembly
  .field assembly static class EnclType/NestedType sNestClass

  .method public hidebysig static void TakesADelegate(class EnclType/NestedType SomeFunction) cil managed
  {
    ldarg.0
    ldc.i4.s   21
    callvirt   instance void EnclType/NestedType::Invoke(int32)
    ret
  }

  .method public hidebysig static void  DelegateFunction(int32 i) cil managed
  {
    ret
  }

}


// Client module: PureManaged
// Client location: MainModule
// Client interaction: ImplementsEncl

.class public Client implements EnclType
{

  .method public specialname instance void .ctor() cil managed
  {
    .maxstack 8
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public hidebysig instance void method2()
  {
    // create the delegate which is a static field of EnclType
    ldarg.0
    ldftn      void class EnclType::DelegateFunction(int32)
    newobj     instance void class EnclType/NestedType::.ctor(object, native int)

    stsfld     class EnclType/NestedType class EnclType::sNestClass
    ldsfld     class EnclType/NestedType class EnclType::sNestClass
    call       void class EnclType::TakesADelegate(class EnclType/NestedType)
    ret
  }

}

// entry point location: GlobalManaged

.class public auto ansi abstract sealed beforefieldinit nesting7
    extends [mscorlib]System.Object
{
  .method public static int32 Main()
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init (class [mscorlib]System.Exception V_0,
                  int32 V_1)

    .try
    {
      newobj instance void class Client::.ctor()
      call   instance void class Client::method2()

      ldstr      "PASS"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s   100
      stloc.1
      leave.s    end
    }
    catch [mscorlib]System.Exception
    {
      stloc.0
      ldstr      "{0}Caught unexpected exception."
      ldloc.0
      call void [System.Console]System.Console::WriteLine(string,object)
      ldstr      "FAIL"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s   101
      stloc.1
      leave.s    end
    }
    end: ldloc.1
    ret
  }
}
