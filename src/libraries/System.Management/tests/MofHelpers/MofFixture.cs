// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Reflection;
using Xunit;

namespace System.Management.Tests
{
    public class MofFixture : IDisposable
    {
        private Assembly _assembly = typeof(MofFixture).GetTypeInfo().Assembly;
        private string _mofCompilerPath = Path.Combine(Environment.SystemDirectory, @"wbem\mofcomp.exe");

        public MofFixture()
        {
            if (AdminHelpers.IsProcessElevated())
                ExtractAndCompileMof("WmiEBvt.mof");
        }

        public void Dispose()
        {
            if (AdminHelpers.IsProcessElevated())
                ExtractAndCompileMof("CleanUp.mof");
        }

        private void ExtractAndCompileMof(string mofResourceName)
        {
            string mofFilePath = null;
            try
            {
                mofFilePath = ExtractMofFromResourcesToFile(mofResourceName);
                CompileMof(mofFilePath);
            }
            finally
            {
                if (mofFilePath != null)
                    File.Delete(mofFilePath);
            }
        }

        private string ExtractMofFromResourcesToFile(string mofResourceName)
        {
            var mofFilePath = Path.Combine(Path.GetTempPath(), mofResourceName);
            using (var mofStream = new StreamReader(_assembly.GetManifestResourceStream(mofResourceName)))
            {
                string mofContent = mofStream.ReadToEnd();
                File.WriteAllText(mofFilePath, mofContent);
            }

            return mofFilePath;
        }

        private void CompileMof(string mofFilePath)
        {
            var psi = new ProcessStartInfo(_mofCompilerPath, mofFilePath);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            Process p = Process.Start(psi);
            p.WaitForExit();
            string output = p.StandardOutput.ReadToEnd();
            Assert.True(p.ExitCode == 0, $"Failed to compile mof file {mofFilePath} output: {output}");
        }
    }
}
