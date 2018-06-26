// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.XmlDiff;

namespace XmlCoreTest.Common
{
    /*************************************************
    author alexkr
    date   12/14/2005

    This class contains common static methods used to verify the results of an XSLT data-driven test.
    It contains the following methods:
        CompareXml         - used to compare two XML outputs, captured as files, using XmlDiff.
        CompareChecksum    - used to compare two non-XML outputs, captured as files, using a Checksum calculation.
        CompareException   - used to compare two Exceptions, or the meta-data of two Exceptions, using an Exception wrapper class.

    ************************************************/

    public class XsltVerificationLibrary
    {
        public static string winSDKPath = string.Empty;

        /*************************************************
        XMLDiff compare for XSLTV2 data driven tests.
        It supports custom data-driven xmldiff options passed from the command line,
        It might support an optional helperObject that can perform delayed logging, but does not yet.
        ************************************************/

        public static bool CompareXml(string baselineFile, string actualFile, string xmldiffoptionvalue)
        {
            return CompareXml(baselineFile, actualFile, xmldiffoptionvalue, null);
        }

        public static bool CompareXml(string baselineFile, string actualFile, string xmldiffoptionvalue, DelayedWriteLogger logger)
        {
            using (var fsActual = new FileStream(actualFile, FileMode.Open, FileAccess.Read))
            using (var fsExpected = new FileStream(baselineFile, FileMode.Open, FileAccess.Read))
            {
                return CompareXml(fsActual, fsExpected, xmldiffoptionvalue, logger);
            }
        }

        public static bool CompareXml(string baselineFile, Stream actualStream)
        {
            actualStream.Seek(0, SeekOrigin.Begin);

            using (var expectedStream = new FileStream(baselineFile, FileMode.Open, FileAccess.Read))
                return CompareXml(expectedStream, actualStream, string.Empty, null);
        }

        public static bool CompareXml(Stream expectedStream, Stream actualStream, string xmldiffoptionvalue, DelayedWriteLogger logger)
        {
            bool bResult = false;

            // Default Diff options used by XSLT V2 driver.
            int defaultXmlDiffOptions = (int)(XmlDiffOption.InfosetComparison | XmlDiffOption.IgnoreEmptyElement | XmlDiffOption.IgnoreAttributeOrder);
            XmlDiff diff = new XmlDiff();

            if (xmldiffoptionvalue == null || xmldiffoptionvalue.Equals(string.Empty))
                diff.Option = (XmlDiffOption)defaultXmlDiffOptions;
            else
            {
                if (logger != null) logger.LogMessage("Custom XmlDiffOptions used. Value passed is " + xmldiffoptionvalue);
                diff.Option = (XmlDiffOption)int.Parse(xmldiffoptionvalue);
            }

            XmlParserContext context = new XmlParserContext(new NameTable(), null, "", XmlSpace.None);

            try
            {
                bResult =
                   diff.Compare(new XmlTextReader(actualStream, XmlNodeType.Element, context),
                             new XmlTextReader(expectedStream, XmlNodeType.Element, context));
            }
            catch (Exception e)
            {
                bResult = false;
                if (logger != null)
                {
                    logger.LogMessage("Exception thrown in XmlDiff compare!");
                    logger.LogXml(e.ToString());
                    throw;
                }
            }

            if (bResult)
                return true;

            if (logger != null)
            {
                logger.LogMessage("Mismatch in XmlDiff");
                logger.LogMessage("Actual result: ");
            }

            return false;
        }

        /*************************************************
        Checksum calculation. Legacy.
        ************************************************/

        public static bool CompareChecksum(string Baseline, string OutFile, int driverVersion)
        {
            return CompareChecksum(Baseline, OutFile, driverVersion, null);
        }

        public static bool CompareChecksum(string Baseline, string OutFile, int driverVersion, DelayedWriteLogger logger)
        {
            return CompareChecksum(
               Baseline,
               1,                //start from the first line in the baseline file
               OutFile,
               1,                //start from the first line in the output file
               driverVersion,
               logger);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="Baseline"></param>
        /// <param name="baselineStartLine">The line to start comparison in the baseline file</param>
        /// <param name="OutFile"></param>
        /// <param name="outFileStartLine">The line to start comparison in the output file</param>
        /// <param name="driverVersion"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static bool CompareChecksum(string Baseline, int baselineStartLine, string OutFile, int outFileStartLine, int driverVersion, DelayedWriteLogger logger)
        {
            // Keep people honest.
            if (driverVersion == 2)
            {
                if (logger != null) logger.LogMessage("Calculating checksum for baseline output {0}...", Baseline);

                string expectedCheckSum = CalcChecksum(Baseline, baselineStartLine, logger);
                string actualChecksum = CalcChecksum(OutFile, outFileStartLine, logger);

                if (expectedCheckSum.Equals(actualChecksum))
                    return true;
                else
                {
                    if (logger != null)
                    {
                        logger.LogMessage("Actual checksum: {0}, Expected checksum: {1}", actualChecksum, expectedCheckSum);
                        logger.LogMessage("Actual result: ");
                        logger.WriteOutputFileToLog(OutFile);
                    }
                    return false;
                }
            }
            else throw new NotSupportedException("Not a supported driver version");
        }

        private static string CalcChecksum(string fileName, DelayedWriteLogger logger)
        {
            return CalcChecksum(fileName, 1, logger);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="startFromLine">The line to start calculating the checksum. Any text before this line is ignored. First line is 1.</param>
        /// <param name="logger"></param>
        /// <returns></returns>
        private static string CalcChecksum(string fileName, int startFromLine, DelayedWriteLogger logger)
        {
            const int BUFFERSIZE = 4096;
            decimal dResult = 0;        // Numerical value of the checksum
            int i = 0;                  // Generic counter
            int cBytesRead = 1;        // # of bytes read at one time
            int cTotalRead = 0;         // Total # of bytes read so far
            decimal dEndBuffer = 0;     // Buffer to remove from the end (This is necessary because
            // notepad adds CR/LF onto the end of every file)
            char[] rgBuffer = new char[BUFFERSIZE];

            string xml = "";

            StreamReader fs = null;

            try
            {
                fs = new StreamReader(new FileStream(fileName, FileMode.Open, FileAccess.Read));

                //switch to the line to start from, lines start from 1
                for (int j = 1; j < startFromLine; j++)
                {
                    fs.ReadLine();
                }

                cBytesRead = fs.Read(rgBuffer, 0, BUFFERSIZE);

                while (cBytesRead > 0)
                {
                    // Keep XML property up to date
                    xml = string.Concat(xml, new string(rgBuffer, 0, cBytesRead));

                    // Calculate the checksum
                    for (i = 0; i < cBytesRead; i++)
                    {
                        dResult += Math.Round((decimal)(rgBuffer[i] / (cTotalRead + i + 1.0)), 10);
                    }

                    cTotalRead += cBytesRead;
                    dEndBuffer = 0;

                    // Keep reading (in case file is bigger than 4K)
                    cBytesRead = fs.Read(rgBuffer, 0, BUFFERSIZE);
                }
            }
            catch (Exception ex)
            {
                if (logger != null) logger.LogXml(ex.ToString());
                return "";
            }
            finally
            {
                if (fs != null) fs.Dispose();
            }
            return Convert.ToString(dResult - dEndBuffer, NumberFormatInfo.InvariantInfo);
        }

        // Is this really all there is to it?
        public static bool DetectEmittedPDB(string fileName)
        {
            if (File.Exists(fileName)) return true;
            else return File.Exists(fileName + ".pdb");
        }

        // Call PEVerify on the Assembly name, parse the output,
        // and return true if the output is PASS and false if the output is FAIL.
        public static bool VerifyAssemblyUsingPEVerify(string asmName, bool isValidCase, DelayedWriteLogger logger, ref string output)
        {
            Debug.Assert(asmName != null);
            Debug.Assert(asmName != string.Empty);

            if (!asmName.Contains(".dll") || !asmName.Contains(".DLL"))
                asmName = asmName + ".dll";

            if (File.Exists(asmName))
            {
                return VerifyAssemblyUsingPEVerify(asmName, logger, ref output);
            }
            else
            {
                if (isValidCase)
                {
                    string message = "PEVerify could not be run, no assembly present: " + asmName;
                    if (logger != null)
                        logger.LogMessage(message);
                    output = message;
                    return false;
                }
                else return true;
            }
        }

        public static bool VerifySingleAssemblyUsingPEVerify(string asmName, DelayedWriteLogger logger, ref string output)
        {
            Debug.Assert(asmName != null);
            Debug.Assert(asmName != string.Empty);

            bool result = false;

            if (File.Exists(asmName))
            {
                //add double quotes for names with whitespace in them
                string processArguments = " /quiet " + "\"" + asmName + "\"";

                // Call PEVerify to verify persistant assembly.
                Process peVerifyProcess = new Process();
                peVerifyProcess.StartInfo.FileName = SearchPath("peverify.exe");
                peVerifyProcess.StartInfo.Arguments = " " + processArguments;
                //peVerifyProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                peVerifyProcess.StartInfo.CreateNoWindow = true;
                peVerifyProcess.StartInfo.UseShellExecute = false;
                peVerifyProcess.StartInfo.RedirectStandardOutput = true;

                peVerifyProcess.Start();
                output = peVerifyProcess.StandardOutput.ReadToEnd();
                peVerifyProcess.WaitForExit();

                // Check the output for the Assembly name, and the word "PASS"
                // For example:
                // C:>peverify /quiet 4AC8BD29F3CB888FAD76F7C08FD57AD3.dll
                // 4AC8BD29F3CB888FAD76F7C08FD57AD3.dll PASS
                if (peVerifyProcess.ExitCode == 0 && output.Contains(asmName) && output.Contains("PASS"))
                    result = true;
                else
                {
                    if (logger != null)
                        logger.LogMessage("PEVerify could not be run or FAILED : {0}", asmName + " " + output);
                    result = false;
                }
            }
            else
            {
                if (logger != null)
                    logger.LogMessage("Assembly file could not be found : {0}", asmName);
                result = false;
            }

            return result;
        }

        public static bool VerifyAssemblyUsingPEVerify(string asmName, DelayedWriteLogger logger, ref string output)
        {
            string scriptAsmNameFormat = Path.ChangeExtension(asmName, null) + "_Script{0}.dll";
            int scriptCounter = 0;
            string testAsm = asmName;
            bool result = false;

            do
            {
                result = VerifySingleAssemblyUsingPEVerify(testAsm, logger, ref output);
                testAsm = string.Format(scriptAsmNameFormat, ++scriptCounter);
            }
            while (result && File.Exists(testAsm));

            return result;
        }

        public static string SearchPath(string fileName)
        {
            var locations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 32 bit if on 64 bit Windows
            locations.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft SDKs\Windows"));
            // 32 bit if on 32 bit Windows, otherwise 64 bit
            locations.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Microsoft SDKs\Windows"));
            // 64 bit if in 32 bit process on 64 bit Windows
            locations.Add(Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432"), @"Microsoft SDKs\Windows"));

            var files = new List<string>();

            foreach (var location in locations)
            {
                if (Directory.Exists(location))
                    files.AddRange(Directory.GetFiles(location, fileName, SearchOption.AllDirectories));
            }

            if (files.Count == 0)
                throw new FileNotFoundException(fileName);

            // Crudely prefer newer versions, eg 4.6.2 over 4.6.1,
            // but it currently is not important
            files.Sort(StringComparer.OrdinalIgnoreCase);

            return files[files.Count - 1];
        }
    }

    /*************************************************
    This class is a delayed-write message logger, similar to CError.WriteLine etc. It is
    instantiated by your XsltDriver and used to record messages that you would normally write
    with CError.WriteLine, CError.WriteLineIgnore, or CError.WriteXml, in a buffered manner.
    The messages can be written to the final log file using WriteOutputFileToLog() by your driver code
    if there is a variation failure, or just discarded when a test passes to keep results.log files
    within a managable size on disk.

    As an example, using this method reduced the size of the XSLT V2 log file from 3MB per run to around 300k.
    ************************************************/

    public class DelayedWriteLogger
    {
        private ArrayList _messageLog;

        public DelayedWriteLogger()
        {
            _messageLog = new ArrayList();
        }

        // Writes the buffer of messages to the results.log file.
        public void WriteOutputFileToLog(string fileName)
        {
            StreamReader fs = null;
            try
            {
                char[] rgBuffer = new char[4096];

                fs = new StreamReader(new FileStream(fileName, FileMode.Open, FileAccess.Read));

                int cBytesRead = fs.Read(rgBuffer, 0, 4096);
                while (cBytesRead > 0)
                {
                    this.Log(new string(rgBuffer, 0, cBytesRead));
                    cBytesRead = fs.Read(rgBuffer, 0, 4096);
                }
            }
            finally
            {
                if (fs != null) fs.Dispose();
                this.LogMessage(string.Empty);
            }
        }

        public void Log(string msg)
        {
            LogMessage(MessageType.Write, msg, null);
        }

        public void LogMessage(string msg)
        {
            LogMessage(MessageType.WriteLine, msg, null);
        }

        public void LogMessage(string msg, params object[] args)
        {
            LogMessage(MessageType.WriteLineParams, msg, args);
        }

        public void LogMessageIgnore(string msg)
        {
            LogMessage(MessageType.WriteLineIgnore, msg, null);
        }

        public void LogXml(string msg)
        {
            LogMessage(MessageType.Xml, msg, null);
        }

        private void LogMessage(MessageType msgtype, string msg, params object[] args)
        {
            LogMessage message = new LogMessage(msgtype, msg, args);
            _messageLog.Add(message);
        }

        public void WriteLoggedMessages()
        {
            foreach (LogMessage mg in _messageLog)
            {
                switch (mg.type)
                {
                    case MessageType.Write:
                        CError.Write(mg.msg);
                        break;

                    case MessageType.WriteLine:
                        CError.WriteLine(mg.msg);
                        break;

                    case MessageType.WriteLineIgnore:
                        CError.WriteLineIgnore(mg.msg);
                        break;

                    case MessageType.WriteLineParams:
                        CError.WriteLine(mg.msg, mg.args);
                        break;

                    case MessageType.Xml:
                        CError.WriteXml(mg.msg);
                        break;
                }
            }
        }
    }

    // used by DelayedWriteLogger
    internal enum MessageType
    {
        Write,
        WriteLine,
        WriteLineParams,
        WriteLineIgnore,
        Xml
    }

    // used by DelayedWriteLogger
    internal class LogMessage
    {
        public LogMessage(MessageType t, string m, object[] o)
        {
            type = t;
            msg = m;
            args = o;
        }

        internal MessageType type;
        public string msg;
        public object[] args;
    }

    /*************************************************
    author alexkr
    date   12/12/2005

    This class is used to compare expected exceptions with actual exceptions in the CompareException
    method, used by the XSLT V2 data driven driver.

    Its use is somewhat subtle, so please read all comments, understand the Equals method, and ask for
    introduction before you use it in any new code, or modify existing code. Why? There are many
    assumptions made when using this class. Here are some...

       Non-Xml Exceptions are always compared by type, and must never have ExceptionResourceId or ExceptionMessage attributes in a control file.
       Xml Exceptions can be either ExceptionId only, ExceptionId and ExceptionMessage for all Xml_UserException exceptions, or ExceptionId and ExceptionResourceId for all non-Xml_UserExceptions
       Never have both ExceptionResourceId AND ExceptionMessage attributes set for the same Exception meta-data. The behavior is undefined.
       Xml Exceptions that are Xml_UserExceptions and include a MessageFragment will only be compared in ENU runs. Globalized runs resort to Type comparison (ExceptionId) only.
       Xml Exceptions that are non-Xml_UserExceptions and include an ExceptionResourceId will be compared for all runs.

    **************************************************/

    public class XsltRuntimeException
    {
        public static string XMLUSEREX = "Xml_UserException";

        private XsltRuntimeException()
        {
        } // no op, do not call

        public XsltRuntimeException(string a, bool d)
            : this(a, XMLUSEREX, string.Empty, d)
        {
        }

        public XsltRuntimeException(string a, string b, string c, bool d)
        {
            ExceptionId = a;
            ResourceId = b;
            MessageFragment = c;
            _examineMessages = d;
        }

        public string ExceptionId;
        public string ResourceId;
        public string MessageFragment;
        private bool _examineMessages;

        public override string ToString()
        {
            return ExceptionId + "\n" + ResourceId + "\n" + MessageFragment;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        // The Equals method must always be used this way
        // ExpectedException.Equals( ActualException )?
        // Please note that if ExamineMessages is true, we are in a non-localized build and we can safely examine exception messages. If false, we fall back to type comparison exclusively.
        public override bool Equals(object o)
        {
            XsltRuntimeException objectToCompareTo = o as XsltRuntimeException;

            Debug.Assert(objectToCompareTo != null);
            Debug.Assert(objectToCompareTo.ExceptionId != null, "ExceptionId must be initialized to a valid fully qualified type name");
            Debug.Assert(objectToCompareTo.ResourceId != null, "ResourceId must be initialized to String.Empty, in which case MessageFragment is used, or a legal resource Id");
            Debug.Assert(objectToCompareTo.MessageFragment != null, "MessageFragment must be initialized to String.Empty, in which case ResourceId is used, or a string that represents a specific exception message.");
            Debug.Assert(this.ExceptionId != null, "ExceptionId must be initialized to a valid fully qualified type name");
            Debug.Assert(this.ResourceId != null, "ResourceId must be initialized to String.Empty, in which case MessageFragment is used, or a legal resource Id");
            Debug.Assert(this.MessageFragment != null, "MessageFragment must be initialized to String.Empty, in which case ResourceId is used, or a string that represents a specific exception message.");

            // Inside this block, we have two properly initialized XsltRuntimeExceptions to compare.

            /////
            // There are several basic comparisons, each with its own degree of certainty.
            //
            // The first comparison is the original used by the driver, which we no longer use: if there is any
            //     exception at all for an expected "Invalid" case, we match.
            //
            // The second comparison is the initial revision to the driver: an Exception type comparison. This is the least certain.
            //     type matches should no longer exist once this exception work is complete. These will be flagged by failing the test.
            //
            // The third comparison is for Xml_UserExceptions. In this case we try to compare the Exception Type and Exception Message.
            //     If the Exception type does not match, but the message does, we count that as a match. Note that this match is NOT
            //     immue to the effects of globalized resource strings, and hence is less certain.
            //
            // The fourth comparison is for NON Xml_UserExceptions. In this case we try to compare the Exception Type and ResourceId.
            //     If the Exception type does not match, but the resource id does, we count that a match. Note that this match is
            //     immune to the effects of globalized resource strings. It is the most certain match we can make.
            //
            /////
            if (!IsXmlException(this.ExceptionId))
            {
                // Aye, here's the dillema. If its not an XML exception we can't look up its resource ID.
                // So in these cases (very rare anyway) we revert to the old simple days of comparing type to type.
                return objectToCompareTo.ExceptionId.Equals(this.ExceptionId);
            }
            else
            {
                if (!objectToCompareTo.ResourceId.Equals(string.Empty) && !objectToCompareTo.ResourceId.Equals(XMLUSEREX) &&
                    objectToCompareTo.ResourceId.Equals(this.ResourceId))
                {
                    //**** The highest degree of certainty we have. ****//

                    if (objectToCompareTo.ExceptionId.Equals(this.ExceptionId))
                        return true;
                    // ResourceIds match, but Exception types dont.
                    else
                    {
                        CError.WriteLine("match?\n {0} \ncompare to \n {1} \n pls investigate why this resource id is being thrown in 2 differet exceptions", objectToCompareTo.ToString(), this.ToString());
                        return true;
                    }
                }
                // ResourceId is Empty or Xml_UserException, or they dont match
                else if (!this.MessageFragment.Equals(string.Empty) && _examineMessages)
                {
                    if (objectToCompareTo.ResourceId.Equals(this.ResourceId) &&
                        objectToCompareTo.MessageFragment.Contains(this.MessageFragment))
                    {
                        //**** Very high certainty equivalence ****//

                        if (objectToCompareTo.ExceptionId.Equals(this.ExceptionId))
                            return true;

                        // Messages match, but Exception types dont.
                        else
                        {
                            CError.WriteLine("match?(message matches but exact typename doesn't)\n {0} \ncompare to \n {1} ", objectToCompareTo.ToString(), this.ToString());
                            return false; // for now.
                        }
                    }
                }
                // ResourceId is Empty or Xml_UserException or they dont match, Message is Empty or they dont match
                else
                {
                    //**** Lower degree of certainty ****//
                    // This is an exception that has not yet been updated in the control file with complete information.
                    // Use the older, less flexible comparison logic.
                    CError.WriteLine("old comparison:{0} \ncompare to \n {1} ", objectToCompareTo.ToString(), this.ToString());
                    return objectToCompareTo.ExceptionId.Equals(this.ExceptionId);
                }
            }
            return false;
        }

        public static ArrayList XmlExceptionList = new ArrayList(5);

        private static void setXmlExceptionList()
        {
            XmlExceptionList.Add("System.Xml.Xsl.XPath.XPathCompileException");
            XmlExceptionList.Add("System.Xml.Xsl.XslLoadException");
            XmlExceptionList.Add("System.Xml.Xsl.XslTransformException");
            XmlExceptionList.Add("System.Xml.Xsl.XsltException");
            XmlExceptionList.Add("System.Xml.XmlException");
        }

        public static bool IsXmlException(string typeName)
        {
            setXmlExceptionList();
            return XmlExceptionList.Contains(typeName);
        }
    }
}
