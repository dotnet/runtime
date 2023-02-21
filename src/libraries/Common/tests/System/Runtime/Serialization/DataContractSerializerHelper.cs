// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using Xunit;

namespace System.Runtime.Serialization.Tests
{
    public static class DataContractSerializerHelper
    {
        internal static T SerializeAndDeserialize<T>(T value, string baseline, DataContractSerializerSettings settings = null, Func<DataContractSerializer> serializerFactory = null, bool skipStringCompare = false, bool verifyBinaryRoundTrip = true)
        {
            DataContractSerializer dcs;
            if (serializerFactory != null)
            {
                dcs = serializerFactory();
            }
            else
            {
                dcs = (settings != null) ? new DataContractSerializer(typeof(T), settings) : new DataContractSerializer(typeof(T));
            }

            T deserialized;
            using (MemoryStream ms = new MemoryStream())
            {
                dcs.WriteObject(ms, value);
                ms.Position = 0;

                string actualOutput = new StreamReader(ms).ReadToEnd();

                if (!skipStringCompare)
                {
                    Utils.CompareResult result = Utils.Compare(baseline, actualOutput);
                    Assert.True(result.Equal, string.Format("{1}{0}Test failed for input: {2}{0}Expected: {3}{0}Actual: {4}",
                        Environment.NewLine, result.ErrorMessage, value, baseline, actualOutput));
                }

                ms.Position = 0;
                deserialized = (T)dcs.ReadObject(ms);
            }

            if (verifyBinaryRoundTrip)
            {
                RoundTripBinarySerialization<T>(value, settings, serializerFactory);
            }

            return deserialized;
        }

        internal static T RoundTripBinarySerialization<T>(T value, DataContractSerializerSettings settings = null, Func<DataContractSerializer> serializerFactory = null)
        {
            DataContractSerializer dcs;
            if (serializerFactory != null)
            {
                dcs = serializerFactory();
            }
            else
            {
                dcs = (settings != null) ? new DataContractSerializer(typeof(T), settings) : new DataContractSerializer(typeof(T));
            }

            using (MemoryStream ms = new MemoryStream())
            using (XmlDictionaryWriter binWriter = XmlDictionaryWriter.CreateBinaryWriter(ms))
            {
                dcs.WriteObject(binWriter, value);
                binWriter.Flush();
                ms.Position = 0;
                XmlDictionaryReader binReader = XmlDictionaryReader.CreateBinaryReader(ms, XmlDictionaryReaderQuotas.Max);
                return (T)dcs.ReadObject(binReader);
            }
        }
    }

}
