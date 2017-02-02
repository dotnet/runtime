// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;

public class PortName_Property
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/21 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.PortName";
    public static readonly String s_strTFName = "PortName.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //Determines how long the randomly generated PortName is
    public static readonly int rndPortNameSize = 255;

    private enum ThrowAt { Set, Open };

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    private DosDevices _dosDevices;

    public PortName_Property()
    {
        if (IsWinXPOrHigher())
            _dosDevices = new DosDevices();
        else
            _dosDevices = null;
    }

    public static void Main(string[] args)
    {
        PortName_Property objTest = new PortName_Property();
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

        //Note Other test cases relevent to PortName are handled in Open_PortName.cs and ctor_str.cs
        retValue &= tcSupport.BeginTestcase(new TestDelegate(PortName_COM1_After_Open), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(PortName_COM2_After_Open), TCSupport.SerialPortRequirements.TwoSerialPorts);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(PortName_Empty), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(PortName_null), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(PortName_SlashSlash), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(PortName_SlashSlashSlash), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(PortName_SlashSlashRND), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(PortName_FileName), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(PortName_COM257), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(PortName_LPT), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(PortName_LPT1), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(PortName_PHYSICALDRIVE0), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(PortName_A), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(PortName_C), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(PortName_SystemDrive), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(PortName_RND), TCSupport.SerialPortRequirements.OneSerialPort);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool PortName_COM1_After_Open()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        Console.WriteLine("Verifying setting PortName=COM1 after open has been called");

        bool retValue = true;

        retValue &= VerifyExceptionAfterOpen(com, TCSupport.LocalMachineSerialInfo.FirstAvailablePortName, typeof(System.InvalidOperationException));
        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying setting PortName=COM1 after open has been called FAILED");
        }

        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    public bool PortName_COM2_After_Open()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);

        Console.WriteLine("Verifying setting PortName=COM2 after open has been called");

        bool retValue = true;

        retValue = VerifyExceptionAfterOpen(com, TCSupport.LocalMachineSerialInfo.SecondAvailablePortName, typeof(System.InvalidOperationException));
        if (!retValue)
        {
            Console.WriteLine("Err_002!!! Verifying setting PortName=COM2 after open has been called FAILED");
        }

        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    public bool PortName_Empty()
    {
        Console.WriteLine("Verifying setting PortName=\"\"");
        if (!VerifyException("", ThrowAt.Set, typeof(ArgumentException), typeof(System.ArgumentException)))
        {
            Console.WriteLine("Err_003!!! Verifying setting PortName=\"\" FAILED");
            return false;
        }

        return true;
    }


    public bool PortName_null()
    {
        Console.WriteLine("Verifying setting PortName=null");
        if (!VerifyException(null, ThrowAt.Set, typeof(System.ArgumentNullException), typeof(System.ArgumentNullException)))
        {
            Console.WriteLine("Err_004!!! Verifying setting PortName=null FAILED");
            return false;
        }

        return true;
    }


    private bool PortName_RND()
    {
        Random rndGen = new Random();
        System.Text.StringBuilder rndStrBuf = new System.Text.StringBuilder();

        for (int i = 0; i < rndPortNameSize; i++)
        {
            rndStrBuf.Append((char)rndGen.Next(0, System.UInt16.MaxValue));
        }

        Console.WriteLine("Verifying setting PortName to a random string");
        if (!VerifyException(rndStrBuf.ToString(), ThrowAt.Open, typeof(System.ArgumentException), typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_005!!! Verifying setting PortName to a random string FAILED");
            return false;
        }

        return true;
    }


    private bool PortName_SlashSlash()
    {
        Console.WriteLine("Verifying setting PortName=\\\\");
        if (!VerifyException("\\\\", ThrowAt.Set, typeof(ArgumentException), typeof(System.ArgumentException)))
        {
            Console.WriteLine("Err_006!!! Verifying setting PortName=\\\\ FAILED");
            return false;
        }

        return true;
    }


    private bool PortName_SlashSlashSlash()
    {
        Console.WriteLine("Verifying setting PortName=\\\\\\");
        if (!VerifyException("\\\\\\", ThrowAt.Set, typeof(ArgumentException), typeof(System.ArgumentException)))
        {
            Console.WriteLine("Err_007!!! Verifying setting PortName=\\\\\\ FAILED");
            return false;
        }

        return true;
    }


    private bool PortName_SlashSlashRND()
    {
        Random rndGen = new Random();
        System.Text.StringBuilder rndStrBuf = new System.Text.StringBuilder();

        rndStrBuf.Append("\\\\");
        for (int i = 0; i < rndPortNameSize; i++)
        {
            rndStrBuf.Append((char)rndGen.Next(0, System.UInt16.MaxValue));
        }

        Console.WriteLine("Verifying setting PortName=\\\\ + RND_STR");
        if (!VerifyException(rndStrBuf.ToString(), ThrowAt.Set, typeof(ArgumentException), typeof(System.ArgumentException)))
        {
            Console.WriteLine("Err_008!!! Verifying setting PortName=\\\\ + RND_STR FAILED");
            return false;
        }

        return true;
    }


    private bool PortName_FileName()
    {
        string fileName = "PortNameEqualToFileName.txt";
        System.IO.FileStream testFile = System.IO.File.Open(fileName, System.IO.FileMode.Create);
        System.Text.ASCIIEncoding asciiEncd = new System.Text.ASCIIEncoding();
        string testStr = "Hello World";

        testFile.Write(asciiEncd.GetBytes(testStr), 0, asciiEncd.GetByteCount(testStr));

        testFile.Close();
        Console.WriteLine("Verifying setting PortName={0}", fileName);

        if (!VerifyException(fileName, ThrowAt.Open, typeof(System.ArgumentException), typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_004!!! Verifying setting PortName to a file name FAILED");
            return false;
        }

        Console.WriteLine("Verifying setting PortName={0}", Environment.CurrentDirectory + fileName);

        if (!VerifyException(Environment.CurrentDirectory + fileName, ThrowAt.Open, typeof(System.ArgumentException), typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_009!!! Verifying setting PortName to a file name FAILED");
            return false;
        }

        System.IO.File.Delete(fileName);
        return true;
    }


    private bool PortName_COM257()
    {
        Console.WriteLine("Verifying setting PortName=COM257");
        if (!VerifyException("COM257", ThrowAt.Open, typeof(System.IO.IOException), typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_010!!! Verifying setting PortName=COM257 FAILED");
            return false;
        }

        return true;
    }


    private bool PortName_LPT()
    {
        Type expectedException;

        if (IsWinXPOrHigher())
        {
            expectedException = _dosDevices.CommonNameExists("LPT") ? typeof(ArgumentException) : typeof(System.ArgumentException);
        }
        else
        {
            //			This is not Reliable on Win2K so for now we will assume that the machine does NOT have this device
            //			expectedException = dosDevices.InternalNameExists(@"\DosDevices\LPT") ? typeof(ArgumentException) :  typeof(System.ArgumentException);

            expectedException = typeof(System.ArgumentException);
        }

        Console.WriteLine("Verifying setting PortName=LPT");
        if (!VerifyException("LPT", ThrowAt.Open, expectedException, typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_0982sakhoe!!! Verifying setting PortName=LPT FAILED");
            return false;
        }

        return true;
    }


    private bool PortName_LPT1()
    {
        Type expectedException;

        if (IsWinXPOrHigher())
        {
            expectedException = _dosDevices.CommonNameExists("LPT1") ? typeof(ArgumentException) : typeof(System.ArgumentException);
        }
        else
        {
            //			This is not Reliable on Win2K so for now we will assume that the machine DOES have this device
            //			expectedException = dosDevices.InternalNameExists(@"\DosDevices\LPT1") ? typeof(ArgumentException) :  typeof(System.ArgumentException);

            expectedException = typeof(ArgumentException);
        }

        Console.WriteLine("Verifying setting PortName=LPT1");
        if (!VerifyException("LPT1", ThrowAt.Open, expectedException, typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_2072nsoah!!! Verifying setting PortName=LPT1 FAILED");
            return false;
        }

        return true;
    }


    private bool PortName_PHYSICALDRIVE0()
    {
        Type expectedException;

        if (IsWinXPOrHigher())
        {
            expectedException = _dosDevices.CommonNameExists("PHYSICALDRIVE0") ? typeof(ArgumentException) : typeof(System.ArgumentException);
        }
        else
        {
            //			This is not Reliable on Win2K so for now we will assume that the machine DOES have this device
            expectedException = typeof(ArgumentException);
        }


        Console.WriteLine("Verifying setting PortName=PHYSICALDRIVE0");
        if (!VerifyException("PHYSICALDRIVE0", ThrowAt.Open, expectedException, typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_08723shz!!! Verifying setting PortName=PHYSICALDRIVE0 FAILED");
            return false;
        }

        return true;
    }


    private bool PortName_A()
    {
        Type expectedException;

        if (IsWinXPOrHigher())
        {
            expectedException = _dosDevices.CommonNameExists("A:") ? typeof(ArgumentException) : typeof(System.ArgumentException);
        }
        else
        {
            //			This is not Reliable on Win2K so for now we will assume that the machine DOES have this device			
            //			expectedException = dosDevices.InternalNameContains(@"\Device\Floppy") ? typeof(ArgumentException) :  typeof(System.ArgumentException);
            expectedException = typeof(ArgumentException);
        }

        Console.WriteLine("Verifying setting PortName=A:");
        if (!VerifyException("A:", ThrowAt.Open, expectedException, typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_0972ahios!!! Verifying setting PortName=A: FAILED");
            return false;
        }

        return true;
    }


    private bool PortName_C()
    {
        Console.WriteLine("Verifying setting PortName=C:");

        if (!VerifyException("C:", ThrowAt.Open, new Type[] { typeof(System.ArgumentException), typeof(ArgumentException) }, typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_0702sklpa!!! Verifying setting PortName=C: FAILED");
            return false;
        }

        return true;
    }

    private bool PortName_SystemDrive()
    {
        Console.WriteLine("Verifying setting PortName=%SYSTEMDRIVE%");
        String portName = Environment.GetEnvironmentVariable("SystemDrive");

        if (!String.IsNullOrEmpty(portName) &&
            !VerifyException(portName, ThrowAt.Open, new Type[] { typeof(System.ArgumentException) }, typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_01548!!! Verifying setting PortName=C: FAILED");
            return false;
        }

        return true;
    }
    #endregion

    #region Verification for Test Cases

    private bool IsWinXPOrHigher()
    {
        OperatingSystem currentVersion = Environment.OSVersion;

        if (currentVersion.Platform == PlatformID.Win32NT)
        {
            if (5 < currentVersion.Version.Major)
                return true;
            if (5 == currentVersion.Version.Major && 1 <= currentVersion.Version.Minor)
                return true;
        }

        return false;
    }

    private bool VerifyException(string portName, ThrowAt throwAt, System.Type expectedExceptionAtOpen, System.Type expectedExceptionAfterOpen)
    {
        return VerifyException(portName, throwAt, new Type[] { expectedExceptionAtOpen }, expectedExceptionAfterOpen);
    }

    private bool VerifyException(string portName, ThrowAt throwAt, System.Type[] expectedExceptionAtOpen, System.Type expectedExceptionAfterOpen)
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        bool retValue = true;

        retValue &= VerifyExceptionAtOpen(com, portName, throwAt, expectedExceptionAtOpen);

        if (com.IsOpen)
            com.Close();

        retValue &= VerifyExceptionAfterOpen(com, portName, expectedExceptionAfterOpen);

        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    private bool VerifyExceptionAtOpen(SerialPort com, string portName, ThrowAt throwAt, System.Type expectedException)
    {
        return VerifyExceptionAtOpen(com, portName, throwAt, new Type[] { expectedException });
    }

    private bool VerifyExceptionAtOpen(SerialPort com, string portName, ThrowAt throwAt, System.Type[] expectedExceptions)
    {
        string origPortName = com.PortName;
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        if (null != expectedExceptions && 0 < expectedExceptions.Length)
        {
            serPortProp.SetAllPropertiesToDefaults();
        }
        else
        {
            serPortProp.SetAllPropertiesToOpenDefaults();
        }

        if (ThrowAt.Open == throwAt)
            serPortProp.SetProperty("PortName", portName);
        else
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        try
        {
            com.PortName = portName;

            if (ThrowAt.Open == throwAt)
                com.Open();

            if (null != expectedExceptions && 0 < expectedExceptions.Length)
            {
                Console.Write("ERROR!!! Expected Open() to throw ");
                for (int i = 0; i < expectedExceptions.Length; ++i) Console.Write(expectedExceptions[i] + " ");
                Console.WriteLine(" and nothing was thrown");
                retValue = false;
            }
        }
        catch (System.Exception e)
        {
            if (null == expectedExceptions || 0 == expectedExceptions.Length)
            {
                Console.WriteLine("ERROR!!! Expected Open() NOT to throw an exception and the following was thrown:\n{0}", e);
                retValue = false;
            }
            else
            {
                bool exceptionFound = false;
                Type actualExceptionType = e.GetType();

                for (int i = 0; i < expectedExceptions.Length; ++i)
                {
                    if (actualExceptionType == expectedExceptions[i])
                    {
                        exceptionFound = true;
                        break;
                    }
                }

                if (exceptionFound)
                {
                    Console.WriteLine("Caught expected exception:\n{0}", e.GetType());
                }
                else
                {
                    Console.Write("ERROR!!! Expected Open() throw ");
                    for (int i = 0; i < expectedExceptions.Length; ++i) Console.Write(expectedExceptions[i] + " ");
                    Console.WriteLine(" and  the following was thrown:\n{0}", e);
                    retValue = false;
                }
            }
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com);
        com.PortName = origPortName;

        return retValue;
    }


    private bool VerifyExceptionAfterOpen(SerialPort com, string portName, System.Type expectedException)
    {
        bool retValue = true;
        SerialPortProperties serPortProp = new SerialPortProperties();

        com.Open();
        serPortProp.SetAllPropertiesToOpenDefaults();
        serPortProp.SetProperty("PortName", com.PortName);

        try
        {
            com.PortName = portName;
            if (null != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the PortName after Open() to throw {0} and nothing was thrown", expectedException);
                retValue = false;
            }
        }
        catch (System.Exception e)
        {
            if (null == expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the PortName after Open() NOT to throw an exception and {0} was thrown", e.GetType());
                retValue = false;
            }
            else if (e.GetType() != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected setting the PortName after Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                retValue = false;
            }
        }

        retValue &= serPortProp.VerifyPropertiesAndPrint(com);

        return retValue;
    }
    #endregion
}
