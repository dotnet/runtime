// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public sealed class NonSerializedAttribute : Attribute
    {
        public NonSerializedAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
    public sealed class SerializableAttribute : Attribute
    {
        public SerializableAttribute()
        {
        }
    }
}

namespace System.Runtime.Serialization
{
    public interface ISerializable
    {
        void GetObjectData(SerializationInfo info, StreamingContext context);
    }

    public interface IDeserializationCallback
    {
        void OnDeserialization(object sender);
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public sealed class OptionalFieldAttribute : Attribute
    {
        public int VersionAdded
        {
            get { return default(int); }
            set { }
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class OnSerializingAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class OnSerializedAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class OnDeserializingAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class OnDeserializedAttribute : Attribute
    {
    }

    public sealed class SerializationInfo
    {
        private SerializationInfo() 
        { 
        }
    }

    [Serializable]
    [Flags]
    public enum StreamingContextStates
    {
        All = 0xFF,
    }
}
