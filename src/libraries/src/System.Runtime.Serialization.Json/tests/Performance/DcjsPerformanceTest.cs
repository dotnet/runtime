using System.IO;

namespace System.Runtime.Serialization.Json.Tests.Performance
{
    internal class DcjsSerializerFactory : SerializerFactory
    {
        private static DcjsSerializerFactory _instance = null;
        public static DcjsSerializerFactory GetInstance()
        {
            if (_instance == null)
            {
                _instance = new DcjsSerializerFactory();
            }
            return _instance;
        }

        public override IPerfTestSerializer GetSerializer(SerializerType serializerType)
        {
            switch (serializerType)
            {
                case SerializerType.DataContractJsonSerializer:
                    return new DcjsSerializer();
                default:
                    throw new NotImplementedException();
            }
        }
    }

    internal class DcjsSerializer : IPerfTestSerializer
    {
        private DataContractJsonSerializer _serializer;

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
            _serializer = new DataContractJsonSerializer(obj.GetType());
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
