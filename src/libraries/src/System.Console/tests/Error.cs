// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

//
// System.Console BCL test cases
//
public class Error
{
    [Fact]
    public static void Error01()
    {
        //Verify the stream returned by the error property works
        String strTemp = "Test";
        String strText = null;
        StreamWriter sw = null;
        try
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                //Create a stream writer and set error stream to use the created stream.
                sw = new StreamWriter(memStream);
                Console.SetError(sw);

                //Get the error stream and verify the results.
                TextWriter tw = Console.Error;
                Assert.True(tw != null, "Invalid textwriter object.");

                tw.Write(strTemp);     // Write the string to the stream

                sw.Flush();

                memStream.Seek(0, SeekOrigin.Begin);

                using (StreamReader sr = new StreamReader(memStream))
                {
                    strText = sr.ReadToEnd();
                }

                Assert.True(strTemp.Equals(strText), "Text is not written correctly");
            }
        }
        catch (Exception e)
        {
            Assert.True(false, "Unexpected exception occured :: " + e.ToString());
        }
        finally
        {
            if (sw != null)
            {
                sw.Dispose();
            }
        }
    }
}
