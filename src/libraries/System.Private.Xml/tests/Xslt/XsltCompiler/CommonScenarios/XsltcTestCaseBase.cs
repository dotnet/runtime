// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit.Abstractions;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Xsl;
using XmlCoreTest.Common;
using OLEDB.Test.ModuleCore;
using System.Runtime.Loader;

namespace System.Xml.Tests
{
    public class XsltcTestCaseBase : CTestCase
    {
        // Generic data for all derived test cases
        public String szDefaultNS = "urn:my-object";

        public String szEmpty = "";
        public String szInvalid = "*?%(){}[]&!@#$";
        public String szLongNS = "http://www.microsoft.com/this/is/a/very/long/namespace/uri/to/do/the/api/testing/for/xslt/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/";
        public String szLongString = "ThisIsAVeryLongStringToBeStoredAsAVariableToDetermineHowLargeThisBufferForAVariableNameCanBeAndStillFunctionAsExpected";
        public String szSimple = "myArg";
        public String[] szWhiteSpace = { "  ", "\n", "\t", "\r", "\t\n  \r\t" };
        public String szXslNS = "http://www.w3.org/1999/XSL/Transform";

        // Other global variables
        protected bool _createFromInputFile = false; // This is intiialized from a parameter passed from LTM as a dimension, that dictates whether the variation is to be created using an input file.

        protected bool _isInProc; // Is the current test run in proc or /Host None?

        private static ITestOutputHelper s_output;
        public XsltcTestCaseBase(ITestOutputHelper output)
        {
            s_output = output;
        }

        public static bool xsltcExeFound()
        {
            try
            {
                // Verify xsltc.exe is available
                XmlCoreTest.Common.XsltVerificationLibrary.SearchPath("xsltc.exe");
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            return true;
        }

        public override int Init(object objParam)
        {
            // initialize whether this run is in proc or not

            string executionMode = "File";

            _createFromInputFile = executionMode.Equals("File");

            return 1;
        }

        protected static void CompareOutput(string expected, Stream actualStream)
        {
            using (var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes(expected)))
            {
                CompareOutput(expectedStream, actualStream);
            }
        }

        private static string NormalizeLineEndings(string s)
        {
            return s.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        protected static void CompareOutput(Stream expectedStream, Stream actualStream, int count = 0)
        {
            actualStream.Seek(0, SeekOrigin.Begin);

            using (var expectedReader = new StreamReader(expectedStream))
            using (var actualReader = new StreamReader(actualStream))
            {
                for (int i = 0; i < count; i++)
                {
                    actualReader.ReadLine();
                    expectedReader.ReadLine();
                }

                string actual = NormalizeLineEndings(actualReader.ReadToEnd());
                string expected = NormalizeLineEndings(expectedReader.ReadToEnd());

                if (actual.Equals(expected))
                {
                    return;
                }

                throw new CTestFailedException("Output was not as expected.", actual, expected, null);
            }
        }

        protected bool LoadPersistedTransformAssembly(string asmName, string typeName, string baselineFile, bool pdb)
        {
            var other = (AssemblyLoader)Activator.CreateInstance(typeof(AssemblyLoader), typeof(AssemblyLoader).FullName);

            bool result = other.Verify(asmName, typeName, baselineFile, pdb);

            return result;
        }

        protected string ReplaceCurrentWorkingDirectory(string commandLine)
        {
            return commandLine.Replace(@"$(CurrentWorkingDirectory)", XsltcModule.TargetDirectory);
        }

        protected bool ShouldSkip(object[] varParams)
        {
            // some test only applicable in English environment, so skip them if current cultral is not english
            bool isCultralEnglish = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower() == "en";
            if (isCultralEnglish)
            {
                return false;
            }

            // look up key word "EnglishOnly", if hit return true, otherwise false
            return varParams != null && varParams.Any(o => o.ToString() == "EnglishOnly");
        }

        protected void VerifyTest(String cmdLine, String baselineFile, bool loadFromFile)
        {
            VerifyTest(cmdLine, String.Empty, false, String.Empty, baselineFile, loadFromFile);
        }

        protected void VerifyTest(String cmdLine, String asmName, bool asmCreated, String typeName, String baselineFile, bool loadFromFile)
        {
            VerifyTest(cmdLine, asmName, asmCreated, typeName, String.Empty, false, baselineFile, loadFromFile);
        }

        protected void VerifyTest(String cmdLine, String asmName, bool asmCreated, String typeName, String pdbName, bool pdbCreated, String baselineFile, bool loadFromFile)
        {
            VerifyTest(cmdLine, asmName, asmCreated, typeName, pdbName, pdbCreated, baselineFile, true, loadFromFile);
        }

        protected void VerifyTest(String cmdLine, String asmName, bool asmCreated, String typeName, String pdbName, bool pdbCreated, String baselineFile, bool runAssemblyVerification, bool loadFromFile)
        {
            string targetDirectory = XsltcModule.TargetDirectory;

            string output = asmCreated ? TryCreatePersistedTransformAssembly(cmdLine, _createFromInputFile, true, targetDirectory) : TryCreatePersistedTransformAssembly(cmdLine, _createFromInputFile, false, targetDirectory);

            //verify assembly file existence
            if (asmName != null && String.CompareOrdinal(String.Empty, asmName) != 0)
            {
                if (File.Exists(GetPath(asmName)) != asmCreated)
                {
                    throw new CTestFailedException("Assembly File Creation Check: FAILED");
                }
            }

            //verify pdb existence
            if (pdbName != null && String.CompareOrdinal(String.Empty, pdbName) != 0)
            {
                if (File.Exists(GetPath(pdbName)) != pdbCreated)
                {
                    throw new CTestFailedException("PDB File Creation Check: FAILED");
                }
            }

            if (asmCreated && !String.IsNullOrEmpty(typeName))
            {
                if (!LoadPersistedTransformAssembly(GetPath(asmName), typeName, baselineFile, pdbCreated))
                {
                    throw new CTestFailedException("Assembly loaded failed");
                }
            }
            else
            {
                using (var ms = new MemoryStream())
                using (var sw = new StreamWriter(ms) { AutoFlush = true })
                using (var expected = new FileStream(GetPath(baselineFile), FileMode.Open, FileAccess.Read))
                {
                    sw.Write(output);
                    CompareOutput(expected, ms, 4);
                }
            }

            SafeDeleteFile(GetPath(pdbName));
            SafeDeleteFile(GetPath(asmName));

            return;
        }

        private static void SafeDeleteFile(string fileName)
        {
            try
            {
                var fileInfo = new FileInfo(fileName);

                if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
                {
                    fileInfo.Directory.Create();
                }

                if (fileInfo.Exists)
                {
                    fileInfo.Delete();
                }
            }
            catch (ArgumentException)
            {
            }
            catch (PathTooLongException)
            {
            }
            catch (Exception e)
            {
                s_output.WriteLine(e.Message);
            }
        }

        // Used to generate a unique name for an input file, and write that file, based on a specified command line.
        private string CreateInputFile(string commandLine)
        {
            string fileName = Path.Combine(XsltcModule.TargetDirectory, Guid.NewGuid() + ".ipf");

            File.WriteAllText(fileName, commandLine);

            return fileName;
        }

        private string GetPath(string fileName)
        {
            return XsltcModule.TargetDirectory + Path.DirectorySeparatorChar + fileName;
        }

        /// <summary>
        ///     Currently this method supports only 1 input file. For variations that require more than one input file to test
        ///     @file
        ///     functionality, custom-craft and write those input files in the body of the variation method, then pass an
        ///     appropriate
        ///     commandline such as @file1 @file2 @file3, along with createFromInputFile = false.
        /// </summary>
        /// <param name="commandLine"></param>
        /// <param name="createFromInputFile"></param>
        /// <param name="expectedToSucceed"></param>
        /// <param name="targetDirectory"></param>
        /// <returns></returns>
        private string TryCreatePersistedTransformAssembly(string commandLine, bool createFromInputFile, bool expectedToSucceed, string targetDirectory)
        {
            // If createFromInputFile is specified, create an input file now that the compiler can consume.
            string processArguments = createFromInputFile ? "@" + CreateInputFile(commandLine) : commandLine;

            var processStartInfo = new ProcessStartInfo
            {
                FileName = XsltVerificationLibrary.SearchPath("xsltc.exe"),
                Arguments = processArguments,
                //WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WorkingDirectory = targetDirectory
            };

            // Call xsltc to create persistant assembly.
            var compilerProcess = new Process
            {
                StartInfo = processStartInfo
            };

            compilerProcess.Start();
            string output = compilerProcess.StandardOutput.ReadToEnd();
            compilerProcess.WaitForExit();

            if (createFromInputFile)
            {
                SafeDeleteFile(processArguments.Substring(1));
            }

            if (expectedToSucceed)
            {
                // The Assembly was created successfully
                if (compilerProcess.ExitCode == 0)
                {
                    return output;
                }

                throw new CTestFailedException("Failed to create assembly: " + output);
            }

            return output;
        }

        public class AssemblyLoader //: MarshalByRefObject
        {
            public AssemblyLoader(string asmName)
            {
            }

            public bool Verify(string asmName, string typeName, string baselineFile, bool pdb)
            {
                try
                {
                    var xslt = new XslCompiledTransform();
                    Assembly xsltasm = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(asmName));

                    if (xsltasm == null)
                    {
                        //_output.WriteLine("Could not load file");
                        return false;
                    }

                    Type t = xsltasm.GetType(typeName);

                    if (t == null)
                    {
                        //_output.WriteLine("No type loaded");
                        return false;
                    }

                    xslt.Load(t);

                    var inputXml = new XmlDocument();

                    using (var stream = new MemoryStream())
                    using (var sw = new StreamWriter(stream) { AutoFlush = true })
                    {
                        inputXml.LoadXml("<foo><bar>Hello, world!</bar></foo>");
                        xslt.Transform(inputXml, null, sw);

                        if (!XsltVerificationLibrary.CompareXml(Path.Combine(XsltcModule.TargetDirectory, baselineFile), stream))
                        {
                            //_output.WriteLine("Baseline file comparison failed");
                            return false;
                        }
                    }

                    return true;
                }
                catch (Exception e)
                {
                    s_output.WriteLine(e.Message);
                    return false;
                }
            }

            private static byte[] loadFile(string filename)
            {
                using (var fs = new FileStream(filename, FileMode.Open))
                {
                    var buffer = new byte[(int)fs.Length];
                    fs.Read(buffer, 0, buffer.Length);
                    return buffer;
                }
            }
        }
    }
}