// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Newtonsoft.Json.Bson;
using Xunit;

namespace Serialization
{
public class JsonBenchmarks
{

#if DEBUG
    public const int Iterations = 1;
    public const int JsonNetIterations = 1;
#else
    public const int Iterations = 30000;
    public const int JsonNetIterations = 90000;
#endif

    const string DataContractXml = @"<JsonBenchmarks.TestObject xmlns=""http://schemas.datacontract.org/2004/07/Serialization"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Id>33</Id><Name>SqMtx</Name><Results xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:double>101.3</a:double><a:double>99.8</a:double><a:double>99.6</a:double><a:double>100.4</a:double></Results><WhenRun>2015-01-01T00:00:00-08:00</WhenRun></JsonBenchmarks.TestObject>";

    const string DataContractJson = @"{""Id"":33,""Name"":""SqMtx"",""Results"":[101.3,99.8,99.6,100.4],""WhenRun"":""\/Date(1420099200000-0800)\/""}";

    const string JsonNetJson = @"{""Id"":33,""Name"":""SqMtx"",""Results"":[101.3,99.8,99.6,100.4],""WhenRun"":""2015-01-01T00:00:00-08:00""}";

    byte[] JsonNetBinary = new byte[] { 0x68, 0x00, 0x00, 0x00, 0x10, 0x49, 0x64, 0x00, 0x21, 0x00, 0x00, 0x00, 0x02, 0x4E, 0x61, 0x6D, 0x65, 0x00, 0x06, 0x00, 0x00, 0x00, 0x53, 0x71, 0x4D, 0x74, 0x78, 0x00, 0x04, 0x52, 0x65, 0x73, 0x75, 0x6C, 0x74, 0x73, 0x00, 0x31, 0x00, 0x00, 0x00, 0x01, 0x30, 0x00, 0x33, 0x33, 0x33, 0x33, 0x33, 0x53, 0x59, 0x40, 0x01, 0x31, 0x00, 0x33, 0x33, 0x33, 0x33, 0x33, 0xF3, 0x58, 0x40, 0x01, 0x32, 0x00, 0x66, 0x66, 0x66, 0x66, 0x66, 0xE6, 0x58, 0x40, 0x01, 0x33, 0x00, 0x9A, 0x99, 0x99, 0x99, 0x99, 0x19, 0x59, 0x40, 0x00, 0x09, 0x57, 0x68, 0x65, 0x6E, 0x52, 0x75, 0x6E, 0x00, 0x00, 0x24, 0x82, 0xA4, 0x4A, 0x01, 0x00, 0x00, 0x00 };

    static volatile object VolatileObject;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Escape(object obj) {
        VolatileObject = obj;
    }

    [DataContract]
    public class TestObject
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public double[] Results { get; set; }

        [DataMember]
        public DateTime WhenRun { get; set; }

        public static TestObject Expected()
        {
            TestObject t = new TestObject();
            t.Id = 33;
            t.Name = "SqMtx";
            t.Results = new double[] { 101.3, 99.8, 99.6, 100.4 };
            t.WhenRun = DateTime.Parse("Jan 1, 2015 8:00 GMT");
            return t;
        }
    }

    private bool Deserialize()
    {
        bool result = true;
        DeserializeObject();
        return result;
    }

    private void DeserializeObject()
    {
        DeserializeDataContractBench();
        DeserializeDataContractJsonBench();
        DeserializeJsonNetBinaryBench();
        DeserializeJsonNetBench();
    }

    private void DeserializeDataContractBench() {
        DataContractSerializer ds = new DataContractSerializer(typeof(TestObject));
        MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(DataContractXml));
        DeserializeDataContractBenchInner(ds, ms);
    }

    private void DeserializeDataContractBenchInner(DataContractSerializer ds, MemoryStream ms)
    {
        TestObject t;
        for (int i = 0; i < Iterations; i++)
        {
            t = (TestObject)ds.ReadObject(ms);
            Escape(t.Name);
            ms.Seek(0, SeekOrigin.Begin);
        }
    }

    private void DeserializeDataContractJsonBench()
    {
        DataContractJsonSerializer ds = new DataContractJsonSerializer(typeof(TestObject));
        MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(DataContractJson));
        DeserializeDataContractJsonBenchInner(ds, ms);
    }

    private void DeserializeDataContractJsonBenchInner(DataContractJsonSerializer ds, MemoryStream ms)
    {
        TestObject t;
        for (int i = 0; i < Iterations; i++)
        {
            t = (TestObject) ds.ReadObject(ms);
            Escape(t.Name);
            ms.Seek(0, SeekOrigin.Begin);
        }
    }

    private void DeserializeJsonNetBinaryBench()
    {
        DeserializeJsonNetBinaryBenchInner();
    }

    private void DeserializeJsonNetBinaryBenchInner()
    {
        Newtonsoft.Json.JsonSerializer ds = new Newtonsoft.Json.JsonSerializer();
        Type ty = typeof(TestObject);
        for (int i = 0; i < JsonNetIterations; i++)
        {
            BsonDataReader br = new BsonDataReader(new MemoryStream(JsonNetBinary));
            TestObject t = (TestObject)ds.Deserialize(br, ty);
            Escape(t.Name);
        }
    }

    private void DeserializeJsonNetBench()
    {
        DeserializeJsonNetBenchInner();
    }

    private void DeserializeJsonNetBenchInner()
    {
        Newtonsoft.Json.JsonSerializer ds = new Newtonsoft.Json.JsonSerializer();
        TestObject t;
        Type ty = typeof(TestObject);
        for (int i = 0; i < JsonNetIterations; i++)
        {
            StringReader sr = new StringReader(JsonNetJson);
            t = (TestObject)ds.Deserialize(sr, ty);
            Escape(t.Name);
        }
    }

    [Fact]
    public static int TestEntryPoint() {
        var tests = new JsonBenchmarks();
        bool result = tests.Deserialize();
        return result ? 100 : -1;
    }
}
}
