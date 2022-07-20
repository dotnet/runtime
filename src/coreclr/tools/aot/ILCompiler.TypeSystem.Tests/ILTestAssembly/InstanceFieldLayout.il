// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public sequential ansi sealed beforefieldinit IsByRefLike.InvalidStruct
       extends [CoreTestAssembly]System.ValueType
{
  .field private object& ByRef
}

.class public auto ansi beforefieldinit IsByRefLike.InvalidClass1
       extends [CoreTestAssembly]System.Object
{
  .field private object& ByRef
}

.class public auto ansi beforefieldinit IsByRefLike.InvalidClass2
       extends [CoreTestAssembly]System.Object
{
  .custom instance void [CoreTestAssembly]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
  .field private object& ByRef
}
