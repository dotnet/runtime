// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;

namespace System.Security.Permissions
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed class PrincipalPermission : IPermission, ISecurityEncodable, IUnrestrictedPermission
    {
        private IDRole[] _idArray;

        public PrincipalPermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
            {
                _idArray = new IDRole[] { new IDRole(true, null, null) };
            }
            else if (state == PermissionState.None)
            {
                _idArray = new IDRole[] { new IDRole(false, string.Empty, string.Empty) };
            }
            else
            {
                throw new ArgumentException(SR.Argument_InvalidPermissionState);
            }
        }

        public PrincipalPermission(string name, string role)
        {
            _idArray = new IDRole[] { new IDRole(true, name, role) };
        }

        public PrincipalPermission(string name, string role, bool isAuthenticated)
        {
            _idArray = new IDRole[] { new IDRole(isAuthenticated, name, role) };
        }

        private PrincipalPermission(IDRole[] array)
        {
            _idArray = array;
        }

        private bool IsEmpty()
        {
            foreach (IDRole idRole in _idArray)
            {
                if (idRole.ID is null || idRole.ID.Length != 0 || idRole.Role is null || idRole.Role.Length != 0 || idRole.Authenticated)
                    return false;
            }
            return true;
        }

        private bool VerifyType(IPermission perm)
        {
            // if perm is null, then obviously not of the same type
            return (perm is not null) && (perm.GetType() == GetType());
        }

        public bool IsUnrestricted()
        {
            foreach (IDRole idRole in _idArray)
            {
                if (idRole.ID is not null || idRole.Role is not null || !idRole.Authenticated)
                    return false;
            }
            return true;
        }

        public bool IsSubsetOf(IPermission target)
        {
            if (target is null)
            {
                return IsEmpty();
            }
            else if (!VerifyType(target))
            {
                throw new ArgumentException(SR.Format(SR.Argument_WrongType, GetType().FullName), nameof(target));
            }

            PrincipalPermission operand = (PrincipalPermission)target;

            if (operand.IsUnrestricted())
            {
                return true;
            }
            else if (IsUnrestricted())
            {
                return false;
            }

            foreach (IDRole idRole in _idArray)
            {
                bool foundMatch = false;
                foreach (IDRole operandIdRole in operand._idArray)
                {
                    if ((operandIdRole.Authenticated == idRole.Authenticated) &&
                        (operandIdRole.ID is null || (idRole.ID is not null && idRole.ID.Equals(operandIdRole.ID))) &&
                        (operandIdRole.Role is null || (idRole.Role is not null && idRole.Role.Equals(operandIdRole.Role))))
                    {
                        foundMatch = true;
                        break;
                    }
                }

                if (!foundMatch)
                    return false;
            }

            return true;
        }

        public IPermission Intersect(IPermission target)
        {
            if (target is null)
            {
                return null;
            }
            else if (!VerifyType(target))
            {
                throw new ArgumentException(SR.Format(SR.Argument_WrongType, GetType().FullName), nameof(target));
            }
            else if (IsUnrestricted())
            {
                return target.Copy();
            }

            PrincipalPermission operand = (PrincipalPermission)target;

            if (operand.IsUnrestricted())
            {
                return Copy();
            }

            List<IDRole> idroles = null;
            foreach (IDRole idRole in _idArray)
            {
                foreach (IDRole operandIdRole in operand._idArray)
                {
                    if (operandIdRole.Authenticated == idRole.Authenticated)
                    {
                        string newID = string.Empty;
                        string newRole = string.Empty;
                        bool newAuthenticated = operandIdRole.Authenticated;
                        bool addToNewIDRoles = false;

                        if (operandIdRole.ID is null || idRole.ID is null || idRole.ID.Equals(operandIdRole.ID))
                        {
                            newID = operandIdRole.ID is null ? idRole.ID : operandIdRole.ID;
                            addToNewIDRoles = true;
                        }
                        if (operandIdRole.Role is null || idRole.Role is null || idRole.Role.Equals(operandIdRole.Role))
                        {
                            newRole = operandIdRole.Role is null ? idRole.Role : operandIdRole.Role;
                            addToNewIDRoles = true;
                        }
                        if (addToNewIDRoles)
                        {
                            if (idroles is null)
                                idroles = new List<IDRole>();
                            idroles.Add(new IDRole(newAuthenticated, newID, newRole));
                        }
                    }
                }
            }

            return (idroles is null) ? null : new PrincipalPermission(idroles.ToArray());
        }

        public IPermission Union(IPermission other)
        {
            if (other is null)
            {
                return Copy();
            }
            else if (!VerifyType(other))
            {
                throw new ArgumentException(SR.Format(SR.Argument_WrongType, GetType().FullName), nameof(other));
            }

            PrincipalPermission operand = (PrincipalPermission)other;

            if (IsUnrestricted() || operand.IsUnrestricted())
            {
                return new PrincipalPermission(PermissionState.Unrestricted);
            }

            IDRole[] idrolesArray = new IDRole[_idArray.Length + operand._idArray.Length];
            Array.Copy(_idArray, idrolesArray, _idArray.Length);
            Array.Copy(operand._idArray, 0, idrolesArray, _idArray.Length, operand._idArray.Length);

            return new PrincipalPermission(idrolesArray);
        }

        public override bool Equals(object obj)
        {
            IPermission perm = obj as IPermission;
            if (obj is not null && perm is null)
                return false;
            if (!IsSubsetOf(perm))
                return false;
            if (perm is not null && !perm.IsSubsetOf(this))
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (IDRole idRole in _idArray)
                hash += idRole.GetHashCode();
            return hash;
        }

        public IPermission Copy()
        {
            return new PrincipalPermission(_idArray);
        }

        public void Demand()
        {
            IPrincipal principal = Thread.CurrentPrincipal;
            if (principal is null)
                throw new SecurityException(SR.Security_PrincipalPermission);
            if (_idArray is null)
                return;

            // A demand passes when the grant satisfies all entries.
            foreach (IDRole idRole in _idArray)
            {
                // If the demand is authenticated, we need to check the identity and role
                if (!idRole.Authenticated)
                {
                    return;
                }
                else if (principal.Identity.IsAuthenticated &&
                         (idRole.ID is null || string.Equals(principal.Identity.Name, idRole.ID, StringComparison.OrdinalIgnoreCase)))
                {
                    if (idRole.Role is null || principal.IsInRole(idRole.Role))
                        return;
                }
            }

            throw new SecurityException(SR.Security_PrincipalPermission);
        }

        public SecurityElement ToXml()
        {
            SecurityElement root = new SecurityElement("IPermission");

            string typename = "System.Security.Permissions.PrincipalPermission";
            root.AddAttribute("class", typename + ", " + GetType().Module.Assembly.FullName.Replace('\"', '\''));
            root.AddAttribute("version", "1");

            if (_idArray is not null)
            {
                foreach (IDRole idRole in _idArray)
                {
                    root.AddChild(idRole.ToXml());
                }
            }

            return root;
        }

        public void FromXml(SecurityElement elem)
        {
            if (elem is null)
                throw new ArgumentNullException(nameof(elem));

            if (elem.Tag is null || !elem.Tag.Equals("Permission") && !elem.Tag.Equals("IPermission"))
                throw new ArgumentException(SR.Argument_NotAPermissionElement);

            string version = elem.Attribute("version");

            if (version is null || (version is not null && !version.Equals("1")))
                throw new ArgumentException(SR.Argument_InvalidXMLBadVersion);

            if (elem.Children is not null && elem.Children.Count != 0)
            {
                int numChildren = elem.Children.Count;
                int count = 0;

                _idArray = new IDRole[numChildren];
                foreach (object curr in elem.Children)
                {
                    _idArray[count++] = new IDRole((SecurityElement)curr);
                }
            }
            else
                _idArray = Array.Empty<IDRole>();
        }

        public override string ToString()
        {
            return ToXml().ToString();
        }
    }
}
