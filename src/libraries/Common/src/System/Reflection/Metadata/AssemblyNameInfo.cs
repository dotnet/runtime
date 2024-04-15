// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Reflection.Metadata
{
    [DebuggerDisplay("{FullName}")]
#if SYSTEM_PRIVATE_CORELIB
    internal
#else
    public
#endif
    sealed class AssemblyNameInfo : IEquatable<AssemblyNameInfo>
    {
        private string? _fullName;

#if !SYSTEM_PRIVATE_CORELIB
        public AssemblyNameInfo(string name, Version? version = null, string? cultureName = null, AssemblyNameFlags flags = AssemblyNameFlags.None, Collections.Immutable.ImmutableArray<byte> publicKeyOrToken = default)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Version = version;
            CultureName = cultureName;
            Flags = flags;
            PublicKeyOrToken = publicKeyOrToken;
        }
#endif

        internal AssemblyNameInfo(AssemblyNameParser.AssemblyNameParts parts)
        {
            Name = parts._name;
            Version = parts._version;
            CultureName = parts._cultureName;
            Flags = parts._flags;
#if SYSTEM_PRIVATE_CORELIB
            PublicKeyOrToken = parts._publicKeyOrToken;
#else
            PublicKeyOrToken = parts._publicKeyOrToken is null ? default : parts._publicKeyOrToken.Length == 0
                ? Collections.Immutable.ImmutableArray<byte>.Empty
    #if NET8_0_OR_GREATER
                : Runtime.InteropServices.ImmutableCollectionsMarshal.AsImmutableArray(parts._publicKeyOrToken);
    #else
                : Collections.Immutable.ImmutableArray.Create(parts._publicKeyOrToken);
    #endif
#endif
        }

        public string Name { get; }
        public Version? Version { get; }
        public string? CultureName { get; }
        public AssemblyNameFlags Flags { get; }

#if SYSTEM_PRIVATE_CORELIB
        public byte[]? PublicKeyOrToken { get; }
#else
        public Collections.Immutable.ImmutableArray<byte> PublicKeyOrToken { get; }
#endif

        public string FullName
        {
            get
            {
                if (_fullName is null)
                {
                    byte[]? publicKeyToken = ((Flags & AssemblyNameFlags.PublicKey) != 0) ? null :
#if SYSTEM_PRIVATE_CORELIB
                    PublicKeyOrToken;
#elif NET8_0_OR_GREATER
                    !PublicKeyOrToken.IsDefault ? Runtime.InteropServices.ImmutableCollectionsMarshal.AsArray(PublicKeyOrToken) : null;
#else
                    ToArray(PublicKeyOrToken);
#endif
                    _fullName = AssemblyNameFormatter.ComputeDisplayName(Name, Version, CultureName, publicKeyToken);
                }

                return _fullName;
            }
        }

        public bool Equals(AssemblyNameInfo? other)
        {
            if (other is null || Flags != other.Flags || !Name.Equals(other.Name) || !string.Equals(CultureName, other.CultureName))
            {
                return false;
            }

            if (Version is null)
            {
                if (other.Version is not null)
                {
                    return false;
                }
            }
            else
            {
                if (!Version.Equals(other.Version))
                {
                    return false;
                }
            }

            return SequenceEqual(PublicKeyOrToken, other.PublicKeyOrToken);

#if SYSTEM_PRIVATE_CORELIB
            static bool SequenceEqual(byte[]? left, byte[]? right)
            {
                if (left is null)
                {
                    if (right is not null)
                    {
                        return false;
                    }
                }
                else if (right is null)
                {
                    return false;
                }
                else if (left.Length != right.Length)
                {
                    return false;
                }
                else
                {
                    for (int i = 0; i < left.Length; i++)
                    {
                        if (left[i] != right[i])
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
#else
            static bool SequenceEqual(Collections.Immutable.ImmutableArray<byte> left, Collections.Immutable.ImmutableArray<byte> right)
            {
                int leftLength = left.IsDefaultOrEmpty ? 0 : left.Length;
                int rightLength = right.IsDefaultOrEmpty ? 0 : right.Length;

                if (leftLength != rightLength)
                {
                    return false;
                }
                else if (leftLength > 0)
                {
                    for (int i = 0; i < leftLength; i++)
                    {
                        if (left[i] != right[i])
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
#endif
        }

        public override bool Equals(object? obj) => Equals(obj as AssemblyNameInfo);

        public override int GetHashCode()
        {
#if NETCOREAPP
            HashCode hashCode = default;
            hashCode.Add(Name);
            hashCode.Add(Flags);
            hashCode.Add(Version);

    #if SYSTEM_PRIVATE_CORELIB
            if (PublicKeyOrToken is not null)
            {
                hashCode.AddBytes(PublicKeyOrToken);
            }
    #else
            if (!PublicKeyOrToken.IsDefaultOrEmpty)
            {
                hashCode.AddBytes(Runtime.InteropServices.ImmutableCollectionsMarshal.AsArray(PublicKeyOrToken));
            }
    #endif
            return hashCode.ToHashCode();
#else
            return FullName.GetHashCode();
#endif
        }

        public AssemblyName ToAssemblyName()
        {
            AssemblyName assemblyName = new();
            assemblyName.Name = Name;
            assemblyName.CultureName = CultureName;
            assemblyName.Version = Version;

#if SYSTEM_PRIVATE_CORELIB
            assemblyName._flags = Flags;

            if (PublicKeyOrToken is not null)
            {
                if ((Flags & AssemblyNameFlags.PublicKey) != 0)
                {
                    assemblyName.SetPublicKey(PublicKeyOrToken);
                }
                else
                {
                    assemblyName.SetPublicKeyToken(PublicKeyOrToken);
                }
            }
#else
            assemblyName.Flags = Flags;

            if (!PublicKeyOrToken.IsDefault)
            {
                if ((Flags & AssemblyNameFlags.PublicKey) != 0)
                {
                    assemblyName.SetPublicKey(ToArray(PublicKeyOrToken));
                }
                else
                {
                    assemblyName.SetPublicKeyToken(ToArray(PublicKeyOrToken));
                }
            }
#endif

            return assemblyName;
        }

        /// <summary>
        /// Parses a span of characters into a assembly name.
        /// </summary>
        /// <param name="assemblyName">A span containing the characters representing the assembly name to parse.</param>
        /// <returns>Parsed type name.</returns>
        /// <exception cref="ArgumentException">Provided assembly name was invalid.</exception>
        public static AssemblyNameInfo Parse(ReadOnlySpan<char> assemblyName)
            => TryParse(assemblyName, out AssemblyNameInfo? result)
                ? result!
                : throw new ArgumentException("TODO_adsitnik_add_or_reuse_resource");

        /// <summary>
        /// Tries to parse a span of characters into an assembly name.
        /// </summary>
        /// <param name="assemblyName">A span containing the characters representing the assembly name to parse.</param>
        /// <param name="result">Contains the result when parsing succeeds.</param>
        /// <returns>true if assembly name was converted successfully, otherwise, false.</returns>
        public static bool TryParse(ReadOnlySpan<char> assemblyName,
#if SYSTEM_REFLECTION_METADATA || SYSTEM_PRIVATE_CORELIB // required by some tools that include this file but don't include the attribute
            [NotNullWhen(true)]
#endif
        out AssemblyNameInfo? result)
        {
            AssemblyNameParser.AssemblyNameParts parts = default;
            if (AssemblyNameParser.TryParse(assemblyName, ref parts))
            {
                result = new(parts);
                return true;
            }

            result = null;
            return false;
        }

#if !SYSTEM_PRIVATE_CORELIB
        private static byte[]? ToArray(Collections.Immutable.ImmutableArray<byte> input)
        {
            // not using System.Linq.ImmutableArrayExtensions.ToArray as TypeSystem does not allow System.Linq
            if (input.IsDefault)
            {
                return null;
            }
            else if (input.IsEmpty)
            {
                return Array.Empty<byte>();
            }

            byte[] result = new byte[input.Length];
            input.CopyTo(result, 0);
            return result;
        }
#endif
    }
}
