// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.Storage.Streams;

namespace System.IO
{
    public class TestStreamProvider
    {
        private const string TempFileNameBase = @"WinRT.Testing.IO.Streams.NetFxStreamProvider.DataFile";
        private const string TempFileNameExt = @".tmp";
        private const string TempFileName = TempFileNameBase + TempFileNameExt;

        private static byte[] s_modelStreamContents = null;
        private static int s_modelStreamLength = 0x100000;

        private static readonly string s_tempFileFolder;
        private static readonly string s_tempFilePath;

        static TestStreamProvider()
        {
            s_tempFileFolder = ".";
            s_tempFilePath = Path.Combine(s_tempFileFolder, TempFileName);
        }

        public static byte[] ModelStreamContents
        {
            get
            {
                if (s_modelStreamContents != null)
                    return s_modelStreamContents;

                const int randomSeed = 20090918;

                Random rnd = new Random(randomSeed);
                s_modelStreamContents = new byte[ModelStreamLength];
                rnd.NextBytes(s_modelStreamContents);

                return s_modelStreamContents;
            }
        }

        public static int ModelStreamLength
        {
            get
            {
                return s_modelStreamLength;
            }
            set
            {
                s_modelStreamContents = null;
                s_modelStreamLength = value;
            }
        }

        public static bool CheckContent(byte[] values, int offsInModelContents, int count)
        {
            for (int i = 0; i < count; i++)
            {

                if (!CheckContent(values[i], i + offsInModelContents))
                {

                    Console.WriteLine("Fail on {0}, {1}, {2}, {3}", i, i + offsInModelContents, values[i], ModelStreamContents[i + offsInModelContents]);

                    return false;
                }
            }

            return true;
        }

        public static bool CheckContent(byte value, int index)
        {
            return value == ModelStreamContents[index];
        }

        public static MemoryStream CreateMemoryStream()
        {
            byte[] data = new byte[ModelStreamLength];
            Array.Copy(ModelStreamContents, data, data.Length);

            MemoryStream stream = new MemoryStream(data, 0, data.Length, true);
            return stream;
        }

        public static Stream CreateReadOnlyStream()
        {
            byte[] data = new byte[ModelStreamLength];
            Array.Copy(ModelStreamContents, data, data.Length);

            MemoryStream stream = new MemoryStream(data, 0, data.Length, false);
            return stream;
        }

        public static Stream CreateWriteOnlyStream()
        {
            byte[] data = new byte[ModelStreamLength];
            Array.Copy(ModelStreamContents, data, data.Length);

            MemoryStream stream = new WriteOnlyStream(data);
            return stream;
        }

        public static IInputStream CreateMemoryStreamAsInputStream()
        {
            MemoryStream memStream = CreateMemoryStream();
            return memStream.AsInputStream();
        }
    }
}
