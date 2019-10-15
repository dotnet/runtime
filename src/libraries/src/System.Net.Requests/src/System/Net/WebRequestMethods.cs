// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    public static class WebRequestMethods
    {
        public static class Ftp
        {
            public const string DownloadFile = "RETR";
            public const string ListDirectory = "NLST";
            public const string UploadFile = "STOR";
            public const string DeleteFile = "DELE";
            public const string AppendFile = "APPE";
            public const string GetFileSize = "SIZE";
            public const string UploadFileWithUniqueName = "STOU";
            public const string MakeDirectory = "MKD";
            public const string RemoveDirectory = "RMD";
            public const string ListDirectoryDetails = "LIST";
            public const string GetDateTimestamp = "MDTM";
            public const string PrintWorkingDirectory = "PWD";
            public const string Rename = "RENAME";
        }

        public static class Http
        {
            public const string Get = "GET";
            public const string Connect = "CONNECT";
            public const string Head = "HEAD";
            public const string Put = "PUT";
            public const string Post = "POST";
            public const string MkCol = "MKCOL";
        }

        public static class File
        {
            public const string DownloadFile = "GET";
            public const string UploadFile = "PUT";
        }
    }
}
