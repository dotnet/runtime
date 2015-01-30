// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

//
// System.Console BCL test cases
//
public class Out
{
    [Fact]
    public static void Out01()
    {
        // Verify Out property works as expected.

        String strTemp = "Test";
        String strText = null;
        StreamWriter sw = null;

        try
        {
            // First, save the standard output.
            TextWriter twTemp = Console.Out;

            using (MemoryStream memStream = new MemoryStream())
            {
                //Create a stream writer 
                sw = new StreamWriter(memStream);

                //Change the default output stream
                Console.SetOut(sw);

                //Get the output stream and verify.
                TextWriter tw = Console.Out;
                Assert.True(tw != null, "Invalid textwriter object ");

                tw.Write(strTemp);     // Write the string to the stream
                sw.Flush();

                memStream.Seek(0, SeekOrigin.Begin);

                Console.SetOut(twTemp); // Set output stream back to the default one.

                //Create a stream reader to read the content from the MemoryStream
                using (StreamReader sr = new StreamReader(memStream))
                {
                    strText = sr.ReadToEnd();
                }
            }

            //Compare the two strings and log the results.
            Assert.True(strTemp.Equals(strText), "Text is not written correctly");
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
