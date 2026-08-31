/*
Author:
    Jay Krell (jaykrell@microsoft.com)

Copyright 2018 Microsoft
Licensed under the MIT license. See LICENSE file in the project root for full license information.

Derived from tailcall-interface.cs.
*/

.assembly extern mscorlib {  }

.assembly 'tailcall-interface-conservestack' { }

.class public sequential sealed Point
extends [mscorlib]System.ValueType
{
.field public int32 x
.field public int32 y
}

.class interface private abstract I1
{

.method public newslot abstract virtual instance void perturb_interface_offset1() { }

.method public newslot abstract virtual instance void F1(class I2 i2, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
}

.method public newslot abstract virtual instance void GF1<TF>(class I2 i2, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
}

}

.class interface private abstract GI1`1<TC>
{

.method public newslot abstract virtual instance void perturb_interface_offset1()
{
}

.method public newslot abstract virtual instance void perturb_interface_offset2()
{
}

.method public newslot abstract virtual instance void F1(class GI2`1<!TC> i2, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
}

.method public newslot abstract virtual instance void GF1<TF>(class GI2`1<!TC> i2, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
}

.method public newslot abstract virtual instance void HF1<TF>(class GI2`1<!!TF> i2, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
}
}

.class interface private abstract I2
{
.method public newslot abstract virtual instance void perturb_interface_offset1() { }

.method public newslot abstract virtual instance void perturb_interface_offset2() { }

.method public newslot abstract virtual instance void perturb_interface_offset3() { }

.method public newslot abstract virtual instance void F2(class I1 i1, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
}

.method public newslot abstract virtual instance void GF2<TF>(class I1 i1, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
}

}

.class interface private abstract GI2`1<TC>
{

.method public newslot abstract virtual instance void perturb_interface_offset1()
{
}

.method public newslot abstract virtual instance void perturb_interface_offset2()
{
}

.method public newslot abstract virtual instance void perturb_interface_offset3()
{
}

.method public newslot abstract virtual instance void perturb_interface_offset4()
{
}

.method public newslot abstract virtual instance void F2(class GI1`1<!TC> i1, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
}
.method public newslot abstract virtual instance void GF2<TF>(class GI1`1<!TC> i1, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
}
.method public newslot abstract virtual instance void HF2<TF>(class GI1`1<!!TF> i1, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
}
}

.class public C1 implements I1
{
.field static int64 i
.field public static int64 errors

.method private newslot virtual final instance void I1.perturb_interface_offset1()
{
.override I1::perturb_interface_offset1
ret
}

.method public static void check(int64 stack1, int64 stack2)
{
ldsfld int64 C1::i
ldc.i8 1
add
stsfld int64 C1::i

ldarg 0
ldarg 1
bne.un IL_0013

//ldstr "{0}"
//ldsfld int64 C1::i
//box [mscorlib]System.Int64
//call void [mscorlib]System.Console::WriteLine(string, object)
ret

IL_0013:
ldsfld int64 C1::errors
ldc.i8 1
add
stsfld int64 C1::errors

ldstr "{0} tailcall failure stack1:{1:X} stack2:{2:X} delta:{3}"

ldc.i4 4
newarr [mscorlib]System.Object

dup
ldc.i4 0
ldsfld int64 C1::i
box [mscorlib]System.Int64
stelem.ref

dup
ldc.i4 1
ldarg 0
box [mscorlib]System.Int64
stelem.ref

dup
ldc.i4 2
ldarg 1
box [mscorlib]System.Int64
stelem.ref

dup
ldc.i4 3
ldarg 0
ldarg 1
sub
box [mscorlib]System.Int64
stelem.ref

call void class [mscorlib]System.Console::WriteLine(string, object[])
ret
}
.method private newslot virtual final instance void I1.F1(class I2 i2, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override I1::F1

ldarg 2
ldc.i8 0
ble IL_0014

ldarg 1
ldarg 0
ldarg 2
ldc.i8 1
sub
ldarg 3
ldarga counter
conv.u
conv.u8
tail.
callvirt instance void I2::F2(class I1, int64, int64, int64)
ret

IL_0014:
ldarga counter
conv.u
conv.u8
ldarg current_stack
call void C1::check(int64, int64)
ret
}

.method private newslot virtual final instance void I1.GF1<TF>(class I2 i2, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override I1::GF1

ldarg 2
ldc.i8 0
ble IL_0014

ldarg 1
ldarg 0
ldarg 2
ldc.i8 1
sub
ldarg 3
ldarga counter
conv.u
conv.u8
tail.
callvirt instance void I2::GF2<!!0>(class I1, int64, int64, int64)
ret

IL_0014:
ldarga counter
conv.u
conv.u8
ldarg current_stack
call void C1::check(int64, int64)
ret
}
.method public instance void .ctor()
{
ldarg 0
call instance void [mscorlib]System.Object::.ctor()
ret
}
}
.class public GC1`1<TC>
implements class GI1`1<!TC>
{
.method private newslot virtual final instance void 'GI1<TC>.perturb_interface_offset1'()
{
.override method instance void class GI1`1<!TC>::perturb_interface_offset1()
ret
}
.method private newslot virtual final instance void 'GI1<TC>.perturb_interface_offset2'()
{
.override method instance void class GI1`1<!TC>::perturb_interface_offset2()
ret
}
.method public static void check(int64 stack1, int64 stack2)
{
ldarg 0
ldarg 1
call void C1::check(int64, int64)
ret
}
.method private newslot virtual final instance void 'GI1<TC>.F1'(class GI2`1<!TC> i2, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override method instance void class GI1`1<!TC>::F1(class GI2`1<!0>, int64, int64, int64)

ldarg 2
ldc.i8 0
ble IL_0014

ldarg 1
ldarg 0
ldarg 2
ldc.i8 1
sub
ldarg 3
ldarga counter
conv.u
conv.u8
tail.
callvirt instance void class GI2`1<!TC>::F2(class GI1`1<!0>, int64, int64, int64)
ret

IL_0014:
ldarga counter
conv.u
conv.u8
ldarg current_stack
call void class GC1`1<!TC>::check(int64, int64)
ret
}
.method private newslot virtual final instance void 'GI1<TC>.GF1'<TF>(class GI2`1<!TC> i2, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override method instance void class GI1`1<!TC>::GF1<[1]>(class GI2`1<!0>, int64, int64, int64)

ldarg 2
ldc.i8 0
ble IL_0014
ldarg 1
ldarg 0
ldarg 2
ldc.i8 1
sub
ldarg 3
ldarga counter
conv.u
conv.u8
tail.
callvirt instance void class GI2`1<!TC>::GF2<!!0>(class GI1`1<!0>, int64, int64, int64)
ret

IL_0014:
ldarga counter
conv.u
conv.u8
ldarg current_stack
call void class GC1`1<!TC>::check(int64, int64)
ret
}

.method private newslot virtual final instance void 'GI1<TC>.HF1'<TF>(class GI2`1<!!TF> i2, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override method instance void class GI1`1<!TC>::HF1<[1]>(class GI2`1<!!TF>, int64, int64, int64)

ldarg 2
ldc.i8 0
ble IL_0014

ldarg 1
ldarg 0
ldarg 2
ldc.i8 1
sub
ldarg 3
ldarga counter
conv.u
conv.u8
tail.
callvirt instance void class GI2`1<!!0>::HF2<!0>(class GI1`1<!!0>, int64, int64, int64)
ret

IL_0014:
ldarga counter
conv.u
conv.u8
ldarg current_stack
call void class GC1`1<!TC>::check(int64, int64)
ret
}

.method public instance void .ctor()
{
ldarg 0
call instance void [mscorlib]System.Object::.ctor()
ret
}
}
.class public C2
implements I2
{
.method private newslot virtual final instance void I2.perturb_interface_offset1()
{
.override I2::perturb_interface_offset1
ret
}
.method private newslot virtual final instance void I2.perturb_interface_offset2()
{
.override I2::perturb_interface_offset2
ret
}
.method private newslot virtual final instance void I2.perturb_interface_offset3()
{
.override I2::perturb_interface_offset3
ret
}

.method private static void check(int64 stack1, int64 stack2)
{
ldarg 0
ldarg 1
call void C1::check(int64, int64)
ret
}

.method private newslot virtual final instance void I2.F2(class I1 i1, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override I2::F2

ldarg 2
ldc.i8 0
ble IL_0014

ldarg 1
ldarg 0
ldarg 2
ldc.i8 1
sub
ldarg 3
ldarga counter
conv.u
conv.u8
tail.
callvirt instance void I1::F1(class I2, int64, int64, int64)
ret

IL_0014:
ldarga counter
conv.u
conv.u8
ldarg current_stack
call void C2::check(int64, int64)
ret
}
.method private newslot virtual final instance void I2.GF2<TF>(class I1 i1, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override I2::GF2

ldarg 2
ldc.i8 0
ble IL_0014
ldarg 1
ldarg 0
ldarg 2
ldc.i8 1
sub
ldarg 3
ldarga counter
conv.u
conv.u8
tail.
callvirt instance void I1::GF1<!!0>(class I2, int64, int64, int64)
ret
IL_0014:
ldarga counter
conv.u
conv.u8
ldarg current_stack
call void C2::check(int64, int64)
ret
}
.method public instance void .ctor()
{
ldarg 0
call instance void [mscorlib]System.Object::.ctor()
ret
}
}
.class public GC2`1<TC>
implements class GI2`1<!TC>
{
.method private newslot virtual final instance void 'GI2<TC>.perturb_interface_offset1'()
{
.override method instance void class GI2`1<!TC>::perturb_interface_offset1()
ret
}
.method private newslot virtual final instance void 'GI2<TC>.perturb_interface_offset2'()
{
.override method instance void class GI2`1<!TC>::perturb_interface_offset2()
ret
}
.method private newslot virtual final instance void 'GI2<TC>.perturb_interface_offset3'()
{
.override method instance void class GI2`1<!TC>::perturb_interface_offset3()
ret
}
.method private newslot virtual final instance void 'GI2<TC>.perturb_interface_offset4'()
{
.override method instance void class GI2`1<!TC>::perturb_interface_offset4()
ret
}
.method public static void check(int64 stack1, int64 stack2)
{
ldarg 0
ldarg 1
call void C1::check(int64, int64)
ret
}
.method private newslot virtual final instance void 'GI2<TC>.F2'(class GI1`1<!TC> i1, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override method instance void class GI2`1<!TC>::F2(class GI1`1<!0>, int64, int64, int64)

ldarg 2
ldc.i8 0
ble IL_0014

ldarg 1
ldarg 0
ldarg 2
ldc.i8 1
sub
ldarg 3
ldarga counter
conv.u
conv.u8
tail.
callvirt instance void class GI1`1<!TC>::F1(class GI2`1<!0>, int64, int64, int64)
ret

IL_0014:
ldarga counter
conv.u
conv.u8
ldarg current_stack
call void class GC2`1<!TC>::check(int64, int64)
ret
}
.method private newslot virtual final instance void 'GI2<TC>.GF2'<TF>(class GI1`1<!TC> i1, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override method instance void class GI2`1<!TC>::GF2<[1]>(class GI1`1<!0>, int64, int64, int64)

ldarg 2
ldc.i8 0
ble IL_0014

ldarg 1
ldarg 0
ldarg 2
ldc.i8 1
sub
ldarg 3
ldarga counter
conv.u
conv.u8
tail.
callvirt instance void class GI1`1<!TC>::GF1<!!0>(class GI2`1<!0>, int64, int64, int64)
ret

IL_0014:
ldarga counter
conv.u
conv.u8
ldarg current_stack
call void class GC2`1<!TC>::check(int64, int64)
ret
}
.method private newslot virtual final instance void 'GI2<TC>.HF2'<TF>(class GI1`1<!!TF> i1, int64 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override method instance void class GI2`1<!TC>::HF2<[1]>(class GI1`1<!!TF>, int64, int64, int64)

ldarg 2
ldc.i8 0
ble IL_0014
ldarg 1
ldarg 0
ldarg 2
ldc.i8 1
sub
ldarg 3
ldarga counter
conv.u
conv.u8
tail.
callvirt instance void class GI1`1<!!0>::HF1<!0>(class GI2`1<!!0>, int64, int64, int64)
ret

IL_0014:
ldarga counter
conv.u
conv.u8
ldarg current_stack
call void class GC2`1<!TC>::check(int64, int64)
ret
}
.method public instance void .ctor()
{
ldarg 0
call instance void [mscorlib]System.Object::.ctor()
ret
}
}
.class public A
{
.method public instance void .ctor()
{
ldarg 0
call instance void [mscorlib]System.Object::.ctor()
ret
}
}
.class public B
{
.method public instance void .ctor()
{
ldarg 0
call instance void [mscorlib]System.Object::.ctor()
ret
}
}
.class interface private abstract IC
{
.method public newslot abstract virtual instance !!T cast1<T>(object o, int64 counter, int64 stack) noinlining
{
}
.method public newslot abstract virtual instance class B cast2(object o, int64 counter, int64 stack) noinlining
{
}
.method public newslot abstract virtual instance !!T cast3<T>(object o, int64 counter, int64 stack) noinlining
{
}
.method public newslot abstract virtual instance class B[] cast4(object o, int64 counter, int64 stack) noinlining
{
}
.method public newslot abstract virtual instance !!T[] cast5<T>(object o, int64 counter, int64 stack) noinlining
{
}
}
.class public C
{
.method public static void check(int64 stack1, int64 stack2) noinlining
{
ldarg 0
ldarg 1
call void C1::check(int64, int64)
ret
}

.method public instance !!T cast1<T>(object o, int64 counter, int64 stack) noinlining
{
ldarg 2
ldc.i8 0
ble IL_0013

ldarg 0
ldarg 1
ldarg 2
ldc.i8 1
sub
ldarga counter
conv.u
conv.u8
tail.
call instance !!0 C::cast1<!!0>(object, int64, int64)
ret

IL_0013:
ldarga counter
conv.u
conv.u8
ldarg 3
call void C::check(int64, int64)
ldarg 1
unbox.any !!T
ret
}
.method public instance class B cast2(object o, int64 counter, int64 stack) noinlining
{
ldarg 2
ldc.i8 0
ble IL_0013

ldarg 0
ldarg 1
ldarg 2
ldc.i8 1
sub
ldarga counter
conv.u
conv.u8
tail.
call instance class B C::cast2(object, int64, int64)
ret

IL_0013:
ldarga counter
conv.u
conv.u8
ldarg 3
call void C::check(int64, int64)
ldarg 0
ldarg 1
ldc.i8 99
ldc.i8 0
tail.
call instance !!0 C::cast1<class B>(object, int64, int64)
ret
}
.method public instance !!T cast3<T>(object o, int64 counter, int64 stack) noinlining
{
ldarg 2
ldc.i8 0
ble IL_0013

ldarg 0
ldarg 1
ldarg 2
ldc.i8 1
sub
ldarga counter
conv.u
conv.u8
tail.
call instance !!0 C::cast3<!!0>(object, int64, int64)
ret

IL_0013:
ldarga counter
conv.u
conv.u8
ldarg 3
call void C::check(int64, int64)
ldarg 0
ldarg 1
ldc.i8 99
ldc.i8 0
tail.
call instance !!0 C::cast1<!!0>(object, int64, int64)
ret
}
.method public instance class B[] cast4(object o, int64 counter, int64 stack) noinlining
{
ldarg 2
ldc.i8 0
ble IL_0013

ldarg 0
ldarg 1
ldarg 2
ldc.i8 1
sub
ldarga counter
conv.u
conv.u8
tail.
call instance class B[] C::cast4(object, int64, int64)
ret

IL_0013:
ldarga counter
conv.u
conv.u8
ldarg 3
call void C::check(int64, int64)
ldarg 0
ldarg 1
ldc.i8 99
ldc.i8 0
tail.
call instance !!0 C::cast1<class B[]>(object, int64, int64)
ret
}
.method public instance !!T[] cast5<T>(object o, int64 counter, int64 stack) noinlining
{
ldarg 2
ldc.i8 0
ble IL_0013

ldarg 0
ldarg 1
ldarg 2
ldc.i8 1
sub
ldarga counter
conv.u
conv.u8
tail.
call instance !!0[] C::cast5<!!0>(object, int64, int64)
ret

IL_0013:
ldarga counter
conv.u
conv.u8
ldarg 3
call void C::check(int64, int64)
ldarg 0
ldarg 1
ldc.i8 99
ldc.i8 0
tail.
call instance !!0 C::cast1<!!0[]>(object, int64, int64)
ret
}
.method public instance void .ctor()
{
ldarg 0
call instance void [mscorlib]System.Object::.ctor()
ret
}
}
.class public D`1<T1>
{
.method public static void check(int64 stack1, int64 stack2) noinlining
{
ldarg 0
ldarg 1
call void C::check(int64, int64)
ret
}
.method public static !!T cast1<T>(object o, int64 counter, int64 stack) noinlining
{
ldarg 1
ldc.i8 0
ble IL_0012

ldarg 0
ldarg 1
ldc.i8 1
sub
ldarga counter
conv.u
conv.u8
tail.
call !!0 class D`1<!T1>::cast1<!!0>(object, int64, int64)
ret

IL_0012:
ldarga counter
conv.u
conv.u8
ldarg 2
call void class D`1<!T1>::check(int64, int64)
ldarg 0
unbox.any !!T
ret
}
.method public instance class B cast2(object o, int64 counter, int64 stack) noinlining
{
ldarg 2
ldc.i8 0
ble IL_0013

ldarg 0
ldarg 1
ldarg 2
ldc.i8 1
sub
ldarga counter
conv.u
conv.u8
tail.
call instance class B class D`1<!T1>::cast2(object, int64, int64)
ret

IL_0013:
ldarga counter
conv.u
conv.u8
ldarg 3
call void class D`1<!T1>::check(int64, int64)
ldarg 1
ldc.i8 99
ldc.i8 0
tail.
call !!0 class D`1<!T1>::cast1<class B>(object, int64, int64)
ret
}
.method public instance !!T cast3<T>(object o, int64 counter, int64 stack) noinlining
{
ldarg 2
ldc.i8 0
ble IL_0013

ldarg 0
ldarg 1
ldarg 2
ldc.i8 1
sub
ldarga counter
conv.u
conv.u8
tail.
call instance !!0 class D`1<!T1>::cast3<!!0>(object, int64, int64)
ret

IL_0013:
ldarga counter
conv.u
conv.u8
ldarg 3
call void class D`1<!T1>::check(int64, int64)
ldarg 1
ldc.i8 99
ldc.i8 0
tail.
call !!0 class D`1<!T1>::cast1<!!0>(object, int64, int64)
ret
}
.method public instance class B[] cast4(object o, int64 counter, int64 stack) noinlining
{
ldarg 2
ldc.i8 0
ble IL_0013

ldarg 0
ldarg 1
ldarg 2
ldc.i8 1
sub
ldarga counter
conv.u
conv.u8
tail.
call instance class B[] class D`1<!T1>::cast4(object, int64, int64)
ret

IL_0013:
ldarga counter
conv.u
conv.u8
ldarg 3
call void class D`1<!T1>::check(int64, int64)
ldarg 1
ldc.i8 99
ldc.i8 0
tail.
call !!0 class D`1<!T1>::cast1<class B[]>(object, int64, int64)
ret
}
.method public instance !!T[] cast5<T>(object o, int64 counter, int64 stack) noinlining
{
ldarg 2
ldc.i8 0
ble IL_0013

ldarg 0
ldarg 1
ldarg 2
ldc.i8 1
sub
ldarga counter
conv.u
conv.u8
tail.
call instance !!0[] class D`1<!T1>::cast5<!!0>(object, int64, int64)
ret

IL_0013:
ldarga counter
conv.u
conv.u8
ldarg 3
call void class D`1<!T1>::check(int64, int64)
ldarg 1
ldc.i8 99
ldc.i8 0
tail.
call !!0 class D`1<!T1>::cast1<!!0[]>(object, int64, int64)
ret
}
.method public instance !T1 cast6(object o, int64 counter, int64 stack) noinlining
{
ldarg 2
ldc.i8 0
ble IL_0013

ldarg 0
ldarg 1
ldarg 2
ldc.i8 1
sub
ldarga counter
conv.u
conv.u8
tail.
call instance !0 class D`1<!T1>::cast6(object, int64, int64)
ret

IL_0013:
ldarga counter
conv.u
conv.u8
ldarg 3
call void class D`1<!T1>::check(int64, int64)
ldarg 1
ldc.i8 99
ldc.i8 0
tail.
call !!0 class D`1<!T1>::cast1<!0>(object, int64, int64)
ret
}
.method public instance !T1 cast7<T>(object o, int64 counter, int64 stack) noinlining
{
ldarg 2
ldc.i8 0
ble IL_0013

ldarg 0
ldarg 1
ldarg 2
ldc.i8 1
sub
ldarga counter
conv.u
conv.u8
tail.
call instance !0 class D`1<!T1>::cast7<!!0>(object, int64, int64)
ret

IL_0013:
ldarga counter
conv.u
conv.u8
ldarg 3
call void class D`1<!T1>::check(int64, int64)
ldarg 1
ldc.i8 99
ldc.i8 0
tail.
call !!0 class D`1<!T1>::cast1<!0>(object, int64, int64)
ret
}
.method public instance !T1[] cast8(object o, int64 counter, int64 stack) noinlining
{
ldarg 2
ldc.i8 0
ble IL_0013

ldarg 0
ldarg 1
ldarg 2
ldc.i8 1
sub
ldarga counter
conv.u
conv.u8
tail.
call instance !0[] class D`1<!T1>::cast8(object, int64, int64)
ret

IL_0013:
ldarga counter
conv.u
conv.u8
ldarg 3
call void class D`1<!T1>::check(int64, int64)
ldarg 0
ldarg 1
ldc.i8 99
ldc.i8 0
tail.
call instance !!0 class D`1<!T1>::cast3<!0[]>(object, int64, int64)
ret
}
.method public instance !T1[] cast9<T>(object o, int64 counter, int64 stack) noinlining
{
ldarg 2
ldc.i8 0
ble IL_0013

ldarg 0
ldarg 1
ldarg 2
ldc.i8 1
sub
ldarga counter
conv.u
conv.u8
tail.
call instance !0[] class D`1<!T1>::cast9<!!0>(object, int64, int64)
ret

IL_0013:
ldarga counter
conv.u
conv.u8
ldarg 3
call void class D`1<!T1>::check(int64, int64)
ldarg 0
ldarg 1
ldc.i8 99
ldc.i8 0
tail.
call instance !!0 class D`1<!T1>::cast3<!0[]>(object, int64, int64)
ret
}
.method public instance void .ctor()
{
ldarg 0
call instance void [mscorlib]System.Object::.ctor()
ret
}
}
.class private C3
{
.method static void print() noinlining
{
ret
}

.method static void print(object o) noinlining
{
call void C3::print()
ret
}

.method public static void Main(string[] args) noinlining
{
.entrypoint
.locals init (
	class D`1<class A> V_0,
	class D`1<class B> V_1,
	class D`1<class B[]> V_2,
	class C V_3,
	class B V_4,
	class B[] V_5,
	int64 V_6,
	class I1 V_7,
	class I2 V_8,
	int64 V_9,
	class GI1`1<object> V_10,
	class GI1`1<object[]> V_11,
	class GI1`1<int64> V_12,
	class GI1`1<int64[]> V_13,
	class GI1`1<valuetype Point> V_14,
	class GI1`1<valuetype Point[]> V_15,
	class GI2`1<object> V_16,
	class GI2`1<object[]> V_17,
	class GI2`1<int64> V_18,
	class GI2`1<int64[]> V_19,
	class GI2`1<valuetype Point> V_20,
	class GI2`1<valuetype Point[]> V_21,
	int64 depth
	)

ldc.i8 9999999
stloc depth
newobj instance void class D`1<class A>::.ctor()
stloc 0
newobj instance void class D`1<class B>::.ctor()
stloc 1
newobj instance void class D`1<class B[]>::.ctor()
stloc 2
newobj instance void C::.ctor()
stloc 3
newobj instance void B::.ctor()
stloc V_4
ldc.i8 1
newarr B
stloc V_5

newobj instance void C1::.ctor()
stloc V_7
newobj instance void C2::.ctor()
stloc V_8

newobj instance void class GC1`1<object>::.ctor()
stloc V_10
newobj instance void class GC1`1<object[]>::.ctor()
stloc V_11
newobj instance void class GC1`1<int64>::.ctor()
stloc V_12
newobj instance void class GC1`1<int64[]>::.ctor()
stloc V_13
newobj instance void class GC1`1<valuetype Point>::.ctor()
stloc V_14
newobj instance void class GC1`1<valuetype Point[]>::.ctor()
stloc V_15
newobj instance void class GC2`1<object>::.ctor()
stloc V_16
newobj instance void class GC2`1<object[]>::.ctor()
stloc V_17
newobj instance void class GC2`1<int64>::.ctor()
stloc V_18
newobj instance void class GC2`1<int64[]>::.ctor()
stloc V_19
newobj instance void class GC2`1<valuetype Point>::.ctor()
stloc V_20
newobj instance void class GC2`1<valuetype Point[]>::.ctor()
stloc V_21

// test-split-prefix do not remove or edit this line

ldloc 0
ldloc V_4
ldloc depth
ldc.i8 0
callvirt instance class B class D`1<class A>::cast2(object, int64, int64)
call void C3::print(object)

ldloc 0
ldloc V_4
ldloc depth
ldc.i8 0
callvirt instance !!0 class D`1<class A>::cast3<class B>(object, int64, int64)
call void C3::print(object)

ldloc 0
ldloc V_5
ldloc depth
ldc.i8 0
conv.i8
callvirt instance !!0 class D`1<class A>::cast3<class B[]>(object, int64, int64)
call void C3::print(object)

ldloc 0
ldloc V_5
ldloc depth
ldc.i8 0
callvirt instance class B[] class D`1<class A>::cast4(object, int64, int64)
call void C3::print(object)

ldloc 0
ldloc V_5
ldloc depth
ldc.i8 0
callvirt instance !!0[] class D`1<class A>::cast5<class B>(object, int64, int64)
call void C3::print(object)

ldloc 1
ldloc V_4
ldloc depth
ldc.i8 0
callvirt instance !0 class D`1<class B>::cast6(object, int64, int64)
call void C3::print(object)

ldloc 1
ldloc V_4
ldloc depth
ldc.i8 0
callvirt instance !0 class D`1<class B>::cast7<class A>(object, int64, int64)
call void C3::print(object)

ldloc 2
ldloc V_5
ldloc depth
ldc.i8 0
callvirt instance !0 class D`1<class B[]>::cast7<class A[]>(object, int64, int64)
call void C3::print(object)

ldloc 1
ldloc V_5
ldloc depth
ldc.i8 0
callvirt instance !0[] class D`1<class B>::cast8(object, int64, int64)
call void C3::print(object)

ldloc 1
ldloc V_5
ldloc depth
ldc.i8 0
callvirt instance !0[] class D`1<class B>::cast9<class A>(object, int64, int64)
call void C3::print(object)

ldloc 3
ldloc V_4
ldloc depth
ldc.i8 0
callvirt instance class B C::cast2(object, int64, int64)
call void C3::print(object)

ldloc 3
ldloc V_4
ldloc depth
ldc.i8 0
callvirt instance !!0 C::cast3<class B>(object, int64, int64)
call void C3::print(object)

ldloc 3
ldloc V_5
ldloc depth
ldc.i8 0
callvirt instance !!0 C::cast3<class B[]>(object, int64, int64)
call void C3::print(object)

ldloc 3
ldloc V_5
ldloc depth
ldc.i8 0
callvirt instance class B[] C::cast4(object, int64, int64)
call void C3::print(object)

ldloc 3
ldloc V_5
ldloc depth
ldc.i8 0
callvirt instance !!0[] C::cast5<class B>(object, int64, int64)
call void C3::print(object)

ldloc V_7
ldloc V_8
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void I1::F1(class I2, int64, int64, int64)

ldloc V_7
ldloc V_8
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void I1::GF1<object>(class I2, int64, int64, int64)

ldloc V_7
ldloc V_8
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void I1::GF1<class A>(class I2, int64, int64, int64)

ldloc V_7
ldloc V_8
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void I1::GF1<class A[]>(class I2, int64, int64, int64)

ldloc V_7
ldloc V_8
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void I1::GF1<int64>(class I2, int64, int64, int64)

ldloc V_7
ldloc V_8
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void I1::GF1<int64[]>(class I2, int64, int64, int64)

// This is the only failure on desktop.
ldloc V_10
ldloc V_16
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<object>::F1(class GI2`1<!0>, int64, int64, int64)

ldloc V_10
ldloc V_16
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<object>::GF1<object>(class GI2`1<!0>, int64, int64, int64)

ldloc V_10
ldloc V_16
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<object>::GF1<class A>(class GI2`1<!0>, int64, int64, int64)

ldloc V_10
ldloc V_16
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<object>::GF1<class A[]>(class GI2`1<!0>, int64, int64, int64)

ldloc V_10
ldloc V_16
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<object>::GF1<int64>(class GI2`1<!0>, int64, int64, int64)

ldloc V_10
ldloc V_16
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<object>::GF1<int64[]>(class GI2`1<!0>, int64, int64, int64)

ldloc V_11
ldloc V_17
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<object[]>::GF1<object[]>(class GI2`1<!0>, int64, int64, int64)

ldloc V_11
ldloc V_17
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<object[]>::GF1<class A>(class GI2`1<!0>, int64, int64, int64)

ldloc V_11
ldloc V_17
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<object[]>::GF1<class A[]>(class GI2`1<!0>, int64, int64, int64)

ldloc V_11
ldloc V_17
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<object[]>::GF1<int64>(class GI2`1<!0>, int64, int64, int64)

ldloc V_11
ldloc V_17
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<object[]>::GF1<int64[]>(class GI2`1<!0>, int64, int64, int64)

ldloc V_12
ldloc V_18
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<int64>::GF1<object>(class GI2`1<!0>, int64, int64, int64)

ldloc V_12
ldloc V_18
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<int64>::GF1<class A>(class GI2`1<!0>, int64, int64, int64)

ldloc V_12
ldloc V_18
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<int64>::GF1<class A[]>(class GI2`1<!0>, int64, int64, int64)

ldloc V_12
ldloc V_18
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<int64>::GF1<int64>(class GI2`1<!0>, int64, int64, int64)

ldloc V_12
ldloc V_18
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<int64>::GF1<int64[]>(class GI2`1<!0>, int64, int64, int64)

ldloc V_13
ldloc V_19
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<int64[]>::GF1<object>(class GI2`1<!0>, int64, int64, int64)

ldloc V_13
ldloc V_19
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<int64[]>::GF1<class A>(class GI2`1<!0>, int64, int64, int64)

ldloc V_13
ldloc V_19
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<int64[]>::GF1<class A[]>(class GI2`1<!0>, int64, int64, int64)

ldloc V_13
ldloc V_19
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<int64[]>::GF1<int64>(class GI2`1<!0>, int64, int64, int64)

ldloc V_13
ldloc V_19
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<int64[]>::GF1<int64[]>(class GI2`1<!0>, int64, int64, int64)

ldloc V_14
ldloc V_20
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<valuetype Point>::GF1<object>(class GI2`1<!0>, int64, int64, int64)

ldloc V_14
ldloc V_20
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<valuetype Point>::GF1<class A>(class GI2`1<!0>, int64, int64, int64)

ldloc V_14
ldloc V_20
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<valuetype Point>::GF1<class A[]>(class GI2`1<!0>, int64, int64, int64)

ldloc V_14
ldloc V_20
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<valuetype Point>::GF1<int64>(class GI2`1<!0>, int64, int64, int64)

ldloc V_14
ldloc V_20
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<valuetype Point>::GF1<int64[]>(class GI2`1<!0>, int64, int64, int64)

ldloc V_15
ldloc V_21
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<valuetype Point[]>::GF1<object>(class GI2`1<!0>, int64, int64, int64)

ldloc V_15
ldloc V_21
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<valuetype Point[]>::GF1<class A>(class GI2`1<!0>, int64, int64, int64)

ldloc V_15
ldloc V_21
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<valuetype Point[]>::GF1<class A[]>(class GI2`1<!0>, int64, int64, int64)

ldloc V_15
ldloc V_21
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<valuetype Point[]>::GF1<int64>(class GI2`1<!0>, int64, int64, int64)

ldloc V_15
ldloc V_21
ldloc depth
ldloca V_6
conv.u
conv.u8
ldc.i8 0
callvirt instance void class GI1`1<valuetype Point[]>::GF1<int64[]>(class GI2`1<!0>, int64, int64, int64)

// test-split-suffix do not remove or edit this line

ldsfld int64 C1::errors
dup
stloc V_9
brfalse IL_0591

ldstr "failure {0}"
br IL_0596

IL_0591:
ldstr "success"

IL_0596:
ldloc V_9
box [mscorlib]System.Int64
call void [mscorlib]System.Console::WriteLine(string, object)
ldloc V_9
conv.i4
call void [mscorlib]System.Environment::Exit(int32)
ret
}

}
