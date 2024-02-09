// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata
{
    [DebuggerDisplay("{AssemblyQualifiedName}")]
#if SYSTEM_PRIVATE_CORELIB
    internal
#else
    public
#endif
    sealed class TypeName
    {
        /// <summary>
        /// Positive value is array rank.
        /// Negative value is modifier encoded using constants defined in <see cref="TypeNameParserHelpers"/>.
        /// </summary>
        private readonly int _rankOrModifier;
        private readonly TypeName[]? _genericArguments;
        private string? _assemblyQualifiedName;

        internal TypeName(string name, string fullName,
            AssemblyName? assemblyName,
            int rankOrModifier = default,
            TypeName? underlyingType = default,
            TypeName? containingType = default,
            TypeName[]? genericTypeArguments = default)
        {
            Name = name;
            FullName = fullName;
            AssemblyName = assemblyName;
            _rankOrModifier = rankOrModifier;
            UnderlyingType = underlyingType;
            ContainingType = containingType;
            _genericArguments = genericTypeArguments;
            TotalComplexity = GetTotalComplexity(underlyingType, containingType, genericTypeArguments);
        }

        /// <summary>
        /// The assembly-qualified name of the type; e.g., "System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089".
        /// </summary>
        /// <remarks>
        /// If <see cref="AssemblyName"/> is null, simply returns <see cref="FullName"/>.
        /// </remarks>
        public string AssemblyQualifiedName
            => _assemblyQualifiedName ??= AssemblyName is null ? FullName : $"{FullName}, {AssemblyName.FullName}";

        /// <summary>
        /// The assembly which contains this type, or null if this <see cref="TypeName"/> was not
        /// created from a fully-qualified name.
        /// </summary>
        public AssemblyName? AssemblyName { get; } // TODO: AssemblyName is mutable, are we fine with that? Does it not offer too much?

        /// <summary>
        /// If this type is a nested type (see <see cref="IsNestedType"/>), gets
        /// the containing type. If this type is not a nested type, returns null.
        /// </summary>
        /// <remarks>
        /// For example, given "Namespace.Containing+Nested", unwraps the outermost type and returns "Namespace.Containing".
        /// </remarks>
        public TypeName? ContainingType { get; }

        /// <summary>
        /// The full name of this type, including namespace, but without the assembly name; e.g., "System.Int32".
        /// Nested types are represented with a '+'; e.g., "MyNamespace.MyType+NestedType".
        /// </summary>
        /// <remarks>
        /// <para>For constructed generic types, the type arguments will be listed using their fully qualified
        /// names. For example, given "List&lt;int&gt;", the <see cref="FullName"/> property will return
        /// "System.Collections.Generic.List`1[[System.Int32, mscorlib, ...]]".</para>
        /// <para>For open generic types, the convention is to use a backtick ("`") followed by
        /// the arity of the generic type. For example, given "Dictionary&lt;,&gt;", the <see cref="FullName"/>
        /// property will return "System.Collections.Generic.Dictionary`2". Given "Dictionary&lt;,&gt;.Enumerator",
        /// the <see cref="FullName"/> property will return "System.Collections.Generic.Dictionary`2+Enumerator".
        /// See ECMA-335, Sec. I.10.7.2 (Type names and arity encoding) for more information.</para>
        /// </remarks>
        public string FullName { get; }

        /// <summary>
        /// Returns true if this type represents any kind of array, regardless of the array's
        /// rank or its bounds.
        /// </summary>
        public bool IsArray => _rankOrModifier == TypeNameParserHelpers.SZArray || _rankOrModifier > 0;

        /// <summary>
        /// Returns true if this type represents a constructed generic type (e.g., "List&lt;int&gt;").
        /// </summary>
        /// <remarks>
        /// Returns false for open generic types (e.g., "Dictionary&lt;,&gt;").
        /// </remarks>
        public bool IsConstructedGenericType => _genericArguments is not null;

        /// <summary>
        /// Returns true if this is a "plain" type; that is, not an array, not a pointer, and
        /// not a constructed generic type. Examples of elemental types are "System.Int32",
        /// "System.Uri", and "YourNamespace.YourClass".
        /// </summary>
        /// <remarks>
        /// <para>This property returning true doesn't mean that the type is a primitive like string
        /// or int; it just means that there's no underlying type (<see cref="UnderlyingType"/> returns null).</para>
        /// <para>This property will return true for generic type definitions (e.g., "Dictionary&lt;,&gt;").
        /// This is because determining whether a type truly is a generic type requires loading the type
        /// and performing a runtime check.</para>
        /// </remarks>
        public bool IsElementalType => UnderlyingType is null;

        /// <summary>
        /// Returns true if this is a managed pointer type (e.g., "ref int").
        /// Managed pointer types are sometimes called byref types (<seealso cref="Type.IsByRef"/>)
        /// </summary>
        public bool IsManagedPointerType => _rankOrModifier == TypeNameParserHelpers.ByRef; // name inconsistent with Type.IsByRef

        /// <summary>
        /// Returns true if this is a nested type (e.g., "Namespace.Containing+Nested").
        /// For nested types <seealso cref="ContainingType"/> returns their containing type.
        /// </summary>
        public bool IsNestedType => ContainingType is not null;

        /// <summary>
        /// Returns true if this type represents a single-dimensional, zero-indexed array (e.g., "int[]").
        /// </summary>
        public bool IsSzArrayType => _rankOrModifier == TypeNameParserHelpers.SZArray; // name could be more user-friendly

        /// <summary>
        /// Returns true if this type represents an unmanaged pointer (e.g., "int*" or "void*").
        /// Unmanaged pointer types are often just called pointers (<seealso cref="Type.IsPointer"/>)
        /// </summary>
        public bool IsUnmanagedPointerType => _rankOrModifier == TypeNameParserHelpers.Pointer; // name inconsistent with Type.IsPointer

        /// <summary>
        /// Returns true if this type represents a variable-bound array; that is, an array of rank greater
        /// than 1 (e.g., "int[,]") or a single-dimensional array which isn't necessarily zero-indexed.
        /// </summary>
        public bool IsVariableBoundArrayType => _rankOrModifier > 1;

        /// <summary>
        /// The name of this type, without the namespace and the assembly name; e.g., "Int32".
        /// Nested types are represented without a '+'; e.g., "MyNamespace.MyType+NestedType" is just "NestedType".
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Represents the total amount of work that needs to be performed to fully inspect
        /// this instance, including any generic arguments or underlying types.
        /// </summary>
        /// <remarks>
        /// <para>There's not really a parallel concept to this in reflection. Think of it
        /// as the total number of <see cref="TypeName"/> instances that would be created if
        /// you were to totally deconstruct this instance and visit each intermediate <see cref="TypeName"/>
        /// that occurs as part of deconstruction.</para>
        /// <para>"int" and "Person" each have complexities of 1 because they're standalone types.</para>
        /// <para>"int[]" has a complexity of 2 because to fully inspect it involves inspecting the
        /// array type itself, <em>plus</em> unwrapping the underlying type ("int") and inspecting that.</para>
        /// <para>
        /// "Dictionary&lt;string, List&lt;int[][]&gt;&gt;" has complexity 8 because fully visiting it
        /// involves inspecting 8 <see cref="TypeName"/> instances total:
        /// <list type="bullet">
        /// <item>Dictionary&lt;string, List&lt;int[][]&gt;&gt; (the original type)</item>
        /// <item>Dictionary`2 (the generic type definition)</item>
        /// <item>string (a type argument of Dictionary)</item>
        /// <item>List&lt;int[][]&gt; (a type argument of Dictionary)</item>
        /// <item>List`1 (the generic type definition)</item>
        /// <item>int[][] (a type argument of List)</item>
        /// <item>int[] (the underlying type of int[][])</item>
        /// <item>int (the underlying type of int[])</item>
        /// </list>
        /// </para>
        /// </remarks>
        public int TotalComplexity { get; }

        /// <summary>
        /// If this type is not an elemental type (see <see cref="IsElementalType"/>), gets
        /// the underlying type. If this type is an elemental type, returns null.
        /// </summary>
        /// <remarks>
        /// For example, given "int[][]", unwraps the outermost array and returns "int[]".
        /// Given "Dictionary&lt;string, int&gt;", returns the generic type definition "Dictionary&lt;,&gt;".
        /// </remarks>
        public TypeName? UnderlyingType { get; }

        public static TypeName Parse(ReadOnlySpan<char> typeName, TypeNameParserOptions? options = default)
            => TypeNameParser.Parse(typeName, throwOnError: true, options)!;

        public static bool TryParse(ReadOnlySpan<char> typeName,
#if !INTERNAL_NULLABLE_ANNOTATIONS // remove along with the define from ILVerification.csproj when SystemReflectionMetadataVersion points to new version with the new types
            [NotNullWhen(true)]
#endif
        out TypeName? result, TypeNameParserOptions? options = default)
        {
            result = TypeNameParser.Parse(typeName, throwOnError: false, options);
            return result is not null;
        }

        public int GetArrayRank()
            => _rankOrModifier switch
            {
                TypeNameParserHelpers.SZArray => 1,
                _ when _rankOrModifier > 0 => _rankOrModifier,
                _ => throw new ArgumentException("SR.Argument_HasToBeArrayClass") // TODO: use actual resource (used by Type.GetArrayRank)
            };

        /// <summary>
        /// If this <see cref="TypeName"/> represents a constructed generic type, returns an array
        /// of all the generic arguments. Otherwise it returns an empty array.
        /// </summary>
        /// <remarks>
        /// <para>For example, given "Dictionary&lt;string, int&gt;", returns a 2-element array containing
        /// string and int.</para>
        /// <para>The caller controls the returned array and may mutate it freely.</para>
        /// </remarks>
        public TypeName[] GetGenericArguments()
            => _genericArguments is not null
                ? (TypeName[])_genericArguments.Clone() // we return a copy on purpose, to not allow for mutations. TODO: consider returning a ROS
                : Array.Empty<TypeName>(); // TODO: should we throw (Levi's parser throws InvalidOperationException in such case), Type.GetGenericArguments just returns an empty array

        private static int GetTotalComplexity(TypeName? underlyingType, TypeName? containingType, TypeName[]? genericTypeArguments)
        {
            int result = 1;

            if (underlyingType is not null)
            {
                result = checked(result + underlyingType.TotalComplexity);
            }

            if (containingType is not null)
            {
                result = checked(result + containingType.TotalComplexity);
            }

            if (genericTypeArguments is not null)
            {
                // New total complexity will be the sum of the cumulative args' complexity + 2:
                // - one for the generic type definition "MyGeneric`x"
                // - one for the constructed type definition "MyGeneric`x[[...]]"
                // - and the cumulative complexity of all the arguments
                foreach (TypeName genericArgument in genericTypeArguments)
                {
                    result = checked(result + genericArgument.TotalComplexity);
                }
            }

            return result;
        }
    }
}
