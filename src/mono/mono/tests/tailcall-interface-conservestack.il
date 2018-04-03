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

.method public newslot abstract virtual instance int32 F1(class I2 i2, int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
}

.method public newslot abstract virtual instance int32 GF1<TF>(class I2 i2, int32 counter, int64 initial_stack, int64 current_stack) noinlining
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

.method public newslot abstract virtual instance int32 F1(class GI2`1<!TC> i2, int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
}

.method public newslot abstract virtual instance int32 GF1<TF>(class GI2`1<!TC> i2, int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
}

.method public newslot abstract virtual instance int32 HF1<TF>(class GI2`1<!!TF> i2, int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
}
}

.class interface private abstract I2
{
.method public newslot abstract virtual instance void perturb_interface_offset1() { }

.method public newslot abstract virtual instance void perturb_interface_offset2() { }

.method public newslot abstract virtual instance void perturb_interface_offset3() { }

.method public newslot abstract virtual instance int32 F2(class I1 i1, int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
}

.method public newslot abstract virtual instance int32 GF2<TF>(class I1 i1, int32 counter, int64 initial_stack, int64 current_stack) noinlining
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

.method public newslot abstract virtual instance int32 F2(class GI1`1<!TC> i1, int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
}
.method public newslot abstract virtual instance int32 GF2<TF>(class GI1`1<!TC> i1, int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
}
.method public newslot abstract virtual instance int32 HF2<TF>(class GI1`1<!!TF> i1, int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
}
}

.class public C1 implements I1
{
.field private static int32 i
.field public static int32 errors

.method private newslot virtual final instance void I1.perturb_interface_offset1()
{
.override I1::perturb_interface_offset1
ret
}

.method public static int32 check(int64 stack1, int64 stack2)
{
.locals init (int32 V_0)
ldsfld int32 C1::i
ldc.i4.1
add
stsfld int32 C1::i
ldarg.0
ldarg.1
bne.un.s IL_0013
ldc.i4.0
br.s IL_0014
IL_0013:
ldc.i4.1
IL_0014:
stloc.0
ldloc.0
brtrue.s IL_001a
ldc.i4.0
ret

IL_001a:
ldsfld int32 C1::errors
ldc.i4.1
add
stsfld int32 C1::errors
ldstr "{0} tailcall failure"
ldsfld int32 C1::i
box [mscorlib]System.Int32
call void [mscorlib]System.Console::WriteLine(string, object)
ldloc.0
ret
}
.method private newslot virtual final instance int32 I1.F1(class I2 i2, int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override I1::F1
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0014
ldarg.1
ldarg.0
ldarg.2
ldc.i4.1
sub
ldarg.3
ldloca.s V_0
conv.u
conv.u8
tail.
callvirt instance int32 I2::F2(class I1, int32, int64, int64)
ret
IL_0014:
ldloca.s V_0
conv.u
conv.u8
ldarg.s current_stack
tail.
call int32 C1::check(int64, int64)
ret
}
.method private newslot virtual final instance int32 I1.GF1<TF>(class I2 i2, int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override I1::GF1
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0014
ldarg.1
ldarg.0
ldarg.2
ldc.i4.1
sub
ldarg.3
ldloca.s V_0
conv.u
conv.u8
tail.
callvirt instance int32 I2::GF2<!!0>(class I1, int32, int64, int64)
ret
IL_0014:
ldloca.s V_0
conv.u
conv.u8
ldarg.s current_stack
tail.
call int32 C1::check(int64, int64)
ret
}
.method public instance void .ctor()
{
ldarg.0
tail.
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
.method public static int32 check(int64 stack1, int64 stack2)
{
ldarg.0
ldarg.1
tail.
call int32 C1::check(int64, int64)
ret
}
.method private newslot virtual final instance int32 'GI1<TC>.F1'(class GI2`1<!TC> i2, int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override method instance int32 class GI1`1<!TC>::F1(class GI2`1<!0>,
int32,
int64,
int64)
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0014
ldarg.1
ldarg.0
ldarg.2
ldc.i4.1
sub
ldarg.3
ldloca.s V_0
conv.u
conv.u8
tail.
callvirt instance int32 class GI2`1<!TC>::F2(class GI1`1<!0>, int32, int64, int64)
ret
IL_0014:
ldloca.s V_0
conv.u
conv.u8
ldarg.s current_stack
tail.
call int32 class GC1`1<!TC>::check(int64, int64)
ret
}
.method private newslot virtual final instance int32 'GI1<TC>.GF1'<TF>(class GI2`1<!TC> i2, int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override method instance int32 class GI1`1<!TC>::GF1<[1]>(class GI2`1<!0>,
int32,
int64,
int64)
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0014
ldarg.1
ldarg.0
ldarg.2
ldc.i4.1
sub
ldarg.3
ldloca.s V_0
conv.u
conv.u8
tail.
callvirt instance int32 class GI2`1<!TC>::GF2<!!0>(class GI1`1<!0>, int32, int64, int64)
ret
IL_0014:
ldloca.s V_0
conv.u
conv.u8
ldarg.s current_stack
tail.
call int32 class GC1`1<!TC>::check(int64, int64)
ret
}
.method private newslot virtual final instance int32 'GI1<TC>.HF1'<TF>(class GI2`1<!!TF> i2, int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override method instance int32 class GI1`1<!TC>::HF1<[1]>(class GI2`1<!!TF>,
int32,
int64,
int64)
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0014
ldarg.1
ldarg.0
ldarg.2
ldc.i4.1
sub
ldarg.3
ldloca.s V_0
conv.u
conv.u8
tail.
callvirt instance int32 class GI2`1<!!0>::HF2<!0>(class GI1`1<!!0>, int32, int64, int64)
ret
IL_0014:
ldloca.s V_0
conv.u
conv.u8
ldarg.s current_stack
tail.
call int32 class GC1`1<!TC>::check(int64, int64)
ret
}
.method public instance void .ctor()
{
ldarg.0
tail.
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
.method private static int32 check(int64 stack1, int64 stack2)
{
ldarg.0
ldarg.1
tail.
call int32 C1::check(int64, int64)
ret
}
.method private newslot virtual final instance int32 I2.F2(class I1 i1, int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override I2::F2
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0014
ldarg.1
ldarg.0
ldarg.2
ldc.i4.1
sub
ldarg.3
ldloca.s V_0
conv.u
conv.u8
tail.
callvirt instance int32 I1::F1(class I2, int32, int64, int64)
ret
IL_0014:
ldloca.s V_0
conv.u
conv.u8
ldarg.s current_stack
tail.
call int32 C2::check(int64, int64)
ret
}
.method private newslot virtual final instance int32 I2.GF2<TF>(class I1 i1, int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override I2::GF2
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0014
ldarg.1
ldarg.0
ldarg.2
ldc.i4.1
sub
ldarg.3
ldloca.s V_0
conv.u
conv.u8
tail.
callvirt instance int32 I1::GF1<!!0>(class I2, int32, int64, int64)
ret
IL_0014:
ldloca.s V_0
conv.u
conv.u8
ldarg.s current_stack
tail.
call int32 C2::check(int64, int64)
ret
}
.method public instance void .ctor()
{
ldarg.0
tail.
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
.method public static int32 check(int64 stack1, int64 stack2)
{
ldarg.0
ldarg.1
tail.
call int32 C1::check(int64, int64)
ret
}
.method private newslot virtual final instance int32 'GI2<TC>.F2'(class GI1`1<!TC> i1, int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override method instance int32 class GI2`1<!TC>::F2(class GI1`1<!0>,
int32,
int64,
int64)
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0014
ldarg.1
ldarg.0
ldarg.2
ldc.i4.1
sub
ldarg.3
ldloca.s V_0
conv.u
conv.u8
tail.
callvirt instance int32 class GI1`1<!TC>::F1(class GI2`1<!0>, int32, int64, int64)
ret
IL_0014:
ldloca.s V_0
conv.u
conv.u8
ldarg.s current_stack
tail.
call int32 class GC2`1<!TC>::check(int64, int64)
ret
}
.method private newslot virtual final instance int32 'GI2<TC>.GF2'<TF>(class GI1`1<!TC> i1, int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override method instance int32 class GI2`1<!TC>::GF2<[1]>(class GI1`1<!0>,
int32,
int64,
int64)
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0014
ldarg.1
ldarg.0
ldarg.2
ldc.i4.1
sub
ldarg.3
ldloca.s V_0
conv.u
conv.u8
tail.
callvirt instance int32 class GI1`1<!TC>::GF1<!!0>(class GI2`1<!0>, int32, int64, int64)
ret
IL_0014:
ldloca.s V_0
conv.u
conv.u8
ldarg.s current_stack
tail.
call int32 class GC2`1<!TC>::check(int64, int64)
ret
}
.method private newslot virtual final instance int32 'GI2<TC>.HF2'<TF>(class GI1`1<!!TF> i1, int32 counter, int64 initial_stack, int64 current_stack) noinlining
{
.override method instance int32 class GI2`1<!TC>::HF2<[1]>(class GI1`1<!!TF>,
int32,
int64,
int64)
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0014
ldarg.1
ldarg.0
ldarg.2
ldc.i4.1
sub
ldarg.3
ldloca.s V_0
conv.u
conv.u8
tail.
callvirt instance int32 class GI1`1<!!0>::HF1<!0>(class GI2`1<!!0>, int32, int64, int64)
ret
IL_0014:
ldloca.s V_0
conv.u
conv.u8
ldarg.s current_stack
tail.
call int32 class GC2`1<!TC>::check(int64, int64)
ret
}
.method public instance void .ctor()
{
ldarg.0
tail.
call instance void [mscorlib]System.Object::.ctor()
ret
}
}
.class public A
{
.method public instance void .ctor()
{
ldarg.0
tail.
call instance void [mscorlib]System.Object::.ctor()
ret
}
}
.class public B
{
.method public instance void .ctor()
{
ldarg.0
tail.
call instance void [mscorlib]System.Object::.ctor()
ret
}
}
.class interface private abstract IC
{
.method public newslot abstract virtual instance !!T cast1<T>(object o, int32 counter, int64 stack) noinlining
{
}
.method public newslot abstract virtual instance class B cast2(object o, int32 counter, int64 stack) noinlining
{
}
.method public newslot abstract virtual instance !!T cast3<T>(object o, int32 counter, int64 stack) noinlining
{
}
.method public newslot abstract virtual instance class B[] cast4(object o, int32 counter, int64 stack) noinlining
{
}
.method public newslot abstract virtual instance !!T[] cast5<T>(object o, int32 counter, int64 stack) noinlining
{
}
}
.class public C
{
.method public static void check(int64 stack1, int64 stack2) noinlining
{
ldarg.0
ldarg.1
call int32 C1::check(int64, int64)
pop
ret
}
.method public instance !!T cast1<T>(object o, int32 counter, int64 stack) noinlining
{
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0013
ldarg.0
ldarg.1
ldarg.2
ldc.i4.1
sub
ldloca.s V_0
conv.u
conv.u8
tail.
call instance !!0 C::cast1<!!0>(object, int32, int64)
ret
IL_0013:
ldloca.s V_0
conv.u
conv.u8
ldarg.3
call void C::check(int64, int64)
ldarg.1
unbox.any !!T
ret
}
.method public instance class B cast2(object o, int32 counter, int64 stack) noinlining
{
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0013
ldarg.0
ldarg.1
ldarg.2
ldc.i4.1
sub
ldloca.s V_0
conv.u
conv.u8
tail.
call instance class B C::cast2(object, int32, int64)
ret
IL_0013:
ldloca.s V_0
conv.u
conv.u8
ldarg.3
call void C::check(int64, int64)
ldarg.0
ldarg.1
ldc.i4.s 100
ldc.i4.0
conv.i8
tail.
call instance !!0 C::cast1<class B>(object, int32, int64)
ret
}
.method public instance !!T cast3<T>(object o, int32 counter, int64 stack) noinlining
{
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0013
ldarg.0
ldarg.1
ldarg.2
ldc.i4.1
sub
ldloca.s V_0
conv.u
conv.u8
tail.
call instance !!0 C::cast3<!!0>(object, int32, int64)
ret
IL_0013:
ldloca.s V_0
conv.u
conv.u8
ldarg.3
call void C::check(int64, int64)
ldarg.0
ldarg.1
ldc.i4.s 100
ldc.i4.0
conv.i8
tail.
call instance !!0 C::cast1<!!0>(object, int32, int64)
ret
}
.method public instance class B[] cast4(object o, int32 counter, int64 stack) noinlining
{
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0013
ldarg.0
ldarg.1
ldarg.2
ldc.i4.1
sub
ldloca.s V_0
conv.u
conv.u8
tail.
call instance class B[] C::cast4(object, int32, int64)
ret
IL_0013:
ldloca.s V_0
conv.u
conv.u8
ldarg.3
call void C::check(int64, int64)
ldarg.0
ldarg.1
ldc.i4.s 100
ldc.i4.0
conv.i8
tail.
call instance !!0 C::cast1<class B[]>(object, int32, int64)
ret
}
.method public instance !!T[] cast5<T>(object o, int32 counter, int64 stack) noinlining
{
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0013
ldarg.0
ldarg.1
ldarg.2
ldc.i4.1
sub
ldloca.s V_0
conv.u
conv.u8
tail.
call instance !!0[] C::cast5<!!0>(object, int32, int64)
ret
IL_0013:
ldloca.s V_0
conv.u
conv.u8
ldarg.3
call void C::check(int64, int64)
ldarg.0
ldarg.1
ldc.i4.s 100
ldc.i4.0
conv.i8
tail.
call instance !!0 C::cast1<!!0[]>(object, int32, int64)
ret
}
.method public instance void .ctor()
{
ldarg.0
tail.
call instance void [mscorlib]System.Object::.ctor()
ret
}
}
.class public D`1<T1>
{
.method public static void check(int64 stack1, int64 stack2) noinlining
{
ldarg.0
ldarg.1
tail.
call void C::check(int64, int64)
ret
}
.method public static !!T cast1<T>(object o, int32 counter, int64 stack) noinlining
{
.locals init (int32 V_0)
ldarg.1
ldc.i4.0
ble.s IL_0012
ldarg.0
ldarg.1
ldc.i4.1
sub
ldloca.s V_0
conv.u
conv.u8
tail.
call !!0 class D`1<!T1>::cast1<!!0>(object, int32, int64)
ret
IL_0012:
ldloca.s V_0
conv.u
conv.u8
ldarg.2
call void class D`1<!T1>::check(int64, int64)
ldarg.0
unbox.any !!T
ret
}
.method public instance class B cast2(object o, int32 counter, int64 stack) noinlining
{
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0013
ldarg.0
ldarg.1
ldarg.2
ldc.i4.1
sub
ldloca.s V_0
conv.u
conv.u8
tail.
call instance class B class D`1<!T1>::cast2(object, int32, int64)
ret
IL_0013:
ldloca.s V_0
conv.u
conv.u8
ldarg.3
call void class D`1<!T1>::check(int64, int64)
ldarg.1
ldc.i4.s 100
ldc.i4.0
conv.i8
tail.
call !!0 class D`1<!T1>::cast1<class B>(object, int32, int64)
ret
}
.method public instance !!T cast3<T>(object o, int32 counter, int64 stack) noinlining
{
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0013
ldarg.0
ldarg.1
ldarg.2
ldc.i4.1
sub
ldloca.s V_0
conv.u
conv.u8
tail.
call instance !!0 class D`1<!T1>::cast3<!!0>(object, int32, int64)
ret
IL_0013:
ldloca.s V_0
conv.u
conv.u8
ldarg.3
call void class D`1<!T1>::check(int64, int64)
ldarg.1
ldc.i4.s 100
ldc.i4.0
conv.i8
tail.
call !!0 class D`1<!T1>::cast1<!!0>(object, int32, int64)
ret
}
.method public instance class B[] cast4(object o, int32 counter, int64 stack) noinlining
{
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0013
ldarg.0
ldarg.1
ldarg.2
ldc.i4.1
sub
ldloca.s V_0
conv.u
conv.u8
tail.
call instance class B[] class D`1<!T1>::cast4(object, int32, int64)
ret
IL_0013:
ldloca.s V_0
conv.u
conv.u8
ldarg.3
call void class D`1<!T1>::check(int64, int64)
ldarg.1
ldc.i4.s 100
ldc.i4.0
conv.i8
tail.
call !!0 class D`1<!T1>::cast1<class B[]>(object, int32, int64)
ret
}
.method public instance !!T[] cast5<T>(object o, int32 counter, int64 stack) noinlining
{
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0013
ldarg.0
ldarg.1
ldarg.2
ldc.i4.1
sub
ldloca.s V_0
conv.u
conv.u8
tail.
call instance !!0[] class D`1<!T1>::cast5<!!0>(object, int32, int64)
ret
IL_0013:
ldloca.s V_0
conv.u
conv.u8
ldarg.3
call void class D`1<!T1>::check(int64, int64)
ldarg.1
ldc.i4.s 100
ldc.i4.0
conv.i8
tail.
call !!0 class D`1<!T1>::cast1<!!0[]>(object, int32, int64)
ret
}
.method public instance !T1 cast6(object o, int32 counter, int64 stack) noinlining
{
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0013
ldarg.0
ldarg.1
ldarg.2
ldc.i4.1
sub
ldloca.s V_0
conv.u
conv.u8
tail.
call instance !0 class D`1<!T1>::cast6(object, int32, int64)
ret
IL_0013:
ldloca.s V_0
conv.u
conv.u8
ldarg.3
call void class D`1<!T1>::check(int64, int64)
ldarg.1
ldc.i4.s 100
ldc.i4.0
conv.i8
tail.
call !!0 class D`1<!T1>::cast1<!0>(object, int32, int64)
ret
}
.method public instance !T1 cast7<T>(object o, int32 counter, int64 stack) noinlining
{
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0013
ldarg.0
ldarg.1
ldarg.2
ldc.i4.1
sub
ldloca.s V_0
conv.u
conv.u8
tail.
call instance !0 class D`1<!T1>::cast7<!!0>(object, int32, int64)
ret
IL_0013:
ldloca.s V_0
conv.u
conv.u8
ldarg.3
call void class D`1<!T1>::check(int64, int64)
ldarg.1
ldc.i4.s 100
ldc.i4.0
conv.i8
tail.
call !!0 class D`1<!T1>::cast1<!0>(object, int32, int64)
ret
}
.method public instance !T1[] cast8(object o, int32 counter, int64 stack) noinlining
{
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0013
ldarg.0
ldarg.1
ldarg.2
ldc.i4.1
sub
ldloca.s V_0
conv.u
conv.u8
tail.
call instance !0[] class D`1<!T1>::cast8(object, int32, int64)
ret
IL_0013:
ldloca.s V_0
conv.u
conv.u8
ldarg.3
call void class D`1<!T1>::check(int64, int64)
ldarg.0
ldarg.1
ldc.i4.s 100
ldc.i4.0
conv.i8
tail.
call instance !!0 class D`1<!T1>::cast3<!0[]>(object, int32, int64)
ret
}
.method public instance !T1[] cast9<T>(object o, int32 counter, int64 stack) noinlining
{
.locals init (int32 V_0)
ldarg.2
ldc.i4.0
ble.s IL_0013
ldarg.0
ldarg.1
ldarg.2
ldc.i4.1
sub
ldloca.s V_0
conv.u
conv.u8
tail.
call instance !0[] class D`1<!T1>::cast9<!!0>(object, int32, int64)
ret
IL_0013:
ldloca.s V_0
conv.u
conv.u8
ldarg.3
call void class D`1<!T1>::check(int64, int64)
ldarg.0
ldarg.1
ldc.i4.s 100
ldc.i4.0
conv.i8
tail.
call instance !!0 class D`1<!T1>::cast3<!0[]>(object, int32, int64)
ret
}
.method public instance void .ctor()
{
ldarg.0
tail.
call instance void [mscorlib]System.Object::.ctor()
ret
}
}
.class private C3
{
.field private int32 i
.method private instance void print(object o) noinlining
{
ldarg.0
ldarg.0
ldfld int32 C3::i
ldc.i4.1
add
stfld int32 C3::i
ret
}
.method public instance void Main() noinlining
{
.locals init (class D`1<class A> V_0, class D`1<class B> V_1, class D`1<class B[]> V_2, class C V_3, class B V_4, class B[] V_5, int32 V_6, class I1 V_7, class I2 V_8, int32 V_9, class GI1`1<object> V_10, class GI1`1<object[]> V_11, class GI1`1<int32> V_12, class GI1`1<int32[]> V_13, class GI1`1<valuetype Point> V_14, class GI1`1<valuetype Point[]> V_15, class GI2`1<object> V_16, class GI2`1<object[]> V_17, class GI2`1<int32> V_18, class GI2`1<int32[]> V_19, class GI2`1<valuetype Point> V_20, class GI2`1<valuetype Point[]> V_21)
newobj instance void class D`1<class A>::.ctor()
stloc.0
newobj instance void class D`1<class B>::.ctor()
stloc.1
newobj instance void class D`1<class B[]>::.ctor()
stloc.2
newobj instance void C::.ctor()
stloc.3
newobj instance void B::.ctor()
stloc.s V_4
ldc.i4.1
newarr B
stloc.s V_5

newobj instance void C1::.ctor()
stloc.s V_7
newobj instance void C2::.ctor()
stloc.s V_8

newobj instance void class GC1`1<object>::.ctor()
stloc.s V_10
newobj instance void class GC1`1<object[]>::.ctor()
stloc.s V_11
newobj instance void class GC1`1<int32>::.ctor()
stloc.s V_12
newobj instance void class GC1`1<int32[]>::.ctor()
stloc.s V_13
newobj instance void class GC1`1<valuetype Point>::.ctor()
stloc.s V_14
newobj instance void class GC1`1<valuetype Point[]>::.ctor()
stloc.s V_15
newobj instance void class GC2`1<object>::.ctor()
stloc.s V_16
newobj instance void class GC2`1<object[]>::.ctor()
stloc.s V_17
newobj instance void class GC2`1<int32>::.ctor()
stloc.s V_18
newobj instance void class GC2`1<int32[]>::.ctor()
stloc.s V_19
newobj instance void class GC2`1<valuetype Point>::.ctor()
stloc.s V_20
newobj instance void class GC2`1<valuetype Point[]>::.ctor()
stloc.s V_21

ldc.i4.0
stloc.s V_9

ldarg.0
ldloc.0
ldloc.s V_4
ldc.i4.s 100
ldc.i4.0
conv.i8
callvirt instance class B class D`1<class A>::cast2(object, int32, int64)
call instance void C3::print(object)

ldarg.0
ldloc.0
ldloc.s V_4
ldc.i4.s 100
ldc.i4.0
conv.i8
callvirt instance !!0 class D`1<class A>::cast3<class B>(object, int32, int64)
call instance void C3::print(object)

ldarg.0
ldloc.0
ldloc.s V_5
ldc.i4.s 100
ldc.i4.0
conv.i8
callvirt instance !!0 class D`1<class A>::cast3<class B[]>(object, int32, int64)
call instance void C3::print(object)

ldarg.0
ldloc.0
ldloc.s V_5
ldc.i4.s 100
ldc.i4.0
conv.i8
callvirt instance class B[] class D`1<class A>::cast4(object, int32, int64)
call instance void C3::print(object)

ldarg.0
ldloc.0
ldloc.s V_5
ldc.i4.s 100
ldc.i4.0
conv.i8
callvirt instance !!0[] class D`1<class A>::cast5<class B>(object, int32, int64)
call instance void C3::print(object)

ldarg.0
ldloc.1
ldloc.s V_4
ldc.i4.s 100
ldc.i4.0
conv.i8
callvirt instance !0 class D`1<class B>::cast6(object, int32, int64)
call instance void C3::print(object)

ldarg.0
ldloc.1
ldloc.s V_4
ldc.i4.s 100
ldc.i4.0
conv.i8
callvirt instance !0 class D`1<class B>::cast7<class A>(object, int32, int64)
call instance void C3::print(object)

ldarg.0
ldloc.2
ldloc.s V_5
ldc.i4.s 100
ldc.i4.0
conv.i8
callvirt instance !0 class D`1<class B[]>::cast7<class A[]>(object, int32, int64)
call instance void C3::print(object)

ldarg.0
ldloc.1
ldloc.s V_5
ldc.i4.s 100
ldc.i4.0
conv.i8
callvirt instance !0[] class D`1<class B>::cast8(object, int32, int64)
call instance void C3::print(object)

ldarg.0
ldloc.1
ldloc.s V_5
ldc.i4.s 100
ldc.i4.0
conv.i8
callvirt instance !0[] class D`1<class B>::cast9<class A>(object, int32, int64)
call instance void C3::print(object)

ldarg.0
ldloc.3
ldloc.s V_4
ldc.i4.s 100
ldc.i4.0
conv.i8
callvirt instance class B C::cast2(object, int32, int64)
call instance void C3::print(object)


ldc.i4.0


ldarg.0
ldloc.3
ldloc.s V_4
ldc.i4.s 100
ldc.i4.0
conv.i8
callvirt instance !!0 C::cast3<class B>(object, int32, int64)
call instance void C3::print(object)

ldarg.0
ldloc.3
ldloc.s V_5
ldc.i4.s 100
ldc.i4.0
conv.i8
callvirt instance !!0 C::cast3<class B[]>(object, int32, int64)
call instance void C3::print(object)

ldarg.0
ldloc.3
ldloc.s V_5
ldc.i4.s 100
ldc.i4.0
conv.i8
callvirt instance class B[] C::cast4(object, int32, int64)
call instance void C3::print(object)

ldarg.0
ldloc.3
ldloc.s V_5
ldc.i4.s 100
ldc.i4.0
conv.i8
callvirt instance !!0[] C::cast5<class B>(object, int32, int64)
call instance void C3::print(object)

ldloc.s V_7
ldloc.s V_8
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 I1::F1(class I2, int32, int64, int64)
add

ldloc.s V_7
ldloc.s V_8
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 I1::GF1<object>(class I2, int32, int64, int64)
add

ldloc.s V_7
ldloc.s V_8
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 I1::GF1<class A>(class I2, int32, int64, int64)
add

ldloc.s V_7
ldloc.s V_8
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 I1::GF1<class A[]>(class I2, int32, int64, int64)
add

ldloc.s V_7
ldloc.s V_8
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 I1::GF1<int32>(class I2, int32, int64, int64)
add

ldloc.s V_7
ldloc.s V_8
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 I1::GF1<int32[]>(class I2, int32, int64, int64)
add

// This is the only failure on desktop.
ldloc.s V_10
ldloc.s V_16
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<object>::F1(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_10
ldloc.s V_16
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<object>::GF1<object>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_10
ldloc.s V_16
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<object>::GF1<class A>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_10
ldloc.s V_16
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<object>::GF1<class A[]>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_10
ldloc.s V_16
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<object>::GF1<int32>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_10
ldloc.s V_16
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<object>::GF1<int32[]>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_11
ldloc.s V_17
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<object[]>::GF1<object[]>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_11
ldloc.s V_17
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<object[]>::GF1<class A>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_11
ldloc.s V_17
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<object[]>::GF1<class A[]>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_11
ldloc.s V_17
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<object[]>::GF1<int32>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_11
ldloc.s V_17
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<object[]>::GF1<int32[]>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_12
ldloc.s V_18
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<int32>::GF1<object>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_12
ldloc.s V_18
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<int32>::GF1<class A>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_12
ldloc.s V_18
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<int32>::GF1<class A[]>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_12
ldloc.s V_18
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<int32>::GF1<int32>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_12
ldloc.s V_18
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<int32>::GF1<int32[]>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_13
ldloc.s V_19
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<int32[]>::GF1<object>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_13
ldloc.s V_19
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<int32[]>::GF1<class A>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_13
ldloc.s V_19
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<int32[]>::GF1<class A[]>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_13
ldloc.s V_19
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<int32[]>::GF1<int32>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_13
ldloc.s V_19
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<int32[]>::GF1<int32[]>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_14
ldloc.s V_20
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<valuetype Point>::GF1<object>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_14
ldloc.s V_20
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<valuetype Point>::GF1<class A>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_14
ldloc.s V_20
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<valuetype Point>::GF1<class A[]>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_14
ldloc.s V_20
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<valuetype Point>::GF1<int32>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_14
ldloc.s V_20
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<valuetype Point>::GF1<int32[]>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_15
ldloc.s V_21
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<valuetype Point[]>::GF1<object>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_15
ldloc.s V_21
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<valuetype Point[]>::GF1<class A>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_15
ldloc.s V_21
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<valuetype Point[]>::GF1<class A[]>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_15
ldloc.s V_21
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<valuetype Point[]>::GF1<int32>(class GI2`1<!0>, int32, int64, int64)
add

ldloc.s V_15
ldloc.s V_21
ldc.i4.s 100
ldloca.s V_6
conv.u
conv.u8
ldc.i4.0
conv.i8
callvirt instance int32 class GI1`1<valuetype Point[]>::GF1<int32[]>(class GI2`1<!0>, int32, int64, int64)
add

ldsfld int32 C1::errors
add
dup
stloc.s V_9
brfalse.s IL_0591

ldstr "failure {0}"
br.s IL_0596

IL_0591:
ldstr "success"

IL_0596:
ldloc.s V_9
box [mscorlib]System.Int32
call void [mscorlib]System.Console::WriteLine(string, object)
ldloc.s V_9
tail.
call void [mscorlib]System.Environment::Exit(int32)
ret
}

.method public static void Main(string[] args) noinlining
{
.entrypoint
newobj instance void C3::.ctor()
tail.
call instance void C3::Main()
ret
}

.method public instance void .ctor()
{
ldarg.0
tail.
call instance void [mscorlib]System.Object::.ctor()
ret
}
}
