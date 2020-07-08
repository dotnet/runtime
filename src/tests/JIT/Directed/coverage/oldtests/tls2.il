// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly tls2 {}
.namespace 'tls'
{
  .class public auto ansi beforefieldinit tls2
         extends [mscorlib]System.Object
  {
//    .field public static uint8 b at T_00006000
    .custom instance void [mscorlib]System.ThreadStaticAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public static uint8 b
    .method private hidebysig specialname rtspecialname static 
            void  .cctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldc.i4.1
      IL_0001:  stsfld     uint8 'tls'.tls2::b
      IL_0006:  ret
    } // end of method tls2::.cctor

  }

}
//.data tls T_00006000 = bytearray (
//                 02) 