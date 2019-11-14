// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Tests;
using System.IO;
using Xunit;

namespace System.CodeDom.Tests
{
    public class TempFileCollectionTests : ICollection_NonGeneric_Tests
    {
        protected override ICollection NonGenericICollectionFactory() => new TempFileCollection();

        protected override void AddToCollection(ICollection collection, int numberOfItemsToAdd)
        {
            Random random = new Random(numberOfItemsToAdd);
            for (int i = 0; i < numberOfItemsToAdd; i++)
            {
                ((TempFileCollection)collection).AddFile(random.Next().ToString(), keepFile: true);
            }
        }

        protected override bool NullAllowed => false;
        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;
        protected override bool ICollection_NonGeneric_HasNullSyncRoot => true;

        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectValueType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectReferenceType_ThrowType => typeof(InvalidCastException);

        public override void ICollection_NonGeneric_CopyTo_NonZeroLowerBound(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            Array arr = Array.CreateInstance(typeof(object), new int[1] { count }, new int[1] { 2 });

            if (count == 0)
            {
                collection.CopyTo(arr, 0);
            }
            else
            {
                Assert.Throws<IndexOutOfRangeException>(() => collection.CopyTo(arr, 0));
            }
        }

        [Fact]
        public void Ctor_Empty()
        {
            var collection = new TempFileCollection();
            Assert.Equal(0, collection.Count);
            Assert.Empty(collection.TempDir);
            Assert.False(collection.KeepFiles);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("TempDir")]
        public void Ctor_String(string tempDir)
        {
            var collection = new TempFileCollection(tempDir);
            Assert.Equal(0, collection.Count);
            Assert.Equal(tempDir ?? string.Empty, collection.TempDir);
            Assert.False(collection.KeepFiles);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", true)]
        [InlineData("TempDir", false)]
        public void Ctor_String_Bool(string tempDir, bool keepFiles)
        {
            var collection = new TempFileCollection(tempDir, keepFiles);
            Assert.Equal(0, collection.Count);
            Assert.Equal(tempDir ?? string.Empty, collection.TempDir);
            Assert.Equal(keepFiles, collection.KeepFiles);
        }

        public static IEnumerable<object[]> BasePath_TestData()
        {
            yield return new object[] { "NoSuchDirectory" };
            yield return new object[] { TempDirectory() };
        }

        [Theory]
        [MemberData(nameof(BasePath_TestData))]
        public void BasePath_Get(string tempDir)
        {
            var collection = new TempFileCollection(tempDir);
            if (Directory.Exists(tempDir))
            {
                Assert.StartsWith(tempDir, collection.BasePath);
            }
            else
            {
                Assert.Throws<DirectoryNotFoundException>(() => collection.BasePath);
                Assert.StartsWith(tempDir, collection.BasePath);
            }
        }

        [Fact]
        public void AddFileExtension()
        {
            string tempDirectory = TempDirectory();
            using (var collection = new TempFileCollection(tempDirectory))
            {
                string file = collection.AddExtension("txt");
                Assert.False(File.Exists(file));
                Assert.Equal(collection.BasePath + "." + "txt", file);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AddExtension_InvalidFileExtension_ThrowsArgumentException(string fileExtension)
        {
            using (var collection = new TempFileCollection())
            {
                AssertExtensions.Throws<ArgumentException>("fileExtension", () => collection.AddExtension(fileExtension));
                AssertExtensions.Throws<ArgumentException>("fileExtension", () => collection.AddExtension(fileExtension, keepFile: false));
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void AddFile(bool fileExists, bool keepFile)
        {
            string directory = TempDirectory();
            const string FileName = "file.extension";
            string filePath = Path.Combine(directory, FileName);
            if (fileExists)
            {
                File.Create(filePath).Dispose();
            }
            try
            {
                using (var collection = new TempFileCollection(directory))
                {
                    // AddFile(fileName) is a misnomer, and should really be AddFile(filePath),
                    // as only files added with their full path are deleted.
                    collection.AddFile(filePath, keepFile);
                    Assert.Equal(fileExists, File.Exists(filePath));
                }

                Assert.Equal(fileExists && keepFile, File.Exists(filePath));
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AddFile_MultipleFiles_DeletesAllIfKeepFilesFalse(bool keepFiles)
        {
            string directory = TempDirectory();
            string filePath1 = Path.Combine(directory, "file1.extension");
            string filePath2 = Path.Combine(directory, "file2.extension");

            File.Create(filePath1).Dispose();
            File.Create(filePath2).Dispose();

            try
            {
                using (var collection = new TempFileCollection(directory))
                {
                    collection.AddFile(filePath1, keepFiles);
                    collection.AddFile(filePath2, keepFiles);
                }

                Assert.Equal(keepFiles, File.Exists(filePath1));
                Assert.Equal(keepFiles, File.Exists(filePath2));
            }
            finally
            {
                if (File.Exists(filePath1))
                {
                    File.Delete(filePath1);
                }
                if (File.Exists(filePath2))
                {
                    File.Delete(filePath2);
                }
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AddFile_InvalidFileName_ThrowsArgumentException(string fileName)
        {
            using (var collection = new TempFileCollection())
            {
                AssertExtensions.Throws<ArgumentException>("fileName", () => collection.AddFile(fileName, keepFile: false));
            }
        }

        [Fact]
        public void AddFile_DuplicateFileName_ThrowsArgumentException()
        {
            using (var collection = new TempFileCollection())
            {
                const string FileName = "FileName";
                collection.AddFile(FileName, keepFile: false);
                AssertExtensions.Throws<ArgumentException>("fileName", () => collection.AddFile(FileName, keepFile: false));

                // Case insensitive
                AssertExtensions.Throws<ArgumentException>("fileName", () => collection.AddFile(FileName.ToLowerInvariant(), keepFile: false));
            }
        }

        [Fact]
        public void GetEnumerator_Empty_ReturnsFalse()
        {
            using (var collection = new TempFileCollection())
            {
                Assert.False(collection.GetEnumerator().MoveNext());
            }
        }

        [Fact]
        public void CopyTo()
        {
            using (var collection = new TempFileCollection())
            {
                const int ArrayIndex = 1;
                const string FileName = "File";
                collection.AddFile(FileName, keepFile: false);

                var array = new string[ArrayIndex + collection.Count];
                collection.CopyTo(array, ArrayIndex);

                Assert.Null(array[0]);
                Assert.Equal(FileName, array[ArrayIndex]);
            }
        }

        [Fact]
        public void Delete()
        {
            string directory = TempDirectory();
            string filePath1 = Path.Combine(directory, "file1.extension");

            File.Create(filePath1).Dispose();

            try
            {
                using (var collection = new TempFileCollection(directory))
                {
                    collection.AddFile(filePath1, false);
                    Assert.True(File.Exists(filePath1));
                    collection.Delete();
                    Assert.False(File.Exists(filePath1));
                    Assert.Equal(0, collection.Count);
                }
            }
            finally
            {
                File.Delete(filePath1);
            }
        }

        private static string s_tempDirectory = null;
        private static string TempDirectory()
        {
            if (s_tempDirectory == null)
            {
                string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDirectory);
                s_tempDirectory = tempDirectory;
            }
            return s_tempDirectory;
        }
    }
}
