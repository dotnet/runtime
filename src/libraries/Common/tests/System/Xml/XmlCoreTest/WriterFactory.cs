// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Xml;
using OLEDB.Test.ModuleCore;
using System.Collections;
using System.Collections.Generic;
using System.Xml.XmlDiff;

namespace XmlCoreTest.Common
{
    /*
     * Helper class used by all test frameworks to create an instance of XmlWriter
     * Supports the following writer types
     */
    [Flags]
    public enum WriterType
    {
        All = UTF8Writer | UnicodeWriter | CustomWriter | CharCheckingWriter | UTF8WriterIndent | UnicodeWriterIndent | WrappedWriter | Async | NoAsync,
        AllButCustom = All & ~CustomWriter,
        AllButIndenting = All & ~UTF8WriterIndent & ~UnicodeWriterIndent,

        Async = 1024,
        NoAsync = 2048,

        UTF8Writer = 1,	          // V2 writer with Encoding.UTF8
        UnicodeWriter = 2,        // V2 writer with Encoding.Unicode
        CustomWriter = 4,         // CustomWriter implemented in this same folder
        CharCheckingWriter = 8,	  // CharCheckingWriter implemented in this same folder
        UTF8WriterIndent = 16,    // UTF8WriterIndent implemented in this same folder
        UnicodeWriterIndent = 32, // UnicodeWriterIndent implemented in this same folder
        WrappedWriter = 64        // WrappedWriter implemented in this same folder
    }


    public class WriterFactory
    {
        private string _fileName;
        private bool _overrideAsync = false;
        private bool _async;

        protected WriterType writerType1;
        public WriterType WriterType
        {
            get
            {
                return writerType1;
            }
        }

        public WriterFactory(WriterType t, bool overrideAsync = false, bool async = false)
        {
            writerType1 = t;
            _overrideAsync = overrideAsync;
            _async = async;
        }

        private XmlWriterSettings _wSettings = null;
        private XmlWriter _xmlWriter = null;
        private Stream _writerStream = null;

        XmlWriter CreateWriterImpl()
        {
            this.CleanUp();

            switch (writerType1)
            {
                case WriterType.UTF8Writer:

                    _writerStream = new MemoryStream();
                    _wSettings.CloseOutput = false;
                    _wSettings.Encoding = Encoding.UTF8;
                    _wSettings.NamespaceHandling = NamespaceHandling.Default;
                    _xmlWriter = WriterHelper.Create(_writerStream, _wSettings, _overrideAsync, _async);
                    FilePathUtil.addStream(_fileName, _writerStream);
                    break;
                case WriterType.UnicodeWriter:
                    _writerStream = new MemoryStream();
                    _wSettings.CloseOutput = false;
                    _wSettings.Encoding = Encoding.Unicode;
                    _wSettings.NamespaceHandling = NamespaceHandling.OmitDuplicates;
                    _xmlWriter = WriterHelper.Create(_writerStream, _wSettings, _overrideAsync, _async);
                    FilePathUtil.addStream(_fileName, _writerStream);
                    break;
                case WriterType.CustomWriter:
                    _writerStream = new MemoryStream();
                    _wSettings.CloseOutput = false;
                    if (_overrideAsync)
                        _wSettings.Async = _async;

                    FilePathUtil.addStream(_fileName, _writerStream);
                    _xmlWriter = new CustomWriter(_fileName, _wSettings);
                    break;
                case WriterType.UTF8WriterIndent:
                    _writerStream = new MemoryStream();
                    _wSettings.CloseOutput = false;
                    _wSettings.Encoding = Encoding.UTF8;
                    _wSettings.Indent = true;
                    _xmlWriter = WriterHelper.Create(_writerStream, _wSettings, _overrideAsync, _async);
                    FilePathUtil.addStream(_fileName, _writerStream);
                    break;
                case WriterType.UnicodeWriterIndent:
                    _writerStream = new MemoryStream();
                    _wSettings.CloseOutput = false;
                    _wSettings.Encoding = Encoding.Unicode;
                    _wSettings.Indent = true;
                    _xmlWriter = WriterHelper.Create(_writerStream, _wSettings, _overrideAsync, _async);
                    FilePathUtil.addStream(_fileName, _writerStream);
                    break;
                case WriterType.CharCheckingWriter:
                    _writerStream = new MemoryStream();
                    _wSettings.CloseOutput = false;
                    _wSettings.CheckCharacters = false;
                    XmlWriter ww = WriterHelper.Create(_writerStream, _wSettings, _overrideAsync, _async);
                    FilePathUtil.addStream(_fileName, _writerStream);
                    XmlWriterSettings ws = _wSettings.Clone();
                    ws.CheckCharacters = true;
                    _xmlWriter = WriterHelper.Create(ww, ws, _overrideAsync, _async);
                    break;
                case WriterType.WrappedWriter:
                    _writerStream = new MemoryStream();
                    _wSettings.CloseOutput = false;
                    XmlWriter w = WriterHelper.Create(_writerStream, _wSettings, _overrideAsync, _async);
                    FilePathUtil.addStream(_fileName, _writerStream);
                    _xmlWriter = WriterHelper.Create(w, _wSettings, _overrideAsync, _async);
                    break;
                default:
                    throw new Exception("Incorrect writer type");
            }
            return _xmlWriter;
        }

        public XmlWriter CreateWriter()
        {
            _fileName = "writer.out";
            return this.CreateWriter(_fileName);
        }

        public XmlWriter CreateWriter(string fileName)
        {
            _fileName = fileName;
            _wSettings = new XmlWriterSettings();
            _wSettings.OmitXmlDeclaration = true;
            _wSettings.ConformanceLevel = ConformanceLevel.Document;
            _wSettings.CloseOutput = true;
            return this.CreateWriterImpl();
        }

        public XmlWriter CreateWriter(string fileName, XmlWriterSettings wSettings)
        {
            _wSettings = wSettings;
            _fileName = fileName;
            return this.CreateWriterImpl();
        }

        public XmlWriter CreateWriter(XmlWriterSettings wSettings)
        {
            _wSettings = wSettings;
            _fileName = "writer.out";
            return this.CreateWriterImpl();
        }

        public XmlWriter CreateWriter(ConformanceLevel cl)
        {
            _fileName = "writer.out";
            _wSettings = new XmlWriterSettings();
            _wSettings.ConformanceLevel = cl;
            _wSettings.OmitXmlDeclaration = true;
            _wSettings.CloseOutput = true;
            return this.CreateWriterImpl();
        }

        public string GetString()
        {
            string strRet = string.Empty;
            switch (writerType1)
            {
                case WriterType.UTF8Writer:
                case WriterType.UnicodeWriter:
                case WriterType.CustomWriter:
                case WriterType.UTF8WriterIndent:
                case WriterType.UnicodeWriterIndent:
                case WriterType.CharCheckingWriter:
                case WriterType.WrappedWriter:
                    Stream tempStream = new MemoryStream();
                    FilePathUtil.getStream(_fileName).CopyTo(tempStream);
                    if (tempStream.CanSeek)
                    {
                        tempStream.Position = 0;
                    }
                    using (Stream temp = tempStream)
                    {
                        using (StreamReader srTemp = new StreamReader(temp))
                        {
                            strRet = srTemp.ReadToEnd();
                        }
                    }
                    break;
                default:
                    throw new Exception("Incorrect writer type");
            }
            return strRet;
        }

        public XmlReader GetReader()
        {
            XmlReader xr = null;

            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.CheckCharacters = false;
            readerSettings.CloseInput = false;
            readerSettings.ConformanceLevel = ConformanceLevel.Auto;

            switch (WriterType)
            {
                case WriterType.UTF8Writer:
                case WriterType.UnicodeWriter:
                case WriterType.CustomWriter:
                case WriterType.UTF8WriterIndent:
                case WriterType.UnicodeWriterIndent:
                case WriterType.CharCheckingWriter:
                case WriterType.WrappedWriter:
                    xr = ReaderHelper.Create(FilePathUtil.getStream(_fileName), readerSettings);
                    break;
                default:
                    throw new Exception("Incorrect writer type");
            }
            return xr;
        }

        public bool CompareReader(XmlReader xrExpected)
        {
            return CompareReader(xrExpected, XmlDiffOption.IgnoreAttributeOrder);
        }

        public bool CompareReader(XmlReader xrExpected, XmlDiffOption option)
        {
            bool bReturn = false;

            XmlReader xrActual = GetReader();

            XmlDiff diff = new XmlDiff();
            diff.Option = option;

            try
            {
                bReturn = diff.Compare(xrExpected, xrActual);
            }
            catch (Exception e)
            {
                CError.WriteLine(e);
            }
            finally
            {
                xrActual.Dispose();
                xrExpected.Dispose();
            }
            if (!bReturn)
            {
                CError.WriteLine("Mismatch in XmlDiff");
                CError.WriteLine("Actual o/p:");
                CError.WriteLine(this.GetString());
            }

            return bReturn;
        }

        public bool CompareString(string strExpected)
        {
            string strActual = this.GetString();

            if (strExpected != strActual)
            {
                int expLen = (strExpected == null ? 0 : strExpected.Length);
                int actLen = (strActual == null ? 0 : strActual.Length);

                int minLen = (expLen < actLen ? expLen : actLen);

                // find the first different character
                int i;
                for (i = 0; i < minLen; i++)
                {
                    if (strExpected[i] != strActual[i])
                    {
                        CError.WriteLine("Position:" + i);
                        CError.WriteLine("Expected char:'" + strExpected[i] + "'(" + Convert.ToInt32(strExpected[i]) + ")");
                        CError.WriteLine("Actual char:'" + strActual[i] + "'(" + Convert.ToInt32(strActual[i]) + ")");
                        break;
                    }
                }
                if (i == minLen)
                {
                    // one string contains the other
                    CError.WriteLine("Expected length:" + expLen + " Actual length:" + actLen);
                    return false;
                }
                CError.WriteLine("Expected string:" + strExpected);
                CError.WriteLine("Actual string:" + strActual);
                CError.Compare(false, "CompareString failed");
                return false;
            }
            return true;
        }

        private const char PREFIX_CHAR = '~';
        private const char AUTOGENERATED = 'a';
        private const char FIXED = 'f';

        public bool CompareStringWithPrefixes(string strExpected)
        {
            MyDict<string, object> AutoIDs = new MyDict<string, object>();
            List<string> AttNames = new List<string>();
            List<string> AttScopes = new List<string>();

            string strActual = this.GetString();

            int expLen = (strExpected == null ? 0 : strExpected.Length);
            int actLen = (strActual == null ? 0 : strActual.Length);

            int minLen = (expLen < actLen ? expLen : actLen);

            // find the first different character
            int i, j = 0;
            for (i = 0; i < actLen; i++)
            {
                if (j >= expLen)
                {
                    CError.WriteLine("Output longer than expected!");
                    CError.WriteLine("Actual string: '" + strActual + "'");
                    return false;
                }

                if (strExpected[j] != strActual[i])
                {
                    if (strExpected[j] != PREFIX_CHAR)
                    {
                        CError.WriteLine("Position:" + i);
                        CError.WriteLine("Expected char:'" + strExpected[i] + "'(" + Convert.ToInt32(strExpected[i]) + ")");
                        CError.WriteLine("Actual char:'" + strActual[i] + "'(" + Convert.ToInt32(strActual[i]) + ")");
                        return false;
                    }

                    bool AutoGenerated = strExpected[++j] == AUTOGENERATED;
                    j += 2;
                    string ActName = "";
                    string ExpName = "";
                    string Scope = "";
                    while (i <= actLen)
                    {
                        if (strActual[i] == '=' || strActual[i] == ' ' || strActual[i] == ':')
                        {
                            i--;
                            break;
                        }
                        else
                        {
                            ActName += strActual[i];
                        }
                        i++;
                    }
                    while (strExpected[j] != ' ')
                    {
                        ExpName += strExpected[j++];
                    }
                    j++;
                    while (strExpected[j] != PREFIX_CHAR)
                    {
                        Scope += strExpected[j++];
                    }

                    if (AutoGenerated)
                    {
                        if (AutoIDs.ContainsKey(ExpName))
                        {
                            if ((string)AutoIDs[ExpName] != ActName)
                            {
                                CError.WriteLine("Invalid Prefix: '" + ActName + "'");
                                return false;
                            }
                        }
                        else
                        {
                            AutoIDs.Add(ExpName, ActName);
                        }
                    }
                    else
                    {
                        if (ExpName != ActName)
                        {
                            CError.WriteLine("Invalid Prefix: '" + ActName + "'");
                            return false;
                        }
                    }

                    for (int k = 0; k < AttNames.Count; k++)
                    {
                        if ((string)AttNames[k] == ActName)
                        {
                            for (int m = 0; m < ((string)AttScopes[k]).Length; m++)
                                for (int n = 0; n < Scope.Length; n++)
                                    if (((string)AttScopes[k])[m] == Scope[n])
                                    {
                                        CError.WriteLine("Invalid Prefix: '" + ActName + "'");
                                        return false;
                                    }
                        }
                    }
                    AttNames.Add(ActName);
                    AttScopes.Add(Scope);
                }
                j++;
            }
            if (j != expLen)
            {
                CError.WriteLine("Output shorter than expected!");
                CError.WriteLine("Actual string: '" + strActual + "'");
                return false;
            }

            return true;
        }

        public void CleanUp()
        {
            if (_writerStream != null)
                _writerStream.Dispose();
        }
    }

    public static class WriterHelper
    {
        private static bool ShouldEnableAsync(bool overrideAsync, bool async)
        {
            return overrideAsync ? async : AsyncUtil.IsAsyncEnabled;
        }

        public static XmlWriter Create(string outputFileName, bool overrideAsync = false, bool async = false)
        {
            FilePathUtil.addStream(outputFileName, new MemoryStream());
            if (ShouldEnableAsync(overrideAsync, async))
            {
                return XmlWriterAsync.Create(FilePathUtil.getStream(outputFileName));
            }
            else
            {
                return XmlWriter.Create(FilePathUtil.getStream(outputFileName));
            }
        }

        public static XmlWriter Create(string outputFileName, XmlWriterSettings settings, bool overrideAsync = false, bool async = false)
        {
            FilePathUtil.addStream(outputFileName, new MemoryStream());
            if (ShouldEnableAsync(overrideAsync, async))
            {
                return XmlWriterAsync.Create(FilePathUtil.getStream(outputFileName), settings);
            }
            else
            {
                return XmlWriter.Create(FilePathUtil.getStream(outputFileName), settings);
            }
        }

        public static XmlWriter Create(Stream output, bool overrideAsync = false, bool async = false)
        {
            if (ShouldEnableAsync(overrideAsync, async))
            {
                return XmlWriterAsync.Create(output);
            }
            else
            {
                return XmlWriter.Create(output);
            }
        }

        public static XmlWriter Create(Stream output, XmlWriterSettings settings, bool overrideAsync = false, bool async = false)
        {
            if (ShouldEnableAsync(overrideAsync, async))
            {
                return XmlWriterAsync.Create(output, settings);
            }
            else
            {
                return XmlWriter.Create(output, settings);
            }
        }

        public static XmlWriter Create(TextWriter output, bool overrideAsync = false, bool async = false)
        {
            if (ShouldEnableAsync(overrideAsync, async))
            {
                return XmlWriterAsync.Create(output);
            }
            else
            {
                return XmlWriter.Create(output);
            }
        }

        public static XmlWriter Create(TextWriter output, XmlWriterSettings settings, bool overrideAsync = false, bool async = false)
        {
            if (ShouldEnableAsync(overrideAsync, async))
            {
                return XmlWriterAsync.Create(output, settings);
            }
            else
            {
                return XmlWriter.Create(output, settings);
            }
        }

        public static XmlWriter Create(StringBuilder output, bool overrideAsync = false, bool async = false)
        {
            if (ShouldEnableAsync(overrideAsync, async))
            {
                return XmlWriterAsync.Create(output);
            }
            else
            {
                return XmlWriter.Create(output);
            }
        }

        public static XmlWriter Create(StringBuilder output, XmlWriterSettings settings, bool overrideAsync = false, bool async = false)
        {
            if (ShouldEnableAsync(overrideAsync, async))
            {
                return XmlWriterAsync.Create(output, settings);
            }
            else
            {
                return XmlWriter.Create(output, settings);
            }
        }

        public static XmlWriter Create(XmlWriter output, bool overrideAsync = false, bool async = false)
        {
            if (ShouldEnableAsync(overrideAsync, async))
            {
                return XmlWriterAsync.Create(output);
            }
            else
            {
                return XmlWriter.Create(output);
            }
        }

        public static XmlWriter Create(XmlWriter output, XmlWriterSettings settings, bool overrideAsync = false, bool async = false)
        {
            if (ShouldEnableAsync(overrideAsync, async))
            {
                return XmlWriterAsync.Create(output, settings);
            }
            else
            {
                return XmlWriter.Create(output, settings);
            }
        }
    }
}
