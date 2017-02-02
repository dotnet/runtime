// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;
public delegate bool TestDelegate();

public class SerialPortProperties
{
    #region Default Property Values

    //All of the following properties are the defualts of SerialPort when the
    //just the default constructor has been called. The names of the data members here must
    //begin with default followed by the EXACT(case sensitive) name of
    //the property in SerialPort class. If a default for a property is not set
    //here then the property can never be set in this class and will never be 
    //used for verification.
    public static readonly int defaultBaudRate = 9600;
    public static readonly string defaultPortName = "COM1";
    public static readonly int defaultDataBits = 8;
    public static readonly System.IO.Ports.StopBits defaultStopBits = System.IO.Ports.StopBits.One;
    public static readonly System.IO.Ports.Parity defaultParity = System.IO.Ports.Parity.None;
    public static readonly System.IO.Ports.Handshake defaultHandshake = System.IO.Ports.Handshake.None;
    public static readonly System.Text.Encoding defaultEncoding = new System.Text.ASCIIEncoding();
    public static readonly bool defaultDtrEnable = false;
    public static readonly bool defaultRtsEnable = false;
    public static readonly int defaultReadTimeout = -1;
    public static readonly int defaultWriteTimeout = -1;
    public static readonly System.Type defaultBytesToRead = typeof(System.InvalidOperationException);
    public static readonly System.Type defaultBytesToWrite = typeof(System.InvalidOperationException);
    public static readonly bool defaultIsOpen = false;
    public static readonly System.Type defaultBaseStream = typeof(InvalidOperationException);
    public static readonly int defaultReceivedBytesThreshold = 1;
    public static readonly bool defaultDiscardNull = false;
    public static readonly byte defaultParityReplace = (byte)'?';
    public static readonly System.Type defaultCDHolding = typeof(System.InvalidOperationException);
    public static readonly System.Type defaultCtsHolding = typeof(System.InvalidOperationException);
    public static readonly System.Type defaultDsrHolding = typeof(System.InvalidOperationException);
    public static readonly string defaultNewLine = "\n";
    public static readonly System.Type defaultBreakState = typeof(System.InvalidOperationException);
    public static readonly int defaultReadBufferSize = 4 * 1024;
    public static readonly int defaultWriteBufferSize = 2 * 1024;

    //All of the following properties are the defualts of SerialPort when the
    //serial port connection is open. The names of the data members here must
    //begin with openDefault followed by the EXACT(case sensitive) name of
    //the property in SerialPort class. 
    public static readonly int openDefaultBaudRate = 9600;
    public static readonly string openDefaultPortName = "COM1";
    public static readonly int openDefaultDataBits = 8;
    public static readonly System.IO.Ports.StopBits openDefaultStopBits = System.IO.Ports.StopBits.One;
    public static readonly System.IO.Ports.Parity openDefaultParity = System.IO.Ports.Parity.None;
    public static readonly System.IO.Ports.Handshake openDefaultHandshake = System.IO.Ports.Handshake.None;
    public static readonly System.Text.Encoding openDefaultEncoding = new System.Text.ASCIIEncoding();
    public static readonly bool openDefaultDtrEnable = false;
    public static readonly bool openDefaultRtsEnable = false;
    public static readonly int openDefaultReadTimeout = -1;
    public static readonly int openDefaultWriteTimeout = -1;
    public static readonly int openDefaultBytesToRead = 0;
    public static readonly int openDefaultBytesToWrite = 0;
    public static readonly bool openDefaultIsOpen = true;
    public static readonly int openDefaultReceivedBytesThreshold = 1;
    public static readonly bool openDefaultDiscardNull = false;
    public static readonly byte openDefaultParityReplace = (byte)'?';
    //Removing the following properties from default checks.  Sometimes these can be true (as read from the GetCommModemStatus win32 API)
    //which causes test failures.  Since these are read-only properties and are simply obtained from a bitfield, there is a very low probability
    //of regression involving these properties.
    //public static readonly bool openDefaultCDHolding = false;
    //public static readonly bool openDefaultCtsHolding = false;
    //public static readonly bool openDefaultDsrHolding = false;
    public static readonly string openDefaultNewLine = "\n";
    public static readonly bool openDefaultBreakState = false;
    public static readonly int openReadBufferSize = 4 * 1024;
    public static readonly int openWriteBufferSize = 2 * 1024;
    #endregion

    #region Private Data Members
    private Hashtable _properties;
    private Hashtable _openDefaultProperties;
    private Hashtable _defaultProperties;
    #endregion

    #region Constructors
    public SerialPortProperties()
    {
        _properties = new Hashtable();
        _openDefaultProperties = new Hashtable();
        _defaultProperties = new Hashtable();
        LoadDefaults();
    }
    #endregion

    #region Public Methods
    public object GetDefualtOpenProperty(string name)
    {
        return _openDefaultProperties[name];
    }


    public object GetDefualtProperty(string name)
    {
        return _defaultProperties[name];
    }


    public object GetProperty(string name)
    {
        return _properties[name];
    }


    public void SetAllPropertiesToOpenDefaults()
    {
        SetAllPropertiesToDefaults(_openDefaultProperties);
    }


    public void SetAllPropertiesToDefaults()
    {
        SetAllPropertiesToDefaults(_defaultProperties);
    }


    public object SetopenDefaultProperty(string name)
    {
        return SetDefaultProperty(name, _openDefaultProperties);
    }


    public object SetdefaultProperty(string name)
    {
        return SetDefaultProperty(name, _defaultProperties);
    }


    public object SetProperty(string name, object value)
    {
        object retVal = null;

        if (null != _openDefaultProperties[name])
        {
            retVal = _properties[name];
            _properties[name] = value;
        }

        return retVal;
    }


    public override string ToString()
    {
        StringBuilder strBuf = new System.Text.StringBuilder();

        foreach (object key in _properties.Keys)
        {
            strBuf.Append(key + "=" + _properties[key] + "\n");
        }//End For each field

        return strBuf.ToString();
    }


    public Hashtable VerifyProperties(SerialPort port)
    {
        Type serialPortType = typeof(SerialPort);
        Hashtable badProps = new Hashtable();

        //If we could not get the type of SerialPort then verification can not 
        //occur and we should throw an exception
        if (null == serialPortType)
        {
            throw new System.Exception("Could not create a Type object of SerialPort");
        }

        //For each key in the properties Hashtable verify that the property in the 
        //SerialPort Object port with the same name as the key has the same value as
        //the value corresponding to the key in the HashTable. If the port is in a 
        //state where accessing a property is suppose to throw an exception this can be
        //verified by setting the value in the hashtable to the System.Type of the 
        //expected exception		
        foreach (object key in _properties.Keys)
        {
            object value = _properties[key];

            try
            {
                PropertyInfo serialPortProperty = serialPortType.GetProperty((string)key);
                object serialPortValue;

                if ((string)key == "RtsEnable" &&
                    ((port.Handshake == Handshake.RequestToSend &&
                        (System.IO.Ports.Handshake)_properties["Handshake"] == Handshake.RequestToSend) ||
                    (port.Handshake == Handshake.RequestToSendXOnXOff &&
                        (System.IO.Ports.Handshake)_properties["Handshake"] == Handshake.RequestToSendXOnXOff)))
                {
                    continue;
                }

                serialPortValue = serialPortProperty.GetValue(port, null);

                if (value == null)
                {
                    if (value != serialPortValue)
                    {
                        badProps[key] = serialPortValue;
                    }
                }
                else if (!value.Equals(serialPortValue))
                {
                    badProps[key] = serialPortValue;
                }
            }
            catch (Exception e)
            {
                //An exception was thrown while retrieving the value of the propert in 
                //the SerialPort object. However this may be the exepected operation of 
                //SerialPort so the type of the exception must be verified. Reflection
                //throws it's own exception ontop of SerialPorts so we must use the
                //InnerException of the own that Reflection throws
                if (null != e.InnerException)
                {
                    if (null == value || !value.Equals(e.InnerException.GetType()))
                    {
                        badProps[key] = e.GetType();
                    }
                }
                else
                {
                    badProps[key] = e.GetType();
                }
            }
        }//End For each field

        if (0 == badProps.Count)
        {
            return null;
        }
        else
        {
            return badProps;
        }
    }


    public bool VerifyPropertiesAndPrint(SerialPort port)
    {
        Hashtable badProps;

        if (null == (badProps = VerifyProperties(port)))
        {
            //Console.WriteLine("SerialPort values correctly set");
            return true;
        }
        else
        {
            Console.WriteLine("ERROR!!! SerialPort properties not correctly set");
            foreach (object key in badProps.Keys)
            {
                Console.WriteLine("{0}={1} expected {2}", key, badProps[key], GetProperty((string)key));
            }
        }

        return false;
    }

    #endregion

    #region Private Methods
    private void LoadDefaults()
    {
        LoadFields("openDefault", _openDefaultProperties);
        LoadFields("default", _defaultProperties);
    }


    private void LoadFields(string stratsWith, Hashtable fields)
    {
        Type serialPropertiesType = Type.GetType("SerialPortProperties", true);

        //For each field in this class that starts with the string startsWith create
        //a key value pair in the hashtable fields with key being the rest of the 
        //field name after startsWith and the value is whatever the value is of the field
        //in this class
        foreach (System.Reflection.FieldInfo defaultField in serialPropertiesType.GetFields())
        {
            String defaultFieldName = defaultField.Name;

            if (0 == defaultFieldName.IndexOf(stratsWith))
            {
                String fieldName = defaultFieldName.Replace(stratsWith, "");
                object defaultValue = defaultField.GetValue(this);

                fields[fieldName] = defaultValue;
            }//End If the current field is one of the default fields
        }//End For each field
    }


    private void SetAllPropertiesToDefaults(Hashtable defaultProperties)
    {
        _properties.Clear();

        //For each key in the defaultProperties Hashtable set poperties[key]
        //with the corresponding value in defaultProperties
        foreach (object key in defaultProperties.Keys)
        {
            _properties[key] = defaultProperties[key];
        }
    }


    private object SetDefaultProperty(string name, Hashtable defaultProperties)
    {
        object retVal = null;

        //Only Set the default value if it exists in the defaultProperties Hashtable 
        //This will prevent the abilility to create arbitrary keys(Property names)
        if (null != defaultProperties[name])
        {
            retVal = _properties[name];
            _properties[name] = defaultProperties[name];
        }

        return retVal;
    }
    #endregion
}

[Serializable()]
public class LocalMachineSerialInfo
{
    private string _firstAvailablePortName;
    private string _secondAvailablePortName;
    private bool _nullModemPresent;
    private string _loopbackPortName;


    public LocalMachineSerialInfo(string firstAvailablePortName, string secondAvailablePortName, string loopBackPortName, bool nullModemPresent)
    {
        _firstAvailablePortName = firstAvailablePortName;
        _secondAvailablePortName = secondAvailablePortName;
        _loopbackPortName = loopBackPortName;
        _nullModemPresent = nullModemPresent;
        Console.WriteLine("First available port name  : " + firstAvailablePortName);
        Console.WriteLine("Second available port name : " + secondAvailablePortName);
        Console.WriteLine("Loopback port name         : " + loopBackPortName);
        Console.WriteLine("NUllModem present          : " + nullModemPresent);
    }


    public string FirstAvailablePortName
    {
        get
        {
            return _firstAvailablePortName;
        }
    }


    public string SecondAvailablePortName
    {
        get
        {
            return _secondAvailablePortName;
        }
    }


    public bool NullModemPresent
    {
        get
        {
            return _nullModemPresent;
        }
    }

    public string LoopbackPortName
    {
        get
        {
            return _loopbackPortName;
        }
    }
}



public class SerialPortConnection
{
    public static bool VerifyConnection(String portName1, String portName2)
    {
        SerialPort com1 = new SerialPort(portName1);
        SerialPort com2 = new SerialPort(portName2);
        bool retValue = true;

        try
        {
            com1.Open();
            com2.Open();
            retValue = VerifyReadWrite(com1, com2);
        }
        catch (Exception)
        {
            //One of the com ports does not exist on the machine that this is being run on
            //thus their can not be a connection between com1 and com2
            retValue = false;
        }
        finally
        {
            com1.Close();
            com2.Close();
        }

        return retValue;
    }

    public static bool VerifyLoopback(String portName)
    {
        bool retValue = true;

        SerialPort com = new SerialPort(portName);

        try
        {
            com.Open();
            retValue = VerifyReadWrite(com, com);
        }
        catch (Exception)
        {
            //The com ports does not exist on the machine that this is being run on
            //thus their can not be a loopback between the ports
            retValue = false;
        }
        finally
        {
            com.Close();
        }

        return retValue;
    }

    private static bool VerifyReadWrite(SerialPort com1, SerialPort com2)
    {
        try
        {
            com1.ReadTimeout = 1000;
            com2.ReadTimeout = 1000;

            com1.WriteLine("Ping");

            if ("Ping" != com2.ReadLine())
            {
                return false;
            }

            com2.WriteLine("Response");

            if ("Response" != com1.ReadLine())
            {
                return false;
            }

            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }
}



public class TCSupport
{
    public enum SerialPortRequirements { None, OneSerialPort, TwoSerialPorts, NullModem, Loopback, LoopbackOrNullModem };
    public enum OperatingSystemRequirements { None, NotWin9X }; //Could expand this later but for now this is all we need

    static public readonly int PassExitCode = 100;
    static public readonly int FailExitCode = 1;
    static public readonly int NoNullCableExitCode = 99;
    static public readonly bool SerialPortRequirements_CausesError = false;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = PassExitCode;

    private static LocalMachineSerialInfo s_localMachineSerialInfo;
    private static SerialPortRequirements s_localMachineSerialPortRequirements;

    public TCSupport()
    {
        InitializeSerialInfo();
    }

    private void InitializeSerialInfo()
    {
        GenerateSerialInfo();

        if (s_localMachineSerialInfo.LoopbackPortName != null)
            s_localMachineSerialPortRequirements = SerialPortRequirements.Loopback;
        else if (s_localMachineSerialInfo.NullModemPresent)
            s_localMachineSerialPortRequirements = SerialPortRequirements.NullModem;
        else if (s_localMachineSerialInfo.SecondAvailablePortName != null && s_localMachineSerialInfo.SecondAvailablePortName != String.Empty)
            s_localMachineSerialPortRequirements = SerialPortRequirements.TwoSerialPorts;
        else if (s_localMachineSerialInfo.FirstAvailablePortName != null && s_localMachineSerialInfo.FirstAvailablePortName != String.Empty)
            s_localMachineSerialPortRequirements = SerialPortRequirements.OneSerialPort;
        else
            s_localMachineSerialPortRequirements = SerialPortRequirements.None;
    }

    private void GenerateSerialInfo()
    {
        string[] availablePortNames = PortHelper.GetPorts();
        Console.WriteLine("total ports : " + availablePortNames.Length);
        bool nullModemPresent = false;

        string portName1 = null, portName2 = null, loopbackPortName = null;

        for (int i = 0; i < availablePortNames.Length; ++i)
        {
            SerialPort com = new SerialPort(availablePortNames[i]);

            try
            {
                com.Open();
                com.Close();

                if (null == portName1)
                {
                    portName1 = availablePortNames[i];
                }
                else if (null == portName2)
                {
                    portName2 = availablePortNames[i];
                    break;
                }
            }
            catch (Exception) { }
        }

        if (null != portName1 && SerialPortConnection.VerifyLoopback(portName1))
        {
            loopbackPortName = portName1;
        }

        if (null != portName2)
        {
            if (null == loopbackPortName && SerialPortConnection.VerifyLoopback(portName2))
            {
                loopbackPortName = portName2;
            }

            nullModemPresent = SerialPortConnection.VerifyConnection(portName1, portName2);
        }

        s_localMachineSerialInfo = new LocalMachineSerialInfo(portName1, portName2, loopbackPortName, nullModemPresent);
    }

    private bool BeginTestcase(TestDelegate test)
    {
        bool retValue;

        if (test())
        {
            _numTestcases++;
            retValue = true;
        }
        else
        {
            _numErrors++;
            retValue = false;
            _exitValue = FailExitCode;
        }

        Console.WriteLine("");
        return retValue;
    }


    public bool BeginTestcase(TestDelegate test, SerialPortRequirements serialPortRequirements)
    {
        return BeginTestcase(test, serialPortRequirements, OperatingSystemRequirements.None);
    }

    public bool BeginTestcase(TestDelegate test, SerialPortRequirements serialPortRequirements,
        OperatingSystemRequirements operatingSystemRequirements)
    {
        bool retValue;
        bool suffecientRequirements = false;

        switch (serialPortRequirements)
        {
            case SerialPortRequirements.None:
                suffecientRequirements = true;
                break;

            case SerialPortRequirements.OneSerialPort:
                if (s_localMachineSerialPortRequirements == SerialPortRequirements.OneSerialPort ||
                    s_localMachineSerialPortRequirements == SerialPortRequirements.TwoSerialPorts ||
                    s_localMachineSerialPortRequirements == SerialPortRequirements.Loopback ||
                    s_localMachineSerialPortRequirements == SerialPortRequirements.NullModem)
                {
                    suffecientRequirements = true;
                }

                break;
            case SerialPortRequirements.TwoSerialPorts:
                if (s_localMachineSerialPortRequirements == SerialPortRequirements.TwoSerialPorts ||
                    s_localMachineSerialPortRequirements == SerialPortRequirements.Loopback ||
                    s_localMachineSerialPortRequirements == SerialPortRequirements.NullModem)
                {
                    suffecientRequirements = true;
                }

                break;
            case SerialPortRequirements.NullModem:
                if (s_localMachineSerialPortRequirements == SerialPortRequirements.NullModem)
                {
                    suffecientRequirements = true;
                }

                break;
            case SerialPortRequirements.Loopback:
                if (s_localMachineSerialPortRequirements == SerialPortRequirements.Loopback)
                {
                    suffecientRequirements = true;
                }

                break;
            case SerialPortRequirements.LoopbackOrNullModem:
                if (s_localMachineSerialPortRequirements == SerialPortRequirements.Loopback ||
                    s_localMachineSerialPortRequirements == SerialPortRequirements.NullModem)
                {
                    suffecientRequirements = true;
                }

                break;
        }

        switch (operatingSystemRequirements)
        {
            case OperatingSystemRequirements.NotWin9X:
                if (IsWin9X())
                {
                    suffecientRequirements = false;
                    /*This MAY be a bad idea if SerialPortRequirements_CausesError is true
                    since if a test scenario is run under win9x the test case will fail.	*/
                }
                break;
        }


        if (!suffecientRequirements)
        {
            if (SerialPortRequirements_CausesError)
            {
                Console.WriteLine("Err_999!!!: Test {0} requires {1} and this machine only has {2}",
                    test.Method.Name, serialPortRequirements, s_localMachineSerialPortRequirements);
                retValue = false;
                _numErrors++;

                //Only set the exitValue to NoNullCableExitCode if it has not been set already. 
                //So a test case that fails only from not having a null modem cable will have this exit code
                if (PassExitCode == _exitValue)
                {
                    _exitValue = NoNullCableExitCode;
                }
            }
            else
            {
                Console.WriteLine("Test {0} requires {1} and this machine only has {2} so this scenario will not be run.",
                    test.Method.Name, serialPortRequirements, s_localMachineSerialPortRequirements);
                retValue = true;
            }
        }
        else
        {
            retValue = BeginTestcase(test);
        }

        return retValue;
    }

    public static bool SufficientHardwareRequirements(SerialPortRequirements serialPortRequirements)
    {
        switch (serialPortRequirements)
        {
            case SerialPortRequirements.None:
                return true;

            case SerialPortRequirements.OneSerialPort:
                return s_localMachineSerialPortRequirements == SerialPortRequirements.OneSerialPort ||
                    s_localMachineSerialPortRequirements == SerialPortRequirements.TwoSerialPorts ||
                    s_localMachineSerialPortRequirements == SerialPortRequirements.Loopback ||
                    s_localMachineSerialPortRequirements == SerialPortRequirements.NullModem;
            case SerialPortRequirements.TwoSerialPorts:
                return s_localMachineSerialPortRequirements == SerialPortRequirements.TwoSerialPorts ||
                    s_localMachineSerialPortRequirements == SerialPortRequirements.Loopback ||
                    s_localMachineSerialPortRequirements == SerialPortRequirements.NullModem;
            case SerialPortRequirements.NullModem:
                return s_localMachineSerialPortRequirements == SerialPortRequirements.NullModem;
            case SerialPortRequirements.Loopback:
                return s_localMachineSerialPortRequirements == SerialPortRequirements.Loopback;
            case SerialPortRequirements.LoopbackOrNullModem:
                return s_localMachineSerialPortRequirements == SerialPortRequirements.Loopback ||
                    s_localMachineSerialPortRequirements == SerialPortRequirements.NullModem;
        }

        return false;
    }

    public static SerialPort InitFirstSerialPort()
    {
        if (LocalMachineSerialInfo.NullModemPresent)
        {
            return new SerialPort(LocalMachineSerialInfo.FirstAvailablePortName);
        }
        else if (null != LocalMachineSerialInfo.LoopbackPortName)
        {
            return new SerialPort(LocalMachineSerialInfo.LoopbackPortName);
        }
        else if (null != LocalMachineSerialInfo.FirstAvailablePortName)
        {
            return new SerialPort(LocalMachineSerialInfo.FirstAvailablePortName);
        }

        return null;
    }

    public static SerialPort InitSecondSerialPort(SerialPort com)
    {
        if (LocalMachineSerialInfo.NullModemPresent)
        {
            return new SerialPort(LocalMachineSerialInfo.SecondAvailablePortName);
        }
        else if (null != LocalMachineSerialInfo.LoopbackPortName)
        {
            return com;
        }
        else if (null != LocalMachineSerialInfo.SecondAvailablePortName)
        {
            return new SerialPort(LocalMachineSerialInfo.SecondAvailablePortName);
        }

        return null;
    }

    public int NumErrors
    {
        get
        {
            return _numErrors;
        }
    }


    public int NumTestcases
    {
        get
        {
            return _numTestcases;
        }
    }


    public int ExitValue
    {
        get
        {
            return _exitValue;
        }
    }


    public static LocalMachineSerialInfo LocalMachineSerialInfo
    {
        get
        {
            return s_localMachineSerialInfo;
        }
    }


    public static SerialPortRequirements LocalMachineSerialPortRequirements
    {
        get
        {
            return s_localMachineSerialPortRequirements;
        }
    }

    public static bool IsWin9X()
    {
        return Environment.OSVersion.Platform == PlatformID.Win32Windows;
    }

    public delegate bool Predicate();

    public delegate T ValueGenerator<T>();

    public static bool WaitForPredicate(Predicate predicate, int maxWait, string errorMessageFormat, params object[] formatArgs)
    {
        return WaitForPredicate(predicate, maxWait, String.Format(errorMessageFormat, formatArgs));
    }
    public static bool WaitForPredicate(Predicate predicate, int maxWait, string errorMessage)
    {
        System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
        bool predicateValue = false;

        stopWatch.Start();

        while (!predicateValue && stopWatch.ElapsedMilliseconds < maxWait)
        {
            predicateValue = predicate();
            System.Threading.Thread.Sleep(10);
        }

        if (!predicateValue)
            Console.WriteLine(errorMessage);

        return predicateValue;
    }

    public static bool WaitForExpected<T>(ValueGenerator<T> actualValueGenerator, T expectedValue, int maxWait, string errorMessage)
    {
        System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
        bool result = false;
        T actualValue;
        int iterationWaitTime = 0;

        stopWatch.Start();

        do
        {
            actualValue = actualValueGenerator();
            result = actualValue == null ? null == expectedValue : actualValue.Equals(expectedValue);

            System.Threading.Thread.Sleep(iterationWaitTime);
            iterationWaitTime = 10;//This is just to ensure there is no delay the first time we check
        } while (!result && stopWatch.ElapsedMilliseconds < maxWait);

        if (!result)
        {
            Console.WriteLine(errorMessage +
                " Expected:" + (null == expectedValue ? "<null>" : expectedValue.ToString()) +
                " Actual:" + (null == actualValue ? "<null>" : actualValue.ToString()));
        }

        return result;
    }

    private const int MIN_RANDOM_CHAR = 0;

    public const int MIN_HIGH_SURROGATE = 0xD800;
    public const int MAX_HIGH_SURROGATE = 0xDBFF;

    public const int MIN_LOW_SURROGATE = 0xDC00;
    public const int MAX_LOW_SURROGATE = 0xDFFF;

    public const int MIN_RANDOM_ASCII_CHAR = 0;
    public const int MAX_RANDOM_ASCII_CHAR = 127;


    private static Random s_random = new Random(-55);

    public enum CharacterOptions { None, Surrogates, ASCII };

    public static char[] GetRandomChars(int count, bool withSurrogates)
    {
        if (withSurrogates)
            return GetRandomCharsWithSurrogates(count);
        else
            return GetRandomCharsWithoutSurrogates(count);
    }

    public static char[] GetRandomChars(int count, CharacterOptions options)
    {
        if (0 != (options & CharacterOptions.Surrogates))
            return GetRandomCharsWithSurrogates(count);
        if (0 != (options & CharacterOptions.ASCII))
            return GetRandomASCIIChars(count);
        else
            return GetRandomCharsWithoutSurrogates(count);
    }

    public static void GetRandomChars(char[] chars, int index, int count, CharacterOptions options)
    {
        if (0 != (options & CharacterOptions.Surrogates))
            GetRandomCharsWithSurrogates(chars, index, count);
        if (0 != (options & CharacterOptions.ASCII))
            GetRandomASCIIChars(chars, index, count);
        else
            GetRandomCharsWithoutSurrogates(chars, index, count);
    }

    public static String GetRandomString(int count, CharacterOptions options)
    {
        return new String(GetRandomChars(count, options));
    }

    public static System.Text.StringBuilder GetRandomStringBuilder(int count, CharacterOptions options)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(count);
        sb.Append(GetRandomChars(count, options));

        return sb;
    }

    public static char[] GetRandomASCIIChars(int count)
    {
        char[] chars = new char[count];

        GetRandomASCIIChars(chars, 0, count);

        return chars;
    }

    public static void GetRandomASCIIChars(char[] chars, int index, int count)
    {
        for (int i = 0; i < count; ++i)
        {
            chars[i] = GenerateRandomASCII();
        }
    }


    public static char[] GetRandomCharsWithoutSurrogates(int count)
    {
        char[] chars = new char[count];

        GetRandomCharsWithoutSurrogates(chars, 0, count);

        return chars;
    }

    public static void GetRandomCharsWithoutSurrogates(char[] chars, int index, int count)
    {
        for (int i = 0; i < count; ++i)
        {
            chars[i] = GenerateRandomCharNonSurrogate();
        }
    }


    public static char[] GetRandomCharsWithSurrogates(int count)
    {
        char[] chars = new char[count];

        GetRandomCharsWithSurrogates(chars, 0, count);

        return chars;
    }

    public static void GetRandomCharsWithSurrogates(char[] chars, int index, int count)
    {
        int randomChar;

        for (int i = 0; i < count; ++i)
        {
            randomChar = GenerateRandomCharWithHighSurrogate();

            if (MIN_HIGH_SURROGATE <= randomChar)
            {
                if (i < (count - 1))
                {
                    chars[i] = (char)randomChar;

                    ++i;
                    chars[i] = GenerateRandomLowSurrogate();
                }
                else
                {
                    chars[i] = GenerateRandomCharNonSurrogate();
                }
            }
            else
            {
                chars[i] = (char)randomChar;
            }
        }
    }

    public static char GenerateRandomASCII()
    {
        return (char)s_random.Next(MIN_RANDOM_ASCII_CHAR, MAX_RANDOM_ASCII_CHAR + 1);
    }

    public static char GenerateRandomHighSurrogate()
    {
        return (char)s_random.Next(MIN_HIGH_SURROGATE, MAX_HIGH_SURROGATE + 1);
    }

    public static char GenerateRandomLowSurrogate()
    {
        return (char)s_random.Next(MIN_LOW_SURROGATE, MAX_LOW_SURROGATE + 1);
    }

    public static char GenerateRandomCharWithHighSurrogate()
    {
        return (char)s_random.Next(MIN_RANDOM_CHAR, MAX_HIGH_SURROGATE + 1);
    }

    public static char GenerateRandomCharNonSurrogate()
    {
        return (char)s_random.Next(MIN_RANDOM_CHAR, MIN_HIGH_SURROGATE);
    }

    public static byte[] GetRandomBytes(int count)
    {
        byte[] bytes = new byte[count];

        GetRandomBytes(bytes, 0, count);

        return bytes;
    }

    /*
    Returns a random char that is not c
    */
    public static char GetRandomOtherChar(char c, CharacterOptions options)
    {
        switch (options)
        {
            case CharacterOptions.ASCII:
                return GetRandomOtherASCIIChar(c);
            default:
                return GetRandomOtherUnicodeChar(c);
        }
    }

    public static char GetRandomOtherUnicodeChar(char c)
    {
        char newChar;

        do
        {
            newChar = GenerateRandomCharNonSurrogate();
        } while (newChar == c);

        return newChar;
    }

    public static char GetRandomOtherASCIIChar(char c)
    {
        char newChar;

        do
        {
            newChar = GenerateRandomASCII();
        } while (newChar == c);

        return newChar;
    }

    public static void GetRandomBytes(byte[] bytes, int index, int count)
    {
        for (int i = 0; i < count; ++i)
        {
            bytes[i] = (byte)s_random.Next(0, 256);
        }
    }

    public static bool IsSurrogate(char c)
    {
        return IsHighSurrogate(c) || IsLowSurrogate(c);
    }

    public static bool IsHighSurrogate(char c)
    {
        return MIN_HIGH_SURROGATE <= c && c <= MAX_HIGH_SURROGATE;
    }

    public static bool IsLowSurrogate(char c)
    {
        return MIN_LOW_SURROGATE <= c && c <= MAX_LOW_SURROGATE;
    }

    public static int OrdinalIndexOf(string input, string search)
    {
        return OrdinalIndexOf(input, 0, input.Length, search);
    }


    public static int OrdinalIndexOf(string input, int startIndex, string search)
    {
        return OrdinalIndexOf(input, startIndex, input.Length - startIndex, search);
    }

    public static int OrdinalIndexOf(string input, int startIndex, int count, string search)
    {
        int lastSearchIndex = (count + startIndex) - search.Length;

        System.Diagnostics.Debug.Assert(lastSearchIndex < input.Length, "Searching will result in accessing element past the end of the array");

        for (int i = startIndex; i <= lastSearchIndex; ++i)
        {
            if (input[i] == search[0])
            {
                bool match = true;
                for (int searchIndex = 1, inputIndex = i + 1; searchIndex < search.Length; ++searchIndex, ++inputIndex)
                {
                    match = input[inputIndex] == search[searchIndex];

                    if (!match)
                    {
                        break;
                    }
                }

                if (match)
                {
                    return i;
                }
            }
        }

        return -1;
    }


    public static void PrintChars(char[] chars)
    {
        for (int i = 0; i < chars.Length; i++)
        {
            Console.WriteLine("(char){0}, //Char={1}, {0:X}", (int)chars[i], chars[i]);
        }
    }

    public static void PrintBytes(byte[] bytes)
    {
        for (int i = 0; i < bytes.Length; i++)
        {
            Console.WriteLine("{0}, //{0:X} Index: {1}", (int)bytes[i], i);
        }
    }


    /// <summary>
    /// Verifies the contents of the array.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="expectedArray">The expected items in the array.</param>
    /// <param name="actualArray">The actual array.</param>
    /// <returns>true if expectedArray and actualArray have the same contents.</returns>
    public static bool VerifyArray<T>(T[] expectedArray, T[] actualArray)
    {
        if (expectedArray.Length != actualArray.Length)
        {
            Console.WriteLine("Err_29289ahieadb Array Length");
            return false;
        }
        else
        {
            return VerifyArray<T>(expectedArray, actualArray, 0, expectedArray.Length);
        }
    }

    /// <summary>
    /// Verifies the contents of the array.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="expectedArray">The expected items in the array.</param>
    /// <param name="actualArray">The actual array.</param>
    /// <param name="index">The index to start verifying the items at.</param>
    /// <param name="length">The number of item to verify</param>
    /// <returns>true if expectedArray and actualArray have the same contents.</returns>
    /// <summary>
    public static bool VerifyArray<T>(T[] expectedArray, T[] actualArray, int index, int length)
    {
        bool retValue = true;
        bool result;
        int tempLength;

        tempLength = length + index;
        for (int i = index; i < tempLength; ++i)
        {
            result = expectedArray[i] == null ? null != actualArray[i] : expectedArray[i].Equals(actualArray[i]);

            if (!result)
            {
                retValue = false;
                Console.WriteLine("Err_55808aoped Items differ at {0} expected {1} actual {2}", i, expectedArray[i], actualArray[i]);
            }
        }

        return retValue;
    }
}
public class PortHelper
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern Int32 GetLastError();
    [DllImport("kernel32.dll")]
    private static extern int QueryDosDevice(string lpDeviceName, IntPtr lpTargetPath, int ucchMax);
    public static string[] GetPorts()
    {
        string[] portsArray = null;
        List<string> ports = new List<string>(); ;
        int returnSize = 0;
        int maxSize = 1000000;
        string allDevices = null;
        IntPtr mem;
        string[] retval = null;
        const int ERROR_INSUFFICIENT_BUFFER = 122;
        while (returnSize == 0)
        {
            mem = Marshal.AllocHGlobal(maxSize);
            if (mem != IntPtr.Zero)
            {
                // mem points to memory that needs freeing
                try
                {
                    returnSize = QueryDosDevice(null, mem, maxSize);
                    if (returnSize != 0)
                    {
                        allDevices = Marshal.PtrToStringAnsi(mem, returnSize);
                        retval = allDevices.Split('\0');
                        break;    // not really needed, but makes it more clear...
                    }
                    else if (GetLastError() == ERROR_INSUFFICIENT_BUFFER)
                    {
                        maxSize *= 10;
                    }
                    else
                    {
                        Marshal.ThrowExceptionForHR(GetLastError());
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(mem);
                }
            }
            else
            {
                throw new OutOfMemoryException();
            }
        }
        foreach (string str in retval)
        {
            if (str.StartsWith("COM"))
            {
                ports.Add(str);
                Console.WriteLine("Ports on the device :" + str);
            }
        }
        return ports.ToArray();
    }
}



