// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Internal.Runtime;
using Internal.Runtime.Augments;

using Internal.NativeFormat;
using Internal.TypeSystem;

namespace Internal.Runtime.TypeLoader
{
    internal struct TemplateLocator
    {
        private const uint BadTokenFixupValue = 0xFFFFFFFF;

        //
        // Returns the template type handle for a generic instantiation type
        //
        public static TypeDesc TryGetTypeTemplate(TypeDesc concreteType, ref NativeLayoutInfo nativeLayoutInfo)
        {
#if GENERICS_FORCE_USG
            return TryGetUniversalTypeTemplate(concreteType, ref nativeLayoutInfo);
#else
            // First, see if there is a specific canonical template
            TypeDesc result = TryGetTypeTemplate_Internal(concreteType, CanonicalFormKind.Specific, out nativeLayoutInfo.Module, out nativeLayoutInfo.Offset);

            // If not found, see if there's a universal canonical template
            result ??= TryGetUniversalTypeTemplate(concreteType, ref nativeLayoutInfo);

            return result;
#endif
        }

        public static TypeDesc TryGetUniversalTypeTemplate(TypeDesc concreteType, ref NativeLayoutInfo nativeLayoutInfo)
        {
            return TryGetTypeTemplate_Internal(concreteType, CanonicalFormKind.Universal, out nativeLayoutInfo.Module, out nativeLayoutInfo.Offset);
        }

#if GENERICS_FORCE_USG
        public TypeDesc TryGetNonUniversalTypeTemplate(TypeDesc concreteType, ref NativeLayoutInfo nativeLayoutInfo)
        {
            return TryGetTypeTemplate_Internal(concreteType, CanonicalFormKind.Specific, out nativeLayoutInfo.Module, out nativeLayoutInfo.Offset);
        }
#endif

        /// <summary>
        /// Get the NativeLayout for a type from a ReadyToRun image.
        /// </summary>
        public static bool TryGetMetadataNativeLayout(TypeDesc concreteType, out NativeFormatModuleInfo nativeLayoutInfoModule, out uint nativeLayoutInfoToken)
        {
            nativeLayoutInfoModule = null;
            nativeLayoutInfoToken = 0;
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            var nativeMetadataType = concreteType.GetTypeDefinition() as TypeSystem.NativeFormat.NativeFormatType;
            if (nativeMetadataType == null)
                return false;

            var canonForm = concreteType.ConvertToCanonForm(CanonicalFormKind.Specific);
            var hashCode = canonForm.GetHashCode();

#if SUPPORTS_R2R_LOADING
            foreach (var moduleInfo in ModuleList.EnumerateModules())
            {
                if (moduleInfo.MetadataReader == null)
                    continue;

                ExternalReferencesTable externalFixupsTable;
                NativeHashtable typeTemplatesHashtable = LoadHashtable(moduleInfo.Handle, ReflectionMapBlob.MetadataBasedTypeTemplateMap, out externalFixupsTable);

                if (typeTemplatesHashtable.IsNull)
                    continue;

                var enumerator = typeTemplatesHashtable.Lookup(hashCode);
                var nativeMetadataUnit = nativeMetadataType.Context.ResolveMetadataUnit(moduleInfo);

                NativeParser entryParser;
                while (!(entryParser = enumerator.GetNext()).IsNull)
                {
                    var entryTypeHandle = entryParser.GetUnsigned().AsHandle();
                    TypeDesc typeDesc = nativeMetadataUnit.GetType(entryTypeHandle);
                    Debug.Assert(typeDesc != null);
                    if (typeDesc == canonForm)
                    {
                        TypeLoaderLogger.WriteLine("Found metadata template for type " + concreteType.ToString() + ": " + typeDesc.ToString());
                        nativeLayoutInfoToken = entryParser.GetUnsigned();
                        if (nativeLayoutInfoToken == BadTokenFixupValue)
                        {
                            throw new BadImageFormatException();
                        }

                        nativeLayoutInfoModule = moduleHandle;
                        return true;
                    }
                }
            }
#endif
#endif

            return false;
        }

        /// <summary>
        /// Get the NativeLayout for a method from a ReadyToRun image.
        /// </summary>
        public static bool TryGetMetadataNativeLayout(MethodDesc concreteMethod, out NativeFormatModuleInfo nativeLayoutInfoModule, out uint nativeLayoutInfoToken)
        {
            nativeLayoutInfoModule = null;
            nativeLayoutInfoToken = 0;
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            var nativeMetadataType = concreteMethod.GetTypicalMethodDefinition() as TypeSystem.NativeFormat.NativeFormatMethod;
            if (nativeMetadataType == null)
                return false;

            var canonForm = concreteMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);
            var hashCode = canonForm.GetHashCode();

#if SUPPORTS_R2R_LOADING
            foreach (var moduleInfo in ModuleList.EnumerateModules())
            {
                if (moduleInfo.MetadataReader == null)
                    continue;

                ExternalReferencesTable externalFixupsTable;
                NativeHashtable methodTemplatesHashtable = LoadHashtable(moduleInfo.Handle, ReflectionMapBlob.MetadataBasedGenericMethodsTemplateMap, out externalFixupsTable);

                if (methodTemplatesHashtable.IsNull)
                    continue;

                var enumerator = methodTemplatesHashtable.Lookup(hashCode);
                var nativeMetadataUnit = nativeMetadataType.Context.ResolveMetadataUnit(moduleInfo);

                NativeParser entryParser;
                while (!(entryParser = enumerator.GetNext()).IsNull)
                {
                    var entryTypeHandle = entryParser.GetUnsigned().AsHandle();
                    MethodDesc methodDesc = nativeMetadataUnit.GetMethod(entryTypeHandle, null);
                    Debug.Assert(methodDesc != null);
                    if (methodDesc == canonForm)
                    {
                        TypeLoaderLogger.WriteLine("Found metadata template for method " + concreteMethod.ToString() + ": " + methodDesc.ToString());
                        nativeLayoutInfoToken = entryParser.GetUnsigned();
                        if (nativeLayoutInfoToken == BadTokenFixupValue)
                        {
                            throw new BadImageFormatException();
                        }

                        nativeLayoutInfoModule = moduleInfo;
                        return true;
                    }
                }
            }
#endif
#endif
            return false;
        }

        private static TypeDesc TryGetTypeTemplate_Internal(TypeDesc concreteType, CanonicalFormKind kind, out NativeFormatModuleInfo nativeLayoutInfoModule, out uint nativeLayoutInfoToken)
        {
            nativeLayoutInfoModule = null;
            nativeLayoutInfoToken = 0;
            var canonForm = concreteType.ConvertToCanonForm(kind);
            var hashCode = canonForm.GetHashCode();

            foreach (NativeFormatModuleInfo moduleInfo in ModuleList.EnumerateModules())
            {
                ExternalReferencesTable externalFixupsTable;
                NativeHashtable typeTemplatesHashtable = LoadHashtable(moduleInfo, ReflectionMapBlob.TypeTemplateMap, out externalFixupsTable);

                if (typeTemplatesHashtable.IsNull)
                    continue;

                var enumerator = typeTemplatesHashtable.Lookup(hashCode);

                NativeParser entryParser;
                while (!(entryParser = enumerator.GetNext()).IsNull)
                {
                    RuntimeTypeHandle candidateTemplateTypeHandle = externalFixupsTable.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                    TypeDesc candidateTemplate = concreteType.Context.ResolveRuntimeTypeHandle(candidateTemplateTypeHandle);

                    if (canonForm == candidateTemplate.ConvertToCanonForm(kind))
                    {
                        TypeLoaderLogger.WriteLine("Found template for type " + concreteType.ToString() + ": " + candidateTemplate.ToString());
                        nativeLayoutInfoToken = entryParser.GetUnsigned();
                        if (nativeLayoutInfoToken == BadTokenFixupValue)
                        {
                            // TODO: once multifile gets fixed up, make this throw a BadImageFormatException
                            TypeLoaderLogger.WriteLine("ERROR: template not fixed up, skipping");
                            continue;
                        }

                        Debug.Assert(
                            (kind != CanonicalFormKind.Universal) ||
                            (kind == CanonicalFormKind.Universal && candidateTemplate == candidateTemplate.ConvertToCanonForm(kind)));

                        nativeLayoutInfoModule = moduleInfo;
                        return candidateTemplate;
                    }
                }
            }

            TypeLoaderLogger.WriteLine("ERROR: Cannot find a suitable template for type " + concreteType.ToString());
            return null;
        }

        //
        // Returns the template method for a generic method instantiation
        //
        public static InstantiatedMethod TryGetGenericMethodTemplate(InstantiatedMethod concreteMethod, out NativeFormatModuleInfo nativeLayoutInfoModule, out uint nativeLayoutInfoToken)
        {
            // First, see if there is a specific canonical template
            InstantiatedMethod result = TryGetGenericMethodTemplate_Internal(concreteMethod, CanonicalFormKind.Specific, out nativeLayoutInfoModule, out nativeLayoutInfoToken);

            // If not found, see if there's a universal canonical template
            result ??= TryGetGenericMethodTemplate_Internal(concreteMethod, CanonicalFormKind.Universal, out nativeLayoutInfoModule, out nativeLayoutInfoToken);

            return result;
        }
        private static InstantiatedMethod TryGetGenericMethodTemplate_Internal(InstantiatedMethod concreteMethod, CanonicalFormKind kind, out NativeFormatModuleInfo nativeLayoutInfoModule, out uint nativeLayoutInfoToken)
        {
            nativeLayoutInfoModule = null;
            nativeLayoutInfoToken = 0;
            var canonForm = concreteMethod.GetCanonMethodTarget(kind);
            var hashCode = canonForm.GetHashCode();

            foreach (NativeFormatModuleInfo moduleInfo in ModuleList.EnumerateModules())
            {
                NativeReader nativeLayoutReader = TypeLoaderEnvironment.GetNativeLayoutInfoReader(moduleInfo.Handle);
                if (nativeLayoutReader == null)
                    continue;

                NativeHashtable genericMethodTemplatesHashtable = LoadHashtable(moduleInfo, ReflectionMapBlob.GenericMethodsTemplateMap, out _);

                if (genericMethodTemplatesHashtable.IsNull)
                    continue;

                var context = new NativeLayoutInfoLoadContext
                {
                    _typeSystemContext = concreteMethod.Context,
                    _typeArgumentHandles = concreteMethod.OwningType.Instantiation,
                    _methodArgumentHandles = concreteMethod.Instantiation,
                    _module = moduleInfo
                };

                var enumerator = genericMethodTemplatesHashtable.Lookup(hashCode);

                NativeParser entryParser;
                while (!(entryParser = enumerator.GetNext()).IsNull)
                {
                    var methodSignatureParser = new NativeParser(nativeLayoutReader, entryParser.GetUnsigned());

                    // Get the unified generic method holder and convert it to its canonical form
                    var candidateTemplate = (InstantiatedMethod)context.GetMethod(ref methodSignatureParser);
                    Debug.Assert(candidateTemplate.Instantiation.Length > 0);

                    if (canonForm == candidateTemplate.GetCanonMethodTarget(kind))
                    {
                        TypeLoaderLogger.WriteLine("Found template for generic method " + concreteMethod.ToString() + ": " + candidateTemplate.ToString());
                        nativeLayoutInfoModule = moduleInfo;
                        nativeLayoutInfoToken = entryParser.GetUnsigned();
                        if (nativeLayoutInfoToken == BadTokenFixupValue)
                        {
                            // TODO: once multifile gets fixed up, make this throw a BadImageFormatException
                            TypeLoaderLogger.WriteLine("ERROR: template not fixed up, skipping");
                            continue;
                        }

                        Debug.Assert(
                            (kind != CanonicalFormKind.Universal) ||
                            (kind == CanonicalFormKind.Universal && candidateTemplate == candidateTemplate.GetCanonMethodTarget(kind)));

                        return candidateTemplate;
                    }
                }
            }

            TypeLoaderLogger.WriteLine("ERROR: Cannot find a suitable template for generic method " + concreteMethod.ToString());
            return null;
        }

        // Lazy loadings of hashtables (load on-demand only)
        private static unsafe NativeHashtable LoadHashtable(NativeFormatModuleInfo module, ReflectionMapBlob hashtableBlobId, out ExternalReferencesTable externalFixupsTable)
        {
            // Load the common fixups table
            externalFixupsTable = default(ExternalReferencesTable);
            if (!externalFixupsTable.InitializeCommonFixupsTable(module))
                return default(NativeHashtable);

            // Load the hashtable
            byte* pBlob;
            uint cbBlob;
            if (!module.TryFindBlob(hashtableBlobId, out pBlob, out cbBlob))
                return default(NativeHashtable);

            NativeReader reader = new NativeReader(pBlob, cbBlob);
            NativeParser parser = new NativeParser(reader, 0);
            return new NativeHashtable(parser);
        }
    }
}
