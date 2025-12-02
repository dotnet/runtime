// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// Describes effects to record capture start and end points.
    /// </summary>
    /// <remarks>
    /// These are applied into registers (arrays of positions for all capture starts and ends) and amount to assignments
    /// of the current input position. Effects are generated and associated with transitions in effect-aware versions
    /// of CreateDerivative in SymbolicRegexNode.
    /// </remarks>
    internal readonly struct DerivativeEffect(DerivativeEffectKind kind, int captureNumber)
    {
        public DerivativeEffectKind Kind { get; } = kind;
        public int CaptureNumber { get; } = captureNumber;
    }

    internal enum DerivativeEffectKind
    {
        /// <summary>Effect to assign the current input position to an index in the capture starts array.</summary>
        CaptureStart,
        /// <summary>Effect to assign the current input position to an index in the capture ends array.</summary>
        CaptureEnd,
    };
}
