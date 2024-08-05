// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

#if !SYSTEM_PRIVATE_CORELIB
using System.Collections.Immutable;
using System.Linq;
#endif

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Describes an assembly.
    /// </summary>
    /// <remarks>
    /// It's a more lightweight, immutable version of <seealso cref="AssemblyName"/> that does not pre-allocate <seealso cref="System.Globalization.CultureInfo"/> instances.
    /// </remarks>
    [DebuggerDisplay("{FullName}")]
#if SYSTEM_REFLECTION_METADATA
    public
#else
    internal
#endif
    sealed class AssemblyNameInfo
    {
        internal readonly AssemblyNameFlags _flags;
        private string? _fullName;

#if !SYSTEM_PRIVATE_CORELIB
        /// <summary>
        /// Initializes a new instance of the AssemblyNameInfo class.
        /// </summary>
        /// <param name="name">The simple name of the assembly.</param>
        /// <param name="version">The version of the assembly.</param>
        /// <param name="cultureName">The name of the culture associated with the assembly.</param>
        /// <param name="flags">The attributes of the assembly.</param>
        /// <param name="publicKeyOrToken">The public key or its token. Set <paramref name="flags"/> to <seealso cref="AssemblyNameFlags.PublicKey"/> when it's public key.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
        public AssemblyNameInfo(string name, Version? version = null, string? cultureName = null, AssemblyNameFlags flags = AssemblyNameFlags.None, ImmutableArray<byte> publicKeyOrToken = default)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Version = version;
            CultureName = cultureName;
            _flags = flags;
            PublicKeyOrToken = publicKeyOrToken;
        }
#endif

        internal AssemblyNameInfo(AssemblyNameParser.AssemblyNameParts parts)
        {
            Name = parts._name;
            Version = parts._version;
            CultureName = parts._cultureName;
            _flags = parts._flags;
#if SYSTEM_PRIVATE_CORELIB
            PublicKeyOrToken = parts._publicKeyOrToken;
#else
            PublicKeyOrToken = parts._publicKeyOrToken is null ? default : parts._publicKeyOrToken.Length == 0
                ? ImmutableArray<byte>.Empty
    #if NET8_0_OR_GREATER
                : Runtime.InteropServices.ImmutableCollectionsMarshal.AsImmutableArray(parts._publicKeyOrToken);
    #else
                : ImmutableArray.Create(parts._publicKeyOrToken);
    #endif
#endif
        }

        /// <summary>
        /// Gets the simple name of the assembly.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the version of the assembly.
        /// </summary>
        public Version? Version { get; }

        /// <summary>
        /// Gets the name of the culture associated with the assembly.
        /// </summary>
        public string? CultureName { get; }

        /// <summary>
        /// Gets the attributes of the assembly.
        /// </summary>
        public AssemblyNameFlags Flags => _flags;

        /// <summary>
        /// Gets the public key or the public key token of the assembly.
        /// </summary>
        /// <remarks>Check <seealso cref="Flags"/> for <seealso cref="AssemblyNameFlags.PublicKey"/> flag to see whether it's public key or its token.</remarks>
#if SYSTEM_PRIVATE_CORELIB
        public byte[]? PublicKeyOrToken { get; }
#else
        public ImmutableArray<byte> PublicKeyOrToken { get; }
#endif

        /// <summary>
        /// Gets the full name of the assembly, also known as the display name.
        /// </summary>
        /// <remarks>In contrary to <seealso cref="AssemblyName.FullName"/> it does not validate public key token neither computes it based on the provided public key.</remarks>
        public string FullName
        {
            get
            {
                if (_fullName is null)
                {
                    bool isPublicKey = (Flags & AssemblyNameFlags.PublicKey) != 0;

                    byte[]? publicKeyOrToken =
#if SYSTEM_PRIVATE_CORELIB
                    PublicKeyOrToken;
#elif NET8_0_OR_GREATER
                    !PublicKeyOrToken.IsDefault ? Runtime.InteropServices.ImmutableCollectionsMarshal.AsArray(PublicKeyOrToken) : null;
#else
                    !PublicKeyOrToken.IsDefault ? PublicKeyOrToken.ToArray() : null;
#endif
                    _fullName = AssemblyNameFormatter.ComputeDisplayName(Name, Version, CultureName,
                        pkt: isPublicKey ? null : publicKeyOrToken,
                        ExtractAssemblyNameFlags(_flags), ExtractAssemblyContentType(_flags),
                        pk: isPublicKey ? publicKeyOrToken : null);
                }

                return _fullName;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <seealso cref="AssemblyName"/> class based on the stored information.
        /// </summary>
        public AssemblyName ToAssemblyName()
        {
            AssemblyName assemblyName = new();
            assemblyName.Name = Name;
            assemblyName.CultureName = CultureName;
            assemblyName.Version = Version;
            assemblyName.Flags = Flags;
            assemblyName.ContentType = ExtractAssemblyContentType(_flags);
#pragma warning disable SYSLIB0037 // Type or member is obsolete
            assemblyName.ProcessorArchitecture = ExtractProcessorArchitecture(_flags);
#pragma warning restore SYSLIB0037 // Type or member is obsolete

#if SYSTEM_PRIVATE_CORELIB
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
            if (!PublicKeyOrToken.IsDefault)
            {
                // A copy of the array needs to be created, as AssemblyName allows for the mutation of provided array.
                if ((Flags & AssemblyNameFlags.PublicKey) != 0)
                {
                    assemblyName.SetPublicKey(PublicKeyOrToken.ToArray());
                }
                else
                {
                    assemblyName.SetPublicKeyToken(PublicKeyOrToken.ToArray());
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
                : throw new ArgumentException(SR.InvalidAssemblyName, nameof(assemblyName));

        /// <summary>
        /// Tries to parse a span of characters into an assembly name.
        /// </summary>
        /// <param name="assemblyName">A span containing the characters representing the assembly name to parse.</param>
        /// <param name="result">Contains the result when parsing succeeds.</param>
        /// <returns>true if assembly name was converted successfully, otherwise, false.</returns>
        public static bool TryParse(ReadOnlySpan<char> assemblyName, [NotNullWhen(true)] out AssemblyNameInfo? result)
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

        internal static AssemblyNameFlags ExtractAssemblyNameFlags(AssemblyNameFlags combinedFlags)
            => combinedFlags & unchecked((AssemblyNameFlags)0xFFFFF10F);

        internal static AssemblyContentType ExtractAssemblyContentType(AssemblyNameFlags flags)
            => (AssemblyContentType)((((int)flags) >> 9) & 0x7);

        internal static ProcessorArchitecture ExtractProcessorArchitecture(AssemblyNameFlags flags)
            => (ProcessorArchitecture)((((int)flags) >> 4) & 0x7);
    }
}
