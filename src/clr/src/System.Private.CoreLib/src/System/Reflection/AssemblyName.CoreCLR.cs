// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Assemblies;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Reflection
{
    public sealed partial class AssemblyName : ICloneable, IDeserializationCallback, ISerializable
    {
        public AssemblyName(string assemblyName)
        {
            if (assemblyName == null)
                throw new ArgumentNullException(nameof(assemblyName));
            if ((assemblyName.Length == 0) ||
                (assemblyName[0] == '\0'))
                throw new ArgumentException(SR.Format_StringZeroLength);

            _name = assemblyName;
            nInit();
        }

        internal AssemblyName(string? name,
            byte[]? publicKey,
            byte[]? publicKeyToken,
            Version? version,
            CultureInfo? cultureInfo,
            AssemblyHashAlgorithm hashAlgorithm,
            AssemblyVersionCompatibility versionCompatibility,
            string? codeBase,
            AssemblyNameFlags flags,
            StrongNameKeyPair? keyPair) // Null if ref, matching Assembly if def
        {
            _name = name;
            _publicKey = publicKey;
            _publicKeyToken = publicKeyToken;
            _version = version;
            _cultureInfo = cultureInfo;
            _hashAlgorithm = hashAlgorithm;
            _versionCompatibility = versionCompatibility;
            _codeBase = codeBase;
            _flags = flags;
            _strongNameKeyPair = keyPair;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern void nInit();
        
        // This call opens and closes the file, but does not add the
        // assembly to the domain.
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern AssemblyName nGetFileInformation(string s);

        internal static AssemblyName GetFileInformationCore(string assemblyFile)
        {
            string fullPath = Path.GetFullPath(assemblyFile);
            return nGetFileInformation(fullPath);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern byte[]? ComputePublicKeyToken();

        internal void SetProcArchIndex(PortableExecutableKinds pek, ImageFileMachine ifm)
        {
            ProcessorArchitecture = CalculateProcArchIndex(pek, ifm, _flags);
        }

        internal static ProcessorArchitecture CalculateProcArchIndex(PortableExecutableKinds pek, ImageFileMachine ifm, AssemblyNameFlags flags)
        {
            if (((uint)flags & 0xF0) == 0x70)
                return ProcessorArchitecture.None;

            if ((pek & PortableExecutableKinds.PE32Plus) == PortableExecutableKinds.PE32Plus)
            {
                switch (ifm)
                {
                    case ImageFileMachine.IA64:
                        return ProcessorArchitecture.IA64;
                    case ImageFileMachine.AMD64:
                        return ProcessorArchitecture.Amd64;
                    case ImageFileMachine.I386:
                        if ((pek & PortableExecutableKinds.ILOnly) == PortableExecutableKinds.ILOnly)
                            return ProcessorArchitecture.MSIL;
                        break;
                }
            }
            else
            {
                if (ifm == ImageFileMachine.I386)
                {
                    if ((pek & PortableExecutableKinds.Required32Bit) == PortableExecutableKinds.Required32Bit)
                        return ProcessorArchitecture.X86;

                    if ((pek & PortableExecutableKinds.ILOnly) == PortableExecutableKinds.ILOnly)
                        return ProcessorArchitecture.MSIL;

                    return ProcessorArchitecture.X86;
                }
                if (ifm == ImageFileMachine.ARM)
                {
                    return ProcessorArchitecture.Arm;
                }
            }
            return ProcessorArchitecture.None;
        }
    }
}
