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
using System.Reflection.Runtime.General;

namespace Internal.Runtime.TypeLoader
{
    public sealed partial class TypeLoaderEnvironment
    {
        // Container data structures with info on dynamically created method and type instantiations that need registration.
        // More convinient to pass around in calls, instead of passing the individual components
        internal struct DynamicGenericsRegistrationData
        {
            public int TypesToRegisterCount;
            public IEnumerable<TypeEntryToRegister> TypesToRegister;
            public int MethodsToRegisterCount;
            public IEnumerable<GenericMethodEntry> MethodsToRegister;
        }

        // To keep the synchronization simple, we execute all dynamic generic type registration/lookups under a global lock
        private Lock _dynamicGenericsLock = new Lock();

        internal void RegisterDynamicGenericTypesAndMethods(DynamicGenericsRegistrationData registrationData)
        {
            using (LockHolder.Hold(_dynamicGenericsLock))
            {
                int registeredTypesCount = 0;
                int registeredMethodsCount = 0;
                int nativeFormatTypesRegisteredCount = 0;
                TypeEntryToRegister[] registeredTypes = null;
                GenericMethodEntry[] registeredMethods = null;

                try
                {
                    if (registrationData.TypesToRegister != null)
                    {
                        registeredTypes = new TypeEntryToRegister[registrationData.TypesToRegisterCount];

                        foreach (TypeEntryToRegister typeEntry in registrationData.TypesToRegister)
                        {
                            // Keep track of registered type handles so that that we can rollback the registration on exception
                            registeredTypes[registeredTypesCount++] = typeEntry;

                            // Information tracked in these dictionaries is (partially) redundant with information tracked by MRT.
                            // We can save a bit of memory by avoiding the redundancy where possible. For now, we are keeping it simple.

                            // Register type -> components mapping first so that we can use it during rollback below
                            if (typeEntry.GenericTypeEntry != null)
                            {
                                GenericTypeEntry registeredTypeEntry = _dynamicGenericTypes.AddOrGetExisting(typeEntry.GenericTypeEntry);
                                if (registeredTypeEntry != typeEntry.GenericTypeEntry && registeredTypeEntry._isRegisteredSuccessfully)
                                    throw new ArgumentException(SR.Argument_AddingDuplicate);

                                registeredTypeEntry._instantiatedTypeHandle = typeEntry.GenericTypeEntry._instantiatedTypeHandle;
                                registeredTypeEntry._isRegisteredSuccessfully = true;
                            }
                            else
                            {
                                MetadataType metadataType = typeEntry.MetadataDefinitionType;
                                IntPtr nonGcStaticFields = IntPtr.Zero;
                                IntPtr gcStaticFields = IntPtr.Zero;
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
#if SUPPORTS_R2R_LOADING
                                uint nonGcStaticsRva = 0;
                                uint gcStaticsRva = 0;

                                // For images where statics are directly embedded in the image, store the information about where
                                // to find statics info
                                if (TypeLoaderEnvironment.TryGetStaticsTableEntry(metadataType, out nonGcStaticsRva, out gcStaticsRva))
                                {
                                    ModuleInfo moduleInfo = TypeLoaderEnvironment.GetModuleInfoForType(metadataType);

                                    if (nonGcStaticsRva == 0)
                                        nonGcStaticFields = TypeLoaderEnvironment.NoStaticsData;
                                    else
                                        nonGcStaticFields = moduleInfo.Handle + checked((int)nonGcStaticsRva);

                                    if (gcStaticsRva == 0)
                                        gcStaticFields = TypeLoaderEnvironment.NoStaticsData;
                                    else
                                        gcStaticFields = moduleInfo.Handle + checked((int)gcStaticsRva);
                                }
#endif

                                TypeSystem.NativeFormat.NativeFormatType nativeFormatType = metadataType as TypeSystem.NativeFormat.NativeFormatType;
                                if (nativeFormatType != null)
                                {
                                    RegisterNewNamedTypeRuntimeTypeHandle(new QTypeDefinition(nativeFormatType.MetadataReader,
                                                                                nativeFormatType.Handle),
                                        nativeFormatType.GetTypeBuilderState().HalfBakedRuntimeTypeHandle,
                                        nonGcStaticFields,
                                        gcStaticFields);
                                }
#if ECMA_METADATA_SUPPORT
                                TypeSystem.Ecma.EcmaType ecmaFormatType = metadataType as TypeSystem.Ecma.EcmaType;
                                if (ecmaFormatType != null)
                                {
                                    RegisterNewNamedTypeRuntimeTypeHandle(new QTypeDefinition(ecmaFormatType.MetadataReader,
                                                                                ecmaFormatType.Handle),
                                        ecmaFormatType.GetTypeBuilderState().HalfBakedRuntimeTypeHandle,
                                        nonGcStaticFields,
                                        gcStaticFields);
                                }
#endif

                                nativeFormatTypesRegisteredCount++;
#else
                                Environment.FailFast("Ready to Run module type?");
#endif
                            }
                        }
                    }
                    Debug.Assert(registeredTypesCount == registrationData.TypesToRegisterCount);

                    if (registrationData.MethodsToRegister != null)
                    {
                        registeredMethods = new GenericMethodEntry[registrationData.MethodsToRegisterCount];

                        foreach (GenericMethodEntry methodEntry in registrationData.MethodsToRegister)
                        {
                            Debug.Assert(methodEntry._methodDictionary != IntPtr.Zero);

                            // Keep track of registered method dictionaries so that that we can rollback the registration on exception
                            registeredMethods[registeredMethodsCount++] = methodEntry;

                            // Register method dictionary -> components mapping first so that we can use it during rollback below
                            GenericMethodEntry registeredMethodComponentsEntry = _dynamicGenericMethodComponents.AddOrGetExisting(methodEntry);
                            if (registeredMethodComponentsEntry != methodEntry && registeredMethodComponentsEntry._isRegisteredSuccessfully)
                                throw new ArgumentException(SR.Argument_AddingDuplicate);

                            GenericMethodEntry registeredMethodEntry = _dynamicGenericMethods.AddOrGetExisting(methodEntry);
                            if (registeredMethodEntry != methodEntry && registeredMethodEntry._isRegisteredSuccessfully)
                                throw new ArgumentException(SR.Argument_AddingDuplicate);

                            Debug.Assert(registeredMethodComponentsEntry == registeredMethodEntry);
                            registeredMethodEntry._methodDictionary = methodEntry._methodDictionary;
                            registeredMethodEntry._isRegisteredSuccessfully = true;
                        }
                    }
                    Debug.Assert(registeredMethodsCount == registrationData.MethodsToRegisterCount);
                }
                catch
                {
                    // Catch and rethrow any exceptions instead of using finally block. Otherwise, filters that are run during
                    // the first pass of exception unwind may see partially registered types.

                    // TODO: Convert this to filter for better diagnostics once we switch to Roslyn

                    // Undo types that were registered. There should be no memory allocations or other failure points.
                    try
                    {
                        for (int i = 0; i < registeredTypesCount; i++)
                        {
                            var typeEntry = registeredTypes[i];
                            // There is no Remove feature in the LockFreeReaderHashtable...
                            if (typeEntry.GenericTypeEntry != null)
                            {
                                GenericTypeEntry failedEntry = _dynamicGenericTypes.GetValueIfExists(typeEntry.GenericTypeEntry);
                                if (failedEntry != null)
                                    failedEntry._isRegisteredSuccessfully = false;
                            }
                            else
                            {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
                                TypeSystem.NativeFormat.NativeFormatType nativeFormatType = typeEntry.MetadataDefinitionType as TypeSystem.NativeFormat.NativeFormatType;
                                if (nativeFormatType != null)
                                {
                                    UnregisterNewNamedTypeRuntimeTypeHandle(new QTypeDefinition(nativeFormatType.MetadataReader,
                                                                                nativeFormatType.Handle),
                                                                            nativeFormatType.GetTypeBuilderState().HalfBakedRuntimeTypeHandle);
                                }
#if ECMA_METADATA_SUPPORT
                                TypeSystem.Ecma.EcmaType ecmaFormatType = typeEntry.MetadataDefinitionType as TypeSystem.Ecma.EcmaType;
                                if (ecmaFormatType != null)
                                {
                                    UnregisterNewNamedTypeRuntimeTypeHandle(new QTypeDefinition(ecmaFormatType.MetadataReader,
                                                                                ecmaFormatType.Handle),
                                                                            ecmaFormatType.GetTypeBuilderState().HalfBakedRuntimeTypeHandle);
                                }
#endif
#else
                                Environment.FailFast("Ready to Run module type?");
#endif
                            }
                        }
                        for (int i = 0; i < registeredMethodsCount; i++)
                        {
                            // There is no Remove feature in the LockFreeReaderHashtable...
                            GenericMethodEntry failedEntry = _dynamicGenericMethods.GetValueIfExists(registeredMethods[i]);
                            if (failedEntry != null)
                                failedEntry._isRegisteredSuccessfully = false;

                            failedEntry = _dynamicGenericMethodComponents.GetValueIfExists(registeredMethods[i]);
                            if (failedEntry != null)
                                failedEntry._isRegisteredSuccessfully = false;
                        }
                    }
                    catch (Exception e)
                    {
                        // Catch any exceptions and fail fast just in case
                        Environment.FailFast("Exception during registration rollback", e);
                    }

                    throw;
                }

                if (nativeFormatTypesRegisteredCount > 0)
                    FinishAddingNewNamedTypes();
            }
        }

        public void RegisterConstructedLazyDictionaryForContext(IntPtr context, IntPtr signature, IntPtr dictionary)
        {
            Debug.Assert(_typeLoaderLock.IsAcquired);
            _lazyGenericDictionaries.Add(new LazyDictionaryContext { _context = context, _signature = signature }, dictionary);
        }
    }
}
