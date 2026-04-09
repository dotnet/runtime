// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Metadata
{
    public readonly partial struct AssemblyReference
    {
        /// <summary>
        /// Creates an <see cref="AssemblyName"/> instance corresponding to this assembly reference.
        /// </summary>
        public AssemblyName GetAssemblyName()
        {
            return _reader.GetAssemblyName(Name, Version, Culture, PublicKeyOrToken, AssemblyHashAlgorithm.None, Flags);
        }

        /// <summary>
        /// Creates an <see cref="AssemblyNameInfo"/> instance corresponding to this assembly reference.
        /// </summary>
        public AssemblyNameInfo GetAssemblyNameInfo()
        {
            return _reader.GetAssemblyNameInfo(Name, Version, Culture, PublicKeyOrToken, Flags);
        }
    }
}
