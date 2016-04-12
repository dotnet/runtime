// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    #region EnumerateFiles

    public class Directory_EnumFiles_Str : Directory_GetFiles_str
    {
        public override string[] GetEntries(string path)
        {
            return Directory.EnumerateFiles(path).ToArray();
        }
    }

    public class Directory_EnumFiles_str_str : Directory_GetFiles_str_str
    {
        public override string[] GetEntries(string path)
        {
            return Directory.EnumerateFiles(path, "*").ToArray();
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return Directory.EnumerateFiles(path, searchPattern).ToArray();
        }
    }

    public class Directory_EnumFiles_str_str_so : Directory_GetFiles_str_str_so
    {
        public override string[] GetEntries(string path)
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly).ToArray();
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return Directory.EnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly).ToArray();
        }

        public override string[] GetEntries(string path, string searchPattern, SearchOption option)
        {
            return Directory.EnumerateFiles(path, searchPattern, option).ToArray();
        }
    }
    #endregion

    #region EnumerateFileSystemEntries

    public class Directory_EnumFSE_str : Directory_GetFileSystemEntries_str
    {
        public override string[] GetEntries(string dirName)
        {
            return Directory.EnumerateFileSystemEntries(dirName).ToArray();
        }
    }

    public class Directory_EnumFSE_str_str : Directory_GetFileSystemEntries_str_str
    {
        public override string[] GetEntries(string dirName)
        {
            return Directory.EnumerateFileSystemEntries(dirName, "*").ToArray();
        }

        public override string[] GetEntries(string dirName, string searchPattern)
        {
            return Directory.EnumerateFileSystemEntries(dirName, searchPattern).ToArray();
        }

        [Fact]
        public void Clone_Enumerator_Empty()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            var enumerator1 = Directory.EnumerateFileSystemEntries(testDir.FullName);
            var enumerator2 = enumerator1;
            Assert.Equal(enumerator1.ToArray(), enumerator2.ToArray());
        }

        [Fact]
        public void Clone_Enumerator_Trimmed_SearchPattern()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            var enumerator1 = Directory.EnumerateFileSystemEntries(testDir.FullName, ((char)0xA).ToString());
            var enumerator2 = enumerator1;
            Assert.Empty(enumerator1.ToArray());
            Assert.Empty(enumerator2.ToArray());
        }

        [Fact]
        public void Delete_Directory_After_Creating_Enumerable()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            DirectoryInfo subDir1 = Directory.CreateDirectory(Path.Combine(testDir.FullName, "a"));
            DirectoryInfo subDir2 = Directory.CreateDirectory(Path.Combine(testDir.FullName, "b"));
            var enumerator = Directory.EnumerateDirectories(testDir.FullName);
            foreach (var dir in enumerator)
            {
                Directory.Delete(dir);
            }
            Assert.Equal(0, enumerator.ToArray().Length);
        }


        [Fact]
        public void Trailing_Slash_Adds_Trailing_Star()
        {
            // A searchpattern of c:\temp\ will become c:\temp\* internally
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            DirectoryInfo subDir1 = Directory.CreateDirectory(Path.Combine(testDir.FullName, "a"));
            DirectoryInfo subDir2 = Directory.CreateDirectory(Path.Combine(testDir.FullName, "b"));
            var enumerator = Directory.EnumerateDirectories(testDir.FullName, "a" + Path.DirectorySeparatorChar);
            Assert.Equal(0, enumerator.ToArray().Length);
        }
    }

    public class Directory_EnumFSE_str_str_so : Directory_GetFileSystemEntries_str_str_so
    {
        public override string[] GetEntries(string dirName)
        {
            return Directory.EnumerateFileSystemEntries(dirName, "*", SearchOption.TopDirectoryOnly).ToArray();
        }

        public override string[] GetEntries(string dirName, string searchPattern)
        {
            return Directory.EnumerateFileSystemEntries(dirName, searchPattern, SearchOption.TopDirectoryOnly).ToArray();
        }

        public override string[] GetEntries(string dirName, string searchPattern, SearchOption option)
        {
            return Directory.EnumerateFileSystemEntries(dirName, searchPattern, option).ToArray();
        }
    }

    public class Directory_EnumFSE_str_str_so_alldirs : Directory_GetFileSystemEntries_str_str_so_alldirs
    {
        public override string[] GetEntries(string dirName)
        {
            return Directory.EnumerateFileSystemEntries(dirName, "*", SearchOption.AllDirectories).ToArray();
        }

        public override string[] GetEntries(string dirName, string searchPattern)
        {
            return Directory.EnumerateFileSystemEntries(dirName, searchPattern, SearchOption.AllDirectories).ToArray();
        }

        public override string[] GetEntries(string dirName, string searchPattern, SearchOption option)
        {
            return Directory.EnumerateFileSystemEntries(dirName, searchPattern, option).ToArray();
        }
    }

    #endregion

    #region EnumerateDirectories

    public class Directory_EnumDir_str : Directory_GetDirectories_str
    {
        public override string[] GetEntries(string dirName)
        {
            return Directory.EnumerateDirectories(dirName).ToArray();
        }
    }

    public class Directory_EnumDir_str_str : Directory_GetDirectories_str_str
    {
        public override string[] GetEntries(string dirName)
        {
            return Directory.EnumerateDirectories(dirName, "*").ToArray();
        }

        public override string[] GetEntries(string dirName, string searchPattern)
        {
            return Directory.EnumerateDirectories(dirName, searchPattern).ToArray();
        }
    }

    public class Directory_EnumDir_str_str_so : Directory_GetDirectories_str_str_so
    {
        public override string[] GetEntries(string dirName)
        {
            return Directory.EnumerateDirectories(dirName, "*", SearchOption.TopDirectoryOnly).ToArray();
        }

        public override string[] GetEntries(string dirName, string searchPattern)
        {
            return Directory.EnumerateDirectories(dirName, searchPattern, SearchOption.TopDirectoryOnly).ToArray();
        }

        public override string[] GetEntries(string dirName, string searchPattern, SearchOption option)
        {
            return Directory.EnumerateDirectories(dirName, searchPattern, option).ToArray();
        }
    }

    #endregion
}
