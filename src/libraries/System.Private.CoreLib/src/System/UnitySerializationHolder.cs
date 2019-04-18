// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// Holds Null class for which we guarantee that there is only ever one instance of.
    /// This only exists for compatibility with .NET Framework.
    /// </summary>
    [Serializable]
    // Needs to be public to support binary serialization compatibility
    public sealed class UnitySerializationHolder : ISerializable, IObjectReference
    {
        internal const int NullUnity = 0x0002;
        private readonly int _unityType;
        private readonly string? _data;

        /// <summary>
        /// A helper method that returns the SerializationInfo that a class utilizing 
        /// UnitySerializationHelper should return from a call to GetObjectData. It contains
        /// the unityType (defined above) and any optional data (used only for the reflection types).
        /// </summary>
        internal static void GetUnitySerializationInfo(SerializationInfo info, int unityType)
        {
            info.SetType(typeof(UnitySerializationHolder));
            info.AddValue("Data", null, typeof(string));
            info.AddValue("UnityType", unityType);
            info.AddValue("AssemblyName", string.Empty);
        }

        public UnitySerializationHolder(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            // We are ignoring any other serialization input as we are only concerned about DBNull.
            // We also store data and use it for erorr logging.
            _unityType = info.GetInt32("UnityType");
            _data = info.GetString("Data");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) =>
            throw new NotSupportedException(SR.NotSupported_UnitySerHolder);

        public object GetRealObject(StreamingContext context)
        {
            // We are only support deserializing DBNull and throwing for everything else.
            if (_unityType != NullUnity)
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidUnity, _data ?? "UnityType"));
            }

            // We are always returning the same DBNull instance.
            return DBNull.Value;
        }
    }
}
