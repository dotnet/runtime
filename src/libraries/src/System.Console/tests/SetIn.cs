// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

//
// System.Console BCL test cases
//
public class SetIn
{
    [Fact]
    public static void SetIn01()
    {
        // Change SetIn=null and verify.
        TextReader tr = Console.In;
        try
        {
            Console.SetIn(null);

            //SetIn method should throw an exception when you pass null as the stream
            Assert.True(false, "SetIn doesn't thrown even when null passed");
        }
        catch (ArgumentNullException)
        {
            //Expected exception thrown.
            Console.SetIn(tr);
        }
        catch (Exception e)
        {
            Console.SetIn(tr);
            Assert.True(false, "Unexpected exception occured :: " + e.ToString());
        }
    }

    [Fact]
    public static void SetIn02()
    {
        // Verify SetIn method works with a valid input stream.
        StreamWriter sw = null;
        try
        {
            String[] strTemp = new String[20];

            // First, save the standard Input.
            TextReader twTemp = Console.In;

            using (MemoryStream memStream = new MemoryStream())
            {
                //Create a stream writer and write text to MemoryStream
                sw = new StreamWriter(memStream);

                for (int iLoop = 0; iLoop < 20; iLoop++)
                {
                    strTemp[iLoop] = String.Format("Test {0}", iLoop);
                    sw.WriteLine(strTemp[iLoop]);     // Write the string to the stream
                }

                sw.Flush();
                memStream.Seek(0, SeekOrigin.Begin);

                //create stream reader to read the text from the MemoryStream
                using (StreamReader sr = new StreamReader(memStream))
                {
                    Console.SetIn(sr);

                    //Verify that input stream works as expected.
                    TextReader tr = Console.In;
                    Assert.True(tr != null, "Invalid textReader object.");

                    for (int iLoop = 0; iLoop < 20; iLoop++)
                    {
                        Assert.True(tr.ReadLine().Equals(strTemp[iLoop]), "retrieved string from Input is not right");
                    }
                }
            }

            Console.SetIn(twTemp); // Set Input stream back to the default one.
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
