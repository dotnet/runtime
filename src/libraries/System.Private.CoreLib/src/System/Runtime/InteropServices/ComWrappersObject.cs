// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using System.Threading;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Provides an optional base type for objects returned from <see cref="ComWrappers.CreateObject(IntPtr, CreateObjectFlags)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deriving from this type is not required, but it allows <see cref="ComWrappers"/> to store the state it associates
    /// with an object in the object itself. Otherwise, that state is kept in a table keyed on the object, which requires
    /// a hash code for every object that is wrapped. Assigning one is a significant part of the cost of creating a wrapper.
    /// </para>
    /// <para>
    /// This type has no members other than its constructor, and it imposes no requirements on derived types beyond the
    /// ones that already apply to any object returned from <see cref="ComWrappers.CreateObject(IntPtr, CreateObjectFlags)"/>,
    /// with one exception: the state lives in a field, so <see cref="object.MemberwiseClone"/> copies it. A shallow copy of
    /// a wrapper is reported as being one itself, and refers to the same native object the original does. Deriving types
    /// that support cloning should produce their copies some other way.
    /// </para>
    /// </remarks>
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public abstract class ComWrappersObject
    {
        /// <summary>
        /// The <see cref="ComWrappers.NativeObjectWrapper"/> tracking the native object this instance wraps, if it has
        /// been registered with a <see cref="ComWrappers"/> instance.
        /// </summary>
        /// <remarks>
        /// This takes the place of the entry that would otherwise be in <c>ComWrappers.s_nativeObjectWrapperTable</c>,
        /// and it has the same lifetime: the wrapper is kept alive for exactly as long as the object it tracks is.
        /// It is only ever assigned through <see cref="Interlocked.CompareExchange{T}(ref T, T, T)"/>, so that the
        /// first registration wins, as it would in the table.
        /// </remarks>
        internal ComWrappers.NativeObjectWrapper? _nativeObjectWrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComWrappersObject"/> class.
        /// </summary>
        protected ComWrappersObject()
        {
        }
    }
}
