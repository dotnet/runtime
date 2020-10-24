.assembly extern legacy library mscorlib {}
.assembly global {}
.field static public int32 a at D_00

.method public static rtspecialname specialname void .cctor ()
{
ldc.i4 100
stsfld int32 a
ret
}

.method public static int32 Main()
{
.entrypoint
ldsfld int32 a
ret
}

.data D_00 = int32(1)
