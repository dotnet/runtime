using System;
using System.Collections.Generic;

namespace Microsoft.Interop
{
    internal abstract class StubCodeContext
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
            /// Call the generated P/Invoke
            /// </summary>
            /// <remarks>
            /// <see cref="IMarshallingGenerator.AsArgument(TypePositionInfo)"/> should provide the
            /// argument to pass to the P/Invoke
            /// </remarks>
            Invoke,

            /// <summary>
            /// Convert native data to managed data
            /// </summary>
            Unmarshal,

            /// <summary>
            /// Perform any cleanup required
            /// </summary>
            Cleanup,
            
            /// <summary>
            /// Keep alive any managed objects that need to stay alive across the call.
            /// </summary>
            KeepAlive,

            /// <summary>
            /// Convert native data to managed data even in the case of an exception during
            /// the non-cleanup phases.
            /// </summary>
            GuaranteedUnmarshal
        }

        public Stage CurrentStage { get; set; } = Stage.Invalid;

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
        public StubCodeContext? ParentContext { get; protected set; }

        public const string GeneratedNativeIdentifierSuffix = "_gen_native";

        /// <summary>
        /// Get managed and native instance identifiers for the <paramref name="info"/>
        /// </summary>
        /// <param name="info">Object for which to get identifiers</param>
        /// <returns>Managed and native identifiers</returns>
        public virtual (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            return (info.InstanceIdentifier, $"__{info.InstanceIdentifier.TrimStart('@')}{GeneratedNativeIdentifierSuffix}");
        }

        public virtual string GetAdditionalIdentifier(TypePositionInfo info, string name)
        {
            return $"{GetIdentifiers(info).native}__{name}";
        }
    }
}
