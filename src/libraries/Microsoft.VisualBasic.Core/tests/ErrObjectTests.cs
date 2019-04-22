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
            ProjectData.SetProjectError(new ArgumentException(), 3);
            var errObj = Information.Err();
            errObj.Number = 5;
            errObj.Description = "Description";
            errObj.Clear();
            Assert.Equal(0, errObj.Erl);
            Assert.Equal(0, errObj.LastDllError);
            Assert.Equal(0, errObj.Number);
            Assert.Equal("", errObj.Description);
            Assert.Null(errObj.GetException());
        }

        [Fact]
        public void Raise()
        {
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
        public void FilterDefaultMessage()
        {
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
    }
}
