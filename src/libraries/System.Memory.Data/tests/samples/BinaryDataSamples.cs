// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;
using Xunit;

namespace Azure.Core.Experimental.Tests.samples
{
    public class BinaryDataSamples
    {
        [Fact(Skip = "Only verifying that the sample builds")]
        public void ToFromString()
        {
            #region Snippet:BinaryDataToFromString
            var data = new BinaryData("some data");

            // ToString will decode the bytes using UTF-8
            Console.WriteLine(data.ToString()); // prints "some data"
            #endregion
        }

        [Fact(Skip = "Only verifying that the sample builds")]

        public void ToFromBytes()
        {
            #region Snippet:BinaryDataToFromBytes
            byte[] bytes = Encoding.UTF8.GetBytes("some data");

            // Create BinaryData using a constructor ...
            BinaryData data = new BinaryData(bytes);

            // Or using a static factory method.
            data = BinaryData.FromBytes(bytes);

            // There is an implicit cast defined for ReadOnlyMemory<byte>
            ReadOnlyMemory<byte> rom = data;

            // There is also an implicit cast defined for ReadOnlySpan<byte>
            ReadOnlySpan<byte> ros = data;

            // there is also a ToMemory method that gives access to the ReadOnlyMemory.
            rom = data.ToMemory();

            // and a ToArray method that converts into a byte array.
            byte[] array = data.ToArray();
            #endregion
        }

        [Fact(Skip = "Only verifying that the sample builds")]
        public void ToFromStream()
        {
            #region Snippet:BinaryDataToFromStream
            var bytes = Encoding.UTF8.GetBytes("some data");
            Stream stream = new MemoryStream(bytes);
            var data = BinaryData.FromStream(stream);

            // Calling ToStream will give back a stream that is backed by ReadOnlyMemory, so it is not writable.
            stream = data.ToStream();
            Console.WriteLine(stream.CanWrite); // prints false
            #endregion
        }

        [Fact(Skip = "Only verifying that the sample builds")]

        public void ToFromCustomType()
        {
            #region Snippet:BinaryDataToFromCustomModel
            var model = new CustomModel
            {
                A = "some text",
                B = 5,
                C = true
            };

            var data = BinaryData.FromObjectAsJson(model);
            model = data.ToObjectFromJson<CustomModel>();
            #endregion
        }

        private class CustomModel
        {
            public string A { get; set; }
            public int B { get; set; }
            public bool C { get; set; }
        }
    }
}
