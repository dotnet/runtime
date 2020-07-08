// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly tls1 {}
.namespace 'tls'
{
  .class public auto ansi beforefieldinit tls1
         extends [mscorlib]System.Object
  {
    .field public static int32 i
    .custom instance void [mscorlib]System.ThreadStaticAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public static uint8 b
    .custom instance void [mscorlib]System.ThreadStaticAttribute::.ctor() = ( 01 00 00 00 ) 
    .method private hidebysig specialname rtspecialname static 
            void  .cctor() cil managed
    {
      // Code size       14 (0xe)
      .maxstack  8
      IL_0000:  ldc.i4.s   10
      IL_0002:  stsfld     int32 'tls'.tls1::i
      IL_0007:  ldc.i4.1
      IL_0008:  stsfld     uint8 'tls'.tls1::b
      IL_000d:  ret
    } // end of method tls1::.cctor

  } // end of class tls1

}
