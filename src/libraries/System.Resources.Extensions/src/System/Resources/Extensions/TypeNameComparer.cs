// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics.Hashing;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Resources.Extensions
{
    /// <summary>
    /// Compares type names as strings, ignoring version.
    /// When type names are missing, mscorlib is assumed.
    /// This comparer is not meant to capture all scenarios (eg: TypeForwards)
    /// but is meant to serve as a best effort, avoiding false positives, in the
    /// absense of real type metadata.
    /// </summary>
    internal sealed class TypeNameComparer : IEqualityComparer<string>
    {
        public static TypeNameComparer Instance { get; } = new TypeNameComparer();

        // these match the set of whitespace characters allowed by the runtime's type parser
        private static readonly char[] s_whiteSpaceChars =
        {
            ' ', '\n', '\r', '\t'
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<char> ReadTypeName(ReadOnlySpan<char> assemblyQualifiedTypeName)
        {
            // the runtime doesn't tolerate anything between type name and comma
            int comma = assemblyQualifiedTypeName.IndexOf(',');

            return comma == -1 ? assemblyQualifiedTypeName : assemblyQualifiedTypeName.Slice(0, comma);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<char> ReadAssemblySimpleName(ReadOnlySpan<char> assemblyName)
        {
            int comma = assemblyName.IndexOf(',');

            return comma == -1 ? assemblyName : assemblyName.Slice(0, comma).TrimEnd(s_whiteSpaceChars);
        }

        private static bool IsMscorlib(ReadOnlySpan<char> assemblyName)
        {
            // to match IsMscorlib() in VM, which will ignore public key token and culture for corelib
            return assemblyName.Equals("mscorlib".AsSpan(), StringComparison.OrdinalIgnoreCase);
        }

        // Compare two type names ignoring version
        // If a type name is missing assembly, we assume it came from mscorlib
        // since this is what Type.GetType will do.
#pragma warning disable CS8767 // This API member has [AllowNull] implemented interface, but we don't want to accept nulls here.
        public bool Equals(string assemblyQualifiedTypeName1, string assemblyQualifiedTypeName2)
#pragma warning restore CS8767
        {
            if (assemblyQualifiedTypeName1 == null)
                throw new ArgumentNullException(nameof(assemblyQualifiedTypeName1));
            if (assemblyQualifiedTypeName2 == null)
                throw new ArgumentNullException(nameof(assemblyQualifiedTypeName2));

            if (ReferenceEquals(assemblyQualifiedTypeName1, assemblyQualifiedTypeName2))
                return true;

            ReadOnlySpan<char> typeSpan1 = assemblyQualifiedTypeName1.AsSpan().TrimStart(s_whiteSpaceChars);
            ReadOnlySpan<char> typeSpan2 = assemblyQualifiedTypeName2.AsSpan().TrimStart(s_whiteSpaceChars);

            // First, compare type names
            ReadOnlySpan<char> type1 = ReadTypeName(typeSpan1);
            ReadOnlySpan<char> type2 = ReadTypeName(typeSpan2);
            if (!type1.Equals(type2, StringComparison.Ordinal))
                return false;

            // skip separator and whitespace
            typeSpan1 = typeSpan1.Length > type1.Length ? typeSpan1.Slice(type1.Length + 1).TrimStart(s_whiteSpaceChars) : ReadOnlySpan<char>.Empty;
            typeSpan2 = typeSpan2.Length > type2.Length ? typeSpan2.Slice(type2.Length + 1).TrimStart(s_whiteSpaceChars) : ReadOnlySpan<char>.Empty;

            // Now, compare assembly simple names ignoring case
            ReadOnlySpan<char> simpleName1 = ReadAssemblySimpleName(typeSpan1);
            ReadOnlySpan<char> simpleName2 = ReadAssemblySimpleName(typeSpan2);

            // Don't allow assembly name without simple name portion
            if (simpleName1.IsEmpty && !typeSpan1.IsEmpty ||
                simpleName2.IsEmpty && !typeSpan2.IsEmpty)
                return false;

            // if both are missing assembly name, or either is missing
            // assembly name and the other is mscorlib
            if (simpleName1.IsEmpty)
                return (simpleName2.IsEmpty || IsMscorlib(simpleName2));
            if (simpleName2.IsEmpty)
                return IsMscorlib(simpleName1);

            if (!simpleName1.Equals(simpleName2, StringComparison.OrdinalIgnoreCase))
                return false;

            // both are mscorlib, ignore culture and key
            if (IsMscorlib(simpleName1))
                return true;

            // both have a matching assembly name parse it to get remaining portions
            // to compare culture and public key token
            // the following results in allocations.
            AssemblyName an1 = new AssemblyName(typeSpan1.ToString());
            AssemblyName an2 = new AssemblyName(typeSpan2.ToString());

            if (an1.CultureInfo?.LCID != an2.CultureInfo?.LCID)
                return false;

            byte[]? pkt1 = an1.GetPublicKeyToken();
            byte[]? pkt2 = an2.GetPublicKeyToken();
            return pkt1.AsSpan().SequenceEqual(pkt2);
        }

        public int GetHashCode(string assemblyQualifiedTypeName)
        {
            // non-allocating GetHashCode that hashes the type name portion of the string
            ReadOnlySpan<char> typeSpan = assemblyQualifiedTypeName.AsSpan().TrimStart(s_whiteSpaceChars);
            ReadOnlySpan<char> typeName = ReadTypeName(typeSpan);

            int hashCode = 0;
            for (int i = 0; i < typeName.Length; i++)
            {
                hashCode = HashHelpers.Combine(hashCode, typeName[i].GetHashCode());
            }

            return hashCode;
        }
    }
}
