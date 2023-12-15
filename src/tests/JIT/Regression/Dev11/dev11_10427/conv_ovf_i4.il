// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly 'conv_ovf_i4' {}
.assembly extern xunit.core {}
.assembly extern System.Console { auto }
.assembly extern System.Runtime.Extensions { auto }
.assembly extern mscorlib { auto }


.class public auto ansi Test_conv_ovf_i4 extends [mscorlib] System.Object {
.method static public int32 main() il managed
{
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.locals init ([0] int64 alpha, [1] int32 delta, [2] int32 result)

.try
{
call int32 [System.Runtime.Extensions]System.Environment::get_TickCount()
conv.i8
ldc.i8 0x80000000
or
stloc.0
ldc.i4 100
stloc result

ldloc.0
ldc.i4.m1
conv.u8
and
conv.ovf.i4
stloc.1

ldstr "First conv.ovf.i4 raised no exception. delta = "
call void [System.Console]System.Console::Write(string)
ldloc.1
call void [System.Console]System.Console::WriteLine(int32)
ldc.i4 101
stloc result

ldloc.0
ldc.i4.m1
conv.u8
and

stloc.0
ldloc.0

conv.ovf.i4
stloc.1

ldstr "Second conv.ovf.i4 raised no exception. delta = "
call void [System.Console]System.Console::Write(string)
ldloc.1
call void [System.Console]System.Console::WriteLine(int32)

leave.s EXIT
}
catch [mscorlib]System.Exception
{
callvirt instance class [mscorlib]System.Type [mscorlib]System.Exception::GetType()
callvirt instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
call void [System.Console]System.Console::Write(string)

leave.s EXIT
}

EXIT:
ldloc result
ret
}
}
