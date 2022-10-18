// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;

namespace LibObjectFile.Dwarf
{

    public abstract class DwarfObject : ObjectFileNode
    {
        public DwarfFile GetParentFile()
        {
            var check = (ObjectFileNode)this;
            while (check != null)
            {
                if (check is DwarfFile dwarfFile) return dwarfFile;
                check = check.Parent;
            }
            return null;
        }

        public DwarfUnit GetParentUnit()
        {
            var check = (ObjectFileNode)this;
            while (check != null)
            {
                if (check is DwarfUnit dwarfUnit) return dwarfUnit;
                check = check.Parent;
            }
            return null;
        }

        public DwarfSection GetParentSection()
        {
            var check = (ObjectFileNode)this;
            while (check != null)
            {
                if (check is DwarfSection dwarfSection) return dwarfSection;
                check = check.Parent;
            }
            return null;
        }
    }

    public abstract class DwarfObject<TContainer> : DwarfObject where TContainer : ObjectFileNode
    {
        protected override void ValidateParent(ObjectFileNode parent)
        {
            if (!(parent is TContainer))
            {
                throw new ArgumentException($"Parent must inherit from type {nameof(TContainer)}");
            }
        }


        /// <summary>
        /// Gets the containing <see cref="ElfObjectFile"/>. Might be null if this section or segment
        /// does not belong to an existing <see cref="ElfObjectFile"/>.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public new TContainer Parent
        {
            get => (TContainer)base.Parent;
            internal set => base.Parent = value;
        }

        internal void UpdateLayoutInternal(DwarfLayoutContext layoutContext)
        {
            UpdateLayout(layoutContext);
        }

        protected abstract void UpdateLayout(DwarfLayoutContext layoutContext);


        internal void ReadInternal(DwarfReader reader)
        {
            Read(reader);
        }

        protected abstract void Read(DwarfReader reader);
        

        internal void WriteInternal(DwarfWriter writer)
        {
            Write(writer);
        }

        protected abstract void Write(DwarfWriter writer);
    }
}