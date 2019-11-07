// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
.assembly Types680 {}
.assembly extern mscorlib {}

.assembly extern CustomTypes {}

.class interface public abstract I0<T0,T1,class .ctor ([CustomTypes]C_I) T2,T3>

{
   .method public abstract  virtual instance int32 M0()
   {
   }
   .method public abstract  virtual instance int32 M1<M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3,valuetype ([CustomTypes]Iface)M4>()
   {
   }
}
.class public C1<T0,T1,class .ctor T2,T3>
extends [mscorlib]System.Object
implements class I0<!T3,!T3,class [CustomTypes]C_I,!T0>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void  [mscorlib]System.Object::.ctor() 
      ret
   }
   .method public virtual instance int32 M0()
   {
      ldc.i4 2
      ret
   }
   .method public virtual instance int32 M1<M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3,valuetype ([CustomTypes]Iface)M4>()
   {
      ldc.i4 3
      ret
   }
}
.class public C2
extends class C1<int32,string,class [mscorlib]System.Object,class [mscorlib]System.Object>
implements class I0<class [mscorlib]System.Object,string,class [CustomTypes]C_I,class [CustomTypes]C>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C1<int32,string,class [mscorlib]System.Object,class [mscorlib]System.Object>::.ctor() 
      ret
   }
}
.class interface public abstract I3
implements class I0<valuetype [CustomTypes]S,class [CustomTypes]C,class [CustomTypes]C_I,int32>
{
   .method public abstract  virtual instance int32 M2<valuetype ([CustomTypes]Iface)M0,valuetype ([CustomTypes]Iface)M1,M2>()
   {
   }
   .method public abstract  virtual instance int32 M3<valuetype .ctor ([CustomTypes]Iface) M0>()
   {
   }
   .method public abstract  virtual instance int32 M4<M0>()
   {
   }
   .method public abstract  virtual instance int32 M5()
   {
   }
   .method public abstract  virtual instance int32 M6()
   {
   }
}
.class public C4<T0>
extends [mscorlib]System.Object
implements class I0<!T0,!T0,class [CustomTypes]C_I,!T0>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void  [mscorlib]System.Object::.ctor() 
      ret
   }
   .method public virtual instance int32 M0()
   {
      ldc.i4 9
      ret
   }
   .method public virtual instance int32 M1<M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3,valuetype ([CustomTypes]Iface)M4>()
   {
      ldc.i4 10
      ret
   }
}
.class public C5
extends C2
implements class I0<int32,class [CustomTypes]C,class [CustomTypes]C_I,class [mscorlib]System.Object>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void C2::.ctor() 
      ret
   }
}
.class public C6<T0>
extends class C1<!T0,!T0,class [CustomTypes]GenClassWithCtor<int32>,!T0>

{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C1<!T0,!T0,class [CustomTypes]GenClassWithCtor<int32>,!T0>::.ctor() 
      ret
   }
}
.class interface public abstract I7<T0,.ctor T1>

{
   .method public abstract  virtual instance int32 M7()
   {
   }
   .method public abstract  virtual instance int32 M8<.ctor ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1,class ([CustomTypes]C_I, [CustomTypes]Iface) M2,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M3>()
   {
   }
   .method public abstract  virtual instance int32 M9<class .ctor ([CustomTypes]C_I) M0,M1,M2,M3,M4>()
   {
   }
}
.class public C8<T0,T1>
extends C5

{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void C5::.ctor() 
      ret
   }
}
.class interface public abstract I9
implements class I0<class [mscorlib]System.Object,valuetype [CustomTypes]S,class [CustomTypes]C_I,class [mscorlib]System.Object>
{
   .method public abstract  virtual instance int32 M10()
   {
   }
   .method public abstract  virtual instance int32 M11<M0,M1,class .ctor ([CustomTypes]C_I) M2,M3>()
   {
   }
   .method public abstract  virtual instance int32 M12<M0,class ([CustomTypes]C_I, [CustomTypes]Iface) M1>()
   {
   }
   .method public abstract  virtual instance int32 M13()
   {
   }
}
.class interface public abstract I10<T0,.ctor T1>
implements class I7<!T0,!T1>
{
   .method public abstract  virtual instance int32 M14<class M0,.ctor ([CustomTypes]C_I) M1,class ([CustomTypes]Iface) M2>()
   {
   }
   .method public abstract  virtual instance int32 M15()
   {
   }
   .method public abstract  virtual instance int32 M16()
   {
   }
   .method public abstract  virtual instance int32 M17<M0,class M1,M2,M3>()
   {
   }
}
.class interface public abstract I11

{
   .method public abstract  virtual instance int32 M18()
   {
   }
   .method public abstract  virtual instance int32 M19<M0>()
   {
   }
   .method public abstract  virtual instance int32 M20<class .ctor ([CustomTypes]Iface) M0>()
   {
   }
}
.class public C12
extends [mscorlib]System.Object
implements class I7<class [CustomTypes]C,valuetype [CustomTypes]GenStruct<int32>>, 
           class I10<class [mscorlib]System.Object,valuetype [mscorlib]System.Nullable`1<int32>>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void  [mscorlib]System.Object::.ctor() 
      ret
   }
   .method public virtual instance int32 M7()
   {
      ldc.i4 25
      ret
   }
   .method public virtual instance int32 M8<.ctor ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1,class ([CustomTypes]C_I, [CustomTypes]Iface) M2,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M3>()
   {
      ldc.i4 26
      ret
   }
   .method public virtual instance int32 M9<class .ctor ([CustomTypes]C_I) M0,M1,M2,M3,M4>()
   {
      ldc.i4 27
      ret
   }
   .method public virtual instance int32 M14<class M0,.ctor ([CustomTypes]C_I) M1,class ([CustomTypes]Iface) M2>()
   {
      ldc.i4 28
      ret
   }
   .method public virtual instance int32 M15()
   {
      ldc.i4 29
      ret
   }
   .method public virtual instance int32 M16()
   {
      ldc.i4 30
      ret
   }
   .method public virtual instance int32 M17<M0,class M1,M2,M3>()
   {
      ldc.i4 31
      ret
   }
}
.class interface public abstract I13

{
   .method public abstract  virtual instance int32 M21<M0,class ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3>()
   {
   }
   .method public abstract  virtual instance int32 M22<M0,M1,class .ctor ([CustomTypes]C_I) M2,valuetype .ctor ([CustomTypes]Iface) M3,([CustomTypes]Iface) M4>()
   {
   }
   .method public abstract  virtual instance int32 M23()
   {
   }
}
.class public C14
extends class C4<int32>

{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C4<int32>::.ctor() 
      ret
   }
}
.class public C15
extends [mscorlib]System.Object

{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void  [mscorlib]System.Object::.ctor() 
      ret
   }
}
.class interface public abstract I16

{
   .method public abstract  virtual instance int32 M24<M0>()
   {
   }
   .method public abstract  virtual instance int32 M25<class .ctor ([CustomTypes]C_I) M0,M1,M2,M3,([CustomTypes]Iface) M4>()
   {
   }
   .method public abstract  virtual instance int32 M26()
   {
   }
}
.class public C17<T0,T1,valuetype .ctor ([CustomTypes]Iface) T2,T3>
extends class C1<!T3,!T0,class [mscorlib]System.Object,!T0>

{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C1<!T3,!T0,class [mscorlib]System.Object,!T0>::.ctor() 
      ret
   }
}
.class public C18<T0,.ctor T1>
extends [mscorlib]System.Object
implements class I7<!T0,!T1>, 
           I13, 
           I16, 
           I9
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void  [mscorlib]System.Object::.ctor() 
      ret
   }
   .method public virtual instance int32 M7()
   {
      ldc.i4 38
      ret
   }
   .method public virtual instance int32 M8<.ctor ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1,class ([CustomTypes]C_I, [CustomTypes]Iface) M2,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M3>()
   {
      ldc.i4 39
      ret
   }
   .method public virtual instance int32 M9<class .ctor ([CustomTypes]C_I) M0,M1,M2,M3,M4>()
   {
      ldc.i4 40
      ret
   }
   .method public virtual instance int32 M21<M0,class ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3>()
   {
      ldc.i4 41
      ret
   }
   .method public virtual instance int32 M22<M0,M1,class .ctor ([CustomTypes]C_I) M2,valuetype .ctor ([CustomTypes]Iface) M3,([CustomTypes]Iface) M4>()
   {
      ldc.i4 42
      ret
   }
   .method public virtual instance int32 M23()
   {
      ldc.i4 43
      ret
   }
   .method public virtual instance int32 M24<M0>()
   {
      ldc.i4 44
      ret
   }
   .method public virtual instance int32 M25<class .ctor ([CustomTypes]C_I) M0,M1,M2,M3,([CustomTypes]Iface) M4>()
   {
      ldc.i4 45
      ret
   }
   .method public virtual instance int32 M26()
   {
      ldc.i4 46
      ret
   }
   .method public virtual instance int32 M10()
   {
      ldc.i4 47
      ret
   }
   .method public virtual instance int32 M11<M0,M1,class .ctor ([CustomTypes]C_I) M2,M3>()
   {
      ldc.i4 48
      ret
   }
   .method public virtual instance int32 M12<M0,class ([CustomTypes]C_I, [CustomTypes]Iface) M1>()
   {
      ldc.i4 49
      ret
   }
   .method public virtual instance int32 M13()
   {
      ldc.i4 50
      ret
   }
   .method public virtual instance int32 M0()
   {
      ldc.i4 51
      ret
   }
   .method public virtual instance int32 M1<M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3,valuetype ([CustomTypes]Iface)M4>()
   {
      ldc.i4 52
      ret
   }
}
.class interface public abstract I19<T0,T1,class ([CustomTypes]C_I, [CustomTypes]Iface) T2,T3,T4>
implements I3, 
           class I7<!T4,class [CustomTypes]GenClassWithCtor<int32>>
{
   .method public abstract  virtual instance int32 M27()
   {
   }
   .method public abstract  virtual instance int32 M28<M0>()
   {
   }
   .method public abstract  virtual instance int32 M29<valuetype M0>()
   {
   }
   .method public abstract  virtual instance int32 M30<M0>()
   {
   }
}
.class interface public abstract I20<([CustomTypes]Iface) T0,(class [mscorlib]System.Exception[]) T1,T2>

{
   .method public abstract  virtual instance int32 M31<M0,valuetype M1,.ctor ([CustomTypes]Iface)M2,M3>()
   {
   }
   .method public abstract  virtual instance int32 M32<M0,M1,valuetype .ctor ([CustomTypes]Iface) M2>()
   {
   }
   .method public abstract  virtual instance int32 M33<class M0>()
   {
   }
}
.class interface public abstract I21<([CustomTypes]Iface) T0>
implements class I0<string,class [mscorlib]System.Object,class [CustomTypes]C_I,class [CustomTypes]C>, 
           I16, 
           class I7<class [mscorlib]System.Object,valuetype [CustomTypes]Enum1>, 
           I9, 
           I3
{
   .method public abstract  virtual instance int32 M34()
   {
   }
   .method public abstract  virtual instance int32 M35<M0,class .ctor ([CustomTypes]C_I) M1,M2>()
   {
   }
   .method public abstract  virtual instance int32 M36()
   {
   }
   .method public abstract  virtual instance int32 M37<.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M0,M1>()
   {
   }
}
.class public C22<T0,T1,T2>
extends [mscorlib]System.Object

{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void  [mscorlib]System.Object::.ctor() 
      ret
   }
}
.class public C23
extends class C6<class [mscorlib]System.Object>

{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C6<class [mscorlib]System.Object>::.ctor() 
      ret
   }
}
.class interface public abstract I24

{
   .method public abstract  virtual instance int32 M38()
   {
   }
   .method public abstract  virtual instance int32 M39()
   {
   }
   .method public abstract  virtual instance int32 M40<.ctor ([CustomTypes]C_I) M0>()
   {
   }
}
.class public C25<T0>
extends C23

{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void C23::.ctor() 
      ret
   }
}
.class interface public abstract I26<T0,T1>
implements class I7<!T1,valuetype [CustomTypes]Enum1>, 
           I11, 
           I13, 
           class I10<!T0,valuetype [CustomTypes]GenStruct<int32>>, 
           I16, 
           I3, 
           class I0<!T1,!T0,class [CustomTypes]C_I,!T1>, 
           class I20<class [CustomTypes]Iface,class [mscorlib]System.Exception[],!T0>
{
   .method public abstract  virtual instance int32 M41()
   {
   }
   .method public abstract  virtual instance int32 M42()
   {
   }
   .method public abstract  virtual instance int32 M43<M0,M1,M2,M3>()
   {
   }
   .method public abstract  virtual instance int32 M44<([CustomTypes]Iface) M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2>()
   {
   }
   .method public abstract  virtual instance int32 M45<([CustomTypes]C_I, [CustomTypes]Iface) M0,M1>()
   {
   }
}
.class interface public abstract I27<class .ctor ([CustomTypes]Iface) T0,class T1,([CustomTypes]C_I) T2,T3>

{
   .method public abstract  virtual instance int32 M46<M0,M1,M2>()
   {
   }
   .method public abstract  virtual instance int32 M47<([CustomTypes]Iface) M0>()
   {
   }
   .method public abstract  virtual instance int32 M48<M0,M1>()
   {
   }
   .method public abstract  virtual instance int32 M49<M0,M1>()
   {
   }
   .method public abstract  virtual instance int32 M50<M0,valuetype M1>()
   {
   }
}
.class public C28<valuetype .ctor ([CustomTypes]Iface) T0>
extends class C25<int32>
implements I16, 
           class I10<int32,class [mscorlib]System.Object>, 
           class I20<class [CustomTypes]Iface,class [mscorlib]System.Exception[],valuetype [CustomTypes]S>, 
           class I26<class [CustomTypes]C,string>, 
           I9
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C25<int32>::.ctor() 
      ret
   }
   .method public virtual instance int32 M24<M0>()
   {
      ldc.i4 77
      ret
   }
   .method public virtual instance int32 M25<class .ctor ([CustomTypes]C_I) M0,M1,M2,M3,([CustomTypes]Iface) M4>()
   {
      ldc.i4 78
      ret
   }
   .method public virtual instance int32 M26()
   {
      ldc.i4 79
      ret
   }
   .method public virtual instance int32 M14<class M0,.ctor ([CustomTypes]C_I) M1,class ([CustomTypes]Iface) M2>()
   {
      ldc.i4 80
      ret
   }
   .method public virtual instance int32 M15()
   {
      ldc.i4 81
      ret
   }
   .method public virtual instance int32 M16()
   {
      ldc.i4 82
      ret
   }
   .method public virtual instance int32 M17<M0,class M1,M2,M3>()
   {
      ldc.i4 83
      ret
   }
   .method public virtual instance int32 M31<M0,valuetype M1,.ctor ([CustomTypes]Iface)M2,M3>()
   {
      ldc.i4 84
      ret
   }
   .method public virtual instance int32 M32<M0,M1,valuetype .ctor ([CustomTypes]Iface) M2>()
   {
      ldc.i4 85
      ret
   }
   .method public virtual instance int32 M33<class M0>()
   {
      ldc.i4 86
      ret
   }
   .method public virtual instance int32 M41()
   {
      ldc.i4 87
      ret
   }
   .method public virtual instance int32 M42()
   {
      ldc.i4 88
      ret
   }
   .method public virtual instance int32 M43<M0,M1,M2,M3>()
   {
      ldc.i4 89
      ret
   }
   .method public virtual instance int32 M44<([CustomTypes]Iface) M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2>()
   {
      ldc.i4 90
      ret
   }
   .method public virtual instance int32 M45<([CustomTypes]C_I, [CustomTypes]Iface) M0,M1>()
   {
      ldc.i4 91
      ret
   }
   .method public virtual instance int32 M10()
   {
      ldc.i4 92
      ret
   }
   .method public virtual instance int32 M11<M0,M1,class .ctor ([CustomTypes]C_I) M2,M3>()
   {
      ldc.i4 93
      ret
   }
   .method public virtual instance int32 M12<M0,class ([CustomTypes]C_I, [CustomTypes]Iface) M1>()
   {
      ldc.i4 94
      ret
   }
   .method public virtual instance int32 M13()
   {
      ldc.i4 95
      ret
   }
   .method public virtual instance int32 M7()
   {
      ldc.i4 96
      ret
   }
   .method public virtual instance int32 M8<.ctor ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1,class ([CustomTypes]C_I, [CustomTypes]Iface) M2,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M3>()
   {
      ldc.i4 97
      ret
   }
   .method public virtual instance int32 M9<class .ctor ([CustomTypes]C_I) M0,M1,M2,M3,M4>()
   {
      ldc.i4 98
      ret
   }
   .method public virtual instance int32 M18()
   {
      ldc.i4 99
      ret
   }
   .method public virtual instance int32 M19<M0>()
   {
      ldc.i4 100
      ret
   }
   .method public virtual instance int32 M20<class .ctor ([CustomTypes]Iface) M0>()
   {
      ldc.i4 101
      ret
   }
   .method public virtual instance int32 M21<M0,class ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3>()
   {
      ldc.i4 102
      ret
   }
   .method public virtual instance int32 M22<M0,M1,class .ctor ([CustomTypes]C_I) M2,valuetype .ctor ([CustomTypes]Iface) M3,([CustomTypes]Iface) M4>()
   {
      ldc.i4 103
      ret
   }
   .method public virtual instance int32 M23()
   {
      ldc.i4 104
      ret
   }
   .method public virtual instance int32 M2<valuetype ([CustomTypes]Iface)M0,valuetype ([CustomTypes]Iface)M1,M2>()
   {
      ldc.i4 105
      ret
   }
   .method public virtual instance int32 M3<valuetype .ctor ([CustomTypes]Iface) M0>()
   {
      ldc.i4 106
      ret
   }
   .method public virtual instance int32 M4<M0>()
   {
      ldc.i4 107
      ret
   }
   .method public virtual instance int32 M5()
   {
      ldc.i4 108
      ret
   }
   .method public virtual instance int32 M6()
   {
      ldc.i4 109
      ret
   }
   .method public virtual instance int32 M0()
   {
      ldc.i4 110
      ret
   }
   .method public virtual instance int32 M1<M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3,valuetype ([CustomTypes]Iface)M4>()
   {
      ldc.i4 111
      ret
   }
}
.class interface public abstract I29
implements class I7<class [CustomTypes]C,valuetype [CustomTypes]GenStruct<int32>>, 
           I3, 
           class I26<valuetype [CustomTypes]S,class [CustomTypes]C>, 
           I13
{
   .method public abstract  virtual instance int32 M51()
   {
   }
   .method public abstract  virtual instance int32 M52()
   {
   }
   .method public abstract  virtual instance int32 M53<M0,.ctor ([CustomTypes]C_I) M1,M2,class ([CustomTypes]C_I) M3>()
   {
   }
   .method public abstract  virtual instance int32 M54<(class [mscorlib]System.Exception[]) M0,M1>()
   {
   }
   .method public abstract  virtual instance int32 M55()
   {
   }
}
.class public C30
extends [mscorlib]System.Object
implements I24, 
           I13, 
           class I21<class [CustomTypes]Iface>, 
           class I27<class [CustomTypes]C_I,class [CustomTypes]Delegate1,class [CustomTypes]C_I,class [mscorlib]System.Object>, 
           I3, 
           I29
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void  [mscorlib]System.Object::.ctor() 
      ret
   }
   .method public virtual instance int32 M38()
   {
      ldc.i4 117
      ret
   }
   .method public virtual instance int32 M39()
   {
      ldc.i4 118
      ret
   }
   .method public virtual instance int32 M40<.ctor ([CustomTypes]C_I) M0>()
   {
      ldc.i4 119
      ret
   }
   .method public virtual instance int32 M21<M0,class ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3>()
   {
      ldc.i4 120
      ret
   }
   .method public virtual instance int32 M22<M0,M1,class .ctor ([CustomTypes]C_I) M2,valuetype .ctor ([CustomTypes]Iface) M3,([CustomTypes]Iface) M4>()
   {
      ldc.i4 121
      ret
   }
   .method public virtual instance int32 M23()
   {
      ldc.i4 122
      ret
   }
   .method public virtual instance int32 M34()
   {
      ldc.i4 123
      ret
   }
   .method public virtual instance int32 M35<M0,class .ctor ([CustomTypes]C_I) M1,M2>()
   {
      ldc.i4 124
      ret
   }
   .method public virtual instance int32 M36()
   {
      ldc.i4 125
      ret
   }
   .method public virtual instance int32 M37<.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M0,M1>()
   {
      ldc.i4 126
      ret
   }
   .method public virtual instance int32 M46<M0,M1,M2>()
   {
      ldc.i4 127
      ret
   }
   .method public virtual instance int32 M47<([CustomTypes]Iface) M0>()
   {
      ldc.i4 128
      ret
   }
   .method public virtual instance int32 M48<M0,M1>()
   {
      ldc.i4 129
      ret
   }
   .method public virtual instance int32 M49<M0,M1>()
   {
      ldc.i4 130
      ret
   }
   .method public virtual instance int32 M50<M0,valuetype M1>()
   {
      ldc.i4 131
      ret
   }
   .method public virtual instance int32 M2<valuetype ([CustomTypes]Iface)M0,valuetype ([CustomTypes]Iface)M1,M2>()
   {
      ldc.i4 132
      ret
   }
   .method public virtual instance int32 M3<valuetype .ctor ([CustomTypes]Iface) M0>()
   {
      ldc.i4 133
      ret
   }
   .method public virtual instance int32 M4<M0>()
   {
      ldc.i4 134
      ret
   }
   .method public virtual instance int32 M5()
   {
      ldc.i4 135
      ret
   }
   .method public virtual instance int32 M6()
   {
      ldc.i4 136
      ret
   }
   .method public virtual instance int32 M51()
   {
      ldc.i4 137
      ret
   }
   .method public virtual instance int32 M52()
   {
      ldc.i4 138
      ret
   }
   .method public virtual instance int32 M53<M0,.ctor ([CustomTypes]C_I) M1,M2,class ([CustomTypes]C_I) M3>()
   {
      ldc.i4 139
      ret
   }
   .method public virtual instance int32 M54<(class [mscorlib]System.Exception[]) M0,M1>()
   {
      ldc.i4 140
      ret
   }
   .method public virtual instance int32 M55()
   {
      ldc.i4 141
      ret
   }
   .method public virtual instance int32 M0()
   {
      ldc.i4 142
      ret
   }
   .method public virtual instance int32 M1<M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3,valuetype ([CustomTypes]Iface)M4>()
   {
      ldc.i4 143
      ret
   }
   .method public virtual instance int32 M24<M0>()
   {
      ldc.i4 144
      ret
   }
   .method public virtual instance int32 M25<class .ctor ([CustomTypes]C_I) M0,M1,M2,M3,([CustomTypes]Iface) M4>()
   {
      ldc.i4 145
      ret
   }
   .method public virtual instance int32 M26()
   {
      ldc.i4 146
      ret
   }
   .method public virtual instance int32 M7()
   {
      ldc.i4 147
      ret
   }
   .method public virtual instance int32 M8<.ctor ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1,class ([CustomTypes]C_I, [CustomTypes]Iface) M2,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M3>()
   {
      ldc.i4 148
      ret
   }
   .method public virtual instance int32 M9<class .ctor ([CustomTypes]C_I) M0,M1,M2,M3,M4>()
   {
      ldc.i4 149
      ret
   }
   .method public virtual instance int32 M10()
   {
      ldc.i4 150
      ret
   }
   .method public virtual instance int32 M11<M0,M1,class .ctor ([CustomTypes]C_I) M2,M3>()
   {
      ldc.i4 151
      ret
   }
   .method public virtual instance int32 M12<M0,class ([CustomTypes]C_I, [CustomTypes]Iface) M1>()
   {
      ldc.i4 152
      ret
   }
   .method public virtual instance int32 M13()
   {
      ldc.i4 153
      ret
   }
   .method public virtual instance int32 M41()
   {
      ldc.i4 154
      ret
   }
   .method public virtual instance int32 M42()
   {
      ldc.i4 155
      ret
   }
   .method public virtual instance int32 M43<M0,M1,M2,M3>()
   {
      ldc.i4 156
      ret
   }
   .method public virtual instance int32 M44<([CustomTypes]Iface) M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2>()
   {
      ldc.i4 157
      ret
   }
   .method public virtual instance int32 M45<([CustomTypes]C_I, [CustomTypes]Iface) M0,M1>()
   {
      ldc.i4 158
      ret
   }
   .method public virtual instance int32 M18()
   {
      ldc.i4 159
      ret
   }
   .method public virtual instance int32 M19<M0>()
   {
      ldc.i4 160
      ret
   }
   .method public virtual instance int32 M20<class .ctor ([CustomTypes]Iface) M0>()
   {
      ldc.i4 161
      ret
   }
   .method public virtual instance int32 M14<class M0,.ctor ([CustomTypes]C_I) M1,class ([CustomTypes]Iface) M2>()
   {
      ldc.i4 162
      ret
   }
   .method public virtual instance int32 M15()
   {
      ldc.i4 163
      ret
   }
   .method public virtual instance int32 M16()
   {
      ldc.i4 164
      ret
   }
   .method public virtual instance int32 M17<M0,class M1,M2,M3>()
   {
      ldc.i4 165
      ret
   }
   .method public virtual instance int32 M31<M0,valuetype M1,.ctor ([CustomTypes]Iface)M2,M3>()
   {
      ldc.i4 166
      ret
   }
   .method public virtual instance int32 M32<M0,M1,valuetype .ctor ([CustomTypes]Iface) M2>()
   {
      ldc.i4 167
      ret
   }
   .method public virtual instance int32 M33<class M0>()
   {
      ldc.i4 168
      ret
   }
}
.class public C31
extends [mscorlib]System.Object
implements I13, 
           I3, 
           class I10<valuetype [CustomTypes]S,class [mscorlib]System.Object>, 
           I11, 
           class I27<class [CustomTypes]C_I,class [CustomTypes]ClassNoCtor,class [CustomTypes]C_I,valuetype [CustomTypes]S>, 
           class I21<class [CustomTypes]Iface>, 
           class I20<class [CustomTypes]Iface,class [mscorlib]System.Exception[],valuetype [CustomTypes]S>, 
           class I26<int32,valuetype [CustomTypes]S>, 
           I9
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void  [mscorlib]System.Object::.ctor() 
      ret
   }
   .method public virtual instance int32 M21<M0,class ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3>()
   {
      ldc.i4 169
      ret
   }
   .method public virtual instance int32 M22<M0,M1,class .ctor ([CustomTypes]C_I) M2,valuetype .ctor ([CustomTypes]Iface) M3,([CustomTypes]Iface) M4>()
   {
      ldc.i4 170
      ret
   }
   .method public virtual instance int32 M23()
   {
      ldc.i4 171
      ret
   }
   .method public virtual instance int32 M2<valuetype ([CustomTypes]Iface)M0,valuetype ([CustomTypes]Iface)M1,M2>()
   {
      ldc.i4 172
      ret
   }
   .method public virtual instance int32 M3<valuetype .ctor ([CustomTypes]Iface) M0>()
   {
      ldc.i4 173
      ret
   }
   .method public virtual instance int32 M4<M0>()
   {
      ldc.i4 174
      ret
   }
   .method public virtual instance int32 M5()
   {
      ldc.i4 175
      ret
   }
   .method public virtual instance int32 M6()
   {
      ldc.i4 176
      ret
   }
   .method public virtual instance int32 M14<class M0,.ctor ([CustomTypes]C_I) M1,class ([CustomTypes]Iface) M2>()
   {
      ldc.i4 177
      ret
   }
   .method public virtual instance int32 M15()
   {
      ldc.i4 178
      ret
   }
   .method public virtual instance int32 M16()
   {
      ldc.i4 179
      ret
   }
   .method public virtual instance int32 M17<M0,class M1,M2,M3>()
   {
      ldc.i4 180
      ret
   }
   .method public virtual instance int32 M18()
   {
      ldc.i4 181
      ret
   }
   .method public virtual instance int32 M19<M0>()
   {
      ldc.i4 182
      ret
   }
   .method public virtual instance int32 M20<class .ctor ([CustomTypes]Iface) M0>()
   {
      ldc.i4 183
      ret
   }
   .method public virtual instance int32 M46<M0,M1,M2>()
   {
      ldc.i4 184
      ret
   }
   .method public virtual instance int32 M47<([CustomTypes]Iface) M0>()
   {
      ldc.i4 185
      ret
   }
   .method public virtual instance int32 M48<M0,M1>()
   {
      ldc.i4 186
      ret
   }
   .method public virtual instance int32 M49<M0,M1>()
   {
      ldc.i4 187
      ret
   }
   .method public virtual instance int32 M50<M0,valuetype M1>()
   {
      ldc.i4 188
      ret
   }
   .method public virtual instance int32 M34()
   {
      ldc.i4 189
      ret
   }
   .method public virtual instance int32 M35<M0,class .ctor ([CustomTypes]C_I) M1,M2>()
   {
      ldc.i4 190
      ret
   }
   .method public virtual instance int32 M36()
   {
      ldc.i4 191
      ret
   }
   .method public virtual instance int32 M37<.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M0,M1>()
   {
      ldc.i4 192
      ret
   }
   .method public virtual instance int32 M31<M0,valuetype M1,.ctor ([CustomTypes]Iface)M2,M3>()
   {
      ldc.i4 193
      ret
   }
   .method public virtual instance int32 M32<M0,M1,valuetype .ctor ([CustomTypes]Iface) M2>()
   {
      ldc.i4 194
      ret
   }
   .method public virtual instance int32 M33<class M0>()
   {
      ldc.i4 195
      ret
   }
   .method public virtual instance int32 M41()
   {
      ldc.i4 196
      ret
   }
   .method public virtual instance int32 M42()
   {
      ldc.i4 197
      ret
   }
   .method public virtual instance int32 M43<M0,M1,M2,M3>()
   {
      ldc.i4 198
      ret
   }
   .method public virtual instance int32 M44<([CustomTypes]Iface) M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2>()
   {
      ldc.i4 199
      ret
   }
   .method public virtual instance int32 M45<([CustomTypes]C_I, [CustomTypes]Iface) M0,M1>()
   {
      ldc.i4 200
      ret
   }
   .method public virtual instance int32 M10()
   {
      ldc.i4 201
      ret
   }
   .method public virtual instance int32 M11<M0,M1,class .ctor ([CustomTypes]C_I) M2,M3>()
   {
      ldc.i4 202
      ret
   }
   .method public virtual instance int32 M12<M0,class ([CustomTypes]C_I, [CustomTypes]Iface) M1>()
   {
      ldc.i4 203
      ret
   }
   .method public virtual instance int32 M13()
   {
      ldc.i4 204
      ret
   }
   .method public virtual instance int32 M0()
   {
      ldc.i4 205
      ret
   }
   .method public virtual instance int32 M1<M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3,valuetype ([CustomTypes]Iface)M4>()
   {
      ldc.i4 206
      ret
   }
   .method public virtual instance int32 M7()
   {
      ldc.i4 207
      ret
   }
   .method public virtual instance int32 M8<.ctor ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1,class ([CustomTypes]C_I, [CustomTypes]Iface) M2,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M3>()
   {
      ldc.i4 208
      ret
   }
   .method public virtual instance int32 M9<class .ctor ([CustomTypes]C_I) M0,M1,M2,M3,M4>()
   {
      ldc.i4 209
      ret
   }
   .method public virtual instance int32 M24<M0>()
   {
      ldc.i4 210
      ret
   }
   .method public virtual instance int32 M25<class .ctor ([CustomTypes]C_I) M0,M1,M2,M3,([CustomTypes]Iface) M4>()
   {
      ldc.i4 211
      ret
   }
   .method public virtual instance int32 M26()
   {
      ldc.i4 212
      ret
   }
}
.class interface public abstract I32
implements class I19<class [CustomTypes]C,valuetype [CustomTypes]S,class [CustomTypes]C_I,int32,string>
{
   .method public abstract  virtual instance int32 M56<.ctor M0,class ([CustomTypes]Iface) M1>()
   {
   }
}
.class public C33
extends class C28<valuetype [CustomTypes]S_I>
implements class I19<class [CustomTypes]C,class [mscorlib]System.Object,class [CustomTypes]C_I,int32,valuetype [CustomTypes]S>, 
           I13
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C28<valuetype [CustomTypes]S_I>::.ctor() 
      ret
   }
   .method public virtual instance int32 M27()
   {
      ldc.i4 214
      ret
   }
   .method public virtual instance int32 M28<M0>()
   {
      ldc.i4 215
      ret
   }
   .method public virtual instance int32 M29<valuetype M0>()
   {
      ldc.i4 216
      ret
   }
   .method public virtual instance int32 M30<M0>()
   {
      ldc.i4 217
      ret
   }
   .method public virtual instance int32 M21<M0,class ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3>()
   {
      ldc.i4 218
      ret
   }
   .method public virtual instance int32 M22<M0,M1,class .ctor ([CustomTypes]C_I) M2,valuetype .ctor ([CustomTypes]Iface) M3,([CustomTypes]Iface) M4>()
   {
      ldc.i4 219
      ret
   }
   .method public virtual instance int32 M23()
   {
      ldc.i4 220
      ret
   }
   .method public virtual instance int32 M2<valuetype ([CustomTypes]Iface)M0,valuetype ([CustomTypes]Iface)M1,M2>()
   {
      ldc.i4 221
      ret
   }
   .method public virtual instance int32 M3<valuetype .ctor ([CustomTypes]Iface) M0>()
   {
      ldc.i4 222
      ret
   }
   .method public virtual instance int32 M4<M0>()
   {
      ldc.i4 223
      ret
   }
   .method public virtual instance int32 M5()
   {
      ldc.i4 224
      ret
   }
   .method public virtual instance int32 M6()
   {
      ldc.i4 225
      ret
   }
   .method public virtual instance int32 M0()
   {
      ldc.i4 226
      ret
   }
   .method public virtual instance int32 M1<M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3,valuetype ([CustomTypes]Iface)M4>()
   {
      ldc.i4 227
      ret
   }
   .method public virtual instance int32 M7()
   {
      ldc.i4 228
      ret
   }
   .method public virtual instance int32 M8<.ctor ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1,class ([CustomTypes]C_I, [CustomTypes]Iface) M2,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M3>()
   {
      ldc.i4 229
      ret
   }
   .method public virtual instance int32 M9<class .ctor ([CustomTypes]C_I) M0,M1,M2,M3,M4>()
   {
      ldc.i4 230
      ret
   }
}
.class public C34<class .ctor ([CustomTypes]C_I) T0,.ctor ([CustomTypes]C_I) T1>
extends [mscorlib]System.Object
implements I9, 
           I16, 
           I11, 
           class I19<valuetype [CustomTypes]S,int32,class [CustomTypes]C_I,string,string>, 
           I3, 
           I24, 
           I29, 
           class I20<class [CustomTypes]Iface,class [mscorlib]System.Exception[],valuetype [CustomTypes]S>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void  [mscorlib]System.Object::.ctor() 
      ret
   }
   .method public virtual instance int32 M10()
   {
      ldc.i4 231
      ret
   }
   .method public virtual instance int32 M11<M0,M1,class .ctor ([CustomTypes]C_I) M2,M3>()
   {
      ldc.i4 232
      ret
   }
   .method public virtual instance int32 M12<M0,class ([CustomTypes]C_I, [CustomTypes]Iface) M1>()
   {
      ldc.i4 233
      ret
   }
   .method public virtual instance int32 M13()
   {
      ldc.i4 234
      ret
   }
   .method public virtual instance int32 M24<M0>()
   {
      ldc.i4 235
      ret
   }
   .method public virtual instance int32 M25<class .ctor ([CustomTypes]C_I) M0,M1,M2,M3,([CustomTypes]Iface) M4>()
   {
      ldc.i4 236
      ret
   }
   .method public virtual instance int32 M26()
   {
      ldc.i4 237
      ret
   }
   .method public virtual instance int32 M18()
   {
      ldc.i4 238
      ret
   }
   .method public virtual instance int32 M19<M0>()
   {
      ldc.i4 239
      ret
   }
   .method public virtual instance int32 M20<class .ctor ([CustomTypes]Iface) M0>()
   {
      ldc.i4 240
      ret
   }
   .method public virtual instance int32 M27()
   {
      ldc.i4 241
      ret
   }
   .method public virtual instance int32 M28<M0>()
   {
      ldc.i4 242
      ret
   }
   .method public virtual instance int32 M29<valuetype M0>()
   {
      ldc.i4 243
      ret
   }
   .method public virtual instance int32 M30<M0>()
   {
      ldc.i4 244
      ret
   }
   .method public virtual instance int32 M2<valuetype ([CustomTypes]Iface)M0,valuetype ([CustomTypes]Iface)M1,M2>()
   {
      ldc.i4 245
      ret
   }
   .method public virtual instance int32 M3<valuetype .ctor ([CustomTypes]Iface) M0>()
   {
      ldc.i4 246
      ret
   }
   .method public virtual instance int32 M4<M0>()
   {
      ldc.i4 247
      ret
   }
   .method public virtual instance int32 M5()
   {
      ldc.i4 248
      ret
   }
   .method public virtual instance int32 M6()
   {
      ldc.i4 249
      ret
   }
   .method public virtual instance int32 M38()
   {
      ldc.i4 250
      ret
   }
   .method public virtual instance int32 M39()
   {
      ldc.i4 251
      ret
   }
   .method public virtual instance int32 M40<.ctor ([CustomTypes]C_I) M0>()
   {
      ldc.i4 252
      ret
   }
   .method public virtual instance int32 M51()
   {
      ldc.i4 253
      ret
   }
   .method public virtual instance int32 M52()
   {
      ldc.i4 254
      ret
   }
   .method public virtual instance int32 M53<M0,.ctor ([CustomTypes]C_I) M1,M2,class ([CustomTypes]C_I) M3>()
   {
      ldc.i4 255
      ret
   }
   .method public virtual instance int32 M54<(class [mscorlib]System.Exception[]) M0,M1>()
   {
      ldc.i4 256
      ret
   }
   .method public virtual instance int32 M55()
   {
      ldc.i4 257
      ret
   }
   .method public virtual instance int32 M31<M0,valuetype M1,.ctor ([CustomTypes]Iface)M2,M3>()
   {
      ldc.i4 258
      ret
   }
   .method public virtual instance int32 M32<M0,M1,valuetype .ctor ([CustomTypes]Iface) M2>()
   {
      ldc.i4 259
      ret
   }
   .method public virtual instance int32 M33<class M0>()
   {
      ldc.i4 260
      ret
   }
   .method public virtual instance int32 M0()
   {
      ldc.i4 261
      ret
   }
   .method public virtual instance int32 M1<M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3,valuetype ([CustomTypes]Iface)M4>()
   {
      ldc.i4 262
      ret
   }
   .method public virtual instance int32 M7()
   {
      ldc.i4 263
      ret
   }
   .method public virtual instance int32 M8<.ctor ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1,class ([CustomTypes]C_I, [CustomTypes]Iface) M2,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M3>()
   {
      ldc.i4 264
      ret
   }
   .method public virtual instance int32 M9<class .ctor ([CustomTypes]C_I) M0,M1,M2,M3,M4>()
   {
      ldc.i4 265
      ret
   }
   .method public virtual instance int32 M41()
   {
      ldc.i4 266
      ret
   }
   .method public virtual instance int32 M42()
   {
      ldc.i4 267
      ret
   }
   .method public virtual instance int32 M43<M0,M1,M2,M3>()
   {
      ldc.i4 268
      ret
   }
   .method public virtual instance int32 M44<([CustomTypes]Iface) M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2>()
   {
      ldc.i4 269
      ret
   }
   .method public virtual instance int32 M45<([CustomTypes]C_I, [CustomTypes]Iface) M0,M1>()
   {
      ldc.i4 270
      ret
   }
   .method public virtual instance int32 M21<M0,class ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3>()
   {
      ldc.i4 271
      ret
   }
   .method public virtual instance int32 M22<M0,M1,class .ctor ([CustomTypes]C_I) M2,valuetype .ctor ([CustomTypes]Iface) M3,([CustomTypes]Iface) M4>()
   {
      ldc.i4 272
      ret
   }
   .method public virtual instance int32 M23()
   {
      ldc.i4 273
      ret
   }
   .method public virtual instance int32 M14<class M0,.ctor ([CustomTypes]C_I) M1,class ([CustomTypes]Iface) M2>()
   {
      ldc.i4 274
      ret
   }
   .method public virtual instance int32 M15()
   {
      ldc.i4 275
      ret
   }
   .method public virtual instance int32 M16()
   {
      ldc.i4 276
      ret
   }
   .method public virtual instance int32 M17<M0,class M1,M2,M3>()
   {
      ldc.i4 277
      ret
   }
}
.class public C35<T0,T1,class .ctor ([CustomTypes]C_I) T2>
extends C12

{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void C12::.ctor() 
      ret
   }
}
.class interface public abstract I36

{
   .method public abstract  virtual instance int32 M57<class ([CustomTypes]C_I, [CustomTypes]Iface) M0,M1,M2,M3,class ([CustomTypes]C_I) M4>()
   {
   }
   .method public abstract  virtual instance int32 M58()
   {
   }
   .method public abstract  virtual instance int32 M59()
   {
   }
   .method public abstract  virtual instance int32 M60<M0,class M1>()
   {
   }
}
