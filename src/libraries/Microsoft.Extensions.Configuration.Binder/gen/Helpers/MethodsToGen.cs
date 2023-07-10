// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    [Flags]
    public enum MethodsToGen_CoreBindingHelper
    {
        None = 0x0,
        BindCore = 0x1,
        BindCoreUntyped = 0x2,
        GetCore = 0x4,
        GetValueCore = 0x8,
        Initialize = 0x10,
    }

    /// <summary>
    /// Methods on Microsoft.Extensions.Configuration.ConfigurationBinder
    /// </summary>
    [Flags]
    internal enum MethodsToGen_ConfigurationBinder
    {
        None = 0x0,

        /// <summary>
        /// Bind(IConfiguration, object).
        /// </summary>
        Bind_instance = 0x1,

        /// <summary>
        /// Bind(IConfiguration, object, Action<BinderOptions>).
        /// </summary>
        Bind_instance_BinderOptions = 0x2,

        /// <summary>
        /// Bind(IConfiguration, string, object).
        /// </summary>
        Bind_key_instance = 0x4,

        /// <summary>
        /// Get<T>(IConfiguration).
        /// </summary>
        Get_T = 0x8,

        /// <summary>
        /// Get<T>(IConfiguration, Action<BinderOptions>).
        /// </summary>
        Get_T_BinderOptions = 0x10,

        /// <summary>
        /// Get<T>(IConfiguration, Type).
        /// </summary>
        Get_TypeOf = 0x20,

        /// <summary>
        /// Get<T>(IConfiguration, Type, Action<BinderOptions>).
        /// </summary>
        Get_TypeOf_BinderOptions = 0x40,

        /// <summary>
        /// GetValue(IConfiguration, Type, string).
        /// </summary>
        GetValue_TypeOf_key = 0x80,

        /// <summary>
        /// GetValue(IConfiguration, Type, object).
        /// </summary>
        GetValue_TypeOf_key_defaultValue = 0x100,

        /// <summary>
        /// GetValue<T>(IConfiguration, string).
        /// </summary>
        GetValue_T_key = 0x200,

        /// <summary>
        /// GetValue<T>(IConfiguration, string, T).
        /// </summary>
        GetValue_T_key_defaultValue = 0x400,

        // Method groups
        Bind = Bind_instance | Bind_instance_BinderOptions | Bind_key_instance,
        Get = Get_T | Get_T_BinderOptions | Get_TypeOf | Get_TypeOf_BinderOptions,
        GetValue = GetValue_T_key | GetValue_T_key_defaultValue | GetValue_TypeOf_key | GetValue_TypeOf_key_defaultValue,

        Any = Bind | Get | GetValue,
    }

    [Flags]
    internal enum MethodsToGen_Extensions_OptionsBuilder
    {
        None = 0x0,

        /// <summary>
        /// Bind<T>(OptionsBuilder<T>, IConfiguration).
        /// </summary>
        Bind_T = 0x1,

        /// <summary>
        /// Bind<T>(OptionsBuilder<T>, IConfiguration, Action<BinderOptions>?).
        /// </summary>
        Bind_T_BinderOptions = 0x2,

        /// <summary>
        /// BindConfiguration<T>(OptionsBuilder<T>, string, Action<BinderOptions>?).
        /// </summary>
        BindConfiguration_T_path_BinderOptions = 0x4,

        // Method group. BindConfiguration_T is its own method group.
        Bind = Bind_T | Bind_T_BinderOptions,

        Any = Bind | BindConfiguration_T_path_BinderOptions,
    }

    /// <summary>
    /// Methods on Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions
    /// </summary>
    [Flags]
    public enum MethodsToGen_Extensions_ServiceCollection
    {
        None = 0x0,

        /// <summary>
        /// Configure<T>(IServiceCollection, IConfiguration).
        /// </summary>
        Configure_T = 0x1,

        /// <summary>
        /// Configure<T>(IServiceCollection, string, IConfiguration).
        /// </summary>
        Configure_T_name = 0x2,

        /// <summary>
        /// Configure<T>(IServiceCollection, IConfiguration, Action<BinderOptions>?).
        /// </summary>
        Configure_T_BinderOptions = 0x4,

        /// <summary>
        /// Configure<T>(IServiceCollection, string, IConfiguration, Action<BinderOptions>?).
        /// </summary>
        Configure_T_name_BinderOptions = 0x8,

        Configure = Configure_T | Configure_T_name | Configure_T_BinderOptions | Configure_T_name_BinderOptions,

        Any = Configure,
    }
}
