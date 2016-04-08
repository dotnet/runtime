
.assembly extern legacy library mscorlib {}
.assembly refanytype1 { }
.method public static int32 f()
{
ldc.i4 100
ret
}
.method public static int32 Main()
{
.entrypoint
call int32 f()
mkrefany int32
refanytype
pop
ldc.i4 100
ret
}