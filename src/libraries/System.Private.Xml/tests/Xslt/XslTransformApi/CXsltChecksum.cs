// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit.Abstractions;
using System;
using System.Globalization;
using System.IO;
using System.Xml;

public class CXsltChecksum
{
    private bool _fTrace;			// Flag for turning on trace in CXsltCache
    private string _strXml;			// XML from the cache

    private ITestOutputHelper _output;

    // --------------------------------------------------------------------------------------------------
    //    Constructor
    // --------------------------------------------------------------------------------------------------
    public CXsltChecksum(bool fTrace, ITestOutputHelper output)
    {
        _fTrace = fTrace;
        _output = output;
    }

    // --------------------------------------------------------------------------------------------------
    //    Properties
    // --------------------------------------------------------------------------------------------------
    public string Xml
    {
        get
        {
            return _strXml;
        }
    }

    public double Calc(XmlReader xr)
    {
        Decimal dResult = 0;	// Numerical value of the checksum
        int i = 0;				// Generic counter
        int iLength = 0;		// Length of the data
        CXmlCache xc;			// Cached output from XslTransform

        _strXml = "";

        // Load the data into the cache
        xc = new CXmlCache();
        xc.ExpandAttributeValues = true;
        xc.Trace = _fTrace;

        xc.Load(xr);
        _strXml = xc.Xml;

        // If there is data to write and omit-xml-declaration is "no", then write the XmlDecl to the stream

        if (_strXml.Length > 0)
            _strXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + _strXml;

        // Calculate the checksum
        iLength = _strXml.Length;

        for (i = 0; i < iLength; i++)
        {
            dResult += Math.Round((Decimal)(_strXml[i] / (i + 1.0)), 10);
            //_output.WriteLine("#{0}({1})", _strXml[i].ToByte(), dResult);
        }
        return Convert.ToDouble(dResult, NumberFormatInfo.InvariantInfo);
    }

    public double Calc(string strFileName)
    {
        Decimal dResult = 0;		// Numerical value of the checksum
        int i = 0;					// Generic counter
        int cBytesRead = 1;			// # of bytes read at one time
        int cTotalRead = 0;			// Total # of bytes read so far
        Decimal dEndBuffer = 0;		// Buffer to remove from the end (This is necessary because
        // 	notepad adds CR/LF onto the end of every file

        Char[] rgBuffer = new Char[4096];
        _strXml = "";

        try
        {
            using (StreamReader fs = new StreamReader(new FileStream(strFileName, FileMode.Open, FileAccess.Read)))
            {
                cBytesRead = fs.Read(rgBuffer, 0, 4096);

                while (cBytesRead > 0)
                {
                    // Keep XML property up to date
                    _strXml = String.Concat(_strXml, new String(rgBuffer, 0, cBytesRead));

                    // Calculate the checksum
                    for (i = 0; i < cBytesRead; i++)
                    {
                        dResult += Math.Round((Decimal)(rgBuffer[i] / (cTotalRead + i + 1.0)), 10);
                        //_output.WriteLine("#{0}({1}) -- {2}", rgBuffer[i], dResult, rgBuffer[i].ToChar());
                    }
                    cTotalRead += cBytesRead;
                    dEndBuffer = 0;

                    // Keep reading (in case file is bigger than 4K)
                    cBytesRead = fs.Read(rgBuffer, 0, 4096);
                }
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine(ex.Message);
            return 0;
        }
        return Convert.ToDouble(dResult - dEndBuffer, NumberFormatInfo.InvariantInfo);
    }
}