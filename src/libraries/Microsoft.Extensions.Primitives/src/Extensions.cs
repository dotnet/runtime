// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.Extensions.Primitives
{
    public static class Extensions
    {
        /// <summary>
        /// Add the given <see cref="StringSegment"/> to the <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> to add to.</param>
        /// <param name="segment">The <see cref="StringSegment"/> to add.</param>
        /// <returns>The original <see cref="StringBuilder"/>.</returns>
        public static StringBuilder Append(this StringBuilder builder, StringSegment segment)
        {
            return builder.Append(segment.Buffer, segment.Offset, segment.Length);
        }
    }
}
