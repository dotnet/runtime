// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib
{
}

.assembly GitHub_23039
{
}
.assembly extern xunit.core {}

// Bug where assertion prop was tripped up because morph swapped arg
// order on an isinst helper call that fed into a conditional jump,
// and the object being queried was null.

.class public auto ansi beforefieldinit X`1<T> extends [mscorlib]System.Object
{
   .method public static hidebysig int32 F() cil managed noinlining
   {
       .locals init ([0] object)
       ldloc.0
       isinst class X`1<!T>
       brtrue IS
       ldc.i4 100
       ret
IS:    ldc.i4 -1
       ret
   }
}

.class public auto ansi beforefieldinit Y extends [mscorlib]System.Object
{
   .method public static hidebysig int32 Main() cil managed 
   {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      call int32 class X`1<object>::F()
      ret      
   }
}
