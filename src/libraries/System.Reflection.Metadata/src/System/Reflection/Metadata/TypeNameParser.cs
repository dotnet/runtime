// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Reflection.Metadata
{
    public ref struct TypeNameParser
    {
        private const string EndOfTypeNameDelimiters = "[]&*,+";
#if NET8_0_OR_GREATER
        private static readonly SearchValues<char> _endOfTypeNameDelimitersSearchValues = SearchValues.Create(EndOfTypeNameDelimiters);
#endif

        private readonly TypeNameParserOptions _parseOptions;
        private ReadOnlySpan<char> _inputString;

        private TypeNameParser(ReadOnlySpan<char> name, TypeNameParserOptions? options) : this()
        {
            _inputString = name.TrimStart(' '); // spaces at beginning are always OK;
            _parseOptions = options ?? new();
        }

        public static TypeName Parse(ReadOnlySpan<char> name, bool allowFullyQualifiedName = true, TypeNameParserOptions? options = default)
        {
            TypeNameParser parser = new(name, options);

            int recursiveDepth = 0;
            TypeName typeName = parser.ParseNextTypeName(allowFullyQualifiedName, ref recursiveDepth);
            if (!parser._inputString.IsEmpty)
            {
                ThrowInvalidTypeName();
            }

            return typeName;
        }

        public override string ToString() => _inputString.ToString(); // TODO: add proper debugger display stuff

        private TypeName ParseNextTypeName(bool allowFullyQualifiedName, ref int recursiveDepth)
        {
            Dive(ref recursiveDepth);

            List<int>? nestedNameLengths = null;
            int typeNameLength = GetTypeNameLengthWithNestedNameLengths(_inputString, ref nestedNameLengths);

            ReadOnlySpan<char> typeName = _inputString.Slice(0, typeNameLength);

            _parseOptions.ValidateIdentifier(typeName);

            _inputString = _inputString.Slice(typeNameLength);

            List<TypeName>? genericArgs = null; // TODO: use some stack-based list in CoreLib

            // Are there any captured generic args? We'll look for "[[".
            // There are no spaces allowed before the first '[', but spaces are allowed
            // after that. The check slices _inputString, so we'll capture it into
            // a local so we can restore it later if needed.
            ReadOnlySpan<char> capturedBeforeProcessing = _inputString;
            if (TryStripFirstCharAndTrailingSpaces(ref _inputString, '[')
                && TryStripFirstCharAndTrailingSpaces(ref _inputString, '['))
            {
                int startingRecursionCheck = recursiveDepth;
                int maxObservedRecursionCheck = recursiveDepth;

            ParseAnotherGenericArg:

                recursiveDepth = startingRecursionCheck;
                TypeName genericArg = ParseNextTypeName(allowFullyQualifiedName: true, ref recursiveDepth); // generic args always allow AQNs
                if (recursiveDepth > maxObservedRecursionCheck)
                {
                    maxObservedRecursionCheck = recursiveDepth;
                }

                // There had better be a ']' after the type name.
                if (!TryStripFirstCharAndTrailingSpaces(ref _inputString, ']'))
                {
                    ThrowInvalidTypeName();
                }

                (genericArgs ??= new()).Add(genericArg);

                // Is there a ',[' indicating another generic type arg?
                if (TryStripFirstCharAndTrailingSpaces(ref _inputString, ','))
                {
                    if (!TryStripFirstCharAndTrailingSpaces(ref _inputString, '['))
                    {
                        ThrowInvalidTypeName();
                    }

                    goto ParseAnotherGenericArg;
                }

                // The only other allowable character is ']', indicating the end of
                // the generic type arg list.
                if (!TryStripFirstCharAndTrailingSpaces(ref _inputString, ']'))
                {
                    ThrowInvalidTypeName();
                }

                // And now that we're at the end, restore the max observed recursion count.
                recursiveDepth = maxObservedRecursionCheck;
            }

            // If there was an error stripping the generic args, back up to
            // before we started processing them, and let the decorator
            // parser try handling it.
            if (genericArgs is null)
            {
                _inputString = capturedBeforeProcessing;
            }

            int previousDecorator = default;
            // capture the current state so we can reprocess it again once we know the AssemblyName
            capturedBeforeProcessing = _inputString;
            // iterate over the decorators to ensure there are no illegal combinations
            while (TryParseNextDecorator(ref _inputString, out int parsedDecorator))
            {
                Dive(ref recursiveDepth);

                if (previousDecorator == TypeName.ByRef) // it's illegal for managed reference to be followed by any other decorator
                {
                    ThrowInvalidTypeName();
                }
                previousDecorator = parsedDecorator;
            }

            AssemblyName? assemblyName = allowFullyQualifiedName ? ParseAssemblyName() : null;

            TypeName? containingType = GetContainingType(ref typeName, nestedNameLengths, assemblyName);
            TypeName result = new(typeName.ToString(), assemblyName, rankOrModifier: 0, underlyingType: null, containingType, genericArgs?.ToArray());

            if (previousDecorator != default) // some decorators were recognized
            {
                StringBuilder sb = new StringBuilder(typeName.Length + 4);
#if NET8_0_OR_GREATER
                sb.Append(typeName);
#else
                for (int i = 0; i < typeName.Length; i++)
                {
                    sb.Append(typeName[i]);
                }
#endif
                while (TryParseNextDecorator(ref capturedBeforeProcessing, out int parsedModifier))
                {
                    // we are not reusing the input string, as it could have contain whitespaces that we want to exclude
                    string trimmedModifier = parsedModifier switch
                    {
                        TypeName.ByRef => "&",
                        TypeName.Pointer => "*",
                        TypeName.SZArray => "[]",
                        1 => "[*]",
                        _ => ArrayRankToString(parsedModifier)
                    };
                    sb.Append(trimmedModifier);
                    result = new(sb.ToString(), assemblyName, parsedModifier, underlyingType: result);
                }
            }

            return result;
        }

        private static int GetTypeNameLengthWithNestedNameLengths(ReadOnlySpan<char> input, ref List<int>? nestedNameLengths)
        {
            bool isNestedType;
            int totalLength = 0;
            do
            {
                int length = GetTypeNameLength(input.Slice(totalLength), out isNestedType);
                Debug.Assert(length > 0, "GetTypeNameLength should never return a negative value");

                if (isNestedType)
                {
                    // do not validate the type name now, it will be validated as a whole nested type name later
                    (nestedNameLengths ??= new()).Add(length);
                    totalLength += 1; // skip the '+' sign in next search
                }
                totalLength += length;
            } while (isNestedType);

            return totalLength;
        }

        // Normalizes "not found" to input length, since caller is expected to slice.
        private static int GetTypeNameLength(ReadOnlySpan<char> input, out bool isNestedType)
        {
            // NET 6+ guarantees that MemoryExtensions.IndexOfAny has worst-case complexity
            // O(m * i) if a match is found, or O(m * n) if a match is not found, where:
            //   i := index of match position
            //   m := number of needles
            //   n := length of search space (haystack)
            //
            // Downlevel versions of .NET do not make this guarantee, instead having a
            // worst-case complexity of O(m * n) even if a match occurs at the beginning of
            // the search space. Since we're running this in a loop over untrusted user
            // input, that makes the total loop complexity potentially O(m * n^2), where
            // 'n' is adversary-controlled. To avoid DoS issues here, we'll loop manually.

#if NET8_0_OR_GREATER
            int offset = input.IndexOfAny(_endOfTypeNameDelimitersSearchValues);
#elif NET6_0_OR_GREATER
            int offset = input.IndexOfAny(EndOfTypeNameDelimiters);
#else
            int offset;
            for (offset = 0; offset < input.Length; offset++)
            {
                if (EndOfTypeNameDelimiters.IndexOf(input[offset]) >= 0) { break; }
            }
#endif
            isNestedType = offset > 0 && offset < input.Length && input[offset] == '+';

            return (int)Math.Min((uint)offset, (uint)input.Length);
        }

        private AssemblyName? ParseAssemblyName()
        {
            if (TryStripFirstCharAndTrailingSpaces(ref _inputString, ','))
            {
                // The only delimiter which can terminate an assembly name is ']'.
                // Otherwise EOL serves as the terminator.
                int assemblyNameLength = (int)Math.Min((uint)_inputString.IndexOf(']'), (uint)_inputString.Length);

                string candidate = _inputString.Slice(0, assemblyNameLength).ToString();
                _inputString = _inputString.Slice(assemblyNameLength);
                // we may want to consider throwing a different exception for an empty string here
                // TODO: make sure the parsing below is safe for untrusted input
                return new AssemblyName(candidate);
            }

            return null;
        }

        private static TypeName? GetContainingType(ref ReadOnlySpan<char> typeName, List<int>? nestedNameLengths, AssemblyName? assemblyName)
        {
            if (nestedNameLengths is null)
            {
                return null;
            }

            TypeName? containingType = null;
            foreach (int nestedNameLength in nestedNameLengths)
            {
                containingType = new(typeName.Slice(0, nestedNameLength).ToString(), assemblyName, rankOrModifier: 0, null, containingType: containingType, null);
                typeName = typeName.Slice(nestedNameLength + 1); // don't include the `+` in type name
            }

            return containingType;
        }

        private void Dive(ref int depth)
        {
            if (depth >= _parseOptions.MaxRecursiveDepth)
            {
                Throw();
            }
            depth++;

            [DoesNotReturn]
            static void Throw() => throw new InvalidOperationException("SR.RecursionCheck_MaxDepthExceeded");
        }

        [DoesNotReturn]
        private static void ThrowInvalidTypeName() => throw new ArgumentException("SR.Argument_InvalidTypeName");

        private static bool TryStripFirstCharAndTrailingSpaces(ref ReadOnlySpan<char> span, char value)
        {
            if (!span.IsEmpty && span[0] == value)
            {
                span = span.Slice(1).TrimStart(' ');
                return true;
            }
            return false;
        }

        private static bool TryParseNextDecorator(ref ReadOnlySpan<char> input, out int rankOrModifier)
        {
            // Then try pulling a single decorator.
            // Whitespace cannot precede the decorator, but it can follow the decorator.

            ReadOnlySpan<char> originalInput = input; // so we can restore on 'false' return

            if (TryStripFirstCharAndTrailingSpaces(ref input, '*'))
            {
                rankOrModifier = TypeName.Pointer;
                return true;
            }

            if (TryStripFirstCharAndTrailingSpaces(ref input, '&'))
            {
                rankOrModifier = TypeName.ByRef;
                return true;
            }

            if (TryStripFirstCharAndTrailingSpaces(ref input, '['))
            {
                // SZArray := []
                // MDArray := [*] or [,] or [,,,, ...]

                int rank = 1;
                bool hasSeenAsterisk = false;

            ReadNextArrayToken:

                if (TryStripFirstCharAndTrailingSpaces(ref input, ']'))
                {
                    // End of array marker
                    rankOrModifier = rank == 1 && !hasSeenAsterisk ? TypeName.SZArray : rank;
                    return true;
                }

                if (!hasSeenAsterisk)
                {
                    if (rank == 1 && TryStripFirstCharAndTrailingSpaces(ref input, '*'))
                    {
                        // [*]
                        hasSeenAsterisk = true;
                        goto ReadNextArrayToken;
                    }
                    else if (TryStripFirstCharAndTrailingSpaces(ref input, ','))
                    {
                        // [,,, ...]
                        checked { rank++; }
                        goto ReadNextArrayToken;
                    }
                }

                // Don't know what this token is.
                // Fall through to 'return false' statement.
            }

            input = originalInput; // ensure 'ref input' not mutated
            rankOrModifier = 0;
            return false;
        }

        private static string ArrayRankToString(int arrayRank)
        {
#if NET8_0_OR_GREATER
            return string.Create(2 + arrayRank - 1, arrayRank, (buffer, rank) =>
            {
                buffer[0] = '[';
                for (int i = 1; i < rank; i++)
                    buffer[i] = ',';
                buffer[^1] = ']';
            });
#else
            StringBuilder sb = new(2 + arrayRank - 1);
            sb.Append('[');
            for (int i = 1; i < arrayRank; i++)
                sb.Append(',');
            sb.Append(']');
            return sb.ToString();
#endif
        }
    }

    public sealed class TypeName
    {
        internal const int SZArray = -1;
        internal const int Pointer = -2;
        internal const int ByRef = -3;

        // Positive value is array rank.
        // Negative value is modifier encoded using constants above.
        private readonly int _rankOrModifier;
        private readonly TypeName[]? _genericArguments;

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
            AssemblyQualifiedName = assemblyName is null ? name : $"{name}, {assemblyName.FullName}";
        }

        /// <summary>
        /// The assembly-qualified name of the type; e.g., "System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089".
        /// </summary>
        /// <remarks>
        /// If <see cref="AssemblyName"/> is null, simply returns <see cref="Name"/>.
        /// </remarks>
        public string AssemblyQualifiedName { get; }

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
    }
#pragma warning restore IL2055, IL2057, IL2075, IL2096

    public class TypeNameParserOptions
    {
        private int _maxRecursiveDepth = int.MaxValue;

        public int MaxRecursiveDepth
        {
            get => _maxRecursiveDepth;
            set
            {
#if NET8_0_OR_GREATER
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0, nameof(value));
#endif

                _maxRecursiveDepth = value;
            }
        }

        internal bool AllowSpacesOnly { get; set; }

        internal bool AllowEscaping { get; set; }

        internal bool StrictValidation { get; set; }

        public virtual void ValidateIdentifier(ReadOnlySpan<char> candidate)
        {
            if (candidate.IsEmpty)
            {
                throw new ArgumentException("TODO");
            }
        }
    }

    internal class SafeTypeNameParserOptions : TypeNameParserOptions
    {
        public SafeTypeNameParserOptions(bool allowNonAsciiIdentifiers)
        {
            AllowNonAsciiIdentifiers = allowNonAsciiIdentifiers;
            MaxRecursiveDepth = 10;
        }

        public bool AllowNonAsciiIdentifiers { get; set; }

        public override void ValidateIdentifier(ReadOnlySpan<char> candidate)
        {
            base.ValidateIdentifier(candidate);

            // allow specific ASCII chars
        }
    }

    internal class RoslynTypeNameParserOptions : TypeNameParserOptions
    {
        public override void ValidateIdentifier(ReadOnlySpan<char> candidate)
        {
            // it seems that Roslyn is not performing any kind of validation
        }
    }
}
