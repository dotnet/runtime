// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly deadlock {}
.assembly extern xunit.core
{
    .publickeytoken = (
        8d 05 b1 bb 7a 6f db 6c
     )
     .ver 2:4:2:0
}
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
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
ldsfld class B A::b
pop
ldc.i4 100
ret
}
