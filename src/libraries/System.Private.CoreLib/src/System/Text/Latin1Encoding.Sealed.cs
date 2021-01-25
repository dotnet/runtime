// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text
{
    internal partial class Latin1Encoding : Encoding
    {
        /// <summary>
        /// A special instance of <see cref="Latin1Encoding"/> that is initialized with "don't throw on invalid sequences;
        /// use best-fit substitution instead" semantics. This type allows for devirtualization of calls made directly
        /// off of <see cref="Encoding.Latin1"/>.
        /// </summary>
        internal sealed class Latin1EncodingSealed : Latin1Encoding
        {
            public override object Clone()
            {
                // The base implementation of Encoding.Clone calls object.MemberwiseClone and marks the new object mutable.
                // We don't want to do this because it violates the invariants we have set for the sealed type.
                // Instead, we'll create a new instance of the base Latin1Encoding type and mark it mutable.

                return new Latin1Encoding()
                {
                    IsReadOnly = false
                };
            }
        }
    }
}
