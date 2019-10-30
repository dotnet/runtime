// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.RemoteExecutor;
using System;
using Xunit;

namespace Microsoft.VisualBasic.CompilerServices.Tests
{
    public class ProjectDataTests
    {
        [Fact]
        public void CreateProjectError()
        {
            _ = Assert.Throws<ArgumentException>(() => ProjectData.CreateProjectError(0)).ToString();
            _ = Assert.IsType<Exception>(ProjectData.CreateProjectError(1)).ToString();
            _ = Assert.IsType<OutOfMemoryException>(ProjectData.CreateProjectError(7)).ToString();
            _ = Assert.IsType<Exception>(ProjectData.CreateProjectError(32768)).ToString();
            _ = Assert.IsType<Exception>(ProjectData.CreateProjectError(40068)).ToString();
            _ = Assert.IsType<Exception>(ProjectData.CreateProjectError(41000)).ToString();
        }

        [Fact]
        public void SetProjectError()
        {
            Exception e = new ArgumentException();
            ProjectData.SetProjectError(e);
            Assert.Same(e, Information.Err().GetException());
            Assert.Equal(0, Information.Err().Erl);

            e = new InvalidOperationException();
            ProjectData.SetProjectError(e, 3);
            Assert.Same(e, Information.Err().GetException());
            Assert.Equal(3, Information.Err().Erl);

            e = new Exception();
            ProjectData.SetProjectError(e);
            Assert.Same(e, Information.Err().GetException());
            Assert.Equal(0, Information.Err().Erl);
        }

        [Fact]
        public void ClearProjectError()
        {
            ProjectData.SetProjectError(new ArgumentException(), 3);
            ProjectData.ClearProjectError();
            Assert.Null(Information.Err().GetException());
            Assert.Equal(0, Information.Err().Erl);
        }

        [Fact]
        public void EndApp()
        {
            RemoteExecutor.Invoke(
                new Action(() =>
                {
                    // See FileSystemTests.CloseAllFiles() for a test that EndApp() closes open files.
                    ProjectData.EndApp();
                    throw new Exception(); // Shouldn't reach here.
                }),
                new RemoteInvokeOptions() { ExpectedExitCode = 0 }).Dispose();
        }
    }
}
