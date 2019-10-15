// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata
{
    internal static class PortablePdbVersions
    {
        /// <summary>
        /// Version of Portable PDB format emitted by the writer by default. Metadata version string.
        /// </summary>
        internal const string DefaultMetadataVersion = "PDB v1.0";

        /// <summary>
        /// Version of Portable PDB format emitted by the writer by default.
        /// </summary>
        internal const ushort DefaultFormatVersion = 0x0100;

        /// <summary>
        /// Minimal supported version of Portable PDB format.
        /// </summary>
        internal const ushort MinFormatVersion = 0x0100;

        /// <summary>
        /// Minimal supported version of Embedded Portable PDB blob.
        /// </summary>
        internal const ushort MinEmbeddedVersion = 0x0100;

        /// <summary>
        /// Version of Embedded Portable PDB blob format emitted by the writer by default.
        /// </summary>
        internal const ushort DefaultEmbeddedVersion = 0x0100;

        /// <summary>
        /// Minimal version of the Embedded Portable PDB blob that the current reader can't interpret.
        /// </summary>
        internal const ushort MinUnsupportedEmbeddedVersion = 0x0200;

        internal const uint DebugDirectoryEmbeddedSignature = 0x4244504d;

        internal const ushort PortableCodeViewVersionMagic = 0x504d;
        internal static uint DebugDirectoryEntryVersion(ushort portablePdbVersion) => PortableCodeViewVersionMagic << 16 | (uint)portablePdbVersion;
        internal static uint DebugDirectoryEmbeddedVersion(ushort portablePdbVersion) => (uint)DefaultEmbeddedVersion << 16 | portablePdbVersion;
        internal static string Format(ushort version) => (version >> 8) + "." + (version & 0xff);
    }
}
