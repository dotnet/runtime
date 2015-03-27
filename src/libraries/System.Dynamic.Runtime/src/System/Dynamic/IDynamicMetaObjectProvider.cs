// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace System.Dynamic
{
    /// <summary>
    /// Represents a dynamic object, that can have its operations bound at runtime.
    /// </summary>
    /// <remarks>
    /// Objects that want to participate in the binding process should implement an IDynamicMetaObjectProvider interface,
    /// and implement <see cref="IDynamicMetaObjectProvider.GetMetaObject" /> to return a <see cref="DynamicMetaObject" />.
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
