// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Metadata
{
#if SYSTEM_PRIVATE_CORELIB
    internal
#else
    public
#endif
    sealed class TypeNameParserOptions
    {
        private int _maxComplexity = int.MaxValue;

        public bool AllowFullyQualifiedName { get; set; } = true;

        /// <summary>
        /// Limits the maximum value of <seealso cref="TypeName.TotalComplexity"/> that parser can handle.
        /// </summary>
        public int MaxTotalComplexity
        {
            get => _maxComplexity;
            set
            {
#if NET8_0_OR_GREATER
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0, nameof(value));
#else
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(paramName: nameof(value));
                }
#endif

                _maxComplexity = value;
            }
        }

        /// <summary>
        /// Extends ECMA-335 standard limitations with a set of opinionated rules based on most up-to-date security knowledge.
        /// </summary>
        /// <remarks>
        /// When parsing AssemblyName, only Version, Culture and PublicKeyToken attributes are allowed.
        /// The comparison is also case-sensitive (in contrary to <seealso cref="AssemblyName(string)"/> constructor).
        /// </remarks>
        public bool StrictValidation { get; set; }
    }
}
