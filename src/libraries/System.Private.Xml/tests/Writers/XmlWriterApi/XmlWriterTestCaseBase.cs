// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public class XmlWriterUtils
    {
        public static string nl = Environment.NewLine;

        public bool Async { get; private set; }

        public WriterType WriterType => WriterFactory.WriterType;

        internal WriterFactory WriterFactory { get; private set; }

        public XmlWriterUtils(WriterType writerType, bool async)
        {
            WriterFactory = new WriterFactory(writerType);
            Async = async;
        }

        public virtual XmlWriter CreateWriter()
        {
            return WriterFactory.CreateWriter();
        }

        public virtual XmlWriter CreateWriter(XmlWriterSettings s)
        {
            return WriterFactory.CreateWriter(s);
        }

        public virtual XmlWriter CreateWriter(ConformanceLevel cl)
        {
            return WriterFactory.CreateWriter(cl);
        }

        public virtual XmlReader GetReader()
        {
            return WriterFactory.GetReader();
        }

        internal static string BaselinePath => Path.Combine(FilePathUtil.GetTestDataPath(), @"XmlWriter2\");

        public static string FullPath(string fileName)
        {
            if (fileName == null || fileName == string.Empty)
                return fileName;

            return BaselinePath + fileName;
        }

        public bool CompareReader(string strExpected)
        {
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.CheckCharacters = false;
            readerSettings.CloseInput = true;
            readerSettings.ConformanceLevel = ConformanceLevel.Auto;

            StringReader sr = new StringReader(strExpected);
            XmlReader xrExpected = XmlReader.Create(sr, readerSettings);
            return WriterFactory.CompareReader(xrExpected);
        }

        public virtual string GetString()
        {
            return WriterFactory.GetString();
        }

        public bool CompareString(string strExpected)
        {
            CError.WriteLine(WriterFactory.GetString());
            if (strExpected.Contains("~"))
                return WriterFactory.CompareStringWithPrefixes(strExpected);

            return WriterFactory.CompareString(strExpected);
        }

        public bool IsIndent()
        {
            return (WriterType == WriterType.UTF8WriterIndent || WriterType == WriterType.UnicodeWriterIndent);
        }

        public void CheckErrorState(WriteState ws)
        {
            if (WriterType == WriterType.CharCheckingWriter)
                return;
            CError.Compare(ws, WriteState.Error, "WriteState should be Error");
        }

        public void CheckElementState(WriteState ws)
        {
            CError.Compare(ws, WriteState.Element, "WriteState should be Element");
        }

        public bool CompareBaseline(string baselineFile)
        {
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.CloseInput = true;

            XmlReader xrExpected = XmlReader.Create(FilePathUtil.getStream(FullPath(baselineFile)), readerSettings);
            return WriterFactory.CompareReader(xrExpected);
        }
    }
}
