// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Diagnostics;

using Internal.NativeFormat;
using Internal.TypeSystem;

namespace Internal.Runtime.TypeLoader
{
    internal static class TemplateLocator
    {
        private const uint BadTokenFixupValue = 0xFFFFFFFF;

        //
        // Returns the template type handle for a generic instantiation type
        //
        public static TypeDesc TryGetTypeTemplate(TypeDesc concreteType, ref NativeLayoutInfo nativeLayoutInfo)
        {
            nativeLayoutInfo.Module = null;
            nativeLayoutInfo.Offset = 0;
            var canonForm = concreteType.ConvertToCanonForm(CanonicalFormKind.Specific);
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

                    if (canonForm == candidateTemplate.ConvertToCanonForm(CanonicalFormKind.Specific))
                    {
                        TypeLoaderLogger.WriteLine("Found template for type " + concreteType.ToString() + ": " + candidateTemplate.ToString());
                        nativeLayoutInfo.Offset = entryParser.GetUnsigned();
                        if (nativeLayoutInfo.Offset == BadTokenFixupValue)
                        {
                            // TODO: once multifile gets fixed up, make this throw a BadImageFormatException
                            TypeLoaderLogger.WriteLine("ERROR: template not fixed up, skipping");
                            continue;
                        }

                        nativeLayoutInfo.Module = moduleInfo;
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
            nativeLayoutInfoModule = null;
            nativeLayoutInfoToken = 0;
            var canonForm = concreteMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);
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

                    if (canonForm == candidateTemplate.GetCanonMethodTarget(CanonicalFormKind.Specific))
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
