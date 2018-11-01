// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#define DEBUG
using System.Reflection;
using Xunit;

namespace System.Diagnostics.Tests
{
    // These tests test the static Debug class. They cannot be run in parallel
    // DebugTestsNoListeners: tests Debug behavior before Debug is set with Trace Listeners.
    [Collection("System.Diagnostics.Debug")]
    public class DebugTestsNoListeners : DebugTests
    {
        protected override bool DebugUsesTraceListeners { get { return false; } }

        protected void GoToNextLine()
        {
            // Start from next line before running next test: refer to nameof(Debug_WriteLine_WontIndentAfterWrite)
            VerifyLogged(() => Debug.WriteLine(""), Environment.NewLine);
        }

        [Fact]
        public void Debug_Write_Indents()
        {
            // This test when run alone verifies Debug.Write indentation, even on first call, is correct.
            Debug.Indent();
            VerifyLogged(() => Debug.Write("pizza"),        new string(' ', Debug.IndentLevel * Debug.IndentSize) +  "pizza");
            Debug.Unindent();
            GoToNextLine();
        }

        [Fact]
        public void Debug_WriteLine_Indents()
        {
            // This test when run alone verifies Debug.WriteLine indentation, even on first call, is correct.
            Debug.Indent();
            VerifyLogged(() => Debug.WriteLine("pizza"),    new string(' ', Debug.IndentLevel * Debug.IndentSize) +  "pizza" + Environment.NewLine);
            Debug.Unindent();
        }

        [Fact]
        public void Debug_WriteLine_WontIndentAfterWrite()
        {            
            Debug.Indent();
            int expectedIndentation = Debug.IndentLevel * Debug.IndentSize;
            
            VerifyLogged(() => Debug.Write("pizza"),        new string(' ', expectedIndentation) +  "pizza");
            
            // WriteLine wont indent after Write:
            VerifyLogged(() => Debug.WriteLine("pizza"),    "pizza" + Environment.NewLine);

            VerifyLogged(() => Debug.WriteLine("pizza"),    new string(' ', expectedIndentation) +  "pizza" + Environment.NewLine);
            VerifyLogged(() => Debug.Write("pizza"),        new string(' ', expectedIndentation) +  "pizza");

            // WriteLine wont indent after Write:
            VerifyLogged(() => Debug.WriteLine("pizza"),    "pizza" + Environment.NewLine); 
            VerifyLogged(() => Debug.WriteLine("pizza"),    new string(' ', expectedIndentation) +  "pizza" + Environment.NewLine);
            Debug.Unindent();
        }

        [Fact]
        public void Debug_WriteNull_SkipsIndentation()
        {
            Debug.Indent();
            VerifyLogged(() => Debug.Write(null), "");
            Debug.Unindent();
        }

        [Fact]
        public void Debug_WriteLineNull_IndentsEmptyStringProperly()
        {
            Debug.Indent();
            int expected = Debug.IndentSize * Debug.IndentLevel;

            VerifyLogged(() => Debug.WriteLine(null), new string(' ', expected) + Environment.NewLine);

            // reset
            Debug.Unindent();
        }

        [Fact]
        public void Asserts()
        {
            VerifyLogged(() => Debug.Assert(true), "");
            VerifyLogged(() => Debug.Assert(true, "assert passed"), "");
            VerifyLogged(() => Debug.Assert(true, "assert passed", "nothing is wrong"), "");
            VerifyLogged(() => Debug.Assert(true, "assert passed", "nothing is wrong {0} {1}", 'a', 'b'), "");

            VerifyAssert(() => Debug.Assert(false), "");
            VerifyAssert(() => Debug.Assert(false, "assert passed"), "assert passed");
            VerifyAssert(() => Debug.Assert(false, "assert passed", "nothing is wrong"), "assert passed", "nothing is wrong");
            VerifyAssert(() => Debug.Assert(false, "assert passed", "nothing is wrong {0} {1}", 'a', 'b'), "assert passed", "nothing is wrong a b");      
        }

        [Fact]
        public void Fail()
        {
            VerifyAssert(() => Debug.Fail("something bad happened"), "something bad happened");
            VerifyAssert(() => Debug.Fail("something bad happened", "really really bad"), "something bad happened", "really really bad");        
        }

        [Fact]
        public void Write()
        {
            VerifyLogged(() => Debug.Write(5), "5");
            VerifyLogged(() => Debug.Write((string)null), "");
            VerifyLogged(() => Debug.Write((object)null), "");
            VerifyLogged(() => Debug.Write(5, "category"), "category: 5");
            VerifyLogged(() => Debug.Write((object)null, "category"), "category: ");
            VerifyLogged(() => Debug.Write("logged"), "logged");
            VerifyLogged(() => Debug.Write("logged", "category"), "category: logged");
            VerifyLogged(() => Debug.Write("logged", (string)null), "logged");

            string longString = new string('d', 8192);
            VerifyLogged(() => Debug.Write(longString), longString);

            GoToNextLine();
        }

        [Fact]
        public void Print()
        {
            VerifyLogged(() => Debug.Print("logged"), "logged");
            VerifyLogged(() => Debug.Print("logged {0}", 5), "logged 5");

            GoToNextLine();
        }

        [Fact]
        public void WriteLine()
        {
            VerifyLogged(() => Debug.WriteLine(5), "5" + Environment.NewLine);
            VerifyLogged(() => Debug.WriteLine((string)null), Environment.NewLine);
            VerifyLogged(() => Debug.WriteLine((object)null), Environment.NewLine);
            VerifyLogged(() => Debug.WriteLine(5, "category"), "category: 5" + Environment.NewLine);
            VerifyLogged(() => Debug.WriteLine((object)null, "category"), "category: " + Environment.NewLine);
            VerifyLogged(() => Debug.WriteLine("logged"), "logged" + Environment.NewLine);
            VerifyLogged(() => Debug.WriteLine("logged", "category"), "category: logged" + Environment.NewLine);
            VerifyLogged(() => Debug.WriteLine("logged", (string)null), "logged" + Environment.NewLine);
            VerifyLogged(() => Debug.WriteLine("{0} {1}", 'a', 'b'), "a b" + Environment.NewLine);
        }

        [Fact]
        public void WriteIf()
        {
            VerifyLogged(() => Debug.WriteIf(true, 5), "5");
            VerifyLogged(() => Debug.WriteIf(false, 5), "");

            VerifyLogged(() => Debug.WriteIf(true, 5, "category"), "category: 5");
            VerifyLogged(() => Debug.WriteIf(false, 5, "category"), "");

            VerifyLogged(() => Debug.WriteIf(true, "logged"), "logged");
            VerifyLogged(() => Debug.WriteIf(false, "logged"), "");

            VerifyLogged(() => Debug.WriteIf(true, "logged", "category"), "category: logged");
            VerifyLogged(() => Debug.WriteIf(false, "logged", "category"), "");

            GoToNextLine();
        }

        [Fact]
        public void WriteLineIf()
        {
            VerifyLogged(() => Debug.WriteLineIf(true, 5), "5" + Environment.NewLine);
            VerifyLogged(() => Debug.WriteLineIf(false, 5), "");

            VerifyLogged(() => Debug.WriteLineIf(true, 5, "category"), "category: 5" + Environment.NewLine);
            VerifyLogged(() => Debug.WriteLineIf(false, 5, "category"), "");

            VerifyLogged(() => Debug.WriteLineIf(true, "logged"), "logged" + Environment.NewLine);
            VerifyLogged(() => Debug.WriteLineIf(false, "logged"), "");

            VerifyLogged(() => Debug.WriteLineIf(true, "logged", "category"), "category: logged" + Environment.NewLine);
            VerifyLogged(() => Debug.WriteLineIf(false, "logged", "category"), "");     
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(3, 3)]
        [InlineData(4, 4)]
        public void IndentSize_Set_GetReturnsExpected(int indentSize, int expectedIndentSize)
        {
            Debug.IndentLevel = 0;

            Debug.IndentSize = indentSize;
            Assert.Equal(expectedIndentSize, Debug.IndentSize);
            VerifyLogged(() => Debug.WriteLine("pizza"), "pizza" + Environment.NewLine);

            // Indent once.
            Debug.Indent();
            string expectedIndentOnce = new string(' ', expectedIndentSize);
            VerifyLogged(() => Debug.WriteLine("pizza"), expectedIndentOnce + "pizza" + Environment.NewLine);

            // Indent again.
            Debug.Indent();
            string expectedIndentTwice = new string(' ', expectedIndentSize * 2);
            VerifyLogged(() => Debug.WriteLine("pizza"), expectedIndentTwice + "pizza" + Environment.NewLine);

            // Unindent.
            Debug.Unindent();
            Debug.Unindent();
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        public void IndentLevel_Set_GetReturnsExpected(int indentLevel, int expectedIndentLevel)
        {
            Debug.IndentLevel = indentLevel;
            Assert.Equal(expectedIndentLevel, Debug.IndentLevel);
            string expectedIndentOnce = new string(' ', expectedIndentLevel * Debug.IndentSize);
            VerifyLogged(() => Debug.WriteLine("pizza"), expectedIndentOnce + "pizza" + Environment.NewLine);

            // Indent once.
            Debug.Indent();
            Assert.Equal(expectedIndentLevel + 1, Debug.IndentLevel);
            string expectedIndentTwice = new string(' ', (expectedIndentLevel + 1) * Debug.IndentSize);
            VerifyLogged(() => Debug.WriteLine("pizza"), expectedIndentTwice + "pizza" + Environment.NewLine);

            // Unindent.
            Debug.Unindent();
            Assert.Equal(expectedIndentLevel, Debug.IndentLevel);
            Debug.Unindent();
        }
    }
}
