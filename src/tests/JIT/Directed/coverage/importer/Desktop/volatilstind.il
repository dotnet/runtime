.assembly extern mscorlib {}
.assembly extern xunit.core {}
.assembly volatilstind { }
.method public static void f()
{
.locals init (int32* V_0)
ldloc.0
ldc.i4 1
volatile. stind.i4
ret
}
.class public auto ansi volatilstind {
.method public static int32 Main()
{
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.try
{
call void f()
leave FAIL
}
catch [mscorlib]System.NullReferenceException
{
leave PASS
}
FAIL:
ldc.i4 1
ret
PASS:
ldc.i4 100
ret
}
}