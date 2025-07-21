// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Metadata
{
    public sealed class TypeNameParseOptions
    {
        private int _maxNodes = 20;

        /// <summary>
        /// Limits the maximum value of <seealso cref="TypeName.GetNodeCount">node count</seealso> that parser can handle.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Setting this to a large value can render <see cref="TypeName"/> susceptible to Denial of Service
        /// attacks when parsing or handling malicious input.
        /// </para>
        /// <para>The default value is 20.</para>
        /// </remarks>
        public int MaxNodes
        {
            get => _maxNodes;
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

                _maxNodes = value;
            }
        }
    }
}
