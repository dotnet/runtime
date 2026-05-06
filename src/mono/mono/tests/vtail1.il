.assembly extern mscorlib { }

.assembly vtail1 { }

.class A extends [mscorlib]System.Object {
.method public hidebysig specialname rtspecialname instance void .ctor() cil managed
{
.maxstack 8
ldarg.0
call instance void [mscorlib]System.Object::.ctor()
ret
}
.method public hidebysig static int32 vtail1() cil managed noinlining {
.entrypoint
.maxstack 4
.locals init (uint8 V_0)
newobj instance void A::.ctor()
ldloca.s V_0
conv.u
ldc.i4.0
conv.i8
ldc.i4.5
callvirt instance int32 A::vtail2(uint8*, int64, int32)
ret
}
.method public hidebysig newslot virtual instance int32 vtail2(uint8* root_stack, int64 diff_stack, int32 counter) cil managed noinlining {
.maxstack 5
.locals init (uint8 V_0)
ldarg.3
ldc.i4.0
ble.s IL_0017
ldarg.0
ldarg.1
ldloca.s V_0
conv.u
ldarg.1
sub
ldc.i4.1
div
conv.i8
ldarg.3
ldc.i4.1
sub
tail. callvirt instance int32 A::vtail2(uint8*, int64, int32)
ret
IL_0017: ldarg.1
ldloca.s V_0
conv.u
ldarg.2
ldstr "vtail1"
call int32 A::check(uint8*, uint8*, int64, string)
ret
}
.method private hidebysig static int32 check(uint8* root_stack, uint8* local, int64 diff_stack, string name) cil managed noinlining {
.maxstack 8
ldarg.1
ldarg.0
sub
ldc.i4.1
div
conv.i8
ldarg.2
beq.s IL_0026
ldstr "{0} failure {1}"
ldarg.3
ldarg.0
ldarg.1
sub
ldc.i4.1
div
conv.i8
box [mscorlib]System.Int64
call string [mscorlib]System.String::Format(string, object, object)
call void [mscorlib]System.Console::WriteLine(string)
ldc.i4.1
ret
IL_0026: ldstr "{0} success"
ldarg.3
call string [mscorlib]System.String::Format(string, object)
call void [mscorlib]System.Console::WriteLine(string)
ldc.i4.0
ret
}
.method private hidebysig static int32 check(int64 root_stack, uint8* local, int64 diff_stack, string name) cil managed noinlining {
.maxstack 8
ldarg.0
conv.u
ldarg.1
ldarg.2
ldarg.3
call int32 A::check(uint8*, uint8*, int64, string)
ret
}
}
