// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices
{
    /// <include file='doc\DirectoryVirtualListViewContext.uex' path='docs/doc[@for="DirectoryVirtualListViewContext"]/*' />
    /// <devdoc>
    ///    <para>Specifies how to construct directory virtual list view response.</para>
    /// </devdoc>
    public class DirectoryVirtualListViewContext
    {
        internal byte[] context;

        /// <include file='doc\DirectoryVirtualListViewContext.uex' path='docs/doc[@for="DirectoryVirtualListViewContext.DirectoryVirtualListViewContext"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public DirectoryVirtualListViewContext() : this(new byte[0])
        {
        }

        internal DirectoryVirtualListViewContext(byte[] context)
        {
            if (context == null)
            {
                this.context = new byte[0];
            }
            else
            {
                this.context = new byte[context.Length];
                for (int i = 0; i < context.Length; i++)
                {
                    this.context[i] = context[i];
                }
            }
        }

        /// <include file='doc\DirectoryVirtualListViewContext .uex' path='docs/doc[@for="DirectoryVirtualListViewContext .Copy"]/*' />
        /// <devdoc>
        ///    <para>Returns a copy of the current DirectoryVirtualListViewContext instance.</para>
        /// </devdoc>
    	public DirectoryVirtualListViewContext Copy()
        {
            DirectoryVirtualListViewContext context = new DirectoryVirtualListViewContext(this.context);
            return context;
        }
    }
}
