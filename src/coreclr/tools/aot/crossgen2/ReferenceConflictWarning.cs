// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ILCompiler
{
    /// <summary>
    /// Diagnoses conflicting <c>--reference</c> inputs. crossgen2 keeps only the first reference
    /// seen for a given assembly simple name; a later reference with the same simple name is dropped
    /// and the type system binds against the first one. When the dropped file is a genuinely different
    /// assembly (different module MVID or assembly version) this can produce R2R code that throws
    /// <see cref="MissingMethodException"/> or otherwise fails at runtime, so we warn.
    /// </summary>
    internal static class ReferenceConflictWarning
    {
        /// <summary>
        /// Callback compatible with <c>Helpers.BuildPathDictionary</c>'s duplicate notification:
        /// (simple name, kept path, dropped path). Warns when the dropped reference is a different
        /// assembly than the one crossgen2 will bind to. Best effort: never throws.
        /// </summary>
        public static void WarnIfConflictingVersions(string simpleName, string keptPath, string droppedPath)
        {
            // Cheap path equality skips the common case of the same file matched by overlapping
            // patterns; genuinely different files are compared by assembly identity below.
            if (string.Equals(keptPath, droppedPath, StringComparison.Ordinal))
                return;

            if (!TryGetAssemblyIdentity(keptPath, out Version keptVersion, out Guid keptMvid)
                || !TryGetAssemblyIdentity(droppedPath, out Version droppedVersion, out Guid droppedMvid))
            {
                // If either file cannot be read as an assembly, stay silent and let the real load
                // path report any genuine error.
                return;
            }

            // Identical copies of one assembly share an MVID and version; only warn when the dropped
            // file is genuinely a different assembly.
            if (keptMvid != droppedMvid || keptVersion != droppedVersion)
            {
                Console.WriteLine(string.Format(SR.WarningConflictingReferenceVersions,
                    simpleName, keptPath, keptVersion, droppedPath, droppedVersion));
            }
        }

        private static bool TryGetAssemblyIdentity(string filePath, out Version version, out Guid mvid)
        {
            version = null;
            mvid = Guid.Empty;

            try
            {
                using FileStream stream = File.OpenRead(filePath);
                using PEReader peReader = new PEReader(stream);
                if (!peReader.HasMetadata)
                    return false;

                MetadataReader reader = peReader.GetMetadataReader();
                if (!reader.IsAssembly)
                    return false;

                version = reader.GetAssemblyDefinition().Version;
                mvid = reader.GetGuid(reader.GetModuleDefinition().Mvid);
                return true;
            }
            catch
            {
                // Best effort: a malformed or inaccessible reference must never fail the compilation here.
                return false;
            }
        }
    }
}
