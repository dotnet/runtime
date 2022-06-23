// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.IO;
using OLEDB.Test.ModuleCore;
using System.Collections.Generic;
using System.Reflection;

namespace XmlCoreTest.Common
{
    public static partial class FilePathUtil
    {
        public static string GetDataPath()
        {
            return GetVariableValue("DataPath");
        }

        public static string GetHttpDataPath()
        {
            return GetVariableValue("HttpDataPath");
        }

        public static string GetFileDataPath()
        {
            return GetVariableValue("FileDataPath");
        }

        public static string GetStandardPath()
        {
            return Path.Combine(GetDataPath(), "StandardTests");
        }

        public static string GetFileStandardPath()
        {
            return Path.Combine(GetFileDataPath(), "StandardTests");
        }

        public static string GetHttpStandardPath()
        {
            return Path.Combine(GetHttpDataPath(), "StandardTests");
        }

        public static string GetWriteableTestRootPath()
        {
            return (PlatformDetection.IsiOS || PlatformDetection.IstvOS) ? Path.GetTempPath() : string.Empty;
        }

        public static string GetTestDataPath()
        {
            return Path.Combine(GetDataPath(), "TestData");
        }

        public static string GetFileTestDataPath()
        {
            return Path.Combine(GetFileDataPath(), "TestData");
        }

        public static string GetHttpTestDataPath()
        {
            return Path.Combine(GetHttpDataPath(), "TestData");
        }

        public static string GetVariableValue(string name)
        {
            string value = CModInfo.Options[name] as string;

            if (value == null || value.Equals(string.Empty))
            {
                value = "";
            }
            return value;
        }

        public static string ExpandVariables(string inputPath)
        {
            StringBuilder resultPath = new StringBuilder();

            int lastPosition = 0, variableStart;
            while (lastPosition < inputPath.Length)
            {
                variableStart = inputPath.IndexOf('$', lastPosition);
                if (variableStart == -1)
                {
                    resultPath.Append(inputPath.Substring(lastPosition));
                    break;
                }
                if (variableStart == inputPath.Length - 1 || inputPath[variableStart + 1] != '(')
                {
                    resultPath.Append(inputPath.Substring(lastPosition, variableStart - lastPosition) + '$');
                    lastPosition = variableStart + 1;
                }
                else
                {
                    int variableEnd = inputPath.IndexOf(')', variableStart);
                    if (variableEnd == -1)
                    {
                        resultPath.Append(inputPath.Substring(lastPosition));
                        break;
                    }
                    string variableValue = GetVariableValue(inputPath.Substring(variableStart + 2, variableEnd - variableStart - 2));
                    if (variableValue != null)
                    {
                        resultPath.Append(variableValue);
                    }
                    lastPosition = variableEnd + 1;
                }
            }

            return resultPath.ToString();
        }

        private static MyDict<string, MemoryStream> s_XmlFileInMemoryCache = null;

        private static readonly object s_XmlFileMemoryCacheLock = new object();
        static void initXmlFileCacheIfNotYet()
        {
            lock (s_XmlFileMemoryCacheLock)
            {
                if (s_XmlFileInMemoryCache == null)
                {
                    MyDict<string, MemoryStream> cache = new MyDict<string, MemoryStream>();

                    foreach (Tuple<string, byte[]> file in GetDataFiles())
                    {
                        var ms = new MemoryStream(file.Item2);
                        cache[NormalizeFilePath(file.Item1)] = ms;
                    }

                    s_XmlFileInMemoryCache = cache;
                }
            }
        }

        public static Stream getStreamDirect(string filename)
        {
            foreach (Tuple<string, byte[]> file in GetDataFiles())
            {
                if (file.Item1 == filename)
                {
                    return new MemoryStream(file.Item2);
                }
            }

            throw new FileNotFoundException("File Not Found: " + filename);
        }

        public static Stream getStream(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return null;

            initXmlFileCacheIfNotYet();

            string normalizedFileName = NormalizeFilePath(filename);

            lock (s_XmlFileMemoryCacheLock)
            {
                MemoryStream ms = s_XmlFileInMemoryCache[normalizedFileName];

                if (ms == null)
                {
                    throw new FileNotFoundException("File Not Found: " + filename);
                }

                // Always give out a new stream, so there's no concern about concurrent use
                MemoryStream msnew = new MemoryStream();
                ms.Position = 0;
                ms.CopyTo(msnew);
                msnew.Position = 0;
                return msnew;
            }
        }

        public static void addStream(string filename, MemoryStream s)
        {
            ArgumentNullException.ThrowIfNull(s);

            if (null == filename)
                return;

            initXmlFileCacheIfNotYet();

            lock (s_XmlFileMemoryCacheLock)
            {
                // overwrite any existing
                s_XmlFileInMemoryCache[NormalizeFilePath(filename)] = s;
            }
        }

        static string NormalizeFilePath(string path)
        {
            return path.Replace('/', '\\').ToLower().Trim();
        }
    }
}
