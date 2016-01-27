// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace DefaultNamespace
{

    using System;

    internal class cb6054ToByte_all
    {
#pragma warning disable 0414
        public static readonly String s_strActiveBugNums = "None.";
#pragma warning restore 0414
        public static readonly String s_strDtTmVer = "1999/07/15 16:20";  // t-seansp
        public static readonly String s_strClassMethod = "Convert.ToByte( all )";
        public static readonly String s_strTFName = "Cb6054ToByte_all";
        public static readonly String s_strTFAbbrev = "Cb6054";
        public static readonly String s_strTFPath = "Jit\\Regression\\m10\\b02352";
        public bool verbose = false;

        public static String[] s_strMethodsCovered =
        {
               "Method_Covered:  Convert.ToByte( Boolean )"           //bool
              ,"Method_Covered:  Convert.ToByte( Byte )"              //byte
              ,"Method_Covered:  Convert.ToByte( Double )"            //double
              ,"Method_Covered:  Convert.ToByte( Single )"            //single
              ,"Method_Covered:  Convert.ToByte( Int32 )"             //int
              ,"Method_Covered:  Convert.ToByte( Int64 )"             //long
              ,"Method_Covered:  Convert.ToByte( Int16 )"             //short
              ,"Method_Covered:  Convert.ToByte( Currency )"          //System/Currency
              ,"Method_Covered:  Convert.ToByte( Decimal )"           //System/Decimal
              ,"Method_Covered:  Convert.ToByte( String )"            //System/String
              ,"Method_Covered:  Convert.ToByte( Object )"           //System/Object
              ,"Method_Covered:  Convert.ToByte( String, Int32 )"     //System/String, int
          };
        public static void printoutCoveredMethods()
        {
            Console.Error.WriteLine("");
            Console.Error.WriteLine("Method_Count==12 (" + s_strMethodsCovered.Length + "==confirm) !!");
            Console.Error.WriteLine("");

            for (int ia = 0; ia < s_strMethodsCovered.Length; ia++)
            {
                Console.Error.WriteLine(s_strMethodsCovered[ia]);
            }

            Console.Error.WriteLine("");
        }
        public virtual Boolean runTest()
        {
            Console.Error.WriteLine(s_strTFPath + " " + s_strTFName + " ,for " + s_strClassMethod + "  ,Source ver " + s_strDtTmVer);
            String strLoc = "Loc_000oo";
            int inCountTestcases = 0;
            int inCountErrors = 0;


            if (verbose) Console.WriteLine("Method: Byte Convert.ToByte( Int32 )");
            try
            {
                Int32[] int3Array = {
                          10,
                          0,
                          100,
                          255,
                      };
                Byte[] int3Results = {
                          10,
                          0,
                          100,
                          255,
                      };
                for (int i = 0; i < int3Array.Length; i++)
                {
                    inCountTestcases++;
                    if (verbose) Console.Write("Testing : " + int3Array[i] + "...");
                    try
                    {
                        Byte result = Convert.ToByte(int3Array[i]);
                        if (verbose) Console.WriteLine(result + "==" + int3Results[i]);
                        if (!result.Equals(int3Results[i]))
                        {
                            inCountErrors++;
                            strLoc = "Err_rint3Ar," + i;
                            Console.Error.WriteLine(strLoc + " Expected = '" + int3Results[i] +
                                                     "' ... Received = '" + result + "'.");
                        }
                    }
                    catch (Exception e)
                    {
                        inCountErrors++;
                        strLoc = "Err_xint3Ar," + i;
                        Console.Error.WriteLine(strLoc + " Exception Thrown: " + e.GetType().FullName);
                    }
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : Lower Bound");
                try
                {
                    Byte result = Convert.ToByte(((IConvertible)(-100)).ToInt32(null));
                    inCountErrors++;
                    strLoc = "Err_xint3A1";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (OverflowException e)
                {
                    if (!e.GetType().FullName.Equals("System.OverflowException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xint3B1";
                        Console.Error.WriteLine(strLoc + "More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xint3C1";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : Upper Bound");
                try
                {
                    Byte result = Convert.ToByte(((IConvertible)1000).ToInt32(null));
                    inCountErrors++;
                    strLoc = "Err_xint3A2";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (OverflowException e)
                {
                    if (!e.GetType().FullName.Equals("System.OverflowException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xint3B2";
                        Console.Error.WriteLine(strLoc + "More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xint3C2";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Uncaught Exception in Byte Convert.ToByte( Int32 )");
                Console.WriteLine("Exception->" + e.GetType().FullName);
            }

            if (verbose) Console.WriteLine("Method: Byte Convert.ToByte( Int64 )");
            try
            {
                Int64[] int6Array = {
                          10L,
                          100L,
                      };
                Byte[] int6Results = {
                          (( (IConvertible)10 )).ToByte(null),
                          (( (IConvertible)100 )).ToByte(null),
                      };
                for (int i = 0; i < int6Array.Length; i++)
                {
                    inCountTestcases++;
                    if (verbose) Console.Write("Testing : " + int6Array[i] + "...");
                    try
                    {
                        Byte result = Convert.ToByte(int6Array[i]);
                        if (verbose) Console.WriteLine(result + "==" + int6Results[i]);
                        if (!result.Equals(int6Results[i]))
                        {
                            inCountErrors++;
                            strLoc = "Err_rint6Ar," + i;
                            Console.Error.WriteLine(strLoc + " Expected = '" + int6Results[i] +
                                                     "' ... Received = '" + result + "'.");
                        }
                    }
                    catch (Exception e)
                    {
                        inCountErrors++;
                        strLoc = "Err_xint6Ar," + i;
                        Console.Error.WriteLine(strLoc + " Exception Thrown: " + e.GetType().FullName);
                    }
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : Lower Bound");
                try
                {
                    Byte result = Convert.ToByte(((IConvertible)(-100)).ToInt64(null));
                    inCountErrors++;
                    strLoc = "Err_xInt6A1";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (OverflowException e)
                {
                    if (!e.GetType().FullName.Equals("System.OverflowException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xInt6B1";
                        Console.Error.WriteLine(strLoc + "More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xInt6C1";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : Upper Bound");
                try
                {
                    Byte result = Convert.ToByte(((IConvertible)1000).ToInt64(null));
                    inCountErrors++;
                    strLoc = "Err_xInt6A2";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (OverflowException e)
                {
                    if (!e.GetType().FullName.Equals("System.OverflowException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xInt6B2";
                        Console.Error.WriteLine(strLoc + "More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xInt6C2";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Uncaught Exception in Byte Convert.ToByte( Int64 )");
                Console.WriteLine("Exception->" + e.GetType().FullName);
            }

            if (verbose) Console.WriteLine("Method: Byte Convert.ToByte( Int16 )");
            try
            {
                Int16[] int1Array = {
                          0,
                          255,
                          100,
                          2,
                      };
                Byte[] int1Results = {
                          0,
                          255,
                          100,
                          2,
                      };
                for (int i = 0; i < int1Array.Length; i++)
                {
                    inCountTestcases++;
                    if (verbose) Console.Write("Testing : " + int1Array[i] + "...");
                    try
                    {
                        Byte result = Convert.ToByte(int1Array[i]);
                        if (verbose) Console.WriteLine(result + "==" + int1Results[i]);
                        if (!result.Equals(int1Results[i]))
                        {
                            inCountErrors++;
                            strLoc = "Err_rint1Ar," + i;
                            Console.Error.WriteLine(strLoc + " Expected = '" + int1Results[i] +
                                                     "' ... Received = '" + result + "'.");
                        }
                    }
                    catch (Exception e)
                    {
                        inCountErrors++;
                        strLoc = "Err_xint1Ar," + i;
                        Console.Error.WriteLine(strLoc + " Exception Thrown: " + e.GetType().FullName);
                    }
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : Lower Bound");
                try
                {
                    Byte result = Convert.ToByte(((IConvertible)(-100)).ToInt16(null));
                    inCountErrors++;
                    strLoc = "Err_xint1A1";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (OverflowException e)
                {
                    if (!e.GetType().FullName.Equals("System.OverflowException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xint1B1";
                        Console.Error.WriteLine(strLoc + "More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xint1C1";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : Upper Bound");
                try
                {
                    Byte result = Convert.ToByte(((IConvertible)1000).ToInt16(null));
                    inCountErrors++;
                    strLoc = "Err_xint1A2";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (OverflowException e)
                {
                    if (!e.GetType().FullName.Equals("System.OverflowException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xint1B2";
                        Console.Error.WriteLine(strLoc + "More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xint1C2";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Uncaught Exception in Byte Convert.ToByte( Int16 )");
                Console.WriteLine("Exception->" + e.GetType().FullName);
            }


            if (verbose) Console.WriteLine("Method: Byte Convert.ToByte( Decimal )");
            try
            {
                Decimal[] deciArray = {
                          ( (IConvertible)Byte.MaxValue ).ToDecimal(null),
                          ( (IConvertible)Byte.MinValue ).ToDecimal(null),
                          new Decimal( 254.01 ),
                          ( (IConvertible) ( Byte.MaxValue/2 ) ).ToDecimal(null),
                      };
                Byte[] deciResults = {
                          Byte.MaxValue,
                          Byte.MinValue,
                          254,
                          Byte.MaxValue/2,
                      };
                for (int i = 0; i < deciArray.Length; i++)
                {
                    inCountTestcases++;
                    if (verbose) Console.Write("Testing : " + deciArray[i] + "...");
                    try
                    {
                        Byte result = Convert.ToByte(deciArray[i]);
                        if (verbose) Console.WriteLine(result + "==" + deciResults[i]);
                        if (!result.Equals(deciResults[i]))
                        {
                            inCountErrors++;
                            strLoc = "Err_rdeciAr," + i;
                            Console.Error.WriteLine(strLoc + " Expected = '" + deciResults[i] +
                                                     "' ... Received = '" + result + "'.");
                        }
                    }
                    catch (Exception e)
                    {
                        inCountErrors++;
                        strLoc = "Err_xdeciAr," + i;
                        Console.Error.WriteLine(strLoc + " Exception Thrown: " + e.GetType().FullName);
                    }
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : Lower Bound");
                try
                {
                    Byte result = Convert.ToByte(((IConvertible)(-100)).ToDecimal(null));
                    inCountErrors++;
                    strLoc = "Err_xdeciA1";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (OverflowException e)
                {
                    if (!e.GetType().FullName.Equals("System.OverflowException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xdeciB1";
                        Console.Error.WriteLine(strLoc + "More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xdeciC1";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : Upper Bound");
                try
                {
                    Byte result = Convert.ToByte(((IConvertible)1000).ToDecimal(null));
                    inCountErrors++;
                    strLoc = "Err_xdeciA2";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (OverflowException e)
                {
                    if (!e.GetType().FullName.Equals("System.OverflowException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xdeciB2";
                        Console.Error.WriteLine(strLoc + "More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xdeciC2";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Uncaught Exception in Byte Convert.ToByte( Decimal )");
                Console.WriteLine("Exception->" + e.GetType().FullName);
            }

            if (verbose) Console.WriteLine("Method: Byte Convert.ToByte( String )");
            try
            {
                String[] striArray = {
                          ( Byte.MaxValue ).ToString(),
                          ( Byte.MinValue ).ToString(),
                      };
                Byte[] striResults = {
                          Byte.MaxValue,
                          Byte.MinValue,
                      };
                for (int i = 0; i < striArray.Length; i++)
                {
                    inCountTestcases++;
                    if (verbose) Console.Write("Testing : " + striArray[i] + "...");
                    try
                    {
                        Byte result = Convert.ToByte(striArray[i]);
                        if (verbose) Console.WriteLine(result + "==" + striResults[i]);
                        if (!result.Equals(striResults[i]))
                        {
                            inCountErrors++;
                            strLoc = "Err_rstriAr," + i;
                            Console.Error.WriteLine(strLoc + " Expected = '" + striResults[i] +
                                                     "' ... Received = '" + result + "'.");
                        }
                    }
                    catch (Exception e)
                    {
                        inCountErrors++;
                        strLoc = "Err_xstriAr," + i;
                        Console.Error.WriteLine(strLoc + " Exception Thrown: " + e.GetType().FullName);
                    }
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : Argument null");
                try
                {
                    String[] dummy = { null, };

                    throw new System.ArgumentNullException();
                }
                catch (ArgumentNullException e)
                {
                    if (!e.GetType().FullName.Equals("System.ArgumentNullException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xstriB1";
                        Console.Error.WriteLine(strLoc + " More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xstriC1";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : UpperBound");
                try
                {
                    Byte result = Convert.ToByte("256");
                    inCountErrors++;
                    strLoc = "Err_xstriA2";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (OverflowException e)
                {
                    if (!e.GetType().FullName.Equals("System.OverflowException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xstriB2";
                        Console.Error.WriteLine(strLoc + " More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xstriC2";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : LowerBound");
                try
                {
                    Byte result = Convert.ToByte("-1");
                    inCountErrors++;
                    strLoc = "Err_xstriA4";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (OverflowException e)
                {
                    if (!e.GetType().FullName.Equals("System.OverflowException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xstriB4";
                        Console.Error.WriteLine(strLoc + " More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xstriC4";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : Format");
                try
                {
                    Byte result = Convert.ToByte("!56");
                    inCountErrors++;
                    strLoc = "Err_xstriA3";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (FormatException e)
                {
                    if (!e.GetType().FullName.Equals("System.FormatException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xstriB3";
                        Console.Error.WriteLine(strLoc + " More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xstriC3";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Uncaught Exception in Byte Convert.ToByte( String )");
                Console.WriteLine("Exception->" + e.GetType().FullName);
            }

            if (verbose) Console.WriteLine("Method: Byte Convert.ToByte( String, Int32 )");
            try
            {
                String[] striArray = {
                          "10",
                          "100",
                          "1011",
                          "ff",
                          "0xff",
                          "77",
                          "11",
                          "11111111",
};
                Int32[] striBase = {
                          10,
                          10,
                          2,
                          16,
                          16,
                          8,
                          2,
                          2,
                      };

                Byte[] striResults = {
                          10,
                          100,
                          11,
                          255,
                          255,
                          63,
                          3,
                          255,
};
                for (int i = 0; i < striArray.Length; i++)
                {
                    inCountTestcases++;
                    if (verbose) Console.Write("Testing : " + striArray[i] + "," + striBase[i] + "...");
                    try
                    {
                        Byte result = Convert.ToByte(striArray[i], striBase[i]);
                        if (verbose) Console.WriteLine(result + "==" + striResults[i]);
                        if (!result.Equals(striResults[i]))
                        {
                            inCountErrors++;
                            strLoc = "Err_rstri2Ar," + i;
                            Console.Error.WriteLine(strLoc + " Expected = '" + striResults[i] +
                                                     "' ... Received = '" + result + "'.");
                        }
                    }
                    catch (Exception e)
                    {
                        inCountErrors++;
                        strLoc = "Err_xstri2Ar," + i;
                        Console.Error.WriteLine(strLoc + " Exception Thrown: " + e.GetType().FullName);
                    }
                }

                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : Argument (bad base)");
                try
                {
                    String[] dummy = { null, };

                    Byte result = Convert.ToByte(dummy[0], 11);
                    inCountErrors++;
                    strLoc = "Err_xstri2A5";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (ArgumentException e)
                {
                    if (!e.GetType().FullName.Equals("System.ArgumentException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xstri2B5";
                        Console.Error.WriteLine(strLoc + " More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xstri2C5";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : Argument null");
                try
                {
                    String[] dummy = { null, };

                    Byte result = Convert.ToByte(dummy[0], 10);
                    if (result != 0)
                    {
                        inCountErrors++;
                        strLoc = "Err_xstri2A1";
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xstri2C1";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : UpperBound");
                try
                {
                    Byte result = Convert.ToByte("256", 10);
                    inCountErrors++;
                    strLoc = "Err_xstri2A2";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (OverflowException e)
                {
                    if (!e.GetType().FullName.Equals("System.OverflowException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xstri2B2";
                        Console.Error.WriteLine(strLoc + " More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xstri2C2";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : UpperBound (binary)");
                try
                {
                    Byte result = Convert.ToByte("111111111", 2);
                    inCountErrors++;
                    strLoc = "Err_xstri2A5";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (OverflowException e)
                {
                    if (!e.GetType().FullName.Equals("System.OverflowException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xstri2B5";
                        Console.Error.WriteLine(strLoc + " More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xstri2C5";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : UpperBound (hex)");
                try
                {
                    Byte result = Convert.ToByte("ffffe", 16);
                    inCountErrors++;
                    strLoc = "Err_xstri2A6";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (OverflowException e)
                {
                    if (!e.GetType().FullName.Equals("System.OverflowException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xstri2B6";
                        Console.Error.WriteLine(strLoc + " More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xstri2C6";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : UpperBound (octal)");
                try
                {
                    Byte result = Convert.ToByte("7777777", 8);
                    inCountErrors++;
                    strLoc = "Err_xstri2A7";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (OverflowException e)
                {
                    if (!e.GetType().FullName.Equals("System.OverflowException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xstri2B7";
                        Console.Error.WriteLine(strLoc + " More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xstri2C7";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : Format (hex)");
                try
                {
                    Byte result = Convert.ToByte("fffg", 16);
                    inCountErrors++;
                    strLoc = "Err_xstri2A8";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (FormatException e)
                {
                    if (!e.GetType().FullName.Equals("System.FormatException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xstri2B8";
                        Console.Error.WriteLine(strLoc + " More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xstri2C8";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : Format (hex)");
                try
                {
                    Byte result = Convert.ToByte("0xxfff", 16);
                    inCountErrors++;
                    strLoc = "Err_xstri2A8";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (FormatException e)
                {
                    if (!e.GetType().FullName.Equals("System.FormatException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xstri2B8";
                        Console.Error.WriteLine(strLoc + " More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xstri2C8";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : Format (octal)");
                try
                {
                    Byte result = Convert.ToByte("8", 8);
                    inCountErrors++;
                    strLoc = "Err_xstri2A0";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (FormatException e)
                {
                    if (!e.GetType().FullName.Equals("System.FormatException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xstri2B0";
                        Console.Error.WriteLine(strLoc + " More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xstri2C0";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : Format (bin)");
                try
                {
                    Byte result = Convert.ToByte("112", 2);
                    inCountErrors++;
                    strLoc = "Err_xstri2A9";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (FormatException e)
                {
                    if (!e.GetType().FullName.Equals("System.FormatException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xstri2B9";
                        Console.Error.WriteLine(strLoc + " More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xstri2C9";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : LowerBound");
                try
                {
                    Byte result = Convert.ToByte("-1", 10);
                    inCountErrors++;
                    strLoc = "Err_xstri2A4";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (OverflowException e)
                {
                    if (!e.GetType().FullName.Equals("System.OverflowException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xstri2B4";
                        Console.Error.WriteLine(strLoc + " More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xstri2C4";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
                inCountTestcases++;
                if (verbose) Console.WriteLine("Testing : Format");
                try
                {
                    Byte result = Convert.ToByte("!56", 10);
                    inCountErrors++;
                    strLoc = "Err_xstri2A3";
                    Console.Error.WriteLine(strLoc + " No Exception Thrown.");
                }
                catch (FormatException e)
                {
                    if (!e.GetType().FullName.Equals("System.FormatException"))
                    {
                        inCountErrors++;
                        strLoc = "Err_xstri2B3";
                        Console.Error.WriteLine(strLoc + " More specific Exception thrown : " + e.GetType().FullName);
                    }
                }
                catch (Exception e)
                {
                    inCountErrors++;
                    strLoc = "Err_xstri2C3";
                    Console.Error.WriteLine(strLoc + " Wrong Exception Thrown: " + e.GetType().FullName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Uncaught Exception in Byte Convert.ToByte( String, Int32 )");
                Console.WriteLine("Exception->" + e.GetType().FullName);
            }


            Console.Error.Write(s_strTFName);
            Console.Error.Write(": ");
            if (inCountErrors == 0)
            {
                Console.Error.WriteLine(" inCountTestcases==" + inCountTestcases + " paSs");
                return true;
            }
            else
            {
                Console.Error.WriteLine(s_strTFPath + s_strTFName + ".cs");
                Console.Error.WriteLine(" inCountTestcases==" + inCountTestcases);
                Console.Error.WriteLine("FAiL");
                Console.Error.WriteLine(" inCountErrors==" + inCountErrors);
                return false;
            }
        }

        public static int Main(String[] args)
        {
            bool bResult = false; // Assume FAiL
            cb6054ToByte_all cbX = new cb6054ToByte_all();
            try { if (args[0].Equals("-v")) cbX.verbose = true; } catch (Exception) { }

            try
            {
                printoutCoveredMethods();
                bResult = cbX.runTest();
            }
            catch (Exception exc_main)
            {
                bResult = false;
                Console.WriteLine("FAiL!  Error Err_9999zzz!  Uncaught Exception caught in main(), exc_main==" + exc_main);
            }

            if (!bResult)
            {
                Console.WriteLine(s_strTFPath + s_strTFName);
                Console.Error.WriteLine(" ");
                Console.Error.WriteLine("Try 'cb6054ToByte_all.exe -v' to see tests...");
                Console.Error.WriteLine("FAiL!  " + s_strTFAbbrev);  // Last line is a nice eye catcher location.
                Console.Error.WriteLine(" ");
            }

            return bResult ? 100 : 1;
        }
    }
}
