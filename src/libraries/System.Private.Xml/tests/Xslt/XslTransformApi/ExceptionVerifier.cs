// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;
using Xunit.Abstractions;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using System.Xml;

namespace System.Xml.Tests
{
    public class LineInfo
    {
        public int LineNumber { get; private set; }

        public int LinePosition { get; private set; }

        public string FilePath { get; private set; }

        public LineInfo(int lineNum, int linePos)
        {
            LineNumber = lineNum;
            LinePosition = linePos;
            FilePath = String.Empty;
        }

        public LineInfo(int lineNum, int linePos, string filePath)
        {
            LineNumber = lineNum;
            LinePosition = linePos;
            FilePath = filePath;
        }
    }

    [Flags]
    public enum ExceptionVerificationFlags
    {
        None = 0,
        IgnoreMultipleDots = 1,
        IgnoreLineInfo = 2,
    }

    public class ExceptionVerifier
    {
        private readonly Assembly _asm;
        private Assembly _locAsm;
        private readonly Hashtable _resources;

        private string _actualMessage;
        private string _expectedMessage;
        private Exception _ex;

        private ExceptionVerificationFlags _verificationFlags = ExceptionVerificationFlags.None;

        private ITestOutputHelper _output;

        public bool IgnoreMultipleDots
        {
            get
            {
                return (_verificationFlags & ExceptionVerificationFlags.IgnoreMultipleDots) != 0;
            }
            set
            {
                if (value)
                    _verificationFlags = _verificationFlags | ExceptionVerificationFlags.IgnoreMultipleDots;
                else
                    _verificationFlags = _verificationFlags & (~ExceptionVerificationFlags.IgnoreMultipleDots);
            }
        }

        public bool IgnoreLineInfo
        {
            get
            {
                return (_verificationFlags & ExceptionVerificationFlags.IgnoreLineInfo) != 0;
            }
            set
            {
                if (value)
                    _verificationFlags = _verificationFlags | ExceptionVerificationFlags.IgnoreLineInfo;
                else
                    _verificationFlags = _verificationFlags & (~ExceptionVerificationFlags.IgnoreLineInfo);
            }
        }

        private const string ESCAPE_ANY = "~%anything%~";
        private const string ESCAPE_NUMBER = "~%number%~";

        public ExceptionVerifier(string assemblyName, ExceptionVerificationFlags flags, ITestOutputHelper output)
        {
            _output = output;

            if (assemblyName == null)
                throw new VerifyException("Assembly name cannot be null");

            _verificationFlags = flags;

            try
            {
                switch (assemblyName.ToUpper())
                {
                    case "SYSTEM.XML":
                        {
                            var dom = new XmlDocument();
                            _asm = dom.GetType().GetTypeInfo().Assembly;
                        }
                        break;
                    //case "SYSTEM.DATA":
                    //{
                    //    var ds = new DataSet();
                    //    asm = ds.GetType().Assembly;
                    //}
                    //    break;
                    default:
                        throw new FileLoadException("Cannot load assembly from " + GetRuntimeInstallDir() + assemblyName + ".dll");
                        //asm = Assembly.LoadFrom(GetRuntimeInstallDir() + assemblyName + ".dll");
                        //break;
                }

                if (_asm == null)
                    throw new VerifyException("Can not load assembly " + assemblyName);

                // let's determine if this is a loc run, if it is then we need to load satellite assembly
                _locAsm = null;
                if (!CultureInfo.CurrentCulture.Equals(new CultureInfo("en-US")) && !CultureInfo.CurrentCulture.Equals(new CultureInfo("en")))
                {
                    try
                    {
                        // load satellite assembly
                        _locAsm = _asm.GetSatelliteAssembly(new CultureInfo(CultureInfo.CurrentCulture.Parent.IetfLanguageTag));
                    }
                    catch (FileNotFoundException e1)
                    {
                        _output.WriteLine(e1.ToString());
                    }
                    catch (FileLoadException e2)
                    {
                        _output.WriteLine(e2.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                _output.WriteLine("Exception: " + e.Message);
                _output.WriteLine("Stack: " + e.StackTrace);
                throw new VerifyException("Error while loading assembly");
            }

            string[] resArray;
            Stream resStream = null;
            var bFound = false;

            // Check that assembly manifest has resources
            if (null != _locAsm)
                resArray = _locAsm.GetManifestResourceNames();
            else
                resArray = _asm.GetManifestResourceNames();

            foreach (var s in resArray)
            {
                if (s.EndsWith(".resources"))
                {
                    resStream = null != _locAsm ? _locAsm.GetManifestResourceStream(s) : _asm.GetManifestResourceStream(s);
                    bFound = true;
                    if (bFound && resStream != null)
                    {
                        // Populate hashtable from resources
                        var resReader = new ResourceReader(resStream);
                        if (_resources == null)
                        {
                            _resources = new Hashtable();
                        }
                        var ide = resReader.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            if (!_resources.ContainsKey(ide.Key.ToString()))
                                _resources.Add(ide.Key.ToString(), ide.Value.ToString());
                        }
                        resReader.Dispose();
                    }
                    //break;
                }
            }

            if (!bFound || resStream == null)
                throw new VerifyException("GetManifestResourceStream() failed");
        }

        private static string GetRuntimeInstallDir()
        {
            // Get mscorlib path
            var s = typeof(object).GetTypeInfo().Module.FullyQualifiedName;
            // Remove mscorlib.dll from the path
            return Directory.GetParent(s).ToString() + "\\";
        }

        public ExceptionVerifier(string assemblyName, ITestOutputHelper output)
            : this(assemblyName, ExceptionVerificationFlags.None, output)
        { }

        private void ExceptionInfoOutput()
        {
            // Use reflection to obtain "res" property value
            var exceptionType = _ex.GetType();
            var fInfo = exceptionType.GetField("res", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase) ??
                        exceptionType.GetTypeInfo().BaseType.GetField("res", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

            if (fInfo == null)
                throw new VerifyException("Cannot obtain Resource ID from Exception.");

            _output.WriteLine(
                            "\n===== Original Exception Message =====\n" + _ex.Message +
                            "\n===== Resource Id =====\n" + fInfo.GetValue(_ex) +
                            "\n===== HelpLink =====\n" + _ex.HelpLink +
                            "\n===== Source =====\n" + _ex.Source /*+
                            "\n===== TargetSite =====\n" + ex.TargetSite + "\n"*/);

            _output.WriteLine(
                            "\n===== InnerException =====\n" + _ex.InnerException +
                            "\n===== StackTrace =====\n" + _ex.StackTrace);
        }

        public string[] ReturnAllMatchingResIds(string message)
        {
            var ide = _resources.GetEnumerator();
            var list = new ArrayList();

            _output.WriteLine("===== All mached ResIDs =====");
            while (ide.MoveNext())
            {
                var resMessage = ide.Value.ToString();

                resMessage = ESCAPE_ANY + Regex.Replace(resMessage, @"\{\d*\}", ESCAPE_ANY) + ESCAPE_ANY;
                resMessage = MakeEscapes(resMessage).Replace(ESCAPE_ANY, ".*");
                if (Regex.Match(message, resMessage, RegexOptions.Singleline).ToString() == message)
                {
                    list.Add(ide.Key);
                    _output.WriteLine("  [" + ide.Key.ToString() + "] = \"" + ide.Value.ToString() + "\"");
                }
            }

            return (string[])list.ToArray(typeof(string[]));
        }

        // Common helper methods used by different overloads of IsExceptionOk()
        private static void CheckNull(Exception e)
        {
            if (e == null)
            {
                throw new VerifyException("NULL exception passed to IsExceptionOk()");
            }
        }

        private void CompareMessages()
        {
            if (IgnoreMultipleDots && _expectedMessage.EndsWith("."))
                _expectedMessage = _expectedMessage.TrimEnd(new char[] { '.' }) + ".";
            _expectedMessage = Regex.Escape(_expectedMessage);
            _expectedMessage = _expectedMessage.Replace(ESCAPE_ANY, ".*");
            _expectedMessage = _expectedMessage.Replace(ESCAPE_NUMBER, @"\d*");

            // ignore case
            _expectedMessage = _expectedMessage.ToLowerInvariant();
            _actualMessage = _actualMessage.ToLowerInvariant();
            if (Regex.Match(_actualMessage, _expectedMessage, RegexOptions.Singleline).ToString() != _actualMessage)
            {
                // Unescape before printing the expected message string
                _expectedMessage = Regex.Unescape(_expectedMessage);
                _output.WriteLine("Mismatch in error message");
                _output.WriteLine("===== Expected Message =====\n" + _expectedMessage);
                _output.WriteLine("===== Expected Message Length =====\n" + _expectedMessage.Length);
                _output.WriteLine("===== Actual Message =====\n" + _actualMessage);
                _output.WriteLine("===== Actual Message Length =====\n" + _actualMessage.Length);
                throw new VerifyException("Mismatch in error message");
            }
        }

        public void IsExceptionOk(Exception e, string expectedResId)
        {
            CheckNull(e);
            _ex = e;
            if (expectedResId == null)
            {
                // Pint actual exception info and quit
                // This can be used to dump exception properties, verify them and then plug them into our expected results
                ExceptionInfoOutput();
                throw new VerifyException("Did not pass resource ID to verify");
            }

            IsExceptionOk(e, new object[] { expectedResId });
        }

        public void IsExceptionOk(Exception e, string expectedResId, string[] paramValues)
        {
            var list = new ArrayList { expectedResId };

            foreach (var param in paramValues)
                list.Add(param);

            IsExceptionOk(e, list.ToArray());
        }

        public void IsExceptionOk(Exception e, string expectedResId, string[] paramValues, LineInfo lineInfo)
        {
            var list = new ArrayList { expectedResId, lineInfo };

            foreach (var param in paramValues)
                list.Add(param);

            IsExceptionOk(e, list.ToArray());
        }

        public void IsExceptionOk(Exception e, object[] IdsAndParams)
        {
            CheckNull(e);
            _ex = e;

            _actualMessage = e.Message;
            _expectedMessage = ConstructExpectedMessage(IdsAndParams);

            CompareMessages();
        }

        private static string MakeEscapes(string str)
        {
            return new[] { "\\", "$", "{", "[", "(", "|", ")", "*", "+", "?" }.Aggregate(str, (current, esc) => current.Replace(esc, "\\" + esc));
        }

        public string ConstructExpectedMessage(object[] IdsAndParams)
        {
            var lineInfoMessage = "";
            var paramList = new ArrayList();
            var paramsStartPosition = 1;

            // Verify that input list contains at least one element - ResId
            if (IdsAndParams.Length == 0 || !(IdsAndParams[0] is string))
                throw new VerifyException("ResID at IDsAndParams[0] missing!");
            string expectedResId = (IdsAndParams[0] as string);

            // Verify that resource id exists in resources
            if (!_resources.ContainsKey(expectedResId))
            {
                ExceptionInfoOutput();
                throw new VerifyException("Resources in [" + _asm.GetName().Name + "] does not contain string resource: " + expectedResId);
            }

            // If LineInfo exist, construct LineInfo message
            if (IdsAndParams.Length > 1 && (IdsAndParams[1] is LineInfo))
            {
                if (!IgnoreLineInfo)
                {
                    var lineInfo = (IdsAndParams[1] as LineInfo);

                    // Xml_ErrorPosition = "Line {0}, position {1}."
                    lineInfoMessage = String.IsNullOrEmpty(lineInfo.FilePath) ? _resources["Xml_ErrorPosition"].ToString() : _resources["Xml_ErrorFilePosition"].ToString();

                    var lineNumber = lineInfo.LineNumber.ToString();
                    var linePosition = lineInfo.LinePosition.ToString();
                    lineInfoMessage = String.IsNullOrEmpty(lineInfo.FilePath) ? String.Format(lineInfoMessage, lineNumber, linePosition) : String.Format(lineInfoMessage, lineInfo.FilePath, lineNumber, linePosition);
                }
                else
                    lineInfoMessage = ESCAPE_ANY;

                lineInfoMessage = " " + lineInfoMessage;
                paramsStartPosition = 2;
            }

            string message = _resources[expectedResId].ToString();
            for (var i = paramsStartPosition; i < IdsAndParams.Length; i++)
            {
                if (IdsAndParams[i] is object[])
                    paramList.Add(ConstructExpectedMessage(IdsAndParams[i] as object[]));
                else
                {
                    if (IdsAndParams[i] == null)
                        paramList.Add(ESCAPE_ANY);
                    else
                        paramList.Add(IdsAndParams[i] as string);
                }
            }

            try
            {
                message = string.Format(message, paramList.ToArray());
            }
            catch (FormatException)
            {
                throw new VerifyException("Mismatch in number of parameters!");
            }

            return message + lineInfoMessage;
        }
    }

    public class VerifyException : Exception
    {
        public VerifyException(string msg)
            : base(msg)
        { }
    }
}