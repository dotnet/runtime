// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
.assembly Types958 {}
.assembly extern mscorlib {}

.assembly extern CustomTypes {}

.class interface public abstract I0<T0,valuetype .ctor ([CustomTypes]Iface) T1>

{
   .method public abstract  virtual instance int32 M0<([CustomTypes]C_I) M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,class .ctor M2,.ctor ([CustomTypes]C_I) M3>()
   {
   }
   .method public abstract  virtual instance int32 M1<M0,(class [mscorlib]System.Exception[]) M1,M2,.ctor ([CustomTypes]C_I) M3>()
   {
   }
   .method public abstract  virtual instance int32 M2()
   {
   }
   .method public abstract  virtual instance int32 M3()
   {
   }
}
.class public C1
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
.class public C2<T0,T1,.ctor ([CustomTypes]C_I) T2,T3>
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
.class public C3<T0,class ([CustomTypes]C_I, [CustomTypes]Iface) T1,T2,T3>
extends C1
implements class I0<!T0,valuetype [CustomTypes]S_I>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void C1::.ctor() 
      ret
   }
   .method public virtual instance int32 M0<([CustomTypes]C_I) M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,class .ctor M2,.ctor ([CustomTypes]C_I) M3>()
   {
      ldc.i4 4
      ret
   }
   .method public virtual instance int32 M1<M0,(class [mscorlib]System.Exception[]) M1,M2,.ctor ([CustomTypes]C_I) M3>()
   {
      ldc.i4 5
      ret
   }
   .method public virtual instance int32 M2()
   {
      ldc.i4 6
      ret
   }
   .method public virtual instance int32 M3()
   {
      ldc.i4 7
      ret
   }
}
.class public C4<T0>
extends class C3<!T0,class [CustomTypes]C_I,!T0,!T0>
implements class I0<!T0,valuetype [CustomTypes]S_I>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C3<!T0,class [CustomTypes]C_I,!T0,!T0>::.ctor() 
      ret
   }
}
.class interface public abstract I5<class ([CustomTypes]C_I, [CustomTypes]Iface) T0,class .ctor ([CustomTypes]C_I) T1,T2>

{
   .method public abstract  virtual instance int32 M4<M0,M1>()
   {
   }
}
.class interface public abstract I6<T0,T1,class .ctor ([CustomTypes]Iface) T2,class .ctor ([CustomTypes]Iface) T3,T4>

{
   .method public abstract  virtual instance int32 M5()
   {
   }
}
.class public C7
extends class C4<class [CustomTypes]C>
implements class I6<string,string,class [CustomTypes]C_I,class [CustomTypes]C_I,class [mscorlib]System.Object>, 
           class I5<class [CustomTypes]C_I,class [CustomTypes]C_I,class [CustomTypes]C>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C4<class [CustomTypes]C>::.ctor() 
      ret
   }
   .method public virtual instance int32 M5()
   {
      ldc.i4 10
      ret
   }
   .method public virtual instance int32 M4<M0,M1>()
   {
      ldc.i4 11
      ret
   }
}
.class public C8<([CustomTypes]C_I) T0>
extends C7
implements class I6<class [CustomTypes]C,int32,class [CustomTypes]C_I,class [CustomTypes]C_I,valuetype [CustomTypes]S>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void C7::.ctor() 
      ret
   }
}
.class interface public abstract I9
implements class I0<int32,valuetype [CustomTypes]S_I>
{
   .method public abstract  virtual instance int32 M6()
   {
   }
   .method public abstract  virtual instance int32 M7()
   {
   }
   .method public abstract  virtual instance int32 M8()
   {
   }
   .method public abstract  virtual instance int32 M9<M0,M1>()
   {
   }
   .method public abstract  virtual instance int32 M10()
   {
   }
}
.class interface public abstract I10
implements I9, 
           class I0<valuetype [CustomTypes]S,valuetype [CustomTypes]S_I>, 
           class I5<class [CustomTypes]C_I,class [CustomTypes]C_I,string>
{
   .method public abstract  virtual instance int32 M11()
   {
   }
   .method public abstract  virtual instance int32 M12()
   {
   }
   .method public abstract  virtual instance int32 M13()
   {
   }
   .method public abstract  virtual instance int32 M14<([CustomTypes]Iface) M0,M1,M2,valuetype ([CustomTypes]Iface)M3>()
   {
   }
   .method public abstract  virtual instance int32 M15()
   {
   }
}
.class interface public abstract I11<T0,T1>
implements class I5<class [CustomTypes]C_I,class [CustomTypes]C_I,!T0>, 
           I9, 
           class I6<!T0,!T0,class [CustomTypes]C_I,class [CustomTypes]C_I,!T1>
{
   .method public abstract  virtual instance int32 M16<class .ctor ([CustomTypes]C_I) M0,valuetype .ctor ([CustomTypes]Iface) M1,([CustomTypes]C_I, [CustomTypes]Iface) M2,(class [mscorlib]System.Exception[]) M3,M4>()
   {
   }
   .method public abstract  virtual instance int32 M17<.ctor ([CustomTypes]Iface)M0,M1,class .ctor ([CustomTypes]C_I) M2>()
   {
   }
   .method public abstract  virtual instance int32 M18<.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M0>()
   {
   }
   .method public abstract  virtual instance int32 M19()
   {
   }
   .method public abstract  virtual instance int32 M20<M0,class M1>()
   {
   }
}
.class interface public abstract I12<class .ctor ([CustomTypes]C_I) T0,T1>
implements class I0<!T1,valuetype [CustomTypes]S_I>
{
   .method public abstract  virtual instance int32 M21()
   {
   }
   .method public abstract  virtual instance int32 M22<class ([CustomTypes]C_I, [CustomTypes]Iface) M0,M1,M2,([CustomTypes]C_I, [CustomTypes]Iface) M3,M4>()
   {
   }
   .method public abstract  virtual instance int32 M23<M0,M1,class ([CustomTypes]C_I, [CustomTypes]Iface) M2,M3>()
   {
   }
   .method public abstract  virtual instance int32 M24()
   {
   }
   .method public abstract  virtual instance int32 M25()
   {
   }
}
.class interface public abstract I13
implements I10
{
   .method public abstract  virtual instance int32 M26<([CustomTypes]C_I, [CustomTypes]Iface) M0>()
   {
   }
   .method public abstract  virtual instance int32 M27()
   {
   }
   .method public abstract  virtual instance int32 M28()
   {
   }
   .method public abstract  virtual instance int32 M29()
   {
   }
}
.class public C14<T0,(class [mscorlib]System.Exception[]) T1,T2>
extends C1
implements I10, 
           I9
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void C1::.ctor() 
      ret
   }
   .method public virtual instance int32 M11()
   {
      ldc.i4 36
      ret
   }
   .method public virtual instance int32 M12()
   {
      ldc.i4 37
      ret
   }
   .method public virtual instance int32 M13()
   {
      ldc.i4 38
      ret
   }
   .method public virtual instance int32 M14<([CustomTypes]Iface) M0,M1,M2,valuetype ([CustomTypes]Iface)M3>()
   {
      ldc.i4 39
      ret
   }
   .method public virtual instance int32 M15()
   {
      ldc.i4 40
      ret
   }
   .method public virtual instance int32 M6()
   {
      ldc.i4 41
      ret
   }
   .method public virtual instance int32 M7()
   {
      ldc.i4 42
      ret
   }
   .method public virtual instance int32 M8()
   {
      ldc.i4 43
      ret
   }
   .method public virtual instance int32 M9<M0,M1>()
   {
      ldc.i4 44
      ret
   }
   .method public virtual instance int32 M10()
   {
      ldc.i4 45
      ret
   }
   .method public virtual instance int32 M0<([CustomTypes]C_I) M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,class .ctor M2,.ctor ([CustomTypes]C_I) M3>()
   {
      ldc.i4 46
      ret
   }
   .method public virtual instance int32 M1<M0,(class [mscorlib]System.Exception[]) M1,M2,.ctor ([CustomTypes]C_I) M3>()
   {
      ldc.i4 47
      ret
   }
   .method public virtual instance int32 M2()
   {
      ldc.i4 48
      ret
   }
   .method public virtual instance int32 M3()
   {
      ldc.i4 49
      ret
   }
   .method public virtual instance int32 M4<M0,M1>()
   {
      ldc.i4 50
      ret
   }
}
.class interface public abstract I15
implements class I6<string,int32,class [CustomTypes]C_I,class [CustomTypes]C_I,int32>, 
           I10, 
           class I12<class [CustomTypes]C_I,int32>
{
   .method public abstract  virtual instance int32 M30<M0,M1>()
   {
   }
}
.class public C16
extends [mscorlib]System.Object
implements class I0<class [mscorlib]System.Object,valuetype [CustomTypes]S_I>, 
           class I11<class [mscorlib]System.Object,class [CustomTypes]C>, 
           class I12<class [CustomTypes]C_I,class [CustomTypes]C>, 
           I13, 
           I15, 
           I9, 
           class I6<class [mscorlib]System.Object,int32,class [CustomTypes]C_I,class [CustomTypes]C_I,class [CustomTypes]C>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void  [mscorlib]System.Object::.ctor() 
      ret
   }
   .method public virtual instance int32 M0<([CustomTypes]C_I) M0,.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M1,class .ctor M2,.ctor ([CustomTypes]C_I) M3>()
   {
      ldc.i4 52
      ret
   }
   .method public virtual instance int32 M1<M0,(class [mscorlib]System.Exception[]) M1,M2,.ctor ([CustomTypes]C_I) M3>()
   {
      ldc.i4 53
      ret
   }
   .method public virtual instance int32 M2()
   {
      ldc.i4 54
      ret
   }
   .method public virtual instance int32 M3()
   {
      ldc.i4 55
      ret
   }
   .method public virtual instance int32 M16<class .ctor ([CustomTypes]C_I) M0,valuetype .ctor ([CustomTypes]Iface) M1,([CustomTypes]C_I, [CustomTypes]Iface) M2,(class [mscorlib]System.Exception[]) M3,M4>()
   {
      ldc.i4 56
      ret
   }
   .method public virtual instance int32 M17<.ctor ([CustomTypes]Iface)M0,M1,class .ctor ([CustomTypes]C_I) M2>()
   {
      ldc.i4 57
      ret
   }
   .method public virtual instance int32 M18<.ctor ([CustomTypes]C_I, [CustomTypes]Iface) M0>()
   {
      ldc.i4 58
      ret
   }
   .method public virtual instance int32 M19()
   {
      ldc.i4 59
      ret
   }
   .method public virtual instance int32 M20<M0,class M1>()
   {
      ldc.i4 60
      ret
   }
   .method public virtual instance int32 M21()
   {
      ldc.i4 61
      ret
   }
   .method public virtual instance int32 M22<class ([CustomTypes]C_I, [CustomTypes]Iface) M0,M1,M2,([CustomTypes]C_I, [CustomTypes]Iface) M3,M4>()
   {
      ldc.i4 62
      ret
   }
   .method public virtual instance int32 M23<M0,M1,class ([CustomTypes]C_I, [CustomTypes]Iface) M2,M3>()
   {
      ldc.i4 63
      ret
   }
   .method public virtual instance int32 M24()
   {
      ldc.i4 64
      ret
   }
   .method public virtual instance int32 M25()
   {
      ldc.i4 65
      ret
   }
   .method public virtual instance int32 M26<([CustomTypes]C_I, [CustomTypes]Iface) M0>()
   {
      ldc.i4 66
      ret
   }
   .method public virtual instance int32 M27()
   {
      ldc.i4 67
      ret
   }
   .method public virtual instance int32 M28()
   {
      ldc.i4 68
      ret
   }
   .method public virtual instance int32 M29()
   {
      ldc.i4 69
      ret
   }
   .method public virtual instance int32 M30<M0,M1>()
   {
      ldc.i4 70
      ret
   }
   .method public virtual instance int32 M6()
   {
      ldc.i4 71
      ret
   }
   .method public virtual instance int32 M7()
   {
      ldc.i4 72
      ret
   }
   .method public virtual instance int32 M8()
   {
      ldc.i4 73
      ret
   }
   .method public virtual instance int32 M9<M0,M1>()
   {
      ldc.i4 74
      ret
   }
   .method public virtual instance int32 M10()
   {
      ldc.i4 75
      ret
   }
   .method public virtual instance int32 M5()
   {
      ldc.i4 76
      ret
   }
   .method public virtual instance int32 M4<M0,M1>()
   {
      ldc.i4 77
      ret
   }
   .method public virtual instance int32 M11()
   {
      ldc.i4 78
      ret
   }
   .method public virtual instance int32 M12()
   {
      ldc.i4 79
      ret
   }
   .method public virtual instance int32 M13()
   {
      ldc.i4 80
      ret
   }
   .method public virtual instance int32 M14<([CustomTypes]Iface) M0,M1,M2,valuetype ([CustomTypes]Iface)M3>()
   {
      ldc.i4 81
      ret
   }
   .method public virtual instance int32 M15()
   {
      ldc.i4 82
      ret
   }
}
