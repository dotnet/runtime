// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices
{
    public class DirectoryVirtualListViewContext
    {
        internal readonly byte[] _context;

        public DirectoryVirtualListViewContext() : this(new byte[0])
        {
        }

        internal DirectoryVirtualListViewContext(byte[] context)
        {
            if (context == null)
            {
                _context = new byte[0];
            }
            else
            {
                _context = new byte[context.Length];
                for (int i = 0; i < context.Length; i++)
                {
                    _context[i] = context[i];
                }
            }
        }

    	public DirectoryVirtualListViewContext Copy()
        {
            return new DirectoryVirtualListViewContext(_context);
        }
    }
}
