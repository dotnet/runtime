// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Moq;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.NET.Sdk.WebAssembly.Tests
{
    public class ConvertDllsToWebCilTests
    {
        private ConvertDllsToWebCil task = new ConvertDllsToWebCil();
        private List<BuildErrorEventArgs> errors = new List<BuildErrorEventArgs>();

        public ConvertDllsToWebCilTests()
        {
            var buildEngine = new Mock<IBuildEngine>();
            buildEngine.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>())).Callback<BuildErrorEventArgs>(e => errors.Add(e));
            task.BuildEngine = buildEngine.Object;
        }

        [Fact]
        public void TestEmptyInput()
        {
            string input = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".dll");

            try
            {
                File.Create(input).Dispose();

                task.Candidates = new ITaskItem[] { new TaskItem(input) };
                task.IsEnabled = true;
                task.OutputPath = task.IntermediateOutputPath = Path.GetTempPath();

                bool result = task.Execute();

                Assert.False(result);
                Assert.Single(errors);
                Assert.Contains(input, errors[0].Message);
            }
            finally
            {
                File.Delete(input);
            }
        }

        [Fact]
        public void TestInvalidDirectoryInput()
        {
            string input = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".dll");

            try
            {
                Directory.CreateDirectory(input);

                task.Candidates = new ITaskItem[] { new TaskItem(input) };
                task.IsEnabled = true;
                task.OutputPath = task.IntermediateOutputPath = Path.GetTempPath();

                bool result = task.Execute();

                Assert.False(result);
                Assert.Single(errors);
                Assert.Contains(input, errors[0].Message);
            }
            finally
            {
                Directory.Delete(input);
            }
        }
    }
}
