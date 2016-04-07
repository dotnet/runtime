
.assembly extern mscorlib {}
.assembly badldslfd { }
.class Test extends [mscorlib]System.Object
{
.field int32 i
.method static int32 f()
{
ldsfld int32 Test::i
ret
}
.method public static int32 Main()
{
.entrypoint
.try
{
call int32 Test::f()
leave.s FAIL
}
catch [mscorlib]System.Security.VerificationException
{
leave.s PASS
}
PASS:
ldc.i4 100
ret
FAIL:
ldc.i4 1
ret
}
}