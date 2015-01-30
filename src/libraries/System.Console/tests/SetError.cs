// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

//
// System.Console BCL test cases
//
public class SetError
{
    [Fact]
    public static void SetError01()
    {
        // Change SetError=null and verify.
        TextWriter tw = Console.Error;
        try
        {
            Console.SetError(null);

            //SetError method should throw an exception when you pass null as the stream
            Assert.True(false, "SetError doesn't thrown even when null passed");
        }
        catch (ArgumentNullException)
        {
            // Expected exception thrown.
            Console.SetError(tw);
        }
        catch (Exception e)
        {
            Console.SetError(tw);
            Assert.True(false, "Unexpected exception occured :: " + e.ToString());
        }
    }

    [Fact]
    public static void SetError02()
    {
        // Change the error stream and verify the results.

        String strTemp = "Test";
        String strText = null;
        StreamWriter sw = null;

        try
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                //Create a stream writer
                sw = new StreamWriter(memStream);

                //Change the default output stream to the one created above
                Console.SetError(sw);

                //Get the error stream and verify.
                TextWriter tw = Console.Error;
                Assert.True(tw != null, "Invalid textwriter object");

                Console.Error.Write(strTemp);     // Write the string to the stream

                sw.Flush();

                memStream.Seek(0, SeekOrigin.Begin);

                //create a stream reader to read the content from the MemoryStream.
                using (StreamReader sr = new StreamReader(memStream))
                {
                    strText = sr.ReadToEnd();
                }

                //Compare the two strings and log the results.
                Assert.True(strText.Equals(strTemp), "Text is not written correctly.");
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
