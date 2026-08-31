// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly Test6_supplier{}

.assembly extern mscorlib{}
.assembly extern Test6_FriendPriInterface{}


.class public A implements [Test6_FriendPriInterface]IA
{
.method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A::.ctor

.method public hidebysig virtual final 
        instance void  methodIA() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0006:  ret
  } // end of method A::.ctor
}


.class public B implements class [Test6_FriendPriInterface]'IB`1'<int32>
{
.method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method B::.ctor

.method public hidebysig virtual final 
        instance void  methodIB() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0006:  ret
  } // end of method B::.ctor
}
