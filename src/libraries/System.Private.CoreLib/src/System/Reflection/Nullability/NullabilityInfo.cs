// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace System.Reflection
{
    public sealed class NullabilityInfo
    {
        internal NullabilityInfo(Type type, NullabilityState readState, NullabilityState writeState,
            NullabilityInfo? elementType, NullabilityInfo[] typeArguments)
        {
            Type = type;
            ReadState = readState;
            WriteState = writeState;
            ElementType = elementType;
            TypeArguments = typeArguments;
        }

        public Type Type { get; }
        public NullabilityState ReadState { get; internal set; }
        public NullabilityState WriteState { get; internal set; }
        public NullabilityInfo? ElementType { get; }
        public NullabilityInfo[] TypeArguments { get; }
    }

    public enum NullabilityState
    {
        Unknown,
        NotNull,
        Nullable
    }
}
