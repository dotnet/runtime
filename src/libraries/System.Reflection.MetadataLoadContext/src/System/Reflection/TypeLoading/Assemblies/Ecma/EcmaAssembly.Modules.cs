// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace System.Reflection.TypeLoading.Ecma
{
    /// <summary>
    /// Base class for all Assembly objects created by a MetadataLoadContext and get its metadata from a PEReader.
    /// </summary>
    internal sealed partial class EcmaAssembly
    {
        public sealed override event ModuleResolveEventHandler ModuleResolve;

        protected sealed override RoModule LoadModule(string moduleName, bool containsMetadata)
        {
            FileStream peStream = FindModuleNextToAssembly(moduleName);
            if (peStream != null)
                return CreateModule(peStream, containsMetadata);

            Module moduleFromEvent = ModuleResolve?.Invoke(this, new ResolveEventArgs(moduleName));
            if (moduleFromEvent != null)
            {
                if (!(moduleFromEvent is RoModule roModuleFromEvent && roModuleFromEvent.Loader == Loader))
                    throw new FileLoadException(SR.ModuleResolveEventReturnedExternalModule);
                return roModuleFromEvent;
            }

            throw new FileNotFoundException(SR.Format(SR.FileNotFoundModule, moduleName));
        }

        private FileStream FindModuleNextToAssembly(string moduleName)
        {
            Assembly containingAssembly = this;
            string location = containingAssembly.Location;
            if (location == null || location.Length == 0)
                return null;
            string directoryPath = Path.GetDirectoryName(location);
            string modulePath = Path.Combine(directoryPath, moduleName);
            if (File.Exists(modulePath))
                return File.OpenRead(modulePath);

            return null;
        }

        protected sealed override RoModule CreateModule(Stream peStream, bool containsMetadata)
        {
            string location = RoModule.FullyQualifiedNameForModulesLoadedFromByteArrays;
            if (peStream is FileStream fs)
            {
                location = fs.Name;
            }

            if (!containsMetadata)
            {
                peStream.Close();
                return new RoResourceModule(this, location);
            }

            PEReader peReader = new PEReader(peStream);
            Loader.RegisterForDisposal(peReader);
            return new EcmaModule(this, location, peReader, peReader.GetMetadataReader());
        }

        protected sealed override IEnumerable<AssemblyFileInfo> GetAssemblyFileInfosFromManifest(bool includeManifestModule, bool includeResourceModules)
        {
            MetadataReader reader = Reader;
            if (includeManifestModule)
            {
                yield return new AssemblyFileInfo(reader.GetModuleDefinition().Name.GetString(reader), true, 0);
            }

            foreach (AssemblyFileHandle h in reader.AssemblyFiles)
            {
                AssemblyFile af = h.GetAssemblyFile(reader);
                if (includeResourceModules || af.ContainsMetadata)
                {
                    yield return new AssemblyFileInfo(af.Name.GetString(reader), af.ContainsMetadata, h.GetToken().GetTokenRowNumber());
                }
            }
        }
    }
}
