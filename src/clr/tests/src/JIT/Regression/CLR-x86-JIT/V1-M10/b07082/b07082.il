// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b07082
{
}
.class ILGEN_939796994 {
.method static int32 main(class System.String[]) {
.entrypoint
.maxstack	2

ldc.i4 8
ldc.i4 7 
clt.un

ldc.i4 100
add

ret
}
}