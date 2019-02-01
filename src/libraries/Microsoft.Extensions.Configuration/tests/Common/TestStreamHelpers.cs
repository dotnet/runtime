// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
                    throw new NotImplementedException();
                }
            }

            public string Name
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string PhysicalPath
            {
                get
                {
                    throw new NotImplementedException();
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
