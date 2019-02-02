// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

namespace System.Runtime.Serialization
{
    using System;
    using System.Security;
    using System.Runtime.CompilerServices;

    internal sealed class SurrogateDataContract : DataContract
    {
        private SurrogateDataContractCriticalHelper _helper;

        internal SurrogateDataContract(Type type, ISerializationSurrogate serializationSurrogate)
            : base(new SurrogateDataContractCriticalHelper(type, serializationSurrogate))
        {
            _helper = base.Helper as SurrogateDataContractCriticalHelper;
        }

        internal ISerializationSurrogate SerializationSurrogate
        {
            get { return _helper.SerializationSurrogate; }
        }

        public override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
        {
            SerializationInfo serInfo = new SerializationInfo(UnderlyingType, XmlObjectSerializer.FormatterConverter, !context.UnsafeTypeForwardingEnabled);
            SerializationSurrogateGetObjectData(obj, serInfo, context.GetStreamingContext());
            context.WriteSerializationInfo(xmlWriter, UnderlyingType, serInfo);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private object SerializationSurrogateSetObjectData(object obj, SerializationInfo serInfo, StreamingContext context)
        {
            return SerializationSurrogate.SetObjectData(obj, serInfo, context, null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static object GetRealObject(IObjectReference obj, StreamingContext context)
        {
            return obj.GetRealObject(context);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private object GetUninitializedObject(Type objType)
        {
            return FormatterServices.GetUninitializedObject(objType);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SerializationSurrogateGetObjectData(object obj, SerializationInfo serInfo, StreamingContext context)
        {
            SerializationSurrogate.GetObjectData(obj, serInfo, context);
        }

        public override object ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context)
        {
            xmlReader.Read();
            Type objType = UnderlyingType;
            object obj = objType.IsArray ? Array.CreateInstance(objType.GetElementType(), 0) : GetUninitializedObject(objType);
            context.AddNewObject(obj);
            string objectId = context.GetObjectId();
            SerializationInfo serInfo = context.ReadSerializationInfo(xmlReader, objType);
            object newObj = SerializationSurrogateSetObjectData(obj, serInfo, context.GetStreamingContext());
            if (newObj == null)
                newObj = obj;
            if (newObj is IDeserializationCallback)
                ((IDeserializationCallback)newObj).OnDeserialization(null);
            if (newObj is IObjectReference)
                newObj = GetRealObject((IObjectReference)newObj, context.GetStreamingContext());
            context.ReplaceDeserializedObject(objectId, obj, newObj);
            xmlReader.ReadEndElement();
            return newObj;
        }

        private class SurrogateDataContractCriticalHelper : DataContract.DataContractCriticalHelper
        {
            ISerializationSurrogate serializationSurrogate;

            internal SurrogateDataContractCriticalHelper(Type type, ISerializationSurrogate serializationSurrogate)
                : base(type)
            {
                this.serializationSurrogate = serializationSurrogate;
                string name, ns;
                DataContract.GetDefaultStableName(DataContract.GetClrTypeFullName(type), out name, out ns);
                SetDataContractName(CreateQualifiedName(name, ns));
            }

            internal ISerializationSurrogate SerializationSurrogate
            {
                get { return serializationSurrogate; }
            }
        }
    }
}
