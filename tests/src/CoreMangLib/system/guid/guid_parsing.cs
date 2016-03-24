// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

public class Test
 {
 public Boolean runTest()
	{
	TestLibrary.Logging.WriteLine( "parse testing started." );
    bool passing = true;
    Guid testGuid = Guid.NewGuid();
    try  
    {
        //format is null (invalid case)
        passing &= VerifyParse(testGuid.ToString("B"), null, testGuid.ToString("N"), true, typeof(ArgumentNullException), false);

        //format is empty string (invalid case)
        passing &= VerifyParse(testGuid.ToString("B"), String.Empty, testGuid.ToString("N"), true, typeof(FormatException), false);

        //format is incorrect char (invalid case)
        passing &= VerifyParse(testGuid.ToString("B"), "t", testGuid.ToString("N"), true, typeof(FormatException), false);

        //format is multiple chars (invalid case)
        passing &= VerifyParse(testGuid.ToString("B"), "nfoobar", testGuid.ToString("N"), true, typeof(FormatException), false);

        //simple valid cases
        passing &= VerifyParse(testGuid.ToString("N"), "n", testGuid.ToString("N"));
        passing &= VerifyParse(testGuid.ToString("d"), "D", testGuid.ToString("N"));
        passing &= VerifyParse(testGuid.ToString("B"), "b", testGuid.ToString("N"));
        passing &= VerifyParse(testGuid.ToString("p"), "P", testGuid.ToString("N"));
        passing &= VerifyParse(testGuid.ToString("X"), "x", testGuid.ToString("N"));

        //extra white space
        passing &= VerifyParse("   " + testGuid.ToString("n") + "   ", "N", testGuid.ToString("N"));
        passing &= VerifyParse("   " + testGuid.ToString("D") + "   ", "d", testGuid.ToString("N"));
        passing &= VerifyParse("   " + testGuid.ToString("b") + "   ", "B", testGuid.ToString("N"));
        passing &= VerifyParse("   " + testGuid.ToString("P") + "   ", "p", testGuid.ToString("N"));
        passing &= VerifyParse("   " + testGuid.ToString("x") + "   ", "X", testGuid.ToString("N"));

        //missing digits in X format
        passing &= VerifyParse("{0x2345678,0x1234,0x1234,{0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12}}", "X", "02345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x234,0x1234,{0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12}}", "X", "12345678023412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x234,{0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12}}", "X", "12345678123402341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x2,0x12,0x12,0x12,0x12,0x12,0x12,0x12}}", "X", "12345678123412340212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x2,0x12,0x12,0x12,0x12,0x12,0x12}}", "X", "12345678123412341202121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12,0x2,0x12,0x12,0x12,0x12,0x12}}", "X", "12345678123412341212021212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12,0x12,0x2,0x12,0x12,0x12,0x12}}", "X", "12345678123412341212120212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12,0x12,0x12,0x2,0x12,0x12,0x12}}", "X", "12345678123412341212121202121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12,0x12,0x12,0x12,0x2,0x12,0x12}}", "X", "12345678123412341212121212021212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12,0x12,0x12,0x12,0x12,0x2,0x12}}", "X", "12345678123412341212121212120212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x2}}", "X", "12345678123412341212121212121202");
        passing &= VerifyParse("{0x2345678,0x234,0x234,{0x2,0x2,0x2,0x2,0x2,0x2,0x2,0x2}}", "X", "02345678023402340202020202020202");
     
        //internal whitespace in X format, not in the number
        passing &= VerifyParse("{   0x12345678,0x1234,0x1234,{0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678   ,0x1234,0x1234,{0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,   0x1234,0x1234,{0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234   ,0x1234,{0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,   0x1234,{0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234   ,{0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,   {0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{   0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12   ,0x12,0x12,0x12,0x12,0x12,0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,   0x12,0x12,0x12,0x12,0x12,0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12   ,0x12,0x12,0x12,0x12,0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12,   0x12,0x12,0x12,0x12,0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12,0x12   ,0x12,0x12,0x12,0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12,0x12,   0x12,0x12,0x12,0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12,0x12,0x12   ,0x12,0x12,0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12,0x12,0x12,   0x12,0x12,0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12,0x12,0x12,0x12   ,0x12,0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12,0x12,0x12,0x12,   0x12,0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12,0x12,0x12,0x12,0x12   ,0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12,0x12,0x12,0x12,0x12,   0x12,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12,0x12,0x12,0x12,0x12,0x12   ,0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12,0x12,0x12,0x12,0x12,0x12,   0x12}}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12   }}", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{0x12345678,0x1234,0x1234,{0x12,0x12,0x12,0x12,0x12,0x12,0x12,0x12}   }", "X", "12345678123412341212121212121212");
        passing &= VerifyParse("{   0x12345678   ,   0x1234   ,   0x1234   ,   {   0x12   ,   0x12   ,   0x12   ,   0x12   ,   0x12   ,   0x12   ,   0x12   ,   0x12   }   }", "X", "12345678123412341212121212121212");
        
        //null string
        passing &= VerifyParse(null, "N", null, false, typeof(ArgumentNullException));
        passing &= VerifyParse(null, "D", null, false, typeof(ArgumentNullException));
        passing &= VerifyParse(null, "B", null, false, typeof(ArgumentNullException));
        passing &= VerifyParse(null, "P", null, false, typeof(ArgumentNullException));
        passing &= VerifyParse(null, "X", null, false, typeof(ArgumentNullException));

        //empty string
        passing &= VerifyParse(String.Empty, "N", null, false, typeof(FormatException));
        passing &= VerifyParse(String.Empty, "D", null, false, typeof(FormatException));
        passing &= VerifyParse(String.Empty, "B", null, false, typeof(FormatException));
        passing &= VerifyParse(String.Empty, "P", null, false, typeof(FormatException));
        passing &= VerifyParse(String.Empty, "X", null, false, typeof(FormatException));

        //whitespace string
        passing &= VerifyParse("   ", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("   ", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("   ", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("   ", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("   ", "X", null, false, typeof(FormatException));

        //internal whitespace in N format
        passing &= VerifyParse("1   234567890abcdef1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("12   34567890abcdef1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("123   4567890abcdef1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234   567890abcdef1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("12345   67890abcdef1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("123456   7890abcdef1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567   890abcdef1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678   90abcdef1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("123456789   0abcdef1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890   abcdef1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890a   bcdef1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890ab   cdef1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abc   def1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcd   ef1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcde   f1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef   1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef1   234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef12   34567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef123   4567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef1234   567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef12345   67890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef123456   7890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef1234567   890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef12345678   90ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef123456789   0ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef1234567890   ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef1234567890A   BCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef1234567890AB   CDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef1234567890ABC   DEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef1234567890ABCD   EF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef1234567890ABCDE   F", "N", null, false, typeof(FormatException));

        //internal whitespace in D format
        passing &= VerifyParse("1   2345678-90ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12   345678-90ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("123   45678-90ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("1234   5678-90ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345   678-90ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("123456   78-90ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567   8-90ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678   -90ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-   90ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-9   0ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90   ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90a   b-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab   -cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-   cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-c   def-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cd   ef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cde   f-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef   -1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-   1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1   234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-12   34-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-123   4-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234   -567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234-   567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234-5   67890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234-56   7890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234-567   890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234-5678   90ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234-56789   0ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234-567890   ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234-567890A   BCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234-567890AB   CDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234-567890ABC   DEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234-567890ABCD   EF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234-567890ABCDE   F", "D", null, false, typeof(FormatException));

        //internal whitespace in B format
        passing &= VerifyParse("{   12345678-90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{1   2345678-90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12   345678-90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{123   45678-90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{1234   5678-90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345   678-90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{123456   78-90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{1234567   8-90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678   -90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-   90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-9   0ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90   ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90a   b-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab   -cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-   cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-c   def-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cd   ef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cde   f-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef   -1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-   1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1   234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-12   34-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-123   4-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234   -567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-   567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-5   67890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-56   7890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-567   890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-5678   90ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-56789   0ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-567890   ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-567890A   BCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-567890AB   CDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-567890ABC   DEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-567890ABCD   EF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-567890ABCDE   F}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-567890ABCDEF   }", "B", null, false, typeof(FormatException));

        //internal whitespace in P format
        passing &= VerifyParse("(   12345678-90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(1   2345678-90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12   345678-90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(123   45678-90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(1234   5678-90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345   678-90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(123456   78-90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(1234567   8-90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678   -90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-   90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-9   0ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90   ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90a   b-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab   -cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-   cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-c   def-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cd   ef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cde   f-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef   -1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-   1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1   234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-12   34-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-123   4-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234   -567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-   567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-5   67890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-56   7890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-567   890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-5678   90ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-56789   0ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-567890   ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-567890A   BCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-567890AB   CDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-567890ABC   DEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-567890ABCD   EF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-567890ABCDE   F)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-567890ABCDEF   )", "P", null, false, typeof(FormatException));

        //internal whitespace in X format in a number
        passing &= VerifyParse("{0   x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x   12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x1   2345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12   345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x123   45678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x1234   5678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345   678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x123456   78,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x1234567   8,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0   x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x   90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x9   0ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90   ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90a   b,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0   xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0x   cdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xc   def,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcd   ef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcde   f,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0   x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x   1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1   234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x12   34,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x123   4,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0   x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x   56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x5   6,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0   x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x   78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x7   8,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0   x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x   90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x9   0,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0   xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0x   AB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xA   B,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0   xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0x   CD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xC   D,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0   xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0x   EF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xE   F}}", "X", null, false, typeof(FormatException));
       
        //missing non-digit in D format
        passing &= VerifyParse("1234567890ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90abcdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef1234567890ABCDEF", "D", null, true, typeof(FormatException), false);

        //missing non-digit in B format
        passing &= VerifyParse("12345678-90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{1234567890ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90abcdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-567890ABCDEF", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef1234567890ABCDEF", "B", null, true, typeof(FormatException), false);
        
        //missing non-digit in P format
        passing &= VerifyParse("12345678-90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(1234567890ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90abcdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-567890ABCDEF", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef1234567890ABCDEF", "P", null, true, typeof(FormatException), false);

        //missing non-digit in X format
        passing &= VerifyParse("0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{012345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x123456780x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,090ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0cdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,01234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{056,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x560x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,078,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x780x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,090,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x900xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0AB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0CD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0EF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,0x56,0x78,0x90,0xAB,0xCD,0xEF}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,cdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,AB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,CD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,EF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678,90ab,cdef,1234,{56,78,90,AB,CD,EF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{1234567890abcdef1234567890ABCDEF}", "X", null, true, typeof(FormatException), false);
        passing &= VerifyParse("(1234567890abcdef1234567890ABCDEF)", "X", null, true, typeof(FormatException), false);
        passing &= VerifyParse("1234567890abcdef1234567890ABCDEF", "X", null, true, typeof(FormatException), false);

        //misplaced non-digit in D format
        passing &= VerifyParse("1234567-890ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90a-bcdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cde-f1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-12345-67890ABCDEF", "D", null, false, typeof(FormatException));

        //misplaced non-digit in B format
        passing &= VerifyParse("1{2345678-90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{123456789-0ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90abc-def-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef1-234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-12345-67890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-567890ABCDE}F", "B", null, false, typeof(FormatException));

        //misplaced non-digit in P format
        passing &= VerifyParse("1(2345678-90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(123456789-0ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90abc-def-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef1-234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-12345-67890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-567890ABCDE)F", "P", null, false, typeof(FormatException));

        //misplaced non-digit in X format
        passing &= VerifyParse("0x{12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{x012345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{01x2345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x1234567,80x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,x090ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,09x0ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab0,xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,x0cdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0cxdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef0,x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,x01234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,01x234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234{,0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234{,0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{x056,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{05x6,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x560,x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,x078,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,07x8,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x780,x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,x090,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,09x0,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x900x,AB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,xA0B,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0AxB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB0,xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,x0CD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0CxD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD0,xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,x0EF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0ExF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xE}F}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,{0x1234,0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("0x12345678,{}0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{12340x5678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x123456780x,90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,c0xdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef0x,1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,0x{56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x560x,78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,90x0,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,A0xB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB0x,CD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD0x,EF}}", "X", null, false, typeof(FormatException));

        //wrong non-digit in D format
        passing &= VerifyParse("12345678+90ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab|cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef=1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234_567890ABCDEF", "D", null, false, typeof(FormatException));

        //wrong non-digit in B format
        passing &= VerifyParse("[12345678-90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678+90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab=cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef|1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234*567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-567890ABCDEF]", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("[12345678-90ab-cdef-1234-567890ABCDEF]", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-567890ABCDEF)", "B", null, true, typeof(FormatException), false);

        //wrong non-digit in P format
        passing &= VerifyParse("{12345678-90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678+90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab?cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef*1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234>567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-567890ABCDEF>", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("[12345678-90ab-cdef-1234-567890ABCDEF]", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-567890ABCDEF}", "P", null, true, typeof(FormatException), false);
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "P", null, true, typeof(FormatException), false);

        //wrong non-digit in X format
        passing &= VerifyParse("(0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{ox12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0.12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678.0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,Ox90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0?90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab=0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,hxcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0hcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef;0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,Hx1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0H1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234:{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,[0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{ox56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0o56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56|0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,Ox78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0O78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78~0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,bx90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0b90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90;0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,-xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0-AB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB;0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,hxCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0hCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD;0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,hxEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0hEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF})", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,(0x56,0x78,0x90,0xAB,0xCD,0xEF)}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("(0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF})", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("(0x12345678,0x90ab,0xcdef,0x1234,(0x56,0x78,0x90,0xAB,0xCD,0xEF))", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,[0x56,0x78,0x90,0xAB,0xCD,0xEF]}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("[0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}]", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("[0x12345678,0x90ab,0xcdef,0x1234,[0x56,0x78,0x90,0xAB,0xCD,0xEF]]", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-567890ABCDEF}", "X", null, true, typeof(FormatException), false);
        passing &= VerifyParse("(12345678-90ab-cdef-1234-567890ABCDEF)", "X", null, true, typeof(FormatException), false);
        
        //extra non-digits in N format
        passing &= VerifyParse("+1234567890abcdef1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abc+def1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890abcdef1234567890ABCDEF+", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234-567890ABCDEF", "N", null, true, typeof(FormatException), false);
        passing &= VerifyParse("{12345678-90ab-cdef-1234-567890ABCDEF}", "N", null, true, typeof(FormatException), false);
        passing &= VerifyParse("(12345678-90ab-cdef-1234-567890ABCDEF)", "N", null, true, typeof(FormatException), false);
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "N", null, true, typeof(FormatException), false);

        //extra non-digits in D format
        passing &= VerifyParse("-12345678-90ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("1234-5678-90ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678--90ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90-ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab--cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cd-ef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef--1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-12-34-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234--567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234-567890ABCDEF-", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678*-90ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab+-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef=-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234|-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("[12345678-90ab-cdef-1234-567890ABCDEF]", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-567890ABCDEF)", "D", null, true, typeof(FormatException), false);
        passing &= VerifyParse("{12345678-90ab-cdef-1234-567890ABCDEF}", "D", null, true, typeof(FormatException), false);
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "D", null, true, typeof(FormatException), false);

        //extra non-digits in B format
        passing &= VerifyParse("{{12345678-90ab-cdef-1234-567890ABCDEF}}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{1234-5678-90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678--90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90-ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab--cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cd-ef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef--1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-12-34-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234--567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-567890-ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{(12345678-90ab-cdef-1234-567890ABCDEF)}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("({12345678-90ab-cdef-1234-567890ABCDEF})", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{1234+5678-90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90=ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cd*ef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-12|34-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-5678&90ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("^{12345678-90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{%12345678-90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-567890ABCDEF$}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-567890ABCDEF}#", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "B", null, true, typeof(FormatException), false);

        //extra non-digits in P format
        passing &= VerifyParse("((12345678-90ab-cdef-1234-567890ABCDEF))", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(1234-5678-90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678--90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90-ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab--cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cd-ef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef--1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-12-34-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234--567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-567890-ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("({12345678-90ab-cdef-1234-567890ABCDEF})", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("{(12345678-90ab-cdef-1234-567890ABCDEF)}", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(1234+5678-90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90=ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cd*ef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-12|34-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-5678&90ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("^(12345678-90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(%12345678-90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-567890ABCDEF$)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-567890ABCDEF)#", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(0x12345678,0x90ab,0xcdef,0x1234,(0x56,0x78,0x90,0xAB,0xCD,0xEF))", "P", null, false, typeof(FormatException));

        //extra non-digits in X format
        passing &= VerifyParse("{{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("({0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}})", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{(0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF})}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{{0x56,0x78,0x90,0xAB,0xCD,0xEF}}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,({0x56,0x78,0x90,0xAB,0xCD,0xEF})}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{(0x56,0x78,0x90,0xAB,0xCD,0xEF)}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{00x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0xx12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,00x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0xx90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,00xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xxcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,00x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0xx1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{00x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0xx56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,00x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0xx78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,00x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0xx90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,00xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xxAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,00xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xxCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,00xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xxEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678-,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab-,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef-,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234-,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x1234!5678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90@ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xc#def,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1$234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x5%6,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x7^8,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x9&0,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xA*B,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xC<D,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xE>F}}", "X", null, false, typeof(FormatException));

        //missing digit in N format
        passing &= VerifyParse("234567890abcdef1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("1234567890ABCDEF", "N", null, false, typeof(FormatException));

        //missing digit in D format
        passing &= VerifyParse("2345678-90ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-9ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdf-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-134-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234-56789ABCDEF", "D", null, false, typeof(FormatException));

        //missing digit in B format
        passing &= VerifyParse("{2345678-90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-9ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdf-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-134-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-56789ABCDEF}", "B", null, false, typeof(FormatException));

        //missing digit in P format
        passing &= VerifyParse("(2345678-90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-9ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdf-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-134-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-56789ABCDEF)", "P", null, false, typeof(FormatException));

        //misplaced digit in X format
        passing &= VerifyParse("{0x2345678,0x901ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x0ab,0x9cdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xdef,0x12c34,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x123415678,0x90ab,0xcdef,0x234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x6,0x578,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x8,0x970,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x798,0x0,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xACD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xD,0xECF}}", "X", null, false, typeof(FormatException));
       
        //invalid digit in N format
        passing &= VerifyParse("1234567890abcdeg1234567890ABCDEF", "N", null, false, typeof(FormatException));

        //invalid digit in D format
        passing &= VerifyParse("234g5678-90ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-9hab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdif-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-13j4-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234-56789GABCDEF", "D", null, false, typeof(FormatException));

        //invalid digit in B format
        passing &= VerifyParse("{234g5678-90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-9hab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdif-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-13j4-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-56789GABCDEF}", "B", null, false, typeof(FormatException));

        //invalid digit in P format
        passing &= VerifyParse("(234g5678-90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-9hab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdif-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-13j4-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-56789GABCDEF)", "P", null, false, typeof(FormatException));

        //invalid digit in X format
        passing &= VerifyParse("{0x123g5678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x9hab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcief,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1G34,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0xH6,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x7I,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0xJ0,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAK,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xLD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xMF}}", "X", null, false, typeof(FormatException));

        //extra digit in N format
        passing &= VerifyParse("12345678190abcdef1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("123456781234590abcdef1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("01234567890abcdef1234567890ABCDEF", "N", null, false, typeof(FormatException));
        passing &= VerifyParse("0000001234567890abcdef1234567890ABCDEF", "N", null, false, typeof(FormatException));

        //extra digit in the D format
        passing &= VerifyParse("12343455678-90ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-930ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-c5def-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-13234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234-563457890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("012345678-90ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-0090ab-cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-0cdef-1234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-00001234-567890ABCDEF", "D", null, false, typeof(FormatException));
        passing &= VerifyParse("12345678-90ab-cdef-1234-00567890ABCDEF", "D", null, false, typeof(FormatException));

        //extra digit in the B format
        passing &= VerifyParse("{112345678-90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-910ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-11cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1111234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-111111111567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{00012345678-90ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-090ab-cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-0000cdef-1234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-01234-567890ABCDEF}", "B", null, false, typeof(FormatException));
        passing &= VerifyParse("{12345678-90ab-cdef-1234-00000567890ABCDEF}", "B", null, false, typeof(FormatException));

        //extra digit in the P format
        passing &= VerifyParse("(121111345678-90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-92220ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cd3ef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-14234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-5655557890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(00012345678-90ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-090ab-cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-0cdef-1234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-001234-567890ABCDEF)", "P", null, false, typeof(FormatException));
        passing &= VerifyParse("(12345678-90ab-cdef-1234-00000567890ABCDEF)", "P", null, false, typeof(FormatException));

        //extra digit in the X format
        passing &= VerifyParse("{0x121111345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x920ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xc33def,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x124434,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x556,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0xa78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x9b0,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAcB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCDD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x012345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x00090ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0x00cdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x001234,{0x56,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x056,0x78,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x078,0x90,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x0090,0xAB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0x0000AB,0xCD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0x00CD,0xEF}}", "X", null, false, typeof(FormatException));
        passing &= VerifyParse("{0x12345678,0x90ab,0xcdef,0x1234,{0x56,0x78,0x90,0xAB,0xCD,0x00EF}}", "X", null, false, typeof(FormatException));
    }
    catch(Exception exc_general)
    {
    	TestLibrary.Logging.WriteLine("Error: Unexpected Exception: {0}", exc_general);
        passing = false;
    }

	if (passing)
	{
		Console.Write( "Passed!" );
	}
	else
	{
		TestLibrary.Logging.WriteLine( "Failed!" );
	}
    return passing;
 }

 private bool VerifyParse(string input, string format, string resultN)
 {
     return VerifyParse(input, format, resultN, true, null, true);
 }

 private bool VerifyParse(string input, string format, string resultN, bool valid, Type exceptionType)
 {
     return VerifyParse(input, format, resultN, valid, exceptionType, true);
 }

 private bool VerifyParse(string input, string format, string resultN, bool valid, Type exceptionType, bool validFormat)
 {
     bool result = true;
     TestLibrary.Logging.WriteLine("");
     TestLibrary.Logging.WriteLine("Testing input: \"{0}\" with format:{1}", input, format);

     try
     {
         if (!validFormat) //only test the methods that take a format - invalid due to format
         {
             Guid guid3, guid4;
             bool try2;

             try
             {
                 guid3 = Guid.ParseExact(input, format);
                 TestLibrary.Logging.WriteLine("Expected Exception not thrown: {0}", exceptionType);
                 result = false;
             }
             catch (Exception ex)
             {
                 if (ex.GetType() != exceptionType)
                 {
                     TestLibrary.Logging.WriteLine("Wrong Exception thrown: Expected:{0} Got:{1}", exceptionType, ex);
                     result = false;
                 }
             }

             if (valid)  //valid Guid but doesn't match format specifier.
             {
                 try2 = Guid.TryParseExact(input, format, out guid4);
                 if (try2)
                 {
                     TestLibrary.Logging.WriteLine("Incorrect return value from try.");
                     result = false;
                 }
                 if (guid4 != Guid.Empty)
                 {
                     TestLibrary.Logging.WriteLine("Guid returned from try not Empty.");
                     result = false;
                 }
             }
             else
             {
                 try
                 {
                     try2 = Guid.TryParseExact(input, format, out guid4);
                     TestLibrary.Logging.WriteLine("Expected Exception not thrown: {0}", exceptionType);
                     result = false;
                 }
                 catch (Exception ex)
                 {
                     if (ex.GetType() != exceptionType)
                     {
                         TestLibrary.Logging.WriteLine("Wrong Exception thrown: Expected:{0} Got:{1}", exceptionType, ex);
                         result = false;
                     }
                 }
             }

         }
         else if (valid) //valid case
         {
             Guid guid1, guid2, guid3, guid4;
             bool try1, try2;

             guid1 = Guid.Parse(input);
             try1 = Guid.TryParse(input, out guid2);
             guid3 = Guid.ParseExact(input, format);
             try2 = Guid.TryParseExact(input, format, out guid4);

             if (guid4.ToString("N") != resultN)
             {
                 TestLibrary.Logging.WriteLine("Wrong Result: Expected:{0} Got:{1}", resultN, guid4.ToString("N"));
                 result = false;
             }
             if ((guid1 != guid4) || (guid2 != guid4) || (guid3 != guid4))
             {
                 TestLibrary.Logging.WriteLine("Not all results equal.");
                 result = false;
             }
             if (!try1 || !try2)
             {
                 TestLibrary.Logging.WriteLine("Incorrect return value from try.");
                 result = false;
             }
         }
         else  //invalid case
         {
             Guid guid1, guid2, guid3, guid4;
             bool try1, try2;

             try1 = Guid.TryParse(input, out guid2);
             try2 = Guid.TryParseExact(input, format, out guid4);
             if (try1 || try2 )
             {
                 TestLibrary.Logging.WriteLine("Incorrect return value from try.");
                 result = false;
             }
             if (guid2 != Guid.Empty || guid4 != Guid.Empty)
             {
                 TestLibrary.Logging.WriteLine("Guid returned from try not Empty.");
                 result = false;
             }

             try
             {
                 guid1 = Guid.Parse(input);
                 TestLibrary.Logging.WriteLine("Expected Exception not thrown: {0}", exceptionType);
                 result = false;
             }
             catch (Exception ex)
             {
                 if (ex.GetType() != exceptionType)
                 {
                     TestLibrary.Logging.WriteLine("Wrong Exception thrown: Expected:{0} Got:{1}", exceptionType, ex);
                     result = false;
                 }
             }

             try
             {
                 guid3 = Guid.ParseExact(input, format);
                 TestLibrary.Logging.WriteLine("Expected Exception not thrown: {0}", exceptionType);
                 result = false;
             }
             catch (Exception ex)
             {
                 if (ex.GetType() != exceptionType)
                 {
                     TestLibrary.Logging.WriteLine("Wrong Exception thrown: Expected:{0} Got:{1}", exceptionType, ex);
                     result = false;
                 }
             }
         }
     }
     catch (Exception exc)
     {
         TestLibrary.Logging.WriteLine("Unexpected exception for input: \"{0}\" with format:{1} exception:{2}", input, format, exc);
     }

     if (!result)
     {
         TestLibrary.Logging.WriteLine("Incorrect result for input: \"{0}\" with format:{1}", input, format);
     }

     return result;
 }

 public static int Main(String[] args) 
 {
	Boolean bResult = false;
    Test test = new Test();

	try
	{
		bResult = test.runTest();
	}
	catch (Exception exc)
	{
		bResult = false;
		TestLibrary.Logging.WriteLine("Unexpected Exception thrown: {0}", exc);
	}

	if (bResult == false) return 1;

    return 100;
 }
}
