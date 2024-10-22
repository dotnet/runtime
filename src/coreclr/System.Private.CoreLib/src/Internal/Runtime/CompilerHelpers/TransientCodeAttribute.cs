// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// See TransientCodeKind in jitinterface.cpp.
    /// </summary>
    internal enum TransientCodeKind
    {
        None = 0,

        /// <summary>
        /// C++/CLI copy constructor helper
        /// </summary>
        CopyConstructor,
    }

    /// <summary>
    /// Type used to indicate the kind of transient code generation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class TransientCodeAttribute : Attribute
    {
        internal TransientCodeAttribute(TransientCodeKind codeKind)
        {
            CodeKind = codeKind;
        }

        public TransientCodeKind CodeKind { get; }
    }
}
