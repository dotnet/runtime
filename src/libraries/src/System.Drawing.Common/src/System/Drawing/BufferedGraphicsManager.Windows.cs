// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.ConstrainedExecution;

namespace System.Drawing
{
    /// <summary>
    /// The BufferedGraphicsManager is used for accessing a BufferedGraphicsContext.
    /// </summary>
    public static class BufferedGraphicsManager
    {
        /// <summary>
        /// Static constructor.  Here, we hook the exit & unload events so we can clean up our context buffer.
        /// </summary>
        static BufferedGraphicsManager()
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnShutdown);
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(OnShutdown);
            Current = new BufferedGraphicsContext();
        }

        /// <summary>
        /// Retrieves the context associated with the app domain.
        /// </summary>
        public static BufferedGraphicsContext Current { get; }

        /// <summary>
        /// Called on process exit
        /// </summary>
        [PrePrepareMethod]
        private static void OnShutdown(object sender, EventArgs e) => Current.Invalidate();
    }
}
