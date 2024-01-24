// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

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
            TypeName typeName = parser.Parse(allowFullyQualifiedName);
            // TODO: throw for non-consumed input like trailing whitespaces
            return typeName;
        }

        private TypeName Parse(bool allowFullyQualifiedName)
        {
            _inputString = _inputString.TrimStart(' '); // spaces at beginning are ok, BTW Roslyn does not need that as their input comes already trimmed

            int offset = GetOffsetOfEndOfTypeName(_inputString);

            string candidate = _inputString.Slice(0, offset).ToString();

            _parseOptions.ValidateIdentifier(candidate);

            _inputString = _inputString.Slice(offset);

            AssemblyName? assemblyName = allowFullyQualifiedName ? ParseAssemblyName() : null;

            return new (candidate, assemblyName);
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
                _inputString = _inputString.Slice(assemblyNameLength);
                // we may want to consider throwing a different exception for an empty string here
                // TODO: make sure the parsing below is safe for untrusted input
                return new AssemblyName(candidate);
            }

            return null;
        }
    }

    public readonly struct TypeName
    {
        internal TypeName(string name, AssemblyName? assemblyName) : this()
        {
            Name = name;
            AssemblyName = assemblyName;
            AssemblyQualifiedName = assemblyName is null ? name : $"{name}, {assemblyName.FullName}";
        }

        public string AssemblyQualifiedName { get; } // TODO: do we really need that?
        public AssemblyName? AssemblyName { get; } // TODO: AssemblyName is mutable, are we fine with that? Does it not offer too much?
        public bool ContainsGenericParameters { get; }
        public TypeName[] GenericTypeArguments => Array.Empty<TypeName>();
        public bool IsArray { get; }
        public bool IsVariableBoundArrayType { get; }
        public bool IsManagedPointerType { get; } // inconsistent with Type.IsByRef
        public bool IsUnmanagedPointerType { get; } // inconsistent with Type.IsPointer
        public bool IsNested { get; } // ?? not needed right now?

        /// <summary>
        /// The name of this type, including namespace, but without the assembly name; e.g., "System.Int32".
        /// Nested types are represented with a '+'; e.g., "MyNamespace.MyType+NestedType".
        /// </summary>
        public string Name { get; }

        public int GetArrayRank() => 0;
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
