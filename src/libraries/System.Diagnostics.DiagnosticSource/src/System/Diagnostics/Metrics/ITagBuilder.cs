// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// Builder which appends tags
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TReturn">The type of the builder.</typeparam>
    public interface ITagBuilder<T, out TReturn> where T : struct where TReturn : ITagBuilder<T, TReturn>
    {
        /// <summary>
        /// Append a tag.
        /// </summary>
        /// <typeparam name="TTag"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        TReturn WithTag<TTag>(string key, TTag value);
    }
}
