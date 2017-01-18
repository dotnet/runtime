// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

namespace System.Dynamic
{
    /// <summary>
    /// Represents a dynamic object, that can have its operations bound at runtime.
    /// </summary>
    /// <remarks>
    /// Objects that want to participate in the binding process should implement an <see cref="IDynamicMetaObjectProvider"/> interface,
    /// and implement <see cref="GetMetaObject" /> to return a <see cref="DynamicMetaObject" />.
    /// </remarks>
    public interface IDynamicMetaObjectProvider
    {
        /// <summary>
        /// Returns the <see cref="DynamicMetaObject" /> responsible for binding operations performed on this object.
        /// </summary>
        /// <param name="parameter">The expression tree representation of the runtime value.</param>
        /// <returns>The <see cref="DynamicMetaObject" /> to bind this object.</returns>
        DynamicMetaObject GetMetaObject(Expression parameter);
    }
}
