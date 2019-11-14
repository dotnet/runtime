// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
.assembly Types983 {}
.assembly extern mscorlib {}

.assembly extern CustomTypes {}

.class interface public abstract I0<T0,.ctor ([CustomTypes]Iface)T1,.ctor ([CustomTypes]C_I) T2>

{
   .method public abstract  virtual instance int32 M0()
   {
   }
   .method public abstract  virtual instance int32 M1<M0,([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3,M4>()
   {
   }
   .method public abstract  virtual instance int32 M2<M0,M1,class .ctor ([CustomTypes]C_I) M2,.ctor ([CustomTypes]C_I) M3,M4>()
   {
   }
   .method public abstract  virtual instance int32 M3<([CustomTypes]C_I) M0,class .ctor ([CustomTypes]C_I) M1,M2,M3,class .ctor M4>()
   {
   }
   .method public abstract  virtual instance int32 M4<M0,M1,M2,M3,class .ctor M4>()
   {
   }
}
.class interface public abstract I1<T0,class .ctor T1,T2>

{
   .method public abstract  virtual instance int32 M5()
   {
   }
   .method public abstract  virtual instance int32 M6<valuetype ([CustomTypes]Iface)M0,M1,M2,class ([CustomTypes]C_I) M3,M4>()
   {
   }
   .method public abstract  virtual instance int32 M7()
   {
   }
   .method public abstract  virtual instance int32 M8<.ctor ([CustomTypes]C_I) M0>()
   {
   }
   .method public abstract  virtual instance int32 M9<M0>()
   {
   }
}
.class public C2<.ctor ([CustomTypes]C_I) T0,T1>
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
.class public C3<T0,.ctor T1,T2,T3>
extends class C2<class [CustomTypes]C_I,!T3>
implements class I0<!T2,class [CustomTypes]C_I,class [CustomTypes]C_I>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C2<class [CustomTypes]C_I,!T3>::.ctor() 
      ret
   }
   .method public virtual instance int32 M0()
   {
      ldc.i4 10
      ret
   }
   .method public virtual instance int32 M1<M0,([CustomTypes]C_I, [CustomTypes]Iface) M1,M2,M3,M4>()
   {
      ldc.i4 11
      ret
   }
   .method public virtual instance int32 M2<M0,M1,class .ctor ([CustomTypes]C_I) M2,.ctor ([CustomTypes]C_I) M3,M4>()
   {
      ldc.i4 12
      ret
   }
   .method public virtual instance int32 M3<([CustomTypes]C_I) M0,class .ctor ([CustomTypes]C_I) M1,M2,M3,class .ctor M4>()
   {
      ldc.i4 13
      ret
   }
   .method public virtual instance int32 M4<M0,M1,M2,M3,class .ctor M4>()
   {
      ldc.i4 14
      ret
   }
}
.class public C4<([CustomTypes]Iface) T0,T1,valuetype T2,valuetype T3>
extends class C3<!T1,valuetype [CustomTypes]GenStruct<int32>,!T1,!T1>
implements class I1<!T1,class [CustomTypes]ClassWithCtor,!T1>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C3<!T1,valuetype [CustomTypes]GenStruct<int32>,!T1,!T1>::.ctor() 
      ret
   }
   .method public virtual instance int32 M5()
   {
      ldc.i4 15
      ret
   }
   .method public virtual instance int32 M6<valuetype ([CustomTypes]Iface)M0,M1,M2,class ([CustomTypes]C_I) M3,M4>()
   {
      ldc.i4 16
      ret
   }
   .method public virtual instance int32 M7()
   {
      ldc.i4 17
      ret
   }
   .method public virtual instance int32 M8<.ctor ([CustomTypes]C_I) M0>()
   {
      ldc.i4 18
      ret
   }
   .method public virtual instance int32 M9<M0>()
   {
      ldc.i4 19
      ret
   }
}
.class interface public abstract I5
implements class I1<valuetype [CustomTypes]S,class [mscorlib]System.Object,int32>
{
   .method public abstract  virtual instance int32 M10()
   {
   }
}
.class interface public abstract I6
implements I5
{
   .method public abstract  virtual instance int32 M11<M0,class .ctor M1,valuetype .ctor ([CustomTypes]Iface) M2,M3>()
   {
   }
}
.class interface public abstract I7

{
   .method public abstract  virtual instance int32 M12<([CustomTypes]Iface) M0,(class [mscorlib]System.Exception[]) M1>()
   {
   }
}
.class public C8<T0,class .ctor T1,T2>
extends class C3<!T2,valuetype [CustomTypes]Enum1,!T0,!T2>
implements class I0<!T0,class [CustomTypes]C_I,class [CustomTypes]C_I>, 
           I5
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C3<!T2,valuetype [CustomTypes]Enum1,!T0,!T2>::.ctor() 
      ret
   }
   .method public virtual instance int32 M10()
   {
      ldc.i4 23
      ret
   }
   .method public virtual instance int32 M5()
   {
      ldc.i4 24
      ret
   }
   .method public virtual instance int32 M6<valuetype ([CustomTypes]Iface)M0,M1,M2,class ([CustomTypes]C_I) M3,M4>()
   {
      ldc.i4 25
      ret
   }
   .method public virtual instance int32 M7()
   {
      ldc.i4 26
      ret
   }
   .method public virtual instance int32 M8<.ctor ([CustomTypes]C_I) M0>()
   {
      ldc.i4 27
      ret
   }
   .method public virtual instance int32 M9<M0>()
   {
      ldc.i4 28
      ret
   }
}
.class public C9
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
.class public C10<T0,T1,T2,([CustomTypes]C_I) T3,class .ctor ([CustomTypes]Iface) T4>
extends C9

{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void C9::.ctor() 
      ret
   }
}
.class public C11<T0,class ([CustomTypes]Iface) T1,T2>
extends class C10<!T0,!T0,!T2,class [CustomTypes]C_I,class [CustomTypes]C_I>

{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C10<!T0,!T0,!T2,class [CustomTypes]C_I,class [CustomTypes]C_I>::.ctor() 
      ret
   }
}
.class public C12<T0,valuetype T1,T2,([CustomTypes]Iface) T3,T4>
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
.class public C13
extends [mscorlib]System.Object
implements class I1<class [CustomTypes]C,class [mscorlib]System.Object,class [CustomTypes]C>, 
           I5
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void  [mscorlib]System.Object::.ctor() 
      ret
   }
   .method public virtual instance int32 M5()
   {
      ldc.i4 29
      ret
   }
   .method public virtual instance int32 M6<valuetype ([CustomTypes]Iface)M0,M1,M2,class ([CustomTypes]C_I) M3,M4>()
   {
      ldc.i4 30
      ret
   }
   .method public virtual instance int32 M7()
   {
      ldc.i4 31
      ret
   }
   .method public virtual instance int32 M8<.ctor ([CustomTypes]C_I) M0>()
   {
      ldc.i4 32
      ret
   }
   .method public virtual instance int32 M9<M0>()
   {
      ldc.i4 33
      ret
   }
   .method public virtual instance int32 M10()
   {
      ldc.i4 34
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
.class public C15<.ctor ([CustomTypes]C_I) T0>
extends class C3<int32,valuetype [CustomTypes]Enum1,string,class [mscorlib]System.Object>
implements I7, 
           I5
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C3<int32,valuetype [CustomTypes]Enum1,string,class [mscorlib]System.Object>::.ctor() 
      ret
   }
   .method public virtual instance int32 M12<([CustomTypes]Iface) M0,(class [mscorlib]System.Exception[]) M1>()
   {
      ldc.i4 35
      ret
   }
   .method public virtual instance int32 M10()
   {
      ldc.i4 36
      ret
   }
   .method public virtual instance int32 M5()
   {
      ldc.i4 37
      ret
   }
   .method public virtual instance int32 M6<valuetype ([CustomTypes]Iface)M0,M1,M2,class ([CustomTypes]C_I) M3,M4>()
   {
      ldc.i4 38
      ret
   }
   .method public virtual instance int32 M7()
   {
      ldc.i4 39
      ret
   }
   .method public virtual instance int32 M8<.ctor ([CustomTypes]C_I) M0>()
   {
      ldc.i4 40
      ret
   }
   .method public virtual instance int32 M9<M0>()
   {
      ldc.i4 41
      ret
   }
}
.class public C16<.ctor ([CustomTypes]C_I, [CustomTypes]Iface) T0,.ctor ([CustomTypes]C_I) T1,class T2>
extends class C3<string,class [mscorlib]System.Object,class [CustomTypes]C,string>
implements class I0<string,class [CustomTypes]C_I,!T1>, 
           class I1<class [CustomTypes]C,class [CustomTypes]GenClassWithCtor<int32>,string>, 
           I5
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C3<string,class [mscorlib]System.Object,class [CustomTypes]C,string>::.ctor() 
      ret
   }
   .method public virtual instance int32 M5()
   {
      ldc.i4 42
      ret
   }
   .method public virtual instance int32 M6<valuetype ([CustomTypes]Iface)M0,M1,M2,class ([CustomTypes]C_I) M3,M4>()
   {
      ldc.i4 43
      ret
   }
   .method public virtual instance int32 M7()
   {
      ldc.i4 44
      ret
   }
   .method public virtual instance int32 M8<.ctor ([CustomTypes]C_I) M0>()
   {
      ldc.i4 45
      ret
   }
   .method public virtual instance int32 M9<M0>()
   {
      ldc.i4 46
      ret
   }
   .method public virtual instance int32 M10()
   {
      ldc.i4 47
      ret
   }
}
.class public C17
extends class C8<class [CustomTypes]C,class [CustomTypes]ClassWithCtor,int32>
implements class I0<int32,class [CustomTypes]C_I,class [CustomTypes]C_I>
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void class C8<class [CustomTypes]C,class [CustomTypes]ClassWithCtor,int32>::.ctor() 
      ret
   }
}
.class public C18<T0,class .ctor ([CustomTypes]Iface) T1>
extends C17

{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void C17::.ctor() 
      ret
   }
}
.class public C19<(class [mscorlib]System.Exception[]) T0,T1>
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
