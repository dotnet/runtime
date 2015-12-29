// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json.Bson;
using Microsoft.Xunit.Performance;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

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

    [Benchmark]
    private void SerializeDataContract() 
    {
        foreach (var iteration in Benchmark.Iterations) {
            using (iteration.StartMeasurement()) {
                SerializeDataContractBench();
            }
        }
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

    [Benchmark]
    private void SerializeDataContractJson()
    {
        foreach (var iteration in Benchmark.Iterations) {
            using (iteration.StartMeasurement()) {
                SerializeDataContractJsonBench();
            }
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

    [Benchmark]
    private void SerializeJsonNetBinary()
    {
        foreach (var iteration in Benchmark.Iterations) {
            using (iteration.StartMeasurement()) {
                SerializeJsonNetBinaryBench();
            }
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
            var w = new BsonWriter(ms);
            s.Serialize(w, o);
            Escape(w);
            w.Flush();
        }
    }

    [Benchmark]
    private void SerializeJsonNet()
    {
        foreach (var iteration in Benchmark.Iterations) {
            using (iteration.StartMeasurement()) {
                SerializeJsonNetBench();
            }
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

    public static int Main() {
        var tests = new JsonBenchmarks();
        bool result = tests.Serialize();
        return result ? 100 : -1;
    }
}
