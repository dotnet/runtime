// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern mscorlib
{
    .ver 2:0:5:0
    .publickeytoken = (b7 7a 5c 56 19 34 e0 89)
}

//Header for this dll
.assembly TestLib
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module TestLib.dll

//The parent-class wich inherits enum
.class public auto ansi VA
    extends [mscorlib]System.Enum
{
    
    .field public specialname rtspecialname int32 value__
    
    .field public static literal valuetype VA None = int32(0)
    .field public static literal valuetype VA Leet = int32(1337)
    
}

//The vulnerable object: Enum.ToObject will overwrite both: the actual value of the enum (int32) and the obj-field (actually a pointer)
.class public auto ansi UB1
    extends VA
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        ret
    }
    .field public class [mscorlib]System.Object obj
}
