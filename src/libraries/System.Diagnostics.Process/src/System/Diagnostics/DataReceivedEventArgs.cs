// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>Represents the method that will handle the <see cref="System.Diagnostics.Process.OutputDataReceived" /> event or <see cref="System.Diagnostics.Process.ErrorDataReceived" /> event of a <see cref="System.Diagnostics.Process" />.</summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="System.Diagnostics.DataReceivedEventArgs" /> that contains the event data.</param>
    /// <format type="text/markdown"><![CDATA[
    /// [!INCLUDE[remarks] (~/includes/remarks/System.Diagnostics/DataReceivedEventHandler/DataReceivedEventHandler.md)]
    /// ]]></format>
    /// <altmember cref="System.Diagnostics.DataReceivedEventArgs"/>
    /// <altmember cref="System.Diagnostics.Process.OutputDataReceived"/>
    /// <altmember cref="System.Diagnostics.Process.ErrorDataReceived"/>
    /// <altmember cref="System.Diagnostics.Process"/>
    public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);

    /// <summary>Provides data for the <see cref="System.Diagnostics.Process.OutputDataReceived" /> and <see cref="System.Diagnostics.Process.ErrorDataReceived" /> events.</summary>
    /// <format type="text/markdown"><![CDATA[
    /// [!INCLUDE[remarks] (~/includes/remarks/System.Diagnostics/DataReceivedEventArgs/DataReceivedEventArgs.md)]
    /// ]]></format>
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
