// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace System.Reflection
{
    public sealed class NullabilityInfo
    {
        /*internal NullabilityInfo(Type type, NullableState state)
        {
            Type = type;
            ReadState = state;
            WriteState = state;
        }

        internal NullabilityInfo(Type type, NullableState readState, NullableState writeState)
        {
            Type = type;
            ReadState = readState;
            WriteState = writeState;
        }*/

        internal NullabilityInfo(Type type, NullableState readState, NullableState writeState,
            ReadOnlyCollection<NullableState>? arrayElements, ReadOnlyCollection<NullableState>? typeArguments)
        {
            Type = type;
            ReadState = readState;
            WriteState = writeState;
            ArrayElements = arrayElements;
            TypeArguments = typeArguments;
        }

        public Type Type { get; }
        public NullableState ReadState { get; internal set; }
        public NullableState WriteState { get; internal set; }
        public ReadOnlyCollection<NullableState>? ArrayElements { get; }
        public ReadOnlyCollection<NullableState>? TypeArguments { get; }
    }

    public enum NullableState
    {
        Unknown,
        NonNullable,
        Nullable,
        NotNullableWhen, // Has NotNullWhenAttribute or NotNullIfNotNullAttribute, check CustomAttributes for the attribute and value
        NullableWhen // Has MaybeNullWhenAttribute check CustomAttributes for the value
    }
}
