// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Runtime;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection.Runtime.General;

using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

using Internal.Metadata.NativeFormat;
using Internal.NativeFormat;
using Internal.TypeSystem;
using Internal.TypeSystem.NativeFormat;

namespace Internal.Runtime.TypeLoader
{
    public sealed partial class TypeLoaderEnvironment
    {
        public enum MethodAddressType
        {
            None,
            Exact,
            Canonical,
            UniversalCanonical
        }

        /// <summary>
        /// Resolve a MethodDesc to a callable method address and unboxing stub address.
        /// </summary>
        /// <param name="method">Native metadata method description object</param>
        /// <param name="methodAddress">Resolved method address</param>
        /// <param name="unboxingStubAddress">Resolved unboxing stub address</param>
        /// <param name="foundAddressType">Output - The type of method address match found. A canonical address may require extra parameters to call.</param>
        /// <returns>true when the resolution succeeded, false when not</returns>
        public static bool TryGetMethodAddressFromMethodDesc(
            MethodDesc method,
            out IntPtr methodAddress,
            out IntPtr unboxingStubAddress,
            out MethodAddressType foundAddressType)
        {
            methodAddress = IntPtr.Zero;
            unboxingStubAddress = IntPtr.Zero;
            foundAddressType = MethodAddressType.None;

#if SUPPORT_DYNAMIC_CODE
            if (foundAddressType == MethodAddressType.None)
                MethodEntrypointStubs.TryGetMethodEntrypoint(method, out methodAddress, out unboxingStubAddress, out foundAddressType);
#endif
            if (foundAddressType != MethodAddressType.None)
                return true;

            // Otherwise try to find it via an invoke map
            return TryGetMethodAddressFromTypeSystemMethodViaInvokeMap(method, out methodAddress, out unboxingStubAddress, out foundAddressType);
        }

        /// <summary>
        /// Resolve a MethodDesc to a callable method address and unboxing stub address by searching
        /// by searching in the InvokeMaps. This function is a wrapper around TryGetMethodInvokeDataFromInvokeMap
        /// that produces output in the format which matches the code table system.
        /// </summary>
        /// <param name="method">Native metadata method description object</param>
        /// <param name="methodAddress">Resolved method address</param>
        /// <param name="unboxingStubAddress">Resolved unboxing stub address</param>
        /// <param name="foundAddressType">Output - The type of method address match found. A canonical address may require extra parameters to call.</param>
        /// <returns>true when the resolution succeeded, false when not</returns>
        private static bool TryGetMethodAddressFromTypeSystemMethodViaInvokeMap(
            MethodDesc method,
            out IntPtr methodAddress,
            out IntPtr unboxingStubAddress,
            out MethodAddressType foundAddressType)
        {
            methodAddress = IntPtr.Zero;
            unboxingStubAddress = IntPtr.Zero;
            foundAddressType = MethodAddressType.None;
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            NativeFormatMethod nativeFormatMethod = method.GetTypicalMethodDefinition() as NativeFormatMethod;
            if (nativeFormatMethod == null)
                return false;

            MethodSignatureComparer methodSignatureComparer = new MethodSignatureComparer(
                nativeFormatMethod.MetadataReader, nativeFormatMethod.Handle);

            // Try to find a specific canonical match, or if that fails, a universal match
            if (TryGetMethodInvokeDataFromInvokeMap(
                nativeFormatMethod,
                method,
                ref methodSignatureComparer,
                CanonicalFormKind.Specific,
                out methodAddress,
                out foundAddressType) ||

                TryGetMethodInvokeDataFromInvokeMap(
                nativeFormatMethod,
                method,
                ref methodSignatureComparer,
                CanonicalFormKind.Universal,
                out methodAddress,
                out foundAddressType))
            {
                if (method.OwningType.IsValueType && !method.Signature.IsStatic)
                {
                    // In this case the invoke map found an unboxing stub, and we should pull the method address out as well
                    unboxingStubAddress = methodAddress;
                    methodAddress = RuntimeAugments.GetCodeTarget(unboxingStubAddress);

                    if (!method.HasInstantiation && ((foundAddressType != MethodAddressType.Exact) || method.OwningType.IsCanonicalSubtype(CanonicalFormKind.Any)))
                    {
                        IntPtr underlyingTarget; // unboxing and instantiating stub handling
                        if (!TypeLoaderEnvironment.TryGetTargetOfUnboxingAndInstantiatingStub(methodAddress, out underlyingTarget))
                        {
                            Environment.FailFast("Expected this to be an unboxing and instantiating stub.");
                        }
                        methodAddress = underlyingTarget;
                    }
                }

                return true;
            }

#endif
            return false;
        }

        /// <summary>
        /// Attempt a virtual dispatch on a given instanceType based on the method found via a metadata token
        /// </summary>
        private static bool TryDispatchMethodOnTarget_Inner(NativeFormatModuleInfo module, int metadataToken, RuntimeTypeHandle targetInstanceType, out IntPtr methodAddress)
        {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            TypeSystemContext context = TypeSystemContextFactory.Create();

            NativeFormatMetadataUnit metadataUnit = context.ResolveMetadataUnit(module);
            MethodDesc targetMethod = metadataUnit.GetMethod(metadataToken.AsHandle(), null);
            TypeDesc instanceType = context.ResolveRuntimeTypeHandle(targetInstanceType);

            MethodDesc realTargetMethod = targetMethod;

            // For non-interface methods we support the target method not being the exact target. (This allows
            // a canonical method to be passed in and work for any generic type instantiation.)
            if (!targetMethod.OwningType.IsInterface)
                realTargetMethod = instanceType.FindMethodOnTypeWithMatchingTypicalMethod(targetMethod);

            bool success = LazyVTableResolver.TryDispatchMethodOnTarget(instanceType, realTargetMethod, out methodAddress);

            TypeSystemContextFactory.Recycle(context);
            return success;
#else
            methodAddress = IntPtr.Zero;
            return false;
#endif
        }

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
#if DEBUG
        private static int s_ConvertDispatchCellInfoCounter;
#endif

        /// <summary>
        /// Attempt to convert the dispatch cell to a metadata token to a more efficient vtable dispatch or interface/slot dispatch.
        /// Failure to convert is not a correctness issue. We also support performing a dispatch based on metadata token alone.
        /// </summary>
        private static DispatchCellInfo ConvertDispatchCellInfo_Inner(NativeFormatModuleInfo module, DispatchCellInfo cellInfo)
        {
            Debug.Assert(cellInfo.CellType == DispatchCellType.MetadataToken);

            TypeSystemContext context = TypeSystemContextFactory.Create();

            MethodDesc targetMethod = context.ResolveMetadataUnit(module).GetMethod(cellInfo.MetadataToken.AsHandle(), null);
            Debug.Assert(!targetMethod.HasInstantiation); // At this time we do not support generic virtuals through the dispatch mechanism
            Debug.Assert(targetMethod.IsVirtual);
            if (targetMethod.OwningType.IsInterface)
            {
                if (!LazyVTableResolver.TryGetInterfaceSlotNumberFromMethod(targetMethod, out cellInfo.InterfaceSlot))
                {
                    // Unable to resolve interface method. Fail, by not mutating cellInfo
                    return cellInfo;
                }

                if (!targetMethod.OwningType.RetrieveRuntimeTypeHandleIfPossible())
                {
                    new TypeBuilder().BuildType(targetMethod.OwningType);
                }

                cellInfo.CellType = DispatchCellType.InterfaceAndSlot;
                cellInfo.InterfaceType = targetMethod.OwningType.RuntimeTypeHandle.ToIntPtr();
                cellInfo.MetadataToken = 0;
            }
            else
            {
                // Virtual function case, attempt to resolve to a VTable slot offset.
                // If the offset is less than 4096 update the cellInfo
#if DEBUG
                // The path of resolving a metadata token at dispatch time is relatively rare in practice.
                // Force it to occur in debug builds with much more regularity
                if ((s_ConvertDispatchCellInfoCounter % 16) == 0)
                {
                    s_ConvertDispatchCellInfoCounter++;
                    TypeSystemContextFactory.Recycle(context);
                    return cellInfo;
                }
                s_ConvertDispatchCellInfoCounter++;
#endif

                int slotIndexOfMethod = LazyVTableResolver.VirtualMethodToSlotIndex(targetMethod);
                int vtableOffset = -1;
                if (slotIndexOfMethod >= 0)
                    vtableOffset = LazyVTableResolver.SlotIndexToEETypeVTableOffset(slotIndexOfMethod);
                if ((vtableOffset < 4096) && (vtableOffset != -1))
                {
                    cellInfo.CellType = DispatchCellType.VTableOffset;
                    cellInfo.VTableOffset = checked((uint)vtableOffset);
                    cellInfo.MetadataToken = 0;
                }
                // Otherwise, do nothing, and resolve with a metadata dispatch later
            }

            TypeSystemContextFactory.Recycle(context);
            return cellInfo;
        }
#endif

        /// <summary>
        /// Resolve a dispatch on an interface MethodTable/slot index pair to a function pointer
        /// </summary>
        private unsafe bool TryResolveTypeSlotDispatch_Inner(MethodTable* pTargetType, MethodTable* pInterfaceType, ushort slot, out IntPtr methodAddress)
        {
            methodAddress = IntPtr.Zero;

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            TypeSystemContext context = TypeSystemContextFactory.Create();

            TypeDesc targetType;
            TypeDesc interfaceType;

            targetType = context.ResolveRuntimeTypeHandle(pTargetType->ToRuntimeTypeHandle());
            interfaceType = context.ResolveRuntimeTypeHandle(pInterfaceType->ToRuntimeTypeHandle());

            if (!(interfaceType.GetTypeDefinition() is MetadataType))
            {
                // If the interface open type is not a metadata type, this must be an interface not known in the metadata world.
                // Use the redhawk resolver for this directly.
                TypeDesc pregeneratedType = LazyVTableResolver.GetMostDerivedPregeneratedOrTemplateLoadedType(targetType);
                pregeneratedType.RetrieveRuntimeTypeHandleIfPossible();
                interfaceType.RetrieveRuntimeTypeHandleIfPossible();
                methodAddress = RuntimeAugments.ResolveDispatchOnType(pregeneratedType.RuntimeTypeHandle, interfaceType.RuntimeTypeHandle, slot);
            }
            else
            {
                MethodDesc interfaceMethod;

                if (!LazyVTableResolver.TryGetMethodFromInterfaceSlot(interfaceType, slot, out interfaceMethod))
                    return false;

                if (!LazyVTableResolver.TryDispatchMethodOnTarget(targetType, interfaceMethod, out methodAddress))
                    return false;
            }

            TypeSystemContextFactory.Recycle(context);

            return true;
#else
            return false;
#endif
        }
    }
}
