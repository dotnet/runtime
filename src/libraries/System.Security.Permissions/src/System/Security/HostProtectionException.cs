// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

namespace System.Security
{
#if NETCOREAPP
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial class HostProtectionException : SystemException
    {
        private const string ProtectedResourcesName = "ProtectedResources";
        private const string DemandedResourcesName = "DemandedResources";
        private const int E_HostProtection = -2146232768;

        public HostProtectionException() : base()
        {
            HResult = E_HostProtection;
            ProtectedResources = HostProtectionResource.None;
            DemandedResources = HostProtectionResource.None;
        }

        public HostProtectionException(string message) : base(message)
        {
            HResult = E_HostProtection;
            ProtectedResources = HostProtectionResource.None;
            DemandedResources = HostProtectionResource.None;
        }

        public HostProtectionException(string message, Exception e) : base(message, e)
        {
            HResult = E_HostProtection;
            ProtectedResources = HostProtectionResource.None;
            DemandedResources = HostProtectionResource.None;
        }

        public HostProtectionException(string message, HostProtectionResource protectedResources, HostProtectionResource demandedResources)
            : base(message)
        {
            HResult = E_HostProtection;
            ProtectedResources = protectedResources;
            DemandedResources = demandedResources;
        }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected HostProtectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ProtectedResources = (HostProtectionResource)info.GetValue(ProtectedResourcesName, typeof(HostProtectionResource));
            DemandedResources = (HostProtectionResource)info.GetValue(DemandedResourcesName, typeof(HostProtectionResource));
        }

        public HostProtectionResource DemandedResources { get; }

        public HostProtectionResource ProtectedResources { get; }

        private static void AppendResourceString(string resourceString, object attr, StringBuilder sb)
        {
            if (attr == null)
                return;

            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
            sb.Append(resourceString);
            sb.Append(Environment.NewLine);
            sb.Append(attr);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(base.ToString());
            AppendResourceString(SR.HostProtection_ProtectedResources, ProtectedResources, sb);
            AppendResourceString(SR.HostProtection_DemandedResources, DemandedResources, sb);

            return sb.ToString();
        }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(ProtectedResourcesName, ProtectedResources, typeof(HostProtectionResource));
            info.AddValue(DemandedResourcesName, DemandedResources, typeof(HostProtectionResource));
        }
    }
}
