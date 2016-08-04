// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
.assembly extern mscorlib { }
.assembly extern test { }

.assembly fieldgetter { }
.module fieldgetter.dll

//==========================================================================================

.class public auto ansi beforefieldinit Gen`1<T>
       extends [mscorlib]System.Object
{
  .field public int32 m_Field1
  .field public string m_Field2
  .field public !T m_Field3
  .field public static class [mscorlib]System.Collections.Generic.List`1<!T> m_Field4
  .field public static valuetype [mscorlib]System.Collections.Generic.KeyValuePair`2<!T,int32> m_Field5
  
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Gen`1::.ctor
} // end of class Gen`1

.class interface public abstract auto ansi IFieldGetter
{
  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenT_Field1() cil managed
  {
  } // end of method IFieldGetter::GetGenT_Field1

  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenT_Field2() cil managed
  {
  } // end of method IFieldGetter::GetGenT_Field2

  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenT_Field3() cil managed
  {
  } // end of method IFieldGetter::GetGenT_Field3

  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenT_Field4() cil managed
  {
  } // end of method IFieldGetter::GetGenT_Field4

  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenT_Field5() cil managed
  {
  } // end of method IFieldGetter::GetGenT_Field5

  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenDllT_Field1() cil managed
  {
  } // end of method IFieldGetter::GetGenDllT_Field1

  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenDllT_Field2() cil managed
  {
  } // end of method IFieldGetter::GetGenDllT_Field2

  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenDllT_Field3() cil managed
  {
  } // end of method IFieldGetter::GetGenDllT_Field3

  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenDllT_Field4() cil managed
  {
  } // end of method IFieldGetter::GetGenDllT_Field4

  .method public hidebysig newslot abstract virtual 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenDllT_Field5() cil managed
  {
  } // end of method IFieldGetter::GetGenDllT_Field5

} // end of class IFieldGetter

.class public auto ansi beforefieldinit FieldGetter`1<T>
       extends [mscorlib]System.Object
       implements IFieldGetter
{
  .method public hidebysig newslot virtual final 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenT_Field1() cil managed
  {
    ldtoken field int32 class Gen`1<!0>::m_Field1
    ldtoken class Gen`1<!0>
	call class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Reflection.FieldInfo::GetFieldFromHandle(valuetype [mscorlib]System.RuntimeFieldHandle, valuetype [mscorlib]System.RuntimeTypeHandle)
    ret
  } // end of method FieldGetter`1::GetGenT_Field1

  .method public hidebysig newslot virtual final 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenT_Field2() cil managed
  {
    ldtoken field string class Gen`1<!0>::m_Field2
    ldtoken class Gen`1<!0>
	call class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Reflection.FieldInfo::GetFieldFromHandle(valuetype [mscorlib]System.RuntimeFieldHandle, valuetype [mscorlib]System.RuntimeTypeHandle)
    ret
  } // end of method FieldGetter`1::GetGenT_Field2

  .method public hidebysig newslot virtual final 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenT_Field3() cil managed
  {
    ldtoken field !0 class Gen`1<!0>::m_Field3
    ldtoken class Gen`1<!0>
	call class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Reflection.FieldInfo::GetFieldFromHandle(valuetype [mscorlib]System.RuntimeFieldHandle, valuetype [mscorlib]System.RuntimeTypeHandle)
    ret
  } // end of method FieldGetter`1::GetGenT_Field3

  .method public hidebysig newslot virtual final 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenT_Field4() cil managed
  {
    ldtoken field class [mscorlib]System.Collections.Generic.List`1<!0> class Gen`1<!0>::m_Field4
    ldtoken class Gen`1<!0>
	call class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Reflection.FieldInfo::GetFieldFromHandle(valuetype [mscorlib]System.RuntimeFieldHandle, valuetype [mscorlib]System.RuntimeTypeHandle)
    ret
  } // end of method FieldGetter`1::GetGenT_Field4

  .method public hidebysig newslot virtual final 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenT_Field5() cil managed
  {
    ldtoken field valuetype [mscorlib]System.Collections.Generic.KeyValuePair`2<!0,int32> class Gen`1<!0>::m_Field5
    ldtoken class Gen`1<!0>
	call class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Reflection.FieldInfo::GetFieldFromHandle(valuetype [mscorlib]System.RuntimeFieldHandle, valuetype [mscorlib]System.RuntimeTypeHandle)
    ret
  } // end of method FieldGetter`1::GetGenT_Field5
  
  
  
  

  .method public hidebysig newslot virtual final 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenDllT_Field1() cil managed
  {
    ldtoken field string class [test]MyGeneric`2<!0,!0>::m_Field1
    ldtoken class [test]MyGeneric`2<!0,!0>
	call class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Reflection.FieldInfo::GetFieldFromHandle(valuetype [mscorlib]System.RuntimeFieldHandle, valuetype [mscorlib]System.RuntimeTypeHandle)
    ret
  } // end of method FieldGetter`1::GetGenDllT_Field1

  .method public hidebysig newslot virtual final 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenDllT_Field2() cil managed
  {
    ldtoken field !0 class [test]MyGeneric`2<!0,!0>::m_Field2
    ldtoken class [test]MyGeneric`2<!0,!0>
	call class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Reflection.FieldInfo::GetFieldFromHandle(valuetype [mscorlib]System.RuntimeFieldHandle, valuetype [mscorlib]System.RuntimeTypeHandle)
    ret
  } // end of method FieldGetter`1::GetGenDllT_Field2

  .method public hidebysig newslot virtual final 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenDllT_Field3() cil managed
  {
    ldtoken field class [mscorlib]System.Collections.Generic.List`1<!0> class [test]MyGeneric`2<!0,!0>::m_Field3
    ldtoken class [test]MyGeneric`2<!0,!0>
	call class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Reflection.FieldInfo::GetFieldFromHandle(valuetype [mscorlib]System.RuntimeFieldHandle, valuetype [mscorlib]System.RuntimeTypeHandle)
    ret
  } // end of method FieldGetter`1::GetGenDllT_Field3

  .method public hidebysig newslot virtual final 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenDllT_Field4() cil managed
  {
    ldtoken field valuetype [mscorlib]System.Collections.Generic.KeyValuePair`2<!0,int32> class [test]MyGeneric`2<!0,!0>::m_Field4
    ldtoken class [test]MyGeneric`2<!0,!0>
	call class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Reflection.FieldInfo::GetFieldFromHandle(valuetype [mscorlib]System.RuntimeFieldHandle, valuetype [mscorlib]System.RuntimeTypeHandle)
    ret
  } // end of method FieldGetter`1::GetGenDllT_Field4

  .method public hidebysig newslot virtual final 
          instance class [mscorlib]System.Reflection.FieldInfo 
          GetGenDllT_Field5() cil managed
  {
    ldtoken field int32 class [test]MyGeneric`2<!0,!0>::m_Field5
    ldtoken class [test]MyGeneric`2<!0,!0>
	call class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Reflection.FieldInfo::GetFieldFromHandle(valuetype [mscorlib]System.RuntimeFieldHandle, valuetype [mscorlib]System.RuntimeTypeHandle)
    ret
  } // end of method FieldGetter`1::GetGenDllT_Field5

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method FieldGetter`1::.ctor

} // end of class FieldGetter`1

