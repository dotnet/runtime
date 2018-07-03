// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/// This class is primarily used to test buffer boundary integrity of readers.
///	This class constructs a memory stream from the given buffer boundary length such that
///	the required tag completely lies exactly on the start and end of buffer boundary.
/// The class makes up the additional bytes by filling in whitespace if so.
///	The first buffer length consists of the XML Decl and the Root Start (done by PrepareStream() )
/// The next buffer length consists of the actual start and end text with the variable content stretched
/// out to end at the buffer boundary.
///
using System;
using System.Xml;
using System.Text;
using System.IO;
using OLEDB.Test.ModuleCore;
using System.Collections.Generic;

namespace XmlCoreTest.Common
{


    /// <summary>
    ///  This class contains helper methods for Readers.
    ///	 ConvertToBinaryStream : Converts the given xml string to the binary equivalent of the string and returns it
    ///		using a memory stream.
    /// Common usage pattern would be something like :
    /// XmlReader.Create( new MemoryStream(ReaderHelper.ConvertToBinaryStream("<elem>abc</elem>", true, false)), "baseUri", readerSettings );
    /// </summary>
    public static partial class ReaderHelper
    {
        //All possible enum strings are listed here. 
        //Some of them are duplicates reader types for legacy readers.
        public enum ReaderType
        {
            COREREADER,
            COREVALIDATINGREADER,
            CUSTOMREADER,
            CHARCHECKINGREADER,
            SUBTREEREADER,
            WRAPPEDREADER
        }


        public enum ReadOverload
        {
            String,
            TextReader,
            Stream,
            URL,
            XmlReader
        }

        public class CreateReaderParams
        {
            public ReaderType readerType;
            public string BaseUri = null;
            public ReadOverload InputType;
            public object Input;

            public XmlParserContext ParserContext = null;
            public XmlNameTable NT = null;

            public bool EnableNormalization = true;
            public bool IsFragment = false;

            public XmlReaderSettings Settings;

            public CreateReaderParams(ReaderType type)
            {
                readerType = type;
            }
            public CreateReaderParams(
                ReaderType type,
                ReadOverload inputType,
                object input,
                string baseUri,
                bool isFragment,
                bool enableNormalization,
                XmlReaderSettings readerSettings)
            {
                readerType = type;
                BaseUri = baseUri;
                Input = input;
                InputType = inputType;
                IsFragment = isFragment;
                EnableNormalization = enableNormalization;
                Settings = readerSettings;
            }

            public CreateReaderParams(
                ReaderType type,
                ReadOverload inputType,
                object input,
                XmlReaderSettings readerSettings)
            {
                readerType = type;
                Input = input;
                InputType = inputType;
                Settings = readerSettings;
            }
        }

        //Using these overloads with external entities may not work because there is no evidence or baseuri used.
        public static XmlReader CreateReader(ReaderType readerType, TextReader stringReader, bool enableNormalization)
        {
            return CreateReader(readerType.ToString(), stringReader, enableNormalization, null, null);
        }

        public static XmlReader CreateReader(string readerType, TextReader stringReader, bool enableNormalization)
        {
            return CreateReader(readerType, stringReader, enableNormalization, null, null); //use default eventhandler
        }

        public static XmlReader CreateReader(string readerType, TextReader stringReader, bool enableNormalization, object eventHndlr, XmlReaderSettings settings)
        {
            return CreateReader(readerType, stringReader, enableNormalization, eventHndlr, settings, false);
        }
        public static XmlReader CreateReader(string readerType, TextReader stringReader, bool enableNormalization, object eventHndlr, XmlReaderSettings settings, bool isFragment)
        {
            CError.WriteLineIgnore(readerType);
            ReaderType type = (ReaderType)Enum.Parse(typeof(ReaderType), readerType.ToUpperInvariant());
            CreateReaderParams readerParams = new CreateReaderParams(type);
            readerParams.InputType = ReadOverload.TextReader;
            readerParams.Input = stringReader;
            readerParams.EnableNormalization = enableNormalization;
            readerParams.IsFragment = isFragment;
            readerParams.Settings = settings;

            return CreateReader(readerParams);
        }

        //This API doesn't attach a default validation event handler.
        public static XmlReader CreateReader(ReaderType readerType, Stream stream, string baseUri, bool enableNormalization)
        {
            return CreateReader(readerType.ToString(), stream, baseUri, enableNormalization, null, null, false);
        }
        public static XmlReader CreateReader(string readerType, Stream stream, string baseUri, bool enableNormalization)
        {
            return CreateReader(readerType, stream, baseUri, enableNormalization, null, null, false); //use default eventhandler
        }

        public static XmlReader CreateReader(string readerType, Stream stream, string baseUri, bool enableNormalization, object eventHndlr, XmlReaderSettings settings, bool isFragment)
        {
            CError.WriteLineIgnore(readerType);
            ReaderType type = (ReaderType)Enum.Parse(typeof(ReaderType), readerType.ToUpperInvariant());
            CreateReaderParams readerParams = new CreateReaderParams(type);
            readerParams.InputType = ReadOverload.Stream;
            readerParams.Input = stream;
            readerParams.BaseUri = baseUri;
            readerParams.EnableNormalization = enableNormalization;
            readerParams.IsFragment = isFragment;
            readerParams.Settings = settings;

            return CreateReader(readerParams);
        }

        //This API attaches a default validation event handler.
        public static XmlReader CreateReader(ReaderType readerType, string url, bool enableNormalization)
        {
            return CreateReader(readerType.ToString(), url, enableNormalization, null, null);
        }
        public static XmlReader CreateReader(string readerType, string url, bool enableNormalization)
        {
            return CreateReader(readerType, url, enableNormalization, null, null); //use default eventhandler
        }

        public static XmlReader CreateReader(string readerType, string url, bool enableNormalization, object eventHndlr, XmlReaderSettings settings)
        {
            return CreateReader(readerType, url, enableNormalization, eventHndlr, settings, false);
        }
        public static XmlReader CreateReader(string readerType, string url, bool enableNormalization, object eventHndlr, XmlReaderSettings settings, bool isFragment)
        {
            CError.WriteLineIgnore(readerType);
            ReaderType type = (ReaderType)Enum.Parse(typeof(ReaderType), readerType.ToUpperInvariant());
            CreateReaderParams readerParams = new CreateReaderParams(type);
            readerParams.InputType = ReadOverload.URL;
            readerParams.Input = url;
            readerParams.EnableNormalization = enableNormalization;
            readerParams.IsFragment = isFragment;
            readerParams.Settings = settings;

            return CreateReader(readerParams);
        }

        public static XmlReader CreateReader(ReaderType readerType, XmlReader underlyingReader, bool enableNormalization)
        {
            return CreateReader(readerType.ToString(), underlyingReader, enableNormalization, null, null);
        }
        public static XmlReader CreateReader(string readerType, XmlReader underlyingReader, bool enableNormalization)
        {
            return CreateReader(readerType, underlyingReader, enableNormalization, null, null); //use default eventhandler
        }

        public static XmlReader CreateReader(string readerType, XmlReader underlyingReader, bool enableNormalization, object eventHndlr, XmlReaderSettings settings)
        {
            return CreateReader(readerType, underlyingReader, enableNormalization, eventHndlr, settings, false);
        }
        public static XmlReader CreateReader(string readerType, XmlReader underlyingReader, bool enableNormalization, object eventHndlr, XmlReaderSettings settings, bool isFragment)
        {
            CError.WriteLineIgnore(readerType);
            ReaderType type = (ReaderType)Enum.Parse(typeof(ReaderType), readerType.ToUpperInvariant());
            CreateReaderParams readerParams = new CreateReaderParams(type);
            readerParams.InputType = ReadOverload.XmlReader;
            readerParams.Input = underlyingReader;
            readerParams.EnableNormalization = enableNormalization;
            readerParams.IsFragment = isFragment;
            readerParams.Settings = settings;

            return CreateReader(readerParams);
        }

        public static void CreateXSLTStyleSheetWCopyTestFile(string strFileName)
        {
        }


        public static XmlReader CreateReader(CreateReaderParams createParams)
        {
            switch (createParams.readerType)
            {
                case ReaderType.COREREADER:
                case ReaderType.COREVALIDATINGREADER:
                    return CreateFactoryReader(createParams);

                case ReaderType.CHARCHECKINGREADER:
                    return CreateCharCheckingReader(createParams);
                case ReaderType.SUBTREEREADER:
                    return CreateSubtreeReader(createParams);
                case ReaderType.WRAPPEDREADER:
                    return CreateWrappedReader(createParams);

                default:
                    throw new CTestFailedException("Unsupported ReaderType");
            }
        }

        public static XmlReader CreateCharCheckingReader(CreateReaderParams createParams)
        {
            XmlReaderSettings settings = GetSettings(createParams);
            XmlParserContext parserContext = GetParserContext(createParams);
            XmlReader r = null;
            settings.CheckCharacters = false;
            XmlReaderSettings rs = settings.Clone();
            rs.CheckCharacters = true;
            switch (createParams.InputType)
            {
                case ReadOverload.String:
                    r = Create(new StringReader((string)createParams.Input), settings, parserContext);
                    return Create(r, rs);

                case ReadOverload.URL:
                    r = Create((string)createParams.Input, settings);
                    return Create(r, rs);

                case ReadOverload.Stream:
                    r = Create((Stream)createParams.Input, settings, parserContext);
                    return Create(r, rs);

                case ReadOverload.TextReader:
                    r = Create((TextReader)createParams.Input, settings, parserContext);
                    return Create(r, rs);

                case ReadOverload.XmlReader:
                    r = Create((XmlReader)createParams.Input, settings);
                    return Create(r, rs);

                default:
                    throw new CTestFailedException("Unknown ReadOverload");
            }
        }

        public static XmlReader CreateSubtreeReader(CreateReaderParams createParams)
        {
            XmlReaderSettings settings = GetSettings(createParams);
            XmlParserContext parserContext = GetParserContext(createParams);
            XmlReader r = null;
            switch (createParams.InputType)
            {
                case ReadOverload.String:
                    r = Create(new StringReader((string)createParams.Input), settings, parserContext);
                    while (r.Read())
                    {
                        if (r.NodeType == XmlNodeType.Element)
                            break;
                    }
                    return r.ReadSubtree();

                case ReadOverload.URL:
                    r = Create((string)createParams.Input, settings);
                    while (r.Read())
                    {
                        if (r.NodeType == XmlNodeType.Element)
                            break;
                    }
                    return r.ReadSubtree();

                case ReadOverload.Stream:
                    r = Create((Stream)createParams.Input, settings, parserContext);
                    while (r.Read())
                    {
                        if (r.NodeType == XmlNodeType.Element)
                            break;
                    }
                    return r.ReadSubtree();

                case ReadOverload.TextReader:
                    r = Create((TextReader)createParams.Input, settings, parserContext);
                    while (r.Read())
                    {
                        if (r.NodeType == XmlNodeType.Element)
                            break;
                    }
                    return r.ReadSubtree();

                case ReadOverload.XmlReader:
                    r = Create((XmlReader)createParams.Input, settings);
                    while (r.Read())
                    {
                        if (r.NodeType == XmlNodeType.Element)
                            break;
                    }
                    return r.ReadSubtree();

                default:
                    throw new CTestFailedException("Unknown ReadOverload");
            }
        }

        public static XmlReader CreateWrappedReader(CreateReaderParams createParams)
        {
            XmlReaderSettings settings = GetSettings(createParams);
            XmlParserContext parserContext = GetParserContext(createParams);
            XmlReader r = null;
            switch (createParams.InputType)
            {
                case ReadOverload.String:
                    r = Create(new StringReader((string)createParams.Input), settings, parserContext);
                    return Create(r, settings);

                case ReadOverload.URL:
                    r = Create((string)createParams.Input, settings);
                    return Create(r, settings);

                case ReadOverload.Stream:
                    r = Create((Stream)createParams.Input, settings, parserContext);
                    return Create(r, settings);

                case ReadOverload.TextReader:
                    r = Create((TextReader)createParams.Input, settings, parserContext);
                    return Create(r, settings);

                case ReadOverload.XmlReader:
                    r = Create((XmlReader)createParams.Input, settings);
                    return Create(r, settings);

                default:
                    throw new CTestFailedException("Unknown ReadOverload");
            }
        }

        public static XmlReaderSettings GetSettings(CreateReaderParams createParams)
        {
            XmlReaderSettings settings = new XmlReaderSettings();

            if (createParams.Settings != null)
            {
                settings = createParams.Settings;
            }
            else
            {
                if (createParams.IsFragment)
                    settings.ConformanceLevel = ConformanceLevel.Fragment;
            }

            return settings;
        }

        public static XmlParserContext GetParserContext(CreateReaderParams createParams)
        {
            if (createParams.ParserContext != null)
                return createParams.ParserContext;
            else
            {
                if (createParams.BaseUri != null)
                {
                    XmlParserContext parserContext = new XmlParserContext(null, null, null, null, null, null, createParams.BaseUri, null, XmlSpace.None);
                    return parserContext;
                }
                else
                {
                    return null;
                }
            }
        }

        public static XmlReader CreateFactoryReader(CreateReaderParams createParams)
        {
            XmlReaderSettings settings = GetSettings(createParams);
            XmlParserContext parserContext = GetParserContext(createParams);
            switch (createParams.InputType)
            {
                case ReadOverload.String:
                    return Create(new StringReader((string)createParams.Input), settings, parserContext);

                case ReadOverload.URL:
                    return Create((string)createParams.Input, settings, parserContext);

                case ReadOverload.Stream:
                    return Create((Stream)createParams.Input, settings, parserContext);

                case ReadOverload.TextReader:
                    return Create((TextReader)createParams.Input, settings, parserContext);

                case ReadOverload.XmlReader:
                    return Create((XmlReader)createParams.Input, settings);

                default:
                    throw new CTestFailedException("Unknown ReadOverload");
            }
        }

        private static Random s_rand = new Random((int)DateTime.Now.Ticks);

        public static IEnumerable<string> GenerateNames(int count, bool isValid, CharType charType)
        {
            Func<CharType, string> generator = isValid ? (Func<CharType, string>)UnicodeCharHelper.GetValidCharacters : (Func<CharType, string>)UnicodeCharHelper.GetInvalidCharacters;
            string chars = generator(charType);
            for (int i = 0; i < count; i++)
            {
                char c = chars[s_rand.Next(chars.Length)];
                yield return GetNameWithChar(c, charType);
            }
        }

        public static string GetNameWithChar(char c, CharType charType)
        {
            switch (charType)
            {
                case CharType.NameStartChar:
                    return new string(new char[] { c, 'a', 'b' });
                case CharType.NameChar:
                    return new string(new char[] { 'a', c, 'b' });
                case CharType.NameStartSurrogateHighChar:
                    return new string(new char[] { c, '\udc00', 'a', 'b' });
                case CharType.NameStartSurrogateLowChar:
                    return new string(new char[] { '\udb7f', c, 'a', 'b' });
                case CharType.NameSurrogateHighChar:
                    return new string(new char[] { 'a', 'b', c, '\udc00' });
                case CharType.NameSurrogateLowChar:
                    return new string(new char[] { 'a', 'b', '\udb7f', c });
                default:
                    throw new CTestFailedException("TEST ISSUE: CharType FAILURE!");
            }
        }

        public static XmlReader Create(string inputUri)
        {
            if (AsyncUtil.IsAsyncEnabled)
            {
                return XmlReaderAsync.Create(FilePathUtil.getStream(inputUri));
            }
            else
            {
                return XmlReader.Create(FilePathUtil.getStream(inputUri));
            }
        }

        public static XmlReader Create(string inputUri, XmlReaderSettings settings)
        {
            if (AsyncUtil.IsAsyncEnabled)
            {
                return XmlReaderAsync.Create(FilePathUtil.getStream(inputUri), settings);
            }
            else
            {
                return XmlReader.Create(FilePathUtil.getStream(inputUri), settings);
            }
        }

        public static XmlReader Create(string inputUri, XmlReaderSettings settings, XmlParserContext inputContext)
        {
            if (AsyncUtil.IsAsyncEnabled)
            {
                return XmlReaderAsync.Create(FilePathUtil.getStream(inputUri), settings, inputContext);
            }
            else
            {
                return XmlReader.Create(FilePathUtil.getStream(inputUri));//, settings, inputContext);
            }
        }

        public static XmlReader Create(Stream input)
        {
            if (AsyncUtil.IsAsyncEnabled)
            {
                return XmlReaderAsync.Create(input);
            }
            else
            {
                return XmlReader.Create(input);
            }
        }

        public static XmlReader Create(Stream input, XmlReaderSettings settings)
        {
            if (AsyncUtil.IsAsyncEnabled)
            {
                return XmlReaderAsync.Create(input, settings);
            }
            else
            {
                return XmlReader.Create(input, settings);
            }
        }

        public static XmlReader Create(Stream input, XmlReaderSettings settings, string baseUri)
        {
            if (AsyncUtil.IsAsyncEnabled)
            {
                return XmlReaderAsync.Create(input, settings, baseUri);
            }
            else
            {
                return XmlReader.Create(input, settings);//, baseUri);
            }
        }

        public static XmlReader Create(Stream input, XmlReaderSettings settings, XmlParserContext inputContext)
        {
            if (AsyncUtil.IsAsyncEnabled)
            {
                return XmlReaderAsync.Create(input, settings, inputContext);
            }
            else
            {
                return XmlReader.Create(input, settings, inputContext);
            }
        }

        public static XmlReader Create(TextReader input)
        {
            if (AsyncUtil.IsAsyncEnabled)
            {
                return XmlReaderAsync.Create(input);
            }
            else
            {
                return XmlReader.Create(input);
            }
        }

        public static XmlReader Create(TextReader input, XmlReaderSettings settings)
        {
            if (AsyncUtil.IsAsyncEnabled)
            {
                return XmlReaderAsync.Create(input, settings);
            }
            else
            {
                return XmlReader.Create(input, settings);
            }
        }

        public static XmlReader Create(TextReader input, XmlReaderSettings settings, string baseUri)
        {
            if (AsyncUtil.IsAsyncEnabled)
            {
                return XmlReaderAsync.Create(input, settings, baseUri);
            }
            else
            {
                return XmlReader.Create(input, settings);//, baseUri);
            }
        }

        public static XmlReader Create(TextReader input, XmlReaderSettings settings, XmlParserContext inputContext)
        {
            if (AsyncUtil.IsAsyncEnabled)
            {
                return XmlReaderAsync.Create(input, settings, inputContext);
            }
            else
            {
                return XmlReader.Create(input, settings, inputContext);
            }
        }

        public static XmlReader Create(XmlReader reader, XmlReaderSettings settings)
        {
            if (AsyncUtil.IsAsyncEnabled)
            {
                return XmlReaderAsync.Create(reader, settings);
            }
            else
            {
                return XmlReader.Create(reader, settings);
            }
        }
    }
}
