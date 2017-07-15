// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.ActiveDirectory
{
    internal class PropertyManager
    {
        public static string DefaultNamingContext = "defaultNamingContext";
        public static string SchemaNamingContext = "schemaNamingContext";
        public static string ConfigurationNamingContext = "configurationNamingContext";
        public static string RootDomainNamingContext = "rootDomainNamingContext";
        public static string MsDSBehaviorVersion = "msDS-Behavior-Version";
        public static string FsmoRoleOwner = "fsmoRoleOwner";
        public static string ForestFunctionality = "forestFunctionality";
        public static string NTMixedDomain = "ntMixedDomain";
        public static string DomainFunctionality = "domainFunctionality";
        public static string ObjectCategory = "objectCategory";
        public static string SystemFlags = "systemFlags";
        public static string DnsRoot = "dnsRoot";
        public static string DistinguishedName = "distinguishedName";
        public static string TrustParent = "trustParent";
        // disable csharp compiler warning #0414: field assigned unused value
#pragma warning disable 0414
        public static string FlatName = "flatName";
        public static string Name = "name";
        public static string Flags = "flags";
        public static string TrustType = "trustType";
        public static string TrustAttributes = "trustAttributes";
#pragma warning restore 0414
        public static string BecomeSchemaMaster = "becomeSchemaMaster";
        public static string BecomeDomainMaster = "becomeDomainMaster";
        public static string BecomePdc = "becomePdc";
        public static string BecomeRidMaster = "becomeRidMaster";
        public static string BecomeInfrastructureMaster = "becomeInfrastructureMaster";
        public static string DnsHostName = "dnsHostName";
        public static string Options = "options";
        public static string CurrentTime = "currentTime";
        public static string HighestCommittedUSN = "highestCommittedUSN";
        public static string OperatingSystem = "operatingSystem";
        public static string HasMasterNCs = "hasMasterNCs";
        public static string MsDSHasMasterNCs = "msDS-HasMasterNCs";
        public static string MsDSHasFullReplicaNCs = "msDS-hasFullReplicaNCs";
        public static string NCName = "nCName";
        public static string Cn = "cn";
        // disable csharp compiler warning #0414: field assigned unused value
#pragma warning disable 0414
        public static string NETBIOSName = "nETBIOSName";
#pragma warning restore 0414
        public static string DomainDNS = "domainDNS";
        public static string InstanceType = "instanceType";
        public static string MsDSSDReferenceDomain = "msDS-SDReferenceDomain";
        public static string MsDSPortLDAP = "msDS-PortLDAP";
        public static string MsDSPortSSL = "msDS-PortSSL";
        public static string MsDSNCReplicaLocations = "msDS-NC-Replica-Locations";
        public static string MsDSNCROReplicaLocations = "msDS-NC-RO-Replica-Locations";
        public static string SupportedCapabilities = "supportedCapabilities";
        public static string ServerName = "serverName";
        public static string Enabled = "Enabled";
        public static string ObjectGuid = "objectGuid";
        public static string Keywords = "keywords";
        public static string ServiceBindingInformation = "serviceBindingInformation";
        public static string MsDSReplAuthenticationMode = "msDS-ReplAuthenticationMode";
        public static string HasPartialReplicaNCs = "hasPartialReplicaNCs";
        // disable csharp compiler warning #0414: field assigned unused value
#pragma warning disable 0414
        public static string Container = "container";
#pragma warning restore 0414
        public static string LdapDisplayName = "ldapDisplayName";
        public static string AttributeID = "attributeID";
        public static string AttributeSyntax = "attributeSyntax";
        public static string Description = "description";
        public static string SearchFlags = "searchFlags";
        public static string OMSyntax = "oMSyntax";
        public static string OMObjectClass = "oMObjectClass";
        public static string IsSingleValued = "isSingleValued";
        public static string IsDefunct = "isDefunct";
        public static string RangeUpper = "rangeUpper";
        public static string RangeLower = "rangeLower";
        public static string IsMemberOfPartialAttributeSet = "isMemberOfPartialAttributeSet";
        // disable csharp compiler warning #0414: field assigned unused value
#pragma warning disable 0414
        public static string ObjectVersion = "objectVersion";
#pragma warning restore 0414
        public static string LinkID = "linkID";
        public static string ObjectClassCategory = "objectClassCategory";
        public static string SchemaUpdateNow = "schemaUpdateNow";
        public static string SubClassOf = "subClassOf";
        public static string SchemaIDGuid = "schemaIDGUID";
        public static string PossibleSuperiors = "possSuperiors";
        public static string PossibleInferiors = "possibleInferiors";
        public static string MustContain = "mustContain";
        public static string MayContain = "mayContain";
        public static string SystemMustContain = "systemMustContain";
        public static string SystemMayContain = "systemMayContain";
        public static string GovernsID = "governsID";
        public static string IsGlobalCatalogReady = "isGlobalCatalogReady";
        // disable csharp compiler warning #0414: field assigned unused value
#pragma warning disable 0414
        public static string NTSecurityDescriptor = "ntSecurityDescriptor";
#pragma warning restore 0414
        public static string DsServiceName = "dsServiceName";
        public static string ReplicateSingleObject = "replicateSingleObject";
        // disable csharp compiler warning #0414: field assigned unused value
#pragma warning disable 0414
        public static string MsDSMasteredBy = "msDS-masteredBy";
#pragma warning restore 0414
        public static string DefaultSecurityDescriptor = "defaultSecurityDescriptor";
        public static string NamingContexts = "namingContexts";
        public static string MsDSDefaultNamingContext = "msDS-DefaultNamingContext";
        public static string OperatingSystemVersion = "operatingSystemVersion";
        public static string AuxiliaryClass = "auxiliaryClass";
        public static string SystemAuxiliaryClass = "systemAuxiliaryClass";
        public static string SystemPossibleSuperiors = "systemPossSuperiors";
        public static string InterSiteTopologyGenerator = "interSiteTopologyGenerator";
        public static string FromServer = "fromServer";
        public static string RIDAvailablePool = "rIDAvailablePool";

        // disable csharp compiler warning #0414: field assigned unused value
#pragma warning disable 0414
        public static string SiteList = "siteList";
#pragma warning restore 0414
        public static string MsDSHasInstantiatedNCs = "msDS-HasInstantiatedNCs";

        public static object GetPropertyValue(DirectoryEntry directoryEntry, string propertyName)
        {
            return GetPropertyValue(null, directoryEntry, propertyName);
        }

        public static object GetPropertyValue(DirectoryContext context, DirectoryEntry directoryEntry, string propertyName)
        {
            Debug.Assert(directoryEntry != null, "PropertyManager::GetPropertyValue - directoryEntry is null");

            Debug.Assert(propertyName != null, "PropertyManager::GetPropertyValue - propertyName is null");

            try
            {
                if (directoryEntry.Properties[propertyName].Count == 0)
                {
                    if (directoryEntry.Properties[PropertyManager.DistinguishedName].Count != 0)
                    {
                        throw new ActiveDirectoryOperationException(SR.Format(SR.PropertyNotFoundOnObject , propertyName, directoryEntry.Properties[PropertyManager.DistinguishedName].Value));
                    }
                    else
                    {
                        throw new ActiveDirectoryOperationException(SR.Format(SR.PropertyNotFound , propertyName));
                    }
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }

            return directoryEntry.Properties[propertyName].Value;
        }

        public static object GetSearchResultPropertyValue(SearchResult res, string propertyName)
        {
            Debug.Assert(res != null, "PropertyManager::GetSearchResultPropertyValue - res is null");

            Debug.Assert(propertyName != null, "PropertyManager::GetSearchResultPropertyValue - propertyName is null");

            ResultPropertyValueCollection propertyValues = null;
            try
            {
                propertyValues = res.Properties[propertyName];
                if ((propertyValues == null) || (propertyValues.Count < 1))
                {
                    throw new ActiveDirectoryOperationException(SR.Format(SR.PropertyNotFound , propertyName));
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(e);
            }

            return propertyValues[0];
        }
    }
}
