// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata;

namespace System.Reflection.TypeLoading.Ecma
{
    /// <summary>
    /// Base class for all Assembly objects created by a MetadataLoadContext and get its metadata from a PEReader.
    /// </summary>
    internal sealed partial class EcmaAssembly
    {
        protected sealed override AssemblyNameData ComputeNameData()
        {
            MetadataReader reader = Reader;
            AssemblyDefinition ad = AssemblyDefinition;
            AssemblyNameData data = new AssemblyNameData
            {
                Name = ad.Name.GetString(reader),
                Version = ad.Version,
                CultureName = ad.Culture.GetStringOrNull(reader) ?? string.Empty
            };
            byte[] pk = ad.PublicKey.GetBlobBytes(reader);
            data.PublicKey = pk;
            if (pk.Length != 0)
            {
                // AssemblyName will automatically compute the PKT on demand but given that we're doing all this work and caching it, we might
                // as well do this now.
                data.PublicKeyToken = pk.ComputePublicKeyToken();
            }

            AssemblyNameFlags anFlagsAndContentType = ad.Flags.ToAssemblyNameFlags() | AssemblyNameFlags.PublicKey;
            data.Flags = anFlagsAndContentType.ExtractAssemblyNameFlags();

            // We've finished setting the AssemblyName properties that actually pertain to binding and the Ecma-355
            // concept of an assembly name.
            //
            // The rest of the properties are properties historically set by the runtime Reflection and thumbtacked
            // onto the AssemblyName object in the CLR tradition of treating AssemblyName as a dumping ground for all
            // kinds of info. Nevertheless, some of this info is potentially useful and not exposed elsewhere on Assembly
            // so we'll be nice and set it.
            data.HashAlgorithm = ad.HashAlgorithm.ToConfigurationAssemblyHashAlgorithm();
            data.ContentType = anFlagsAndContentType.ExtractAssemblyContentType();

            ManifestModule.GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine);
            switch (machine)
            {
                case ImageFileMachine.AMD64:
                    data.ProcessorArchitecture = ProcessorArchitecture.Amd64;
                    break;

                case ImageFileMachine.ARM:
                    data.ProcessorArchitecture = ProcessorArchitecture.Arm;
                    break;

                case ImageFileMachine.IA64:
                    data.ProcessorArchitecture = ProcessorArchitecture.IA64;
                    break;

                case ImageFileMachine.I386:
                    if ((peKind & PortableExecutableKinds.Required32Bit) != 0)
                        data.ProcessorArchitecture = ProcessorArchitecture.X86;
                    else
                        data.ProcessorArchitecture = ProcessorArchitecture.MSIL;
                    break;

                default:
                    // No real precedent for what to do here - CLR will never get as far as giving you an Assembly object
                    // if the PE file specifies an unsupported machine. Since this Reflection implementation is a metadata inspection
                    // layer, and the library will never make any decisions based on this value, throwing isn't really the best response here.
                    data.ProcessorArchitecture = ProcessorArchitecture.None;  
                    break;
            }
            return data;
        }
    }
}
