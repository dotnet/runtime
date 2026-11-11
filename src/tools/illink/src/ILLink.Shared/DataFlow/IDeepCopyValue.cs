// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.DataFlow
{
    public interface IValueWithMutableState
    {
        // True if values can change without replacing the containing lattice value.
        bool HasMutableState { get; }
    }

    // Adds ability to deep copy a value
    public interface IDeepCopyValue<TSingleValue>
    {
        public TSingleValue DeepCopy();
    }
}
