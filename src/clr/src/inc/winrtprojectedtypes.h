// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This header defines the list of types that are redirected in the CLR projection of WinRT.
//

//
// The DEFINE_PROJECTED_TYPE macro takes the following parameters:
//   * An ASCII string representing the namespace in winmd which contains the type being projected from
//   * An ASCII string representing the name in winmd of the type being projected from
//   * An ASCII string representing the namespace in .NET which contains the type being projected to
//   * An ASCII string representing the name in .NET of the type being projected to
//   * A symbol which is used to represent the assembly the .NET type is defined in 
//   * A symbol which is used to represent the contract assembly the .NET type is defined in
//   * A symbol which is used to represent the WinRT type
//   * A symbol which is used to represent the .NET type
//   * A symbol which indicates what kind of type this is (struct, runtimeclassclass, etc)

//
// Optionally, the DEFINE_PROJECTED_RUNTIMECLASS, DEFINE_PROJECTED_STRUCT, DEFINE_PROJECTED_ENUM, DEFINE_PROJECTED_PINTERFACE,
// DEFINE_PROJECTED_INTERFACE, and DEFINE_PROJECTED_ATTRIBUTE macros can be defined by the consumer of this header file, in
// order to get extra information about the projected types.
// 
// Note that the input to these macros is in terms of the original winmd - so HResult is a DEFINE_PROJECTED_STRUCT even though it
// projects to the class Exception.  If you are adding a projection where the WinRT and CLR views differ upon if the type is a
// value type or not, you'll need to update the signature rewriting code in md\winmd\adapter.cpp as well as the export code in
// toolbox\winmdexp\projectedtypes.cspp.
//
// If these extra macros are not defined, then the DEFINE_PROJECTED_TYPE macro is used to register the type
//
// Additionally, the DEFINE_HIDDEN_WINRT_TYPE macro can be defined by the consumer of this file to get information about WinRT
// types that are not projected but should be hidden from the developer, for instance because they are not intended to be used
// by 3rd party components.
//
//

// DEFINE_PROJECTED_RUNTIMECLASS adds the following parameters:
//   * A ASCII string representing the namespace qualified default interface name
//   * The IID of the default interface
#ifndef DEFINE_PROJECTED_RUNTIMECLASS
#define DEFINE_PROJECTED_RUNTIMECLASS(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, szDefaultInterfaceName, DefaultInterfaceIID) \
    DEFINE_PROJECTED_TYPE(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, Runtimeclass)
#define __LOCAL_DEFINE_PROJECTED_RUNTIMECLASS
#endif // !DEFINE_PROJECTED_RUNTIMECLASS

// DEFINE_PROJECTED_STRUCT adds the following parameters:
//   * An array of Unicode strings representing the types of those fields
//
//   Note that if a field is of a non-primitive type, it must be represented in WinRTGuidGenerator::MetaDataLocator::Locate
#ifndef DEFINE_PROJECTED_STRUCT
#define DEFINE_PROJECTED_STRUCT(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, fieldSizes) \
    DEFINE_PROJECTED_TYPE(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, Struct)
#define __LOCAL_DEFINE_PROJECTED_STRUCT
#endif // !DEFINE_PROJECTED_STRUCT

#ifndef DEFINE_PROJECTED_JUPITER_STRUCT
#define DEFINE_PROJECTED_JUPITER_STRUCT(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, fieldSizes) \
    DEFINE_PROJECTED_TYPE(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, Struct)
#define __LOCAL_DEFINE_PROJECTED_JUPITER_STRUCT
#endif // !DEFINE_PROJECTED_JUPITER_STRUCT

#ifndef STRUCT_FIELDS
#define STRUCT_FIELDS(...) __VA_ARGS__ 
#endif // !STRUCT_FIELDS

// DEFINE_PROJECTED_ENUM adds the following parameters:
//   * An ASCII string defining the size of the backing field of the enumeration
#ifndef DEFINE_PROJECTED_ENUM
#define DEFINE_PROJECTED_ENUM(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, szBackingFieldSize) \
    DEFINE_PROJECTED_TYPE(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, Enum)
#define __LOCAL_DEFINE_PROJECTED_ENUM
#endif // !DEFINE_PROJECTED_ENUM

// DEFINE_PROJECTED_INTERFACE adds the following extra parameters:
//   * The IID of the interface
#ifndef DEFINE_PROJECTED_INTERFACE
#define DEFINE_PROJECTED_INTERFACE(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, PIID) \
    DEFINE_PROJECTED_TYPE(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, Interface)
#define __LOCAL_DEFINE_PROJECTED_INTERFACE
#endif // !DEFINE_PROJECTED_INTERFACE

// DEFINE_PROJECTED_PINTERFACE adds the following extra parameters:
//   * The number of generic type parameters on the interface
//   * The PIID of the interface
#ifndef DEFINE_PROJECTED_PINTERFACE
#define DEFINE_PROJECTED_PINTERFACE(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, GenericTypeParameterCount, PIID) \
    DEFINE_PROJECTED_TYPE(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, PInterface)
#define __LOCAL_DEFINE_PROJECTED_PINTERFACE
#endif // !DEFINE_PROJECTED_PINTERFACE

// DEFINE_PROJECTED_DELEGATE adds the following extra parameters:
//   * The IID of the delegate
#ifndef DEFINE_PROJECTED_DELEGATE
#define DEFINE_PROJECTED_DELEGATE(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, PIID) \
    DEFINE_PROJECTED_TYPE(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, Delegate)
#define __LOCAL_DEFINE_PROJECTED_DELEGATE
#endif // !DEFINE_PROJECTED_DELEGATE

// DEFINE_PROJECTED_PDELEGATE adds the following extra parameters:
//   * The number of generic type parameters on the interface
//   * The PIID of the delegate
#ifndef DEFINE_PROJECTED_PDELEGATE
#define DEFINE_PROJECTED_PDELEGATE(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, GenericTypeParameterCount, PIID) \
    DEFINE_PROJECTED_TYPE(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, PDelegate)
#define __LOCAL_DEFINE_PROJECTED_PDELEGATE
#endif // !DEFINE_PROJECTED_PDELEGATE

#ifndef PIID
#define PIID(...) { __VA_ARGS__ }
#endif // !PIID

// DEFINE_PROJECTED_ATTRIBUTE adds no additional parameters
#ifndef DEFINE_PROJECTED_ATTRIBUTE
#define DEFINE_PROJECTED_ATTRIBUTE(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex) \
    DEFINE_PROJECTED_TYPE(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAsmIdx, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, Attribute)
#define __LOCAL_DEFINE_PROJECTED_ATTRIBUTE
#endif // !DEFINE_PROJECTED_ATTRIBUTE

#ifndef DEFINE_HIDDEN_WINRT_TYPE
#define DEFINE_HIDDEN_WINRT_TYPE(szWinRTNamespace, szWinRTName)
#define __LOCAL_DEFINE_HIDDEN_WINRT_TYPE
#endif // !DEFINE_HIDDEN_WINRT_TYPE

//                             szWinRTNamespace                         szWinRTName                            szClrNamespace                                     szClrName                              nClrAssemblyIndex                   nContractAsmIdx                                             WinRTRedirectedTypeIndex                                        ClrRedirectedTypeIndex                                                   Extra parameters
//                             ----------------                         -----------                            --------------                                     ---------                              -----------------                   ---------------                                             ---------------------                                           ----------------------                                                   ----------------
DEFINE_PROJECTED_ATTRIBUTE   ("Windows.Foundation.Metadata",           "AttributeUsageAttribute",             "System",                                          "AttributeUsageAttribute",              Mscorlib,                           SystemRuntime,                                              Windows_Foundation_Metadata_AttributeUsageAttribute,            System_AttributeUsage)
DEFINE_PROJECTED_ENUM        ("Windows.Foundation.Metadata",           "AttributeTargets",                    "System",                                          "AttributeTargets",                     Mscorlib,                           SystemRuntime,                                              Windows_Foundation_Metadata_AttributeTargets,                   System_AttributeTargets,                                                 "Int32")

DEFINE_PROJECTED_STRUCT      ("Windows.UI",                            "Color",                               "Windows.UI",                                      "Color",                                SystemRuntimeWindowsRuntime,        SystemRuntimeWindowsRuntime,                                Windows_UI_Color,                                               System_Windows_Color,                                                    STRUCT_FIELDS(W("UInt8"), W("UInt8"), W("UInt8"), W("UInt8")))

DEFINE_PROJECTED_STRUCT      ("Windows.Foundation",                    "DateTime",                            "System",                                          "DateTimeOffset",                       Mscorlib,                           SystemRuntime,                                              Windows_Foundation_DateTime,                                    System_DateTimeOffset,                                                   STRUCT_FIELDS(W("Int64")))
DEFINE_PROJECTED_PDELEGATE   ("Windows.Foundation",                    "EventHandler`1",                      "System",                                          "EventHandler`1",                       Mscorlib,                           SystemRuntime,                                              Windows_Foundation_EventHandlerGeneric,                         System_EventHandlerGeneric,                                              1, PIID(0x9de1c535, 0x6ae1, 0x11e0, {0x84, 0xe1, 0x18, 0xa9, 0x05, 0xbc, 0xc5, 0x3f}))
DEFINE_PROJECTED_STRUCT      ("Windows.Foundation",                    "EventRegistrationToken",              "System.Runtime.InteropServices.WindowsRuntime",   "EventRegistrationToken",               Mscorlib,                           SystemRuntimeInteropServicesWindowsRuntime,                 Windows_Foundation_EventRegistrationToken,                      System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken,    STRUCT_FIELDS(W("Int64")))
DEFINE_PROJECTED_STRUCT      ("Windows.Foundation",                    "HResult",                             "System",                                          "Exception",                            Mscorlib,                           SystemRuntime,                                              Windows_Foundation_HResult,                                     System_Exception,                                                        STRUCT_FIELDS(W("Int32")))
DEFINE_PROJECTED_PINTERFACE  ("Windows.Foundation",                    "IReference`1",                        "System",                                          "Nullable`1",                           Mscorlib,                           SystemRuntime,                                              Windows_Foundation_IReference,                                  System_Nullable,                                                         1, PIID(0x61c17706, 0x2d65, 0x11e0, {0x9a, 0xe8, 0xd4, 0x85, 0x64, 0x01, 0x54, 0x72}))
DEFINE_PROJECTED_STRUCT      ("Windows.Foundation",                    "Point",                               "Windows.Foundation",                              "Point",                                SystemRuntimeWindowsRuntime,        SystemRuntimeWindowsRuntime,                                Windows_Foundation_Point,                                       Windows_Foundation_Point_clr,                                            STRUCT_FIELDS(W("Single"), W("Single")))
DEFINE_PROJECTED_STRUCT      ("Windows.Foundation",                    "Rect",                                "Windows.Foundation",                              "Rect",                                 SystemRuntimeWindowsRuntime,        SystemRuntimeWindowsRuntime,                                Windows_Foundation_Rect,                                        Windows_Foundation_Rect_clr,                                             STRUCT_FIELDS(W("Single"), W("Single"), W("Single"), W("Single")))
DEFINE_PROJECTED_STRUCT      ("Windows.Foundation",                    "Size",                                "Windows.Foundation",                              "Size",                                 SystemRuntimeWindowsRuntime,        SystemRuntimeWindowsRuntime,                                Windows_Foundation_Size,                                        Windows_Foundation_Size_clr,                                             STRUCT_FIELDS(W("Single"), W("Single")))
DEFINE_PROJECTED_STRUCT      ("Windows.Foundation",                    "TimeSpan",                            "System",                                          "TimeSpan",                             Mscorlib,                           SystemRuntime,                                              Windows_Foundation_TimeSpan,                                    System_TimeSpan,                                                         STRUCT_FIELDS(W("Int64")))
DEFINE_PROJECTED_RUNTIMECLASS("Windows.Foundation",                    "Uri",                                 "System",                                          "Uri",                                  SystemRuntime,                      SystemRuntime,                                              Windows_Foundation_Uri,                                         System_Uri,                                                              "Windows.Foundation.IUriRuntimeClass", __uuidof(ABI::Windows::Foundation::IUriRuntimeClass))

DEFINE_PROJECTED_INTERFACE   ("Windows.Foundation",                    "IClosable",                           "System",                                          "IDisposable",                          Mscorlib,                           SystemRuntime,                                              Windows_Foundation_IClosable,                                   System_IDisposable,                                                      PIID(0x30d5a829, 0x7fa4, 0x4026, {0x83, 0xbb, 0xd7, 0x5b, 0xae, 0x4e, 0xa9, 0x9e}))

DEFINE_PROJECTED_PINTERFACE  ("Windows.Foundation.Collections",        "IIterable`1",                         "System.Collections.Generic",                      "IEnumerable`1",                        Mscorlib,                           SystemRuntime,                                              Windows_Foundation_Collections_IIterable,                       System_Collections_Generic_IEnumerable,                                  1, PIID(0xfaa585ea, 0x6214, 0x4217, {0xaf, 0xda, 0x7f, 0x46, 0xde, 0x58, 0x69, 0xb3}))
DEFINE_PROJECTED_PINTERFACE  ("Windows.Foundation.Collections",        "IVector`1",                           "System.Collections.Generic",                      "IList`1",                              Mscorlib,                           SystemRuntime,                                              Windows_Foundation_Collections_IVector,                         System_Collections_Generic_IList,                                        1, PIID(0x913337e9, 0x11a1, 0x4345, {0xa3, 0xa2, 0x4e, 0x7f, 0x95, 0x6e, 0x22, 0x2d}))
DEFINE_PROJECTED_PINTERFACE  ("Windows.Foundation.Collections",        "IVectorView`1",                       "System.Collections.Generic",                      "IReadOnlyList`1",                      Mscorlib,                           SystemRuntime,                                              Windows_Foundation_Collections_IVectorView,                     System_Collections_Generic_IReadOnlyList,                                1, PIID(0xbbe1fa4c, 0xb0e3, 0x4583, {0xba, 0xef, 0x1f, 0x1b, 0x2e, 0x48, 0x3e, 0x56}))
DEFINE_PROJECTED_PINTERFACE  ("Windows.Foundation.Collections",        "IMap`2",                              "System.Collections.Generic",                      "IDictionary`2",                        Mscorlib,                           SystemRuntime,                                              Windows_Foundation_Collections_IMap,                            System_Collections_Generic_IDictionary,                                  2, PIID(0x3c2925fe, 0x8519, 0x45c1, {0xaa, 0x79, 0x19, 0x7b, 0x67, 0x18, 0xc1, 0xc1}))
DEFINE_PROJECTED_PINTERFACE  ("Windows.Foundation.Collections",        "IMapView`2",                          "System.Collections.Generic",                      "IReadOnlyDictionary`2",                Mscorlib,                           SystemRuntime,                                              Windows_Foundation_Collections_IMapView,                        System_Collections_Generic_IReadOnlyDictionary,                          2, PIID(0xe480ce40, 0xa338, 0x4ada, {0xad, 0xcf, 0x27, 0x22, 0x72, 0xe4, 0x8c, 0xb9}))
DEFINE_PROJECTED_PINTERFACE  ("Windows.Foundation.Collections",        "IKeyValuePair`2",                     "System.Collections.Generic",                      "KeyValuePair`2",                       Mscorlib,                           SystemRuntime,                                              Windows_Foundation_Collections_IKeyValuePair,                   System_Collections_Generic_KeyValuePair,                                 2, PIID(0x02b51929, 0xc1c4, 0x4a7e, {0x89, 0x40, 0x03, 0x12, 0xb5, 0xc1, 0x85, 0x00}))

DEFINE_PROJECTED_INTERFACE   ("Windows.UI.Xaml.Input",                 "ICommand",                            "System.Windows.Input",                            "ICommand",                             SystemObjectModel,                  SystemObjectModel,                                          Windows_UI_Xaml_Input_ICommand,                                 System_Windows_Input_ICommand,                                           PIID(0xE5AF3542, 0xCA67, 0x4081, {0x99, 0x5B, 0x70, 0x9D, 0xD1, 0x37, 0x92, 0xDF}))

DEFINE_PROJECTED_INTERFACE   ("Windows.UI.Xaml.Interop",               "IBindableIterable",                   "System.Collections",                              "IEnumerable",                          Mscorlib,                           SystemRuntime,                                              Windows_UI_Xaml_Interop_IBindableIterable,                      System_Collections_IEnumerable,                                          PIID(0x036d2c08, 0xdf29, 0x41af, {0x8a, 0xa2, 0xd7, 0x74, 0xbe, 0x62, 0xba, 0x6f}))
DEFINE_PROJECTED_INTERFACE   ("Windows.UI.Xaml.Interop",               "IBindableVector",                     "System.Collections",                              "IList",                                Mscorlib,                           SystemRuntime,                                              Windows_UI_Xaml_Interop_IBindableVector,                        System_Collections_IList,                                                PIID(0x393de7de, 0x6fd0, 0x4c0d, {0xbb, 0x71, 0x47, 0x24, 0x4a, 0x11, 0x3e, 0x93}))

DEFINE_PROJECTED_INTERFACE   ("Windows.UI.Xaml.Interop",               "INotifyCollectionChanged",            "System.Collections.Specialized",                  "INotifyCollectionChanged",             SystemObjectModel,                  SystemObjectModel,                                          Windows_UI_Xaml_Interop_INotifyCollectionChanged,               System_Collections_Specialized_INotifyCollectionChanged,                 PIID(0x28b167d5, 0x1a31, 0x465b, {0x9b, 0x25, 0xd5, 0xc3, 0xae, 0x68, 0x6c, 0x40}))
DEFINE_PROJECTED_DELEGATE    ("Windows.UI.Xaml.Interop",               "NotifyCollectionChangedEventHandler", "System.Collections.Specialized",                  "NotifyCollectionChangedEventHandler",  SystemObjectModel,                  SystemObjectModel,                                          Windows_UI_Xaml_Interop_NotifyCollectionChangedEventHandler,    System_Collections_Specialized_NotifyCollectionChangedEventHandler,      PIID(0xca10b37c, 0xf382, 0x4591, {0x85, 0x57, 0x5e, 0x24, 0x96, 0x52, 0x79, 0xb0}))
DEFINE_PROJECTED_RUNTIMECLASS("Windows.UI.Xaml.Interop",               "NotifyCollectionChangedEventArgs",    "System.Collections.Specialized",                  "NotifyCollectionChangedEventArgs",     SystemObjectModel,                  SystemObjectModel,                                          Windows_UI_Xaml_Interop_NotifyCollectionChangedEventArgs,       System_Collections_Specialized_NotifyCollectionChangedEventArgs,         "Windows.UI.Xaml.Interop.INotifyCollectionChangedEventArgs", PIID(0x4cf68d33, 0xe3f2, 0x4964, {0xb8, 0x5e, 0x94, 0x5b, 0x4f, 0x7e, 0x2f, 0x21}))
DEFINE_PROJECTED_ENUM        ("Windows.UI.Xaml.Interop",               "NotifyCollectionChangedAction",       "System.Collections.Specialized",                  "NotifyCollectionChangedAction",        SystemObjectModel,                  SystemObjectModel,                                          Windows_UI_Xaml_Interop_NotifyCollectionChangedAction,          System_Collections_Specialized_NotifyCollectionChangedAction,            "Int32")

DEFINE_PROJECTED_INTERFACE   ("Windows.UI.Xaml.Data",                  "INotifyPropertyChanged",              "System.ComponentModel",                           "INotifyPropertyChanged",               SystemObjectModel,                  SystemObjectModel,                                          Windows_UI_Xaml_Data_INotifyPropertyChanged,                    System_ComponentModel_INotifyPropertyChanged,                            PIID(0xcf75d69c, 0xf2f4, 0x486b, {0xb3, 0x02, 0xbb, 0x4c, 0x09, 0xba, 0xeb, 0xfa}))
DEFINE_PROJECTED_DELEGATE    ("Windows.UI.Xaml.Data",                  "PropertyChangedEventHandler",         "System.ComponentModel",                           "PropertyChangedEventHandler",          SystemObjectModel,                  SystemObjectModel,                                          Windows_UI_Xaml_Data_PropertyChangedEventHandler,               System_ComponentModel_PropertyChangedEventHandler,                       PIID(0x50f19c16, 0x0a22, 0x4d8e, {0xa0, 0x89, 0x1e, 0xa9, 0x95, 0x16, 0x57, 0xd2}))
DEFINE_PROJECTED_RUNTIMECLASS("Windows.UI.Xaml.Data",                  "PropertyChangedEventArgs",            "System.ComponentModel",                           "PropertyChangedEventArgs",             SystemObjectModel,                  SystemObjectModel,                                          Windows_UI_Xaml_Data_PropertyChangedEventArgs,                  System_ComponentModel_PropertyChangedEventArgs,                          "Windows.UI.Xaml.Data.IPropertyChangedEventArgs", PIID(0x4f33a9a0, 0x5cf4, 0x47a4, {0xb1, 0x6f, 0xd7, 0xfa, 0xaf, 0x17, 0x45, 0x7e}))

DEFINE_PROJECTED_JUPITER_STRUCT("Windows.UI.Xaml",                     "CornerRadius",                        "Windows.UI.Xaml",                                 "CornerRadius",                         SystemRuntimeWindowsRuntimeUIXaml,  SystemRuntimeWindowsRuntimeUIXaml,                          Windows_UI_Xaml_CornerRadius,                                   Windows_UI_Xaml_CornerRadius_clr,                                        STRUCT_FIELDS(W("Double"), W("Double"), W("Double"), W("Double")))
DEFINE_PROJECTED_JUPITER_STRUCT("Windows.UI.Xaml",                     "Duration",                            "Windows.UI.Xaml",                                 "Duration",                             SystemRuntimeWindowsRuntimeUIXaml,  SystemRuntimeWindowsRuntimeUIXaml,                          Windows_UI_Xaml_Duration,                                       Windows_UI_Xaml_Duration_clr,                                            STRUCT_FIELDS(W("Windows.Foundation.TimeSpan"), W("Windows.UI.Xaml.DurationType")))
DEFINE_PROJECTED_ENUM          ("Windows.UI.Xaml",                     "DurationType",                        "Windows.UI.Xaml",                                 "DurationType",                         SystemRuntimeWindowsRuntimeUIXaml,  SystemRuntimeWindowsRuntimeUIXaml,                          Windows_UI_Xaml_DurationType,                                   Windows_UI_Xaml_DurationType_clr,                                        "Int32")
DEFINE_PROJECTED_JUPITER_STRUCT("Windows.UI.Xaml",                     "GridLength",                          "Windows.UI.Xaml",                                 "GridLength",                           SystemRuntimeWindowsRuntimeUIXaml,  SystemRuntimeWindowsRuntimeUIXaml,                          Windows_UI_Xaml_GridLength,                                     Windows_UI_Xaml_GridLength_clr,                                          STRUCT_FIELDS(W("Double"), W("Windows.UI.Xaml.GridUnitType")))
DEFINE_PROJECTED_ENUM          ("Windows.UI.Xaml",                     "GridUnitType",                        "Windows.UI.Xaml",                                 "GridUnitType",                         SystemRuntimeWindowsRuntimeUIXaml,  SystemRuntimeWindowsRuntimeUIXaml,                          Windows_UI_Xaml_GridUnitType,                                   Windows_UI_Xaml_GridUnitType_clr,                                        "Int32")
DEFINE_PROJECTED_JUPITER_STRUCT("Windows.UI.Xaml",                     "Thickness",                           "Windows.UI.Xaml",                                 "Thickness",                            SystemRuntimeWindowsRuntimeUIXaml,  SystemRuntimeWindowsRuntimeUIXaml,                          Windows_UI_Xaml_Thickness,                                      Windows_UI_Xaml_Thickness_clr,                                           STRUCT_FIELDS(W("Double"), W("Double"), W("Double"), W("Double")))

DEFINE_PROJECTED_JUPITER_STRUCT("Windows.UI.Xaml.Interop",             "TypeName",                            "System",                                          "Type",                                 Mscorlib,                           SystemRuntime,                                              Windows_UI_Xaml_Interop_TypeName,                               System_Type,                                                             STRUCT_FIELDS(W("String"), W("Windows.UI.Xaml.Interop.TypeKind")))

DEFINE_PROJECTED_JUPITER_STRUCT("Windows.UI.Xaml.Controls.Primitives", "GeneratorPosition",                   "Windows.UI.Xaml.Controls.Primitives",             "GeneratorPosition",                    SystemRuntimeWindowsRuntimeUIXaml,  SystemRuntimeWindowsRuntimeUIXaml,                          Windows_UI_Xaml_Controls_Primitives_GeneratorPosition,          Windows_UI_Xaml_Controls_Primitives_GeneratorPosition_clr,               STRUCT_FIELDS(W("Int32"), W("Int32")))

DEFINE_PROJECTED_JUPITER_STRUCT("Windows.UI.Xaml.Media",               "Matrix",                              "Windows.UI.Xaml.Media",                           "Matrix",                               SystemRuntimeWindowsRuntimeUIXaml,  SystemRuntimeWindowsRuntimeUIXaml,                          Windows_UI_Xaml_Media_Matrix,                                   Windows_UI_Xaml_Media_Matrix_clr,                                        STRUCT_FIELDS(W("Double"), W("Double"), W("Double"), W("Double"), W("Double"), W("Double")))

DEFINE_PROJECTED_JUPITER_STRUCT("Windows.UI.Xaml.Media.Animation",     "KeyTime",                             "Windows.UI.Xaml.Media.Animation",                 "KeyTime",                              SystemRuntimeWindowsRuntimeUIXaml,  SystemRuntimeWindowsRuntimeUIXaml,                          Windows_UI_Xaml_Media_Animation_KeyTime,                        Windows_UI_Xaml_Media_Animation_KeyTime_clr,                             STRUCT_FIELDS(W("Windows.Foundation.TimeSpan")))
DEFINE_PROJECTED_JUPITER_STRUCT("Windows.UI.Xaml.Media.Animation",     "RepeatBehavior",                      "Windows.UI.Xaml.Media.Animation",                 "RepeatBehavior",                       SystemRuntimeWindowsRuntimeUIXaml,  SystemRuntimeWindowsRuntimeUIXaml,                          Windows_UI_Xaml_Media_Animation_RepeatBehavior,                 Windows_UI_Xaml_Media_Animation_RepeatBehavior_clr,                      STRUCT_FIELDS(W("Double"), W("Windows.Foundation.TimeSpan"), W("Windows.UI.Xaml.Media.Animation.RepeatBehaviorType")))
DEFINE_PROJECTED_ENUM          ("Windows.UI.Xaml.Media.Animation",     "RepeatBehaviorType",                  "Windows.UI.Xaml.Media.Animation",                 "RepeatBehaviorType",                   SystemRuntimeWindowsRuntimeUIXaml,  SystemRuntimeWindowsRuntimeUIXaml,                          Windows_UI_Xaml_Media_Animation_RepeatBehaviorType,             Windows_UI_Xaml_Media_Animation_RepeatBehaviorType_clr,                  "Int32")

DEFINE_PROJECTED_JUPITER_STRUCT("Windows.UI.Xaml.Media.Media3D",       "Matrix3D",                            "Windows.UI.Xaml.Media.Media3D",                   "Matrix3D",                             SystemRuntimeWindowsRuntimeUIXaml,  SystemRuntimeWindowsRuntimeUIXaml,                          Windows_UI_Xaml_Media_Media3D_Matrix3D,                         Windows_UI_Xaml_Media_Media3D_Matrix3D_clr,                              STRUCT_FIELDS(W("Double"), W("Double"), W("Double"), W("Double"), W("Double"), W("Double"), W("Double"), W("Double"), W("Double"), W("Double"), W("Double"), W("Double"), W("Double"), W("Double"), W("Double"), W("Double")))

DEFINE_PROJECTED_STRUCT      ("Windows.Foundation.Numerics",           "Vector2",                             "System.Numerics",                                 "Vector2",                              SystemNumericsVectors,              SystemNumericsVectors,                                      Windows_Foundation_Numerics_Vector2,                            System_Numerics_Vector2,                                                 STRUCT_FIELDS(L"Single", L"Single"))
DEFINE_PROJECTED_STRUCT      ("Windows.Foundation.Numerics",           "Vector3",                             "System.Numerics",                                 "Vector3",                              SystemNumericsVectors,              SystemNumericsVectors,                                      Windows_Foundation_Numerics_Vector3,                            System_Numerics_Vector3,                                                 STRUCT_FIELDS(L"Single", L"Single", L"Single"))
DEFINE_PROJECTED_STRUCT      ("Windows.Foundation.Numerics",           "Vector4",                             "System.Numerics",                                 "Vector4",                              SystemNumericsVectors,              SystemNumericsVectors,                                      Windows_Foundation_Numerics_Vector4,                            System_Numerics_Vector4,                                                 STRUCT_FIELDS(L"Single", L"Single", L"Single", L"Single"))
DEFINE_PROJECTED_STRUCT      ("Windows.Foundation.Numerics",           "Matrix3x2",                           "System.Numerics",                                 "Matrix3x2",                            SystemNumericsVectors,              SystemNumericsVectors,                                      Windows_Foundation_Numerics_Matrix3x2,                          System_Numerics_Matrix3x2,                                               STRUCT_FIELDS(L"Single", L"Single", L"Single", L"Single", L"Single", L"Single"))
DEFINE_PROJECTED_STRUCT      ("Windows.Foundation.Numerics",           "Matrix4x4",                           "System.Numerics",                                 "Matrix4x4",                            SystemNumericsVectors,              SystemNumericsVectors,                                      Windows_Foundation_Numerics_Matrix4x4,                          System_Numerics_Matrix4x4,                                               STRUCT_FIELDS(L"Single", L"Single", L"Single", L"Single", L"Single", L"Single", L"Single", L"Single", L"Single", L"Single", L"Single", L"Single", L"Single", L"Single", L"Single", L"Single"))
DEFINE_PROJECTED_STRUCT      ("Windows.Foundation.Numerics",           "Plane",                               "System.Numerics",                                 "Plane",                                SystemNumericsVectors,              SystemNumericsVectors,                                      Windows_Foundation_Numerics_Plane,                              System_Numerics_Plane,                                                   STRUCT_FIELDS(L"Windows.Foundation.Numerics.Vector3", L"Single"))
DEFINE_PROJECTED_STRUCT      ("Windows.Foundation.Numerics",           "Quaternion",                          "System.Numerics",                                 "Quaternion",                           SystemNumericsVectors,              SystemNumericsVectors,                                      Windows_Foundation_Numerics_Quaternion,                         System_Numerics_Quaternion,                                              STRUCT_FIELDS(L"Single", L"Single", L"Single", L"Single"))

#ifdef DEFINE_PROJECTED_ATTRIBUTETARGETS_VALUE

// Windows.Foundation.Metadata.AttributeTarget and System.AttributeTarget enum
// define different bits for everything (@todo: Be nice to change that before we ship.)
//
// This table encapsulates the correspondence. NOTE: Some rows in the CLR column store a 0
// to indicate that the CLR has no corresponding bit for the WinRT value.
//
//                                      WinRT             CLR
DEFINE_PROJECTED_ATTRIBUTETARGETS_VALUE(0x00000001, 0x00001000)    // AttributeTargets.Delegate
DEFINE_PROJECTED_ATTRIBUTETARGETS_VALUE(0x00000002, 0x00000010)    // AttributeTargets.Enum
DEFINE_PROJECTED_ATTRIBUTETARGETS_VALUE(0x00000004, 0x00000200)    // AttributeTargets.Event
DEFINE_PROJECTED_ATTRIBUTETARGETS_VALUE(0x00000008, 0x00000100)    // AttributeTargets.Field
DEFINE_PROJECTED_ATTRIBUTETARGETS_VALUE(0x00000010, 0x00000400)    // AttributeTargets.Interface
DEFINE_PROJECTED_ATTRIBUTETARGETS_VALUE(0x00000020, 0x00000000)    // AttributeTargets.InterfaceGroup (no equivalent in CLR)
DEFINE_PROJECTED_ATTRIBUTETARGETS_VALUE(0x00000040, 0x00000040)    // AttributeTargets.Method
DEFINE_PROJECTED_ATTRIBUTETARGETS_VALUE(0x00000080, 0x00000800)    // AttributeTargets.Parameter
DEFINE_PROJECTED_ATTRIBUTETARGETS_VALUE(0x00000100, 0x00000080)    // AttributeTargets.Property
DEFINE_PROJECTED_ATTRIBUTETARGETS_VALUE(0x00000200, 0x00000004)    // AttributeTargets.RuntimeClass <--> Class
DEFINE_PROJECTED_ATTRIBUTETARGETS_VALUE(0x00000400, 0x00000008)    // AttributeTargets.Struct
DEFINE_PROJECTED_ATTRIBUTETARGETS_VALUE(0x00000800, 0x00000000)    // AttributeTargets.InterfaceImpl (no equivalent in CLR)

#endif // #ifdef DEFINE_PROJECTED_ATTRIBUTETARGETS_VALUES


DEFINE_HIDDEN_WINRT_TYPE("Windows.Foundation.Metadata", "GCPressureAttribute")
DEFINE_HIDDEN_WINRT_TYPE("Windows.Foundation.Metadata", "GCPressureAmount")

DEFINE_HIDDEN_WINRT_TYPE("Windows.Foundation", "IPropertyValue")
DEFINE_HIDDEN_WINRT_TYPE("Windows.Foundation", "IReferenceArray`1")


#ifdef __LOCAL_DEFINE_PROJECTED_RUNTIMECLASS
#undef DEFINE_PROJECTED_RUNTIMECLASS
#endif // __LOCAL_DEFINE_PROJECTED_RUNTIMECLASS

#ifdef __LOCAL_DEFINE_PROJECTED_STRUCT
#undef DEFINE_PROJECTED_STRUCT
#endif // __LOCAL_DEFINE_PROJECTED_STRUCT

#ifdef __LOCAL_DEFINE_PROJECTED_JUPITER_STRUCT
#undef DEFINE_PROJECTED_JUPITER_STRUCT
#endif // __LOCAL_DEFINE_PROJECTED_JUPITER_STRUCT

#ifdef __LOCAL_DEFINE_PROJECTED_ENUM
#undef DEFINE_PROJECTED_ENUM
#endif // __LOCAL_DEFINE_PROJECTED_ENUM

#ifdef __LOCAL_DEFINE_PROJECTED_INTERFACE
#undef DEFINE_PROJECTED_INTERFACE
#endif // __LOCAL_DEFINE_PROJECTED_INTERFACE

#ifdef __LOCAL_DEFINE_PROJECTED_PINTERFACE
#undef DEFINE_PROJECTED_PINTERFACE
#endif // __LOCAL_DEFINE_PROJECTED_PINTERFACE

#ifdef __LOCAL_DEFINE_PROJECTED_DELEGATE
#undef DEFINE_PROJECTED_DELEGATE
#endif // __LOCAL_DEFINE_PROJECTED_DELEGATE

#ifdef __LOCAL_DEFINE_PROJECTED_PDELEGATE
#undef DEFINE_PROJECTED_PDELEGATE
#endif // __LOCAL_DEFINE_PROJECTED_PDELEGATE

#ifdef __LOCAL_DEFINE_PROJECTED_ATTRIBUTE
#undef DEFINE_PROJECTED_ATTRIBUTE
#endif // __LOCAL_DEFINE_PROJECTED_ATTRIBUTE

#ifdef __LOCAL_DEFINE_HIDDEN_WINRT_TYPE
#undef DEFINE_HIDDEN_WINRT_TYPE
#endif // __LOCAL_DEFINE_HIDDEN_WINRT_TYPE

#undef JUPITER_PROJECTION_NS
#undef JUPITER_PROJECTION_CONTROLS_PRIMITIVES_NS
#undef JUPITER_PROJECTION_MEDIA_NS              
#undef JUPITER_PROJECTION_MEDIA_ANIMATION_NS    
#undef JUPITER_PROJECTION_MEDIA_3D_NS           
#undef JUPITER_FOUNDATION_PROJECTION_NS         
