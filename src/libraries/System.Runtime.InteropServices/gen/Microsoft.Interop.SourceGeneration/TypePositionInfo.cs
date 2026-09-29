// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Interop
{

    /// <summary>
    /// Describes how to marshal the contents of a value in comparison to the value itself.
    /// Only makes sense for array-like types. For example, an "out" array doesn't change the
    /// pointer to the array value, but it marshals the contents of the native array back to the
    /// contents of the managed array.
    /// </summary>
    [Flags]
    public enum ByValueContentsMarshalKind
    {
        /// <summary>
        /// Marshal contents from managed to native only.
        /// This is the default behavior.
        /// </summary>
        Default = 0x0,
        /// <summary>
        /// Marshal contents from managed to native only.
        /// This is the default behavior.
        /// </summary>
        In = 0x1,
        /// <summary>
        /// Marshal contents from native to managed only.
        /// </summary>
        Out = 0x2,
        /// <summary>
        /// Marshal contents both to and from native.
        /// </summary>
        InOut = In | Out
    }

    /// <summary>
    /// Positional type information involved in unmanaged/managed scenarios.
    /// </summary>
    public sealed record TypePositionInfo(ManagedTypeInfo ManagedType, MarshallingInfo MarshallingAttributeInfo)
    {
        public const int UnsetIndex = int.MinValue;
        public const int ReturnIndex = UnsetIndex + 1;
        public const int ErrorIndex = UnsetIndex + 2;
        public const int ExceptionIndex = ErrorIndex;

        public static bool IsSpecialIndex(int index)
        {
            return index is UnsetIndex or ReturnIndex or ErrorIndex;
        }

        public static int IncrementIndex(int index)
        {
            return IsSpecialIndex(index) ? index : index + 1;
        }

        public string InstanceIdentifier { get; init; } = string.Empty;

        public RefKind RefKind { get; init; } = RefKind.None;

        public bool IsByRef => RefKind != RefKind.None;

        public ScopedKind ScopedKind { get; init; } = ScopedKind.None;

        public ByValueContentsMarshalKind ByValueContentsMarshalKind { get; init; }

        public bool IsManagedReturnPosition { get => ManagedIndex == ReturnIndex; }
        public bool IsNativeReturnPosition { get => NativeIndex == ReturnIndex; }
        public bool IsErrorHandlingPosition { get; init; }
        public bool IsManagedIdentifierSynthetic => IsSpecialIndex(ManagedIndex);

        public int ManagedIndex { get; init; } = UnsetIndex;
        public int NativeIndex { get; init; } = UnsetIndex;
        public bool IsExplicitThis { get; init; }

        public bool PositionsEqual(TypePositionInfo other)
        {
            return ManagedIndex == other.ManagedIndex && NativeIndex == other.NativeIndex;
        }

    }

    public enum ErrorHandlingLocation
    {
        None = -1,
        ReturnValue = 0,
        LastParameter = 1,
        HiddenReturnValue = 2,
        HiddenLastParameter = 3,
    }

    public sealed record ErrorHandlingInfo(
        ManagedTypeInfo ManagedType,
        MarshallingInfo MarshallingInfo,
        ErrorHandlingLocation Location);
}
