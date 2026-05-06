// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration.Test
{
    public static class TestStreamHelpers
    {
        public static readonly string ArbitraryFilePath = "Unit tests do not touch file system";

        public static IFileProvider StringToFileProvider(string str)
        {
            return new TestFileProvider(str);

        }

        private class TestFile : IFileInfo
        {
            private readonly string _data;

            public TestFile(string str)
            {
                _data = str;
            }

            public bool Exists
            {
                get
                {
                    return true;
                }
            }

            public bool IsDirectory
            {
                get
                {
                    return false;
                }
            }

            public DateTimeOffset LastModified
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public long Length
            {
                get
                {
                    return 0;
                }
            }

            public string Name
            {
                get
                {
                    return null;
                }
            }

            public string PhysicalPath
            {
                get
                {
                    return null;
                }
            }

            public Stream CreateReadStream()
            {
                return StringToStream(_data);
            }
        }

        private class TestFileProvider : IFileProvider
        {
            private string _data;
            public TestFileProvider(string str)
            {
                _data = str;
            }

            public IDirectoryContents GetDirectoryContents(string subpath)
            {
                throw new NotImplementedException();
            }

            public IFileInfo GetFileInfo(string subpath)
            {
                return new TestFile(_data);
            }

            public IChangeToken Watch(string filter)
            {
                throw new NotImplementedException();
            }
        }

        public static Stream StringToStream(string str)
        {
            var memStream = new MemoryStream();
            var textWriter = new StreamWriter(memStream);
            textWriter.Write(str);
            textWriter.Flush();
            memStream.Seek(0, SeekOrigin.Begin);

            return memStream;
        }

        public static string StreamToString(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}
