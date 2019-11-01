// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Delegate | AttributeTargets.Interface)]
    public sealed class EditorBrowsableAttribute : Attribute
    {
        public EditorBrowsableAttribute(EditorBrowsableState state)
        {
            State = state;
        }

        public EditorBrowsableAttribute() : this(EditorBrowsableState.Always)
        {
        }

        public EditorBrowsableState State { get; }

        public override bool Equals(object? obj)
        {
            if (obj == this)
            {
                return true;
            }

            return (obj is EditorBrowsableAttribute other) && other.State == State;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
