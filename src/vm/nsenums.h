// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// NSENUMS.H -
//

//
// Defines NStruct-related enums
//

// NStruct Field Type's
//
// Columns:
//    Name            - name of enum
//    Size            - the native size (in bytes) of the field.
//                      for some fields, this value cannot be computed
//                      without more information. if so, put a zero here
//                      and make sure CollectNStructFieldMetadata()
//                      has code to compute the size.
//    WinRTSupported  - true if the field type is supported in WinRT
//                      scenarios.
//
//    PS - Append new entries only at the end of the enum to avoid phone versioning break.
//         Name (COM+ - Native)   Size

DEFINE_NFT(NFT_NONE,                        0,                      false)

DEFINE_NFT(NFT_STRINGUNI,                   sizeof(LPVOID),         false)
DEFINE_NFT(NFT_STRINGANSI,                  sizeof(LPVOID),         false)
DEFINE_NFT(NFT_FIXEDSTRINGUNI,              0,                      false)
DEFINE_NFT(NFT_FIXEDSTRINGANSI,             0,                      false)

DEFINE_NFT(NFT_FIXEDCHARARRAYANSI,          0,                      false)
DEFINE_NFT(NFT_FIXEDARRAY,                  0,                      false)

DEFINE_NFT(NFT_DELEGATE,                   sizeof(LPVOID),         false)

DEFINE_NFT(NFT_COPY1,                       1,                      true)
DEFINE_NFT(NFT_COPY2,                       2,                      true)
DEFINE_NFT(NFT_COPY4,                       4,                      true)
DEFINE_NFT(NFT_COPY8,                       8,                      true)

DEFINE_NFT(NFT_ANSICHAR,                    1,                      false)
DEFINE_NFT(NFT_WINBOOL,                     sizeof(BOOL),           false)

DEFINE_NFT(NFT_NESTEDLAYOUTCLASS,           0,                      false)
DEFINE_NFT(NFT_NESTEDVALUECLASS,            0,                      true)

DEFINE_NFT(NFT_CBOOL,                       1,                      true)

DEFINE_NFT(NFT_DATE,                        sizeof(DATE),           false)
DEFINE_NFT(NFT_DECIMAL,                     sizeof(DECIMAL),        false)
DEFINE_NFT(NFT_INTERFACE,                   sizeof(IUnknown*),      false)

DEFINE_NFT(NFT_SAFEHANDLE,                  sizeof(LPVOID),         false)
DEFINE_NFT(NFT_CRITICALHANDLE,              sizeof(LPVOID),         false)
DEFINE_NFT(NFT_BSTR,                        sizeof(BSTR),           false)

#ifdef FEATURE_COMINTEROP
DEFINE_NFT(NFT_SAFEARRAY,                   0,                      false)
DEFINE_NFT(NFT_HSTRING,                     sizeof(HSTRING),        true)
DEFINE_NFT(NFT_VARIANT,                     sizeof(VARIANT),        false)
DEFINE_NFT(NFT_VARIANTBOOL,                 sizeof(VARIANT_BOOL),   false)
DEFINE_NFT(NFT_CURRENCY,                    sizeof(CURRENCY),       false)
DEFINE_NFT(NFT_DATETIMEOFFSET,              sizeof(INT64),          true)
DEFINE_NFT(NFT_SYSTEMTYPE,                  sizeof(TypeNameNative), true)  // System.Type -> Windows.UI.Xaml.Interop.TypeName
DEFINE_NFT(NFT_WINDOWSFOUNDATIONHRESULT,    sizeof(int),            true)  // Windows.Foundation.HResult is marshaled to System.Exception.
#endif // FEATURE_COMINTEROP
DEFINE_NFT(NFT_STRINGUTF8,                  sizeof(LPVOID),         false)
DEFINE_NFT(NFT_ILLEGAL,                     1,                      true)

#ifdef FEATURE_COMINTEROP
DEFINE_NFT(NFT_WINDOWSFOUNDATIONIREFERENCE, sizeof(IUnknown*),      true)  // Windows.Foundation.IReference`1 is marshaled to System.Nullable`1.
#endif // FEATURE_COMINTEROP

// Append new entries only at the end of the enum to avoid phone versioning break.
