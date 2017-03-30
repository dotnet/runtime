// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using SerializationTypes;
using System.IO;
using System.Runtime.Serialization;
using Xunit;

public static partial class DataContractSerializerTests
{
	[Fact]
    public static void DCS_MyPersonSurrogate()
    {
        DataContractSerializer dcs = new DataContractSerializer(typeof(Family));
        dcs.SetSerializationSurrogateProvider(new MyPersonSurrogateProvider());
        MemoryStream ms = new MemoryStream();
        Family myFamily = new Family
        {
            Members = new NonSerializablePerson[]
            {
                new NonSerializablePerson("John", 34),
                new NonSerializablePerson("Jane", 32),
                new NonSerializablePerson("Bob", 5),
            }
        };
        dcs.WriteObject(ms, myFamily);
        ms.Position = 0;
        var newFamily = (Family)dcs.ReadObject(ms);
        Assert.StrictEqual(myFamily.Members.Length, newFamily.Members.Length);
        for (int i = 0; i < myFamily.Members.Length; ++i)
        {
            Assert.StrictEqual(myFamily.Members[i].Name, newFamily.Members[i].Name);
        }
    }

    [Fact]
    public static void DCS_FileStreamSurrogate()
    {
        const string TestFileName = "Test.txt";
        const string TestFileData = "Some data for data contract surrogate test";

        // Create the serializer and specify the surrogate
        var dcs = new DataContractSerializer(typeof(MyFileStream));
        dcs.SetSerializationSurrogateProvider(MyFileStreamSurrogateProvider.Singleton);

        // Create and initialize the stream
        byte[] serializedStream;

        // Serialize the stream
        using (MyFileStream stream1 = new MyFileStream(TestFileName))
        {
            stream1.WriteLine(TestFileData);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                dcs.WriteObject(memoryStream, stream1);
                serializedStream = memoryStream.ToArray();
            }
        }

        // Deserialize the stream
        using (MemoryStream stream = new MemoryStream(serializedStream))
        {
            using (MyFileStream stream2 = (MyFileStream)dcs.ReadObject(stream))
            {
                string fileData = stream2.ReadLine();
                Assert.StrictEqual(TestFileData, fileData);
            }
        }
    }
}
