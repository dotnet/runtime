// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly switch1{}
.class switch1 {
.method static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10

 ldc.i4 55
 ldc.i4 55
switch(FOO1)
 ldc.i4 -1
 br FOO
FOO1:
 ldc.i4 1
FOO:
pop
ldc.i4 55
sub
ldc.i4 100
add
 ret

}
}