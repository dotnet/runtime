// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace System.Reflection.TypeLoading.Ecma
{
    /// <summary>
    /// Base class for all Assembly objects created by a MetadataLoadContext and get its metadata from a PEReader.
    /// </summary>
    internal sealed partial class EcmaAssembly : RoAssembly
    {
        private readonly string _location;
        private readonly EcmaModule _manifestModule;

        internal EcmaAssembly(MetadataLoadContext loader, PEReader peReader, MetadataReader reader, string location)
            : base(loader, reader.AssemblyFiles.Count)
        {
            Debug.Assert(loader != null);
            Debug.Assert(peReader != null);
            Debug.Assert(reader != null);
            Debug.Assert(location != null);

            _location = location;
            _neverAccessThisExceptThroughAssemblyDefinitionProperty = reader.GetAssemblyDefinition();

            _manifestModule = new EcmaModule(this, location, peReader, reader);
        }

        internal sealed override RoModule GetRoManifestModule() => _manifestModule;
        internal EcmaModule GetEcmaManifestModule() => _manifestModule;

        public sealed override MethodInfo EntryPoint => GetEcmaManifestModule().ComputeEntryPoint(fileRefEntryPointAllowed: true);

        public sealed override string ImageRuntimeVersion => Reader.MetadataVersion;
        public sealed override bool IsDynamic => false;
        public sealed override string Location => _location;

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes => AssemblyDefinition.GetCustomAttributes().ToTrueCustomAttributes(GetEcmaManifestModule());

        protected sealed override AssemblyNameData[] ComputeAssemblyReferences()
        {
            MetadataReader reader = Reader;
            AssemblyNameData[] assemblyReferences = new AssemblyNameData[reader.AssemblyReferences.Count];
            int index = 0;
            foreach (AssemblyReferenceHandle handle in reader.AssemblyReferences)
            {
                AssemblyReference ar = handle.GetAssemblyReference(reader);
                AssemblyNameData data = new AssemblyNameData();

                AssemblyNameFlags flags = ar.Flags.ToAssemblyNameFlags();
                data.Flags = flags;
                data.Name = ar.Name.GetString(reader);
                data.Version = ar.Version.AdjustForUnspecifiedVersionComponents();
                data.CultureName = ar.Culture.GetStringOrNull(reader) ?? string.Empty;
                if ((flags & AssemblyNameFlags.PublicKey) != 0)
                {
                    byte[] pk = ar.PublicKeyOrToken.GetBlobBytes(reader);
                    data.PublicKey = pk;
                    if (pk.Length != 0)
                    {
                        // AssemblyName will automatically compute the PKT on demand but given that we're doing all this work and caching it, we might
                        // as well do this now.
                        data.PublicKeyToken = pk.ComputePublicKeyToken();
                    }
                }
                else
                {
                    data.PublicKeyToken = ar.PublicKeyOrToken.GetBlobBytes(reader);
                }

                assemblyReferences[index++] = data;
            }

            return assemblyReferences;
        }

        protected sealed override void IterateTypeForwards(TypeForwardHandler handler)
        {
            MetadataReader reader = Reader;
            foreach (ExportedTypeHandle exportedTypeHandle in reader.ExportedTypes)
            {
                ExportedType exportedType = reader.GetExportedType(exportedTypeHandle);
                if (!exportedType.IsForwarder)
                    continue;

                EntityHandle implementation = exportedType.Implementation;
                if (implementation.Kind != HandleKind.AssemblyReference) // This check also weeds out nested types. This is intentional.
                    continue;

                RoAssembly redirectedAssembly = ((AssemblyReferenceHandle)implementation).ResolveToAssemblyOrExceptionAssembly(GetEcmaManifestModule());
                ReadOnlySpan<byte> ns = exportedType.Namespace.AsReadOnlySpan(reader);
                ReadOnlySpan<byte> name = exportedType.Name.AsReadOnlySpan(reader);
                handler(redirectedAssembly, ns, name);
            }
        }

        internal MetadataReader Reader => _manifestModule.Reader;

        private ref readonly AssemblyDefinition AssemblyDefinition { get { Loader.DisposeCheck(); return ref _neverAccessThisExceptThroughAssemblyDefinitionProperty; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]  // Block from debugger watch windows so they don't AV the debugged process.
        private readonly AssemblyDefinition _neverAccessThisExceptThroughAssemblyDefinitionProperty;
    }
}
