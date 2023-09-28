// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom.Compiler;
using System.IO;
using Xunit;

namespace System.CodeDom.Tests.System.CodeDom.Compiler;

public class IndentedTextWriterTests
{
    [Fact]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
    public void ShouldIndentFirstLineWhenRequested()
    {
        using StringWriter stringWriter = new();
        using IndentedTextWriter indentedTextWriter = new(stringWriter);
        indentedTextWriter.Indent++;
        const string message1 = "New line";
        const string message2 = "Second line";
        indentedTextWriter.Write(message1);
        indentedTextWriter.WriteLine();
        indentedTextWriter.Write(message2);
        Assert.Equal($"{IndentedTextWriter.DefaultTabString}{message1}{indentedTextWriter.NewLine}{IndentedTextWriter.DefaultTabString}{message2}",
            stringWriter.ToString());
    }

    [Fact]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
    public void ShouldntIndentFirstLineWhenNotRequested()
    {
        using StringWriter stringWriter = new();
        using IndentedTextWriter indentedTextWriter = new(stringWriter);
        const string message1 = "New line";
        const string message2 = "Second line";
        indentedTextWriter.Write(message1);
        indentedTextWriter.WriteLine();
        indentedTextWriter.Write(message2);
        Assert.Equal($"{message1}{indentedTextWriter.NewLine}{message2}", stringWriter.ToString());
    }
}
