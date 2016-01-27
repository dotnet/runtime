// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Dynamic
{
    /// <summary>
    /// Represents information about a dynamic get member operation, indicating
    /// if the get member should invoke properties when performing the get.
    /// </summary>
    public interface IInvokeOnGetBinder
    {
        /// <summary>
        /// Gets the value indicating if this GetMember should invoke properties
        /// when performing the get. The default value when this interface is not present
        /// is true.
        /// </summary>
        /// <remarks>
        /// This property is used by some languages to get a better COM interop experience.
        /// When the value is set to false, the dynamic COM object won't invoke the object
        /// but will instead bind to the name, and return an object that can be invoked or
        /// indexed later. This is useful for indexed properties and languages that don't
        /// produce InvokeMember call sites.
        /// </remarks>
        bool InvokeOnGet { get; }
    }
}
