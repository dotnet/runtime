// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#ifndef __CLASSNAMES_H__
#define __CLASSNAMES_H__

#include "namespace.h"

// These system class names are not assembly qualified.

#define g_AppDomainClassName "System.AppDomain"
#define g_ArgIteratorName "ArgIterator"
#define g_ArrayClassName "System.Array"

#define g_NullableName "Nullable`1"
#define g_ByReferenceName "ByReference`1"

#define g_CollectionsEnumerableItfName "System.Collections.IEnumerable"
#define g_CollectionsEnumeratorClassName "System.Collections.IEnumerator"
#define g_CollectionsCollectionItfName "System.Collections.ICollection"
#define g_CollectionsGenericCollectionItfName "System.Collections.Generic.ICollection`1"
#define g_CollectionsGenericReadOnlyCollectionItfName "System.Collections.Generic.IReadOnlyCollection`1"

#ifdef FEATURE_COMINTEROP
#define g_ECMAKeyToken "B77A5C561934E089"       // The ECMA key used by some framework assemblies: mscorlib, system, etc.
#define g_FXKeyToken "b03f5f7f11d50a3a"         // The FX key used by other framework assemblies: System.Web, System.Drawing, etc.
#define g_CorelibAsmName "System.Private.CoreLib"
#define g_SystemAsmName "System"
#define g_SystemRuntimeAsmName "System.Runtime"
#define g_DrawingAsmName "System.Drawing"
#define g_ObjectModelAsmName "System.ObjectModel"
#define g_SystemRuntimeWindowsRuntimeAsmName "System.Runtime.WindowsRuntime"
#define g_ColorClassName "System.Drawing.Color"
#define g_ColorTranslatorClassName "System.Drawing.ColorTranslator"
#define g_SystemUriClassName "System.Uri"
#define g_WinRTUriClassName "Windows.Foundation.Uri"
#define g_WinRTUriClassNameW W("Windows.Foundation.Uri")
#define g_WinRTIUriRCFactoryName "Windows.Foundation.IUriRuntimeClassFactory"
#define g_INotifyCollectionChangedName "System.Collections.Specialized.INotifyCollectionChanged"
#define g_NotifyCollectionChangedEventHandlerName "System.Collections.Specialized.NotifyCollectionChangedEventHandler"
#define g_NotifyCollectionChangedEventArgsName "System.Collections.Specialized.NotifyCollectionChangedEventArgs"
#define g_NotifyCollectionChangedEventArgsMarshalerName "System.Runtime.InteropServices.WindowsRuntime.NotifyCollectionChangedEventArgsMarshaler"
#define g_INotifyPropertyChangedName "System.ComponentModel.INotifyPropertyChanged"
#define g_PropertyChangedEventHandlerName "System.ComponentModel.PropertyChangedEventHandler"
#define g_PropertyChangedEventArgsName "System.ComponentModel.PropertyChangedEventArgs"
#define g_PropertyChangedEventArgsMarshalerName "System.Runtime.InteropServices.WindowsRuntime.PropertyChangedEventArgsMarshaler"
#define g_WinRTIIteratorClassName   "Windows.Foundation.Collections.IIterator`1"
#define g_WinRTIIteratorClassNameW W("Windows.Foundation.Collections.IIterator`1")
#define g_ICommandName "System.Windows.Input.ICommand"
#define g_ComObjectName "__ComObject"
#define g_RuntimeClassName "RuntimeClass"
#define g_INotifyCollectionChanged_WinRTName "System.Runtime.InteropServices.WindowsRuntime.INotifyCollectionChanged_WinRT"
#define g_NotifyCollectionChangedToManagedAdapterName "System.Runtime.InteropServices.WindowsRuntime.NotifyCollectionChangedToManagedAdapter"
#define g_NotifyCollectionChangedToWinRTAdapterName "System.Runtime.InteropServices.WindowsRuntime.NotifyCollectionChangedToWinRTAdapter"
#define g_INotifyPropertyChanged_WinRTName "System.Runtime.InteropServices.WindowsRuntime.INotifyPropertyChanged_WinRT"
#define g_NotifyPropertyChangedToManagedAdapterName "System.Runtime.InteropServices.WindowsRuntime.NotifyPropertyChangedToManagedAdapter"
#define g_NotifyPropertyChangedToWinRTAdapterName "System.Runtime.InteropServices.WindowsRuntime.NotifyPropertyChangedToWinRTAdapter"
#define g_ICommand_WinRTName "System.Runtime.InteropServices.WindowsRuntime.ICommand_WinRT"
#define g_ICommandToManagedAdapterName "System.Runtime.InteropServices.WindowsRuntime.ICommandToManagedAdapter"
#define g_ICommandToWinRTAdapterName "System.Runtime.InteropServices.WindowsRuntime.ICommandToWinRTAdapter"
#define g_NotifyCollectionChangedEventHandler_WinRT "System.Runtime.InteropServices.WindowsRuntime.NotifyCollectionChangedEventHandler_WinRT"
#define g_PropertyChangedEventHandler_WinRT_Name "System.Runtime.InteropServices.WindowsRuntime.PropertyChangedEventHandler_WinRT"
#endif // FEATURE_COMINTEROP


#define g_DateClassName "System.DateTime"
#define g_DateTimeOffsetClassName "System.DateTimeOffset"
#define g_DecimalClassName "System.Decimal"
#define g_DecimalName "Decimal"

#define g_Vector64ClassName "System.Runtime.Intrinsics.Vector64`1"
#define g_Vector64Name "Vector64`1"

#define g_Vector128ClassName "System.Runtime.Intrinsics.Vector128`1"
#define g_Vector128Name "Vector128`1"

#define g_Vector256ClassName "System.Runtime.Intrinsics.Vector256`1"
#define g_Vector256Name "Vector256`1"

#ifdef FEATURE_COMINTEROP

#define g_WindowsFoundationActivatableAttributeClassName      "Windows.Foundation.Metadata.ActivatableAttribute"
#define g_WindowsFoundationComposableAttributeClassName       "Windows.Foundation.Metadata.ComposableAttribute"
#define g_WindowsFoundationStaticAttributeClassName           "Windows.Foundation.Metadata.StaticAttribute"
#define g_WindowsFoundationDefaultClassName "Windows.Foundation.Metadata.DefaultAttribute"
#define g_WindowsFoundationMarshalingBehaviorAttributeClassName "Windows.Foundation.Metadata.MarshalingBehaviorAttribute"
#define g_WindowsFoundationGCPressureAttributeClassName "Windows.Foundation.Metadata.GCPressureAttribute"
#endif // FEATURE_COMINTEROP

#define g_EnumeratorToEnumClassName "System.Runtime.InteropServices.CustomMarshalers.EnumeratorToEnumVariantMarshaler"
#define g_ExceptionClassName "System.Exception"
#define g_ExecutionEngineExceptionClassName "System.ExecutionEngineException"

#define g_ThreadStaticAttributeClassName "System.ThreadStaticAttribute"
#define g_TypeIdentifierAttributeClassName "System.Runtime.InteropServices.TypeIdentifierAttribute"

#define g_ObjectClassName "System.Object"
#define g_ObjectName "Object"
#define g_OutOfMemoryExceptionClassName "System.OutOfMemoryException"

#define g_ReflectionClassName "System.RuntimeType"
#define g_ReflectionConstructorName "System.Reflection.RuntimeConstructorInfo"
#define g_ReflectionEventInfoName "System.Reflection.EventInfo"
#define g_ReflectionEventName "System.Reflection.RuntimeEventInfo"
#define g_ReflectionExpandoItfName "System.Runtime.InteropServices.Expando.IExpando"
#define g_CMExpandoToDispatchExMarshaler "System.Runtime.InteropServices.CustomMarshalers.ExpandoToDispatchExMarshaler"
#define g_CMExpandoViewOfDispatchEx "System.Runtime.InteropServices.CustomMarshalers.ExpandoViewOfDispatchEx"
#define g_ReflectionFieldName "System.Reflection.RuntimeFieldInfo"
#define g_ReflectionMemberInfoName "System.Reflection.MemberInfo"
#define g_MethodBaseName "System.Reflection.MethodBase"
#define g_ReflectionFieldInfoName "System.Reflection.FieldInfo"
#define g_ReflectionPropertyInfoName "System.Reflection.PropertyInfo"
#define g_ReflectionConstructorInfoName "System.Reflection.ConstructorInfo"
#define g_ReflectionMethodInfoName "System.Reflection.MethodInfo"
#define g_ReflectionMethodName "System.Reflection.RuntimeMethodInfo"
#define g_ReflectionMethodInterfaceName "System.IRuntimeMethodInfo"
#define g_ReflectionAssemblyName "System.Reflection.RuntimeAssembly"
#define g_ReflectionModuleName "System.Reflection.RuntimeModule"
#define g_ReflectionParamInfoName "System.Reflection.ParameterInfo"
#define g_ReflectionParamName "System.Reflection.RuntimeParameterInfo"
#define g_ReflectionPropInfoName "System.Reflection.RuntimePropertyInfo"
#define g_ReflectionReflectItfName "System.Reflection.IReflect"
#define g_RuntimeArgumentHandleName      "RuntimeArgumentHandle"
#define g_RuntimeFieldHandleClassName    "System.RuntimeFieldHandle"
#define g_RuntimeMethodHandleClassName   "System.RuntimeMethodHandle"
#define g_RuntimeMethodHandleInternalName        "RuntimeMethodHandleInternal"
#define g_RuntimeTypeHandleClassName     "System.RuntimeTypeHandle"

#define g_StackOverflowExceptionClassName "System.StackOverflowException"
#define g_StringBufferClassName "System.Text.StringBuilder"
#define g_StringBufferName "StringBuilder"
#define g_StringClassName "System.String"
#define g_StringName "String"
#define g_SharedStaticsClassName "System.SharedStatics"

#define g_ThreadClassName "System.Threading.Thread"
#define g_TypeClassName   "System.Type"

#define g_VariantClassName "System.Variant"
#define g_GuidClassName "System.Guid"

#define g_CompilerServicesFixedAddressValueTypeAttribute "System.Runtime.CompilerServices.FixedAddressValueTypeAttribute"
#define g_CompilerServicesUnsafeValueTypeAttribute "System.Runtime.CompilerServices.UnsafeValueTypeAttribute"
#define g_CompilerServicesIsByRefLikeAttribute "System.Runtime.CompilerServices.IsByRefLikeAttribute"
#define g_CompilerServicesIntrinsicAttribute "System.Runtime.CompilerServices.IntrinsicAttribute"
#define g_UnmanagedFunctionPointerAttribute "System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute"
#define g_DefaultDllImportSearchPathsAttribute "System.Runtime.InteropServices.DefaultDllImportSearchPathsAttribute"
#define g_NativeCallableAttribute "System.Runtime.InteropServices.NativeCallableAttribute"
#define g_FixedBufferAttribute "System.Runtime.CompilerServices.FixedBufferAttribute"

#define g_CompilerServicesTypeDependencyAttribute "System.Runtime.CompilerServices.TypeDependencyAttribute"

#define g_ReferenceAssemblyAttribute "System.Runtime.CompilerServices.ReferenceAssemblyAttribute"

#define g_CriticalFinalizerObjectName "CriticalFinalizerObject"

#endif //!__CLASSNAMES_H__
