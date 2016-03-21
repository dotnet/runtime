using System.IO;

namespace System.Runtime.Serialization.Xml.Tests.Performance
{
    internal class DcsSerializerFactory : SerializerFactory
    {
        private static DcsSerializerFactory _instance = null;
        public static DcsSerializerFactory GetInstance()
        {
            if (_instance == null)
            {
                _instance = new DcsSerializerFactory();
            }
            return _instance;
        }

        public override IPerfTestSerializer GetSerializer(SerializerType serializerType)
        {
            switch (serializerType)
            {
                case SerializerType.DataContractSerializer:
                    return new DcsSerializer();
                default:
                    throw new NotImplementedException();
            }
        }
    }

    internal class DcsSerializer : IPerfTestSerializer
    {
        private DataContractSerializer _serializer;

        public void Deserialize(string s)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream stream)
        {
            // Assumption: Deserialize() is always called after Init()
            // Assumption: stream != null
            stream.Position = 0;
            _serializer.ReadObject(stream);
        }

        public void Init(object obj)
        {
            _serializer = new DataContractSerializer(obj.GetType());
        }

        public void Serialize(object obj, Stream stream)
        {
            // Assumption: Serialize() is always called after Init()
            // Assumption: stream != null and stream position will be reset to 0 after this method
            _serializer.WriteObject(stream, obj);
            stream.Position = 0;
        }
    }
}
