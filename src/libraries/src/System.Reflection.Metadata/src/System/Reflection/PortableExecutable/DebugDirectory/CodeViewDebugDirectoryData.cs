// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Reflection.PortableExecutable
{
    public struct CodeViewDebugDirectoryData
    {
        /// <summary>
        /// GUID (Globally Unique Identifier) of the associated PDB.
        /// </summary>
        public Guid Guid { get; private set; }

        /// <summary>
        /// Iteration of the PDB. The first iteration is 1. The iteration is incremented each time the PDB content is augmented.
        /// </summary>
        public int Age { get; private set; }

        /// <summary>
        /// Path to the .pdb file containing debug information for the PE/COFF file.
        /// </summary>
        public string Path { get; private set; }

        internal CodeViewDebugDirectoryData(Guid guid, int age, string path)
        {
            Debug.Assert(path != null);

            Path = path;
            Guid = guid;
            Age = age;
        }
    }
}
