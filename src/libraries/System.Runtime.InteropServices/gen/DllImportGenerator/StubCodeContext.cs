using System;

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
            KeepAlive
        }

        public Stage CurrentStage { get; protected set; }

        public abstract bool PinningSupported { get; }

        public abstract bool StackSpaceUsable { get; }

        protected const string GeneratedNativeIdentifierSuffix = "_gen_native";

        /// <summary>
        /// Get managed and native instance identifiers for the <paramref name="info"/>
        /// </summary>
        /// <param name="info">Object for which to get identifiers</param>
        /// <returns>Managed and native identifiers</returns>
        public virtual (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            return (info.InstanceIdentifier, $"__{info.InstanceIdentifier}{GeneratedNativeIdentifierSuffix}");
        }
    }
}
