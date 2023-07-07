// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME {}
.assembly extern xunit.core {}
.namespace Test_deadlock {
.class public A extends [mscorlib]System.Object
{
.field static public class Test_deadlock.A a
.field static public class Test_deadlock.B b
.method public static rtspecialname specialname void .cctor ()
{
ldnull
stsfld class Test_deadlock.B Test_deadlock.A::b
ldsfld class Test_deadlock.A Test_deadlock.B::a
stsfld class Test_deadlock.A Test_deadlock.A::a
ret
}
}

.class public B extends [mscorlib]System.Object
{
.field static public class Test_deadlock.A a
.field static public class Test_deadlock.B b
.method public static rtspecialname specialname void .cctor ()
{
ldnull
stsfld class Test_deadlock.A Test_deadlock.B::a
ldsfld class Test_deadlock.B Test_deadlock.A::b
stsfld class Test_deadlock.B Test_deadlock.B::b
ret
}
}

.class public Test_deadlock extends[mscorlib]System.Object
{
.method public static int32 Main()
{
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
ldsfld class Test_deadlock.B Test_deadlock.A::b
pop
ldc.i4 100
ret
}
}
}
