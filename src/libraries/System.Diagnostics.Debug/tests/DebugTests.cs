#define DEBUG

using System;
using System.Diagnostics;

using Xunit;

namespace System.Diagnostics.Tests
{
    public class DebugTests
    {
        [Fact]
        public void Asserts()
        {
            VerifyLogged(() => { Debug.Assert(true); }, "");
            VerifyLogged(() => { Debug.Assert(true, "assert passed"); }, "");
            VerifyLogged(() => { Debug.Assert(true, "assert passed"); }, "");
            VerifyLogged(() => { Debug.Assert(true, "assert passed", "nothing is wrong"); }, "");
            VerifyLogged(() => { Debug.Assert(true, "assert passed", "nothing is wrong {0} {1}", 'a', 'b'); }, "");

            VerifyAssert(() => { Debug.Assert(false); }, "");
            VerifyAssert(() => { Debug.Assert(false, "assert passed"); }, "");
            VerifyAssert(() => { Debug.Assert(false, "assert passed"); }, "");
            VerifyAssert(() => { Debug.Assert(false, "assert passed", "nothing is wrong"); }, "");
            VerifyAssert(() => { Debug.Assert(false, "assert passed", "nothing is wrong {0} {1}", 'a', 'b'); }, "");        
        }

        [Fact]
        public void Fail()
        {
            VerifyAssert(() => { Debug.Fail("something bad happened"); }, "something bad happened");
            VerifyAssert(() => { Debug.Fail("something bad happened", "really really bad"); }, "something bad happened", "really really bad");        
        }

        [Fact]
        public void Write()
        {
            VerifyLogged(() => { Debug.Write(5); }, "5");
            VerifyLogged(() => { Debug.Write((object)null); }, "");
            VerifyLogged(() => { Debug.Write(5, "category"); }, "category:5");
            VerifyLogged(() => { Debug.Write((object)null, "category"); }, "category:");
            VerifyLogged(() => { Debug.Write("logged"); }, "logged");
            VerifyLogged(() => { Debug.Write("logged", "category"); }, "category:logged");
        }

        [Fact]
        public void WriteLine()
        {
            VerifyLogged(() => { Debug.WriteLine(5); }, "5" + Environment.NewLine);
            VerifyLogged(() => { Debug.WriteLine((object)null); }, Environment.NewLine);
            VerifyLogged(() => { Debug.WriteLine(5, "category"); }, "category:5" + Environment.NewLine);
            VerifyLogged(() => { Debug.WriteLine((object)null, "category"); }, "category:" + Environment.NewLine);
            VerifyLogged(() => { Debug.WriteLine("logged"); }, "logged" + Environment.NewLine);         
            VerifyLogged(() => { Debug.WriteLine("logged", "category"); }, "category:logged" + Environment.NewLine);
            VerifyLogged(() => { Debug.WriteLine("{0} {1}", 'a', 'b'); }, "a b" + Environment.NewLine);
        }

        [Fact]
        public void WriteIf()
        {
            VerifyLogged(() => { Debug.WriteIf(true, 5); }, "5");
            VerifyLogged(() => { Debug.WriteIf(false, 5); }, "");

            VerifyLogged(() => { Debug.WriteIf(true, 5, "category"); }, "category:5");
            VerifyLogged(() => { Debug.WriteIf(false, 5, "category"); }, "");

            VerifyLogged(() => { Debug.WriteIf(true, "logged"); }, "logged");
            VerifyLogged(() => { Debug.WriteIf(false, "logged"); }, "");

            VerifyLogged(() => { Debug.WriteIf(true, "logged", "category"); }, "category:logged");
            VerifyLogged(() => { Debug.WriteIf(false, "logged", "category"); }, "");                
        }

        [Fact]
        public void WriteLineIf()
        {
            VerifyLogged(() => { Debug.WriteLineIf(true, 5); }, "5" + Environment.NewLine);
            VerifyLogged(() => { Debug.WriteLineIf(false, 5); }, "");

            VerifyLogged(() => { Debug.WriteLineIf(true, 5, "category"); }, "category:5" + Environment.NewLine);
            VerifyLogged(() => { Debug.WriteLineIf(false, 5, "category"); }, "");

            VerifyLogged(() => { Debug.WriteLineIf(true, "logged"); }, "logged" + Environment.NewLine);
            VerifyLogged(() => { Debug.WriteLineIf(false, "logged"); }, "");

            VerifyLogged(() => { Debug.WriteLineIf(true, "logged", "category"); }, "category:logged" + Environment.NewLine);
            VerifyLogged(() => { Debug.WriteLineIf(false, "logged", "category"); }, "");     
        }


        static void VerifyLogged(Action test, string expectedOutput)
        {
            Debug.IDebugLogger oldLogger = Debug.s_logger;
            Debug.s_logger = WriteLogger.Instance;

            try
            {
                WriteLogger.Instance.Clear();
                test();
                Assert.Equal(expectedOutput, WriteLogger.Instance.LoggedOutput);
            }
            finally
            {
                Debug.s_logger = oldLogger;
            }
        }

        static void VerifyAssert(Action test, params string[] expectedOutputStrings)
        {
            Debug.IDebugLogger oldLogger = Debug.s_logger;
            Debug.s_logger = WriteLogger.Instance;

            try
            {
                WriteLogger.Instance.Clear();
                test();
                for (int i = 0; i < expectedOutputStrings.Length; i++)
                {
                    Assert.Contains(expectedOutputStrings[i], WriteLogger.Instance.LoggedOutput);
                    Assert.Contains(expectedOutputStrings[i], WriteLogger.Instance.AssertUIOutput);
                }

            }
            finally
            {
                Debug.s_logger = oldLogger;
            }            
        }

        class WriteLogger : Debug.IDebugLogger
        {
            public readonly static WriteLogger Instance = new WriteLogger();

            private WriteLogger()
            {

            }

            public string LoggedOutput
            {
                get;
                private set;
            }

            public string AssertUIOutput
            {
                get;
                private set;
            }

            public void Clear()
            {
                LoggedOutput = string.Empty;
                AssertUIOutput = string.Empty;
            }

            public void ShowAssertDialog(string stackTrace, string message, string detailMessage)
            {
                AssertUIOutput += stackTrace + message + detailMessage;
            }

            public void WriteLineCore(string message)
            {
                LoggedOutput += message + Environment.NewLine;
            }

            public void WriteCore(string message)
            {
                LoggedOutput += message;
            }
        }
    }
}
