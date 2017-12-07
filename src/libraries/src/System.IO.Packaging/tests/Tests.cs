// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace System.IO.Packaging.Tests
{
    public class Tests : FileCleanupTestBase
    {
        private const string Mime_MediaTypeNames_Text_Xml = "text/xml";
        private const string Mime_MediaTypeNames_Image_Jpeg = "image/jpeg"; // System.Net.Mime.MediaTypeNames.Image.Jpeg
        private const string s_DocumentXml = @"<Hello>Test</Hello>";
        private const string s_ResourceXml = @"<Resource>Test</Resource>";

        private FileInfo GetTempFileInfoFromExistingFile(string existingFileName, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            FileInfo existingDoc = new FileInfo(existingFileName);
            byte[] content = File.ReadAllBytes(existingDoc.FullName);
            FileInfo newFile =  new FileInfo($"{GetTestFilePath(null, memberName, lineNumber)}.{existingDoc.Extension}");
            File.WriteAllBytes(newFile.FullName, content);
            return newFile;
        }

        public FileInfo GetTempFileInfoWithExtension(string extension, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            return new FileInfo($"{GetTestFilePath(null, memberName, lineNumber)}.{extension}");
        }

        [Fact]
        public void T201_FileFormatException()
        {
            var e = new FileFormatException();
            Assert.NotEmpty(e.Message);
            Assert.Null(e.SourceUri);
            Assert.Null(e.InnerException);
        }

        [Fact]
        public void T202_FileFormatException()
        {
            var e2 = new IOException("Test");
            var e = new FileFormatException("Test", e2);
            Assert.Equal("Test", e.Message);
            Assert.Null(e.SourceUri);
            Assert.Same(e2, e.InnerException);
        }

        [Fact]
        public void T203_FileFormatException()
        {
            var partUri = new Uri("/idontexist.xml", UriKind.Relative);
            var e = new FileFormatException(partUri);
            Assert.NotEmpty(e.Message);
            Assert.Same(partUri, e.SourceUri);
            Assert.Null(e.InnerException);
        }

        [Fact]
        public void T203A_FileFormatException()
        {
            Uri partUri = null;
            var e = new FileFormatException(partUri);
            Assert.NotEmpty(e.Message);
            Assert.Null(e.SourceUri);
            Assert.Null(e.InnerException);
        }

        [Fact]
        public void T204_FileFormatException()
        {
            var partUri = new Uri("/idontexist.xml", UriKind.Relative);
            var e = new FileFormatException(partUri, "Test");
            Assert.Equal("Test", e.Message);
            Assert.Same(partUri, e.SourceUri);
            Assert.Null(e.InnerException);
        }

        [Fact]
        public void T205_FileFormatException()
        {
            var partUri = new Uri("/idontexist.xml", UriKind.Relative);
            var e2 = new IOException("Test");
            var e = new FileFormatException(partUri, e2);
            Assert.NotEmpty(e.Message);
            Assert.Same(partUri, e.SourceUri);
            Assert.Same(e2, e.InnerException);
        }

        [Fact]
        public void T205A_FileFormatException()
        {
            Uri partUri = null;
            var e2 = new IOException("Test");
            var e = new FileFormatException(partUri, e2);
            Assert.NotEmpty(e.Message);
            Assert.Null(e.SourceUri);
            Assert.Same(e2, e.InnerException);
        }

        [Fact]
        public void T206_FileFormatException()
        {
            var partUri = new Uri("/idontexist.xml", UriKind.Relative);
            var e2 = new IOException("Test");
            var e = new FileFormatException(partUri, "Test", e2);
            Assert.Equal("Test", e.Message);
            Assert.Same(partUri, e.SourceUri);
            Assert.Same(e2, e.InnerException);
        }

        [Fact]
        public void T208_InvalidParameter()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                Assert.Equal(0, ms.Length);
                PackagePart packagePartDocument = null;
                AssertExtensions.Throws<ArgumentException>(null, () => { packagePartDocument = package.CreatePart(partUriDocument, "image/jpeg; prop= ;"); });
            }
        }

        [Fact]
        public void PackageOpen_CreateNew_NonEmptyStream_Throws()
        {
            byte[] ba = File.ReadAllBytes("plain.docx");
            using (var ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Assert.Throws<IOException>(() => Package.Open(ms, FileMode.CreateNew, FileAccess.ReadWrite));
            }
        }

        [Fact]
        public void PackageOpen_Open_EmptyStream_Throws()
        {
            using (var ms = new MemoryStream())
            {
                Assert.Throws<FileFormatException>(() => Package.Open(ms, FileMode.Open, FileAccess.ReadWrite));
            }
        }

        [Fact]
        public void T172_EmptyRelationshipPart()
        {
            var docName = "EmptyRelationshipElement.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Open);
                PackageRelationship docPackageRelationship = package
                    .GetRelationshipsByType(DocumentRelationshipType)
                    .FirstOrDefault();
                if (docPackageRelationship != null)
                {
                    Uri documentUri = PackUriHelper.ResolvePartUri(
                            new Uri("/", UriKind.Relative), docPackageRelationship.TargetUri);
                    var mdp = package.GetPart(documentUri);
                    var relationships = mdp.GetRelationships().ToList();
                    foreach (var r in relationships)
                        mdp.DeleteRelationship(r.Id);
                }
                package.Close();
            }
        }

        [Fact]
        public void T171_IterateExternalRelationship()
        {
            var docName = "Hyperlink.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Open);
                PackageRelationship docPackageRelationship = package
                    .GetRelationshipsByType(DocumentRelationshipType)
                    .FirstOrDefault();
                if (docPackageRelationship != null)
                {
                    Uri documentUri = PackUriHelper.ResolvePartUri(
                            new Uri("/", UriKind.Relative), docPackageRelationship.TargetUri);
                    var mdp = package.GetPart(documentUri);
                    var relationships = mdp.GetRelationships().ToList();
                    foreach (var r in relationships)
                        mdp.DeleteRelationship(r.Id);
                }
                package.Close();
            }
        }

        [Fact]
        public void T170_InvalidRelationshipId()
        {
            var docName = "plain.docx";
            var fiGuidName = GetTempFileInfoFromExistingFile(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));
            using (Package package = Package.Open(fiGuidName.FullName, FileMode.Create, FileAccess.ReadWrite))
            {
                Assert.Throws<XmlException>(() =>
                {
                    package.DeleteRelationship("ab:23");
                });
            }
            fiGuidName.Delete();
        }

        [Fact]
        public void T169_DeleteAllRelationshipOfMainDocPart()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Open);
                PackageRelationship docPackageRelationship = package
                    .GetRelationshipsByType(DocumentRelationshipType)
                    .FirstOrDefault();
                if (docPackageRelationship != null)
                {
                    Uri documentUri = PackUriHelper.ResolvePartUri(
                            new Uri("/", UriKind.Relative), docPackageRelationship.TargetUri);
                    var mdp = package.GetPart(documentUri);
                    var relationships = mdp.GetRelationships().ToList();
                    foreach (var r in relationships)
                        mdp.DeleteRelationship(r.Id);
                }
                package.Close();
            }
        }

        [Fact]
        public void T168_InvalidRelationshipId()
        {
            var docName = "plain.docx";
            var fiGuidName = GetTempFileInfoFromExistingFile(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));
            using (Package package = Package.Open(fiGuidName.FullName, FileMode.Create, FileAccess.ReadWrite))
            {
                PackagePart packagePartDocument = package.CreatePart(partUriDocument, Mime_MediaTypeNames_Text_Xml);
                using (Stream partStream = packagePartDocument.GetStream())
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    {
                        package.CreateRelationship(packagePartDocument.Uri, TargetMode.Internal, "");
                    });
            }
            fiGuidName.Delete();
        }

        [Fact]
        public void T167_DeleteNonExistentRelationship()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    PackageRelationship docPackageRelationship = package
                      .GetRelationshipsByType(DocumentRelationshipType)
                      .FirstOrDefault();
                    if (docPackageRelationship != null)
                    {
                        Uri documentUri = PackUriHelper.ResolvePartUri(
                               new Uri("/", UriKind.Relative), docPackageRelationship.TargetUri);
                        PackagePart documentPart = package.GetPart(documentUri);
                        documentPart.DeleteRelationship("dummyId");
                    }
                }
            }
        }

        [Fact]
        public void T166_GetPartRelationshipById_InvalidId()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    PackageRelationship docPackageRelationship =
                      package
                      .GetRelationshipsByType(DocumentRelationshipType)
                      .FirstOrDefault();

                    if (docPackageRelationship != null)
                    {
                        Uri documentUri =
                            PackUriHelper
                            .ResolvePartUri(
                               new Uri("/", UriKind.Relative),
                                     docPackageRelationship.TargetUri);
                        PackagePart documentPart =
                            package.GetPart(documentUri);

                        Assert.Throws<InvalidOperationException>(() =>
                            {
                                var styleRelation2 = documentPart.GetRelationship("dummyId");
                            });
                    }
                }
            }
        }

        [Fact]
        public void T165_DeleteNonExistentPart()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    var documentPath2 = "/abc.xaml";
                    Uri partUriDocument2 = new Uri(documentPath2, UriKind.Relative);
                    // following will not throw - just fails silently
                    package.DeletePart(partUriDocument2);
                }
            }
        }

        [Fact]
        public void T164_InvalidPartUri()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);

            var documentPath2 = "/abc.xaml";
            Uri partUriDocument2 = new Uri(documentPath2, UriKind.Relative);

            var documentPath = "/abc.xaml/new.xaml";
            Uri partUriDocument = new Uri(documentPath, UriKind.Relative);


            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                PackagePart packagePartDocument = null;
                Assert.Throws<InvalidOperationException>(() => {
                    packagePartDocument = package.CreatePart(partUriDocument2, "image/jpeg");
                    packagePartDocument = package.CreatePart(partUriDocument, "image/jpeg");
                });
            }
        }

        [Fact]
        public void T163_FileFormatExceptionUriProperty()
        {
            var docName = "invaliddocpropsct.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
                {
                    try
                    {
                        PackageProperties pp = package.PackageProperties;
                    }
                    catch (FileFormatException ffe)
                    {
                        var u = ffe.SourceUri;
                    }
                }
            }
        }

        [Fact]
        public void T162_ParameterWithUnquotedQuote()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                PackagePart packagePartDocument = null;
                AssertExtensions.Throws<ArgumentException>(null, () => { packagePartDocument = package.CreatePart(partUriDocument, "image/jpeg; prop=\"\u0022\""); });
            }
        }

        [Fact]
        public void T161_ParameterWithInvalidChar()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                PackagePart packagePartDocument = null;
                AssertExtensions.Throws<ArgumentException>(null, () => { packagePartDocument = package.CreatePart(partUriDocument, "image/jpeg; prop=\"\u0001value2\""); });
            }
        }

        [Fact]
        public void T160_InvalidContentType()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                PackagePart packagePartDocument = null;
                AssertExtensions.Throws<ArgumentException>(null, () => { packagePartDocument = package.CreatePart(partUriDocument, "/"); });
            }
        }

        [Fact]
        public void T159_EmptyParameter()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                PackagePart packagePartDocument = null;
                AssertExtensions.Throws<ArgumentException>(null, () => { packagePartDocument = package.CreatePart(partUriDocument, "image/jpeg; prop=;"); });
            }
        }

        [Fact]
        public void T158_ParameterWithInvalidChar()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                PackagePart packagePartDocument = null;
                AssertExtensions.Throws<ArgumentException>(null, () => { packagePartDocument = package.CreatePart(partUriDocument, "image/jpeg; prop=\"   value   \"    ; prop2=\"\u0001value2"); });
            }
        }

        [Fact]
        public void T157_ParameterWithWhiteSpace()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                PackagePart packagePartDocument = null;
                packagePartDocument = package.CreatePart(partUriDocument, "image/jpeg; prop=\"   value   \"    ; prop2=value2");
            }
        }

        [Fact]
        public void T156_CreatePart_ContentTypeWithInvalidSubtype()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                PackagePart packagePartDocument = null;
                AssertExtensions.Throws<ArgumentException>(null, () => { packagePartDocument = package.CreatePart(partUriDocument, "image\r\njpeg"); });
            }
        }


        [Fact]
        public void T155_BadDateTimeDocProps()
        {
            var docName = "BadDocProps.docx";
            var ba = File.ReadAllBytes(docName);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open, FileAccess.ReadWrite))
                {
                    Assert.Throws<XmlException>(() =>
                        {
                            PackageProperties pp = package.PackageProperties;
                            DateTime? c = pp.Created;
                            DateTime? m = pp.Modified;
                        });
                }
            }
        }

        [Fact]
        public void T154_CreatedModifiedDocProps()
        {
            var docName = "MoreDocProps.docx";
            var ba = File.ReadAllBytes(docName);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
                {
                    PackageProperties pp = package.PackageProperties;
                    DateTime? c = pp.Created;
                    DateTime? m = pp.Modified;
                }
            }
        }

        [Fact]
        public void T153_NoDocProps()
        {
            var docName = "NoDocProps.docx";
            var ba = File.ReadAllBytes(docName);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
                {
                    PackageProperties pp = package.PackageProperties;
                    pp.Category = "foo";
                }
            }
        }

        [Fact]
        public void T152_InvalidDocProps()
        {
            var docName = "invaliddocpropsct.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open, FileAccess.ReadWrite))
                {
                    Assert.Throws<FileFormatException>(() =>
                    {
                        PackageProperties pp = package.PackageProperties;
                    });
                }
            }
        }

        [Fact]
        public void T151_InvalidDocProps()
        {
            var docName = "invaliddocprops.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open, FileAccess.ReadWrite))
                {
                    Assert.Throws<FileFormatException>(() =>
                    {
                        PackageProperties pp = package.PackageProperties;
                    });
                }
            }
        }

        [Fact]
        public void T150_CreatePart_InvalidContentType()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                PackagePart packagePartDocument = null;
                AssertExtensions.Throws<ArgumentException>(null, () =>
                {
                    packagePartDocument = package.CreatePart(partUriDocument, "text/text;foo=\"value\";");
                });
            }
        }

        [Fact]
        public void T149_CreatePart_InvalidContentType()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                PackagePart packagePartDocument = null;
                AssertExtensions.Throws<ArgumentException>(null, () =>
                {
                    packagePartDocument = package.CreatePart(partUriDocument, Mime_MediaTypeNames_Text_Xml + ";\"value\"");
                });
            }
        }

        [Fact]
        public void T148_CreatePart_ContentTypeWithTwoSubtypes()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                PackagePart packagePartDocument = null;
                packagePartDocument = package.CreatePart(partUriDocument, Mime_MediaTypeNames_Text_Xml + ";param1=value1;param2=\"value2\"");
            }
        }

        [Fact]
        public void T147_CreatePart_ContentTypeWithTwoSubtypes()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                PackagePart packagePartDocument = null;
                packagePartDocument = package.CreatePart(partUriDocument, Mime_MediaTypeNames_Text_Xml + "; param1=value1; param2=value2");
            }
        }

        [Fact]
        public void T146_CreatePart_ContentTypeWithInvalidSubtype()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                PackagePart packagePartDocument = null;
                AssertExtensions.Throws<ArgumentException>(null, () => { packagePartDocument = package.CreatePart(partUriDocument, "image\rjpeg"); });
            }
        }

        [Fact]
        public void T145_CreatePart_ContentTypeWithInvalidSubtype()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                PackagePart packagePartDocument = null;
                AssertExtensions.Throws<ArgumentException>(null, () => { packagePartDocument = package.CreatePart(partUriDocument, "imagejpeg;property"); });
            }
        }

        [Fact]
        public void T144_CreatePart_ContentTypeWithInvalidSubtype()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                PackagePart packagePartDocument = null;
                AssertExtensions.Throws<ArgumentException>(null, () => { packagePartDocument = package.CreatePart(partUriDocument, Mime_MediaTypeNames_Text_Xml + ";property"); });
            }
        }

        [Fact]
        public void T143_CreatePart_ContentTypeWithInvalidSubtype()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                PackagePart packagePartDocument = null;
                AssertExtensions.Throws<ArgumentException>(null, () => { packagePartDocument = package.CreatePart(partUriDocument, Mime_MediaTypeNames_Text_Xml + ";"); });
            }
        }

        [Fact]
        public void T142_CreatePart_ContentTypeWithSubtypesDoubleSemicolon()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                PackagePart packagePartDocument = null;
                AssertExtensions.Throws<ArgumentException>(null, () => { packagePartDocument = package.CreatePart(partUriDocument, Mime_MediaTypeNames_Text_Xml + ";; param1=value1"); });
            }
        }

        [Fact]
        public void T141_CreatePart_ContentTypeWithSubtypes()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
                {
                    PackagePart packagePartDocument = null;
                    packagePartDocument = package.CreatePart(partUriDocument, Mime_MediaTypeNames_Text_Xml + "; param1=value1");
                }
            }
        }

        [Fact]
        public void T140_CreatePart_SpaceAtBeginningOfContentType()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                PackagePart packagePartDocument = null;
                AssertExtensions.Throws<ArgumentException>(null, () => packagePartDocument = package.CreatePart(partUriDocument, " " + Mime_MediaTypeNames_Text_Xml));
            }
        }

        [Fact]
        public void T139_Access_Length_Prop_of_Stream()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            using (Package package = Package.Open(tempGuidName.FullName, FileMode.Open, FileAccess.Read))
            {
                PackageRelationship docPackageRelationship =
                  package
                  .GetRelationshipsByType(DocumentRelationshipType)
                  .FirstOrDefault();

                if (docPackageRelationship != null)
                {
                    Uri documentUri =
                        PackUriHelper
                        .ResolvePartUri(
                           new Uri("/", UriKind.Relative),
                                 docPackageRelationship.TargetUri);

                    var mainPart = package.GetPart(documentUri);

                    using (Stream partStream = mainPart.GetStream(FileMode.OpenOrCreate, FileAccess.Read))
                    {
                        var len = partStream.Length;
                        Assert.Equal(len, 2142);
                    }
                }
            }
			tempGuidName.Delete();
        }

        [Fact]
        public void T049_PackUriHelper_GetRelationshipPartUri()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    var rootUri = new Uri("/", UriKind.Relative);
                    var rootRelationshipPartUri = PackUriHelper.GetRelationshipPartUri(rootUri);
                    Assert.Equal(rootRelationshipPartUri.ToString(), "/_rels/.rels");

                    PackageRelationship docPackageRelationship =
                      package
                      .GetRelationshipsByType(DocumentRelationshipType)
                      .FirstOrDefault();

                    if (docPackageRelationship != null)
                    {
                        Uri documentUri =
                            PackUriHelper
                            .ResolvePartUri(
                               new Uri("/", UriKind.Relative),
                                     docPackageRelationship.TargetUri);

                        var docRelationshipPartUri = PackUriHelper.GetRelationshipPartUri(documentUri);
                        Assert.Equal(docRelationshipPartUri.ToString(), "/word/_rels/document.xml.rels");
                    }
                }
            }
        }

        [Fact]
        public void T048_PackUriHelper_GetRelativeUri()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    PackageRelationship docPackageRelationship =
                      package
                      .GetRelationshipsByType(DocumentRelationshipType)
                      .FirstOrDefault();

                    if (docPackageRelationship != null)
                    {
                        Uri documentUri =
                            PackUriHelper
                            .ResolvePartUri(
                               new Uri("/", UriKind.Relative),
                                     docPackageRelationship.TargetUri);

                        Uri stylesUri = new Uri("/word/styles.xml", UriKind.Relative);

                        Assert.Throws<ArgumentNullException>(() => PackUriHelper.GetRelativeUri(null, stylesUri));
                        Assert.Throws<ArgumentNullException>(() => PackUriHelper.GetRelativeUri(documentUri, null));
                        var relativeUri = PackUriHelper.GetRelativeUri(documentUri, stylesUri);
                        Assert.Equal(relativeUri.ToString(), "styles.xml");
                    }
                }
            }
        }

        [Fact]
        public void T047_PackUriHelper_IsRelationshipPartUri()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    int nonRel = 0;
                    int rel = 0;
                    foreach (var part in package.GetParts())
                    {
                        if (PackUriHelper.IsRelationshipPartUri(part.Uri))
                        {
                            rel++;
                            var src = PackUriHelper.GetSourcePartUriFromRelationshipPartUri(part.Uri);
                            var isRootOrMainDoc = src.ToString() == "/word/document.xml" ||
                                src.ToString() == "/";
                            Assert.True(isRootOrMainDoc);
                        }
                        else
                            nonRel++;
                    }
                    Assert.Equal(nonRel, 8);
                    Assert.Equal(rel, 2);
                }
            }
        }

        [Fact]
        public void T046_PackageRelationshipSelectorFromPartUsingId()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    PackageRelationship docPackageRelationship =
                      package
                      .GetRelationshipsByType(DocumentRelationshipType)
                      .FirstOrDefault();

                    if (docPackageRelationship != null)
                    {
                        Uri documentUri =
                            PackUriHelper
                            .ResolvePartUri(
                               new Uri("/", UriKind.Relative),
                                     docPackageRelationship.TargetUri);
                        Assert.Throws<ArgumentNullException>(() => new PackageRelationshipSelector(null, PackageRelationshipSelectorType.Id, "rId1"));
                        Assert.Throws<ArgumentNullException>(() => new PackageRelationshipSelector(documentUri, PackageRelationshipSelectorType.Id, null));
                        PackageRelationshipSelector prs = new PackageRelationshipSelector(documentUri, PackageRelationshipSelectorType.Id, "rId1");
                        var cnt = prs.Select(package).Count();
                        Assert.Equal(cnt, 1);
                    }
                }
            }
        }

        [Fact]
        public void T045_PackageRelationshipSelectorFromPackageUsingId()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    var mainPartUri = new Uri("/", UriKind.Relative);
                    PackageRelationshipSelector prs = new PackageRelationshipSelector(mainPartUri, PackageRelationshipSelectorType.Id, "rId1");
                    var cnt = prs.Select(package).Count();
                    Assert.Equal(cnt, 1);
                }
            }
        }

        [Fact]
        public void T044_PackageRelationshipSelectorFromPackage()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    var mainPartUri = new Uri("/", UriKind.Relative);
                    PackageRelationshipSelector prs = new PackageRelationshipSelector(mainPartUri, PackageRelationshipSelectorType.Type, DocumentRelationshipType);
                    var cnt = prs.Select(package).Count();
                    Assert.Equal(cnt, 1);
                }
            }
        }

        [Fact]
        public void T043_PackageRelationshipSelector()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            const string StylesRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles";

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    var mainPartUri = new Uri("/word/document.xml", UriKind.Relative);
                    PackageRelationshipSelector prs = new PackageRelationshipSelector(mainPartUri, PackageRelationshipSelectorType.Type, StylesRelationshipType);
                    var cnt = prs.Select(package).Count();
                    Assert.Equal(cnt, 1);
                }
            }
        }

        [Fact]
        public void T042_DeletePartRelationship()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    PackageRelationship docPackageRelationship =
                      package
                      .GetRelationshipsByType(DocumentRelationshipType)
                      .FirstOrDefault();

                    Assert.Equal(docPackageRelationship.Package, package);

                    if (docPackageRelationship != null)
                    {
                        Uri documentUri =
                            PackUriHelper
                            .ResolvePartUri(
                               new Uri("/", UriKind.Relative),
                                     docPackageRelationship.TargetUri);
                        PackagePart documentPart =
                            package.GetPart(documentUri);

                        var validCompressionOption = documentPart.CompressionOption == CompressionOption.Normal ||
                            documentPart.CompressionOption == CompressionOption.SuperFast;
                        Assert.True(validCompressionOption);
                        Assert.Equal(documentPart.ContentType, "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml");
                        Assert.NotNull(documentPart.Package);
                        Assert.Equal(documentPart.Uri.ToString(), "/word/document.xml");
                    }
                }
            }
        }

        [Fact]
        public void T041_PartExists()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open, FileAccess.ReadWrite))
                {
                    //var relationships = package
                    //    .GetRelationships();

                    //var cnt = relationships.Count();

                    PackageRelationship docPackageRelationship4 =
                                  package
                                  .GetRelationshipsByType("http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument")
                                  .FirstOrDefault();

                    if (docPackageRelationship4 == null)
                        docPackageRelationship4 =
                            package
                            .GetRelationshipsByType("application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml")
                            .FirstOrDefault();

                    Uri documentUri = PackUriHelper
                        .ResolvePartUri(
                           new Uri("/", UriKind.Relative),
                                 docPackageRelationship4.TargetUri);

                    var mainDocumentPart = package.GetPart(documentUri);
                    Uri documentUri2 = new Uri("/word/document.xml", UriKind.RelativeOrAbsolute);
                    Assert.True(package.PartExists(documentUri));
                    Assert.Equal(package.FileOpenAccess, FileAccess.ReadWrite);
                }
            }
        }

        [Fact]
        public void T040_InvalidRelationshipId()
        {
            var docName = "plain.docx";
            var fiGuidName = GetTempFileInfoFromExistingFile(docName);

            XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            using (Package package = Package.Open(fiGuidName.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            using (PackageProperties pp = package.PackageProperties)
            {
                Assert.Null(pp.Category);
                Assert.Null(pp.ContentStatus);
                Assert.Null(pp.ContentType);
                Assert.Equal(pp.Creator, "Eric White");
                Assert.Equal(pp.Description, "");
                Assert.Null(pp.Identifier);
                Assert.Null(pp.Language);
                Assert.Equal(pp.Subject, "");
                Assert.Equal(pp.Title, "");
                Assert.Null(pp.Version);
            }
            fiGuidName.Delete();
        }

        [Fact]
        public void T039_OpenStreamThreeArgs()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open, FileAccess.ReadWrite))
                {
                    PackageRelationship docPackageRelationship =
                      package
                      .GetRelationshipsByType(DocumentRelationshipType)
                      .FirstOrDefault();

                    var relationshipsBefore = package.GetRelationships().Count();
                    package.DeleteRelationship(docPackageRelationship.Id);
                    var relationshipsAfter = package.GetRelationships().Count();
                    Assert.Equal(relationshipsBefore, relationshipsAfter + 1);
                }
            }
        }

        [Fact]
        public void T038_DisposePackage()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Open);
                PackageProperties pp = package.PackageProperties;
                pp.Category = "TEST-CATEGORY";
                package.Flush();
                ((IDisposable)package).Dispose();
            }
        }

        [Fact]
        public void T037_DeleteRootRelationship()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    PackageRelationship docPackageRelationship =
                      package
                      .GetRelationshipsByType(DocumentRelationshipType)
                      .FirstOrDefault();

                    var relationshipsBefore = package.GetRelationships().Count();
                    package.DeleteRelationship(docPackageRelationship.Id);
                    var relationshipsAfter = package.GetRelationships().Count();
                    Assert.Equal(relationshipsBefore, relationshipsAfter + 1);
                }
            }
        }


        [Fact]
        public void T036_CreateRelationshipWithId()
        {
            var documentPath = "document.xml";
            var resourcePath = "resources.xml";
            var packageRelationshipType = "http://packageRelType";
            var ResourceRelationshipType = "http://resourceRelType";

            var packagePath1 = GetTempFileInfoWithExtension(".docx");
            Uri partUriDocument = PackUriHelper.CreatePartUri(
                                      new Uri(documentPath, UriKind.Relative));
            Uri partUriResource = PackUriHelper.CreatePartUri(
                      new Uri(resourcePath, UriKind.Relative));

            using (Package package = Package.Open(packagePath1.FullName, FileMode.Create))
            {
                Assert.Throws<ArgumentNullException>(
                    () => package.CreatePart(null,
                                   Mime_MediaTypeNames_Text_Xml,
                                   CompressionOption.Normal)
                    );

                // Add the Document part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(partUriDocument,
                                   Mime_MediaTypeNames_Text_Xml,
                                   CompressionOption.Normal);

                // Copy the data to the Document Part
                using (Stream partStream = packagePartDocument.GetStream())
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }

                // Add a Package Relationship to the Document Part
                package.CreateRelationship(packagePartDocument.Uri,
                                           TargetMode.Internal,
                                           packageRelationshipType,
                                           "rId9998");

                // Add a Resource Part to the Package
                PackagePart packagePartResource =
                    package.CreatePart(partUriResource,
                                   Mime_MediaTypeNames_Image_Jpeg,
                                   CompressionOption.Normal);

                // Copy the data to the Resource Part 
                using (Stream partStream = packagePartResource.GetStream())
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_ResourceXml);
                }

                // Add Relationship from the Document part to the Resource part
                packagePartDocument.CreateRelationship(
                                        new Uri(@"../resources/image1.jpg", UriKind.Relative),
                                        TargetMode.Internal,
                                        ResourceRelationshipType,
                                        "rId9999");

            }
            packagePath1.Delete();
        }

        [Fact]
        public void OpenInternalTargetRelationships()
        {
            // This is to test different behavior on Mono vs .NET Core
            using (var ms = new MemoryStream())
            {
                using (var package = Package.Open(ms, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    package.CreateRelationship(new Uri("/target", UriKind.Relative), TargetMode.Internal, "type");
                }

                ms.Position = 0;

                using (var package = Package.Open(ms, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    var relationships = package.GetRelationships();

                    var relationship = Assert.Single(relationships);

                    Assert.Equal(new Uri("/", UriKind.Relative), relationship.SourceUri);
                    Assert.Equal(new Uri("/target", UriKind.Relative), relationship.TargetUri);
                    Assert.Equal(TargetMode.Internal, relationship.TargetMode);
                }
            }
        }

        [Fact]
        public void T035_ModifyAllPackageProperties()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    PackageProperties pp = package.PackageProperties;

                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormatComma("pp.Category: >{0}<", pp.Category);
                    sb.AppendFormatComma("pp.ContentStatus: >{0}<", pp.ContentStatus);
                    sb.AppendFormatComma("pp.ContentType: >{0}<", pp.ContentType);
                    //sb.AppendFormatNL("pp.Created: >{0}<", pp.Created);
                    sb.AppendFormatComma("pp.Creator: >{0}<", pp.Creator);
                    sb.AppendFormatComma("pp.Description: >{0}<", pp.Description);
                    sb.AppendFormatComma("pp.Identifier: >{0}<", pp.Identifier);
                    sb.AppendFormatComma("pp.Keywords: >{0}<", pp.Keywords);
                    sb.AppendFormatComma("pp.Language: >{0}<", pp.Language);
                    sb.AppendFormatComma("pp.LastModifiedBy: >{0}<", pp.LastModifiedBy);
                    sb.AppendFormatComma("pp.LastPrinted: >{0}<", pp.LastPrinted);
                    //sb.AppendFormatNL("pp.Modified: >{0}<", pp.Modified);
                    sb.AppendFormatComma("pp.Revision: >{0}<", pp.Revision);
                    sb.AppendFormatComma("pp.Subject: >{0}<", pp.Subject);
                    sb.AppendFormatComma("pp.Title: >{0}<", pp.Title);
                    sb.AppendFormatComma("pp.Version: >{0}<", pp.Version);

                    string s = sb.ToString();
                    Assert.Equal(sb.ToString().Trim(), @"pp.Category: >(null)<, pp.ContentStatus: >(null)<, pp.ContentType: >(null)<, pp.Creator: >Eric White<, pp.Description: ><, pp.Identifier: >(null)<, pp.Keywords: ><, pp.Language: >(null)<, pp.LastModifiedBy: >Eric White<, pp.LastPrinted: >(null)<, pp.Revision: >2<, pp.Subject: ><, pp.Title: ><, pp.Version: >(null)<,");

                    pp.Category = "Category";
                    pp.ContentStatus = "ContentStatus";
                    pp.ContentType = "ContentType";
                    pp.Created = new DateTime(2015, 4, 27);
                    pp.Creator = "Creator";
                    pp.Description = "Description";
                    pp.Identifier = "Identifier";
                    pp.Keywords = "Keywords";
                    pp.Language = "Language";
                    pp.LastModifiedBy = "LastModifiedBy";
                    pp.LastPrinted = new DateTime(2015, 4, 27);
                    pp.Modified = new DateTime(2015, 4, 27);
                    pp.Revision = "Revision";
                    pp.Subject = "Subject";
                    pp.Title = "Title";
                    pp.Version = "Version";

                    sb = new StringBuilder();
                    sb.AppendFormatComma("pp.Category: >{0}<", pp.Category);
                    sb.AppendFormatComma("pp.ContentStatus: >{0}<", pp.ContentStatus);
                    sb.AppendFormatComma("pp.ContentType: >{0}<", pp.ContentType);
                    sb.AppendFormatComma("pp.Creator: >{0}<", pp.Creator);
                    sb.AppendFormatComma("pp.Description: >{0}<", pp.Description);
                    sb.AppendFormatComma("pp.Identifier: >{0}<", pp.Identifier);
                    sb.AppendFormatComma("pp.Keywords: >{0}<", pp.Keywords);
                    sb.AppendFormatComma("pp.Language: >{0}<", pp.Language);
                    sb.AppendFormatComma("pp.LastModifiedBy: >{0}<", pp.LastModifiedBy);
                    sb.AppendFormatComma("pp.Revision: >{0}<", pp.Revision);
                    sb.AppendFormatComma("pp.Subject: >{0}<", pp.Subject);
                    sb.AppendFormatComma("pp.Title: >{0}<", pp.Title);
                    sb.AppendFormatComma("pp.Version: >{0}<", pp.Version);

                    s = sb.ToString();
                    Assert.Equal(sb.ToString().Trim(), @"pp.Category: >Category<, pp.ContentStatus: >ContentStatus<, pp.ContentType: >ContentType<, pp.Creator: >Creator<, pp.Description: >Description<, pp.Identifier: >Identifier<, pp.Keywords: >Keywords<, pp.Language: >Language<, pp.LastModifiedBy: >LastModifiedBy<, pp.Revision: >Revision<, pp.Subject: >Subject<, pp.Title: >Title<, pp.Version: >Version<,");

                }
            }
        }

        [Fact]
        public void T034_GetPartRelationshipById()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
            const string StylesRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles";

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    PackageRelationship docPackageRelationship =
                      package
                      .GetRelationshipsByType(DocumentRelationshipType)
                      .FirstOrDefault();

                    if (docPackageRelationship != null)
                    {
                        Uri documentUri =
                            PackUriHelper
                            .ResolvePartUri(
                               new Uri("/", UriKind.Relative),
                                     docPackageRelationship.TargetUri);
                        PackagePart documentPart =
                            package.GetPart(documentUri);

                        //  Find the styles part. There will only be one.
                        PackageRelationship styleRelation =
                          documentPart.GetRelationshipsByType(StylesRelationshipType)
                          .FirstOrDefault();

                        Assert.NotNull(styleRelation);
                        var styleRelation2 = documentPart.GetRelationship(styleRelation.Id);
                        Assert.Equal(styleRelation.SourceUri.ToString(), styleRelation2.SourceUri.ToString());
                        Assert.Equal(styleRelation.TargetUri.ToString(), styleRelation2.TargetUri.ToString());
                    }
                }
            }
        }

        [Fact]
        public void T033_GetStreamOverload()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);

            XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    PackageRelationship docPackageRelationship4 =
                                  package
                                  .GetRelationshipsByType("http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument")
                                  .FirstOrDefault();

                    Uri documentUri = PackUriHelper
                        .ResolvePartUri(
                           new Uri("/", UriKind.Relative),
                                 docPackageRelationship4.TargetUri);

                    var mainDocumentPart = package.GetPart(documentUri);
                    XDocument xdMain = null;
                    using (var partStream = mainDocumentPart.GetStream(FileMode.Open))
                    {
                        xdMain = XDocument.Load(partStream);
                        var lastPara = xdMain
                            .Root
                            .Elements(W + "body")
                            .Elements(W + "p")
                            .LastOrDefault();
                        lastPara.AddAfterSelf(
                            new XElement(W + "p",
                                new XElement(W + "r",
                                    new XElement(W + "t", "Hello again"))));
                    }

                    using (var partStream = mainDocumentPart.GetStream(FileMode.Open, FileAccess.ReadWrite))
                    {
                        xdMain.Save(partStream);
                    }
                }
            }
        }

        [Fact]
        public void T032_DeletePartRelationship()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
            const string StylesRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles";

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    PackageRelationship docPackageRelationship =
                      package
                      .GetRelationshipsByType(DocumentRelationshipType)
                      .FirstOrDefault();

                    if (docPackageRelationship != null)
                    {
                        Uri documentUri =
                            PackUriHelper
                            .ResolvePartUri(
                               new Uri("/", UriKind.Relative),
                                     docPackageRelationship.TargetUri);
                        PackagePart documentPart =
                            package.GetPart(documentUri);

                        //  Find the styles part. There will only be one.
                        PackageRelationship styleRelation =
                          documentPart.GetRelationshipsByType(StylesRelationshipType)
                          .FirstOrDefault();

                        int partsBefore = package.GetParts().Count();
                        var nonRelationshipParts = package.GetParts().Cast<ZipPackagePart>().Where(p => p.ContentType != "application/vnd.openxmlformats-package.relationships+xml");
                        int relationshipsBefore = nonRelationshipParts.Select(p => p.GetRelationships().Count()).Sum();

                        Assert.True(documentPart.RelationshipExists(styleRelation.Id));
                        documentPart.DeleteRelationship(styleRelation.Id);
                        Assert.False(documentPart.RelationshipExists(styleRelation.Id));

                        int partsAfter = package.GetParts().Count();
                        int relationshipsAfter = nonRelationshipParts.Select(p => p.GetRelationships().Count()).Sum();

                        Assert.Equal(partsBefore, partsAfter);
                        Assert.Equal(relationshipsBefore, relationshipsAfter + 1);
                    }
                }
            }
        }

        [Fact]
        public void T031_DeleteMainDocumentPart()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    PackageRelationship docPackageRelationship = package
                        .GetRelationshipsByType(DocumentRelationshipType)
                        .FirstOrDefault();

                    if (docPackageRelationship != null)
                    {
                        Uri documentUri =
                            PackUriHelper
                            .ResolvePartUri(
                               new Uri("/", UriKind.Relative),
                                     docPackageRelationship.TargetUri);

                        int partsBefore = package.GetParts().Count();
                        int relationshipsBefore = package.GetRelationships().Count();

                        PackagePart documentPart =
                            package.GetPart(documentUri);
                        package.DeletePart(documentPart.Uri);

                        int partsAfter = package.GetParts().Count();
                        int relationshipsAfter = package.GetRelationships().Count();
                        Assert.Equal(partsBefore, partsAfter + 2);
                        Assert.Equal(relationshipsBefore, relationshipsAfter);
                    }
                }
            }
        }

        [Fact]
        public void T030_GetRootRelationshipById()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    PackageRelationship docPackageRelationship =
                      package
                      .GetRelationshipsByType(DocumentRelationshipType)
                      .FirstOrDefault();

                    Assert.True(package.RelationshipExists(docPackageRelationship.Id));
                    var docPackageRelationship2 = package.GetRelationship(docPackageRelationship.Id);
                    Assert.Equal(docPackageRelationship.SourceUri.ToString(), docPackageRelationship2.SourceUri.ToString());
                    Assert.Equal(docPackageRelationship.TargetUri.ToString(), docPackageRelationship2.TargetUri.ToString());
                }
            }
        }

        [Fact]
        public void T029_DeletePackageRelationship()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    PackageRelationship docPackageRelationship =
                      package
                      .GetRelationshipsByType(DocumentRelationshipType)
                      .FirstOrDefault();

                    StringBuilder sb = new StringBuilder();
                    int cnt = 0;
                    foreach (var part in package.GetParts())
                    {
                        sb.Append(String.Format("#{0}" + NL, cnt++));
                        sb.Append(String.Format("Uri: {0}" + NL, part.Uri));
                        sb.Append(String.Format("ContentType: {0}" + NL, part.ContentType));
                    }
                    string s = sb.ToString().Replace(NL, "~");
                    string other = @"#0~Uri: /docProps/app.xml~ContentType: application/vnd.openxmlformats-officedocument.extended-properties+xml~#1~Uri: /docProps/core.xml~ContentType: application/vnd.openxmlformats-package.core-properties+xml~#2~Uri: /word/document.xml~ContentType: application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml~#3~Uri: /word/fontTable.xml~ContentType: application/vnd.openxmlformats-officedocument.wordprocessingml.fontTable+xml~#4~Uri: /word/settings.xml~ContentType: application/vnd.openxmlformats-officedocument.wordprocessingml.settings+xml~#5~Uri: /word/styles.xml~ContentType: application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml~#6~Uri: /word/theme/theme1.xml~ContentType: application/vnd.openxmlformats-officedocument.theme+xml~#7~Uri: /word/webSettings.xml~ContentType: application/vnd.openxmlformats-officedocument.wordprocessingml.webSettings+xml~#8~Uri: /word/_rels/document.xml.rels~ContentType: application/vnd.openxmlformats-package.relationships+xml~#9~Uri: /_rels/.rels~ContentType: application/vnd.openxmlformats-package.relationships+xml~";
                    Assert.Equal(s, other);

                    int relationshipsBefore = package.GetRelationships().Count();
                    package.DeleteRelationship(docPackageRelationship.Id);
                    int relationshipsAfter = package.GetRelationships().Count();
                    Assert.Equal(relationshipsBefore, relationshipsAfter + 1);
                }
            }
        }

        [Fact]
        public void T028_ModifyPackagePropertiesAndFlush()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Open);
                PackageProperties pp = package.PackageProperties;
                pp.Category = "TEST-CATEGORY";
                package.Flush();
                package.Close();
            }
        }

        [Fact]
        public void T027_ModifyPackageProperties()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open))
                {
                    PackageProperties pp = package.PackageProperties;
                    pp.Category = "TEST-CATEGORY";
                }
            }
        }

        [Fact]
        public void T026_PackageFlush()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                Uri stylesUri = new Uri("/_rels/.rels", UriKind.Relative);
                package.DeletePart(stylesUri);
                package.Flush();
                package.Close();
            }
        }


        [Fact]
        public void T025_PackageClose()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
                Uri stylesUri = new Uri("/_rels/.rels", UriKind.Relative);
                package.DeletePart(stylesUri);
                package.Close();
            }
        }

        [Fact]
        public void T024_DeleteRootRelsPart()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
                {
                    Uri stylesUri = new Uri("/_rels/.rels", UriKind.Relative);
                    package.DeletePart(stylesUri);
                }
            }
        }

        [Fact]
        public void T023_DeleteRelsPart()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
                {
                    Uri stylesUri = new Uri("/word/_rels/document.xml.rels", UriKind.Relative);
                    package.DeletePart(stylesUri);
                }
            }
        }

        [Fact]
        public void T022_DeletePartInvalidArgument()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
                {
                    Uri documentUri = new Uri("/word/document.xml", UriKind.Relative);
                    Assert.Throws<ArgumentNullException>(() => package.DeletePart(null));
                }
            }
        }


        [Fact]
        public void T021_DeletePartThatContainsRelationships()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open, FileAccess.ReadWrite))
                {
                    Uri documentUri = new Uri("/word/document.xml", UriKind.Relative);
                    package.DeletePart(documentUri);
                }
            }
        }

        [Fact]
        public void T020_GetNonExistentPart()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open, FileAccess.ReadWrite))
                {
                    PackageRelationship docPackageRelationship =
                        package.GetRelationshipsByType(DocumentRelationshipType).FirstOrDefault();
                    Assert.NotNull(docPackageRelationship);

                    Uri documentUri = new Uri("/worddummy/document.xml", UriKind.Relative);
                    Assert.Throws<InvalidOperationException>(() => package.GetPart(documentUri));
                }
            }
        }

        [Fact]
        public void T019_GetPartFromRelativeUri()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open, FileAccess.ReadWrite))
                {
                    PackageRelationship docPackageRelationship =
                        package.GetRelationshipsByType(DocumentRelationshipType).FirstOrDefault();
                    Assert.NotNull(docPackageRelationship);

                    Uri documentUri = new Uri("/word/document.xml", UriKind.Relative);
                    PackagePart documentPart = package.GetPart(documentUri);

                    //  Load the document XML in the part into an XDocument instance.
                    var xDoc = XDocument.Load(XmlReader.Create(documentPart.GetStream()));
                    Assert.NotNull(xDoc);
                }
            }
        }

        [Fact]
        public void T018_GetMainDocPartAndStyleDefPart()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
            const string StylesRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles";

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open, FileAccess.ReadWrite))
                {
                    PackageRelationship docPackageRelationship =
                                  package
                                  .GetRelationshipsByType(DocumentRelationshipType)
                                  .FirstOrDefault();

                    Assert.NotNull(docPackageRelationship);
                    Uri documentUri =
                        PackUriHelper
                        .ResolvePartUri(
                           new Uri("/", UriKind.Relative),
                                 docPackageRelationship.TargetUri);
                    PackagePart documentPart =
                        package.GetPart(documentUri);

                    //  Load the document XML in the part into an XDocument instance.
                    var xDoc = XDocument.Load(XmlReader.Create(documentPart.GetStream()));

                    //  Find the styles part. There will only be one.
                    PackageRelationship styleRelation =
                      documentPart.GetRelationshipsByType(StylesRelationshipType)
                      .FirstOrDefault();
                    Assert.NotNull(styleRelation);

                    Uri styleUri = PackUriHelper.ResolvePartUri(documentUri, styleRelation.TargetUri);
                    PackagePart stylePart = package.GetPart(styleUri);

                    //  Load the style XML in the part into an XDocument instance.
                    var styleDoc = XDocument.Load(XmlReader.Create(stylePart.GetStream()));
                }
            }
        }

        [Fact]
        public void T017_CreatePartTwice()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));
            var packageRelationshipType = "http://packageRelType";

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
                {
                    PackagePart packagePartDocument = package.CreatePart(partUriDocument, Mime_MediaTypeNames_Text_Xml);

                    // Copy the data to the Document Part
                    using (Stream partStream = packagePartDocument.GetStream())
                    using (StreamWriter sw = new StreamWriter(partStream))
                    {
                        sw.Write(s_DocumentXml);
                    }

                    // Add a Package Relationship to the Document Part
                    package.CreateRelationship(packagePartDocument.Uri,
                                                TargetMode.Internal,
                                                packageRelationshipType);

                    // do it again
                    Assert.Throws<InvalidOperationException>(() => package.CreatePart(partUriDocument, Mime_MediaTypeNames_Text_Xml));
                }
            }
        }

        [Fact]
        public void T016_CreatePartInvalidArg()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);
            var documentPath = "document.xml";
            Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
                {
                    PackagePart packagePartDocument = null;
                    Assert.Throws<ArgumentNullException>(() => packagePartDocument = package.CreatePart(null, Mime_MediaTypeNames_Text_Xml));
                    Assert.Throws<ArgumentNullException>(() => packagePartDocument = package.CreatePart(partUriDocument, null));
                }
            }
        }

        [Fact]
        public void T015_CreatePart()
        {
            var docName = "plain.docx";
            var fiGuidName = GetTempFileInfoFromExistingFile(docName);
            var documentPath = "document.xml";
            var packageRelationshipType = "http://packageRelType";

            Uri partUriDocument = PackUriHelper.CreatePartUri(
                                      new Uri(documentPath, UriKind.Relative));

            using (Package package = Package.Open(fiGuidName.FullName, FileMode.Create, FileAccess.ReadWrite))
            {
                PackagePart packagePartDocument =
                    package.CreatePart(partUriDocument,
                                    Mime_MediaTypeNames_Text_Xml);

                // Copy the data to the Document Part
                using (Stream partStream = packagePartDocument.GetStream())
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }

                // Add a Package Relationship to the Document Part
                package.CreateRelationship(packagePartDocument.Uri,
                                            TargetMode.Internal,
                                            packageRelationshipType);
            }
            fiGuidName.Delete();
        }

        [Fact]
        public void T014_PackageOpenStream()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.Open, FileAccess.ReadWrite))
                {
                    var partCount = package.GetParts().Count();
                    Assert.Equal(10, partCount);
                }
            }
        }

        [Fact]
        public void T013_PackageOpenStream()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms, FileMode.OpenOrCreate))
                {
                    var partCount = package.GetParts().Count();
                    Assert.Equal(10, partCount);
                }
            }
        }

        [Fact]
        public void T012_PackageOpenStream()
        {
            var docName = "plain.docx";
            var ba = File.ReadAllBytes(docName);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(ba, 0, ba.Length);
                using (Package package = Package.Open(ms))
                {
                    var partCount = package.GetParts().Count();
                    Assert.Equal(10, partCount);
                }
            }
        }

        [Fact]
        public void PackageOpenStream_VerifyDefaultAccess()
        {
            var docName = "plain.docx";
            using (Package package = Package.Open(File.OpenRead(docName)))
            {
                var partCount = package.GetParts().Count();
                Assert.Equal(10, partCount);
            }
        }

        [Fact]
        public void T011_PackageOpen()
        {
            var docName = "plain.docx";
            var fiGuidName = GetTempFileInfoFromExistingFile(docName);
            var documentPath = "document.xml";

            Uri partUriDocument = PackUriHelper.CreatePartUri(
                                      new Uri(documentPath, UriKind.Relative));

            using (Package package = Package.Open(fiGuidName.FullName, FileMode.Open, FileAccess.Read))
            {
                Assert.Throws<IOException>(() =>
                    package.CreatePart(partUriDocument, Mime_MediaTypeNames_Text_Xml, CompressionOption.Normal));
            }
            fiGuidName.Delete();
        }

        [Fact]
        public void T010_PackageOpen()
        {
            var docName = "plain.docx";
            var fiGuidName = GetTempFileInfoFromExistingFile(docName);

            using (Package package = Package.Open(fiGuidName.FullName, FileMode.Open, FileAccess.Read))
            {
                var partCount = package.GetParts().Count();
                Assert.Equal(10, partCount);
            }
            fiGuidName.Delete();
        }

        [Fact]
        public void T009_PackageOpen()
        {
            var docName = "plain.docx";
            var fiGuidName = GetTempFileInfoFromExistingFile(docName);

            using (Package package = Package.Open(fiGuidName.FullName))
            {
                var partCount = package.GetParts().Count();
                Assert.Equal(10, partCount);
            }
            fiGuidName.Delete();
        }

        [Fact]
        public void T008_PackageProperties()
        {
            var docName = "docprops.docx";
            var fiGuidName = GetTempFileInfoFromExistingFile(docName);

            XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            using (Package package = Package.Open(fiGuidName.FullName, FileMode.Open))
            using (PackageProperties pp = package.PackageProperties)
            {
                Assert.Equal(pp.Category, "Test-Category");
                Assert.Equal(pp.ContentStatus, "Test-Status");
                Assert.Null(pp.ContentType);
                Assert.Equal(pp.Creator, "Eric White");
                Assert.Equal(pp.Description, "Test-Comments");
                Assert.Null(pp.Identifier);
                Assert.Null(pp.Language);
                Assert.Equal(pp.Subject, "Test-Subject");
                Assert.Equal(pp.Title, "Test-Title");
                Assert.Null(pp.Version);
            }
            fiGuidName.Delete();
        }

        [Fact]
        public void T007_PackageProperties()
        {
            var docName = "plain.docx";
            var fiGuidName = GetTempFileInfoFromExistingFile(docName);

            XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            using (Package package = Package.Open(fiGuidName.FullName, FileMode.Open))
            using (PackageProperties pp = package.PackageProperties)
            {
                Assert.Null(pp.Category);
                Assert.Null(pp.ContentStatus);
                Assert.Null(pp.ContentType);
                Assert.Equal(pp.Creator, "Eric White");
                Assert.Equal(pp.Description, "");
                Assert.Null(pp.Identifier);
                Assert.Null(pp.Language);
                Assert.Equal(pp.Subject, "");
                Assert.Equal(pp.Title, "");
                Assert.Null(pp.Version);
            }
            fiGuidName.Delete();
        }

        [Fact]
        public void T006_CreateExternalRelationshipInvalidUri()
        {
            var documentPath = "document.xml";
            var resourcePath = "resources.xml";

            var packageRelationshipType = "http://packageRelType";
            var ResourceRelationshipType = "http://resourceRelType";

            FileInfo packagePath2 = null;
            try
            {
                packagePath2 = GetTempFileInfoWithExtension(".docx");
                Uri partUriDocument2 = PackUriHelper.CreatePartUri(
                                          new Uri(documentPath, UriKind.Relative));
                Uri partUriResource2 = PackUriHelper.CreatePartUri(
                          new Uri(resourcePath, UriKind.Relative));

                using (Package package = Package.Open(packagePath2.FullName, FileMode.Create))
                {
                    // Add the Document part to the Package
                    PackagePart packagePartDocument =
                        package.CreatePart(partUriDocument2,
                                       Mime_MediaTypeNames_Text_Xml,
                                       CompressionOption.Normal);

                    // Copy the data to the Document Part
                    var fiDocumentPath = GetTempFileInfoWithExtension(".xml");
                    File.WriteAllText(fiDocumentPath.FullName, s_DocumentXml);
                    using (FileStream fileStream = new FileStream(fiDocumentPath.FullName, FileMode.Open, FileAccess.Read))
                    using (Stream partStream = packagePartDocument.GetStream())
                    {
                        CopyStream(fileStream, partStream);
                    }
                    fiDocumentPath.Delete();

                    // Add a Package Relationship to the Document Part
                    package.CreateRelationship(packagePartDocument.Uri,
                                               TargetMode.Internal,
                                               packageRelationshipType);

                    Uri uri = new Uri(@"c:/resources/image1.jpg", UriKind.Absolute);

                    // Internal relationships cannot use absolute Uris
                    AssertExtensions.Throws<ArgumentException>("targetUri", () => packagePartDocument.CreateRelationship(uri,
                                                                TargetMode.Internal,
                                                                ResourceRelationshipType));
                }
            }
            finally
            {
                if (packagePath2 != null && packagePath2.Exists)
                    packagePath2.Delete();
            }
        }

        [Fact]
        public void T005_CreateInternalRelationship()
        {
            var documentPath = "document.xml";
            var resourcePath = "resources.xml";
            var packageRelationshipType = "http://packageRelType";
            var ResourceRelationshipType = "http://resourceRelType";

            var packagePath1 = GetTempFileInfoWithExtension(".docx");
            Uri partUriDocument = PackUriHelper.CreatePartUri(
                                      new Uri(documentPath, UriKind.Relative));
            Uri partUriResource = PackUriHelper.CreatePartUri(
                      new Uri(resourcePath, UriKind.Relative));

            using (Package package = Package.Open(packagePath1.FullName, FileMode.Create))
            {
                // Add the Document part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(partUriDocument,
                                   Mime_MediaTypeNames_Text_Xml,
                                   CompressionOption.Normal);

                // Copy the data to the Document Part
                using (Stream partStream = packagePartDocument.GetStream())
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }

                // Add a Package Relationship to the Document Part
                package.CreateRelationship(packagePartDocument.Uri,
                                           TargetMode.Internal,
                                           packageRelationshipType);

                // Add a Resource Part to the Package
                PackagePart packagePartResource =
                    package.CreatePart(partUriResource,
                                   Mime_MediaTypeNames_Image_Jpeg,
                                   CompressionOption.Normal);

                // Copy the data to the Resource Part 
                using (Stream partStream = packagePartResource.GetStream())
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_ResourceXml);
                }

                // Add Relationship from the Document part to the Resource part
                packagePartDocument.CreateRelationship(
                                        new Uri(@"../resources/image1.jpg",
                                        UriKind.Relative),
                                        TargetMode.Internal,
                                        ResourceRelationshipType);
            }
            packagePath1.Delete();
        }

        [Fact]
        public void T004_CreateInternalRelationship()
        {
            var documentPath = "document.xml";
            var resourcePath = "resources.xml";
            var packageRelationshipType = "http://packageRelType";
            var ResourceRelationshipType = "http://resourceRelType";

            var packagePath1 = GetTempFileInfoWithExtension(".docx");
            Uri partUriDocument = PackUriHelper.CreatePartUri(
                                      new Uri(documentPath, UriKind.Relative));
            Uri partUriResource = PackUriHelper.CreatePartUri(
                      new Uri(resourcePath, UriKind.Relative));

            using (Package package = Package.Open(packagePath1.FullName, FileMode.Create))
            {
                // Add the Document part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(partUriDocument,
                                   Mime_MediaTypeNames_Text_Xml,
                                   CompressionOption.Normal);

                // Copy the data to the Document Part
                var fiDocumentPath = GetTempFileInfoWithExtension(".xml");
                File.WriteAllText(fiDocumentPath.FullName, s_DocumentXml);
                using (FileStream fileStream = new FileStream(fiDocumentPath.FullName, FileMode.Open, FileAccess.Read))
                using (Stream partStream = packagePartDocument.GetStream())
                {
                    CopyStream(fileStream, partStream);
                }
                fiDocumentPath.Delete();

                // Add a Package Relationship to the Document Part
                package.CreateRelationship(packagePartDocument.Uri,
                                           TargetMode.Internal,
                                           packageRelationshipType);

                // Add a Resource Part to the Package
                PackagePart packagePartResource =
                    package.CreatePart(partUriResource,
                                   Mime_MediaTypeNames_Image_Jpeg,
                                   CompressionOption.Normal);

                // Copy the data to the Resource Part 
                fiDocumentPath = GetTempFileInfoWithExtension(".xml");
                File.WriteAllText(fiDocumentPath.FullName, s_DocumentXml);
                using (FileStream fileStream = new FileStream(fiDocumentPath.FullName, FileMode.Open, FileAccess.Read))
                using (Stream partStream = packagePartResource.GetStream())
                {
                    CopyStream(fileStream, partStream);
                }
                fiDocumentPath.Delete();

                // Add Relationship from the Document part to the Resource part
                packagePartDocument.CreateRelationship(
                                        new Uri(@"../resources/image1.jpg",
                                        UriKind.Relative),
                                        TargetMode.Internal,
                                        ResourceRelationshipType);
            }
            packagePath1.Delete();
        }

        [Fact]
        public void T003_CreateExternalRelationship()
        {
            var documentPath = "document.xml";
            var resourcePath = "resources.xml";

            var packageRelationshipType = "http://packageRelType";
            var ResourceRelationshipType = "http://resourceRelType";

            var packagePath2 = $"{GetTestFilePath()}.docx";
            Uri partUriDocument2 = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));
            Uri partUriResource2 = PackUriHelper.CreatePartUri(new Uri(resourcePath, UriKind.Relative));

            using (Package package = Package.Open(packagePath2, FileMode.Create))
            {
                // Add the Document part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(partUriDocument2,
                                   Mime_MediaTypeNames_Text_Xml,
                                   CompressionOption.Normal);

                // Copy the data to the Document Part
                var fiDocumentPath = GetTempFileInfoWithExtension(".xml");
                File.WriteAllText(fiDocumentPath.FullName, s_DocumentXml);
                using (FileStream fileStream = new FileStream(fiDocumentPath.FullName, FileMode.Open, FileAccess.Read))
                using (Stream partStream = packagePartDocument.GetStream())
                {
                    CopyStream(fileStream, partStream);
                }
                fiDocumentPath.Delete();

                // Add a Package Relationship to the Document Part
                package.CreateRelationship(packagePartDocument.Uri,
                                           TargetMode.Internal,
                                           packageRelationshipType);

                // Add external relationship
                packagePartDocument.CreateRelationship(
                                        new Uri(@"c:/resources/image1.jpg",
                                        UriKind.Absolute),
                                        TargetMode.External,
                                        ResourceRelationshipType);
            }

        }

        private static void CopyStream(Stream source, Stream target)
        {
            const int BufSize = 0x4096;
            byte[] buf = new byte[BufSize];
            int bytesRead = 0;
            while ((bytesRead = source.Read(buf, 0, BufSize)) > 0)
                target.Write(buf, 0, bytesRead);
        }


        [Fact]
        public void T001_AddParagraphToDocument()
        {
            var docName = "plain.docx";
            var fiGuidName = GetTempFileInfoFromExistingFile(docName);

            XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            using (Package package = Package.Open(fiGuidName.FullName, FileMode.Open))
            {
                PackageRelationship docPackageRelationship4 =
                              package
                              .GetRelationshipsByType("http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument")
                              .FirstOrDefault();

                Uri documentUri = PackUriHelper
                    .ResolvePartUri(
                       new Uri("/", UriKind.Relative),
                             docPackageRelationship4.TargetUri);

                var mainDocumentPart = package.GetPart(documentUri);
                XDocument xdMain = null;
                using (var partStream = mainDocumentPart.GetStream())
                {
                    xdMain = XDocument.Load(partStream);
                    var lastPara = xdMain
                        .Root
                        .Elements(W + "body")
                        .Elements(W + "p")
                        .LastOrDefault();
                    lastPara.AddAfterSelf(
                        new XElement(W + "p",
                            new XElement(W + "r",
                                new XElement(W + "t", "Hello again"))));
                }
                using (var partStream = mainDocumentPart.GetStream(FileMode.Open, FileAccess.ReadWrite))
                {
                    xdMain.Save(partStream);
                }
            }

            fiGuidName.Delete();
        }

        [Fact]
        public void T002_IterateParts()
        {
            var docName = "plain.docx";
            var fiGuidName = GetTempFileInfoFromExistingFile(docName);

            using (Package package = Package.Open(fiGuidName.FullName, FileMode.Open))
            {
                var parts = package.GetParts();
                var numberOfParts = parts.Count();
                Assert.Equal(numberOfParts, 10);
                long sumLen = 0;
                foreach (var part in parts)
                {
                    long len = 0;
                    using (var partStream = part.GetStream())
                    {
                        len = partStream.Length;
                    }
                    sumLen += len;
                }
                Assert.Equal(sumLen, 44768);
            }
			fiGuidName.Delete();
        }

        private string NL = Environment.NewLine;

        [Fact]
        public void T138_String_Truncate_ReadWrite_OpenOrCreate_Read_Write()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            // Truncate is invalid
            Assert.Throws<NotSupportedException>(() =>
            {
                Package package = Package.Open(tempGuidName.FullName, FileMode.Truncate, FileAccess.ReadWrite);
            });
			tempGuidName.Delete();
        }

        [Fact]
        public void T137_String_Truncate_Write_Star()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            // Truncate is invalid
            Assert.Throws<NotSupportedException>(() =>
            {
                Package package = Package.Open(tempGuidName.FullName, FileMode.Truncate, FileAccess.Write);
            });
			tempGuidName.Delete();
        }

        [Fact]
        public void T136_String_Truncate_Read_Star()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            // Truncate is invalid
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Package package = Package.Open(tempGuidName.FullName, FileMode.Truncate, FileAccess.Read);
            });
			tempGuidName.Delete();
        }

        [Fact]
        public void T135_String_OpenOrCreate_ReadWrite_OpenOrCreate_Read_Write()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            using (Package package = Package.Open(tempGuidName.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                PackageRelationship docPackageRelationship =
                  package
                  .GetRelationshipsByType(DocumentRelationshipType)
                  .FirstOrDefault();

                Assert.NotNull(docPackageRelationship);

                Uri documentUri =
                    PackUriHelper
                    .ResolvePartUri(
                       new Uri("/", UriKind.Relative),
                             docPackageRelationship.TargetUri);

                var mainPart = package.GetPart(documentUri);

                Assert.Throws<IOException>(() =>
                {
                    using (Stream partStream = mainPart.GetStream(FileMode.Create, FileAccess.Read))
                    {
                        XDocument xd = XDocument.Load(partStream);
                        Assert.Equal(0, xd.DescendantNodes().Count());
                    }
                });

                // opening in create mode clears the part, so no data in it.
                Assert.Throws<XmlException>(() =>
                {
                    using (Stream partStream = mainPart.GetStream(FileMode.Create, FileAccess.ReadWrite))
                    {
                        XDocument xd = XDocument.Load(partStream);
                        Assert.Equal(0, xd.DescendantNodes().Count());
                    }
                });

                using (Stream partStream = mainPart.GetStream(FileMode.Create, FileAccess.ReadWrite))
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }
            }
            tempGuidName.Delete();
        }

        [Fact]
        public void T134_String_OpenOrCreate_ReadWrite_OpenOrCreate_Read_Write()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            using (Package package = Package.Open(tempGuidName.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                PackageRelationship docPackageRelationship =
                  package
                  .GetRelationshipsByType(DocumentRelationshipType)
                  .FirstOrDefault();

                Assert.NotNull(docPackageRelationship);

                Uri documentUri =
                    PackUriHelper
                    .ResolvePartUri(
                       new Uri("/", UriKind.Relative),
                             docPackageRelationship.TargetUri);

                var mainPart = package.GetPart(documentUri);

                using (Stream partStream = mainPart.GetStream(FileMode.OpenOrCreate, FileAccess.Read))
                {
                    XDocument xd = XDocument.Load(partStream);
                    Assert.Equal(13, xd.DescendantNodes().Count());
                }

                using (Stream partStream = mainPart.GetStream(FileMode.OpenOrCreate, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }
            }

            tempGuidName.Delete();
        }

        [Fact]
        public void T133_String_OpenOrCreate_ReadWrite_Star()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            using (Package package = Package.Open(tempGuidName.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                PackageRelationship docPackageRelationship =
                  package
                  .GetRelationshipsByType(DocumentRelationshipType)
                  .FirstOrDefault();

                Assert.NotNull(docPackageRelationship);

                Uri documentUri =
                    PackUriHelper
                    .ResolvePartUri(
                       new Uri("/", UriKind.Relative),
                             docPackageRelationship.TargetUri);

                var mainPart = package.GetPart(documentUri);

                using (Stream partStream = mainPart.GetStream(FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    XDocument xd = XDocument.Load(partStream);
                    Assert.Equal(13, xd.DescendantNodes().Count());
                }

                using (Stream partStream = mainPart.GetStream(FileMode.Create, FileAccess.ReadWrite))
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }
            }

            tempGuidName.Delete();
        }

        [Fact]
        public void T132_String_OpenOrCreate_ReadWrite_Star()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            using (Package package = Package.Open(tempGuidName.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                PackageRelationship docPackageRelationship =
                  package
                  .GetRelationshipsByType(DocumentRelationshipType)
                  .FirstOrDefault();

                Assert.NotNull(docPackageRelationship);

                Uri documentUri =
                    PackUriHelper
                    .ResolvePartUri(
                       new Uri("/", UriKind.Relative),
                             docPackageRelationship.TargetUri);

                var mainPart = package.GetPart(documentUri);

                using (Stream partStream = mainPart.GetStream(FileMode.Open, FileAccess.ReadWrite))
                {
                    XDocument xd = XDocument.Load(partStream);
                    Assert.Equal(13, xd.DescendantNodes().Count());
                }

                using (Stream partStream = mainPart.GetStream(FileMode.Open, FileAccess.ReadWrite))
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }
            }

            tempGuidName.Delete();
        }

        [Fact]
        public void T131_String_OpenOrCreate_ReadWrite_Star()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            using (Package package = Package.Open(tempGuidName.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                PackageRelationship docPackageRelationship =
                  package
                  .GetRelationshipsByType(DocumentRelationshipType)
                  .FirstOrDefault();

                Assert.NotNull(docPackageRelationship);
                Uri documentUri =
                    PackUriHelper
                    .ResolvePartUri(
                       new Uri("/", UriKind.Relative),
                             docPackageRelationship.TargetUri);

                var mainPart = package.GetPart(documentUri);

                using (Stream partStream = mainPart.GetStream(FileMode.Open, FileAccess.Read))
                {
                    XDocument xd = XDocument.Load(partStream);
                    Assert.Equal(13, xd.DescendantNodes().Count());
                }

                using (Stream partStream = mainPart.GetStream(FileMode.Open, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }
            }

            tempGuidName.Delete();
        }

        [Fact]
        public void T130_String_OpenOrCreate_Write_Star()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Package package = Package.Open(tempGuidName.FullName, FileMode.OpenOrCreate, FileAccess.Write);
            });
			tempGuidName.Delete();
        }

        [Fact]
        public void T129_String_OpenOrCreate_Read_Star()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Package package = Package.Open(tempGuidName.FullName, FileMode.OpenOrCreate, FileAccess.Read);
            });
			tempGuidName.Delete();
        }

        [Fact]
        public void T128_String_Open_ReadWrite_OpenOrCreate_Read_Write_ReadWrite()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            using (Package package = Package.Open(tempGuidName.FullName, FileMode.Open, FileAccess.ReadWrite))
            {
                PackageRelationship docPackageRelationship =
                  package
                  .GetRelationshipsByType(DocumentRelationshipType)
                  .FirstOrDefault();

                Assert.NotNull(docPackageRelationship);
                Uri documentUri =
                        PackUriHelper
                        .ResolvePartUri(
                           new Uri("/", UriKind.Relative),
                                 docPackageRelationship.TargetUri);

                var mainPart = package.GetPart(documentUri);

                using (Stream partStream = mainPart.GetStream(FileMode.OpenOrCreate, FileAccess.Read))
                {
                    XDocument xd = XDocument.Load(partStream);
                    Assert.Equal(13, xd.DescendantNodes().Count());
                }

                using (Stream partStream = mainPart.GetStream(FileMode.OpenOrCreate, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }

                Assert.Throws<XmlException>(() =>
                {
                    using (Stream partStream = mainPart.GetStream(FileMode.OpenOrCreate, FileAccess.Read))
                    {
                        XDocument xd = XDocument.Load(partStream);
                        Assert.Equal(0, xd.DescendantNodes().Count());
                    }
                });
            }

            tempGuidName.Delete();
        }

        [Fact]
        public void T127_String_Open_ReadWrite_Open_Read_Write_ReadWrite()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            using (Package package = Package.Open(tempGuidName.FullName, FileMode.Open, FileAccess.ReadWrite))
            {
                PackageRelationship docPackageRelationship =
                  package
                  .GetRelationshipsByType(DocumentRelationshipType)
                  .FirstOrDefault();

                Assert.NotNull(docPackageRelationship);
                Uri documentUri =
                        PackUriHelper
                        .ResolvePartUri(
                           new Uri("/", UriKind.Relative),
                                 docPackageRelationship.TargetUri);

                var mainPart = package.GetPart(documentUri);

                using (Stream partStream = mainPart.GetStream(FileMode.Open, FileAccess.Read))
                {
                    XDocument xd = XDocument.Load(partStream);
                    Assert.Equal(13, xd.DescendantNodes().Count());
                }

                using (Stream partStream = mainPart.GetStream(FileMode.Create, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }

                using (Stream partStream = mainPart.GetStream(FileMode.Open, FileAccess.ReadWrite))
                {
                    XDocument xd = XDocument.Load(partStream);
                    Assert.Equal(2, xd.DescendantNodes().Count());
                }
            }

            tempGuidName.Delete();
        }

        [Fact]
        public void T126_String_Open_Write_Star()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Package package = Package.Open(tempGuidName.FullName, FileMode.Open, FileAccess.Write);
            });
			tempGuidName.Delete();
        }

        [Fact]
        public void T125_String_Open_Read_OpenOrCreate()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            using (Package package = Package.Open(tempGuidName.FullName, FileMode.Open, FileAccess.Read))
            {
                PackageRelationship docPackageRelationship =
                  package
                  .GetRelationshipsByType(DocumentRelationshipType)
                  .FirstOrDefault();

                Assert.NotNull(docPackageRelationship);
                Uri documentUri =
                        PackUriHelper
                        .ResolvePartUri(
                           new Uri("/", UriKind.Relative),
                                 docPackageRelationship.TargetUri);

                var mainPart = package.GetPart(documentUri);

                using (Stream partStream = mainPart.GetStream(FileMode.OpenOrCreate, FileAccess.Read))
                {
                    XDocument xd = XDocument.Load(partStream);
                    Assert.Equal(13, xd.DescendantNodes().Count());
                }
                Assert.Throws<IOException>(() =>
                {
                    using (Stream partStream = mainPart.GetStream(FileMode.OpenOrCreate, FileAccess.Write))
                    using (StreamWriter sw = new StreamWriter(partStream))
                    {
                        sw.Write(s_DocumentXml);
                    }
                });
                Assert.Throws<IOException>(() =>
                {
                    using (Stream partStream = mainPart.GetStream(FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    using (StreamWriter sw = new StreamWriter(partStream))
                    {
                        sw.Write(s_DocumentXml);
                    }
                });
            }

            tempGuidName.Delete();
        }

        [Fact]
        public void T124_String_Open_Read_Create_Write()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            using (Package package = Package.Open(tempGuidName.FullName, FileMode.Open, FileAccess.Read))
            {
                PackageRelationship docPackageRelationship =
                  package
                  .GetRelationshipsByType(DocumentRelationshipType)
                  .FirstOrDefault();

                Assert.NotNull(docPackageRelationship);
                Uri documentUri =
                        PackUriHelper
                        .ResolvePartUri(
                           new Uri("/", UriKind.Relative),
                                 docPackageRelationship.TargetUri);

                var mainPart = package.GetPart(documentUri);

                Assert.Throws<IOException>(() =>
                {
                    using (Stream partStream = mainPart.GetStream(FileMode.Create, FileAccess.Write))
                    using (StreamWriter sw = new StreamWriter(partStream))
                    {
                        sw.Write(s_DocumentXml);
                    }
                });
                Assert.Throws<IOException>(() =>
                {
                    using (Stream partStream = mainPart.GetStream(FileMode.Create, FileAccess.ReadWrite))
                    using (StreamWriter sw = new StreamWriter(partStream))
                    {
                        sw.Write(s_DocumentXml);
                    }
                });
            }

            tempGuidName.Delete();
        }

        [Fact]
        public void T123_String_Open_Read_Create_Star()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            using (Package package = Package.Open(tempGuidName.FullName, FileMode.Open, FileAccess.Read))
            {
                PackageRelationship docPackageRelationship =
                  package
                  .GetRelationshipsByType(DocumentRelationshipType)
                  .FirstOrDefault();

                Assert.NotNull(docPackageRelationship);
                Uri documentUri =
                        PackUriHelper
                        .ResolvePartUri(
                           new Uri("/", UriKind.Relative),
                                 docPackageRelationship.TargetUri);

                var mainPart = package.GetPart(documentUri);

                Assert.Throws<IOException>(() =>
                {
                    using (Stream partStream = mainPart.GetStream(FileMode.Create, FileAccess.Read))
                    using (StreamWriter sw = new StreamWriter(partStream))
                    {
                        sw.Write(s_DocumentXml);
                    }
                });
            }

            tempGuidName.Delete();
        }

        [Fact]
        public void T122_String_Open_Read_Open_ReadWrite()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            using (Package package = Package.Open(tempGuidName.FullName, FileMode.Open, FileAccess.Read))
            {
                PackageRelationship docPackageRelationship =
                  package
                  .GetRelationshipsByType(DocumentRelationshipType)
                  .FirstOrDefault();

                Assert.NotNull(docPackageRelationship);
                Uri documentUri =
                        PackUriHelper
                        .ResolvePartUri(
                           new Uri("/", UriKind.Relative),
                                 docPackageRelationship.TargetUri);

                var mainPart = package.GetPart(documentUri);

                // can't open a part for ReadWrite when the package is open for Read
                Assert.Throws<IOException>(() =>
                {
                    using (Stream partStream = mainPart.GetStream(FileMode.Open, FileAccess.ReadWrite))
                    using (StreamWriter sw = new StreamWriter(partStream))
                    {
                        sw.Write(s_DocumentXml);
                    }
                });
            }

            tempGuidName.Delete();
        }

        [Fact]
        public void T121_String_Open_Read_Open_Write()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            using (Package package = Package.Open(tempGuidName.FullName, FileMode.Open, FileAccess.Read))
            {
                PackageRelationship docPackageRelationship =
                  package
                  .GetRelationshipsByType(DocumentRelationshipType)
                  .FirstOrDefault();

                Assert.NotNull(docPackageRelationship);
                Uri documentUri =
                        PackUriHelper
                        .ResolvePartUri(
                           new Uri("/", UriKind.Relative),
                                 docPackageRelationship.TargetUri);

                var mainPart = package.GetPart(documentUri);

                Assert.Throws<IOException>(() =>
                {
                    using (Stream partStream = mainPart.GetStream(FileMode.Open, FileAccess.Write))
                    using (StreamWriter sw = new StreamWriter(partStream))
                    {
                        sw.Write(s_DocumentXml);
                    }
                });
            }

            tempGuidName.Delete();
        }

        [Fact]
        public void T120_String_Open_Read_Open_Read()
        {
            var ba = File.ReadAllBytes("plain.docx");
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            File.WriteAllBytes(tempGuidName.FullName, ba);

            using (Package package = Package.Open(tempGuidName.FullName, FileMode.Open, FileAccess.Read))
            {
                PackageRelationship docPackageRelationship =
                  package
                  .GetRelationshipsByType(DocumentRelationshipType)
                  .FirstOrDefault();

                Assert.NotNull(docPackageRelationship);
                Uri documentUri =
                        PackUriHelper
                        .ResolvePartUri(
                           new Uri("/", UriKind.Relative),
                                 docPackageRelationship.TargetUri);

                var mainPart = package.GetPart(documentUri);
                using (var mainPartStream = mainPart.GetStream(FileMode.Open, FileAccess.Read))
                {
                    var xd = XDocument.Load(mainPartStream);
                    var count = xd.DescendantNodes().Count();
                    Assert.Equal(13, count);
                }
            }

            tempGuidName.Delete();
        }

        [Fact]
        public void T119_String_CreateNew_ReadWrite_OpenOrCreate_ReadWrite()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            using (Package package = Package.Open(tempGuidName.FullName, FileMode.CreateNew, FileAccess.ReadWrite))
            {
                Uri uri = PackUriHelper.CreatePartUri(new Uri("dummy.xml", UriKind.Relative));

                // Add a part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(uri,
                                    Mime_MediaTypeNames_Text_Xml,
                                    CompressionOption.Normal);

                using (Stream partStream = packagePartDocument.GetStream(FileMode.OpenOrCreate, FileAccess.ReadWrite))
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }

                using (Stream partStream = packagePartDocument.GetStream(FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    XDocument xd = XDocument.Load(partStream);
                    Assert.Equal(2, xd.DescendantNodes().Count());
                }
            }
			tempGuidName.Delete();
        }

        [Fact]
        public void T118_String_CreateNew_ReadWrite_Truncate_ReadWrite()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            using (Package package = Package.Open(tempGuidName.FullName, FileMode.CreateNew, FileAccess.ReadWrite))
            {
                Uri uri = PackUriHelper.CreatePartUri(new Uri("dummy.xml", UriKind.Relative));

                // Add a part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(uri,
                                    Mime_MediaTypeNames_Text_Xml,
                                    CompressionOption.Normal);

                // Truncate is not a supported value
                AssertExtensions.Throws<ArgumentException>(null, () =>
                {
                    using (Stream partStream = packagePartDocument.GetStream(FileMode.Truncate, FileAccess.ReadWrite))
                    using (StreamWriter sw = new StreamWriter(partStream))
                    {
                        sw.Write(s_DocumentXml);
                    }
                });
            }
			tempGuidName.Delete();
        }

        [Fact]
        public void T117_String_CreateNew_ReadWrite_CreateNew_Star()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            using (Package package = Package.Open(tempGuidName.FullName, FileMode.CreateNew, FileAccess.ReadWrite))
            {
                Uri uri = PackUriHelper.CreatePartUri(new Uri("dummy.xml", UriKind.Relative));

                // Add a part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(uri,
                                    Mime_MediaTypeNames_Text_Xml,
                                    CompressionOption.Normal);

                AssertExtensions.Throws<ArgumentException>(null, () =>
                {
                    using (Stream partStream = packagePartDocument.GetStream(FileMode.CreateNew, FileAccess.ReadWrite))
                    using (StreamWriter sw = new StreamWriter(partStream))
                    {
                        sw.Write(s_DocumentXml);
                    }
                });
            }
			tempGuidName.Delete();
        }

        [Fact]
        public void T116_String_CreateNew_ReadWrite_Open_ReadAndWrite()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            using (Package package = Package.Open(tempGuidName.FullName, FileMode.CreateNew, FileAccess.ReadWrite))
            {
                Uri uri = PackUriHelper.CreatePartUri(new Uri("dummy.xml", UriKind.Relative));

                // Add a part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(uri,
                                    Mime_MediaTypeNames_Text_Xml,
                                    CompressionOption.Normal);

                using (Stream partStream = packagePartDocument.GetStream(FileMode.Create, FileAccess.ReadWrite))
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }

                using (Stream partStream = packagePartDocument.GetStream(FileMode.Open, FileAccess.ReadWrite))
                {
                    XDocument xd = XDocument.Load(partStream);
                    Assert.Equal(2, xd.DescendantNodes().Count());
                }
            }
			tempGuidName.Delete();
        }

        [Fact]
        public void T115_String_CreateNew_ReadWrite_Open_ReadAndWrite()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            using (Package package = Package.Open(tempGuidName.FullName, FileMode.CreateNew, FileAccess.ReadWrite))
            {
                Uri uri = PackUriHelper.CreatePartUri(new Uri("dummy.xml", UriKind.Relative));

                // Add a part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(uri,
                                    Mime_MediaTypeNames_Text_Xml,
                                    CompressionOption.Normal);

                using (Stream partStream = packagePartDocument.GetStream(FileMode.Create, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }

                Assert.Throws<IOException>(() =>
                {
                    using (Stream partStream = packagePartDocument.GetStream(FileMode.Create, FileAccess.Read))
                    {
                        XDocument xd = XDocument.Load(partStream);
                        Assert.Equal(0, xd.DescendantNodes().Count());
                    }
                });
            }
			tempGuidName.Delete();
        }

        [Fact]
        public void T114_String_CreateNew_ReadWrite_Open_ReadAndWrite()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            using (Package package = Package.Open(tempGuidName.FullName, FileMode.CreateNew, FileAccess.ReadWrite))
            {
                Uri uri = PackUriHelper.CreatePartUri(new Uri("dummy.xml", UriKind.Relative));

                // Add a part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(uri,
                                    Mime_MediaTypeNames_Text_Xml,
                                    CompressionOption.Normal);

                using (Stream partStream = packagePartDocument.GetStream(FileMode.Open, FileAccess.ReadWrite))
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }

                using (Stream partStream = packagePartDocument.GetStream(FileMode.Open, FileAccess.ReadWrite))
                {
                    XDocument xd = XDocument.Load(partStream);
                    Assert.Equal(2, xd.DescendantNodes().Count());
                }
            }
			tempGuidName.Delete();
        }

        [Fact]
        public void T113_String_CreateNew_Write_Open_Read()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Package package = Package.Open(tempGuidName.FullName, FileMode.CreateNew, FileAccess.Write);
            });
			tempGuidName.Delete();
        }

        [Fact]
        public void T112_String_CreateNew_Read_Create_Read()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Package package = Package.Open(tempGuidName.FullName, FileMode.CreateNew, FileAccess.Read);
            });
			tempGuidName.Delete();
        }

        [Fact]
        public void T111_String_Create_Write_Star()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            // opening the package attempts to read the package, and no permissions.
            AssertExtensions.Throws<ArgumentException>(null, () => Package.Open(tempGuidName.FullName, FileMode.Create, FileAccess.Write));
			tempGuidName.Delete();
        }

        [Fact]
        public void T110_String_Create_ReadWrite_OpenOrCreate_ReadWrite()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            using (Package package = Package.Open(tempGuidName.FullName, FileMode.Create, FileAccess.ReadWrite))
            {
                Uri uri = PackUriHelper.CreatePartUri(new Uri("dummy.xml", UriKind.Relative));

                // Add a part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(uri,
                                    Mime_MediaTypeNames_Text_Xml,
                                    CompressionOption.Normal);

                using (Stream partStream = packagePartDocument.GetStream(FileMode.OpenOrCreate, FileAccess.Read | FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }

                using (Stream partStream = packagePartDocument.GetStream(FileMode.OpenOrCreate, FileAccess.Read | FileAccess.Write))
                {
                    XDocument xd = XDocument.Load(partStream);
                    Assert.Equal(2, xd.DescendantNodes().Count());
                }
            }
			tempGuidName.Delete();
        }

        [Fact]
        public void T109_String_Create_ReadWrite_OpenOrCreate_ReadWrite()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            using (Package package = Package.Open(tempGuidName.FullName, FileMode.Create, FileAccess.ReadWrite))
            {
                Uri uri = PackUriHelper.CreatePartUri(new Uri("dummy.xml", UriKind.Relative));

                // Add a part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(uri,
                                    Mime_MediaTypeNames_Text_Xml,
                                    CompressionOption.Normal);

                using (Stream partStream = packagePartDocument.GetStream(FileMode.OpenOrCreate, FileAccess.ReadWrite))
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }

                using (Stream partStream = packagePartDocument.GetStream(FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    XDocument xd = XDocument.Load(partStream);
                    Assert.Equal(2, xd.DescendantNodes().Count());
                }
            }
			tempGuidName.Delete();
        }

        [Fact]
        public void T108_String_Create_ReadWrite_OpenOrCreate_Read()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            using (Package package = Package.Open(tempGuidName.FullName, FileMode.Create, FileAccess.ReadWrite))
            {
                Uri uri = PackUriHelper.CreatePartUri(new Uri("dummy.xml", UriKind.Relative));

                // Add a part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(uri,
                                    Mime_MediaTypeNames_Text_Xml,
                                    CompressionOption.Normal);

                using (Stream partStream = packagePartDocument.GetStream(FileMode.OpenOrCreate, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }

                using (Stream partStream = packagePartDocument.GetStream(FileMode.OpenOrCreate, FileAccess.Read))
                {
                    XDocument xd = XDocument.Load(partStream);
                    Assert.Equal(2, xd.DescendantNodes().Count());
                }
            }
			tempGuidName.Delete();
        }

        [Fact]
        public void T107_String_Create_ReadWrite_Open_ReadWrite()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            using (Package package = Package.Open(tempGuidName.FullName, FileMode.Create, FileAccess.ReadWrite))
            {
                Uri uri = PackUriHelper.CreatePartUri(new Uri("dummy.xml", UriKind.Relative));

                // Add a part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(uri,
                                    Mime_MediaTypeNames_Text_Xml,
                                    CompressionOption.Normal);

                using (Stream partStream = packagePartDocument.GetStream(FileMode.Open, FileAccess.Read | FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }

                using (Stream partStream = packagePartDocument.GetStream(FileMode.Open, FileAccess.Read | FileAccess.Write))
                {
                    XDocument xd = XDocument.Load(partStream);
                    Assert.Equal(2, xd.DescendantNodes().Count());
                }
            }
			tempGuidName.Delete();
        }

        [Fact]
        public void T106_String_Create_ReadWrite_Open_ReadWrite()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            using (Package package = Package.Open(tempGuidName.FullName, FileMode.Create, FileAccess.ReadWrite))
            {
                Uri uri = PackUriHelper.CreatePartUri(new Uri("dummy.xml", UriKind.Relative));

                // Add a part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(uri,
                                    Mime_MediaTypeNames_Text_Xml,
                                    CompressionOption.Normal);

                using (Stream partStream = packagePartDocument.GetStream(FileMode.Open, FileAccess.ReadWrite))
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }

                using (Stream partStream = packagePartDocument.GetStream(FileMode.Open, FileAccess.ReadWrite))
                {
                    XDocument xd = XDocument.Load(partStream);
                    Assert.Equal(2, xd.DescendantNodes().Count());
                }
            }
			tempGuidName.Delete();
        }

        [Fact]
        public void T105_String_Create_ReadWrite_Open_Write()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            using (Package package = Package.Open(tempGuidName.FullName, FileMode.Create, FileAccess.ReadWrite))
            {
                Uri uri = PackUriHelper.CreatePartUri(new Uri("dummy.xml", UriKind.Relative));

                // Add a part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(uri,
                                    Mime_MediaTypeNames_Text_Xml,
                                    CompressionOption.Normal);

                using (Stream partStream = packagePartDocument.GetStream(FileMode.Open, FileAccess.Write))
                {
                    AssertExtensions.Throws<ArgumentException>(null, () =>
                    {
                        using (StreamReader sr = new StreamReader(partStream))
                        {
                            XDocument xd = XDocument.Load(sr);
                        }
                    });
                }
            }
			tempGuidName.Delete();
        }

        [Fact]
        public void T104_String_Create_ReadWrite_Open_Write()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            using (Package package = Package.Open(tempGuidName.FullName, FileMode.Create, FileAccess.ReadWrite))
            {
                Uri uri = PackUriHelper.CreatePartUri(new Uri("dummy.xml", UriKind.Relative));

                // Add a part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(uri,
                                    Mime_MediaTypeNames_Text_Xml,
                                    CompressionOption.Normal);

                using (Stream partStream = packagePartDocument.GetStream(FileMode.Open, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(partStream))
                {
                    sw.Write(s_DocumentXml);
                }
            }
			tempGuidName.Delete();
        }

        [Fact]
        public void T103_String_Create_ReadWrite_Open_Read()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            using (Package package = Package.Open(tempGuidName.FullName, FileMode.Create, FileAccess.ReadWrite))
            {
                Uri uri = PackUriHelper.CreatePartUri(new Uri("dummy.xml", UriKind.Relative));

                // Add a part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(uri,
                                    Mime_MediaTypeNames_Text_Xml,
                                    CompressionOption.Normal);

                using (Stream partStream = packagePartDocument.GetStream(FileMode.Open, FileAccess.Read))
                {
                    // just created the part, so nothing in it.
                    // but can't write, as expected.
                    Assert.Equal(0, partStream.Length);
                }
            }
			tempGuidName.Delete();
        }

        [Fact]
        public void T102_String_Create_ReadWrite_Open_Read()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            using (Package package = Package.Open(tempGuidName.FullName, FileMode.Create, FileAccess.ReadWrite))
            {
                Uri uri = PackUriHelper.CreatePartUri(new Uri("dummy.xml", UriKind.Relative));

                // Add a part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(uri,
                                    Mime_MediaTypeNames_Text_Xml,
                                    CompressionOption.Normal);

                AssertExtensions.Throws<ArgumentException>(null, () =>
                {
                    using (Stream partStream = packagePartDocument.GetStream(FileMode.Open, FileAccess.Read))
                    using (StreamWriter sw = new StreamWriter(partStream))
                    {
                        sw.Write(s_DocumentXml);
                    }
                });
            }
			tempGuidName.Delete();
        }

        [Fact]
        public void T101_String_Create_ReadWrite_Create_Read()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            using (Package package = Package.Open(tempGuidName.FullName, FileMode.Create, FileAccess.ReadWrite))
            {
                Uri uri = PackUriHelper.CreatePartUri(new Uri("dummy.xml", UriKind.Relative));

                // Add a part to the Package
                PackagePart packagePartDocument =
                    package.CreatePart(uri,
                                    Mime_MediaTypeNames_Text_Xml,
                                    CompressionOption.Normal);

                Assert.Throws<IOException>(() =>
                {
                    Stream partStream = packagePartDocument.GetStream(FileMode.Create, FileAccess.Read);
                });
                Assert.Throws<IOException>(() =>
                {
                    Stream partStream = packagePartDocument.GetStream(FileMode.CreateNew, FileAccess.Read);
                });
                Assert.Throws<IOException>(() =>
                {
                    Stream partStream = packagePartDocument.GetStream(FileMode.Truncate, FileAccess.Read);
                });
                Assert.Throws<IOException>(() =>
                {
                    Stream partStream = packagePartDocument.GetStream(FileMode.Append, FileAccess.Read);
                });
            }
			tempGuidName.Delete();
        }

        [Fact]
        public void T100_String_Create_Read_Star()
        {
            var tempGuidName = GetTempFileInfoWithExtension(".docx");
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Package package = Package.Open(tempGuidName.FullName, FileMode.Create, FileAccess.Read);
            });
            tempGuidName.Delete();
        }

        [Fact]
        public void SetEmptyPropertyToNull()
        {
            using (var ms = new MemoryStream())
            using (var package = Package.Open(ms, FileMode.Create))
            {
                package.PackageProperties.Category = null;
                Assert.Null(package.PackageProperties.Category);

                package.PackageProperties.ContentStatus = null;
                Assert.Null(package.PackageProperties.ContentStatus);

                package.PackageProperties.ContentType = null;
                Assert.Null(package.PackageProperties.ContentType);

                package.PackageProperties.Created = null;
                Assert.Null(package.PackageProperties.Created);

                package.PackageProperties.Creator = null;
                Assert.Null(package.PackageProperties.Creator);

                package.PackageProperties.Description = null;
                Assert.Null(package.PackageProperties.Description);

                package.PackageProperties.Identifier = null;
                Assert.Null(package.PackageProperties.Identifier);

                package.PackageProperties.Keywords = null;
                Assert.Null(package.PackageProperties.Keywords);

                package.PackageProperties.Language = null;
                Assert.Null(package.PackageProperties.Language);

                package.PackageProperties.LastModifiedBy = null;
                Assert.Null(package.PackageProperties.LastModifiedBy);

                package.PackageProperties.LastPrinted = null;
                Assert.Null(package.PackageProperties.LastPrinted);

                package.PackageProperties.Modified = null;
                Assert.Null(package.PackageProperties.Modified);

                package.PackageProperties.Revision = null;
                Assert.Null(package.PackageProperties.Revision);

                package.PackageProperties.Subject = null;
                Assert.Null(package.PackageProperties.Subject);

                package.PackageProperties.Title = null;
                Assert.Null(package.PackageProperties.Title);

                package.PackageProperties.Version = null;
                Assert.Null(package.PackageProperties.Version);
            }
        }

        private const string DocumentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
    }

    public static class MyExtensions
    {
        public static void AppendFormatComma(this StringBuilder sb, string format, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null)
                    args[i] = "(null)";
            }
            var s = String.Format(format, args) + ", ";
            sb.Append(s);
        }
    }
}
