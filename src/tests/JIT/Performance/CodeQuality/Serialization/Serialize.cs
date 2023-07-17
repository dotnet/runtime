// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json.Bson;
using Xunit;

namespace Serialization
{
public class JsonBenchmarks
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 75000;
#endif

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

        public static TestObject New()
        {
            TestObject t = new TestObject();
            t.Id = 33;
            t.Name = "SqMtx";
            t.Results = new double[] { 101.3, 99.8, 99.6, 100.4 };
            t.WhenRun = DateTime.Parse("Jan 1, 2015 8:00 GMT");
            return t;
        }
    }

    private bool Serialize()
    {
        bool result = true;
        SerializeObject();
        return result;
    }

    private void SerializeObject()
    {
        SerializeDataContractBench();
        SerializeDataContractJsonBench();
        SerializeJsonNetBinaryBench();
        SerializeJsonNetBench();
    }

    private void SerializeDataContractBench() {
        TestObject t = TestObject.New();
        MemoryStream ms = new MemoryStream();
        SerializeDataContractBenchInner(t, ms);
    }

    private void SerializeDataContractBenchInner(object o, MemoryStream ms)
    {
        for (int i = 0; i < Iterations; i++)
        {
            var s = new DataContractSerializer(o.GetType());
            s.WriteObject(ms, o);
            Escape(ms);
            ms.Flush();
        }
    }

    private void SerializeDataContractJsonBench()
    {
        TestObject t = TestObject.New();
        MemoryStream ms = new MemoryStream();
        SerializeDataContractJsonBenchInner(t, ms);
    }

    private void SerializeDataContractJsonBenchInner(object o, MemoryStream ms)
    {
        for (int i = 0; i < Iterations; i++)
        {
            var s = new DataContractJsonSerializer(o.GetType());
            s.WriteObject(ms, o);
            Escape(ms);
            ms.Flush();
        }
    }

    private void SerializeJsonNetBinaryBench()
    {
        TestObject t = TestObject.New();
        MemoryStream ms = new MemoryStream();
        SerializeJsonNetBinaryBenchInner(t, ms);
    }

    private void SerializeJsonNetBinaryBenchInner(object o, MemoryStream ms)
    {
        for (int i = 0; i < Iterations; i++)
        {
            var s = new Newtonsoft.Json.JsonSerializer();
            var w = new BsonDataWriter(ms);
            s.Serialize(w, o);
            Escape(w);
            w.Flush();
        }
    }

    private void SerializeJsonNetBench()
    {
        TestObject t = TestObject.New();
        SerializeJsonNetBenchInner(t);
    }

    private void SerializeJsonNetBenchInner(object o)
    {
        for (int i = 0; i < Iterations; i++)
        {
            var s = Newtonsoft.Json.JsonConvert.SerializeObject(o);
            Escape(s);
        }
    }

    [Fact]
    public static int TestEntryPoint() {
        var tests = new JsonBenchmarks();
        bool result = tests.Serialize();
        return result ? 100 : -1;
    }
}
}
