// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Runtime;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

using Internal.NativeFormat;
using Internal.TypeSystem;

namespace Internal.Runtime.TypeLoader
{
    public sealed partial class TypeLoaderEnvironment
    {
#if GVM_RESOLUTION_TRACE
        private string GetTypeNameDebug(RuntimeTypeHandle rtth)
        {
            string result;

            if (RuntimeAugments.IsGenericType(rtth))
            {
                RuntimeTypeHandle[] typeArgumentsHandles;
                RuntimeTypeHandle openTypeDef = RuntimeAugments.GetGenericInstantiation(rtth, out typeArgumentsHandles); ;
                result = GetTypeNameDebug(openTypeDef) + "<";
                for (int i = 0; i < typeArgumentsHandles.Length; i++)
                    result += (i == 0 ? "" : ",") + GetTypeNameDebug(typeArgumentsHandles[i]);
                return result + ">";
            }
            else
            {
                System.Reflection.Runtime.General.QTypeDefinition qTypeDefinition;

                // Check if we have metadata.
                if (Instance.TryGetMetadataForNamedType(rtth, out qTypeDefinition))
                    return qTypeDefinition.NativeFormatHandle.GetFullName(qTypeDefinition.NativeFormatReader);
            }

            return rtth.LowLevelToStringRawEETypeAddress();
        }
#endif

        public bool TryGetGenericVirtualTargetForTypeAndSlot(RuntimeTypeHandle targetHandle, ref RuntimeTypeHandle declaringType, RuntimeTypeHandle[] genericArguments, ref string methodName, ref RuntimeSignature methodSignature, bool lookForDefaultImplementation, out IntPtr methodPointer, out IntPtr dictionaryPointer, out bool slotUpdated)
        {
            MethodNameAndSignature methodNameAndSignature = new MethodNameAndSignature(methodName, methodSignature);

#if GVM_RESOLUTION_TRACE
            Debug.WriteLine("GVM resolution starting for " + GetTypeNameDebug(declaringType) + "." + methodNameAndSignature.Name + "(...)  on a target of type " + GetTypeNameDebug(targetHandle) + " ...");
#endif

            if (RuntimeAugments.IsInterface(declaringType))
            {
                if (!ResolveInterfaceGenericVirtualMethodSlot(targetHandle, lookForDefaultImplementation, ref declaringType, ref methodNameAndSignature))
                {
                    methodPointer = dictionaryPointer = IntPtr.Zero;
                    slotUpdated = false;
                    return false;
                }

                if (RuntimeAugments.IsInterface(declaringType))
                {
                    slotUpdated = false;
                    if (!TryGetGenericVirtualMethodPointer(declaringType, methodNameAndSignature, genericArguments, out methodPointer, out dictionaryPointer))
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("Generic virtual method pointer lookup failure.");
                        sb.AppendLine();
                        sb.AppendLine("Declaring type handle: " + declaringType.LowLevelToStringRawEETypeAddress());
                        sb.AppendLine("Target type handle: " + targetHandle.LowLevelToStringRawEETypeAddress());
                        sb.AppendLine("Method name: " + methodNameAndSignature.Name);
                        sb.AppendLine("Instantiation:");
                        for (int i = 0; i < genericArguments.Length; i++)
                        {
                            sb.AppendLine("  Argument " + i.LowLevelToString() + ": " + genericArguments[i].LowLevelToStringRawEETypeAddress());
                        }

                        Environment.FailFast(sb.ToString());
                    }
                }
                else
                {
                    methodPointer = IntPtr.Zero;
                    dictionaryPointer = IntPtr.Zero;
                    slotUpdated = true;
                    methodName = methodNameAndSignature.Name;
                    methodSignature = methodNameAndSignature.Signature;
                }
                return true;
            }
            else
            {
                slotUpdated = false;
                return ResolveGenericVirtualMethodTarget(targetHandle, declaringType, genericArguments, methodNameAndSignature, out methodPointer, out dictionaryPointer);
            }
        }

        private static MethodNameAndSignature GetMethodNameAndSignatureFromNativeReader(NativeReader nativeLayoutReader, TypeManagerHandle moduleHandle, uint nativeLayoutOffset)
        {
            NativeParser parser = new NativeParser(nativeLayoutReader, nativeLayoutOffset);

            string methodName = parser.GetString();

            // Signatures are indirected to through a relative offset so that we don't have to parse them
            // when not comparing signatures (parsing them requires resolving types and is tremendously
            // expensive).
            NativeParser sigParser = parser.GetParserFromRelativeOffset();
            RuntimeSignature methodSig = RuntimeSignature.CreateFromNativeLayoutSignature(moduleHandle, sigParser.Offset);

            return new MethodNameAndSignature(methodName, methodSig);
        }

        private static RuntimeTypeHandle GetOpenTypeDefinition(RuntimeTypeHandle typeHandle, out RuntimeTypeHandle[] typeArgumentsHandles)
        {
            if (RuntimeAugments.IsGenericType(typeHandle))
                return RuntimeAugments.GetGenericInstantiation(typeHandle, out typeArgumentsHandles);

            typeArgumentsHandles = null;
            return typeHandle;
        }

        private static RuntimeTypeHandle GetTypeDefinition(RuntimeTypeHandle typeHandle)
        {
            if (RuntimeAugments.IsGenericType(typeHandle))
                return RuntimeAugments.GetGenericDefinition(typeHandle);

            return typeHandle;
        }

        private static bool FindMatchingInterfaceSlot(NativeFormatModuleInfo module, NativeReader nativeLayoutReader, ref NativeParser entryParser, ref ExternalReferencesTable extRefs, ref RuntimeTypeHandle declaringType, ref MethodNameAndSignature methodNameAndSignature, RuntimeTypeHandle instanceTypeHandle, RuntimeTypeHandle openTargetTypeHandle, RuntimeTypeHandle[] targetTypeInstantiation, bool variantDispatch, bool defaultMethods)
        {
            uint numTargetImplementations = entryParser.GetUnsigned();

#if GVM_RESOLUTION_TRACE
            Debug.WriteLine(" :: Declaring type = " + GetTypeNameDebug(declaringType));
            Debug.WriteLine(" :: Target type = " + GetTypeNameDebug(openTargetTypeHandle));
#endif

            for (uint j = 0; j < numTargetImplementations; j++)
            {
                uint nameAndSigToken = entryParser.GetUnsigned();

                MethodNameAndSignature targetMethodNameAndSignature = null;
                RuntimeTypeHandle targetTypeHandle = default;
                bool isDefaultInterfaceMethodImplementation;

                if (nameAndSigToken != SpecialGVMInterfaceEntry.Diamond && nameAndSigToken != SpecialGVMInterfaceEntry.Reabstraction)
                {
                    targetMethodNameAndSignature = GetMethodNameAndSignatureFromNativeReader(nativeLayoutReader, module.Handle, nameAndSigToken);
                    targetTypeHandle = extRefs.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                    isDefaultInterfaceMethodImplementation = RuntimeAugments.IsInterface(targetTypeHandle);
#if GVM_RESOLUTION_TRACE
                    Debug.WriteLine("    Searching for GVM implementation on targe type = " + GetTypeNameDebug(targetTypeHandle));
#endif
                }
                else
                {
                    isDefaultInterfaceMethodImplementation = true;
                }

                uint numIfaceImpls = entryParser.GetUnsigned();

                for (uint k = 0; k < numIfaceImpls; k++)
                {
                    RuntimeTypeHandle implementingTypeHandle = extRefs.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());

#if GVM_RESOLUTION_TRACE
                    Debug.WriteLine("      -> Current implementing type = " + GetTypeNameDebug(implementingTypeHandle));
#endif

                    uint numIfaceSigs = entryParser.GetUnsigned();

                    if (!openTargetTypeHandle.Equals(implementingTypeHandle)
                        || defaultMethods != isDefaultInterfaceMethodImplementation)
                    {
                        // Skip over signatures data
                        for (uint l = 0; l < numIfaceSigs; l++)
                            entryParser.GetUnsigned();

                        continue;
                    }

                    for (uint l = 0; l < numIfaceSigs; l++)
                    {
                        RuntimeTypeHandle currentIfaceTypeHandle = default(RuntimeTypeHandle);

                        NativeParser ifaceSigParser = new NativeParser(nativeLayoutReader, entryParser.GetUnsigned());

                        if (TypeLoaderEnvironment.Instance.GetTypeFromSignatureAndContext(ref ifaceSigParser, module.Handle, targetTypeInstantiation, null, out currentIfaceTypeHandle))
                        {
#if GVM_RESOLUTION_TRACE
                            Debug.WriteLine("         -> Current interface on type = " + GetTypeNameDebug(currentIfaceTypeHandle));
#endif
                            Debug.Assert(!currentIfaceTypeHandle.IsNull());

                            if ((!variantDispatch && declaringType.Equals(currentIfaceTypeHandle)) ||
                                (variantDispatch && RuntimeAugments.IsAssignableFrom(declaringType, currentIfaceTypeHandle)))
                            {
#if GVM_RESOLUTION_TRACE
                                Debug.WriteLine("    " + (declaringType.Equals(currentIfaceTypeHandle) ? "Exact" : "Variant-compatible") + " match found on this target type!");
#endif
                                if (targetMethodNameAndSignature == null)
                                {
                                    if (nameAndSigToken == SpecialGVMInterfaceEntry.Diamond)
                                    {
                                        throw new AmbiguousImplementationException();
                                    }
                                    else
                                    {
                                        Debug.Assert(nameAndSigToken == SpecialGVMInterfaceEntry.Reabstraction);
                                        throw new EntryPointNotFoundException();
                                    }
                                }

                                // We found the GVM slot target for the input interface GVM call, so let's update the interface GVM slot and return success to the caller
                                if (!RuntimeAugments.IsInterface(targetTypeHandle) || !RuntimeAugments.IsGenericTypeDefinition(targetTypeHandle))
                                {
                                    // Not a default interface method or default interface method on a non-generic type.
                                    // We have a usable type handle.
                                    declaringType = targetTypeHandle;
                                }
                                else if (RuntimeAugments.IsGenericType(currentIfaceTypeHandle) && RuntimeAugments.GetGenericDefinition(currentIfaceTypeHandle).Equals(targetTypeHandle))
                                {
                                    // Default interface method implemented on the same type that declared the slot.
                                    // Use the instantiation as-is from what we found.
                                    declaringType = currentIfaceTypeHandle;
                                }
                                else
                                {
                                    declaringType = default;

                                    // Default interface method implemented on a different generic interface.
                                    // We need to find a usable instantiation. There should be only one match because we
                                    // would be dealing with a diamond otherwise.
                                    int numInstanceInterfaces = RuntimeAugments.GetInterfaceCount(instanceTypeHandle);
                                    for (int instIntfIndex = 0; instIntfIndex < numInstanceInterfaces; instIntfIndex++)
                                    {
                                        RuntimeTypeHandle instIntf = RuntimeAugments.GetInterface(instanceTypeHandle, instIntfIndex);
                                        if (RuntimeAugments.IsGenericType(instIntf)
                                            && RuntimeAugments.GetGenericDefinition(instIntf).Equals(targetTypeHandle))
                                        {
                                            // Got a potential interface. Check if the implementing interface is in the interface
                                            // list. We don't want IsAssignableFrom because we need an exact match.
                                            int numIntInterfaces = RuntimeAugments.GetInterfaceCount(instIntf);
                                            for (int intIntfIndex = 0; intIntfIndex < numIntInterfaces; intIntfIndex++)
                                            {
                                                if (RuntimeAugments.GetInterface(instIntf, intIntfIndex).Equals(currentIfaceTypeHandle))
                                                {
                                                    Debug.Assert(declaringType.IsNull());
                                                    declaringType = instIntf;
#if !DEBUG
                                                    break;
#endif
                                                }
                                            }
#if !DEBUG
                                            if (!declaringType.IsNull())
                                                break;
#endif
                                        }
                                    }

                                    Debug.Assert(!declaringType.IsNull());
                                }

                                methodNameAndSignature = targetMethodNameAndSignature;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static bool ResolveInterfaceGenericVirtualMethodSlot(RuntimeTypeHandle targetTypeHandle, bool lookForDefaultImplementation, ref RuntimeTypeHandle declaringType, ref MethodNameAndSignature methodNameAndSignature)
        {
            if (IsPregeneratedOrTemplateRuntimeTypeHandle(targetTypeHandle))
            {
                // If the target type isn't dynamic, or at least is template type generated, the static lookup logic is what we want.
                return ResolveInterfaceGenericVirtualMethodSlot_Static(targetTypeHandle, lookForDefaultImplementation, ref declaringType, ref methodNameAndSignature);
            }
            else
            {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
                TypeSystemContext context = TypeSystemContextFactory.Create();
                DefType targetType = (DefType)context.ResolveRuntimeTypeHandle(targetTypeHandle);

                // Method being called...
                MethodDesc targetVirtualMethod = ResolveTypeHandleAndMethodNameAndSigToVirtualMethodDesc(context, declaringType, methodNameAndSignature);

                if (targetVirtualMethod == null)
                {
                    // If we can't find the method in the type system, it must only be present in the static environment. Search there instead.
                    TypeSystemContextFactory.Recycle(context);
                    return ResolveInterfaceGenericVirtualMethodSlot_Static(targetTypeHandle, ref declaringType, ref methodNameAndSignature);
                }

                TypeDesc instanceDefTypeToExamine;
                MethodDesc newlyFoundVirtualMethod = LazyVTableResolver.ResolveInterfaceMethodToVirtualMethod(targetType, out instanceDefTypeToExamine, targetVirtualMethod);

                targetVirtualMethod = newlyFoundVirtualMethod;

                // The pregenerated base type must be the one that implements the interface method
                // Call into Redhawk to deal with this.
                if ((newlyFoundVirtualMethod == null) && (instanceDefTypeToExamine != null))
                {
                    TypeSystemContextFactory.Recycle(context);
                    // If we can't find the method in the type system, the overload must be defined in the static environment. Search there instead.
                    return ResolveInterfaceGenericVirtualMethodSlot_Static(instanceDefTypeToExamine.GetRuntimeTypeHandle(), ref declaringType, ref methodNameAndSignature);
                }

                declaringType = targetVirtualMethod.OwningType.GetRuntimeTypeHandle();
                methodNameAndSignature = targetVirtualMethod.NameAndSignature;
                TypeSystemContextFactory.Recycle(context);
                return true;
#else
                Environment.FailFast("GVM Resolution for non template or pregenerated type");
                return false;
#endif
            }
        }

        private static bool ResolveInterfaceGenericVirtualMethodSlot_Static(RuntimeTypeHandle targetTypeHandle, bool lookForDefaultImplementation, ref RuntimeTypeHandle declaringType, ref MethodNameAndSignature methodNameAndSignature)
        {
            // Get the open type definition of the containing type of the generic virtual method being resolved
            RuntimeTypeHandle openCallingTypeHandle = GetTypeDefinition(declaringType);

            // Get the open type definition of the current type of the object instance on which the GVM is being resolved
            RuntimeTypeHandle openTargetTypeHandle;
            RuntimeTypeHandle[] targetTypeInstantiation;
            openTargetTypeHandle = GetOpenTypeDefinition(targetTypeHandle, out targetTypeInstantiation);

#if GVM_RESOLUTION_TRACE
            Debug.WriteLine("INTERFACE GVM call = " + GetTypeNameDebug(declaringType) + "." + methodNameAndSignature.Name);
#endif

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules(RuntimeAugments.GetModuleFromTypeHandle(openTargetTypeHandle)))
            {
                NativeReader gvmTableReader;
                if (!TryGetNativeReaderForBlob(module, ReflectionMapBlob.InterfaceGenericVirtualMethodTable, out gvmTableReader))
                    continue;

                NativeReader nativeLayoutReader;
                if (!TryGetNativeReaderForBlob(module, ReflectionMapBlob.NativeLayoutInfo, out nativeLayoutReader))
                    continue;

                NativeParser gvmTableParser = new NativeParser(gvmTableReader, 0);
                NativeHashtable gvmHashtable = new NativeHashtable(gvmTableParser);

                ExternalReferencesTable extRefs = default(ExternalReferencesTable);
                extRefs.InitializeCommonFixupsTable(module);

                var lookup = gvmHashtable.Lookup(openCallingTypeHandle.GetHashCode());

                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    RuntimeTypeHandle interfaceTypeHandle = extRefs.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                    if (!openCallingTypeHandle.Equals(interfaceTypeHandle))
                        continue;

                    uint nameAndSigToken = entryParser.GetUnsigned();
                    MethodNameAndSignature interfaceMethodNameAndSignature = GetMethodNameAndSignatureFromNativeReader(nativeLayoutReader, module.Handle, nameAndSigToken);

                    if (!interfaceMethodNameAndSignature.Equals(methodNameAndSignature))
                        continue;

                    // For each of the possible GVM slot targets for the current interface call, we will do the following:
                    //
                    //  Step 1: Scan the types that currently provide implementations for the current GVM slot target, and look
                    //          for ones that match the target object's type.
                    //
                    //  Step 2: For each type that we find in step #1, get a list of all the interfaces that the current GVM target
                    //          provides an implementation for
                    //
                    //  Step 3: For each interface in the list in step #2, parse the signature of that interface, do the generic argument
                    //          substitution (in case of a generic interface), and check if this interface signature is assignable from the
                    //          calling interface signature (from the name and sig input). if there is an exact match based on
                    //          interface type, then we've found the right slot. Otherwise, re-scan the entry again and see if some interface
                    //          type is compatible with the initial slots interface by means of variance.
                    //          This is done by calling the TypeLoaderEnvironment helper function.
                    //
                    // Example:
                    //      public interface IFoo<out T, out U>
                    //      {
                    //          string M1<V>();
                    //      }
                    //      public class Foo1<T, U> : IFoo<T, U>, IFoo<Kvp<T, string>, U>
                    //      {
                    //          string IFoo<T, U>.M1<V>() { ... }
                    //          public virtual string M1<V>() { ... }
                    //      }
                    //      public class Foo2<T, U> : Foo1<object, U>, IFoo<U, T>
                    //      {
                    //          string IFoo<U, T>.M1<V>() { ... }
                    //      }
                    //
                    //  GVM Table layout for IFoo<T, U>.M1<V>:
                    //  {
                    //      InterfaceTypeHandle = IFoo<T, U>
                    //      InterfaceMethodNameAndSignature = { "M1", SigOf(string M1) }
                    //      GVMTargetSlots[] =
                    //      {
                    //          {
                    //              TargetMethodNameAndSignature = { "M1", SigOf(M1) }
                    //              TargetTypeHandle = Foo1<T, U>
                    //              ImplementingTypes[] = {
                    //                  ImplementingTypeHandle = Foo1<T, U>
                    //                  ImplementedInterfacesSignatures[] = { SigOf(IFoo<!0, !1>) }
                    //              }
                    //          },
                    //
                    //          {
                    //              TargetMethodNameAndSignature = { "M1", SigOf(M1) }
                    //              TargetTypeHandle = Foo1<T, U>
                    //              ImplementingTypes[] = {
                    //                  ImplementingTypeHandle = Foo1<T, U>
                    //                  ImplementedInterfacesSignatures[] = { SigOf(IFoo<Kvp<!0, string>, !1>) }
                    //              }
                    //          },
                    //
                    //          {
                    //              TargetMethodNameAndSignature = { "M1", SigOf(M1) }
                    //              TargetTypeHandle = Foo2<T, U>
                    //              ImplementingTypes = {
                    //                  ImplementingTypeHandle = Foo2<T, U>
                    //                  ImplementedInterfacesSignatures[] = { SigOf(IFoo<!1, !0>) }
                    //              }
                    //          },
                    //      }
                    //  }
                    //

                    uint currentOffset = entryParser.Offset;

                    // Non-variant dispatch of a variant generic interface generic virtual method.
                    if (FindMatchingInterfaceSlot(module, nativeLayoutReader, ref entryParser, ref extRefs, ref declaringType, ref methodNameAndSignature, targetTypeHandle, openTargetTypeHandle, targetTypeInstantiation, false, lookForDefaultImplementation))
                    {
                        return true;
                    }

                    entryParser.Offset = currentOffset;

                    // Variant dispatch of a variant generic interface generic virtual method.
                    if (FindMatchingInterfaceSlot(module, nativeLayoutReader, ref entryParser, ref extRefs, ref declaringType, ref methodNameAndSignature, targetTypeHandle, openTargetTypeHandle, targetTypeInstantiation, true, lookForDefaultImplementation))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
        public MethodDesc ResolveTypeHandleAndMethodNameAndSigToVirtualMethodDesc(TypeSystemContext context, RuntimeTypeHandle declaringTypeHandle, MethodNameAndSignature methodNameAndSignature)
        {
            TypeDesc declaringType = context.ResolveRuntimeTypeHandle(declaringTypeHandle);
            MethodDesc targetVirtualMethod = null;
            foreach (MethodDesc m in declaringType.GetAllMethods())
            {
                if (!m.IsVirtual)
                    continue;

                if (m.NameAndSignature.Equals(methodNameAndSignature))
                {
                    targetVirtualMethod = m;
                }
            }

            return targetVirtualMethod;
        }
#endif

        public static unsafe bool IsPregeneratedOrTemplateRuntimeTypeHandle(RuntimeTypeHandle rtth)
        {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            if (!rtth.IsDynamicType())
                return true;

            if (rtth.ToEETypePtr()->DynamicModule == null)
                return true;

            return rtth.ToEETypePtr()->DynamicModule->DynamicTypeSlotDispatchResolve == null;
#else
            return true;
#endif
        }

        private bool ResolveGenericVirtualMethodTarget(RuntimeTypeHandle targetTypeHandle, RuntimeTypeHandle declaringTypeHandle, RuntimeTypeHandle[] genericArguments, MethodNameAndSignature callingMethodNameAndSignature, out IntPtr methodPointer, out IntPtr dictionaryPointer)
        {
            if (IsPregeneratedOrTemplateRuntimeTypeHandle(targetTypeHandle))
            {
                // If the target type isn't dynamic, or at least is template type generated, the static lookup logic is what we want.
                return ResolveGenericVirtualMethodTarget_Static(targetTypeHandle, declaringTypeHandle, genericArguments, callingMethodNameAndSignature, out methodPointer, out dictionaryPointer);
            }
            else
            {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
                methodPointer = IntPtr.Zero;
                dictionaryPointer = IntPtr.Zero;

                TypeSystemContext context = TypeSystemContextFactory.Create();
                DefType targetType = (DefType)context.ResolveRuntimeTypeHandle(targetTypeHandle);

                // Method being called...
                MethodDesc targetVirtualMethod = ResolveTypeHandleAndMethodNameAndSigToVirtualMethodDesc(context, declaringTypeHandle, callingMethodNameAndSignature);

                if (targetVirtualMethod == null)
                {
                    // If we can't find the method in the type system, it must only be present in the static environment. Search there instead.
                    TypeSystemContextFactory.Recycle(context);
                    return ResolveGenericVirtualMethodTarget_Static(targetTypeHandle, declaringTypeHandle, genericArguments, callingMethodNameAndSignature, out methodPointer, out dictionaryPointer);
                }

                MethodDesc dispatchMethod = targetType.FindVirtualFunctionTargetMethodOnObjectType(targetVirtualMethod);

                if (dispatchMethod == null)
                    return false;

                Instantiation targetMethodInstantiation = context.ResolveRuntimeTypeHandles(genericArguments);
                MethodDesc instantiatedDispatchMethod = dispatchMethod.Context.ResolveGenericMethodInstantiation(dispatchMethod.OwningType.IsValueType/* get the unboxing stub */,
                    dispatchMethod.OwningType.GetClosestDefType(),
                    dispatchMethod.NameAndSignature,
                    targetMethodInstantiation, IntPtr.Zero, false);

                GenericDictionaryCell cell = GenericDictionaryCell.CreateMethodCell(instantiatedDispatchMethod, false);
                using (LockHolder.Hold(_typeLoaderLock))
                {
                    // Now that we hold the lock, we may find that existing types can now find
                    // their associated RuntimeTypeHandle. Flush the type builder states as a way
                    // to force the reresolution of RuntimeTypeHandles which couldn't be found before.
                    context.FlushTypeBuilderStates();

                    TypeBuilder.ResolveSingleCell(cell, out methodPointer);
                }

                TypeSystemContextFactory.Recycle(context);

                return true;
#else
                methodPointer = IntPtr.Zero;
                dictionaryPointer = IntPtr.Zero;
                Environment.FailFast("GVM Resolution for non template or pregenerated type");
                return false;
#endif
            }
        }

        private unsafe bool ResolveGenericVirtualMethodTarget_Static(RuntimeTypeHandle targetTypeHandle, RuntimeTypeHandle declaringType, RuntimeTypeHandle[] genericArguments, MethodNameAndSignature callingMethodNameAndSignature, out IntPtr methodPointer, out IntPtr dictionaryPointer)
        {
            methodPointer = dictionaryPointer = IntPtr.Zero;

            // Get the open type definition of the containing type of the generic virtual method being resolved
            RuntimeTypeHandle openCallingTypeHandle = GetTypeDefinition(declaringType);

            // Get the open type definition of the current type of the object instance on which the GVM is being resolved
            RuntimeTypeHandle openTargetTypeHandle = GetTypeDefinition(targetTypeHandle);

            int hashCode = openCallingTypeHandle.GetHashCode();
            hashCode = ((hashCode << 13) ^ hashCode) ^ openTargetTypeHandle.GetHashCode();

#if GVM_RESOLUTION_TRACE
            Debug.WriteLine("GVM Target Resolution = " + GetTypeNameDebug(targetTypeHandle) + "." + callingMethodNameAndSignature.Name);
#endif

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules(RuntimeAugments.GetModuleFromTypeHandle(openTargetTypeHandle)))
            {
                NativeReader gvmTableReader;
                if (!TryGetNativeReaderForBlob(module, ReflectionMapBlob.GenericVirtualMethodTable, out gvmTableReader))
                    continue;

                NativeReader nativeLayoutReader;
                if (!TryGetNativeReaderForBlob(module, ReflectionMapBlob.NativeLayoutInfo, out nativeLayoutReader))
                    continue;

                NativeParser gvmTableParser = new NativeParser(gvmTableReader, 0);
                NativeHashtable gvmHashtable = new NativeHashtable(gvmTableParser);
                ExternalReferencesTable extRefs = default(ExternalReferencesTable);
                extRefs.InitializeCommonFixupsTable(module);

                var lookup = gvmHashtable.Lookup(hashCode);

                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    RuntimeTypeHandle parsedCallingTypeHandle = extRefs.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                    if (!parsedCallingTypeHandle.Equals(openCallingTypeHandle))
                        continue;

                    RuntimeTypeHandle parsedTargetTypeHandle = extRefs.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                    if (!parsedTargetTypeHandle.Equals(openTargetTypeHandle))
                        continue;

                    uint parsedCallingNameAndSigToken = entryParser.GetUnsigned();
                    MethodNameAndSignature parsedCallingNameAndSignature = GetMethodNameAndSignatureFromNativeReader(nativeLayoutReader, module.Handle, parsedCallingNameAndSigToken);

                    if (!parsedCallingNameAndSignature.Equals(callingMethodNameAndSignature))
                        continue;

                    uint parsedTargetMethodNameAndSigToken = entryParser.GetUnsigned();
                    MethodNameAndSignature targetMethodNameAndSignature = GetMethodNameAndSignatureFromNativeReader(nativeLayoutReader, module.Handle, parsedTargetMethodNameAndSigToken);

                    Debug.Assert(targetMethodNameAndSignature != null);

                    if (!TryGetGenericVirtualMethodPointer(targetTypeHandle, targetMethodNameAndSignature, genericArguments, out methodPointer, out dictionaryPointer))
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("Generic virtual method pointer lookup failure.");
                        sb.AppendLine();
                        sb.AppendLine("Declaring type handle: " + declaringType.LowLevelToStringRawEETypeAddress());
                        sb.AppendLine("Target type handle: " + targetTypeHandle.LowLevelToStringRawEETypeAddress());
                        sb.AppendLine("Method name: " + targetMethodNameAndSignature.Name);
                        sb.AppendLine("Instantiation:");
                        for (int i = 0; i < genericArguments.Length; i++)
                        {
                            sb.AppendLine("  Argument " + i.LowLevelToString() + ": " + genericArguments[i].LowLevelToStringRawEETypeAddress());
                        }

                        Environment.FailFast(sb.ToString());
                    }

                    return true;
                }
            }

            return false;
        }
    }
}
