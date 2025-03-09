// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using System.Text;
using Xunit;

namespace System.IO.Compression.Tests;

// Class that can run command line tools for handling zip files.
public class CliZipTools
{
    private const string Exe7z = "/usr/bin/7z";
    private const string ExeZip = "/usr/bin/zip";
    private const string ExeUnzip = "/usr/bin/unzip";

    // t: test archive integrity
    // -bb3: set the output log level to 3 (maximum)
    // -bd: disable progress indicator
    // {0}=zip input filename
    private const string ArgsTest7z = "t -bb3 -bd {0}";

    // x: extract with full paths
    // -bb3: set the output log level to 3 (maximum)
    // -bd: disable progress indicator
    // {0}=zip input filename
    // {1}=target directory for extraction
    private const string ArgsExtract7z = "x -bb3 -bd {0} -o{1}";

    // {0}=zip input filename
    // -d: specify target directory for extraction
    // {1}=target directory for extraction
    private const string ArgsExtractUnzip = "{0} -d {1}";

    private readonly StringBuilder _output;
    private readonly StringBuilder _error;

    public CliZipTools()
    {
        _output = new StringBuilder();
        _error = new StringBuilder();
    }

    public string Output => _output.ToString();
    public string Error => _error.ToString();

    public void TestWith7Zip(string inputZipFile) => ExecuteProcess(Exe7z, string.Format(ArgsTest7z, inputZipFile));
    public void ExtractWith7Zip(string inputZipFile, string outputDirectory) => ExecuteProcess(Exe7z, string.Format(ArgsExtract7z, inputZipFile, outputDirectory));
    public void ExtractWithUnzip(string inputZipFile, string outputDirectory) => ExecuteProcess(ExeUnzip, string.Format(ArgsExtractUnzip, inputZipFile, outputDirectory));


    private void ExecuteProcess(string exe, string args)
    {
        Assert.True(File.Exists(exe));

        _output.Clear();
        _error.Clear();

        using Process p = new Process();

        p.OutputDataReceived += new DataReceivedEventHandler(OutputDataHandler);
        p.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataHandler);

        p.StartInfo.UseShellExecute = false;
        p.StartInfo.CreateNoWindow = true;

        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;

        p.StartInfo.FileName = exe;
        p.StartInfo.Arguments = args;

        p.Start();

        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        p.WaitForExit();

        Assert.True(p.ExitCode == 0, $"Command failed:\n{exe} {args}\nError output:\n{_error}");
    }

    private void OutputDataHandler(object sender, DataReceivedEventArgs e) => _output.AppendLine(e.Data);
    private void ErrorDataHandler(object sender, DataReceivedEventArgs e) => _error.AppendLine(e.Data);
}