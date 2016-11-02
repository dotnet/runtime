// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;

namespace System.Security.Permissions
{
    [Serializable]
    internal partial class IDRole
    {
        internal bool m_authenticated;
        internal string m_id;
        internal string m_role;

        public override int GetHashCode()
        {
            return ((m_authenticated ? 0 : 101) +
                        (m_id == null ? 0 : m_id.GetHashCode()) +
                        (m_role == null ? 0 : m_role.GetHashCode()));
        }

        internal SecurityElement ToXml()
        {
            SecurityElement root = new SecurityElement("Identity");

            if (m_authenticated)
                root.AddAttribute("Authenticated", "true");

            if (m_id != null)
            {
                root.AddAttribute("ID", SecurityElement.Escape(m_id));
            }

            if (m_role != null)
            {
                root.AddAttribute("Role", SecurityElement.Escape(m_role));
            }

            return root;
        }

        internal void FromXml(SecurityElement e)
        {
            string elAuth = e.Attribute("Authenticated");
            if (elAuth != null)
            {
                m_authenticated = string.Compare(elAuth, "true", StringComparison.OrdinalIgnoreCase) == 0;
            }
            else
            {
                m_authenticated = false;
            }

            string elID = e.Attribute("ID");
            if (elID != null)
            {
                m_id = elID;
            }
            else
            {
                m_id = null;
            }

            string elRole = e.Attribute("Role");
            if (elRole != null)
            {
                m_role = elRole;
            }
            else
            {
                m_role = null;
            }
        }
    }

    [Serializable]
    public sealed partial class PrincipalPermission : IPermission, ISecurityEncodable, IUnrestrictedPermission
    {
        private IDRole[] m_array;

        public PrincipalPermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
            {
                m_array = new IDRole[1];
                m_array[0] = new IDRole();
                m_array[0].m_authenticated = true;
                m_array[0].m_id = null;
                m_array[0].m_role = null;
            }
            else if (state == PermissionState.None)
            {
                m_array = new IDRole[1];
                m_array[0] = new IDRole();
                m_array[0].m_authenticated = false;
                m_array[0].m_id = "";
                m_array[0].m_role = "";
            }
            else
            {
                throw new ArgumentException(SR.Argument_InvalidPermissionState);
            }
        }

        public PrincipalPermission(string name, string role) : this(name, role, isAuthenticated: true) { }

        public PrincipalPermission(string name, string role, bool isAuthenticated)
        {
            m_array = new IDRole[1];
            m_array[0] = new IDRole();
            m_array[0].m_authenticated = isAuthenticated;
            m_array[0].m_id = name;
            m_array[0].m_role = role;
        }

        private PrincipalPermission(IDRole[] array)
        {
            m_array = array;
        }

        private bool IsEmpty()
        {
            for (int i = 0; i < m_array.Length; ++i)
            {
                if ((m_array[i].m_id == null || m_array[i].m_id.Length != 0) ||
                    (m_array[i].m_role == null || m_array[i].m_role.Length != 0) ||
                    m_array[i].m_authenticated)
                {
                    return false;
                }
            }
            return true;
        }

        private bool VerifyType(IPermission perm)
        {
            // if perm is null, then obviously not of the same type
            return (perm != null) && (perm.GetType() == GetType());
        }

        public bool IsUnrestricted()
        {
            for (int i = 0; i < m_array.Length; ++i)
            {
                if (m_array[i].m_id != null || m_array[i].m_role != null || !m_array[i].m_authenticated)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsSubsetOf(IPermission target)
        {
            if (target == null)
            {
                return IsEmpty();
            }

            try
            {
                PrincipalPermission operand = (PrincipalPermission)target;

                if (operand.IsUnrestricted())
                    return true;
                else if (IsUnrestricted())
                    return false;
                else
                {
                    for (int i = 0; i < m_array.Length; ++i)
                    {
                        bool foundMatch = false;

                        for (int j = 0; j < operand.m_array.Length; ++j)
                        {
                            if (operand.m_array[j].m_authenticated == m_array[i].m_authenticated &&
                                (operand.m_array[j].m_id == null ||
                                 (m_array[i].m_id != null && m_array[i].m_id.Equals(operand.m_array[j].m_id))) &&
                                (operand.m_array[j].m_role == null ||
                                 (m_array[i].m_role != null && m_array[i].m_role.Equals(operand.m_array[j].m_role))))
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
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(SR.Argument_WrongType, GetType().FullName);
            }
        }

        public IPermission Intersect(IPermission target)
        {
            if (target == null)
            {
                return null;
            }
            else if (!VerifyType(target))
            {
                throw new ArgumentException(SR.Argument_WrongType, GetType().FullName);
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

            for (int i = 0; i < m_array.Length; ++i)
            {
                for (int j = 0; j < operand.m_array.Length; ++j)
                {
                    if (operand.m_array[j].m_authenticated == m_array[i].m_authenticated)
                    {
                        if (operand.m_array[j].m_id == null ||
                            m_array[i].m_id == null ||
                            m_array[i].m_id.Equals(operand.m_array[j].m_id))
                        {
                            if (idroles == null)
                            {
                                idroles = new List<IDRole>();
                            }

                            IDRole idrole = new IDRole();

                            idrole.m_id = operand.m_array[j].m_id == null ? m_array[i].m_id : operand.m_array[j].m_id;

                            if (operand.m_array[j].m_role == null ||
                                m_array[i].m_role == null ||
                                m_array[i].m_role.Equals(operand.m_array[j].m_role))
                            {
                                idrole.m_role = operand.m_array[j].m_role == null ? m_array[i].m_role : operand.m_array[j].m_role;
                            }
                            else
                            {
                                idrole.m_role = "";
                            }

                            idrole.m_authenticated = operand.m_array[j].m_authenticated;

                            idroles.Add(idrole);
                        }
                        else if (operand.m_array[j].m_role == null ||
                                 m_array[i].m_role == null ||
                                 m_array[i].m_role.Equals(operand.m_array[j].m_role))
                        {
                            if (idroles == null)
                            {
                                idroles = new List<IDRole>();
                            }

                            IDRole idrole = new IDRole();

                            idrole.m_id = "";
                            idrole.m_role = operand.m_array[j].m_role == null ? m_array[i].m_role : operand.m_array[j].m_role;
                            idrole.m_authenticated = operand.m_array[j].m_authenticated;

                            idroles.Add(idrole);
                        }
                    }
                }
            }

            if (idroles == null)
            {
                return null;
            }
            else
            {
                IDRole[] idrolesArray = new IDRole[idroles.Count];

                IEnumerator idrolesEnumerator = idroles.GetEnumerator();
                int index = 0;

                while (idrolesEnumerator.MoveNext())
                {
                    idrolesArray[index++] = (IDRole)idrolesEnumerator.Current;
                }

                return new PrincipalPermission(idrolesArray);
            }
        }

        public IPermission Union(IPermission other)
        {
            if (other == null)
            {
                return Copy();
            }
            else if (!VerifyType(other))
            {
                throw new ArgumentException(SR.Argument_WrongType, GetType().FullName);
            }

            PrincipalPermission operand = (PrincipalPermission)other;

            if (IsUnrestricted() || operand.IsUnrestricted())
            {
                return new PrincipalPermission(PermissionState.Unrestricted);
            }

            // Now we have to do a real union
            int combinedLength = m_array.Length + operand.m_array.Length;
            IDRole[] idrolesArray = new IDRole[combinedLength];

            int i, j;
            for (i = 0; i < m_array.Length; ++i)
            {
                idrolesArray[i] = m_array[i];
            }

            for (j = 0; j < operand.m_array.Length; ++j)
            {
                idrolesArray[i + j] = operand.m_array[j];
            }

            return new PrincipalPermission(idrolesArray);

        }

        public override bool Equals(object obj)
        {
            IPermission perm = obj as IPermission;
            if (obj != null && perm == null)
                return false;
            if (!IsSubsetOf(perm))
                return false;
            if (perm != null && !perm.IsSubsetOf(this))
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            int i;
            for (i = 0; i < m_array.Length; i++)
                hash += m_array[i].GetHashCode();
            return hash;
        }

        public IPermission Copy()
        {
            return new PrincipalPermission(m_array);
        }

        public void Demand()
        {
            new SecurityPermission(SecurityPermissionFlag.ControlPrincipal).Assert();
            IPrincipal principal = Thread.CurrentPrincipal;
            if (principal == null)
                throw new SecurityException(SR.Security_PrincipalPermission);
            if (m_array == null)
                return;

            // A demand passes when the grant satisfies all entries.
            int count = m_array.Length;
            bool foundMatch = false;
            for (int i = 0; i < count; ++i)
            {
                // If the demand is authenticated, we need to check the identity and role
                if (m_array[i].m_authenticated)
                {
                    IIdentity identity = principal.Identity;

                    if ((identity.IsAuthenticated &&
                         (m_array[i].m_id == null || string.Compare(identity.Name, m_array[i].m_id, StringComparison.OrdinalIgnoreCase) == 0)))
                    {
                        if (m_array[i].m_role == null)
                        {
                            foundMatch = true;
                        }
                        else
                        {
                            foundMatch = principal.IsInRole(m_array[i].m_role);
                        }

                        if (foundMatch)
                            break;
                    }
                }
                else
                {
                    foundMatch = true;
                    break;
                }
            }

            if (!foundMatch)
                throw new SecurityException(SR.Security_PrincipalPermission);
        }

        public SecurityElement ToXml()
        {
            SecurityElement root = new SecurityElement("IPermission");

            string typename = "System.Security.Permissions.PrincipalPermission";
            root.AddAttribute("class", typename + ", " + GetType().Module.Assembly.FullName.Replace('\"', '\''));
            root.AddAttribute("version", "1");

            if (m_array != null)
            {
                int count = m_array.Length;
                for (int i = 0; i < count; ++i)
                {
                    root.AddChild(m_array[i].ToXml());
                }
            }

            return root;
        }

        public void FromXml(SecurityElement elem)
        {
            if (elem == null)
                throw new ArgumentNullException(nameof(elem));

            if (elem.Tag == null || !elem.Tag.Equals("Permission") && !elem.Tag.Equals("IPermission"))
                throw new ArgumentException(SR.Argument_NotAPermissionElement);

            string version = elem.Attribute("version");

            if (version == null || (version != null && !version.Equals("1")))
                throw new ArgumentException(SR.Argument_InvalidXMLBadVersion);

            if (elem.InternalChildren != null && elem.InternalChildren.Count != 0)
            {
                int numChildren = elem.InternalChildren.Count;
                int count = 0;

                m_array = new IDRole[numChildren];

                IEnumerator enumerator = elem.Children.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    IDRole idrole = new IDRole();

                    idrole.FromXml((SecurityElement)enumerator.Current);

                    m_array[count++] = idrole;
                }
            }
            else
                m_array = new IDRole[0];
        }

        public override string ToString()
        {
            return ToXml().ToString();
        }
    }
}
