// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Data;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace DataSetXmlSerializationTrimmingTests
{
    class Program
    {
        static int Main(string[] args)
        {
            IXmlSerializable xmlSerializable = new DataSet();

            // Calling GetSchema should throw NotSupportedException
            try
            {
                xmlSerializable.GetSchema();
                return -1;
            }
            catch (NotSupportedException)
            {
            }

            // Calling ReadXml should throw NotSupportedException
            try
            {
                xmlSerializable.ReadXml(null);
                return -2;
            }
            catch (NotSupportedException)
            {
            }

            // Calling WriteXml should throw NotSupportedException
            try
            {
                xmlSerializable.WriteXml(null);
                return -3;
            }
            catch (NotSupportedException)
            {
            }

            xmlSerializable = new DataTable();

            // Calling GetSchema should throw NotSupportedException
            try
            {
                xmlSerializable.GetSchema();
                return -4;
            }
            catch (NotSupportedException)
            {
            }

            // Calling ReadXml should throw NotSupportedException
            try
            {
                xmlSerializable.ReadXml(null);
                return -5;
            }
            catch (NotSupportedException)
            {
            }

            // Calling WriteXml should throw NotSupportedException
            try
            {
                xmlSerializable.WriteXml(null);
                return -6;
            }
            catch (NotSupportedException)
            {
            }

            return 100;
        }
    }
}
