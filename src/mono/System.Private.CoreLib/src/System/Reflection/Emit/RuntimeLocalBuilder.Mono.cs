// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

//
// System.Reflection.Emit/RuntimeLocalBuilder.cs
//
// Authors:
//   Paolo Molaro (lupus@ximian.com)
//   Martin Baulig (martin@gnome.org)
//   Miguel de Icaza (miguel@ximian.com)
//
// (C) 2001, 2002 Ximian, Inc.  http://www.ximian.com
//

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    [StructLayout(LayoutKind.Sequential)]
    internal sealed partial class RuntimeLocalBuilder : LocalBuilder
    {
#region Sync with MonoReflectionLocalBuilder in object-internals.h
        internal Type type;
        internal bool is_pinned;
        internal ushort position;
        private string? name;
#endregion

        internal ILGenerator ilgen;
        private int startOffset;
        private int endOffset;

        [DynamicDependency(nameof(name))]  // Automatically keeps all previous fields too due to StructLayout
        internal RuntimeLocalBuilder(Type t, ILGenerator ilgen)
        {
            this.type = t;
            this.ilgen = ilgen;
        }

        public override Type LocalType
        {
            get
            {
                return type;
            }
        }

        public override bool IsPinned
        {
            get
            {
                return is_pinned;
            }
        }

        public override int LocalIndex
        {
            get
            {
                return position;
            }
        }

        internal string? Name
        {
            get { return name; }
        }

        internal int StartOffset
        {
            get { return startOffset; }
        }

        internal int EndOffset
        {
            get { return endOffset; }
        }
    }
}
