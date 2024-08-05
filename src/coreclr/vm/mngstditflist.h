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
