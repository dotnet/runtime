// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.DirectoryServices
{
    public class DirectoryVirtualListViewContext
    {
        internal readonly byte[] _context;

        public DirectoryVirtualListViewContext() : this(null)
        {
        }

        internal DirectoryVirtualListViewContext(byte[]? context)
        {
            if (context == null)
            {
                _context = Array.Empty<byte>();
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
