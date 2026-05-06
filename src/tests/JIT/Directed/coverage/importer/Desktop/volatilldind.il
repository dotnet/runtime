.assembly extern mscorlib {}
.assembly extern xunit.core {}
.assembly ASSEMBLY_NAME { }
.method public static void f()
{
.locals init (int32* V_0)
ldloc V_0
volatile. ldind.i4
pop
ret
}
.class public auto ansi volatilldind {
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