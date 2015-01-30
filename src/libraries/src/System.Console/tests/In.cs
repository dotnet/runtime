// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

//
// System.Console BCL test cases
//
public class Console_In
{
    [Fact]
    public static void In01()
    {
        // Verify In property works as expected.

        StreamWriter sw = null;
        try
        {
            String[] strTemp = new String[20];

            // First, save the standard Input.
            TextReader twTemp = Console.In;

            using (MemoryStream memStream = new MemoryStream())
            {
                sw = new StreamWriter(memStream);

                for (int iLoop = 0; iLoop < 20; iLoop++)
                {
                    strTemp[iLoop] = String.Format("Test {0}", iLoop);
                    sw.WriteLine(strTemp[iLoop]);     // Write the string to the stream
                }

                sw.Flush();
                memStream.Seek(0, SeekOrigin.Begin);

                using (StreamReader sr = new StreamReader(memStream))
                {
                    Console.SetIn(sr);

                    //Get the Input stream and verify.
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
