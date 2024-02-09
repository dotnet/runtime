// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if SYSTEM_PRIVATE_CORELIB
#define NET8_0_OR_GREATER
#endif
using System.Collections.Generic;
using System.Diagnostics;

#if SYSTEM_PRIVATE_CORELIB
using StringBuilder = System.Text.ValueStringBuilder;
#else
using StringBuilder = System.Text.StringBuilder;
#endif

using static System.Reflection.Metadata.TypeNameParserHelpers;

#nullable enable

namespace System.Reflection.Metadata
{
    [DebuggerDisplay("{_inputString}")]
    internal ref struct TypeNameParser
    {
        private const int MaxArrayRank = 32;
        private static readonly TypeNameParserOptions _defaults = new();
        private readonly bool _throwOnError;
        private readonly TypeNameParserOptions _parseOptions;
        private ReadOnlySpan<char> _inputString;

        private TypeNameParser(ReadOnlySpan<char> name, bool throwOnError, TypeNameParserOptions? options) : this()
        {
            _inputString = name;
            _throwOnError = throwOnError;
            _parseOptions = options ?? _defaults;
        }

        internal static TypeName? Parse(ReadOnlySpan<char> typeName, bool throwOnError, TypeNameParserOptions? options = default)
        {
            ReadOnlySpan<char> trimmedName = TrimStart(typeName); // whitespaces at beginning are always OK
            if (trimmedName.IsEmpty)
            {
                // whitespace input needs to report the error index as 0
                return ThrowInvalidTypeNameOrReturnNull(throwOnError, 0);
            }

            int recursiveDepth = 0;
            TypeNameParser parser = new(trimmedName, throwOnError, options);
            TypeName? parsedName = parser.ParseNextTypeName(parser._parseOptions.AllowFullyQualifiedName, ref recursiveDepth);

            if (parsedName is not null && parser._inputString.IsEmpty) // unconsumed input == error
            {
                return parsedName;
            }
            else if (!throwOnError)
            {
                return null;
            }

            // there was an error and we need to throw
#if !SYSTEM_PRIVATE_CORELIB
            if (recursiveDepth >= parser._parseOptions.MaxRecursiveDepth)
            {
                throw new InvalidOperationException("SR.RecursionCheck_MaxDepthExceeded");
            }
#endif
            int errorIndex = typeName.Length - parser._inputString.Length;
            return ThrowInvalidTypeNameOrReturnNull(throwOnError, errorIndex);

            static TypeName? ThrowInvalidTypeNameOrReturnNull(bool throwOnError, int errorIndex = 0)
            {
                if (!throwOnError)
                {
                    return null;
                }

#if SYSTEM_PRIVATE_CORELIB
                throw new ArgumentException(SR.Arg_ArgumentException, $"typeName@{errorIndex}");
#else
                throw new ArgumentException("SR.Argument_InvalidTypeName");
#endif
            }
        }

        // this method should return null instead of throwing, so the caller can get errorIndex and include it in error msg
        private TypeName? ParseNextTypeName(bool allowFullyQualifiedName, ref int recursiveDepth)
        {
            if (!TryDive(ref recursiveDepth))
            {
                return null;
            }

            List<int>? nestedNameLengths = null;
            if (!TryGetTypeNameInfo(_inputString, ref nestedNameLengths, out int fullTypeNameLength, out int genericArgCount))
            {
                return null;
            }

            ReadOnlySpan<char> fullTypeName = _inputString.Slice(0, fullTypeNameLength);
            if (!_parseOptions.ValidateIdentifier(fullTypeName, _throwOnError))
            {
                return null;
            }
            _inputString = _inputString.Slice(fullTypeNameLength);

            int genericArgIndex = 0;
            // Don't allocate now, as it may be an open generic type like "List`1"
            TypeName[]? genericArgs = null;

            // Are there any captured generic args? We'll look for "[[" and "[".
            // There are no spaces allowed before the first '[', but spaces are allowed
            // after that. The check slices _inputString, so we'll capture it into
            // a local so we can restore it later if needed.
            ReadOnlySpan<char> capturedBeforeProcessing = _inputString;
            if (IsBeginningOfGenericAgs(ref _inputString, out bool doubleBrackets))
            {
                int startingRecursionCheck = recursiveDepth;
                int maxObservedRecursionCheck = recursiveDepth;

            ParseAnotherGenericArg:

                // Invalid generic argument count provided after backtick.
                // Examples:
                // - too many: List`1[[a], [b]]
                // - not expected: NoBacktick[[a]]
                if (genericArgIndex >= genericArgCount)
                {
                    return null;
                }

                recursiveDepth = startingRecursionCheck;
                // Namespace.Type`1[[GenericArgument1, AssemblyName1],[GenericArgument2, AssemblyName2]] - double square bracket syntax allows for fully qualified type names
                // Namespace.Type`1[GenericArgument1,GenericArgument2] - single square bracket syntax is legal only for non-fully qualified type names
                TypeName? genericArg = ParseNextTypeName(allowFullyQualifiedName: doubleBrackets, ref recursiveDepth);
                if (genericArg is null) // parsing failed
                {
                    return null;
                }

                if (recursiveDepth > maxObservedRecursionCheck)
                {
                    maxObservedRecursionCheck = recursiveDepth;
                }

                // For [[, there had better be a ']' after the type name.
                if (doubleBrackets && !TryStripFirstCharAndTrailingSpaces(ref _inputString, ']'))
                {
                    return null;
                }

                // TODO adsitnik: don't allocate an array that would require reaching the max depth limit
                (genericArgs ??= new TypeName[genericArgCount])[genericArgIndex++] = genericArg;

                if (TryStripFirstCharAndTrailingSpaces(ref _inputString, ','))
                {
                    // For [[, is there a ',[' indicating another generic type arg?
                    // For [, it's just a ','
                    if (doubleBrackets && !TryStripFirstCharAndTrailingSpaces(ref _inputString, '['))
                    {
                        return null;
                    }

                    goto ParseAnotherGenericArg;
                }

                // The only other allowable character is ']', indicating the end of
                // the generic type arg list.
                if (!TryStripFirstCharAndTrailingSpaces(ref _inputString, ']'))
                {
                    return null;
                }

                // We have reached the end of generic arguments, but parsed fewer than expected.
                // Example: A`2[[b]]
                if (genericArgIndex != genericArgCount)
                {
                    return null;
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
                if (!TryDive(ref recursiveDepth))
                {
                    return null;
                }

                if (previousDecorator == ByRef) // it's illegal for managed reference to be followed by any other decorator
                {
                    return null;
                }
                else if (parsedDecorator > MaxArrayRank)
                {
                    return null;
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
                return null;
#endif
            }

            TypeName? containingType = GetContainingType(fullTypeName, nestedNameLengths, assemblyName);
            string name = GetName(fullTypeName).ToString();
            TypeName? underlyingType = genericArgs is null ? null : new(name, fullTypeName.ToString(), assemblyName, containingType: containingType);
            string genericTypeFullName = GetGenericTypeFullName(fullTypeName, genericArgs);
            TypeName result = new(name, genericTypeFullName, assemblyName, rankOrModifier: 0, underlyingType, containingType, genericArgs);

            if (previousDecorator != default) // some decorators were recognized
            {
                StringBuilder fullNameSb = new(genericTypeFullName.Length + 4);
                fullNameSb.Append(genericTypeFullName);
                StringBuilder nameSb = new(name.Length + 4);
                nameSb.Append(name);

                while (TryParseNextDecorator(ref capturedBeforeProcessing, out int parsedModifier))
                {
                    // we are not reusing the input string, as it could have contain whitespaces that we want to exclude
                    string trimmedModifier = GetRankOrModifierStringRepresentation(parsedModifier);
                    nameSb.Append(trimmedModifier);
                    fullNameSb.Append(trimmedModifier);

                    result = new(nameSb.ToString(), fullNameSb.ToString(), assemblyName, parsedModifier, underlyingType: result);
                }
            }

            return result;
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

#if SYSTEM_PRIVATE_CORELIB
                assemblyName = new();
                assemblyName.Init(parts);
#else
                // TODO: fix the perf and avoid doing it twice (missing public ctors for System.Reflection.Metadata)
                assemblyName = new(candidate.ToString());
#endif
                _inputString = _inputString.Slice(assemblyNameLength);
                return true;
            }

            return true;
        }

        private static TypeName? GetContainingType(ReadOnlySpan<char> fullTypeName, List<int>? nestedNameLengths, AssemblyName? assemblyName)
        {
            if (nestedNameLengths is null)
            {
                return null;
            }

            TypeName? containingType = null;
            int nameOffset = 0;
            foreach (int nestedNameLength in nestedNameLengths)
            {
                Debug.Assert(nestedNameLength > 0, "TryGetTypeNameInfo should return error on zero lengths");
                ReadOnlySpan<char> fullName = fullTypeName.Slice(0, nameOffset + nestedNameLength);
                ReadOnlySpan<char> name = GetName(fullName);
                containingType = new(name.ToString(), fullName.ToString(), assemblyName, containingType: containingType);
                nameOffset += nestedNameLength + 1; // include the '+' that was skipped in name
            }

            return containingType;
        }

        private bool TryDive(ref int depth)
        {
            if (depth >= _parseOptions.MaxRecursiveDepth)
            {
                return false;
            }
            depth++;
            return true;
        }

        private static bool TryParseNextDecorator(ref ReadOnlySpan<char> input, out int rankOrModifier)
        {
            // Then try pulling a single decorator.
            // Whitespace cannot precede the decorator, but it can follow the decorator.

            ReadOnlySpan<char> originalInput = input; // so we can restore on 'false' return

            if (TryStripFirstCharAndTrailingSpaces(ref input, '*'))
            {
                rankOrModifier = TypeNameParserHelpers.Pointer;
                return true;
            }

            if (TryStripFirstCharAndTrailingSpaces(ref input, '&'))
            {
                rankOrModifier = ByRef;
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
                    rankOrModifier = rank == 1 && !hasSeenAsterisk ? SZArray : rank;
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
    }
}
