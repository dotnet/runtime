// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#define DEBUG
using System.Reflection;
using Xunit;

namespace System.Diagnostics.Tests
{
    // These tests test the static Debug class. They cannot be run in parallel
    // Shows that Debug behaves identically on all of the test cases in DebugTestsNoListeners even when DebugUsesTraceListeners is true.
    [Collection("System.Diagnostics.Debug")]
    public class DebugTestsUsingListeners : DebugTestsNoListeners
    {
        protected override bool DebugUsesTraceListeners { get { return true; } }

        [Fact]
        public void Trace_Write_TraceListenerAlwaysIndentsOnFirstCall()
        {
            Trace.Indent();
            int expectedIndentation = Trace.IndentLevel * Trace.IndentSize;

            VerifyLogged(() => Debug.Write("pizza"),    new string(' ', expectedIndentation) + "pizza");
            VerifyLogged(() => Debug.Write("pizza"),    "pizza");
            VerifyLogged(() => Trace.Write("pizza"),    "pizza");

            Trace.Listeners.Clear();
            // New TraceListener indents on first Debug call
            Trace.Listeners.Add(new DefaultTraceListener());
            VerifyLogged(() => Trace.Write("pizza"),    new string(' ', expectedIndentation) + "pizza");
            VerifyLogged(() => Debug.Write("pizza"),    "pizza");

            Trace.Listeners.Clear();
            // New TraceListener indents on first Trace call
            Trace.Listeners.Add(new DefaultTraceListener());
            VerifyLogged(() => Debug.Write("pizza"),    new string(' ', expectedIndentation) + "pizza");
            VerifyLogged(() => Trace.Write("pizza"),    "pizza");

            TraceListener secondListener = new DefaultTraceListener();
            Trace.Listeners.Add(secondListener);
            // Only new TraceListener will indent on its first Trace/Debug call:
            VerifyLogged(() => Debug.Write("pizza"),    "pizza" + new string(' ', expectedIndentation) + "pizza");
            VerifyLogged(() => Trace.Write("pizza"),    "pizza" + "pizza");

            TraceListener thirdListener = new DefaultTraceListener();
            Trace.Listeners.Add(thirdListener);
            // Only new TraceListener will indent on its first Trace/Debug call:
            VerifyLogged(() => Trace.Write("pizza"),    "pizza" + "pizza" + new string(' ', expectedIndentation) + "pizza");
            VerifyLogged(() => Debug.Write("pizza"),    "pizza" + "pizza" + "pizza");

            Trace.Listeners.Remove(secondListener);
            Trace.Listeners.Remove(thirdListener);
            Trace.Unindent();
            GoToNextLine();
        }

        [Fact]
        public void Trace_Write_Indents()
        {
            // This test when run alone verifies Trace.Write indentation, even on first call, is correct.
            Trace.Indent();
            VerifyLogged(() => Trace.Write("pizza"),        new string(' ', Trace.IndentLevel * Trace.IndentSize) +  "pizza");
            Trace.Unindent();

            GoToNextLine();
        }

        [Fact]
        public void Trace_WriteLine_Indents()
        {
            // This test when run alone verifies Debug.WriteLine indentation, even on first call, is correct.
            Debug.Indent();
            VerifyLogged(() => Trace.WriteLine("pizza"),    new string(' ', Trace.IndentLevel * Trace.IndentSize) +  "pizza" + Environment.NewLine);
            Debug.Unindent();
        }

        [Fact]
        public void Trace_WriteLine_WontIndentAfterWrite()
        {
            Trace.Indent();
            int expectedIndentation = Trace.IndentLevel * Trace.IndentSize;

            VerifyLogged(() => Trace.Write("pizza"),        new string(' ', expectedIndentation) +  "pizza");

            // WriteLine wont indent after Write:
            VerifyLogged(() => Trace.WriteLine("pizza"),    "pizza" + Environment.NewLine);

            VerifyLogged(() => Trace.WriteLine("pizza"),    new string(' ', expectedIndentation) +  "pizza" + Environment.NewLine);
            VerifyLogged(() => Trace.Write("pizza"),        new string(' ', expectedIndentation) +  "pizza");

            // WriteLine wont indent after Write:
            VerifyLogged(() => Trace.WriteLine("pizza"),    "pizza" + Environment.NewLine);
            VerifyLogged(() => Trace.WriteLine("pizza"),    new string(' ', expectedIndentation) +  "pizza" + Environment.NewLine);
            Trace.Unindent();
        }

        [Fact]
        public void Debug_WriteLine_SameIndentationForBothTraceAndDebug()
        {
            Trace.Indent();
            int expected = Debug.IndentSize * Debug.IndentLevel;

            VerifyLogged(() => Debug.WriteLine("pizza"), new string(' ', expected) +  "pizza" + Environment.NewLine);
            VerifyLogged(() => Trace.WriteLine("pizza"), new string(' ', expected) +  "pizza" + Environment.NewLine);

            // reset
            Trace.Unindent();
        }

        [Fact]
        public void Trace_WriteNull_SkipsIndentation()
        {
            Trace.Indent();
            VerifyLogged(() => Debug.Write(null), "");
            VerifyLogged(() => Trace.Write(null), "");
            Trace.Unindent();
        }

        [Fact]
        public void Trace_WriteLineNull_IndentsEmptyStringProperly()
        {
            Trace.Indent();
            int expected = Debug.IndentSize * Debug.IndentLevel;

            VerifyLogged(() => Debug.WriteLine(null), new string(' ', expected) + Environment.NewLine);
            VerifyLogged(() => Trace.WriteLine(null), new string(' ', expected) + Environment.NewLine);

            // reset
            Trace.Unindent();
        }

        [Fact]
        public void Trace_UpdatingDebugIndentation_UpdatesTraceIndentation_AndViceVersa()
        {
            int before = Debug.IndentSize * Debug.IndentLevel;
            Assert.Equal(Debug.IndentSize, Trace.IndentSize);
            Assert.Equal(Debug.IndentSize, Trace.Listeners[0].IndentSize);
            Assert.Equal(Debug.IndentLevel, Trace.IndentLevel);
            Assert.Equal(Debug.IndentLevel, Trace.Listeners[0].IndentLevel);

            Debug.IndentLevel = 3;
            Assert.Equal(3, Trace.IndentLevel);
            Assert.Equal(3, Debug.IndentLevel);
            Assert.Equal(3, Trace.Listeners[0].IndentLevel);

            Debug.IndentLevel = 0;
            Assert.Equal(0, Trace.IndentLevel);
            Assert.Equal(0, Debug.IndentLevel);
            Assert.Equal(0, Trace.Listeners[0].IndentLevel);

            Debug.Indent();
            Assert.Equal(1, Trace.IndentLevel);
            Assert.Equal(1, Debug.IndentLevel);
            Assert.Equal(1, Trace.Listeners[0].IndentLevel);

            Trace.Indent();
            Assert.Equal(2, Trace.IndentLevel);
            Assert.Equal(2, Debug.IndentLevel);
            Assert.Equal(2, Trace.Listeners[0].IndentLevel);

            Debug.Unindent();
            Trace.Unindent();
            Assert.Equal(0, Trace.IndentLevel);
            Assert.Equal(0, Debug.IndentLevel);
            Assert.Equal(0, Trace.Listeners[0].IndentLevel);

            Debug.Unindent();
            Assert.Equal(0, Trace.IndentLevel);
            Assert.Equal(0, Debug.IndentLevel);
            Assert.Equal(0, Trace.Listeners[0].IndentLevel);

            Trace.IndentSize = 7;
            Assert.Equal(7, Debug.IndentSize);
            Assert.Equal(7, Trace.Listeners[0].IndentSize);

            Debug.IndentSize = 4;
            Assert.Equal(4, Trace.IndentSize);
            Assert.Equal(4, Trace.Listeners[0].IndentSize);

            Debug.IndentLevel = 0; // reset
            Assert.Equal(before, Debug.IndentSize * Debug.IndentLevel);
        }

        [Fact]
        public void Trace_Refresh_ResetsIndentSize()
        {
            int before = Debug.IndentSize * Debug.IndentLevel;
            Debug.IndentSize = 5;
            Debug.IndentLevel = 3;
            Trace.Refresh();

            Assert.Equal(4, Debug.IndentSize);
            Assert.Equal(3, Debug.IndentLevel);

            Debug.IndentLevel = 0; // reset
            Assert.Equal(before, Debug.IndentSize * Debug.IndentLevel);
        }

        [Fact]
        public void Trace_ClearTraceListeners_StopsWritingToDebugger()
        {
            VerifyLogged(() => Debug.Write("pizza"), "pizza");
            VerifyLogged(() => Trace.Write("pizza"), "pizza");
            Trace.Listeners.Clear();
            VerifyLogged(() => Debug.Write("pizza"), string.Empty);
            VerifyLogged(() => Trace.Write("pizza"), string.Empty);
            Trace.Refresh();
            VerifyLogged(() => Debug.Write("pizza"), "pizza");
            VerifyLogged(() => Trace.Write("pizza"), "pizza");

            GoToNextLine();
        }

        [Fact]
        public void TraceWriteIf()
        {
            VerifyLogged(() => Trace.WriteIf(true, 5), "5");
            VerifyLogged(() => Trace.WriteIf(false, 5), "");

            VerifyLogged(() => Trace.WriteIf(true, 5, "category"), "category: 5");
            VerifyLogged(() => Trace.WriteIf(false, 5, "category"), "");

            VerifyLogged(() => Trace.WriteIf(true, "logged"), "logged");
            VerifyLogged(() => Trace.WriteIf(false, "logged"), "");

            VerifyLogged(() => Trace.WriteIf(true, "logged", "category"), "category: logged");
            VerifyLogged(() => Trace.WriteIf(false, "logged", "category"), "");

            GoToNextLine();
        }

        [Fact]
        public void TraceWriteLineIf()
        {
            VerifyLogged(() => Trace.WriteLineIf(true, 5), "5" + Environment.NewLine);
            VerifyLogged(() => Trace.WriteLineIf(false, 5), "");

            VerifyLogged(() => Trace.WriteLineIf(true, 5, "category"), "category: 5" + Environment.NewLine);
            VerifyLogged(() => Trace.WriteLineIf(false, 5, "category"), "");

            VerifyLogged(() => Trace.WriteLineIf(true, "logged"), "logged" + Environment.NewLine);
            VerifyLogged(() => Trace.WriteLineIf(false, "logged"), "");

            VerifyLogged(() => Trace.WriteLineIf(true, "logged", "category"), "category: logged" + Environment.NewLine);
            VerifyLogged(() => Trace.WriteLineIf(false, "logged", "category"), "");
        }

        [Fact]
        public void Trace_AssertUiEnabledFalse_SkipsFail()
        {
            var initialListener = (DefaultTraceListener)Trace.Listeners[0];
            Trace.Listeners.Clear();
            Trace.Fail("Skips fail fast");
            Debug.Fail("Skips fail fast");

            initialListener.AssertUiEnabled = false;
            Trace.Listeners.Add(initialListener);
            Debug.Fail("Skips fail fast");
            Trace.Fail("Skips fail fast");

            var myListener = new MyTraceListener();
            string expectedDialog = $"Mock dialog - message: short, detailed message: long";
            Trace.Listeners.Clear();
            Trace.Listeners.Add(myListener);

            try
            {
                myListener.AssertUiEnabled = false;
                Debug.Fail("short", "long");
                Assert.Equal("", myListener.OutputString);
                Trace.Fail("short", "long");
                Assert.Equal("", myListener.OutputString);

                myListener.AssertUiEnabled = true;
                Debug.Fail("short", "long");
                Assert.Equal(expectedDialog, myListener.OutputString);
                Trace.Fail("short", "long");
                Assert.Equal(expectedDialog + expectedDialog, myListener.OutputString);
            }
            finally
            {
                Trace.Listeners.Clear();
                Trace.Listeners.Add(initialListener);
            }
        }

        class MyTraceListener : DefaultTraceListener
        {
            private void FailCore(string stackTrace, string message, string detailMessage, string errorSource)
            {
                OutputString += $"Mock dialog - message: {message}, detailed message: {detailMessage}";
            }
            public string OutputString { get; private set; } = string.Empty;

            public override void Fail(string message, string detailMessage)
            {
                WriteLine(message, detailMessage);
                if (AssertUiEnabled)
                {
                    FailCore(string.Empty, message, detailMessage, "Assertion Failed");
                }
            }
        }
    }
}
