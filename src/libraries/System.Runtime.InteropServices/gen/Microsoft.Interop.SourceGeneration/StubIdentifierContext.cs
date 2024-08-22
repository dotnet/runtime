// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Interop
{
    public abstract record StubIdentifierContext
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
            /// <see cref="IUnboundMarshallingGenerator.AsArgument(TypePositionInfo)"/> should provide the
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
            /// Perform any cleanup required on caller allocated resources
            /// </summary>
            CleanupCallerAllocated,

            /// <summary>
            /// Perform any cleanup required on callee allocated resources
            /// </summary>
            CleanupCalleeAllocated,

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

        public CodeEmitOptions CodeEmitOptions { get; init; }

        /// <summary>
        /// The context in which the code will be generated.
        /// </summary>
        public required StubCodeContext CodeContext { get; init; }

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
    }
}
