// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>
    /// Provides access to the ARM Large System Extensions (the Armv8.1 atomic instructions).
    /// </summary>
    /// <remarks>
    /// This type is internal because it exists so that <see cref="System.Threading.Interlocked"/> can
    /// light the instructions up opportunistically: <see cref="IsSupported"/> folds to a constant
    /// whenever LSE is (or is not) part of the compilation's baseline instruction set, and becomes a
    /// runtime check against the CPU features word otherwise - which is the common case for NativeAOT,
    /// whose baseline is typically armv8-a.
    ///
    /// Unlike the Interlocked APIs these never fall back to an ldaxr/stlxr retry loop, so they may
    /// only be called when <see cref="IsSupported"/> is true. The type argument must be an integer
    /// (or an enum over one) no wider than a pointer; sizes of 1 and 2 bytes are only supported by
    /// <see cref="CompareAndSwap{T}"/> and <see cref="Swap{T}"/>. Any other type argument compiles
    /// into a throw.
    /// </remarks>
    [Intrinsic]
    internal abstract class Lse : ArmBase
    {
        internal Lse() { }

        public static new bool IsSupported { get => IsSupported; }

        /// <summary>Compare and swap: <c>casal</c>.</summary>
        public static T CompareAndSwap<T>(ref T location, T value, T comparand) =>
            CompareAndSwap(ref location, value, comparand);

        /// <summary>Atomic add, returning the original value: <c>ldaddal</c>.</summary>
        public static T LoadAdd<T>(ref T location, T value) => LoadAdd(ref location, value);

        /// <summary>Atomically clears the bits of <paramref name="location"/> that are not set in
        /// <paramref name="value"/> - that is, an atomic "and" - and returns the original value.</summary>
        /// <remarks>Lowers to <c>mvn</c> followed by <c>ldclral</c>, since <c>ldclral</c> itself clears the
        /// bits that <em>are</em> set in its operand.</remarks>
        public static T LoadClear<T>(ref T location, T value) => LoadClear(ref location, value);

        /// <summary>Atomic bit set, returning the original value: <c>ldsetal</c>.</summary>
        public static T LoadSet<T>(ref T location, T value) => LoadSet(ref location, value);

        /// <summary>Atomic exchange: <c>swpal</c>.</summary>
        public static T Swap<T>(ref T location, T value) => Swap(ref location, value);
    }
}
