using System.Runtime.Serialization;

namespace SerializationTestTypes
{
    [DataContract]
    public class ObjectContainer
    {
        [DataMember]
        private object _data;

        [DataMember]
        private object _data2;

        public ObjectContainer(object input)
        {
            _data = input;
            _data2 = _data;
        }

        public object Data
        {
            get { return _data; }
        }

        public object Data2
        {
            get { return _data2; }
        }
    }
}
