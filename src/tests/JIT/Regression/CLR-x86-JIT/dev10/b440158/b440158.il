// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.




.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .ver 2:0:0:0
}
.assembly Contains
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       
  .hash algorithm 0x00008004
  .ver 2:0:30414:3
}
.assembly extern xunit.core {}
.module Contains.exe
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       
.corflags 0x00000001    



.class interface public abstract auto ansi IComparableValue
{
  .method public hidebysig newslot specialname abstract virtual 
          instance class [mscorlib]System.IComparable 
          get_Val() cil managed
  {
  } 

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_Val(class [mscorlib]System.IComparable 'value') cil managed
  {
  } 

  .property instance class [mscorlib]System.IComparable
          Val()
  {
    .get instance class [mscorlib]System.IComparable IComparableValue::get_Val()
    .set instance void IComparableValue::set_Val(class [mscorlib]System.IComparable)
  } 
} 

.class public auto ansi beforefieldinit Driver`2<(IComparableValue) K,V>
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void 
          NonGenericIDictionaryContainsValidations(!K[] keys,
                                                   !V[] values) cil managed
  {
    .maxstack  4
    .locals init (class [mscorlib]System.Collections.Generic.Dictionary`2<!K,!V> V_0,
             class [mscorlib]System.Collections.IDictionary V_1,
             int32 V_2,
             class [mscorlib]System.Exception V_3,
             bool V_4)
    IL_0000:  nop
    IL_0001:  newobj     instance void class ValueKeyComparer`1<!K>::.ctor()
    IL_0006:  newobj     instance void class [mscorlib]System.Collections.Generic.Dictionary`2<!K,!V>::.ctor(class [mscorlib]System.Collections.Generic.IEqualityComparer`1<!0>)
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  stloc.1
    IL_000e:  ldc.i4.0
    IL_000f:  stloc.2
    IL_0010:  br.s       IL_002d

    IL_0012:  nop
    IL_0013:  ldloc.0
    IL_0014:  ldarg.1
    IL_0015:  ldloc.2
    IL_0016:  ldelem     !K
    IL_001b:  ldarg.2
    IL_001c:  ldloc.2
    IL_001d:  ldelem     !V
    IL_0022:  callvirt   instance void class [mscorlib]System.Collections.Generic.Dictionary`2<!K,!V>::Add(!0,
                                                                                                           !1)
    IL_0027:  nop
    IL_0028:  nop
    IL_0029:  ldloc.2
    IL_002a:  ldc.i4.1
    IL_002b:  add
    IL_002c:  stloc.2
    IL_002d:  ldloc.2
    IL_002e:  ldarg.1
    IL_002f:  ldlen
    IL_0030:  conv.i4
    IL_0031:  clt
    IL_0033:  stloc.s    V_4
    IL_0035:  ldloc.s    V_4
    IL_0037:  brtrue.s   IL_0012

    IL_0039:  ldtoken    !K
    IL_003e:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0043:  ldtoken    [mscorlib]System.ValueType
    IL_0048:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_004d:  callvirt   instance bool [mscorlib]System.Type::IsSubclassOf(class [mscorlib]System.Type)
    IL_0052:  stloc.s    V_4
    IL_0054:  ldloc.s    V_4
    IL_0056:  brtrue.s   IL_0071

    IL_0058:  nop
    .try
    {
      IL_0059:  nop
      IL_005a:  ldloc.1
      IL_005b:  ldnull
      IL_005c:  unbox.any  !K
      IL_0061:  box        !K
      IL_0066:  callvirt   instance bool [mscorlib]System.Collections.IDictionary::Contains(object)
      IL_006b:  pop
      IL_006c:  leave.s    IL_0071

    }  
    catch [mscorlib]System.ArgumentException 
    {
      IL_006e:  pop
      IL_006f:  leave.s    IL_0071

    }  
    IL_0071:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 

.class public auto ansi beforefieldinit ValueKeyComparer`1<(IComparableValue) T>
       extends [mscorlib]System.Object
       implements class [mscorlib]System.Collections.Generic.IEqualityComparer`1<!T>
{
  .method public hidebysig newslot virtual final 
          instance bool  Equals(!T x,
                                !T y) cil managed
  {
    .maxstack  3
    .locals init (bool V_0,
             bool V_1)
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  ldarg.1
    IL_0003:  box        !T
    IL_0008:  ceq
    IL_000a:  ldc.i4.0
    IL_000b:  ceq
    IL_000d:  stloc.1
    IL_000e:  ldloc.1
    IL_000f:  brtrue.s   IL_001d

    IL_0011:  ldnull
    IL_0012:  ldarg.2
    IL_0013:  box        !T
    IL_0018:  ceq
    IL_001a:  stloc.0
    IL_001b:  br.s       IL_006c

    IL_001d:  ldnull
    IL_001e:  ldarga.s   x
    IL_0020:  constrained. !T
    IL_0026:  callvirt   instance class [mscorlib]System.IComparable IComparableValue::get_Val()
    IL_002b:  ceq
    IL_002d:  ldc.i4.0
    IL_002e:  ceq
    IL_0030:  stloc.1
    IL_0031:  ldloc.1
    IL_0032:  brtrue.s   IL_0047

    IL_0034:  ldnull
    IL_0035:  ldarga.s   y
    IL_0037:  constrained. !T
    IL_003d:  callvirt   instance class [mscorlib]System.IComparable IComparableValue::get_Val()
    IL_0042:  ceq
    IL_0044:  stloc.0
    IL_0045:  br.s       IL_006c

    IL_0047:  ldc.i4.0
    IL_0048:  ldarga.s   x
    IL_004a:  constrained. !T
    IL_0050:  callvirt   instance class [mscorlib]System.IComparable IComparableValue::get_Val()
    IL_0055:  ldarga.s   y
    IL_0057:  constrained. !T
    IL_005d:  callvirt   instance class [mscorlib]System.IComparable IComparableValue::get_Val()
    IL_0062:  callvirt   instance int32 [mscorlib]System.IComparable::CompareTo(object)
    IL_0067:  ceq
    IL_0069:  stloc.0
    IL_006a:  br.s       IL_006c

    IL_006c:  ldloc.0
    IL_006d:  ret
  } 

  .method public hidebysig newslot virtual final 
          instance int32  GetHashCode(!T obj) cil managed
  {
    .maxstack  2
    .locals init (string V_0,
             int32 V_1,
             char V_2,
             int32 V_3,
             string V_4,
             int32 V_5,
             bool V_6)
    IL_0000:  nop
    IL_0001:  ldarga.s   obj
    IL_0003:  constrained. !T
    IL_0009:  callvirt   instance class [mscorlib]System.IComparable IComparableValue::get_Val()
    IL_000e:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0013:  stloc.0
    IL_0014:  ldc.i4.0
    IL_0015:  stloc.1
    IL_0016:  nop
    IL_0017:  ldloc.0
    IL_0018:  stloc.s    V_4
    IL_001a:  ldc.i4.0
    IL_001b:  stloc.s    V_5
    IL_001d:  br.s       IL_0035

    IL_001f:  ldloc.s    V_4
    IL_0021:  ldloc.s    V_5
    IL_0023:  callvirt   instance char [mscorlib]System.String::get_Chars(int32)
    IL_0028:  stloc.2
    IL_0029:  nop
    IL_002a:  ldloc.1
    IL_002b:  ldloc.2
    IL_002c:  add
    IL_002d:  stloc.1
    IL_002e:  nop
    IL_002f:  ldloc.s    V_5
    IL_0031:  ldc.i4.1
    IL_0032:  add
    IL_0033:  stloc.s    V_5
    IL_0035:  ldloc.s    V_5
    IL_0037:  ldloc.s    V_4
    IL_0039:  callvirt   instance int32 [mscorlib]System.String::get_Length()
    IL_003e:  clt
    IL_0040:  stloc.s    V_6
    IL_0042:  ldloc.s    V_6
    IL_0044:  brtrue.s   IL_001f

    IL_0046:  ldloc.1
    IL_0047:  ldc.i4.0
    IL_0048:  clt
    IL_004a:  ldc.i4.0
    IL_004b:  ceq
    IL_004d:  stloc.s    V_6
    IL_004f:  ldloc.s    V_6
    IL_0051:  brtrue.s   IL_0058

    IL_0053:  ldloc.1
    IL_0054:  neg
    IL_0055:  stloc.3
    IL_0056:  br.s       IL_005c

    IL_0058:  ldloc.1
    IL_0059:  stloc.3
    IL_005a:  br.s       IL_005c

    IL_005c:  ldloc.3
    IL_005d:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 

.class public auto ansi beforefieldinit Contains
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  5
    .locals init (class Driver`2<valuetype ValX1`1<string>,class RefX1`1<int32>> V_0)
    IL_0000:  nop
    .try
    {
      IL_0001:  newobj     instance void class Driver`2<valuetype ValX1`1<string>,class RefX1`1<int32>>::.ctor()
      IL_0006:  stloc.s    V_0
      IL_0008:  nop
      IL_0009:  ldloc.s    V_0
      IL_000b:  ldc.i4.0
      IL_000c:  newarr     valuetype ValX1`1<string>
      IL_0011:  ldc.i4.0
      IL_0012:  newarr     class RefX1`1<int32>
      IL_0017:  callvirt   instance void class Driver`2<valuetype ValX1`1<string>,class RefX1`1<int32>>::NonGenericIDictionaryContainsValidations(!0[],
                                                                                                                                                  !1[])
      IL_001c:  leave.s    IL_0021

    }  
    catch [mscorlib]System.Exception 
    {
      IL_001e:  pop
      IL_001f:  leave.s    IL_0028

    }  
    IL_0021:  nop
    IL_0022:  ldc.i4     0x64
    IL_0027:  ret

    IL_0028:  ldc.i4     0x64
    IL_002d:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 

.class public sequential ansi sealed beforefieldinit ValX1`1<([mscorlib]System.IComparable) T>
       extends [mscorlib]System.ValueType
       implements IComparableValue
{
  .field private !T _val
  .method public hidebysig newslot specialname virtual final 
          instance class [mscorlib]System.IComparable 
          get_Val() cil managed
  {
    .maxstack  1
    .locals init (class [mscorlib]System.IComparable V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      !0 valuetype ValX1`1<!T>::_val
    IL_0007:  box        !T
    IL_000c:  stloc.0
    IL_000d:  br.s       IL_000f

    IL_000f:  ldloc.0
    IL_0010:  ret
  } 

  .method public hidebysig newslot specialname virtual final 
          instance void  set_Val(class [mscorlib]System.IComparable 'value') cil managed
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  unbox.any  !T
    IL_0008:  stfld      !0 valuetype ValX1`1<!T>::_val
    IL_000d:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(!T t) cil managed
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  stfld      !0 valuetype ValX1`1<!T>::_val
    IL_0008:  ret
  } 

  .property instance class [mscorlib]System.IComparable
          Val()
  {
    .get instance class [mscorlib]System.IComparable ValX1`1::get_Val()
    .set instance void ValX1`1::set_Val(class [mscorlib]System.IComparable)
  } 
} 

.class public auto ansi beforefieldinit RefX1`1<([mscorlib]System.IComparable) T>
       extends [mscorlib]System.Object
       implements IComparableValue
{
  .field private !T _val
  .method public hidebysig newslot specialname virtual final 
          instance class [mscorlib]System.IComparable 
          get_Val() cil managed
  {
    .maxstack  1
    .locals init (class [mscorlib]System.IComparable V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      !0 class RefX1`1<!T>::_val
    IL_0007:  box        !T
    IL_000c:  stloc.0
    IL_000d:  br.s       IL_000f

    IL_000f:  ldloc.0
    IL_0010:  ret
  } 

  .method public hidebysig newslot specialname virtual final 
          instance void  set_Val(class [mscorlib]System.IComparable 'value') cil managed
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  unbox.any  !T
    IL_0008:  stfld      !0 class RefX1`1<!T>::_val
    IL_000d:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(!T t) cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      !0 class RefX1`1<!T>::_val
    IL_000f:  nop
    IL_0010:  ret
  } 

  .property instance class [mscorlib]System.IComparable
          Val()
  {
    .get instance class [mscorlib]System.IComparable RefX1`1::get_Val()
    .set instance void RefX1`1::set_Val(class [mscorlib]System.IComparable)
  } 
} 
