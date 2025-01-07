// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration.Assemblies;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono;

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
            _name = Marshal.PtrToStringUTF8(native->name);

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
                _cultureInfo = CultureInfo.GetCultureInfo(Marshal.PtrToStringUTF8(native->culture)!);

            if (native->public_key != IntPtr.Zero)
            {
                _publicKey = DecodeBlobArray(native->public_key);
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

        private static int DecodeBlobSize(IntPtr in_ptr, out IntPtr out_ptr)
        {
            uint size;
            unsafe
            {
                byte* ptr = (byte*)in_ptr;

                if ((*ptr & 0x80) == 0)
                {
                    size = (uint)(ptr[0] & 0x7f);
                    ptr++;
                }
                else if ((*ptr & 0x40) == 0)
                {
                    size = (uint)(((ptr[0] & 0x3f) << 8) + ptr[1]);
                    ptr += 2;
                }
                else
                {
                    size = (uint)(((ptr[0] & 0x1f) << 24) +
                        (ptr[1] << 16) +
                        (ptr[2] << 8) +
                        ptr[3]);
                    ptr += 4;
                }
                out_ptr = (IntPtr)ptr;
            }

            return (int)size;
        }

        internal static byte[] DecodeBlobArray(IntPtr ptr)
        {
            IntPtr out_ptr;
            int size = DecodeBlobSize(ptr, out out_ptr);
            byte[] res = new byte[size];
            Marshal.Copy(out_ptr, res, 0, size);
            return res;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void FreeAssemblyName(ref MonoAssemblyName name, bool freeStruct);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe MonoAssemblyName* GetNativeName(IntPtr assemblyPtr);
    }
}
