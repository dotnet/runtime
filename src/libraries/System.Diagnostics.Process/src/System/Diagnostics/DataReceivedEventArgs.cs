// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>Represents the method that will handle the <see cref="System.Diagnostics.Process.OutputDataReceived" /> event or <see cref="System.Diagnostics.Process.ErrorDataReceived" /> event of a <see cref="System.Diagnostics.Process" />.</summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="System.Diagnostics.DataReceivedEventArgs" /> that contains the event data.</param>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// When you create a <xref:System.Diagnostics.DataReceivedEventHandler> delegate, you identify the method that will handle the event. To associate the event with your event handler, add an instance of the delegate to the event. The event handler is called whenever the event occurs, unless you remove the delegate. For more information about event-handler delegates, see [Handling and Raising Events](/dotnet/standard/events/).
    /// To asynchronously collect the redirected <xref:System.Diagnostics.Process.StandardOutput%2A> or <xref:System.Diagnostics.Process.StandardError%2A> stream output of a process, add your event handler to the <xref:System.Diagnostics.Process.OutputDataReceived> or <xref:System.Diagnostics.Process.ErrorDataReceived> event. These events are raised each time the process writes a line to the corresponding redirected stream. When the redirected stream is closed, a null line is sent to the event handler. Ensure that your event handler checks for this condition before accessing the <xref:System.Diagnostics.DataReceivedEventArgs.Data%2A> property. For example, you can use the `static` method <xref:string.IsNullOrEmpty%2A?displayProperty=nameWithType> to validate the <xref:System.Diagnostics.DataReceivedEventArgs.Data%2A> property in your event handler.
    /// ## Examples
    /// The following code example illustrates how to perform asynchronous read operations on the redirected <xref:System.Diagnostics.Process.StandardOutput%2A> stream of the **sort** command. The **sort** command is a console application that reads and sorts text input.
    /// The example creates a <xref:System.Diagnostics.DataReceivedEventHandler> delegate for the `SortOutputHandler` event handler and associates the delegate with the <xref:System.Diagnostics.Process.OutputDataReceived> event. The event handler receives text lines from the redirected <xref:System.Diagnostics.Process.StandardOutput%2A> stream, formats the text, and writes the text to the screen.
    /// [!code-cpp[Process_AsyncStreams#1](~/samples/snippets/cpp/VS_Snippets_CLR/process_asyncstreams/CPP/sort_async.cpp#1)]
    /// [!code-csharp[Process_AsyncStreams#1](~/samples/snippets/csharp/VS_Snippets_CLR/process_asyncstreams/CS/sort_async.cs#1)]
    /// [!code-vb[Process_AsyncStreams#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/process_asyncstreams/VB/sort_async.vb#1)]
    /// ]]></format></remarks>
    /// <altmember cref="System.Diagnostics.DataReceivedEventArgs"/>
    /// <altmember cref="System.Diagnostics.Process.OutputDataReceived"/>
    /// <altmember cref="System.Diagnostics.Process.ErrorDataReceived"/>
    /// <altmember cref="System.Diagnostics.Process"/>
    public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);

    /// <summary>Provides data for the <see cref="System.Diagnostics.Process.OutputDataReceived" /> and <see cref="System.Diagnostics.Process.ErrorDataReceived" /> events.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// To asynchronously collect the redirected <xref:System.Diagnostics.Process.StandardOutput%2A> or <xref:System.Diagnostics.Process.StandardError%2A> stream output of a process, you must create a method that handles the redirected stream output events. The event-handler method is called when the process writes to the redirected stream. The event delegate calls your event handler with an instance of <xref:System.Diagnostics.DataReceivedEventArgs>. The <xref:System.Diagnostics.DataReceivedEventArgs.Data%2A> property contains the text line that the process wrote to the redirected stream.
    /// ## Examples
    /// The following code example illustrates how to perform asynchronous read operations on the redirected <xref:System.Diagnostics.Process.StandardOutput%2A> stream of the `sort` command. The `sort` command is a console application that reads and sorts text input.
    /// The example creates an event delegate for the `SortOutputHandler` event handler and associates it with the <xref:System.Diagnostics.Process.OutputDataReceived> event. The event handler receives text lines from the redirected <xref:System.Diagnostics.Process.StandardOutput%2A> stream, formats the text, and writes the text to the screen.
    /// [!code-cpp[Process_AsyncStreams#1](~/samples/snippets/cpp/VS_Snippets_CLR/process_asyncstreams/CPP/sort_async.cpp#1)]
    /// [!code-csharp[Process_AsyncStreams#1](~/samples/snippets/csharp/VS_Snippets_CLR/process_asyncstreams/CS/sort_async.cs#1)]
    /// [!code-vb[Process_AsyncStreams#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/process_asyncstreams/VB/sort_async.vb#1)]
    /// ]]></format></remarks>
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
        /// <remarks><format type="text/markdown"><![CDATA[
        /// When you redirect the <xref:System.Diagnostics.Process.StandardOutput%2A> or <xref:System.Diagnostics.Process.StandardError%2A> stream of a <xref:System.Diagnostics.Process> to your event handler, an event is raised each time the process writes a line to the redirected stream. The <xref:System.Diagnostics.DataReceivedEventArgs.Data%2A> property is the line that the <xref:System.Diagnostics.Process> wrote to the redirected output stream. Your event handler can use the <xref:System.Diagnostics.DataReceivedEventArgs.Data%2A> property to filter process output or write output to an alternate location. For example, you might create an event handler that stores all error output lines into a designated error log file.
        /// A line is defined as a sequence of characters followed by a line feed ("\n") or a carriage return immediately followed by a line feed ("\r\n"). The line characters are encoded using the default system ANSI code page. The <xref:System.Diagnostics.DataReceivedEventArgs.Data%2A> property does not include the terminating carriage return or line feed.
        /// When the redirected stream is closed, a null line is sent to the event handler. Ensure your event handler checks the <xref:System.Diagnostics.DataReceivedEventArgs.Data%2A> property appropriately before accessing it. For example, you can use the static method <xref:string.IsNullOrEmpty%2A?displayProperty=nameWithType> to validate the <xref:System.Diagnostics.DataReceivedEventArgs.Data%2A> property in your event handler.
        /// ## Examples
        /// The following code example illustrates a simple event handler associated with the <xref:System.Diagnostics.Process.OutputDataReceived> event. The event handler receives text lines from the redirected <xref:System.Diagnostics.Process.StandardOutput%2A> stream, formats the text, and writes the text to the screen.
        /// [!code-cpp[Process_AsyncStreams#4](~/samples/snippets/cpp/VS_Snippets_CLR/process_asyncstreams/CPP/datareceivedevent.cpp#4)]
        /// [!code-csharp[Process_AsyncStreams#4](~/samples/snippets/csharp/VS_Snippets_CLR/process_asyncstreams/CS/datareceivedevent.cs#4)]
        /// [!code-vb[Process_AsyncStreams#4](~/samples/snippets/visualbasic/VS_Snippets_CLR/process_asyncstreams/VB/datareceivedevent.vb#4)]
        /// ]]></format></remarks>
        public string? Data
        {
            get { return _data; }
        }
    }
}
