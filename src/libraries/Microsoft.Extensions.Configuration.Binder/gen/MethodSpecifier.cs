// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    [Flags]
    internal enum MethodSpecifier
    {
        None = 0x0,

        // ConfigurationBinder binding methods.

        /// <summary>
        /// ConfiguationBinder.Bind(IConfiguration, object).
        /// </summary>
        Bind_object = 0x1,

        /// <summary>
        /// ConfiguationBinder.Bind(IConfiguration, object, Action<BinderOptions>).
        /// </summary>
        Bind_object_BinderOptions = 0x2,

        /// <summary>
        /// ConfiguationBinder.Bind(IConfiguration, string, object).
        /// </summary>
        Bind_key_object = 0x4,

        /// <summary>
        /// ConfiguationBinder.Get<T>(IConfiguration).
        /// </summary>
        Get_T = 0x8,

        /// <summary>
        /// ConfiguationBinder.Get<T>(IConfiguration, Action<BinderOptions>).
        /// </summary>
        Get_T_BinderOptions = 0x10,

        /// <summary>
        /// ConfiguationBinder.Get<T>(IConfiguration, Type).
        /// </summary>
        Get_TypeOf = 0x20,

        /// <summary>
        /// ConfiguationBinder.Get<T>(IConfiguration, Type, Action<BinderOptions>).
        /// </summary>
        Get_TypeOf_BinderOptions = 0x40,

        // Higher level binding methods from Microsoft.Extensions.DependencyInjection

        Configure = 0x80,

        // Helper methods
        BindCore = 0x100,
        HasValueOrChildren = 0x200,
        HasChildren = 0x400,
    }
}
