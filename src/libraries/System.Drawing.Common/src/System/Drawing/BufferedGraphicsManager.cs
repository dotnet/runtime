// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    using System.Runtime.ConstrainedExecution;

    /// <include file='doc\BufferedGraphicsManager.uex' path='docs/doc[@for="BufferedGraphicsManager"]/*' />
    /// <devdoc>
    ///         The BufferedGraphicsManager is used for accessing a BufferedGraphicsContext.
    /// </devdoc>
    public sealed class BufferedGraphicsManager
    {
        private static BufferedGraphicsContext s_bufferedGraphicsContext;

        /// <include file='doc\BufferedGraphicsManager.uex' path='docs/doc[@for="BufferedGraphicsManager.BufferedGraphicsManager"]/*' />
        /// <devdoc>
        ///         Private constructor.
        /// </devdoc>
        private BufferedGraphicsManager()
        {
        }

        /// <include file='doc\BufferedGraphicsManager.uex' path='docs/doc[@for="BufferedGraphicsManager.BufferedGraphicsManager"]/*' />
        /// <devdoc>
        ///         Static constructor.  Here, we hook the exit & unload events so we can clean up our context buffer.
        /// </devdoc>
        static BufferedGraphicsManager()
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(BufferedGraphicsManager.OnShutdown);
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(BufferedGraphicsManager.OnShutdown);
            s_bufferedGraphicsContext = new BufferedGraphicsContext();
        }

        /// <include file='doc\BufferedGraphicsManager.uex' path='docs/doc[@for="BufferedGraphicsManager.Current"]/*' />
        /// <devdoc>
        ///         Retrieves the context associated with the app domain.
        /// </devdoc>
        public static BufferedGraphicsContext Current
        {
            get
            {
                return s_bufferedGraphicsContext;
            }
        }

        /// <include file='doc\BufferedGraphicsManager.uex' path='docs/doc[@for="BufferedGraphicsManager.OnProcessExit"]/*' />
        /// <devdoc>
        ///         Called on process exit
        /// </devdoc>
        [PrePrepareMethod]
        private static void OnShutdown(object sender, EventArgs e)
        {
            BufferedGraphicsManager.Current.Invalidate();
        }
    }
}
