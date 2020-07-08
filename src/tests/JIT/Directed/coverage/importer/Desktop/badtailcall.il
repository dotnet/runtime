.assembly extern mscorlib {}
.assembly badtailcall { }
.method public static int32 f()
{
ldc.i4 100
ret
}
.method public static int32 f0()
{
ldc.i4 1
tail. call int32 f()
ret
}
.method public static int32 Main()
{
.entrypoint
.try
{
call int32 f()
leave.s FAIL
}
catch [mscorlib]System.Security.VerificationException
{
leave.s PASS
}
FAIL:
ldc.i4 100
ret
PASS:
ldc.i4 100
ret
}
