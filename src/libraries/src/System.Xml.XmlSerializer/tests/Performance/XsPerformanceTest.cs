using System.IO;
using System.Runtime.Serialization;

namespace System.Xml.XmlSerializer.Tests.Performance
{
    internal class XsSerializerFactory : SerializerFactory
    {
        private static XsSerializerFactory _instance = null;
        public static XsSerializerFactory GetInstance()
        {
            if (_instance == null)
            {
                _instance = new XsSerializerFactory();
            }
            return _instance;
        }

        public override IPerfTestSerializer GetSerializer(SerializerType serializerType)
        {
            switch (serializerType)
            {
                case SerializerType.XmlSerializer:
                    return new XsSerializer();
                default:
                    throw new NotImplementedException();
            }
        }
    }

    internal class XsSerializer : IPerfTestSerializer
    {
        private Serialization.XmlSerializer _serializer;

        public void Deserialize(string s)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream stream)
        {
            // Assumption: Deserialize() is always called after Init()
            // Assumption: stream != null
            stream.Position = 0;
            _serializer.Deserialize(stream);
        }

        public void Init(object obj)
        {
            _serializer = new Serialization.XmlSerializer(obj.GetType());
        }

        public void Serialize(object obj, Stream stream)
        {
            // Assumption: Serialize() is always called after Init()
            // Assumption: stream != null and stream position will be reset to 0 after this method
            _serializer.Serialize(stream, obj);
            stream.Position = 0;
        }
    }
}
