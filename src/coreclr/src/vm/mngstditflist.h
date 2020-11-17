// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Header:  MngStdItfList.h
**
**
** Purpose: This file contains the list of managed standard
**          interfaces. Each standard interface also has the
**          list of method that it contains.
**
===========================================================*/

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

//
// Helper macros
//

#define MNGSTDITF_DEFINE_METH(FriendlyName, MethName, MethSig, FcallDecl) \
    MNGSTDITF_DEFINE_METH_IMPL(FriendlyName, MethName, MethName, MethSig, FcallDecl)

#define MNGSTDITF_DEFINE_METH2(FriendlyName, MethName, MethSig, FcallDecl) \
    MNGSTDITF_DEFINE_METH_IMPL(FriendlyName, MethName##_2, MethName, MethSig, FcallDecl)

#define MNGSTDITF_DEFINE_METH3(FriendlyName, MethName, MethSig, FcallDecl) \
    MNGSTDITF_DEFINE_METH_IMPL(FriendlyName, MethName##_3, MethName, MethSig, FcallDecl)



//
// MNGSTDITF_BEGIN_INTERFACE(FriendlyName, strMngItfName, strUCOMMngItfName, strCustomMarshalerName, strCustomMarshalerCookie, strManagedViewName, NativeItfIID) \
//
// This macro defines a new managed standard interface.
//
// FriendlyName             Friendly name for the class that implements the ECall's.
// idMngItf                 BinderClassID of the managed interface.
// idUCOMMngItf             BinderClassID of the UCom version of the managed interface.
// idCustomMarshaler        BinderClassID of the custom marshaler.
// idGetInstMethod          BinderMethodID of the GetInstance method of the custom marshaler.
// strCustomMarshalerCookie String containing the cookie to be passed to the custom marshaler.
// strManagedViewName       String containing the name of the managed view of the native interface.
// NativeItfIID             IID of the native interface.
// bCanCastOnNativeItfQI    If this is true casting to a COM object that supports the native interface
//                          will cause the cast to succeed.
//

//
// MNGSTDITF_DEFINE_METH(FriendlyName, MethName, MethSig)
//
// This macro defines a method of the standard managed interface.
// MNGSTDITF_DEFINE_METH2 and MNGSTDITF_DEFINE_METH3 are used to
// define overloaded versions of the method.
//
// FriendlyName             Friendly name for the class that implements the ECall's.
// MethName                 This is the method name
// MethSig                  This is the method signature.
//


//
// IReflect
//


#define MNGSTDITF_IREFLECT_DECL__GETMETHOD      FCDECL6(Object*, GetMethod,     Object* refThisUNSAFE, Object* refNameUNSAFE, INT32 enumBindingAttr, Object* refBinderUNSAFE, Object* refTypesArrayUNSAFE, Object* refModifiersArrayUNSAFE)
#define MNGSTDITF_IREFLECT_DECL__GETMETHOD_2    FCDECL3(Object*, GetMethod_2,   Object* refThisUNSAFE, Object* refNameUNSAFE, INT32 enumBindingAttr)
#define MNGSTDITF_IREFLECT_DECL__GETMETHODS     FCDECL2(Object*, GetMethods,    Object* refThisUNSAFE, INT32 enumBindingAttr)
#define MNGSTDITF_IREFLECT_DECL__GETFIELD       FCDECL3(Object*, GetField,      Object* refThisUNSAFE, Object* refNameUNSAFE, INT32 enumBindingAttr)
#define MNGSTDITF_IREFLECT_DECL__GETFIELDS      FCDECL2(Object*, GetFields,     Object* refThisUNSAFE, INT32 enumBindingAttr)
#define MNGSTDITF_IREFLECT_DECL__GETPROPERTY    FCDECL7(Object*, GetProperty,   Object* refThisUNSAFE, Object* refNameUNSAFE, INT32 enumBindingAttr, Object* refBinderUNSAFE, Object* refReturnTypeUNSAFE, Object* refTypesArrayUNSAFE, Object* refModifiersArrayUNSAFE)
#define MNGSTDITF_IREFLECT_DECL__GETPROPERTY_2  FCDECL3(Object*, GetProperty_2, Object* refThisUNSAFE, Object* refNameUNSAFE, INT32 enumBindingAttr)
#define MNGSTDITF_IREFLECT_DECL__GETPROPERTIES  FCDECL2(Object*, GetProperties, Object* refThisUNSAFE, INT32 enumBindingAttr)
#define MNGSTDITF_IREFLECT_DECL__GETMEMBER      FCDECL3(Object*, GetMember,     Object* refThisUNSAFE, Object* refNameUNSAFE, INT32 enumBindingAttr)
#define MNGSTDITF_IREFLECT_DECL__GETMEMBERS     FCDECL2(Object*, GetMembers,    Object* refThisUNSAFE, INT32 enumBindingAttr)
#define MNGSTDITF_IREFLECT_DECL__INVOKEMEMBER   FCDECL9(Object*, InvokeMember,  Object* refThisUNSAFE, Object* refNameUNSAFE, INT32 enumBindingAttr, Object* refBinderUNSAFE, Object* refTargetUNSAFE, Object* refArgsArrayUNSAFE, Object* refModifiersArrayUNSAFE, Object* refCultureUNSAFE, Object* refNamedParamsArrayUNSAFE)
#define MNGSTDITF_IREFLECT_DECL__GET_UNDERLYING_SYSTEM_TYPE FCDECL1(Object*, get_UnderlyingSystemType, Object* refThisUNSAFE)

MNGSTDITF_BEGIN_INTERFACE(StdMngIReflect, g_ReflectionReflectItfName, "System.Runtime.InteropServices.ComTypes.IReflect", g_CMExpandoToDispatchExMarshaler, "IReflect", g_CMExpandoViewOfDispatchEx, IID_IDispatchEx, TRUE)
    MNGSTDITF_DEFINE_METH(StdMngIReflect, GetMethod,    &gsig_IM_Str_BindingFlags_Binder_ArrType_ArrParameterModifier_RetMethodInfo, MNGSTDITF_IREFLECT_DECL__GETMETHOD)
    MNGSTDITF_DEFINE_METH2(StdMngIReflect,GetMethod,    &gsig_IM_Str_BindingFlags_RetMethodInfo,    MNGSTDITF_IREFLECT_DECL__GETMETHOD_2)
    MNGSTDITF_DEFINE_METH(StdMngIReflect, GetMethods,   &gsig_IM_BindingFlags_RetArrMethodInfo,     MNGSTDITF_IREFLECT_DECL__GETMETHODS)
    MNGSTDITF_DEFINE_METH(StdMngIReflect, GetField,     &gsig_IM_Str_BindingFlags_RetFieldInfo,     MNGSTDITF_IREFLECT_DECL__GETFIELD)
    MNGSTDITF_DEFINE_METH(StdMngIReflect, GetFields,    &gsig_IM_BindingFlags_RetArrFieldInfo,      MNGSTDITF_IREFLECT_DECL__GETFIELDS)
    MNGSTDITF_DEFINE_METH(StdMngIReflect, GetProperty,  &gsig_IM_Str_BindingFlags_Binder_Type_ArrType_ArrParameterModifier_RetPropertyInfo, MNGSTDITF_IREFLECT_DECL__GETPROPERTY)
    MNGSTDITF_DEFINE_METH2(StdMngIReflect,GetProperty,  &gsig_IM_Str_BindingFlags_RetPropertyInfo,  MNGSTDITF_IREFLECT_DECL__GETPROPERTY_2)
    MNGSTDITF_DEFINE_METH(StdMngIReflect, GetProperties,&gsig_IM_BindingFlags_RetArrPropertyInfo,   MNGSTDITF_IREFLECT_DECL__GETPROPERTIES)
    MNGSTDITF_DEFINE_METH(StdMngIReflect, GetMember,    &gsig_IM_Str_BindingFlags_RetMemberInfo,    MNGSTDITF_IREFLECT_DECL__GETMEMBER)
    MNGSTDITF_DEFINE_METH(StdMngIReflect, GetMembers,   &gsig_IM_BindingFlags_RetArrMemberInfo,     MNGSTDITF_IREFLECT_DECL__GETMEMBERS)
    MNGSTDITF_DEFINE_METH(StdMngIReflect, InvokeMember, &gsig_IM_Str_BindingFlags_Binder_Obj_ArrObj_ArrParameterModifier_CultureInfo_ArrStr_RetObj, MNGSTDITF_IREFLECT_DECL__INVOKEMEMBER)
    MNGSTDITF_DEFINE_METH(StdMngIReflect, get_UnderlyingSystemType, &gsig_IM_RetType,               MNGSTDITF_IREFLECT_DECL__GET_UNDERLYING_SYSTEM_TYPE)
MNGSTDITF_END_INTERFACE(StdMngIReflect)

//
// IEnumerator
//

#define MNGSTDITF_IENUMERATOR_DECL__MOVE_NEXT      FCDECL1(FC_BOOL_RET, MoveNext, Object* refThisUNSAFE)
#define MNGSTDITF_IENUMERATOR_DECL__GET_CURRENT    FCDECL1(Object*, get_Current, Object* refThisUNSAFE)
#define MNGSTDITF_IENUMERATOR_DECL__RESET          FCDECL1(void, Reset, Object* refThisUNSAFE)

MNGSTDITF_BEGIN_INTERFACE(StdMngIEnumerator, g_CollectionsEnumeratorClassName, "System.Runtime.InteropServices.ComTypes.IEnumerator", g_EnumeratorToEnumClassName, "", "System.Runtime.InteropServices.CustomMarshalers.EnumeratorViewOfEnumVariant", IID_IEnumVARIANT, TRUE)
    MNGSTDITF_DEFINE_METH(StdMngIEnumerator, MoveNext,      &gsig_IM_RetBool,   MNGSTDITF_IENUMERATOR_DECL__MOVE_NEXT)
    MNGSTDITF_DEFINE_METH(StdMngIEnumerator, get_Current,   &gsig_IM_RetObj,    MNGSTDITF_IENUMERATOR_DECL__GET_CURRENT)
    MNGSTDITF_DEFINE_METH(StdMngIEnumerator, Reset,         &gsig_IM_RetVoid,   MNGSTDITF_IENUMERATOR_DECL__RESET)
MNGSTDITF_END_INTERFACE(StdMngIEnumerator)

//
// IEnumerable
//

#define MNGSTDITF_IENUMERABLE_DECL__GETENUMERATOR   FCDECL1(Object*, GetEnumerator, Object* refThisUNSAFE)

MNGSTDITF_BEGIN_INTERFACE(StdMngIEnumerable, g_CollectionsEnumerableItfName, "System.Runtime.InteropServices.ComTypes.IEnumerable", "System.Runtime.InteropServices.CustomMarshalers.EnumerableToDispatchMarshaler", "", "System.Runtime.InteropServices.CustomMarshalers.EnumerableViewOfDispatch", IID_IDispatch, FALSE)
    MNGSTDITF_DEFINE_METH(StdMngIEnumerable, GetEnumerator, &gsig_IM_RetIEnumerator, MNGSTDITF_IENUMERABLE_DECL__GETENUMERATOR)
MNGSTDITF_END_INTERFACE(StdMngIEnumerable)
