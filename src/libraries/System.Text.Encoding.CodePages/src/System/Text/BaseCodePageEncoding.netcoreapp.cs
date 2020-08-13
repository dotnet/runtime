// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;

namespace System.Text
{
    internal abstract partial class BaseCodePageEncoding : EncodingNLS, ISerializable
    {
        internal static unsafe EncodingInfo [] GetEncodings(CodePagesEncodingProvider provider)
        {
            lock (s_streamLock)
            {
                s_codePagesEncodingDataStream.Seek(CODEPAGE_DATA_FILE_HEADER_SIZE, SeekOrigin.Begin);

                int codePagesCount;
                fixed (byte* pBytes = &s_codePagesDataHeader[0])
                {
                    CodePageDataFileHeader* pDataHeader = (CodePageDataFileHeader*)pBytes;
                    codePagesCount = pDataHeader->CodePageCount;
                }

                EncodingInfo [] encodingInfoList = new EncodingInfo[codePagesCount];

                CodePageIndex codePageIndex = default;
                Span<byte> pCodePageIndex = new Span<byte>(&codePageIndex, Unsafe.SizeOf<CodePageIndex>());

                for (int i = 0; i < codePagesCount; i++)
                {
                    s_codePagesEncodingDataStream.Read(pCodePageIndex);

                    string codePageName;
                    switch (codePageIndex.CodePage)
                    {
                        // Fixup some encoding names.
                        case 950:   codePageName = "big5"; break;
                        case 10002: codePageName = "x-mac-chinesetrad"; break;
                        case 20833: codePageName = "x-ebcdic-koreanextended"; break;
                        default:    codePageName = new string(&codePageIndex.CodePageName); break;
                    }

                    string? resourceName = EncodingNLS.GetLocalizedEncodingNameResource(codePageIndex.CodePage);
                    string? displayName = null;

                    if (resourceName != null && resourceName.StartsWith("Globalization_cp_", StringComparison.OrdinalIgnoreCase))
                    {
                        displayName = SR.GetResourceString(resourceName);
                    }

                    encodingInfoList[i] = new EncodingInfo(provider, codePageIndex.CodePage, codePageName, displayName ?? codePageName);
                }

                return encodingInfoList;
            }
        }
    }
}
