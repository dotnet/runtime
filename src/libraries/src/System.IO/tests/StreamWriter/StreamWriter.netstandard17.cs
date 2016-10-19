// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public partial class WriteTests
    {
        [Fact]
        public void Synchronized_NewObject()
        {
            using (Stream str = CreateStream())
            {
                StreamWriter writer = new StreamWriter(str);
                TextWriter synced = TextWriter.Synchronized(writer);
                Assert.NotEqual(writer, synced);
                writer.Write("app");
                synced.Write("pad");

                writer.Flush();
                synced.Flush();

                str.Position = 0;
                StreamReader reader = new StreamReader(str);
                Assert.Equal("apppad", reader.ReadLine());
            }
        }
    }
}
