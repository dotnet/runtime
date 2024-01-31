// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Reflection.Metadata
{
#if SYSTEM_PRIVATE_CORELIB
    internal
#else
    public
#endif
    ref struct TypeNameParser
    {
        private const string EndOfTypeNameDelimiters = "[]&*,+";
#if NET8_0_OR_GREATER
        private static readonly SearchValues<char> _endOfTypeNameDelimitersSearchValues = SearchValues.Create(EndOfTypeNameDelimiters);
#endif
        private readonly bool _throwOnError;
        private readonly TypeNameParserOptions _parseOptions;
        private ReadOnlySpan<char> _inputString;

        private TypeNameParser(ReadOnlySpan<char> name, bool throwOnError, TypeNameParserOptions? options) : this()
        {
            _inputString = name.TrimStart(' '); // spaces at beginning are always OK
            _throwOnError = throwOnError;
            _parseOptions = options ?? new();
        }

        public static TypeName? Parse(ReadOnlySpan<char> name, bool allowFullyQualifiedName = true, bool throwOnError = true, TypeNameParserOptions? options = default)
        {
            TypeNameParser parser = new(name, throwOnError, options);

            int recursiveDepth = 0;
            TypeName? typeName = parser.ParseNextTypeName(allowFullyQualifiedName, ref recursiveDepth);
            if (typeName is not null && !parser._inputString.IsEmpty)
            {
                return ThrowInvalidTypeNameOrReturnNull(throwOnError);
            }

            return typeName;
        }

        public override string ToString() => _inputString.ToString(); // TODO: add proper debugger display stuff

        private TypeName? ParseNextTypeName(bool allowFullyQualifiedName, ref int recursiveDepth)
        {
            if (!Dive(ref recursiveDepth))
            {
                return null;
            }

            List<int>? nestedNameLengths = null;
            if (!TryGetTypeNameLengthWithNestedNameLengths(_inputString, ref nestedNameLengths, out int typeNameLength))
            {
                return ThrowInvalidTypeNameOrReturnNull(_throwOnError);
            }

            ReadOnlySpan<char> typeName = _inputString.Slice(0, typeNameLength);
            if (!_parseOptions.ValidateIdentifier(typeName, _throwOnError))
            {
                return null;
            }
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
                TypeName? genericArg = ParseNextTypeName(allowFullyQualifiedName: true, ref recursiveDepth); // generic args always allow AQNs
                if (genericArg is null) // parsing failed, but not thrown due to _throwOnError being true
                {
                    return null;
                }

                if (recursiveDepth > maxObservedRecursionCheck)
                {
                    maxObservedRecursionCheck = recursiveDepth;
                }

                // There had better be a ']' after the type name.
                if (!TryStripFirstCharAndTrailingSpaces(ref _inputString, ']'))
                {
                    return ThrowInvalidTypeNameOrReturnNull(_throwOnError);
                }

                (genericArgs ??= new()).Add(genericArg);

                // Is there a ',[' indicating another generic type arg?
                if (TryStripFirstCharAndTrailingSpaces(ref _inputString, ','))
                {
                    if (!TryStripFirstCharAndTrailingSpaces(ref _inputString, '['))
                    {
                        return ThrowInvalidTypeNameOrReturnNull(_throwOnError);
                    }

                    goto ParseAnotherGenericArg;
                }

                // The only other allowable character is ']', indicating the end of
                // the generic type arg list.
                if (!TryStripFirstCharAndTrailingSpaces(ref _inputString, ']'))
                {
                    return ThrowInvalidTypeNameOrReturnNull(_throwOnError);
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
                if (!Dive(ref recursiveDepth))
                {
                    return null;
                }

                if (previousDecorator == TypeName.ByRef) // it's illegal for managed reference to be followed by any other decorator
                {
                    return ThrowInvalidTypeNameOrReturnNull(_throwOnError);
                }
                previousDecorator = parsedDecorator;
            }

            AssemblyName? assemblyName = null;
            if (allowFullyQualifiedName && !TryParseAssemblyName(ref assemblyName))
            {
#if SYSTEM_PRIVATE_CORELIB
                // backward compat: throw FileLoadException for non-empty invalid strings
                if (!_throwOnError && _inputString.TrimStart().StartsWith(",")) // TODO: refactor
                {
                    return null;
                }
                throw new IO.FileLoadException(SR.InvalidAssemblyName, _inputString.ToString());
#else
                return ThrowInvalidTypeNameOrReturnNull(_throwOnError);
#endif
            }

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

        private static bool TryGetTypeNameLengthWithNestedNameLengths(ReadOnlySpan<char> input, ref List<int>? nestedNameLengths, out int totalLength)
        {
            bool isNestedType;
            totalLength = 0;
            do
            {
                int length = GetTypeNameLength(input.Slice(totalLength), out isNestedType);
                if (length <= 0) // it's possible only for a pair of unescaped '+' characters
                {
                    return false;
                }

                if (isNestedType)
                {
                    // do not validate the type name now, it will be validated as a whole nested type name later
                    (nestedNameLengths ??= new()).Add(length);
                    totalLength += 1; // skip the '+' sign in next search
                }
                totalLength += length;
            } while (isNestedType);

            return true;
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

        /// <returns>false means the input was invalid and parsing has failed. Empty input is valid and returns true.</returns>
        private bool TryParseAssemblyName(ref AssemblyName? assemblyName)
        {
            ReadOnlySpan<char> capturedBeforeProcessing = _inputString;
            if (TryStripFirstCharAndTrailingSpaces(ref _inputString, ','))
            {
                if (_inputString.IsEmpty)
                {
                    _inputString = capturedBeforeProcessing; // restore the state
                    return false;
                }

                // The only delimiter which can terminate an assembly name is ']'.
                // Otherwise EOL serves as the terminator.
                int assemblyNameLength = (int)Math.Min((uint)_inputString.IndexOf(']'), (uint)_inputString.Length);
                ReadOnlySpan<char> candidate = _inputString.Slice(0, assemblyNameLength);
                AssemblyNameParser.AssemblyNameParts parts = default;
                // TODO: make sure the parsing below is safe for untrusted input
                if (!AssemblyNameParser.TryParse(candidate, ref parts))
                {
                    return false;
                }

                // TODO: fix the perf and avoid doing it twice (missing public ctors for System.Reflection.Metadata)
                assemblyName = new(candidate.ToString());
                _inputString = _inputString.Slice(assemblyNameLength);
                return true;
            }

            return true;
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
                Debug.Assert(nestedNameLength > 0, "TryGetTypeNameLengthWithNestedNameLengths should throw on zero lengths");
                containingType = new(typeName.Slice(0, nestedNameLength).ToString(), assemblyName, rankOrModifier: 0, null, containingType: containingType, null);
                typeName = typeName.Slice(nestedNameLength + 1); // don't include the `+` in type name
            }

            return containingType;
        }

        private bool Dive(ref int depth)
        {
            if (depth >= _parseOptions.MaxRecursiveDepth)
            {
                if (_throwOnError)
                {
                    return false;
                }
                else
                {
                    Throw();
                }
            }
            depth++;
            return true;

            [DoesNotReturn]
            static void Throw() => throw new InvalidOperationException("SR.RecursionCheck_MaxDepthExceeded");
        }

        private static TypeName? ThrowInvalidTypeNameOrReturnNull(bool throwOnError)
            => throwOnError ? throw new ArgumentException("SR.Argument_InvalidTypeName") : null;

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
}
