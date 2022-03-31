// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
}

.assembly 'library' { }

.class interface public abstract auto ansi IFaceNonGeneric
{
    .method public newslot virtual abstract static void NormalMethod() {}
    .method public newslot virtual abstract static void GenericMethod<U>() {}
}

.class interface public abstract auto ansi IFaceGeneric`1<T>
{
    .method public newslot virtual abstract static void NormalMethod() {}
    .method public newslot virtual abstract static void GenericMethod<U>() {}
}

.class interface public abstract auto ansi IFaceCuriouslyRecurringGeneric`1<(class IFaceCuriouslyRecurringGeneric`1<!0>) T> 
{
    .method public newslot virtual abstract static void NormalMethod() {}
    .method public newslot virtual abstract static void GenericMethod<U>() {}
}

.class public sealed auto ansi GenericStruct`1<T>
       extends[System.Runtime] System.ValueType
{
}
.class public auto ansi NonGenericClass
       extends [System.Runtime]System.Object
       implements IFaceNonGeneric,
                  class IFaceGeneric`1<string>,
                  class IFaceGeneric`1<object>,
                  class IFaceCuriouslyRecurringGeneric`1<class NonGenericClass>
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  }
  .method public static void 'IFaceNonGeneric.NormalMethod'() cil managed noinlining
  {
    .override method void IFaceNonGeneric::NormalMethod()
    ldtoken class NonGenericClass
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceNonGeneric.NormalMethod'"
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceNonGeneric.NormalMethod'
  .method public static void 'IFaceNonGeneric.GenericMethod'<U>() cil managed noinlining
  {
    .override method void IFaceNonGeneric::GenericMethod<[1]>()
    ldtoken class NonGenericClass
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceNonGeneric.GenericMethod'"
    ldstr "<"
    ldtoken !!0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    ldstr ">"
    call string[System.Runtime] System.String::Concat(string, string, string,string)
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceNonGeneric.GenericMethod'<U>
  .method public static void 'IFaceGeneric`1<string>.NormalMethod'() cil managed noinlining
  {
    .override method void class IFaceGeneric`1<string>::NormalMethod()
    ldtoken class NonGenericClass
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceGeneric`1<string>.NormalMethod'"
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceGeneric`1<string>.NormalMethod'
  .method public static void 'IFaceGeneric`1<string>.GenericMethod'<U>() cil managed noinlining
  {
    .override method void class IFaceGeneric`1<string>::GenericMethod<[1]>()
    ldtoken class NonGenericClass
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceGeneric`1<string>.GenericMethod'"
    ldstr "<"
    ldtoken !!0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    ldstr ">"
    call string[System.Runtime] System.String::Concat(string, string, string,string)
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceGeneric`1<string>.GenericMethod'<U>
  .method public static void 'IFaceGeneric`1<object>.NormalMethod'() cil managed noinlining
  {
    .override method void class IFaceGeneric`1<object>::NormalMethod()
    ldtoken class NonGenericClass
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceGeneric`1<object>.NormalMethod'"
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceGeneric`1<object>.NormalMethod'
  .method public static void 'IFaceGeneric`1<object>.GenericMethod'<U>() cil managed noinlining
  {
    .override method void class IFaceGeneric`1<object>::GenericMethod<[1]>()
    ldtoken class NonGenericClass
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceGeneric`1<object>.GenericMethod'"
    ldstr "<"
    ldtoken !!0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    ldstr ">"
    call string[System.Runtime] System.String::Concat(string, string, string,string)
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceGeneric`1<object>.GenericMethod'<U>
  .method public static void 'IFaceCuriouslyRecurringGeneric`1<class NonGenericClass>.NormalMethod'() cil managed noinlining
  {
    .override method void class IFaceCuriouslyRecurringGeneric`1<class NonGenericClass>::NormalMethod()
    ldtoken class NonGenericClass
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceCuriouslyRecurringGeneric`1<class NonGenericClass>.NormalMethod'"
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceCuriouslyRecurringGeneric`1<class NonGenericClass>.NormalMethod'
  .method public static void 'IFaceCuriouslyRecurringGeneric`1<class NonGenericClass>.GenericMethod'<U>() cil managed noinlining
  {
    .override method void class IFaceCuriouslyRecurringGeneric`1<class NonGenericClass>::GenericMethod<[1]>()
    ldtoken class NonGenericClass
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceCuriouslyRecurringGeneric`1<class NonGenericClass>.GenericMethod'"
    ldstr "<"
    ldtoken !!0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    ldstr ">"
    call string[System.Runtime] System.String::Concat(string, string, string,string)
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceCuriouslyRecurringGeneric`1<class NonGenericClass>.GenericMethod'<U>

} // end of class NonGenericClass
.class public auto ansi sealed NonGenericValuetype
       extends [System.Runtime]System.ValueType
       implements IFaceNonGeneric,
                  class IFaceGeneric`1<string>,
                  class IFaceGeneric`1<object>,
                  class IFaceCuriouslyRecurringGeneric`1<valuetype NonGenericValuetype>
{
  .method public static void 'IFaceNonGeneric.NormalMethod'() cil managed noinlining
  {
    .override method void IFaceNonGeneric::NormalMethod()
    ldtoken valuetype NonGenericValuetype
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceNonGeneric.NormalMethod'"
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceNonGeneric.NormalMethod'
  .method public static void 'IFaceNonGeneric.GenericMethod'<U>() cil managed noinlining
  {
    .override method void IFaceNonGeneric::GenericMethod<[1]>()
    ldtoken valuetype NonGenericValuetype
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceNonGeneric.GenericMethod'"
    ldstr "<"
    ldtoken !!0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    ldstr ">"
    call string[System.Runtime] System.String::Concat(string, string, string,string)
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceNonGeneric.GenericMethod'<U>
  .method public static void 'IFaceGeneric`1<string>.NormalMethod'() cil managed noinlining
  {
    .override method void class IFaceGeneric`1<string>::NormalMethod()
    ldtoken valuetype NonGenericValuetype
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceGeneric`1<string>.NormalMethod'"
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceGeneric`1<string>.NormalMethod'
  .method public static void 'IFaceGeneric`1<string>.GenericMethod'<U>() cil managed noinlining
  {
    .override method void class IFaceGeneric`1<string>::GenericMethod<[1]>()
    ldtoken valuetype NonGenericValuetype
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceGeneric`1<string>.GenericMethod'"
    ldstr "<"
    ldtoken !!0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    ldstr ">"
    call string[System.Runtime] System.String::Concat(string, string, string,string)
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceGeneric`1<string>.GenericMethod'<U>
  .method public static void 'IFaceGeneric`1<object>.NormalMethod'() cil managed noinlining
  {
    .override method void class IFaceGeneric`1<object>::NormalMethod()
    ldtoken valuetype NonGenericValuetype
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceGeneric`1<object>.NormalMethod'"
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceGeneric`1<object>.NormalMethod'
  .method public static void 'IFaceGeneric`1<object>.GenericMethod'<U>() cil managed noinlining
  {
    .override method void class IFaceGeneric`1<object>::GenericMethod<[1]>()
    ldtoken valuetype NonGenericValuetype
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceGeneric`1<object>.GenericMethod'"
    ldstr "<"
    ldtoken !!0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    ldstr ">"
    call string[System.Runtime] System.String::Concat(string, string, string,string)
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceGeneric`1<object>.GenericMethod'<U>
  .method public static void 'IFaceCuriouslyRecurringGeneric`1<valuetype NonGenericValuetype>.NormalMethod'() cil managed noinlining
  {
    .override method void class IFaceCuriouslyRecurringGeneric`1<valuetype NonGenericValuetype>::NormalMethod()
    ldtoken valuetype NonGenericValuetype
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceCuriouslyRecurringGeneric`1<valuetype NonGenericValuetype>.NormalMethod'"
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceCuriouslyRecurringGeneric`1<valuetype NonGenericValuetype>.NormalMethod'
  .method public static void 'IFaceCuriouslyRecurringGeneric`1<valuetype NonGenericValuetype>.GenericMethod'<U>() cil managed noinlining
  {
    .override method void class IFaceCuriouslyRecurringGeneric`1<valuetype NonGenericValuetype>::GenericMethod<[1]>()
    ldtoken valuetype NonGenericValuetype
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceCuriouslyRecurringGeneric`1<valuetype NonGenericValuetype>.GenericMethod'"
    ldstr "<"
    ldtoken !!0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    ldstr ">"
    call string[System.Runtime] System.String::Concat(string, string, string,string)
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceCuriouslyRecurringGeneric`1<valuetype NonGenericValuetype>.GenericMethod'<U>

} // end of class NonGenericValuetype
.class public auto ansi GenericClass`1<T>
       extends [System.Runtime]System.Object
       implements IFaceNonGeneric,
                  class IFaceGeneric`1<string>,
                  class IFaceGeneric`1<object>,
                  class IFaceCuriouslyRecurringGeneric`1<class GenericClass`1<!0>>
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  }
  .method public static void 'IFaceNonGeneric.NormalMethod'() cil managed noinlining
  {
    .override method void IFaceNonGeneric::NormalMethod()
    ldtoken class GenericClass`1<!0>
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceNonGeneric.NormalMethod'"
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceNonGeneric.NormalMethod'
  .method public static void 'IFaceNonGeneric.GenericMethod'<U>() cil managed noinlining
  {
    .override method void IFaceNonGeneric::GenericMethod<[1]>()
    ldtoken class GenericClass`1<!0>
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceNonGeneric.GenericMethod'"
    ldstr "<"
    ldtoken !!0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    ldstr ">"
    call string[System.Runtime] System.String::Concat(string, string, string,string)
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceNonGeneric.GenericMethod'<U>
  .method public static void 'IFaceGeneric`1<string>.NormalMethod'() cil managed noinlining
  {
    .override method void class IFaceGeneric`1<string>::NormalMethod()
    ldtoken class GenericClass`1<!0>
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceGeneric`1<string>.NormalMethod'"
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceGeneric`1<string>.NormalMethod'
  .method public static void 'IFaceGeneric`1<string>.GenericMethod'<U>() cil managed noinlining
  {
    .override method void class IFaceGeneric`1<string>::GenericMethod<[1]>()
    ldtoken class GenericClass`1<!0>
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceGeneric`1<string>.GenericMethod'"
    ldstr "<"
    ldtoken !!0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    ldstr ">"
    call string[System.Runtime] System.String::Concat(string, string, string,string)
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceGeneric`1<string>.GenericMethod'<U>
  .method public static void 'IFaceGeneric`1<object>.NormalMethod'() cil managed noinlining
  {
    .override method void class IFaceGeneric`1<object>::NormalMethod()
    ldtoken class GenericClass`1<!0>
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceGeneric`1<object>.NormalMethod'"
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceGeneric`1<object>.NormalMethod'
  .method public static void 'IFaceGeneric`1<object>.GenericMethod'<U>() cil managed noinlining
  {
    .override method void class IFaceGeneric`1<object>::GenericMethod<[1]>()
    ldtoken class GenericClass`1<!0>
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceGeneric`1<object>.GenericMethod'"
    ldstr "<"
    ldtoken !!0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    ldstr ">"
    call string[System.Runtime] System.String::Concat(string, string, string,string)
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceGeneric`1<object>.GenericMethod'<U>
  .method public static void 'IFaceCuriouslyRecurringGeneric`1<class GenericClass`1<!0>>.NormalMethod'() cil managed noinlining
  {
    .override method void class IFaceCuriouslyRecurringGeneric`1<class GenericClass`1<!0>>::NormalMethod()
    ldtoken class GenericClass`1<!0>
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceCuriouslyRecurringGeneric`1<class GenericClass`1<!0>>.NormalMethod'"
    ldstr "!0"
    ldtoken !0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    call instance string [System.Runtime]System.String::Replace(string, string)
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceCuriouslyRecurringGeneric`1<class GenericClass`1<!0>>.NormalMethod'
  .method public static void 'IFaceCuriouslyRecurringGeneric`1<class GenericClass`1<!0>>.GenericMethod'<U>() cil managed noinlining
  {
    .override method void class IFaceCuriouslyRecurringGeneric`1<class GenericClass`1<!0>>::GenericMethod<[1]>()
    ldtoken class GenericClass`1<!0>
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceCuriouslyRecurringGeneric`1<class GenericClass`1<!0>>.GenericMethod'"
    ldstr "!0"
    ldtoken !0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    call instance string [System.Runtime]System.String::Replace(string, string)
    ldstr "<"
    ldtoken !!0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    ldstr ">"
    call string[System.Runtime] System.String::Concat(string, string, string,string)
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceCuriouslyRecurringGeneric`1<class GenericClass`1<!0>>.GenericMethod'<U>

} // end of class GenericClass`1
.class public auto ansi sealed GenericValuetype`1<T>
       extends [System.Runtime]System.ValueType
       implements IFaceNonGeneric,
                  class IFaceGeneric`1<string>,
                  class IFaceGeneric`1<object>,
                  class IFaceCuriouslyRecurringGeneric`1<valuetype GenericValuetype`1<!0>>
{
  .method public static void 'IFaceNonGeneric.NormalMethod'() cil managed noinlining
  {
    .override method void IFaceNonGeneric::NormalMethod()
    ldtoken valuetype GenericValuetype`1<!0>
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceNonGeneric.NormalMethod'"
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceNonGeneric.NormalMethod'
  .method public static void 'IFaceNonGeneric.GenericMethod'<U>() cil managed noinlining
  {
    .override method void IFaceNonGeneric::GenericMethod<[1]>()
    ldtoken valuetype GenericValuetype`1<!0>
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceNonGeneric.GenericMethod'"
    ldstr "<"
    ldtoken !!0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    ldstr ">"
    call string[System.Runtime] System.String::Concat(string, string, string,string)
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceNonGeneric.GenericMethod'<U>
  .method public static void 'IFaceGeneric`1<string>.NormalMethod'() cil managed noinlining
  {
    .override method void class IFaceGeneric`1<string>::NormalMethod()
    ldtoken valuetype GenericValuetype`1<!0>
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceGeneric`1<string>.NormalMethod'"
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceGeneric`1<string>.NormalMethod'
  .method public static void 'IFaceGeneric`1<string>.GenericMethod'<U>() cil managed noinlining
  {
    .override method void class IFaceGeneric`1<string>::GenericMethod<[1]>()
    ldtoken valuetype GenericValuetype`1<!0>
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceGeneric`1<string>.GenericMethod'"
    ldstr "<"
    ldtoken !!0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    ldstr ">"
    call string[System.Runtime] System.String::Concat(string, string, string,string)
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceGeneric`1<string>.GenericMethod'<U>
  .method public static void 'IFaceGeneric`1<object>.NormalMethod'() cil managed noinlining
  {
    .override method void class IFaceGeneric`1<object>::NormalMethod()
    ldtoken valuetype GenericValuetype`1<!0>
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceGeneric`1<object>.NormalMethod'"
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceGeneric`1<object>.NormalMethod'
  .method public static void 'IFaceGeneric`1<object>.GenericMethod'<U>() cil managed noinlining
  {
    .override method void class IFaceGeneric`1<object>::GenericMethod<[1]>()
    ldtoken valuetype GenericValuetype`1<!0>
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceGeneric`1<object>.GenericMethod'"
    ldstr "<"
    ldtoken !!0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    ldstr ">"
    call string[System.Runtime] System.String::Concat(string, string, string,string)
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceGeneric`1<object>.GenericMethod'<U>
  .method public static void 'IFaceCuriouslyRecurringGeneric`1<valuetype GenericValuetype`1<!0>>.NormalMethod'() cil managed noinlining
  {
    .override method void class IFaceCuriouslyRecurringGeneric`1<valuetype GenericValuetype`1<!0>>::NormalMethod()
    ldtoken valuetype GenericValuetype`1<!0>
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceCuriouslyRecurringGeneric`1<valuetype GenericValuetype`1<!0>>.NormalMethod'"
    ldstr "!0"
    ldtoken !0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    call instance string [System.Runtime]System.String::Replace(string, string)
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceCuriouslyRecurringGeneric`1<valuetype GenericValuetype`1<!0>>.NormalMethod'
  .method public static void 'IFaceCuriouslyRecurringGeneric`1<valuetype GenericValuetype`1<!0>>.GenericMethod'<U>() cil managed noinlining
  {
    .override method void class IFaceCuriouslyRecurringGeneric`1<valuetype GenericValuetype`1<!0>>::GenericMethod<[1]>()
    ldtoken valuetype GenericValuetype`1<!0>
    call string Statics::MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldstr "'IFaceCuriouslyRecurringGeneric`1<valuetype GenericValuetype`1<!0>>.GenericMethod'"
    ldstr "!0"
    ldtoken !0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    call instance string [System.Runtime]System.String::Replace(string, string)
    ldstr "<"
    ldtoken !!0
    call string Statics::MakeName(valuetype[System.Runtime]System.RuntimeTypeHandle)
    ldstr ">"
    call string[System.Runtime] System.String::Concat(string, string, string,string)
    call string[System.Runtime] System.String::Concat(string, string)
    stsfld string Statics::String
    ret
  } // end of method 'IFaceCuriouslyRecurringGeneric`1<valuetype GenericValuetype`1<!0>>.GenericMethod'<U>
} // end of class GenericValuetype`1

.class public auto ansi TestEntrypoint
       extends [System.Runtime]System.Object
{
  .method public static void Test_Call_NonGenericNonGenericClass_NonGeneric_NonGeneric_NormalMethod() cil managed noinlining
  {
    constrained. class NonGenericClass
    call void class IFaceNonGeneric::NormalMethod()
    ldstr "Call_NonGenericNonGenericClass_NonGeneric_NonGeneric_NormalMethod"
    ldstr "class NonGenericClass'IFaceNonGeneric.NormalMethod'"
    call void Statics::CheckForFailure(string,string)
    ret
  } // end of method Test_Call_NonGenericNonGenericClass_NonGeneric_NonGeneric_NormalMethod
  .method public static void Test_Call_NonGenericNonGenericClass_NonGeneric_NonGeneric_GenericMethodOverInt() cil managed noinlining
  {
    constrained. class NonGenericClass
    call void class IFaceNonGeneric::GenericMethod<int32>()
    ldstr "Call_NonGenericNonGenericClass_NonGeneric_NonGeneric_GenericMethodOverInt"
    ldstr "class NonGenericClass'IFaceNonGeneric.GenericMethod'<int32>"
    call void [GenericContextCommonCs]Statics::CheckForFailure(string,string)
    ret
  } // end of method Test_Call_NonGenericNonGenericClass_NonGeneric_NonGeneric_GenericMethodOverInt
  .method public static int32 Test() cil managed noinlining
  {
    .locals init (class [System.Runtime]System.Exception V_0)
    .try {
        call void TestEntrypoint::Test_Call_NonGenericNonGenericClass_NonGeneric_NonGeneric_NormalMethod()
        leave.s Call_NonGenericNonGenericClass_NonGeneric_NonGeneric_NormalMethodDone
    } catch [System.Runtime]System.Exception {
        stloc.0
        ldstr "Call_NonGenericNonGenericClass_NonGeneric_NonGeneric_NormalMethod"
        ldstr "class NonGenericClass'IFaceNonGeneric.NormalMethod'"
        ldloc.0
        callvirt   instance string [System.Runtime]System.Object::ToString()
        call void Statics::CheckForFailure(string,string,string)
        leave.s Call_NonGenericNonGenericClass_NonGeneric_NonGeneric_NormalMethodDone
    }
Call_NonGenericNonGenericClass_NonGeneric_NonGeneric_NormalMethodDone: nop
    .try {
        call void TestEntrypoint::Test_Call_NonGenericNonGenericClass_NonGeneric_NonGeneric_GenericMethodOverInt()
        leave.s Call_NonGenericNonGenericClass_NonGeneric_NonGeneric_GenericMethodOverIntDone
    } catch [System.Runtime]System.Exception {
        stloc.0
        ldstr "Call_NonGenericNonGenericClass_NonGeneric_NonGeneric_GenericMethodOverInt"
        ldstr "class NonGenericClass'IFaceNonGeneric.GenericMethod'<int32>"
        ldloc.0
        callvirt   instance string [System.Runtime]System.Object::ToString()
        call void [GenericContextCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s Call_NonGenericNonGenericClass_NonGeneric_NonGeneric_GenericMethodOverIntDone
    }
Call_NonGenericNonGenericClass_NonGeneric_NonGeneric_GenericMethodOverIntDone: nop
    call int32 Statics::ReportResults()
    ret
  } // end of method Test
} // end of class TestEntrypoint
.class public abstract auto ansi sealed beforefieldinit Statics
       extends [System.Runtime]System.Object
{
  .field public static string String
  .field public static int32 Failures
  .field public static int32 Successes
  .field public static string BangBang1Param
  .field public static native int modreq([System.Runtime]System.Runtime.CompilerServices.IsVolatile) FtnHolder
  .field public static class [System.Runtime]System.Action modreq([System.Runtime]System.Runtime.CompilerServices.IsVolatile) ActionHolder
  .method public hidebysig static void  CheckForFailure(string scenario,
                                                        string expectedResult,
                                                        string actualResult) cil managed
  {
    // Code size       145 (0x91)
    .maxstack  6
    .locals init ([0] bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  call       bool [System.Runtime]System.String::op_Inequality(string,
                                                                           string)
    IL_0008:  stloc.0
    IL_0009:  ldloc.0
    IL_000a:  brfalse.s  IL_005d

    IL_000c:  nop
    IL_000d:  ldstr      "FAILURE({0}) - Scenario {1} failed - expected {2},"
    + " got {3}"
    IL_0012:  ldc.i4.4
    IL_0013:  newarr     [System.Runtime]System.Object
    IL_0018:  dup
    IL_0019:  ldc.i4.0
    IL_001a:  ldsfld     int32 Statics::Failures
    IL_001f:  box        [System.Runtime]System.Int32
    IL_0024:  stelem.ref
    IL_0025:  dup
    IL_0026:  ldc.i4.1
    IL_0027:  ldarg.0
    IL_0028:  stelem.ref
    IL_0029:  dup
    IL_002a:  ldc.i4.2
    IL_002b:  ldarg.1
    IL_002c:  dup
    IL_002d:  brtrue.s   IL_0035

    IL_002f:  pop
    IL_0030:  ldstr      "<null>"
    IL_0035:  stelem.ref
    IL_0036:  dup
    IL_0037:  ldc.i4.3
    IL_0038:  ldarg.2
    IL_0039:  dup
    IL_003a:  brtrue.s   IL_0042

    IL_003c:  pop
    IL_003d:  ldstr      "<null>"
    IL_0042:  stelem.ref
    IL_0043:  call       string [System.Runtime]System.String::Format(string,
                                                                      object[])
    IL_0048:  call       void [System.Console]System.Console::WriteLine(string)
    IL_004d:  nop
    IL_004e:  ldsfld     int32 Statics::Failures
    IL_0053:  ldc.i4.1
    IL_0054:  add
    IL_0055:  stsfld     int32 Statics::Failures
    IL_005a:  nop
    IL_005b:  br.s       IL_0090

    IL_005d:  nop
    IL_005e:  ldstr      "Scenario {0} succeeded ({1}) Success ({2})"
    IL_0063:  ldarg.0
    IL_0064:  ldarg.1
    IL_0065:  dup
    IL_0066:  brtrue.s   IL_006e

    IL_0068:  pop
    IL_0069:  ldstr      "<null>"
    IL_006e:  ldsfld     int32 Statics::Successes
    IL_0073:  box        [System.Runtime]System.Int32
    IL_0078:  call       string [System.Runtime]System.String::Format(string,
                                                                      object,
                                                                      object,
                                                                      object)
    IL_007d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0082:  nop
    IL_0083:  ldsfld     int32 Statics::Successes
    IL_0088:  ldc.i4.1
    IL_0089:  add
    IL_008a:  stsfld     int32 Statics::Successes
    IL_008f:  nop
    IL_0090:  ret
  } // end of method Statics::CheckForFailure

  .method public hidebysig static void  CheckForFailure(string scenario,
                                                        string expectedResult) cil managed
  {
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  ldsfld     string Statics::String
    IL_0008:  call       void Statics::CheckForFailure(string,
                                                       string,
                                                       string)
    IL_000d:  nop
    IL_000e:  ret
  } // end of method Statics::CheckForFailure

  .method public hidebysig static string 
          MakeName(valuetype [System.Runtime]System.RuntimeTypeHandle t) cil managed
  {
    // Code size       17 (0x11)
    .maxstack  1
    .locals init ([0] string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0007:  call       string Statics::MakeName(class [System.Runtime]System.Type)
    IL_000c:  stloc.0
    IL_000d:  br.s       IL_000f

    IL_000f:  ldloc.0
    IL_0010:  ret
  } // end of method Statics::MakeName

  .method public hidebysig static int32  ReportResults() cil managed
  {
    // Code size       76 (0x4c)
    .maxstack  2
    .locals init ([0] bool V_0,
             [1] int32 V_1)
    IL_0000:  nop
    IL_0001:  ldstr      "{0} successes reported"
    IL_0006:  ldsfld     int32 Statics::Successes
    IL_000b:  box        [System.Runtime]System.Int32
    IL_0010:  call       string [System.Runtime]System.String::Format(string,
                                                                      object)
    IL_0015:  call       void [System.Console]System.Console::WriteLine(string)
    IL_001a:  nop
    IL_001b:  ldstr      "{0} failures reported"
    IL_0020:  ldsfld     int32 Statics::Failures
    IL_0025:  box        [System.Runtime]System.Int32
    IL_002a:  call       string [System.Runtime]System.String::Format(string,
                                                                      object)
    IL_002f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0034:  nop
    IL_0035:  ldsfld     int32 Statics::Failures
    IL_003a:  ldc.i4.0
    IL_003b:  cgt
    IL_003d:  stloc.0
    IL_003e:  ldloc.0
    IL_003f:  brfalse.s  IL_0045

    IL_0041:  ldc.i4.1
    IL_0042:  stloc.1
    IL_0043:  br.s       IL_004a

    IL_0045:  ldc.i4.s   100
    IL_0047:  stloc.1
    IL_0048:  br.s       IL_004a

    IL_004a:  ldloc.1
    IL_004b:  ret
  } // end of method Statics::ReportResults

  .method public hidebysig static string 
          MakeName(class [System.Runtime]System.Type t) cil managed
  {
    // Code size       283 (0x11b)
    .maxstack  2
    .locals init ([0] class [System.Runtime]System.Text.StringBuilder sb,
             [1] bool V_1,
             [2] string V_2,
             [3] bool V_3,
             [4] bool V_4,
             [5] bool V_5,
             [6] bool V_6,
             [7] bool first,
             [8] class [System.Runtime]System.Type[] V_8,
             [9] int32 V_9,
             [10] class [System.Runtime]System.Type inst,
             [11] bool V_11)
    IL_0000:  nop
    IL_0001:  newobj     instance void [System.Runtime]System.Text.StringBuilder::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldarg.0
    IL_0008:  ldtoken    [System.Runtime]System.Int32
    IL_000d:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0012:  call       bool [System.Runtime]System.Type::op_Equality(class [System.Runtime]System.Type,
                                                                       class [System.Runtime]System.Type)
    IL_0017:  stloc.1
    IL_0018:  ldloc.1
    IL_0019:  brfalse.s  IL_0026

    IL_001b:  ldstr      "int32"
    IL_0020:  stloc.2
    IL_0021:  br         IL_0119

    IL_0026:  ldarg.0
    IL_0027:  ldtoken    [System.Runtime]System.String
    IL_002c:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0031:  call       bool [System.Runtime]System.Type::op_Equality(class [System.Runtime]System.Type,
                                                                       class [System.Runtime]System.Type)
    IL_0036:  stloc.3
    IL_0037:  ldloc.3
    IL_0038:  brfalse.s  IL_0045

    IL_003a:  ldstr      "string"
    IL_003f:  stloc.2
    IL_0040:  br         IL_0119

    IL_0045:  ldarg.0
    IL_0046:  ldtoken    [System.Runtime]System.Object
    IL_004b:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0050:  call       bool [System.Runtime]System.Type::op_Equality(class [System.Runtime]System.Type,
                                                                       class [System.Runtime]System.Type)
    IL_0055:  stloc.s    V_4
    IL_0057:  ldloc.s    V_4
    IL_0059:  brfalse.s  IL_0066

    IL_005b:  ldstr      "object"
    IL_0060:  stloc.2
    IL_0061:  br         IL_0119

    IL_0066:  ldarg.0
    IL_0067:  callvirt   instance bool [System.Runtime]System.Type::get_IsValueType()
    IL_006c:  stloc.s    V_5
    IL_006e:  ldloc.s    V_5
    IL_0070:  brfalse.s  IL_0080

    IL_0072:  ldloc.0
    IL_0073:  ldstr      "valuetype "
    IL_0078:  callvirt   instance class [System.Runtime]System.Text.StringBuilder [System.Runtime]System.Text.StringBuilder::Append(string)
    IL_007d:  pop
    IL_007e:  br.s       IL_008c

    IL_0080:  ldloc.0
    IL_0081:  ldstr      "class "
    IL_0086:  callvirt   instance class [System.Runtime]System.Text.StringBuilder [System.Runtime]System.Text.StringBuilder::Append(string)
    IL_008b:  pop
    IL_008c:  ldloc.0
    IL_008d:  ldarg.0
    IL_008e:  callvirt   instance string [System.Runtime]System.Reflection.MemberInfo::get_Name()
    IL_0093:  callvirt   instance class [System.Runtime]System.Text.StringBuilder [System.Runtime]System.Text.StringBuilder::Append(string)
    IL_0098:  pop
    IL_0099:  ldarg.0
    IL_009a:  callvirt   instance class [System.Runtime]System.Type[] [System.Runtime]System.Type::GetGenericArguments()
    IL_009f:  ldlen
    IL_00a0:  ldc.i4.0
    IL_00a1:  cgt.un
    IL_00a3:  stloc.s    V_6
    IL_00a5:  ldloc.s    V_6
    IL_00a7:  brfalse.s  IL_0110

    IL_00a9:  nop
    IL_00aa:  ldloc.0
    IL_00ab:  ldc.i4.s   60
    IL_00ad:  callvirt   instance class [System.Runtime]System.Text.StringBuilder [System.Runtime]System.Text.StringBuilder::Append(char)
    IL_00b2:  pop
    IL_00b3:  ldc.i4.1
    IL_00b4:  stloc.s    first
    IL_00b6:  nop
    IL_00b7:  ldarg.0
    IL_00b8:  callvirt   instance class [System.Runtime]System.Type[] [System.Runtime]System.Type::GetGenericArguments()
    IL_00bd:  stloc.s    V_8
    IL_00bf:  ldc.i4.0
    IL_00c0:  stloc.s    V_9
    IL_00c2:  br.s       IL_00fe

    IL_00c4:  ldloc.s    V_8
    IL_00c6:  ldloc.s    V_9
    IL_00c8:  ldelem.ref
    IL_00c9:  stloc.s    inst
    IL_00cb:  nop
    IL_00cc:  ldloc.s    first
    IL_00ce:  ldc.i4.0
    IL_00cf:  ceq
    IL_00d1:  stloc.s    V_11
    IL_00d3:  ldloc.s    V_11
    IL_00d5:  brfalse.s  IL_00e4

    IL_00d7:  nop
    IL_00d8:  ldloc.0
    IL_00d9:  ldc.i4.s   44
    IL_00db:  callvirt   instance class [System.Runtime]System.Text.StringBuilder [System.Runtime]System.Text.StringBuilder::Append(char)
    IL_00e0:  pop
    IL_00e1:  nop
    IL_00e2:  br.s       IL_00e9

    IL_00e4:  nop
    IL_00e5:  ldc.i4.0
    IL_00e6:  stloc.s    first
    IL_00e8:  nop
    IL_00e9:  ldloc.0
    IL_00ea:  ldloc.s    inst
    IL_00ec:  call       string Statics::MakeName(class [System.Runtime]System.Type)
    IL_00f1:  callvirt   instance class [System.Runtime]System.Text.StringBuilder [System.Runtime]System.Text.StringBuilder::Append(string)
    IL_00f6:  pop
    IL_00f7:  nop
    IL_00f8:  ldloc.s    V_9
    IL_00fa:  ldc.i4.1
    IL_00fb:  add
    IL_00fc:  stloc.s    V_9
    IL_00fe:  ldloc.s    V_9
    IL_0100:  ldloc.s    V_8
    IL_0102:  ldlen
    IL_0103:  conv.i4
    IL_0104:  blt.s      IL_00c4

    IL_0106:  ldloc.0
    IL_0107:  ldc.i4.s   62
    IL_0109:  callvirt   instance class [System.Runtime]System.Text.StringBuilder [System.Runtime]System.Text.StringBuilder::Append(char)
    IL_010e:  pop
    IL_010f:  nop
    IL_0110:  ldloc.0
    IL_0111:  callvirt   instance string [System.Runtime]System.Object::ToString()
    IL_0116:  stloc.2
    IL_0117:  br.s       IL_0119

    IL_0119:  ldloc.2
    IL_011a:  ret
  } // end of method Statics::MakeName

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "string"
    IL_0005:  stsfld     string Statics::BangBang1Param
    IL_000a:  ret
  } // end of method Statics::.cctor

} // end of class Statics


