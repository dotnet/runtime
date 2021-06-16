// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogMessagesTests
    {
        [Fact]
        public void EventLogMessagesContainsNoTypes()
        {
            Assembly messageAssembly = Assembly.Load("System.Diagnostics.EventLog.Messages");
            Assert.NotNull(messageAssembly);
            Assert.Empty(messageAssembly.GetTypes());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(65535)]
        public unsafe void CanFormatMessage(uint messageId)
        {
            string messageDllPath = Path.Combine(Path.GetDirectoryName(typeof(EventLog).Assembly.Location), "System.Diagnostics.EventLog.Messages.dll");
            Assert.True(File.Exists(messageDllPath));
            using SafeLibraryHandle hMessageDll = Interop.Kernel32.LoadLibraryExW(messageDllPath, IntPtr.Zero, Interop.Kernel32.LOAD_LIBRARY_AS_DATAFILE);

            string messageString = "hello message";
            char[] buffer = new char[1024];
            fixed (char* pMessageString = messageString)
            {
                IntPtr[] insertion = new[] { (IntPtr)pMessageString };
                int messageLength = Interop.Kernel32.FormatMessage(
                    Interop.Kernel32.FORMAT_MESSAGE_FROM_HMODULE | Interop.Kernel32.FORMAT_MESSAGE_ARGUMENT_ARRAY,
                    hMessageDll,
                    messageId,
                    0,
                    buffer,
                    buffer.Length,
                    insertion);

                Assert.True(messageLength > 0);
                string formattedMessage = new string(buffer, 0, messageLength);
                Assert.Equal(messageString, formattedMessage);
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        public void CanReadAndWriteMessages()
        {
            string messageDllPath = Path.Combine(Path.GetDirectoryName(typeof(EventLog).Assembly.Location), "System.Diagnostics.EventLog.Messages.dll");
            EventSourceCreationData log = new EventSourceCreationData($"TestEventMessageSource {Guid.NewGuid()}", "Application")
            {
                MessageResourceFile = messageDllPath
            };
            try
            {
                if (EventLog.SourceExists(log.Source))
                {
                    EventLog.DeleteEventSource(log.Source);
                }

                EventLog.CreateEventSource(log);
                string message = $"Hello {Guid.NewGuid()}";
                Helpers.Retry(() => EventLog.WriteEntry(log.Source, message));

                using (EventLogReader reader = new EventLogReader(new EventLogQuery("Application", PathType.LogName, $"*[System/Provider/@Name=\"{log.Source}\"]")))
                {
                    EventRecord evt = reader.ReadEvent();

                    string logMessage = evt.FormatDescription();

                    Assert.Equal(message, logMessage);
                }
            }
            finally
            {
                EventLog.DeleteEventSource(log.Source);
            }
        }
    }
}
