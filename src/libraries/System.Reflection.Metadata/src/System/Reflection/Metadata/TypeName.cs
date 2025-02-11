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
        private readonly int _rankOrModifier;
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
        private string? _name, _namespace, _fullName, _assemblyQualifiedName;

        internal TypeName(string? fullName,
            AssemblyNameInfo? assemblyName,
            TypeName? elementOrGenericType = default,
            TypeName? declaringType = default,
#if SYSTEM_PRIVATE_CORELIB
            List<TypeName>? genericTypeArguments = default,
#else
            ImmutableArray<TypeName>.Builder? genericTypeArguments = default,
#endif
            int rankOrModifier = default,
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
            TypeName? elementOrGenericType,
            TypeName? declaringType,
            ImmutableArray<TypeName> genericTypeArguments,
            int rankOrModifier = default,
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
        {
            get
            {
                if (_assemblyQualifiedName is null)
                {
                    if (_fullName is not null && AssemblyName is null)
                    {
                        // _fullName may carry more information than FullName property, so we need to use FullName property.
                        _assemblyQualifiedName = FullName;
                    }
                    else
                    {
                        ValueStringBuilder builder = new(stackalloc char[256]);
                        AppendFullName(ref builder); // see recursion comments in AppendFullName
                        if (AssemblyName is not null)
                        {
                            builder.Append(", ");
                            AssemblyName.AppendFullName(ref builder);
                        }
                        _assemblyQualifiedName = builder.ToString();

                        if (AssemblyName is null)
                        {
                            // If the type name was not created from a assembly-qualified name,
                            // the FullName and AssemblyQualifiedName are the same.
                            _fullName = _assemblyQualifiedName;
                        }
                    }
                }

                return _assemblyQualifiedName;
            }
        }

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
                    ValueStringBuilder builder = new(stackalloc char[128]);
                    AppendFullName(ref builder);
                    _fullName = builder.ToString();
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

        private void AppendFullName(ref ValueStringBuilder builder)
        {
            // This is a recursive method over potentially hostile input. Protection against DoS is offered
            // via the [Try]Parse method and TypeNameParserOptions.MaxNodes property at construction time.
            // This FullName property getter and related methods assume that this TypeName instance has an
            // acceptable node count.
            //
            // The node count controls the total amount of work performed by this method, including:
            // - The max possible stack depth due to the recursive methods calls.

            if (_fullName is null)
            {
                if (IsConstructedGenericType)
                {
                    GetGenericTypeDefinition().AppendFullName(ref builder);
                    builder.Append('[');
                    foreach (TypeName genericArg in GetGenericArguments())
                    {
                        builder.Append('[');
                        genericArg.AppendFullName(ref builder);
                        // Generic arguments need to be always fully qualified.
                        if (genericArg.AssemblyName is not null)
                        {
                            builder.Append(", ");
                            genericArg.AssemblyName.AppendFullName(ref builder);
                        }
                        builder.Append("],");
                    }
                    builder[builder.Length - 1] = ']'; // replace ',' with ']'
                }
                else if (IsArray || IsPointer || IsByRef)
                {
                    GetElementType().AppendFullName(ref builder);
                    TypeNameParserHelpers.AppendRankOrModifierStringRepresentation(_rankOrModifier, ref builder);
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
                builder.Append(_fullName.AsSpan(0, _nestedNameLength));
            }
            else
            {
                builder.Append(_fullName);
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
        [MemberNotNullWhen(false, nameof(_elementOrGenericType))]
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
        [MemberNotNullWhen(true, nameof(_declaringType))]
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
                    ValueStringBuilder builder = new(stackalloc char[64]);
                    AppendName(ref builder);
                    _name = builder.ToString();
                }

                return _name;
            }
        }

        private void AppendName(ref ValueStringBuilder builder)
        {
            // Lookups to Name and FullName might be recursive. See comments in AppendFullName method.

            if (IsConstructedGenericType)
            {
                GetGenericTypeDefinition().AppendName(ref builder);
            }
            else if (IsPointer || IsByRef || IsArray)
            {
                GetElementType().AppendName(ref builder);
                TypeNameParserHelpers.AppendRankOrModifierStringRepresentation(_rankOrModifier, ref builder);
            }
            else
            {
                // _fullName can be null only in constructed generic or modified types, which we handled above.
                Debug.Assert(_fullName is not null);
                ReadOnlySpan<char> name = _fullName.AsSpan();
                if (_nestedNameLength > 0)
                {
                    name = name.Slice(0, _nestedNameLength);
                }
                if (IsNested)
                {
                    // If the type is nested, we know the length of the declaring type's full name.
                    // Get the characters after that plus one for the '+' separator.
                    name = name.Slice(_declaringType._nestedNameLength + 1);
                }
                else if (TypeNameParserHelpers.IndexOfNamespaceDelimiter(name) is int idx && idx >= 0)
                {
                    // If the type is not nested, find the namespace delimiter in the full name and return the substring after it.
                    name = name.Slice(idx + 1);
                }
                builder.Append(name);
            }
        }

        /// <summary>
        /// The namespace of this type; e.g., "System".
        /// </summary>
        /// <exception cref="InvalidOperationException">This instance is a nested type.</exception>
        public string Namespace
        {
            get
            {
                if (_namespace is null)
                {
                    TypeName rootTypeName = this;
                    while (!rootTypeName.IsSimple)
                    {
                        rootTypeName = rootTypeName._elementOrGenericType;
                    }

                    if (rootTypeName.IsNested)
                    {
                        TypeNameParserHelpers.ThrowInvalidOperation_NestedTypeNamespace();
                    }

                    // By setting the namespace field at the root type name, we avoid recomputing it for all derived names.
                    if (rootTypeName._namespace is null)
                    {
                        // At this point the type does not have a modifier applied to it, so it should have its full name set.
                        Debug.Assert(rootTypeName._fullName is not null);
                        ReadOnlySpan<char> rootFullName = rootTypeName._fullName.AsSpan();
                        if (rootTypeName._nestedNameLength > 0)
                        {
                            rootFullName = rootFullName.Slice(0, rootTypeName._nestedNameLength);
                        }
                        if (TypeNameParserHelpers.IndexOfNamespaceDelimiter(rootFullName) is int idx && idx >= 0)
                        {
                            rootTypeName._namespace = rootFullName.Slice(0, idx).ToString();
                        }
                        else
                        {
                            rootTypeName._namespace = string.Empty;
                        }
                    }
                    _namespace = rootTypeName._namespace;
                }

                return _namespace;
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
        /// <exception cref="OverflowException">The total number of <see cref="TypeName"/> instances that are used to describe
        /// this instance exceed <see cref="int.MaxValue"/>.</exception>
        public int GetNodeCount()
        {
            // This method uses checked arithmetic to avoid silent overflows.
            // It's impossible to parse a TypeName with NodeCount > int.MaxValue
            // (TypeNameParseOptions.MaxNodes is an int), but it's possible
            // to create such names with the Make* APIs.
            int result = 1;

            if (IsArray || IsPointer || IsByRef)
            {
                result = checked(result + GetElementType().GetNodeCount());
            }
            else if (IsConstructedGenericType)
            {
                result = checked(result + GetGenericTypeDefinition().GetNodeCount());

                foreach (TypeName genericArgument in GetGenericArguments())
                {
                    result = checked(result + genericArgument.GetNodeCount());
                }
            }
            else if (IsNested)
            {
                result = checked(result + DeclaringType.GetNodeCount());
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
        /// Converts any escaped characters in the input type name or namespace.
        /// </summary>
        /// <param name="name">The input string containing the name to convert.</param>
        /// <returns>A string of characters with any escaped characters converted to their unescaped form.</returns>
        /// <remarks>
        /// <para>The unescaped string can be used for looking up the type name or namespace in metadata.</para>
        /// <para>This method removes escape characters even if they precede a character that does not require escaping.</para>
        /// </remarks>
        public static string Unescape(string name)
        {
            if (name is null)
            {
                TypeNameParserHelpers.ThrowArgumentNullException(nameof(name));
            }

            return TypeNameParserHelpers.Unescape(name);
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
        /// Creates a new <see cref="TypeName" /> object that represents current simple name with provided assembly name.
        /// </summary>
        /// <param name="assemblyName">Assembly name.</param>
        /// <returns>Created simple name.</returns>
        /// <exception cref="InvalidOperationException">The current type name is not simple.</exception>
        public TypeName WithAssemblyName(AssemblyNameInfo? assemblyName)
        {
            // Recursive method. See comments in FullName property getter for more information
            // on how this is protected against attack.
            //
            // n.b. AssemblyNameInfo could also be hostile. The typical exploit is that a single
            // long AssemblyNameInfo is associated with one or more simple TypeName objects,
            // leading to an alg. complexity attack (DoS). It's important that TypeName doesn't
            // actually *do* anything with the provided AssemblyNameInfo rather than store it.
            // For example, don't use it inside a string concat operation unless the caller
            // explicitly requested that to happen. If the input is hostile, the caller should
            // never perform such concats in a loop.

            if (!IsSimple)
            {
                TypeNameParserHelpers.ThrowInvalidOperation_NotSimpleName(FullName);
            }

            TypeName? declaringType = IsNested
                ? DeclaringType.WithAssemblyName(assemblyName)
                : null;

            return new TypeName(fullName: _fullName,
                assemblyName: assemblyName,
                elementOrGenericType: null,
                declaringType: declaringType,
                genericTypeArguments: ImmutableArray<TypeName>.Empty,
                nestedNameLength: _nestedNameLength);
        }

        /// <summary>
        /// Creates a <see cref="TypeName" /> object representing a one-dimensional array
        /// of the current type, with a lower bound of zero.
        /// </summary>
        /// <returns>
        /// A <see cref="TypeName" /> object representing a one-dimensional array
        /// of the current type, with a lower bound of zero.
        /// </returns>
        public TypeName MakeSZArrayTypeName() => MakeElementTypeName(TypeNameParserHelpers.SZArray);

        /// <summary>
        /// Creates a <see cref="TypeName" /> object representing an array of the current type,
        /// with the specified number of dimensions.
        /// </summary>
        /// <param name="rank">The number of dimensions for the array. This number must be more than zero and less than or equal to 32.</param>
        /// <returns>
        /// A <see cref="TypeName" /> object representing an array of the current type,
        /// with the specified number of dimensions.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">rank is invalid. For example, 0 or negative.</exception>
        public TypeName MakeArrayTypeName(int rank)
            => rank <= 0
                ? throw new ArgumentOutOfRangeException(nameof(rank))
                : MakeElementTypeName(rank);

        /// <summary>
        /// Creates a <see cref="TypeName" /> object that represents a pointer to the current type.
        /// </summary>
        /// <returns>
        /// A <see cref="TypeName" /> object that represents a pointer to the current type.
        /// </returns>
        public TypeName MakePointerTypeName() => MakeElementTypeName(TypeNameParserHelpers.Pointer);

        /// <summary>
        /// Creates a <see cref="TypeName" /> object that represents a managed reference to the current type.
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
        {
            if (!IsSimple)
            {
                TypeNameParserHelpers.ThrowInvalidOperation_NotSimpleName(FullName);
            }

            return new TypeName(fullName: null, AssemblyName, elementOrGenericType: this, declaringType: _declaringType, genericTypeArguments: typeArguments);
        }

        private TypeName MakeElementTypeName(int rankOrModifier)
            => new TypeName(
                fullName: null,
                assemblyName: AssemblyName,
                elementOrGenericType: this,
                declaringType: null,
                genericTypeArguments: ImmutableArray<TypeName>.Empty,
                rankOrModifier: rankOrModifier);
#endif
    }
}
