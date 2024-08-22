// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Interop
{
    /// <summary>
    /// A description of how the code generator will combine the code from different stages of the stub and how it will call into any native context of the stub.
    /// </summary>
    /// <param name="SingleFrameSpansNativeContext">The stub's code is emitted in a single frame and the native context is a callee of that frame.</param>
    /// <param name="AdditionalTemporaryStateLivesAcrossStages">Additional state defined by code generators is preserved across all stages of the generated stub.</param>
    /// <param name="CodeEmitOptions">General options that control how code is emitted.</param>
    /// <param name="Direction">The direction the stub is calling.</param>
    /// <remarks>
    /// Stubs that emit code into a single frame that spans the native context can do two things:
    /// <list type="bullet">
    /// <item> A <c>fixed</c> statement can be used on an individual value in the <see cref="Stage.Pin"/> stage and the pointer can be passed to native code.</item>
    /// <item>Memory can be allocated via the <c>stackalloc</c> keyword and will live through the full native context of the call.</item>
    /// </list>
    ///
    /// When <paramref name="AdditionalTemporaryStateLivesAcrossStages"/> is <c>false</c>, any additional variables can only be considered to have the state they had immediately after a setup phase.
    /// </remarks>
    public sealed record StubCodeContext(
        bool SingleFrameSpansNativeContext,
        bool AdditionalTemporaryStateLivesAcrossStages,
        MarshalDirection Direction)
    {
        public static readonly StubCodeContext DefaultManagedToNativeStub = new(
            SingleFrameSpansNativeContext: true,
            AdditionalTemporaryStateLivesAcrossStages: true,
            Direction: MarshalDirection.ManagedToUnmanaged);

        public static readonly StubCodeContext DefaultNativeToManagedStub = new(
            SingleFrameSpansNativeContext: false,
            AdditionalTemporaryStateLivesAcrossStages: true,
            Direction: MarshalDirection.UnmanagedToManaged);

        public static StubCodeContext CreateElementMarshallingContext(StubCodeContext containingContext)
        {
            return new StubCodeContext(
                SingleFrameSpansNativeContext: false,
                AdditionalTemporaryStateLivesAcrossStages: false,
                Direction: containingContext.Direction)
            {
                ElementIndirectionLevel = containingContext.ElementIndirectionLevel + 1,
            };
        }

        public int ElementIndirectionLevel { get; init; }

        /// <summary>
        /// Compute if the provided element is the return element for the stub that is being generated (not any inner call).
        /// </summary>
        /// <param name="info">The element information</param>
        /// <returns><c>true</c> if the element is in the return position for this stub; otherwise, false.</returns>
        public bool IsInStubReturnPosition(TypePositionInfo info)
        {
            return Direction switch
            {
                MarshalDirection.ManagedToUnmanaged => info.IsManagedReturnPosition,
                MarshalDirection.UnmanagedToManaged => info.IsNativeReturnPosition,
                _ => throw new InvalidOperationException("Stub contexts should not be bidirectional"),
            };
        }

        /// <summary>
        /// Compute if the provided element is the return element for the invocation in the stub.
        /// </summary>
        /// <param name="info">The element information</param>
        /// <returns><c>true</c> if the element is in the return position for the invocation; otherwise, false.</returns>
        public bool IsInInvocationReturnPosition(TypePositionInfo info)
        {
            return Direction switch
            {
                MarshalDirection.ManagedToUnmanaged => info.IsNativeReturnPosition,
                MarshalDirection.UnmanagedToManaged => info.IsManagedReturnPosition,
                _ => throw new InvalidOperationException("Stub contexts should not be bidirectional"),
            };
        }
    };
}
