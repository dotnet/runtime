// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration.Assemblies;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

    public sealed partial class AssemblyName
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
            ProcessorArchitecture = CalculateProcArch(pek, ifm, _flags);
#pragma warning restore SYSLIB0037
        }

        private static ProcessorArchitecture CalculateProcArch(PortableExecutableKinds pek, ImageFileMachine ifm, AssemblyNameFlags aFlags)
        {
            // 0x70 specifies "reference assembly".
            // For these, CLR wants to return None as arch so they can be always loaded, regardless of process type.
            if (((uint)aFlags & 0xF0) == 0x70)
                return ProcessorArchitecture.None;

            switch (ifm)
            {
                case ImageFileMachine.IA64:
                    return ProcessorArchitecture.IA64;
                case ImageFileMachine.ARM:
                    return ProcessorArchitecture.Arm;
                case ImageFileMachine.AMD64:
                    return ProcessorArchitecture.Amd64;
                case ImageFileMachine.I386:
                    {
                        if ((pek & PortableExecutableKinds.ILOnly) != 0 &&
                            (pek & PortableExecutableKinds.Required32Bit) == 0)
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

        private static unsafe void ParseAsAssemblySpec(char* pAssemblyName, void* pAssemblySpec)
        {
            AssemblyNameParser.AssemblyNameParts parts = AssemblyNameParser.Parse(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(pAssemblyName));

            fixed (char* pName = parts._name)
            fixed (char* pCultureName = parts._cultureName)
            fixed (byte* pPublicKeyOrToken = parts._publicKeyOrToken)
            {
                NativeAssemblyNameParts nameParts = default;

                nameParts._flags = parts._flags;
                nameParts._pName = pName;
                nameParts._pCultureName = pCultureName;

                nameParts._pPublicKeyOrToken = pPublicKeyOrToken;
                nameParts._cbPublicKeyOrToken = (parts._publicKeyOrToken != null) ? parts._publicKeyOrToken.Length : 0;

                nameParts.SetVersion(parts._version, defaultValue: ushort.MaxValue);

                InitializeAssemblySpec(&nameParts, pAssemblySpec);
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AssemblyName_InitializeAssemblySpec")]
        private static unsafe partial void InitializeAssemblySpec(NativeAssemblyNameParts* pAssemblyNameParts, void* pAssemblySpec);
    }
}
