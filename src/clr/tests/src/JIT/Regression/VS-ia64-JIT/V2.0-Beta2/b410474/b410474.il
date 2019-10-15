// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib{}
.assembly b410474{}
.class public A{.method static void V1() {.maxstack  50
ldsfld int8 [b410474]A::a0100
ldc.i4 0
beq a0101
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a0101:
ldsfld int8 [b410474]A::a0101
ldc.i4 1
beq a0102
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a0102:
ldsfld float32 [b410474]A::a0102
ldc.r4 2.0
beq a0103
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a0103:
ldsfld int16 [b410474]A::a0103
ldc.i4 3
beq a0104
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a0104:
ldsfld float32 [b410474]A::a0104
ldc.r4 4.0
beq a0105
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a0105:
ret}
.method static void V2() {.maxstack  50
ldsflda int8 [b410474]A::a0100
ldind.i1
ldc.i4 0
beq a0100
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a0100:
ldsflda int8 [b410474]A::a0101
ldind.i1
ldc.i4 1
beq a0101
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a0101:
ldsflda float32 [b410474]A::a0102
ldind.r4
ldc.r4 2.0
beq a0102
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a0102:
ldsflda int16 [b410474]A::a0103
ldind.i2
ldc.i4 3
beq a0103
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a0103:
ldsflda float32 [b410474]A::a0104
ldind.r4
ldc.r4 4.0
beq a0104
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a0104:
ret}
.method static void V3() {.maxstack  50
ldsfld float32 [b410474]A::a0102
ldc.r4 2.0
beq a0106
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a0106:
ldsfld int8 [b410474]A::a0101
ldc.i4 1
beq a0107
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a0107:
ldsfld float32 [b410474]A::a0104
ldc.r4 4.0
beq a0108
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a0108:
ldsfld int16 [b410474]A::a0103
ldc.i4 3
beq a0109
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a0109:
ldsfld int16 [b410474]A::a0103
ldc.i4 3
beq a01010
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a01010:
ret}
.method static void V4() {.maxstack  50
ldsflda int8 [b410474]A::a0100
conv.i8
dup
ldc.i8 0xffffffff00000000
and
ldc.i4 32
shr.un
conv.i8
ldc.i4 32
shl
or
conv.i
ldind.i1
ldc.i4 0
beq a0100
ldstr "a0100"
newobj instance void [mscorlib]System.Exception::.ctor(string)
throw
a0100:
ldsflda int8 [b410474]A::a0101
conv.r8
ldc.r8 234.098
add
conv.r8
ldc.r8 -234.098
add
conv.i
ldind.i1
ldc.i4 1
beq a0101
ldstr "a0101"
newobj instance void [mscorlib]System.Exception::.ctor(string)
throw
a0101:
ldsflda float32 [b410474]A::a0102
conv.i8
dup
dup
xor
xor
conv.i
ldind.r4
ldc.r4 2.0
beq a0102
ldstr "a0102"
newobj instance void [mscorlib]System.Exception::.ctor(string)
throw
a0102:
ldsflda int16 [b410474]A::a0103
conv.i8
ldc.i8 49048
add
conv.i8
ldc.i8 49048
sub
conv.i
ldind.i2
ldc.i4 3
beq a0103
ldstr "a0103"
newobj instance void [mscorlib]System.Exception::.ctor(string)
throw
a0103:
ldsflda float32 [b410474]A::a0104
conv.i8
dup
dup
xor
xor
conv.i
ldind.r4
ldc.r4 4.0
beq a0104
ldstr "a0104"
newobj instance void [mscorlib]System.Exception::.ctor(string)
throw
a0104:
ret}
.method static void V5() {.maxstack  50
ldsfld int8 [b410474]A::a0100
ldc.i4 1
add
stsfld int8 [b410474]A::a0100
ldsfld int8 [b410474]A::a0100
ldc.i4 1
beq a0100
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a0100:
ldsfld int8 [b410474]A::a0101
ldc.i4 1
add
stsfld int8 [b410474]A::a0101
ldsfld int8 [b410474]A::a0101
ldc.i4 2
beq a0101
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a0101:
ldsfld float32 [b410474]A::a0102
ldc.r4 1
add
stsfld float32 [b410474]A::a0102
ldsfld float32 [b410474]A::a0102
ldc.r4 3.0
beq a0102
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a0102:
ldsfld int16 [b410474]A::a0103
ldc.i4 1
add
stsfld int16 [b410474]A::a0103
ldsfld int16 [b410474]A::a0103
ldc.i4 4
beq a0103
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a0103:
ldsfld float32 [b410474]A::a0104
ldc.r4 1
add
stsfld float32 [b410474]A::a0104
ldsfld float32 [b410474]A::a0104
ldc.r4 5.0
beq a0104
newobj instance void [mscorlib]System.Exception::.ctor()
throw
a0104:
ret}
.method static int32 Main(string[] args){.entrypoint .maxstack  5
call void [b410474]A::V1()
call void [b410474]A::V2()
call void [b410474]A::V3()
call void [b410474]A::V4()
call void [b410474]A::V5()

ldstr "Test SUCCESS"
call void [System.Console]System.Console::WriteLine(string)

ldc.i4 100
ret}
.field public static float32 a0102 at b0101
.field public static int8 a0100 at b0102
.field public static int8 a0101 at b0103
.field public static int16 a0103 at b0104
.field public static float32 a0104 at b0105
}
.data b0101 = float32(2.0)
.data b0102 = int8(0)
.data b0103 = int8(1)
.data b0104 = int16(3)
.data b0105 = float32(4.0)
