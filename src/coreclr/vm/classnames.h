// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#ifndef __CLASSNAMES_H__
#define __CLASSNAMES_H__

#include "namespace.h"

// These system class names are not assembly qualified.

#define g_AppDomainClassName "System.AppDomain"
#define g_ArrayClassName "System.Array"

#define g_NullableName "Nullable`1"

#define g_CollectionsEnumeratorClassName "System.Collections.IEnumerator"

#ifdef FEATURE_COMINTEROP
#define g_ColorClassName "System.Drawing.Color"
#define g_ComObjectName "__ComObject"
#endif // FEATURE_COMINTEROP

#define g_DateClassName "System.DateTime"
#define g_DateTimeOffsetClassName "System.DateTimeOffset"
#define g_DecimalClassName "System.Decimal"

#define g_Int128ClassName "System.Int128"
#define g_Int128Name "Int128"

#define g_UInt128ClassName "System.UInt128"
#define g_UInt128Name "UInt128"

#define g_Vector64ClassName "System.Runtime.Intrinsics.Vector64`1"
#define g_Vector64Name "Vector64`1"

#define g_Vector128ClassName "System.Runtime.Intrinsics.Vector128`1"
#define g_Vector128Name "Vector128`1"

#define g_Vector256ClassName "System.Runtime.Intrinsics.Vector256`1"
#define g_Vector256Name "Vector256`1"

#define g_Vector512ClassName "System.Runtime.Intrinsics.Vector512`1"
#define g_Vector512Name "Vector512`1"

#define g_ObjectClassName "System.Object"

#define g_RuntimeFieldHandleClassName    "System.RuntimeFieldHandle"
#define g_RuntimeMethodHandleClassName   "System.RuntimeMethodHandle"
#define g_RuntimeTypeHandleClassName     "System.RuntimeTypeHandle"

#define g_StringBufferClassName "System.Text.StringBuilder"
#define g_StringClassName "System.String"
#define g_StringName "String"

#define g_VariantClassName "System.Variant"
#define g_GuidClassName "System.Guid"

#define g_CompilerServicesFixedAddressValueTypeAttribute "System.Runtime.CompilerServices.FixedAddressValueTypeAttribute"
#define g_CompilerServicesUnsafeValueTypeAttribute "System.Runtime.CompilerServices.UnsafeValueTypeAttribute"
#define g_CompilerServicesIsByRefLikeAttribute "System.Runtime.CompilerServices.IsByRefLikeAttribute"
#define g_CompilerServicesIntrinsicAttribute "System.Runtime.CompilerServices.IntrinsicAttribute"
#define g_UnmanagedFunctionPointerAttribute "System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute"
#define g_DefaultDllImportSearchPathsAttribute "System.Runtime.InteropServices.DefaultDllImportSearchPathsAttribute"
#define g_UnmanagedCallersOnlyAttribute "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute"
#define g_FixedBufferAttribute "System.Runtime.CompilerServices.FixedBufferAttribute"

#define g_ReferenceAssemblyAttribute "System.Runtime.CompilerServices.ReferenceAssemblyAttribute"

#define g_CriticalFinalizerObjectName "CriticalFinalizerObject"

#define g_DisableRuntimeMarshallingAttribute "System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute"

#endif //!__CLASSNAMES_H__
