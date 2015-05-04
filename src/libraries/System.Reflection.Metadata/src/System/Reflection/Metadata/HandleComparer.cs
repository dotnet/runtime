// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Reflection.Metadata
{
    public sealed class HandleComparer : IEqualityComparer<Handle>, IComparer<Handle>, IEqualityComparer<EntityHandle>, IComparer<EntityHandle>
    {
        private static readonly HandleComparer _default = new HandleComparer();

        private HandleComparer()
        {
        }

        public static HandleComparer Default
        {
            get { return _default; }
        }

        public bool Equals(Handle x, Handle y)
        {
            return x.Equals(y);
        }

        public bool Equals(EntityHandle x, EntityHandle y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(Handle obj)
        {
            return obj.GetHashCode();
        }

        public int GetHashCode(EntityHandle obj)
        {
            return obj.GetHashCode();
        }

        /// <summary>
        /// Compares two handles.
        /// </summary>
        /// <remarks>
        /// The order of handles that differ in kind is undefined.
        /// Returns 0 if and only if <see cref="Equals(Handle, Handle)"/> returns true.
        /// </remarks>
        public int Compare(Handle x, Handle y)
        {
            return Handle.Compare(x, y);
        }

        /// <summary>
        /// Compares two entity handles.
        /// </summary>
        /// <remarks>
        /// The order of handles that differ in kind is undefined.
        /// Returns 0 if and only if <see cref="Equals(EntityHandle, EntityHandle)"/> returns true.
        /// </remarks>
        public int Compare(EntityHandle x, EntityHandle y)
        {
            return EntityHandle.Compare(x, y);
        }
    }
}