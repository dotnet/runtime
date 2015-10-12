// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.IO;
using Microsoft.Xunit.Performance;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace System.Runtime.Tests
{
    public class Perf_Deserialization
    {
        [Benchmark]
        public void DeserializeLargeObjectGraph()
        {
            var obj = CreateObject(SimpleType.GraphSize, 0);
            var serializer = new DataContractSerializer(typeof(SimpleType));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, obj);
                stream.Position = 0;

                foreach (var iteration in Benchmark.Iterations)
                    using (iteration.StartMeasurement())
                        for (int i = 0; i < 3; i++)
                        {
                            var obj2 = serializer.ReadObject(stream);
                            stream.Position = 0;
                        }
            }
        }

        public SimpleType CreateObject(int height, int id)
        {
            var index = height * 10 + id;
            var obj = new SimpleType()
            {
                IntProperty = index,
                StringProperty = index + " string value",
                EnumProperty = (MyEnum)(index % (Enum.GetNames(typeof(MyEnum)).Length)),
                CollectionProperty = new List<string>(),
                SimpleTypeList = new List<SimpleType>()
            };
            for (var i = 0; i < SimpleType.CollectionSize; ++i)
            {
                obj.CollectionProperty.Add(index + "." + i);
            }
            if (height > 0)
            {
                for (var i = 0; i < SimpleType.SimpleTypeListSize; ++i)
                {
                    obj.SimpleTypeList.Add(CreateObject(height - 1, i));
                }
            }
            return obj;
        }

        public enum MyEnum { Value1, Value2, Value3 }

        public class SimpleType
        {
            public const int SimpleTypeListSize = 2;
            public const int GraphSize = 17;
            public const int CollectionSize = 7;

            public string StringProperty { get; set; }
            public int IntProperty { get; set; }
            public MyEnum EnumProperty { get; set; }
            public List<string> CollectionProperty { get; set; }
            public List<SimpleType> SimpleTypeList { get; set; }
        }
    }
}
