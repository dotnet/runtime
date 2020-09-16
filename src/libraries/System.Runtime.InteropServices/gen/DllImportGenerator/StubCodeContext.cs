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
            Cleanup
        }

        public Stage CurrentStage { get; protected set; }

        /// <summary>
        /// Identifier for managed return value
        /// </summary>
        public const string ReturnIdentifier = "__retVal";

        /// <summary>
        /// Identifier for native return value
        /// </summary>
        /// <remarks>Same as the managed identifier by default</remarks>
        public string ReturnNativeIdentifier { get; private set; } = ReturnIdentifier;

        private const string InvokeReturnIdentifier = "__invokeRetVal";
        private const string generatedNativeIdentifierSuffix = "_gen_native";

        /// <summary>
        /// Generate an identifier for the native return value and update the context with the new value
        /// </summary>
        /// <returns>Identifier for the native return value</returns>
        public string GenerateReturnNativeIdentifier()
        {
            if (CurrentStage != Stage.Setup)
                throw new InvalidOperationException();

            // Update the native identifier for the return value
            ReturnNativeIdentifier = $"{ReturnIdentifier}{generatedNativeIdentifierSuffix}";
            return ReturnNativeIdentifier;
        }

        /// <summary>
        /// Get managed and native instance identifiers for the <paramref name="info"/>
        /// </summary>
        /// <param name="info">Object for which to get identifiers</param>
        /// <returns>Managed and native identifiers</returns>
        public (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            string managedIdentifier;
            string nativeIdentifier;
            if (info.IsManagedReturnPosition && !info.IsNativeReturnPosition)
            {
                managedIdentifier = ReturnIdentifier;
                nativeIdentifier = ReturnNativeIdentifier;
            }
            else if (!info.IsManagedReturnPosition && info.IsNativeReturnPosition)
            {
                managedIdentifier = InvokeReturnIdentifier;
                nativeIdentifier = InvokeReturnIdentifier;
            }
            else
            {
                managedIdentifier = info.IsManagedReturnPosition
                    ? ReturnIdentifier
                    : info.InstanceIdentifier;

                nativeIdentifier = info.IsNativeReturnPosition
                    ? ReturnNativeIdentifier
                    : $"__{info.InstanceIdentifier}{generatedNativeIdentifierSuffix}";
            }

            return (managedIdentifier, nativeIdentifier);
        }
    }
}
