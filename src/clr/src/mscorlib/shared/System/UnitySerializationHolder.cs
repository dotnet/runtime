// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Reflection;
using System.Diagnostics.Contracts;

namespace System
{
    // Holds classes (Empty, Null, Missing) for which we guarantee that there is only ever one instance of.
#if CORECLR
    internal
#else
    public  // On CoreRT, this must be public because of the Reflection.Core/CoreLib divide and the need to whitelist past the ReflectionBlock.
#endif
    class UnitySerializationHolder : ISerializable, IObjectReference
    {
        internal const int NullUnity = 0x0002;
        const string DataName = "Data";
        const string UnityTypeName = "UnityType";
        const string AssemblyNameName = "AssemblyName";

        private readonly string _data;
        private readonly string _assemblyName;
        private int _unityType;

        public static void GetUnitySerializationInfo(SerializationInfo info, int unityType, string data, Assembly assembly)
        {
            // A helper method that returns the SerializationInfo that a class utilizing 
            // UnitySerializationHelper should return from a call to GetObjectData.  It contains
            // the unityType (defined above) and any optional data (used only for the reflection
            // types.)

            info.SetType(typeof(UnitySerializationHolder));
            info.AddValue(DataName, data, typeof(string));
            info.AddValue(UnityTypeName, unityType);
            info.AddValue(AssemblyNameName, assembly?.FullName ?? string.Empty);
        }

        public UnitySerializationHolder(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            _unityType = info.GetInt32(UnityTypeName);
            _data = info.GetString(DataName);
            _assemblyName = info.GetString(AssemblyNameName);
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) =>
            throw new NotSupportedException(SR.NotSupported_UnitySerHolder);

        public virtual object GetRealObject(StreamingContext context) => DBNull.Value;
    }
}
