// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.TextWriterTraceListenerTests
{
    public class ConsoleTraceListenerTests
    {
        [Fact]
        public static void DefaultCtorPropertiesCheck()
        {
            using (var listener = new ConsoleTraceListener())
                Assert.Equal(Console.Out, listener.Writer);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void BoolCtorPropertiesCheck(bool value)
        {
            using (TextWriter expected = value ? Console.Error : Console.Out)
            using (var listener = new ConsoleTraceListener(useErrorStream: value))
                Assert.Equal(expected, listener.Writer);
        }

        [Fact]
        public static void NopClose()
        {
            using (var listener = new ConsoleTraceListener())
            {
                listener.Close();
                Assert.NotNull(listener.Writer);
                Assert.Equal(Console.Out, listener.Writer);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void WriteExpectedOutput(bool value)
        {
            RemoteExecutor.Invoke((_value) =>
            {
                string message = "Write this message please";
                bool setErrorStream = bool.Parse(_value);
                using (var stringWriter = new StringWriter())
                {
                    if (setErrorStream)
                        Console.SetError(stringWriter);
                    else
                        Console.SetOut(stringWriter);

                    using (var listener = new ConsoleTraceListener(useErrorStream: setErrorStream))
                    {
                        listener.Write(message);
                        string writerOutput = stringWriter.ToString();
                        Assert.Equal(message, writerOutput);
                        Assert.DoesNotContain(Environment.NewLine, writerOutput);
                    }
                }

                return RemoteExecutor.SuccessExitCode;
            }, value.ToString()).Dispose();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void WriteLineExpectedOutput(bool value)
        {
            RemoteExecutor.Invoke((_value) =>
            {
                string message = "A new message to the listener";
                bool setErrorStream = bool.Parse(_value);
                using (var stringWriter = new StringWriter())
                {
                    if (setErrorStream)
                        Console.SetError(stringWriter);
                    else
                        Console.SetOut(stringWriter);

                    using (var listener = new ConsoleTraceListener(useErrorStream: setErrorStream))
                    {
                        listener.WriteLine(message);
                        Assert.Contains(message, stringWriter.ToString() + Environment.NewLine);
                    }
                }

                return RemoteExecutor.SuccessExitCode;
            }, value.ToString()).Dispose();
        }
    }
}
