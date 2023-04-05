// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection.PortableExecutable;
using Microsoft.Win32.SafeHandles;

namespace System.Reflection.Metadata
{
    public sealed partial class MetadataReader
    {
        internal AssemblyName GetAssemblyName(StringHandle nameHandle, Version version, StringHandle cultureHandle, BlobHandle publicKeyOrTokenHandle, AssemblyHashAlgorithm assemblyHashAlgorithm, AssemblyFlags flags)
        {
            string name = GetString(nameHandle);
            // compat: normalize Nil culture name to "" to match original behavior of AssemblyName.GetAssemblyName()
            string cultureName = (!cultureHandle.IsNil) ? GetString(cultureHandle) : "";
            var hashAlgorithm = (Configuration.Assemblies.AssemblyHashAlgorithm)assemblyHashAlgorithm;
            // compat: original native implementation used to guarantee that publicKeyOrToken is never null in this scenario.
            byte[]? publicKeyOrToken = !publicKeyOrTokenHandle.IsNil ? GetBlobBytes(publicKeyOrTokenHandle) : Array.Empty<byte>();

            var assemblyName = new AssemblyName()
            {
                Name = name,
                Version = version,
                CultureName = cultureName,
#pragma warning disable SYSLIB0037 // AssemblyName.HashAlgorithm is obsolete
                HashAlgorithm = hashAlgorithm,
#pragma warning restore
                Flags = GetAssemblyNameFlags(flags),
                ContentType = GetContentTypeFromAssemblyFlags(flags)
            };

            bool hasPublicKey = (flags & AssemblyFlags.PublicKey) != 0;
            if (hasPublicKey)
            {
                assemblyName.SetPublicKey(publicKeyOrToken);
            }
            else
            {
                assemblyName.SetPublicKeyToken(publicKeyOrToken);
            }

            return assemblyName;
        }

        /// <summary>
        /// Gets the <see cref="AssemblyName"/> for a given file.
        /// </summary>
        /// <param name="assemblyFile">The path for the assembly which <see cref="AssemblyName"/> is to be returned.</param>
        /// <returns>An <see cref="AssemblyName"/> that represents the given <paramref name="assemblyFile"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="assemblyFile"/> is null.</exception>
        /// <exception cref="ArgumentException">If <paramref name="assemblyFile"/> is invalid.</exception>
        /// <exception cref="FileNotFoundException">If <paramref name="assemblyFile"/> is not found.</exception>
        /// <exception cref="BadImageFormatException">If <paramref name="assemblyFile"/> is not a valid assembly.</exception>
        public static unsafe AssemblyName GetAssemblyName(string assemblyFile)
        {
            if (assemblyFile is null)
            {
                Throw.ArgumentNull(nameof(assemblyFile));
            }

            FileStream? fileStream = null;
            MemoryMappedFile? mappedFile = null;
            MemoryMappedViewAccessor? accessor = null;
            PEReader? peReader = null;

            try
            {
                try
                {
                    // Create stream because CreateFromFile(string, ...) uses FileShare.None which is too strict
                    fileStream = new FileStream(assemblyFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1, useAsync: false);
                    if (fileStream.Length == 0)
                    {
                        throw new BadImageFormatException(SR.PEImageDoesNotHaveMetadata, assemblyFile);
                    }

                    mappedFile = MemoryMappedFile.CreateFromFile(
                        fileStream, null, fileStream.Length, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
                    accessor = mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

                    SafeMemoryMappedViewHandle? safeBuffer = accessor.SafeMemoryMappedViewHandle;
                    peReader = new PEReader((byte*)safeBuffer.DangerousGetHandle(), (int)safeBuffer.ByteLength);
                    MetadataReader mdReader = peReader.GetMetadataReader(MetadataReaderOptions.None);
                    AssemblyName assemblyName = mdReader.GetAssemblyDefinition().GetAssemblyName();

                    AssemblyFlags aFlags = mdReader.AssemblyTable.GetFlags();
#pragma warning disable SYSLIB0037 // AssemblyName.ProcessorArchitecture is obsolete
                    assemblyName.ProcessorArchitecture = CalculateProcArch(peReader, aFlags);
#pragma warning restore SYSLIB0037

                    return assemblyName;
                }
                finally
                {
                    peReader?.Dispose();
                    accessor?.Dispose();
                    mappedFile?.Dispose();
                    fileStream?.Dispose();
                }
            }
            catch (InvalidOperationException ex)
            {
                throw new BadImageFormatException(ex.Message, assemblyFile, ex);
            }
        }

        private static ProcessorArchitecture CalculateProcArch(PEReader peReader, AssemblyFlags aFlags)
        {
            // 0x70 specifies "reference assembly".
            // For these, CLR wants to return None as arch so they can be always loaded, regardless of process type.
            if (((uint)aFlags & 0xF0) == 0x70)
                return ProcessorArchitecture.None;

            PEHeaders peHeaders = peReader.PEHeaders;
            switch (peHeaders.CoffHeader.Machine)
            {
                case Machine.IA64:
                    return ProcessorArchitecture.IA64;
                case Machine.Arm:
                    return ProcessorArchitecture.Arm;
                case Machine.Amd64:
                    return ProcessorArchitecture.Amd64;
                case Machine.I386:
                    {
                        CorFlags flags = peHeaders.CorHeader!.Flags;
                        if ((flags & CorFlags.ILOnly) != 0 &&
                            (flags & CorFlags.Requires32Bit) == 0)
                        {
                            // platform neutral.
                            return ProcessorArchitecture.MSIL;
                        }

                        // requires x86
                        return ProcessorArchitecture.X86;
                    }
            }

            // ProcessorArchitecture is a legacy API and does not cover other Machine kinds.
            // For example ARM64 is not expressible
            return ProcessorArchitecture.None;
        }

        private static AssemblyNameFlags GetAssemblyNameFlags(AssemblyFlags flags)
        {
            AssemblyNameFlags assemblyNameFlags = AssemblyNameFlags.None;

            if ((flags & AssemblyFlags.PublicKey) != 0)
                assemblyNameFlags |= AssemblyNameFlags.PublicKey;

            if ((flags & AssemblyFlags.Retargetable) != 0)
                assemblyNameFlags |= AssemblyNameFlags.Retargetable;

            if ((flags & AssemblyFlags.EnableJitCompileTracking) != 0)
                assemblyNameFlags |= AssemblyNameFlags.EnableJITcompileTracking;

            if ((flags & AssemblyFlags.DisableJitCompileOptimizer) != 0)
                assemblyNameFlags |= AssemblyNameFlags.EnableJITcompileOptimizer;

            return assemblyNameFlags;
        }

        private static AssemblyContentType GetContentTypeFromAssemblyFlags(AssemblyFlags flags)
        {
            return (AssemblyContentType)(((int)flags & (int)AssemblyFlags.ContentTypeMask) >> 9);
        }
    }
}
