// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>Represents the method that will handle the <see cref="System.Diagnostics.Process.OutputDataReceived" /> event or <see cref="System.Diagnostics.Process.ErrorDataReceived" /> event of a <see cref="System.Diagnostics.Process" />.</summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="System.Diagnostics.DataReceivedEventArgs" /> that contains the event data.</param>
    /// <format type="text/markdown"><![CDATA[
    /// [!INCLUDE[remarks] (~/includes/remarks/System.Diagnostics/DataReceivedEventArgs/DataReceivedEventHandler.md)]
    /// ]]></format>
    /// <altmember cref="System.Diagnostics.DataReceivedEventArgs"/>
    /// <altmember cref="System.Diagnostics.Process.OutputDataReceived"/>
    /// <altmember cref="System.Diagnostics.Process.ErrorDataReceived"/>
    /// <altmember cref="System.Diagnostics.Process"/>
    public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);

    /// <summary>Provides data for the <see cref="System.Diagnostics.Process.OutputDataReceived" /> and <see cref="System.Diagnostics.Process.ErrorDataReceived" /> events.</summary>
    /// <remarks>To asynchronously collect the redirected <see cref="System.Diagnostics.Process.StandardOutput" /> or <see cref="System.Diagnostics.Process.StandardError" /> stream output of a process, you must create a method that handles the redirected stream output events. The event-handler method is called when the process writes to the redirected stream. The event delegate calls your event handler with an instance of <see cref="System.Diagnostics.DataReceivedEventArgs" />. The <see cref="System.Diagnostics.DataReceivedEventArgs.Data" /> property contains the text line that the process wrote to the redirected stream.</remarks>
    /// <example>The following code example illustrates how to perform asynchronous read operations on the redirected <see cref="System.Diagnostics.Process.StandardOutput" /> stream of the `sort` command. The `sort` command is a console application that reads and sorts text input.
    /// The example creates an event delegate for the `SortOutputHandler` event handler and associates it with the <see cref="System.Diagnostics.Process.OutputDataReceived" /> event. The event handler receives text lines from the redirected <see cref="System.Diagnostics.Process.StandardOutput" /> stream, formats the text, and writes the text to the screen.
    /// <format type="text/markdown"><![CDATA[
    /// [!code-cpp[Process_AsyncStreams#1](~/samples/snippets/cpp/VS_Snippets_CLR/process_asyncstreams/CPP/sort_async.cpp#1)]
    /// [!code-csharp[Process_AsyncStreams#1](~/samples/snippets/csharp/VS_Snippets_CLR/process_asyncstreams/CS/sort_async.cs#1)]
    /// [!code-vb[Process_AsyncStreams#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/process_asyncstreams/VB/sort_async.vb#1)]
    /// ]]></format></example>
    /// <altmember cref="System.Diagnostics.DataReceivedEventHandler"/>
    /// <altmember cref="System.Diagnostics.Process.OutputDataReceived"/>
    /// <altmember cref="System.Diagnostics.Process.ErrorDataReceived"/>
    /// <altmember cref="System.Diagnostics.Process"/>
    public class DataReceivedEventArgs : EventArgs
    {
        private readonly string? _data;

        internal DataReceivedEventArgs(string? data)
        {
            _data = data;
        }

        /// <summary>Gets the line of characters that was written to a redirected <see cref="System.Diagnostics.Process" /> output stream.</summary>
        /// <value>The line that was written by an associated <see cref="System.Diagnostics.Process" /> to its redirected <see cref="System.Diagnostics.Process.StandardOutput" /> or <see cref="System.Diagnostics.Process.StandardError" /> stream.</value>
        /// <format type="text/markdown"><![CDATA[
        /// [!INCLUDE[remarks] (~/includes/remarks/System.Diagnostics/DataReceivedEventArgs/Data.md)]
        /// ]]></format>
        public string? Data
        {
            get { return _data; }
        }
    }
}
