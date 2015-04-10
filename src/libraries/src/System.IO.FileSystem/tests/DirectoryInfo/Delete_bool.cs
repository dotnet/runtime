// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public partial class DirectoryInfo_Delete_bool : FileSystemTest
    {
        [Fact]
        public void Dot()
        {
            Assert.Throws<IOException>(() => new DirectoryInfo(".").Delete(false));
        }

        [Fact]
        public void DoesNotExist_false()
        {
            Assert.Throws<DirectoryNotFoundException>(() => new DirectoryInfo("ThisDoesNotExist").Delete(false));
        }

        [Fact]
        public void DoesNotExist_true()
        {
            Assert.Throws<DirectoryNotFoundException>(() => new DirectoryInfo("ThisDoesNotExist").Delete(true));
        }

        [Fact]
        public void RegularDirectory_false()
        {
            Directory.CreateDirectory(Path.Combine(TestDirectory, "Subdir")).Delete(false);
        }

        [Fact]
        public void RegularDirectory_true()
        {
            Directory.CreateDirectory(Path.Combine(TestDirectory, "Subdir")).Delete(true);
        }

        [Fact]
        public void WithSubdirectories()
        {
            string subdirRoot = "WithSubdirectories";
            DirectoryInfo dir1 = Directory.CreateDirectory(Path.Combine(TestDirectory, subdirRoot, "Test1"));
            DirectoryInfo dir2 = Directory.CreateDirectory(Path.Combine(TestDirectory, subdirRoot, "Test2"));

            dir1.Delete(false);

            Assert.Throws<IOException>(() => new DirectoryInfo(Path.Combine(TestDirectory, subdirRoot)).Delete(false));

            dir2.Delete(true);
        }

        [Fact]
        public void WithFile()
        {
            string subdirRoot = "WithFile";
            DirectoryInfo dir1 = Directory.CreateDirectory(Path.Combine(TestDirectory, subdirRoot, "Test1"));
            DirectoryInfo dir2 = Directory.CreateDirectory(Path.Combine(TestDirectory, subdirRoot, "Test2"));

            new FileStream(Path.Combine(dir1.FullName, "Hello.tmp"), FileMode.Create).Dispose();

            Assert.Throws<IOException>(() => new DirectoryInfo(Path.Combine(TestDirectory, subdirRoot)).Delete(false));

            dir2.Delete(true);
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // directories with in-use files can be deleted on Unix
        public void FileInUse()
        {
            string subdirRoot = "FileInUse";
            DirectoryInfo dir = Directory.CreateDirectory(Path.Combine(TestDirectory, subdirRoot));
            using(FileStream fs = new FileStream(Path.Combine(TestDirectory, subdirRoot, "Test.tmp"), FileMode.Create))
            {
                Assert.Throws<IOException>(() => dir.Delete(true));
            } 
            dir.Delete(true);
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // readonly directories can be deleted on Unix
        public void ReadOnly()
        {
            DirectoryInfo dir = Directory.CreateDirectory(Path.Combine(TestDirectory, "ReadOnly"));
            dir.Attributes = FileAttributes.ReadOnly;

            Assert.Throws<IOException>(() => dir.Delete(false));   
            Assert.Throws<IOException>(() => dir.Delete(true));

            dir.Attributes = new FileAttributes();
            dir.Delete(true);
        }

        [Fact]
        public void Hidden()
        {
            DirectoryInfo dir = Directory.CreateDirectory(Path.Combine(TestDirectory, "ReadOnly"));
            dir.Attributes = FileAttributes.Hidden;
            dir.Delete(true);
        }
    }
}
