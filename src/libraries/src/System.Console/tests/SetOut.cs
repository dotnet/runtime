// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

//
// System.Console BCL test cases
//
public class SetOut
{
    [Fact]
    public static void SetOut01()
    {
        // Change SetOut=null and verify.
        TextWriter tw = Console.Out;
        try
        {
            Console.SetOut(null);

            //SetOut method should throw an exception when you pass null as the stream
            Assert.True(false, "SetOut doesn't thrown even when null passed");
        }
        catch (ArgumentNullException)
        {
            //Expected exception thrown.
            Console.SetOut(tw);
        }
        catch (Exception e)
        {
            Console.SetOut(tw);
            Assert.True(false, "Unexpected exception occured :: " + e.ToString());
        }
    }

    [Fact]
    public static void SetOut02()
    {
        //Change SetOut to a valid stream and verify the results.

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
                Assert.True(tw != null, "Invalid textwriter object.");

                Console.Write(strTemp);     // Write the string to the stream
                sw.Flush();

                memStream.Seek(0, SeekOrigin.Begin);

                Console.SetOut(twTemp); // Set output stream back to the default one.

                //Create a stream reader to read the content in the MemoryStream
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
