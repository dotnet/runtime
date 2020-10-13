// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Permissions
{
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
            Authenticated = elAuth is null ? false : string.Equals(elAuth, "true", StringComparison.OrdinalIgnoreCase);
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
            if (ID is not null)
            {
                root.AddAttribute("ID", SecurityElement.Escape(ID));
            }
            if (Role is not null)
            {
                root.AddAttribute("Role", SecurityElement.Escape(Role));
            }

            return root;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Authenticated ? 0 : 101) +
                        (ID is null ? 0 : ID.GetHashCode()) +
                        (Role is null ? 0 : Role.GetHashCode()));
            }
        }
    }
}
