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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Internal.Runtime.CompilerHelpers;
using Internal.Runtime.CompilerServices;

using ReflectionPointer = System.Reflection.Pointer;

namespace Internal.Runtime.Augments
{
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
        // Helper API to perform the equivalent of a "newobj" for any MethodTable.
        // This is the raw version that does not special case any MethodTable, and should be used with
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
            return RuntimeImports.RhGetRuntimeHelperForType(type.ToEETypePtr(), RuntimeHelperKind.AllocateObject);
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

        public static unsafe TypeManagerHandle GetModuleFromTypeHandle(RuntimeTypeHandle typeHandle)
        {
            return typeHandle.ToMethodTable()->TypeManager;
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
            return ReflectionPointer.Box(*(void**)address, Type.GetTypeFromHandle(fieldType));
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
            return ReflectionPointer.Box((void*)Unsafe.As<byte, IntPtr>(ref address), Type.GetTypeFromHandle(fieldType));
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
            return ReflectionPointer.Box((void*)ptrValue, Type.GetTypeFromHandle(fieldTypeHandle));
        }

        public static unsafe object GetThreadStaticBase(IntPtr cookie)
        {
            return ThreadStatics.GetThreadStaticBaseForType(*(TypeManagerSlot**)cookie, (int)*((IntPtr*)(cookie) + 1));
        }

        public static int GetHighestStaticThreadStaticIndex(TypeManagerHandle typeManager)
        {
            RuntimeImports.RhGetModuleSection(typeManager, ReadyToRunSectionType.ThreadStaticRegion, out int length);
            return length / IntPtr.Size;
        }

        public static unsafe int ObjectHeaderSize => sizeof(EETypePtr);

        public static unsafe void EnsureClassConstructorRun(IntPtr staticClassConstructionContext)
        {
            StaticClassConstructionContext* context = (StaticClassConstructionContext*)staticClassConstructionContext;
            ClassConstructorRunner.EnsureClassConstructorRun(context);
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

        public static unsafe int GetArrayRankOrMinusOneForSzArray(RuntimeTypeHandle arrayHandle)
        {
            Debug.Assert(IsArrayType(arrayHandle));
            return arrayHandle.ToMethodTable()->IsSzArray ? -1 : arrayHandle.ToMethodTable()->ArrayRank;
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
        // This implements the "IsAssignableFrom()" api for runtime-created types. By policy, we let the underlying runtime decide assignability.
        //
        public static bool IsAssignableFrom(RuntimeTypeHandle dstType, RuntimeTypeHandle srcType)
        {
            EETypePtr dstEEType = dstType.ToEETypePtr();
            EETypePtr srcEEType = srcType.ToEETypePtr();

            return RuntimeImports.AreTypesAssignable(srcEEType, dstEEType);
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
            if (eeType.IsGenericTypeDefinition || eeType.IsPointer || eeType.IsByRef || eeType.IsFunctionPointer)
            {
                baseTypeHandle = default(RuntimeTypeHandle);
                return false;
            }
            baseTypeHandle = new RuntimeTypeHandle(eeType.BaseType);
            return true;
        }

        public static int GetGCDescSize(RuntimeTypeHandle typeHandle)
        {
            return RuntimeImports.RhGetGCDescSize(typeHandle.ToEETypePtr());
        }

        public static int GetInterfaceCount(RuntimeTypeHandle typeHandle)
        {
            return typeHandle.ToEETypePtr().Interfaces.Count;
        }

        public static RuntimeTypeHandle GetInterface(RuntimeTypeHandle typeHandle, int index)
        {
            return new RuntimeTypeHandle(typeHandle.ToEETypePtr().Interfaces[index]);
        }

        public static IntPtr NewInterfaceDispatchCell(RuntimeTypeHandle interfaceTypeHandle, int slotNumber)
        {
            IntPtr cell = RuntimeImports.RhNewInterfaceDispatchCell(interfaceTypeHandle.ToEETypePtr(), slotNumber);
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

        public static IntPtr ResolveDispatchOnType(RuntimeTypeHandle instanceType, RuntimeTypeHandle interfaceType, int slot)
        {
            return RuntimeImports.RhResolveDispatchOnType(instanceType.ToEETypePtr(), interfaceType.ToEETypePtr(), checked((ushort)slot));
        }

        public static unsafe IntPtr ResolveStaticDispatchOnType(RuntimeTypeHandle instanceType, RuntimeTypeHandle interfaceType, int slot, out RuntimeTypeHandle genericContext)
        {
            EETypePtr genericContextPtr = default;
            IntPtr result = RuntimeImports.RhResolveDispatchOnType(instanceType.ToEETypePtr(), interfaceType.ToEETypePtr(), checked((ushort)slot), &genericContextPtr);
            if (result != IntPtr.Zero)
                genericContext = new RuntimeTypeHandle(genericContextPtr);
            else
                genericContext = default;
            return result;
        }

        public static IntPtr ResolveDispatch(object instance, RuntimeTypeHandle interfaceType, int slot)
        {
            return RuntimeImports.RhResolveDispatch(instance, interfaceType.ToEETypePtr(), checked((ushort)slot));
        }

        public static bool IsUnmanagedPointerType(RuntimeTypeHandle typeHandle)
        {
            return typeHandle.ToEETypePtr().IsPointer;
        }

        public static bool IsFunctionPointerType(RuntimeTypeHandle typeHandle)
        {
            return typeHandle.ToEETypePtr().IsFunctionPointer;
        }

        public static unsafe RuntimeTypeHandle GetFunctionPointerReturnType(RuntimeTypeHandle typeHandle)
        {
            return new RuntimeTypeHandle(typeHandle.ToMethodTable()->FunctionPointerReturnType);
        }

        public static unsafe int GetFunctionPointerParameterCount(RuntimeTypeHandle typeHandle)
        {
            return (int)typeHandle.ToMethodTable()->NumFunctionPointerParameters;
        }

        public static unsafe RuntimeTypeHandle GetFunctionPointerParameterType(RuntimeTypeHandle typeHandle, int argumentIndex)
        {
            Debug.Assert(argumentIndex < GetFunctionPointerParameterCount(typeHandle));
            return new RuntimeTypeHandle(typeHandle.ToMethodTable()->FunctionPointerParameters[argumentIndex]);
        }

        public static unsafe RuntimeTypeHandle[] GetFunctionPointerParameterTypes(RuntimeTypeHandle typeHandle)
        {
            int paramCount = GetFunctionPointerParameterCount(typeHandle);
            if (paramCount == 0)
                return Array.Empty<RuntimeTypeHandle>();

            RuntimeTypeHandle[] result = new RuntimeTypeHandle[paramCount];
            MethodTableList parameters = typeHandle.ToMethodTable()->FunctionPointerParameters;
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new RuntimeTypeHandle(parameters[i]);
            }

            return result;
        }

        public static unsafe bool IsUnmanagedFunctionPointerType(RuntimeTypeHandle typeHandle)
        {
            return typeHandle.ToMethodTable()->IsUnmanagedFunctionPointer;
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
            if (srcEEType.IsFunctionPointer || dstEEType.IsFunctionPointer)
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

        public static object CheckArgument(object srcObject, RuntimeTypeHandle dstType, BinderBundle? binderBundle)
        {
            return InvokeUtils.CheckArgument(srcObject, dstType.ToEETypePtr(), InvokeUtils.CheckArgumentSemantics.DynamicInvoke, binderBundle);
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

        // if functionPointer points at an import or unboxing stub, find the target of the stub
        public static IntPtr GetCodeTarget(IntPtr functionPointer)
        {
            return RuntimeImports.RhGetCodeTarget(functionPointer);
        }

        public static IntPtr GetTargetOfUnboxingAndInstantiatingStub(IntPtr functionPointer)
        {
            return RuntimeImports.RhGetTargetOfUnboxingAndInstantiatingStub(functionPointer);
        }

        //==============================================================================================
        // Internals
        //==============================================================================================

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

            return callbacks.TryGetMethodNameFromStartAddress(ip, out _);
        }

        private static TypeLoaderCallbacks s_typeLoaderCallbacks;

        public static object CreateThunksHeap(IntPtr commonStubAddress)
        {
            object? newHeap = ThunksHeap.CreateThunksHeap(commonStubAddress);
            if (newHeap == null)
                throw new OutOfMemoryException();
            return newHeap;
        }

        public static IntPtr AllocateThunk(object thunksHeap)
        {
            IntPtr newThunk = ((ThunksHeap)thunksHeap).AllocateThunk();
            if (newThunk == IntPtr.Zero)
                throw new OutOfMemoryException();
            return newThunk;
        }

        public static void FreeThunk(object thunksHeap, IntPtr thunkAddress)
        {
            ((ThunksHeap)thunksHeap).FreeThunk(thunkAddress);
        }

        public static void SetThunkData(object thunksHeap, IntPtr thunkAddress, IntPtr context, IntPtr target)
        {
            ((ThunksHeap)thunksHeap).SetThunkData(thunkAddress, context, target);
        }

        public static bool TryGetThunkData(object thunksHeap, IntPtr thunkAddress, out IntPtr context, out IntPtr target)
        {
            return ((ThunksHeap)thunksHeap).TryGetThunkData(thunkAddress, out context, out target);
        }

        public static int GetThunkSize()
        {
            return RuntimeImports.RhpGetThunkSize();
        }

        public static Delegate CreateObjectArrayDelegate(Type delegateType, Func<object?[], object?> invoker)
        {
            return Delegate.CreateObjectArrayDelegate(delegateType, invoker);
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

        public static bool SupportsRelativePointers
        {
            get
            {
                return Internal.Runtime.MethodTable.SupportsRelativePointers;
            }
        }
    }
}
