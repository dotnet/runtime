// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono;
using System.Configuration.Assemblies;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection
{
    public partial class AssemblyName
    {
        internal static AssemblyName Create(IntPtr monoAssembly, string? codeBase)
        {
            AssemblyName aname = new AssemblyName();
            unsafe
            {
                MonoAssemblyName* native = GetNativeName(monoAssembly);
                aname.FillName(native, codeBase, true, true, true);
            }
            return aname;
        }

        internal unsafe void FillName(MonoAssemblyName* native, string? codeBase, bool addVersion, bool addPublickey, bool defaultToken)
        {
            _name = RuntimeMarshal.PtrToUtf8String(native->name);

            _flags = (AssemblyNameFlags)native->flags;

            _hashAlgorithm = (AssemblyHashAlgorithm)native->hash_alg;

            _versionCompatibility = AssemblyVersionCompatibility.SameMachine;

            if (addVersion)
            {
                int build = native->build == 65535 ? -1 : native->build;
                int revision = native->revision == 65535 ? -1 : native->revision;

                if (build == -1)
                    _version = new Version(native->major, native->minor);
                else if (revision == -1)
                    _version = new Version(native->major, native->minor, build);
                else
                    _version = new Version(native->major, native->minor, build, revision);
            }

            _codeBase = codeBase;

            if (native->culture != IntPtr.Zero)
                _cultureInfo = CultureInfo.GetCultureInfo(RuntimeMarshal.PtrToUtf8String(native->culture));

            if (native->public_key != IntPtr.Zero)
            {
                _publicKey = RuntimeMarshal.DecodeBlobArray(native->public_key);
                _flags |= AssemblyNameFlags.PublicKey;
            }
            else if (addPublickey)
            {
                _publicKey = Array.Empty<byte>();
                _flags |= AssemblyNameFlags.PublicKey;
            }

            // MonoAssemblyName keeps the public key token as an hexadecimal string
            if (native->public_key_token[0] != 0)
            {
                var keyToken = new byte[8];
                for (int i = 0, j = 0; i < 8; ++i)
                {
                    keyToken[i] = (byte)(HexConverter.FromChar(native->public_key_token[j++]) << 4 | HexConverter.FromChar(native->public_key_token[j++]));
                }
                _publicKeyToken = keyToken;
            }
            else if (defaultToken)
            {
                _publicKeyToken = Array.Empty<byte>();
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe MonoAssemblyName* GetNativeName(IntPtr assemblyPtr);
    }
}
