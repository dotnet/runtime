// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern mscorlib { }
.assembly extern A { }
.assembly extern B { }
.assembly extern GenRetType { }
.assembly extern Dictionary { }

.assembly GenBaseType { }

.class public auto ansi beforefieldinit GenBaseType<A,B>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed 
  {
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } 

  .method public hidebysig newslot virtual instance object MyFunc(string& res) 
  {
    ldarg.1
    ldstr "object GenBaseType.MyFunc()"
    stind.ref
    newobj instance void [A]A::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class [B]B MyFunc(string& res) 
  {
    ldarg.1
    ldstr "B GenBaseType.MyFunc()"
    stind.ref
    newobj instance void [B]B::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class [B]B GenToNonGen(string& res) 
  {
    ldarg.1
    ldstr "B GenBaseType.GenToNonGen()"
    stind.ref
    newobj instance void [B]B::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class [B]B NonGenThroughGenFunc(string& res) 
  {
    ldarg.1
    ldstr "B GenBaseType.NonGenThroughGenFunc()"
    stind.ref
    newobj instance void [B]B::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class [GenRetType]GenRetType<int32,object> MyGenFunc(string& res) 
  {
    ldarg.1
    ldstr "GenRetType<int32,object> GenBaseType.MyGenFunc()"
    stind.ref
    newobj instance void class [GenRetType]GenRetType<int32,object>::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class [GenRetType]GenRetType<!A, class [Dictionary]Dictionary<string,!B>> MyGenFunc(string& res) 
  {
    ldarg.1
    ldstr "GenRetType<!A, class Dictionary<string,!B>> GenBaseType.MyGenFunc()"
    stind.ref
    newobj instance void class [GenRetType]GenRetType<!A, class [Dictionary]Dictionary<string,!B>>::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class [GenRetType]GenRetType<!A,!B> TestNonGenericDerived(string& res) 
  {
    ldarg.1
    ldstr "GenRetType<!A,!B> GenBaseType.TestNonGenericDerived()"
    stind.ref
    newobj instance void class [GenRetType]GenRetType<!A,!B>::.ctor()
    ret
  }
}
