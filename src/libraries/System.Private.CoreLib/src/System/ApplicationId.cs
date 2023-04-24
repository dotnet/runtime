// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System
{
    public sealed class ApplicationId
    {
        private readonly byte[] _publicKeyToken;

        public ApplicationId(byte[] publicKeyToken, string name, Version version, string? processorArchitecture, string? culture)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentNullException.ThrowIfNull(version);
            ArgumentNullException.ThrowIfNull(publicKeyToken);

            _publicKeyToken = (byte[])publicKeyToken.Clone();
            Name = name;
            Version = version;
            ProcessorArchitecture = processorArchitecture;
            Culture = culture;
        }

        public string? Culture { get; }

        public string Name { get; }

        public string? ProcessorArchitecture { get; }

        public Version Version { get; }

        public byte[] PublicKeyToken => (byte[])_publicKeyToken.Clone();

        public ApplicationId Copy() => new ApplicationId(_publicKeyToken, Name, Version, ProcessorArchitecture, Culture);

        public override string ToString()
        {
            var sb = new ValueStringBuilder(stackalloc char[128]);
            sb.Append(Name);

            if (Culture != null)
            {
                sb.Append(", culture=\"");
                sb.Append(Culture);
                sb.Append('"');
            }

            sb.Append(", version=\"");
            sb.Append(Version.ToString());
            sb.Append('"');

            if (_publicKeyToken != null)
            {
                sb.Append(", publicKeyToken=\"");
                HexConverter.EncodeToUtf16(_publicKeyToken, sb.AppendSpan(2 * _publicKeyToken.Length), HexConverter.Casing.Upper);
                sb.Append('"');
            }

            if (ProcessorArchitecture != null)
            {
                sb.Append(", processorArchitecture =\"");
                sb.Append(ProcessorArchitecture);
                sb.Append('"');
            }

            return sb.ToString();
        }

        public override bool Equals([NotNullWhen(true)] object? o) =>
            o is ApplicationId other &&
            Equals(Name, other.Name) &&
            Equals(Version, other.Version) &&
            Equals(ProcessorArchitecture, other.ProcessorArchitecture) &&
            Equals(Culture, other.Culture) &&
            _publicKeyToken.AsSpan().SequenceEqual(other._publicKeyToken);

        public override int GetHashCode() =>
            // Note: purposely skipping publicKeyToken, processor architecture and culture as they
            // are less likely to make things not equal than name and version.
            Name.GetHashCode() ^ Version.GetHashCode();
    }
}
