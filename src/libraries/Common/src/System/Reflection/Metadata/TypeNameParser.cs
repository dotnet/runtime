// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using static System.Reflection.Metadata.TypeNameParserHelpers;

#nullable enable

namespace System.Reflection.Metadata
{
    [DebuggerDisplay("{_inputString}")]
    internal ref struct TypeNameParser
    {
        private static readonly TypeNameParseOptions _defaults = new();
        private readonly bool _throwOnError;
        private readonly TypeNameParseOptions _parseOptions;
        private ReadOnlySpan<char> _inputString;

        private TypeNameParser(ReadOnlySpan<char> name, bool throwOnError, TypeNameParseOptions? options) : this()
        {
            _inputString = name;
            _throwOnError = throwOnError;
            _parseOptions = options ?? _defaults;
        }

        internal static TypeName? Parse(ReadOnlySpan<char> typeName, bool throwOnError, TypeNameParseOptions? options = default)
        {
            ReadOnlySpan<char> trimmedName = typeName.TrimStart(); // whitespaces at beginning are always OK
            if (trimmedName.IsEmpty)
            {
                // whitespace input needs to report the error index as 0
                return throwOnError ? throw ArgumentException_InvalidTypeName(errorIndex: 0) : null;
            }

            int recursiveDepth = 0;
            TypeNameParser parser = new(trimmedName, throwOnError, options);
            TypeName? parsedName = parser.ParseNextTypeName(allowFullyQualifiedName: true, ref recursiveDepth);

            if (parsedName is not null && parser._inputString.IsEmpty) // unconsumed input == error
            {
                return parsedName;
            }
            else if (!throwOnError)
            {
                return null;
            }

            if (recursiveDepth >= parser._parseOptions.MaxNodes)
            {
                throw InvalidOperation_MaxNodesExceeded(parser._parseOptions.MaxNodes);
            }

            int errorIndex = typeName.Length - parser._inputString.Length;
            throw ArgumentException_InvalidTypeName(errorIndex);
        }

        // this method should return null instead of throwing, so the caller can get errorIndex and include it in error msg
        private TypeName? ParseNextTypeName(bool allowFullyQualifiedName, ref int recursiveDepth)
        {
            if (!TryDive(ref recursiveDepth))
            {
                return null;
            }

            List<int>? nestedNameLengths = null;
            if (!TryGetTypeNameInfo(ref _inputString, ref nestedNameLengths, out int fullTypeNameLength, out int genericArgCount))
            {
                return null;
            }

            ReadOnlySpan<char> fullTypeName = _inputString.Slice(0, fullTypeNameLength);
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
                // Namespace.Type`1[[GenericArgument1, AssemblyName1], GenericArgument2] - mixed mode
                // Namespace.Type`1[GenericArgument1, [GenericArgument2, AssemblyName2]] - mixed mode
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

                if (genericArgs is null)
                {
                    // Parsing the rest would hit the limit.
                    // -1 because the first generic arg has been already parsed.
                    if (maxObservedRecursionCheck + genericArgCount - 1 > _parseOptions.MaxNodes)
                    {
                        recursiveDepth = _parseOptions.MaxNodes;
                        return null;
                    }

                    genericArgs = new TypeName[genericArgCount];
                }
                genericArgs[genericArgIndex++] = genericArg;

                // Is there a ',[' indicating fully qualified generic type arg?
                // Is there a ',' indicating non-fully qualified generic type arg?
                if (TryStripFirstCharAndTrailingSpaces(ref _inputString, ','))
                {
                    doubleBrackets = TryStripFirstCharAndTrailingSpaces(ref _inputString, '[');

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

                if (previousDecorator == ByRef // it's illegal for managed reference to be followed by any other decorator
                    || parsedDecorator > MaxArrayRank)
                {
#if SYSTEM_PRIVATE_CORELIB
                    throw new TypeLoadException(); // CLR throws TypeLoadException for invalid decorators
#else
                    return null;
#endif
                }
                previousDecorator = parsedDecorator;
            }

            AssemblyName? assemblyName = null;
            if (allowFullyQualifiedName && !TryParseAssemblyName(ref assemblyName))
            {
#if SYSTEM_PRIVATE_CORELIB
                // backward compat: throw FileLoadException for non-empty invalid strings
                if (!_throwOnError && _inputString.TrimStart().StartsWith(","))
                {
                    return null;
                }
                throw new IO.FileLoadException(SR.InvalidAssemblyName, _inputString.ToString());
#else
                return null;
#endif
            }

            TypeName? declaringType = GetDeclaringType(fullTypeName, nestedNameLengths, assemblyName);
            string name = GetName(fullTypeName).ToString();
            TypeName? underlyingType = genericArgs is null ? null : new(name, fullTypeName.ToString(), assemblyName, declaringType: declaringType);
            string genericTypeFullName = GetGenericTypeFullName(fullTypeName, genericArgs);
            TypeName result = new(name, genericTypeFullName, assemblyName, rankOrModifier: 0, underlyingType, declaringType, genericArgs);

            if (previousDecorator != default) // some decorators were recognized
            {
                ValueStringBuilder fullNameSb = new(stackalloc char[128]);
                fullNameSb.Append(genericTypeFullName);

                ValueStringBuilder nameSb = new(stackalloc char[32]);
                nameSb.Append(name);

                while (TryParseNextDecorator(ref capturedBeforeProcessing, out int parsedModifier))
                {
                    // we are not reusing the input string, as it could have contain whitespaces that we want to exclude
                    string trimmedModifier = GetRankOrModifierStringRepresentation(parsedModifier);
                    nameSb.Append(trimmedModifier);
                    fullNameSb.Append(trimmedModifier);

                    result = new(nameSb.AsSpan().ToString(), fullNameSb.AsSpan().ToString(), assemblyName, parsedModifier, elementOrGenericType: result);
                }

                // The code above is not calling ValueStringBuilder.ToString() directly,
                // because it calls Dispose and we want to reuse the builder content until we are done with all decorators.
                fullNameSb.Dispose();
                nameSb.Dispose();
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

                if (!AssemblyNameParser.TryParse(candidate, ref parts))
                {
                    return false;
                }

                assemblyName = new AssemblyName();
#if SYSTEM_PRIVATE_CORELIB
                assemblyName.Init(parts);
#else
                assemblyName.Name = parts._name;
                assemblyName.CultureName = parts._cultureName;
                assemblyName.Version = parts._version;

                if (parts._publicKeyOrToken is not null)
                {
                    if ((parts._flags & AssemblyNameFlags.PublicKey) != 0)
                    {
                        assemblyName.SetPublicKey(parts._publicKeyOrToken);
                    }
                    else
                    {
                        assemblyName.SetPublicKeyToken(parts._publicKeyOrToken);
                    }
                }
#endif
                _inputString = _inputString.Slice(assemblyNameLength);
                return true;
            }

            return true;
        }

        private static TypeName? GetDeclaringType(ReadOnlySpan<char> fullTypeName, List<int>? nestedNameLengths, AssemblyName? assemblyName)
        {
            if (nestedNameLengths is null)
            {
                return null;
            }

            TypeName? declaringType = null;
            int nameOffset = 0;
            foreach (int nestedNameLength in nestedNameLengths)
            {
                Debug.Assert(nestedNameLength > 0, "TryGetTypeNameInfo should return error on zero lengths");
                ReadOnlySpan<char> fullName = fullTypeName.Slice(0, nameOffset + nestedNameLength);
                ReadOnlySpan<char> name = GetName(fullName);
                declaringType = new(name.ToString(), fullName.ToString(), assemblyName, declaringType: declaringType);
                nameOffset += nestedNameLength + 1; // include the '+' that was skipped in name
            }

            return declaringType;
        }

        private bool TryDive(ref int depth)
        {
            if (depth >= _parseOptions.MaxNodes)
            {
                return false;
            }
            depth++;
            return true;
        }
    }
}
