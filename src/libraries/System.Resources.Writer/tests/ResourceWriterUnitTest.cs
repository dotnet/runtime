// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using Xunit;

namespace System.Resources.ResourceWriterTests
{
    public class ResourceWriterTests
    {
        //The Following two collections are tightly coupled
        // If s_dict is changed, please manually update _RefBuffer
        private static Dictionary<string, string> s_dict = new Dictionary<string, string>{
                                                                           { "name1", "value1"},
                                                                           { "name2", "value2"},
                                                                           { "name3", "value3"}
                                                                         };
        private static byte[] _RefBuffer = new byte[] { 206, 202, 239, 190, 1, 0, 0, 0, 145, 0, 0, 0, 108, 83, 121, 115, 116, 101, 109, 46, 82, 101, 115, 111, 117, 114, 99, 101, 115, 46, 82, 101, 115, 111, 117, 114, 99, 101, 82, 101, 97, 100, 101, 114, 44, 32, 109, 115, 99, 111, 114, 108, 105, 98, 44, 32, 86, 101, 114, 115, 105, 111, 110, 61, 52, 46, 48, 46, 48, 46, 48, 44, 32, 67, 117, 108, 116, 117, 114, 101, 61, 110, 101, 117, 116, 114, 97, 108, 44, 32, 80, 117, 98, 108, 105, 99, 75, 101, 121, 84, 111, 107, 101, 110, 61, 98, 55, 55, 97, 53, 99, 53, 54, 49, 57, 51, 52, 101, 48, 56, 57, 35, 83, 121, 115, 116, 101, 109, 46, 82, 101, 115, 111, 117, 114, 99, 101, 115, 46, 82, 117, 110, 116, 105, 109, 101, 82, 101, 115, 111, 117, 114, 99, 101, 83, 101, 116, 2, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 80, 65, 68, 80, 65, 68, 80, 208, 41, 193, 10, 209, 41, 193, 10, 211, 41, 193, 10, 15, 0, 0, 0, 30, 0, 0, 0, 0, 0, 0, 0, 249, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 49, 0, 0, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 50, 0, 8, 0, 0, 0, 10, 110, 0, 97, 0, 109, 0, 101, 0, 51, 0, 16, 0, 0, 0, 1, 6, 118, 97, 108, 117, 101, 49, 1, 6, 118, 97, 108, 117, 101, 50, 1, 6, 118, 97, 108, 117, 101, 51 };

        [Fact]
        public static void ExceptionforResWriter01()
        {
            Assert.Throws<ArgumentNullException>(() =>
                {
                    MemoryStream ms2 = null;
                    var rw = new ResourceWriter(ms2);
                });
        }

        [Fact]
        public static void ExceptionforResWriter02()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
                {
                    byte[] buffer = new byte[_RefBuffer.Length];
                    using (var ms2 = new MemoryStream(buffer, false))
                    {
                        var rw = new ResourceWriter(ms2);
                    }
                });
        }

        [Fact]
        public static void ExceptionforResWriter03()
        {
            byte[] buffer = new byte[_RefBuffer.Length];
            using (var ms2 = new MemoryStream(buffer, true))
            using (var rw1 = new ResourceWriter(ms2))
            {
                Assert.Throws<ArgumentNullException>(() => rw1.AddResource(null, "args"));
            }
        }

        [Fact]
        public static void ExceptionforResWriter04()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
                {
                    byte[] buffer = new byte[_RefBuffer.Length];
                    using (var ms2 = new MemoryStream(buffer, true))
                    {
                        using (var rw1 = new ResourceWriter(ms2))
                        {
                            rw1.AddResource("key1", "args");
                            rw1.AddResource("key1", "args");
                        }
                    }
                });
        }

        [Fact]
        public static void ExceptionforResWriter05()
        {
            Assert.Throws<InvalidOperationException>(() =>
                {
                    byte[] buffer = new byte[_RefBuffer.Length];
                    using (var ms2 = new MemoryStream(buffer, true))
                    {
                        var rw1 = new ResourceWriter(ms2);
                        rw1.AddResource("key2", "args");
                        rw1.Dispose();
                        rw1.AddResource("key2", "args");
                    }
                });
        }

        [Fact]
        public static void TestEmptyResources()
        {
            byte[] buffer = new byte[_RefBuffer.Length];
            using (var ms2 = new MemoryStream(buffer, true))
            using (var rw1 = new ResourceWriter(ms2))
            {
                rw1.Generate();
                // 180 is the length of the resources header.
                Assert.Equal(180, ms2.Position);
            }
        }

        [Fact]
        public static void GenerateResources()
        {
            byte[] buffer;
            using (var ms2 = new MemoryStream())
            {
                using (var rw = new ResourceWriter(ms2))
                {
                    foreach (var e in s_dict)
                    {
                        string name = e.Key;
                        string values = e.Value;

                        rw.AddResource(name, values);
                    }

                    rw.Generate();
                }
                buffer = ms2.ToArray();
            }
            Assert.Equal(_RefBuffer, buffer);
        }
    }

}





