// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.ActiveDirectory
{
    internal static class PropertyManager
    {
        public const string DefaultNamingContext = "defaultNamingContext";
        public const string SchemaNamingContext = "schemaNamingContext";
        public const string ConfigurationNamingContext = "configurationNamingContext";
        public const string RootDomainNamingContext = "rootDomainNamingContext";
        public const string MsDSBehaviorVersion = "msDS-Behavior-Version";
        public const string FsmoRoleOwner = "fsmoRoleOwner";
        public const string ForestFunctionality = "forestFunctionality";
        public const string NTMixedDomain = "ntMixedDomain";
        public const string DomainFunctionality = "domainFunctionality";
        public const string ObjectCategory = "objectCategory";
        public const string SystemFlags = "systemFlags";
        public const string DnsRoot = "dnsRoot";
        public const string DistinguishedName = "distinguishedName";
        public const string TrustParent = "trustParent";
        // disable csharp compiler warning #0414: field assigned unused value
#pragma warning disable 0414
        public const string FlatName = "flatName";
        public const string Name = "name";
        public const string Flags = "flags";
        public const string TrustType = "trustType";
        public const string TrustAttributes = "trustAttributes";
#pragma warning restore 0414
        public const string BecomeSchemaMaster = "becomeSchemaMaster";
        public const string BecomeDomainMaster = "becomeDomainMaster";
        public const string BecomePdc = "becomePdc";
        public const string BecomeRidMaster = "becomeRidMaster";
        public const string BecomeInfrastructureMaster = "becomeInfrastructureMaster";
        public const string DnsHostName = "dnsHostName";
        public const string Options = "options";
        public const string CurrentTime = "currentTime";
        public const string HighestCommittedUSN = "highestCommittedUSN";
        public const string OperatingSystem = "operatingSystem";
        public const string HasMasterNCs = "hasMasterNCs";
        public const string MsDSHasMasterNCs = "msDS-HasMasterNCs";
        public const string MsDSHasFullReplicaNCs = "msDS-hasFullReplicaNCs";
        public const string NCName = "nCName";
        public const string Cn = "cn";
        public const string NETBIOSName = "nETBIOSName";
        public const string DomainDNS = "domainDNS";
        public const string InstanceType = "instanceType";
        public const string MsDSSDReferenceDomain = "msDS-SDReferenceDomain";
        public const string MsDSPortLDAP = "msDS-PortLDAP";
        public const string MsDSPortSSL = "msDS-PortSSL";
        public const string MsDSNCReplicaLocations = "msDS-NC-Replica-Locations";
        public const string MsDSNCROReplicaLocations = "msDS-NC-RO-Replica-Locations";
        public const string SupportedCapabilities = "supportedCapabilities";
        public const string ServerName = "serverName";
        public const string Enabled = "Enabled";
        public const string ObjectGuid = "objectGuid";
        public const string Keywords = "keywords";
        public const string ServiceBindingInformation = "serviceBindingInformation";
        public const string MsDSReplAuthenticationMode = "msDS-ReplAuthenticationMode";
        public const string HasPartialReplicaNCs = "hasPartialReplicaNCs";
        public const string Container = "container";
        public const string LdapDisplayName = "ldapDisplayName";
        public const string AttributeID = "attributeID";
        public const string AttributeSyntax = "attributeSyntax";
        public const string Description = "description";
        public const string SearchFlags = "searchFlags";
        public const string OMSyntax = "oMSyntax";
        public const string OMObjectClass = "oMObjectClass";
        public const string IsSingleValued = "isSingleValued";
        public const string IsDefunct = "isDefunct";
        public const string RangeUpper = "rangeUpper";
        public const string RangeLower = "rangeLower";
        public const string IsMemberOfPartialAttributeSet = "isMemberOfPartialAttributeSet";
        public const string ObjectVersion = "objectVersion";
        public const string LinkID = "linkID";
        public const string ObjectClassCategory = "objectClassCategory";
        public const string SchemaUpdateNow = "schemaUpdateNow";
        public const string SubClassOf = "subClassOf";
        public const string SchemaIDGuid = "schemaIDGUID";
        public const string PossibleSuperiors = "possSuperiors";
        public const string PossibleInferiors = "possibleInferiors";
        public const string MustContain = "mustContain";
        public const string MayContain = "mayContain";
        public const string SystemMustContain = "systemMustContain";
        public const string SystemMayContain = "systemMayContain";
        public const string GovernsID = "governsID";
        public const string IsGlobalCatalogReady = "isGlobalCatalogReady";
        public const string NTSecurityDescriptor = "ntSecurityDescriptor";
        public const string DsServiceName = "dsServiceName";
        public const string ReplicateSingleObject = "replicateSingleObject";
        public const string MsDSMasteredBy = "msDS-masteredBy";
        public const string DefaultSecurityDescriptor = "defaultSecurityDescriptor";
        public const string NamingContexts = "namingContexts";
        public const string MsDSDefaultNamingContext = "msDS-DefaultNamingContext";
        public const string OperatingSystemVersion = "operatingSystemVersion";
        public const string AuxiliaryClass = "auxiliaryClass";
        public const string SystemAuxiliaryClass = "systemAuxiliaryClass";
        public const string SystemPossibleSuperiors = "systemPossSuperiors";
        public const string InterSiteTopologyGenerator = "interSiteTopologyGenerator";
        public const string FromServer = "fromServer";
        public const string RIDAvailablePool = "rIDAvailablePool";

        public const string SiteList = "siteList";
        public const string MsDSHasInstantiatedNCs = "msDS-HasInstantiatedNCs";

        public static object? GetPropertyValue(DirectoryEntry directoryEntry, string propertyName)
        {
            return GetPropertyValue(null, directoryEntry, propertyName);
        }

        public static object? GetPropertyValue(DirectoryContext? context, DirectoryEntry directoryEntry, string propertyName)
        {
            Debug.Assert(directoryEntry != null, "PropertyManager::GetPropertyValue - directoryEntry is null");

            Debug.Assert(propertyName != null, "PropertyManager::GetPropertyValue - propertyName is null");

            try
            {
                if (directoryEntry.Properties[propertyName].Count == 0)
                {
                    if (directoryEntry.Properties[PropertyManager.DistinguishedName].Count != 0)
                    {
                        throw new ActiveDirectoryOperationException(SR.Format(SR.PropertyNotFoundOnObject, propertyName, directoryEntry.Properties[PropertyManager.DistinguishedName].Value));
                    }
                    else
                    {
                        throw new ActiveDirectoryOperationException(SR.Format(SR.PropertyNotFound, propertyName));
                    }
                }
            }
            catch (COMException e)
            {
                throw ExceptionHelper.GetExceptionFromCOMException(context, e);
            }

            return directoryEntry.Properties[propertyName].Value;
        }

        public static object? GetSearchResultPropertyValue(SearchResult res, string propertyName)
        {
            Debug.Assert(res != null, "PropertyManager::GetSearchResultPropertyValue - res is null");

            Debug.Assert(propertyName != null, "PropertyManager::GetSearchResultPropertyValue - propertyName is null");

            ResultPropertyValueCollection? propertyValues = null;
            try
            {
                propertyValues = res.Properties[propertyName];
                if ((propertyValues == null) || (propertyValues.Count < 1))
                {
                    throw new ActiveDirectoryOperationException(SR.Format(SR.PropertyNotFound, propertyName));
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
