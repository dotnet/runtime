// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class WriteLine
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/05 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.WriteLine(string)";
    public static readonly String s_strTFName = "WriteLine.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //The string size used when verifying NewLine
    public static readonly int NEWLINE_TESTING_STRING_SIZE = 4;

    //The string size used when veryifying encoding 
    public static readonly int ENCODING_STRING_SIZE = 4;

    //The string size used for large string testing
    public static readonly int LARGE_STRING_SIZE = 2048;

    //Delegate to start asynchronous write on the SerialPort com with string of size strSize
    public delegate void AsyncWriteDelegate(SerialPort com, int strSize);

    //The default number of times the write method is called when verifying write
    public static readonly int DEFAULT_NUM_WRITES = 3;
    public static readonly string DEFAULT_NEW_LINE = "\n";
    public static readonly int MIN_NUM_NEWLINE_CHARS = 1;
    public static readonly int MAX_NUM_NEWLINE_CHARS = 5;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        WriteLine objTest = new WriteLine();
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(objTest.AppDomainUnhandledException_EventHandler);

        Console.WriteLine(s_strTFPath + " " + s_strTFName + " , for " + s_strClassMethod + " , Source ver : " + s_strDtTmVer);

        try
        {
            objTest.RunTest();
        }
        catch (Exception e)
        {
            Console.WriteLine(s_strTFAbbrev + " : FAIL The following exception was thorwn in RunTest(): \n" + e.ToString());
            objTest._numErrors++;
            objTest._exitValue = TCSupport.FailExitCode;
        }

        ////	Finish Diagnostics
        if (objTest._numErrors == 0)
        {
            Console.WriteLine("PASS.	 " + s_strTFPath + " " + s_strTFName + " ,numTestcases==" + objTest._numTestcases);
        }
        else
        {
            Console.WriteLine("FAIL!	 " + s_strTFPath + " " + s_strTFName + " ,numErrors==" + objTest._numErrors);

            if (TCSupport.PassExitCode == objTest._exitValue)
                objTest._exitValue = TCSupport.FailExitCode;
        }

        Environment.ExitCode = objTest._exitValue;
    }

    private void AppDomainUnhandledException_EventHandler(Object sender, UnhandledExceptionEventArgs e)
    {
        _numErrors++;
        Console.WriteLine("\nAn unhandled exception was thrown and not caught in the app domain: \n{0}", e.ExceptionObject);
        Console.WriteLine("Test FAILED!!!\n");

        Environment.ExitCode = 101;
    }

    public bool RunTest()
    {
        bool retValue = true;
        TCSupport tcSupport = new TCSupport();

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ASCIIEncoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        //		retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF7Encoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF8Encoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UTF32Encoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(UnicodeEncoding), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(NullString), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(EmptyString), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(String_Null_Char), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(LargeString), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(StrContains_NewLine_RND), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(StrContains_NewLine_CRLF), TCSupport.SerialPortRequirements.LoopbackOrNullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(StrContains_NewLine_null), TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool ASCIIEncoding()
    {
        Console.WriteLine("Verifying write method with ASCIIEncoding");
        if (!VerifyWrite(new System.Text.ASCIIEncoding(), ENCODING_STRING_SIZE, GenRandomNewLine(true)))
        {
            Console.WriteLine("Err_001!!! Verifying write method with ASCIIEncoding FAILED");
            return false;
        }

        return true;
    }


    public bool UTF7Encoding()
    {
        Console.WriteLine("Verifying write method with UTF7Encoding");
        if (!VerifyWrite(new System.Text.UTF7Encoding(), ENCODING_STRING_SIZE, GenRandomNewLine(false)))
        {
            Console.WriteLine("Err_002!!! Verifying write method with UTF7Encoding FAILED");
            return false;
        }

        return true;
    }


    public bool UTF8Encoding()
    {
        Console.WriteLine("Verifying write method with UTF8Encoding");
        if (!VerifyWrite(new System.Text.UTF8Encoding(), ENCODING_STRING_SIZE, GenRandomNewLine(false)))
        {
            Console.WriteLine("Err_003!!! Verifying write method with UTF8Encoding FAILED");
            return false;
        }

        return true;
    }


    public bool UTF32Encoding()
    {
        Console.WriteLine("Verifying write method with UTF32Encoding");
        if (!VerifyWrite(new System.Text.UTF32Encoding(), ENCODING_STRING_SIZE, GenRandomNewLine(false)))
        {
            Console.WriteLine("Err_004!!! Verifying write method with UTF32Encoding FAILED");
            return false;
        }

        return true;
    }


    public bool UnicodeEncoding()
    {
        Console.WriteLine("Verifying write method with UnicodeEncoding");
        if (!VerifyWrite(new System.Text.UnicodeEncoding(), ENCODING_STRING_SIZE, GenRandomNewLine(false)))
        {
            Console.WriteLine("Err_005!!! Verifying write method with UnicodeEncoding FAILED");
            return false;
        }

        return true;
    }


    public bool NullString()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        Console.WriteLine("Verifying Write with a null string");
        com.Open();

        try
        {
            com.WriteLine(null);
        }
        catch (System.ArgumentNullException)
        {
        }
        catch (System.Exception e)
        {
            Console.WriteLine("Write threw {0} expected System.ArgumentNullException", e.GetType());
            retValue = false;
        }

        if (!retValue)
            Console.WriteLine("Err_006!!! Verifying Write with a null string FAILED");

        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    public bool EmptyString()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        bool retValue = true;

        Console.WriteLine("Verifying Write with an empty string");

        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        retValue &= VerifyWriteLine(com1, com2, "");

        if (!retValue)
            Console.WriteLine("Err_007!!! Verifying Write with an empty string FAILED");

        return retValue;
    }


    public bool String_Null_Char()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        bool retValue = true;

        Console.WriteLine("Verifying Write with an string containing only the null character");

        com1.Open();
        com1.NewLine = GenRandomNewLine(true);

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        retValue &= VerifyWriteLine(com1, com2, "\0");

        if (!retValue)
            Console.WriteLine("Err_008!!! Verifying Write with an string containing only the null character FAILED");

        return retValue;
    }


    public bool LargeString()
    {
        Console.WriteLine("Verifying write method with a large string size");
        if (!VerifyWrite(new System.Text.UnicodeEncoding(), LARGE_STRING_SIZE, DEFAULT_NEW_LINE, 1))
        {
            Console.WriteLine("Err_009!!! Verifying write method with a large string size FAILED");
            return false;
        }

        return true;
    }


    public bool StrContains_NewLine_RND()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        Random rndGen = new Random(-55);
        System.Text.StringBuilder strBldrToWrite = TCSupport.GetRandomStringBuilder(NEWLINE_TESTING_STRING_SIZE, TCSupport.CharacterOptions.None);
        bool retValue = true;
        string newLine = GenRandomNewLine(true);

        Console.WriteLine("Verifying write method with a random NewLine string and writing a string that contains the NewLine");

        com1.NewLine = newLine;
        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        strBldrToWrite.Insert(rndGen.Next(0, NEWLINE_TESTING_STRING_SIZE), newLine);

        if (!VerifyWriteLine(com1, com2, strBldrToWrite.ToString()))
        {
            Console.Write("Err_010!!! Verifying write method with a random NewLine string and writing a string that contains the NewLine FAILED");
            retValue = false;
        }

        return retValue;
    }


    public bool StrContains_NewLine_CRLF()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        Random rndGen = new Random(-55);
        System.Text.StringBuilder strBldrToWrite = TCSupport.GetRandomStringBuilder(NEWLINE_TESTING_STRING_SIZE, TCSupport.CharacterOptions.None);
        bool retValue = true;
        string newLine = "\r\n";

        Console.WriteLine("Verifying write method with a NewLine=\\r\\n string and writing a string that contains the NewLine");

        com1.NewLine = newLine;
        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        strBldrToWrite.Insert(rndGen.Next(0, NEWLINE_TESTING_STRING_SIZE), newLine);

        if (!VerifyWriteLine(com1, com2, strBldrToWrite.ToString()))
        {
            Console.Write("Err_011!!! Verifying write method with a NewLine=\\r\\n string and writing a string that contains the NewLine FAILED");
            retValue = false;
        }

        return retValue;
    }


    public bool StrContains_NewLine_null()
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        Random rndGen = new Random(-55);
        System.Text.StringBuilder strBldrToWrite = TCSupport.GetRandomStringBuilder(NEWLINE_TESTING_STRING_SIZE, TCSupport.CharacterOptions.None);
        bool retValue = true;
        string newLine = "\0";

        Console.WriteLine("Verifying write method with a NewLine=\\0 string and writing a string that contains the NewLine");

        com1.NewLine = newLine;
        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        strBldrToWrite.Insert(rndGen.Next(0, NEWLINE_TESTING_STRING_SIZE), newLine);

        if (!VerifyWriteLine(com1, com2, strBldrToWrite.ToString()))
        {
            Console.Write("Err_011!!! Verifying write method with a NewLine=\\0 string and writing a string that contains the NewLine FAILED");
            retValue = false;
        }

        return retValue;
    }
    #endregion

    #region Verification for Test Cases

    public bool VerifyWrite(System.Text.Encoding encoding, int strSize, string newLine)
    {
        return VerifyWrite(encoding, strSize, newLine, DEFAULT_NUM_WRITES);
    }


    public bool VerifyWrite(System.Text.Encoding encoding, int strSize, string newLine, int numWrites)
    {
        SerialPort com1 = TCSupport.InitFirstSerialPort();
        SerialPort com2 = TCSupport.InitSecondSerialPort(com1);
        Random rndGen = new Random(-55);
        string stringToWrite = TCSupport.GetRandomString(NEWLINE_TESTING_STRING_SIZE, TCSupport.CharacterOptions.None);

        com1.Encoding = encoding;
        com1.Open();

        if (!com2.IsOpen) //This is necessary since com1 and com2 might be the same port if we are using a loopback
            com2.Open();

        return VerifyWriteLine(com1, com2, stringToWrite, numWrites);
    }


    public bool VerifyWriteLine(SerialPort com1, SerialPort com2, string stringToWrite)
    {
        return VerifyWriteLine(com1, com2, stringToWrite, DEFAULT_NUM_WRITES);
    }


    public bool VerifyWriteLine(SerialPort com1, SerialPort com2, string stringToWrite, int numWrites)
    {
        bool retValue = true;
        char[] expectedChars, actualChars;
        byte[] expectedBytes, actualBytes;
        int byteRead;
        int index = 0;
        int numNewLineBytes;
        char[] newLineChars = com1.NewLine.ToCharArray();
        System.Text.StringBuilder expectedStrBldr = new System.Text.StringBuilder();
        string expectedString, actualString;

        expectedBytes = com1.Encoding.GetBytes(stringToWrite.ToCharArray());
        expectedChars = com1.Encoding.GetChars(expectedBytes);

        numNewLineBytes = com1.Encoding.GetByteCount(com1.NewLine.ToCharArray());
        actualBytes = new byte[(expectedBytes.Length + numNewLineBytes) * numWrites];

        for (int i = 0; i < numWrites; i++)
        {
            com1.WriteLine(stringToWrite);
            expectedStrBldr.Append(expectedChars);
            expectedStrBldr.Append(com1.NewLine);
        }

        expectedString = expectedStrBldr.ToString();

        com2.ReadTimeout = 500;

        System.Threading.Thread.Sleep((int)(((expectedBytes.Length * 10.0) / com1.BaudRate) * 1000) + 250);

        while (true)
        {
            try
            {
                byteRead = com2.ReadByte();
            }
            catch (TimeoutException)
            {
                break;
            }

            if (actualBytes.Length <= index)
            {
                //If we have read in more bytes then we expect
                Console.WriteLine("ERROR!!!: We have received more bytes then were sent");
                retValue = false;
                break;
            }

            actualBytes[index] = (byte)byteRead;
            index++;

            if (actualBytes.Length - index != com2.BytesToRead)
            {
                System.Console.WriteLine("ERROR!!!: Expected BytesToRead={0} actual={1}", actualBytes.Length - index, com2.BytesToRead);
                retValue = false;
            }
        }

        actualChars = com1.Encoding.GetChars(actualBytes);
        actualString = new string(actualChars);

        if (!expectedString.Equals(actualString))
        {
            System.Console.WriteLine("ERROR!!!: Expected to read string: \"{0}\" actually read \"{1}\"", expectedString, actualString);
            retValue = false;
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }


    private string GenRandomNewLine(bool validAscii)
    {
        Random rndGen = new Random(-55);
        int newLineLength = rndGen.Next(MIN_NUM_NEWLINE_CHARS, MAX_NUM_NEWLINE_CHARS);

        if (validAscii)
            return new String(TCSupport.GetRandomChars(newLineLength, TCSupport.CharacterOptions.ASCII));
        else
            return new String(TCSupport.GetRandomChars(newLineLength, TCSupport.CharacterOptions.Surrogates));
    }
    #endregion
}
