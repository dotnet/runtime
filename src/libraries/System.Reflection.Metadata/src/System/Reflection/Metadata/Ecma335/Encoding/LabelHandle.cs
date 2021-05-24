// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata.Ecma335
{
    public readonly struct LabelHandle : IEquatable<LabelHandle>
    {
        /// <summary>
        /// 1-based id identifying the label within the context of a <see cref="ControlFlowBuilder"/>.
        /// </summary>
        public int Id { get; }

        internal LabelHandle(int id)
        {
            Debug.Assert(id >= 1);
            Id = id;
        }

        public bool IsNil => Id == 0;

        public bool Equals(LabelHandle other) => Id == other.Id;
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is LabelHandle labelHandle && Equals(labelHandle);
        public override int GetHashCode() => Id.GetHashCode();

        public static bool operator ==(LabelHandle left, LabelHandle right) => left.Equals(right);
        public static bool operator !=(LabelHandle left, LabelHandle right) => !left.Equals(right);
    }
}
