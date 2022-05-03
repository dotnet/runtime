// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration.Assemblies;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Reflection
{
    //
    // Unmanaged view of AssemblyNameParser.AssemblyNameParts used to interop with the VM
    //
    internal unsafe struct NativeAssemblyNameParts
    {
        public char* _pName;
        public ushort _major, _minor, _build, _revision;
        public char* _pCultureName;
        public byte* _pPublicKeyOrToken;
        public int _cbPublicKeyOrToken;
        public AssemblyNameFlags _flags;

        //
        // Native AssemblySpec stores uint16 components for the version. Managed AssemblyName.Version stores int32.
        // When the former are initialized from the latter, the components are truncated to uint16 size.
        // When the latter are initialized from the former, they are zero-extended to int32 size.
        // For uint16 components, the max value is used to indicate an unspecified component.
        //

        public void SetVersion(Version? version, ushort defaultValue)
        {
            if (version != null)
            {
                _major = (ushort)version.Major;
                _minor = (ushort)version.Minor;
                _build = (ushort)version.Build;
                _revision = (ushort)version.Revision;
            }
            else
            {
                _major = defaultValue;
                _minor = defaultValue;
                _build = defaultValue;
                _revision = defaultValue;
            }
        }

        public Version? GetVersion()
        {
            if (_major == ushort.MaxValue || _minor == ushort.MaxValue)
                return null;

            if (_build == ushort.MaxValue)
                return new Version(_major, _minor);

            if (_revision == ushort.MaxValue)
                return new Version(_major, _minor, _build);

            return new Version(_major, _minor, _build, _revision);
        }
    }

    public sealed partial class AssemblyName : ICloneable, IDeserializationCallback, ISerializable
    {
        internal unsafe AssemblyName(NativeAssemblyNameParts* pParts)
            : this()
        {
            if (pParts->_pName != null)
            {
                _name = new string(pParts->_pName);
            }

            if (pParts->_pCultureName != null)
            {
                _cultureInfo = new CultureInfo(new string(pParts->_pCultureName));
            }

            if (pParts->_pPublicKeyOrToken != null)
            {
                byte[] publicKeyOrToken = new ReadOnlySpan<byte>(pParts->_pPublicKeyOrToken, pParts->_cbPublicKeyOrToken).ToArray();

                if ((pParts->_flags & AssemblyNameFlags.PublicKey) != 0)
                {
                    _publicKey = publicKeyOrToken;
                }
                else
                {
                    _publicKeyToken = publicKeyOrToken;
                }
            }

            _version = pParts->GetVersion();

            _flags = pParts->_flags;
        }

        internal byte[]? RawPublicKey => _publicKey;
        internal byte[]? RawPublicKeyToken => _publicKeyToken;

        internal AssemblyNameFlags RawFlags
        {
            get => _flags;
            set => _flags = value;
        }

        internal void SetProcArchIndex(PortableExecutableKinds pek, ImageFileMachine ifm)
        {
#pragma warning disable SYSLIB0037 // AssemblyName.ProcessorArchitecture is obsolete
            ProcessorArchitecture = CalculateProcArchIndex(pek, ifm, _flags);
#pragma warning restore SYSLIB0037
        }

        private static ProcessorArchitecture CalculateProcArchIndex(PortableExecutableKinds pek, ImageFileMachine ifm, AssemblyNameFlags flags)
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
