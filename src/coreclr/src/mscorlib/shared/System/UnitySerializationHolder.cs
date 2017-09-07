// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Reflection;

namespace System
{
    /// <summary>
    /// Holds Null class for which we guarantee that there is only ever one instance of.
    /// This only exists for backwarts compatibility with 
    /// </summary>
#if CORECLR
    internal
#else
    public  // On CoreRT, this must be public because of the Reflection.Core/CoreLib divide and the need to whitelist past the ReflectionBlock.
#endif
    sealed class UnitySerializationHolder : ISerializable, IObjectReference
    {
        internal const int NullUnity = 0x0002;

        public static void GetUnitySerializationInfo(SerializationInfo info, int unityType, string data, Assembly assembly)
        {
            // A helper method that returns the SerializationInfo that a class utilizing 
            // UnitySerializationHelper should return from a call to GetObjectData.  It contains
            // the unityType (defined above) and any optional data (used only for the reflection
            // types.)

            info.SetType(typeof(UnitySerializationHolder));
            info.AddValue("Data", data, typeof(string));
            info.AddValue("UnityType", unityType);
            info.AddValue("AssemblyName", assembly?.FullName ?? string.Empty);
        }

        public UnitySerializationHolder(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) =>
            throw new NotSupportedException(SR.NotSupported_UnitySerHolder);

        public object GetRealObject(StreamingContext context)
        {
            // We are always returning the same DBNull instance and ignoring serialization input.

            return DBNull.Value;
        }
    }
}
