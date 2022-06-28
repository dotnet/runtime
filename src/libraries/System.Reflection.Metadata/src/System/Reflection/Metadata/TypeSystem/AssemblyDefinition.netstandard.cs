// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Metadata
{
    public readonly partial struct AssemblyDefinition
    {
        public AssemblyName GetAssemblyName()
        {
            AssemblyFlags flags = Flags;

            // compat: assembly names from metadata definitions should set the bit for the full key.
            if (!PublicKey.IsNil)
            {
                flags |= AssemblyFlags.PublicKey;
            }

            return _reader.GetAssemblyName(Name, Version, Culture, PublicKey, HashAlgorithm, flags);
        }
    }
}
