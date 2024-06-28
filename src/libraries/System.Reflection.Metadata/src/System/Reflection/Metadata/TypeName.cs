// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

#if !SYSTEM_PRIVATE_CORELIB
using System.Collections.Immutable;
#endif

namespace System.Reflection.Metadata
{
    [DebuggerDisplay("{AssemblyQualifiedName}")]
#if SYSTEM_REFLECTION_METADATA
    public
#else
    internal
#endif
    sealed class TypeName
    {
        /// <summary>
        /// Positive value is array rank.
        /// Negative value is modifier encoded using constants defined in <see cref="TypeNameParserHelpers"/>.
        /// </summary>
        private readonly sbyte _rankOrModifier;
        /// <summary>
        /// To avoid the need of allocating a string for all declaring types (example: A+B+C+D+E+F+G),
        /// length of the name is stored and the fullName passed in ctor represents the full name of the nested type.
        /// So when the name is needed, a substring is being performed.
        /// </summary>
        private readonly int _nestedNameLength;
        private readonly TypeName? _elementOrGenericType;
        private readonly TypeName? _declaringType;
#if SYSTEM_PRIVATE_CORELIB
        private readonly List<TypeName>? _genericArguments;
#else
        private readonly ImmutableArray<TypeName> _genericArguments;
#endif
        private string? _name, _fullName, _assemblyQualifiedName;

        internal TypeName(string? fullName,
            AssemblyNameInfo? assemblyName,
            TypeName? elementOrGenericType = default,
            TypeName? declaringType = default,
#if SYSTEM_PRIVATE_CORELIB
            List<TypeName>? genericTypeArguments = default,
#else
            ImmutableArray<TypeName>.Builder? genericTypeArguments = default,
#endif
            sbyte rankOrModifier = default,
            int nestedNameLength = -1)
        {
            _fullName = fullName;
            AssemblyName = assemblyName;
            _rankOrModifier = rankOrModifier;
            _elementOrGenericType = elementOrGenericType;
            _declaringType = declaringType;
            _nestedNameLength = nestedNameLength;

#if SYSTEM_PRIVATE_CORELIB
            _genericArguments = genericTypeArguments;
#else
            _genericArguments = genericTypeArguments is null
                ? ImmutableArray<TypeName>.Empty
                : genericTypeArguments.Count == genericTypeArguments.Capacity ? genericTypeArguments.MoveToImmutable() : genericTypeArguments.ToImmutableArray();
#endif
        }

#if SYSTEM_REFLECTION_METADATA
        private TypeName(string? fullName,
            AssemblyNameInfo? assemblyName,
            TypeName? elementOrGenericType = default,
            TypeName? declaringType = default,
            ImmutableArray<TypeName> genericTypeArguments = default,
            sbyte rankOrModifier = default,
            int nestedNameLength = -1)
        {
            _fullName = fullName;
            AssemblyName = assemblyName;
            _elementOrGenericType = elementOrGenericType;
            _declaringType = declaringType;
            _genericArguments = genericTypeArguments;
            _rankOrModifier = rankOrModifier;
            _nestedNameLength = nestedNameLength;
        }
#endif

        /// <summary>
        /// The assembly-qualified name of the type; e.g., "System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089".
        /// </summary>
        /// <remarks>
        /// If <see cref="AssemblyName"/> returns null, simply returns <see cref="FullName"/>.
        /// </remarks>
        public string AssemblyQualifiedName
            => _assemblyQualifiedName ??= AssemblyName is null ? FullName : $"{FullName}, {AssemblyName.FullName}";

        /// <summary>
        /// Returns assembly name which contains this type, or null if this <see cref="TypeName"/> was not
        /// created from a fully-qualified name.
        /// </summary>
        public AssemblyNameInfo? AssemblyName { get; }

        /// <summary>
        /// If this type is a nested type (see <see cref="IsNested"/>), gets
        /// the declaring type. If this type is not a nested type, throws.
        /// </summary>
        /// <remarks>
        /// For example, given "Namespace.Declaring+Nested", unwraps the outermost type and returns "Namespace.Declaring".
        /// </remarks>
        /// <exception cref="InvalidOperationException">The current type is not a nested type.</exception>
        public TypeName DeclaringType
        {
            get
            {
                if (_declaringType is null)
                {
                    TypeNameParserHelpers.ThrowInvalidOperation_NotNestedType();
                }

                return _declaringType;
            }
        }

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
        public string FullName
        {
            get
            {
                if (_fullName is null)
                {
                    if (IsConstructedGenericType)
                    {
                        _fullName = TypeNameParserHelpers.GetGenericTypeFullName(GetGenericTypeDefinition().FullName.AsSpan(),
#if SYSTEM_PRIVATE_CORELIB
                            CollectionsMarshal.AsSpan(_genericArguments));
#else
                            _genericArguments.AsSpan());
#endif
                    }
                    else if (IsArray || IsPointer || IsByRef)
                    {
                        ValueStringBuilder builder = new(stackalloc char[128]);
                        builder.Append(GetElementType().FullName);
                        _fullName = TypeNameParserHelpers.GetRankOrModifierStringRepresentation(_rankOrModifier, ref builder);
                    }
                    else
                    {
                        Debug.Fail("Pre-allocated full name should have been provided in the ctor");
                    }
                }
                else if (_nestedNameLength > 0 && _fullName.Length > _nestedNameLength) // Declaring types
                {
                    // Stored fullName represents the full name of the nested type.
                    // Example: Namespace.Declaring+Nested
                    _fullName = _fullName.Substring(0, _nestedNameLength);
                }

                return _fullName!;
            }
        }

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
        public bool IsConstructedGenericType =>
#if SYSTEM_PRIVATE_CORELIB
            _genericArguments is not null;
#else
            _genericArguments.Length > 0;
#endif

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
        public bool IsSimple => _elementOrGenericType is null;

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
        public string Name
        {
            get
            {
                if (_name is null)
                {
                    if (IsConstructedGenericType)
                    {
                        _name = TypeNameParserHelpers.GetName(GetGenericTypeDefinition().FullName.AsSpan()).ToString();
                    }
                    else if (IsPointer || IsByRef || IsArray)
                    {
                        ValueStringBuilder builder = new(stackalloc char[64]);
                        builder.Append(GetElementType().Name);
                        _name = TypeNameParserHelpers.GetRankOrModifierStringRepresentation(_rankOrModifier, ref builder);
                    }
                    else if (_nestedNameLength > 0 && _fullName is not null)
                    {
                        _name = TypeNameParserHelpers.GetName(_fullName.AsSpan(0, _nestedNameLength)).ToString();
                    }
                    else
                    {
                        _name = TypeNameParserHelpers.GetName(FullName.AsSpan()).ToString();
                    }
                }

                return _name;
            }
        }

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

            if (IsNested)
            {
                result += DeclaringType.GetNodeCount();
            }
            else if (IsConstructedGenericType)
            {
                result++;
            }
            else if (IsArray || IsPointer || IsByRef)
            {
                result += GetElementType().GetNodeCount();
            }

            foreach (TypeName genericArgument in GetGenericArguments())
            {
                result += genericArgument.GetNodeCount();
            }

            return result;
        }

        /// <summary>
        /// The TypeName of the object encompassed or referred to by the current array, pointer, or reference type.
        /// </summary>
        /// <remarks>
        /// For example, given "int[][]", unwraps the outermost array and returns "int[]".
        /// </remarks>
        /// <exception cref="InvalidOperationException">The current type is not an array, pointer or reference.</exception>
        public TypeName GetElementType()
        {
            if (!(IsArray || IsPointer || IsByRef))
            {
                TypeNameParserHelpers.ThrowInvalidOperation_NoElement();
            }

            return _elementOrGenericType!;
        }

        /// <summary>
        /// Returns a TypeName object that represents a generic type name definition from which the current generic type name can be constructed.
        /// </summary>
        /// <remarks>
        /// Given "Dictionary&lt;string, int&gt;", returns the generic type definition "Dictionary&lt;,&gt;".
        /// </remarks>
        /// <exception cref="InvalidOperationException">The current type is not a generic type.</exception>
        public TypeName GetGenericTypeDefinition()
        {
            if (!IsConstructedGenericType)
            {
                TypeNameParserHelpers.ThrowInvalidOperation_NotGenericType();
            }

            return _elementOrGenericType!;
        }

        /// <summary>
        /// Parses a span of characters into a type name.
        /// </summary>
        /// <param name="typeName">A span containing the characters representing the type name to parse.</param>
        /// <param name="options">An object that describes optional <seealso cref="TypeNameParseOptions"/> parameters to use.</param>
        /// <returns>Parsed type name.</returns>
        /// <exception cref="ArgumentException">Provided type name was invalid.</exception>
        /// <exception cref="InvalidOperationException">Parsing has exceeded the limit set by <seealso cref="TypeNameParseOptions.MaxNodes"/>.</exception>
        public static TypeName Parse(ReadOnlySpan<char> typeName, TypeNameParseOptions? options = default)
            => TypeNameParser.Parse(typeName, throwOnError: true, options)!;

        /// <summary>
        /// Tries to parse a span of characters into a type name.
        /// </summary>
        /// <param name="typeName">A span containing the characters representing the type name to parse.</param>
        /// <param name="options">An object that describes optional <seealso cref="TypeNameParseOptions"/> parameters to use.</param>
        /// <param name="result">Contains the result when parsing succeeds.</param>
        /// <returns>true if type name was converted successfully, otherwise, false.</returns>
        public static bool TryParse(ReadOnlySpan<char> typeName, [NotNullWhen(true)] out TypeName? result, TypeNameParseOptions? options = default)
        {
            result = TypeNameParser.Parse(typeName, throwOnError: false, options);
            return result is not null;
        }

        /// <summary>
        /// Gets the number of dimensions in an array.
        /// </summary>
        /// <returns>An integer that contains the number of dimensions in the current type.</returns>
        /// <exception cref="InvalidOperationException">The current type is not an array.</exception>
        public int GetArrayRank()
        {
            if (!(_rankOrModifier == TypeNameParserHelpers.SZArray || _rankOrModifier > 0))
            {
                TypeNameParserHelpers.ThrowInvalidOperation_HasToBeArrayClass();
            }

            return _rankOrModifier == TypeNameParserHelpers.SZArray ? 1 : _rankOrModifier;
        }

        /// <summary>
        /// If this <see cref="TypeName"/> represents a constructed generic type, returns an array
        /// of all the generic arguments. Otherwise it returns an empty array.
        /// </summary>
        /// <remarks>
        /// <para>For example, given "Dictionary&lt;string, int&gt;", returns a 2-element array containing
        /// string and int.</para>
        /// </remarks>
        public
#if SYSTEM_PRIVATE_CORELIB
        ReadOnlySpan<TypeName> GetGenericArguments() => CollectionsMarshal.AsSpan(_genericArguments);
#else
        ImmutableArray<TypeName> GetGenericArguments() => _genericArguments;
#endif

#if SYSTEM_REFLECTION_METADATA
        /// <summary>
        /// Returns a <see cref="TypeName" /> object representing a one-dimensional array
        /// of the current type, with a lower bound of zero.
        /// </summary>
        /// <returns>
        /// A <see cref="TypeName" /> object representing a one-dimensional array
        /// of the current type, with a lower bound of zero.
        /// </returns>
        public TypeName MakeArrayTypeName() => MakeElementTypeName(TypeNameParserHelpers.SZArray);

        /// <summary>
        /// Returns a <see cref="TypeName" /> object representing an array of the current type,
        /// with the specified number of dimensions.
        /// </summary>
        /// <param name="rank">The number of dimensions for the array. This number must be more than zero and less than or equal to 32.</param>
        /// <returns>
        /// A <see cref="TypeName" /> object representing an array of the current type,
        /// with the specified number of dimensions.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">rank is invalid. For example, 0 or negative.</exception>
        public TypeName MakeArrayTypeName(int rank)
            => rank <= 0 || rank > 32
                ? throw new ArgumentOutOfRangeException(nameof(rank))
                : MakeElementTypeName((sbyte)rank);

        /// <summary>
        /// Returns a <see cref="TypeName" /> object that represents a pointer to the current type.
        /// </summary>
        /// <returns>
        /// A <see cref="TypeName" /> object that represents a pointer to the current type.
        /// </returns>
        public TypeName MakePointerTypeName() => MakeElementTypeName(TypeNameParserHelpers.Pointer);

        /// <summary>
        /// Returns a <see cref="TypeName" /> object that represents a managed reference to the current type.
        /// </summary>
        /// <returns>
        /// A <see cref="TypeName" /> object that represents a managed reference to the current type.
        /// </returns>
        public TypeName MakeByRefTypeName() => MakeElementTypeName(TypeNameParserHelpers.ByRef);

        /// <summary>
        /// Creates a new constructed generic type name.
        /// </summary>
        /// <param name="typeArguments">An array of type names to be used as generic arguments of the current simple type name.</param>
        /// <returns>
        /// A <see cref="TypeName" /> representing the constructed type name formed by using the elements
        /// of <paramref name="typeArguments"/> for the generic arguments of the current simple type name.
        /// </returns>
        /// <exception cref="InvalidOperationException">The current type name is not simple.</exception>
        public TypeName MakeGenericTypeName(ImmutableArray<TypeName> typeArguments)
            => IsSimple
                ? new TypeName(fullName: null, AssemblyName, elementOrGenericType: this, genericTypeArguments: typeArguments)
                : throw new InvalidOperationException(SR.Format(SR.Arg_NotSimpleTypeName, FullName));

        /// <summary>
        /// Returns a <see cref="TypeName" /> object that represents the current type name with provided assembly name.
        /// </summary>
        /// <returns>
        /// A <see cref="TypeName" /> object that represents the current type name with provided assembly name.
        /// </returns>
        public TypeName WithAssemblyName(AssemblyNameInfo? assemblyName)
            => WithAssemblyName(this, AssemblyName, assemblyName)!;

        private static TypeName? WithAssemblyName(TypeName? typeName, AssemblyNameInfo? oldName, AssemblyNameInfo? newName)
        {
            if (typeName is null)
            {
                return null;
            }
            else if (!ReferenceEquals(typeName.AssemblyName, oldName))
            {
                return typeName; // There is nothing to update.
            }

            return new TypeName(
                fullName: typeName._fullName,
                newName,
                elementOrGenericType: WithAssemblyName(typeName._elementOrGenericType, oldName, newName),
                declaringType: WithAssemblyName(typeName._declaringType, oldName, newName),
                // We don't update the assembly names of generic arguments on purpose!
                genericTypeArguments: typeName._genericArguments,
                rankOrModifier: typeName._rankOrModifier,
                nestedNameLength: typeName._nestedNameLength);
        }

        private TypeName MakeElementTypeName(sbyte rankOrModifier)
            => new TypeName(
                fullName: null,
                assemblyName: AssemblyName,
                elementOrGenericType: this,
                genericTypeArguments: ImmutableArray<TypeName>.Empty,
                rankOrModifier: rankOrModifier);
#endif
    }
}
