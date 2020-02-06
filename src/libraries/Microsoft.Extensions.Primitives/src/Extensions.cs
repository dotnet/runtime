// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
