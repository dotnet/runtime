// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using System.Security.Permissions;

namespace System.Net.Mail
{
    public enum SmtpAccess { None = 0, Connect = 1, ConnectToUnrestrictedPort = 2 };

    [Serializable]
    internal sealed class SmtpPermission : CodeAccessPermission, IUnrestrictedPermission
    {
        private SmtpAccess _access;
        private bool _unrestricted;

        public SmtpPermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
            {
                _access = SmtpAccess.ConnectToUnrestrictedPort;
                _unrestricted = true;
            }
            else
            {
                _access = SmtpAccess.None;
            }
        }

        public SmtpPermission(bool unrestricted)
        {
            if (unrestricted)
            {
                _access = SmtpAccess.ConnectToUnrestrictedPort;
                _unrestricted = true;
            }
            else
            {
                _access = SmtpAccess.None;
            }
        }

        public SmtpPermission(SmtpAccess access)
        {
            _access = access;
        }

        public SmtpAccess Access
        {
            get
            {
                return _access;
            }
        }

        public void AddPermission(SmtpAccess access)
        {
            if (access > _access)
                _access = access;
        }

        public bool IsUnrestricted()
        {
            return _unrestricted;
        }

        public override IPermission Copy()
        {
            if (_unrestricted)
            {
                return new SmtpPermission(true);
            }
            return new SmtpPermission(_access);
        }

        public override IPermission Union(IPermission target)
        {
            if (target == null)
            {
                return Copy();
            }
            SmtpPermission other = target as SmtpPermission;
            if (other == null)
            {
                throw new ArgumentException(SR.Format(SR.net_perm_target), nameof(target));
            }

            if (_unrestricted || other.IsUnrestricted())
            {
                return new SmtpPermission(true);
            }

            return new SmtpPermission(_access > other._access ? _access : other._access);
        }

        public override IPermission Intersect(IPermission target)
        {
            if (target == null)
            {
                return null;
            }

            SmtpPermission other = target as SmtpPermission;
            if (other == null)
            {
                throw new ArgumentException(SR.Format(SR.net_perm_target), nameof(target));
            }

            if (IsUnrestricted() && other.IsUnrestricted())
            {
                return new SmtpPermission(true);
            }

            return new SmtpPermission(_access < other._access ? _access : other._access);
        }

        public override bool IsSubsetOf(IPermission target)
        {
            // Pattern suggested by security engine
            if (target == null)
            {
                return (_access == SmtpAccess.None);
            }

            SmtpPermission other = target as SmtpPermission;
            if (other == null)
            {
                throw new ArgumentException(SR.Format(SR.net_perm_target), nameof(target));
            }

            if (_unrestricted && !other.IsUnrestricted())
            {
                return false;
            }

            return (other._access >= _access);
        }

        public override void FromXml(SecurityElement securityElement)
        {
            if (securityElement == null)
            {
                throw new ArgumentNullException(nameof(securityElement));
            }
            if (!securityElement.Tag.Equals("IPermission"))
            {
                throw new ArgumentException(SR.net_not_ipermission, nameof(securityElement));
            }

            string className = securityElement.Attribute("class");

            if (className == null)
            {
                throw new ArgumentException(SR.net_no_classname, nameof(securityElement));
            }
            if (className.IndexOf(GetType().FullName) < 0)
            {
                throw new ArgumentException(SR.net_no_typename, nameof(securityElement));
            }

            String str = securityElement.Attribute("Unrestricted");
            if (str != null)
            {
                if (0 == string.Compare(str, "true", StringComparison.OrdinalIgnoreCase))
                {
                    _access = SmtpAccess.ConnectToUnrestrictedPort;
                    _unrestricted = true;
                    return;
                }
            }

            str = securityElement.Attribute("Access");
            if (str != null)
            {
                if (0 == string.Compare(str, "Connect", StringComparison.OrdinalIgnoreCase))
                {
                    _access = SmtpAccess.Connect;
                }
                else if (0 == string.Compare(str, "ConnectToUnrestrictedPort", StringComparison.OrdinalIgnoreCase))
                {
                    _access = SmtpAccess.ConnectToUnrestrictedPort;
                }
                else if (0 == string.Compare(str, "None", StringComparison.OrdinalIgnoreCase))
                {
                    _access = SmtpAccess.None;
                }
                else
                {
                    throw new ArgumentException(SR.Format(SR.net_perm_invalid_val_in_element), "Access");
                }
            }
        }

        public override SecurityElement ToXml()
        {
            SecurityElement securityElement = new SecurityElement("IPermission");

            securityElement.AddAttribute("class", GetType().FullName + ", " + GetType().Module.Assembly.FullName.Replace('\"', '\''));
            securityElement.AddAttribute("version", "1");

            if (_unrestricted)
            {
                securityElement.AddAttribute("Unrestricted", "true");
                return securityElement;
            }

            if (_access == SmtpAccess.Connect)
            {
                securityElement.AddAttribute("Access", "Connect");
            }
            else if (_access == SmtpAccess.ConnectToUnrestrictedPort)
            {
                securityElement.AddAttribute("Access", "ConnectToUnrestrictedPort");
            }
            return securityElement;
        }
    }
}
