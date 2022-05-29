// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Internal.Runtime.Augments
//-------------------------------------------------
//  Why does this exist?:
//    Reflection.Execution cannot physically live in System.Private.CoreLib.dll
//    as it has a dependency on System.Reflection.Metadata. Its inherently
//    low-level nature means, however, it is closely tied to System.Private.CoreLib.dll.
//    This contract provides the two-communication between those two .dll's.
//
//
//  Implemented by:
//    System.Private.CoreLib.dll
//
//  Consumed by:
//    Reflection.Execution.dll

using System;
using System.Runtime;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;

using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;

namespace Internal.Runtime.Augments
{
    using BinderBundle = System.Reflection.BinderBundle;
    using Pointer = System.Reflection.Pointer;

    [ReflectionBlocked]
    public static class RuntimeAugments
    {
        /// <summary>
        /// Callbacks used for metadata-based stack trace resolution.
        /// </summary>
        private static StackTraceMetadataCallbacks s_stackTraceMetadataCallbacks;

        //==============================================================================================
        // One-time initialization.
        //==============================================================================================
        [CLSCompliant(false)]
        public static void Initialize(ReflectionExecutionDomainCallbacks callbacks)
        {
            s_reflectionExecutionDomainCallbacks = callbacks;
        }

        [CLSCompliant(false)]
        public static void InitializeLookups(TypeLoaderCallbacks callbacks)
        {
            s_typeLoaderCallbacks = callbacks;
        }

        [CLSCompliant(false)]
        public static void InitializeStackTraceMetadataSupport(StackTraceMetadataCallbacks callbacks)
        {
            s_stackTraceMetadataCallbacks = callbacks;
        }

        //==============================================================================================
        // Access to the underlying execution engine's object allocation routines.
        //==============================================================================================

        //
        // Perform the equivalent of a "newobj", but without invoking any constructors. Other than the MethodTable, the result object is zero-initialized.
        //
        // Special cases:
        //
        //    Strings: The .ctor performs both the construction and initialization
        //      and compiler special cases these.
        //
        //    Nullable<T>: the boxed result is the underlying type rather than Nullable so the constructor
        //      cannot truly initialize it.
        //
        //    In these cases, this helper returns "null" and ConstructorInfo.Invoke() must deal with these specially.
        //
        public static object NewObject(RuntimeTypeHandle typeHandle)
        {
            EETypePtr eeType = typeHandle.ToEETypePtr();
            if (eeType.IsNullable
                || eeType == EETypePtr.EETypePtrOf<string>()
               )
                return null;
            return RuntimeImports.RhNewObject(eeType);
        }

        //
        // Helper API to perform the equivalent of a "newobj" for any MethodTable.
        // Unlike the NewObject API, this is the raw version that does not special case any MethodTable, and should be used with
        // caution for very specific scenarios.
        //
        public static object RawNewObject(RuntimeTypeHandle typeHandle)
        {
            return RuntimeImports.RhNewObject(typeHandle.ToEETypePtr());
        }

        //
        // Perform the equivalent of a "newarr" The resulting array is zero-initialized.
        //
        public static Array NewArray(RuntimeTypeHandle typeHandleForArrayType, int count)
        {
            // Don't make the easy mistake of passing in the element MethodTable rather than the "array of element" MethodTable.
            Debug.Assert(typeHandleForArrayType.ToEETypePtr().IsSzArray);
            return RuntimeImports.RhNewArray(typeHandleForArrayType.ToEETypePtr(), count);
        }

        //
        // Perform the equivalent of a "newarr" The resulting array is zero-initialized.
        //
        // Note that invoking NewMultiDimArray on a rank-1 array type is not the same thing as invoking NewArray().
        //
        // As a concession to the fact that we don't actually support non-zero lower bounds, "lowerBounds" accepts "null"
        // to avoid unnecessary array allocations by the caller.
        //
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The compiler ensures that if we have a TypeHandle of a Rank-1 MdArray, we also generated the SzArray.")]
        public static unsafe Array NewMultiDimArray(RuntimeTypeHandle typeHandleForArrayType, int[] lengths, int[]? lowerBounds)
        {
            Debug.Assert(lengths != null);
            Debug.Assert(lowerBounds == null || lowerBounds.Length == lengths.Length);

            if (lowerBounds != null)
            {
                foreach (int lowerBound in lowerBounds)
                {
                    if (lowerBound != 0)
                        throw new PlatformNotSupportedException(SR.PlatformNotSupported_NonZeroLowerBound);
                }
            }

            if (lengths.Length == 1)
            {
                // We just checked above that all lower bounds are zero. In that case, we should actually allocate
                // a new SzArray instead.
                Type elementType = Type.GetTypeFromHandle(new RuntimeTypeHandle(typeHandleForArrayType.ToEETypePtr().ArrayElementType))!;
                return RuntimeImports.RhNewArray(elementType.MakeArrayType().TypeHandle.ToEETypePtr(), lengths[0]);
            }

            // Create a local copy of the lengths that cannot be modified by the caller
            int* pImmutableLengths = stackalloc int[lengths.Length];
            for (int i = 0; i < lengths.Length; i++)
                pImmutableLengths[i] = lengths[i];

            return Array.NewMultiDimArray(typeHandleForArrayType.ToEETypePtr(), pImmutableLengths, lengths.Length);
        }

        public static IntPtr GetAllocateObjectHelperForType(RuntimeTypeHandle type)
        {
            return RuntimeImports.RhGetRuntimeHelperForType(CreateEETypePtr(type), RuntimeHelperKind.AllocateObject);
        }

        public static IntPtr GetAllocateArrayHelperForType(RuntimeTypeHandle type)
        {
            return RuntimeImports.RhGetRuntimeHelperForType(CreateEETypePtr(type), RuntimeHelperKind.AllocateArray);
        }

        public static IntPtr GetCastingHelperForType(RuntimeTypeHandle type, bool throwing)
        {
            return RuntimeImports.RhGetRuntimeHelperForType(CreateEETypePtr(type),
                throwing ? RuntimeHelperKind.CastClass : RuntimeHelperKind.IsInst);
        }

        public static IntPtr GetDispatchMapForType(RuntimeTypeHandle typeHandle)
        {
            return CreateEETypePtr(typeHandle).DispatchMap;
        }

        public static IntPtr GetFallbackDefaultConstructor()
        {
            return Activator.GetFallbackDefaultConstructor();
        }

        //
        // Helper to create a delegate on a runtime-supplied type.
        //
        public static Delegate CreateDelegate(RuntimeTypeHandle typeHandleForDelegate, IntPtr ldftnResult, object thisObject, bool isStatic, bool isOpen)
        {
            return Delegate.CreateDelegate(typeHandleForDelegate.ToEETypePtr(), ldftnResult, thisObject, isStatic: isStatic, isOpen: isOpen);
        }

        //
        // Helper to extract the artifact that uniquely identifies a method in the runtime mapping tables.
        //
        public static IntPtr GetDelegateLdFtnResult(Delegate d, out RuntimeTypeHandle typeOfFirstParameterIfInstanceDelegate, out bool isOpenResolver, out bool isInterpreterEntrypoint)
        {
            return d.GetFunctionPointer(out typeOfFirstParameterIfInstanceDelegate, out isOpenResolver, out isInterpreterEntrypoint);
        }

        public static void GetDelegateData(Delegate delegateObj, out object firstParameter, out object helperObject, out IntPtr extraFunctionPointerOrData, out IntPtr functionPointer)
        {
            firstParameter = delegateObj.m_firstParameter;
            helperObject = delegateObj.m_helperObject;
            extraFunctionPointerOrData = delegateObj.m_extraFunctionPointerOrData;
            functionPointer = delegateObj.m_functionPointer;
        }

        // Low level method that returns the loaded modules as array. ReadOnlySpan returning overload
        // cannot be used early during startup.
        public static int GetLoadedModules(TypeManagerHandle[] resultArray)
        {
            return Internal.Runtime.CompilerHelpers.StartupCodeHelpers.GetLoadedModules(resultArray);
        }

        public static ReadOnlySpan<TypeManagerHandle> GetLoadedModules()
        {
            return Internal.Runtime.CompilerHelpers.StartupCodeHelpers.GetLoadedModules();
        }

        public static IntPtr GetOSModuleFromPointer(IntPtr pointerVal)
        {
            return RuntimeImports.RhGetOSModuleFromPointer(pointerVal);
        }

        public static unsafe bool FindBlob(TypeManagerHandle typeManager, int blobId, IntPtr ppbBlob, IntPtr pcbBlob)
        {
            return RuntimeImports.RhFindBlob(typeManager, (uint)blobId, (byte**)ppbBlob, (uint*)pcbBlob);
        }

        public static IntPtr GetPointerFromTypeHandle(RuntimeTypeHandle typeHandle)
        {
            return typeHandle.ToEETypePtr().RawValue;
        }

        public static TypeManagerHandle GetModuleFromTypeHandle(RuntimeTypeHandle typeHandle)
        {
            return RuntimeImports.RhGetModuleFromEEType(GetPointerFromTypeHandle(typeHandle));
        }

        public static RuntimeTypeHandle CreateRuntimeTypeHandle(IntPtr ldTokenResult)
        {
            return new RuntimeTypeHandle(new EETypePtr(ldTokenResult));
        }

        public static unsafe void StoreValueTypeField(IntPtr address, object fieldValue, RuntimeTypeHandle fieldType)
        {
            RuntimeImports.RhUnbox(fieldValue, ref *(byte*)address, fieldType.ToEETypePtr());
        }

        public static unsafe ref byte GetRawData(object obj)
        {
            return ref obj.GetRawData();
        }

        public static unsafe object LoadValueTypeField(IntPtr address, RuntimeTypeHandle fieldType)
        {
            return RuntimeImports.RhBox(fieldType.ToEETypePtr(), ref *(byte*)address);
        }

        public static unsafe object LoadPointerTypeField(IntPtr address, RuntimeTypeHandle fieldType)
        {
            return Pointer.Box(*(void**)address, Type.GetTypeFromHandle(fieldType));
        }

        public static unsafe void StoreValueTypeField(ref byte address, object fieldValue, RuntimeTypeHandle fieldType)
        {
            RuntimeImports.RhUnbox(fieldValue, ref address, fieldType.ToEETypePtr());
        }

        public static unsafe void StoreValueTypeField(object obj, int fieldOffset, object fieldValue, RuntimeTypeHandle fieldType)
        {
            ref byte address = ref Unsafe.AddByteOffset(ref obj.GetRawData(), new IntPtr(fieldOffset - ObjectHeaderSize));
            RuntimeImports.RhUnbox(fieldValue, ref address, fieldType.ToEETypePtr());
        }

        public static unsafe object LoadValueTypeField(object obj, int fieldOffset, RuntimeTypeHandle fieldType)
        {
            ref byte address = ref Unsafe.AddByteOffset(ref obj.GetRawData(), new IntPtr(fieldOffset - ObjectHeaderSize));
            return RuntimeImports.RhBox(fieldType.ToEETypePtr(), ref address);
        }

        public static unsafe object LoadPointerTypeField(object obj, int fieldOffset, RuntimeTypeHandle fieldType)
        {
            ref byte address = ref Unsafe.AddByteOffset(ref obj.GetRawData(), new IntPtr(fieldOffset - ObjectHeaderSize));
            return Pointer.Box((void*)Unsafe.As<byte, IntPtr>(ref address), Type.GetTypeFromHandle(fieldType));
        }

        public static unsafe void StoreReferenceTypeField(IntPtr address, object fieldValue)
        {
            Volatile.Write<object>(ref Unsafe.As<IntPtr, object>(ref *(IntPtr*)address), fieldValue);
        }

        public static unsafe object LoadReferenceTypeField(IntPtr address)
        {
            return Volatile.Read<object>(ref Unsafe.As<IntPtr, object>(ref *(IntPtr*)address));
        }

        public static void StoreReferenceTypeField(object obj, int fieldOffset, object fieldValue)
        {
            ref byte address = ref Unsafe.AddByteOffset(ref obj.GetRawData(), new IntPtr(fieldOffset - ObjectHeaderSize));
            Volatile.Write<object>(ref Unsafe.As<byte, object>(ref address), fieldValue);
        }

        public static object LoadReferenceTypeField(object obj, int fieldOffset)
        {
            ref byte address = ref Unsafe.AddByteOffset(ref obj.GetRawData(), new IntPtr(fieldOffset - ObjectHeaderSize));
            return Unsafe.As<byte, object>(ref address);
        }

        [CLSCompliant(false)]
        public static void StoreValueTypeFieldValueIntoValueType(TypedReference typedReference, int fieldOffset, object fieldValue, RuntimeTypeHandle fieldTypeHandle)
        {
            Debug.Assert(TypedReference.TargetTypeToken(typedReference).ToEETypePtr().IsValueType);

            RuntimeImports.RhUnbox(fieldValue, ref Unsafe.Add<byte>(ref typedReference.Value, fieldOffset), fieldTypeHandle.ToEETypePtr());
        }

        [CLSCompliant(false)]
        public static object LoadValueTypeFieldValueFromValueType(TypedReference typedReference, int fieldOffset, RuntimeTypeHandle fieldTypeHandle)
        {
            Debug.Assert(TypedReference.TargetTypeToken(typedReference).ToEETypePtr().IsValueType);
            Debug.Assert(fieldTypeHandle.ToEETypePtr().IsValueType);

            return RuntimeImports.RhBox(fieldTypeHandle.ToEETypePtr(), ref Unsafe.Add<byte>(ref typedReference.Value, fieldOffset));
        }

        [CLSCompliant(false)]
        public static void StoreReferenceTypeFieldValueIntoValueType(TypedReference typedReference, int fieldOffset, object fieldValue)
        {
            Debug.Assert(TypedReference.TargetTypeToken(typedReference).ToEETypePtr().IsValueType);

            Unsafe.As<byte, object>(ref Unsafe.Add<byte>(ref typedReference.Value, fieldOffset)) = fieldValue;
        }

        [CLSCompliant(false)]
        public static object LoadReferenceTypeFieldValueFromValueType(TypedReference typedReference, int fieldOffset)
        {
            Debug.Assert(TypedReference.TargetTypeToken(typedReference).ToEETypePtr().IsValueType);

            return Unsafe.As<byte, object>(ref Unsafe.Add<byte>(ref typedReference.Value, fieldOffset));
        }

        [CLSCompliant(false)]
        public static unsafe object LoadPointerTypeFieldValueFromValueType(TypedReference typedReference, int fieldOffset, RuntimeTypeHandle fieldTypeHandle)
        {
            Debug.Assert(TypedReference.TargetTypeToken(typedReference).ToEETypePtr().IsValueType);
            Debug.Assert(fieldTypeHandle.ToEETypePtr().IsPointer);

            IntPtr ptrValue = Unsafe.As<byte, IntPtr>(ref Unsafe.Add<byte>(ref typedReference.Value, fieldOffset));
            return Pointer.Box((void*)ptrValue, Type.GetTypeFromHandle(fieldTypeHandle));
        }

        public static unsafe object GetThreadStaticBase(IntPtr cookie)
        {
            return ThreadStatics.GetThreadStaticBaseForType(*(TypeManagerSlot**)cookie, (int)*((IntPtr*)(cookie) + 1));
        }

        public static unsafe int ObjectHeaderSize => sizeof(EETypePtr);

        [DebuggerGuidedStepThroughAttribute]
        public static object CallDynamicInvokeMethod(
            object thisPtr,
            IntPtr methodToCall,
            IntPtr dynamicInvokeHelperMethod,
            IntPtr dynamicInvokeHelperGenericDictionary,
            object defaultParametersContext,
            object[] parameters,
            BinderBundle binderBundle,
            bool wrapInTargetInvocationException,
            bool methodToCallIsThisCall)
        {
            object result = InvokeUtils.CallDynamicInvokeMethod(
                thisPtr,
                methodToCall,
                dynamicInvokeHelperMethod,
                dynamicInvokeHelperGenericDictionary,
                defaultParametersContext,
                parameters,
                binderBundle,
                wrapInTargetInvocationException,
                methodToCallIsThisCall);
            System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        public static unsafe void EnsureClassConstructorRun(IntPtr staticClassConstructionContext)
        {
            StaticClassConstructionContext* context = (StaticClassConstructionContext*)staticClassConstructionContext;
            ClassConstructorRunner.EnsureClassConstructorRun(context);
        }

        public static object GetEnumValue(Enum e)
        {
            return e.GetValue();
        }

        public static Type GetEnumUnderlyingType(RuntimeTypeHandle enumTypeHandle)
        {
            Debug.Assert(enumTypeHandle.ToEETypePtr().IsEnum);

            EETypeElementType elementType = enumTypeHandle.ToEETypePtr().ElementType;
            switch (elementType)
            {
                case EETypeElementType.Boolean:
                    return typeof(bool);
                case EETypeElementType.Char:
                    return typeof(char);
                case EETypeElementType.SByte:
                    return typeof(sbyte);
                case EETypeElementType.Byte:
                    return typeof(byte);
                case EETypeElementType.Int16:
                    return typeof(short);
                case EETypeElementType.UInt16:
                    return typeof(ushort);
                case EETypeElementType.Int32:
                    return typeof(int);
                case EETypeElementType.UInt32:
                    return typeof(uint);
                case EETypeElementType.Int64:
                    return typeof(long);
                case EETypeElementType.UInt64:
                    return typeof(ulong);
                default:
                    throw new NotSupportedException();
            }
        }

        public static RuntimeTypeHandle GetRelatedParameterTypeHandle(RuntimeTypeHandle parameterTypeHandle)
        {
            EETypePtr elementType = parameterTypeHandle.ToEETypePtr().ArrayElementType;
            return new RuntimeTypeHandle(elementType);
        }

        public static bool IsValueType(RuntimeTypeHandle type)
        {
            return type.ToEETypePtr().IsValueType;
        }

        public static bool IsInterface(RuntimeTypeHandle type)
        {
            return type.ToEETypePtr().IsInterface;
        }

        public static unsafe object Box(RuntimeTypeHandle type, IntPtr address)
        {
            return RuntimeImports.RhBox(type.ToEETypePtr(), ref *(byte*)address);
        }

        // Used to mutate the first parameter in a closed static delegate.  Note that this does no synchronization of any kind;
        // use only on delegate instances you're sure nobody else is using.
        public static void SetClosedStaticDelegateFirstParameter(Delegate del, object firstParameter)
        {
            del.SetClosedStaticFirstParameter(firstParameter);
        }

        //==============================================================================================
        // Execution engine policies.
        //==============================================================================================
        //
        // This returns a generic type with one generic parameter (representing the array element type)
        // whose base type and interface list determines what TypeInfo.BaseType and TypeInfo.ImplementedInterfaces
        // return for types that return true for IsArray.
        //
        public static RuntimeTypeHandle ProjectionTypeForArrays
        {
            get
            {
                return typeof(Array<>).TypeHandle;
            }
        }

        //
        // Returns the name of a virtual assembly we dump types private class library-Reflectable ty[es for internal class library use.
        // The assembly binder visible to apps will never reveal this assembly.
        //
        // Note that this is not versionable as it is exposed as a const (and needs to be a const so we can used as a custom attribute argument - which
        // is the other reason this string is not versionable.)
        //
        public const string HiddenScopeAssemblyName = "HiddenScope, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";

        //
        // This implements the "IsAssignableFrom()" api for runtime-created types. By policy, we let the underlying runtime decide assignability.
        //
        public static bool IsAssignableFrom(RuntimeTypeHandle dstType, RuntimeTypeHandle srcType)
        {
            EETypePtr dstEEType = dstType.ToEETypePtr();
            EETypePtr srcEEType = srcType.ToEETypePtr();

            return RuntimeImports.AreTypesAssignable(srcEEType, dstEEType);
        }

        public static bool IsInstanceOfInterface(object obj, RuntimeTypeHandle interfaceTypeHandle)
        {
            return (null != RuntimeImports.IsInstanceOfInterface(interfaceTypeHandle.ToEETypePtr(), obj));
        }

        //
        // Return a type's base type using the runtime type system. If the underlying runtime type system does not support
        // this operation, return false and TypeInfo.BaseType will fall back to metadata.
        //
        // Note that "default(RuntimeTypeHandle)" is a valid result that will map to a null result. (For example, System.Object has a "null" base type.)
        //
        public static bool TryGetBaseType(RuntimeTypeHandle typeHandle, out RuntimeTypeHandle baseTypeHandle)
        {
            EETypePtr eeType = typeHandle.ToEETypePtr();
            if (eeType.IsGenericTypeDefinition || eeType.IsPointer || eeType.IsByRef)
            {
                baseTypeHandle = default(RuntimeTypeHandle);
                return false;
            }
            baseTypeHandle = new RuntimeTypeHandle(eeType.BaseType);
            return true;
        }

        //
        // Return a type's transitive implemeted interface list using the runtime type system. If the underlying runtime type system does not support
        // this operation, return null and TypeInfo.ImplementedInterfaces will fall back to metadata. Note that returning null is not the same thing
        // as returning a 0-length enumerable.
        //
        public static IEnumerable<RuntimeTypeHandle> TryGetImplementedInterfaces(RuntimeTypeHandle typeHandle)
        {
            EETypePtr eeType = typeHandle.ToEETypePtr();
            if (eeType.IsGenericTypeDefinition || eeType.IsPointer || eeType.IsByRef)
                return null;

            LowLevelList<RuntimeTypeHandle> implementedInterfaces = new LowLevelList<RuntimeTypeHandle>();
            for (int i = 0; i < eeType.Interfaces.Count; i++)
            {
                EETypePtr ifcEEType = eeType.Interfaces[i];
                RuntimeTypeHandle ifcrth = new RuntimeTypeHandle(ifcEEType);
                if (Callbacks.IsReflectionBlocked(ifcrth))
                    continue;

                implementedInterfaces.Add(ifcrth);
            }
            return implementedInterfaces.ToArray();
        }

        private static RuntimeTypeHandle CreateRuntimeTypeHandle(EETypePtr eeType)
        {
            return new RuntimeTypeHandle(eeType);
        }

        private static EETypePtr CreateEETypePtr(RuntimeTypeHandle runtimeTypeHandle)
        {
            return runtimeTypeHandle.ToEETypePtr();
        }

        public static int GetGCDescSize(RuntimeTypeHandle typeHandle)
        {
            EETypePtr eeType = CreateEETypePtr(typeHandle);
            return RuntimeImports.RhGetGCDescSize(eeType);
        }

        public static int GetInterfaceCount(RuntimeTypeHandle typeHandle)
        {
            return typeHandle.ToEETypePtr().Interfaces.Count;
        }

        public static RuntimeTypeHandle GetInterface(RuntimeTypeHandle typeHandle, int index)
        {
            EETypePtr eeInterface = typeHandle.ToEETypePtr().Interfaces[index];
            return CreateRuntimeTypeHandle(eeInterface);
        }

        public static IntPtr NewInterfaceDispatchCell(RuntimeTypeHandle interfaceTypeHandle, int slotNumber)
        {
            EETypePtr eeInterfaceType = CreateEETypePtr(interfaceTypeHandle);
            IntPtr cell = RuntimeImports.RhNewInterfaceDispatchCell(eeInterfaceType, slotNumber);
            if (cell == IntPtr.Zero)
                throw new OutOfMemoryException();
            return cell;
        }

        public static int GetValueTypeSize(RuntimeTypeHandle typeHandle)
        {
            return (int)typeHandle.ToEETypePtr().ValueTypeSize;
        }

        [Intrinsic]
        public static RuntimeTypeHandle GetCanonType(CanonTypeKind kind)
        {
            // Compiler needs to expand this. This is not expressible in IL.
            throw new NotSupportedException();
        }

        public static RuntimeTypeHandle GetGenericDefinition(RuntimeTypeHandle typeHandle)
        {
            EETypePtr eeType = typeHandle.ToEETypePtr();
            Debug.Assert(eeType.IsGeneric);
            return new RuntimeTypeHandle(eeType.GenericDefinition);
        }

        public static RuntimeTypeHandle GetGenericArgument(RuntimeTypeHandle typeHandle, int argumentIndex)
        {
            EETypePtr eeType = typeHandle.ToEETypePtr();
            Debug.Assert(eeType.IsGeneric);
            return new RuntimeTypeHandle(eeType.Instantiation[argumentIndex]);
        }

        public static RuntimeTypeHandle GetGenericInstantiation(RuntimeTypeHandle typeHandle, out RuntimeTypeHandle[] genericTypeArgumentHandles)
        {
            EETypePtr eeType = typeHandle.ToEETypePtr();

            Debug.Assert(eeType.IsGeneric);

            var instantiation = eeType.Instantiation;
            genericTypeArgumentHandles = new RuntimeTypeHandle[instantiation.Length];
            for (int i = 0; i < instantiation.Length; i++)
            {
                genericTypeArgumentHandles[i] = new RuntimeTypeHandle(instantiation[i]);
            }

            return new RuntimeTypeHandle(eeType.GenericDefinition);
        }

        public static bool IsGenericType(RuntimeTypeHandle typeHandle)
        {
            return typeHandle.ToEETypePtr().IsGeneric;
        }

        public static bool IsArrayType(RuntimeTypeHandle typeHandle)
        {
            return typeHandle.ToEETypePtr().IsArray;
        }

        public static bool IsByRefLike(RuntimeTypeHandle typeHandle) => typeHandle.ToEETypePtr().IsByRefLike;

        public static bool IsDynamicType(RuntimeTypeHandle typeHandle)
        {
            return typeHandle.ToEETypePtr().IsDynamicType;
        }

        public static bool HasCctor(RuntimeTypeHandle typeHandle)
        {
            return typeHandle.ToEETypePtr().HasCctor;
        }

        public static RuntimeTypeHandle RuntimeTypeHandleOf<T>()
        {
            return new RuntimeTypeHandle(EETypePtr.EETypePtrOf<T>());
        }

        public static IntPtr ResolveDispatchOnType(RuntimeTypeHandle instanceType, RuntimeTypeHandle interfaceType, int slot)
        {
            return RuntimeImports.RhResolveDispatchOnType(CreateEETypePtr(instanceType), CreateEETypePtr(interfaceType), checked((ushort)slot));
        }

        public static IntPtr ResolveDispatch(object instance, RuntimeTypeHandle interfaceType, int slot)
        {
            return RuntimeImports.RhResolveDispatch(instance, CreateEETypePtr(interfaceType), checked((ushort)slot));
        }

        public static IntPtr GVMLookupForSlot(RuntimeTypeHandle type, RuntimeMethodHandle slot)
        {
            return GenericVirtualMethodSupport.GVMLookupForSlot(type, slot);
        }

        public static bool IsUnmanagedPointerType(RuntimeTypeHandle typeHandle)
        {
            return typeHandle.ToEETypePtr().IsPointer;
        }

        public static bool IsByRefType(RuntimeTypeHandle typeHandle)
        {
            return typeHandle.ToEETypePtr().IsByRef;
        }

        public static bool IsGenericTypeDefinition(RuntimeTypeHandle typeHandle)
        {
            return typeHandle.ToEETypePtr().IsGenericTypeDefinition;
        }

        //
        // This implements the equivalent of the desktop's InvokeUtil::CanPrimitiveWiden() routine.
        //
        public static bool CanPrimitiveWiden(RuntimeTypeHandle srcType, RuntimeTypeHandle dstType)
        {
            EETypePtr srcEEType = srcType.ToEETypePtr();
            EETypePtr dstEEType = dstType.ToEETypePtr();

            if (srcEEType.IsGenericTypeDefinition || dstEEType.IsGenericTypeDefinition)
                return false;
            if (srcEEType.IsPointer || dstEEType.IsPointer)
                return false;
            if (srcEEType.IsByRef || dstEEType.IsByRef)
                return false;

            if (!srcEEType.IsPrimitive)
                return false;
            if (!dstEEType.IsPrimitive)
                return false;
            if (!srcEEType.CorElementTypeInfo.CanWidenTo(dstEEType.CorElementType))
                return false;
            return true;
        }

        public static object CheckArgument(object srcObject, RuntimeTypeHandle dstType, BinderBundle binderBundle)
        {
            return InvokeUtils.CheckArgument(srcObject, dstType, binderBundle);
        }

        // FieldInfo.SetValueDirect() has a completely different set of rules on how to coerce the argument from
        // the other Reflection api.
        public static object CheckArgumentForDirectFieldAccess(object srcObject, RuntimeTypeHandle dstType)
        {
            return InvokeUtils.CheckArgument(srcObject, dstType.ToEETypePtr(), InvokeUtils.CheckArgumentSemantics.SetFieldDirect, binderBundle: null);
        }

        public static bool IsAssignable(object srcObject, RuntimeTypeHandle dstType)
        {
            EETypePtr srcEEType = srcObject.GetEETypePtr();
            return RuntimeImports.AreTypesAssignable(srcEEType, dstType.ToEETypePtr());
        }

        //==============================================================================================
        // Nullable<> support
        //==============================================================================================
        public static bool IsNullable(RuntimeTypeHandle declaringTypeHandle)
        {
            return declaringTypeHandle.ToEETypePtr().IsNullable;
        }

        public static RuntimeTypeHandle GetNullableType(RuntimeTypeHandle nullableType)
        {
            EETypePtr theT = nullableType.ToEETypePtr().NullableType;
            return new RuntimeTypeHandle(theT);
        }

        /// <summary>
        /// Locate the file path for a given native application module.
        /// </summary>
        /// <param name="ip">Address inside the module</param>
        /// <param name="moduleBase">Module base address</param>
        public static unsafe string TryGetFullPathToApplicationModule(IntPtr ip, out IntPtr moduleBase)
        {
            moduleBase = RuntimeImports.RhGetOSModuleFromPointer(ip);
            if (moduleBase == IntPtr.Zero)
                return null;
#if TARGET_UNIX
            // RhGetModuleFileName on Unix calls dladdr that accepts any ip. Avoid the redundant lookup
            // and pass the ip into RhGetModuleFileName directly. Also, older versions of Musl have a bug
            // that leads to crash with the redundant lookup.
            byte* pModuleNameUtf8;
            int numUtf8Chars = RuntimeImports.RhGetModuleFileName(ip, out pModuleNameUtf8);
            string modulePath = System.Text.Encoding.UTF8.GetString(pModuleNameUtf8, numUtf8Chars);
#else // TARGET_UNIX
            char* pModuleName;
            int numChars = RuntimeImports.RhGetModuleFileName(moduleBase, out pModuleName);
            string modulePath = new string(pModuleName, 0, numChars);
#endif // TARGET_UNIX
            return modulePath;
        }

        public static IntPtr GetRuntimeTypeHandleRawValue(RuntimeTypeHandle runtimeTypeHandle)
        {
            return runtimeTypeHandle.RawValue;
        }

        // if functionPointer points at an import or unboxing stub, find the target of the stub
        public static IntPtr GetCodeTarget(IntPtr functionPointer)
        {
            return RuntimeImports.RhGetCodeTarget(functionPointer);
        }

        public static IntPtr GetTargetOfUnboxingAndInstantiatingStub(IntPtr functionPointer)
        {
            return RuntimeImports.RhGetTargetOfUnboxingAndInstantiatingStub(functionPointer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr RuntimeCacheLookup(IntPtr context, IntPtr signature, RuntimeObjectFactory factory, object contextObject, out IntPtr auxResult)
        {
            return TypeLoaderExports.RuntimeCacheLookupInCache(context, signature, factory, contextObject, out auxResult);
        }

        //==============================================================================================
        // Internals
        //==============================================================================================
        [CLSCompliant(false)]
        public static ReflectionExecutionDomainCallbacks CallbacksIfAvailable
        {
            get
            {
                return s_reflectionExecutionDomainCallbacks;
            }
        }

        [CLSCompliant(false)]
        public static ReflectionExecutionDomainCallbacks Callbacks
        {
            get
            {
                ReflectionExecutionDomainCallbacks callbacks = s_reflectionExecutionDomainCallbacks;
                Debug.Assert(callbacks != null);
                return callbacks;
            }
        }

        internal static TypeLoaderCallbacks TypeLoaderCallbacksIfAvailable
        {
            get
            {
                return s_typeLoaderCallbacks;
            }
        }

        internal static TypeLoaderCallbacks TypeLoaderCallbacks
        {
            get
            {
                TypeLoaderCallbacks callbacks = s_typeLoaderCallbacks;
                Debug.Assert(callbacks != null);
                return callbacks;
            }
        }

        internal static StackTraceMetadataCallbacks StackTraceCallbacksIfAvailable
        {
            get
            {
                return s_stackTraceMetadataCallbacks;
            }
        }

        public static string TryGetMethodDisplayStringFromIp(IntPtr ip)
        {
            StackTraceMetadataCallbacks callbacks = StackTraceCallbacksIfAvailable;
            if (callbacks == null)
                return null;

            ip = RuntimeImports.RhFindMethodStartAddress(ip);
            if (ip == IntPtr.Zero)
                return null;

            return callbacks.TryGetMethodNameFromStartAddress(ip);
        }

        private static volatile ReflectionExecutionDomainCallbacks s_reflectionExecutionDomainCallbacks;
        private static TypeLoaderCallbacks s_typeLoaderCallbacks;

        public static void ReportUnhandledException(Exception exception)
        {
            RuntimeExceptionHelpers.ReportUnhandledException(exception);
        }

        public static unsafe RuntimeTypeHandle GetRuntimeTypeHandleFromObjectReference(object obj)
        {
            return new RuntimeTypeHandle(obj.GetEETypePtr());
        }

        // Move memory which may be on the heap which may have object references in it.
        // In general, a memcpy on the heap is unsafe, but this is able to perform the
        // correct write barrier such that the GC is not incorrectly impacted.
        public static unsafe void BulkMoveWithWriteBarrier(IntPtr dmem, IntPtr smem, int size)
        {
            RuntimeImports.RhBulkMoveWithWriteBarrier(ref *(byte*)dmem.ToPointer(), ref *(byte*)smem.ToPointer(), (uint)size);
        }

        public static IntPtr GetUniversalTransitionThunk()
        {
            return RuntimeImports.RhGetUniversalTransitionThunk();
        }

        public static object CreateThunksHeap(IntPtr commonStubAddress)
        {
            object newHeap = RuntimeImports.RhCreateThunksHeap(commonStubAddress);
            if (newHeap == null)
                throw new OutOfMemoryException();
            return newHeap;
        }

        public static IntPtr AllocateThunk(object thunksHeap)
        {
            IntPtr newThunk = RuntimeImports.RhAllocateThunk(thunksHeap);
            if (newThunk == IntPtr.Zero)
                throw new OutOfMemoryException();
            TypeLoaderCallbacks.RegisterThunk(newThunk);
            return newThunk;
        }

        public static void FreeThunk(object thunksHeap, IntPtr thunkAddress)
        {
            RuntimeImports.RhFreeThunk(thunksHeap, thunkAddress);
        }

        public static void SetThunkData(object thunksHeap, IntPtr thunkAddress, IntPtr context, IntPtr target)
        {
            RuntimeImports.RhSetThunkData(thunksHeap, thunkAddress, context, target);
        }

        public static bool TryGetThunkData(object thunksHeap, IntPtr thunkAddress, out IntPtr context, out IntPtr target)
        {
            return RuntimeImports.RhTryGetThunkData(thunksHeap, thunkAddress, out context, out target);
        }

        public static int GetThunkSize()
        {
            return RuntimeImports.RhGetThunkSize();
        }

        [DebuggerStepThrough]
        /* TEMP workaround due to bug 149078 */
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CallDescrWorker(IntPtr callDescr)
        {
            RuntimeImports.RhCallDescrWorker(callDescr);
        }

        [DebuggerStepThrough]
        /* TEMP workaround due to bug 149078 */
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CallDescrWorkerNative(IntPtr callDescr)
        {
            RuntimeImports.RhCallDescrWorkerNative(callDescr);
        }

        public static Delegate CreateObjectArrayDelegate(Type delegateType, Func<object?[], object?> invoker)
        {
            return Delegate.CreateObjectArrayDelegate(delegateType, invoker);
        }

        internal static class RawCalliHelper
        {
            [DebuggerHidden]
            [DebuggerStepThrough]
            [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
            public static unsafe void Call<T>(System.IntPtr pfn, void* arg1, ref T arg2)
                => ((delegate*<void*, ref T, void>)pfn)(arg1, ref arg2);

            [DebuggerHidden]
            [DebuggerStepThrough]
            [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
            public static unsafe void Call<T, U>(System.IntPtr pfn, void* arg1, ref T arg2, ref U arg3)
                => ((delegate*<void*, ref T, ref U, void>)pfn)(arg1, ref arg2, ref arg3);
        }

        /// <summary>
        /// This method creates a conservatively reported region and calls a function
        /// while that region is conservatively reported.
        /// </summary>
        /// <param name="cbBuffer">size of buffer to allocated (buffer size described in bytes)</param>
        /// <param name="pfnTargetToInvoke">function pointer to execute.</param>
        /// <param name="context">context to pass to inner function. Passed by-ref to allow for efficient use of a struct as a context.</param>
        [DebuggerGuidedStepThroughAttribute]
        [CLSCompliant(false)]
        public static unsafe void RunFunctionWithConservativelyReportedBuffer<T>(int cbBuffer, delegate*<void*, ref T, void> pfnTargetToInvoke, ref T context)
        {
            RuntimeImports.ConservativelyReportedRegionDesc regionDesc = default;
            RunFunctionWithConservativelyReportedBufferInternal(cbBuffer, pfnTargetToInvoke, ref context, ref regionDesc);
            System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
        }

        // Marked as no-inlining so optimizer won't decide to optimize away the fact that pRegionDesc is a pinned interior pointer.
        // This function must also not make a p/invoke transition, or the fixed statement reporting of the ConservativelyReportedRegionDesc
        // will be ignored.
        [DebuggerGuidedStepThroughAttribute]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void RunFunctionWithConservativelyReportedBufferInternal<T>(int cbBuffer, delegate*<void*, ref T, void> pfnTargetToInvoke, ref T context, ref RuntimeImports.ConservativelyReportedRegionDesc regionDesc)
        {
            fixed (RuntimeImports.ConservativelyReportedRegionDesc* pRegionDesc = &regionDesc)
            {
                int cbBufferAligned = (cbBuffer + (sizeof(IntPtr) - 1)) & ~(sizeof(IntPtr) - 1);
                // The conservative region must be IntPtr aligned, and a multiple of IntPtr in size
                void* region = stackalloc IntPtr[cbBufferAligned / sizeof(IntPtr)];
                NativeMemory.Clear(region, (nuint)cbBufferAligned);
                RuntimeImports.RhInitializeConservativeReportingRegion(pRegionDesc, region, cbBufferAligned);

                RawCalliHelper.Call<T>((IntPtr)pfnTargetToInvoke, region, ref context);
                System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();

                RuntimeImports.RhDisableConservativeReportingRegion(pRegionDesc);
            }
        }

        /// <summary>
        /// This method creates a conservatively reported region and calls a function
        /// while that region is conservatively reported.
        /// </summary>
        /// <param name="cbBuffer">size of buffer to allocated (buffer size described in bytes)</param>
        /// <param name="pfnTargetToInvoke">function pointer to execute.</param>
        /// <param name="context">context to pass to inner function. Passed by-ref to allow for efficient use of a struct as a context.</param>
        /// <param name="context2">context2 to pass to inner function. Passed by-ref to allow for efficient use of a struct as a context.</param>
        [DebuggerGuidedStepThroughAttribute]
        [CLSCompliant(false)]
        public static unsafe void RunFunctionWithConservativelyReportedBuffer<T, U>(int cbBuffer, delegate*<void*, ref T, ref U, void> pfnTargetToInvoke, ref T context, ref U context2)
        {
            RuntimeImports.ConservativelyReportedRegionDesc regionDesc = default;
            RunFunctionWithConservativelyReportedBufferInternal(cbBuffer, pfnTargetToInvoke, ref context, ref context2, ref regionDesc);
            System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
        }

        // Marked as no-inlining so optimizer won't decide to optimize away the fact that pRegionDesc is a pinned interior pointer.
        // This function must also not make a p/invoke transition, or the fixed statement reporting of the ConservativelyReportedRegionDesc
        // will be ignored.
        [DebuggerGuidedStepThroughAttribute]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void RunFunctionWithConservativelyReportedBufferInternal<T, U>(int cbBuffer, delegate*<void*, ref T, ref U, void> pfnTargetToInvoke, ref T context, ref U context2, ref RuntimeImports.ConservativelyReportedRegionDesc regionDesc)
        {
            fixed (RuntimeImports.ConservativelyReportedRegionDesc* pRegionDesc = &regionDesc)
            {
                int cbBufferAligned = (cbBuffer + (sizeof(IntPtr) - 1)) & ~(sizeof(IntPtr) - 1);
                // The conservative region must be IntPtr aligned, and a multiple of IntPtr in size
                void* region = stackalloc IntPtr[cbBufferAligned / sizeof(IntPtr)];
                NativeMemory.Clear(region, (nuint)cbBufferAligned);
                RuntimeImports.RhInitializeConservativeReportingRegion(pRegionDesc, region, cbBufferAligned);

                RawCalliHelper.Call<T, U>((IntPtr)pfnTargetToInvoke, region, ref context, ref context2);
                System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();

                RuntimeImports.RhDisableConservativeReportingRegion(pRegionDesc);
            }
        }

        public static string GetLastResortString(RuntimeTypeHandle typeHandle)
        {
            return typeHandle.LastResortToString;
        }

        public static IntPtr RhHandleAlloc(object value, GCHandleType type)
        {
            return RuntimeImports.RhHandleAlloc(value, type);
        }

        public static void RhHandleFree(IntPtr handle)
        {
            RuntimeImports.RhHandleFree(handle);
        }

        public static IntPtr RhpGetCurrentThread()
        {
            return RuntimeImports.RhpGetCurrentThread();
        }

        public static void RhpInitiateThreadAbort(IntPtr thread, bool rude)
        {
            Exception ex = new ThreadAbortException();
            RuntimeImports.RhpInitiateThreadAbort(thread, ex, rude);
        }

        public static void RhpCancelThreadAbort(IntPtr thread)
        {
            RuntimeImports.RhpCancelThreadAbort(thread);
        }

        public static void RhYield()
        {
            RuntimeImports.RhYield();
        }

        public static bool SupportsRelativePointers
        {
            get
            {
                return Internal.Runtime.MethodTable.SupportsRelativePointers;
            }
        }

        public static bool IsPrimitive(RuntimeTypeHandle typeHandle)
        {
            return typeHandle.ToEETypePtr().IsPrimitive && !typeHandle.ToEETypePtr().IsEnum;
        }

        public static byte[] ComputePublicKeyToken(byte[] publicKey)
        {
            return System.Reflection.AssemblyNameHelpers.ComputePublicKeyToken(publicKey);
        }
    }
}
