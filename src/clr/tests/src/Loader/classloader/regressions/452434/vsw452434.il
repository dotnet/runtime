// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
// this is regression test for VSW 452434.
// The issue here was that we are using the same RVA multiple times which created a problem when ngening the image.

.assembly Type{}
.assembly extern mscorlib {}

.data i1data = int32(43962)

// RVA (Field ValueType)
.class public CReloc8
{	
	.field public static valuetype S1 s1 at i1data
	.field public static valuetype S2 s2 at i1data        
}

.class public sequential ansi sealed beforefieldinit S1
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} // end of class S

.class public sequential ansi sealed beforefieldinit S2
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 2
} // end of class S
