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
        private readonly AssemblyName? _assemblyName;
        private readonly TypeName? _underlyingType;
        private readonly TypeName? _declaringType;
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
            _assemblyName = assemblyName;
            _rankOrModifier = rankOrModifier;
            _underlyingType = underlyingType;
            _declaringType = containingType;
            _genericArguments = genericTypeArguments;

            Debug.Assert(!(IsArray || IsPointer || IsByRef) || _underlyingType is not null);
            Debug.Assert(_genericArguments is null || _underlyingType is not null);
        }

        /// <summary>
        /// The assembly-qualified name of the type; e.g., "System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089".
        /// </summary>
        /// <remarks>
        /// If <see cref="GetAssemblyName()"/> returns null, simply returns <see cref="FullName"/>.
        /// </remarks>
        public string AssemblyQualifiedName
            => _assemblyQualifiedName ??= _assemblyName is null ? FullName : $"{FullName}, {_assemblyName.FullName}";

        /// <summary>
        /// Returns the name of the assembly (not the full name>).
        /// </summary>
        /// <remarks>
        /// If <see cref="GetAssemblyName()"/> returns null, simply returns null.
        /// </remarks>
        public string? AssemblySimpleName => _assemblyName?.Name;

        /// <summary>
        /// If this type is a nested type (see <see cref="IsNested"/>), gets
        /// the declaring type. If this type is not a nested type, throws.
        /// </summary>
        /// <remarks>
        /// For example, given "Namespace.Declaring+Nested", unwraps the outermost type and returns "Namespace.Declaring".
        /// </remarks>
        public TypeName DeclaringType => _declaringType is not null ? _declaringType : throw new InvalidOperationException();

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
        /// Returns true if this is a "plain" type; that is, not an array, not a pointer, not a reference, and
        /// not a constructed generic type. Examples of elemental types are "System.Int32",
        /// "System.Uri", and "YourNamespace.YourClass".
        /// </summary>
        /// <remarks>
        /// <para>This property returning true doesn't mean that the type is a primitive like string
        /// or int; it just means that there's no underlying type.</para>
        /// <para>This property will return true for generic type definitions (e.g., "Dictionary&lt;,&gt;").
        /// This is because determining whether a type truly is a generic type requires loading the type
        /// and performing a runtime check.</para>
        /// </remarks>
        public bool IsSimple => _underlyingType is null;

        /// <summary>
        /// Returns true if this is a managed pointer type (e.g., "ref int").
        /// Managed pointer types are sometimes called byref types (<seealso cref="Type.IsByRef"/>)
        /// </summary>
        public bool IsByRef => _rankOrModifier == TypeNameParserHelpers.ByRef;

        /// <summary>
        /// Returns true if this is a nested type (e.g., "Namespace.Declaring+Nested").
        /// For nested types <seealso cref="DeclaringType"/> returns their declaring type.
        /// </summary>
        public bool IsNested => _declaringType is not null;

        /// <summary>
        /// Returns true if this type represents a single-dimensional, zero-indexed array (e.g., "int[]").
        /// </summary>
        public bool IsSZArray => _rankOrModifier == TypeNameParserHelpers.SZArray;

        /// <summary>
        /// Returns true if this type represents an unmanaged pointer (e.g., "int*" or "void*").
        /// Unmanaged pointer types are often just called pointers (<seealso cref="Type.IsPointer"/>)
        /// </summary>
        public bool IsPointer => _rankOrModifier == TypeNameParserHelpers.Pointer;

        /// <summary>
        /// Returns true if this type represents a variable-bound array; that is, an array of rank greater
        /// than 1 (e.g., "int[,]") or a single-dimensional array which isn't necessarily zero-indexed.
        /// </summary>
        public bool IsVariableBoundArrayType => _rankOrModifier >= 1;

        /// <summary>
        /// The name of this type, without the namespace and the assembly name; e.g., "Int32".
        /// Nested types are represented without a '+'; e.g., "MyNamespace.MyType+NestedType" is just "NestedType".
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Represents the total number of <see cref="TypeName"/> instances that are used to describe
        /// this instance, including any generic arguments or underlying types.
        /// </summary>
        /// <remarks>
        /// <para>This value is computed every time this method gets called, it's not cached.</para>
        /// <para>There's not really a parallel concept to this in reflection. Think of it
        /// as the total number of <see cref="TypeName"/> instances that would be created if
        /// you were to totally deconstruct this instance and visit each intermediate <see cref="TypeName"/>
        /// that occurs as part of deconstruction.</para>
        /// <para>"int" and "Person" each have complexities of 1 because they're standalone types.</para>
        /// <para>"int[]" has a node count of 2 because to fully inspect it involves inspecting the
        /// array type itself, <em>plus</em> unwrapping the underlying type ("int") and inspecting that.</para>
        /// <para>
        /// "Dictionary&lt;string, List&lt;int[][]&gt;&gt;" has node count 8 because fully visiting it
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
        public int GetNodeCount()
        {
            int result = 1;

            if (_underlyingType is not null)
            {
                result = checked(result + _underlyingType.GetNodeCount());
            }

            if (_declaringType is not null)
            {
                result = checked(result + _declaringType.GetNodeCount());
            }

            if (_genericArguments is not null)
            {
                foreach (TypeName genericArgument in _genericArguments)
                {
                    result = checked(result + genericArgument.GetNodeCount());
                }
            }

            return result;
        }

        /// <summary>
        /// The TypeName of the object encompassed or referred to by the current array, pointer, or reference type.
        /// </summary>
        /// <remarks>
        /// For example, given "int[][]", unwraps the outermost array and returns "int[]".
        /// </remarks>
        public TypeName GetElementType()
            => IsArray || IsPointer || IsByRef
                ? _underlyingType!
                : throw new InvalidOperationException();

        /// <summary>
        /// Returns a TypeName object that represents a generic type name definition from which the current generic type name can be constructed.
        /// </summary>
        /// <remarks>
        /// Given "Dictionary&lt;string, int&gt;", returns the generic type definition "Dictionary&lt;,&gt;".
        /// </remarks>
        public TypeName GetGenericTypeDefinition()
            => IsConstructedGenericType
                ? _underlyingType!
                : throw new InvalidOperationException("SR.InvalidOperation_NotGenericType"); // TODO: use actual resource

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
        /// Returns assembly name which contains this type, or null if this <see cref="TypeName"/> was not
        /// created from a fully-qualified name.
        /// </summary>
        /// <remarks>Since <seealso cref="AssemblyName"/> is mutable, this method returns a copy of it.</remarks>
        public AssemblyName? GetAssemblyName()
        {
            if (_assemblyName is null)
            {
                return null;
            }

#if SYSTEM_PRIVATE_CORELIB
            return _assemblyName; // no need for a copy in CoreLib
#else
            return (AssemblyName)_assemblyName.Clone();
#endif
        }

        /// <summary>
        /// If this <see cref="TypeName"/> represents a constructed generic type, returns a buffer
        /// of all the generic arguments. Otherwise it returns an empty buffer.
        /// </summary>
        /// <remarks>
        /// <para>For example, given "Dictionary&lt;string, int&gt;", returns a 2-element span containing
        /// string and int.</para>
        /// </remarks>
#if SYSTEM_PRIVATE_CORELIB
        public TypeName[] GetGenericArguments() => _genericArguments ?? Array.Empty<TypeName>();
#else
        public Collections.Immutable.ImmutableArray<TypeName> GetGenericArguments()
            => _genericArguments is null ? Collections.Immutable.ImmutableArray<TypeName>.Empty :
#if NET8_0_OR_GREATER
            Runtime.InteropServices.ImmutableCollectionsMarshal.AsImmutableArray(_genericArguments);
#else
            Collections.Immutable.ImmutableArray.Create(_genericArguments);
    #endif
#endif
    }
}
