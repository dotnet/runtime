// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly deadlock {}
.class public A extends [mscorlib]System.Object
{
.field static public class A a
.field static public class B b
.method public static rtspecialname specialname void .cctor ()
{
ldnull
stsfld class B A::b
ldsfld class A B::a
stsfld class A A::a
ret
}
}

.class public B extends [mscorlib]System.Object
{
.field static public class A a
.field static public class B b
.method public static rtspecialname specialname void .cctor ()
{
ldnull
stsfld class A B::a
ldsfld class B A::b
stsfld class B B::b
ret
}
}

.method public static int32 Main()
{
.entrypoint
ldsfld class B A::b
pop
ldc.i4 100
ret
}
