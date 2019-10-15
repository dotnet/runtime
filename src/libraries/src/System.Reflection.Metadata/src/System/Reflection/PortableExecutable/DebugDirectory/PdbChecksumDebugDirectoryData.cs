// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace System.Reflection.PortableExecutable
{
    public readonly struct PdbChecksumDebugDirectoryData
    {
        /// <summary>
        /// Checksum algorithm name.
        /// </summary>
        public string AlgorithmName { get; }

        /// <summary>
        /// GUID (Globally Unique Identifier) of the associated PDB.
        /// </summary>
        public ImmutableArray<byte> Checksum { get; }

        internal PdbChecksumDebugDirectoryData(string algorithmName, ImmutableArray<byte> checksum)
        {
            Debug.Assert(!string.IsNullOrEmpty(algorithmName));
            Debug.Assert(!checksum.IsDefaultOrEmpty);

            AlgorithmName = algorithmName;
            Checksum = checksum;
        }
    }
}
