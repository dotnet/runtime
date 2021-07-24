// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Reflection.TypeLoading
{
    internal static class Helpers
    {
        [return: NotNullIfNotNull("original")]
        public static T[]? CloneArray<T>(this T[]? original)
        {
            if (original == null)
                return null;

            if (original.Length == 0)
                return Array.Empty<T>();

            // We want to return the exact type of T[] even if "original" is a type of T2[] (due to array variance.)
            // The arrays produced by this helper are usually passed directly to app code.
            T[] copy = new T[original.Length];
            Array.Copy(sourceArray: original, sourceIndex: 0, destinationArray: copy, destinationIndex: 0, length: original.Length);
            return copy;
        }

        public static ReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> enumeration)
        {
            // todo: use IEnumerable<T> extension: return new ReadOnlyCollection<T>(enumeration.ToArray());
            List<T> list = new List<T>(enumeration);
            return new ReadOnlyCollection<T>(list.ToArray());
        }

        public static int GetTokenRowNumber(this int token) => token & 0x00ffffff;

        public static RoMethod? FilterInheritedAccessor(this RoMethod accessor)
        {
            if (accessor.ReflectedType == accessor.DeclaringType)
                return accessor;

            if (accessor.IsPrivate)
                return null;

            // If the accessor is virtual, .NET Framework tries to look for a overriding member starting from ReflectedType - a situation
            // which probably isn't expressible in any known language. Detecting overrides veers into vtable-building business which
            // is something this library tries to avoid. If anyone ever cares about this, welcome to fix.

            return accessor;
        }

        public static MethodInfo? FilterAccessor(this MethodInfo accessor, bool nonPublic)
        {
            if (nonPublic)
                return accessor;
            if (accessor.IsPublic)
                return accessor;
            return null;
        }

        public static string ComputeArraySuffix(int rank, bool multiDim)
        {
            Debug.Assert(rank == 1 || multiDim);

            if (!multiDim)
                return "[]";
            if (rank == 1)
                return "[*]";
            return "[" + new string(',', rank - 1) + "]";
        }

        // Escape identifiers as described in "Specifying Fully Qualified Type Names" on msdn.
        // Current link is http://msdn.microsoft.com/en-us/library/yfsftwz6(v=vs.110).aspx
        public static string EscapeTypeNameIdentifier(this string identifier)
        {
            // Some characters in a type name need to be escaped
            if (identifier.IndexOfAny(s_charsToEscape) != -1)
            {
                StringBuilder sbEscapedName = new StringBuilder(identifier.Length);
                foreach (char c in identifier)
                {
                    if (c.NeedsEscapingInTypeName())
                        sbEscapedName.Append('\\');

                    sbEscapedName.Append(c);
                }
                identifier = sbEscapedName.ToString();
            }
            return identifier;
        }

        public static bool TypeNameContainsTypeParserMetacharacters(this string identifier)
        {
            return identifier.IndexOfAny(s_charsToEscape) != -1;
        }

        public static bool NeedsEscapingInTypeName(this char c)
        {
            return Array.IndexOf(s_charsToEscape, c) >= 0;
        }

        public static string UnescapeTypeNameIdentifier(this string identifier)
        {
            if (identifier.IndexOf('\\') != -1)
            {
                StringBuilder sbUnescapedName = new StringBuilder(identifier.Length);
                for (int i = 0; i < identifier.Length; i++)
                {
                    if (identifier[i] == '\\')
                    {
                        // If we have a trailing '\\', the framework somehow messed up escaping the original identifier. Since that's
                        // unlikely to happen and unactionable, we'll just let the next line IndexOutOfRange if that happens.
                        i++;
                    }
                    sbUnescapedName.Append(identifier[i]);
                }
                identifier = sbUnescapedName.ToString();
            }
            return identifier;
        }

        private static readonly char[] s_charsToEscape = new char[] { '\\', '[', ']', '+', '*', '&', ',' };

        /// <summary>
        /// For AssemblyReferences, convert "unspecified" components from the ECMA format (0xffff) to the in-memory System.Version format (0xffffffff).
        /// </summary>
        public static Version? AdjustForUnspecifiedVersionComponents(this Version v)
        {
            int mask =
                ((v.Revision == ushort.MaxValue) ? 0b0001 : 0) |
                ((v.Build == ushort.MaxValue) ? 0b0010 : 0) |
                ((v.Minor == ushort.MaxValue) ? 0b0100 : 0) |
                ((v.Major == ushort.MaxValue) ? 0b1000 : 0);

            return mask switch
            {
                0b0000 => v,
                0b0001 => new Version(v.Major, v.Minor, v.Build),
                0b0010 => new Version(v.Major, v.Minor),
                0b0011 => new Version(v.Major, v.Minor),
                _ => null,
            };
        }

        public static byte[]? ComputePublicKeyToken(this byte[] pkt)
        {
            // @TODO - https://github.com/dotnet/corefxlab/issues/2447 - This is not the best way to compute the PKT as AssemblyName
            // throws if the PK isn't a valid PK blob. That's not something we should block a metadata inspection tool for so we
            // should compute the PKT ourselves as soon as we can convince the libraries analyzers to let us use SHA1.
            AssemblyName an = new AssemblyName();
            an.SetPublicKey(pkt);
            return an.GetPublicKeyToken();
        }

        public static AssemblyNameFlags ConvertAssemblyFlagsToAssemblyNameFlags(AssemblyFlags assemblyFlags)
        {
            AssemblyNameFlags assemblyNameFlags = AssemblyNameFlags.None;

            if ((assemblyFlags & AssemblyFlags.Retargetable) != 0)
            {
                assemblyNameFlags |= AssemblyNameFlags.Retargetable;
            }

            return assemblyNameFlags;
        }

        //
        // Note that for a top level type, the resulting ns is string.Empty, *not* null.
        // This is a concession to the fact that MetadataReader's fast String equal methods
        // don't accept null.
        //
        public static void SplitTypeName(this string fullName, out string ns, out string name)
        {
            Debug.Assert(fullName != null);

            int indexOfLastDot = fullName.LastIndexOf('.');
            if (indexOfLastDot == -1)
            {
                ns = string.Empty;
                name = fullName;
            }
            else
            {
                ns = fullName.Substring(0, indexOfLastDot);
                name = fullName.Substring(indexOfLastDot + 1);
            }
        }

        //
        // Rejoin a namespace and type name back into a full name.
        //
        // Note that for a top level type, the namespace is string.Empty, *not* null (as Reflection surfaces it.)
        // This is a concession to the fact that MetadataReader's fast String equal methods don't accept null.
        //
        public static string AppendTypeName(this string ns, string name)
        {
            Debug.Assert(ns != null, "For top level types, the namespace must be string.Empty, not null");
            Debug.Assert(name != null);

            return ns.Length == 0 ? name : ns + "." + name;
        }

        //
        // Common helper for ConstructorInfo.ToString() and MethodInfo.ToString()
        //
        public static string ToString(this IRoMethodBase roMethodBase, MethodSig<string> methodSigStrings)
        {
            TypeContext typeContext = roMethodBase.TypeContext;

            StringBuilder sb = new StringBuilder();
            sb.Append(methodSigStrings[-1]);
            sb.Append(' ');
            sb.Append(roMethodBase.MethodBase.Name);

            Type[]? genericMethodArguments = typeContext.GenericMethodArguments;
            int count = genericMethodArguments == null ? 0 : genericMethodArguments.Length;
            if (count != 0)
            {
                sb.Append('[');
                for (int gpi = 0; gpi < count; gpi++)
                {
                    if (gpi != 0)
                        sb.Append(',');
                    sb.Append(genericMethodArguments![gpi].ToString());
                }
                sb.Append(']');
            }

            sb.Append('(');
            for (int position = 0; position < methodSigStrings.Parameters.Length; position++)
            {
                if (position != 0)
                    sb.Append(", ");
                sb.Append(methodSigStrings[position]);
            }
            sb.Append(')');
            return sb.ToString();
        }

        public static bool HasSameMetadataDefinitionAsCore<M>(this M thisMember, MemberInfo other) where M : MemberInfo
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            // Ensure that "other" is one of our MemberInfo objects. Do this check before calling any methods on it!
            if (!(other is M))
                return false;

            if (thisMember.MetadataToken != other.MetadataToken)
                return false;

            if (!(thisMember.Module.Equals(other.Module)))
                return false;

            return true;
        }

        public static RoType? LoadTypeFromAssemblyQualifiedName(string name, RoAssembly defaultAssembly, bool ignoreCase, bool throwOnError)
        {
            if (!name.TypeNameContainsTypeParserMetacharacters())
            {
                // Fast-path: the type contains none of the parser metacharacters nor the escape character. Just treat as plain old type name.
                name.SplitTypeName(out string ns, out string simpleName);
                RoType? type = defaultAssembly.GetTypeCore(ns, simpleName, ignoreCase: ignoreCase, out Exception? e);
                if (type != null)
                    return type;
                if (throwOnError)
                    throw e!;
            }

            MetadataLoadContext loader = defaultAssembly.Loader;

            Func<AssemblyName, Assembly> assemblyResolver =
                delegate (AssemblyName assemblyName)
                {
                    return loader.LoadFromAssemblyName(assemblyName);
                };

            Func<Assembly?, string, bool, Type?> typeResolver =
                delegate (Assembly? assembly, string fullName, bool ignoreCase2)
                {
                    if (assembly == null)
                        assembly = defaultAssembly;

                    Debug.Assert(assembly is RoAssembly);
                    RoAssembly roAssembly = (RoAssembly)assembly;

                    fullName = fullName.UnescapeTypeNameIdentifier();
                    fullName.SplitTypeName(out string ns, out string simpleName);
                    Type? type = roAssembly.GetTypeCore(ns, simpleName, ignoreCase: ignoreCase2, out Exception? e);
                    if (type != null)
                        return type;
                    if (throwOnError)
                        throw e!;
                    return null;
                };

            return (RoType?)Type.GetType(name, assemblyResolver: assemblyResolver, typeResolver: typeResolver, throwOnError: throwOnError, ignoreCase: ignoreCase);
        }

        public static Type[] ExtractCustomModifiers(this RoType type, bool isRequired)
        {
            int count = 0;
            RoType walk = type;
            while (walk is RoModifiedType roModifiedType)
            {
                if (roModifiedType.IsRequired == isRequired)
                {
                    count++;
                }
                walk = roModifiedType.UnmodifiedType;
            }

            Type[] modifiers = new Type[count];
            walk = type;
            int index = count;
            while (walk is RoModifiedType roModifiedType)
            {
                if (roModifiedType.IsRequired == isRequired)
                {
                    modifiers[--index] = roModifiedType.Modifier;
                }
                walk = roModifiedType.UnmodifiedType;
            }
            Debug.Assert(index == 0);

            return modifiers;
        }

        public static RoType SkipTypeWrappers(this RoType type)
        {
            while (type is RoWrappedType roWrappedType)
            {
                type = roWrappedType.UnmodifiedType;
            }
            return type;
        }

        public static bool IsVisibleOutsideAssembly(this Type type)
        {
            TypeAttributes visibility = type.Attributes & TypeAttributes.VisibilityMask;
            if (visibility == TypeAttributes.Public)
                return true;

            if (visibility == TypeAttributes.NestedPublic)
                return type.DeclaringType!.IsVisibleOutsideAssembly();

            return false;
        }

        //
        // Converts an AssemblyName to a RoAssemblyName that is free from any future mutations on the AssemblyName.
        //
        public static RoAssemblyName ToRoAssemblyName(this AssemblyName assemblyName)
        {
            if (assemblyName.Name == null)
                throw new ArgumentException();

            // AssemblyName's PKT property getters do NOT copy the array before giving it out. Make our own copy
            // as the original is wide open to tampering by anyone.
            byte[]? pkt = assemblyName.GetPublicKeyToken().CloneArray();

            return new RoAssemblyName(assemblyName.Name, assemblyName.Version, assemblyName.CultureName, pkt, assemblyName.Flags);
        }

        public static byte[] ToUtf8(this string s) => Encoding.UTF8.GetBytes(s);

#if NETCOREAPP3_1_OR_GREATER
        public static string ToUtf16(this ReadOnlySpan<byte> utf8) => Encoding.UTF8.GetString(utf8);
#else
        public static unsafe string ToUtf16(this ReadOnlySpan<byte> utf8)
        {
            if (utf8.IsEmpty)
            {
                return string.Empty;
            }

            fixed (byte* ptr = utf8)
            {
                return Encoding.UTF8.GetString(ptr, utf8.Length);
            }
        }
#endif

        // Guards ToString() implementations. Sample usage:
        //
        //    public sealed override string ToString() => Loader.GetDisposedString() ?? <your real ToString() code>;"
        //
        public static string? GetDisposedString(this MetadataLoadContext loader) => loader.IsDisposed ? SR.MetadataLoadContextDisposed : null;

        public static TypeContext ToTypeContext(this RoType[] instantiation) => new TypeContext(instantiation, null);
    }
}
