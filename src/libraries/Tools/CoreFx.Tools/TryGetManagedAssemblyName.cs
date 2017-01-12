using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.DotNet.Build.Tasks
{ 
    public partial class GenerateDepsJson
    {
        public bool TryGetManagedAssemblyName(string file, out AssemblyName result)
        {
            result = null;
            try
            {
                using (var peReader = new PEReader(File.OpenRead(file)))
                {
                        if (peReader.HasMetadata)
                        {
                            MetadataReader reader = peReader.GetMetadataReader();
                            if (reader.IsAssembly)
                            {
                                AssemblyDefinition assemblyDef = reader.GetAssemblyDefinition();

                                result = new AssemblyName();
                                result.Name = reader.GetString(assemblyDef.Name);
                                result.CultureName = reader.GetString(assemblyDef.Culture);
                                result.Version = assemblyDef.Version;

                                if (!assemblyDef.PublicKey.IsNil)
                                {
                                    result.SetPublicKey(reader.GetBlobBytes(assemblyDef.PublicKey));
                                }
                            }
                        }
                        else
                        {
                            //Native
                            return false;
                        }
                    }
            }
            catch (BadImageFormatException)
            {
                // not a PE
                return false;
            }
            return true;
        }
    }
}
