// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly Types101 {}
.assembly extern mscorlib {}

.assembly extern CustomTypes {}

.class interface public abstract I0<class .ctor ([CustomTypes]C_I) T0,T1>

{
   .method public abstract  virtual instance int32 M0()
   {
   }
   .method public abstract  virtual instance int32 M1<M0,valuetype .ctor ([CustomTypes]Iface) M1,class M2,M3,M4>()
   {
   }
   .method public abstract  virtual instance int32 M2<class .ctor ([CustomTypes]Iface) M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3,M4>()
   {
   }
}
.class public C1<valuetype T0,T1,class ([CustomTypes]C_I) T2>
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
.class public C2<([CustomTypes]C_I) T0,T1>
extends [mscorlib]System.Object
implements class I0<class [CustomTypes]C_I,!T1>
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
      ldc.i4 3
      ret
   }
   .method public virtual instance int32 M1<M0,valuetype .ctor ([CustomTypes]Iface) M1,class M2,M3,M4>()
   {
      ldc.i4 4
      ret
   }
   .method public virtual instance int32 M2<class .ctor ([CustomTypes]Iface) M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3,M4>()
   {
      ldc.i4 5
      ret
   }
}
.class public C3<class ([CustomTypes]C_I) T0,T1,T2,T3>
extends class C2<class [CustomTypes]C_I,!T3>
implements class I0<class [CustomTypes]C_I,!T3>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C2<class [CustomTypes]C_I,!T3>::.ctor() 
      ret
   }
}
.class public C4
extends [mscorlib]System.Object
implements class I0<class [CustomTypes]C_I,class [CustomTypes]C>
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
      ldc.i4 6
      ret
   }
   .method public virtual instance int32 M1<M0,valuetype .ctor ([CustomTypes]Iface) M1,class M2,M3,M4>()
   {
      ldc.i4 7
      ret
   }
   .method public virtual instance int32 M2<class .ctor ([CustomTypes]Iface) M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3,M4>()
   {
      ldc.i4 8
      ret
   }
}
.class public C5
extends class C2<class [CustomTypes]C_I,class [CustomTypes]C>

{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C2<class [CustomTypes]C_I,class [CustomTypes]C>::.ctor() 
      ret
   }
}
.class interface public abstract I6<class .ctor ([CustomTypes]Iface) T0,T1>
implements class I0<class [CustomTypes]C_I,!T1>
{
   .method public abstract  virtual instance int32 M3<([CustomTypes]C_I) M0,class .ctor ([CustomTypes]C_I) M1,M2,M3,class ([CustomTypes]Iface) M4>()
   {
   }
   .method public abstract  virtual instance int32 M4<M0,M1,.ctor ([CustomTypes]Iface)M2,([CustomTypes]C_I, [CustomTypes]Iface) M3>()
   {
   }
   .method public abstract  virtual instance int32 M5<valuetype .ctor M0,([CustomTypes]C_I, [CustomTypes]Iface) M1>()
   {
   }
   .method public abstract  virtual instance int32 M6()
   {
   }
   .method public abstract  virtual instance int32 M7()
   {
   }
}
.class public C7
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
.class interface public abstract I8<([CustomTypes]C_I) T0,T1,([CustomTypes]C_I, [CustomTypes]Iface) T2,T3,T4>
implements class I6<class [CustomTypes]C_I,!T1>
{
   .method public abstract  virtual instance int32 M8()
   {
   }
   .method public abstract  virtual instance int32 M9<M0>()
   {
   }
}
.class interface public abstract I9<([CustomTypes]C_I, [CustomTypes]Iface) T0>
implements class I0<class [CustomTypes]C_I,class [mscorlib]System.Object>
{
   .method public abstract  virtual instance int32 M10()
   {
   }
   .method public abstract  virtual instance int32 M11<class ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1>()
   {
   }
   .method public abstract  virtual instance int32 M12()
   {
   }
}
.class public C10<class ([CustomTypes]C_I, [CustomTypes]Iface) T0,T1,class ([CustomTypes]Iface) T2>
extends class C2<class [CustomTypes]C_I,class [mscorlib]System.Object>

{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C2<class [CustomTypes]C_I,class [mscorlib]System.Object>::.ctor() 
      ret
   }
}
.class interface public abstract I11<T0,class ([CustomTypes]Iface) T1>
implements class I9<class [CustomTypes]C_I>
{
   .method public abstract  virtual instance int32 M13<M0,M1,class M2>()
   {
   }
   .method public abstract  virtual instance int32 M14<class ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1,([CustomTypes]Iface) M2>()
   {
   }
   .method public abstract  virtual instance int32 M15<([CustomTypes]C_I) M0,.ctor ([CustomTypes]C_I) M1,M2,M3,M4>()
   {
   }
   .method public abstract  virtual instance int32 M16()
   {
   }
   .method public abstract  virtual instance int32 M17()
   {
   }
}
.class interface public abstract I12<T0,class ([CustomTypes]C_I) T1,T2>

{
   .method public abstract  virtual instance int32 M18()
   {
   }
   .method public abstract  virtual instance int32 M19()
   {
   }
}
.class public C13<class .ctor ([CustomTypes]Iface) T0>
extends [mscorlib]System.Object
implements class I0<class [CustomTypes]C_I,class [mscorlib]System.Object>, 
           class I11<int32,class [CustomTypes]C_I>, 
           class I9<class [CustomTypes]C_I>
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
      ldc.i4 26
      ret
   }
   .method public virtual instance int32 M1<M0,valuetype .ctor ([CustomTypes]Iface) M1,class M2,M3,M4>()
   {
      ldc.i4 27
      ret
   }
   .method public virtual instance int32 M2<class .ctor ([CustomTypes]Iface) M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3,M4>()
   {
      ldc.i4 28
      ret
   }
   .method public virtual instance int32 M13<M0,M1,class M2>()
   {
      ldc.i4 29
      ret
   }
   .method public virtual instance int32 M14<class ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1,([CustomTypes]Iface) M2>()
   {
      ldc.i4 30
      ret
   }
   .method public virtual instance int32 M15<([CustomTypes]C_I) M0,.ctor ([CustomTypes]C_I) M1,M2,M3,M4>()
   {
      ldc.i4 31
      ret
   }
   .method public virtual instance int32 M16()
   {
      ldc.i4 32
      ret
   }
   .method public virtual instance int32 M17()
   {
      ldc.i4 33
      ret
   }
   .method public virtual instance int32 M10()
   {
      ldc.i4 34
      ret
   }
   .method public virtual instance int32 M11<class ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1>()
   {
      ldc.i4 35
      ret
   }
   .method public virtual instance int32 M12()
   {
      ldc.i4 36
      ret
   }
}
.class public C14
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
.class interface public abstract I15

{
   .method public abstract  virtual instance int32 M20()
   {
   }
   .method public abstract  virtual instance int32 M21<M0,class ([CustomTypes]Iface) M1,M2,([CustomTypes]C_I, [CustomTypes]Iface) M3,class .ctor M4>()
   {
   }
}
.class public C16<.ctor T0,class T1,valuetype .ctor ([CustomTypes]Iface) T2>
extends [mscorlib]System.Object
implements class I11<string,class [CustomTypes]C_I>, 
           class I12<class [mscorlib]System.Object,class [CustomTypes]C_I,valuetype [CustomTypes]S>, 
           class I6<class [CustomTypes]C_I,int32>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void  [mscorlib]System.Object::.ctor() 
      ret
   }
   .method public virtual instance int32 M13<M0,M1,class M2>()
   {
      ldc.i4 39
      ret
   }
   .method public virtual instance int32 M14<class ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1,([CustomTypes]Iface) M2>()
   {
      ldc.i4 40
      ret
   }
   .method public virtual instance int32 M15<([CustomTypes]C_I) M0,.ctor ([CustomTypes]C_I) M1,M2,M3,M4>()
   {
      ldc.i4 41
      ret
   }
   .method public virtual instance int32 M16()
   {
      ldc.i4 42
      ret
   }
   .method public virtual instance int32 M17()
   {
      ldc.i4 43
      ret
   }
   .method public virtual instance int32 M18()
   {
      ldc.i4 44
      ret
   }
   .method public virtual instance int32 M19()
   {
      ldc.i4 45
      ret
   }
   .method public virtual instance int32 M3<([CustomTypes]C_I) M0,class .ctor ([CustomTypes]C_I) M1,M2,M3,class ([CustomTypes]Iface) M4>()
   {
      ldc.i4 46
      ret
   }
   .method public virtual instance int32 M4<M0,M1,.ctor ([CustomTypes]Iface)M2,([CustomTypes]C_I, [CustomTypes]Iface) M3>()
   {
      ldc.i4 47
      ret
   }
   .method public virtual instance int32 M5<valuetype .ctor M0,([CustomTypes]C_I, [CustomTypes]Iface) M1>()
   {
      ldc.i4 48
      ret
   }
   .method public virtual instance int32 M6()
   {
      ldc.i4 49
      ret
   }
   .method public virtual instance int32 M7()
   {
      ldc.i4 50
      ret
   }
   .method public virtual instance int32 M10()
   {
      ldc.i4 51
      ret
   }
   .method public virtual instance int32 M11<class ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1>()
   {
      ldc.i4 52
      ret
   }
   .method public virtual instance int32 M12()
   {
      ldc.i4 53
      ret
   }
   .method public virtual instance int32 M0()
   {
      ldc.i4 54
      ret
   }
   .method public virtual instance int32 M1<M0,valuetype .ctor ([CustomTypes]Iface) M1,class M2,M3,M4>()
   {
      ldc.i4 55
      ret
   }
   .method public virtual instance int32 M2<class .ctor ([CustomTypes]Iface) M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3,M4>()
   {
      ldc.i4 56
      ret
   }
}
.class interface public abstract I17<class T0>
implements class I11<class [CustomTypes]C,class [CustomTypes]C_I>, 
           class I6<class [CustomTypes]C_I,class [CustomTypes]C>, 
           class I0<class [CustomTypes]C_I,valuetype [CustomTypes]S>
{
   .method public abstract  virtual instance int32 M22<M0,M1,class ([CustomTypes]C_I) M2>()
   {
   }
   .method public abstract  virtual instance int32 M23()
   {
   }
}
.class public C18
extends class C3<class [CustomTypes]C_I,string,string,string>
implements class I11<int32,class [CustomTypes]C_I>, 
           class I0<class [CustomTypes]C_I,valuetype [CustomTypes]S>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C3<class [CustomTypes]C_I,string,string,string>::.ctor() 
      ret
   }
   .method public virtual instance int32 M13<M0,M1,class M2>()
   {
      ldc.i4 59
      ret
   }
   .method public virtual instance int32 M14<class ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1,([CustomTypes]Iface) M2>()
   {
      ldc.i4 60
      ret
   }
   .method public virtual instance int32 M15<([CustomTypes]C_I) M0,.ctor ([CustomTypes]C_I) M1,M2,M3,M4>()
   {
      ldc.i4 61
      ret
   }
   .method public virtual instance int32 M16()
   {
      ldc.i4 62
      ret
   }
   .method public virtual instance int32 M17()
   {
      ldc.i4 63
      ret
   }
   .method public virtual instance int32 M10()
   {
      ldc.i4 64
      ret
   }
   .method public virtual instance int32 M11<class ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1>()
   {
      ldc.i4 65
      ret
   }
   .method public virtual instance int32 M12()
   {
      ldc.i4 66
      ret
   }
}
.class public C19
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
.class public C20
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
.class interface public abstract I21<T0,([CustomTypes]C_I, [CustomTypes]Iface) T1,(class [mscorlib]System.Exception[]) T2,T3>

{
   .method public abstract  virtual instance int32 M24<M0,M1,.ctor M2>()
   {
   }
}
.class interface public abstract I22

{
   .method public abstract  virtual instance int32 M25<M0,M1>()
   {
   }
   .method public abstract  virtual instance int32 M26<M0>()
   {
   }
}
.class public C23
extends C7

{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void C7::.ctor() 
      ret
   }
}
.class interface public abstract I24

{
   .method public abstract  virtual instance int32 M27<class .ctor M0>()
   {
   }
}
.class public C25
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
.class interface public abstract I26<T0,T1,T2,valuetype .ctor T3,T4>
implements I15, 
           class I6<class [CustomTypes]C_I,!T0>, 
           class I11<!T1,class [CustomTypes]C_I>, 
           class I0<class [CustomTypes]C_I,!T2>
{
   .method public abstract  virtual instance int32 M28<valuetype .ctor ([CustomTypes]Iface) M0,M1,(class [mscorlib]System.Exception[]) M2>()
   {
   }
   .method public abstract  virtual instance int32 M29()
   {
   }
   .method public abstract  virtual instance int32 M30<class .ctor M0>()
   {
   }
   .method public abstract  virtual instance int32 M31()
   {
   }
}
.class public C27
extends [mscorlib]System.Object
implements class I26<string,class [mscorlib]System.Object,int32,valuetype [mscorlib]System.Collections.Generic.KeyValuePair`2<valuetype [CustomTypes]NonGenStruct,int32>,class [mscorlib]System.Object>, 
           I24, 
           class I8<class [CustomTypes]C_I,string,class [CustomTypes]C_I,valuetype [CustomTypes]S,class [mscorlib]System.Object>, 
           class I12<string,class [CustomTypes]C_I,class [mscorlib]System.Object>, 
           class I6<class [CustomTypes]C_I,string>, 
           class I9<class [CustomTypes]C_I>, 
           class I17<class [CustomTypes]Delegate1>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void  [mscorlib]System.Object::.ctor() 
      ret
   }
   .method public virtual instance int32 M28<valuetype .ctor ([CustomTypes]Iface) M0,M1,(class [mscorlib]System.Exception[]) M2>()
   {
      ldc.i4 75
      ret
   }
   .method public virtual instance int32 M29()
   {
      ldc.i4 76
      ret
   }
   .method public virtual instance int32 M30<class .ctor M0>()
   {
      ldc.i4 77
      ret
   }
   .method public virtual instance int32 M31()
   {
      ldc.i4 78
      ret
   }
   .method public virtual instance int32 M27<class .ctor M0>()
   {
      ldc.i4 79
      ret
   }
   .method public virtual instance int32 M8()
   {
      ldc.i4 80
      ret
   }
   .method public virtual instance int32 M9<M0>()
   {
      ldc.i4 81
      ret
   }
   .method public virtual instance int32 M18()
   {
      ldc.i4 82
      ret
   }
   .method public virtual instance int32 M19()
   {
      ldc.i4 83
      ret
   }
   .method public virtual instance int32 M3<([CustomTypes]C_I) M0,class .ctor ([CustomTypes]C_I) M1,M2,M3,class ([CustomTypes]Iface) M4>()
   {
      ldc.i4 84
      ret
   }
   .method public virtual instance int32 M4<M0,M1,.ctor ([CustomTypes]Iface)M2,([CustomTypes]C_I, [CustomTypes]Iface) M3>()
   {
      ldc.i4 85
      ret
   }
   .method public virtual instance int32 M5<valuetype .ctor M0,([CustomTypes]C_I, [CustomTypes]Iface) M1>()
   {
      ldc.i4 86
      ret
   }
   .method public virtual instance int32 M6()
   {
      ldc.i4 87
      ret
   }
   .method public virtual instance int32 M7()
   {
      ldc.i4 88
      ret
   }
   .method public virtual instance int32 M10()
   {
      ldc.i4 89
      ret
   }
   .method public virtual instance int32 M11<class ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1>()
   {
      ldc.i4 90
      ret
   }
   .method public virtual instance int32 M12()
   {
      ldc.i4 91
      ret
   }
   .method public virtual instance int32 M22<M0,M1,class ([CustomTypes]C_I) M2>()
   {
      ldc.i4 92
      ret
   }
   .method public virtual instance int32 M23()
   {
      ldc.i4 93
      ret
   }
   .method public virtual instance int32 M20()
   {
      ldc.i4 94
      ret
   }
   .method public virtual instance int32 M21<M0,class ([CustomTypes]Iface) M1,M2,([CustomTypes]C_I, [CustomTypes]Iface) M3,class .ctor M4>()
   {
      ldc.i4 95
      ret
   }
   .method public virtual instance int32 M13<M0,M1,class M2>()
   {
      ldc.i4 96
      ret
   }
   .method public virtual instance int32 M14<class ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1,([CustomTypes]Iface) M2>()
   {
      ldc.i4 97
      ret
   }
   .method public virtual instance int32 M15<([CustomTypes]C_I) M0,.ctor ([CustomTypes]C_I) M1,M2,M3,M4>()
   {
      ldc.i4 98
      ret
   }
   .method public virtual instance int32 M16()
   {
      ldc.i4 99
      ret
   }
   .method public virtual instance int32 M17()
   {
      ldc.i4 100
      ret
   }
   .method public virtual instance int32 M0()
   {
      ldc.i4 101
      ret
   }
   .method public virtual instance int32 M1<M0,valuetype .ctor ([CustomTypes]Iface) M1,class M2,M3,M4>()
   {
      ldc.i4 102
      ret
   }
   .method public virtual instance int32 M2<class .ctor ([CustomTypes]Iface) M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3,M4>()
   {
      ldc.i4 103
      ret
   }
}
.class public C28
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
.class interface public abstract I29<class .ctor ([CustomTypes]Iface) T0,T1>

{
   .method public abstract  virtual instance int32 M32<.ctor ([CustomTypes]Iface)M0,M1,(class [mscorlib]System.Exception[]) M2>()
   {
   }
   .method public abstract  virtual instance int32 M33()
   {
   }
   .method public abstract  virtual instance int32 M34()
   {
   }
   .method public abstract  virtual instance int32 M35<M0,M1,class .ctor ([CustomTypes]C_I) M2,valuetype .ctor M3>()
   {
   }
   .method public abstract  virtual instance int32 M36()
   {
   }
}
.class public C30
extends C18
implements I22, 
           class I9<class [CustomTypes]C_I>, 
           class I21<string,class [CustomTypes]C_I,class [mscorlib]System.Exception[],class [CustomTypes]C>, 
           class I11<valuetype [CustomTypes]S,class [CustomTypes]C_I>, 
           class I29<class [CustomTypes]C_I,string>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void C18::.ctor() 
      ret
   }
   .method public virtual instance int32 M25<M0,M1>()
   {
      ldc.i4 109
      ret
   }
   .method public virtual instance int32 M26<M0>()
   {
      ldc.i4 110
      ret
   }
   .method public virtual instance int32 M10()
   {
      ldc.i4 111
      ret
   }
   .method public virtual instance int32 M11<class ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1>()
   {
      ldc.i4 112
      ret
   }
   .method public virtual instance int32 M12()
   {
      ldc.i4 113
      ret
   }
   .method public virtual instance int32 M24<M0,M1,.ctor M2>()
   {
      ldc.i4 114
      ret
   }
   .method public virtual instance int32 M32<.ctor ([CustomTypes]Iface)M0,M1,(class [mscorlib]System.Exception[]) M2>()
   {
      ldc.i4 115
      ret
   }
   .method public virtual instance int32 M33()
   {
      ldc.i4 116
      ret
   }
   .method public virtual instance int32 M34()
   {
      ldc.i4 117
      ret
   }
   .method public virtual instance int32 M35<M0,M1,class .ctor ([CustomTypes]C_I) M2,valuetype .ctor M3>()
   {
      ldc.i4 118
      ret
   }
   .method public virtual instance int32 M36()
   {
      ldc.i4 119
      ret
   }
}
.class interface public abstract I31

{
   .method public abstract  virtual instance int32 M37()
   {
   }
   .method public abstract  virtual instance int32 M38<class M0,M1,M2,valuetype .ctor M3,M4>()
   {
   }
   .method public abstract  virtual instance int32 M39<M0,M1,M2,M3,M4>()
   {
   }
}
.class interface public abstract I32
implements I24, 
           class I29<class [CustomTypes]C_I,int32>, 
           I22, 
           class I9<class [CustomTypes]C_I>, 
           I31
{
   .method public abstract  virtual instance int32 M40<.ctor ([CustomTypes]Iface)M0,M1,class ([CustomTypes]C_I, [CustomTypes]Iface) M2,M3,M4>()
   {
   }
   .method public abstract  virtual instance int32 M41<.ctor ([CustomTypes]C_I) M0,M1,M2>()
   {
   }
   .method public abstract  virtual instance int32 M42()
   {
   }
   .method public abstract  virtual instance int32 M43<class ([CustomTypes]C_I) M0>()
   {
   }
   .method public abstract  virtual instance int32 M44()
   {
   }
}
.class public C33<(class [mscorlib]System.Exception[]) T0,T1>
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
.class public C34
extends C20

{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void C20::.ctor() 
      ret
   }
}
.class interface public abstract I35<valuetype .ctor T0,T1,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) T2,valuetype T3,class .ctor T4>

{
   .method public abstract  virtual instance int32 M45<.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M0>()
   {
   }
   .method public abstract  virtual instance int32 M46<.ctor M0,([CustomTypes]Iface) M1,M2>()
   {
   }
   .method public abstract  virtual instance int32 M47()
   {
   }
}
.class public C36<(class [mscorlib]System.Exception[]) T0,valuetype T1,T2>
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
.class interface public abstract I37<T0,T1,valuetype .ctor T2>

{
   .method public abstract  virtual instance int32 M48()
   {
   }
   .method public abstract  virtual instance int32 M49<M0,([CustomTypes]Iface) M1>()
   {
   }
}
.class interface public abstract I38<T0,(class [mscorlib]System.Exception[]) T1,T2,valuetype .ctor T3>

{
   .method public abstract  virtual instance int32 M50<M0,M1>()
   {
   }
}
.class public C39
extends [mscorlib]System.Object
implements class I6<class [CustomTypes]C_I,class [CustomTypes]C>, 
           I22, 
           class I17<class [CustomTypes]ClassNoCtor>, 
           class I8<class [CustomTypes]C_I,class [CustomTypes]C,class [CustomTypes]C_I,valuetype [CustomTypes]S,string>, 
           class I0<class [CustomTypes]C_I,int32>, 
           class I26<valuetype [CustomTypes]S,int32,string,valuetype [CustomTypes]GenStruct<int32>,valuetype [CustomTypes]S>, 
           class I37<int32,class [CustomTypes]C,valuetype [mscorlib]System.DateTime>, 
           class I11<valuetype [CustomTypes]S,class [CustomTypes]C_I>, 
           I32, 
           class I9<class [CustomTypes]C_I>, 
           class I12<class [CustomTypes]C,class [CustomTypes]C_I,class [mscorlib]System.Object>, 
           class I38<int32,class [mscorlib]System.Exception[],int32,valuetype [CustomTypes]GenStruct<int32>>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void  [mscorlib]System.Object::.ctor() 
      ret
   }
   .method public virtual instance int32 M3<([CustomTypes]C_I) M0,class .ctor ([CustomTypes]C_I) M1,M2,M3,class ([CustomTypes]Iface) M4>()
   {
      ldc.i4 134
      ret
   }
   .method public virtual instance int32 M4<M0,M1,.ctor ([CustomTypes]Iface)M2,([CustomTypes]C_I, [CustomTypes]Iface) M3>()
   {
      ldc.i4 135
      ret
   }
   .method public virtual instance int32 M5<valuetype .ctor M0,([CustomTypes]C_I, [CustomTypes]Iface) M1>()
   {
      ldc.i4 136
      ret
   }
   .method public virtual instance int32 M6()
   {
      ldc.i4 137
      ret
   }
   .method public virtual instance int32 M7()
   {
      ldc.i4 138
      ret
   }
   .method public virtual instance int32 M25<M0,M1>()
   {
      ldc.i4 139
      ret
   }
   .method public virtual instance int32 M26<M0>()
   {
      ldc.i4 140
      ret
   }
   .method public virtual instance int32 M22<M0,M1,class ([CustomTypes]C_I) M2>()
   {
      ldc.i4 141
      ret
   }
   .method public virtual instance int32 M23()
   {
      ldc.i4 142
      ret
   }
   .method public virtual instance int32 M8()
   {
      ldc.i4 143
      ret
   }
   .method public virtual instance int32 M9<M0>()
   {
      ldc.i4 144
      ret
   }
   .method public virtual instance int32 M0()
   {
      ldc.i4 145
      ret
   }
   .method public virtual instance int32 M1<M0,valuetype .ctor ([CustomTypes]Iface) M1,class M2,M3,M4>()
   {
      ldc.i4 146
      ret
   }
   .method public virtual instance int32 M2<class .ctor ([CustomTypes]Iface) M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3,M4>()
   {
      ldc.i4 147
      ret
   }
   .method public virtual instance int32 M28<valuetype .ctor ([CustomTypes]Iface) M0,M1,(class [mscorlib]System.Exception[]) M2>()
   {
      ldc.i4 148
      ret
   }
   .method public virtual instance int32 M29()
   {
      ldc.i4 149
      ret
   }
   .method public virtual instance int32 M30<class .ctor M0>()
   {
      ldc.i4 150
      ret
   }
   .method public virtual instance int32 M31()
   {
      ldc.i4 151
      ret
   }
   .method public virtual instance int32 M48()
   {
      ldc.i4 152
      ret
   }
   .method public virtual instance int32 M49<M0,([CustomTypes]Iface) M1>()
   {
      ldc.i4 153
      ret
   }
   .method public virtual instance int32 M13<M0,M1,class M2>()
   {
      ldc.i4 154
      ret
   }
   .method public virtual instance int32 M14<class ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1,([CustomTypes]Iface) M2>()
   {
      ldc.i4 155
      ret
   }
   .method public virtual instance int32 M15<([CustomTypes]C_I) M0,.ctor ([CustomTypes]C_I) M1,M2,M3,M4>()
   {
      ldc.i4 156
      ret
   }
   .method public virtual instance int32 M16()
   {
      ldc.i4 157
      ret
   }
   .method public virtual instance int32 M17()
   {
      ldc.i4 158
      ret
   }
   .method public virtual instance int32 M40<.ctor ([CustomTypes]Iface)M0,M1,class ([CustomTypes]C_I, [CustomTypes]Iface) M2,M3,M4>()
   {
      ldc.i4 159
      ret
   }
   .method public virtual instance int32 M41<.ctor ([CustomTypes]C_I) M0,M1,M2>()
   {
      ldc.i4 160
      ret
   }
   .method public virtual instance int32 M42()
   {
      ldc.i4 161
      ret
   }
   .method public virtual instance int32 M43<class ([CustomTypes]C_I) M0>()
   {
      ldc.i4 162
      ret
   }
   .method public virtual instance int32 M44()
   {
      ldc.i4 163
      ret
   }
   .method public virtual instance int32 M10()
   {
      ldc.i4 164
      ret
   }
   .method public virtual instance int32 M11<class ([CustomTypes]C_I) M0,class ([CustomTypes]C_I) M1>()
   {
      ldc.i4 165
      ret
   }
   .method public virtual instance int32 M12()
   {
      ldc.i4 166
      ret
   }
   .method public virtual instance int32 M18()
   {
      ldc.i4 167
      ret
   }
   .method public virtual instance int32 M19()
   {
      ldc.i4 168
      ret
   }
   .method public virtual instance int32 M50<M0,M1>()
   {
      ldc.i4 169
      ret
   }
   .method public virtual instance int32 M20()
   {
      ldc.i4 170
      ret
   }
   .method public virtual instance int32 M21<M0,class ([CustomTypes]Iface) M1,M2,([CustomTypes]C_I, [CustomTypes]Iface) M3,class .ctor M4>()
   {
      ldc.i4 171
      ret
   }
   .method public virtual instance int32 M27<class .ctor M0>()
   {
      ldc.i4 172
      ret
   }
   .method public virtual instance int32 M32<.ctor ([CustomTypes]Iface)M0,M1,(class [mscorlib]System.Exception[]) M2>()
   {
      ldc.i4 173
      ret
   }
   .method public virtual instance int32 M33()
   {
      ldc.i4 174
      ret
   }
   .method public virtual instance int32 M34()
   {
      ldc.i4 175
      ret
   }
   .method public virtual instance int32 M35<M0,M1,class .ctor ([CustomTypes]C_I) M2,valuetype .ctor M3>()
   {
      ldc.i4 176
      ret
   }
   .method public virtual instance int32 M36()
   {
      ldc.i4 177
      ret
   }
   .method public virtual instance int32 M37()
   {
      ldc.i4 178
      ret
   }
   .method public virtual instance int32 M38<class M0,M1,M2,valuetype .ctor M3,M4>()
   {
      ldc.i4 179
      ret
   }
   .method public virtual instance int32 M39<M0,M1,M2,M3,M4>()
   {
      ldc.i4 180
      ret
   }
}
.class interface public abstract I40<T0>

{
   .method public abstract  virtual instance int32 M51()
   {
   }
}
.class public C41<.ctor ([CustomTypes]C_I) T0>
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
