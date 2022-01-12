// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// throw in catch handler

using System;
using System.IO;

namespace TestUtil
{
    // This class implements a string writer that writes to a string buffer and a
    // given text writer, which allows echoing the written string if stdout is
    // specified as the text writer.

    public class StringRecorder : StringWriter
    {
        private TextWriter _outStream;
        private int _outLimit;                   // maximum output size limit in characters
        private bool _bufferIsFull;              // if set, stop writting/recording output

        // Constructs a new StringRecorder that writes to the given TextWriter.
        public StringRecorder(TextWriter ostream, int olimit)
        {
            if (ostream == null)
            {
                throw new ArgumentNullException("ostream", "Output stream cannot be null.");
            }
            this._outStream = ostream;
            this._outLimit = olimit;
            this._bufferIsFull = false;
        }

        public StringRecorder(TextWriter ostream) : this(ostream, 0)
        {
        }

        // Only these three methods need to be overridden in order to override
        // all different overloads of Write/WriteLine methods.

        public override void Write(char c)
        {
            if (!this._bufferIsFull)
            {
                _outStream.Write(c);
                base.Write(c);
                this.CheckOverflow();
            }
        }

        public override void Write(string val)
        {
            if (!this._bufferIsFull)
            {
                _outStream.Write(val);
                base.Write(val);
                this.CheckOverflow();
            }
        }

        public override void Write(char[] buffer, int index, int count)
        {
            if (!this._bufferIsFull)
            {
                _outStream.Write(buffer, index, count);
                base.Write(buffer, index, count);
                this.CheckOverflow();
            }
        }

        protected void CheckOverflow()
        {
            if (this._outLimit > 0 && this.ToString().Length > this._outLimit)
            {
                this._bufferIsFull = true;
                this._outStream.WriteLine("ERROR: Output exceeded maximum limit, extra output will be discarded!");
            }
        }
    }



    // This class represents a test log. It allows for redirecting both stdout
    // and stderr of the test to StringRecorder objects. The redirected output
    // can then be compared to expected output supplied to the class
    // constructor.

    public class TestLog
    {

        const int SUCC_RET_CODE = 100;
        const int FAIL_RET_CODE = 1;
        const int OUTPUT_LIMIT_FACTOR = 100;

        const string IGNORE_STR = "#IGNORE#";

        protected string expectedOut;
        protected string expectedError;
        protected static TextWriter stdOut = System.Console.Out;
        protected static TextWriter stdError = System.Console.Error;
        protected StringWriter testOut;
        protected StringWriter testError;

        public TestLog() : this(null, null)
        {
        }

        public TestLog(object expOut) : this(expOut, null)
        {
        }

        // Creates a new TestLog and set both expected output, and
        // expected error to supplied values.
        public TestLog(object expOut, object expError)
        {
            this.expectedOut = expOut == null ? String.Empty : expOut.ToString();
            this.expectedError = expError == null ? String.Empty : expError.ToString();
        }

        // Start recoding by redirecting both stdout and stderr to
        // string recorders.
        public void StartRecording()
        {
            this.testOut = new StringRecorder(stdOut, this.expectedOut != null ? this.expectedOut.ToString().Length * OUTPUT_LIMIT_FACTOR : 0);
            this.testError = new StringRecorder(stdError, this.expectedError != null ? this.expectedError.ToString().Length * OUTPUT_LIMIT_FACTOR : 0);

            System.Console.SetOut(this.testOut);
            System.Console.SetError(this.testError);
        }

        // Stop recording by resetting both stdout and stderr to their
        // initial values.
        public void StopRecording()
        {
            // For now we disable the ability of stop recoding, so that we still recoed until the program exits.
            // This issue came up with finally being called twice. The first time we stop recoding and from this
            // point on we loose all output.
            //			System.Console.SetOut(stdOut);
            //			System.Console.SetError(stdError);
        }

        // Returns true if both expected output and expected error are
        // identical to actual output and actual error; false otherwise.
        protected bool Identical()
        {
            return this.testOut.ToString().Equals(this.expectedOut) && this.testError.ToString().Equals(this.expectedError);
        }

        // Display differences between expected output and actual output.
        protected string Diff()
        {
            string result = String.Empty;
            if (!this.testOut.ToString().Equals(this.expectedOut))
            {
                string newLine = this.testOut.NewLine;
                string delimStr = newLine[0].ToString();
                string[] actualLines = ((this.ActualOutput.Trim()).Replace(newLine, delimStr)).Split(delimStr.ToCharArray());
                string[] expectedLines = ((this.ExpectedOutput.Trim()).Replace(newLine, delimStr)).Split(delimStr.ToCharArray());
                int commonLineCount = actualLines.Length < expectedLines.Length ? actualLines.Length : expectedLines.Length;
                bool identical = true;
                for (int i = 0; i < commonLineCount && identical; ++i)
                {
                    string actualLine = actualLines[i];
                    string expectedLine = expectedLines[i];
                    bool similar = true;
                    while (!actualLine.Equals(expectedLine) && similar)
                    {
                        bool ignoreMode = false;
                        while (expectedLine.StartsWith(IGNORE_STR))
                        {
                            expectedLine = expectedLine.Substring(IGNORE_STR.Length);
                            ignoreMode = true;
                        }
                        int nextIgnore = expectedLine.IndexOf(IGNORE_STR);
                        if (nextIgnore > 0)
                        {
                            string expectedToken = expectedLine.Substring(0, nextIgnore);
                            int at = actualLine.IndexOf(expectedToken);
                            similar = (at == 0) || (ignoreMode && at > 0);
                            expectedLine = expectedLine.Substring(nextIgnore);
                            actualLine = similar ? actualLine.Substring(at + expectedToken.Length) : actualLine;
                        }
                        else
                        {
                            similar = (ignoreMode && actualLine.EndsWith(expectedLine)) || actualLine.Equals(expectedLine);
                            expectedLine = String.Empty;
                            actualLine = String.Empty;
                        }
                    }
                    if (!similar)
                    {
                        result += ("< " + expectedLines[i] + newLine);
                        result += "---" + newLine;
                        result += ("> " + actualLines[i] + newLine);
                        identical = false;
                    }
                }
                if (identical)
                {
                    for (int i = commonLineCount; i < expectedLines.Length; ++i)
                    {
                        result += ("< " + expectedLines[i] + newLine);
                    }
                    for (int i = commonLineCount; i < actualLines.Length; ++i)
                    {
                        result += ("< " + actualLines[i] + newLine);
                    }
                }
            }
            return result;
        }

        // Verifies test output and error strings. If identical it returns
        // successful return code; otherwise it prints expected output and
        // diff results, and it returns failed result code.
        public int VerifyOutput()
        {
            System.Console.SetOut(stdOut);
            System.Console.SetError(stdError);

            int retCode = -1;
            string diff = this.Diff();
            if (String.Empty.Equals(diff))
            {
                //				stdOut.WriteLine();
                //				stdOut.WriteLine("PASSED");
                retCode = SUCC_RET_CODE;
            }
            else
            {
                stdOut.WriteLine();
                stdOut.WriteLine("FAILED!");
                stdOut.WriteLine();
                stdOut.WriteLine("[EXPECTED OUTPUT]");
                stdOut.WriteLine(this.ExpectedOutput);
                stdOut.WriteLine("[DIFF RESULT]");
                stdOut.WriteLine(diff);
                retCode = FAIL_RET_CODE;
            }
            return retCode;
        }

        // Returns actual test output.
        public string ActualOutput
        {
            get
            {
                return this.testOut.ToString();
            }
        }

        // Returns actual test error.
        public string ActualError
        {
            get
            {
                return this.testError.ToString();
            }
        }

        // Returns expected test output.
        public string ExpectedOutput
        {
            get
            {
                return this.expectedOut.ToString();
            }
        }

        // Returns expected test error.
        public string ExpectedError
        {
            get
            {
                return this.expectedError.ToString();
            }
        }
    }

}

