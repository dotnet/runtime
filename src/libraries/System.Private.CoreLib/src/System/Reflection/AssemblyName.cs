// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.ComponentModel;
using System.Configuration.Assemblies;
using System.Diagnostics;
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
                [typeof(string)],
                null) ??
                throw new MissingMethodException(readerType.FullName, "GetAssemblyName");
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
            if (ReferenceEquals(reference, definition))
                return true;
            ArgumentNullException.ThrowIfNull(reference);
            ArgumentNullException.ThrowIfNull(definition);

            string refName = reference.Name ?? string.Empty;
            string defName = definition.Name ?? string.Empty;
            return refName.Equals(defName, StringComparison.OrdinalIgnoreCase);
        }

        // This implementation of Escape has been copied from UriHelper from System.Private.Uri and adapted to match AssemblyName's requirements.
        internal static string EscapeCodeBase(string? codebase)
        {
            if (codebase == null)
                return string.Empty;

            int indexOfFirstToEscape = codebase.AsSpan().IndexOfAnyExcept(UnreservedReserved);
            if (indexOfFirstToEscape < 0)
            {
                // Nothing to escape, just return the original value.
                return codebase;
            }

            // Otherwise, create a ValueStringBuilder to store the escaped data into,
            // escape the rest, and concat the result with the characters we skipped above.
            var vsb = new ValueStringBuilder(stackalloc char[StackallocThreshold]);

            // We may throw for very large inputs (when growing the ValueStringBuilder).
            vsb.EnsureCapacity(codebase.Length);

            EscapeStringToBuilder(codebase.AsSpan(indexOfFirstToEscape), ref vsb);

            string result = string.Concat(codebase.AsSpan(0, indexOfFirstToEscape), vsb.AsSpan());
            vsb.Dispose();
            return result;
        }

        internal static void EscapeStringToBuilder(scoped ReadOnlySpan<char> stringToEscape, ref ValueStringBuilder vsb)
        {
            // Allocate enough stack space to hold any Rune's UTF8 encoding.
            Span<byte> utf8Bytes = stackalloc byte[4];

            while (!stringToEscape.IsEmpty)
            {
                char ch = stringToEscape[0];

                if (!char.IsAscii(ch))
                {
                    if (Rune.DecodeFromUtf16(stringToEscape, out Rune r, out int charsConsumed) != OperationStatus.Done)
                    {
                        r = Rune.ReplacementChar;
                    }

                    Debug.Assert(stringToEscape.EnumerateRunes() is { } e && e.MoveNext() && e.Current == r);
                    Debug.Assert(charsConsumed is 1 or 2);

                    stringToEscape = stringToEscape.Slice(charsConsumed);

                    // The rune is non-ASCII, so encode it as UTF8, and escape each UTF8 byte.
                    r.TryEncodeToUtf8(utf8Bytes, out int bytesWritten);
                    foreach (byte b in utf8Bytes.Slice(0, bytesWritten))
                    {
                        PercentEncodeByte(b, ref vsb);
                    }
                }
                else if (!UnreservedReserved.Contains(ch))
                {
                    PercentEncodeByte((byte)ch, ref vsb);
                    stringToEscape = stringToEscape.Slice(1);
                }
                else
                {
                    // We have a character we don't want to escape. It's likely there are more, do a vectorized search.
                    int charsToCopy = stringToEscape.IndexOfAnyExcept(UnreservedReserved);
                    if (charsToCopy < 0)
                    {
                        charsToCopy = stringToEscape.Length;
                    }
                    Debug.Assert(charsToCopy > 0);

                    vsb.Append(stringToEscape.Slice(0, charsToCopy));
                    stringToEscape = stringToEscape.Slice(charsToCopy);
                }
            }
        }

        internal static void PercentEncodeByte(byte ch, ref ValueStringBuilder vsb)
        {
            vsb.Append('%');
            HexConverter.ToCharsBuffer(ch, vsb.AppendSpan(2), 0, HexConverter.Casing.Upper);
        }

        [field: AllowNull]
        private static SearchValues<char> UnreservedReserved => field ??= SearchValues.Create("!#$&'()*+,-./0123456789:;=?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz~");

        private const int StackallocThreshold = 512;
    }
}
