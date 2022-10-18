// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using LibObjectFile.Elf;

namespace LibObjectFile.Ar
{
    public abstract class ArObject : ObjectFileNode
    {
        protected override void ValidateParent(ObjectFileNode parent)
        {
            if (!(parent is ArArchiveFile))
            {
                throw new ArgumentException($"Parent must inherit from type {nameof(ArArchiveFile)}");
            }
        }


        /// <summary>
        /// Gets the containing <see cref="ElfObjectFile"/>. Might be null if this section or segment
        /// does not belong to an existing <see cref="ElfObjectFile"/>.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public new ArArchiveFile Parent
        {
            get => (ArArchiveFile)base.Parent;
            internal set => base.Parent = value;
        }

        public abstract void UpdateLayout(DiagnosticBag diagnostics);
    }
}