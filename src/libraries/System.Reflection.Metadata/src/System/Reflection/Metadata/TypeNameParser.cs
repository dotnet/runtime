// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata
{
    public ref struct TypeNameParser
    {
        private const string EndOfTypeNameDelimiters = "[]&*,"; // TODO: Roslyn is using '+' here as well
#if NET8_0_OR_GREATER
        private static readonly SearchValues<char> _endOfTypeNameDelimitersSearchValues = SearchValues.Create(EndOfTypeNameDelimiters);
#endif

        private readonly TypeNameParserOptions _parseOptions;
        private ReadOnlySpan<char> _inputString;

        private TypeNameParser(ReadOnlySpan<char> name, TypeNameParserOptions? options) : this()
        {
            _inputString = name;
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

        public override string ToString() => _inputString.ToString();

        private TypeName ParseNextTypeName(bool allowFullyQualifiedName, ref int recursiveDepth)
        {
            System.Diagnostics.Debugger.Launch();

            Dive(ref recursiveDepth);

            _inputString = _inputString.TrimStart(' '); // spaces at beginning are always OK

            int offset = GetOffsetOfEndOfTypeName(_inputString);

            string candidate = _inputString.Slice(0, offset).ToString();

            _parseOptions.ValidateIdentifier(candidate);

            _inputString = _inputString.Slice(offset);

            List<TypeName>? genericArgs = null;
            ReadOnlySpan<char> capturedBeforeGenericProcessing = _inputString;
            if (_inputString.Length > 2 && _inputString[0] == '[')
            {
                // Are there any captured generic args? We'll look for "[[".
                // There are no spaces allowed before the first '[', but spaces are allowed
                // after that. The check slices _inputString, so we'll capture it into
                // a local so we can restore it later if needed.
                _inputString = _inputString.Slice(1).TrimStart(' ');

                if (_inputString.Length > 1 && _inputString[0] == '[')
                {
                    _inputString = _inputString.Slice(1); // the next call to ParseNextTypeName is going to trim the starting spaces

                    int startingRecursionCheck = recursiveDepth;
                    int maxObservedRecursionCheck = recursiveDepth;

                ParseAnotherGenericArg:

                    recursiveDepth = startingRecursionCheck;
                    TypeName genericArg = ParseNextTypeName(allowFullyQualifiedName: true, ref recursiveDepth);
                    if (recursiveDepth > maxObservedRecursionCheck)
                    {
                        maxObservedRecursionCheck = recursiveDepth;
                    }

                    // There had better be a ']' after the type name.
                    if (_inputString.IsEmpty || _inputString[0] != ']')
                    {
                        ThrowInvalidTypeName();
                    }

                    (genericArgs ??= new()).Add(genericArg);

                    // Is there a ',[' indicating another generic type arg?
                    if (!_inputString.IsEmpty && _inputString[0] == ',')
                    {
                        _inputString = _inputString.TrimStart(' ');
                        if (_inputString.IsEmpty || _inputString[0] != '[')
                        {
                            ThrowInvalidTypeName();
                        }

                        goto ParseAnotherGenericArg;
                    }

                    // The only other allowable character is ']', indicating the end of
                    // the generic type arg list.
                    if (_inputString.IsEmpty || _inputString[0] != ']')
                    {
                        ThrowInvalidTypeName();
                    }

                    // And now that we're at the end, restore the max observed recursion count.
                    recursiveDepth = maxObservedRecursionCheck;
                }
            }

            // If there was an error stripping the generic args, back up to
            // before we started processing them, and let the decorator
            // parser try handling it.
            if (genericArgs is null)
            {
                _inputString = capturedBeforeGenericProcessing;
            }

            // Strip off decorators one at a time, bumping the recursive depth each time.
            // TODO

            AssemblyName? assemblyName = allowFullyQualifiedName ? ParseAssemblyName() : null;

            return new(candidate, assemblyName, 0, null, genericArgs?.ToArray());
        }

        // Normalizes "not found" to input length, since caller is expected to slice.
        private static int GetOffsetOfEndOfTypeName(ReadOnlySpan<char> input)
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

            return (int)Math.Min((uint)offset, (uint)input.Length);
        }

        private AssemblyName? ParseAssemblyName()
        {
            if (!_inputString.IsEmpty && _inputString[0] == ',')
            {
                _inputString = _inputString.Slice(1).TrimStart(' ');

                // The only delimiter which can terminate an assembly name is ']'.
                // Otherwise EOL serves as the terminator.
                int assemblyNameLength = (int)Math.Min((uint)_inputString.IndexOf(']'), (uint)_inputString.Length);

                string candidate = _inputString.Slice(0, assemblyNameLength).ToString();

                // we may want to consider throwing a different exception for an empty string here
                // TODO: make sure the parsing below is safe for untrusted input

                try
                {
                    AssemblyName result = new(candidate);
                    _inputString = _inputString.Slice(assemblyNameLength);
                    return result;
                }
                catch (Exception) // TODO: handle invalid assembly names without exceptions
                {
                    return null;
                }
            }

            return null;
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
    }

    public sealed class TypeName
    {
        internal const int Pointer = -2;
        internal const int ByRef = -3;

        // Positive value is array rank.
        // Negative value is modifier encoded using constants above.
        private readonly int _rankOrModifier;
        private readonly TypeName[]? _genericArguments;

        internal TypeName(string name, AssemblyName? assemblyName, int rankOrModifier, TypeName? underlyingType = default, TypeName[]? genericTypeArguments = null)
        {
            Name = name;
            AssemblyName = assemblyName;
            _rankOrModifier = rankOrModifier;
            UnderlyingType = underlyingType;
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
        public bool IsArray => _rankOrModifier > 0;

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
        /// Returns true if this type represents a variable-bound array; that is, an array of rank greater
        /// than 1 (e.g., "int[,]") or a single-dimensional array which isn't necessarily zero-indexed.
        /// </summary>
        public bool IsVariableBoundArrayType => _rankOrModifier > 1;

        /// <summary>
        /// Returns true if this is a managed pointer type (e.g., "ref int").
        /// Managed pointer types are sometimes called byref types (<seealso cref="Type.IsByRef"/>)
        /// </summary>
        public bool IsManagedPointerType => _rankOrModifier == ByRef; // name inconsistent with Type.IsByRef

        /// <summary>
        /// Returns true if this type represents an unmanaged pointer (e.g., "int*" or "void*").
        /// Unmanaged pointer types are often just called pointers (<seealso cref="Type.IsPointer"/>)
        /// </summary>
        public bool IsUnmanagedPointerType => _rankOrModifier == Pointer;// name inconsistent with Type.IsPointer

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
            => _rankOrModifier > 0
                ? _rankOrModifier
                : throw new ArgumentException("SR.Argument_HasToBeArrayClass"); // TODO: use actual resource (used by Type.GetArrayRank)

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
    }

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

        public virtual void ValidateIdentifier(string candidate)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNullOrEmpty(candidate, nameof(candidate));
#endif
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

        public override void ValidateIdentifier(string candidate)
        {
            base.ValidateIdentifier(candidate);

            // allow specific ASCII chars
        }
    }

    internal class RoslynTypeNameParserOptions : TypeNameParserOptions
    {
        public override void ValidateIdentifier(string candidate)
        {
            // it seems that Roslyn is not performing any kind of validation
        }
    }
}
