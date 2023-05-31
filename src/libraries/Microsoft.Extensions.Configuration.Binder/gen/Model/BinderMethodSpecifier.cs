// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    [Flags]
    internal enum BinderMethodSpecifier
    {
        None = 0x0,

        // ConfigurationBinder binding methods.

        /// <summary>
        /// ConfiguationBinder.Bind(IConfiguration, object).
        /// </summary>
        Bind_instance = 0x1,

        /// <summary>
        /// ConfiguationBinder.Bind(IConfiguration, object, Action<BinderOptions>).
        /// </summary>
        Bind_instance_BinderOptions = 0x2,

        /// <summary>
        /// ConfiguationBinder.Bind(IConfiguration, string, object).
        /// </summary>
        Bind_key_instance = 0x4,

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

        /// <summary>
        /// ConfiguationBinder.GetValue<T>(IConfiguration).
        /// </summary>
        GetValue_TypeOf_key = 0x80,

        /// <summary>
        /// ConfiguationBinder.GetValue<T>(IConfiguration, Action<BinderOptions>).
        /// </summary>
        GetValue_TypeOf_key_defaultValue = 0x100,

        /// <summary>
        /// ConfiguationBinder.GetValue<T>(IConfiguration, Type).
        /// </summary>
        GetValue_T_key = 0x200,

        /// <summary>
        /// ConfiguationBinder.GetValue<T>(IConfiguration, Type, Action<BinderOptions>).
        /// </summary>
        GetValue_T_key_defaultValue = 0x400,

        // Higher level binding methods from Microsoft.Extensions.DependencyInjection

        Configure = 0x800,

        // Binding helpers
        BindCore = 0x1000,
        HasChildren = 0x4000,
        Initialize = 0x8000,

        // Method groups
        Bind = Bind_instance | Bind_instance_BinderOptions | Bind_key_instance,
        Get = Get_T | Get_T_BinderOptions | Get_TypeOf | Get_TypeOf_BinderOptions,
        GetValue = GetValue_T_key | GetValue_T_key_defaultValue | GetValue_TypeOf_key | GetValue_TypeOf_key_defaultValue,
        RootMethodsWithConfigOptions = Bind_instance_BinderOptions | Get_T_BinderOptions | Get_TypeOf_BinderOptions,
    }
}
