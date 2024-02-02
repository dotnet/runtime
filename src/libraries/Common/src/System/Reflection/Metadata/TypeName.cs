// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace System.Reflection.Metadata
{
#if SYSTEM_PRIVATE_CORELIB
    internal
#else
    public
#endif
    sealed class TypeName
    {
        internal const int SZArray = -1;
        internal const int Pointer = -2;
        internal const int ByRef = -3;

        // Positive value is array rank.
        // Negative value is modifier encoded using constants above.
        private readonly int _rankOrModifier;
        private readonly TypeName[]? _genericArguments;
        private string? _assemblyQualifiedName;

        internal TypeName(string name, AssemblyName? assemblyName, int rankOrModifier,
            TypeName? underlyingType = default,
            TypeName? containingType = default,
            TypeName[]? genericTypeArguments = default)
        {
            Name = name;
            AssemblyName = assemblyName;
            _rankOrModifier = rankOrModifier;
            UnderlyingType = underlyingType;
            ContainingType = containingType;
            _genericArguments = genericTypeArguments;
        }

        /// <summary>
        /// The assembly-qualified name of the type; e.g., "System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089".
        /// </summary>
        /// <remarks>
        /// If <see cref="AssemblyName"/> is null, simply returns <see cref="Name"/>.
        /// </remarks>
        public string AssemblyQualifiedName
            => _assemblyQualifiedName ??= AssemblyName is null ? Name : $"{Name}, {AssemblyName.FullName}";

        /// <summary>
        /// The assembly which contains this type, or null if this <see cref="TypeName"/> was not
        /// created from a fully-qualified name.
        /// </summary>
        public AssemblyName? AssemblyName { get; } // TODO: AssemblyName is mutable, are we fine with that? Does it not offer too much?

        /// <summary>
        /// Returns true if this type represents any kind of array, regardless of the array's
        /// rank or its bounds.
        /// </summary>
        public bool IsArray => _rankOrModifier == SZArray || _rankOrModifier > 0;

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
        public bool IsElementalType => UnderlyingType is null && !IsConstructedGenericType;

        /// <summary>
        /// Returns true if this is a managed pointer type (e.g., "ref int").
        /// Managed pointer types are sometimes called byref types (<seealso cref="Type.IsByRef"/>)
        /// </summary>
        public bool IsManagedPointerType => _rankOrModifier == ByRef; // name inconsistent with Type.IsByRef

        /// <summary>
        /// Returns true if this is a nested type (e.g., "Namespace.Containing+Nested").
        /// For nested types <seealso cref="ContainingType"/> returns their containing type.
        /// </summary>
        public bool IsNestedType => ContainingType is not null;

        /// <summary>
        /// Returns true if this type represents a single-dimensional, zero-indexed array (e.g., "int[]").
        /// </summary>
        public bool IsSzArrayType => _rankOrModifier == SZArray; // name could be more user-friendly

        /// <summary>
        /// Returns true if this type represents an unmanaged pointer (e.g., "int*" or "void*").
        /// Unmanaged pointer types are often just called pointers (<seealso cref="Type.IsPointer"/>)
        /// </summary>
        public bool IsUnmanagedPointerType => _rankOrModifier == Pointer;// name inconsistent with Type.IsPointer

        /// <summary>
        /// Returns true if this type represents a variable-bound array; that is, an array of rank greater
        /// than 1 (e.g., "int[,]") or a single-dimensional array which isn't necessarily zero-indexed.
        /// </summary>
        public bool IsVariableBoundArrayType => _rankOrModifier > 1;

        /// <summary>
        /// If this type is a nested type (see <see cref="IsNestedType"/>), gets
        /// the containing type. If this type is not a nested type, returns null.
        /// </summary>
        /// <remarks>
        /// For example, given "Namespace.Containing+Nested", unwraps the outermost type and returns "Namespace.Containing".
        /// </remarks>
        public TypeName? ContainingType { get; }

        /// <summary>
        /// The name of this type, including namespace, but without the assembly name; e.g., "System.Int32".
        /// Nested types are represented with a '+'; e.g., "MyNamespace.MyType+NestedType".
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// If this type is not an elemental type (see <see cref="IsElementalType"/>), gets
        /// the underlying type. If this type is an elemental type, returns null.
        /// </summary>
        /// <remarks>
        /// For example, given "int[][]", unwraps the outermost array and returns "int[]".
        /// Given "Dictionary&lt;string, int&gt;", returns the generic type definition "Dictionary&lt;,&gt;".
        /// </remarks>
        public TypeName? UnderlyingType { get; }

        public int GetArrayRank()
            => _rankOrModifier switch
            {
                SZArray => 1,
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

#if !SYSTEM_PRIVATE_CORELIB
#if NET8_0_OR_GREATER
        [RequiresUnreferencedCode("The type might be removed")]
        [RequiresDynamicCode("Required by MakeArrayType")]
#else
#pragma warning disable IL2055, IL2057, IL2075, IL2096
#endif
        public Type? GetType(bool throwOnError = true, bool ignoreCase = false)
        {
            if (ContainingType is not null) // nested type
            {
                BindingFlags flagsCopiedFromClr = BindingFlags.NonPublic | BindingFlags.Public;
                if (ignoreCase)
                {
                    flagsCopiedFromClr |= BindingFlags.IgnoreCase;
                }
                return Make(ContainingType.GetType(throwOnError, ignoreCase)?.GetNestedType(Name, flagsCopiedFromClr));
            }
            else if (UnderlyingType is null)
            {
                Type? type = AssemblyName is null
                    ? Type.GetType(Name, throwOnError, ignoreCase)
                    : Assembly.Load(AssemblyName).GetType(Name, throwOnError, ignoreCase);

                return Make(type);
            }

            return Make(UnderlyingType.GetType(throwOnError, ignoreCase));

            Type? Make(Type? type)
            {
                if (type is null || IsElementalType)
                {
                    return type;
                }
                else if (IsConstructedGenericType)
                {
                    TypeName[] genericArgs = GetGenericArguments();
                    Type[] genericTypes = new Type[genericArgs.Length];
                    for (int i = 0; i < genericArgs.Length; i++)
                    {
                        Type? genericArg = genericArgs[i].GetType(throwOnError, ignoreCase);
                        if (genericArg is null)
                        {
                            return null;
                        }
                        genericTypes[i] = genericArg;
                    }

                    return type.MakeGenericType(genericTypes);
                }
                else if (IsManagedPointerType)
                {
                    return type.MakeByRefType();
                }
                else if (IsUnmanagedPointerType)
                {
                    return type.MakePointerType();
                }
                else if (IsSzArrayType)
                {
                    return type.MakeArrayType();
                }
                else
                {
                    return type.MakeArrayType(rank: GetArrayRank());
                }
            }
        }
#pragma warning restore IL2055, IL2057, IL2075, IL2096
#endif
    }
}
