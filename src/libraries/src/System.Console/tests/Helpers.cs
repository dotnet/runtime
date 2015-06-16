// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

class Helpers
{
    public static void SetAndReadHelper(Action<TextWriter> setHelper, Func<TextWriter> getHelper, Func<StreamReader, string> readHelper)
    {
        const string TestString = "Test";

        TextWriter oldWriterToRestore = getHelper();
        Assert.NotNull(oldWriterToRestore);

        try
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(memStream))
                {
                    setHelper(sw);

                    TextWriter newStream = getHelper();

                    Assert.NotNull(newStream);

                    newStream.Write(TestString);
                    newStream.Flush();

                    memStream.Seek(0, SeekOrigin.Begin);

                    using (StreamReader sr = new StreamReader(memStream))
                    {
                        string fromConsole = readHelper(sr);

                        Assert.Equal(TestString, fromConsole);
                    }
                }
            }
        }
        finally
        {
            setHelper(oldWriterToRestore);
        }
    }
}
