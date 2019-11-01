// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public enum ReaderType
    {
        CoreReader
    }

    public partial class ReaderParamTestCase
    {
        internal const ReaderType readerType = ReaderType.CoreReader;

        public bool IsXPathDataModelReader()
        {
            return false;
        }

        public bool ReaderExpandsEntityRef()
        {
            return true;
        }

        public bool ReaderStripsWhitespace()
        {
            return false;
        }

        public bool ReaderParsesDTD()
        {
            return false;
        }

        public bool ReaderLoosesDefaultAttrInfo()
        {
            return false;
        }

        public XmlReader CreateReader(string fileName)
        {
            fileName = Path.GetFileNameWithoutExtension(fileName);

            StreamReader sr = null;
            sr = new StreamReader(FilePathUtil.getStream(XmlWriterUtils.FullPath(fileName + ".xml")));
            return CreateReader(sr);
        }

        public XmlReader CreateReaderIgnoreWS(string fileName)
        {
            StreamReader sr = null;
            sr = new StreamReader(FilePathUtil.getStream(XmlWriterUtils.FullPath(fileName)));
            return CreateReaderIgnoreWS(sr);
        }

        public XmlReader CreateReaderIgnoreWSFromString(string xml)
        {
            StringReader sr = null;
            sr = new StringReader(xml);
            return CreateReaderIgnoreWS(sr);
        }

#pragma warning disable CS0162 // Unreachable code
        public bool ReaderSupportsEntityRef()
        {
            if (readerType == ReaderType.CoreReader)
                return false;
            else
                return true;
        }

        public XmlReader CreateReader(TextReader sr)
        {
            XmlReader xr = null;
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.DtdProcessing = DtdProcessing.Parse;
            readerSettings.CloseInput = true;

            switch (readerType)
            {
                case ReaderType.CoreReader:
                    xr = ReaderHelper.Create(sr, readerSettings, (string)null);
                    break;
                default:
                    CError.Compare(false, "Unknown reader type: " + readerType);
                    break;
            }
            return xr;
        }

        public XmlReader CreateReaderIgnoreWS(TextReader sr)
        {
            XmlReader xr = null;
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.DtdProcessing = DtdProcessing.Parse;
            readerSettings.CloseInput = true;
            readerSettings.IgnoreWhitespace = true;

            switch (readerType)
            {
                case ReaderType.CoreReader:
                    xr = ReaderHelper.Create(sr, readerSettings, (string)null /*baseUri*/);
                    break;
                default:
                    CError.Compare(false, "Unknown reader type: " + readerType);
                    break;
            }
            return xr;
        }
#pragma warning restore CS0162
    }
}
