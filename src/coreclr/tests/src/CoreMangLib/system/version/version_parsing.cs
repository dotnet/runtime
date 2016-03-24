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

    try  
    {
        //input is null
        passing &= VerifyParseException(null, typeof(ArgumentNullException), false);

        //input is empty
        passing &= VerifyParseException(String.Empty, typeof(ArgumentException), false);

        //input is whitespace
        passing &= VerifyParseException("    ", typeof(ArgumentException), false);
        
        //input has 1 component
        passing &= VerifyParseException("1234", typeof(ArgumentException), false);

        //input has 5 components
        passing &= VerifyParseException("123.123.123.324.543", typeof(ArgumentException), false);

        //input has a negative component
        passing &= NormalCasesException("-123", typeof(ArgumentOutOfRangeException), false);
 
        //component > Int32
        passing &= NormalCasesException((((long)Int32.MaxValue) + 1).ToString("d"), typeof(OverflowException), false);
        
        //component has extra whitespace (internal and external)
        passing &= NormalCases("  123");
        passing &= NormalCases("123   ");
        passing &= NormalCases("  123   ");
        passing &= NormalCasesException("1 23", typeof(FormatException), false);

        //component in hex format
        passing &= NormalCasesException("0x123", typeof(FormatException), false);

        //component in exp format
        passing &= NormalCasesException("12e2", typeof(FormatException), false);

        //component has leading sign
        passing &= NormalCases("+123");

        //component has trailing sign
        passing &= NormalCasesException("123+", typeof(FormatException), false);
        passing &= NormalCasesException("123-", typeof(FormatException), false);

        //component has commas
        passing &= NormalCasesException("12,345", typeof(FormatException), false);

        //component has hex chars
        passing &= NormalCasesException("ac", typeof(FormatException), false);

        //component has non-digit
        passing &= NormalCasesException("12v3", typeof(FormatException), false);
        passing &= NormalCasesException("12^3", typeof(FormatException), false);
        passing &= NormalCasesException("12#3", typeof(FormatException), false);
        passing &= NormalCasesException("1*23", typeof(FormatException), false);

        //component has wrong seperator
        passing &= VerifyParseException("123,123,123,324", typeof(ArgumentException), false);
        passing &= VerifyParseException("123:123:123:324", typeof(ArgumentException), false);
        passing &= VerifyParseException("123;123;123;324", typeof(ArgumentException), false);
        passing &= VerifyParseException("123-123-123-324", typeof(ArgumentException), false);
        passing &= VerifyParseException("123 123 123 324", typeof(ArgumentException), false);

        //added "V" at start
        passing &= VerifyParseException("V123.123.123.324", typeof(FormatException), false);
        passing &= VerifyParseException("v123.123.123.324", typeof(FormatException), false);

        //normal case
        passing &= VerifyParse("123.123");
        passing &= VerifyParse("123.123.123");
        passing &= VerifyParse("123.123.123.123");

        //valid 0 case
        passing &= NormalCases("0");
        passing &= VerifyParse("0.0.0.0");

        //Int32.MaxValue cases
        passing &= NormalCases(Int32.MaxValue.ToString("d"));
    }
    catch(Exception exc_general)
    {
    	TestLibrary.Logging.WriteLine("Error: Unexpected Exception: {0}", exc_general);
        passing = false;
    }

	if (passing)
    {
        TestLibrary.Logging.WriteLine("Passed!");
	}
	else
	{
		TestLibrary.Logging.WriteLine( "Failed!" );
	}
    return passing;
 }

 private bool NormalCasesException(string input, Type exceptionType, bool tryThrows)
 {
     bool passing = true;

     passing &= VerifyParseException(String.Format("{0}.123", input), exceptionType, false);
     passing &= VerifyParseException(String.Format("123.{0}", input), exceptionType, false);
     passing &= VerifyParseException(String.Format("{0}.123.123", input), exceptionType, false);
     passing &= VerifyParseException(String.Format("123.{0}.123", input), exceptionType, false);
     passing &= VerifyParseException(String.Format("123.123.{0}", input), exceptionType, false);
     passing &= VerifyParseException(String.Format("{0}.123.123.324", input), exceptionType, false);
     passing &= VerifyParseException(String.Format("123.{0}.123.324", input), exceptionType, false);
     passing &= VerifyParseException(String.Format("123.123.{0}.324", input), exceptionType, false);
     passing &= VerifyParseException(String.Format("123.123.123.{0}", input), exceptionType, false);

     return passing;
 }
 private bool VerifyParseException(string input, Type exceptionType, bool tryThrows)
 {
     bool result = true;
     TestLibrary.Logging.WriteLine("");
     TestLibrary.Logging.WriteLine("Testing input: \"{0}\"", input);

     try
     {
         try
         {
             Version.Parse(input);
             TestLibrary.Logging.WriteLine("Version.Parse:: Expected Exception not thrown.");
             result = false;
         }
         catch (Exception e)
         {
             if (e.GetType() != exceptionType)
             {
                 TestLibrary.Logging.WriteLine("Version.Parse:: Wrong Exception thrown: Expected:{0} Got:{1}", exceptionType, e);
                 result = false;
             }
         }

         Version test;
         if (tryThrows)
         {
             try
             {
                 Version.TryParse(input, out test);
                 TestLibrary.Logging.WriteLine("Version.TryParse:: Expected Exception not thrown.");
                 result = false;
             }
             catch (Exception e)
             {
                 if (e.GetType() != exceptionType)
                 {
                     TestLibrary.Logging.WriteLine("Version.TryParse:: Wrong Exception thrown: Expected:{0} Got:{1}", exceptionType, e);
                     result = false;
                 }
             }
         }
         else
         {
             bool rVal;
             rVal = Version.TryParse(input, out test);
             if (rVal)
             {
                 TestLibrary.Logging.WriteLine("Version.TryParse:: Expected failure parsing, got true return value.");
                 result = false;
             }
             if (!IsDefaultVersion(test))
             {
                 TestLibrary.Logging.WriteLine("Version.TryParse:: Expected null, got {0} value.", test);
                 result = false;
             }
         }
     }
     catch (Exception exc)
     {
         TestLibrary.Logging.WriteLine("Unexpected exception for input: \"{0}\" exception: {1}", input, exc);
         result = false;
     }

     if (!result)
     {
         TestLibrary.Logging.WriteLine("Incorrect result for input: \"{0}\"", input);
     }

     return result;
 }

 private bool NormalCases(string input)
 {
     bool passing = true;

     passing &= VerifyParse(String.Format("{0}.123", input));
     passing &= VerifyParse(String.Format("123.{0}", input));
     passing &= VerifyParse(String.Format("{0}.123.123", input));
     passing &= VerifyParse(String.Format("123.{0}.123", input));
     passing &= VerifyParse(String.Format("123.123.{0}", input));
     passing &= VerifyParse(String.Format("{0}.123.123.324", input));
     passing &= VerifyParse(String.Format("123.{0}.123.324", input));
     passing &= VerifyParse(String.Format("123.123.{0}.324", input));
     passing &= VerifyParse(String.Format("123.123.123.{0}", input));

     return passing;
 }
 private bool VerifyParse(string input)
 {
     bool result = true;
     TestLibrary.Logging.WriteLine("");
     TestLibrary.Logging.WriteLine("Testing input: \"{0}\"", input);

     try
     {

         String[] parts = input.Split('.');
         int major = Int32.Parse(parts[0]);
         int minor = Int32.Parse(parts[1]);
         int build = 0;
         int revision = 0;
         if (parts.Length > 2) build = Int32.Parse(parts[2]);
         if (parts.Length > 3) revision = Int32.Parse(parts[3]);
         
         Version expected = null;
         if (parts.Length == 2) expected = new Version(major, minor);
         if (parts.Length == 3) expected = new Version(major, minor, build);
         if (parts.Length > 3) expected = new Version(major, minor, build, revision);

         Version test;
         try
         {
             test = Version.Parse(input);
             if (test.CompareTo(expected) != 0)
             {
                 TestLibrary.Logging.WriteLine("Version.Parse:: Expected Result. Expected {0}, Got {1}", expected, test);
                 result = false;
             }
         }
         catch (Exception e)
         {
             TestLibrary.Logging.WriteLine("Version.Parse:: Unexpected Exception thrown: {0}", e);
             result = false;
         }

         try
         {
             bool rVal;
             rVal = Version.TryParse(input, out test);
             if (!rVal)
             {
                 TestLibrary.Logging.WriteLine("Version.TryParse:: Expected no failure parsing, got false return value.");
                 result = false;
             }
             if (test.CompareTo(expected) != 0)
             {
                 TestLibrary.Logging.WriteLine("Version.TryParse:: Expected {0}, Got {1}", expected, test);
                 result = false;
             }
         }
         catch (Exception e)
         {
             TestLibrary.Logging.WriteLine("Version.TryParse:: Unexpected Exception thrown: {0}", e);
             result = false;
         }
     }
     catch (Exception exc)
     {
         TestLibrary.Logging.WriteLine("Unexpected exception for input: \"{0}\" exception:{1}", input, exc);
         result = false;
     }

     if (!result)
     {
         TestLibrary.Logging.WriteLine("Incorrect result for input: \"{0}\"", input);
     }

     return result;
 }
 private bool IsDefaultVersion(Version input)
 {
     bool ret = false;

     if (input == null)
     {
         ret = true;
     }

     return ret;
     
     //use to return Version of 0.0.0.0 on failure - now returning null.
     //return (input.CompareTo(new Version()) == 0);
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
