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
        private static string GetTypeNameDebug(TypeDesc type)
        {
            string result;

            TypeDesc typeDefinition = type.GetTypeDefinition();
            if (type != typeDefinition)
            {
                result = GetTypeNameDebug(typeDefinition) + "<";
                for (int i = 0; i < type.Instantiation.Length; i++)
                    result += (i == 0 ? "" : ",") + GetTypeNameDebug(type.Instantiation[0]);
                return result + ">";
            }
            else if (type.IsDefType)
            {
                System.Reflection.Runtime.General.QTypeDefinition qTypeDefinition;

                RuntimeTypeHandle rtth = type.GetRuntimeTypeHandle();

                // Check if we have metadata.
                if (Instance.TryGetMetadataForNamedType(rtth, out qTypeDefinition))
                    return qTypeDefinition.NativeFormatHandle.GetFullName(qTypeDefinition.NativeFormatReader);
            }
            return "?";
        }

        internal static InstantiatedMethod GVMLookupForSlotWorker(DefType targetType, InstantiatedMethod slotMethod)
        {
            InstantiatedMethod resolution = null;

            bool lookForDefaultImplementations = false;

        again:
            // Walk parent hierarchy attempting to resolve
            DefType currentType = targetType;
            bool resolvingInterfaceMethod = slotMethod.OwningType.IsInterface;

            while (currentType is not null)
            {
                if (resolvingInterfaceMethod)
                {
                    resolution = ResolveInterfaceGenericVirtualMethodSlot(currentType, slotMethod, lookForDefaultImplementations);
                    if (resolution != null)
                    {
                        // If this is a static virtual, we're done, nobody can override this.
                        if (IsStaticMethodSignature(resolution.NameAndSignature))
                        {
                            Debug.Assert(IsStaticMethodSignature(slotMethod.NameAndSignature));
                            break;
                        }

                        // If this is a default implementation, we're also done.
                        if (resolution.OwningType.IsInterface)
                        {
                            Debug.Assert(lookForDefaultImplementations);
                            break;
                        }

                        // Otherwise resolve to whatever implements the virtual method on the type.
                        return GVMLookupForSlotWorker(currentType, resolution);
                    }
                }
                else
                {
                    resolution = ResolveGenericVirtualMethodTarget(currentType, slotMethod);
                    if (resolution != null)
                        break;
                }

                currentType = currentType.BaseType;
            }

            if (resolution == null
                && !lookForDefaultImplementations
                && resolvingInterfaceMethod)
            {
                lookForDefaultImplementations = true;
                goto again;
            }

            if (resolution == null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Generic virtual method pointer lookup failure.");
                sb.AppendLine();
                sb.AppendLine("Declaring type: " + GetTypeNameDebug(slotMethod.OwningType));
                sb.AppendLine("Target type: " + GetTypeNameDebug(targetType));
                sb.AppendLine("Method name: " + slotMethod.NameAndSignature.Name);
                sb.AppendLine("Instantiation:");
                for (int i = 0; i < slotMethod.Instantiation.Length; i++)
                {
                    sb.AppendLine("  Argument " + i.LowLevelToString() + ": " + GetTypeNameDebug(slotMethod.Instantiation[i]));
                }

                Environment.FailFast(sb.ToString());
            }

            return resolution;
        }

        internal unsafe IntPtr ResolveGenericVirtualMethodTarget(RuntimeTypeHandle type, RuntimeMethodHandle slot)
        {
            TypeSystemContext context = TypeSystemContextFactory.Create();
            DefType targetType = (DefType)context.ResolveRuntimeTypeHandle(type);

            InstantiatedMethod slotMethod = (InstantiatedMethod)GetMethodDescForRuntimeMethodHandle(context, slot);

            InstantiatedMethod result = GVMLookupForSlotWorker(targetType, slotMethod);

            if (!TryGetGenericVirtualMethodPointer(result, out IntPtr methodPointer, out IntPtr dictionaryPointer))
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Failed to create generic virtual method implementation");
                sb.AppendLine();
                sb.AppendLine("Declaring type: " + GetTypeNameDebug(result.OwningType));
                sb.AppendLine("Method name: " + result.NameAndSignature.Name);
                sb.AppendLine("Instantiation:");
                for (int i = 0; i < result.Instantiation.Length; i++)
                {
                    sb.AppendLine("  Argument " + i.LowLevelToString() + ": " + GetTypeNameDebug(result.Instantiation[i]));
                }
                Environment.FailFast(sb.ToString());
            }

            TypeSystemContextFactory.Recycle(context);
            return FunctionPointerOps.GetGenericMethodFunctionPointer(methodPointer, dictionaryPointer);
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

        private static RuntimeTypeHandle GetTypeDefinition(RuntimeTypeHandle typeHandle)
        {
            if (RuntimeAugments.IsGenericType(typeHandle))
                return RuntimeAugments.GetGenericDefinition(typeHandle);

            return typeHandle;
        }

        private static InstantiatedMethod FindMatchingInterfaceSlot(NativeFormatModuleInfo module, NativeReader nativeLayoutReader, ref NativeParser entryParser, ref ExternalReferencesTable extRefs, InstantiatedMethod slotMethod, DefType targetType, bool variantDispatch, bool defaultMethods)
        {
            uint numTargetImplementations = entryParser.GetUnsigned();

#if GVM_RESOLUTION_TRACE
            Debug.WriteLine(" :: Declaring type = " + GetTypeNameDebug(slotMethod.OwningType));
            Debug.WriteLine(" :: Target type = " + GetTypeNameDebug(targetType));
#endif

            TypeSystemContext context = slotMethod.Context;
            TypeDesc declaringType = slotMethod.OwningType;

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
                    Debug.WriteLine("    Searching for GVM implementation on targe type = " + RuntimeAugments.GetLastResortString(targetTypeHandle));
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
                    Debug.WriteLine("      -> Current implementing type = " + RuntimeAugments.GetLastResortString(implementingTypeHandle));
#endif

                    uint numIfaceSigs = entryParser.GetUnsigned();

                    if (!targetType.GetTypeDefinition().RuntimeTypeHandle.Equals(implementingTypeHandle)
                        || defaultMethods != isDefaultInterfaceMethodImplementation)
                    {
                        // Skip over signatures data
                        for (uint l = 0; l < numIfaceSigs; l++)
                            entryParser.GetUnsigned();

                        continue;
                    }

                    for (uint l = 0; l < numIfaceSigs; l++)
                    {
                        NativeParser ifaceSigParser = new NativeParser(nativeLayoutReader, entryParser.GetUnsigned());

                        NativeLayoutInfoLoadContext nativeLayoutContext = new NativeLayoutInfoLoadContext();
                        nativeLayoutContext._module = ModuleList.Instance.GetModuleInfoByHandle(module.Handle);
                        nativeLayoutContext._typeSystemContext = context;
                        nativeLayoutContext._typeArgumentHandles = targetType.Instantiation;

                        TypeDesc currentIfaceType = nativeLayoutContext.GetType(ref ifaceSigParser);

                        {
#if GVM_RESOLUTION_TRACE
                            Debug.WriteLine("         -> Current interface on type = " + GetTypeNameDebug(currentIfaceType));
#endif

                            if ((!variantDispatch && declaringType.Equals(currentIfaceType)) ||
                                (variantDispatch && currentIfaceType.CanCastTo(declaringType)))
                            {
#if GVM_RESOLUTION_TRACE
                                Debug.WriteLine("    " + (declaringType.Equals(currentIfaceType) ? "Exact" : "Variant-compatible") + " match found on this target type!");
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

                                DefType interfaceImplType;

                                // We found the GVM slot target for the input interface GVM call, so let's update the interface GVM slot and return success to the caller
                                if (!RuntimeAugments.IsInterface(targetTypeHandle) || !RuntimeAugments.IsGenericTypeDefinition(targetTypeHandle))
                                {
                                    // Not a default interface method or default interface method on a non-generic type.
                                    // We have a usable type handle.
                                    interfaceImplType = (DefType)context.ResolveRuntimeTypeHandle(targetTypeHandle);
                                }
                                else if (currentIfaceType.HasInstantiation && currentIfaceType.GetTypeDefinition().RuntimeTypeHandle.Equals(targetTypeHandle))
                                {
                                    // Default interface method implemented on the same type that declared the slot.
                                    // Use the instantiation as-is from what we found.
                                    interfaceImplType = (DefType)currentIfaceType;
                                }
                                else
                                {
                                    interfaceImplType = null;

                                    // Default interface method implemented on a different generic interface.
                                    // We need to find a usable instantiation. There should be only one match because we
                                    // would be dealing with a diamond otherwise.
                                    foreach (DefType instIntf in targetType.RuntimeInterfaces)
                                    {
                                        if (instIntf.GetTypeDefinition().RuntimeTypeHandle.Equals(targetTypeHandle))
                                        {
                                            // Got a potential interface. Check if the implementing interface is in the interface
                                            // list. We don't want IsAssignableFrom because we need an exact match.
                                            foreach (DefType intfOnIntf in instIntf.RuntimeInterfaces)
                                            {
                                                if (intfOnIntf.Equals(currentIfaceType))
                                                {
                                                    Debug.Assert(interfaceImplType == null);
                                                    interfaceImplType = instIntf;
#if !DEBUG
                                                    break;
#endif
                                                }
                                            }
#if !DEBUG
                                            if (interfaceImplType != null)
                                                break;
#endif
                                        }
                                    }

                                    Debug.Assert(interfaceImplType != null);
                                }

                                return (InstantiatedMethod)context.ResolveGenericMethodInstantiation(false, interfaceImplType, targetMethodNameAndSignature, slotMethod.Instantiation, IntPtr.Zero, false);
                            }
                        }
                    }
                }
            }

            return null;
        }

        private static InstantiatedMethod ResolveInterfaceGenericVirtualMethodSlot(DefType targetType, InstantiatedMethod slotMethod, bool lookForDefaultImplementation)
        {
            // Get the open type definition of the containing type of the generic virtual method being resolved
            RuntimeTypeHandle openCallingTypeHandle = slotMethod.OwningType.GetTypeDefinition().RuntimeTypeHandle;

            // Get the open type definition of the current type of the object instance on which the GVM is being resolved
            RuntimeTypeHandle openTargetTypeHandle = targetType.GetTypeDefinition().RuntimeTypeHandle;

#if GVM_RESOLUTION_TRACE
            Debug.WriteLine("INTERFACE GVM call = " + GetTypeNameDebug(slotMethod.OwningType) + "." + slotMethod.NameAndSignature.Name);
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

                    if (!interfaceMethodNameAndSignature.Equals(slotMethod.NameAndSignature))
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
                    InstantiatedMethod result = FindMatchingInterfaceSlot(module, nativeLayoutReader, ref entryParser, ref extRefs, slotMethod, targetType, false, lookForDefaultImplementation);
                    if (result != null)
                        return result;

                    entryParser.Offset = currentOffset;

                    // Variant dispatch of a variant generic interface generic virtual method.
                    return FindMatchingInterfaceSlot(module, nativeLayoutReader, ref entryParser, ref extRefs, slotMethod, targetType, true, lookForDefaultImplementation);
                }
            }

            return null;
        }

        private static InstantiatedMethod ResolveGenericVirtualMethodTarget(DefType targetType, InstantiatedMethod slotMethod)
        {
            // Get the open type definition of the containing type of the generic virtual method being resolved
            RuntimeTypeHandle openCallingTypeHandle = GetTypeDefinition(slotMethod.OwningType.GetTypeDefinition().RuntimeTypeHandle);

            // Get the open type definition of the current type of the object instance on which the GVM is being resolved
            RuntimeTypeHandle openTargetTypeHandle = GetTypeDefinition(targetType.GetTypeDefinition().RuntimeTypeHandle);

            int hashCode = openCallingTypeHandle.GetHashCode();
            hashCode = ((hashCode << 13) ^ hashCode) ^ openTargetTypeHandle.GetHashCode();

#if GVM_RESOLUTION_TRACE
            Debug.WriteLine("GVM Target Resolution = " + GetTypeNameDebug(targetType) + "." + slotMethod.NameAndSignature.Name);
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

                    if (!parsedCallingNameAndSignature.Equals(slotMethod.NameAndSignature))
                        continue;

                    uint parsedTargetMethodNameAndSigToken = entryParser.GetUnsigned();
                    MethodNameAndSignature targetMethodNameAndSignature = GetMethodNameAndSignatureFromNativeReader(nativeLayoutReader, module.Handle, parsedTargetMethodNameAndSigToken);

                    Debug.Assert(targetMethodNameAndSignature != null);

                    TypeSystemContext context = slotMethod.Context;
                    return (InstantiatedMethod)context.ResolveGenericMethodInstantiation(false, targetType, targetMethodNameAndSignature, slotMethod.Instantiation, IntPtr.Zero, false);
                }
            }

            return null;
        }
    }
}
