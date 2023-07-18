// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Interop
{
    public abstract record StubCodeContext
    {
        /// <summary>
        /// Code generation stage
        /// </summary>
        public enum Stage
        {
            /// <summary>
            /// Invalid stage
            /// </summary>
            Invalid,

            /// <summary>
            /// Perform any setup required
            /// </summary>
            Setup,

            /// <summary>
            /// Convert managed data to native data
            /// </summary>
            Marshal,

            /// <summary>
            /// Pin data in preparation for calling the generated P/Invoke
            /// </summary>
            Pin,

            /// <summary>
            /// Convert managed data to native data, assuming that any values pinned in the <see cref="Pin"/> stage are pinned.
            /// </summary>
            PinnedMarshal,

            /// <summary>
            /// Call the generated P/Invoke
            /// </summary>
            /// <remarks>
            /// <see cref="IMarshallingGenerator.AsArgument(TypePositionInfo)"/> should provide the
            /// argument to pass to the P/Invoke
            /// </remarks>
            Invoke,

            /// <summary>
            /// Capture native values to ensure that we do not leak if an exception is thrown during unmarshalling
            /// </summary>
            UnmarshalCapture,

            /// <summary>
            /// Convert native data to managed data
            /// </summary>
            Unmarshal,

            /// <summary>
            /// Notify a marshaller object that the Invoke stage and all stages preceding the Invoke stage
            /// successfully completed without any exceptions.
            /// </summary>
            NotifyForSuccessfulInvoke,

            /// <summary>
            /// Perform any cleanup required
            /// </summary>
            Cleanup,

            /// <summary>
            /// Convert native data to managed data even in the case of an exception during
            /// the non-cleanup phases.
            /// </summary>
            GuaranteedUnmarshal
        }

        /// <summary>
        /// The current stage being generated.
        /// </summary>
        public Stage CurrentStage { get; init; } = Stage.Invalid;

        public MarshalDirection Direction { get; init; } = MarshalDirection.ManagedToUnmanaged;

        /// <summary>
        /// Gets the currently targeted framework and version for stub code generation.
        /// </summary>
        /// <returns>A framework value and version.</returns>
        public abstract (TargetFramework framework, Version version) GetTargetFramework();

        /// <summary>
        /// The stub emits code that runs in a single stack frame and the frame spans over the native context.
        /// </summary>
        /// <remarks>
        /// Stubs that emit code into a single frame that spans the native context can do two things:
        /// <list type="bullet">
        /// <item> A <c>fixed</c> statement can be used on an individual value in the <see cref="Stage.Pin"/> stage and the pointer can be passed to native code.</item>
        /// <item>Memory can be allocated via the <c>stackalloc</c> keyword and will live through the full native context of the call.</item>
        /// </list>
        /// </remarks>
        public abstract bool SingleFrameSpansNativeContext { get; }

        /// <summary>
        /// Additional variables other than the {managedIdentifier} and {nativeIdentifier} variables can be added to the stub to track additional state for the marshaller in the stub in the Setup phase, and they will live across all phases of the stub.
        /// </summary>
        /// <remarks>
        /// When this property is <c>false</c>, any additional variables can only be considered to have the state they had immediately after the Setup phase.
        /// </remarks>
        public abstract bool AdditionalTemporaryStateLivesAcrossStages { get; }

        /// <summary>
        /// If this context is a nested context, return the parent context. Otherwise, return <c>null</c>.
        /// </summary>
        public StubCodeContext? ParentContext { get; protected init; }

        /// <summary>
        /// Suffix for all generated native identifiers.
        /// </summary>
        public const string GeneratedNativeIdentifierSuffix = "_native";

        /// <summary>
        /// Get managed and native instance identifiers for the <paramref name="info"/>
        /// </summary>
        /// <param name="info">Object for which to get identifiers</param>
        /// <returns>Managed and native identifiers</returns>
        public virtual (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            return (info.InstanceIdentifier, $"__{info.InstanceIdentifier.TrimStart('@')}{GeneratedNativeIdentifierSuffix}");
        }

        /// <summary>
        /// Compute identifiers that are unique for this generator
        /// </summary>
        /// <param name="info">TypePositionInfo the new identifier is used in service of.</param>
        /// <param name="name">Name of variable.</param>
        /// <returns>New identifier name for use.</returns>
        public virtual string GetAdditionalIdentifier(TypePositionInfo info, string name)
        {
            return $"{GetIdentifiers(info).native}__{name}";
        }

        /// <summary>
        /// Compute if the provided element is the return element for the stub that is being generated (not any inner call).
        /// </summary>
        /// <param name="info">The element information</param>
        /// <returns><c>true</c> if the element is in the return position for this stub; otherwise, false.</returns>
        public bool IsInStubReturnPosition(TypePositionInfo info)
        {
            if (Direction == MarshalDirection.ManagedToUnmanaged)
            {
                return info.IsManagedReturnPosition;
            }
            else if (Direction == MarshalDirection.UnmanagedToManaged)
            {
                return info.IsNativeReturnPosition;
            }

            throw new InvalidOperationException("Stub contexts should not be bidirectional");
        }
    }
}
