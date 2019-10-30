// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.PortableExecutable
{
    public readonly struct CodeViewDebugDirectoryData
    {
        /// <summary>
        /// GUID (Globally Unique Identifier) of the associated PDB.
        /// </summary>
        public Guid Guid { get; }

        /// <summary>
        /// Iteration of the PDB. The first iteration is 1. The iteration is incremented each time the PDB content is augmented.
        /// </summary>
        public int Age { get; }

        /// <summary>
        /// Path to the .pdb file containing debug information for the PE/COFF file.
        /// </summary>
        public string Path { get; }

        internal CodeViewDebugDirectoryData(Guid guid, int age, string path)
        {
            Debug.Assert(path != null);

            Path = path;
            Guid = guid;
            Age = age;
        }
    }
}
