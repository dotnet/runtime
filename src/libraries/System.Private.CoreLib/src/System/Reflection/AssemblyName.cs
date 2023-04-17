// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Configuration.Assemblies;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text;
using CultureInfo = System.Globalization.CultureInfo;

namespace System.Reflection
{
    public sealed partial class AssemblyName : ICloneable, IDeserializationCallback, ISerializable
    {
        // If you modify any of these fields, you must also update the
        // AssemblyBaseObject structure in object.h
        private string? _name;
        private byte[]? _publicKey;
        private byte[]? _publicKeyToken;
        private CultureInfo? _cultureInfo;
        private string? _codeBase;
        private Version? _version;

        private AssemblyHashAlgorithm _hashAlgorithm;

        private AssemblyVersionCompatibility _versionCompatibility;
        private AssemblyNameFlags _flags;

        public AssemblyName(string assemblyName)
            : this()
        {
            ArgumentException.ThrowIfNullOrEmpty(assemblyName);
            if (assemblyName[0] == '\0')
                throw new ArgumentException(SR.Format_StringZeroLength);

            AssemblyNameParser.AssemblyNameParts parts = AssemblyNameParser.Parse(assemblyName);
            _name = parts._name;
            _version = parts._version;
            _flags = parts._flags;
            if ((parts._flags & AssemblyNameFlags.PublicKey) != 0)
            {
                _publicKey = parts._publicKeyOrToken;
            }
            else
            {
                _publicKeyToken = parts._publicKeyOrToken;
            }

            if (parts._cultureName != null)
                _cultureInfo = new CultureInfo(parts._cultureName);
        }

        public AssemblyName()
        {
            _versionCompatibility = AssemblyVersionCompatibility.SameMachine;
        }

        // Set and get the name of the assembly. If this is a weak Name
        // then it optionally contains a site. For strong assembly names,
        // the name partitions up the strong name's namespace
        public string? Name
        {
            get => _name;
            set => _name = value;
        }

        public Version? Version
        {
            get => _version;
            set => _version = value;
        }

        // Locales, internally the LCID is used for the match.
        public CultureInfo? CultureInfo
        {
            get => _cultureInfo;
            set => _cultureInfo = value;
        }

        public string? CultureName
        {
            get => _cultureInfo?.Name;
            set => _cultureInfo = (value == null) ? null : new CultureInfo(value);
        }

        [Obsolete(Obsoletions.AssemblyNameCodeBaseMessage, DiagnosticId = Obsoletions.AssemblyNameCodeBaseDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public string? CodeBase
        {
            [RequiresAssemblyFiles("The code will return an empty string for assemblies embedded in a single-file app")]
            get => _codeBase;
            set => _codeBase = value;
        }

        [Obsolete(Obsoletions.AssemblyNameCodeBaseMessage, DiagnosticId = Obsoletions.AssemblyNameCodeBaseDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [RequiresAssemblyFiles("The code will return an empty string for assemblies embedded in a single-file app")]
        public string? EscapedCodeBase
        {
            get
            {
                if (_codeBase == null)
                    return null;
                else
                    return EscapeCodeBase(_codeBase);
            }
        }

        [Obsolete(Obsoletions.AssemblyNameMembersMessage, DiagnosticId = Obsoletions.AssemblyNameMembersDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public ProcessorArchitecture ProcessorArchitecture
        {
            get
            {
                int x = (((int)_flags) & 0x70) >> 4;
                if (x > 5)
                    x = 0;
                return (ProcessorArchitecture)x;
            }
            set
            {
                int x = ((int)value) & 0x07;
                if (x <= 5)
                {
                    _flags = (AssemblyNameFlags)((int)_flags & 0xFFFFFF0F);
                    _flags |= (AssemblyNameFlags)(x << 4);
                }
            }
        }

        public AssemblyContentType ContentType
        {
            get
            {
                int x = (((int)_flags) & 0x00000E00) >> 9;
                if (x > 1)
                    x = 0;
                return (AssemblyContentType)x;
            }
            set
            {
                int x = ((int)value) & 0x07;
                if (x <= 1)
                {
                    _flags = (AssemblyNameFlags)((int)_flags & 0xFFFFF1FF);
                    _flags |= (AssemblyNameFlags)(x << 9);
                }
            }
        }

        // Make a copy of this assembly name.
        public object Clone()
        {
            var name = new AssemblyName
            {
                _name = _name,
                _publicKey = (byte[]?)_publicKey?.Clone(),
                _publicKeyToken = (byte[]?)_publicKeyToken?.Clone(),
                _cultureInfo = _cultureInfo,
                _version = _version,
                _flags = _flags,
                _codeBase = _codeBase,
                _hashAlgorithm = _hashAlgorithm,
                _versionCompatibility = _versionCompatibility,
            };
            return name;
        }

        private static Func<string, AssemblyName>? s_getAssemblyName;
        private static Func<string, AssemblyName> InitGetAssemblyName()
        {
            Type readerType = Type.GetType(
                    "System.Reflection.Metadata.MetadataReader, System.Reflection.Metadata",
                    throwOnError: true)!;

            MethodInfo? getAssemblyNameMethod = readerType.GetMethod(
                "GetAssemblyName",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
                null,
                new Type[] { typeof(string) },
                null);

            if (getAssemblyNameMethod == null)
            {
                throw new MissingMethodException(readerType.FullName, "GetAssemblyName");
            }

            return s_getAssemblyName = getAssemblyNameMethod.CreateDelegate<Func<string, AssemblyName>>();
        }

        /*
         * Get the AssemblyName for a given file. This will only work
         * if the file contains an assembly manifest. This method causes
         * the file to be opened and closed.
         */
        public static AssemblyName GetAssemblyName(string assemblyFile)
        {
            return (s_getAssemblyName ?? InitGetAssemblyName())(assemblyFile);
        }

        public byte[]? GetPublicKey()
        {
            return _publicKey;
        }

        public void SetPublicKey(byte[]? publicKey)
        {
            _publicKey = publicKey;

            if (publicKey == null)
                _flags &= ~AssemblyNameFlags.PublicKey;
            else
                _flags |= AssemblyNameFlags.PublicKey;
        }

        // The compressed version of the public key formed from a truncated hash.
        // Will throw a SecurityException if _publicKey is invalid
        public byte[]? GetPublicKeyToken() => _publicKeyToken ??= AssemblyNameHelpers.ComputePublicKeyToken(_publicKey);

        public void SetPublicKeyToken(byte[]? publicKeyToken)
        {
            _publicKeyToken = publicKeyToken;
        }

        // Flags modifying the name. So far the only flag is PublicKey, which
        // indicates that a full public key and not the compressed version is
        // present.
        // Processor Architecture flags are set only through ProcessorArchitecture
        // property and can't be set or retrieved directly
        // Content Type flags are set only through ContentType property and can't be
        // set or retrieved directly
        public AssemblyNameFlags Flags
        {
            get => (AssemblyNameFlags)((uint)_flags & 0xFFFFF10F);
            set
            {
                _flags &= unchecked((AssemblyNameFlags)0x00000EF0);
                _flags |= (value & unchecked((AssemblyNameFlags)0xFFFFF10F));
            }
        }

        [Obsolete(Obsoletions.AssemblyNameMembersMessage, DiagnosticId = Obsoletions.AssemblyNameMembersDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public AssemblyHashAlgorithm HashAlgorithm
        {
            get => _hashAlgorithm;
            set => _hashAlgorithm = value;
        }

        [Obsolete(Obsoletions.AssemblyNameMembersMessage, DiagnosticId = Obsoletions.AssemblyNameMembersDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public AssemblyVersionCompatibility VersionCompatibility
        {
            get => _versionCompatibility;
            set => _versionCompatibility = value;
        }

        [Obsolete(Obsoletions.StrongNameKeyPairMessage, DiagnosticId = Obsoletions.StrongNameKeyPairDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public StrongNameKeyPair? KeyPair
        {
            get => throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);
            set => throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);
        }

        public string FullName
        {
            get
            {
                if (string.IsNullOrEmpty(this.Name))
                    return string.Empty;

                // Do not call GetPublicKeyToken() here - that latches the result into AssemblyName which isn't a side effect we want.
                byte[]? pkt = _publicKeyToken ?? AssemblyNameHelpers.ComputePublicKeyToken(_publicKey);
                return AssemblyNameFormatter.ComputeDisplayName(Name, Version, CultureName, pkt, Flags, ContentType);
            }
        }

        public override string ToString()
        {
            string s = FullName;
            if (s == null)
                return base.ToString()!;
            else
                return s;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public void OnDeserialization(object? sender)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Compares the simple names disregarding Version, Culture and PKT. While this clearly does not
        /// match the intent of this api, this api has been broken this way since its debut and we cannot
        /// change its behavior now.
        /// </summary>
        public static bool ReferenceMatchesDefinition(AssemblyName? reference, AssemblyName? definition)
        {
            if (object.ReferenceEquals(reference, definition))
                return true;
            ArgumentNullException.ThrowIfNull(reference);
            ArgumentNullException.ThrowIfNull(definition);

            string refName = reference.Name ?? string.Empty;
            string defName = definition.Name ?? string.Empty;
            return refName.Equals(defName, StringComparison.OrdinalIgnoreCase);
        }

        [RequiresAssemblyFiles("The code will return an empty string for assemblies embedded in a single-file app")]
        internal static string EscapeCodeBase(string? codebase)
        {
            if (codebase == null)
                return string.Empty;

            int position = 0;
            char[]? dest = EscapeString(codebase, 0, codebase.Length, null, ref position, true, c_DummyChar, c_DummyChar, c_DummyChar);
            if (dest == null)
                return codebase;

            return new string(dest, 0, position);
        }

        // This implementation of EscapeString has been copied from System.Private.Uri from the runtime repo
        // - forceX characters are always escaped if found
        // - rsvd character will remain unescaped
        //
        // start    - starting offset from input
        // end      - the exclusive ending offset in input
        // destPos  - starting offset in dest for output, on return this will be an exclusive "end" in the output.
        //
        // In case "dest" has lack of space it will be reallocated by preserving the _whole_ content up to current destPos
        //
        // Returns null if nothing has to be escaped AND passed dest was null, otherwise the resulting array with the updated destPos
        //
        internal static unsafe char[]? EscapeString(string input, int start, int end, char[]? dest, ref int destPos,
            bool isUriString, char force1, char force2, char rsvd)
        {
            int i = start;
            int prevInputPos = start;
            byte* bytes = stackalloc byte[c_MaxUnicodeCharsReallocate * c_MaxUTF_8BytesPerUnicodeChar];   // 40*4=160

            fixed (char* pStr = input)
            {
                for (; i < end; ++i)
                {
                    char ch = pStr[i];

                    // a Unicode ?
                    if (ch > '\x7F')
                    {
                        short maxSize = (short)Math.Min(end - i, (int)c_MaxUnicodeCharsReallocate - 1);

                        short count = 1;
                        for (; count < maxSize && pStr[i + count] > '\x7f'; ++count) ;

                        // Is the last a high surrogate?
                        if (pStr[i + count - 1] >= 0xD800 && pStr[i + count - 1] <= 0xDBFF)
                        {
                            // Should be a rare case where the app tries to feed an invalid Unicode surrogates pair
                            if (count == 1 || count == end - i)
                                throw new FormatException(SR.Arg_FormatException);
                            // need to grab one more char as a Surrogate except when it's a bogus input
                            ++count;
                        }

                        dest = EnsureDestinationSize(pStr, dest, i,
                            (short)(count * c_MaxUTF_8BytesPerUnicodeChar * c_EncodedCharsPerByte),
                            c_MaxUnicodeCharsReallocate * c_MaxUTF_8BytesPerUnicodeChar * c_EncodedCharsPerByte,
                            ref destPos, prevInputPos);

                        short numberOfBytes = (short)Encoding.UTF8.GetBytes(pStr + i, count, bytes,
                            c_MaxUnicodeCharsReallocate * c_MaxUTF_8BytesPerUnicodeChar);

                        // This is the only exception that built in UriParser can throw after a Uri ctor.
                        // Should not happen unless the app tries to feed an invalid Unicode string
                        if (numberOfBytes == 0)
                            throw new FormatException(SR.Arg_FormatException);

                        i += (count - 1);

                        for (count = 0; count < numberOfBytes; ++count)
                            EscapeAsciiChar((char)bytes[count], dest, ref destPos);

                        prevInputPos = i + 1;
                    }
                    else if (ch == '%' && rsvd == '%')
                    {
                        // Means we don't reEncode '%' but check for the possible escaped sequence
                        dest = EnsureDestinationSize(pStr, dest, i, c_EncodedCharsPerByte,
                            c_MaxAsciiCharsReallocate * c_EncodedCharsPerByte, ref destPos, prevInputPos);
                        if (i + 2 < end && char.IsAsciiHexDigit(pStr[i + 1]) && char.IsAsciiHexDigit(pStr[i + 2]))
                        {
                            // leave it escaped
                            dest[destPos++] = '%';
                            dest[destPos++] = pStr[i + 1];
                            dest[destPos++] = pStr[i + 2];
                            i += 2;
                        }
                        else
                        {
                            EscapeAsciiChar('%', dest, ref destPos);
                        }
                        prevInputPos = i + 1;
                    }
                    else if (ch == force1 || ch == force2 || (ch != rsvd && (isUriString ? !IsReservedUnreservedOrHash(ch) : !IsUnreserved(ch))))
                    {
                        dest = EnsureDestinationSize(pStr, dest, i, c_EncodedCharsPerByte,
                            c_MaxAsciiCharsReallocate * c_EncodedCharsPerByte, ref destPos, prevInputPos);
                        EscapeAsciiChar(ch, dest, ref destPos);
                        prevInputPos = i + 1;
                    }
                }

                if (prevInputPos != i)
                {
                    // need to fill up the dest array ?
                    if (prevInputPos != start || dest != null)
                        dest = EnsureDestinationSize(pStr, dest, i, 0, 0, ref destPos, prevInputPos);
                }
            }

            return dest;
        }

        //
        // ensure destination array has enough space and contains all the needed input stuff
        //
        private static unsafe char[] EnsureDestinationSize(char* pStr, char[]? dest, int currentInputPos,
            short charsToAdd, short minReallocateChars, ref int destPos, int prevInputPos)
        {
            if (dest is null || dest.Length < destPos + (currentInputPos - prevInputPos) + charsToAdd)
            {
                // allocating or reallocating array by ensuring enough space based on maxCharsToAdd.
                char[] newresult = new char[destPos + (currentInputPos - prevInputPos) + minReallocateChars];

                if (!(dest is null) && destPos != 0)
                    Buffer.BlockCopy(dest, 0, newresult, 0, destPos << 1);
                dest = newresult;
            }

            // ensuring we copied everything form the input string left before last escaping
            while (prevInputPos != currentInputPos)
                dest[destPos++] = pStr[prevInputPos++];
            return dest;
        }

        internal static void EscapeAsciiChar(char ch, char[] to, ref int pos)
        {
            to[pos++] = '%';
            to[pos++] = HexConverter.ToCharUpper(ch >> 4);
            to[pos++] = HexConverter.ToCharUpper(ch);
        }

        private static bool IsReservedUnreservedOrHash(char c)
        {
            if (IsUnreserved(c))
            {
                return true;
            }
            return RFC3986ReservedMarks.Contains(c);
        }

        internal static bool IsUnreserved(char c)
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                return true;
            }
            return RFC3986UnreservedMarks.Contains(c);
        }

        internal const char c_DummyChar = (char)0xFFFF;     // An Invalid Unicode character used as a dummy char passed into the parameter
        private const short c_MaxAsciiCharsReallocate = 40;
        private const short c_MaxUnicodeCharsReallocate = 40;
        private const short c_MaxUTF_8BytesPerUnicodeChar = 4;
        private const short c_EncodedCharsPerByte = 3;
        private const string RFC3986ReservedMarks = ":/?#[]@!$&'()*+,;=";
        private const string RFC3986UnreservedMarks = "-._~";
    }
}
