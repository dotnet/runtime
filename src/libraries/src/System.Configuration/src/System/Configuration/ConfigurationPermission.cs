// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using System.Security.Permissions;

namespace System.Configuration
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    [Serializable]
    public sealed class ConfigurationPermissionAttribute : CodeAccessSecurityAttribute
    {
        public ConfigurationPermissionAttribute(SecurityAction action) : base(action) { }

        public override IPermission CreatePermission()
        {
            PermissionState state = Unrestricted
                ? PermissionState.Unrestricted
                : PermissionState.None;

            return new ConfigurationPermission(state);
        }
    }

    //
    // ConfigurationPermission is used to grant access to configuration sections that
    // would not otherwise be available if the caller attempted to read the configuration
    // files that make up configuration.
    //
    // The permission is a simple boolean one - it is either fully granted or denied.
    // This boolean state is represented by using the PermissionState enumeration.
    //
    [Serializable]
    public sealed class ConfigurationPermission : CodeAccessPermission, IUnrestrictedPermission
    {
        private PermissionState _permissionState; // Unrestricted or None

        //
        // Creates a new instance of ConfigurationPermission
        // that passes all demands or that fails all demands.
        //
        public ConfigurationPermission(PermissionState state)
        {
            // validate state parameter
            switch (state)
            {
                case PermissionState.Unrestricted:
                case PermissionState.None:
                    _permissionState = state;
                    break;

                default:
                    throw ExceptionUtil.ParameterInvalid("state");
            }
        }

        public bool IsUnrestricted()
        {
            return _permissionState == PermissionState.Unrestricted;
        }

        public override IPermission Copy()
        {
            return new ConfigurationPermission(_permissionState);
        }

        public override IPermission Union(IPermission target)
        {
            if (target == null) return Copy();

            if (target.GetType() != typeof(ConfigurationPermission)) throw ExceptionUtil.ParameterInvalid("target");

            // Create an Unrestricted permission if either this or other is unrestricted
            if (_permissionState == PermissionState.Unrestricted)
                return new ConfigurationPermission(PermissionState.Unrestricted);
            ConfigurationPermission other = (ConfigurationPermission)target;
            return new ConfigurationPermission(other._permissionState);
        }

        public override IPermission Intersect(IPermission target)
        {
            if (target == null) return null;

            if (target.GetType() != typeof(ConfigurationPermission)) throw ExceptionUtil.ParameterInvalid("target");

            // Create an None permission if either this or other is None
            if (_permissionState == PermissionState.None) return new ConfigurationPermission(PermissionState.None);
            ConfigurationPermission other = (ConfigurationPermission)target;
            return new ConfigurationPermission(other._permissionState);
        }

        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null) return _permissionState == PermissionState.None;

            if (target.GetType() != typeof(ConfigurationPermission)) throw ExceptionUtil.ParameterInvalid("target");

            ConfigurationPermission other = (ConfigurationPermission)target;
            return (_permissionState == PermissionState.None) ||
                (other._permissionState == PermissionState.Unrestricted);
        }

        public override void FromXml(SecurityElement securityElement)
        {
            if (securityElement == null)
                throw new ArgumentNullException(string.Format(SR.ConfigurationPermissionBadXml, "securityElement"));

            if (!securityElement.Tag.Equals("IPermission"))
                throw new ArgumentException(string.Format(SR.ConfigurationPermissionBadXml, "securityElement"));

            string className = securityElement.Attribute("class");
            if (className == null)
                throw new ArgumentException(string.Format(SR.ConfigurationPermissionBadXml, "securityElement"));

            if (className.IndexOf(GetType().FullName, StringComparison.Ordinal) < 0)
                throw new ArgumentException(string.Format(SR.ConfigurationPermissionBadXml, "securityElement"));

            string version = securityElement.Attribute("version");
            if (version != "1") throw new ArgumentException(string.Format(SR.ConfigurationPermissionBadXml, "version"));

            string unrestricted = securityElement.Attribute("Unrestricted");
            if (unrestricted == null) _permissionState = PermissionState.None;
            else
            {
                switch (unrestricted)
                {
                    case "true":
                        _permissionState = PermissionState.Unrestricted;
                        break;
                    case "false":
                        _permissionState = PermissionState.None;
                        break;
                    default:
                        throw new ArgumentException(string.Format(SR.ConfigurationPermissionBadXml, "Unrestricted"));
                }
            }
        }

        public override SecurityElement ToXml()
        {
            SecurityElement securityElement = new SecurityElement("IPermission");
            securityElement.AddAttribute("class",
                GetType().FullName + ", " + GetType().Module.Assembly.FullName.Replace('\"', '\''));
            securityElement.AddAttribute("version", "1");
            if (IsUnrestricted()) securityElement.AddAttribute("Unrestricted", "true");

            return securityElement;
        }
    }
}