.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly ASSEMBLY_NAME {}
.field static public int32 a at D_00

.method public static rtspecialname specialname void .cctor ()
{
ldc.i4 100
stsfld int32 a
ret
}

.class public auto ansi Test_global extends [mscorlib] System.Object {
.method public static int32 Main()
{
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
ldsfld int32 a
ret
}

.data D_00 = int32(1)
}
