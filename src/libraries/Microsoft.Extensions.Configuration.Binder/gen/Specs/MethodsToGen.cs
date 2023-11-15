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
        GetCore = 0x2,
        GetValueCore = 0x4,
        BindCoreMain = 0x8,
        Initialize = 0x10,
        HasValueOrChildren = 0x20,
        AsConfigWithChildren = 0x40,
        ParsePrimitive = 0x80,
    }

    /// <summary>
    /// Methods on Microsoft.Extensions.Configuration.ConfigurationBinder
    /// </summary>
    [Flags]
    public enum MethodsToGen
    {
        None = 0x0,
        Any = ConfigBinder_Any | OptionsBuilderExt_Any | ServiceCollectionExt_Any,

        #region IConfiguration ext. method overloads: 0x1 - 0x400
        /// <summary>
        /// Bind(IConfiguration, object?).
        /// </summary>
        ConfigBinder_Bind_instance = 0x1,

        /// <summary>
        /// Bind(IConfiguration, object?, Action<BinderOptions>?).
        /// </summary>
        ConfigBinder_Bind_instance_BinderOptions = 0x2,

        /// <summary>
        /// Bind(IConfiguration, string, object?).
        /// </summary>
        ConfigBinder_Bind_key_instance = 0x4,

        /// <summary>
        /// Get<T>(IConfiguration).
        /// </summary>
        ConfigBinder_Get_T = 0x8,

        /// <summary>
        /// Get<T>(IConfiguration, Action<BinderOptions>?).
        /// </summary>
        ConfigBinder_Get_T_BinderOptions = 0x10,

        /// <summary>
        /// Get(IConfiguration, Type).
        /// </summary>
        ConfigBinder_Get_TypeOf = 0x20,

        /// <summary>
        /// Get(IConfiguration, Type, Action<BinderOptions>?).
        /// </summary>
        ConfigBinder_Get_TypeOf_BinderOptions = 0x40,

        /// <summary>
        /// GetValue(IConfiguration, Type, string).
        /// </summary>
        ConfigBinder_GetValue_TypeOf_key = 0x80,

        /// <summary>
        /// GetValue(IConfiguration, Type, object?).
        /// </summary>
        ConfigBinder_GetValue_TypeOf_key_defaultValue = 0x100,

        /// <summary>
        /// GetValue<T>(IConfiguration, string).
        /// </summary>
        ConfigBinder_GetValue_T_key = 0x200,

        /// <summary>
        /// GetValue<T>(IConfiguration, string, T).
        /// </summary>
        ConfigBinder_GetValue_T_key_defaultValue = 0x400,

        // Method groups
        ConfigBinder_Bind = ConfigBinder_Bind_instance | ConfigBinder_Bind_instance_BinderOptions | ConfigBinder_Bind_key_instance,
        ConfigBinder_Get = ConfigBinder_Get_T | ConfigBinder_Get_T_BinderOptions | ConfigBinder_Get_TypeOf | ConfigBinder_Get_TypeOf_BinderOptions,
        ConfigBinder_GetValue = ConfigBinder_GetValue_T_key | ConfigBinder_GetValue_T_key_defaultValue | ConfigBinder_GetValue_TypeOf_key | ConfigBinder_GetValue_TypeOf_key_defaultValue,

        ConfigBinder_Any = ConfigBinder_Bind | ConfigBinder_Get | ConfigBinder_GetValue,
        #endregion ConfigurationBinder ext. method overloads.

        #region OptionsBuilder ext. method overloads: 0x800 - 0x2000
        /// <summary>
        /// Bind<T>(OptionsBuilder<T>, IConfiguration).
        /// </summary>
        OptionsBuilderExt_Bind_T = 0x800,

        /// <summary>
        /// Bind<T>(OptionsBuilder<T>, IConfiguration, Action<BinderOptions>?).
        /// </summary>
        OptionsBuilderExt_Bind_T_BinderOptions = 0x1000,

        /// <summary>
        /// BindConfiguration<T>(OptionsBuilder<T>, string, Action<BinderOptions>?).
        /// </summary>
        OptionsBuilderExt_BindConfiguration_T_path_BinderOptions = 0x2000,

        // Method group. BindConfiguration_T is its own method group.
        OptionsBuilderExt_Bind = OptionsBuilderExt_Bind_T | OptionsBuilderExt_Bind_T_BinderOptions,

        OptionsBuilderExt_BindConfiguration = OptionsBuilderExt_BindConfiguration_T_path_BinderOptions,

        OptionsBuilderExt_Any = OptionsBuilderExt_Bind | OptionsBuilderExt_BindConfiguration,
        #endregion OptionsBuilder ext. method overloads.

        #region IServiceCollection ext. method overloads: 0x4000 - 0x20000
        /// <summary>
        /// Configure<T>(IServiceCollection, IConfiguration).
        /// </summary>
        ServiceCollectionExt_Configure_T = 0x4000,

        /// <summary>
        /// Configure<T>(IServiceCollection, string, IConfiguration).
        /// </summary>
        ServiceCollectionExt_Configure_T_name = 0x8000,

        /// <summary>
        /// Configure<T>(IServiceCollection, IConfiguration, Action<BinderOptions>?).
        /// </summary>
        ServiceCollectionExt_Configure_T_BinderOptions = 0x10000,

        /// <summary>
        /// Configure<T>(IServiceCollection, string, IConfiguration, Action<BinderOptions>?).
        /// </summary>
        ServiceCollectionExt_Configure_T_name_BinderOptions = 0x20000,

        ServiceCollectionExt_Configure = ServiceCollectionExt_Configure_T | ServiceCollectionExt_Configure_T_name | ServiceCollectionExt_Configure_T_BinderOptions | ServiceCollectionExt_Configure_T_name_BinderOptions,

        ServiceCollectionExt_Any = ServiceCollectionExt_Configure,
        #endregion IServiceCollection ext. method overloads: 0x4000 - 0x20000
    }
}
