// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;	//StackFrame
using System.IO;
using System.Xml;
using System.Text;

namespace OLEDB.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////
    // CError
    //
    ////////////////////////////////////////////////////////////////
    public class CError
    {
        //Data
        private static IError s_rIError;
        private static ITestConsole s_rITestConsole;
        private static CLTMConsole s_rLTMConsole;

        //Constructor
        public CError()
        {
        }

        static public IError Error
        {
            set
            {
                //Set the static Error interface...	
                s_rIError = value;

                //Setup the standard c# Console to log to LTM instead...
                //ie: Console.WriteLine will automatically log to LTM
                if (s_rLTMConsole == null)
                {
                    s_rLTMConsole = new CLTMConsole();
                }

                //The Error object may also support additional interfaces
                s_rITestConsole = value as ITestConsole;

                //Disable Asserts
                DisableAsserts();
            }

            get { return s_rIError; }
        }

        static public TextWriter Out
        {
            get { return s_rLTMConsole; }
        }

        static public ITestConsole TestConsole
        {
            get { return s_rITestConsole; }
        }

        static internal void Dispose()
        {
            //Reset the info.  
            s_rIError = null;
            s_rITestConsole = null;

            //Remove listeners
            s_rLTMConsole = null;
        }

        static internal void DisableAsserts()
        {
        }

        //Helpers
        static public void Increment()
        {
            Error.Increment();
        }

        static public tagERRORLEVEL ErrorLevel
        {
            get
            {
                if (Error != null)
                    return Error.GetErrorLevel();
                return tagERRORLEVEL.HR_STRICT;
            }
            set
            {
                if (Error != null)
                    Error.SetErrorLevel(value);
            }
        }

        static public void Transmit(string text)
        {
            Write(text);
        }

        static public string NewLine
        {
            get { return "\n"; }
        }

        static public void Write(object value)
        {
            if (value != null)
                Write(value.ToString());
        }

        static public void WriteLine(object value)
        {
            Write(value);
            WriteLine();
        }

        static public void Write(string text)
        {
            Write(tagCONSOLEFLAGS.CONSOLE_TEXT, text);
        }

        static public void WriteLine(string text)
        {
            Write(tagCONSOLEFLAGS.CONSOLE_TEXT, text);
            WriteLine();
        }

        static public void Write(string text, params object[] args)
        {
            //Delegate
            Write(String.Format(text, args));
        }

        static public void WriteLine(string text, params object[] args)
        {
            //Delegate
            WriteLine(String.Format(text, args));
        }

        static public void Write(char[] value)
        {
            //Delegate
            if (value != null)
                Write(new string(value));
        }

        static public void WriteLine(char[] value)
        {
            //Delegate
            if (value != null)
                Write(new string(value));
            WriteLine();
        }

        static public void WriteXml(string text)
        {
            Write(tagCONSOLEFLAGS.CONSOLE_XML, text);
        }

        static public void WriteRaw(string text)
        {
            Write(tagCONSOLEFLAGS.CONSOLE_RAW, text);
        }

        static public void WriteIgnore(string text)
        {
            Write(tagCONSOLEFLAGS.CONSOLE_IGNORE, text);
        }

        static public void WriteLineIgnore(string text)
        {
            Write(tagCONSOLEFLAGS.CONSOLE_IGNORE, text + CError.NewLine);
        }

        static public void Write(tagCONSOLEFLAGS flags, string text)
        {
            if (flags == tagCONSOLEFLAGS.CONSOLE_TEXT)
            {
                text = FixupXml(text);
            }
            //NOTE:You can also simply use Console.WriteLine and have it show up in LTM...
            //Is the new ITestConsole interface available (using a new LTM)
            if (TestConsole != null)
            {
                TestConsole.Write(flags, text);
            }
            else if (Error != null)
            {
                //Otherwise
                Error.Transmit(text);
            }
        }

        static public void WriteLine()
        {
            if (TestConsole != null)
                TestConsole.WriteLine();
            else if (Error != null)
                Error.Transmit(CError.NewLine);
        }

        static public bool Compare(bool equal, string message)
        {
            if (equal)
                return true;
            return Compare(false, true, message);
        }

        static public bool Compare(object actual, object expected, string message)
        {
            if (InternalEquals(actual, expected))
                return true;

            //Compare not only compares but throws - so your test stops processing
            //This way processing stops upon the first error, so you don't have to check return
            //values or validate values afterwards.  If you have other items to do, then use the 
            //CError.Equals instead of CError.Compare
            Console.WriteLine("ERROR: {0}", message);
            Console.WriteLine("Expected: {0}", expected);
            Console.WriteLine("Actual  : {0}", actual);
            throw new CTestFailedException(message, actual, expected, null);
        }

        static public bool Compare(object actual, object expected1, object expected2, string message)
        {
            if (InternalEquals(actual, expected1) || InternalEquals(actual, expected2))
                return true;

            //Compare not only compares but throws - so your test stops processing
            //This way processing stops upon the first error, so you don't have to check return
            //values or validate values afterwards.  If you have other items to do, then use the 
            //CError.Equals instead of CError.Compare
            Console.WriteLine("expected1: " + expected1);
            Console.WriteLine("expected2: " + expected2);

            throw new CTestFailedException(message, actual, expected1, null);
            //return false;
        }

        static public bool Equals(object actual, object expected, string message)
        {
            try
            {
                //Equals is identical to Compare, except that Equals doesn't throw.
                //This way if We still want to throw the exception so we get the logging and comapre block
                //but the test wants to continue to do other things.
                return CError.Compare(actual, expected, message);
            }
            catch (Exception e)
            {
                CTestBase.HandleException(e);
                return false;
            }
        }

        static public bool Equals(bool equal, string message)
        {
            try
            {
                //Equals is identical to Compare, except that Equals doesn't throw.
                //This way if We still want to throw the exception so we get the logging and comapre block
                //but the test wants to continue to do other things.
                return CError.Compare(equal, message);
            }
            catch (Exception e)
            {
                CTestBase.HandleException(e);
                return false;
            }
        }

        static public bool Warning(bool equal, string message)
        {
            return Warning(equal, true, message, null);
        }

        static public bool Warning(object actual, object expected, string message)
        {
            return Warning(actual, expected, message, null);
        }

        static public bool Warning(object actual, object expected, string message, Exception inner)
        {
            //See if these are equal
            bool equal = InternalEquals(actual, expected);
            if (equal)
                return true;

            try
            {
                //Throw a warning exception
                throw new CTestException(CTestBase.TEST_WARNING, message, actual, expected, inner);
            }
            catch (Exception e)
            {
                //Warning should continue - not halt test progress
                CTestBase.HandleException(e);
                return false;
            }
        }

        static public bool Skip(string message)
        {
            //Delegate
            return Skip(true, message);
        }

        static public bool Skip(bool skip, string message)
        {
            if (skip)
                throw new CTestSkippedException(message);
            return false;
        }

        static internal bool InternalEquals(object actual, object expected)
        {
            //Handle null comparison
            if (actual == null && expected == null)
                return true;
            else if (actual == null || expected == null)
                return false;

            //Otherwise
            return expected.Equals(actual);
        }

        static public bool Log(object actual, object expected, string source, string message, string details, tagERRORLEVEL eErrorLevel)
        {
            //Obtain the error level
            tagERRORLEVEL rSavedLevel = ErrorLevel;

            //Set the error level
            ErrorLevel = eErrorLevel;
            try
            {
                //Get caller function, 0=current
                //StackTrace rStackTrace = new StackTrace();
                //StackFrame rStackFrame = rStackTrace.GetFrame(1);

                //Log the error
                if (TestConsole != null)
                {
                    //ITestConsole.Log
                    TestConsole.Log(Common.Format(actual),			//actual
                                        Common.Format(expected),		//expected
                                        source,							//source
                                        message,						//message
                                        details,						//details
                                        tagCONSOLEFLAGS.CONSOLE_TEXT,	//flags
                                        "fake_filename",
                                        999
                                    );
                }

                else if (Error != null)
                {
                    //We call IError::Compare, which logs the error AND increments the error count...
                    Console.WriteLine("Message:\t" + message);
                    Console.WriteLine("Source:\t\t" + source);
                    Console.WriteLine("Expected:\t" + expected);
                    Console.WriteLine("Received:\t" + actual);
                    Console.WriteLine("Details:" + CError.NewLine + details);
                }
            }

            finally
            {
                //Restore the error level
                ErrorLevel = rSavedLevel;
            }
            return false;
        }

        private static String FixupXml(String value)
        {
            bool escapeXmlStuff = false;
            if (value == null) return null;

            StringBuilder b = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                switch (value[i])
                {
                    case '&':
                        if (escapeXmlStuff) b.Append("&amp;"); else b.Append('&');
                        break;
                    case '<':
                        if (escapeXmlStuff) b.Append("&lt;"); else b.Append('<');
                        break;
                    case '>':
                        if (escapeXmlStuff) b.Append("&gt;"); else b.Append('>');
                        break;
                    case '"':
                        if (escapeXmlStuff) b.Append("&quot;"); else b.Append('"');
                        break;
                    case '\'':
                        if (escapeXmlStuff) b.Append("&apos;"); else b.Append('\'');
                        break;
                    case '\t':
                        b.Append('\t');
                        break;
                    case '\r':
                        b.Append('\r');
                        break;
                    case '\n':
                        b.Append('\n');
                        break;
                    default:
                        if ((value[i] < 0x20) || value[i] >= 0x80)
                        {
                            b.Append(PrintUnknownCharacter(value[i]));
                        }
                        else
                        {
                            b.Append(value[i]);
                        }
                        break;
                }
            }
            return b.ToString();
        }

        private static string PrintUnknownCharacter(char ch)
        {
            int number = ch;
            string result = string.Empty;
            if (number == 0)
            {
                result = "0";
            }
            while (number > 0)
            {
                int n = number % 16;
                number = number / 16;
                if (n < 10)
                {
                    result = (char)(n + (int)'0') + result;
                }
                else
                {
                    result = (char)(n - 10 + (int)'A') + result;
                }
            }
            return "_x" + result + "_";
        }
    }
}
