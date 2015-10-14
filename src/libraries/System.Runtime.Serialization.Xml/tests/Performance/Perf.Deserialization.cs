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
            SimpleType obj = CreateObject(0, 0, -1);
            var serializer = new DataContractSerializer(typeof(SimpleType));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, obj);

                foreach (var iteration in Benchmark.Iterations)
                    using (iteration.StartMeasurement())
                        for (int i = 0; i < 3; i++)
                        {
                            stream.Position = 0;
                            SimpleType obj2 = (SimpleType)serializer.ReadObject(stream);
                        }
            }
        }

        public SimpleType CreateObject(int height, int parentId, int currentId)
        {
            int index = parentId * SimpleType.SimpleTypeListSize + (currentId + 1);
            var obj = new SimpleType()
            {
                IntProperty = index,
                StringProperty = index + " string value",
                EnumProperty = (MyEnum)(index % (Enum.GetNames(typeof(MyEnum)).Length)),
                CollectionProperty = new List<string>(),
                SimpleTypeList = new List<SimpleType>()
            };
            for (int i = 0; i < SimpleType.CollectionSize; ++i)
            {
                obj.CollectionProperty.Add(index + "." + i);
            }
            if (height < SimpleType.GraphSize)
            {
                for (int i = 0; i < SimpleType.SimpleTypeListSize; ++i)
                {
                    obj.SimpleTypeList.Add(CreateObject(height + 1, index, i));
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
