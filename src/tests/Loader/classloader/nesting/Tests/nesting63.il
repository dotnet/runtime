// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
// model revision #2
// To compile: ilasm nesting63.il

.assembly extern mscorlib{}
.assembly nesting63{}

// Nesting module: PureManaged
// Nesting location: MainModule

// enclosing type 
// kind: Class
// flag: None
// visibility: public
// generics: Nongeneric
// depends on nested: None

.class public EnclType
{
  // nested type 
  // kind: Delegate
  // flag: sealed
  // visibility: assembly
  // generics: Generic
  // depends on enclosing: None

  .class sealed nested assembly NestedType<([mscorlib]System.Object) T> extends [mscorlib]System.MulticastDelegate
  {
   // nested member visibility: public

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(object 'object',
                                 native int 'method') runtime managed
    {}

    .method public hidebysig newslot virtual
            instance void  Invoke(int32 i) runtime managed
    {}

    .method public hidebysig newslot virtual 
            instance class [mscorlib]System.IAsyncResult
            BeginInvoke(int32 i,
                        class [mscorlib]System.AsyncCallback callback,
                        object 'object') runtime managed
    {}

    .method public hidebysig newslot virtual
            instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
    {}
 
  }

  // enclosing depends on nested: None
  // enclosing member visibility: famorassem
  // none

  // entry point location: EnclosingType

  .method public static int32 Main()
  {
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

  .method public hidebysig static void TakesADelegate(class EnclType/NestedType<int32> SomeFunction) cil managed
  {
    ldarg.0
    ldc.i4.s   21
    callvirt   instance void class EnclType/NestedType<int32>::Invoke(int32)
    ret
  }

  .method public hidebysig static void  DelegateFunction(int32 i) cil managed
  {
    ret
  }

  .method public specialname instance void .ctor() cil managed
  {
    .maxstack 8
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }

}


// Client module: PureManaged
// Client location: MainModule
// Client interaction: ExtendsEncl

.class public Client extends EnclType
{

  .method public specialname instance void .ctor() cil managed
  {
    .maxstack 8
    ldarg.0
    call instance void EnclType::.ctor()
    ret
  }

  .method public hidebysig instance void method2()
  {
    ldarg.0
    ldftn      void class EnclType::DelegateFunction(int32)
    newobj     instance void class EnclType/NestedType<int32>::.ctor(object, native int)
    
    call       void class EnclType::TakesADelegate(class EnclType/NestedType<int32>)
    ret
  }
}
