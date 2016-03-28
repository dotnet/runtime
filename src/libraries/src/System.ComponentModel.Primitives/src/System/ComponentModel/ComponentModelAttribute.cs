// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    public abstract class ComponentModelAttribute : Attribute
    {
        public virtual bool IsDefaultAttribute() { return false; }

        public virtual object TypeId { get { return GetType(); } }

        public virtual bool Match(object obj) { return Equals(obj); }
    }
}
