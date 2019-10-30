// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualBasic.CompilerServices;
using System;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class ErrObjectTests
    {
        [Fact]
        public void Clear()
        {
            ProjectData.ClearProjectError();
            ProjectData.SetProjectError(new ArgumentException(), 3);
            var errObj = Information.Err();
            errObj.Number = 5;
            errObj.Description = "Description";
            errObj.HelpContext = 6;
            errObj.HelpFile = "File";
            errObj.Source = "Source";
            errObj.Clear();
            Assert.Equal(0, errObj.Erl);
            Assert.Equal(0, errObj.HelpContext);
            Assert.Equal("", errObj.HelpFile);
            Assert.Equal("", errObj.Source);
            Assert.Equal(0, errObj.LastDllError);
            Assert.Equal(0, errObj.Number);
            Assert.Equal("", errObj.Description);
            Assert.Null(errObj.GetException());
            Assert.Equal(0, Information.Erl());
        }

        [Fact]
        public void Raise()
        {
            ProjectData.ClearProjectError();

            ProjectData.SetProjectError(new Exception());
            _ = Assert.Throws<ArgumentException>(() => Information.Err().Raise(0)).ToString();

            ProjectData.SetProjectError(new Exception());
            _ = Assert.Throws<OutOfMemoryException>(() => Information.Err().Raise(7)).ToString();

            ProjectData.SetProjectError(new ArgumentException());
            _ = Assert.Throws<OutOfMemoryException>(() => Information.Err().Raise(7)).ToString();

            ProjectData.SetProjectError(new ArgumentException());
            _ = Assert.Throws<Exception>(() => Information.Err().Raise(32768)).ToString();

            ProjectData.SetProjectError(new InvalidOperationException());
            _ = Assert.Throws<Exception>(() => Information.Err().Raise(1, Description: "MyDescription")).ToString();
        }

        [Fact]
        public void Source()
        {
            ProjectData.ClearProjectError();

            ProjectData.SetProjectError(new Exception() { Source = null });
            Assert.Null(Information.Err().Source);

            ProjectData.SetProjectError(new Exception() { Source = "MySource1" });
            Assert.Equal("MySource1", Information.Err().Source);

            ProjectData.SetProjectError(new Exception() { Source = null });
            Assert.Null(Information.Err().Source);

            ProjectData.SetProjectError(new Exception() { Source = null });
            _ = Assert.Throws<OutOfMemoryException>(() => Information.Err().Raise(7, Source: "MySource2"));
            Assert.Equal("MySource2", Information.Err().Source);
        }

        [Fact]
        public void FilterDefaultMessage()
        {
            ProjectData.ClearProjectError();

            string message = "Description";
            ProjectData.SetProjectError(new System.IO.FileNotFoundException(message));
            Assert.Equal(message, Information.Err().Description);

            message = "";
            ProjectData.SetProjectError(new System.IO.FileNotFoundException(message));
            Assert.NotEqual(message, Information.Err().Description);

            message = "Exception from HRESULT: 0x80";
            ProjectData.SetProjectError(new System.IO.FileNotFoundException(message));
            Assert.NotEqual(message, Information.Err().Description);
        }

        [Fact]
        public void MakeHelpLink()
        {
            ProjectData.ClearProjectError();

            ProjectData.SetProjectError(new ArgumentException());
            Assert.Equal("#0", Assert.Throws<OutOfMemoryException>(() => Information.Err().Raise(7)).HelpLink);

            ProjectData.SetProjectError(new ArgumentException());
            Assert.Equal("#3", Assert.Throws<OutOfMemoryException>(() => Information.Err().Raise(7, HelpContext: 3)).HelpLink);

            ProjectData.SetProjectError(new ArgumentException());
            Assert.Equal("MyFile1#3", Assert.Throws<OutOfMemoryException>(() => Information.Err().Raise(7, HelpFile: "MyFile1")).HelpLink);

            ProjectData.ClearProjectError();
            ProjectData.SetProjectError(new ArgumentException());
            Assert.Equal("MyFile2#0", Assert.Throws<OutOfMemoryException>(() => Information.Err().Raise(7, HelpFile: "MyFile2")).HelpLink);

            ProjectData.SetProjectError(new ArgumentException());
            Assert.Equal("MyFile3#3", Assert.Throws<OutOfMemoryException>(() => Information.Err().Raise(7, HelpContext: 3, HelpFile: "MyFile3")).HelpLink);
        }

        [Theory]
        [InlineData(null, 0, "")]
        [InlineData("", 0, "")]
        [InlineData("#0", 0, "")]
        [InlineData("#1", 1, "")]
        [InlineData("#-3", -3, "")]
        [InlineData("MyFile1", 0, "MyFile1")]
        [InlineData("MyFile4#4", 4, "MyFile4")]
        public void ParseHelpLink(string helpLink, int expectedHelpContext, string expectedHelpFile)
        {
            ProjectData.ClearProjectError();
            ProjectData.SetProjectError(new ArgumentException() { HelpLink = helpLink });
            Assert.Equal(expectedHelpContext, Information.Err().HelpContext);
            Assert.Equal(expectedHelpFile, Information.Err().HelpFile);
        }

        [Theory]
        [InlineData("#")]
        [InlineData("##")]
        [InlineData("##2")]
        [InlineData("MyFile2#")]
        [InlineData("MyFile3##")]
        public void ParseHelpLink_InvalidCastException(string helpLink)
        {
            ProjectData.ClearProjectError();
            ProjectData.SetProjectError(new ArgumentException() { HelpLink = helpLink });
            Assert.Throws<InvalidCastException>(() => Information.Err().HelpContext);
            Assert.Throws<InvalidCastException>(() => Information.Err().HelpFile);
        }
    }
}
