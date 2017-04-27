using System;
using System.Runtime.Serialization;

namespace SerializationTestTypes
{
    #region Simple Class Structs with Ref

    [DataContract(IsReference = true)]
    public class SimpleDC
    {
        [DataMember]
        public string Data;
        public SimpleDC() { }
        public SimpleDC(bool init)
        {
            Data = DateTime.MaxValue.ToLongTimeString();
        }
    }

    #endregion
}
