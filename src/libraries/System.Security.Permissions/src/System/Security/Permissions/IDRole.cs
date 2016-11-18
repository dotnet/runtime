// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [Serializable]
    internal sealed class IDRole
    {
        internal bool Authenticated { get; }
        internal string ID { get; }
        internal string Role { get; }

        internal IDRole(bool authenticated, string id, string role)
        {
            Authenticated = authenticated;
            ID = id;
            Role = role;
        }

        internal IDRole(SecurityElement e)
        {
            string elAuth = e.Attribute("Authenticated");
            Authenticated = elAuth == null ? false : string.Equals(elAuth, "true", StringComparison.OrdinalIgnoreCase);
            ID = e.Attribute("ID");
            Role = e.Attribute("Role");
        }

        internal SecurityElement ToXml()
        {
            SecurityElement root = new SecurityElement("Identity");

            if (Authenticated)
            {
                root.AddAttribute("Authenticated", "true");
            }
            if (ID != null)
            {
                root.AddAttribute("ID", SecurityElement.Escape(ID));
            }
            if (Role != null)
            {
                root.AddAttribute("Role", SecurityElement.Escape(Role));
            }

            return root;
        }

        public override int GetHashCode()
        {
            return ((Authenticated ? 0 : 101) +
                        (ID == null ? 0 : ID.GetHashCode()) +
                        (Role == null ? 0 : Role.GetHashCode()));
        }
    }
}
