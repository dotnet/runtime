// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace ILCompiler.DependencyAnalysisFramework
{
    public abstract class DependencyNode : IDependencyNode
    {
        private object _mark;

        // Only DependencyNodeCore<T> is allowed to derive from this
        internal DependencyNode()
        { }

        internal void SetMark(object mark)
        {
            Debug.Assert(mark != null);
            Debug.Assert(_mark == null);
            _mark = mark;
        }

        internal object GetMark()
        {
            return _mark;
        }

        public bool Marked
        {
            get
            {
                return _mark != null;
            }
        }

        public sealed override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        public sealed override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
