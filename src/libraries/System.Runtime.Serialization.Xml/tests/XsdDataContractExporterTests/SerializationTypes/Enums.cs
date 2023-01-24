using System;
using System.Runtime.Serialization;

#if UseSeparateAssemblyNamespace
namespace SerializableTypes.XsdDataContractExporterTests
#else
namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests
#endif
{
    public class Enums
    {
        public enum Mode
        {   
            NotSpecified,
            Single,
            Multiple,
        }

        [DataContract(Name="DifferentMode")]
        public enum Mode2
        {   
            [EnumMember(Value="None")]
            NotSpecified = -1,
            Single,
            [EnumMember]
            Multiple,
        }

        public enum Color : byte
        {
            Red,
            Green,
            Blue,
            Reserved,
        }

        //[CLSCompliant(false)]
        public enum ULongRange : ulong
        {
            Min = UInt64.MinValue,
            Small = 1,
            Medium = 10,
            Large = 100,
            Max = UInt64.MaxValue,
        }

        [Flags]
        public enum FlagsEnum
        {
            Foo = 1,
            Bar = 2,
            Baz = 4,
            Bazooka = 8
        }

        [Flags]
        [DataContract(Namespace="http://special2.tempuri.org")]
        public enum LongRange : long
        {
            [EnumMember]
            Min = Int64.MinValue,
            [EnumMember(Value="Small")]
            Value1 = 1L,
            Value10 = 10L,
            [EnumMember(Value="Medium")]
            Value100 = 100L,
            [EnumMember(Value="Large")]
            Value1000 = 1000L,
            [EnumMember]
            Max = Int64.MaxValue,
        }

        [DataContract]
        public class EnumContainer
        {
            [DataMember]
            Mode mode;

            [DataMember]
            Mode2 mode2;

            [DataMember]
            Color color;

            [DataMember]
            ULongRange enumValue;

            [DataMember]
            FlagsEnum flagsEnum;

            [DataMember]
            LongRange longFlagsEnum;

            public EnumContainer()
            {
            }
            
            [DataContract]
            public class NestedEnumContainer
            {
                [DataMember]
                Mode mode;

                NestedEnumContainer()
                {
                }
            }

            internal enum NestedSimpleEnum
            {
                Negative = -1,
                NoComment = 0,
                Affirmative = 1,
            }
        }
    }

    public enum Mode
    {   
        NotSpecified,
        Single,
        Multiple,
    }

    [DataContract(Name="Mode")]
    public enum DifferentMode
    {   
        [EnumMember(Value="NotSpecified")]
        Member1,
        [EnumMember]
        Single=1,
        [EnumMember(Value="Multiple")]
        Mult,
    }

}


