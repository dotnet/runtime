// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.TypeLoading
{
    //
    // This is a private assembly name abstraction that's more suitable for use as keys in our caches.
    //
    //  - Immutable, unlike the public AssemblyName
    //  - Has a useful Equals() override, unlike the public AssemblyName.
    //  - Implements IEquatable<> so dictionaries avoid the casting Equals()
    //  - Restricts itself to being an assembly name. Not an assembly name plus all kind of random info that someone
    //    found convenient to thumbtack onto it.
    //
    // We use this as our internal interchange type and only convert to and from the public AssemblyName class at api boundaries.
    //

    internal sealed class RoAssemblyName : IEquatable<RoAssemblyName>
    {
        public string Name { get; }
        public Version Version { get; }
        public string CultureName { get; }
        public byte[] PublicKeyToken;

        // We store the flags to support "Retargetable".
        // The only flag allowed in an ECMA-335 AssemblyReference is the "PublicKey" bit. Since
        // RoAssemblyName always normalizes to the short form public key token, that bit would always be 0.
        public AssemblyNameFlags Flags { get; }

        private static readonly Version s_Version0000 = new Version(0, 0, 0, 0);

        public RoAssemblyName(string? name, Version? version, string? cultureName, byte[]? publicKeyToken, AssemblyNameFlags flags)
        {
            // We forcefully normalize the representation so that Equality is dependable and fast.
            Debug.Assert(name != null);

            Name = name;
            Version = version ?? s_Version0000;
            CultureName = cultureName ?? string.Empty;
            PublicKeyToken = publicKeyToken ?? Array.Empty<byte>();
            Flags = flags;
        }

        public string FullName => ToAssemblyName().FullName;

        // Equality - this compares every bit of data in the RuntimeAssemblyName which is acceptable for use as keys in a cache
        // where semantic duplication is permissible. This method is *not* meant to define ref->def binding rules or
        // assembly binding unification rules.
        public bool Equals(RoAssemblyName? other)
        {
            Debug.Assert(other is not null);
            if (Name != other.Name)
                return false;
            if (Version != other.Version)
                return false;
            if (CultureName != other.CultureName)
                return false;
            if (!(((ReadOnlySpan<byte>)PublicKeyToken).SequenceEqual(other.PublicKeyToken)))
                return false;

            // Do not compare Flags; we do not want to treat AssemblyNames as not being equal due to Flags.

            return true;
        }

        public sealed override bool Equals([NotNullWhen(true)] object? obj) => obj is RoAssemblyName other && Equals(other);
        public sealed override int GetHashCode() => Name.GetHashCode();
        public sealed override string ToString() => FullName;

        public AssemblyName ToAssemblyName()
        {
            AssemblyName an = new AssemblyName()
            {
                Name = Name,
                Version = Version,
                CultureName = CultureName,
                Flags = Flags,
            };

            // We must not hand out our own copy of the PKT to AssemblyName as AssemblyName is amazingly trusting and gives untrusted callers
            // full freedom to scribble on its PKT array. (As do we but we only have trusted callers!)
            an.SetPublicKeyToken(PublicKeyToken.CloneArray());
            return an;
        }
    }
}
