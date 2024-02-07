// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection.Metadata
{
#if SYSTEM_PRIVATE_CORELIB
    internal sealed
#else
    public
#endif
    class TypeNameParserOptions
    {
        private int _maxRecursiveDepth = int.MaxValue;

        public bool AllowFullyQualifiedName { get; set; } = true;

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

#if SYSTEM_PRIVATE_CORELIB
        internal
#else
        public virtual
#endif
        bool ValidateIdentifier(ReadOnlySpan<char> candidate, bool throwOnError)
        {
            Debug.Assert(!StrictValidation, "TODO (ignoring the compiler warning)");

            if (candidate.IsEmpty)
            {
                if (throwOnError)
                {
                    throw new ArgumentException("TODO");
                }
                return false;
            }

            return true;
        }
    }
}
