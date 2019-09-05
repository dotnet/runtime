// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Configuration
{
    public static partial class UserSecretsConfigurationExtensions
    {
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddUserSecrets(this Microsoft.Extensions.Configuration.IConfigurationBuilder configuration, System.Reflection.Assembly assembly) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddUserSecrets(this Microsoft.Extensions.Configuration.IConfigurationBuilder configuration, System.Reflection.Assembly assembly, bool optional) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddUserSecrets(this Microsoft.Extensions.Configuration.IConfigurationBuilder configuration, System.Reflection.Assembly assembly, bool optional, bool reloadOnChange) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddUserSecrets(this Microsoft.Extensions.Configuration.IConfigurationBuilder configuration, string userSecretsId) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddUserSecrets(this Microsoft.Extensions.Configuration.IConfigurationBuilder configuration, string userSecretsId, bool reloadOnChange) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddUserSecrets<T>(this Microsoft.Extensions.Configuration.IConfigurationBuilder configuration) where T : class { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddUserSecrets<T>(this Microsoft.Extensions.Configuration.IConfigurationBuilder configuration, bool optional) where T : class { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddUserSecrets<T>(this Microsoft.Extensions.Configuration.IConfigurationBuilder configuration, bool optional, bool reloadOnChange) where T : class { throw null; }
    }
}
namespace Microsoft.Extensions.Configuration.UserSecrets
{
    public partial class PathHelper
    {
        public PathHelper() { }
        public static string GetSecretsPathFromSecretsId(string userSecretsId) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, Inherited=false, AllowMultiple=false)]
    public partial class UserSecretsIdAttribute : System.Attribute
    {
        public UserSecretsIdAttribute(string userSecretId) { }
        public string UserSecretsId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
}
