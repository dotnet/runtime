// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Interop;

[Flags]
public enum MarshallerShape
{
    None = 0x0,
    ToUnmanaged = 0x1,
    CallerAllocatedBuffer = 0x2,
    StatelessPinnableReference = 0x4,
    StatefulPinnableReference = 0x8,
    ToManaged = 0x10,
    GuaranteedUnmarshal = 0x20,
    Free = 0x40,
    OnInvoked = 0x80,
}

public static class ShapeMemberNames
{
    public const string GetPinnableReference = nameof(GetPinnableReference);
    public const string BufferSize = nameof(BufferSize);
    public const string Free = nameof(Free);

    public static class Value
    {
        public static class Stateless
        {
            public const string ConvertToManaged = nameof(ConvertToManaged);
            public const string ConvertToManagedFinally = nameof(ConvertToManagedFinally);
            public const string ConvertToUnmanaged = nameof(ConvertToUnmanaged);
        }

        public static class Stateful
        {
            // Managed to Unmanaged
            public const string FromManaged = nameof(FromManaged);
            public const string ToUnmanaged = nameof(ToUnmanaged);
            // Unmanaged to managed
            public const string ToManaged = nameof(ToManaged);
            public const string ToManagedFinally = nameof(ToManagedFinally);
            public const string FromUnmanaged = nameof(FromUnmanaged);
            // Optional features
            public const string Free = nameof(Free);
            public const string OnInvoked = nameof(OnInvoked);
        }
    }

    public static class LinearCollection
    {
        public static class Stateless
        {
            // Managed to unmanaged
            public const string AllocateContainerForUnmanagedElements = nameof(AllocateContainerForUnmanagedElements);
            public const string GetManagedValuesSource = nameof(GetManagedValuesSource);
            public const string GetUnmanagedValuesDestination = nameof(GetUnmanagedValuesDestination);

            // Unmanaged to managed
            public const string AllocateContainerForManagedElements = nameof(AllocateContainerForManagedElements);
            public const string AllocateContainerForManagedElementsFinally = nameof(AllocateContainerForManagedElementsFinally);
            public const string GetManagedValuesDestination = nameof(GetManagedValuesDestination);
            public const string GetUnmanagedValuesSource = nameof(GetUnmanagedValuesSource);
        }

        public static class Stateful
        {
            // Managed to Unmanaged
            public const string FromManaged = nameof(FromManaged);
            public const string ToUnmanaged = nameof(ToUnmanaged);
            public const string GetManagedValuesSource = nameof(GetManagedValuesSource);
            public const string GetUnmanagedValuesDestination = nameof(GetUnmanagedValuesDestination);
            // Unmanaged to managed
            public const string GetManagedValuesDestination = nameof(GetManagedValuesDestination);
            public const string GetUnmanagedValuesSource = nameof(GetUnmanagedValuesSource);
            public const string ToManaged = nameof(ToManaged);
            public const string ToManagedFinally = nameof(ToManagedFinally);
            public const string FromUnmanaged = nameof(FromUnmanaged);
            // Optional features
            public const string Free = nameof(Free);
            public const string OnInvoked = nameof(OnInvoked);
        }
    }
}
