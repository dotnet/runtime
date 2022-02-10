// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// Describes effects to be applied to registers.
    /// </summary>
    internal struct DerivativeEffect
    {
        public enum EffectKind
        {
            CaptureStart,
            CaptureEnd,
        };

        public EffectKind Kind;
        public int IntArg0;

        public DerivativeEffect(EffectKind kind, int intArg0)
        {
            Kind = kind;
            IntArg0 = intArg0;
        }
    }
}
