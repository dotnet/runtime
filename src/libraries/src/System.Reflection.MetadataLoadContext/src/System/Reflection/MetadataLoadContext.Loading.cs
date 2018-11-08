// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection.TypeLoading;
using System.Reflection.TypeLoading.Ecma;

namespace System.Reflection
{
    public sealed partial class MetadataLoadContext
    {
        // This maintains the canonical list of Assembly instances for a given def name. Each defname can only appear
        // once in the list and its appearance prevents further assemblies with the same identity from loading unless the Mvids's match.
        // Null entries do *not* appear here.
        private readonly ConcurrentDictionary<RoAssemblyName, RoAssembly> _loadedAssemblies = new ConcurrentDictionary<RoAssemblyName, RoAssembly>();

        private RoAssembly LoadFromStreamCore(Stream peStream)
        {
            PEReader peReader = new PEReader(peStream);
            PEReader peReaderToDispose = peReader; // Ensure peReader is disposed immediately if we throw an exception before we're done.
            try
            {
                if (!peReader.HasMetadata)
                    throw new BadImageFormatException(SR.NoMetadataInPeImage);

                string location = (peStream is FileStream fs) ? (fs.Name ?? string.Empty) : string.Empty;
                MetadataReader reader  = peReader.GetMetadataReader();
                RoAssembly candidate = new EcmaAssembly(this, peReader, reader, location);
                AssemblyNameData defNameData = candidate.GetAssemblyNameDataNoCopy();
                byte[] pkt = defNameData.PublicKeyToken ?? Array.Empty<byte>();
                if (pkt.Length == 0 && defNameData.PublicKey != null && defNameData.PublicKey.Length != 0)
                {
                    pkt = defNameData.PublicKey.ComputePublicKeyToken();
                }
                RoAssemblyName defName = new RoAssemblyName(defNameData.Name, defNameData.Version, defNameData.CultureName, pkt);

                RoAssembly winner = _loadedAssemblies.GetOrAdd(defName, candidate);
                if (winner == candidate)
                {
                    // We won the race.
                    RegisterForDisposal(peReader);
                    peReaderToDispose = null;
                    return _binds.GetOrAdd(defName, winner);

                    // What if we lost the race to bind the defName in the _binds list? Should we ignore it and return the newly created assembly
                    // (like Assembly.LoadModule()) does or return the prior assembly (like we do if we lose the race to commit into _loadedAssemblies?)
                    // There's no perfect answer here. Fundamentally, the dilemma comes about because our apis don't lets apps properly separate 
                    // the act of creating an Assembly object from the act of committing the MetadataLoadContext to bind to it.
                    //
                    // We will choose to return the prior winner so that the api is consistent with itself. This is how other LoadFrom() 
                    // apis work and they're used a lot more than LoadModule().
                }
                else
                {
                    // We lost the race but check for a Mvid mismatch.
                    if (candidate.ManifestModule.ModuleVersionId != winner.ManifestModule.ModuleVersionId)
                        throw new FileLoadException(SR.Format(SR.FileLoadDuplicateAssemblies, defName));
                }

                return winner;
            }
            finally
            {
                peReaderToDispose?.Dispose();
            }
        }
    }
}
