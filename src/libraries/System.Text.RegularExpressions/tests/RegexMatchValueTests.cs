// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class RegexMatchValueTests
{
    [Fact]
    public static void MatchValue()
    {
        //////////// Global Variables used for all tests
        int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "Loc_000oo";
        Regex rgx1;
        Match mtch1;
        String strInput;
        String strExpected;
        GroupCollection grpc1;
        CaptureCollection capc1;
        String[] arrGroupExp =
        {
        "aaabbcccccccccc", "aaa", "bb", "cccccccccc"
        }

        ;
        String[] arrGroupExp1 =
        {
        "abracadabra", "abra", "cad"
        }

        ;
        String[] arrCaptureExp =
        {
        "aaabbcccccccccc", "aaa", "bb", "cccccccccc"
        }

        ;
        String[] arrCaptureExp1 =
        {
        "abracad", "abra"
        }

        ;
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            //[] We are testing the Value property of Match, Group and Capture here. Value is the more semantic equivalent of
            //the current ToString
            //trim leading and trailing white spaces
            //my answer to the csharp alias, Regex.Replace(strInput, @"\s*(.*?)\s*$", "${1}") works fine, Even Freidl gives it
            //a solution, albeit not a very fast one
            iCountTestcases++;
            rgx1 = new Regex(@"\s*(.*?)\s*$");
            strInput = " Hello World ";
            mtch1 = rgx1.Match(strInput);
            if (mtch1.Success)
            {
                strExpected = strInput;
                if (!strExpected.Equals(mtch1.Value))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_759ed! The expected value was not returned, Expected - {0} Returned - {1}", strExpected, mtch1.Value);
                }
            }

            //[]for Groups and Captures
            iCountTestcases++;
            rgx1 = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            strInput = "aaabbccccccccccaaaabc";
            mtch1 = rgx1.Match(strInput);
            if (mtch1.Success)
            {
                strExpected = "aaabbcccccccccc";
                if (!strExpected.Equals(mtch1.Value))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_745fg! The expected value was not returned, Expected - {0} Returned - {1}", strExpected, mtch1.Value);
                }

                grpc1 = mtch1.Groups;
                if (grpc1.Count != 4)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_67fdaq! The expected value was not returned, Expected - 4 Returned - {0}", grpc1.Count);
                }

                for (int i = 0; i < grpc1.Count; i++)
                {
                    if (!arrGroupExp[i].Equals(grpc1[i].Value))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_00213534_{2}! The expected value was not returned, Expected - {0} Returned - {1}", arrGroupExp[i], grpc1[i].Value, i);
                    }

                    //Group has a Captures property too
                    capc1 = grpc1[i].Captures;
                    if (capc1.Count != 1)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_0834esfd! The expected value was not returned, Expected - 1 Returned - {0}", capc1.Count);
                    }

                    if (!arrGroupExp[i].Equals(capc1[0].Value))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_974535! The expected value was not returned, Expected - {0} Returned - {1}", arrGroupExp[i], capc1[0].Value);
                    }
                }

                capc1 = mtch1.Captures;
                if (capc1.Count != 1)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_863trfg! The expected value was not returned, Expected - 1 Returned - {0}", capc1.Count);
                }

                strExpected = "aaabbcccccccccc";
                if (!strExpected.Equals(capc1[0].Value))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_97463dfsg! The expected value was not returned, Expected - {0} Returned - {1}", strExpected, capc1[0].Value);
                }
            }

            //Another example - given by Brad Merril in an article on RegularExpressions
            iCountTestcases++;
            rgx1 = new Regex(@"(abra(cad)?)+");
            strInput = "abracadabra1abracadabra2abracadabra3";
            mtch1 = rgx1.Match(strInput);
            while (mtch1.Success)
            {
                strExpected = "abracadabra";
                if (!strExpected.Equals(mtch1.Value))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_7342wsdg! The expected value was not returned, Expected - {0} Returned - {1}", strExpected, mtch1.Value);
                }

                grpc1 = mtch1.Groups;
                if (grpc1.Count != 3)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_9745fgg! The expected value was not returned, Expected - 4 Returned - {0}", grpc1.Count);
                }

                for (int i = 0; i < grpc1.Count; i++)
                {
                    if (!arrGroupExp1[i].Equals(grpc1[i].Value))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_8756fdg! The expected value was not returned, Expected - {0} Returned - {1}", arrGroupExp1[i], grpc1[i].Value, i);
                    }

                    //Group has a Captures property too
                    capc1 = grpc1[i].Captures;
                    switch (i)
                    {
                        case 1:
                            if (capc1.Count != 2)
                            {
                                iCountErrors++;
                                Console.WriteLine("Err_9745fgs! The expected value was not returned, Expected - 1 Returned - {0}", capc1.Count);
                            }

                            for (int j = 0; j < capc1.Count; j++)
                            {
                                if (!arrCaptureExp1[j].Equals(capc1[j].Value))
                                {
                                    iCountErrors++;
                                    Console.WriteLine("Err_97453sdf! The expected value was not returned, Expected - {0} Returned - {1}", arrGroupExp[j], capc1[j].Value);
                                }
                            }

                            break;
                        case 2:
                            if (capc1.Count != 1)
                            {
                                iCountErrors++;
                                Console.WriteLine("Err_8473rsg! The expected value was not returned, Expected - 1 Returned - {0}", capc1.Count);
                            }

                            strExpected = "cad";
                            if (!strExpected.Equals(capc1[0].Value))
                            {
                                iCountErrors++;
                                Console.WriteLine("Err_9743rfsfg! The expected value was not returned, Expected - {0} Returned - {1}", strExpected, capc1[0].Value);
                            }

                            break;
                    }
                }

                capc1 = mtch1.Captures;
                if (capc1.Count != 1)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_7453fgds! The expected value was not returned, Expected - 1 Returned - {0}", capc1.Count);
                }

                strExpected = "abracadabra";
                if (!strExpected.Equals(capc1[0].Value))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_0753rdg! The expected value was not returned, Expected - {0} Returned - {1}", strExpected, capc1[0].Value);
                }

                mtch1 = mtch1.NextMatch();
            }
            ///////////////////////////////////////////////////////////////////
            /////////////////////////// END TESTS /////////////////////////////
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine("Error Err_8888yyy!  strLoc==" + strLoc + ", exc_general==\n" + exc_general.ToString());
        }

        ////  Finish Diagnostics
        Assert.Equal(0, iCountErrors);
    }
}