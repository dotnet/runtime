// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Internal.Runtime.Binder
{
    internal unsafe struct AssemblyMetaDataInternal
    {
        public ushort usMajorVersion;         // Major Version.
        public ushort usMinorVersion;         // Minor Version.
        public ushort usBuildNumber;          // Build Number.
        public ushort usRevisionNumber;       // Revision Number.
        public byte* szLocale;               // Locale.
    }

    internal enum CorAssemblyFlags
    {
        afPublicKey = 0x0001,     // The assembly ref holds the full (unhashed) public key.

        afPA_None = 0x0000,     // Processor Architecture unspecified
        afPA_MSIL = 0x0010,     // Processor Architecture: neutral (PE32)
        afPA_x86 = 0x0020,     // Processor Architecture: x86 (PE32)
        afPA_IA64 = 0x0030,     // Processor Architecture: Itanium (PE32+)
        afPA_AMD64 = 0x0040,     // Processor Architecture: AMD X64 (PE32+)
        afPA_ARM = 0x0050,     // Processor Architecture: ARM (PE32)
        afPA_ARM64 = 0x0060,     // Processor Architecture: ARM64 (PE32+)
        afPA_NoPlatform = 0x0070,      // applies to any platform but cannot run on any (e.g. reference assembly), should not have "specified" set
        afPA_Specified = 0x0080,     // Propagate PA flags to AssemblyRef record
        afPA_Mask = 0x0070,     // Bits describing the processor architecture
        afPA_FullMask = 0x00F0,     // Bits describing the PA incl. Specified
        afPA_Shift = 0x0004,     // NOT A FLAG, shift count in PA flags <--> index conversion

        afEnableJITcompileTracking = 0x8000, // From "DebuggableAttribute".
        afDisableJITcompileOptimizer = 0x4000, // From "DebuggableAttribute".
        afDebuggableAttributeMask = 0xc000,

        afRetargetable = 0x0100,     // The assembly can be retargeted (at runtime) to an
                                     //  assembly from a different publisher.

        afContentType_Default = 0x0000,
        afContentType_WindowsRuntime = 0x0200,
        afContentType_Mask = 0x0E00, // Bits describing ContentType
    }

    internal enum AssemblyNameIncludeFlags
    {
        INCLUDE_DEFAULT = 0x00,
        INCLUDE_VERSION = 0x01,
        INCLUDE_ARCHITECTURE = 0x02,
        INCLUDE_RETARGETABLE = 0x04,
        INCLUDE_CONTENT_TYPE = 0x08,
        INCLUDE_PUBLIC_KEY_TOKEN = 0x10,
        EXCLUDE_CULTURE = 0x20,
        INCLUDE_ALL = INCLUDE_VERSION
                    | INCLUDE_ARCHITECTURE
                    | INCLUDE_RETARGETABLE
                    | INCLUDE_CONTENT_TYPE
                    | INCLUDE_PUBLIC_KEY_TOKEN,
    }

    internal sealed unsafe class AssemblyName : AssemblyIdentity
    {
        public bool IsDefinition;

        public AssemblyName(IntPtr pPEImage)
        {
            IdentityFlags |=
                AssemblyIdentityFlags.IDENTITY_FLAG_CULTURE | AssemblyIdentityFlags.IDENTITY_FLAG_PUBLIC_KEY_TOKEN_NULL;

            int* dwPAFlags = stackalloc int[2];
            using IMdInternalImport pIMetaDataAssemblyImport = BinderAcquireImport(pPEImage, dwPAFlags);

            Architecture = AssemblyBinderCommon.TranslatePEToArchitectureType(dwPAFlags);

            // Get the assembly token
            uint mda;
            pIMetaDataAssemblyImport.GetAssemblyFromScope(&mda);

            AssemblyMetaDataInternal amd = default;
            byte* pvPublicKeyToken = null;
            uint dwPublicKeyToken = 0;
            byte* pAssemblyName = null;
            CorAssemblyFlags dwRefOrDefFlags = 0;
            uint dwHashAlgId = 0;

            // Get name and metadata
            pIMetaDataAssemblyImport.GetAssemblyProps(
                mda,            // [IN] The Assembly for which to get the properties.
                &pvPublicKeyToken,  // [OUT] Pointer to the PublicKeyToken blob.
                &dwPublicKeyToken,  // [OUT] Count of bytes in the PublicKeyToken Blob.
                &dwHashAlgId,   // [OUT] Hash Algorithm.
                &pAssemblyName, // [OUT] Name.
                &amd,           // [OUT] Assembly MetaData.
                &dwRefOrDefFlags // [OUT] Flags.
                );

            {
                string culture = Encoding.UTF8.GetString(amd.szLocale, string.strlen(amd.szLocale));
                int index = culture.IndexOf(';');
                if (index != -1)
                {
                    culture = culture[..index];
                }

                CultureOrLanguage = culture;
                IdentityFlags |= AssemblyIdentityFlags.IDENTITY_FLAG_CULTURE;
            }

            {
                string assemblyName = Encoding.UTF8.GetString(pAssemblyName, string.strlen(pAssemblyName));

                const int MAX_PATH_FNAME = 260;
                if (assemblyName.Length >= MAX_PATH_FNAME)
                {
                    throw new Exception("FUSION_E_INVALID_NAME");
                }

                SimpleName = assemblyName;
                IdentityFlags |= AssemblyIdentityFlags.IDENTITY_FLAG_SIMPLE_NAME;
            }

            // See if the assembly[def] is retargetable (ie, for a generic assembly).
            if ((dwRefOrDefFlags | CorAssemblyFlags.afRetargetable) != 0)
            {
                IdentityFlags |= AssemblyIdentityFlags.IDENTITY_FLAG_RETARGETABLE;
            }

            // Set ContentType
            if ((dwRefOrDefFlags | CorAssemblyFlags.afContentType_Mask) == CorAssemblyFlags.afContentType_Default)
            {
                ContentType = System.Reflection.AssemblyContentType.Default;
            }
            else
            {
                // We no longer support WindowsRuntime assembly.
                throw new Exception("FUSION_E_INVALID_NAME");
            }

            // Set the assembly version
            {
                Version = new AssemblyVersion
                {
                    Major = amd.usMajorVersion,
                    Minor = amd.usMinorVersion,
                    Build = amd.usBuildNumber,
                    Revision = amd.usRevisionNumber
                };

                IdentityFlags |= AssemblyIdentityFlags.IDENTITY_FLAG_VERSION;
            }

            // Set public key and/or public key token (if we have it)
            if (dwPublicKeyToken != 0 && pvPublicKeyToken != null)
            {
                if ((dwRefOrDefFlags & CorAssemblyFlags.afPublicKey) != 0)
                {
                    byte* pByteToken;
                    uint dwTokenLen;
                    StrongNameTokenFromPublicKey(pvPublicKeyToken, dwPublicKeyToken, &pByteToken, &dwTokenLen);

                    PublicKeyOrTokenBLOB = new ReadOnlySpan<byte>(pByteToken, (int)dwTokenLen).ToArray();
                    StrongNameFreeBuffer(pByteToken);
                }
                else
                {
                    PublicKeyOrTokenBLOB = new ReadOnlySpan<byte>(pvPublicKeyToken, (int)dwPublicKeyToken).ToArray();
                }

                IdentityFlags |= AssemblyIdentityFlags.IDENTITY_FLAG_PUBLIC_KEY_TOKEN;
            }
        }

        // TODO: Is this simple comparison enough?
        public bool IsCoreLib => SimpleName == CoreLib.Name;

        public bool IsNeutralCulture => CultureOrLanguage == NeutralCulture;

        // Foo internal calls

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void StrongNameTokenFromPublicKey(
            byte* pbPublicKeyBlob,        // [in] public key blob
            uint cbPublicKeyBlob,
            byte** ppbStrongNameToken,     // [out] strong name token
            uint* pcbStrongNameToken);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void StrongNameFreeBuffer(byte* buffer);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IMdInternalImport BinderAcquireImport(IntPtr pPEImage, int* dwPAFlags);

        public int GetHashCode(AssemblyNameIncludeFlags dwIncludeFlags)
        {
            uint dwHash = 0;
            AssemblyIdentityFlags dwUseIdentityFlags = IdentityFlags;

            // Prune unwanted name parts
            if ((dwIncludeFlags & AssemblyNameIncludeFlags.INCLUDE_VERSION) == 0)
            {
                dwUseIdentityFlags &= ~AssemblyIdentityFlags.IDENTITY_FLAG_VERSION;
            }
            if ((dwIncludeFlags & AssemblyNameIncludeFlags.INCLUDE_ARCHITECTURE) == 0)
            {
                dwUseIdentityFlags &= ~AssemblyIdentityFlags.IDENTITY_FLAG_PROCESSOR_ARCHITECTURE;
            }
            if ((dwIncludeFlags & AssemblyNameIncludeFlags.INCLUDE_RETARGETABLE) == 0)
            {
                dwUseIdentityFlags &= ~AssemblyIdentityFlags.IDENTITY_FLAG_RETARGETABLE;
            }
            if ((dwIncludeFlags & AssemblyNameIncludeFlags.INCLUDE_CONTENT_TYPE) == 0)
            {
                dwUseIdentityFlags &= ~AssemblyIdentityFlags.IDENTITY_FLAG_CONTENT_TYPE;
            }
            if ((dwIncludeFlags & AssemblyNameIncludeFlags.INCLUDE_PUBLIC_KEY_TOKEN) == 0)
            {
                dwUseIdentityFlags &= ~AssemblyIdentityFlags.IDENTITY_FLAG_PUBLIC_KEY;
                dwUseIdentityFlags &= ~AssemblyIdentityFlags.IDENTITY_FLAG_PUBLIC_KEY_TOKEN;
            }
            if ((dwIncludeFlags & AssemblyNameIncludeFlags.EXCLUDE_CULTURE) != 0)
            {
                dwUseIdentityFlags &= ~AssemblyIdentityFlags.IDENTITY_FLAG_CULTURE;
            }

            static uint HashCaseInsensitive(string str)
            {
                // ported from SString::HashCaseInsensitive
                uint hash = 5381;

                foreach (char ch in str)
                {
                    hash = ((hash << 5) + hash) ^ char.ToUpperInvariant(ch);
                }

                return hash;
            }

            static uint HashBytes(ReadOnlySpan<byte> bytes)
            {
                // ported from coreclr/inc/utilcode.h
                uint hash = 5831;

                foreach (byte b in bytes)
                {
                    hash = ((hash << 5) + hash) ^ b;
                }

                return hash;
            }

            dwHash ^= HashCaseInsensitive(SimpleName);
            dwHash = BitOperations.RotateLeft(dwHash, 4);

            if ((dwUseIdentityFlags & AssemblyIdentityFlags.IDENTITY_FLAG_PUBLIC_KEY) != 0 ||
                (dwUseIdentityFlags & AssemblyIdentityFlags.IDENTITY_FLAG_PUBLIC_KEY_TOKEN) != 0)
            {
                dwHash ^= HashBytes(PublicKeyOrTokenBLOB);
                dwHash = BitOperations.RotateLeft(dwHash, 4);
            }

            if ((dwUseIdentityFlags & AssemblyIdentityFlags.IDENTITY_FLAG_VERSION) != 0)
            {

                dwHash ^= (uint)Version.Major;
                dwHash = BitOperations.RotateLeft(dwHash, 8);
                dwHash ^= (uint)Version.Minor;
                dwHash = BitOperations.RotateLeft(dwHash, 8);
                dwHash ^= (uint)Version.Build;
                dwHash = BitOperations.RotateLeft(dwHash, 8);
                dwHash ^= (uint)Version.Revision;
                dwHash = BitOperations.RotateLeft(dwHash, 8);
            }

            if ((dwUseIdentityFlags & AssemblyIdentityFlags.IDENTITY_FLAG_CULTURE) != 0)
            {
                dwHash ^= HashCaseInsensitive(NormalizedCulture);
                dwHash = BitOperations.RotateLeft(dwHash, 4);
            }

            if ((dwUseIdentityFlags & AssemblyIdentityFlags.IDENTITY_FLAG_RETARGETABLE) != 0)
            {
                dwHash ^= 1;
                dwHash = BitOperations.RotateLeft(dwHash, 4);
            }

            if ((dwUseIdentityFlags & AssemblyIdentityFlags.IDENTITY_FLAG_PROCESSOR_ARCHITECTURE) != 0)
            {
                dwHash ^= (uint)Architecture;
                dwHash = BitOperations.RotateLeft(dwHash, 4);
            }

            if ((dwUseIdentityFlags & AssemblyIdentityFlags.IDENTITY_FLAG_CONTENT_TYPE) != 0)
            {
                dwHash ^= (uint)ContentType;
                dwHash = BitOperations.RotateLeft(dwHash, 4);
            }

            return (int)dwHash;
        }

        public bool Equals(AssemblyIdentity other, AssemblyNameIncludeFlags dwIncludeFlags)
        {
            bool fEquals = false;

            if (ContentType == System.Reflection.AssemblyContentType.WindowsRuntime)
            {   // Assembly is meaningless for WinRT, all assemblies form one joint type namespace
                return ContentType == other.ContentType;
            }

            if (string.Equals(SimpleName, other.SimpleName, StringComparison.InvariantCultureIgnoreCase) &&
                ContentType == other.ContentType)
            {
                fEquals = true;

                if ((dwIncludeFlags & AssemblyNameIncludeFlags.EXCLUDE_CULTURE) == 0)
                {
                    fEquals = string.Equals(NormalizedCulture, other.NormalizedCulture, StringComparison.InvariantCultureIgnoreCase);
                }

                if (fEquals && (dwIncludeFlags & AssemblyNameIncludeFlags.INCLUDE_PUBLIC_KEY_TOKEN) != 0)
                {
                    fEquals = PublicKeyOrTokenBLOB.AsSpan().SequenceEqual(other.PublicKeyOrTokenBLOB);
                }

                if (fEquals && ((dwIncludeFlags & AssemblyNameIncludeFlags.INCLUDE_ARCHITECTURE) != 0))
                {
                    fEquals = Architecture == other.Architecture;
                }

                if (fEquals && ((dwIncludeFlags & AssemblyNameIncludeFlags.INCLUDE_VERSION) != 0))
                {
                    fEquals = Version == other.Version;
                }

                if (fEquals && ((dwIncludeFlags & AssemblyNameIncludeFlags.INCLUDE_RETARGETABLE) != 0))
                {
                    fEquals = IsRetargetable == other.IsRetargetable;
                }
            }

            return fEquals;
        }

        public string GetDisplayName(AssemblyNameIncludeFlags dwIncludeFlags)
        {
            AssemblyIdentityFlags dwUseIdentityFlags = IdentityFlags;

            // Prune unwanted name parts
            if ((dwIncludeFlags & AssemblyNameIncludeFlags.INCLUDE_VERSION) == 0)
            {
                dwUseIdentityFlags &= ~AssemblyIdentityFlags.IDENTITY_FLAG_VERSION;
            }
            if ((dwIncludeFlags & AssemblyNameIncludeFlags.INCLUDE_ARCHITECTURE) == 0)
            {
                dwUseIdentityFlags &= ~AssemblyIdentityFlags.IDENTITY_FLAG_PROCESSOR_ARCHITECTURE;
            }
            if ((dwIncludeFlags & AssemblyNameIncludeFlags.INCLUDE_RETARGETABLE) == 0)
            {
                dwUseIdentityFlags &= ~AssemblyIdentityFlags.IDENTITY_FLAG_RETARGETABLE;
            }
            if ((dwIncludeFlags & AssemblyNameIncludeFlags.INCLUDE_CONTENT_TYPE) == 0)
            {
                dwUseIdentityFlags &= ~AssemblyIdentityFlags.IDENTITY_FLAG_CONTENT_TYPE;
            }

            return TextualIdentityParser.ToString(this, dwUseIdentityFlags);
        }
    }

    internal unsafe interface IMdInternalImport : IDisposable
    {
        public void GetAssemblyFromScope(uint* ptkAssembly);

        public void GetAssemblyProps(uint mda, byte** ppbPublicKey, uint* pcbPublicKey, uint* pulHashAlgId, byte** pszName, void* pMetadata, CorAssemblyFlags* pdwAsselblyFlags);
    }
}
