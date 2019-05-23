// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: mtypes.h
//

//
// Defines the mapping between MARSHAL_TYPE constants and their Marshaler
// classes. Used to generate all the enums and tables.
//


// ------------------------------------------------------------------------------------------------------------------
//                    Marshaler ID                  Marshaler class name                 Supported in WinRT scenarios
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_GENERIC_1,       CopyMarshaler1,                      true)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_GENERIC_U1,      CopyMarshalerU1,                     true)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_GENERIC_2,       CopyMarshaler2,                      true)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_GENERIC_U2,      CopyMarshalerU2,                     true)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_GENERIC_4,       CopyMarshaler4,                      true)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_GENERIC_U4,      CopyMarshalerU4,                     true)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_GENERIC_8,       CopyMarshaler8,                      true)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_WINBOOL,         WinBoolMarshaler,                    false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_CBOOL,           CBoolMarshaler,                      true)
#ifdef FEATURE_COMINTEROP
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_VTBOOL,          VtBoolMarshaler,                     false)
#endif // FEATURE_COMINTEROP

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_ANSICHAR,        AnsiCharMarshaler,                   false)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_FLOAT,           FloatMarshaler,                      true)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_DOUBLE,          DoubleMarshaler,                     true)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_CURRENCY,        CurrencyMarshaler,                   false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_DECIMAL,         DecimalMarshaler,                    false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_DECIMAL_PTR,     DecimalPtrMarshaler,                 false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_GUID,            GuidMarshaler,                       true)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_GUID_PTR,        GuidPtrMarshaler,                    false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_DATE,            DateMarshaler,                       false)
 
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_LPWSTR,          WSTRMarshaler,                       false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_LPSTR,           CSTRMarshaler,                       false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_LPUTF8STR,       CUTF8Marshaler,                      false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_BSTR,            BSTRMarshaler,                       false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_ANSIBSTR,        AnsiBSTRMarshaler,                   false)
#ifdef FEATURE_COMINTEROP
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_HSTRING,         HSTRINGMarshaler,                    true)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_DATETIME,        DateTimeMarshaler,                   true)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_URI,             UriMarshaler,                        true)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_NCCEVENTARGS,    NCCEventArgsMarshaler,               true) // NotifyCollectionChangedEventArgs
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_PCEVENTARGS,     PCEventArgsMarshaler,                true)  // PropertyChangedEventArgs
#endif // FEATURE_COMINTEROP

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_LPWSTR_BUFFER,   WSTRBufferMarshaler,                 false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_LPSTR_BUFFER,    CSTRBufferMarshaler,                 false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_UTF8_BUFFER,     UTF8BufferMarshaler,                 false)

#if defined(FEATURE_COMINTEROP)
// CoreCLR doesn't have any support for marshalling interface pointers.
// Not even support for fake CCWs.
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_INTERFACE,       InterfaceMarshaler,                  true)
#endif // defined(FEATURE_COMINTEROP) 

#ifdef FEATURE_COMINTEROP
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_SAFEARRAY,       SafeArrayMarshaler,                  false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_HIDDENLENGTHARRAY, HiddenLengthArrayMarshaler,        true)
#endif // FEATURE_COMINTEROP
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_NATIVEARRAY,     NativeArrayMarshaler,                false)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_ASANYA,          AsAnyAMarshaler,                     false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_ASANYW,          AsAnyWMarshaler,                     false)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_DELEGATE,        DelegateMarshaler,                   false)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_BLITTABLEPTR,    BlittablePtrMarshaler,               false)

#ifdef FEATURE_COMINTEROP
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_VBBYVALSTR,      VBByValStrMarshaler,                 false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_VBBYVALSTRW,     VBByValStrWMarshaler,                false)
#endif // FEATURE_COMINTEROP

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_LAYOUTCLASSPTR,  LayoutClassPtrMarshaler,             false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_ARRAYWITHOFFSET, ArrayWithOffsetMarshaler,            false)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_BLITTABLEVALUECLASS,             BlittableValueClassMarshaler,  true)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_VALUECLASS,                      ValueClassMarshaler,           true)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_REFERENCECUSTOMMARSHALER,        ReferenceCustomMarshaler,      false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_ARGITERATOR,                     ArgIteratorMarshaler,          false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_BLITTABLEVALUECLASSWITHCOPYCTOR, BlittableValueClassWithCopyCtorMarshaler, false)

#ifdef FEATURE_COMINTEROP
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_OBJECT,                          ObjectMarshaler,               false)
#endif // FEATURE_COMINTEROP

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_HANDLEREF,                       HandleRefMarshaler,            false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_SAFEHANDLE,                      SafeHandleMarshaler,           false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_CRITICALHANDLE,                  CriticalHandleMarshaler,       false)

#ifdef FEATURE_COMINTEROP
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_OLECOLOR,                        OleColorMarshaler,             false)
#endif // FEATURE_COMINTEROP

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_RUNTIMETYPEHANDLE,               RuntimeTypeHandleMarshaler,    false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_RUNTIMEMETHODHANDLE,             RuntimeMethodHandleMarshaler,  false)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_RUNTIMEFIELDHANDLE,              RuntimeFieldHandleMarshaler,   false)

#ifdef FEATURE_COMINTEROP
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_NULLABLE,                        NullableMarshaler,             true)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_SYSTEMTYPE,                      SystemTypeMarshaler,           true)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_KEYVALUEPAIR,                    KeyValuePairMarshaler,         true)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_EXCEPTION,                       HResultExceptionMarshaler,     true)  // For WinRT, marshal exceptions as Windows.Foundation.HResult
#endif // FEATURE_COMINTEROP

#undef DEFINE_MARSHALER_TYPE
