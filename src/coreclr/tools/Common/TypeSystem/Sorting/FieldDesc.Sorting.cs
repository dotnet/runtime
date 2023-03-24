// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Functionality related to deterministic ordering of types and members
    public partial class FieldDesc
    {
        /// <summary>
        /// Gets an identifier that is the same for all instances of this <see cref="FieldDesc"/>
        /// descendant, but different from the <see cref="ClassCode"/> of any other descendant.
        /// </summary>
        /// <remarks>
        /// This is really just a number, ideally produced by "new Random().Next(int.MinValue, int.MaxValue)".
        /// If two manage to conflict (which is pretty unlikely), just make a new one...
        /// </remarks>
        protected internal abstract int ClassCode { get; }

        // Note to implementers: the type of `other` is actually the same as the type of `this`.
        protected internal abstract int CompareToImpl(FieldDesc other, TypeSystemComparer comparer);
    }
}
