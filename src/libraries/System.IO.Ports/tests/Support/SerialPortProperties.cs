// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.IO.Ports;
using System.Reflection;
using System.Text;
using Xunit;

namespace Legacy.Support
{
    public class SerialPortProperties
    {
        // All of the following properties are the defualts of SerialPort when the
        // just the default constructor has been called. The names of the data members here must
        // begin with default followed by the EXACT(case sensitive) name of
        // the property in SerialPort class. If a default for a property is not set
        // here then the property can never be set in this class and will never be
        // used for verification.

        private const int defaultBaudRate = 9600;
        public static readonly string defaultPortName = "COM1";
        private const int defaultDataBits = 8;
        public static readonly StopBits defaultStopBits = StopBits.One;
        public static readonly Parity defaultParity = Parity.None;
        public static readonly Handshake defaultHandshake = Handshake.None;
        public static readonly Encoding defaultEncoding = new ASCIIEncoding();
        public static readonly bool defaultDtrEnable = false;
        public static readonly bool defaultRtsEnable = false;
        private const int defaultReadTimeout = -1;
        private const int defaultWriteTimeout = -1;
        public static readonly Type defaultBytesToRead = typeof(InvalidOperationException);
        public static readonly Type defaultBytesToWrite = typeof(InvalidOperationException);
        public static readonly bool defaultIsOpen = false;
        public static readonly Type defaultBaseStream = typeof(InvalidOperationException);
        private const int defaultReceivedBytesThreshold = 1;
        public static readonly bool defaultDiscardNull = false;
        public static readonly byte defaultParityReplace = (byte)'?';
        public static readonly Type defaultCDHolding = typeof(InvalidOperationException);
        public static readonly Type defaultCtsHolding = typeof(InvalidOperationException);
        public static readonly Type defaultDsrHolding = typeof(InvalidOperationException);
        public static readonly string defaultNewLine = "\n";
        public static readonly Type defaultBreakState = typeof(InvalidOperationException);
        private const int defaultReadBufferSize = 4 * 1024;
        private const int defaultWriteBufferSize = 2 * 1024;

        // All of the following properties are the defualts of SerialPort when the
        // serial port connection is open. The names of the data members here must
        // begin with openDefault followed by the EXACT(case sensitive) name of
        // the property in SerialPort class.

        private const int openDefaultBaudRate = 9600;
        public static readonly string openDefaultPortName = "COM1";
        private const int openDefaultDataBits = 8;
        public static readonly StopBits openDefaultStopBits = StopBits.One;
        public static readonly Parity openDefaultParity = Parity.None;
        public static readonly Handshake openDefaultHandshake = Handshake.None;
        public static readonly Encoding openDefaultEncoding = new ASCIIEncoding();
        public static readonly bool openDefaultDtrEnable = false;
        public static readonly bool openDefaultRtsEnable = false;
        private const int openDefaultReadTimeout = -1;
        private const int openDefaultWriteTimeout = -1;
        private const int openDefaultBytesToRead = 0;
        private const int openDefaultBytesToWrite = 0;
        public static readonly bool openDefaultIsOpen = true;
        private const int openDefaultReceivedBytesThreshold = 1;
        public static readonly bool openDefaultDiscardNull = false;
        public static readonly byte openDefaultParityReplace = (byte)'?';

        // Removing the following properties from default checks.  Sometimes these can be true (as read from the GetCommModemStatus win32 API)
        // which causes test failures.  Since these are read-only properties and are simply obtained from a bitfield, there is a very low probability
        // of regression involving these properties.
        //
        // public static readonly bool openDefaultCDHolding = false;
        // public static readonly bool openDefaultCtsHolding = false;
        // public static readonly bool openDefaultDsrHolding = false;

        public static readonly string openDefaultNewLine = "\n";
        public static readonly bool openDefaultBreakState = false;
        private const int openReadBufferSize = 4 * 1024;
        private const int openWriteBufferSize = 2 * 1024;

        private Hashtable _properties;
        private Hashtable _openDefaultProperties;
        private Hashtable _defaultProperties;

        public SerialPortProperties()
        {
            _properties = new Hashtable();
            _openDefaultProperties = new Hashtable();
            _defaultProperties = new Hashtable();
            LoadDefaults();
        }

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
            StringBuilder strBuf = new StringBuilder();

            foreach (object key in _properties.Keys)
            {
                strBuf.Append(key + "=" + _properties[key] + "\n");
            }

            return strBuf.ToString();
        }

        public Hashtable VerifyProperties(SerialPort port)
        {
            Type serialPortType = typeof(SerialPort);
            Hashtable badProps = new Hashtable();

            // If we could not get the type of SerialPort then verification can not
            // occur and we should throw an exception
            if (null == serialPortType)
            {
                throw new Exception("Could not create a Type object of SerialPort");
            }

            // For each key in the properties Hashtable verify that the property in the
            // SerialPort Object port with the same name as the key has the same value as
            // the value corresponding to the key in the HashTable. If the port is in a
            // state where accessing a property is suppose to throw an exception this can be
            // verified by setting the value in the hashtable to the System.Type of the
            // expected exception
            foreach (object key in _properties.Keys)
            {
                object value = _properties[key];

                try
                {
                    PropertyInfo serialPortProperty = serialPortType.GetProperty((string)key);
                    object serialPortValue;

                    if ((string)key == "RtsEnable" &&
                        ((port.Handshake == Handshake.RequestToSend &&
                            (Handshake)_properties["Handshake"] == Handshake.RequestToSend) ||
                        (port.Handshake == Handshake.RequestToSendXOnXOff &&
                            (Handshake)_properties["Handshake"] == Handshake.RequestToSendXOnXOff)))
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
                    // An exception was thrown while retrieving the value of the property in
                    // the SerialPort object. However this may be the exepected operation of
                    // SerialPort so the type of the exception must be verified. Reflection
                    // throws it's own exception ontop of SerialPorts so we must use the
                    // InnerException of the own that Reflection throws
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
            }

            if (0 == badProps.Count)
            {
                return null;
            }
            else
            {
                return badProps;
            }
        }

        public void VerifyPropertiesAndPrint(SerialPort port)
        {
            Hashtable badProps;

            if (null == (badProps = VerifyProperties(port)))
            {
                // SerialPort values correctly set
                return;
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                foreach (object key in badProps.Keys)
                {
                    sb.AppendLine($"{key}={badProps[key]} expected {GetProperty((string)key)}");
                }

                Assert.Fail(sb.ToString());
            }
        }

        private void LoadDefaults()
        {
            LoadFields("openDefault", _openDefaultProperties);
            LoadFields("default", _defaultProperties);
        }

        private void LoadFields(string startsWith, Hashtable fields)
        {
            Type serialPropertiesType = typeof(SerialPortProperties);

            // For each field in this class that starts with the string startsWith create
            // a key value pair in the hashtable fields with key being the rest of the
            // field name after startsWith and the value is whatever the value is of the field
            // in this class
            foreach (FieldInfo defaultField in serialPropertiesType.GetFields())
            {
                string defaultFieldName = defaultField.Name;

                if (0 == defaultFieldName.IndexOf(startsWith))
                {
                    string fieldName = defaultFieldName.Replace(startsWith, "");
                    object defaultValue = defaultField.GetValue(this);

                    fields[fieldName] = defaultValue;
                }
            }
        }

        private void SetAllPropertiesToDefaults(Hashtable defaultProperties)
        {
            _properties.Clear();

            // For each key in the defaultProperties Hashtable set properties[key]
            // with the corresponding value in defaultProperties
            foreach (object key in defaultProperties.Keys)
            {
                _properties[key] = defaultProperties[key];
            }
        }

        private object SetDefaultProperty(string name, Hashtable defaultProperties)
        {
            object retVal = null;

            // Only Set the default value if it exists in the defaultProperties Hashtable
            // This will prevent the ability to create arbitrary keys(Property names)
            if (null != defaultProperties[name])
            {
                retVal = _properties[name];
                _properties[name] = defaultProperties[name];
            }

            return retVal;
        }
    }
}
