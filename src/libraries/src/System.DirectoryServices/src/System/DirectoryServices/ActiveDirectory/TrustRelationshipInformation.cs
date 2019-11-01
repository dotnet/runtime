// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    public enum TrustType
    {
        TreeRoot = 0,
        ParentChild = 1,
        CrossLink = 2,
        External = 3,
        Forest = 4,
        Kerberos = 5,
        Unknown = 6
    }

    public enum TrustDirection
    {
        Inbound = 1,
        Outbound = 2,
        Bidirectional = Outbound | Inbound
    }

    public class TrustRelationshipInformation
    {
        internal string source = null;
        internal string target = null;
        internal TrustType type;
        internal TrustDirection direction;
        internal DirectoryContext context = null;

        internal TrustRelationshipInformation() { }

        internal TrustRelationshipInformation(DirectoryContext context, string source, TrustObject obj)
        {
            // security context
            this.context = context;
            // source
            this.source = source;
            // target
            this.target = (obj.DnsDomainName == null ? obj.NetbiosDomainName : obj.DnsDomainName);
            // direction
            if ((obj.Flags & (int)DS_DOMAINTRUST_FLAG.DS_DOMAIN_DIRECT_OUTBOUND) != 0 &&
                (obj.Flags & (int)DS_DOMAINTRUST_FLAG.DS_DOMAIN_DIRECT_INBOUND) != 0)
                direction = TrustDirection.Bidirectional;
            else if ((obj.Flags & (int)DS_DOMAINTRUST_FLAG.DS_DOMAIN_DIRECT_OUTBOUND) != 0)
                direction = TrustDirection.Outbound;
            else if ((obj.Flags & (int)DS_DOMAINTRUST_FLAG.DS_DOMAIN_DIRECT_INBOUND) != 0)
                direction = TrustDirection.Inbound;
            // type
            this.type = obj.TrustType;
        }

        public string SourceName => source;

        public string TargetName => target;

        public TrustType TrustType => type;

        public TrustDirection TrustDirection => direction;
    }
}
