// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Internal.Reflection.Core.Execution
{
    //
    // This class abstracts the underlying Redhawk (or whatever execution engine) runtime that sets and gets fields.
    //
    [CLSCompliant(false)]
    public abstract class FieldAccessor
    {
        public abstract object GetField(object obj);
        public abstract object GetFieldDirect(TypedReference typedReference);

        public abstract void SetField(object obj, object value, BinderBundle binderBundle);
        public abstract void SetFieldDirect(TypedReference typedReference, object value);

        /// <summary>
        /// Returns the field offset (asserts and throws if not an instance field). Does not include the size of the object header.
        /// </summary>
        public abstract int Offset { get; }
    }
}
