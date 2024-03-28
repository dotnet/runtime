// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Reflection;
using Xunit;

namespace System.IO.Packaging.Tests;

public class ReflectionTests
{
    [Fact]
    public void Verify_GeneralPurposeBitFlag_NotSetTo_Unicode()
    {
        using MemoryStream ms = new();

        using (ZipPackage package = (ZipPackage)Package.Open(ms, FileMode.Create, FileAccess.Write))
        {
            Uri uri = PackUriHelper.CreatePartUri(new Uri("document.xml", UriKind.Relative));
            ZipPackagePart part = (ZipPackagePart)package.CreatePart(uri, Tests.Mime_MediaTypeNames_Text_Xml, CompressionOption.NotCompressed);
            using (Stream partStream = part.GetStream())
            {
                using StreamWriter sw = new(partStream);
                sw.Write(Tests.s_DocumentXml);
            }
            package.CreateRelationship(part.Uri, TargetMode.Internal, "http://packageRelType", "rId1234");
        }

        ms.Position = 0;
        using (ZipArchive archive = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                FieldInfo fieldInfo = typeof(ZipArchiveEntry).GetField("_generalPurposeBitFlag", BindingFlags.Instance | BindingFlags.NonPublic);
                object fieldObject = fieldInfo.GetValue(entry);
                ushort shortField = (ushort)fieldObject;
                Assert.Equal(0, shortField & 0x800); // If it was UTF8, we would set the general purpose bit flag to 0x800 (UnicodeFileNameAndComment)
                CheckCharacters(entry.Name);
                CheckCharacters(entry.Comment); // Unavailable in .NET Framework
            }
        }

        void CheckCharacters(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                Assert.True(c >= 32 && c <= 126, $"ZipArchiveEntry name character {c} requires UTF8");
            }
        }
    }

}
