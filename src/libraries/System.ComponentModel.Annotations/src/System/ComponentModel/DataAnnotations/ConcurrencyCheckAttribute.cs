// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     This attribute is used to mark the members of a Type that participate in
    ///     optimistic concurrency checks.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class ConcurrencyCheckAttribute : Attribute
    {
    }
}
