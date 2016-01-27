// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 6.00.0366 */
/* Compiler settings for isolation.idl:
    Oicf, W1, Zp8, env=Win32 (32b run)
    protocol : dce , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
//@@MIDL_FILE_HEADING(  )

#pragma warning( disable: 4049 )  /* more than 64k source lines */


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 475
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif // __RPCNDR_H_VERSION__

#ifndef COM_NO_WINDOWS_H
#include "windows.h"
#include "ole2.h"
#endif /*COM_NO_WINDOWS_H*/

#ifndef __isolation_h__
#define __isolation_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __ISectionEntry_FWD_DEFINED__
#define __ISectionEntry_FWD_DEFINED__
typedef interface ISectionEntry ISectionEntry;
#endif 	/* __ISectionEntry_FWD_DEFINED__ */


#ifndef __ISection_FWD_DEFINED__
#define __ISection_FWD_DEFINED__
typedef interface ISection ISection;
#endif 	/* __ISection_FWD_DEFINED__ */


#ifndef __ICDF_FWD_DEFINED__
#define __ICDF_FWD_DEFINED__
typedef interface ICDF ICDF;
#endif 	/* __ICDF_FWD_DEFINED__ */


#ifndef __ISectionWithStringKey_FWD_DEFINED__
#define __ISectionWithStringKey_FWD_DEFINED__
typedef interface ISectionWithStringKey ISectionWithStringKey;
#endif 	/* __ISectionWithStringKey_FWD_DEFINED__ */


#ifndef __ISectionWithBlobKey_FWD_DEFINED__
#define __ISectionWithBlobKey_FWD_DEFINED__
typedef interface ISectionWithBlobKey ISectionWithBlobKey;
#endif 	/* __ISectionWithBlobKey_FWD_DEFINED__ */


#ifndef __ISectionWithGuidKey_FWD_DEFINED__
#define __ISectionWithGuidKey_FWD_DEFINED__
typedef interface ISectionWithGuidKey ISectionWithGuidKey;
#endif 	/* __ISectionWithGuidKey_FWD_DEFINED__ */


#ifndef __ISectionWithIntegerKey_FWD_DEFINED__
#define __ISectionWithIntegerKey_FWD_DEFINED__
typedef interface ISectionWithIntegerKey ISectionWithIntegerKey;
#endif 	/* __ISectionWithIntegerKey_FWD_DEFINED__ */


#ifndef __ISectionWithDefinitionIdentityKey_FWD_DEFINED__
#define __ISectionWithDefinitionIdentityKey_FWD_DEFINED__
typedef interface ISectionWithDefinitionIdentityKey ISectionWithDefinitionIdentityKey;
#endif 	/* __ISectionWithDefinitionIdentityKey_FWD_DEFINED__ */


#ifndef __ISectionWithReferenceIdentityKey_FWD_DEFINED__
#define __ISectionWithReferenceIdentityKey_FWD_DEFINED__
typedef interface ISectionWithReferenceIdentityKey ISectionWithReferenceIdentityKey;
#endif 	/* __ISectionWithReferenceIdentityKey_FWD_DEFINED__ */


#ifndef __ICMS_FWD_DEFINED__
#define __ICMS_FWD_DEFINED__
typedef interface ICMS ICMS;
#endif 	/* __ICMS_FWD_DEFINED__ */


#ifndef __IMuiResourceIdLookupMapEntry_FWD_DEFINED__
#define __IMuiResourceIdLookupMapEntry_FWD_DEFINED__
typedef interface IMuiResourceIdLookupMapEntry IMuiResourceIdLookupMapEntry;
#endif 	/* __IMuiResourceIdLookupMapEntry_FWD_DEFINED__ */


#ifndef __IMuiResourceTypeIdStringEntry_FWD_DEFINED__
#define __IMuiResourceTypeIdStringEntry_FWD_DEFINED__
typedef interface IMuiResourceTypeIdStringEntry IMuiResourceTypeIdStringEntry;
#endif 	/* __IMuiResourceTypeIdStringEntry_FWD_DEFINED__ */


#ifndef __IMuiResourceTypeIdIntEntry_FWD_DEFINED__
#define __IMuiResourceTypeIdIntEntry_FWD_DEFINED__
typedef interface IMuiResourceTypeIdIntEntry IMuiResourceTypeIdIntEntry;
#endif 	/* __IMuiResourceTypeIdIntEntry_FWD_DEFINED__ */


#ifndef __IMuiResourceMapEntry_FWD_DEFINED__
#define __IMuiResourceMapEntry_FWD_DEFINED__
typedef interface IMuiResourceMapEntry IMuiResourceMapEntry;
#endif 	/* __IMuiResourceMapEntry_FWD_DEFINED__ */


#ifndef __IHashElementEntry_FWD_DEFINED__
#define __IHashElementEntry_FWD_DEFINED__
typedef interface IHashElementEntry IHashElementEntry;
#endif 	/* __IHashElementEntry_FWD_DEFINED__ */


#ifndef __IFileEntry_FWD_DEFINED__
#define __IFileEntry_FWD_DEFINED__
typedef interface IFileEntry IFileEntry;
#endif 	/* __IFileEntry_FWD_DEFINED__ */


#ifndef __IFileAssociationEntry_FWD_DEFINED__
#define __IFileAssociationEntry_FWD_DEFINED__
typedef interface IFileAssociationEntry IFileAssociationEntry;
#endif 	/* __IFileAssociationEntry_FWD_DEFINED__ */


#ifndef __ICategoryMembershipDataEntry_FWD_DEFINED__
#define __ICategoryMembershipDataEntry_FWD_DEFINED__
typedef interface ICategoryMembershipDataEntry ICategoryMembershipDataEntry;
#endif 	/* __ICategoryMembershipDataEntry_FWD_DEFINED__ */


#ifndef __ISubcategoryMembershipEntry_FWD_DEFINED__
#define __ISubcategoryMembershipEntry_FWD_DEFINED__
typedef interface ISubcategoryMembershipEntry ISubcategoryMembershipEntry;
#endif 	/* __ISubcategoryMembershipEntry_FWD_DEFINED__ */


#ifndef __ICategoryMembershipEntry_FWD_DEFINED__
#define __ICategoryMembershipEntry_FWD_DEFINED__
typedef interface ICategoryMembershipEntry ICategoryMembershipEntry;
#endif 	/* __ICategoryMembershipEntry_FWD_DEFINED__ */


#ifndef __ICOMServerEntry_FWD_DEFINED__
#define __ICOMServerEntry_FWD_DEFINED__
typedef interface ICOMServerEntry ICOMServerEntry;
#endif 	/* __ICOMServerEntry_FWD_DEFINED__ */


#ifndef __IProgIdRedirectionEntry_FWD_DEFINED__
#define __IProgIdRedirectionEntry_FWD_DEFINED__
typedef interface IProgIdRedirectionEntry IProgIdRedirectionEntry;
#endif 	/* __IProgIdRedirectionEntry_FWD_DEFINED__ */


#ifndef __ICLRSurrogateEntry_FWD_DEFINED__
#define __ICLRSurrogateEntry_FWD_DEFINED__
typedef interface ICLRSurrogateEntry ICLRSurrogateEntry;
#endif 	/* __ICLRSurrogateEntry_FWD_DEFINED__ */


#ifndef __IAssemblyReferenceDependentAssemblyEntry_FWD_DEFINED__
#define __IAssemblyReferenceDependentAssemblyEntry_FWD_DEFINED__
typedef interface IAssemblyReferenceDependentAssemblyEntry IAssemblyReferenceDependentAssemblyEntry;
#endif 	/* __IAssemblyReferenceDependentAssemblyEntry_FWD_DEFINED__ */


#ifndef __IAssemblyReferenceEntry_FWD_DEFINED__
#define __IAssemblyReferenceEntry_FWD_DEFINED__
typedef interface IAssemblyReferenceEntry IAssemblyReferenceEntry;
#endif 	/* __IAssemblyReferenceEntry_FWD_DEFINED__ */


#ifndef __IWindowClassEntry_FWD_DEFINED__
#define __IWindowClassEntry_FWD_DEFINED__
typedef interface IWindowClassEntry IWindowClassEntry;
#endif 	/* __IWindowClassEntry_FWD_DEFINED__ */


#ifndef __IResourceTableMappingEntry_FWD_DEFINED__
#define __IResourceTableMappingEntry_FWD_DEFINED__
typedef interface IResourceTableMappingEntry IResourceTableMappingEntry;
#endif 	/* __IResourceTableMappingEntry_FWD_DEFINED__ */


#ifndef __IEntryPointEntry_FWD_DEFINED__
#define __IEntryPointEntry_FWD_DEFINED__
typedef interface IEntryPointEntry IEntryPointEntry;
#endif 	/* __IEntryPointEntry_FWD_DEFINED__ */


#ifndef __IPermissionSetEntry_FWD_DEFINED__
#define __IPermissionSetEntry_FWD_DEFINED__
typedef interface IPermissionSetEntry IPermissionSetEntry;
#endif 	/* __IPermissionSetEntry_FWD_DEFINED__ */


#ifndef __IAssemblyRequestEntry_FWD_DEFINED__
#define __IAssemblyRequestEntry_FWD_DEFINED__
typedef interface IAssemblyRequestEntry IAssemblyRequestEntry;
#endif 	/* __IAssemblyRequestEntry_FWD_DEFINED__ */


#ifndef __IDescriptionMetadataEntry_FWD_DEFINED__
#define __IDescriptionMetadataEntry_FWD_DEFINED__
typedef interface IDescriptionMetadataEntry IDescriptionMetadataEntry;
#endif 	/* __IDescriptionMetadataEntry_FWD_DEFINED__ */


#ifndef __IDeploymentMetadataEntry_FWD_DEFINED__
#define __IDeploymentMetadataEntry_FWD_DEFINED__
typedef interface IDeploymentMetadataEntry IDeploymentMetadataEntry;
#endif 	/* __IDeploymentMetadataEntry_FWD_DEFINED__ */


#ifndef __IDependentOSMetadataEntry_FWD_DEFINED__
#define __IDependentOSMetadataEntry_FWD_DEFINED__
typedef interface IDependentOSMetadataEntry IDependentOSMetadataEntry;
#endif 	/* __IDependentOSMetadataEntry_FWD_DEFINED__ */


#ifndef __ICompatibleFrameworksMetadataEntry_FWD_DEFINED__
#define __ICompatibleFrameworksMetadataEntry_FWD_DEFINED__
typedef interface ICompatibleFrameworksMetadataEntry ICompatibleFrameworksMetadataEntry;
#endif 	/* __ICompatibleFrameworksMetadataEntry_FWD_DEFINED__ */


#ifndef __IMetadataSectionEntry_FWD_DEFINED__
#define __IMetadataSectionEntry_FWD_DEFINED__
typedef interface IMetadataSectionEntry IMetadataSectionEntry;
#endif 	/* __IMetadataSectionEntry_FWD_DEFINED__ */


#ifndef __IEventEntry_FWD_DEFINED__
#define __IEventEntry_FWD_DEFINED__
typedef interface IEventEntry IEventEntry;
#endif 	/* __IEventEntry_FWD_DEFINED__ */


#ifndef __IEventMapEntry_FWD_DEFINED__
#define __IEventMapEntry_FWD_DEFINED__
typedef interface IEventMapEntry IEventMapEntry;
#endif 	/* __IEventMapEntry_FWD_DEFINED__ */


#ifndef __IEventTagEntry_FWD_DEFINED__
#define __IEventTagEntry_FWD_DEFINED__
typedef interface IEventTagEntry IEventTagEntry;
#endif 	/* __IEventTagEntry_FWD_DEFINED__ */


#ifndef __IRegistryValueEntry_FWD_DEFINED__
#define __IRegistryValueEntry_FWD_DEFINED__
typedef interface IRegistryValueEntry IRegistryValueEntry;
#endif 	/* __IRegistryValueEntry_FWD_DEFINED__ */


#ifndef __IRegistryKeyEntry_FWD_DEFINED__
#define __IRegistryKeyEntry_FWD_DEFINED__
typedef interface IRegistryKeyEntry IRegistryKeyEntry;
#endif 	/* __IRegistryKeyEntry_FWD_DEFINED__ */


#ifndef __IDirectoryEntry_FWD_DEFINED__
#define __IDirectoryEntry_FWD_DEFINED__
typedef interface IDirectoryEntry IDirectoryEntry;
#endif 	/* __IDirectoryEntry_FWD_DEFINED__ */


#ifndef __ISecurityDescriptorReferenceEntry_FWD_DEFINED__
#define __ISecurityDescriptorReferenceEntry_FWD_DEFINED__
typedef interface ISecurityDescriptorReferenceEntry ISecurityDescriptorReferenceEntry;
#endif 	/* __ISecurityDescriptorReferenceEntry_FWD_DEFINED__ */


#ifndef __ICounterSetEntry_FWD_DEFINED__
#define __ICounterSetEntry_FWD_DEFINED__
typedef interface ICounterSetEntry ICounterSetEntry;
#endif 	/* __ICounterSetEntry_FWD_DEFINED__ */


#ifndef __ICounterEntry_FWD_DEFINED__
#define __ICounterEntry_FWD_DEFINED__
typedef interface ICounterEntry ICounterEntry;
#endif 	/* __ICounterEntry_FWD_DEFINED__ */


#ifndef __ICompatibleFrameworkEntry_FWD_DEFINED__
#define __ICompatibleFrameworkEntry_FWD_DEFINED__
typedef interface ICompatibleFrameworkEntry ICompatibleFrameworkEntry;
#endif 	/* __ICompatibleFrameworkEntry_FWD_DEFINED__ */


#ifndef __IACS_FWD_DEFINED__
#define __IACS_FWD_DEFINED__
typedef interface IACS IACS;
#endif 	/* __IACS_FWD_DEFINED__ */


#ifndef __IAppIdMetadataEntry_FWD_DEFINED__
#define __IAppIdMetadataEntry_FWD_DEFINED__
typedef interface IAppIdMetadataEntry IAppIdMetadataEntry;
#endif 	/* __IAppIdMetadataEntry_FWD_DEFINED__ */


#ifndef __IMemberComponentEntry_FWD_DEFINED__
#define __IMemberComponentEntry_FWD_DEFINED__
typedef interface IMemberComponentEntry IMemberComponentEntry;
#endif 	/* __IMemberComponentEntry_FWD_DEFINED__ */


#ifndef __IMemberLookupEntry_FWD_DEFINED__
#define __IMemberLookupEntry_FWD_DEFINED__
typedef interface IMemberLookupEntry IMemberLookupEntry;
#endif 	/* __IMemberLookupEntry_FWD_DEFINED__ */


#ifndef __IStoreCoherencyEntry_FWD_DEFINED__
#define __IStoreCoherencyEntry_FWD_DEFINED__
typedef interface IStoreCoherencyEntry IStoreCoherencyEntry;
#endif 	/* __IStoreCoherencyEntry_FWD_DEFINED__ */


#ifndef __IReferenceIdentity_FWD_DEFINED__
#define __IReferenceIdentity_FWD_DEFINED__
typedef interface IReferenceIdentity IReferenceIdentity;
#endif 	/* __IReferenceIdentity_FWD_DEFINED__ */


#ifndef __IDefinitionIdentity_FWD_DEFINED__
#define __IDefinitionIdentity_FWD_DEFINED__
typedef interface IDefinitionIdentity IDefinitionIdentity;
#endif 	/* __IDefinitionIdentity_FWD_DEFINED__ */


#ifndef __IEnumIDENTITY_ATTRIBUTE_FWD_DEFINED__
#define __IEnumIDENTITY_ATTRIBUTE_FWD_DEFINED__
typedef interface IEnumIDENTITY_ATTRIBUTE IEnumIDENTITY_ATTRIBUTE;
#endif 	/* __IEnumIDENTITY_ATTRIBUTE_FWD_DEFINED__ */


#ifndef __IEnumDefinitionIdentity_FWD_DEFINED__
#define __IEnumDefinitionIdentity_FWD_DEFINED__
typedef interface IEnumDefinitionIdentity IEnumDefinitionIdentity;
#endif 	/* __IEnumDefinitionIdentity_FWD_DEFINED__ */


#ifndef __IEnumReferenceIdentity_FWD_DEFINED__
#define __IEnumReferenceIdentity_FWD_DEFINED__
typedef interface IEnumReferenceIdentity IEnumReferenceIdentity;
#endif 	/* __IEnumReferenceIdentity_FWD_DEFINED__ */


#ifndef __IDefinitionAppId_FWD_DEFINED__
#define __IDefinitionAppId_FWD_DEFINED__
typedef interface IDefinitionAppId IDefinitionAppId;
#endif 	/* __IDefinitionAppId_FWD_DEFINED__ */


#ifndef __IReferenceAppId_FWD_DEFINED__
#define __IReferenceAppId_FWD_DEFINED__
typedef interface IReferenceAppId IReferenceAppId;
#endif 	/* __IReferenceAppId_FWD_DEFINED__ */


#ifndef __IIdentityAuthority_FWD_DEFINED__
#define __IIdentityAuthority_FWD_DEFINED__
typedef interface IIdentityAuthority IIdentityAuthority;
#endif 	/* __IIdentityAuthority_FWD_DEFINED__ */


#ifndef __IAppIdAuthority_FWD_DEFINED__
#define __IAppIdAuthority_FWD_DEFINED__
typedef interface IAppIdAuthority IAppIdAuthority;
#endif 	/* __IAppIdAuthority_FWD_DEFINED__ */


#ifndef __IEnumSTORE_CATEGORY_FWD_DEFINED__
#define __IEnumSTORE_CATEGORY_FWD_DEFINED__
typedef interface IEnumSTORE_CATEGORY IEnumSTORE_CATEGORY;
#endif 	/* __IEnumSTORE_CATEGORY_FWD_DEFINED__ */


#ifndef __IEnumSTORE_CATEGORY_SUBCATEGORY_FWD_DEFINED__
#define __IEnumSTORE_CATEGORY_SUBCATEGORY_FWD_DEFINED__
typedef interface IEnumSTORE_CATEGORY_SUBCATEGORY IEnumSTORE_CATEGORY_SUBCATEGORY;
#endif 	/* __IEnumSTORE_CATEGORY_SUBCATEGORY_FWD_DEFINED__ */


#ifndef __IEnumSTORE_CATEGORY_INSTANCE_FWD_DEFINED__
#define __IEnumSTORE_CATEGORY_INSTANCE_FWD_DEFINED__
typedef interface IEnumSTORE_CATEGORY_INSTANCE IEnumSTORE_CATEGORY_INSTANCE;
#endif 	/* __IEnumSTORE_CATEGORY_INSTANCE_FWD_DEFINED__ */


#ifndef __IStore_FWD_DEFINED__
#define __IStore_FWD_DEFINED__
typedef interface IStore IStore;
#endif 	/* __IStore_FWD_DEFINED__ */


#ifndef __IMigrateStore_FWD_DEFINED__
#define __IMigrateStore_FWD_DEFINED__
typedef interface IMigrateStore IMigrateStore;
#endif 	/* __IMigrateStore_FWD_DEFINED__ */


#ifndef __IEnumSTORE_DEPLOYMENT_METADATA_FWD_DEFINED__
#define __IEnumSTORE_DEPLOYMENT_METADATA_FWD_DEFINED__
typedef interface IEnumSTORE_DEPLOYMENT_METADATA IEnumSTORE_DEPLOYMENT_METADATA;
#endif 	/* __IEnumSTORE_DEPLOYMENT_METADATA_FWD_DEFINED__ */


#ifndef __IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_FWD_DEFINED__
#define __IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_FWD_DEFINED__
typedef interface IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY;
#endif 	/* __IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_FWD_DEFINED__ */


#ifndef __IEnumSTORE_ASSEMBLY_FWD_DEFINED__
#define __IEnumSTORE_ASSEMBLY_FWD_DEFINED__
typedef interface IEnumSTORE_ASSEMBLY IEnumSTORE_ASSEMBLY;
#endif 	/* __IEnumSTORE_ASSEMBLY_FWD_DEFINED__ */


#ifndef __IEnumSTORE_ASSEMBLY_FILE_FWD_DEFINED__
#define __IEnumSTORE_ASSEMBLY_FILE_FWD_DEFINED__
typedef interface IEnumSTORE_ASSEMBLY_FILE IEnumSTORE_ASSEMBLY_FILE;
#endif 	/* __IEnumSTORE_ASSEMBLY_FILE_FWD_DEFINED__ */


#ifndef __IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_FWD_DEFINED__
#define __IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_FWD_DEFINED__
typedef interface IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE;
#endif 	/* __IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_FWD_DEFINED__ */


#ifndef __IEnumCATEGORY_FWD_DEFINED__
#define __IEnumCATEGORY_FWD_DEFINED__
typedef interface IEnumCATEGORY IEnumCATEGORY;
#endif 	/* __IEnumCATEGORY_FWD_DEFINED__ */


#ifndef __IEnumCATEGORY_SUBCATEGORY_FWD_DEFINED__
#define __IEnumCATEGORY_SUBCATEGORY_FWD_DEFINED__
typedef interface IEnumCATEGORY_SUBCATEGORY IEnumCATEGORY_SUBCATEGORY;
#endif 	/* __IEnumCATEGORY_SUBCATEGORY_FWD_DEFINED__ */


#ifndef __IEnumCATEGORY_INSTANCE_FWD_DEFINED__
#define __IEnumCATEGORY_INSTANCE_FWD_DEFINED__
typedef interface IEnumCATEGORY_INSTANCE IEnumCATEGORY_INSTANCE;
#endif 	/* __IEnumCATEGORY_INSTANCE_FWD_DEFINED__ */


#ifndef __IManifestInformation_FWD_DEFINED__
#define __IManifestInformation_FWD_DEFINED__
typedef interface IManifestInformation IManifestInformation;
#endif 	/* __IManifestInformation_FWD_DEFINED__ */


#ifndef __IActContext_FWD_DEFINED__
#define __IActContext_FWD_DEFINED__
typedef interface IActContext IActContext;
#endif 	/* __IActContext_FWD_DEFINED__ */


#ifndef __IStateManager_FWD_DEFINED__
#define __IStateManager_FWD_DEFINED__
typedef interface IStateManager IStateManager;
#endif 	/* __IStateManager_FWD_DEFINED__ */


#ifndef __IManifestParseErrorCallback_FWD_DEFINED__
#define __IManifestParseErrorCallback_FWD_DEFINED__
typedef interface IManifestParseErrorCallback IManifestParseErrorCallback;
#endif 	/* __IManifestParseErrorCallback_FWD_DEFINED__ */


#ifndef __IMuiResourceIdLookupMapEntry_FWD_DEFINED__
#define __IMuiResourceIdLookupMapEntry_FWD_DEFINED__
typedef interface IMuiResourceIdLookupMapEntry IMuiResourceIdLookupMapEntry;
#endif 	/* __IMuiResourceIdLookupMapEntry_FWD_DEFINED__ */


#ifndef __IMuiResourceTypeIdStringEntry_FWD_DEFINED__
#define __IMuiResourceTypeIdStringEntry_FWD_DEFINED__
typedef interface IMuiResourceTypeIdStringEntry IMuiResourceTypeIdStringEntry;
#endif 	/* __IMuiResourceTypeIdStringEntry_FWD_DEFINED__ */


#ifndef __IMuiResourceTypeIdIntEntry_FWD_DEFINED__
#define __IMuiResourceTypeIdIntEntry_FWD_DEFINED__
typedef interface IMuiResourceTypeIdIntEntry IMuiResourceTypeIdIntEntry;
#endif 	/* __IMuiResourceTypeIdIntEntry_FWD_DEFINED__ */


#ifndef __IMuiResourceMapEntry_FWD_DEFINED__
#define __IMuiResourceMapEntry_FWD_DEFINED__
typedef interface IMuiResourceMapEntry IMuiResourceMapEntry;
#endif 	/* __IMuiResourceMapEntry_FWD_DEFINED__ */


#ifndef __IHashElementEntry_FWD_DEFINED__
#define __IHashElementEntry_FWD_DEFINED__
typedef interface IHashElementEntry IHashElementEntry;
#endif 	/* __IHashElementEntry_FWD_DEFINED__ */


#ifndef __IFileEntry_FWD_DEFINED__
#define __IFileEntry_FWD_DEFINED__
typedef interface IFileEntry IFileEntry;
#endif 	/* __IFileEntry_FWD_DEFINED__ */


#ifndef __IFileAssociationEntry_FWD_DEFINED__
#define __IFileAssociationEntry_FWD_DEFINED__
typedef interface IFileAssociationEntry IFileAssociationEntry;
#endif 	/* __IFileAssociationEntry_FWD_DEFINED__ */


#ifndef __ICategoryMembershipDataEntry_FWD_DEFINED__
#define __ICategoryMembershipDataEntry_FWD_DEFINED__
typedef interface ICategoryMembershipDataEntry ICategoryMembershipDataEntry;
#endif 	/* __ICategoryMembershipDataEntry_FWD_DEFINED__ */


#ifndef __ISubcategoryMembershipEntry_FWD_DEFINED__
#define __ISubcategoryMembershipEntry_FWD_DEFINED__
typedef interface ISubcategoryMembershipEntry ISubcategoryMembershipEntry;
#endif 	/* __ISubcategoryMembershipEntry_FWD_DEFINED__ */


#ifndef __ICategoryMembershipEntry_FWD_DEFINED__
#define __ICategoryMembershipEntry_FWD_DEFINED__
typedef interface ICategoryMembershipEntry ICategoryMembershipEntry;
#endif 	/* __ICategoryMembershipEntry_FWD_DEFINED__ */


#ifndef __ICOMServerEntry_FWD_DEFINED__
#define __ICOMServerEntry_FWD_DEFINED__
typedef interface ICOMServerEntry ICOMServerEntry;
#endif 	/* __ICOMServerEntry_FWD_DEFINED__ */


#ifndef __IProgIdRedirectionEntry_FWD_DEFINED__
#define __IProgIdRedirectionEntry_FWD_DEFINED__
typedef interface IProgIdRedirectionEntry IProgIdRedirectionEntry;
#endif 	/* __IProgIdRedirectionEntry_FWD_DEFINED__ */


#ifndef __ICLRSurrogateEntry_FWD_DEFINED__
#define __ICLRSurrogateEntry_FWD_DEFINED__
typedef interface ICLRSurrogateEntry ICLRSurrogateEntry;
#endif 	/* __ICLRSurrogateEntry_FWD_DEFINED__ */


#ifndef __IAssemblyReferenceDependentAssemblyEntry_FWD_DEFINED__
#define __IAssemblyReferenceDependentAssemblyEntry_FWD_DEFINED__
typedef interface IAssemblyReferenceDependentAssemblyEntry IAssemblyReferenceDependentAssemblyEntry;
#endif 	/* __IAssemblyReferenceDependentAssemblyEntry_FWD_DEFINED__ */


#ifndef __IAssemblyReferenceEntry_FWD_DEFINED__
#define __IAssemblyReferenceEntry_FWD_DEFINED__
typedef interface IAssemblyReferenceEntry IAssemblyReferenceEntry;
#endif 	/* __IAssemblyReferenceEntry_FWD_DEFINED__ */


#ifndef __IWindowClassEntry_FWD_DEFINED__
#define __IWindowClassEntry_FWD_DEFINED__
typedef interface IWindowClassEntry IWindowClassEntry;
#endif 	/* __IWindowClassEntry_FWD_DEFINED__ */


#ifndef __IResourceTableMappingEntry_FWD_DEFINED__
#define __IResourceTableMappingEntry_FWD_DEFINED__
typedef interface IResourceTableMappingEntry IResourceTableMappingEntry;
#endif 	/* __IResourceTableMappingEntry_FWD_DEFINED__ */


#ifndef __IEntryPointEntry_FWD_DEFINED__
#define __IEntryPointEntry_FWD_DEFINED__
typedef interface IEntryPointEntry IEntryPointEntry;
#endif 	/* __IEntryPointEntry_FWD_DEFINED__ */


#ifndef __IPermissionSetEntry_FWD_DEFINED__
#define __IPermissionSetEntry_FWD_DEFINED__
typedef interface IPermissionSetEntry IPermissionSetEntry;
#endif 	/* __IPermissionSetEntry_FWD_DEFINED__ */


#ifndef __IAssemblyRequestEntry_FWD_DEFINED__
#define __IAssemblyRequestEntry_FWD_DEFINED__
typedef interface IAssemblyRequestEntry IAssemblyRequestEntry;
#endif 	/* __IAssemblyRequestEntry_FWD_DEFINED__ */


#ifndef __IDescriptionMetadataEntry_FWD_DEFINED__
#define __IDescriptionMetadataEntry_FWD_DEFINED__
typedef interface IDescriptionMetadataEntry IDescriptionMetadataEntry;
#endif 	/* __IDescriptionMetadataEntry_FWD_DEFINED__ */


#ifndef __IDeploymentMetadataEntry_FWD_DEFINED__
#define __IDeploymentMetadataEntry_FWD_DEFINED__
typedef interface IDeploymentMetadataEntry IDeploymentMetadataEntry;
#endif 	/* __IDeploymentMetadataEntry_FWD_DEFINED__ */


#ifndef __IDependentOSMetadataEntry_FWD_DEFINED__
#define __IDependentOSMetadataEntry_FWD_DEFINED__
typedef interface IDependentOSMetadataEntry IDependentOSMetadataEntry;
#endif 	/* __IDependentOSMetadataEntry_FWD_DEFINED__ */


#ifndef __ICompatibleFrameworksMetadataEntry_FWD_DEFINED__
#define __ICompatibleFrameworksMetadataEntry_FWD_DEFINED__
typedef interface ICompatibleFrameworksMetadataEntry ICompatibleFrameworksMetadataEntry;
#endif 	/* __ICompatibleFrameworksMetadataEntry_FWD_DEFINED__ */


#ifndef __IMetadataSectionEntry_FWD_DEFINED__
#define __IMetadataSectionEntry_FWD_DEFINED__
typedef interface IMetadataSectionEntry IMetadataSectionEntry;
#endif 	/* __IMetadataSectionEntry_FWD_DEFINED__ */


#ifndef __IEventEntry_FWD_DEFINED__
#define __IEventEntry_FWD_DEFINED__
typedef interface IEventEntry IEventEntry;
#endif 	/* __IEventEntry_FWD_DEFINED__ */


#ifndef __IEventMapEntry_FWD_DEFINED__
#define __IEventMapEntry_FWD_DEFINED__
typedef interface IEventMapEntry IEventMapEntry;
#endif 	/* __IEventMapEntry_FWD_DEFINED__ */


#ifndef __IEventTagEntry_FWD_DEFINED__
#define __IEventTagEntry_FWD_DEFINED__
typedef interface IEventTagEntry IEventTagEntry;
#endif 	/* __IEventTagEntry_FWD_DEFINED__ */


#ifndef __IRegistryValueEntry_FWD_DEFINED__
#define __IRegistryValueEntry_FWD_DEFINED__
typedef interface IRegistryValueEntry IRegistryValueEntry;
#endif 	/* __IRegistryValueEntry_FWD_DEFINED__ */


#ifndef __IRegistryKeyEntry_FWD_DEFINED__
#define __IRegistryKeyEntry_FWD_DEFINED__
typedef interface IRegistryKeyEntry IRegistryKeyEntry;
#endif 	/* __IRegistryKeyEntry_FWD_DEFINED__ */


#ifndef __IDirectoryEntry_FWD_DEFINED__
#define __IDirectoryEntry_FWD_DEFINED__
typedef interface IDirectoryEntry IDirectoryEntry;
#endif 	/* __IDirectoryEntry_FWD_DEFINED__ */


#ifndef __ISecurityDescriptorReferenceEntry_FWD_DEFINED__
#define __ISecurityDescriptorReferenceEntry_FWD_DEFINED__
typedef interface ISecurityDescriptorReferenceEntry ISecurityDescriptorReferenceEntry;
#endif 	/* __ISecurityDescriptorReferenceEntry_FWD_DEFINED__ */


#ifndef __ICounterSetEntry_FWD_DEFINED__
#define __ICounterSetEntry_FWD_DEFINED__
typedef interface ICounterSetEntry ICounterSetEntry;
#endif 	/* __ICounterSetEntry_FWD_DEFINED__ */


#ifndef __ICounterEntry_FWD_DEFINED__
#define __ICounterEntry_FWD_DEFINED__
typedef interface ICounterEntry ICounterEntry;
#endif 	/* __ICounterEntry_FWD_DEFINED__ */


#ifndef __ICompatibleFrameworkEntry_FWD_DEFINED__
#define __ICompatibleFrameworkEntry_FWD_DEFINED__
typedef interface ICompatibleFrameworkEntry ICompatibleFrameworkEntry;
#endif 	/* __ICompatibleFrameworkEntry_FWD_DEFINED__ */


#ifndef __ICDF_FWD_DEFINED__
#define __ICDF_FWD_DEFINED__
typedef interface ICDF ICDF;
#endif 	/* __ICDF_FWD_DEFINED__ */


#ifndef __ISectionEntry_FWD_DEFINED__
#define __ISectionEntry_FWD_DEFINED__
typedef interface ISectionEntry ISectionEntry;
#endif 	/* __ISectionEntry_FWD_DEFINED__ */


#ifndef __ISection_FWD_DEFINED__
#define __ISection_FWD_DEFINED__
typedef interface ISection ISection;
#endif 	/* __ISection_FWD_DEFINED__ */


#ifndef __ISectionWithStringKey_FWD_DEFINED__
#define __ISectionWithStringKey_FWD_DEFINED__
typedef interface ISectionWithStringKey ISectionWithStringKey;
#endif 	/* __ISectionWithStringKey_FWD_DEFINED__ */


#ifndef __ISectionWithIntegerKey_FWD_DEFINED__
#define __ISectionWithIntegerKey_FWD_DEFINED__
typedef interface ISectionWithIntegerKey ISectionWithIntegerKey;
#endif 	/* __ISectionWithIntegerKey_FWD_DEFINED__ */


#ifndef __ISectionWithGuidKey_FWD_DEFINED__
#define __ISectionWithGuidKey_FWD_DEFINED__
typedef interface ISectionWithGuidKey ISectionWithGuidKey;
#endif 	/* __ISectionWithGuidKey_FWD_DEFINED__ */


#ifndef __ISectionWithBlobKey_FWD_DEFINED__
#define __ISectionWithBlobKey_FWD_DEFINED__
typedef interface ISectionWithBlobKey ISectionWithBlobKey;
#endif 	/* __ISectionWithBlobKey_FWD_DEFINED__ */


#ifndef __ISectionWithReferenceIdentityKey_FWD_DEFINED__
#define __ISectionWithReferenceIdentityKey_FWD_DEFINED__
typedef interface ISectionWithReferenceIdentityKey ISectionWithReferenceIdentityKey;
#endif 	/* __ISectionWithReferenceIdentityKey_FWD_DEFINED__ */


#ifndef __ISectionWithDefinitionIdentityKey_FWD_DEFINED__
#define __ISectionWithDefinitionIdentityKey_FWD_DEFINED__
typedef interface ISectionWithDefinitionIdentityKey ISectionWithDefinitionIdentityKey;
#endif 	/* __ISectionWithDefinitionIdentityKey_FWD_DEFINED__ */


#ifndef __IIdentityAuthority_FWD_DEFINED__
#define __IIdentityAuthority_FWD_DEFINED__
typedef interface IIdentityAuthority IIdentityAuthority;
#endif 	/* __IIdentityAuthority_FWD_DEFINED__ */


#ifndef __IAppIdAuthority_FWD_DEFINED__
#define __IAppIdAuthority_FWD_DEFINED__
typedef interface IAppIdAuthority IAppIdAuthority;
#endif 	/* __IAppIdAuthority_FWD_DEFINED__ */


#ifndef __IDefinitionIdentity_FWD_DEFINED__
#define __IDefinitionIdentity_FWD_DEFINED__
typedef interface IDefinitionIdentity IDefinitionIdentity;
#endif 	/* __IDefinitionIdentity_FWD_DEFINED__ */


#ifndef __IReferenceIdentity_FWD_DEFINED__
#define __IReferenceIdentity_FWD_DEFINED__
typedef interface IReferenceIdentity IReferenceIdentity;
#endif 	/* __IReferenceIdentity_FWD_DEFINED__ */


#ifndef __IDefinitionAppId_FWD_DEFINED__
#define __IDefinitionAppId_FWD_DEFINED__
typedef interface IDefinitionAppId IDefinitionAppId;
#endif 	/* __IDefinitionAppId_FWD_DEFINED__ */


#ifndef __IReferenceAppId_FWD_DEFINED__
#define __IReferenceAppId_FWD_DEFINED__
typedef interface IReferenceAppId IReferenceAppId;
#endif 	/* __IReferenceAppId_FWD_DEFINED__ */


#ifndef __IActContext_FWD_DEFINED__
#define __IActContext_FWD_DEFINED__
typedef interface IActContext IActContext;
#endif 	/* __IActContext_FWD_DEFINED__ */


#ifndef __IManifestParseErrorCallback_FWD_DEFINED__
#define __IManifestParseErrorCallback_FWD_DEFINED__
typedef interface IManifestParseErrorCallback IManifestParseErrorCallback;
#endif 	/* __IManifestParseErrorCallback_FWD_DEFINED__ */


#ifndef __IStore_FWD_DEFINED__
#define __IStore_FWD_DEFINED__
typedef interface IStore IStore;
#endif 	/* __IStore_FWD_DEFINED__ */


#ifndef __IMigrateStore_FWD_DEFINED__
#define __IMigrateStore_FWD_DEFINED__
typedef interface IMigrateStore IMigrateStore;
#endif 	/* __IMigrateStore_FWD_DEFINED__ */


#ifndef __IEnumIDENTITY_ATTRIBUTE_FWD_DEFINED__
#define __IEnumIDENTITY_ATTRIBUTE_FWD_DEFINED__
typedef interface IEnumIDENTITY_ATTRIBUTE IEnumIDENTITY_ATTRIBUTE;
#endif 	/* __IEnumIDENTITY_ATTRIBUTE_FWD_DEFINED__ */


#ifndef __IEnumSTORE_CATEGORY_FWD_DEFINED__
#define __IEnumSTORE_CATEGORY_FWD_DEFINED__
typedef interface IEnumSTORE_CATEGORY IEnumSTORE_CATEGORY;
#endif 	/* __IEnumSTORE_CATEGORY_FWD_DEFINED__ */


#ifndef __IEnumSTORE_CATEGORY_INSTANCE_FWD_DEFINED__
#define __IEnumSTORE_CATEGORY_INSTANCE_FWD_DEFINED__
typedef interface IEnumSTORE_CATEGORY_INSTANCE IEnumSTORE_CATEGORY_INSTANCE;
#endif 	/* __IEnumSTORE_CATEGORY_INSTANCE_FWD_DEFINED__ */


#ifndef __IEnumSTORE_CATEGORY_SUBCATEGORY_FWD_DEFINED__
#define __IEnumSTORE_CATEGORY_SUBCATEGORY_FWD_DEFINED__
typedef interface IEnumSTORE_CATEGORY_SUBCATEGORY IEnumSTORE_CATEGORY_SUBCATEGORY;
#endif 	/* __IEnumSTORE_CATEGORY_SUBCATEGORY_FWD_DEFINED__ */


#ifndef __IEnumSTORE_ASSEMBLY_FWD_DEFINED__
#define __IEnumSTORE_ASSEMBLY_FWD_DEFINED__
typedef interface IEnumSTORE_ASSEMBLY IEnumSTORE_ASSEMBLY;
#endif 	/* __IEnumSTORE_ASSEMBLY_FWD_DEFINED__ */


#ifndef __IEnumSTORE_ASSEMBLY_FILE_FWD_DEFINED__
#define __IEnumSTORE_ASSEMBLY_FILE_FWD_DEFINED__
typedef interface IEnumSTORE_ASSEMBLY_FILE IEnumSTORE_ASSEMBLY_FILE;
#endif 	/* __IEnumSTORE_ASSEMBLY_FILE_FWD_DEFINED__ */


#ifndef __IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_FWD_DEFINED__
#define __IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_FWD_DEFINED__
typedef interface IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE;
#endif 	/* __IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_FWD_DEFINED__ */


#ifndef __IEnumDefinitionIdentity_FWD_DEFINED__
#define __IEnumDefinitionIdentity_FWD_DEFINED__
typedef interface IEnumDefinitionIdentity IEnumDefinitionIdentity;
#endif 	/* __IEnumDefinitionIdentity_FWD_DEFINED__ */


#ifndef __IEnumReferenceIdentity_FWD_DEFINED__
#define __IEnumReferenceIdentity_FWD_DEFINED__
typedef interface IEnumReferenceIdentity IEnumReferenceIdentity;
#endif 	/* __IEnumReferenceIdentity_FWD_DEFINED__ */


/* header files for imported files */
#include "unknwn.h"
#include "oaidl.h"
#include "ocidl.h"
#include "propidl.h"

#ifdef __cplusplus
extern "C"{
#endif 

void * __RPC_USER MIDL_user_allocate(size_t);
void __RPC_USER MIDL_user_free( void * ); 

/* interface __MIDL_itf_isolation_0000 */
/* [local] */ 




























// {3B6DEF2E-5BB3-487f-B6C3-E888FF42A337}
DEFINE_GUID(
     SXS_INSTALL_REFERENCE_SCHEME_CSUTIL, 
     0x3b6def2e, 
     0x5bb3, 
     0x487f, 
     0xb6, 0xc3, 0xe8, 0x88, 0xff, 0x42, 0xa3, 0x37);

// {8cedc215-ac4b-488b-93c0-a50a49cb2fb8}
DEFINE_GUID(
    SXS_INSTALL_REFERENCE_SCHEME_UNINSTALLKEY, 
    0x8cedc215, 
    0xac4b, 
    0x488b, 
    0x93, 0xc0, 0xa5, 0x0a, 0x49, 0xcb, 0x2f, 0xb8);

// {b02f9d65-fb77-4f7a-afa5-b391309f11c9}
DEFINE_GUID(
    SXS_INSTALL_REFERENCE_SCHEME_KEYFILE, 
    0xb02f9d65, 
    0xfb77, 
    0x4f7a, 
    0xaf, 0xa5, 0xb3, 0x91, 0x30, 0x9f, 0x11, 0xc9);

// {2ec93463-b0c3-45e1-8364-327e96aea856}
DEFINE_GUID(
    SXS_INSTALL_REFERENCE_SCHEME_OPAQUESTRING, 
    0x2ec93463, 
    0xb0c3, 
    0x45e1, 
    0x83, 0x64, 0x32, 0x7e, 0x96, 0xae, 0xa8, 0x56);

// d16d444c-56d8-11d5-882d-0080c847b195
DEFINE_GUID(
    SXS_INSTALL_REFERENCE_SCHEME_OSINSTALL,
    0xd16d444c,
    0x56d8,
    0x11d5,
    0x88, 0x2d, 0x00, 0x80, 0xc8, 0x47, 0xb1, 0x95);

//
// Guid for the -installed by sxsinstallassemblyw, who knows?-
// 27dec61e-b43c-4ac8-88db-e209a8242d90
//
DEFINE_GUID(
    SXS_INSTALL_REFERENCE_SCHEME_SXS_INSTALL_ASSEMBLY,
    0x27dec61e,
    0xb43c,
    0x4ac8,
    0x88, 0xdb, 0xe2, 0x09, 0xa8, 0x24, 0x2d, 0x90);

typedef struct _CULTURE_FALLBACK_LIST
    {
    SIZE_T nCultures;
    /* [size_is] */ const LPCWSTR *prgpszCultures;
    } 	CULTURE_FALLBACK_LIST;

typedef struct _CULTURE_FALLBACK_LIST *PCULTURE_FALLBACK_LIST;

typedef const CULTURE_FALLBACK_LIST *PCCULTURE_FALLBACK_LIST;

typedef union _COMPONENT_VERSION
    {
    ULONGLONG Version64;
    struct __MIDL___MIDL_itf_isolation_0000_0001
        {
        ULONG BuildAndRevision;
        ULONG MajorAndMinor;
        } 	Version32;
    struct __MIDL___MIDL_itf_isolation_0000_0002
        {
        USHORT Revision;
        USHORT Build;
        USHORT Minor;
        USHORT Major;
        } 	Version16;
    } 	COMPONENT_VERSION;

typedef union _COMPONENT_VERSION *PCOMPONENT_VERSION;

typedef const COMPONENT_VERSION *PCCOMPONENT_VERSION;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0000_v0_0_s_ifspec;

#ifndef __ISectionEntry_INTERFACE_DEFINED__
#define __ISectionEntry_INTERFACE_DEFINED__

/* interface ISectionEntry */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISectionEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("285a8861-c84a-11d7-850f-005cd062464f")
    ISectionEntry : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetField( 
            /* [in] */ ULONG fieldId,
            /* [retval][out] */ PROPVARIANT *fieldValue) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFieldName( 
            /* [in] */ ULONG fieldId,
            /* [retval][out] */ LPWSTR *pszFieldName) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ISectionEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISectionEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISectionEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISectionEntry * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetField )( 
            ISectionEntry * This,
            /* [in] */ ULONG fieldId,
            /* [retval][out] */ PROPVARIANT *fieldValue);
        
        HRESULT ( STDMETHODCALLTYPE *GetFieldName )( 
            ISectionEntry * This,
            /* [in] */ ULONG fieldId,
            /* [retval][out] */ LPWSTR *pszFieldName);
        
        END_INTERFACE
    } ISectionEntryVtbl;

    interface ISectionEntry
    {
        CONST_VTBL struct ISectionEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISectionEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ISectionEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ISectionEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ISectionEntry_GetField(This,fieldId,fieldValue)	\
    (This)->lpVtbl -> GetField(This,fieldId,fieldValue)

#define ISectionEntry_GetFieldName(This,fieldId,pszFieldName)	\
    (This)->lpVtbl -> GetFieldName(This,fieldId,pszFieldName)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ISectionEntry_GetField_Proxy( 
    ISectionEntry * This,
    /* [in] */ ULONG fieldId,
    /* [retval][out] */ PROPVARIANT *fieldValue);


void __RPC_STUB ISectionEntry_GetField_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISectionEntry_GetFieldName_Proxy( 
    ISectionEntry * This,
    /* [in] */ ULONG fieldId,
    /* [retval][out] */ LPWSTR *pszFieldName);


void __RPC_STUB ISectionEntry_GetFieldName_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ISectionEntry_INTERFACE_DEFINED__ */


#ifndef __ISection_INTERFACE_DEFINED__
#define __ISection_INTERFACE_DEFINED__

/* interface ISection */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISection;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("285a8862-c84a-11d7-850f-005cd062464f")
    ISection : public IUnknown
    {
    public:
        virtual /* [restricted][propget] */ HRESULT STDMETHODCALLTYPE get__NewEnum( 
            /* [retval][out] */ IUnknown **ppunkSectionEntryEnum) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Count( 
            /* [retval][out] */ ULONG *pdwSectionEntryCount) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SectionID( 
            /* [retval][out] */ ULONG *pSectionId) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SectionName( 
            /* [retval][out] */ LPWSTR *pszSectionName) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ISectionVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISection * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISection * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISection * This);
        
        /* [restricted][propget] */ HRESULT ( STDMETHODCALLTYPE *get__NewEnum )( 
            ISection * This,
            /* [retval][out] */ IUnknown **ppunkSectionEntryEnum);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Count )( 
            ISection * This,
            /* [retval][out] */ ULONG *pdwSectionEntryCount);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SectionID )( 
            ISection * This,
            /* [retval][out] */ ULONG *pSectionId);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SectionName )( 
            ISection * This,
            /* [retval][out] */ LPWSTR *pszSectionName);
        
        END_INTERFACE
    } ISectionVtbl;

    interface ISection
    {
        CONST_VTBL struct ISectionVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISection_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ISection_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ISection_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ISection_get__NewEnum(This,ppunkSectionEntryEnum)	\
    (This)->lpVtbl -> get__NewEnum(This,ppunkSectionEntryEnum)

#define ISection_get_Count(This,pdwSectionEntryCount)	\
    (This)->lpVtbl -> get_Count(This,pdwSectionEntryCount)

#define ISection_get_SectionID(This,pSectionId)	\
    (This)->lpVtbl -> get_SectionID(This,pSectionId)

#define ISection_get_SectionName(This,pszSectionName)	\
    (This)->lpVtbl -> get_SectionName(This,pszSectionName)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [restricted][propget] */ HRESULT STDMETHODCALLTYPE ISection_get__NewEnum_Proxy( 
    ISection * This,
    /* [retval][out] */ IUnknown **ppunkSectionEntryEnum);


void __RPC_STUB ISection_get__NewEnum_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISection_get_Count_Proxy( 
    ISection * This,
    /* [retval][out] */ ULONG *pdwSectionEntryCount);


void __RPC_STUB ISection_get_Count_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISection_get_SectionID_Proxy( 
    ISection * This,
    /* [retval][out] */ ULONG *pSectionId);


void __RPC_STUB ISection_get_SectionID_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISection_get_SectionName_Proxy( 
    ISection * This,
    /* [retval][out] */ LPWSTR *pszSectionName);


void __RPC_STUB ISection_get_SectionName_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ISection_INTERFACE_DEFINED__ */


#ifndef __ICDF_INTERFACE_DEFINED__
#define __ICDF_INTERFACE_DEFINED__

/* interface ICDF */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ICDF;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("285a8860-c84a-11d7-850f-005cd062464f")
    ICDF : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetRootSection( 
            /* [in] */ ULONG SectionID,
            /* [out] */ ISection **ppSection) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRootSectionEntry( 
            /* [in] */ ULONG SectionID,
            /* [out] */ ISectionEntry **ppSectionEntry) = 0;
        
        virtual /* [restricted][propget] */ HRESULT STDMETHODCALLTYPE get__NewEnum( 
            /* [retval][out] */ IUnknown **ppunkEnum) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Count( 
            /* [retval][out] */ ULONG *pdwCount) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Item( 
            /* [in] */ ULONG SectionID,
            /* [retval][out] */ IUnknown **ppUnknown) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ICDFVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICDF * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICDF * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICDF * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetRootSection )( 
            ICDF * This,
            /* [in] */ ULONG SectionID,
            /* [out] */ ISection **ppSection);
        
        HRESULT ( STDMETHODCALLTYPE *GetRootSectionEntry )( 
            ICDF * This,
            /* [in] */ ULONG SectionID,
            /* [out] */ ISectionEntry **ppSectionEntry);
        
        /* [restricted][propget] */ HRESULT ( STDMETHODCALLTYPE *get__NewEnum )( 
            ICDF * This,
            /* [retval][out] */ IUnknown **ppunkEnum);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Count )( 
            ICDF * This,
            /* [retval][out] */ ULONG *pdwCount);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Item )( 
            ICDF * This,
            /* [in] */ ULONG SectionID,
            /* [retval][out] */ IUnknown **ppUnknown);
        
        END_INTERFACE
    } ICDFVtbl;

    interface ICDF
    {
        CONST_VTBL struct ICDFVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICDF_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ICDF_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ICDF_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ICDF_GetRootSection(This,SectionID,ppSection)	\
    (This)->lpVtbl -> GetRootSection(This,SectionID,ppSection)

#define ICDF_GetRootSectionEntry(This,SectionID,ppSectionEntry)	\
    (This)->lpVtbl -> GetRootSectionEntry(This,SectionID,ppSectionEntry)

#define ICDF_get__NewEnum(This,ppunkEnum)	\
    (This)->lpVtbl -> get__NewEnum(This,ppunkEnum)

#define ICDF_get_Count(This,pdwCount)	\
    (This)->lpVtbl -> get_Count(This,pdwCount)

#define ICDF_get_Item(This,SectionID,ppUnknown)	\
    (This)->lpVtbl -> get_Item(This,SectionID,ppUnknown)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ICDF_GetRootSection_Proxy( 
    ICDF * This,
    /* [in] */ ULONG SectionID,
    /* [out] */ ISection **ppSection);


void __RPC_STUB ICDF_GetRootSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ICDF_GetRootSectionEntry_Proxy( 
    ICDF * This,
    /* [in] */ ULONG SectionID,
    /* [out] */ ISectionEntry **ppSectionEntry);


void __RPC_STUB ICDF_GetRootSectionEntry_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [restricted][propget] */ HRESULT STDMETHODCALLTYPE ICDF_get__NewEnum_Proxy( 
    ICDF * This,
    /* [retval][out] */ IUnknown **ppunkEnum);


void __RPC_STUB ICDF_get__NewEnum_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICDF_get_Count_Proxy( 
    ICDF * This,
    /* [retval][out] */ ULONG *pdwCount);


void __RPC_STUB ICDF_get_Count_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICDF_get_Item_Proxy( 
    ICDF * This,
    /* [in] */ ULONG SectionID,
    /* [retval][out] */ IUnknown **ppUnknown);


void __RPC_STUB ICDF_get_Item_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ICDF_INTERFACE_DEFINED__ */


#ifndef __ISectionWithStringKey_INTERFACE_DEFINED__
#define __ISectionWithStringKey_INTERFACE_DEFINED__

/* interface ISectionWithStringKey */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISectionWithStringKey;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("285a8871-c84a-11d7-850f-005cd062464f")
    ISectionWithStringKey : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Lookup( 
            /* [in] */ LPCWSTR wzStringKey,
            /* [out] */ IUnknown **ppUnknown) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsCaseInsensitive( 
            /* [retval][out] */ VARIANT_BOOL *pbIsCaseInsentitive) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ISectionWithStringKeyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISectionWithStringKey * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISectionWithStringKey * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISectionWithStringKey * This);
        
        HRESULT ( STDMETHODCALLTYPE *Lookup )( 
            ISectionWithStringKey * This,
            /* [in] */ LPCWSTR wzStringKey,
            /* [out] */ IUnknown **ppUnknown);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsCaseInsensitive )( 
            ISectionWithStringKey * This,
            /* [retval][out] */ VARIANT_BOOL *pbIsCaseInsentitive);
        
        END_INTERFACE
    } ISectionWithStringKeyVtbl;

    interface ISectionWithStringKey
    {
        CONST_VTBL struct ISectionWithStringKeyVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISectionWithStringKey_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ISectionWithStringKey_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ISectionWithStringKey_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ISectionWithStringKey_Lookup(This,wzStringKey,ppUnknown)	\
    (This)->lpVtbl -> Lookup(This,wzStringKey,ppUnknown)

#define ISectionWithStringKey_get_IsCaseInsensitive(This,pbIsCaseInsentitive)	\
    (This)->lpVtbl -> get_IsCaseInsensitive(This,pbIsCaseInsentitive)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ISectionWithStringKey_Lookup_Proxy( 
    ISectionWithStringKey * This,
    /* [in] */ LPCWSTR wzStringKey,
    /* [out] */ IUnknown **ppUnknown);


void __RPC_STUB ISectionWithStringKey_Lookup_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISectionWithStringKey_get_IsCaseInsensitive_Proxy( 
    ISectionWithStringKey * This,
    /* [retval][out] */ VARIANT_BOOL *pbIsCaseInsentitive);


void __RPC_STUB ISectionWithStringKey_get_IsCaseInsensitive_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ISectionWithStringKey_INTERFACE_DEFINED__ */


#ifndef __ISectionWithBlobKey_INTERFACE_DEFINED__
#define __ISectionWithBlobKey_INTERFACE_DEFINED__

/* interface ISectionWithBlobKey */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISectionWithBlobKey;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("285a8872-c84a-11d7-850f-005cd062464f")
    ISectionWithBlobKey : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Lookup( 
            /* [size_is][in] */ byte *pBlobKey,
            /* [in] */ ULONG ulBlobSize,
            /* [out] */ IUnknown **ppUnknown) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ISectionWithBlobKeyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISectionWithBlobKey * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISectionWithBlobKey * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISectionWithBlobKey * This);
        
        HRESULT ( STDMETHODCALLTYPE *Lookup )( 
            ISectionWithBlobKey * This,
            /* [size_is][in] */ byte *pBlobKey,
            /* [in] */ ULONG ulBlobSize,
            /* [out] */ IUnknown **ppUnknown);
        
        END_INTERFACE
    } ISectionWithBlobKeyVtbl;

    interface ISectionWithBlobKey
    {
        CONST_VTBL struct ISectionWithBlobKeyVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISectionWithBlobKey_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ISectionWithBlobKey_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ISectionWithBlobKey_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ISectionWithBlobKey_Lookup(This,pBlobKey,ulBlobSize,ppUnknown)	\
    (This)->lpVtbl -> Lookup(This,pBlobKey,ulBlobSize,ppUnknown)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ISectionWithBlobKey_Lookup_Proxy( 
    ISectionWithBlobKey * This,
    /* [size_is][in] */ byte *pBlobKey,
    /* [in] */ ULONG ulBlobSize,
    /* [out] */ IUnknown **ppUnknown);


void __RPC_STUB ISectionWithBlobKey_Lookup_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ISectionWithBlobKey_INTERFACE_DEFINED__ */


#ifndef __ISectionWithGuidKey_INTERFACE_DEFINED__
#define __ISectionWithGuidKey_INTERFACE_DEFINED__

/* interface ISectionWithGuidKey */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISectionWithGuidKey;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("285a8873-c84a-11d7-850f-005cd062464f")
    ISectionWithGuidKey : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Lookup( 
            /* [in] */ const GUID *pGuidKey,
            /* [out] */ IUnknown **ppUnknown) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ISectionWithGuidKeyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISectionWithGuidKey * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISectionWithGuidKey * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISectionWithGuidKey * This);
        
        HRESULT ( STDMETHODCALLTYPE *Lookup )( 
            ISectionWithGuidKey * This,
            /* [in] */ const GUID *pGuidKey,
            /* [out] */ IUnknown **ppUnknown);
        
        END_INTERFACE
    } ISectionWithGuidKeyVtbl;

    interface ISectionWithGuidKey
    {
        CONST_VTBL struct ISectionWithGuidKeyVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISectionWithGuidKey_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ISectionWithGuidKey_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ISectionWithGuidKey_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ISectionWithGuidKey_Lookup(This,pGuidKey,ppUnknown)	\
    (This)->lpVtbl -> Lookup(This,pGuidKey,ppUnknown)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ISectionWithGuidKey_Lookup_Proxy( 
    ISectionWithGuidKey * This,
    /* [in] */ const GUID *pGuidKey,
    /* [out] */ IUnknown **ppUnknown);


void __RPC_STUB ISectionWithGuidKey_Lookup_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ISectionWithGuidKey_INTERFACE_DEFINED__ */


#ifndef __ISectionWithIntegerKey_INTERFACE_DEFINED__
#define __ISectionWithIntegerKey_INTERFACE_DEFINED__

/* interface ISectionWithIntegerKey */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISectionWithIntegerKey;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("285a8874-c84a-11d7-850f-005cd062464f")
    ISectionWithIntegerKey : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Lookup( 
            /* [in] */ ULONG ulIntegerKey,
            /* [out] */ IUnknown **ppUnknown) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ISectionWithIntegerKeyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISectionWithIntegerKey * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISectionWithIntegerKey * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISectionWithIntegerKey * This);
        
        HRESULT ( STDMETHODCALLTYPE *Lookup )( 
            ISectionWithIntegerKey * This,
            /* [in] */ ULONG ulIntegerKey,
            /* [out] */ IUnknown **ppUnknown);
        
        END_INTERFACE
    } ISectionWithIntegerKeyVtbl;

    interface ISectionWithIntegerKey
    {
        CONST_VTBL struct ISectionWithIntegerKeyVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISectionWithIntegerKey_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ISectionWithIntegerKey_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ISectionWithIntegerKey_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ISectionWithIntegerKey_Lookup(This,ulIntegerKey,ppUnknown)	\
    (This)->lpVtbl -> Lookup(This,ulIntegerKey,ppUnknown)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ISectionWithIntegerKey_Lookup_Proxy( 
    ISectionWithIntegerKey * This,
    /* [in] */ ULONG ulIntegerKey,
    /* [out] */ IUnknown **ppUnknown);


void __RPC_STUB ISectionWithIntegerKey_Lookup_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ISectionWithIntegerKey_INTERFACE_DEFINED__ */


#ifndef __ISectionWithDefinitionIdentityKey_INTERFACE_DEFINED__
#define __ISectionWithDefinitionIdentityKey_INTERFACE_DEFINED__

/* interface ISectionWithDefinitionIdentityKey */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISectionWithDefinitionIdentityKey;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("285a8875-c84a-11d7-850f-005cd062464f")
    ISectionWithDefinitionIdentityKey : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Lookup( 
            /* [in] */ IDefinitionIdentity *pDefinitionIdentityKey,
            /* [out] */ IUnknown **ppUnknown) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ISectionWithDefinitionIdentityKeyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISectionWithDefinitionIdentityKey * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISectionWithDefinitionIdentityKey * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISectionWithDefinitionIdentityKey * This);
        
        HRESULT ( STDMETHODCALLTYPE *Lookup )( 
            ISectionWithDefinitionIdentityKey * This,
            /* [in] */ IDefinitionIdentity *pDefinitionIdentityKey,
            /* [out] */ IUnknown **ppUnknown);
        
        END_INTERFACE
    } ISectionWithDefinitionIdentityKeyVtbl;

    interface ISectionWithDefinitionIdentityKey
    {
        CONST_VTBL struct ISectionWithDefinitionIdentityKeyVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISectionWithDefinitionIdentityKey_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ISectionWithDefinitionIdentityKey_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ISectionWithDefinitionIdentityKey_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ISectionWithDefinitionIdentityKey_Lookup(This,pDefinitionIdentityKey,ppUnknown)	\
    (This)->lpVtbl -> Lookup(This,pDefinitionIdentityKey,ppUnknown)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ISectionWithDefinitionIdentityKey_Lookup_Proxy( 
    ISectionWithDefinitionIdentityKey * This,
    /* [in] */ IDefinitionIdentity *pDefinitionIdentityKey,
    /* [out] */ IUnknown **ppUnknown);


void __RPC_STUB ISectionWithDefinitionIdentityKey_Lookup_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ISectionWithDefinitionIdentityKey_INTERFACE_DEFINED__ */


#ifndef __ISectionWithReferenceIdentityKey_INTERFACE_DEFINED__
#define __ISectionWithReferenceIdentityKey_INTERFACE_DEFINED__

/* interface ISectionWithReferenceIdentityKey */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ISectionWithReferenceIdentityKey;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("285a8876-c84a-11d7-850f-005cd062464f")
    ISectionWithReferenceIdentityKey : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Lookup( 
            /* [in] */ IReferenceIdentity *pReferenceIdentityKey,
            /* [out] */ IUnknown **ppUnknown) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ISectionWithReferenceIdentityKeyVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISectionWithReferenceIdentityKey * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISectionWithReferenceIdentityKey * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISectionWithReferenceIdentityKey * This);
        
        HRESULT ( STDMETHODCALLTYPE *Lookup )( 
            ISectionWithReferenceIdentityKey * This,
            /* [in] */ IReferenceIdentity *pReferenceIdentityKey,
            /* [out] */ IUnknown **ppUnknown);
        
        END_INTERFACE
    } ISectionWithReferenceIdentityKeyVtbl;

    interface ISectionWithReferenceIdentityKey
    {
        CONST_VTBL struct ISectionWithReferenceIdentityKeyVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISectionWithReferenceIdentityKey_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ISectionWithReferenceIdentityKey_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ISectionWithReferenceIdentityKey_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ISectionWithReferenceIdentityKey_Lookup(This,pReferenceIdentityKey,ppUnknown)	\
    (This)->lpVtbl -> Lookup(This,pReferenceIdentityKey,ppUnknown)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ISectionWithReferenceIdentityKey_Lookup_Proxy( 
    ISectionWithReferenceIdentityKey * This,
    /* [in] */ IReferenceIdentity *pReferenceIdentityKey,
    /* [out] */ IUnknown **ppUnknown);


void __RPC_STUB ISectionWithReferenceIdentityKey_Lookup_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ISectionWithReferenceIdentityKey_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0278 */
/* [local] */ 

typedef 
enum _CMSSECTIONID
    {	CMSSECTIONID_FILE_SECTION	= 1,
	CMSSECTIONID_CATEGORY_INSTANCE_SECTION	= 2,
	CMSSECTIONID_COM_REDIRECTION_SECTION	= 3,
	CMSSECTIONID_PROGID_REDIRECTION_SECTION	= 4,
	CMSSECTIONID_CLR_SURROGATE_SECTION	= 5,
	CMSSECTIONID_ASSEMBLY_REFERENCE_SECTION	= 6,
	CMSSECTIONID_WINDOW_CLASS_SECTION	= 8,
	CMSSECTIONID_STRING_SECTION	= 9,
	CMSSECTIONID_ENTRYPOINT_SECTION	= 10,
	CMSSECTIONID_PERMISSION_SET_SECTION	= 11,
	CMSSECTIONENTRYID_METADATA	= 12,
	CMSSECTIONID_ASSEMBLY_REQUEST_SECTION	= 13,
	CMSSECTIONID_REGISTRY_KEY_SECTION	= 16,
	CMSSECTIONID_DIRECTORY_SECTION	= 17,
	CMSSECTIONID_FILE_ASSOCIATION_SECTION	= 18,
	CMSSECTIONID_COMPATIBLE_FRAMEWORKS_SECTION	= 19,
	CMSSECTIONID_EVENT_SECTION	= 101,
	CMSSECTIONID_EVENT_MAP_SECTION	= 102,
	CMSSECTIONID_EVENT_TAG_SECTION	= 103,
	CMSSECTIONID_COUNTERSET_SECTION	= 110,
	CMSSECTIONID_COUNTER_SECTION	= 111
    } 	CMSSECTIONID;






































enum __MIDL___MIDL_itf_isolation_0278_0001
    {	CMS_ASSEMBLY_DEPLOYMENT_FLAG_BEFORE_APPLICATION_STARTUP	= 4,
	CMS_ASSEMBLY_DEPLOYMENT_FLAG_RUN_AFTER_INSTALL	= 16,
	CMS_ASSEMBLY_DEPLOYMENT_FLAG_INSTALL	= 32,
	CMS_ASSEMBLY_DEPLOYMENT_FLAG_TRUST_URL_PARAMETERS	= 64,
	CMS_ASSEMBLY_DEPLOYMENT_FLAG_DISALLOW_URL_ACTIVATION	= 128,
	CMS_ASSEMBLY_DEPLOYMENT_FLAG_MAP_FILE_EXTENSIONS	= 256,
	CMS_ASSEMBLY_DEPLOYMENT_FLAG_CREATE_DESKTOP_SHORTCUT	= 512
    } ;

enum __MIDL___MIDL_itf_isolation_0278_0002
    {	CMS_ASSEMBLY_REFERENCE_FLAG_OPTIONAL	= 1,
	CMS_ASSEMBLY_REFERENCE_FLAG_VISIBLE	= 2,
	CMS_ASSEMBLY_REFERENCE_FLAG_FOLLOW	= 4,
	CMS_ASSEMBLY_REFERENCE_FLAG_IS_PLATFORM	= 8,
	CMS_ASSEMBLY_REFERENCE_FLAG_CULTURE_WILDCARDED	= 16,
	CMS_ASSEMBLY_REFERENCE_FLAG_PROCESSOR_ARCHITECTURE_WILDCARDED	= 32,
	CMS_ASSEMBLY_REFERENCE_FLAG_PREREQUISITE	= 128
    } ;

enum __MIDL___MIDL_itf_isolation_0278_0003
    {	CMS_ASSEMBLY_REFERENCE_DEPENDENT_ASSEMBLY_FLAG_OPTIONAL	= 1,
	CMS_ASSEMBLY_REFERENCE_DEPENDENT_ASSEMBLY_FLAG_VISIBLE	= 2,
	CMS_ASSEMBLY_REFERENCE_DEPENDENT_ASSEMBLY_FLAG_PREREQUISITE	= 4,
	CMS_ASSEMBLY_REFERENCE_DEPENDENT_ASSEMBLY_FLAG_RESOURCE_FALLBACK_CULTURE_INTERNAL	= 8,
	CMS_ASSEMBLY_REFERENCE_DEPENDENT_ASSEMBLY_FLAG_INSTALL	= 16,
	CMS_ASSEMBLY_REFERENCE_DEPENDENT_ASSEMBLY_FLAG_ALLOW_DELAYED_BINDING	= 32
    } ;

enum __MIDL___MIDL_itf_isolation_0278_0004
    {	CMS_FILE_FLAG_OPTIONAL	= 1
    } ;

enum __MIDL___MIDL_itf_isolation_0278_0005
    {	CMS_ENTRY_POINT_FLAG_HOST_IN_BROWSER	= 1,
	CMS_ENTRY_POINT_FLAG_CUSTOMHOSTSPECIFIED	= 2,
	CMS_ENTRY_POINT_FLAG_CUSTOMUX	= 4
    } ;

enum __MIDL___MIDL_itf_isolation_0278_0006
    {	CMS_COM_SERVER_FLAG_IS_CLR_CLASS	= 1
    } ;

enum __MIDL___MIDL_itf_isolation_0278_0007
    {	CMS_REGISTRY_KEY_FLAG_OWNER	= 1,
	CMS_REGISTRY_KEY_FLAG_LEAF_IN_MANIFEST	= 2
    } ;

enum __MIDL___MIDL_itf_isolation_0278_0008
    {	CMS_REGISTRY_VALUE_FLAG_OWNER	= 1
    } ;

enum __MIDL___MIDL_itf_isolation_0278_0009
    {	CMS_DIRECTORY_FLAG_OWNER	= 1
    } ;

enum __MIDL___MIDL_itf_isolation_0278_0010
    {	CMS_MANIFEST_FLAG_ASSEMBLY	= 1,
	CMS_MANIFEST_FLAG_CATEGORY	= 2,
	CMS_MANIFEST_FLAG_FEATURE	= 3,
	CMS_MANIFEST_FLAG_APPLICATION	= 4,
	CMS_MANIFEST_FLAG_USEMANIFESTFORTRUST	= 8
    } ;

enum __MIDL___MIDL_itf_isolation_0278_0011
    {	CMS_USAGE_PATTERN_SCOPE_APPLICATION	= 1,
	CMS_USAGE_PATTERN_SCOPE_PROCESS	= 2,
	CMS_USAGE_PATTERN_SCOPE_MACHINE	= 3,
	CMS_USAGE_PATTERN_SCOPE_MASK	= 7
    } ;
typedef 
enum _CMS_SCHEMA_VERSION
    {	CMS_SCHEMA_VERSION_V1	= 1,
	CMS_SCHEMA_VERSION_V2	= 2
    } 	CMS_SCHEMA_VERSION;

typedef enum _CMS_SCHEMA_VERSION *PCMS_SCHEMA_VERSION;

typedef const CMS_SCHEMA_VERSION *PCCMS_SCHEMA_VERSION;

typedef 
enum _CMS_FILE_HASH_ALGORITHM
    {	CMS_FILE_HASH_ALGORITHM_SHA1	= 1,
	CMS_FILE_HASH_ALGORITHM_SHA256	= 2,
	CMS_FILE_HASH_ALGORITHM_SHA384	= 3,
	CMS_FILE_HASH_ALGORITHM_SHA512	= 4,
	CMS_FILE_HASH_ALGORITHM_MD5	= 5,
	CMS_FILE_HASH_ALGORITHM_MD4	= 6,
	CMS_FILE_HASH_ALGORITHM_MD2	= 7
    } 	CMS_FILE_HASH_ALGORITHM;

typedef enum _CMS_FILE_HASH_ALGORITHM *PCMS_FILE_HASH_ALGORITHM;

typedef const CMS_FILE_HASH_ALGORITHM *PCCMS_FILE_HASH_ALGORITHM;

typedef 
enum _CMS_TIME_UNIT_TYPE
    {	CMS_TIME_UNIT_TYPE_HOURS	= 1,
	CMS_TIME_UNIT_TYPE_DAYS	= 2,
	CMS_TIME_UNIT_TYPE_WEEKS	= 3,
	CMS_TIME_UNIT_TYPE_MONTHS	= 4
    } 	CMS_TIME_UNIT_TYPE;

typedef enum _CMS_TIME_UNIT_TYPE *PCMS_TIME_UNIT_TYPE;

typedef const CMS_TIME_UNIT_TYPE *PCCMS_TIME_UNIT_TYPE;

typedef 
enum _CMS_REGISTRY_VALUE_TYPE
    {	CMS_REGISTRY_VALUE_TYPE_NONE	= 0,
	CMS_REGISTRY_VALUE_TYPE_SZ	= 1,
	CMS_REGISTRY_VALUE_TYPE_EXPAND_SZ	= 2,
	CMS_REGISTRY_VALUE_TYPE_MULTI_SZ	= 3,
	CMS_REGISTRY_VALUE_TYPE_BINARY	= 4,
	CMS_REGISTRY_VALUE_TYPE_DWORD	= 5,
	CMS_REGISTRY_VALUE_TYPE_DWORD_LITTLE_ENDIAN	= 6,
	CMS_REGISTRY_VALUE_TYPE_DWORD_BIG_ENDIAN	= 7,
	CMS_REGISTRY_VALUE_TYPE_LINK	= 8,
	CMS_REGISTRY_VALUE_TYPE_RESOURCE_LIST	= 9,
	CMS_REGISTRY_VALUE_TYPE_FULL_RESOURCE_DESCRIPTOR	= 10,
	CMS_REGISTRY_VALUE_TYPE_RESOURCE_REQUIREMENTS_LIST	= 11,
	CMS_REGISTRY_VALUE_TYPE_QWORD	= 12,
	CMS_REGISTRY_VALUE_TYPE_QWORD_LITTLE_ENDIAN	= 13
    } 	CMS_REGISTRY_VALUE_TYPE;

typedef enum _CMS_REGISTRY_VALUE_TYPE *PCMS_REGISTRY_VALUE_TYPE;

typedef const CMS_REGISTRY_VALUE_TYPE *PCCMS_REGISTRY_VALUE_TYPE;

typedef 
enum _CMS_REGISTRY_VALUE_HINT
    {	CMS_REGISTRY_VALUE_HINT_REPLACE	= 1,
	CMS_REGISTRY_VALUE_HINT_APPEND	= 2,
	CMS_REGISTRY_VALUE_HINT_PREPEND	= 3
    } 	CMS_REGISTRY_VALUE_HINT;

typedef enum _CMS_REGISTRY_VALUE_HINT *PCMS_REGISTRY_VALUE_HINT;

typedef const CMS_REGISTRY_VALUE_HINT *PCCMS_REGISTRY_VALUE_HINT;

typedef 
enum _CMS_SYSTEM_PROTECTION
    {	CMS_SYSTEM_PROTECTION_READ_ONLY_IGNORE_WRITES	= 1,
	CMS_SYSTEM_PROTECTION_READ_ONLY_FAIL_WRITES	= 2,
	CMS_SYSTEM_PROTECTION_OS_ONLY_IGNORE_WRITES	= 3,
	CMS_SYSTEM_PROTECTION_OS_ONLY_FAIL_WRITES	= 4,
	CMS_SYSTEM_PROTECTION_TRANSACTED	= 5,
	CMS_SYSTEM_PROTECTION_APPLICATION_VIRTUALIZED	= 6,
	CMS_SYSTEM_PROTECTION_USER_VIRTUALIZED	= 7,
	CMS_SYSTEM_PROTECTION_APPLICATION_AND_USER_VIRTUALIZED	= 8,
	CMS_SYSTEM_PROTECTION_INHERIT	= 9,
	CMS_SYSTEM_PROTECTION_NOT_PROTECTED	= 10
    } 	CMS_SYSTEM_PROTECTION;

typedef enum _CMS_SYSTEM_PROTECTION *PCMS_SYSTEM_PROTECTION;

typedef const CMS_SYSTEM_PROTECTION *PCCMS_SYSTEM_PROTECTION;

typedef 
enum _CMS_FILE_WRITABLE_TYPE
    {	CMS_FILE_WRITABLE_TYPE_NOT_WRITABLE	= 1,
	CMS_FILE_WRITABLE_TYPE_APPLICATION_DATA	= 2
    } 	CMS_FILE_WRITABLE_TYPE;

typedef enum _CMS_FILE_WRITABLE_TYPE *PCMS_FILE_WRITABLE_TYPE;

typedef const CMS_FILE_WRITABLE_TYPE *PCCMS_FILE_WRITABLE_TYPE;

typedef 
enum _CMS_HASH_TRANSFORM
    {	CMS_HASH_TRANSFORM_IDENTITY	= 1,
	CMS_HASH_TRANSFORM_MANIFESTINVARIANT	= 2
    } 	CMS_HASH_TRANSFORM;

typedef enum _CMS_HASH_TRANSFORM *PCMS_HASH_TRANSFORM;

typedef const CMS_HASH_TRANSFORM *PCCMS_HASH_TRANSFORM;

typedef 
enum _CMS_HASH_DIGESTMETHOD
    {	CMS_HASH_DIGESTMETHOD_SHA1	= 1,
	CMS_HASH_DIGESTMETHOD_SHA256	= 2,
	CMS_HASH_DIGESTMETHOD_SHA384	= 3,
	CMS_HASH_DIGESTMETHOD_SHA512	= 4
    } 	CMS_HASH_DIGESTMETHOD;

typedef enum _CMS_HASH_DIGESTMETHOD *PCMS_HASH_DIGESTMETHOD;

typedef const CMS_HASH_DIGESTMETHOD *PCCMS_HASH_DIGESTMETHOD;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0278_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0278_v0_0_s_ifspec;

#ifndef __ICMS_INTERFACE_DEFINED__
#define __ICMS_INTERFACE_DEFINED__

/* interface ICMS */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_ICMS;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("a504e5b0-8ccf-4cb4-9902-c9d1b9abd033")
    ICMS : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Identity( 
            /* [retval][out] */ IDefinitionIdentity **__MIDL_0015) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FileSection( 
            /* [retval][out] */ ISection **__MIDL_0016) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CategoryMembershipSection( 
            /* [retval][out] */ ISection **__MIDL_0017) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_COMRedirectionSection( 
            /* [retval][out] */ ISection **__MIDL_0018) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ProgIdRedirectionSection( 
            /* [retval][out] */ ISection **__MIDL_0019) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CLRSurrogateSection( 
            /* [retval][out] */ ISection **__MIDL_0020) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AssemblyReferenceSection( 
            /* [retval][out] */ ISection **__MIDL_0021) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_WindowClassSection( 
            /* [retval][out] */ ISection **__MIDL_0022) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StringSection( 
            /* [retval][out] */ ISection **__MIDL_0023) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_EntryPointSection( 
            /* [retval][out] */ ISection **__MIDL_0024) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_PermissionSetSection( 
            /* [retval][out] */ ISection **__MIDL_0025) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MetadataSectionEntry( 
            /* [retval][out] */ ISectionEntry **__MIDL_0026) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AssemblyRequestSection( 
            /* [retval][out] */ ISection **__MIDL_0027) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RegistryKeySection( 
            /* [retval][out] */ ISection **__MIDL_0028) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DirectorySection( 
            /* [retval][out] */ ISection **__MIDL_0029) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FileAssociationSection( 
            /* [retval][out] */ ISection **__MIDL_0030) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CompatibleFrameworksSection( 
            /* [retval][out] */ ISection **__MIDL_0031) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_EventSection( 
            /* [retval][out] */ ISection **__MIDL_0032) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_EventMapSection( 
            /* [retval][out] */ ISection **__MIDL_0033) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_EventTagSection( 
            /* [retval][out] */ ISection **__MIDL_0034) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CounterSetSection( 
            /* [retval][out] */ ISection **__MIDL_0035) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CounterSection( 
            /* [retval][out] */ ISection **__MIDL_0036) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ICMSVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICMS * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICMS * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICMS * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Identity )( 
            ICMS * This,
            /* [retval][out] */ IDefinitionIdentity **__MIDL_0015);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FileSection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0016);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CategoryMembershipSection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0017);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_COMRedirectionSection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0018);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ProgIdRedirectionSection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0019);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CLRSurrogateSection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0020);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AssemblyReferenceSection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0021);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_WindowClassSection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0022);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StringSection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0023);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_EntryPointSection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0024);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_PermissionSetSection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0025);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MetadataSectionEntry )( 
            ICMS * This,
            /* [retval][out] */ ISectionEntry **__MIDL_0026);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AssemblyRequestSection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0027);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RegistryKeySection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0028);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DirectorySection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0029);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FileAssociationSection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0030);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CompatibleFrameworksSection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0031);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_EventSection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0032);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_EventMapSection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0033);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_EventTagSection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0034);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CounterSetSection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0035);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CounterSection )( 
            ICMS * This,
            /* [retval][out] */ ISection **__MIDL_0036);
        
        END_INTERFACE
    } ICMSVtbl;

    interface ICMS
    {
        CONST_VTBL struct ICMSVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICMS_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ICMS_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ICMS_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ICMS_get_Identity(This,__MIDL_0015)	\
    (This)->lpVtbl -> get_Identity(This,__MIDL_0015)

#define ICMS_get_FileSection(This,__MIDL_0016)	\
    (This)->lpVtbl -> get_FileSection(This,__MIDL_0016)

#define ICMS_get_CategoryMembershipSection(This,__MIDL_0017)	\
    (This)->lpVtbl -> get_CategoryMembershipSection(This,__MIDL_0017)

#define ICMS_get_COMRedirectionSection(This,__MIDL_0018)	\
    (This)->lpVtbl -> get_COMRedirectionSection(This,__MIDL_0018)

#define ICMS_get_ProgIdRedirectionSection(This,__MIDL_0019)	\
    (This)->lpVtbl -> get_ProgIdRedirectionSection(This,__MIDL_0019)

#define ICMS_get_CLRSurrogateSection(This,__MIDL_0020)	\
    (This)->lpVtbl -> get_CLRSurrogateSection(This,__MIDL_0020)

#define ICMS_get_AssemblyReferenceSection(This,__MIDL_0021)	\
    (This)->lpVtbl -> get_AssemblyReferenceSection(This,__MIDL_0021)

#define ICMS_get_WindowClassSection(This,__MIDL_0022)	\
    (This)->lpVtbl -> get_WindowClassSection(This,__MIDL_0022)

#define ICMS_get_StringSection(This,__MIDL_0023)	\
    (This)->lpVtbl -> get_StringSection(This,__MIDL_0023)

#define ICMS_get_EntryPointSection(This,__MIDL_0024)	\
    (This)->lpVtbl -> get_EntryPointSection(This,__MIDL_0024)

#define ICMS_get_PermissionSetSection(This,__MIDL_0025)	\
    (This)->lpVtbl -> get_PermissionSetSection(This,__MIDL_0025)

#define ICMS_get_MetadataSectionEntry(This,__MIDL_0026)	\
    (This)->lpVtbl -> get_MetadataSectionEntry(This,__MIDL_0026)

#define ICMS_get_AssemblyRequestSection(This,__MIDL_0027)	\
    (This)->lpVtbl -> get_AssemblyRequestSection(This,__MIDL_0027)

#define ICMS_get_RegistryKeySection(This,__MIDL_0028)	\
    (This)->lpVtbl -> get_RegistryKeySection(This,__MIDL_0028)

#define ICMS_get_DirectorySection(This,__MIDL_0029)	\
    (This)->lpVtbl -> get_DirectorySection(This,__MIDL_0029)

#define ICMS_get_FileAssociationSection(This,__MIDL_0030)	\
    (This)->lpVtbl -> get_FileAssociationSection(This,__MIDL_0030)

#define ICMS_get_CompatibleFrameworksSection(This,__MIDL_0031)	\
    (This)->lpVtbl -> get_CompatibleFrameworksSection(This,__MIDL_0031)

#define ICMS_get_EventSection(This,__MIDL_0032)	\
    (This)->lpVtbl -> get_EventSection(This,__MIDL_0032)

#define ICMS_get_EventMapSection(This,__MIDL_0033)	\
    (This)->lpVtbl -> get_EventMapSection(This,__MIDL_0033)

#define ICMS_get_EventTagSection(This,__MIDL_0034)	\
    (This)->lpVtbl -> get_EventTagSection(This,__MIDL_0034)

#define ICMS_get_CounterSetSection(This,__MIDL_0035)	\
    (This)->lpVtbl -> get_CounterSetSection(This,__MIDL_0035)

#define ICMS_get_CounterSection(This,__MIDL_0036)	\
    (This)->lpVtbl -> get_CounterSection(This,__MIDL_0036)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_Identity_Proxy( 
    ICMS * This,
    /* [retval][out] */ IDefinitionIdentity **__MIDL_0015);


void __RPC_STUB ICMS_get_Identity_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_FileSection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0016);


void __RPC_STUB ICMS_get_FileSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_CategoryMembershipSection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0017);


void __RPC_STUB ICMS_get_CategoryMembershipSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_COMRedirectionSection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0018);


void __RPC_STUB ICMS_get_COMRedirectionSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_ProgIdRedirectionSection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0019);


void __RPC_STUB ICMS_get_ProgIdRedirectionSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_CLRSurrogateSection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0020);


void __RPC_STUB ICMS_get_CLRSurrogateSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_AssemblyReferenceSection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0021);


void __RPC_STUB ICMS_get_AssemblyReferenceSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_WindowClassSection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0022);


void __RPC_STUB ICMS_get_WindowClassSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_StringSection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0023);


void __RPC_STUB ICMS_get_StringSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_EntryPointSection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0024);


void __RPC_STUB ICMS_get_EntryPointSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_PermissionSetSection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0025);


void __RPC_STUB ICMS_get_PermissionSetSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_MetadataSectionEntry_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISectionEntry **__MIDL_0026);


void __RPC_STUB ICMS_get_MetadataSectionEntry_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_AssemblyRequestSection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0027);


void __RPC_STUB ICMS_get_AssemblyRequestSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_RegistryKeySection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0028);


void __RPC_STUB ICMS_get_RegistryKeySection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_DirectorySection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0029);


void __RPC_STUB ICMS_get_DirectorySection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_FileAssociationSection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0030);


void __RPC_STUB ICMS_get_FileAssociationSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_CompatibleFrameworksSection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0031);


void __RPC_STUB ICMS_get_CompatibleFrameworksSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_EventSection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0032);


void __RPC_STUB ICMS_get_EventSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_EventMapSection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0033);


void __RPC_STUB ICMS_get_EventMapSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_EventTagSection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0034);


void __RPC_STUB ICMS_get_EventTagSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_CounterSetSection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0035);


void __RPC_STUB ICMS_get_CounterSetSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICMS_get_CounterSection_Proxy( 
    ICMS * This,
    /* [retval][out] */ ISection **__MIDL_0036);


void __RPC_STUB ICMS_get_CounterSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ICMS_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0280 */
/* [local] */ 

typedef struct _MuiResourceIdLookupMapEntry
    {
    ULONG Count;
    } 	MuiResourceIdLookupMapEntry;

typedef 
enum _MuiResourceIdLookupMapEntryFieldId
    {	MuiResourceIdLookupMap_Count	= 0
    } 	MuiResourceIdLookupMapEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0280_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0280_v0_0_s_ifspec;

#ifndef __IMuiResourceIdLookupMapEntry_INTERFACE_DEFINED__
#define __IMuiResourceIdLookupMapEntry_INTERFACE_DEFINED__

/* interface IMuiResourceIdLookupMapEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IMuiResourceIdLookupMapEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("24abe1f7-a396-4a03-9adf-1d5b86a5569f")
    IMuiResourceIdLookupMapEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ MuiResourceIdLookupMapEntry **__MIDL_0037) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Count( 
            /* [retval][out] */ ULONG *__MIDL_0038) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IMuiResourceIdLookupMapEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IMuiResourceIdLookupMapEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IMuiResourceIdLookupMapEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IMuiResourceIdLookupMapEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IMuiResourceIdLookupMapEntry * This,
            /* [retval][out] */ MuiResourceIdLookupMapEntry **__MIDL_0037);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Count )( 
            IMuiResourceIdLookupMapEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0038);
        
        END_INTERFACE
    } IMuiResourceIdLookupMapEntryVtbl;

    interface IMuiResourceIdLookupMapEntry
    {
        CONST_VTBL struct IMuiResourceIdLookupMapEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IMuiResourceIdLookupMapEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IMuiResourceIdLookupMapEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IMuiResourceIdLookupMapEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IMuiResourceIdLookupMapEntry_get_AllData(This,__MIDL_0037)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0037)

#define IMuiResourceIdLookupMapEntry_get_Count(This,__MIDL_0038)	\
    (This)->lpVtbl -> get_Count(This,__MIDL_0038)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IMuiResourceIdLookupMapEntry_get_AllData_Proxy( 
    IMuiResourceIdLookupMapEntry * This,
    /* [retval][out] */ MuiResourceIdLookupMapEntry **__MIDL_0037);


void __RPC_STUB IMuiResourceIdLookupMapEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMuiResourceIdLookupMapEntry_get_Count_Proxy( 
    IMuiResourceIdLookupMapEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0038);


void __RPC_STUB IMuiResourceIdLookupMapEntry_get_Count_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IMuiResourceIdLookupMapEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0281 */
/* [local] */ 

typedef struct _MuiResourceTypeIdStringEntry
    {
    /* [size_is] */ BYTE *StringIds;
    ULONG StringIdsSize;
    /* [size_is] */ BYTE *IntegerIds;
    ULONG IntegerIdsSize;
    } 	MuiResourceTypeIdStringEntry;

typedef 
enum _MuiResourceTypeIdStringEntryFieldId
    {	MuiResourceTypeIdString_StringIds	= 0,
	MuiResourceTypeIdString_StringIdsSize	= MuiResourceTypeIdString_StringIds + 1,
	MuiResourceTypeIdString_IntegerIds	= MuiResourceTypeIdString_StringIdsSize + 1,
	MuiResourceTypeIdString_IntegerIdsSize	= MuiResourceTypeIdString_IntegerIds + 1
    } 	MuiResourceTypeIdStringEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0281_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0281_v0_0_s_ifspec;

#ifndef __IMuiResourceTypeIdStringEntry_INTERFACE_DEFINED__
#define __IMuiResourceTypeIdStringEntry_INTERFACE_DEFINED__

/* interface IMuiResourceTypeIdStringEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IMuiResourceTypeIdStringEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("11df5cad-c183-479b-9a44-3842b71639ce")
    IMuiResourceTypeIdStringEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ MuiResourceTypeIdStringEntry **__MIDL_0039) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StringIds( 
            /* [retval][out] */ IStream **__MIDL_0040) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IntegerIds( 
            /* [retval][out] */ IStream **__MIDL_0041) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IMuiResourceTypeIdStringEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IMuiResourceTypeIdStringEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IMuiResourceTypeIdStringEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IMuiResourceTypeIdStringEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IMuiResourceTypeIdStringEntry * This,
            /* [retval][out] */ MuiResourceTypeIdStringEntry **__MIDL_0039);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StringIds )( 
            IMuiResourceTypeIdStringEntry * This,
            /* [retval][out] */ IStream **__MIDL_0040);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IntegerIds )( 
            IMuiResourceTypeIdStringEntry * This,
            /* [retval][out] */ IStream **__MIDL_0041);
        
        END_INTERFACE
    } IMuiResourceTypeIdStringEntryVtbl;

    interface IMuiResourceTypeIdStringEntry
    {
        CONST_VTBL struct IMuiResourceTypeIdStringEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IMuiResourceTypeIdStringEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IMuiResourceTypeIdStringEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IMuiResourceTypeIdStringEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IMuiResourceTypeIdStringEntry_get_AllData(This,__MIDL_0039)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0039)

#define IMuiResourceTypeIdStringEntry_get_StringIds(This,__MIDL_0040)	\
    (This)->lpVtbl -> get_StringIds(This,__MIDL_0040)

#define IMuiResourceTypeIdStringEntry_get_IntegerIds(This,__MIDL_0041)	\
    (This)->lpVtbl -> get_IntegerIds(This,__MIDL_0041)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IMuiResourceTypeIdStringEntry_get_AllData_Proxy( 
    IMuiResourceTypeIdStringEntry * This,
    /* [retval][out] */ MuiResourceTypeIdStringEntry **__MIDL_0039);


void __RPC_STUB IMuiResourceTypeIdStringEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMuiResourceTypeIdStringEntry_get_StringIds_Proxy( 
    IMuiResourceTypeIdStringEntry * This,
    /* [retval][out] */ IStream **__MIDL_0040);


void __RPC_STUB IMuiResourceTypeIdStringEntry_get_StringIds_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMuiResourceTypeIdStringEntry_get_IntegerIds_Proxy( 
    IMuiResourceTypeIdStringEntry * This,
    /* [retval][out] */ IStream **__MIDL_0041);


void __RPC_STUB IMuiResourceTypeIdStringEntry_get_IntegerIds_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IMuiResourceTypeIdStringEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0282 */
/* [local] */ 

typedef struct _MuiResourceTypeIdIntEntry
    {
    /* [size_is] */ BYTE *StringIds;
    ULONG StringIdsSize;
    /* [size_is] */ BYTE *IntegerIds;
    ULONG IntegerIdsSize;
    } 	MuiResourceTypeIdIntEntry;

typedef 
enum _MuiResourceTypeIdIntEntryFieldId
    {	MuiResourceTypeIdInt_StringIds	= 0,
	MuiResourceTypeIdInt_StringIdsSize	= MuiResourceTypeIdInt_StringIds + 1,
	MuiResourceTypeIdInt_IntegerIds	= MuiResourceTypeIdInt_StringIdsSize + 1,
	MuiResourceTypeIdInt_IntegerIdsSize	= MuiResourceTypeIdInt_IntegerIds + 1
    } 	MuiResourceTypeIdIntEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0282_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0282_v0_0_s_ifspec;

#ifndef __IMuiResourceTypeIdIntEntry_INTERFACE_DEFINED__
#define __IMuiResourceTypeIdIntEntry_INTERFACE_DEFINED__

/* interface IMuiResourceTypeIdIntEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IMuiResourceTypeIdIntEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("55b2dec1-d0f6-4bf4-91b1-30f73ad8e4df")
    IMuiResourceTypeIdIntEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ MuiResourceTypeIdIntEntry **__MIDL_0042) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StringIds( 
            /* [retval][out] */ IStream **__MIDL_0043) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IntegerIds( 
            /* [retval][out] */ IStream **__MIDL_0044) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IMuiResourceTypeIdIntEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IMuiResourceTypeIdIntEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IMuiResourceTypeIdIntEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IMuiResourceTypeIdIntEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IMuiResourceTypeIdIntEntry * This,
            /* [retval][out] */ MuiResourceTypeIdIntEntry **__MIDL_0042);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StringIds )( 
            IMuiResourceTypeIdIntEntry * This,
            /* [retval][out] */ IStream **__MIDL_0043);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IntegerIds )( 
            IMuiResourceTypeIdIntEntry * This,
            /* [retval][out] */ IStream **__MIDL_0044);
        
        END_INTERFACE
    } IMuiResourceTypeIdIntEntryVtbl;

    interface IMuiResourceTypeIdIntEntry
    {
        CONST_VTBL struct IMuiResourceTypeIdIntEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IMuiResourceTypeIdIntEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IMuiResourceTypeIdIntEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IMuiResourceTypeIdIntEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IMuiResourceTypeIdIntEntry_get_AllData(This,__MIDL_0042)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0042)

#define IMuiResourceTypeIdIntEntry_get_StringIds(This,__MIDL_0043)	\
    (This)->lpVtbl -> get_StringIds(This,__MIDL_0043)

#define IMuiResourceTypeIdIntEntry_get_IntegerIds(This,__MIDL_0044)	\
    (This)->lpVtbl -> get_IntegerIds(This,__MIDL_0044)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IMuiResourceTypeIdIntEntry_get_AllData_Proxy( 
    IMuiResourceTypeIdIntEntry * This,
    /* [retval][out] */ MuiResourceTypeIdIntEntry **__MIDL_0042);


void __RPC_STUB IMuiResourceTypeIdIntEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMuiResourceTypeIdIntEntry_get_StringIds_Proxy( 
    IMuiResourceTypeIdIntEntry * This,
    /* [retval][out] */ IStream **__MIDL_0043);


void __RPC_STUB IMuiResourceTypeIdIntEntry_get_StringIds_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMuiResourceTypeIdIntEntry_get_IntegerIds_Proxy( 
    IMuiResourceTypeIdIntEntry * This,
    /* [retval][out] */ IStream **__MIDL_0044);


void __RPC_STUB IMuiResourceTypeIdIntEntry_get_IntegerIds_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IMuiResourceTypeIdIntEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0283 */
/* [local] */ 

typedef struct _MuiResourceMapEntry
    {
    /* [size_is] */ BYTE *ResourceTypeIdInt;
    ULONG ResourceTypeIdIntSize;
    /* [size_is] */ BYTE *ResourceTypeIdString;
    ULONG ResourceTypeIdStringSize;
    } 	MuiResourceMapEntry;

typedef 
enum _MuiResourceMapEntryFieldId
    {	MuiResourceMap_ResourceTypeIdInt	= 0,
	MuiResourceMap_ResourceTypeIdIntSize	= MuiResourceMap_ResourceTypeIdInt + 1,
	MuiResourceMap_ResourceTypeIdString	= MuiResourceMap_ResourceTypeIdIntSize + 1,
	MuiResourceMap_ResourceTypeIdStringSize	= MuiResourceMap_ResourceTypeIdString + 1
    } 	MuiResourceMapEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0283_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0283_v0_0_s_ifspec;

#ifndef __IMuiResourceMapEntry_INTERFACE_DEFINED__
#define __IMuiResourceMapEntry_INTERFACE_DEFINED__

/* interface IMuiResourceMapEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IMuiResourceMapEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("397927f5-10f2-4ecb-bfe1-3c264212a193")
    IMuiResourceMapEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ MuiResourceMapEntry **__MIDL_0045) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ResourceTypeIdInt( 
            /* [retval][out] */ IStream **__MIDL_0046) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ResourceTypeIdString( 
            /* [retval][out] */ IStream **__MIDL_0047) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IMuiResourceMapEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IMuiResourceMapEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IMuiResourceMapEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IMuiResourceMapEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IMuiResourceMapEntry * This,
            /* [retval][out] */ MuiResourceMapEntry **__MIDL_0045);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ResourceTypeIdInt )( 
            IMuiResourceMapEntry * This,
            /* [retval][out] */ IStream **__MIDL_0046);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ResourceTypeIdString )( 
            IMuiResourceMapEntry * This,
            /* [retval][out] */ IStream **__MIDL_0047);
        
        END_INTERFACE
    } IMuiResourceMapEntryVtbl;

    interface IMuiResourceMapEntry
    {
        CONST_VTBL struct IMuiResourceMapEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IMuiResourceMapEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IMuiResourceMapEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IMuiResourceMapEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IMuiResourceMapEntry_get_AllData(This,__MIDL_0045)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0045)

#define IMuiResourceMapEntry_get_ResourceTypeIdInt(This,__MIDL_0046)	\
    (This)->lpVtbl -> get_ResourceTypeIdInt(This,__MIDL_0046)

#define IMuiResourceMapEntry_get_ResourceTypeIdString(This,__MIDL_0047)	\
    (This)->lpVtbl -> get_ResourceTypeIdString(This,__MIDL_0047)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IMuiResourceMapEntry_get_AllData_Proxy( 
    IMuiResourceMapEntry * This,
    /* [retval][out] */ MuiResourceMapEntry **__MIDL_0045);


void __RPC_STUB IMuiResourceMapEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMuiResourceMapEntry_get_ResourceTypeIdInt_Proxy( 
    IMuiResourceMapEntry * This,
    /* [retval][out] */ IStream **__MIDL_0046);


void __RPC_STUB IMuiResourceMapEntry_get_ResourceTypeIdInt_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMuiResourceMapEntry_get_ResourceTypeIdString_Proxy( 
    IMuiResourceMapEntry * This,
    /* [retval][out] */ IStream **__MIDL_0047);


void __RPC_STUB IMuiResourceMapEntry_get_ResourceTypeIdString_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IMuiResourceMapEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0284 */
/* [local] */ 

typedef struct _HashElementEntry
    {
    ULONG index;
    UCHAR Transform;
    /* [size_is] */ BYTE *TransformMetadata;
    ULONG TransformMetadataSize;
    UCHAR DigestMethod;
    /* [size_is] */ BYTE *DigestValue;
    ULONG DigestValueSize;
    LPCWSTR Xml;
    } 	HashElementEntry;

typedef 
enum _HashElementEntryFieldId
    {	HashElement_Transform	= 0,
	HashElement_TransformMetadata	= HashElement_Transform + 1,
	HashElement_TransformMetadataSize	= HashElement_TransformMetadata + 1,
	HashElement_DigestMethod	= HashElement_TransformMetadataSize + 1,
	HashElement_DigestValue	= HashElement_DigestMethod + 1,
	HashElement_DigestValueSize	= HashElement_DigestValue + 1,
	HashElement_Xml	= HashElement_DigestValueSize + 1
    } 	HashElementEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0284_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0284_v0_0_s_ifspec;

#ifndef __IHashElementEntry_INTERFACE_DEFINED__
#define __IHashElementEntry_INTERFACE_DEFINED__

/* interface IHashElementEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IHashElementEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("9D46FB70-7B54-4f4f-9331-BA9E87833FF5")
    IHashElementEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ HashElementEntry **__MIDL_0048) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_index( 
            /* [retval][out] */ ULONG *__MIDL_0049) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Transform( 
            /* [retval][out] */ UCHAR *__MIDL_0050) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TransformMetadata( 
            /* [retval][out] */ IStream **__MIDL_0051) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DigestMethod( 
            /* [retval][out] */ UCHAR *__MIDL_0052) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DigestValue( 
            /* [retval][out] */ IStream **__MIDL_0053) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Xml( 
            /* [retval][out] */ LPCWSTR *__MIDL_0054) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IHashElementEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IHashElementEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IHashElementEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IHashElementEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IHashElementEntry * This,
            /* [retval][out] */ HashElementEntry **__MIDL_0048);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_index )( 
            IHashElementEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0049);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Transform )( 
            IHashElementEntry * This,
            /* [retval][out] */ UCHAR *__MIDL_0050);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TransformMetadata )( 
            IHashElementEntry * This,
            /* [retval][out] */ IStream **__MIDL_0051);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DigestMethod )( 
            IHashElementEntry * This,
            /* [retval][out] */ UCHAR *__MIDL_0052);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DigestValue )( 
            IHashElementEntry * This,
            /* [retval][out] */ IStream **__MIDL_0053);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Xml )( 
            IHashElementEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0054);
        
        END_INTERFACE
    } IHashElementEntryVtbl;

    interface IHashElementEntry
    {
        CONST_VTBL struct IHashElementEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IHashElementEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IHashElementEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IHashElementEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IHashElementEntry_get_AllData(This,__MIDL_0048)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0048)

#define IHashElementEntry_get_index(This,__MIDL_0049)	\
    (This)->lpVtbl -> get_index(This,__MIDL_0049)

#define IHashElementEntry_get_Transform(This,__MIDL_0050)	\
    (This)->lpVtbl -> get_Transform(This,__MIDL_0050)

#define IHashElementEntry_get_TransformMetadata(This,__MIDL_0051)	\
    (This)->lpVtbl -> get_TransformMetadata(This,__MIDL_0051)

#define IHashElementEntry_get_DigestMethod(This,__MIDL_0052)	\
    (This)->lpVtbl -> get_DigestMethod(This,__MIDL_0052)

#define IHashElementEntry_get_DigestValue(This,__MIDL_0053)	\
    (This)->lpVtbl -> get_DigestValue(This,__MIDL_0053)

#define IHashElementEntry_get_Xml(This,__MIDL_0054)	\
    (This)->lpVtbl -> get_Xml(This,__MIDL_0054)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IHashElementEntry_get_AllData_Proxy( 
    IHashElementEntry * This,
    /* [retval][out] */ HashElementEntry **__MIDL_0048);


void __RPC_STUB IHashElementEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IHashElementEntry_get_index_Proxy( 
    IHashElementEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0049);


void __RPC_STUB IHashElementEntry_get_index_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IHashElementEntry_get_Transform_Proxy( 
    IHashElementEntry * This,
    /* [retval][out] */ UCHAR *__MIDL_0050);


void __RPC_STUB IHashElementEntry_get_Transform_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IHashElementEntry_get_TransformMetadata_Proxy( 
    IHashElementEntry * This,
    /* [retval][out] */ IStream **__MIDL_0051);


void __RPC_STUB IHashElementEntry_get_TransformMetadata_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IHashElementEntry_get_DigestMethod_Proxy( 
    IHashElementEntry * This,
    /* [retval][out] */ UCHAR *__MIDL_0052);


void __RPC_STUB IHashElementEntry_get_DigestMethod_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IHashElementEntry_get_DigestValue_Proxy( 
    IHashElementEntry * This,
    /* [retval][out] */ IStream **__MIDL_0053);


void __RPC_STUB IHashElementEntry_get_DigestValue_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IHashElementEntry_get_Xml_Proxy( 
    IHashElementEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0054);


void __RPC_STUB IHashElementEntry_get_Xml_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IHashElementEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0285 */
/* [local] */ 

typedef struct _FileEntry
    {
    LPCWSTR Name;
    ULONG HashAlgorithm;
    LPCWSTR LoadFrom;
    LPCWSTR SourcePath;
    LPCWSTR ImportPath;
    LPCWSTR SourceName;
    LPCWSTR Location;
    /* [size_is] */ BYTE *HashValue;
    ULONG HashValueSize;
    ULONGLONG Size;
    LPCWSTR Group;
    ULONG Flags;
    MuiResourceMapEntry MuiMapping;
    ULONG WritableType;
    ISection *HashElements;
    } 	FileEntry;

typedef 
enum _FileEntryFieldId
    {	File_HashAlgorithm	= 0,
	File_LoadFrom	= File_HashAlgorithm + 1,
	File_SourcePath	= File_LoadFrom + 1,
	File_ImportPath	= File_SourcePath + 1,
	File_SourceName	= File_ImportPath + 1,
	File_Location	= File_SourceName + 1,
	File_HashValue	= File_Location + 1,
	File_HashValueSize	= File_HashValue + 1,
	File_Size	= File_HashValueSize + 1,
	File_Group	= File_Size + 1,
	File_Flags	= File_Group + 1,
	File_MuiMapping	= File_Flags + 1,
	File_WritableType	= File_MuiMapping + 1,
	File_HashElements	= File_WritableType + 1
    } 	FileEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0285_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0285_v0_0_s_ifspec;

#ifndef __IFileEntry_INTERFACE_DEFINED__
#define __IFileEntry_INTERFACE_DEFINED__

/* interface IFileEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IFileEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("A2A55FAD-349B-469b-BF12-ADC33D14A937")
    IFileEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ FileEntry **__MIDL_0055) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Name( 
            /* [retval][out] */ LPCWSTR *__MIDL_0056) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_HashAlgorithm( 
            /* [retval][out] */ ULONG *__MIDL_0057) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LoadFrom( 
            /* [retval][out] */ LPCWSTR *__MIDL_0058) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SourcePath( 
            /* [retval][out] */ LPCWSTR *__MIDL_0059) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ImportPath( 
            /* [retval][out] */ LPCWSTR *__MIDL_0060) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SourceName( 
            /* [retval][out] */ LPCWSTR *__MIDL_0061) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Location( 
            /* [retval][out] */ LPCWSTR *__MIDL_0062) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_HashValue( 
            /* [retval][out] */ IStream **__MIDL_0063) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Size( 
            /* [retval][out] */ ULONGLONG *__MIDL_0064) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Group( 
            /* [retval][out] */ LPCWSTR *__MIDL_0065) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Flags( 
            /* [retval][out] */ ULONG *__MIDL_0066) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MuiMapping( 
            /* [retval][out] */ IMuiResourceMapEntry **__MIDL_0067) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_WritableType( 
            /* [retval][out] */ ULONG *__MIDL_0068) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_HashElements( 
            /* [retval][out] */ ISection **HashElement) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IFileEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IFileEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IFileEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IFileEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IFileEntry * This,
            /* [retval][out] */ FileEntry **__MIDL_0055);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Name )( 
            IFileEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0056);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_HashAlgorithm )( 
            IFileEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0057);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_LoadFrom )( 
            IFileEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0058);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SourcePath )( 
            IFileEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0059);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ImportPath )( 
            IFileEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0060);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SourceName )( 
            IFileEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0061);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Location )( 
            IFileEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0062);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_HashValue )( 
            IFileEntry * This,
            /* [retval][out] */ IStream **__MIDL_0063);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Size )( 
            IFileEntry * This,
            /* [retval][out] */ ULONGLONG *__MIDL_0064);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Group )( 
            IFileEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0065);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Flags )( 
            IFileEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0066);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MuiMapping )( 
            IFileEntry * This,
            /* [retval][out] */ IMuiResourceMapEntry **__MIDL_0067);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_WritableType )( 
            IFileEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0068);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_HashElements )( 
            IFileEntry * This,
            /* [retval][out] */ ISection **HashElement);
        
        END_INTERFACE
    } IFileEntryVtbl;

    interface IFileEntry
    {
        CONST_VTBL struct IFileEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IFileEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IFileEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IFileEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IFileEntry_get_AllData(This,__MIDL_0055)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0055)

#define IFileEntry_get_Name(This,__MIDL_0056)	\
    (This)->lpVtbl -> get_Name(This,__MIDL_0056)

#define IFileEntry_get_HashAlgorithm(This,__MIDL_0057)	\
    (This)->lpVtbl -> get_HashAlgorithm(This,__MIDL_0057)

#define IFileEntry_get_LoadFrom(This,__MIDL_0058)	\
    (This)->lpVtbl -> get_LoadFrom(This,__MIDL_0058)

#define IFileEntry_get_SourcePath(This,__MIDL_0059)	\
    (This)->lpVtbl -> get_SourcePath(This,__MIDL_0059)

#define IFileEntry_get_ImportPath(This,__MIDL_0060)	\
    (This)->lpVtbl -> get_ImportPath(This,__MIDL_0060)

#define IFileEntry_get_SourceName(This,__MIDL_0061)	\
    (This)->lpVtbl -> get_SourceName(This,__MIDL_0061)

#define IFileEntry_get_Location(This,__MIDL_0062)	\
    (This)->lpVtbl -> get_Location(This,__MIDL_0062)

#define IFileEntry_get_HashValue(This,__MIDL_0063)	\
    (This)->lpVtbl -> get_HashValue(This,__MIDL_0063)

#define IFileEntry_get_Size(This,__MIDL_0064)	\
    (This)->lpVtbl -> get_Size(This,__MIDL_0064)

#define IFileEntry_get_Group(This,__MIDL_0065)	\
    (This)->lpVtbl -> get_Group(This,__MIDL_0065)

#define IFileEntry_get_Flags(This,__MIDL_0066)	\
    (This)->lpVtbl -> get_Flags(This,__MIDL_0066)

#define IFileEntry_get_MuiMapping(This,__MIDL_0067)	\
    (This)->lpVtbl -> get_MuiMapping(This,__MIDL_0067)

#define IFileEntry_get_WritableType(This,__MIDL_0068)	\
    (This)->lpVtbl -> get_WritableType(This,__MIDL_0068)

#define IFileEntry_get_HashElements(This,HashElement)	\
    (This)->lpVtbl -> get_HashElements(This,HashElement)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IFileEntry_get_AllData_Proxy( 
    IFileEntry * This,
    /* [retval][out] */ FileEntry **__MIDL_0055);


void __RPC_STUB IFileEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFileEntry_get_Name_Proxy( 
    IFileEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0056);


void __RPC_STUB IFileEntry_get_Name_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFileEntry_get_HashAlgorithm_Proxy( 
    IFileEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0057);


void __RPC_STUB IFileEntry_get_HashAlgorithm_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFileEntry_get_LoadFrom_Proxy( 
    IFileEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0058);


void __RPC_STUB IFileEntry_get_LoadFrom_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFileEntry_get_SourcePath_Proxy( 
    IFileEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0059);


void __RPC_STUB IFileEntry_get_SourcePath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFileEntry_get_ImportPath_Proxy( 
    IFileEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0060);


void __RPC_STUB IFileEntry_get_ImportPath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFileEntry_get_SourceName_Proxy( 
    IFileEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0061);


void __RPC_STUB IFileEntry_get_SourceName_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFileEntry_get_Location_Proxy( 
    IFileEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0062);


void __RPC_STUB IFileEntry_get_Location_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFileEntry_get_HashValue_Proxy( 
    IFileEntry * This,
    /* [retval][out] */ IStream **__MIDL_0063);


void __RPC_STUB IFileEntry_get_HashValue_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFileEntry_get_Size_Proxy( 
    IFileEntry * This,
    /* [retval][out] */ ULONGLONG *__MIDL_0064);


void __RPC_STUB IFileEntry_get_Size_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFileEntry_get_Group_Proxy( 
    IFileEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0065);


void __RPC_STUB IFileEntry_get_Group_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFileEntry_get_Flags_Proxy( 
    IFileEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0066);


void __RPC_STUB IFileEntry_get_Flags_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFileEntry_get_MuiMapping_Proxy( 
    IFileEntry * This,
    /* [retval][out] */ IMuiResourceMapEntry **__MIDL_0067);


void __RPC_STUB IFileEntry_get_MuiMapping_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFileEntry_get_WritableType_Proxy( 
    IFileEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0068);


void __RPC_STUB IFileEntry_get_WritableType_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFileEntry_get_HashElements_Proxy( 
    IFileEntry * This,
    /* [retval][out] */ ISection **HashElement);


void __RPC_STUB IFileEntry_get_HashElements_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IFileEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0286 */
/* [local] */ 

typedef struct _FileAssociationEntry
    {
    LPCWSTR Extension;
    LPCWSTR Description;
    LPCWSTR ProgID;
    LPCWSTR DefaultIcon;
    LPCWSTR Parameter;
    } 	FileAssociationEntry;

typedef 
enum _FileAssociationEntryFieldId
    {	FileAssociation_Description	= 0,
	FileAssociation_ProgID	= FileAssociation_Description + 1,
	FileAssociation_DefaultIcon	= FileAssociation_ProgID + 1,
	FileAssociation_Parameter	= FileAssociation_DefaultIcon + 1
    } 	FileAssociationEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0286_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0286_v0_0_s_ifspec;

#ifndef __IFileAssociationEntry_INTERFACE_DEFINED__
#define __IFileAssociationEntry_INTERFACE_DEFINED__

/* interface IFileAssociationEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IFileAssociationEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0C66F299-E08E-48c5-9264-7CCBEB4D5CBB")
    IFileAssociationEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ FileAssociationEntry **__MIDL_0069) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Extension( 
            /* [retval][out] */ LPCWSTR *__MIDL_0070) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Description( 
            /* [retval][out] */ LPCWSTR *__MIDL_0071) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ProgID( 
            /* [retval][out] */ LPCWSTR *__MIDL_0072) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DefaultIcon( 
            /* [retval][out] */ LPCWSTR *__MIDL_0073) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Parameter( 
            /* [retval][out] */ LPCWSTR *__MIDL_0074) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IFileAssociationEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IFileAssociationEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IFileAssociationEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IFileAssociationEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IFileAssociationEntry * This,
            /* [retval][out] */ FileAssociationEntry **__MIDL_0069);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Extension )( 
            IFileAssociationEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0070);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Description )( 
            IFileAssociationEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0071);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ProgID )( 
            IFileAssociationEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0072);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DefaultIcon )( 
            IFileAssociationEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0073);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Parameter )( 
            IFileAssociationEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0074);
        
        END_INTERFACE
    } IFileAssociationEntryVtbl;

    interface IFileAssociationEntry
    {
        CONST_VTBL struct IFileAssociationEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IFileAssociationEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IFileAssociationEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IFileAssociationEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IFileAssociationEntry_get_AllData(This,__MIDL_0069)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0069)

#define IFileAssociationEntry_get_Extension(This,__MIDL_0070)	\
    (This)->lpVtbl -> get_Extension(This,__MIDL_0070)

#define IFileAssociationEntry_get_Description(This,__MIDL_0071)	\
    (This)->lpVtbl -> get_Description(This,__MIDL_0071)

#define IFileAssociationEntry_get_ProgID(This,__MIDL_0072)	\
    (This)->lpVtbl -> get_ProgID(This,__MIDL_0072)

#define IFileAssociationEntry_get_DefaultIcon(This,__MIDL_0073)	\
    (This)->lpVtbl -> get_DefaultIcon(This,__MIDL_0073)

#define IFileAssociationEntry_get_Parameter(This,__MIDL_0074)	\
    (This)->lpVtbl -> get_Parameter(This,__MIDL_0074)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IFileAssociationEntry_get_AllData_Proxy( 
    IFileAssociationEntry * This,
    /* [retval][out] */ FileAssociationEntry **__MIDL_0069);


void __RPC_STUB IFileAssociationEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFileAssociationEntry_get_Extension_Proxy( 
    IFileAssociationEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0070);


void __RPC_STUB IFileAssociationEntry_get_Extension_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFileAssociationEntry_get_Description_Proxy( 
    IFileAssociationEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0071);


void __RPC_STUB IFileAssociationEntry_get_Description_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFileAssociationEntry_get_ProgID_Proxy( 
    IFileAssociationEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0072);


void __RPC_STUB IFileAssociationEntry_get_ProgID_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFileAssociationEntry_get_DefaultIcon_Proxy( 
    IFileAssociationEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0073);


void __RPC_STUB IFileAssociationEntry_get_DefaultIcon_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFileAssociationEntry_get_Parameter_Proxy( 
    IFileAssociationEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0074);


void __RPC_STUB IFileAssociationEntry_get_Parameter_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IFileAssociationEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0287 */
/* [local] */ 

typedef struct _CategoryMembershipDataEntry
    {
    ULONG index;
    LPCWSTR Xml;
    LPCWSTR Description;
    } 	CategoryMembershipDataEntry;

typedef 
enum _CategoryMembershipDataEntryFieldId
    {	CategoryMembershipData_Xml	= 0,
	CategoryMembershipData_Description	= CategoryMembershipData_Xml + 1
    } 	CategoryMembershipDataEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0287_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0287_v0_0_s_ifspec;

#ifndef __ICategoryMembershipDataEntry_INTERFACE_DEFINED__
#define __ICategoryMembershipDataEntry_INTERFACE_DEFINED__

/* interface ICategoryMembershipDataEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_ICategoryMembershipDataEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("DA0C3B27-6B6B-4b80-A8F8-6CE14F4BC0A4")
    ICategoryMembershipDataEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ CategoryMembershipDataEntry **__MIDL_0075) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_index( 
            /* [retval][out] */ ULONG *__MIDL_0076) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Xml( 
            /* [retval][out] */ LPCWSTR *__MIDL_0077) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Description( 
            /* [retval][out] */ LPCWSTR *__MIDL_0078) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ICategoryMembershipDataEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICategoryMembershipDataEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICategoryMembershipDataEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICategoryMembershipDataEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            ICategoryMembershipDataEntry * This,
            /* [retval][out] */ CategoryMembershipDataEntry **__MIDL_0075);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_index )( 
            ICategoryMembershipDataEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0076);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Xml )( 
            ICategoryMembershipDataEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0077);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Description )( 
            ICategoryMembershipDataEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0078);
        
        END_INTERFACE
    } ICategoryMembershipDataEntryVtbl;

    interface ICategoryMembershipDataEntry
    {
        CONST_VTBL struct ICategoryMembershipDataEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICategoryMembershipDataEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ICategoryMembershipDataEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ICategoryMembershipDataEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ICategoryMembershipDataEntry_get_AllData(This,__MIDL_0075)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0075)

#define ICategoryMembershipDataEntry_get_index(This,__MIDL_0076)	\
    (This)->lpVtbl -> get_index(This,__MIDL_0076)

#define ICategoryMembershipDataEntry_get_Xml(This,__MIDL_0077)	\
    (This)->lpVtbl -> get_Xml(This,__MIDL_0077)

#define ICategoryMembershipDataEntry_get_Description(This,__MIDL_0078)	\
    (This)->lpVtbl -> get_Description(This,__MIDL_0078)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ICategoryMembershipDataEntry_get_AllData_Proxy( 
    ICategoryMembershipDataEntry * This,
    /* [retval][out] */ CategoryMembershipDataEntry **__MIDL_0075);


void __RPC_STUB ICategoryMembershipDataEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICategoryMembershipDataEntry_get_index_Proxy( 
    ICategoryMembershipDataEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0076);


void __RPC_STUB ICategoryMembershipDataEntry_get_index_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICategoryMembershipDataEntry_get_Xml_Proxy( 
    ICategoryMembershipDataEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0077);


void __RPC_STUB ICategoryMembershipDataEntry_get_Xml_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICategoryMembershipDataEntry_get_Description_Proxy( 
    ICategoryMembershipDataEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0078);


void __RPC_STUB ICategoryMembershipDataEntry_get_Description_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ICategoryMembershipDataEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0288 */
/* [local] */ 

typedef struct _SubcategoryMembershipEntry
    {
    LPCWSTR Subcategory;
    ISection *CategoryMembershipData;
    } 	SubcategoryMembershipEntry;

typedef 
enum _SubcategoryMembershipEntryFieldId
    {	SubcategoryMembership_CategoryMembershipData	= 0
    } 	SubcategoryMembershipEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0288_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0288_v0_0_s_ifspec;

#ifndef __ISubcategoryMembershipEntry_INTERFACE_DEFINED__
#define __ISubcategoryMembershipEntry_INTERFACE_DEFINED__

/* interface ISubcategoryMembershipEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_ISubcategoryMembershipEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("5A7A54D7-5AD5-418e-AB7A-CF823A8D48D0")
    ISubcategoryMembershipEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ SubcategoryMembershipEntry **__MIDL_0079) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Subcategory( 
            /* [retval][out] */ LPCWSTR *__MIDL_0080) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CategoryMembershipData( 
            /* [retval][out] */ ISection **CategoryMembershipData) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ISubcategoryMembershipEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISubcategoryMembershipEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISubcategoryMembershipEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISubcategoryMembershipEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            ISubcategoryMembershipEntry * This,
            /* [retval][out] */ SubcategoryMembershipEntry **__MIDL_0079);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Subcategory )( 
            ISubcategoryMembershipEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0080);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CategoryMembershipData )( 
            ISubcategoryMembershipEntry * This,
            /* [retval][out] */ ISection **CategoryMembershipData);
        
        END_INTERFACE
    } ISubcategoryMembershipEntryVtbl;

    interface ISubcategoryMembershipEntry
    {
        CONST_VTBL struct ISubcategoryMembershipEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISubcategoryMembershipEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ISubcategoryMembershipEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ISubcategoryMembershipEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ISubcategoryMembershipEntry_get_AllData(This,__MIDL_0079)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0079)

#define ISubcategoryMembershipEntry_get_Subcategory(This,__MIDL_0080)	\
    (This)->lpVtbl -> get_Subcategory(This,__MIDL_0080)

#define ISubcategoryMembershipEntry_get_CategoryMembershipData(This,CategoryMembershipData)	\
    (This)->lpVtbl -> get_CategoryMembershipData(This,CategoryMembershipData)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ISubcategoryMembershipEntry_get_AllData_Proxy( 
    ISubcategoryMembershipEntry * This,
    /* [retval][out] */ SubcategoryMembershipEntry **__MIDL_0079);


void __RPC_STUB ISubcategoryMembershipEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISubcategoryMembershipEntry_get_Subcategory_Proxy( 
    ISubcategoryMembershipEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0080);


void __RPC_STUB ISubcategoryMembershipEntry_get_Subcategory_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISubcategoryMembershipEntry_get_CategoryMembershipData_Proxy( 
    ISubcategoryMembershipEntry * This,
    /* [retval][out] */ ISection **CategoryMembershipData);


void __RPC_STUB ISubcategoryMembershipEntry_get_CategoryMembershipData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ISubcategoryMembershipEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0289 */
/* [local] */ 

typedef struct _CategoryMembershipEntry
    {
    IDefinitionIdentity *Identity;
    ISection *SubcategoryMembership;
    } 	CategoryMembershipEntry;

typedef 
enum _CategoryMembershipEntryFieldId
    {	CategoryMembership_SubcategoryMembership	= 0
    } 	CategoryMembershipEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0289_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0289_v0_0_s_ifspec;

#ifndef __ICategoryMembershipEntry_INTERFACE_DEFINED__
#define __ICategoryMembershipEntry_INTERFACE_DEFINED__

/* interface ICategoryMembershipEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_ICategoryMembershipEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("97FDCA77-B6F2-4718-A1EB-29D0AECE9C03")
    ICategoryMembershipEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ CategoryMembershipEntry **__MIDL_0081) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Identity( 
            /* [retval][out] */ IDefinitionIdentity **__MIDL_0082) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SubcategoryMembership( 
            /* [retval][out] */ ISection **SubcategoryMembership) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ICategoryMembershipEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICategoryMembershipEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICategoryMembershipEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICategoryMembershipEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            ICategoryMembershipEntry * This,
            /* [retval][out] */ CategoryMembershipEntry **__MIDL_0081);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Identity )( 
            ICategoryMembershipEntry * This,
            /* [retval][out] */ IDefinitionIdentity **__MIDL_0082);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SubcategoryMembership )( 
            ICategoryMembershipEntry * This,
            /* [retval][out] */ ISection **SubcategoryMembership);
        
        END_INTERFACE
    } ICategoryMembershipEntryVtbl;

    interface ICategoryMembershipEntry
    {
        CONST_VTBL struct ICategoryMembershipEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICategoryMembershipEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ICategoryMembershipEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ICategoryMembershipEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ICategoryMembershipEntry_get_AllData(This,__MIDL_0081)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0081)

#define ICategoryMembershipEntry_get_Identity(This,__MIDL_0082)	\
    (This)->lpVtbl -> get_Identity(This,__MIDL_0082)

#define ICategoryMembershipEntry_get_SubcategoryMembership(This,SubcategoryMembership)	\
    (This)->lpVtbl -> get_SubcategoryMembership(This,SubcategoryMembership)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ICategoryMembershipEntry_get_AllData_Proxy( 
    ICategoryMembershipEntry * This,
    /* [retval][out] */ CategoryMembershipEntry **__MIDL_0081);


void __RPC_STUB ICategoryMembershipEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICategoryMembershipEntry_get_Identity_Proxy( 
    ICategoryMembershipEntry * This,
    /* [retval][out] */ IDefinitionIdentity **__MIDL_0082);


void __RPC_STUB ICategoryMembershipEntry_get_Identity_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICategoryMembershipEntry_get_SubcategoryMembership_Proxy( 
    ICategoryMembershipEntry * This,
    /* [retval][out] */ ISection **SubcategoryMembership);


void __RPC_STUB ICategoryMembershipEntry_get_SubcategoryMembership_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ICategoryMembershipEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0290 */
/* [local] */ 

typedef struct _COMServerEntry
    {
    GUID Clsid;
    ULONG Flags;
    GUID ConfiguredGuid;
    GUID ImplementedClsid;
    GUID TypeLibrary;
    ULONG ThreadingModel;
    LPCWSTR RuntimeVersion;
    LPCWSTR HostFile;
    } 	COMServerEntry;

typedef 
enum _COMServerEntryFieldId
    {	COMServer_Flags	= 0,
	COMServer_ConfiguredGuid	= COMServer_Flags + 1,
	COMServer_ImplementedClsid	= COMServer_ConfiguredGuid + 1,
	COMServer_TypeLibrary	= COMServer_ImplementedClsid + 1,
	COMServer_ThreadingModel	= COMServer_TypeLibrary + 1,
	COMServer_RuntimeVersion	= COMServer_ThreadingModel + 1,
	COMServer_HostFile	= COMServer_RuntimeVersion + 1
    } 	COMServerEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0290_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0290_v0_0_s_ifspec;

#ifndef __ICOMServerEntry_INTERFACE_DEFINED__
#define __ICOMServerEntry_INTERFACE_DEFINED__

/* interface ICOMServerEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_ICOMServerEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("3903B11B-FBE8-477c-825F-DB828B5FD174")
    ICOMServerEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ COMServerEntry **__MIDL_0083) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Clsid( 
            /* [retval][out] */ GUID *__MIDL_0084) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Flags( 
            /* [retval][out] */ ULONG *__MIDL_0085) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ConfiguredGuid( 
            /* [retval][out] */ GUID *__MIDL_0086) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ImplementedClsid( 
            /* [retval][out] */ GUID *__MIDL_0087) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TypeLibrary( 
            /* [retval][out] */ GUID *__MIDL_0088) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ThreadingModel( 
            /* [retval][out] */ ULONG *__MIDL_0089) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RuntimeVersion( 
            /* [retval][out] */ LPCWSTR *__MIDL_0090) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_HostFile( 
            /* [retval][out] */ LPCWSTR *__MIDL_0091) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ICOMServerEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICOMServerEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICOMServerEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICOMServerEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            ICOMServerEntry * This,
            /* [retval][out] */ COMServerEntry **__MIDL_0083);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Clsid )( 
            ICOMServerEntry * This,
            /* [retval][out] */ GUID *__MIDL_0084);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Flags )( 
            ICOMServerEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0085);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ConfiguredGuid )( 
            ICOMServerEntry * This,
            /* [retval][out] */ GUID *__MIDL_0086);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ImplementedClsid )( 
            ICOMServerEntry * This,
            /* [retval][out] */ GUID *__MIDL_0087);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TypeLibrary )( 
            ICOMServerEntry * This,
            /* [retval][out] */ GUID *__MIDL_0088);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ThreadingModel )( 
            ICOMServerEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0089);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RuntimeVersion )( 
            ICOMServerEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0090);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_HostFile )( 
            ICOMServerEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0091);
        
        END_INTERFACE
    } ICOMServerEntryVtbl;

    interface ICOMServerEntry
    {
        CONST_VTBL struct ICOMServerEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICOMServerEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ICOMServerEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ICOMServerEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ICOMServerEntry_get_AllData(This,__MIDL_0083)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0083)

#define ICOMServerEntry_get_Clsid(This,__MIDL_0084)	\
    (This)->lpVtbl -> get_Clsid(This,__MIDL_0084)

#define ICOMServerEntry_get_Flags(This,__MIDL_0085)	\
    (This)->lpVtbl -> get_Flags(This,__MIDL_0085)

#define ICOMServerEntry_get_ConfiguredGuid(This,__MIDL_0086)	\
    (This)->lpVtbl -> get_ConfiguredGuid(This,__MIDL_0086)

#define ICOMServerEntry_get_ImplementedClsid(This,__MIDL_0087)	\
    (This)->lpVtbl -> get_ImplementedClsid(This,__MIDL_0087)

#define ICOMServerEntry_get_TypeLibrary(This,__MIDL_0088)	\
    (This)->lpVtbl -> get_TypeLibrary(This,__MIDL_0088)

#define ICOMServerEntry_get_ThreadingModel(This,__MIDL_0089)	\
    (This)->lpVtbl -> get_ThreadingModel(This,__MIDL_0089)

#define ICOMServerEntry_get_RuntimeVersion(This,__MIDL_0090)	\
    (This)->lpVtbl -> get_RuntimeVersion(This,__MIDL_0090)

#define ICOMServerEntry_get_HostFile(This,__MIDL_0091)	\
    (This)->lpVtbl -> get_HostFile(This,__MIDL_0091)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ICOMServerEntry_get_AllData_Proxy( 
    ICOMServerEntry * This,
    /* [retval][out] */ COMServerEntry **__MIDL_0083);


void __RPC_STUB ICOMServerEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICOMServerEntry_get_Clsid_Proxy( 
    ICOMServerEntry * This,
    /* [retval][out] */ GUID *__MIDL_0084);


void __RPC_STUB ICOMServerEntry_get_Clsid_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICOMServerEntry_get_Flags_Proxy( 
    ICOMServerEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0085);


void __RPC_STUB ICOMServerEntry_get_Flags_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICOMServerEntry_get_ConfiguredGuid_Proxy( 
    ICOMServerEntry * This,
    /* [retval][out] */ GUID *__MIDL_0086);


void __RPC_STUB ICOMServerEntry_get_ConfiguredGuid_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICOMServerEntry_get_ImplementedClsid_Proxy( 
    ICOMServerEntry * This,
    /* [retval][out] */ GUID *__MIDL_0087);


void __RPC_STUB ICOMServerEntry_get_ImplementedClsid_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICOMServerEntry_get_TypeLibrary_Proxy( 
    ICOMServerEntry * This,
    /* [retval][out] */ GUID *__MIDL_0088);


void __RPC_STUB ICOMServerEntry_get_TypeLibrary_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICOMServerEntry_get_ThreadingModel_Proxy( 
    ICOMServerEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0089);


void __RPC_STUB ICOMServerEntry_get_ThreadingModel_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICOMServerEntry_get_RuntimeVersion_Proxy( 
    ICOMServerEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0090);


void __RPC_STUB ICOMServerEntry_get_RuntimeVersion_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICOMServerEntry_get_HostFile_Proxy( 
    ICOMServerEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0091);


void __RPC_STUB ICOMServerEntry_get_HostFile_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ICOMServerEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0291 */
/* [local] */ 

typedef struct _ProgIdRedirectionEntry
    {
    LPCWSTR ProgId;
    GUID RedirectedGuid;
    } 	ProgIdRedirectionEntry;

typedef 
enum _ProgIdRedirectionEntryFieldId
    {	ProgIdRedirection_RedirectedGuid	= 0
    } 	ProgIdRedirectionEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0291_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0291_v0_0_s_ifspec;

#ifndef __IProgIdRedirectionEntry_INTERFACE_DEFINED__
#define __IProgIdRedirectionEntry_INTERFACE_DEFINED__

/* interface IProgIdRedirectionEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IProgIdRedirectionEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("54F198EC-A63A-45ea-A984-452F68D9B35B")
    IProgIdRedirectionEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ ProgIdRedirectionEntry **__MIDL_0092) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ProgId( 
            /* [retval][out] */ LPCWSTR *__MIDL_0093) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RedirectedGuid( 
            /* [retval][out] */ GUID *__MIDL_0094) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IProgIdRedirectionEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IProgIdRedirectionEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IProgIdRedirectionEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IProgIdRedirectionEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IProgIdRedirectionEntry * This,
            /* [retval][out] */ ProgIdRedirectionEntry **__MIDL_0092);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ProgId )( 
            IProgIdRedirectionEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0093);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RedirectedGuid )( 
            IProgIdRedirectionEntry * This,
            /* [retval][out] */ GUID *__MIDL_0094);
        
        END_INTERFACE
    } IProgIdRedirectionEntryVtbl;

    interface IProgIdRedirectionEntry
    {
        CONST_VTBL struct IProgIdRedirectionEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IProgIdRedirectionEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IProgIdRedirectionEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IProgIdRedirectionEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IProgIdRedirectionEntry_get_AllData(This,__MIDL_0092)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0092)

#define IProgIdRedirectionEntry_get_ProgId(This,__MIDL_0093)	\
    (This)->lpVtbl -> get_ProgId(This,__MIDL_0093)

#define IProgIdRedirectionEntry_get_RedirectedGuid(This,__MIDL_0094)	\
    (This)->lpVtbl -> get_RedirectedGuid(This,__MIDL_0094)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IProgIdRedirectionEntry_get_AllData_Proxy( 
    IProgIdRedirectionEntry * This,
    /* [retval][out] */ ProgIdRedirectionEntry **__MIDL_0092);


void __RPC_STUB IProgIdRedirectionEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IProgIdRedirectionEntry_get_ProgId_Proxy( 
    IProgIdRedirectionEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0093);


void __RPC_STUB IProgIdRedirectionEntry_get_ProgId_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IProgIdRedirectionEntry_get_RedirectedGuid_Proxy( 
    IProgIdRedirectionEntry * This,
    /* [retval][out] */ GUID *__MIDL_0094);


void __RPC_STUB IProgIdRedirectionEntry_get_RedirectedGuid_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IProgIdRedirectionEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0292 */
/* [local] */ 

typedef struct _CLRSurrogateEntry
    {
    GUID Clsid;
    LPCWSTR RuntimeVersion;
    LPCWSTR ClassName;
    } 	CLRSurrogateEntry;

typedef 
enum _CLRSurrogateEntryFieldId
    {	CLRSurrogate_RuntimeVersion	= 0,
	CLRSurrogate_ClassName	= CLRSurrogate_RuntimeVersion + 1
    } 	CLRSurrogateEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0292_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0292_v0_0_s_ifspec;

#ifndef __ICLRSurrogateEntry_INTERFACE_DEFINED__
#define __ICLRSurrogateEntry_INTERFACE_DEFINED__

/* interface ICLRSurrogateEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_ICLRSurrogateEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("1E0422A1-F0D2-44ae-914B-8A2DECCFD22B")
    ICLRSurrogateEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ CLRSurrogateEntry **__MIDL_0095) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Clsid( 
            /* [retval][out] */ GUID *__MIDL_0096) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RuntimeVersion( 
            /* [retval][out] */ LPCWSTR *__MIDL_0097) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ClassName( 
            /* [retval][out] */ LPCWSTR *__MIDL_0098) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ICLRSurrogateEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICLRSurrogateEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICLRSurrogateEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICLRSurrogateEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            ICLRSurrogateEntry * This,
            /* [retval][out] */ CLRSurrogateEntry **__MIDL_0095);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Clsid )( 
            ICLRSurrogateEntry * This,
            /* [retval][out] */ GUID *__MIDL_0096);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RuntimeVersion )( 
            ICLRSurrogateEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0097);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ClassName )( 
            ICLRSurrogateEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0098);
        
        END_INTERFACE
    } ICLRSurrogateEntryVtbl;

    interface ICLRSurrogateEntry
    {
        CONST_VTBL struct ICLRSurrogateEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICLRSurrogateEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ICLRSurrogateEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ICLRSurrogateEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ICLRSurrogateEntry_get_AllData(This,__MIDL_0095)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0095)

#define ICLRSurrogateEntry_get_Clsid(This,__MIDL_0096)	\
    (This)->lpVtbl -> get_Clsid(This,__MIDL_0096)

#define ICLRSurrogateEntry_get_RuntimeVersion(This,__MIDL_0097)	\
    (This)->lpVtbl -> get_RuntimeVersion(This,__MIDL_0097)

#define ICLRSurrogateEntry_get_ClassName(This,__MIDL_0098)	\
    (This)->lpVtbl -> get_ClassName(This,__MIDL_0098)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ICLRSurrogateEntry_get_AllData_Proxy( 
    ICLRSurrogateEntry * This,
    /* [retval][out] */ CLRSurrogateEntry **__MIDL_0095);


void __RPC_STUB ICLRSurrogateEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICLRSurrogateEntry_get_Clsid_Proxy( 
    ICLRSurrogateEntry * This,
    /* [retval][out] */ GUID *__MIDL_0096);


void __RPC_STUB ICLRSurrogateEntry_get_Clsid_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICLRSurrogateEntry_get_RuntimeVersion_Proxy( 
    ICLRSurrogateEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0097);


void __RPC_STUB ICLRSurrogateEntry_get_RuntimeVersion_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICLRSurrogateEntry_get_ClassName_Proxy( 
    ICLRSurrogateEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0098);


void __RPC_STUB ICLRSurrogateEntry_get_ClassName_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ICLRSurrogateEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0293 */
/* [local] */ 

typedef struct _AssemblyReferenceDependentAssemblyEntry
    {
    LPCWSTR Group;
    LPCWSTR Codebase;
    ULONGLONG Size;
    /* [size_is] */ BYTE *HashValue;
    ULONG HashValueSize;
    ULONG HashAlgorithm;
    ULONG Flags;
    LPCWSTR ResourceFallbackCulture;
    LPCWSTR Description;
    LPCWSTR SupportUrl;
    ISection *HashElements;
    } 	AssemblyReferenceDependentAssemblyEntry;

typedef 
enum _AssemblyReferenceDependentAssemblyEntryFieldId
    {	AssemblyReferenceDependentAssembly_Group	= 0,
	AssemblyReferenceDependentAssembly_Codebase	= AssemblyReferenceDependentAssembly_Group + 1,
	AssemblyReferenceDependentAssembly_Size	= AssemblyReferenceDependentAssembly_Codebase + 1,
	AssemblyReferenceDependentAssembly_HashValue	= AssemblyReferenceDependentAssembly_Size + 1,
	AssemblyReferenceDependentAssembly_HashValueSize	= AssemblyReferenceDependentAssembly_HashValue + 1,
	AssemblyReferenceDependentAssembly_HashAlgorithm	= AssemblyReferenceDependentAssembly_HashValueSize + 1,
	AssemblyReferenceDependentAssembly_Flags	= AssemblyReferenceDependentAssembly_HashAlgorithm + 1,
	AssemblyReferenceDependentAssembly_ResourceFallbackCulture	= AssemblyReferenceDependentAssembly_Flags + 1,
	AssemblyReferenceDependentAssembly_Description	= AssemblyReferenceDependentAssembly_ResourceFallbackCulture + 1,
	AssemblyReferenceDependentAssembly_SupportUrl	= AssemblyReferenceDependentAssembly_Description + 1,
	AssemblyReferenceDependentAssembly_HashElements	= AssemblyReferenceDependentAssembly_SupportUrl + 1
    } 	AssemblyReferenceDependentAssemblyEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0293_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0293_v0_0_s_ifspec;

#ifndef __IAssemblyReferenceDependentAssemblyEntry_INTERFACE_DEFINED__
#define __IAssemblyReferenceDependentAssemblyEntry_INTERFACE_DEFINED__

/* interface IAssemblyReferenceDependentAssemblyEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IAssemblyReferenceDependentAssemblyEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("C31FF59E-CD25-47b8-9EF3-CF4433EB97CC")
    IAssemblyReferenceDependentAssemblyEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ AssemblyReferenceDependentAssemblyEntry **__MIDL_0099) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Group( 
            /* [retval][out] */ LPCWSTR *__MIDL_0100) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Codebase( 
            /* [retval][out] */ LPCWSTR *__MIDL_0101) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Size( 
            /* [retval][out] */ ULONGLONG *__MIDL_0102) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_HashValue( 
            /* [retval][out] */ IStream **__MIDL_0103) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_HashAlgorithm( 
            /* [retval][out] */ ULONG *__MIDL_0104) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Flags( 
            /* [retval][out] */ ULONG *__MIDL_0105) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ResourceFallbackCulture( 
            /* [retval][out] */ LPCWSTR *__MIDL_0106) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Description( 
            /* [retval][out] */ LPCWSTR *__MIDL_0107) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SupportUrl( 
            /* [retval][out] */ LPCWSTR *__MIDL_0108) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_HashElements( 
            /* [retval][out] */ ISection **HashElement) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IAssemblyReferenceDependentAssemblyEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAssemblyReferenceDependentAssemblyEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAssemblyReferenceDependentAssemblyEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAssemblyReferenceDependentAssemblyEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IAssemblyReferenceDependentAssemblyEntry * This,
            /* [retval][out] */ AssemblyReferenceDependentAssemblyEntry **__MIDL_0099);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Group )( 
            IAssemblyReferenceDependentAssemblyEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0100);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Codebase )( 
            IAssemblyReferenceDependentAssemblyEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0101);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Size )( 
            IAssemblyReferenceDependentAssemblyEntry * This,
            /* [retval][out] */ ULONGLONG *__MIDL_0102);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_HashValue )( 
            IAssemblyReferenceDependentAssemblyEntry * This,
            /* [retval][out] */ IStream **__MIDL_0103);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_HashAlgorithm )( 
            IAssemblyReferenceDependentAssemblyEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0104);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Flags )( 
            IAssemblyReferenceDependentAssemblyEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0105);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ResourceFallbackCulture )( 
            IAssemblyReferenceDependentAssemblyEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0106);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Description )( 
            IAssemblyReferenceDependentAssemblyEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0107);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SupportUrl )( 
            IAssemblyReferenceDependentAssemblyEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0108);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_HashElements )( 
            IAssemblyReferenceDependentAssemblyEntry * This,
            /* [retval][out] */ ISection **HashElement);
        
        END_INTERFACE
    } IAssemblyReferenceDependentAssemblyEntryVtbl;

    interface IAssemblyReferenceDependentAssemblyEntry
    {
        CONST_VTBL struct IAssemblyReferenceDependentAssemblyEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAssemblyReferenceDependentAssemblyEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IAssemblyReferenceDependentAssemblyEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IAssemblyReferenceDependentAssemblyEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IAssemblyReferenceDependentAssemblyEntry_get_AllData(This,__MIDL_0099)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0099)

#define IAssemblyReferenceDependentAssemblyEntry_get_Group(This,__MIDL_0100)	\
    (This)->lpVtbl -> get_Group(This,__MIDL_0100)

#define IAssemblyReferenceDependentAssemblyEntry_get_Codebase(This,__MIDL_0101)	\
    (This)->lpVtbl -> get_Codebase(This,__MIDL_0101)

#define IAssemblyReferenceDependentAssemblyEntry_get_Size(This,__MIDL_0102)	\
    (This)->lpVtbl -> get_Size(This,__MIDL_0102)

#define IAssemblyReferenceDependentAssemblyEntry_get_HashValue(This,__MIDL_0103)	\
    (This)->lpVtbl -> get_HashValue(This,__MIDL_0103)

#define IAssemblyReferenceDependentAssemblyEntry_get_HashAlgorithm(This,__MIDL_0104)	\
    (This)->lpVtbl -> get_HashAlgorithm(This,__MIDL_0104)

#define IAssemblyReferenceDependentAssemblyEntry_get_Flags(This,__MIDL_0105)	\
    (This)->lpVtbl -> get_Flags(This,__MIDL_0105)

#define IAssemblyReferenceDependentAssemblyEntry_get_ResourceFallbackCulture(This,__MIDL_0106)	\
    (This)->lpVtbl -> get_ResourceFallbackCulture(This,__MIDL_0106)

#define IAssemblyReferenceDependentAssemblyEntry_get_Description(This,__MIDL_0107)	\
    (This)->lpVtbl -> get_Description(This,__MIDL_0107)

#define IAssemblyReferenceDependentAssemblyEntry_get_SupportUrl(This,__MIDL_0108)	\
    (This)->lpVtbl -> get_SupportUrl(This,__MIDL_0108)

#define IAssemblyReferenceDependentAssemblyEntry_get_HashElements(This,HashElement)	\
    (This)->lpVtbl -> get_HashElements(This,HashElement)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IAssemblyReferenceDependentAssemblyEntry_get_AllData_Proxy( 
    IAssemblyReferenceDependentAssemblyEntry * This,
    /* [retval][out] */ AssemblyReferenceDependentAssemblyEntry **__MIDL_0099);


void __RPC_STUB IAssemblyReferenceDependentAssemblyEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IAssemblyReferenceDependentAssemblyEntry_get_Group_Proxy( 
    IAssemblyReferenceDependentAssemblyEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0100);


void __RPC_STUB IAssemblyReferenceDependentAssemblyEntry_get_Group_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IAssemblyReferenceDependentAssemblyEntry_get_Codebase_Proxy( 
    IAssemblyReferenceDependentAssemblyEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0101);


void __RPC_STUB IAssemblyReferenceDependentAssemblyEntry_get_Codebase_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IAssemblyReferenceDependentAssemblyEntry_get_Size_Proxy( 
    IAssemblyReferenceDependentAssemblyEntry * This,
    /* [retval][out] */ ULONGLONG *__MIDL_0102);


void __RPC_STUB IAssemblyReferenceDependentAssemblyEntry_get_Size_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IAssemblyReferenceDependentAssemblyEntry_get_HashValue_Proxy( 
    IAssemblyReferenceDependentAssemblyEntry * This,
    /* [retval][out] */ IStream **__MIDL_0103);


void __RPC_STUB IAssemblyReferenceDependentAssemblyEntry_get_HashValue_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IAssemblyReferenceDependentAssemblyEntry_get_HashAlgorithm_Proxy( 
    IAssemblyReferenceDependentAssemblyEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0104);


void __RPC_STUB IAssemblyReferenceDependentAssemblyEntry_get_HashAlgorithm_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IAssemblyReferenceDependentAssemblyEntry_get_Flags_Proxy( 
    IAssemblyReferenceDependentAssemblyEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0105);


void __RPC_STUB IAssemblyReferenceDependentAssemblyEntry_get_Flags_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IAssemblyReferenceDependentAssemblyEntry_get_ResourceFallbackCulture_Proxy( 
    IAssemblyReferenceDependentAssemblyEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0106);


void __RPC_STUB IAssemblyReferenceDependentAssemblyEntry_get_ResourceFallbackCulture_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IAssemblyReferenceDependentAssemblyEntry_get_Description_Proxy( 
    IAssemblyReferenceDependentAssemblyEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0107);


void __RPC_STUB IAssemblyReferenceDependentAssemblyEntry_get_Description_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IAssemblyReferenceDependentAssemblyEntry_get_SupportUrl_Proxy( 
    IAssemblyReferenceDependentAssemblyEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0108);


void __RPC_STUB IAssemblyReferenceDependentAssemblyEntry_get_SupportUrl_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IAssemblyReferenceDependentAssemblyEntry_get_HashElements_Proxy( 
    IAssemblyReferenceDependentAssemblyEntry * This,
    /* [retval][out] */ ISection **HashElement);


void __RPC_STUB IAssemblyReferenceDependentAssemblyEntry_get_HashElements_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IAssemblyReferenceDependentAssemblyEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0294 */
/* [local] */ 

typedef struct _AssemblyReferenceEntry
    {
    IReferenceIdentity *ReferenceIdentity;
    ULONG Flags;
    AssemblyReferenceDependentAssemblyEntry DependentAssembly;
    } 	AssemblyReferenceEntry;

typedef 
enum _AssemblyReferenceEntryFieldId
    {	AssemblyReference_Flags	= 0,
	AssemblyReference_DependentAssembly	= AssemblyReference_Flags + 1
    } 	AssemblyReferenceEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0294_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0294_v0_0_s_ifspec;

#ifndef __IAssemblyReferenceEntry_INTERFACE_DEFINED__
#define __IAssemblyReferenceEntry_INTERFACE_DEFINED__

/* interface IAssemblyReferenceEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IAssemblyReferenceEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("FD47B733-AFBC-45e4-B7C2-BBEB1D9F766C")
    IAssemblyReferenceEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ AssemblyReferenceEntry **__MIDL_0109) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ReferenceIdentity( 
            /* [retval][out] */ IReferenceIdentity **__MIDL_0110) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Flags( 
            /* [retval][out] */ ULONG *__MIDL_0111) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DependentAssembly( 
            /* [retval][out] */ IAssemblyReferenceDependentAssemblyEntry **__MIDL_0112) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IAssemblyReferenceEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAssemblyReferenceEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAssemblyReferenceEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAssemblyReferenceEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IAssemblyReferenceEntry * This,
            /* [retval][out] */ AssemblyReferenceEntry **__MIDL_0109);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ReferenceIdentity )( 
            IAssemblyReferenceEntry * This,
            /* [retval][out] */ IReferenceIdentity **__MIDL_0110);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Flags )( 
            IAssemblyReferenceEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0111);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DependentAssembly )( 
            IAssemblyReferenceEntry * This,
            /* [retval][out] */ IAssemblyReferenceDependentAssemblyEntry **__MIDL_0112);
        
        END_INTERFACE
    } IAssemblyReferenceEntryVtbl;

    interface IAssemblyReferenceEntry
    {
        CONST_VTBL struct IAssemblyReferenceEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAssemblyReferenceEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IAssemblyReferenceEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IAssemblyReferenceEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IAssemblyReferenceEntry_get_AllData(This,__MIDL_0109)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0109)

#define IAssemblyReferenceEntry_get_ReferenceIdentity(This,__MIDL_0110)	\
    (This)->lpVtbl -> get_ReferenceIdentity(This,__MIDL_0110)

#define IAssemblyReferenceEntry_get_Flags(This,__MIDL_0111)	\
    (This)->lpVtbl -> get_Flags(This,__MIDL_0111)

#define IAssemblyReferenceEntry_get_DependentAssembly(This,__MIDL_0112)	\
    (This)->lpVtbl -> get_DependentAssembly(This,__MIDL_0112)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IAssemblyReferenceEntry_get_AllData_Proxy( 
    IAssemblyReferenceEntry * This,
    /* [retval][out] */ AssemblyReferenceEntry **__MIDL_0109);


void __RPC_STUB IAssemblyReferenceEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IAssemblyReferenceEntry_get_ReferenceIdentity_Proxy( 
    IAssemblyReferenceEntry * This,
    /* [retval][out] */ IReferenceIdentity **__MIDL_0110);


void __RPC_STUB IAssemblyReferenceEntry_get_ReferenceIdentity_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IAssemblyReferenceEntry_get_Flags_Proxy( 
    IAssemblyReferenceEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0111);


void __RPC_STUB IAssemblyReferenceEntry_get_Flags_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IAssemblyReferenceEntry_get_DependentAssembly_Proxy( 
    IAssemblyReferenceEntry * This,
    /* [retval][out] */ IAssemblyReferenceDependentAssemblyEntry **__MIDL_0112);


void __RPC_STUB IAssemblyReferenceEntry_get_DependentAssembly_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IAssemblyReferenceEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0295 */
/* [local] */ 

typedef struct _WindowClassEntry
    {
    LPCWSTR ClassName;
    LPCWSTR HostDll;
    BOOLEAN fVersioned;
    } 	WindowClassEntry;

typedef 
enum _WindowClassEntryFieldId
    {	WindowClass_HostDll	= 0,
	WindowClass_fVersioned	= WindowClass_HostDll + 1
    } 	WindowClassEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0295_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0295_v0_0_s_ifspec;

#ifndef __IWindowClassEntry_INTERFACE_DEFINED__
#define __IWindowClassEntry_INTERFACE_DEFINED__

/* interface IWindowClassEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IWindowClassEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("8AD3FC86-AFD3-477a-8FD5-146C291195BA")
    IWindowClassEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ WindowClassEntry **__MIDL_0113) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ClassName( 
            /* [retval][out] */ LPCWSTR *__MIDL_0114) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_HostDll( 
            /* [retval][out] */ LPCWSTR *__MIDL_0115) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_fVersioned( 
            /* [retval][out] */ BOOLEAN *__MIDL_0116) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IWindowClassEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IWindowClassEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IWindowClassEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IWindowClassEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IWindowClassEntry * This,
            /* [retval][out] */ WindowClassEntry **__MIDL_0113);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ClassName )( 
            IWindowClassEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0114);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_HostDll )( 
            IWindowClassEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0115);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_fVersioned )( 
            IWindowClassEntry * This,
            /* [retval][out] */ BOOLEAN *__MIDL_0116);
        
        END_INTERFACE
    } IWindowClassEntryVtbl;

    interface IWindowClassEntry
    {
        CONST_VTBL struct IWindowClassEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IWindowClassEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IWindowClassEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IWindowClassEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IWindowClassEntry_get_AllData(This,__MIDL_0113)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0113)

#define IWindowClassEntry_get_ClassName(This,__MIDL_0114)	\
    (This)->lpVtbl -> get_ClassName(This,__MIDL_0114)

#define IWindowClassEntry_get_HostDll(This,__MIDL_0115)	\
    (This)->lpVtbl -> get_HostDll(This,__MIDL_0115)

#define IWindowClassEntry_get_fVersioned(This,__MIDL_0116)	\
    (This)->lpVtbl -> get_fVersioned(This,__MIDL_0116)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IWindowClassEntry_get_AllData_Proxy( 
    IWindowClassEntry * This,
    /* [retval][out] */ WindowClassEntry **__MIDL_0113);


void __RPC_STUB IWindowClassEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IWindowClassEntry_get_ClassName_Proxy( 
    IWindowClassEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0114);


void __RPC_STUB IWindowClassEntry_get_ClassName_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IWindowClassEntry_get_HostDll_Proxy( 
    IWindowClassEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0115);


void __RPC_STUB IWindowClassEntry_get_HostDll_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IWindowClassEntry_get_fVersioned_Proxy( 
    IWindowClassEntry * This,
    /* [retval][out] */ BOOLEAN *__MIDL_0116);


void __RPC_STUB IWindowClassEntry_get_fVersioned_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IWindowClassEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0296 */
/* [local] */ 

typedef struct _ResourceTableMappingEntry
    {
    LPCWSTR id;
    LPCWSTR FinalStringMapped;
    } 	ResourceTableMappingEntry;

typedef 
enum _ResourceTableMappingEntryFieldId
    {	ResourceTableMapping_FinalStringMapped	= 0
    } 	ResourceTableMappingEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0296_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0296_v0_0_s_ifspec;

#ifndef __IResourceTableMappingEntry_INTERFACE_DEFINED__
#define __IResourceTableMappingEntry_INTERFACE_DEFINED__

/* interface IResourceTableMappingEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IResourceTableMappingEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("70A4ECEE-B195-4c59-85BF-44B6ACA83F07")
    IResourceTableMappingEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ ResourceTableMappingEntry **__MIDL_0117) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_id( 
            /* [retval][out] */ LPCWSTR *__MIDL_0118) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FinalStringMapped( 
            /* [retval][out] */ LPCWSTR *__MIDL_0119) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IResourceTableMappingEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IResourceTableMappingEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IResourceTableMappingEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IResourceTableMappingEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IResourceTableMappingEntry * This,
            /* [retval][out] */ ResourceTableMappingEntry **__MIDL_0117);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_id )( 
            IResourceTableMappingEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0118);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FinalStringMapped )( 
            IResourceTableMappingEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0119);
        
        END_INTERFACE
    } IResourceTableMappingEntryVtbl;

    interface IResourceTableMappingEntry
    {
        CONST_VTBL struct IResourceTableMappingEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IResourceTableMappingEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IResourceTableMappingEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IResourceTableMappingEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IResourceTableMappingEntry_get_AllData(This,__MIDL_0117)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0117)

#define IResourceTableMappingEntry_get_id(This,__MIDL_0118)	\
    (This)->lpVtbl -> get_id(This,__MIDL_0118)

#define IResourceTableMappingEntry_get_FinalStringMapped(This,__MIDL_0119)	\
    (This)->lpVtbl -> get_FinalStringMapped(This,__MIDL_0119)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IResourceTableMappingEntry_get_AllData_Proxy( 
    IResourceTableMappingEntry * This,
    /* [retval][out] */ ResourceTableMappingEntry **__MIDL_0117);


void __RPC_STUB IResourceTableMappingEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IResourceTableMappingEntry_get_id_Proxy( 
    IResourceTableMappingEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0118);


void __RPC_STUB IResourceTableMappingEntry_get_id_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IResourceTableMappingEntry_get_FinalStringMapped_Proxy( 
    IResourceTableMappingEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0119);


void __RPC_STUB IResourceTableMappingEntry_get_FinalStringMapped_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IResourceTableMappingEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0297 */
/* [local] */ 

typedef struct _EntryPointEntry
    {
    LPCWSTR Name;
    LPCWSTR CommandLine_File;
    LPCWSTR CommandLine_Parameters;
    IReferenceIdentity *Identity;
    ULONG Flags;
    } 	EntryPointEntry;

typedef 
enum _EntryPointEntryFieldId
    {	EntryPoint_CommandLine_File	= 0,
	EntryPoint_CommandLine_Parameters	= EntryPoint_CommandLine_File + 1,
	EntryPoint_Identity	= EntryPoint_CommandLine_Parameters + 1,
	EntryPoint_Flags	= EntryPoint_Identity + 1
    } 	EntryPointEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0297_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0297_v0_0_s_ifspec;

#ifndef __IEntryPointEntry_INTERFACE_DEFINED__
#define __IEntryPointEntry_INTERFACE_DEFINED__

/* interface IEntryPointEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IEntryPointEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("1583EFE9-832F-4d08-B041-CAC5ACEDB948")
    IEntryPointEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ EntryPointEntry **__MIDL_0120) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Name( 
            /* [retval][out] */ LPCWSTR *__MIDL_0121) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CommandLine_File( 
            /* [retval][out] */ LPCWSTR *__MIDL_0122) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CommandLine_Parameters( 
            /* [retval][out] */ LPCWSTR *__MIDL_0123) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Identity( 
            /* [retval][out] */ IReferenceIdentity **__MIDL_0124) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Flags( 
            /* [retval][out] */ ULONG *__MIDL_0125) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEntryPointEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEntryPointEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEntryPointEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEntryPointEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IEntryPointEntry * This,
            /* [retval][out] */ EntryPointEntry **__MIDL_0120);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Name )( 
            IEntryPointEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0121);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CommandLine_File )( 
            IEntryPointEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0122);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CommandLine_Parameters )( 
            IEntryPointEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0123);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Identity )( 
            IEntryPointEntry * This,
            /* [retval][out] */ IReferenceIdentity **__MIDL_0124);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Flags )( 
            IEntryPointEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0125);
        
        END_INTERFACE
    } IEntryPointEntryVtbl;

    interface IEntryPointEntry
    {
        CONST_VTBL struct IEntryPointEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEntryPointEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEntryPointEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEntryPointEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEntryPointEntry_get_AllData(This,__MIDL_0120)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0120)

#define IEntryPointEntry_get_Name(This,__MIDL_0121)	\
    (This)->lpVtbl -> get_Name(This,__MIDL_0121)

#define IEntryPointEntry_get_CommandLine_File(This,__MIDL_0122)	\
    (This)->lpVtbl -> get_CommandLine_File(This,__MIDL_0122)

#define IEntryPointEntry_get_CommandLine_Parameters(This,__MIDL_0123)	\
    (This)->lpVtbl -> get_CommandLine_Parameters(This,__MIDL_0123)

#define IEntryPointEntry_get_Identity(This,__MIDL_0124)	\
    (This)->lpVtbl -> get_Identity(This,__MIDL_0124)

#define IEntryPointEntry_get_Flags(This,__MIDL_0125)	\
    (This)->lpVtbl -> get_Flags(This,__MIDL_0125)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IEntryPointEntry_get_AllData_Proxy( 
    IEntryPointEntry * This,
    /* [retval][out] */ EntryPointEntry **__MIDL_0120);


void __RPC_STUB IEntryPointEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IEntryPointEntry_get_Name_Proxy( 
    IEntryPointEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0121);


void __RPC_STUB IEntryPointEntry_get_Name_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IEntryPointEntry_get_CommandLine_File_Proxy( 
    IEntryPointEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0122);


void __RPC_STUB IEntryPointEntry_get_CommandLine_File_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IEntryPointEntry_get_CommandLine_Parameters_Proxy( 
    IEntryPointEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0123);


void __RPC_STUB IEntryPointEntry_get_CommandLine_Parameters_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IEntryPointEntry_get_Identity_Proxy( 
    IEntryPointEntry * This,
    /* [retval][out] */ IReferenceIdentity **__MIDL_0124);


void __RPC_STUB IEntryPointEntry_get_Identity_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IEntryPointEntry_get_Flags_Proxy( 
    IEntryPointEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0125);


void __RPC_STUB IEntryPointEntry_get_Flags_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEntryPointEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0298 */
/* [local] */ 

typedef struct _PermissionSetEntry
    {
    LPCWSTR Id;
    LPCWSTR XmlSegment;
    } 	PermissionSetEntry;

typedef 
enum _PermissionSetEntryFieldId
    {	PermissionSet_XmlSegment	= 0
    } 	PermissionSetEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0298_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0298_v0_0_s_ifspec;

#ifndef __IPermissionSetEntry_INTERFACE_DEFINED__
#define __IPermissionSetEntry_INTERFACE_DEFINED__

/* interface IPermissionSetEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IPermissionSetEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("EBE5A1ED-FEBC-42c4-A9E1-E087C6E36635")
    IPermissionSetEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ PermissionSetEntry **__MIDL_0126) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Id( 
            /* [retval][out] */ LPCWSTR *__MIDL_0127) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_XmlSegment( 
            /* [retval][out] */ LPCWSTR *__MIDL_0128) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IPermissionSetEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IPermissionSetEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IPermissionSetEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IPermissionSetEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IPermissionSetEntry * This,
            /* [retval][out] */ PermissionSetEntry **__MIDL_0126);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Id )( 
            IPermissionSetEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0127);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_XmlSegment )( 
            IPermissionSetEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0128);
        
        END_INTERFACE
    } IPermissionSetEntryVtbl;

    interface IPermissionSetEntry
    {
        CONST_VTBL struct IPermissionSetEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IPermissionSetEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IPermissionSetEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IPermissionSetEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IPermissionSetEntry_get_AllData(This,__MIDL_0126)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0126)

#define IPermissionSetEntry_get_Id(This,__MIDL_0127)	\
    (This)->lpVtbl -> get_Id(This,__MIDL_0127)

#define IPermissionSetEntry_get_XmlSegment(This,__MIDL_0128)	\
    (This)->lpVtbl -> get_XmlSegment(This,__MIDL_0128)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IPermissionSetEntry_get_AllData_Proxy( 
    IPermissionSetEntry * This,
    /* [retval][out] */ PermissionSetEntry **__MIDL_0126);


void __RPC_STUB IPermissionSetEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IPermissionSetEntry_get_Id_Proxy( 
    IPermissionSetEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0127);


void __RPC_STUB IPermissionSetEntry_get_Id_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IPermissionSetEntry_get_XmlSegment_Proxy( 
    IPermissionSetEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0128);


void __RPC_STUB IPermissionSetEntry_get_XmlSegment_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IPermissionSetEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0299 */
/* [local] */ 

typedef struct _AssemblyRequestEntry
    {
    LPCWSTR Name;
    LPCWSTR permissionSetID;
    } 	AssemblyRequestEntry;

typedef 
enum _AssemblyRequestEntryFieldId
    {	AssemblyRequest_permissionSetID	= 0
    } 	AssemblyRequestEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0299_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0299_v0_0_s_ifspec;

#ifndef __IAssemblyRequestEntry_INTERFACE_DEFINED__
#define __IAssemblyRequestEntry_INTERFACE_DEFINED__

/* interface IAssemblyRequestEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IAssemblyRequestEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("2474ECB4-8EFD-4410-9F31-B3E7C4A07731")
    IAssemblyRequestEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ AssemblyRequestEntry **__MIDL_0129) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Name( 
            /* [retval][out] */ LPCWSTR *__MIDL_0130) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_permissionSetID( 
            /* [retval][out] */ LPCWSTR *__MIDL_0131) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IAssemblyRequestEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAssemblyRequestEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAssemblyRequestEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAssemblyRequestEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IAssemblyRequestEntry * This,
            /* [retval][out] */ AssemblyRequestEntry **__MIDL_0129);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Name )( 
            IAssemblyRequestEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0130);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_permissionSetID )( 
            IAssemblyRequestEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0131);
        
        END_INTERFACE
    } IAssemblyRequestEntryVtbl;

    interface IAssemblyRequestEntry
    {
        CONST_VTBL struct IAssemblyRequestEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAssemblyRequestEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IAssemblyRequestEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IAssemblyRequestEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IAssemblyRequestEntry_get_AllData(This,__MIDL_0129)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0129)

#define IAssemblyRequestEntry_get_Name(This,__MIDL_0130)	\
    (This)->lpVtbl -> get_Name(This,__MIDL_0130)

#define IAssemblyRequestEntry_get_permissionSetID(This,__MIDL_0131)	\
    (This)->lpVtbl -> get_permissionSetID(This,__MIDL_0131)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IAssemblyRequestEntry_get_AllData_Proxy( 
    IAssemblyRequestEntry * This,
    /* [retval][out] */ AssemblyRequestEntry **__MIDL_0129);


void __RPC_STUB IAssemblyRequestEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IAssemblyRequestEntry_get_Name_Proxy( 
    IAssemblyRequestEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0130);


void __RPC_STUB IAssemblyRequestEntry_get_Name_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IAssemblyRequestEntry_get_permissionSetID_Proxy( 
    IAssemblyRequestEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0131);


void __RPC_STUB IAssemblyRequestEntry_get_permissionSetID_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IAssemblyRequestEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0300 */
/* [local] */ 

typedef struct _DescriptionMetadataEntry
    {
    LPCWSTR Publisher;
    LPCWSTR Product;
    LPCWSTR SupportUrl;
    LPCWSTR IconFile;
    LPCWSTR ErrorReportUrl;
    LPCWSTR SuiteName;
    } 	DescriptionMetadataEntry;

typedef 
enum _DescriptionMetadataEntryFieldId
    {	DescriptionMetadata_Publisher	= 0,
	DescriptionMetadata_Product	= DescriptionMetadata_Publisher + 1,
	DescriptionMetadata_SupportUrl	= DescriptionMetadata_Product + 1,
	DescriptionMetadata_IconFile	= DescriptionMetadata_SupportUrl + 1,
	DescriptionMetadata_ErrorReportUrl	= DescriptionMetadata_IconFile + 1,
	DescriptionMetadata_SuiteName	= DescriptionMetadata_ErrorReportUrl + 1
    } 	DescriptionMetadataEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0300_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0300_v0_0_s_ifspec;

#ifndef __IDescriptionMetadataEntry_INTERFACE_DEFINED__
#define __IDescriptionMetadataEntry_INTERFACE_DEFINED__

/* interface IDescriptionMetadataEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IDescriptionMetadataEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("CB73147E-5FC2-4c31-B4E6-58D13DBE1A08")
    IDescriptionMetadataEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ DescriptionMetadataEntry **__MIDL_0132) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Publisher( 
            /* [retval][out] */ LPCWSTR *__MIDL_0133) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Product( 
            /* [retval][out] */ LPCWSTR *__MIDL_0134) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SupportUrl( 
            /* [retval][out] */ LPCWSTR *__MIDL_0135) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IconFile( 
            /* [retval][out] */ LPCWSTR *__MIDL_0136) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ErrorReportUrl( 
            /* [retval][out] */ LPCWSTR *__MIDL_0137) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SuiteName( 
            /* [retval][out] */ LPCWSTR *__MIDL_0138) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IDescriptionMetadataEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDescriptionMetadataEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDescriptionMetadataEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDescriptionMetadataEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IDescriptionMetadataEntry * This,
            /* [retval][out] */ DescriptionMetadataEntry **__MIDL_0132);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Publisher )( 
            IDescriptionMetadataEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0133);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Product )( 
            IDescriptionMetadataEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0134);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SupportUrl )( 
            IDescriptionMetadataEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0135);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IconFile )( 
            IDescriptionMetadataEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0136);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ErrorReportUrl )( 
            IDescriptionMetadataEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0137);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SuiteName )( 
            IDescriptionMetadataEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0138);
        
        END_INTERFACE
    } IDescriptionMetadataEntryVtbl;

    interface IDescriptionMetadataEntry
    {
        CONST_VTBL struct IDescriptionMetadataEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDescriptionMetadataEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IDescriptionMetadataEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IDescriptionMetadataEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IDescriptionMetadataEntry_get_AllData(This,__MIDL_0132)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0132)

#define IDescriptionMetadataEntry_get_Publisher(This,__MIDL_0133)	\
    (This)->lpVtbl -> get_Publisher(This,__MIDL_0133)

#define IDescriptionMetadataEntry_get_Product(This,__MIDL_0134)	\
    (This)->lpVtbl -> get_Product(This,__MIDL_0134)

#define IDescriptionMetadataEntry_get_SupportUrl(This,__MIDL_0135)	\
    (This)->lpVtbl -> get_SupportUrl(This,__MIDL_0135)

#define IDescriptionMetadataEntry_get_IconFile(This,__MIDL_0136)	\
    (This)->lpVtbl -> get_IconFile(This,__MIDL_0136)

#define IDescriptionMetadataEntry_get_ErrorReportUrl(This,__MIDL_0137)	\
    (This)->lpVtbl -> get_ErrorReportUrl(This,__MIDL_0137)

#define IDescriptionMetadataEntry_get_SuiteName(This,__MIDL_0138)	\
    (This)->lpVtbl -> get_SuiteName(This,__MIDL_0138)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IDescriptionMetadataEntry_get_AllData_Proxy( 
    IDescriptionMetadataEntry * This,
    /* [retval][out] */ DescriptionMetadataEntry **__MIDL_0132);


void __RPC_STUB IDescriptionMetadataEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDescriptionMetadataEntry_get_Publisher_Proxy( 
    IDescriptionMetadataEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0133);


void __RPC_STUB IDescriptionMetadataEntry_get_Publisher_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDescriptionMetadataEntry_get_Product_Proxy( 
    IDescriptionMetadataEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0134);


void __RPC_STUB IDescriptionMetadataEntry_get_Product_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDescriptionMetadataEntry_get_SupportUrl_Proxy( 
    IDescriptionMetadataEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0135);


void __RPC_STUB IDescriptionMetadataEntry_get_SupportUrl_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDescriptionMetadataEntry_get_IconFile_Proxy( 
    IDescriptionMetadataEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0136);


void __RPC_STUB IDescriptionMetadataEntry_get_IconFile_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDescriptionMetadataEntry_get_ErrorReportUrl_Proxy( 
    IDescriptionMetadataEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0137);


void __RPC_STUB IDescriptionMetadataEntry_get_ErrorReportUrl_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDescriptionMetadataEntry_get_SuiteName_Proxy( 
    IDescriptionMetadataEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0138);


void __RPC_STUB IDescriptionMetadataEntry_get_SuiteName_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IDescriptionMetadataEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0301 */
/* [local] */ 

typedef struct _DeploymentMetadataEntry
    {
    LPCWSTR DeploymentProviderCodebase;
    LPCWSTR MinimumRequiredVersion;
    USHORT MaximumAge;
    UCHAR MaximumAge_Unit;
    ULONG DeploymentFlags;
    } 	DeploymentMetadataEntry;

typedef 
enum _DeploymentMetadataEntryFieldId
    {	DeploymentMetadata_DeploymentProviderCodebase	= 0,
	DeploymentMetadata_MinimumRequiredVersion	= DeploymentMetadata_DeploymentProviderCodebase + 1,
	DeploymentMetadata_MaximumAge	= DeploymentMetadata_MinimumRequiredVersion + 1,
	DeploymentMetadata_MaximumAge_Unit	= DeploymentMetadata_MaximumAge + 1,
	DeploymentMetadata_DeploymentFlags	= DeploymentMetadata_MaximumAge_Unit + 1
    } 	DeploymentMetadataEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0301_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0301_v0_0_s_ifspec;

#ifndef __IDeploymentMetadataEntry_INTERFACE_DEFINED__
#define __IDeploymentMetadataEntry_INTERFACE_DEFINED__

/* interface IDeploymentMetadataEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IDeploymentMetadataEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("CFA3F59F-334D-46bf-A5A5-5D11BB2D7EBC")
    IDeploymentMetadataEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ DeploymentMetadataEntry **__MIDL_0139) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DeploymentProviderCodebase( 
            /* [retval][out] */ LPCWSTR *__MIDL_0140) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MinimumRequiredVersion( 
            /* [retval][out] */ LPCWSTR *__MIDL_0141) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MaximumAge( 
            /* [retval][out] */ USHORT *__MIDL_0142) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MaximumAge_Unit( 
            /* [retval][out] */ UCHAR *__MIDL_0143) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DeploymentFlags( 
            /* [retval][out] */ ULONG *__MIDL_0144) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IDeploymentMetadataEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDeploymentMetadataEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDeploymentMetadataEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDeploymentMetadataEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IDeploymentMetadataEntry * This,
            /* [retval][out] */ DeploymentMetadataEntry **__MIDL_0139);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DeploymentProviderCodebase )( 
            IDeploymentMetadataEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0140);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MinimumRequiredVersion )( 
            IDeploymentMetadataEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0141);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MaximumAge )( 
            IDeploymentMetadataEntry * This,
            /* [retval][out] */ USHORT *__MIDL_0142);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MaximumAge_Unit )( 
            IDeploymentMetadataEntry * This,
            /* [retval][out] */ UCHAR *__MIDL_0143);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DeploymentFlags )( 
            IDeploymentMetadataEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0144);
        
        END_INTERFACE
    } IDeploymentMetadataEntryVtbl;

    interface IDeploymentMetadataEntry
    {
        CONST_VTBL struct IDeploymentMetadataEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDeploymentMetadataEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IDeploymentMetadataEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IDeploymentMetadataEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IDeploymentMetadataEntry_get_AllData(This,__MIDL_0139)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0139)

#define IDeploymentMetadataEntry_get_DeploymentProviderCodebase(This,__MIDL_0140)	\
    (This)->lpVtbl -> get_DeploymentProviderCodebase(This,__MIDL_0140)

#define IDeploymentMetadataEntry_get_MinimumRequiredVersion(This,__MIDL_0141)	\
    (This)->lpVtbl -> get_MinimumRequiredVersion(This,__MIDL_0141)

#define IDeploymentMetadataEntry_get_MaximumAge(This,__MIDL_0142)	\
    (This)->lpVtbl -> get_MaximumAge(This,__MIDL_0142)

#define IDeploymentMetadataEntry_get_MaximumAge_Unit(This,__MIDL_0143)	\
    (This)->lpVtbl -> get_MaximumAge_Unit(This,__MIDL_0143)

#define IDeploymentMetadataEntry_get_DeploymentFlags(This,__MIDL_0144)	\
    (This)->lpVtbl -> get_DeploymentFlags(This,__MIDL_0144)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IDeploymentMetadataEntry_get_AllData_Proxy( 
    IDeploymentMetadataEntry * This,
    /* [retval][out] */ DeploymentMetadataEntry **__MIDL_0139);


void __RPC_STUB IDeploymentMetadataEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDeploymentMetadataEntry_get_DeploymentProviderCodebase_Proxy( 
    IDeploymentMetadataEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0140);


void __RPC_STUB IDeploymentMetadataEntry_get_DeploymentProviderCodebase_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDeploymentMetadataEntry_get_MinimumRequiredVersion_Proxy( 
    IDeploymentMetadataEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0141);


void __RPC_STUB IDeploymentMetadataEntry_get_MinimumRequiredVersion_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDeploymentMetadataEntry_get_MaximumAge_Proxy( 
    IDeploymentMetadataEntry * This,
    /* [retval][out] */ USHORT *__MIDL_0142);


void __RPC_STUB IDeploymentMetadataEntry_get_MaximumAge_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDeploymentMetadataEntry_get_MaximumAge_Unit_Proxy( 
    IDeploymentMetadataEntry * This,
    /* [retval][out] */ UCHAR *__MIDL_0143);


void __RPC_STUB IDeploymentMetadataEntry_get_MaximumAge_Unit_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDeploymentMetadataEntry_get_DeploymentFlags_Proxy( 
    IDeploymentMetadataEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0144);


void __RPC_STUB IDeploymentMetadataEntry_get_DeploymentFlags_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IDeploymentMetadataEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0302 */
/* [local] */ 

typedef struct _DependentOSMetadataEntry
    {
    LPCWSTR SupportUrl;
    LPCWSTR Description;
    USHORT MajorVersion;
    USHORT MinorVersion;
    USHORT BuildNumber;
    UCHAR ServicePackMajor;
    UCHAR ServicePackMinor;
    } 	DependentOSMetadataEntry;

typedef 
enum _DependentOSMetadataEntryFieldId
    {	DependentOSMetadata_SupportUrl	= 0,
	DependentOSMetadata_Description	= DependentOSMetadata_SupportUrl + 1,
	DependentOSMetadata_MajorVersion	= DependentOSMetadata_Description + 1,
	DependentOSMetadata_MinorVersion	= DependentOSMetadata_MajorVersion + 1,
	DependentOSMetadata_BuildNumber	= DependentOSMetadata_MinorVersion + 1,
	DependentOSMetadata_ServicePackMajor	= DependentOSMetadata_BuildNumber + 1,
	DependentOSMetadata_ServicePackMinor	= DependentOSMetadata_ServicePackMajor + 1
    } 	DependentOSMetadataEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0302_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0302_v0_0_s_ifspec;

#ifndef __IDependentOSMetadataEntry_INTERFACE_DEFINED__
#define __IDependentOSMetadataEntry_INTERFACE_DEFINED__

/* interface IDependentOSMetadataEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IDependentOSMetadataEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("CF168CF4-4E8F-4d92-9D2A-60E5CA21CF85")
    IDependentOSMetadataEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ DependentOSMetadataEntry **__MIDL_0145) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SupportUrl( 
            /* [retval][out] */ LPCWSTR *__MIDL_0146) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Description( 
            /* [retval][out] */ LPCWSTR *__MIDL_0147) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MajorVersion( 
            /* [retval][out] */ USHORT *__MIDL_0148) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MinorVersion( 
            /* [retval][out] */ USHORT *__MIDL_0149) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_BuildNumber( 
            /* [retval][out] */ USHORT *__MIDL_0150) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ServicePackMajor( 
            /* [retval][out] */ UCHAR *__MIDL_0151) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ServicePackMinor( 
            /* [retval][out] */ UCHAR *__MIDL_0152) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IDependentOSMetadataEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDependentOSMetadataEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDependentOSMetadataEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDependentOSMetadataEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IDependentOSMetadataEntry * This,
            /* [retval][out] */ DependentOSMetadataEntry **__MIDL_0145);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SupportUrl )( 
            IDependentOSMetadataEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0146);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Description )( 
            IDependentOSMetadataEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0147);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MajorVersion )( 
            IDependentOSMetadataEntry * This,
            /* [retval][out] */ USHORT *__MIDL_0148);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MinorVersion )( 
            IDependentOSMetadataEntry * This,
            /* [retval][out] */ USHORT *__MIDL_0149);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_BuildNumber )( 
            IDependentOSMetadataEntry * This,
            /* [retval][out] */ USHORT *__MIDL_0150);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ServicePackMajor )( 
            IDependentOSMetadataEntry * This,
            /* [retval][out] */ UCHAR *__MIDL_0151);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ServicePackMinor )( 
            IDependentOSMetadataEntry * This,
            /* [retval][out] */ UCHAR *__MIDL_0152);
        
        END_INTERFACE
    } IDependentOSMetadataEntryVtbl;

    interface IDependentOSMetadataEntry
    {
        CONST_VTBL struct IDependentOSMetadataEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDependentOSMetadataEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IDependentOSMetadataEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IDependentOSMetadataEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IDependentOSMetadataEntry_get_AllData(This,__MIDL_0145)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0145)

#define IDependentOSMetadataEntry_get_SupportUrl(This,__MIDL_0146)	\
    (This)->lpVtbl -> get_SupportUrl(This,__MIDL_0146)

#define IDependentOSMetadataEntry_get_Description(This,__MIDL_0147)	\
    (This)->lpVtbl -> get_Description(This,__MIDL_0147)

#define IDependentOSMetadataEntry_get_MajorVersion(This,__MIDL_0148)	\
    (This)->lpVtbl -> get_MajorVersion(This,__MIDL_0148)

#define IDependentOSMetadataEntry_get_MinorVersion(This,__MIDL_0149)	\
    (This)->lpVtbl -> get_MinorVersion(This,__MIDL_0149)

#define IDependentOSMetadataEntry_get_BuildNumber(This,__MIDL_0150)	\
    (This)->lpVtbl -> get_BuildNumber(This,__MIDL_0150)

#define IDependentOSMetadataEntry_get_ServicePackMajor(This,__MIDL_0151)	\
    (This)->lpVtbl -> get_ServicePackMajor(This,__MIDL_0151)

#define IDependentOSMetadataEntry_get_ServicePackMinor(This,__MIDL_0152)	\
    (This)->lpVtbl -> get_ServicePackMinor(This,__MIDL_0152)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IDependentOSMetadataEntry_get_AllData_Proxy( 
    IDependentOSMetadataEntry * This,
    /* [retval][out] */ DependentOSMetadataEntry **__MIDL_0145);


void __RPC_STUB IDependentOSMetadataEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDependentOSMetadataEntry_get_SupportUrl_Proxy( 
    IDependentOSMetadataEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0146);


void __RPC_STUB IDependentOSMetadataEntry_get_SupportUrl_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDependentOSMetadataEntry_get_Description_Proxy( 
    IDependentOSMetadataEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0147);


void __RPC_STUB IDependentOSMetadataEntry_get_Description_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDependentOSMetadataEntry_get_MajorVersion_Proxy( 
    IDependentOSMetadataEntry * This,
    /* [retval][out] */ USHORT *__MIDL_0148);


void __RPC_STUB IDependentOSMetadataEntry_get_MajorVersion_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDependentOSMetadataEntry_get_MinorVersion_Proxy( 
    IDependentOSMetadataEntry * This,
    /* [retval][out] */ USHORT *__MIDL_0149);


void __RPC_STUB IDependentOSMetadataEntry_get_MinorVersion_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDependentOSMetadataEntry_get_BuildNumber_Proxy( 
    IDependentOSMetadataEntry * This,
    /* [retval][out] */ USHORT *__MIDL_0150);


void __RPC_STUB IDependentOSMetadataEntry_get_BuildNumber_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDependentOSMetadataEntry_get_ServicePackMajor_Proxy( 
    IDependentOSMetadataEntry * This,
    /* [retval][out] */ UCHAR *__MIDL_0151);


void __RPC_STUB IDependentOSMetadataEntry_get_ServicePackMajor_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDependentOSMetadataEntry_get_ServicePackMinor_Proxy( 
    IDependentOSMetadataEntry * This,
    /* [retval][out] */ UCHAR *__MIDL_0152);


void __RPC_STUB IDependentOSMetadataEntry_get_ServicePackMinor_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IDependentOSMetadataEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0303 */
/* [local] */ 

typedef struct _CompatibleFrameworksMetadataEntry
    {
    LPCWSTR SupportUrl;
    } 	CompatibleFrameworksMetadataEntry;

typedef 
enum _CompatibleFrameworksMetadataEntryFieldId
    {	CompatibleFrameworksMetadata_SupportUrl	= 0
    } 	CompatibleFrameworksMetadataEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0303_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0303_v0_0_s_ifspec;

#ifndef __ICompatibleFrameworksMetadataEntry_INTERFACE_DEFINED__
#define __ICompatibleFrameworksMetadataEntry_INTERFACE_DEFINED__

/* interface ICompatibleFrameworksMetadataEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_ICompatibleFrameworksMetadataEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("4A33D662-2210-463A-BE9F-FBDF1AA554E3")
    ICompatibleFrameworksMetadataEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ CompatibleFrameworksMetadataEntry **__MIDL_0153) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SupportUrl( 
            /* [retval][out] */ LPCWSTR *__MIDL_0154) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ICompatibleFrameworksMetadataEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICompatibleFrameworksMetadataEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICompatibleFrameworksMetadataEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICompatibleFrameworksMetadataEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            ICompatibleFrameworksMetadataEntry * This,
            /* [retval][out] */ CompatibleFrameworksMetadataEntry **__MIDL_0153);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SupportUrl )( 
            ICompatibleFrameworksMetadataEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0154);
        
        END_INTERFACE
    } ICompatibleFrameworksMetadataEntryVtbl;

    interface ICompatibleFrameworksMetadataEntry
    {
        CONST_VTBL struct ICompatibleFrameworksMetadataEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICompatibleFrameworksMetadataEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ICompatibleFrameworksMetadataEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ICompatibleFrameworksMetadataEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ICompatibleFrameworksMetadataEntry_get_AllData(This,__MIDL_0153)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0153)

#define ICompatibleFrameworksMetadataEntry_get_SupportUrl(This,__MIDL_0154)	\
    (This)->lpVtbl -> get_SupportUrl(This,__MIDL_0154)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ICompatibleFrameworksMetadataEntry_get_AllData_Proxy( 
    ICompatibleFrameworksMetadataEntry * This,
    /* [retval][out] */ CompatibleFrameworksMetadataEntry **__MIDL_0153);


void __RPC_STUB ICompatibleFrameworksMetadataEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICompatibleFrameworksMetadataEntry_get_SupportUrl_Proxy( 
    ICompatibleFrameworksMetadataEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0154);


void __RPC_STUB ICompatibleFrameworksMetadataEntry_get_SupportUrl_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ICompatibleFrameworksMetadataEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0304 */
/* [local] */ 

typedef struct _MetadataSectionEntry
    {
    ULONG SchemaVersion;
    ULONG ManifestFlags;
    ULONG UsagePatterns;
    IDefinitionIdentity *CdfIdentity;
    LPCWSTR LocalPath;
    ULONG HashAlgorithm;
    /* [size_is] */ BYTE *ManifestHash;
    ULONG ManifestHashSize;
    LPCWSTR ContentType;
    LPCWSTR RuntimeImageVersion;
    /* [size_is] */ BYTE *MvidValue;
    ULONG MvidValueSize;
    DescriptionMetadataEntry DescriptionData;
    DeploymentMetadataEntry DeploymentData;
    DependentOSMetadataEntry DependentOSData;
    LPCWSTR defaultPermissionSetID;
    LPCWSTR RequestedExecutionLevel;
    BOOLEAN RequestedExecutionLevelUIAccess;
    IReferenceIdentity *ResourceTypeResourcesDependency;
    IReferenceIdentity *ResourceTypeManifestResourcesDependency;
    LPCWSTR KeyInfoElement;
    CompatibleFrameworksMetadataEntry CompatibleFrameworksData;
    } 	MetadataSectionEntry;

typedef 
enum _MetadataSectionEntryFieldId
    {	MetadataSection_SchemaVersion	= 0,
	MetadataSection_ManifestFlags	= MetadataSection_SchemaVersion + 1,
	MetadataSection_UsagePatterns	= MetadataSection_ManifestFlags + 1,
	MetadataSection_CdfIdentity	= MetadataSection_UsagePatterns + 1,
	MetadataSection_LocalPath	= MetadataSection_CdfIdentity + 1,
	MetadataSection_HashAlgorithm	= MetadataSection_LocalPath + 1,
	MetadataSection_ManifestHash	= MetadataSection_HashAlgorithm + 1,
	MetadataSection_ManifestHashSize	= MetadataSection_ManifestHash + 1,
	MetadataSection_ContentType	= MetadataSection_ManifestHashSize + 1,
	MetadataSection_RuntimeImageVersion	= MetadataSection_ContentType + 1,
	MetadataSection_MvidValue	= MetadataSection_RuntimeImageVersion + 1,
	MetadataSection_MvidValueSize	= MetadataSection_MvidValue + 1,
	MetadataSection_DescriptionData	= MetadataSection_MvidValueSize + 1,
	MetadataSection_DeploymentData	= MetadataSection_DescriptionData + 1,
	MetadataSection_DependentOSData	= MetadataSection_DeploymentData + 1,
	MetadataSection_defaultPermissionSetID	= MetadataSection_DependentOSData + 1,
	MetadataSection_RequestedExecutionLevel	= MetadataSection_defaultPermissionSetID + 1,
	MetadataSection_RequestedExecutionLevelUIAccess	= MetadataSection_RequestedExecutionLevel + 1,
	MetadataSection_ResourceTypeResourcesDependency	= MetadataSection_RequestedExecutionLevelUIAccess + 1,
	MetadataSection_ResourceTypeManifestResourcesDependency	= MetadataSection_ResourceTypeResourcesDependency + 1,
	MetadataSection_KeyInfoElement	= MetadataSection_ResourceTypeManifestResourcesDependency + 1,
	MetadataSection_CompatibleFrameworksData	= MetadataSection_KeyInfoElement + 1
    } 	MetadataSectionEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0304_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0304_v0_0_s_ifspec;

#ifndef __IMetadataSectionEntry_INTERFACE_DEFINED__
#define __IMetadataSectionEntry_INTERFACE_DEFINED__

/* interface IMetadataSectionEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IMetadataSectionEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("AB1ED79F-943E-407d-A80B-0744E3A95B28")
    IMetadataSectionEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ MetadataSectionEntry **__MIDL_0155) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SchemaVersion( 
            /* [retval][out] */ ULONG *__MIDL_0156) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ManifestFlags( 
            /* [retval][out] */ ULONG *__MIDL_0157) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_UsagePatterns( 
            /* [retval][out] */ ULONG *__MIDL_0158) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CdfIdentity( 
            /* [retval][out] */ IDefinitionIdentity **__MIDL_0159) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LocalPath( 
            /* [retval][out] */ LPCWSTR *__MIDL_0160) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_HashAlgorithm( 
            /* [retval][out] */ ULONG *__MIDL_0161) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ManifestHash( 
            /* [retval][out] */ IStream **__MIDL_0162) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ContentType( 
            /* [retval][out] */ LPCWSTR *__MIDL_0163) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RuntimeImageVersion( 
            /* [retval][out] */ LPCWSTR *__MIDL_0164) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MvidValue( 
            /* [retval][out] */ IStream **__MIDL_0165) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DescriptionData( 
            /* [retval][out] */ IDescriptionMetadataEntry **__MIDL_0166) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DeploymentData( 
            /* [retval][out] */ IDeploymentMetadataEntry **__MIDL_0167) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DependentOSData( 
            /* [retval][out] */ IDependentOSMetadataEntry **__MIDL_0168) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_defaultPermissionSetID( 
            /* [retval][out] */ LPCWSTR *__MIDL_0169) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RequestedExecutionLevel( 
            /* [retval][out] */ LPCWSTR *__MIDL_0170) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RequestedExecutionLevelUIAccess( 
            /* [retval][out] */ BOOLEAN *__MIDL_0171) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ResourceTypeResourcesDependency( 
            /* [retval][out] */ IReferenceIdentity **__MIDL_0172) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ResourceTypeManifestResourcesDependency( 
            /* [retval][out] */ IReferenceIdentity **__MIDL_0173) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_KeyInfoElement( 
            /* [retval][out] */ LPCWSTR *__MIDL_0174) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CompatibleFrameworksData( 
            /* [retval][out] */ ICompatibleFrameworksMetadataEntry **__MIDL_0175) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IMetadataSectionEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IMetadataSectionEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IMetadataSectionEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IMetadataSectionEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ MetadataSectionEntry **__MIDL_0155);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SchemaVersion )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0156);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ManifestFlags )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0157);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_UsagePatterns )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0158);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CdfIdentity )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ IDefinitionIdentity **__MIDL_0159);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_LocalPath )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0160);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_HashAlgorithm )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0161);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ManifestHash )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ IStream **__MIDL_0162);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ContentType )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0163);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RuntimeImageVersion )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0164);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MvidValue )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ IStream **__MIDL_0165);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DescriptionData )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ IDescriptionMetadataEntry **__MIDL_0166);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DeploymentData )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ IDeploymentMetadataEntry **__MIDL_0167);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DependentOSData )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ IDependentOSMetadataEntry **__MIDL_0168);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_defaultPermissionSetID )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0169);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RequestedExecutionLevel )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0170);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RequestedExecutionLevelUIAccess )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ BOOLEAN *__MIDL_0171);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ResourceTypeResourcesDependency )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ IReferenceIdentity **__MIDL_0172);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ResourceTypeManifestResourcesDependency )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ IReferenceIdentity **__MIDL_0173);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_KeyInfoElement )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0174);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CompatibleFrameworksData )( 
            IMetadataSectionEntry * This,
            /* [retval][out] */ ICompatibleFrameworksMetadataEntry **__MIDL_0175);
        
        END_INTERFACE
    } IMetadataSectionEntryVtbl;

    interface IMetadataSectionEntry
    {
        CONST_VTBL struct IMetadataSectionEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IMetadataSectionEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IMetadataSectionEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IMetadataSectionEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IMetadataSectionEntry_get_AllData(This,__MIDL_0155)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0155)

#define IMetadataSectionEntry_get_SchemaVersion(This,__MIDL_0156)	\
    (This)->lpVtbl -> get_SchemaVersion(This,__MIDL_0156)

#define IMetadataSectionEntry_get_ManifestFlags(This,__MIDL_0157)	\
    (This)->lpVtbl -> get_ManifestFlags(This,__MIDL_0157)

#define IMetadataSectionEntry_get_UsagePatterns(This,__MIDL_0158)	\
    (This)->lpVtbl -> get_UsagePatterns(This,__MIDL_0158)

#define IMetadataSectionEntry_get_CdfIdentity(This,__MIDL_0159)	\
    (This)->lpVtbl -> get_CdfIdentity(This,__MIDL_0159)

#define IMetadataSectionEntry_get_LocalPath(This,__MIDL_0160)	\
    (This)->lpVtbl -> get_LocalPath(This,__MIDL_0160)

#define IMetadataSectionEntry_get_HashAlgorithm(This,__MIDL_0161)	\
    (This)->lpVtbl -> get_HashAlgorithm(This,__MIDL_0161)

#define IMetadataSectionEntry_get_ManifestHash(This,__MIDL_0162)	\
    (This)->lpVtbl -> get_ManifestHash(This,__MIDL_0162)

#define IMetadataSectionEntry_get_ContentType(This,__MIDL_0163)	\
    (This)->lpVtbl -> get_ContentType(This,__MIDL_0163)

#define IMetadataSectionEntry_get_RuntimeImageVersion(This,__MIDL_0164)	\
    (This)->lpVtbl -> get_RuntimeImageVersion(This,__MIDL_0164)

#define IMetadataSectionEntry_get_MvidValue(This,__MIDL_0165)	\
    (This)->lpVtbl -> get_MvidValue(This,__MIDL_0165)

#define IMetadataSectionEntry_get_DescriptionData(This,__MIDL_0166)	\
    (This)->lpVtbl -> get_DescriptionData(This,__MIDL_0166)

#define IMetadataSectionEntry_get_DeploymentData(This,__MIDL_0167)	\
    (This)->lpVtbl -> get_DeploymentData(This,__MIDL_0167)

#define IMetadataSectionEntry_get_DependentOSData(This,__MIDL_0168)	\
    (This)->lpVtbl -> get_DependentOSData(This,__MIDL_0168)

#define IMetadataSectionEntry_get_defaultPermissionSetID(This,__MIDL_0169)	\
    (This)->lpVtbl -> get_defaultPermissionSetID(This,__MIDL_0169)

#define IMetadataSectionEntry_get_RequestedExecutionLevel(This,__MIDL_0170)	\
    (This)->lpVtbl -> get_RequestedExecutionLevel(This,__MIDL_0170)

#define IMetadataSectionEntry_get_RequestedExecutionLevelUIAccess(This,__MIDL_0171)	\
    (This)->lpVtbl -> get_RequestedExecutionLevelUIAccess(This,__MIDL_0171)

#define IMetadataSectionEntry_get_ResourceTypeResourcesDependency(This,__MIDL_0172)	\
    (This)->lpVtbl -> get_ResourceTypeResourcesDependency(This,__MIDL_0172)

#define IMetadataSectionEntry_get_ResourceTypeManifestResourcesDependency(This,__MIDL_0173)	\
    (This)->lpVtbl -> get_ResourceTypeManifestResourcesDependency(This,__MIDL_0173)

#define IMetadataSectionEntry_get_KeyInfoElement(This,__MIDL_0174)	\
    (This)->lpVtbl -> get_KeyInfoElement(This,__MIDL_0174)

#define IMetadataSectionEntry_get_CompatibleFrameworksData(This,__MIDL_0175)	\
    (This)->lpVtbl -> get_CompatibleFrameworksData(This,__MIDL_0175)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_AllData_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ MetadataSectionEntry **__MIDL_0155);


void __RPC_STUB IMetadataSectionEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_SchemaVersion_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0156);


void __RPC_STUB IMetadataSectionEntry_get_SchemaVersion_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_ManifestFlags_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0157);


void __RPC_STUB IMetadataSectionEntry_get_ManifestFlags_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_UsagePatterns_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0158);


void __RPC_STUB IMetadataSectionEntry_get_UsagePatterns_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_CdfIdentity_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ IDefinitionIdentity **__MIDL_0159);


void __RPC_STUB IMetadataSectionEntry_get_CdfIdentity_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_LocalPath_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0160);


void __RPC_STUB IMetadataSectionEntry_get_LocalPath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_HashAlgorithm_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0161);


void __RPC_STUB IMetadataSectionEntry_get_HashAlgorithm_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_ManifestHash_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ IStream **__MIDL_0162);


void __RPC_STUB IMetadataSectionEntry_get_ManifestHash_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_ContentType_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0163);


void __RPC_STUB IMetadataSectionEntry_get_ContentType_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_RuntimeImageVersion_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0164);


void __RPC_STUB IMetadataSectionEntry_get_RuntimeImageVersion_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_MvidValue_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ IStream **__MIDL_0165);


void __RPC_STUB IMetadataSectionEntry_get_MvidValue_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_DescriptionData_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ IDescriptionMetadataEntry **__MIDL_0166);


void __RPC_STUB IMetadataSectionEntry_get_DescriptionData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_DeploymentData_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ IDeploymentMetadataEntry **__MIDL_0167);


void __RPC_STUB IMetadataSectionEntry_get_DeploymentData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_DependentOSData_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ IDependentOSMetadataEntry **__MIDL_0168);


void __RPC_STUB IMetadataSectionEntry_get_DependentOSData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_defaultPermissionSetID_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0169);


void __RPC_STUB IMetadataSectionEntry_get_defaultPermissionSetID_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_RequestedExecutionLevel_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0170);


void __RPC_STUB IMetadataSectionEntry_get_RequestedExecutionLevel_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_RequestedExecutionLevelUIAccess_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ BOOLEAN *__MIDL_0171);


void __RPC_STUB IMetadataSectionEntry_get_RequestedExecutionLevelUIAccess_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_ResourceTypeResourcesDependency_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ IReferenceIdentity **__MIDL_0172);


void __RPC_STUB IMetadataSectionEntry_get_ResourceTypeResourcesDependency_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_ResourceTypeManifestResourcesDependency_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ IReferenceIdentity **__MIDL_0173);


void __RPC_STUB IMetadataSectionEntry_get_ResourceTypeManifestResourcesDependency_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_KeyInfoElement_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0174);


void __RPC_STUB IMetadataSectionEntry_get_KeyInfoElement_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMetadataSectionEntry_get_CompatibleFrameworksData_Proxy( 
    IMetadataSectionEntry * This,
    /* [retval][out] */ ICompatibleFrameworksMetadataEntry **__MIDL_0175);


void __RPC_STUB IMetadataSectionEntry_get_CompatibleFrameworksData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IMetadataSectionEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0305 */
/* [local] */ 

typedef struct _EventEntry
    {
    ULONG EventID;
    ULONG Level;
    ULONG Version;
    GUID Guid;
    LPCWSTR SubTypeName;
    ULONG SubTypeValue;
    LPCWSTR DisplayName;
    ULONG EventNameMicrodomIndex;
    } 	EventEntry;

typedef 
enum _EventEntryFieldId
    {	Event_Level	= 0,
	Event_Version	= Event_Level + 1,
	Event_Guid	= Event_Version + 1,
	Event_SubTypeName	= Event_Guid + 1,
	Event_SubTypeValue	= Event_SubTypeName + 1,
	Event_DisplayName	= Event_SubTypeValue + 1,
	Event_EventNameMicrodomIndex	= Event_DisplayName + 1
    } 	EventEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0305_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0305_v0_0_s_ifspec;

#ifndef __IEventEntry_INTERFACE_DEFINED__
#define __IEventEntry_INTERFACE_DEFINED__

/* interface IEventEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IEventEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("8AD3FC86-AFD3-477a-8FD5-146C291195BB")
    IEventEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ EventEntry **__MIDL_0176) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_EventID( 
            /* [retval][out] */ ULONG *__MIDL_0177) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Level( 
            /* [retval][out] */ ULONG *__MIDL_0178) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Version( 
            /* [retval][out] */ ULONG *__MIDL_0179) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Guid( 
            /* [retval][out] */ GUID *__MIDL_0180) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SubTypeName( 
            /* [retval][out] */ LPCWSTR *__MIDL_0181) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SubTypeValue( 
            /* [retval][out] */ ULONG *__MIDL_0182) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DisplayName( 
            /* [retval][out] */ LPCWSTR *__MIDL_0183) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_EventNameMicrodomIndex( 
            /* [retval][out] */ ULONG *__MIDL_0184) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEventEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEventEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEventEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEventEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IEventEntry * This,
            /* [retval][out] */ EventEntry **__MIDL_0176);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_EventID )( 
            IEventEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0177);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Level )( 
            IEventEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0178);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Version )( 
            IEventEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0179);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Guid )( 
            IEventEntry * This,
            /* [retval][out] */ GUID *__MIDL_0180);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SubTypeName )( 
            IEventEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0181);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SubTypeValue )( 
            IEventEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0182);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DisplayName )( 
            IEventEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0183);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_EventNameMicrodomIndex )( 
            IEventEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0184);
        
        END_INTERFACE
    } IEventEntryVtbl;

    interface IEventEntry
    {
        CONST_VTBL struct IEventEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEventEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEventEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEventEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEventEntry_get_AllData(This,__MIDL_0176)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0176)

#define IEventEntry_get_EventID(This,__MIDL_0177)	\
    (This)->lpVtbl -> get_EventID(This,__MIDL_0177)

#define IEventEntry_get_Level(This,__MIDL_0178)	\
    (This)->lpVtbl -> get_Level(This,__MIDL_0178)

#define IEventEntry_get_Version(This,__MIDL_0179)	\
    (This)->lpVtbl -> get_Version(This,__MIDL_0179)

#define IEventEntry_get_Guid(This,__MIDL_0180)	\
    (This)->lpVtbl -> get_Guid(This,__MIDL_0180)

#define IEventEntry_get_SubTypeName(This,__MIDL_0181)	\
    (This)->lpVtbl -> get_SubTypeName(This,__MIDL_0181)

#define IEventEntry_get_SubTypeValue(This,__MIDL_0182)	\
    (This)->lpVtbl -> get_SubTypeValue(This,__MIDL_0182)

#define IEventEntry_get_DisplayName(This,__MIDL_0183)	\
    (This)->lpVtbl -> get_DisplayName(This,__MIDL_0183)

#define IEventEntry_get_EventNameMicrodomIndex(This,__MIDL_0184)	\
    (This)->lpVtbl -> get_EventNameMicrodomIndex(This,__MIDL_0184)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IEventEntry_get_AllData_Proxy( 
    IEventEntry * This,
    /* [retval][out] */ EventEntry **__MIDL_0176);


void __RPC_STUB IEventEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IEventEntry_get_EventID_Proxy( 
    IEventEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0177);


void __RPC_STUB IEventEntry_get_EventID_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IEventEntry_get_Level_Proxy( 
    IEventEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0178);


void __RPC_STUB IEventEntry_get_Level_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IEventEntry_get_Version_Proxy( 
    IEventEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0179);


void __RPC_STUB IEventEntry_get_Version_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IEventEntry_get_Guid_Proxy( 
    IEventEntry * This,
    /* [retval][out] */ GUID *__MIDL_0180);


void __RPC_STUB IEventEntry_get_Guid_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IEventEntry_get_SubTypeName_Proxy( 
    IEventEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0181);


void __RPC_STUB IEventEntry_get_SubTypeName_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IEventEntry_get_SubTypeValue_Proxy( 
    IEventEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0182);


void __RPC_STUB IEventEntry_get_SubTypeValue_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IEventEntry_get_DisplayName_Proxy( 
    IEventEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0183);


void __RPC_STUB IEventEntry_get_DisplayName_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IEventEntry_get_EventNameMicrodomIndex_Proxy( 
    IEventEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0184);


void __RPC_STUB IEventEntry_get_EventNameMicrodomIndex_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEventEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0306 */
/* [local] */ 

typedef struct _EventMapEntry
    {
    LPCWSTR MapName;
    LPCWSTR Name;
    ULONG Value;
    BOOLEAN IsValueMap;
    } 	EventMapEntry;

typedef 
enum _EventMapEntryFieldId
    {	EventMap_Name	= 0,
	EventMap_Value	= EventMap_Name + 1,
	EventMap_IsValueMap	= EventMap_Value + 1
    } 	EventMapEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0306_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0306_v0_0_s_ifspec;

#ifndef __IEventMapEntry_INTERFACE_DEFINED__
#define __IEventMapEntry_INTERFACE_DEFINED__

/* interface IEventMapEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IEventMapEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("8AD3FC86-AFD3-477a-8FD5-146C291195BC")
    IEventMapEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ EventMapEntry **__MIDL_0185) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MapName( 
            /* [retval][out] */ LPCWSTR *__MIDL_0186) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Name( 
            /* [retval][out] */ LPCWSTR *__MIDL_0187) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Value( 
            /* [retval][out] */ ULONG *__MIDL_0188) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsValueMap( 
            /* [retval][out] */ BOOLEAN *__MIDL_0189) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEventMapEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEventMapEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEventMapEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEventMapEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IEventMapEntry * This,
            /* [retval][out] */ EventMapEntry **__MIDL_0185);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MapName )( 
            IEventMapEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0186);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Name )( 
            IEventMapEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0187);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Value )( 
            IEventMapEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0188);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsValueMap )( 
            IEventMapEntry * This,
            /* [retval][out] */ BOOLEAN *__MIDL_0189);
        
        END_INTERFACE
    } IEventMapEntryVtbl;

    interface IEventMapEntry
    {
        CONST_VTBL struct IEventMapEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEventMapEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEventMapEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEventMapEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEventMapEntry_get_AllData(This,__MIDL_0185)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0185)

#define IEventMapEntry_get_MapName(This,__MIDL_0186)	\
    (This)->lpVtbl -> get_MapName(This,__MIDL_0186)

#define IEventMapEntry_get_Name(This,__MIDL_0187)	\
    (This)->lpVtbl -> get_Name(This,__MIDL_0187)

#define IEventMapEntry_get_Value(This,__MIDL_0188)	\
    (This)->lpVtbl -> get_Value(This,__MIDL_0188)

#define IEventMapEntry_get_IsValueMap(This,__MIDL_0189)	\
    (This)->lpVtbl -> get_IsValueMap(This,__MIDL_0189)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IEventMapEntry_get_AllData_Proxy( 
    IEventMapEntry * This,
    /* [retval][out] */ EventMapEntry **__MIDL_0185);


void __RPC_STUB IEventMapEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IEventMapEntry_get_MapName_Proxy( 
    IEventMapEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0186);


void __RPC_STUB IEventMapEntry_get_MapName_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IEventMapEntry_get_Name_Proxy( 
    IEventMapEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0187);


void __RPC_STUB IEventMapEntry_get_Name_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IEventMapEntry_get_Value_Proxy( 
    IEventMapEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0188);


void __RPC_STUB IEventMapEntry_get_Value_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IEventMapEntry_get_IsValueMap_Proxy( 
    IEventMapEntry * This,
    /* [retval][out] */ BOOLEAN *__MIDL_0189);


void __RPC_STUB IEventMapEntry_get_IsValueMap_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEventMapEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0307 */
/* [local] */ 

typedef struct _EventTagEntry
    {
    LPCWSTR TagData;
    ULONG EventID;
    } 	EventTagEntry;

typedef 
enum _EventTagEntryFieldId
    {	EventTag_EventID	= 0
    } 	EventTagEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0307_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0307_v0_0_s_ifspec;

#ifndef __IEventTagEntry_INTERFACE_DEFINED__
#define __IEventTagEntry_INTERFACE_DEFINED__

/* interface IEventTagEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IEventTagEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("8AD3FC86-AFD3-477a-8FD5-146C291195BD")
    IEventTagEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ EventTagEntry **__MIDL_0190) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TagData( 
            /* [retval][out] */ LPCWSTR *__MIDL_0191) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_EventID( 
            /* [retval][out] */ ULONG *__MIDL_0192) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEventTagEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEventTagEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEventTagEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEventTagEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IEventTagEntry * This,
            /* [retval][out] */ EventTagEntry **__MIDL_0190);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TagData )( 
            IEventTagEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0191);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_EventID )( 
            IEventTagEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0192);
        
        END_INTERFACE
    } IEventTagEntryVtbl;

    interface IEventTagEntry
    {
        CONST_VTBL struct IEventTagEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEventTagEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEventTagEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEventTagEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEventTagEntry_get_AllData(This,__MIDL_0190)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0190)

#define IEventTagEntry_get_TagData(This,__MIDL_0191)	\
    (This)->lpVtbl -> get_TagData(This,__MIDL_0191)

#define IEventTagEntry_get_EventID(This,__MIDL_0192)	\
    (This)->lpVtbl -> get_EventID(This,__MIDL_0192)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IEventTagEntry_get_AllData_Proxy( 
    IEventTagEntry * This,
    /* [retval][out] */ EventTagEntry **__MIDL_0190);


void __RPC_STUB IEventTagEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IEventTagEntry_get_TagData_Proxy( 
    IEventTagEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0191);


void __RPC_STUB IEventTagEntry_get_TagData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IEventTagEntry_get_EventID_Proxy( 
    IEventTagEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0192);


void __RPC_STUB IEventTagEntry_get_EventID_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEventTagEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0308 */
/* [local] */ 

typedef struct _RegistryValueEntry
    {
    ULONG Flags;
    ULONG OperationHint;
    ULONG Type;
    LPCWSTR Value;
    LPCWSTR BuildFilter;
    } 	RegistryValueEntry;

typedef 
enum _RegistryValueEntryFieldId
    {	RegistryValue_Flags	= 0,
	RegistryValue_OperationHint	= RegistryValue_Flags + 1,
	RegistryValue_Type	= RegistryValue_OperationHint + 1,
	RegistryValue_Value	= RegistryValue_Type + 1,
	RegistryValue_BuildFilter	= RegistryValue_Value + 1
    } 	RegistryValueEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0308_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0308_v0_0_s_ifspec;

#ifndef __IRegistryValueEntry_INTERFACE_DEFINED__
#define __IRegistryValueEntry_INTERFACE_DEFINED__

/* interface IRegistryValueEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IRegistryValueEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("49e1fe8d-ebb8-4593-8c4e-3e14c845b142")
    IRegistryValueEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ RegistryValueEntry **__MIDL_0193) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Flags( 
            /* [retval][out] */ ULONG *__MIDL_0194) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_OperationHint( 
            /* [retval][out] */ ULONG *__MIDL_0195) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Type( 
            /* [retval][out] */ ULONG *__MIDL_0196) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Value( 
            /* [retval][out] */ LPCWSTR *__MIDL_0197) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_BuildFilter( 
            /* [retval][out] */ LPCWSTR *__MIDL_0198) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IRegistryValueEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IRegistryValueEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IRegistryValueEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IRegistryValueEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IRegistryValueEntry * This,
            /* [retval][out] */ RegistryValueEntry **__MIDL_0193);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Flags )( 
            IRegistryValueEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0194);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_OperationHint )( 
            IRegistryValueEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0195);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Type )( 
            IRegistryValueEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0196);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Value )( 
            IRegistryValueEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0197);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_BuildFilter )( 
            IRegistryValueEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0198);
        
        END_INTERFACE
    } IRegistryValueEntryVtbl;

    interface IRegistryValueEntry
    {
        CONST_VTBL struct IRegistryValueEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IRegistryValueEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IRegistryValueEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IRegistryValueEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IRegistryValueEntry_get_AllData(This,__MIDL_0193)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0193)

#define IRegistryValueEntry_get_Flags(This,__MIDL_0194)	\
    (This)->lpVtbl -> get_Flags(This,__MIDL_0194)

#define IRegistryValueEntry_get_OperationHint(This,__MIDL_0195)	\
    (This)->lpVtbl -> get_OperationHint(This,__MIDL_0195)

#define IRegistryValueEntry_get_Type(This,__MIDL_0196)	\
    (This)->lpVtbl -> get_Type(This,__MIDL_0196)

#define IRegistryValueEntry_get_Value(This,__MIDL_0197)	\
    (This)->lpVtbl -> get_Value(This,__MIDL_0197)

#define IRegistryValueEntry_get_BuildFilter(This,__MIDL_0198)	\
    (This)->lpVtbl -> get_BuildFilter(This,__MIDL_0198)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IRegistryValueEntry_get_AllData_Proxy( 
    IRegistryValueEntry * This,
    /* [retval][out] */ RegistryValueEntry **__MIDL_0193);


void __RPC_STUB IRegistryValueEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IRegistryValueEntry_get_Flags_Proxy( 
    IRegistryValueEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0194);


void __RPC_STUB IRegistryValueEntry_get_Flags_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IRegistryValueEntry_get_OperationHint_Proxy( 
    IRegistryValueEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0195);


void __RPC_STUB IRegistryValueEntry_get_OperationHint_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IRegistryValueEntry_get_Type_Proxy( 
    IRegistryValueEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0196);


void __RPC_STUB IRegistryValueEntry_get_Type_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IRegistryValueEntry_get_Value_Proxy( 
    IRegistryValueEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0197);


void __RPC_STUB IRegistryValueEntry_get_Value_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IRegistryValueEntry_get_BuildFilter_Proxy( 
    IRegistryValueEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0198);


void __RPC_STUB IRegistryValueEntry_get_BuildFilter_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IRegistryValueEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0309 */
/* [local] */ 

typedef struct _RegistryKeyEntry
    {
    ULONG Flags;
    ULONG Protection;
    LPCWSTR BuildFilter;
    /* [size_is] */ BYTE *SecurityDescriptor;
    ULONG SecurityDescriptorSize;
    /* [size_is] */ BYTE *Values;
    ULONG ValuesSize;
    /* [size_is] */ BYTE *Keys;
    ULONG KeysSize;
    } 	RegistryKeyEntry;

typedef 
enum _RegistryKeyEntryFieldId
    {	RegistryKey_Flags	= 0,
	RegistryKey_Protection	= RegistryKey_Flags + 1,
	RegistryKey_BuildFilter	= RegistryKey_Protection + 1,
	RegistryKey_SecurityDescriptor	= RegistryKey_BuildFilter + 1,
	RegistryKey_SecurityDescriptorSize	= RegistryKey_SecurityDescriptor + 1,
	RegistryKey_Values	= RegistryKey_SecurityDescriptorSize + 1,
	RegistryKey_ValuesSize	= RegistryKey_Values + 1,
	RegistryKey_Keys	= RegistryKey_ValuesSize + 1,
	RegistryKey_KeysSize	= RegistryKey_Keys + 1
    } 	RegistryKeyEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0309_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0309_v0_0_s_ifspec;

#ifndef __IRegistryKeyEntry_INTERFACE_DEFINED__
#define __IRegistryKeyEntry_INTERFACE_DEFINED__

/* interface IRegistryKeyEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IRegistryKeyEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("186685d1-6673-48c3-bc83-95859bb591df")
    IRegistryKeyEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ RegistryKeyEntry **__MIDL_0199) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Flags( 
            /* [retval][out] */ ULONG *__MIDL_0200) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Protection( 
            /* [retval][out] */ ULONG *__MIDL_0201) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_BuildFilter( 
            /* [retval][out] */ LPCWSTR *__MIDL_0202) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SecurityDescriptor( 
            /* [retval][out] */ IStream **__MIDL_0203) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Values( 
            /* [retval][out] */ IStream **__MIDL_0204) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Keys( 
            /* [retval][out] */ IStream **__MIDL_0205) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IRegistryKeyEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IRegistryKeyEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IRegistryKeyEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IRegistryKeyEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IRegistryKeyEntry * This,
            /* [retval][out] */ RegistryKeyEntry **__MIDL_0199);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Flags )( 
            IRegistryKeyEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0200);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Protection )( 
            IRegistryKeyEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0201);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_BuildFilter )( 
            IRegistryKeyEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0202);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SecurityDescriptor )( 
            IRegistryKeyEntry * This,
            /* [retval][out] */ IStream **__MIDL_0203);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Values )( 
            IRegistryKeyEntry * This,
            /* [retval][out] */ IStream **__MIDL_0204);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Keys )( 
            IRegistryKeyEntry * This,
            /* [retval][out] */ IStream **__MIDL_0205);
        
        END_INTERFACE
    } IRegistryKeyEntryVtbl;

    interface IRegistryKeyEntry
    {
        CONST_VTBL struct IRegistryKeyEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IRegistryKeyEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IRegistryKeyEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IRegistryKeyEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IRegistryKeyEntry_get_AllData(This,__MIDL_0199)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0199)

#define IRegistryKeyEntry_get_Flags(This,__MIDL_0200)	\
    (This)->lpVtbl -> get_Flags(This,__MIDL_0200)

#define IRegistryKeyEntry_get_Protection(This,__MIDL_0201)	\
    (This)->lpVtbl -> get_Protection(This,__MIDL_0201)

#define IRegistryKeyEntry_get_BuildFilter(This,__MIDL_0202)	\
    (This)->lpVtbl -> get_BuildFilter(This,__MIDL_0202)

#define IRegistryKeyEntry_get_SecurityDescriptor(This,__MIDL_0203)	\
    (This)->lpVtbl -> get_SecurityDescriptor(This,__MIDL_0203)

#define IRegistryKeyEntry_get_Values(This,__MIDL_0204)	\
    (This)->lpVtbl -> get_Values(This,__MIDL_0204)

#define IRegistryKeyEntry_get_Keys(This,__MIDL_0205)	\
    (This)->lpVtbl -> get_Keys(This,__MIDL_0205)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IRegistryKeyEntry_get_AllData_Proxy( 
    IRegistryKeyEntry * This,
    /* [retval][out] */ RegistryKeyEntry **__MIDL_0199);


void __RPC_STUB IRegistryKeyEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IRegistryKeyEntry_get_Flags_Proxy( 
    IRegistryKeyEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0200);


void __RPC_STUB IRegistryKeyEntry_get_Flags_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IRegistryKeyEntry_get_Protection_Proxy( 
    IRegistryKeyEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0201);


void __RPC_STUB IRegistryKeyEntry_get_Protection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IRegistryKeyEntry_get_BuildFilter_Proxy( 
    IRegistryKeyEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0202);


void __RPC_STUB IRegistryKeyEntry_get_BuildFilter_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IRegistryKeyEntry_get_SecurityDescriptor_Proxy( 
    IRegistryKeyEntry * This,
    /* [retval][out] */ IStream **__MIDL_0203);


void __RPC_STUB IRegistryKeyEntry_get_SecurityDescriptor_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IRegistryKeyEntry_get_Values_Proxy( 
    IRegistryKeyEntry * This,
    /* [retval][out] */ IStream **__MIDL_0204);


void __RPC_STUB IRegistryKeyEntry_get_Values_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IRegistryKeyEntry_get_Keys_Proxy( 
    IRegistryKeyEntry * This,
    /* [retval][out] */ IStream **__MIDL_0205);


void __RPC_STUB IRegistryKeyEntry_get_Keys_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IRegistryKeyEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0310 */
/* [local] */ 

typedef struct _DirectoryEntry
    {
    ULONG Flags;
    ULONG Protection;
    LPCWSTR BuildFilter;
    /* [size_is] */ BYTE *SecurityDescriptor;
    ULONG SecurityDescriptorSize;
    } 	DirectoryEntry;

typedef 
enum _DirectoryEntryFieldId
    {	Directory_Flags	= 0,
	Directory_Protection	= Directory_Flags + 1,
	Directory_BuildFilter	= Directory_Protection + 1,
	Directory_SecurityDescriptor	= Directory_BuildFilter + 1,
	Directory_SecurityDescriptorSize	= Directory_SecurityDescriptor + 1
    } 	DirectoryEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0310_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0310_v0_0_s_ifspec;

#ifndef __IDirectoryEntry_INTERFACE_DEFINED__
#define __IDirectoryEntry_INTERFACE_DEFINED__

/* interface IDirectoryEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IDirectoryEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("9f27c750-7dfb-46a1-a673-52e53e2337a9")
    IDirectoryEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ DirectoryEntry **__MIDL_0206) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Flags( 
            /* [retval][out] */ ULONG *__MIDL_0207) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Protection( 
            /* [retval][out] */ ULONG *__MIDL_0208) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_BuildFilter( 
            /* [retval][out] */ LPCWSTR *__MIDL_0209) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SecurityDescriptor( 
            /* [retval][out] */ IStream **__MIDL_0210) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IDirectoryEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDirectoryEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDirectoryEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDirectoryEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IDirectoryEntry * This,
            /* [retval][out] */ DirectoryEntry **__MIDL_0206);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Flags )( 
            IDirectoryEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0207);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Protection )( 
            IDirectoryEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0208);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_BuildFilter )( 
            IDirectoryEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0209);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SecurityDescriptor )( 
            IDirectoryEntry * This,
            /* [retval][out] */ IStream **__MIDL_0210);
        
        END_INTERFACE
    } IDirectoryEntryVtbl;

    interface IDirectoryEntry
    {
        CONST_VTBL struct IDirectoryEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDirectoryEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IDirectoryEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IDirectoryEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IDirectoryEntry_get_AllData(This,__MIDL_0206)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0206)

#define IDirectoryEntry_get_Flags(This,__MIDL_0207)	\
    (This)->lpVtbl -> get_Flags(This,__MIDL_0207)

#define IDirectoryEntry_get_Protection(This,__MIDL_0208)	\
    (This)->lpVtbl -> get_Protection(This,__MIDL_0208)

#define IDirectoryEntry_get_BuildFilter(This,__MIDL_0209)	\
    (This)->lpVtbl -> get_BuildFilter(This,__MIDL_0209)

#define IDirectoryEntry_get_SecurityDescriptor(This,__MIDL_0210)	\
    (This)->lpVtbl -> get_SecurityDescriptor(This,__MIDL_0210)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IDirectoryEntry_get_AllData_Proxy( 
    IDirectoryEntry * This,
    /* [retval][out] */ DirectoryEntry **__MIDL_0206);


void __RPC_STUB IDirectoryEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDirectoryEntry_get_Flags_Proxy( 
    IDirectoryEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0207);


void __RPC_STUB IDirectoryEntry_get_Flags_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDirectoryEntry_get_Protection_Proxy( 
    IDirectoryEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0208);


void __RPC_STUB IDirectoryEntry_get_Protection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDirectoryEntry_get_BuildFilter_Proxy( 
    IDirectoryEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0209);


void __RPC_STUB IDirectoryEntry_get_BuildFilter_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDirectoryEntry_get_SecurityDescriptor_Proxy( 
    IDirectoryEntry * This,
    /* [retval][out] */ IStream **__MIDL_0210);


void __RPC_STUB IDirectoryEntry_get_SecurityDescriptor_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IDirectoryEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0311 */
/* [local] */ 

typedef struct _SecurityDescriptorReferenceEntry
    {
    LPCWSTR Name;
    LPCWSTR BuildFilter;
    } 	SecurityDescriptorReferenceEntry;

typedef 
enum _SecurityDescriptorReferenceEntryFieldId
    {	SecurityDescriptorReference_Name	= 0,
	SecurityDescriptorReference_BuildFilter	= SecurityDescriptorReference_Name + 1
    } 	SecurityDescriptorReferenceEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0311_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0311_v0_0_s_ifspec;

#ifndef __ISecurityDescriptorReferenceEntry_INTERFACE_DEFINED__
#define __ISecurityDescriptorReferenceEntry_INTERFACE_DEFINED__

/* interface ISecurityDescriptorReferenceEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_ISecurityDescriptorReferenceEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("a75b74e9-2c00-4ebb-b3f9-62a670aaa07e")
    ISecurityDescriptorReferenceEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ SecurityDescriptorReferenceEntry **__MIDL_0211) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Name( 
            /* [retval][out] */ LPCWSTR *__MIDL_0212) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_BuildFilter( 
            /* [retval][out] */ LPCWSTR *__MIDL_0213) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ISecurityDescriptorReferenceEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISecurityDescriptorReferenceEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISecurityDescriptorReferenceEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISecurityDescriptorReferenceEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            ISecurityDescriptorReferenceEntry * This,
            /* [retval][out] */ SecurityDescriptorReferenceEntry **__MIDL_0211);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Name )( 
            ISecurityDescriptorReferenceEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0212);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_BuildFilter )( 
            ISecurityDescriptorReferenceEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0213);
        
        END_INTERFACE
    } ISecurityDescriptorReferenceEntryVtbl;

    interface ISecurityDescriptorReferenceEntry
    {
        CONST_VTBL struct ISecurityDescriptorReferenceEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISecurityDescriptorReferenceEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ISecurityDescriptorReferenceEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ISecurityDescriptorReferenceEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ISecurityDescriptorReferenceEntry_get_AllData(This,__MIDL_0211)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0211)

#define ISecurityDescriptorReferenceEntry_get_Name(This,__MIDL_0212)	\
    (This)->lpVtbl -> get_Name(This,__MIDL_0212)

#define ISecurityDescriptorReferenceEntry_get_BuildFilter(This,__MIDL_0213)	\
    (This)->lpVtbl -> get_BuildFilter(This,__MIDL_0213)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ISecurityDescriptorReferenceEntry_get_AllData_Proxy( 
    ISecurityDescriptorReferenceEntry * This,
    /* [retval][out] */ SecurityDescriptorReferenceEntry **__MIDL_0211);


void __RPC_STUB ISecurityDescriptorReferenceEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISecurityDescriptorReferenceEntry_get_Name_Proxy( 
    ISecurityDescriptorReferenceEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0212);


void __RPC_STUB ISecurityDescriptorReferenceEntry_get_Name_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISecurityDescriptorReferenceEntry_get_BuildFilter_Proxy( 
    ISecurityDescriptorReferenceEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0213);


void __RPC_STUB ISecurityDescriptorReferenceEntry_get_BuildFilter_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ISecurityDescriptorReferenceEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0312 */
/* [local] */ 

typedef struct _CounterSetEntry
    {
    GUID CounterSetGuid;
    GUID ProviderGuid;
    LPCWSTR Name;
    LPCWSTR Description;
    BOOLEAN InstanceType;
    } 	CounterSetEntry;

typedef 
enum _CounterSetEntryFieldId
    {	CounterSet_ProviderGuid	= 0,
	CounterSet_Name	= CounterSet_ProviderGuid + 1,
	CounterSet_Description	= CounterSet_Name + 1,
	CounterSet_InstanceType	= CounterSet_Description + 1
    } 	CounterSetEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0312_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0312_v0_0_s_ifspec;

#ifndef __ICounterSetEntry_INTERFACE_DEFINED__
#define __ICounterSetEntry_INTERFACE_DEFINED__

/* interface ICounterSetEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_ICounterSetEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("8CD3FC85-AFD3-477a-8FD5-146C291195BB")
    ICounterSetEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ CounterSetEntry **__MIDL_0214) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CounterSetGuid( 
            /* [retval][out] */ GUID *__MIDL_0215) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ProviderGuid( 
            /* [retval][out] */ GUID *__MIDL_0216) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Name( 
            /* [retval][out] */ LPCWSTR *__MIDL_0217) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Description( 
            /* [retval][out] */ LPCWSTR *__MIDL_0218) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_InstanceType( 
            /* [retval][out] */ BOOLEAN *__MIDL_0219) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ICounterSetEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICounterSetEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICounterSetEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICounterSetEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            ICounterSetEntry * This,
            /* [retval][out] */ CounterSetEntry **__MIDL_0214);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CounterSetGuid )( 
            ICounterSetEntry * This,
            /* [retval][out] */ GUID *__MIDL_0215);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ProviderGuid )( 
            ICounterSetEntry * This,
            /* [retval][out] */ GUID *__MIDL_0216);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Name )( 
            ICounterSetEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0217);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Description )( 
            ICounterSetEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0218);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_InstanceType )( 
            ICounterSetEntry * This,
            /* [retval][out] */ BOOLEAN *__MIDL_0219);
        
        END_INTERFACE
    } ICounterSetEntryVtbl;

    interface ICounterSetEntry
    {
        CONST_VTBL struct ICounterSetEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICounterSetEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ICounterSetEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ICounterSetEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ICounterSetEntry_get_AllData(This,__MIDL_0214)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0214)

#define ICounterSetEntry_get_CounterSetGuid(This,__MIDL_0215)	\
    (This)->lpVtbl -> get_CounterSetGuid(This,__MIDL_0215)

#define ICounterSetEntry_get_ProviderGuid(This,__MIDL_0216)	\
    (This)->lpVtbl -> get_ProviderGuid(This,__MIDL_0216)

#define ICounterSetEntry_get_Name(This,__MIDL_0217)	\
    (This)->lpVtbl -> get_Name(This,__MIDL_0217)

#define ICounterSetEntry_get_Description(This,__MIDL_0218)	\
    (This)->lpVtbl -> get_Description(This,__MIDL_0218)

#define ICounterSetEntry_get_InstanceType(This,__MIDL_0219)	\
    (This)->lpVtbl -> get_InstanceType(This,__MIDL_0219)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ICounterSetEntry_get_AllData_Proxy( 
    ICounterSetEntry * This,
    /* [retval][out] */ CounterSetEntry **__MIDL_0214);


void __RPC_STUB ICounterSetEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICounterSetEntry_get_CounterSetGuid_Proxy( 
    ICounterSetEntry * This,
    /* [retval][out] */ GUID *__MIDL_0215);


void __RPC_STUB ICounterSetEntry_get_CounterSetGuid_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICounterSetEntry_get_ProviderGuid_Proxy( 
    ICounterSetEntry * This,
    /* [retval][out] */ GUID *__MIDL_0216);


void __RPC_STUB ICounterSetEntry_get_ProviderGuid_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICounterSetEntry_get_Name_Proxy( 
    ICounterSetEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0217);


void __RPC_STUB ICounterSetEntry_get_Name_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICounterSetEntry_get_Description_Proxy( 
    ICounterSetEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0218);


void __RPC_STUB ICounterSetEntry_get_Description_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICounterSetEntry_get_InstanceType_Proxy( 
    ICounterSetEntry * This,
    /* [retval][out] */ BOOLEAN *__MIDL_0219);


void __RPC_STUB ICounterSetEntry_get_InstanceType_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ICounterSetEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0313 */
/* [local] */ 

typedef struct _CounterEntry
    {
    GUID CounterSetGuid;
    ULONG CounterId;
    LPCWSTR Name;
    LPCWSTR Description;
    ULONG CounterType;
    ULONGLONG Attributes;
    ULONG BaseId;
    ULONG DefaultScale;
    } 	CounterEntry;

typedef 
enum _CounterEntryFieldId
    {	Counter_CounterId	= 0,
	Counter_Name	= Counter_CounterId + 1,
	Counter_Description	= Counter_Name + 1,
	Counter_CounterType	= Counter_Description + 1,
	Counter_Attributes	= Counter_CounterType + 1,
	Counter_BaseId	= Counter_Attributes + 1,
	Counter_DefaultScale	= Counter_BaseId + 1
    } 	CounterEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0313_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0313_v0_0_s_ifspec;

#ifndef __ICounterEntry_INTERFACE_DEFINED__
#define __ICounterEntry_INTERFACE_DEFINED__

/* interface ICounterEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_ICounterEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("8CD3FC86-AFD3-477a-8FD5-146C291195BB")
    ICounterEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ CounterEntry **__MIDL_0220) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CounterSetGuid( 
            /* [retval][out] */ GUID *__MIDL_0221) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CounterId( 
            /* [retval][out] */ ULONG *__MIDL_0222) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Name( 
            /* [retval][out] */ LPCWSTR *__MIDL_0223) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Description( 
            /* [retval][out] */ LPCWSTR *__MIDL_0224) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CounterType( 
            /* [retval][out] */ ULONG *__MIDL_0225) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Attributes( 
            /* [retval][out] */ ULONGLONG *__MIDL_0226) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_BaseId( 
            /* [retval][out] */ ULONG *__MIDL_0227) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DefaultScale( 
            /* [retval][out] */ ULONG *__MIDL_0228) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ICounterEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICounterEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICounterEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICounterEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            ICounterEntry * This,
            /* [retval][out] */ CounterEntry **__MIDL_0220);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CounterSetGuid )( 
            ICounterEntry * This,
            /* [retval][out] */ GUID *__MIDL_0221);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CounterId )( 
            ICounterEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0222);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Name )( 
            ICounterEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0223);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Description )( 
            ICounterEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0224);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CounterType )( 
            ICounterEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0225);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Attributes )( 
            ICounterEntry * This,
            /* [retval][out] */ ULONGLONG *__MIDL_0226);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_BaseId )( 
            ICounterEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0227);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DefaultScale )( 
            ICounterEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0228);
        
        END_INTERFACE
    } ICounterEntryVtbl;

    interface ICounterEntry
    {
        CONST_VTBL struct ICounterEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICounterEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ICounterEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ICounterEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ICounterEntry_get_AllData(This,__MIDL_0220)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0220)

#define ICounterEntry_get_CounterSetGuid(This,__MIDL_0221)	\
    (This)->lpVtbl -> get_CounterSetGuid(This,__MIDL_0221)

#define ICounterEntry_get_CounterId(This,__MIDL_0222)	\
    (This)->lpVtbl -> get_CounterId(This,__MIDL_0222)

#define ICounterEntry_get_Name(This,__MIDL_0223)	\
    (This)->lpVtbl -> get_Name(This,__MIDL_0223)

#define ICounterEntry_get_Description(This,__MIDL_0224)	\
    (This)->lpVtbl -> get_Description(This,__MIDL_0224)

#define ICounterEntry_get_CounterType(This,__MIDL_0225)	\
    (This)->lpVtbl -> get_CounterType(This,__MIDL_0225)

#define ICounterEntry_get_Attributes(This,__MIDL_0226)	\
    (This)->lpVtbl -> get_Attributes(This,__MIDL_0226)

#define ICounterEntry_get_BaseId(This,__MIDL_0227)	\
    (This)->lpVtbl -> get_BaseId(This,__MIDL_0227)

#define ICounterEntry_get_DefaultScale(This,__MIDL_0228)	\
    (This)->lpVtbl -> get_DefaultScale(This,__MIDL_0228)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ICounterEntry_get_AllData_Proxy( 
    ICounterEntry * This,
    /* [retval][out] */ CounterEntry **__MIDL_0220);


void __RPC_STUB ICounterEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICounterEntry_get_CounterSetGuid_Proxy( 
    ICounterEntry * This,
    /* [retval][out] */ GUID *__MIDL_0221);


void __RPC_STUB ICounterEntry_get_CounterSetGuid_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICounterEntry_get_CounterId_Proxy( 
    ICounterEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0222);


void __RPC_STUB ICounterEntry_get_CounterId_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICounterEntry_get_Name_Proxy( 
    ICounterEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0223);


void __RPC_STUB ICounterEntry_get_Name_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICounterEntry_get_Description_Proxy( 
    ICounterEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0224);


void __RPC_STUB ICounterEntry_get_Description_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICounterEntry_get_CounterType_Proxy( 
    ICounterEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0225);


void __RPC_STUB ICounterEntry_get_CounterType_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICounterEntry_get_Attributes_Proxy( 
    ICounterEntry * This,
    /* [retval][out] */ ULONGLONG *__MIDL_0226);


void __RPC_STUB ICounterEntry_get_Attributes_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICounterEntry_get_BaseId_Proxy( 
    ICounterEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0227);


void __RPC_STUB ICounterEntry_get_BaseId_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICounterEntry_get_DefaultScale_Proxy( 
    ICounterEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0228);


void __RPC_STUB ICounterEntry_get_DefaultScale_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ICounterEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0314 */
/* [local] */ 

typedef struct _CompatibleFrameworkEntry
    {
    ULONG index;
    LPCWSTR TargetVersion;
    LPCWSTR Profile;
    LPCWSTR SupportedRuntime;
    } 	CompatibleFrameworkEntry;

typedef 
enum _CompatibleFrameworkEntryFieldId
    {	CompatibleFramework_TargetVersion	= 0,
	CompatibleFramework_Profile	= CompatibleFramework_TargetVersion + 1,
	CompatibleFramework_SupportedRuntime	= CompatibleFramework_Profile + 1
    } 	CompatibleFrameworkEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0314_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0314_v0_0_s_ifspec;

#ifndef __ICompatibleFrameworkEntry_INTERFACE_DEFINED__
#define __ICompatibleFrameworkEntry_INTERFACE_DEFINED__

/* interface ICompatibleFrameworkEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_ICompatibleFrameworkEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("C98BFE2A-62C9-40AD-ADCE-A9037BE2BE6C")
    ICompatibleFrameworkEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ CompatibleFrameworkEntry **__MIDL_0229) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_index( 
            /* [retval][out] */ ULONG *__MIDL_0230) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TargetVersion( 
            /* [retval][out] */ LPCWSTR *__MIDL_0231) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Profile( 
            /* [retval][out] */ LPCWSTR *__MIDL_0232) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SupportedRuntime( 
            /* [retval][out] */ LPCWSTR *__MIDL_0233) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ICompatibleFrameworkEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICompatibleFrameworkEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICompatibleFrameworkEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICompatibleFrameworkEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            ICompatibleFrameworkEntry * This,
            /* [retval][out] */ CompatibleFrameworkEntry **__MIDL_0229);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_index )( 
            ICompatibleFrameworkEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0230);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TargetVersion )( 
            ICompatibleFrameworkEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0231);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Profile )( 
            ICompatibleFrameworkEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0232);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SupportedRuntime )( 
            ICompatibleFrameworkEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0233);
        
        END_INTERFACE
    } ICompatibleFrameworkEntryVtbl;

    interface ICompatibleFrameworkEntry
    {
        CONST_VTBL struct ICompatibleFrameworkEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICompatibleFrameworkEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ICompatibleFrameworkEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define ICompatibleFrameworkEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define ICompatibleFrameworkEntry_get_AllData(This,__MIDL_0229)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0229)

#define ICompatibleFrameworkEntry_get_index(This,__MIDL_0230)	\
    (This)->lpVtbl -> get_index(This,__MIDL_0230)

#define ICompatibleFrameworkEntry_get_TargetVersion(This,__MIDL_0231)	\
    (This)->lpVtbl -> get_TargetVersion(This,__MIDL_0231)

#define ICompatibleFrameworkEntry_get_Profile(This,__MIDL_0232)	\
    (This)->lpVtbl -> get_Profile(This,__MIDL_0232)

#define ICompatibleFrameworkEntry_get_SupportedRuntime(This,__MIDL_0233)	\
    (This)->lpVtbl -> get_SupportedRuntime(This,__MIDL_0233)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ICompatibleFrameworkEntry_get_AllData_Proxy( 
    ICompatibleFrameworkEntry * This,
    /* [retval][out] */ CompatibleFrameworkEntry **__MIDL_0229);


void __RPC_STUB ICompatibleFrameworkEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICompatibleFrameworkEntry_get_index_Proxy( 
    ICompatibleFrameworkEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0230);


void __RPC_STUB ICompatibleFrameworkEntry_get_index_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICompatibleFrameworkEntry_get_TargetVersion_Proxy( 
    ICompatibleFrameworkEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0231);


void __RPC_STUB ICompatibleFrameworkEntry_get_TargetVersion_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICompatibleFrameworkEntry_get_Profile_Proxy( 
    ICompatibleFrameworkEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0232);


void __RPC_STUB ICompatibleFrameworkEntry_get_Profile_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ICompatibleFrameworkEntry_get_SupportedRuntime_Proxy( 
    ICompatibleFrameworkEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0233);


void __RPC_STUB ICompatibleFrameworkEntry_get_SupportedRuntime_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __ICompatibleFrameworkEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0315 */
/* [local] */ 

HRESULT FreeMuiResourceIdLookupMapEntry( 
    /* [in] */ MuiResourceIdLookupMapEntry *__MIDL_0234);

HRESULT FreeMuiResourceTypeIdStringEntry( 
    /* [in] */ MuiResourceTypeIdStringEntry *__MIDL_0235);

HRESULT FreeMuiResourceTypeIdIntEntry( 
    /* [in] */ MuiResourceTypeIdIntEntry *__MIDL_0236);

HRESULT FreeMuiResourceMapEntry( 
    /* [in] */ MuiResourceMapEntry *__MIDL_0237);

HRESULT FreeHashElementEntry( 
    /* [in] */ HashElementEntry *__MIDL_0238);

HRESULT FreeFileEntry( 
    /* [in] */ FileEntry *__MIDL_0239);

HRESULT FreeFileAssociationEntry( 
    /* [in] */ FileAssociationEntry *__MIDL_0240);

HRESULT FreeCategoryMembershipDataEntry( 
    /* [in] */ CategoryMembershipDataEntry *__MIDL_0241);

HRESULT FreeSubcategoryMembershipEntry( 
    /* [in] */ SubcategoryMembershipEntry *__MIDL_0242);

HRESULT FreeCategoryMembershipEntry( 
    /* [in] */ CategoryMembershipEntry *__MIDL_0243);

HRESULT FreeCOMServerEntry( 
    /* [in] */ COMServerEntry *__MIDL_0244);

HRESULT FreeProgIdRedirectionEntry( 
    /* [in] */ ProgIdRedirectionEntry *__MIDL_0245);

HRESULT FreeCLRSurrogateEntry( 
    /* [in] */ CLRSurrogateEntry *__MIDL_0246);

HRESULT FreeAssemblyReferenceDependentAssemblyEntry( 
    /* [in] */ AssemblyReferenceDependentAssemblyEntry *__MIDL_0247);

HRESULT FreeAssemblyReferenceEntry( 
    /* [in] */ AssemblyReferenceEntry *__MIDL_0248);

HRESULT FreeWindowClassEntry( 
    /* [in] */ WindowClassEntry *__MIDL_0249);

HRESULT FreeResourceTableMappingEntry( 
    /* [in] */ ResourceTableMappingEntry *__MIDL_0250);

HRESULT FreeEntryPointEntry( 
    /* [in] */ EntryPointEntry *__MIDL_0251);

HRESULT FreePermissionSetEntry( 
    /* [in] */ PermissionSetEntry *__MIDL_0252);

HRESULT FreeAssemblyRequestEntry( 
    /* [in] */ AssemblyRequestEntry *__MIDL_0253);

HRESULT FreeDescriptionMetadataEntry( 
    /* [in] */ DescriptionMetadataEntry *__MIDL_0254);

HRESULT FreeDeploymentMetadataEntry( 
    /* [in] */ DeploymentMetadataEntry *__MIDL_0255);

HRESULT FreeDependentOSMetadataEntry( 
    /* [in] */ DependentOSMetadataEntry *__MIDL_0256);

HRESULT FreeCompatibleFrameworksMetadataEntry( 
    /* [in] */ CompatibleFrameworksMetadataEntry *__MIDL_0257);

HRESULT FreeMetadataSectionEntry( 
    /* [in] */ MetadataSectionEntry *__MIDL_0258);

HRESULT FreeEventEntry( 
    /* [in] */ EventEntry *__MIDL_0259);

HRESULT FreeEventMapEntry( 
    /* [in] */ EventMapEntry *__MIDL_0260);

HRESULT FreeEventTagEntry( 
    /* [in] */ EventTagEntry *__MIDL_0261);

HRESULT FreeRegistryValueEntry( 
    /* [in] */ RegistryValueEntry *__MIDL_0262);

HRESULT FreeRegistryKeyEntry( 
    /* [in] */ RegistryKeyEntry *__MIDL_0263);

HRESULT FreeDirectoryEntry( 
    /* [in] */ DirectoryEntry *__MIDL_0264);

HRESULT FreeSecurityDescriptorReferenceEntry( 
    /* [in] */ SecurityDescriptorReferenceEntry *__MIDL_0265);

HRESULT FreeCounterSetEntry( 
    /* [in] */ CounterSetEntry *__MIDL_0266);

HRESULT FreeCounterEntry( 
    /* [in] */ CounterEntry *__MIDL_0267);

HRESULT FreeCompatibleFrameworkEntry( 
    /* [in] */ CompatibleFrameworkEntry *__MIDL_0268);

typedef 
enum _ACSSECTIONID
    {	ACSSECTIONID_COMPONENTS_SECTION	= 1,
	ACSSECTIONID_MEMBER_LOOKUP_SECTION	= 2,
	ACSSECTIONID_METADATA_SECTION	= 3,
	ACSSECTIONID_STORE_COHERENCY_SECTION	= 4
    } 	ACSSECTIONID;








extern RPC_IF_HANDLE __MIDL_itf_isolation_0315_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0315_v0_0_s_ifspec;

#ifndef __IACS_INTERFACE_DEFINED__
#define __IACS_INTERFACE_DEFINED__

/* interface IACS */
/* [unique][uuid][object] */ 


EXTERN_C const IID IID_IACS;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("323f59af-4ab7-45a7-9e95-630cdfacef9c")
    IACS : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Identity( 
            /* [retval][out] */ IDefinitionAppId **__MIDL_0269) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ComponentsSection( 
            /* [retval][out] */ ISection **__MIDL_0270) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MemberLookupSection( 
            /* [retval][out] */ ISection **__MIDL_0271) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MetadataSection( 
            /* [retval][out] */ ISection **__MIDL_0272) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StoreCoherencySection( 
            /* [retval][out] */ ISection **__MIDL_0273) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IACSVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IACS * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IACS * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IACS * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Identity )( 
            IACS * This,
            /* [retval][out] */ IDefinitionAppId **__MIDL_0269);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ComponentsSection )( 
            IACS * This,
            /* [retval][out] */ ISection **__MIDL_0270);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MemberLookupSection )( 
            IACS * This,
            /* [retval][out] */ ISection **__MIDL_0271);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MetadataSection )( 
            IACS * This,
            /* [retval][out] */ ISection **__MIDL_0272);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StoreCoherencySection )( 
            IACS * This,
            /* [retval][out] */ ISection **__MIDL_0273);
        
        END_INTERFACE
    } IACSVtbl;

    interface IACS
    {
        CONST_VTBL struct IACSVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IACS_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IACS_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IACS_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IACS_get_Identity(This,__MIDL_0269)	\
    (This)->lpVtbl -> get_Identity(This,__MIDL_0269)

#define IACS_get_ComponentsSection(This,__MIDL_0270)	\
    (This)->lpVtbl -> get_ComponentsSection(This,__MIDL_0270)

#define IACS_get_MemberLookupSection(This,__MIDL_0271)	\
    (This)->lpVtbl -> get_MemberLookupSection(This,__MIDL_0271)

#define IACS_get_MetadataSection(This,__MIDL_0272)	\
    (This)->lpVtbl -> get_MetadataSection(This,__MIDL_0272)

#define IACS_get_StoreCoherencySection(This,__MIDL_0273)	\
    (This)->lpVtbl -> get_StoreCoherencySection(This,__MIDL_0273)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IACS_get_Identity_Proxy( 
    IACS * This,
    /* [retval][out] */ IDefinitionAppId **__MIDL_0269);


void __RPC_STUB IACS_get_Identity_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IACS_get_ComponentsSection_Proxy( 
    IACS * This,
    /* [retval][out] */ ISection **__MIDL_0270);


void __RPC_STUB IACS_get_ComponentsSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IACS_get_MemberLookupSection_Proxy( 
    IACS * This,
    /* [retval][out] */ ISection **__MIDL_0271);


void __RPC_STUB IACS_get_MemberLookupSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IACS_get_MetadataSection_Proxy( 
    IACS * This,
    /* [retval][out] */ ISection **__MIDL_0272);


void __RPC_STUB IACS_get_MetadataSection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IACS_get_StoreCoherencySection_Proxy( 
    IACS * This,
    /* [retval][out] */ ISection **__MIDL_0273);


void __RPC_STUB IACS_get_StoreCoherencySection_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IACS_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0317 */
/* [local] */ 

typedef struct _AppIdMetadataEntry
    {
    ULONG AppIdLength;
    ULONG ComponentCount;
    LPCWSTR SourceURL;
    LPCWSTR LocalInstanceUniquifier;
    } 	AppIdMetadataEntry;

typedef 
enum _AppIdMetadataEntryFieldId
    {	AppIdMetadata_AppIdLength	= 0,
	AppIdMetadata_ComponentCount	= AppIdMetadata_AppIdLength + 1,
	AppIdMetadata_SourceURL	= AppIdMetadata_ComponentCount + 1,
	AppIdMetadata_LocalInstanceUniquifier	= AppIdMetadata_SourceURL + 1
    } 	AppIdMetadataEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0317_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0317_v0_0_s_ifspec;

#ifndef __IAppIdMetadataEntry_INTERFACE_DEFINED__
#define __IAppIdMetadataEntry_INTERFACE_DEFINED__

/* interface IAppIdMetadataEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IAppIdMetadataEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("c75f426f-cb59-4246-88ca-b4dcd969dd6e")
    IAppIdMetadataEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ AppIdMetadataEntry **__MIDL_0274) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AppIdLength( 
            /* [retval][out] */ ULONG *__MIDL_0275) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ComponentCount( 
            /* [retval][out] */ ULONG *__MIDL_0276) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SourceURL( 
            /* [retval][out] */ LPCWSTR *__MIDL_0277) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LocalInstanceUniquifier( 
            /* [retval][out] */ LPCWSTR *__MIDL_0278) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IAppIdMetadataEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAppIdMetadataEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAppIdMetadataEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAppIdMetadataEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IAppIdMetadataEntry * This,
            /* [retval][out] */ AppIdMetadataEntry **__MIDL_0274);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AppIdLength )( 
            IAppIdMetadataEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0275);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ComponentCount )( 
            IAppIdMetadataEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0276);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SourceURL )( 
            IAppIdMetadataEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0277);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_LocalInstanceUniquifier )( 
            IAppIdMetadataEntry * This,
            /* [retval][out] */ LPCWSTR *__MIDL_0278);
        
        END_INTERFACE
    } IAppIdMetadataEntryVtbl;

    interface IAppIdMetadataEntry
    {
        CONST_VTBL struct IAppIdMetadataEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAppIdMetadataEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IAppIdMetadataEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IAppIdMetadataEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IAppIdMetadataEntry_get_AllData(This,__MIDL_0274)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0274)

#define IAppIdMetadataEntry_get_AppIdLength(This,__MIDL_0275)	\
    (This)->lpVtbl -> get_AppIdLength(This,__MIDL_0275)

#define IAppIdMetadataEntry_get_ComponentCount(This,__MIDL_0276)	\
    (This)->lpVtbl -> get_ComponentCount(This,__MIDL_0276)

#define IAppIdMetadataEntry_get_SourceURL(This,__MIDL_0277)	\
    (This)->lpVtbl -> get_SourceURL(This,__MIDL_0277)

#define IAppIdMetadataEntry_get_LocalInstanceUniquifier(This,__MIDL_0278)	\
    (This)->lpVtbl -> get_LocalInstanceUniquifier(This,__MIDL_0278)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IAppIdMetadataEntry_get_AllData_Proxy( 
    IAppIdMetadataEntry * This,
    /* [retval][out] */ AppIdMetadataEntry **__MIDL_0274);


void __RPC_STUB IAppIdMetadataEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IAppIdMetadataEntry_get_AppIdLength_Proxy( 
    IAppIdMetadataEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0275);


void __RPC_STUB IAppIdMetadataEntry_get_AppIdLength_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IAppIdMetadataEntry_get_ComponentCount_Proxy( 
    IAppIdMetadataEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0276);


void __RPC_STUB IAppIdMetadataEntry_get_ComponentCount_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IAppIdMetadataEntry_get_SourceURL_Proxy( 
    IAppIdMetadataEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0277);


void __RPC_STUB IAppIdMetadataEntry_get_SourceURL_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IAppIdMetadataEntry_get_LocalInstanceUniquifier_Proxy( 
    IAppIdMetadataEntry * This,
    /* [retval][out] */ LPCWSTR *__MIDL_0278);


void __RPC_STUB IAppIdMetadataEntry_get_LocalInstanceUniquifier_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IAppIdMetadataEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0318 */
/* [local] */ 

typedef struct _MemberComponentEntry
    {
    IDefinitionIdentity *Identity;
    GUID StoreId;
    } 	MemberComponentEntry;

typedef 
enum _MemberComponentEntryFieldId
    {	MemberComponent_Identity	= 0,
	MemberComponent_StoreId	= MemberComponent_Identity + 1
    } 	MemberComponentEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0318_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0318_v0_0_s_ifspec;

#ifndef __IMemberComponentEntry_INTERFACE_DEFINED__
#define __IMemberComponentEntry_INTERFACE_DEFINED__

/* interface IMemberComponentEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IMemberComponentEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("8f83f8cc-46a4-4347-8578-966a38e4221e")
    IMemberComponentEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ MemberComponentEntry **__MIDL_0279) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Identity( 
            /* [retval][out] */ IDefinitionIdentity **__MIDL_0280) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StoreId( 
            /* [retval][out] */ GUID *__MIDL_0281) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IMemberComponentEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IMemberComponentEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IMemberComponentEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IMemberComponentEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IMemberComponentEntry * This,
            /* [retval][out] */ MemberComponentEntry **__MIDL_0279);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Identity )( 
            IMemberComponentEntry * This,
            /* [retval][out] */ IDefinitionIdentity **__MIDL_0280);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StoreId )( 
            IMemberComponentEntry * This,
            /* [retval][out] */ GUID *__MIDL_0281);
        
        END_INTERFACE
    } IMemberComponentEntryVtbl;

    interface IMemberComponentEntry
    {
        CONST_VTBL struct IMemberComponentEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IMemberComponentEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IMemberComponentEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IMemberComponentEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IMemberComponentEntry_get_AllData(This,__MIDL_0279)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0279)

#define IMemberComponentEntry_get_Identity(This,__MIDL_0280)	\
    (This)->lpVtbl -> get_Identity(This,__MIDL_0280)

#define IMemberComponentEntry_get_StoreId(This,__MIDL_0281)	\
    (This)->lpVtbl -> get_StoreId(This,__MIDL_0281)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IMemberComponentEntry_get_AllData_Proxy( 
    IMemberComponentEntry * This,
    /* [retval][out] */ MemberComponentEntry **__MIDL_0279);


void __RPC_STUB IMemberComponentEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMemberComponentEntry_get_Identity_Proxy( 
    IMemberComponentEntry * This,
    /* [retval][out] */ IDefinitionIdentity **__MIDL_0280);


void __RPC_STUB IMemberComponentEntry_get_Identity_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMemberComponentEntry_get_StoreId_Proxy( 
    IMemberComponentEntry * This,
    /* [retval][out] */ GUID *__MIDL_0281);


void __RPC_STUB IMemberComponentEntry_get_StoreId_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IMemberComponentEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0319 */
/* [local] */ 

typedef struct _MemberLookupEntry
    {
    ULONG Index;
    } 	MemberLookupEntry;

typedef 
enum _MemberLookupEntryFieldId
    {	MemberLookup_Index	= 0
    } 	MemberLookupEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0319_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0319_v0_0_s_ifspec;

#ifndef __IMemberLookupEntry_INTERFACE_DEFINED__
#define __IMemberLookupEntry_INTERFACE_DEFINED__

/* interface IMemberLookupEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IMemberLookupEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("6C128A08-8598-41ce-8CD0-9D58DEFFA50B")
    IMemberLookupEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ MemberLookupEntry **__MIDL_0282) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Index( 
            /* [retval][out] */ ULONG *__MIDL_0283) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IMemberLookupEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IMemberLookupEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IMemberLookupEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IMemberLookupEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IMemberLookupEntry * This,
            /* [retval][out] */ MemberLookupEntry **__MIDL_0282);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Index )( 
            IMemberLookupEntry * This,
            /* [retval][out] */ ULONG *__MIDL_0283);
        
        END_INTERFACE
    } IMemberLookupEntryVtbl;

    interface IMemberLookupEntry
    {
        CONST_VTBL struct IMemberLookupEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IMemberLookupEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IMemberLookupEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IMemberLookupEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IMemberLookupEntry_get_AllData(This,__MIDL_0282)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0282)

#define IMemberLookupEntry_get_Index(This,__MIDL_0283)	\
    (This)->lpVtbl -> get_Index(This,__MIDL_0283)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IMemberLookupEntry_get_AllData_Proxy( 
    IMemberLookupEntry * This,
    /* [retval][out] */ MemberLookupEntry **__MIDL_0282);


void __RPC_STUB IMemberLookupEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IMemberLookupEntry_get_Index_Proxy( 
    IMemberLookupEntry * This,
    /* [retval][out] */ ULONG *__MIDL_0283);


void __RPC_STUB IMemberLookupEntry_get_Index_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IMemberLookupEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0320 */
/* [local] */ 

typedef struct _StoreCoherencyEntry
    {
    ULONGLONG CoherencyId;
    } 	StoreCoherencyEntry;

typedef 
enum _StoreCoherencyEntryFieldId
    {	StoreCoherency_CoherencyId	= 0
    } 	StoreCoherencyEntryFieldId;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0320_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0320_v0_0_s_ifspec;

#ifndef __IStoreCoherencyEntry_INTERFACE_DEFINED__
#define __IStoreCoherencyEntry_INTERFACE_DEFINED__

/* interface IStoreCoherencyEntry */
/* [uuid][unique][object][local] */ 


EXTERN_C const IID IID_IStoreCoherencyEntry;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("87E02E32-9979-4023-A135-FA033E84B037")
    IStoreCoherencyEntry : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AllData( 
            /* [retval][out] */ StoreCoherencyEntry **__MIDL_0284) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CoherencyId( 
            /* [retval][out] */ ULONGLONG *__MIDL_0285) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IStoreCoherencyEntryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IStoreCoherencyEntry * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IStoreCoherencyEntry * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IStoreCoherencyEntry * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AllData )( 
            IStoreCoherencyEntry * This,
            /* [retval][out] */ StoreCoherencyEntry **__MIDL_0284);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CoherencyId )( 
            IStoreCoherencyEntry * This,
            /* [retval][out] */ ULONGLONG *__MIDL_0285);
        
        END_INTERFACE
    } IStoreCoherencyEntryVtbl;

    interface IStoreCoherencyEntry
    {
        CONST_VTBL struct IStoreCoherencyEntryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IStoreCoherencyEntry_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IStoreCoherencyEntry_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IStoreCoherencyEntry_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IStoreCoherencyEntry_get_AllData(This,__MIDL_0284)	\
    (This)->lpVtbl -> get_AllData(This,__MIDL_0284)

#define IStoreCoherencyEntry_get_CoherencyId(This,__MIDL_0285)	\
    (This)->lpVtbl -> get_CoherencyId(This,__MIDL_0285)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IStoreCoherencyEntry_get_AllData_Proxy( 
    IStoreCoherencyEntry * This,
    /* [retval][out] */ StoreCoherencyEntry **__MIDL_0284);


void __RPC_STUB IStoreCoherencyEntry_get_AllData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IStoreCoherencyEntry_get_CoherencyId_Proxy( 
    IStoreCoherencyEntry * This,
    /* [retval][out] */ ULONGLONG *__MIDL_0285);


void __RPC_STUB IStoreCoherencyEntry_get_CoherencyId_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IStoreCoherencyEntry_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0321 */
/* [local] */ 

HRESULT FreeAppIdMetadataEntry( 
    /* [in] */ AppIdMetadataEntry *__MIDL_0286);

HRESULT FreeMemberComponentEntry( 
    /* [in] */ MemberComponentEntry *__MIDL_0287);

HRESULT FreeMemberLookupEntry( 
    /* [in] */ MemberLookupEntry *__MIDL_0288);

HRESULT FreeStoreCoherencyEntry( 
    /* [in] */ StoreCoherencyEntry *__MIDL_0289);

typedef struct _IDENTITY_ATTRIBUTE
    {
    LPCWSTR pszNamespace;
    LPCWSTR pszName;
    LPCWSTR pszValue;
    } 	IDENTITY_ATTRIBUTE;

typedef struct _IDENTITY_ATTRIBUTE *PIDENTITY_ATTRIBUTE;

typedef const IDENTITY_ATTRIBUTE *PCIDENTITY_ATTRIBUTE;

/* [v1_enum] */ 
enum _STORE_ASSEMBLY_STATUS_FLAGS
    {	STORE_ASSEMBLY_STATUS_MANIFEST_ONLY	= 0x1,
	STORE_ASSEMBLY_STATUS_PAYLOAD_RESIDENT	= 0x2,
	STORE_ASSEMBLY_STATUS_PARTIAL_INSTALL	= 0x4
    } ;
typedef struct _STORE_ASSEMBLY
    {
    DWORD dwStatus;
    IDefinitionIdentity *pIDefinitionIdentity;
    LPCWSTR pszManifestPath;
    ULONGLONG ullAssemblySize;
    ULONGLONG ullChangeId;
    } 	STORE_ASSEMBLY;

typedef struct _STORE_ASSEMBLY *PSTORE_ASSEMBLY;

typedef const STORE_ASSEMBLY *PCSTORE_ASSEMBLY;

/* [v1_enum] */ 
enum _STORE_ASSEMBLY_FILE_STATUS_FLAGS
    {	STORE_ASSEMBLY_FILE_STATUS_FLAG_PRESENT	= 0x1
    } ;
typedef struct _STORE_ASSEMBLY_FILE
    {
    DWORD cbSize;
    DWORD dwFlags;
    LPCWSTR pszFileName;
    DWORD dwFileStatusFlags;
    } 	STORE_ASSEMBLY_FILE;

typedef struct _STORE_ASSEMBLY_FILE *PSTORE_ASSEMBLY_FILE;

typedef const STORE_ASSEMBLY_FILE *PCSTORE_ASSEMBLY_FILE;

typedef struct _STORE_ASSEMBLY_INSTALLATION_REFERENCE
    {
    DWORD cbSize;
    DWORD dwFlags;
    GUID guidScheme;
    LPCWSTR pszIdentifier;
    LPCWSTR pszNonCanonicalData;
    } 	STORE_ASSEMBLY_INSTALLATION_REFERENCE;

typedef struct _STORE_ASSEMBLY_INSTALLATION_REFERENCE *PSTORE_ASSEMBLY_INSTALLATION_REFERENCE;

typedef const STORE_ASSEMBLY_INSTALLATION_REFERENCE *PCSTORE_ASSEMBLY_INSTALLATION_REFERENCE;

typedef struct _STORE_CATEGORY
    {
    IDefinitionIdentity *pIDefinitionIdentity;
    } 	STORE_CATEGORY;

typedef struct _STORE_CATEGORY *PSTORE_CATEGORY;

typedef const STORE_CATEGORY *PCSTORE_CATEGORY;

typedef struct _STORE_CATEGORY_SUBCATEGORY
    {
    LPCWSTR pszSubcategory;
    } 	STORE_CATEGORY_SUBCATEGORY;

typedef struct _STORE_CATEGORY_SUBCATEGORY *PSTORE_CATEGORY_SUBCATEGORY;

typedef const STORE_CATEGORY_SUBCATEGORY *PCSTORE_CATEGORY_SUBCATEGORY;

typedef struct _STORE_CATEGORY_INSTANCE
    {
    IDefinitionAppId *pIDefinitionAppId_Application;
    LPCWSTR pszXMLSnippet;
    } 	STORE_CATEGORY_INSTANCE;

typedef struct _STORE_CATEGORY_INSTANCE *PSTORE_CATEGORY_INSTANCE;

typedef const STORE_CATEGORY_INSTANCE *PCSTORE_CATEGORY_INSTANCE;

typedef struct _CATEGORY
    {
    IDefinitionIdentity *pIDefinitionIdentity;
    } 	CATEGORY;

typedef struct _CATEGORY *PCATEGORY;

typedef const CATEGORY *PCCATEGORY;

typedef struct _CATEGORY_SUBCATEGORY
    {
    LPCWSTR pszSubcategory;
    } 	CATEGORY_SUBCATEGORY;

typedef struct _CATEGORY_SUBCATEGORY *PCATEGORY_SUBCATEGORY;

typedef const CATEGORY_SUBCATEGORY *PCCATEGORY_SUBCATEGORY;

typedef struct _CATEGORY_INSTANCE
    {
    IDefinitionAppId *pIDefinitionAppId_Application;
    LPCWSTR pszXMLSnippet;
    } 	CATEGORY_INSTANCE;

typedef struct _CATEGORY_INSTANCE *PCATEGORY_INSTANCE;

typedef const CATEGORY_INSTANCE *PCCATEGORY_INSTANCE;

typedef struct _CREATE_APP_CONTEXT_DATA_PROCESSOR_ARCHITECTURE_FALLBACK_LIST
    {
    DWORD dwSize;
    DWORD dwFlags;
    ULONG nProcessorArchitectures;
    /* [size_is] */ const USHORT *prgusProcessorArchitectures;
    } 	CREATE_APP_CONTEXT_DATA_PROCESSOR_ARCHITECTURE_FALLBACK_LIST;

typedef struct _CREATE_APP_CONTEXT_DATA_PROCESSOR_ARCHITECTURE_FALLBACK_LIST *PCREATE_APP_CONTEXT_DATA_PROCESSOR_ARCHITECTURE_FALLBACK_LIST;

typedef const CREATE_APP_CONTEXT_DATA_PROCESSOR_ARCHITECTURE_FALLBACK_LIST *PCCREATE_APP_CONTEXT_DATA_PROCESSOR_ARCHITECTURE_FALLBACK_LIST;

typedef /* [v1_enum] */ 
enum _CREATE_APP_CONTEXT_DATA_CUSTOM_STORE_TYPE
    {	CREATE_APP_CONTEXT_DATA_CUSTOM_STORE_TYPE_INVALID	= 0,
	CREATE_APP_CONTEXT_DATA_CUSTOM_STORE_TYPE_SYSTEM_STORE	= 1,
	CREATE_APP_CONTEXT_DATA_CUSTOM_STORE_TYPE_USER_STORE	= 2,
	CREATE_APP_CONTEXT_DATA_CUSTOM_STORE_TYPE_PRIVATE_STORE	= 3
    } 	CREATE_APP_CONTEXT_DATA_CUSTOM_STORE_TYPE;

typedef /* [v1_enum] */ enum _CREATE_APP_CONTEXT_DATA_CUSTOM_STORE_TYPE *PCREATE_APP_CONTEXT_DATA_CUSTOM_STORE_TYPE;

typedef const CREATE_APP_CONTEXT_DATA_CUSTOM_STORE_TYPE *PCCREATE_APP_CONTEXT_DATA_CUSTOM_STORE_TYPE;

typedef struct _CREATE_APP_CONTEXT_DATA_CUSTOM_STORE
    {
    DWORD dwSize;
    DWORD dwFlags;
    CREATE_APP_CONTEXT_DATA_CUSTOM_STORE_TYPE iType;
    IStore *pIStore;
    PVOID pvReservedMustBeZero;
    } 	CREATE_APP_CONTEXT_DATA_CUSTOM_STORE;

typedef struct _CREATE_APP_CONTEXT_DATA_CUSTOM_STORE *PCREATE_APP_CONTEXT_DATA_CUSTOM_STORE;

typedef const CREATE_APP_CONTEXT_DATA_CUSTOM_STORE *PCCREATE_APP_CONTEXT_DATA_CUSTOM_STORE;

typedef struct _CREATE_APP_CONTEXT_DATA_CUSTOM_STORE_LIST
    {
    DWORD dwSize;
    DWORD dwFlags;
    ULONG Count;
    /* [size_is] */ const PCCREATE_APP_CONTEXT_DATA_CUSTOM_STORE *prgpStores;
    } 	CREATE_APP_CONTEXT_DATA_CUSTOM_STORE_LIST;

typedef struct _CREATE_APP_CONTEXT_DATA_CUSTOM_STORE_LIST *PCREATE_APP_CONTEXT_DATA_CUSTOM_STORE_LIST;

typedef const CREATE_APP_CONTEXT_DATA_CUSTOM_STORE_LIST *PCCREATE_APP_CONTEXT_DATA_CUSTOM_STORE_LIST;

typedef /* [v1_enum] */ 
enum _CREATE_APP_CONTEXT_DATA_SOURCE_TYPES
    {	CREATE_APP_CONTEXT_DATA_SOURCE_TYPE_APP_DEFINITION	= 1,
	CREATE_APP_CONTEXT_DATA_SOURCE_TYPE_APP_REFERENCE	= 2
    } 	CREATE_APP_CONTEXT_DATA_SOURCE_TYPES;

typedef /* [v1_enum] */ enum _CREATE_APP_CONTEXT_DATA_SOURCE_TYPES *PCREATE_APP_CONTEXT_DATA_SOURCE_TYPES;

typedef const CREATE_APP_CONTEXT_DATA_SOURCE_TYPES *PCCREATE_APP_CONTEXT_DATA_SOURCE_TYPES;

typedef struct _CREATE_APP_CONTEXT_DATA_SOURCE_APP_DEFINITION
    {
    DWORD dwSize;
    DWORD dwFlags;
    IDefinitionAppId *pIDefinitionAppId;
    } 	CREATE_APP_CONTEXT_DATA_SOURCE_APP_DEFINITION;

typedef struct _CREATE_APP_CONTEXT_DATA_SOURCE_APP_DEFINITION *PCREATE_APP_CONTEXT_DATA_SOURCE_APP_DEFINITION;

typedef const CREATE_APP_CONTEXT_DATA_SOURCE_APP_DEFINITION *PCCREATE_APP_CONTEXT_DATA_SOURCE_APP_DEFINITION;

typedef struct _CREATE_APP_CONTEXT_DATA_SOURCE_APP_REFERENCE
    {
    DWORD dwSize;
    DWORD dwFlags;
    IReferenceAppId *pIReferenceAppId;
    } 	CREATE_APP_CONTEXT_DATA_SOURCE_APP_REFERENCE;

typedef struct _CREATE_APP_CONTEXT_DATA_SOURCE_APP_REFERENCE *PCREATE_APP_CONTEXT_DATA_SOURCE_APP_REFERENCE;

typedef const CREATE_APP_CONTEXT_DATA_SOURCE_APP_REFERENCE *PCCREATE_APP_CONTEXT_DATA_SOURCE_APP_REFERENCE;

typedef /* [switch_type] */ union _CREATE_APP_CONTEXT_DATA_SOURCE_UNION
    {
    /* [case()] */ PCCREATE_APP_CONTEXT_DATA_SOURCE_APP_DEFINITION AppDefinition;
    /* [case()] */ PCCREATE_APP_CONTEXT_DATA_SOURCE_APP_REFERENCE AppReference;
    } 	CREATE_APP_CONTEXT_DATA_SOURCE_UNION;

typedef /* [switch_type] */ union _CREATE_APP_CONTEXT_DATA_SOURCE_UNION *PCREATE_APP_CONTEXT_DATA_SOURCE_UNION;

typedef const CREATE_APP_CONTEXT_DATA_SOURCE_UNION *PCCREATE_APP_CONTEXT_DATA_SOURCE_UNION;

typedef struct _CREATE_APP_CONTEXT_DATA_SOURCE
    {
    DWORD dwSize;
    DWORD dwFlags;
    CREATE_APP_CONTEXT_DATA_SOURCE_TYPES iSourceType;
    /* [switch_is] */ CREATE_APP_CONTEXT_DATA_SOURCE_UNION Data;
    } 	CREATE_APP_CONTEXT_DATA_SOURCE;

typedef struct _CREATE_APP_CONTEXT_DATA_SOURCE *PCREATE_APP_CONTEXT_DATA_SOURCE;

typedef const CREATE_APP_CONTEXT_DATA_SOURCE *PCCREATE_APP_CONTEXT_DATA_SOURCE;

/* [v1_enum] */ 
enum _CREATE_APP_CONTEXT_DATA_FLAGS
    {	CREATE_APP_CONTEXT_DATA_FLAG_CUSTOM_STORE_LIST_VALID	= 0x1,
	CREATE_APP_CONTEXT_DATA_FLAG_CULTURE_FALLBACK_LIST_VALID	= 0x2,
	CREATE_APP_CONTEXT_DATA_FLAG_PROCESSOR_ARCHITECTURE_FALLBACK_LIST_VALID	= 0x4,
	CREATE_APP_CONTEXT_DATA_FLAG_PROCESSOR_ARCHITECTURE_VALID	= 0x8,
	CREATE_APP_CONTEXT_DATA_FLAG_SOURCE_VALID	= 0x10,
	CREATE_APP_CONTEXT_DATA_FLAG_IGNORE_VISIBILITY_FLAGS	= 0x100000
    } ;
typedef struct _CREATE_APP_CONTEXT_DATA
    {
    DWORD dwSize;
    DWORD dwFlags;
    PCCREATE_APP_CONTEXT_DATA_CUSTOM_STORE_LIST pCustomStoreList;
    PCCULTURE_FALLBACK_LIST pCultureFallbackList;
    PCCREATE_APP_CONTEXT_DATA_PROCESSOR_ARCHITECTURE_FALLBACK_LIST pProcessorArchitectureFallbackList;
    PCCREATE_APP_CONTEXT_DATA_SOURCE pSource;
    USHORT usProcessorArchitecture;
    } 	CREATE_APP_CONTEXT_DATA;

typedef struct _CREATE_APP_CONTEXT_DATA *PCREATE_APP_CONTEXT_DATA;

typedef const CREATE_APP_CONTEXT_DATA *PCCREATE_APP_CONTEXT_DATA;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0321_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0321_v0_0_s_ifspec;

#ifndef __IReferenceIdentity_INTERFACE_DEFINED__
#define __IReferenceIdentity_INTERFACE_DEFINED__

/* interface IReferenceIdentity */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IReferenceIdentity;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("6eaf5ace-7917-4f3c-b129-e046a9704766")
    IReferenceIdentity : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetAttribute( 
            /* [unique][in] */ LPCWSTR pszNamespace,
            /* [in] */ LPCWSTR pszName,
            /* [retval][out] */ LPWSTR *ppszValue) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetAttribute( 
            /* [unique][in] */ LPCWSTR pszNamespace,
            /* [in] */ LPCWSTR pszName,
            /* [unique][in] */ LPCWSTR pszValue) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumAttributes( 
            /* [retval][out] */ IEnumIDENTITY_ATTRIBUTE **ppIEnumIDENTITY_ATTRIBUTE) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [in] */ SIZE_T cDeltas,
            /* [size_is][in] */ const IDENTITY_ATTRIBUTE rgDeltas[  ],
            /* [retval][out] */ IReferenceIdentity **ppIReferenceIdentity) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IReferenceIdentityVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IReferenceIdentity * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IReferenceIdentity * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IReferenceIdentity * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetAttribute )( 
            IReferenceIdentity * This,
            /* [unique][in] */ LPCWSTR pszNamespace,
            /* [in] */ LPCWSTR pszName,
            /* [retval][out] */ LPWSTR *ppszValue);
        
        HRESULT ( STDMETHODCALLTYPE *SetAttribute )( 
            IReferenceIdentity * This,
            /* [unique][in] */ LPCWSTR pszNamespace,
            /* [in] */ LPCWSTR pszName,
            /* [unique][in] */ LPCWSTR pszValue);
        
        HRESULT ( STDMETHODCALLTYPE *EnumAttributes )( 
            IReferenceIdentity * This,
            /* [retval][out] */ IEnumIDENTITY_ATTRIBUTE **ppIEnumIDENTITY_ATTRIBUTE);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IReferenceIdentity * This,
            /* [in] */ SIZE_T cDeltas,
            /* [size_is][in] */ const IDENTITY_ATTRIBUTE rgDeltas[  ],
            /* [retval][out] */ IReferenceIdentity **ppIReferenceIdentity);
        
        END_INTERFACE
    } IReferenceIdentityVtbl;

    interface IReferenceIdentity
    {
        CONST_VTBL struct IReferenceIdentityVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IReferenceIdentity_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IReferenceIdentity_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IReferenceIdentity_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IReferenceIdentity_GetAttribute(This,pszNamespace,pszName,ppszValue)	\
    (This)->lpVtbl -> GetAttribute(This,pszNamespace,pszName,ppszValue)

#define IReferenceIdentity_SetAttribute(This,pszNamespace,pszName,pszValue)	\
    (This)->lpVtbl -> SetAttribute(This,pszNamespace,pszName,pszValue)

#define IReferenceIdentity_EnumAttributes(This,ppIEnumIDENTITY_ATTRIBUTE)	\
    (This)->lpVtbl -> EnumAttributes(This,ppIEnumIDENTITY_ATTRIBUTE)

#define IReferenceIdentity_Clone(This,cDeltas,rgDeltas,ppIReferenceIdentity)	\
    (This)->lpVtbl -> Clone(This,cDeltas,rgDeltas,ppIReferenceIdentity)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IReferenceIdentity_GetAttribute_Proxy( 
    IReferenceIdentity * This,
    /* [unique][in] */ LPCWSTR pszNamespace,
    /* [in] */ LPCWSTR pszName,
    /* [retval][out] */ LPWSTR *ppszValue);


void __RPC_STUB IReferenceIdentity_GetAttribute_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IReferenceIdentity_SetAttribute_Proxy( 
    IReferenceIdentity * This,
    /* [unique][in] */ LPCWSTR pszNamespace,
    /* [in] */ LPCWSTR pszName,
    /* [unique][in] */ LPCWSTR pszValue);


void __RPC_STUB IReferenceIdentity_SetAttribute_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IReferenceIdentity_EnumAttributes_Proxy( 
    IReferenceIdentity * This,
    /* [retval][out] */ IEnumIDENTITY_ATTRIBUTE **ppIEnumIDENTITY_ATTRIBUTE);


void __RPC_STUB IReferenceIdentity_EnumAttributes_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IReferenceIdentity_Clone_Proxy( 
    IReferenceIdentity * This,
    /* [in] */ SIZE_T cDeltas,
    /* [size_is][in] */ const IDENTITY_ATTRIBUTE rgDeltas[  ],
    /* [retval][out] */ IReferenceIdentity **ppIReferenceIdentity);


void __RPC_STUB IReferenceIdentity_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IReferenceIdentity_INTERFACE_DEFINED__ */


#ifndef __IDefinitionIdentity_INTERFACE_DEFINED__
#define __IDefinitionIdentity_INTERFACE_DEFINED__

/* interface IDefinitionIdentity */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IDefinitionIdentity;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("587bf538-4d90-4a3c-9ef1-58a200a8a9e7")
    IDefinitionIdentity : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetAttribute( 
            /* [unique][in] */ LPCWSTR pszNamespace,
            /* [in] */ LPCWSTR pszName,
            /* [retval][out] */ LPWSTR *ppszValue) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetAttribute( 
            /* [unique][in] */ LPCWSTR pszNamespace,
            /* [in] */ LPCWSTR pszName,
            /* [unique][in] */ LPCWSTR pszValue) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumAttributes( 
            /* [retval][out] */ IEnumIDENTITY_ATTRIBUTE **ppIEAIA) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [in] */ SIZE_T cDeltas,
            /* [size_is][in] */ const IDENTITY_ATTRIBUTE prgDeltas[  ],
            /* [retval][out] */ IDefinitionIdentity **ppIDefinitionIdentity) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IDefinitionIdentityVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDefinitionIdentity * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDefinitionIdentity * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDefinitionIdentity * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetAttribute )( 
            IDefinitionIdentity * This,
            /* [unique][in] */ LPCWSTR pszNamespace,
            /* [in] */ LPCWSTR pszName,
            /* [retval][out] */ LPWSTR *ppszValue);
        
        HRESULT ( STDMETHODCALLTYPE *SetAttribute )( 
            IDefinitionIdentity * This,
            /* [unique][in] */ LPCWSTR pszNamespace,
            /* [in] */ LPCWSTR pszName,
            /* [unique][in] */ LPCWSTR pszValue);
        
        HRESULT ( STDMETHODCALLTYPE *EnumAttributes )( 
            IDefinitionIdentity * This,
            /* [retval][out] */ IEnumIDENTITY_ATTRIBUTE **ppIEAIA);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IDefinitionIdentity * This,
            /* [in] */ SIZE_T cDeltas,
            /* [size_is][in] */ const IDENTITY_ATTRIBUTE prgDeltas[  ],
            /* [retval][out] */ IDefinitionIdentity **ppIDefinitionIdentity);
        
        END_INTERFACE
    } IDefinitionIdentityVtbl;

    interface IDefinitionIdentity
    {
        CONST_VTBL struct IDefinitionIdentityVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDefinitionIdentity_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IDefinitionIdentity_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IDefinitionIdentity_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IDefinitionIdentity_GetAttribute(This,pszNamespace,pszName,ppszValue)	\
    (This)->lpVtbl -> GetAttribute(This,pszNamespace,pszName,ppszValue)

#define IDefinitionIdentity_SetAttribute(This,pszNamespace,pszName,pszValue)	\
    (This)->lpVtbl -> SetAttribute(This,pszNamespace,pszName,pszValue)

#define IDefinitionIdentity_EnumAttributes(This,ppIEAIA)	\
    (This)->lpVtbl -> EnumAttributes(This,ppIEAIA)

#define IDefinitionIdentity_Clone(This,cDeltas,prgDeltas,ppIDefinitionIdentity)	\
    (This)->lpVtbl -> Clone(This,cDeltas,prgDeltas,ppIDefinitionIdentity)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IDefinitionIdentity_GetAttribute_Proxy( 
    IDefinitionIdentity * This,
    /* [unique][in] */ LPCWSTR pszNamespace,
    /* [in] */ LPCWSTR pszName,
    /* [retval][out] */ LPWSTR *ppszValue);


void __RPC_STUB IDefinitionIdentity_GetAttribute_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDefinitionIdentity_SetAttribute_Proxy( 
    IDefinitionIdentity * This,
    /* [unique][in] */ LPCWSTR pszNamespace,
    /* [in] */ LPCWSTR pszName,
    /* [unique][in] */ LPCWSTR pszValue);


void __RPC_STUB IDefinitionIdentity_SetAttribute_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDefinitionIdentity_EnumAttributes_Proxy( 
    IDefinitionIdentity * This,
    /* [retval][out] */ IEnumIDENTITY_ATTRIBUTE **ppIEAIA);


void __RPC_STUB IDefinitionIdentity_EnumAttributes_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDefinitionIdentity_Clone_Proxy( 
    IDefinitionIdentity * This,
    /* [in] */ SIZE_T cDeltas,
    /* [size_is][in] */ const IDENTITY_ATTRIBUTE prgDeltas[  ],
    /* [retval][out] */ IDefinitionIdentity **ppIDefinitionIdentity);


void __RPC_STUB IDefinitionIdentity_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IDefinitionIdentity_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0323 */
/* [local] */ 

typedef struct _IDENTITY_ATTRIBUTE_BLOB
    {
    DWORD ofsNamespace;
    DWORD ofsName;
    DWORD ofsValue;
    } 	IDENTITY_ATTRIBUTE_BLOB;

typedef struct _IDENTITY_ATTRIBUTE_BLOB *PIDENTITY_ATTRIBUTE_BLOB;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0323_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0323_v0_0_s_ifspec;

#ifndef __IEnumIDENTITY_ATTRIBUTE_INTERFACE_DEFINED__
#define __IEnumIDENTITY_ATTRIBUTE_INTERFACE_DEFINED__

/* interface IEnumIDENTITY_ATTRIBUTE */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IEnumIDENTITY_ATTRIBUTE;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("9cdaae75-246e-4b00-a26d-b9aec137a3eb")
    IEnumIDENTITY_ATTRIBUTE : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ IDENTITY_ATTRIBUTE rgAttributes[  ],
            /* [optional][out] */ ULONG *pceltWritten) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CurrentIntoBuffer( 
            /* [in] */ SIZE_T cbAvailable,
            /* [length_is][size_is][out][in] */ BYTE pbData[  ],
            /* [out] */ SIZE_T *pcbUsed) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG celt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ IEnumIDENTITY_ATTRIBUTE **ppIEnumIDENTITY_ATTRIBUTE) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEnumIDENTITY_ATTRIBUTEVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumIDENTITY_ATTRIBUTE * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumIDENTITY_ATTRIBUTE * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumIDENTITY_ATTRIBUTE * This);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumIDENTITY_ATTRIBUTE * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ IDENTITY_ATTRIBUTE rgAttributes[  ],
            /* [optional][out] */ ULONG *pceltWritten);
        
        HRESULT ( STDMETHODCALLTYPE *CurrentIntoBuffer )( 
            IEnumIDENTITY_ATTRIBUTE * This,
            /* [in] */ SIZE_T cbAvailable,
            /* [length_is][size_is][out][in] */ BYTE pbData[  ],
            /* [out] */ SIZE_T *pcbUsed);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumIDENTITY_ATTRIBUTE * This,
            /* [in] */ ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumIDENTITY_ATTRIBUTE * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumIDENTITY_ATTRIBUTE * This,
            /* [out] */ IEnumIDENTITY_ATTRIBUTE **ppIEnumIDENTITY_ATTRIBUTE);
        
        END_INTERFACE
    } IEnumIDENTITY_ATTRIBUTEVtbl;

    interface IEnumIDENTITY_ATTRIBUTE
    {
        CONST_VTBL struct IEnumIDENTITY_ATTRIBUTEVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumIDENTITY_ATTRIBUTE_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEnumIDENTITY_ATTRIBUTE_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEnumIDENTITY_ATTRIBUTE_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEnumIDENTITY_ATTRIBUTE_Next(This,celt,rgAttributes,pceltWritten)	\
    (This)->lpVtbl -> Next(This,celt,rgAttributes,pceltWritten)

#define IEnumIDENTITY_ATTRIBUTE_CurrentIntoBuffer(This,cbAvailable,pbData,pcbUsed)	\
    (This)->lpVtbl -> CurrentIntoBuffer(This,cbAvailable,pbData,pcbUsed)

#define IEnumIDENTITY_ATTRIBUTE_Skip(This,celt)	\
    (This)->lpVtbl -> Skip(This,celt)

#define IEnumIDENTITY_ATTRIBUTE_Reset(This)	\
    (This)->lpVtbl -> Reset(This)

#define IEnumIDENTITY_ATTRIBUTE_Clone(This,ppIEnumIDENTITY_ATTRIBUTE)	\
    (This)->lpVtbl -> Clone(This,ppIEnumIDENTITY_ATTRIBUTE)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IEnumIDENTITY_ATTRIBUTE_Next_Proxy( 
    IEnumIDENTITY_ATTRIBUTE * This,
    /* [in] */ ULONG celt,
    /* [length_is][size_is][out] */ IDENTITY_ATTRIBUTE rgAttributes[  ],
    /* [optional][out] */ ULONG *pceltWritten);


void __RPC_STUB IEnumIDENTITY_ATTRIBUTE_Next_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumIDENTITY_ATTRIBUTE_CurrentIntoBuffer_Proxy( 
    IEnumIDENTITY_ATTRIBUTE * This,
    /* [in] */ SIZE_T cbAvailable,
    /* [length_is][size_is][out][in] */ BYTE pbData[  ],
    /* [out] */ SIZE_T *pcbUsed);


void __RPC_STUB IEnumIDENTITY_ATTRIBUTE_CurrentIntoBuffer_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumIDENTITY_ATTRIBUTE_Skip_Proxy( 
    IEnumIDENTITY_ATTRIBUTE * This,
    /* [in] */ ULONG celt);


void __RPC_STUB IEnumIDENTITY_ATTRIBUTE_Skip_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumIDENTITY_ATTRIBUTE_Reset_Proxy( 
    IEnumIDENTITY_ATTRIBUTE * This);


void __RPC_STUB IEnumIDENTITY_ATTRIBUTE_Reset_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumIDENTITY_ATTRIBUTE_Clone_Proxy( 
    IEnumIDENTITY_ATTRIBUTE * This,
    /* [out] */ IEnumIDENTITY_ATTRIBUTE **ppIEnumIDENTITY_ATTRIBUTE);


void __RPC_STUB IEnumIDENTITY_ATTRIBUTE_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumIDENTITY_ATTRIBUTE_INTERFACE_DEFINED__ */


#ifndef __IEnumDefinitionIdentity_INTERFACE_DEFINED__
#define __IEnumDefinitionIdentity_INTERFACE_DEFINED__

/* interface IEnumDefinitionIdentity */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IEnumDefinitionIdentity;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("f3549d9c-fc73-4793-9c00-1cd204254c0c")
    IEnumDefinitionIdentity : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ IDefinitionIdentity *rgpIDefinitionIdentity[  ],
            /* [out] */ ULONG *pceltWritten) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG celt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ IEnumDefinitionIdentity **ppIEnumDefinitionIdentity) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEnumDefinitionIdentityVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumDefinitionIdentity * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumDefinitionIdentity * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumDefinitionIdentity * This);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumDefinitionIdentity * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ IDefinitionIdentity *rgpIDefinitionIdentity[  ],
            /* [out] */ ULONG *pceltWritten);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumDefinitionIdentity * This,
            /* [in] */ ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumDefinitionIdentity * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumDefinitionIdentity * This,
            /* [out] */ IEnumDefinitionIdentity **ppIEnumDefinitionIdentity);
        
        END_INTERFACE
    } IEnumDefinitionIdentityVtbl;

    interface IEnumDefinitionIdentity
    {
        CONST_VTBL struct IEnumDefinitionIdentityVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumDefinitionIdentity_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEnumDefinitionIdentity_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEnumDefinitionIdentity_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEnumDefinitionIdentity_Next(This,celt,rgpIDefinitionIdentity,pceltWritten)	\
    (This)->lpVtbl -> Next(This,celt,rgpIDefinitionIdentity,pceltWritten)

#define IEnumDefinitionIdentity_Skip(This,celt)	\
    (This)->lpVtbl -> Skip(This,celt)

#define IEnumDefinitionIdentity_Reset(This)	\
    (This)->lpVtbl -> Reset(This)

#define IEnumDefinitionIdentity_Clone(This,ppIEnumDefinitionIdentity)	\
    (This)->lpVtbl -> Clone(This,ppIEnumDefinitionIdentity)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IEnumDefinitionIdentity_Next_Proxy( 
    IEnumDefinitionIdentity * This,
    /* [in] */ ULONG celt,
    /* [length_is][size_is][out] */ IDefinitionIdentity *rgpIDefinitionIdentity[  ],
    /* [out] */ ULONG *pceltWritten);


void __RPC_STUB IEnumDefinitionIdentity_Next_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumDefinitionIdentity_Skip_Proxy( 
    IEnumDefinitionIdentity * This,
    /* [in] */ ULONG celt);


void __RPC_STUB IEnumDefinitionIdentity_Skip_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumDefinitionIdentity_Reset_Proxy( 
    IEnumDefinitionIdentity * This);


void __RPC_STUB IEnumDefinitionIdentity_Reset_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumDefinitionIdentity_Clone_Proxy( 
    IEnumDefinitionIdentity * This,
    /* [out] */ IEnumDefinitionIdentity **ppIEnumDefinitionIdentity);


void __RPC_STUB IEnumDefinitionIdentity_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumDefinitionIdentity_INTERFACE_DEFINED__ */


#ifndef __IEnumReferenceIdentity_INTERFACE_DEFINED__
#define __IEnumReferenceIdentity_INTERFACE_DEFINED__

/* interface IEnumReferenceIdentity */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IEnumReferenceIdentity;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("b30352cf-23da-4577-9b3f-b4e6573be53b")
    IEnumReferenceIdentity : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ IReferenceIdentity **prgpIReferenceIdentity,
            /* [out] */ ULONG *pceltWritten) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            ULONG celt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            IEnumReferenceIdentity **ppIEnumReferenceIdentity) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEnumReferenceIdentityVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumReferenceIdentity * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumReferenceIdentity * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumReferenceIdentity * This);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumReferenceIdentity * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ IReferenceIdentity **prgpIReferenceIdentity,
            /* [out] */ ULONG *pceltWritten);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumReferenceIdentity * This,
            ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumReferenceIdentity * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumReferenceIdentity * This,
            IEnumReferenceIdentity **ppIEnumReferenceIdentity);
        
        END_INTERFACE
    } IEnumReferenceIdentityVtbl;

    interface IEnumReferenceIdentity
    {
        CONST_VTBL struct IEnumReferenceIdentityVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumReferenceIdentity_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEnumReferenceIdentity_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEnumReferenceIdentity_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEnumReferenceIdentity_Next(This,celt,prgpIReferenceIdentity,pceltWritten)	\
    (This)->lpVtbl -> Next(This,celt,prgpIReferenceIdentity,pceltWritten)

#define IEnumReferenceIdentity_Skip(This,celt)	\
    (This)->lpVtbl -> Skip(This,celt)

#define IEnumReferenceIdentity_Reset(This)	\
    (This)->lpVtbl -> Reset(This)

#define IEnumReferenceIdentity_Clone(This,ppIEnumReferenceIdentity)	\
    (This)->lpVtbl -> Clone(This,ppIEnumReferenceIdentity)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IEnumReferenceIdentity_Next_Proxy( 
    IEnumReferenceIdentity * This,
    /* [in] */ ULONG celt,
    /* [length_is][size_is][out] */ IReferenceIdentity **prgpIReferenceIdentity,
    /* [out] */ ULONG *pceltWritten);


void __RPC_STUB IEnumReferenceIdentity_Next_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumReferenceIdentity_Skip_Proxy( 
    IEnumReferenceIdentity * This,
    ULONG celt);


void __RPC_STUB IEnumReferenceIdentity_Skip_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumReferenceIdentity_Reset_Proxy( 
    IEnumReferenceIdentity * This);


void __RPC_STUB IEnumReferenceIdentity_Reset_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumReferenceIdentity_Clone_Proxy( 
    IEnumReferenceIdentity * This,
    IEnumReferenceIdentity **ppIEnumReferenceIdentity);


void __RPC_STUB IEnumReferenceIdentity_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumReferenceIdentity_INTERFACE_DEFINED__ */


#ifndef __IDefinitionAppId_INTERFACE_DEFINED__
#define __IDefinitionAppId_INTERFACE_DEFINED__

/* interface IDefinitionAppId */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IDefinitionAppId;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("d91e12d8-98ed-47fa-9936-39421283d59b")
    IDefinitionAppId : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SubscriptionId( 
            /* [retval][out] */ LPWSTR *ppszSubscription) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_SubscriptionId( 
            /* [in] */ LPCWSTR pszSubscription) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Codebase( 
            /* [retval][out] */ LPWSTR *ppszCodebase) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Codebase( 
            /* [in] */ LPCWSTR pszCodebase) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumAppPath( 
            /* [out] */ IEnumDefinitionIdentity **ppIEnumDefinitionIdentity) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetAppPath( 
            /* [in] */ ULONG cIDefinitionIdentity,
            /* [size_is][in] */ IDefinitionIdentity *rgIDefinitionIdentity[  ]) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IDefinitionAppIdVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDefinitionAppId * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDefinitionAppId * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDefinitionAppId * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SubscriptionId )( 
            IDefinitionAppId * This,
            /* [retval][out] */ LPWSTR *ppszSubscription);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_SubscriptionId )( 
            IDefinitionAppId * This,
            /* [in] */ LPCWSTR pszSubscription);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Codebase )( 
            IDefinitionAppId * This,
            /* [retval][out] */ LPWSTR *ppszCodebase);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Codebase )( 
            IDefinitionAppId * This,
            /* [in] */ LPCWSTR pszCodebase);
        
        HRESULT ( STDMETHODCALLTYPE *EnumAppPath )( 
            IDefinitionAppId * This,
            /* [out] */ IEnumDefinitionIdentity **ppIEnumDefinitionIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *SetAppPath )( 
            IDefinitionAppId * This,
            /* [in] */ ULONG cIDefinitionIdentity,
            /* [size_is][in] */ IDefinitionIdentity *rgIDefinitionIdentity[  ]);
        
        END_INTERFACE
    } IDefinitionAppIdVtbl;

    interface IDefinitionAppId
    {
        CONST_VTBL struct IDefinitionAppIdVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDefinitionAppId_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IDefinitionAppId_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IDefinitionAppId_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IDefinitionAppId_get_SubscriptionId(This,ppszSubscription)	\
    (This)->lpVtbl -> get_SubscriptionId(This,ppszSubscription)

#define IDefinitionAppId_put_SubscriptionId(This,pszSubscription)	\
    (This)->lpVtbl -> put_SubscriptionId(This,pszSubscription)

#define IDefinitionAppId_get_Codebase(This,ppszCodebase)	\
    (This)->lpVtbl -> get_Codebase(This,ppszCodebase)

#define IDefinitionAppId_put_Codebase(This,pszCodebase)	\
    (This)->lpVtbl -> put_Codebase(This,pszCodebase)

#define IDefinitionAppId_EnumAppPath(This,ppIEnumDefinitionIdentity)	\
    (This)->lpVtbl -> EnumAppPath(This,ppIEnumDefinitionIdentity)

#define IDefinitionAppId_SetAppPath(This,cIDefinitionIdentity,rgIDefinitionIdentity)	\
    (This)->lpVtbl -> SetAppPath(This,cIDefinitionIdentity,rgIDefinitionIdentity)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IDefinitionAppId_get_SubscriptionId_Proxy( 
    IDefinitionAppId * This,
    /* [retval][out] */ LPWSTR *ppszSubscription);


void __RPC_STUB IDefinitionAppId_get_SubscriptionId_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IDefinitionAppId_put_SubscriptionId_Proxy( 
    IDefinitionAppId * This,
    /* [in] */ LPCWSTR pszSubscription);


void __RPC_STUB IDefinitionAppId_put_SubscriptionId_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDefinitionAppId_get_Codebase_Proxy( 
    IDefinitionAppId * This,
    /* [retval][out] */ LPWSTR *ppszCodebase);


void __RPC_STUB IDefinitionAppId_get_Codebase_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IDefinitionAppId_put_Codebase_Proxy( 
    IDefinitionAppId * This,
    /* [in] */ LPCWSTR pszCodebase);


void __RPC_STUB IDefinitionAppId_put_Codebase_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDefinitionAppId_EnumAppPath_Proxy( 
    IDefinitionAppId * This,
    /* [out] */ IEnumDefinitionIdentity **ppIEnumDefinitionIdentity);


void __RPC_STUB IDefinitionAppId_EnumAppPath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDefinitionAppId_SetAppPath_Proxy( 
    IDefinitionAppId * This,
    /* [in] */ ULONG cIDefinitionIdentity,
    /* [size_is][in] */ IDefinitionIdentity *rgIDefinitionIdentity[  ]);


void __RPC_STUB IDefinitionAppId_SetAppPath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IDefinitionAppId_INTERFACE_DEFINED__ */


#ifndef __IReferenceAppId_INTERFACE_DEFINED__
#define __IReferenceAppId_INTERFACE_DEFINED__

/* interface IReferenceAppId */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IReferenceAppId;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("054f0bef-9e45-4363-8f5a-2f8e142d9a3b")
    IReferenceAppId : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SubscriptionId( 
            /* [retval][out] */ LPWSTR *ppszSubscription) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_SubscriptionId( 
            /* [in] */ LPCWSTR pszSubscription) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Codebase( 
            /* [retval][out] */ LPWSTR *ppszCodebase) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Codebase( 
            /* [in] */ LPCWSTR pszCodebase) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumAppPath( 
            /* [out] */ IEnumReferenceIdentity **ppIReferenceAppId) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IReferenceAppIdVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IReferenceAppId * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IReferenceAppId * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IReferenceAppId * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SubscriptionId )( 
            IReferenceAppId * This,
            /* [retval][out] */ LPWSTR *ppszSubscription);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_SubscriptionId )( 
            IReferenceAppId * This,
            /* [in] */ LPCWSTR pszSubscription);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Codebase )( 
            IReferenceAppId * This,
            /* [retval][out] */ LPWSTR *ppszCodebase);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Codebase )( 
            IReferenceAppId * This,
            /* [in] */ LPCWSTR pszCodebase);
        
        HRESULT ( STDMETHODCALLTYPE *EnumAppPath )( 
            IReferenceAppId * This,
            /* [out] */ IEnumReferenceIdentity **ppIReferenceAppId);
        
        END_INTERFACE
    } IReferenceAppIdVtbl;

    interface IReferenceAppId
    {
        CONST_VTBL struct IReferenceAppIdVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IReferenceAppId_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IReferenceAppId_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IReferenceAppId_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IReferenceAppId_get_SubscriptionId(This,ppszSubscription)	\
    (This)->lpVtbl -> get_SubscriptionId(This,ppszSubscription)

#define IReferenceAppId_put_SubscriptionId(This,pszSubscription)	\
    (This)->lpVtbl -> put_SubscriptionId(This,pszSubscription)

#define IReferenceAppId_get_Codebase(This,ppszCodebase)	\
    (This)->lpVtbl -> get_Codebase(This,ppszCodebase)

#define IReferenceAppId_put_Codebase(This,pszCodebase)	\
    (This)->lpVtbl -> put_Codebase(This,pszCodebase)

#define IReferenceAppId_EnumAppPath(This,ppIReferenceAppId)	\
    (This)->lpVtbl -> EnumAppPath(This,ppIReferenceAppId)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IReferenceAppId_get_SubscriptionId_Proxy( 
    IReferenceAppId * This,
    /* [retval][out] */ LPWSTR *ppszSubscription);


void __RPC_STUB IReferenceAppId_get_SubscriptionId_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IReferenceAppId_put_SubscriptionId_Proxy( 
    IReferenceAppId * This,
    /* [in] */ LPCWSTR pszSubscription);


void __RPC_STUB IReferenceAppId_put_SubscriptionId_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IReferenceAppId_get_Codebase_Proxy( 
    IReferenceAppId * This,
    /* [retval][out] */ LPWSTR *ppszCodebase);


void __RPC_STUB IReferenceAppId_get_Codebase_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IReferenceAppId_put_Codebase_Proxy( 
    IReferenceAppId * This,
    /* [in] */ LPCWSTR pszCodebase);


void __RPC_STUB IReferenceAppId_put_Codebase_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IReferenceAppId_EnumAppPath_Proxy( 
    IReferenceAppId * This,
    /* [out] */ IEnumReferenceIdentity **ppIReferenceAppId);


void __RPC_STUB IReferenceAppId_EnumAppPath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IReferenceAppId_INTERFACE_DEFINED__ */


#ifndef __IIdentityAuthority_INTERFACE_DEFINED__
#define __IIdentityAuthority_INTERFACE_DEFINED__

/* interface IIdentityAuthority */
/* [local][unique][uuid][object] */ 

/* [v1_enum] */ 
enum _TEXT_TO_DEFINITION_IDENTITY_FLAGS
    {	TEXT_TO_DEFINITION_IDENTITY_FLAG_ALLOW_UNKNOWN_ATTRIBUTES_IN_NULL_NAMESPACE	= 0x1
    } ;
/* [v1_enum] */ 
enum _TEXT_TO_REFERENCE_IDENTITY_FLAGS
    {	TEXT_TO_REFERENCE_IDENTITY_FLAG_ALLOW_UNKNOWN_ATTRIBUTES_IN_NULL_NAMESPACE	= 0x1
    } ;
/* [v1_enum] */ 
enum _DEFINITION_IDENTITY_TO_TEXT_FLAGS
    {	DEFINITION_IDENTITY_TO_TEXT_FLAG_CANONICAL	= 0x1
    } ;
/* [v1_enum] */ 
enum _REFERENCE_IDENTITY_TO_TEXT_FLAGS
    {	REFERENCE_IDENTITY_TO_TEXT_FLAG_CANONICAL	= 0x1
    } ;
/* [v1_enum] */ 
enum _IIDENTITYAUTHORITY_DOES_DEFINITION_MATCH_REFERENCE_FLAGS
    {	IIDENTITYAUTHORITY_DOES_DEFINITION_MATCH_REFERENCE_FLAG_EXACT_MATCH_REQUIRED	= 0x1
    } ;
/* [v1_enum] */ 
enum _IIDENTITYAUTHORITY_DOES_TEXTUAL_DEFINITION_MATCH_TEXTUAL_REFERENCE_FLAGS
    {	IIDENTITYAUTHORITY_DOES_TEXTUAL_DEFINITION_MATCH_TEXTUAL_REFERENCE_FLAG_EXACT_MATCH_REQUIRED	= 0x1
    } ;

EXTERN_C const IID IID_IIdentityAuthority;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("261a6983-c35d-4d0d-aa5b-7867259e77bc")
    IIdentityAuthority : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE TextToDefinition( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentity,
            /* [out] */ IDefinitionIdentity **ppIDefinitionIdentity) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE TextToReference( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentity,
            /* [out] */ IReferenceIdentity **ppIReferenceIdentity) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DefinitionToText( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [out] */ LPWSTR *ppszFormattedIdentity) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DefinitionToTextBuffer( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [in] */ ULONG cchBufferSize,
            /* [length_is][size_is][out][in] */ WCHAR wchBuffer[  ],
            /* [out] */ ULONG *pcchBufferRequired) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReferenceToText( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [out] */ LPWSTR *ppszFormattedIdentity) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReferenceToTextBuffer( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [in] */ ULONG cchBufferSize,
            /* [length_is][size_is][out][in] */ WCHAR wchBuffer[  ],
            /* [out] */ ULONG *pcchBufferRequired) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AreDefinitionsEqual( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pDefinition1,
            /* [in] */ IDefinitionIdentity *pDefinition2,
            /* [out] */ BOOL *pfEqual) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AreReferencesEqual( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pReference1,
            /* [in] */ IReferenceIdentity *pReference2,
            /* [out] */ BOOL *pfEqual) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AreTextualDefinitionsEqual( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentityLeft,
            /* [in] */ LPCWSTR pszIdentityRight,
            /* [out] */ BOOL *pfEqual) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AreTextualReferencesEqual( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentityLeft,
            /* [in] */ LPCWSTR pszIdentityRight,
            /* [out] */ BOOL *pfEqual) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DoesDefinitionMatchReference( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [out] */ BOOL *pfMatches) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DoesTextualDefinitionMatchTextualReference( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszDefinition,
            /* [in] */ LPCWSTR pszReference,
            /* [out] */ BOOL *pfMatches) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE HashReference( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [out] */ ULONGLONG *pullPseudoKey) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE HashDefinition( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [out] */ ULONGLONG *pullPseudoKey) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GenerateDefinitionKey( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [out] */ LPWSTR *ppszKeyForm) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GenerateReferenceKey( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [out] */ LPWSTR *ppszKeyForm) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateDefinition( 
            /* [retval][out] */ IDefinitionIdentity **ppNewIdentity) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateReference( 
            /* [retval][out] */ IReferenceIdentity **ppNewIdentity) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IIdentityAuthorityVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IIdentityAuthority * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IIdentityAuthority * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IIdentityAuthority * This);
        
        HRESULT ( STDMETHODCALLTYPE *TextToDefinition )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentity,
            /* [out] */ IDefinitionIdentity **ppIDefinitionIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *TextToReference )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentity,
            /* [out] */ IReferenceIdentity **ppIReferenceIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *DefinitionToText )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [out] */ LPWSTR *ppszFormattedIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *DefinitionToTextBuffer )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [in] */ ULONG cchBufferSize,
            /* [length_is][size_is][out][in] */ WCHAR wchBuffer[  ],
            /* [out] */ ULONG *pcchBufferRequired);
        
        HRESULT ( STDMETHODCALLTYPE *ReferenceToText )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [out] */ LPWSTR *ppszFormattedIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *ReferenceToTextBuffer )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [in] */ ULONG cchBufferSize,
            /* [length_is][size_is][out][in] */ WCHAR wchBuffer[  ],
            /* [out] */ ULONG *pcchBufferRequired);
        
        HRESULT ( STDMETHODCALLTYPE *AreDefinitionsEqual )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pDefinition1,
            /* [in] */ IDefinitionIdentity *pDefinition2,
            /* [out] */ BOOL *pfEqual);
        
        HRESULT ( STDMETHODCALLTYPE *AreReferencesEqual )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pReference1,
            /* [in] */ IReferenceIdentity *pReference2,
            /* [out] */ BOOL *pfEqual);
        
        HRESULT ( STDMETHODCALLTYPE *AreTextualDefinitionsEqual )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentityLeft,
            /* [in] */ LPCWSTR pszIdentityRight,
            /* [out] */ BOOL *pfEqual);
        
        HRESULT ( STDMETHODCALLTYPE *AreTextualReferencesEqual )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentityLeft,
            /* [in] */ LPCWSTR pszIdentityRight,
            /* [out] */ BOOL *pfEqual);
        
        HRESULT ( STDMETHODCALLTYPE *DoesDefinitionMatchReference )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [out] */ BOOL *pfMatches);
        
        HRESULT ( STDMETHODCALLTYPE *DoesTextualDefinitionMatchTextualReference )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszDefinition,
            /* [in] */ LPCWSTR pszReference,
            /* [out] */ BOOL *pfMatches);
        
        HRESULT ( STDMETHODCALLTYPE *HashReference )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [out] */ ULONGLONG *pullPseudoKey);
        
        HRESULT ( STDMETHODCALLTYPE *HashDefinition )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [out] */ ULONGLONG *pullPseudoKey);
        
        HRESULT ( STDMETHODCALLTYPE *GenerateDefinitionKey )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [out] */ LPWSTR *ppszKeyForm);
        
        HRESULT ( STDMETHODCALLTYPE *GenerateReferenceKey )( 
            IIdentityAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [out] */ LPWSTR *ppszKeyForm);
        
        HRESULT ( STDMETHODCALLTYPE *CreateDefinition )( 
            IIdentityAuthority * This,
            /* [retval][out] */ IDefinitionIdentity **ppNewIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *CreateReference )( 
            IIdentityAuthority * This,
            /* [retval][out] */ IReferenceIdentity **ppNewIdentity);
        
        END_INTERFACE
    } IIdentityAuthorityVtbl;

    interface IIdentityAuthority
    {
        CONST_VTBL struct IIdentityAuthorityVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IIdentityAuthority_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IIdentityAuthority_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IIdentityAuthority_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IIdentityAuthority_TextToDefinition(This,dwFlags,pszIdentity,ppIDefinitionIdentity)	\
    (This)->lpVtbl -> TextToDefinition(This,dwFlags,pszIdentity,ppIDefinitionIdentity)

#define IIdentityAuthority_TextToReference(This,dwFlags,pszIdentity,ppIReferenceIdentity)	\
    (This)->lpVtbl -> TextToReference(This,dwFlags,pszIdentity,ppIReferenceIdentity)

#define IIdentityAuthority_DefinitionToText(This,dwFlags,pIDefinitionIdentity,ppszFormattedIdentity)	\
    (This)->lpVtbl -> DefinitionToText(This,dwFlags,pIDefinitionIdentity,ppszFormattedIdentity)

#define IIdentityAuthority_DefinitionToTextBuffer(This,dwFlags,pIDefinitionIdentity,cchBufferSize,wchBuffer,pcchBufferRequired)	\
    (This)->lpVtbl -> DefinitionToTextBuffer(This,dwFlags,pIDefinitionIdentity,cchBufferSize,wchBuffer,pcchBufferRequired)

#define IIdentityAuthority_ReferenceToText(This,dwFlags,pIReferenceIdentity,ppszFormattedIdentity)	\
    (This)->lpVtbl -> ReferenceToText(This,dwFlags,pIReferenceIdentity,ppszFormattedIdentity)

#define IIdentityAuthority_ReferenceToTextBuffer(This,dwFlags,pIReferenceIdentity,cchBufferSize,wchBuffer,pcchBufferRequired)	\
    (This)->lpVtbl -> ReferenceToTextBuffer(This,dwFlags,pIReferenceIdentity,cchBufferSize,wchBuffer,pcchBufferRequired)

#define IIdentityAuthority_AreDefinitionsEqual(This,dwFlags,pDefinition1,pDefinition2,pfEqual)	\
    (This)->lpVtbl -> AreDefinitionsEqual(This,dwFlags,pDefinition1,pDefinition2,pfEqual)

#define IIdentityAuthority_AreReferencesEqual(This,dwFlags,pReference1,pReference2,pfEqual)	\
    (This)->lpVtbl -> AreReferencesEqual(This,dwFlags,pReference1,pReference2,pfEqual)

#define IIdentityAuthority_AreTextualDefinitionsEqual(This,dwFlags,pszIdentityLeft,pszIdentityRight,pfEqual)	\
    (This)->lpVtbl -> AreTextualDefinitionsEqual(This,dwFlags,pszIdentityLeft,pszIdentityRight,pfEqual)

#define IIdentityAuthority_AreTextualReferencesEqual(This,dwFlags,pszIdentityLeft,pszIdentityRight,pfEqual)	\
    (This)->lpVtbl -> AreTextualReferencesEqual(This,dwFlags,pszIdentityLeft,pszIdentityRight,pfEqual)

#define IIdentityAuthority_DoesDefinitionMatchReference(This,dwFlags,pIDefinitionIdentity,pIReferenceIdentity,pfMatches)	\
    (This)->lpVtbl -> DoesDefinitionMatchReference(This,dwFlags,pIDefinitionIdentity,pIReferenceIdentity,pfMatches)

#define IIdentityAuthority_DoesTextualDefinitionMatchTextualReference(This,dwFlags,pszDefinition,pszReference,pfMatches)	\
    (This)->lpVtbl -> DoesTextualDefinitionMatchTextualReference(This,dwFlags,pszDefinition,pszReference,pfMatches)

#define IIdentityAuthority_HashReference(This,dwFlags,pIReferenceIdentity,pullPseudoKey)	\
    (This)->lpVtbl -> HashReference(This,dwFlags,pIReferenceIdentity,pullPseudoKey)

#define IIdentityAuthority_HashDefinition(This,dwFlags,pIDefinitionIdentity,pullPseudoKey)	\
    (This)->lpVtbl -> HashDefinition(This,dwFlags,pIDefinitionIdentity,pullPseudoKey)

#define IIdentityAuthority_GenerateDefinitionKey(This,dwFlags,pIDefinitionIdentity,ppszKeyForm)	\
    (This)->lpVtbl -> GenerateDefinitionKey(This,dwFlags,pIDefinitionIdentity,ppszKeyForm)

#define IIdentityAuthority_GenerateReferenceKey(This,dwFlags,pIReferenceIdentity,ppszKeyForm)	\
    (This)->lpVtbl -> GenerateReferenceKey(This,dwFlags,pIReferenceIdentity,ppszKeyForm)

#define IIdentityAuthority_CreateDefinition(This,ppNewIdentity)	\
    (This)->lpVtbl -> CreateDefinition(This,ppNewIdentity)

#define IIdentityAuthority_CreateReference(This,ppNewIdentity)	\
    (This)->lpVtbl -> CreateReference(This,ppNewIdentity)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IIdentityAuthority_TextToDefinition_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszIdentity,
    /* [out] */ IDefinitionIdentity **ppIDefinitionIdentity);


void __RPC_STUB IIdentityAuthority_TextToDefinition_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_TextToReference_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszIdentity,
    /* [out] */ IReferenceIdentity **ppIReferenceIdentity);


void __RPC_STUB IIdentityAuthority_TextToReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_DefinitionToText_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
    /* [out] */ LPWSTR *ppszFormattedIdentity);


void __RPC_STUB IIdentityAuthority_DefinitionToText_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_DefinitionToTextBuffer_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
    /* [in] */ ULONG cchBufferSize,
    /* [length_is][size_is][out][in] */ WCHAR wchBuffer[  ],
    /* [out] */ ULONG *pcchBufferRequired);


void __RPC_STUB IIdentityAuthority_DefinitionToTextBuffer_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_ReferenceToText_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceIdentity *pIReferenceIdentity,
    /* [out] */ LPWSTR *ppszFormattedIdentity);


void __RPC_STUB IIdentityAuthority_ReferenceToText_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_ReferenceToTextBuffer_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceIdentity *pIReferenceIdentity,
    /* [in] */ ULONG cchBufferSize,
    /* [length_is][size_is][out][in] */ WCHAR wchBuffer[  ],
    /* [out] */ ULONG *pcchBufferRequired);


void __RPC_STUB IIdentityAuthority_ReferenceToTextBuffer_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_AreDefinitionsEqual_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pDefinition1,
    /* [in] */ IDefinitionIdentity *pDefinition2,
    /* [out] */ BOOL *pfEqual);


void __RPC_STUB IIdentityAuthority_AreDefinitionsEqual_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_AreReferencesEqual_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceIdentity *pReference1,
    /* [in] */ IReferenceIdentity *pReference2,
    /* [out] */ BOOL *pfEqual);


void __RPC_STUB IIdentityAuthority_AreReferencesEqual_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_AreTextualDefinitionsEqual_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszIdentityLeft,
    /* [in] */ LPCWSTR pszIdentityRight,
    /* [out] */ BOOL *pfEqual);


void __RPC_STUB IIdentityAuthority_AreTextualDefinitionsEqual_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_AreTextualReferencesEqual_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszIdentityLeft,
    /* [in] */ LPCWSTR pszIdentityRight,
    /* [out] */ BOOL *pfEqual);


void __RPC_STUB IIdentityAuthority_AreTextualReferencesEqual_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_DoesDefinitionMatchReference_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
    /* [in] */ IReferenceIdentity *pIReferenceIdentity,
    /* [out] */ BOOL *pfMatches);


void __RPC_STUB IIdentityAuthority_DoesDefinitionMatchReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_DoesTextualDefinitionMatchTextualReference_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszDefinition,
    /* [in] */ LPCWSTR pszReference,
    /* [out] */ BOOL *pfMatches);


void __RPC_STUB IIdentityAuthority_DoesTextualDefinitionMatchTextualReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_HashReference_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceIdentity *pIReferenceIdentity,
    /* [out] */ ULONGLONG *pullPseudoKey);


void __RPC_STUB IIdentityAuthority_HashReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_HashDefinition_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
    /* [out] */ ULONGLONG *pullPseudoKey);


void __RPC_STUB IIdentityAuthority_HashDefinition_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_GenerateDefinitionKey_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
    /* [out] */ LPWSTR *ppszKeyForm);


void __RPC_STUB IIdentityAuthority_GenerateDefinitionKey_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_GenerateReferenceKey_Proxy( 
    IIdentityAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceIdentity *pIReferenceIdentity,
    /* [out] */ LPWSTR *ppszKeyForm);


void __RPC_STUB IIdentityAuthority_GenerateReferenceKey_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_CreateDefinition_Proxy( 
    IIdentityAuthority * This,
    /* [retval][out] */ IDefinitionIdentity **ppNewIdentity);


void __RPC_STUB IIdentityAuthority_CreateDefinition_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IIdentityAuthority_CreateReference_Proxy( 
    IIdentityAuthority * This,
    /* [retval][out] */ IReferenceIdentity **ppNewIdentity);


void __RPC_STUB IIdentityAuthority_CreateReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IIdentityAuthority_INTERFACE_DEFINED__ */


#ifndef __IAppIdAuthority_INTERFACE_DEFINED__
#define __IAppIdAuthority_INTERFACE_DEFINED__

/* interface IAppIdAuthority */
/* [local][unique][uuid][object] */ 

/* [v1_enum] */ 
enum IAPPIDAUTHORITY_ARE_DEFINITIONS_EQUAL_FLAGS
    {	IAPPIDAUTHORITY_ARE_DEFINITIONS_EQUAL_FLAG_IGNORE_VERSION	= 0x1
    } ;
/* [v1_enum] */ 
enum IAPPIDAUTHORITY_ARE_REFERENCES_EQUAL_FLAGS
    {	IAPPIDAUTHORITY_ARE_REFERENCES_EQUAL_FLAG_IGNORE_VERSION	= 0x1
    } ;

EXTERN_C const IID IID_IAppIdAuthority;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("8c87810c-2541-4f75-b2d0-9af515488e23")
    IAppIdAuthority : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE TextToDefinition( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentity,
            /* [out] */ IDefinitionAppId **ppIDefinitionAppId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE TextToReference( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentity,
            /* [out] */ IReferenceAppId **ppIReferenceAppId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DefinitionToText( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIDefinitionAppId,
            /* [out] */ LPWSTR *ppszFormattedIdentity) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReferenceToText( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceAppId *pIReferenceAppId,
            /* [out] */ LPWSTR *ppszFormattedIdentity) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AreDefinitionsEqual( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pDefinition1,
            /* [in] */ IDefinitionAppId *pDefinition2,
            /* [out] */ BOOL *pfAreEqual) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AreReferencesEqual( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceAppId *pReference1,
            /* [in] */ IReferenceAppId *pReference2,
            /* [out] */ BOOL *pfAreEqual) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AreTextualDefinitionsEqual( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszAppIdLeft,
            /* [in] */ LPCWSTR pszAppIdRight,
            /* [out] */ BOOL *pfAreEqual) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AreTextualReferencesEqual( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszAppIdLeft,
            /* [in] */ LPCWSTR pszAppIdRight,
            /* [out] */ BOOL *pfAreEqual) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DoesDefinitionMatchReference( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIDefinitionIdentity,
            /* [in] */ IReferenceAppId *pIReferenceIdentity,
            /* [out] */ BOOL *pfMatches) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DoesTextualDefinitionMatchTextualReference( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszDefinition,
            /* [in] */ LPCWSTR pszReference,
            /* [out] */ BOOL *pfMatches) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE HashReference( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceAppId *pIReferenceIdentity,
            /* [out] */ ULONGLONG *pullPseudoKey) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE HashDefinition( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIDefinitionIdentity,
            /* [out] */ ULONGLONG *pullPseudoKey) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GenerateDefinitionKey( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIDefinitionIdentity,
            /* [out] */ LPWSTR *ppszKeyForm) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GenerateReferenceKey( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceAppId *pIReferenceIdentity,
            /* [out] */ LPWSTR *ppszKeyForm) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateDefinition( 
            /* [retval][out] */ IDefinitionAppId **ppNewIdentity) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateReference( 
            /* [retval][out] */ IReferenceAppId **ppNewIdentity) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IAppIdAuthorityVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IAppIdAuthority * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IAppIdAuthority * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IAppIdAuthority * This);
        
        HRESULT ( STDMETHODCALLTYPE *TextToDefinition )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentity,
            /* [out] */ IDefinitionAppId **ppIDefinitionAppId);
        
        HRESULT ( STDMETHODCALLTYPE *TextToReference )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszIdentity,
            /* [out] */ IReferenceAppId **ppIReferenceAppId);
        
        HRESULT ( STDMETHODCALLTYPE *DefinitionToText )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIDefinitionAppId,
            /* [out] */ LPWSTR *ppszFormattedIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *ReferenceToText )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceAppId *pIReferenceAppId,
            /* [out] */ LPWSTR *ppszFormattedIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *AreDefinitionsEqual )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pDefinition1,
            /* [in] */ IDefinitionAppId *pDefinition2,
            /* [out] */ BOOL *pfAreEqual);
        
        HRESULT ( STDMETHODCALLTYPE *AreReferencesEqual )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceAppId *pReference1,
            /* [in] */ IReferenceAppId *pReference2,
            /* [out] */ BOOL *pfAreEqual);
        
        HRESULT ( STDMETHODCALLTYPE *AreTextualDefinitionsEqual )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszAppIdLeft,
            /* [in] */ LPCWSTR pszAppIdRight,
            /* [out] */ BOOL *pfAreEqual);
        
        HRESULT ( STDMETHODCALLTYPE *AreTextualReferencesEqual )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszAppIdLeft,
            /* [in] */ LPCWSTR pszAppIdRight,
            /* [out] */ BOOL *pfAreEqual);
        
        HRESULT ( STDMETHODCALLTYPE *DoesDefinitionMatchReference )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIDefinitionIdentity,
            /* [in] */ IReferenceAppId *pIReferenceIdentity,
            /* [out] */ BOOL *pfMatches);
        
        HRESULT ( STDMETHODCALLTYPE *DoesTextualDefinitionMatchTextualReference )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ LPCWSTR pszDefinition,
            /* [in] */ LPCWSTR pszReference,
            /* [out] */ BOOL *pfMatches);
        
        HRESULT ( STDMETHODCALLTYPE *HashReference )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceAppId *pIReferenceIdentity,
            /* [out] */ ULONGLONG *pullPseudoKey);
        
        HRESULT ( STDMETHODCALLTYPE *HashDefinition )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIDefinitionIdentity,
            /* [out] */ ULONGLONG *pullPseudoKey);
        
        HRESULT ( STDMETHODCALLTYPE *GenerateDefinitionKey )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIDefinitionIdentity,
            /* [out] */ LPWSTR *ppszKeyForm);
        
        HRESULT ( STDMETHODCALLTYPE *GenerateReferenceKey )( 
            IAppIdAuthority * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceAppId *pIReferenceIdentity,
            /* [out] */ LPWSTR *ppszKeyForm);
        
        HRESULT ( STDMETHODCALLTYPE *CreateDefinition )( 
            IAppIdAuthority * This,
            /* [retval][out] */ IDefinitionAppId **ppNewIdentity);
        
        HRESULT ( STDMETHODCALLTYPE *CreateReference )( 
            IAppIdAuthority * This,
            /* [retval][out] */ IReferenceAppId **ppNewIdentity);
        
        END_INTERFACE
    } IAppIdAuthorityVtbl;

    interface IAppIdAuthority
    {
        CONST_VTBL struct IAppIdAuthorityVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IAppIdAuthority_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IAppIdAuthority_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IAppIdAuthority_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IAppIdAuthority_TextToDefinition(This,dwFlags,pszIdentity,ppIDefinitionAppId)	\
    (This)->lpVtbl -> TextToDefinition(This,dwFlags,pszIdentity,ppIDefinitionAppId)

#define IAppIdAuthority_TextToReference(This,dwFlags,pszIdentity,ppIReferenceAppId)	\
    (This)->lpVtbl -> TextToReference(This,dwFlags,pszIdentity,ppIReferenceAppId)

#define IAppIdAuthority_DefinitionToText(This,dwFlags,pIDefinitionAppId,ppszFormattedIdentity)	\
    (This)->lpVtbl -> DefinitionToText(This,dwFlags,pIDefinitionAppId,ppszFormattedIdentity)

#define IAppIdAuthority_ReferenceToText(This,dwFlags,pIReferenceAppId,ppszFormattedIdentity)	\
    (This)->lpVtbl -> ReferenceToText(This,dwFlags,pIReferenceAppId,ppszFormattedIdentity)

#define IAppIdAuthority_AreDefinitionsEqual(This,dwFlags,pDefinition1,pDefinition2,pfAreEqual)	\
    (This)->lpVtbl -> AreDefinitionsEqual(This,dwFlags,pDefinition1,pDefinition2,pfAreEqual)

#define IAppIdAuthority_AreReferencesEqual(This,dwFlags,pReference1,pReference2,pfAreEqual)	\
    (This)->lpVtbl -> AreReferencesEqual(This,dwFlags,pReference1,pReference2,pfAreEqual)

#define IAppIdAuthority_AreTextualDefinitionsEqual(This,dwFlags,pszAppIdLeft,pszAppIdRight,pfAreEqual)	\
    (This)->lpVtbl -> AreTextualDefinitionsEqual(This,dwFlags,pszAppIdLeft,pszAppIdRight,pfAreEqual)

#define IAppIdAuthority_AreTextualReferencesEqual(This,dwFlags,pszAppIdLeft,pszAppIdRight,pfAreEqual)	\
    (This)->lpVtbl -> AreTextualReferencesEqual(This,dwFlags,pszAppIdLeft,pszAppIdRight,pfAreEqual)

#define IAppIdAuthority_DoesDefinitionMatchReference(This,dwFlags,pIDefinitionIdentity,pIReferenceIdentity,pfMatches)	\
    (This)->lpVtbl -> DoesDefinitionMatchReference(This,dwFlags,pIDefinitionIdentity,pIReferenceIdentity,pfMatches)

#define IAppIdAuthority_DoesTextualDefinitionMatchTextualReference(This,dwFlags,pszDefinition,pszReference,pfMatches)	\
    (This)->lpVtbl -> DoesTextualDefinitionMatchTextualReference(This,dwFlags,pszDefinition,pszReference,pfMatches)

#define IAppIdAuthority_HashReference(This,dwFlags,pIReferenceIdentity,pullPseudoKey)	\
    (This)->lpVtbl -> HashReference(This,dwFlags,pIReferenceIdentity,pullPseudoKey)

#define IAppIdAuthority_HashDefinition(This,dwFlags,pIDefinitionIdentity,pullPseudoKey)	\
    (This)->lpVtbl -> HashDefinition(This,dwFlags,pIDefinitionIdentity,pullPseudoKey)

#define IAppIdAuthority_GenerateDefinitionKey(This,dwFlags,pIDefinitionIdentity,ppszKeyForm)	\
    (This)->lpVtbl -> GenerateDefinitionKey(This,dwFlags,pIDefinitionIdentity,ppszKeyForm)

#define IAppIdAuthority_GenerateReferenceKey(This,dwFlags,pIReferenceIdentity,ppszKeyForm)	\
    (This)->lpVtbl -> GenerateReferenceKey(This,dwFlags,pIReferenceIdentity,ppszKeyForm)

#define IAppIdAuthority_CreateDefinition(This,ppNewIdentity)	\
    (This)->lpVtbl -> CreateDefinition(This,ppNewIdentity)

#define IAppIdAuthority_CreateReference(This,ppNewIdentity)	\
    (This)->lpVtbl -> CreateReference(This,ppNewIdentity)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IAppIdAuthority_TextToDefinition_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszIdentity,
    /* [out] */ IDefinitionAppId **ppIDefinitionAppId);


void __RPC_STUB IAppIdAuthority_TextToDefinition_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_TextToReference_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszIdentity,
    /* [out] */ IReferenceAppId **ppIReferenceAppId);


void __RPC_STUB IAppIdAuthority_TextToReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_DefinitionToText_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionAppId *pIDefinitionAppId,
    /* [out] */ LPWSTR *ppszFormattedIdentity);


void __RPC_STUB IAppIdAuthority_DefinitionToText_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_ReferenceToText_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceAppId *pIReferenceAppId,
    /* [out] */ LPWSTR *ppszFormattedIdentity);


void __RPC_STUB IAppIdAuthority_ReferenceToText_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_AreDefinitionsEqual_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionAppId *pDefinition1,
    /* [in] */ IDefinitionAppId *pDefinition2,
    /* [out] */ BOOL *pfAreEqual);


void __RPC_STUB IAppIdAuthority_AreDefinitionsEqual_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_AreReferencesEqual_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceAppId *pReference1,
    /* [in] */ IReferenceAppId *pReference2,
    /* [out] */ BOOL *pfAreEqual);


void __RPC_STUB IAppIdAuthority_AreReferencesEqual_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_AreTextualDefinitionsEqual_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszAppIdLeft,
    /* [in] */ LPCWSTR pszAppIdRight,
    /* [out] */ BOOL *pfAreEqual);


void __RPC_STUB IAppIdAuthority_AreTextualDefinitionsEqual_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_AreTextualReferencesEqual_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszAppIdLeft,
    /* [in] */ LPCWSTR pszAppIdRight,
    /* [out] */ BOOL *pfAreEqual);


void __RPC_STUB IAppIdAuthority_AreTextualReferencesEqual_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_DoesDefinitionMatchReference_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionAppId *pIDefinitionIdentity,
    /* [in] */ IReferenceAppId *pIReferenceIdentity,
    /* [out] */ BOOL *pfMatches);


void __RPC_STUB IAppIdAuthority_DoesDefinitionMatchReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_DoesTextualDefinitionMatchTextualReference_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ LPCWSTR pszDefinition,
    /* [in] */ LPCWSTR pszReference,
    /* [out] */ BOOL *pfMatches);


void __RPC_STUB IAppIdAuthority_DoesTextualDefinitionMatchTextualReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_HashReference_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceAppId *pIReferenceIdentity,
    /* [out] */ ULONGLONG *pullPseudoKey);


void __RPC_STUB IAppIdAuthority_HashReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_HashDefinition_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionAppId *pIDefinitionIdentity,
    /* [out] */ ULONGLONG *pullPseudoKey);


void __RPC_STUB IAppIdAuthority_HashDefinition_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_GenerateDefinitionKey_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionAppId *pIDefinitionIdentity,
    /* [out] */ LPWSTR *ppszKeyForm);


void __RPC_STUB IAppIdAuthority_GenerateDefinitionKey_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_GenerateReferenceKey_Proxy( 
    IAppIdAuthority * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceAppId *pIReferenceIdentity,
    /* [out] */ LPWSTR *ppszKeyForm);


void __RPC_STUB IAppIdAuthority_GenerateReferenceKey_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_CreateDefinition_Proxy( 
    IAppIdAuthority * This,
    /* [retval][out] */ IDefinitionAppId **ppNewIdentity);


void __RPC_STUB IAppIdAuthority_CreateDefinition_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IAppIdAuthority_CreateReference_Proxy( 
    IAppIdAuthority * This,
    /* [retval][out] */ IReferenceAppId **ppNewIdentity);


void __RPC_STUB IAppIdAuthority_CreateReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IAppIdAuthority_INTERFACE_DEFINED__ */


#ifndef __IEnumSTORE_CATEGORY_INTERFACE_DEFINED__
#define __IEnumSTORE_CATEGORY_INTERFACE_DEFINED__

/* interface IEnumSTORE_CATEGORY */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IEnumSTORE_CATEGORY;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("b840a2f5-a497-4a6d-9038-cd3ec2fbd222")
    IEnumSTORE_CATEGORY : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ STORE_CATEGORY rgElements[  ],
            /* [out] */ ULONG *pulFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG ulElements) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ IEnumSTORE_CATEGORY **ppIEnumSTORE_CATEGORY) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEnumSTORE_CATEGORYVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumSTORE_CATEGORY * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumSTORE_CATEGORY * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumSTORE_CATEGORY * This);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumSTORE_CATEGORY * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ STORE_CATEGORY rgElements[  ],
            /* [out] */ ULONG *pulFetched);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumSTORE_CATEGORY * This,
            /* [in] */ ULONG ulElements);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumSTORE_CATEGORY * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumSTORE_CATEGORY * This,
            /* [out] */ IEnumSTORE_CATEGORY **ppIEnumSTORE_CATEGORY);
        
        END_INTERFACE
    } IEnumSTORE_CATEGORYVtbl;

    interface IEnumSTORE_CATEGORY
    {
        CONST_VTBL struct IEnumSTORE_CATEGORYVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumSTORE_CATEGORY_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEnumSTORE_CATEGORY_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEnumSTORE_CATEGORY_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEnumSTORE_CATEGORY_Next(This,celt,rgElements,pulFetched)	\
    (This)->lpVtbl -> Next(This,celt,rgElements,pulFetched)

#define IEnumSTORE_CATEGORY_Skip(This,ulElements)	\
    (This)->lpVtbl -> Skip(This,ulElements)

#define IEnumSTORE_CATEGORY_Reset(This)	\
    (This)->lpVtbl -> Reset(This)

#define IEnumSTORE_CATEGORY_Clone(This,ppIEnumSTORE_CATEGORY)	\
    (This)->lpVtbl -> Clone(This,ppIEnumSTORE_CATEGORY)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IEnumSTORE_CATEGORY_Next_Proxy( 
    IEnumSTORE_CATEGORY * This,
    /* [in] */ ULONG celt,
    /* [length_is][size_is][out] */ STORE_CATEGORY rgElements[  ],
    /* [out] */ ULONG *pulFetched);


void __RPC_STUB IEnumSTORE_CATEGORY_Next_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_CATEGORY_Skip_Proxy( 
    IEnumSTORE_CATEGORY * This,
    /* [in] */ ULONG ulElements);


void __RPC_STUB IEnumSTORE_CATEGORY_Skip_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_CATEGORY_Reset_Proxy( 
    IEnumSTORE_CATEGORY * This);


void __RPC_STUB IEnumSTORE_CATEGORY_Reset_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_CATEGORY_Clone_Proxy( 
    IEnumSTORE_CATEGORY * This,
    /* [out] */ IEnumSTORE_CATEGORY **ppIEnumSTORE_CATEGORY);


void __RPC_STUB IEnumSTORE_CATEGORY_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumSTORE_CATEGORY_INTERFACE_DEFINED__ */


#ifndef __IEnumSTORE_CATEGORY_SUBCATEGORY_INTERFACE_DEFINED__
#define __IEnumSTORE_CATEGORY_SUBCATEGORY_INTERFACE_DEFINED__

/* interface IEnumSTORE_CATEGORY_SUBCATEGORY */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IEnumSTORE_CATEGORY_SUBCATEGORY;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("19be1967-b2fc-4dc1-9627-f3cb6305d2a7")
    IEnumSTORE_CATEGORY_SUBCATEGORY : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ STORE_CATEGORY_SUBCATEGORY rgElements[  ],
            /* [out] */ ULONG *pulFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG ulElements) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ IEnumSTORE_CATEGORY_SUBCATEGORY **ppIEnumSTORE_CATEGORY_SUBCATEGORY) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEnumSTORE_CATEGORY_SUBCATEGORYVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumSTORE_CATEGORY_SUBCATEGORY * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumSTORE_CATEGORY_SUBCATEGORY * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumSTORE_CATEGORY_SUBCATEGORY * This);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumSTORE_CATEGORY_SUBCATEGORY * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ STORE_CATEGORY_SUBCATEGORY rgElements[  ],
            /* [out] */ ULONG *pulFetched);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumSTORE_CATEGORY_SUBCATEGORY * This,
            /* [in] */ ULONG ulElements);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumSTORE_CATEGORY_SUBCATEGORY * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumSTORE_CATEGORY_SUBCATEGORY * This,
            /* [out] */ IEnumSTORE_CATEGORY_SUBCATEGORY **ppIEnumSTORE_CATEGORY_SUBCATEGORY);
        
        END_INTERFACE
    } IEnumSTORE_CATEGORY_SUBCATEGORYVtbl;

    interface IEnumSTORE_CATEGORY_SUBCATEGORY
    {
        CONST_VTBL struct IEnumSTORE_CATEGORY_SUBCATEGORYVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumSTORE_CATEGORY_SUBCATEGORY_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEnumSTORE_CATEGORY_SUBCATEGORY_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEnumSTORE_CATEGORY_SUBCATEGORY_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEnumSTORE_CATEGORY_SUBCATEGORY_Next(This,celt,rgElements,pulFetched)	\
    (This)->lpVtbl -> Next(This,celt,rgElements,pulFetched)

#define IEnumSTORE_CATEGORY_SUBCATEGORY_Skip(This,ulElements)	\
    (This)->lpVtbl -> Skip(This,ulElements)

#define IEnumSTORE_CATEGORY_SUBCATEGORY_Reset(This)	\
    (This)->lpVtbl -> Reset(This)

#define IEnumSTORE_CATEGORY_SUBCATEGORY_Clone(This,ppIEnumSTORE_CATEGORY_SUBCATEGORY)	\
    (This)->lpVtbl -> Clone(This,ppIEnumSTORE_CATEGORY_SUBCATEGORY)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IEnumSTORE_CATEGORY_SUBCATEGORY_Next_Proxy( 
    IEnumSTORE_CATEGORY_SUBCATEGORY * This,
    /* [in] */ ULONG celt,
    /* [length_is][size_is][out] */ STORE_CATEGORY_SUBCATEGORY rgElements[  ],
    /* [out] */ ULONG *pulFetched);


void __RPC_STUB IEnumSTORE_CATEGORY_SUBCATEGORY_Next_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_CATEGORY_SUBCATEGORY_Skip_Proxy( 
    IEnumSTORE_CATEGORY_SUBCATEGORY * This,
    /* [in] */ ULONG ulElements);


void __RPC_STUB IEnumSTORE_CATEGORY_SUBCATEGORY_Skip_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_CATEGORY_SUBCATEGORY_Reset_Proxy( 
    IEnumSTORE_CATEGORY_SUBCATEGORY * This);


void __RPC_STUB IEnumSTORE_CATEGORY_SUBCATEGORY_Reset_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_CATEGORY_SUBCATEGORY_Clone_Proxy( 
    IEnumSTORE_CATEGORY_SUBCATEGORY * This,
    /* [out] */ IEnumSTORE_CATEGORY_SUBCATEGORY **ppIEnumSTORE_CATEGORY_SUBCATEGORY);


void __RPC_STUB IEnumSTORE_CATEGORY_SUBCATEGORY_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumSTORE_CATEGORY_SUBCATEGORY_INTERFACE_DEFINED__ */


#ifndef __IEnumSTORE_CATEGORY_INSTANCE_INTERFACE_DEFINED__
#define __IEnumSTORE_CATEGORY_INSTANCE_INTERFACE_DEFINED__

/* interface IEnumSTORE_CATEGORY_INSTANCE */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IEnumSTORE_CATEGORY_INSTANCE;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("5ba7cb30-8508-4114-8c77-262fcda4fadb")
    IEnumSTORE_CATEGORY_INSTANCE : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG ulElements,
            /* [length_is][size_is][out] */ STORE_CATEGORY_INSTANCE rgInstances[  ],
            /* [out] */ ULONG *pulFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG ulElements) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ IEnumSTORE_CATEGORY_INSTANCE **ppIEnumSTORE_CATEGORY_INSTANCE) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEnumSTORE_CATEGORY_INSTANCEVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumSTORE_CATEGORY_INSTANCE * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumSTORE_CATEGORY_INSTANCE * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumSTORE_CATEGORY_INSTANCE * This);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumSTORE_CATEGORY_INSTANCE * This,
            /* [in] */ ULONG ulElements,
            /* [length_is][size_is][out] */ STORE_CATEGORY_INSTANCE rgInstances[  ],
            /* [out] */ ULONG *pulFetched);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumSTORE_CATEGORY_INSTANCE * This,
            /* [in] */ ULONG ulElements);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumSTORE_CATEGORY_INSTANCE * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumSTORE_CATEGORY_INSTANCE * This,
            /* [out] */ IEnumSTORE_CATEGORY_INSTANCE **ppIEnumSTORE_CATEGORY_INSTANCE);
        
        END_INTERFACE
    } IEnumSTORE_CATEGORY_INSTANCEVtbl;

    interface IEnumSTORE_CATEGORY_INSTANCE
    {
        CONST_VTBL struct IEnumSTORE_CATEGORY_INSTANCEVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumSTORE_CATEGORY_INSTANCE_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEnumSTORE_CATEGORY_INSTANCE_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEnumSTORE_CATEGORY_INSTANCE_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEnumSTORE_CATEGORY_INSTANCE_Next(This,ulElements,rgInstances,pulFetched)	\
    (This)->lpVtbl -> Next(This,ulElements,rgInstances,pulFetched)

#define IEnumSTORE_CATEGORY_INSTANCE_Skip(This,ulElements)	\
    (This)->lpVtbl -> Skip(This,ulElements)

#define IEnumSTORE_CATEGORY_INSTANCE_Reset(This)	\
    (This)->lpVtbl -> Reset(This)

#define IEnumSTORE_CATEGORY_INSTANCE_Clone(This,ppIEnumSTORE_CATEGORY_INSTANCE)	\
    (This)->lpVtbl -> Clone(This,ppIEnumSTORE_CATEGORY_INSTANCE)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IEnumSTORE_CATEGORY_INSTANCE_Next_Proxy( 
    IEnumSTORE_CATEGORY_INSTANCE * This,
    /* [in] */ ULONG ulElements,
    /* [length_is][size_is][out] */ STORE_CATEGORY_INSTANCE rgInstances[  ],
    /* [out] */ ULONG *pulFetched);


void __RPC_STUB IEnumSTORE_CATEGORY_INSTANCE_Next_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_CATEGORY_INSTANCE_Skip_Proxy( 
    IEnumSTORE_CATEGORY_INSTANCE * This,
    /* [in] */ ULONG ulElements);


void __RPC_STUB IEnumSTORE_CATEGORY_INSTANCE_Skip_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_CATEGORY_INSTANCE_Reset_Proxy( 
    IEnumSTORE_CATEGORY_INSTANCE * This);


void __RPC_STUB IEnumSTORE_CATEGORY_INSTANCE_Reset_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_CATEGORY_INSTANCE_Clone_Proxy( 
    IEnumSTORE_CATEGORY_INSTANCE * This,
    /* [out] */ IEnumSTORE_CATEGORY_INSTANCE **ppIEnumSTORE_CATEGORY_INSTANCE);


void __RPC_STUB IEnumSTORE_CATEGORY_INSTANCE_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumSTORE_CATEGORY_INSTANCE_INTERFACE_DEFINED__ */


#ifndef __IStore_INTERFACE_DEFINED__
#define __IStore_INTERFACE_DEFINED__

/* interface IStore */
/* [local][unique][uuid][object] */ 

typedef struct _STORE_SET_CANONICALIZATION_CONTEXT
    {
    DWORD cbSize;
    DWORD dwFlags;
    LPCWSTR pszBaseAddressesFilePath;
    LPCWSTR pszExportsFilePath;
    } 	STORE_SET_CANONICALIZATION_CONTEXT;

typedef struct _STORE_SET_CANONICALIZATION_CONTEXT *PSTORE_SET_CANONICALIZATION_CONTEXT;

typedef const STORE_SET_CANONICALIZATION_CONTEXT *PCSTORE_SET_CANONICALIZATION_CONTEXT;

typedef struct _STORE_STAGE_COMPONENT
    {
    DWORD cbSize;
    DWORD dwFlags;
    IDefinitionAppId *pIApplicationIdentity;
    IDefinitionIdentity *pIComponentIdentity;
    LPCWSTR pszManifestSourcePath;
    } 	STORE_STAGE_COMPONENT;

typedef struct _STORE_STAGE_COMPONENT *PSTORE_STAGE_COMPONENT;

typedef const STORE_STAGE_COMPONENT *PCSTORE_STAGE_COMPONENT;

/* [v1_enum] */ 
enum _STORE_STAGE_COMPONENT_DISPOSITIONS
    {	ISTORE_STAGE_COMPONENT_DISPOSITION_INSTALLED	= 0x1,
	ISTORE_STAGE_COMPONENT_DISPOSITION_REFRESHED_BITS	= 0x2,
	ISTORE_STAGE_COMPONENT_DISPOSITION_ALREADY_INSTALLED	= 0x3
    } ;
typedef struct _STORE_STAGE_COMPONENT_FILE
    {
    DWORD cbSize;
    DWORD dwFlags;
    IDefinitionAppId *pApplication;
    IDefinitionIdentity *pMemberComponent;
    LPCWSTR pszComponentRelativePath;
    LPCWSTR pszSourceFileName;
    } 	STORE_STAGE_COMPONENT_FILE;

typedef struct _STORE_STAGE_COMPONENT_FILE *PSTORE_STAGE_COMPONENT_FILE;

typedef const STORE_STAGE_COMPONENT_FILE *PCSTORE_STAGE_COMPONENT_FILE;

/* [v1_enum] */ 
enum _STORE_STAGE_COMPONENT_FILE_DISPOSITIONS
    {	ISTORE_STAGE_COMPONENT_FILE_DISPOSITION_INSTALLED	= 0x1,
	ISTORE_STAGE_COMPONENT_FILE_DISPOSITION_REFRESHED_BITS	= 0x2,
	ISTORE_STAGE_COMPONENT_FILE_DISPOSITION_ALREADY_INSTALLED	= 0x3
    } ;
/* [v1_enum] */ 
enum _STORE_PIN_DEPLOYMENT_FLAGS
    {	STORE_PIN_DEPLOYMENT_FLAG_NEVER_EXPIRES	= 0x1
    } ;
typedef struct _STORE_PIN_DEPLOYMENT
    {
    DWORD cbSize;
    DWORD dwFlags;
    IDefinitionAppId *pIApplicationIdentity;
    ULONGLONG ExpirationTime;
    PCSTORE_ASSEMBLY_INSTALLATION_REFERENCE pReferenceTrack;
    } 	STORE_PIN_DEPLOYMENT;

typedef struct _STORE_PIN_DEPLOYMENT *PSTORE_PIN_DEPLOYMENT;

typedef const STORE_PIN_DEPLOYMENT *PCSTORE_PIN_DEPLOYMENT;

/* [v1_enum] */ 
enum _STORE_PIN_DEPLOYMENT_DISPOSITIONS
    {	ISTORE_PIN_DEPLOYMENT_DISPOSITION_PINNED	= 0x1
    } ;
typedef struct _STORE_UNPIN_DEPLOYMENT
    {
    DWORD cbSize;
    DWORD dwFlags;
    IDefinitionAppId *pIApplicationIdentity;
    PCSTORE_ASSEMBLY_INSTALLATION_REFERENCE pReferenceTrack;
    } 	STORE_UNPIN_DEPLOYMENT;

typedef struct _STORE_UNPIN_DEPLOYMENT *PSTORE_UNPIN_DEPLOYMENT;

typedef const STORE_UNPIN_DEPLOYMENT *PCSTORE_UNPIN_DEPLOYMENT;

/* [v1_enum] */ 
enum _STORE_UNPIN_DEPLOYMENT_DISPOSITIONS
    {	ISTORE_UNPIN_DEPLOYMENT_DISPOSITION_REMOVED	= 0x1
    } ;
/* [v1_enum] */ 
enum _STORE_INSTALL_DEPLOYMENT_FLAGS
    {	STORE_INSTALL_DEPLOYMENT_FLAG_UNINSTALL_OTHERS	= 0x1
    } ;
typedef struct _STORE_INSTALL_DEPLOYMENT
    {
    DWORD cbSize;
    DWORD dwFlags;
    IDefinitionAppId *pIApplicationIdentity;
    PCSTORE_ASSEMBLY_INSTALLATION_REFERENCE pReferenceTrack;
    } 	STORE_INSTALL_DEPLOYMENT;

typedef struct _STORE_INSTALL_DEPLOYMENT *PSTORE_INSTALL_DEPLOYMENT;

typedef const STORE_INSTALL_DEPLOYMENT *PCSTORE_INSTALL_DEPLOYMENT;

/* [v1_enum] */ 
enum _STORE_INSTALL_DEPLOYMENT_DISPOSITIONS
    {	ISTORE_INSTALL_DEPLOYMENT_DISPOSITION_ALREADY_INSTALLED	= 0x1,
	ISTORE_INSTALL_DEPLOYMENT_DISPOSITION_INSTALLED	= 0x2
    } ;
typedef struct _STORE_UNINSTALL_DEPLOYMENT
    {
    DWORD cbSize;
    DWORD dwFlags;
    IDefinitionAppId *pIApplicationIdentity;
    PCSTORE_ASSEMBLY_INSTALLATION_REFERENCE pReferenceTrack;
    } 	STORE_UNINSTALL_DEPLOYMENT;

typedef struct _STORE_UNINSTALL_DEPLOYMENT *PSTORE_UNINSTALL_DEPLOYMENT;

typedef const STORE_UNINSTALL_DEPLOYMENT *PCSTORE_UNINSTALL_DEPLOYMENT;

/* [v1_enum] */ 
enum _STORE_UNINSTALL_DEPLOYMENT_DISPOSITIONS
    {	ISTORE_UNINSTALL_DEPLOYMENT_DISPOSITION_NOT_EXIST	= 0x1,
	ISTORE_UNINSTALL_DEPLOYMENT_DISPOSITION_UNINSTALLED	= 0x2
    } ;
typedef struct _STORE_SET_DEPLOYMENT_METADATA_PROPERTY
    {
    GUID guidPropertySet;
    LPCWSTR pszName;
    SIZE_T nValueSize;
    /* [length_is][size_is] */ const BYTE *prgbValue;
    } 	STORE_SET_DEPLOYMENT_METADATA_PROPERTY;

typedef struct _STORE_SET_DEPLOYMENT_METADATA_PROPERTY *PSTORE_SET_DEPLOYMENT_METADATA_PROPERTY;

typedef const STORE_SET_DEPLOYMENT_METADATA_PROPERTY *PCSTORE_SET_DEPLOYMENT_METADATA_PROPERTY;

typedef struct _STORE_SET_DEPLOYMENT_METADATA
    {
    DWORD cbSize;
    DWORD dwFlags;
    IDefinitionAppId *pDeploymentIdentity;
    PCSTORE_ASSEMBLY_INSTALLATION_REFERENCE InstallReference;
    SIZE_T cPropertiesToTest;
    /* [length_is][size_is] */ const STORE_SET_DEPLOYMENT_METADATA_PROPERTY *rgPropertiesToTest;
    SIZE_T cPropertiesToSet;
    /* [length_is][size_is] */ const STORE_SET_DEPLOYMENT_METADATA_PROPERTY *rgPropertiesToSet;
    } 	STORE_SET_DEPLOYMENT_METADATA;

typedef struct _STORE_SET_DEPLOYMENT_METADATA *PSTORE_SET_DEPLOYMENT_METADATA;

typedef const STORE_SET_DEPLOYMENT_METADATA *PCSTORE_SET_DEPLOYMENT_METADATA;

/* [v1_enum] */ 
enum _STORE_SET_DEPLOYMENT_METADATA_DISPOSITIONS
    {	ISTORE_SET_DEPLOYMENT_METADATA_DISPOSITION_SET	= 0x2
    } ;
/* [v1_enum] */ 
enum _STORE_SCAVENGE_FLAGS
    {	STORE_SCAVENGE_FLAG_DEEP_CLEAN	= 0,
	STORE_SCAVENGE_FLAG_LIGHT_ONLY	= 0x1,
	STORE_SCAVENGE_FLAG_LIMIT_SIZE	= 0x2,
	STORE_SCAVENGE_FLAG_LIMIT_TIME	= 0x4,
	STORE_SCAVENGE_FLAG_LIMIT_COUNT	= 0x8
    } ;
typedef struct _STORE_SCAVENGE
    {
    DWORD cbSize;
    DWORD dwFlags;
    ULONGLONG SizeReclaimationLimit;
    ULONGLONG RuntimeLimit;
    DWORD ComponentCountLimit;
    } 	STORE_SCAVENGE;

typedef struct _STORE_SCAVENGE *PSTORE_SCAVENGE;

typedef const STORE_SCAVENGE *PCSTORE_SCAVENGE;

/* [v1_enum] */ 
enum _STORE_SCAVENGE_DISPOSITIONS
    {	ISTORE_SCAVENGE_DISPOSITION_SCAVENGED	= 0x1
    } ;
typedef /* [public][public][public][public][public][v1_enum] */ 
enum __MIDL_IStore_0001
    {	STORE_TXN_OP_INVALID	= 0,
	STORE_TXN_OP_SET_CANONICALIZATION_CONTEXT	= 14,
	STORE_TXN_OP_STAGE_COMPONENT	= 20,
	STORE_TXN_OP_PIN_DEPLOYMENT	= 21,
	STORE_TXN_OP_UNPIN_DEPLOYMENT	= 22,
	STORE_TXN_OP_STAGE_COMPONENT_FILE	= 23,
	STORE_TXN_OP_INSTALL_DEPLOYMENT	= 24,
	STORE_TXN_OP_UNINSTALL_DEPLOYMENT	= 25,
	STORE_TXN_OP_SET_DEPLOYMENT_METADATA	= 26,
	STORE_TXN_OP_SCAVENGE	= 27
    } 	STORE_TXN_OP_TYPE;

typedef /* [switch_type] */ union _STORE_TXN_OPERATION_DATA
    {
    /* [case()] */ PCSTORE_SET_CANONICALIZATION_CONTEXT SetCanonicalizationContext;
    /* [case()] */ PCSTORE_STAGE_COMPONENT StageComponent;
    /* [case()] */ PCSTORE_STAGE_COMPONENT_FILE StageComponentFile;
    /* [case()] */ PCSTORE_PIN_DEPLOYMENT PinDeployment;
    /* [case()] */ PCSTORE_UNPIN_DEPLOYMENT UnpinDeployment;
    /* [case()] */ PCSTORE_INSTALL_DEPLOYMENT InstallDeployment;
    /* [case()] */ PCSTORE_UNINSTALL_DEPLOYMENT UninstallDeployment;
    /* [case()] */ PCSTORE_SET_DEPLOYMENT_METADATA SetDeploymentMetadata;
    /* [case()] */ PCSTORE_SCAVENGE Scavenge;
    } 	STORE_TXN_OPERATION_DATA;

typedef /* [switch_type] */ union _STORE_TXN_OPERATION_DATA *PSTORE_TXN_OPERATION_DATA;

C_ASSERT(sizeof(STORE_TXN_OPERATION_DATA) == sizeof(PVOID));
typedef struct _STORE_TXN_OPERATION
    {
    STORE_TXN_OP_TYPE Operation;
    /* [switch_is] */ STORE_TXN_OPERATION_DATA Data;
    } 	STORE_TXN_OPERATION;

typedef struct _STORE_TXN_OPERATION *PSTORE_TXN_OPERATION;

typedef const STORE_TXN_OPERATION *PCSTORE_TXN_OPERATION;

/* [v1_enum] */ 
enum _ISTORE_BIND_REFERENCE_TO_ASSEMBLY_FLAGS
    {	ISTORE_BIND_REFERENCE_TO_ASSEMBLY_FLAG_FORCE_LIBRARY_SEMANTICS	= 0x1
    } ;
/* [v1_enum] */ 
enum _ISTORE_BIND_DEFINITIONS_DISPOSITIONS
    {	ISTORE_BIND_DEFINITIONS_DISPOSITION_STATE_UNDEFINED	= 0,
	ISTORE_BIND_DEFINITIONS_DISPOSITION_STATE_UNTOUCHED	= 1,
	ISTORE_BIND_DEFINITIONS_DISPOSITION_STATE_RESOLVED	= 2,
	ISTORE_BIND_DEFINITIONS_DISPOSITION_STATE_UNRESOLVED	= 3,
	ISTORE_BIND_DEFINITIONS_DISPOSITION_STATE_MASK	= 0xffff,
	ISTORE_BIND_DEFINITIONS_DISPOSITION_FLAG_POLICY_WAS_APPLIED	= 0x10000
    } ;
/* [v1_enum] */ 
enum _ISTORE_BINDING_RESULT_DISPOSITION_STATES
    {	ISTORE_BINDING_RESULT_DISPOSITION_STATE_UNDEFINED	= 0,
	ISTORE_BINDING_RESULT_DISPOSITION_STATE_UNTOUCHED	= 1,
	ISTORE_BINDING_RESULT_DISPOSITION_STATE_RESOLVED	= 2,
	ISTORE_BINDING_RESULT_DISPOSITION_STATE_UNRESOLVED	= 3,
	ISTORE_BINDING_RESULT_DISPOSITION_STATE_MASK	= 0xffff,
	ISTORE_BINDING_RESULT_DISPOSITION_FLAG_POLICY_WAS_APPLIED	= 0x10000
    } ;
typedef struct _ISTORE_BINDING_RESULT
    {
    DWORD dwFlags;
    ULONG ulDisposition;
    COMPONENT_VERSION cvVersion;
    GUID guidCacheCoherencyGuid;
    PVOID pvReserved;
    } 	ISTORE_BINDING_RESULT;

typedef struct _ISTORE_BINDING_RESULT *PISTORE_BINDING_RESULT;

typedef const ISTORE_BINDING_RESULT *PCISTORE_BINDING_RESULT;

/* [v1_enum] */ 
enum _ISTORE_ENUM_ASSEMBLIES_FLAGS
    {	ISTORE_ENUM_ASSEMBLIES_FLAG_LIMIT_TO_VISIBLE_ONLY	= 0x1,
	ISTORE_ENUM_ASSEMBLIES_FLAG_MATCH_SERVICING	= 0x2,
	ISTORE_ENUM_ASSEMBLIES_FLAG_FORCE_LIBRARY_SEMANTICS	= 0x4
    } ;
/* [v1_enum] */ 
enum _ISTORE_ENUM_FILES_FLAGS
    {	ISTORE_ENUM_FILES_FLAG_INCLUDE_INSTALLED_FILES	= 0x1,
	ISTORE_ENUM_FILES_FLAG_INCLUDE_MISSING_FILES	= 0x2
    } ;
/* [v1_enum] */ 
enum _ISTORE_ENUM_PRIVATE_FILES_FLAGS
    {	ISTORE_ENUM_PRIVATE_FILES_FLAG_INCLUDE_INSTALLED_FILES	= 0x1,
	ISTORE_ENUM_PRIVATE_FILES_FLAG_INCLUDE_MISSING_FILES	= 0x2
    } ;
/* [v1_enum] */ 
enum _ISTORE_ENUM_INSTALLER_DEPLOYMENT_METADATA
    {	ISTORE_ENUM_INSTALLER_DEPLOYMENT_METADATA_INCLUDE_FAMILIES	= 0x1,
	ISTORE_ENUM_INSTALLER_DEPLOYMENT_METADATA_INCLUDE_SPECIFICS	= 0x2
    } ;

EXTERN_C const IID IID_IStore;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("a5c62f6d-5e3e-4cd9-b345-6b281d7a1d1e")
    IStore : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Transact( 
            /* [in] */ SIZE_T cOperation,
            /* [size_is][in] */ const STORE_TXN_OPERATION rgOperations[  ],
            /* [size_is][out] */ ULONG rgDispositions[  ],
            /* [size_is][out] */ HRESULT rgResults[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE BindReferenceToAssembly( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [in] */ ULONG cDeploymentsToIgnore,
            /* [length_is][in] */ IDefinitionIdentity *rgpIDefinitionIdentity_DeploymentsToIgnore[  ],
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppAssembly) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CalculateDelimiterOfDeploymentsBasedOnQuota( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ SIZE_T cDeployments,
            /* [length_is][in] */ IDefinitionAppId *rgpIDefinitionAppId_Deployments[  ],
            /* [in] */ PCSTORE_ASSEMBLY_INSTALLATION_REFERENCE pReference,
            /* [in] */ ULONGLONG ulonglongQuote,
            /* [out] */ SIZE_T *Delimiter,
            /* [out] */ ULONGLONG *SizeSharedWithExternalDeployment,
            /* [out] */ ULONGLONG *SizeConsumedByInputDeploymentArray) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE BindDefinitions( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ SIZE_T DefinitionCount,
            /* [length_is][in] */ IDefinitionIdentity *rgpIDefinitionIdentity[  ],
            /* [in] */ ULONG cDeploymentsToIgnore,
            /* [length_is][in] */ IDefinitionIdentity *rgpIDefinitionIdentity_DeploymentsToIgnore[  ],
            /* [size_is][out] */ ISTORE_BINDING_RESULT rgBindingResults[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAssemblyInformation( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppManifest) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumAssemblies( 
            /* [in] */ DWORD dwFlags,
            /* [unique][in] */ IReferenceIdentity *pIReferenceIdentity_ToMatch,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppQueryResult) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumFiles( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pDefinitionIdentity,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppQueryResult) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumInstallationReferences( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pDefinitionIdentity,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppQueryResults) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LockAssemblyPath( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pDefinitionIdentity,
            /* [out] */ LPVOID *ppvCookie,
            /* [out] */ LPWSTR *ppszPayloadRoot) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReleaseAssemblyPath( 
            /* [in] */ LPVOID pvCookie) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE QueryChangeID( 
            /* [in] */ IDefinitionIdentity *pDefinitionIdentity,
            /* [out] */ ULONGLONG *pullChangeId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumCategories( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity_ToMatch,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppIUnknown) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumSubcategories( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pCategoryId,
            /* [in] */ LPCWSTR pszSubcategoryPathPattern,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppIUnknown) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumCategoryInstances( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pCategoryId,
            /* [in] */ LPCWSTR pszSubcategoryPath,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppUnknown) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDeploymentProperty( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pDeploymentInPackage,
            /* [in] */ PCSTORE_ASSEMBLY_INSTALLATION_REFERENCE InstallReference,
            /* [in] */ REFCLSID PropertySet,
            /* [in] */ LPCWSTR pcwszPropertyName,
            /* [retval][out] */ BLOB *PropertyValue) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LockApplicationPath( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIdentity,
            /* [out] */ LPVOID *ppvCookie,
            /* [out] */ LPWSTR *ppszPayloadRoot) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReleaseApplicationPath( 
            /* [in] */ LPVOID Cookie) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumPrivateFiles( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pApplicationId,
            /* [in] */ IDefinitionIdentity *pDefinitionIdentity,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppQueryResult) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumInstallerDeploymentMetadata( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ PCSTORE_ASSEMBLY_INSTALLATION_REFERENCE pReference,
            /* [in] */ IReferenceAppId *pDeploymentFilter,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppQueryResult) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumInstallerDeploymentMetadataProperties( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ PCSTORE_ASSEMBLY_INSTALLATION_REFERENCE pReference,
            /* [in] */ IDefinitionAppId *pAppidDeployment,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppQueryResult) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IStoreVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IStore * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IStore * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IStore * This);
        
        HRESULT ( STDMETHODCALLTYPE *Transact )( 
            IStore * This,
            /* [in] */ SIZE_T cOperation,
            /* [size_is][in] */ const STORE_TXN_OPERATION rgOperations[  ],
            /* [size_is][out] */ ULONG rgDispositions[  ],
            /* [size_is][out] */ HRESULT rgResults[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *BindReferenceToAssembly )( 
            IStore * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity,
            /* [in] */ ULONG cDeploymentsToIgnore,
            /* [length_is][in] */ IDefinitionIdentity *rgpIDefinitionIdentity_DeploymentsToIgnore[  ],
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppAssembly);
        
        HRESULT ( STDMETHODCALLTYPE *CalculateDelimiterOfDeploymentsBasedOnQuota )( 
            IStore * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ SIZE_T cDeployments,
            /* [length_is][in] */ IDefinitionAppId *rgpIDefinitionAppId_Deployments[  ],
            /* [in] */ PCSTORE_ASSEMBLY_INSTALLATION_REFERENCE pReference,
            /* [in] */ ULONGLONG ulonglongQuote,
            /* [out] */ SIZE_T *Delimiter,
            /* [out] */ ULONGLONG *SizeSharedWithExternalDeployment,
            /* [out] */ ULONGLONG *SizeConsumedByInputDeploymentArray);
        
        HRESULT ( STDMETHODCALLTYPE *BindDefinitions )( 
            IStore * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ SIZE_T DefinitionCount,
            /* [length_is][in] */ IDefinitionIdentity *rgpIDefinitionIdentity[  ],
            /* [in] */ ULONG cDeploymentsToIgnore,
            /* [length_is][in] */ IDefinitionIdentity *rgpIDefinitionIdentity_DeploymentsToIgnore[  ],
            /* [size_is][out] */ ISTORE_BINDING_RESULT rgBindingResults[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *GetAssemblyInformation )( 
            IStore * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppManifest);
        
        HRESULT ( STDMETHODCALLTYPE *EnumAssemblies )( 
            IStore * This,
            /* [in] */ DWORD dwFlags,
            /* [unique][in] */ IReferenceIdentity *pIReferenceIdentity_ToMatch,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppQueryResult);
        
        HRESULT ( STDMETHODCALLTYPE *EnumFiles )( 
            IStore * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pDefinitionIdentity,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppQueryResult);
        
        HRESULT ( STDMETHODCALLTYPE *EnumInstallationReferences )( 
            IStore * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pDefinitionIdentity,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppQueryResults);
        
        HRESULT ( STDMETHODCALLTYPE *LockAssemblyPath )( 
            IStore * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pDefinitionIdentity,
            /* [out] */ LPVOID *ppvCookie,
            /* [out] */ LPWSTR *ppszPayloadRoot);
        
        HRESULT ( STDMETHODCALLTYPE *ReleaseAssemblyPath )( 
            IStore * This,
            /* [in] */ LPVOID pvCookie);
        
        HRESULT ( STDMETHODCALLTYPE *QueryChangeID )( 
            IStore * This,
            /* [in] */ IDefinitionIdentity *pDefinitionIdentity,
            /* [out] */ ULONGLONG *pullChangeId);
        
        HRESULT ( STDMETHODCALLTYPE *EnumCategories )( 
            IStore * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *pIReferenceIdentity_ToMatch,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppIUnknown);
        
        HRESULT ( STDMETHODCALLTYPE *EnumSubcategories )( 
            IStore * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pCategoryId,
            /* [in] */ LPCWSTR pszSubcategoryPathPattern,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppIUnknown);
        
        HRESULT ( STDMETHODCALLTYPE *EnumCategoryInstances )( 
            IStore * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pCategoryId,
            /* [in] */ LPCWSTR pszSubcategoryPath,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppUnknown);
        
        HRESULT ( STDMETHODCALLTYPE *GetDeploymentProperty )( 
            IStore * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pDeploymentInPackage,
            /* [in] */ PCSTORE_ASSEMBLY_INSTALLATION_REFERENCE InstallReference,
            /* [in] */ REFCLSID PropertySet,
            /* [in] */ LPCWSTR pcwszPropertyName,
            /* [retval][out] */ BLOB *PropertyValue);
        
        HRESULT ( STDMETHODCALLTYPE *LockApplicationPath )( 
            IStore * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIdentity,
            /* [out] */ LPVOID *ppvCookie,
            /* [out] */ LPWSTR *ppszPayloadRoot);
        
        HRESULT ( STDMETHODCALLTYPE *ReleaseApplicationPath )( 
            IStore * This,
            /* [in] */ LPVOID Cookie);
        
        HRESULT ( STDMETHODCALLTYPE *EnumPrivateFiles )( 
            IStore * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pApplicationId,
            /* [in] */ IDefinitionIdentity *pDefinitionIdentity,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppQueryResult);
        
        HRESULT ( STDMETHODCALLTYPE *EnumInstallerDeploymentMetadata )( 
            IStore * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ PCSTORE_ASSEMBLY_INSTALLATION_REFERENCE pReference,
            /* [in] */ IReferenceAppId *pDeploymentFilter,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppQueryResult);
        
        HRESULT ( STDMETHODCALLTYPE *EnumInstallerDeploymentMetadataProperties )( 
            IStore * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ PCSTORE_ASSEMBLY_INSTALLATION_REFERENCE pReference,
            /* [in] */ IDefinitionAppId *pAppidDeployment,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppQueryResult);
        
        END_INTERFACE
    } IStoreVtbl;

    interface IStore
    {
        CONST_VTBL struct IStoreVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IStore_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IStore_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IStore_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IStore_Transact(This,cOperation,rgOperations,rgDispositions,rgResults)	\
    (This)->lpVtbl -> Transact(This,cOperation,rgOperations,rgDispositions,rgResults)

#define IStore_BindReferenceToAssembly(This,dwFlags,pIReferenceIdentity,cDeploymentsToIgnore,rgpIDefinitionIdentity_DeploymentsToIgnore,riid,ppAssembly)	\
    (This)->lpVtbl -> BindReferenceToAssembly(This,dwFlags,pIReferenceIdentity,cDeploymentsToIgnore,rgpIDefinitionIdentity_DeploymentsToIgnore,riid,ppAssembly)

#define IStore_CalculateDelimiterOfDeploymentsBasedOnQuota(This,dwFlags,cDeployments,rgpIDefinitionAppId_Deployments,pReference,ulonglongQuote,Delimiter,SizeSharedWithExternalDeployment,SizeConsumedByInputDeploymentArray)	\
    (This)->lpVtbl -> CalculateDelimiterOfDeploymentsBasedOnQuota(This,dwFlags,cDeployments,rgpIDefinitionAppId_Deployments,pReference,ulonglongQuote,Delimiter,SizeSharedWithExternalDeployment,SizeConsumedByInputDeploymentArray)

#define IStore_BindDefinitions(This,dwFlags,DefinitionCount,rgpIDefinitionIdentity,cDeploymentsToIgnore,rgpIDefinitionIdentity_DeploymentsToIgnore,rgBindingResults)	\
    (This)->lpVtbl -> BindDefinitions(This,dwFlags,DefinitionCount,rgpIDefinitionIdentity,cDeploymentsToIgnore,rgpIDefinitionIdentity_DeploymentsToIgnore,rgBindingResults)

#define IStore_GetAssemblyInformation(This,dwFlags,pIDefinitionIdentity,riid,ppManifest)	\
    (This)->lpVtbl -> GetAssemblyInformation(This,dwFlags,pIDefinitionIdentity,riid,ppManifest)

#define IStore_EnumAssemblies(This,dwFlags,pIReferenceIdentity_ToMatch,riid,ppQueryResult)	\
    (This)->lpVtbl -> EnumAssemblies(This,dwFlags,pIReferenceIdentity_ToMatch,riid,ppQueryResult)

#define IStore_EnumFiles(This,dwFlags,pDefinitionIdentity,riid,ppQueryResult)	\
    (This)->lpVtbl -> EnumFiles(This,dwFlags,pDefinitionIdentity,riid,ppQueryResult)

#define IStore_EnumInstallationReferences(This,dwFlags,pDefinitionIdentity,riid,ppQueryResults)	\
    (This)->lpVtbl -> EnumInstallationReferences(This,dwFlags,pDefinitionIdentity,riid,ppQueryResults)

#define IStore_LockAssemblyPath(This,dwFlags,pDefinitionIdentity,ppvCookie,ppszPayloadRoot)	\
    (This)->lpVtbl -> LockAssemblyPath(This,dwFlags,pDefinitionIdentity,ppvCookie,ppszPayloadRoot)

#define IStore_ReleaseAssemblyPath(This,pvCookie)	\
    (This)->lpVtbl -> ReleaseAssemblyPath(This,pvCookie)

#define IStore_QueryChangeID(This,pDefinitionIdentity,pullChangeId)	\
    (This)->lpVtbl -> QueryChangeID(This,pDefinitionIdentity,pullChangeId)

#define IStore_EnumCategories(This,dwFlags,pIReferenceIdentity_ToMatch,riid,ppIUnknown)	\
    (This)->lpVtbl -> EnumCategories(This,dwFlags,pIReferenceIdentity_ToMatch,riid,ppIUnknown)

#define IStore_EnumSubcategories(This,dwFlags,pCategoryId,pszSubcategoryPathPattern,riid,ppIUnknown)	\
    (This)->lpVtbl -> EnumSubcategories(This,dwFlags,pCategoryId,pszSubcategoryPathPattern,riid,ppIUnknown)

#define IStore_EnumCategoryInstances(This,dwFlags,pCategoryId,pszSubcategoryPath,riid,ppUnknown)	\
    (This)->lpVtbl -> EnumCategoryInstances(This,dwFlags,pCategoryId,pszSubcategoryPath,riid,ppUnknown)

#define IStore_GetDeploymentProperty(This,dwFlags,pDeploymentInPackage,InstallReference,PropertySet,pcwszPropertyName,PropertyValue)	\
    (This)->lpVtbl -> GetDeploymentProperty(This,dwFlags,pDeploymentInPackage,InstallReference,PropertySet,pcwszPropertyName,PropertyValue)

#define IStore_LockApplicationPath(This,dwFlags,pIdentity,ppvCookie,ppszPayloadRoot)	\
    (This)->lpVtbl -> LockApplicationPath(This,dwFlags,pIdentity,ppvCookie,ppszPayloadRoot)

#define IStore_ReleaseApplicationPath(This,Cookie)	\
    (This)->lpVtbl -> ReleaseApplicationPath(This,Cookie)

#define IStore_EnumPrivateFiles(This,dwFlags,pApplicationId,pDefinitionIdentity,riid,ppQueryResult)	\
    (This)->lpVtbl -> EnumPrivateFiles(This,dwFlags,pApplicationId,pDefinitionIdentity,riid,ppQueryResult)

#define IStore_EnumInstallerDeploymentMetadata(This,dwFlags,pReference,pDeploymentFilter,riid,ppQueryResult)	\
    (This)->lpVtbl -> EnumInstallerDeploymentMetadata(This,dwFlags,pReference,pDeploymentFilter,riid,ppQueryResult)

#define IStore_EnumInstallerDeploymentMetadataProperties(This,dwFlags,pReference,pAppidDeployment,riid,ppQueryResult)	\
    (This)->lpVtbl -> EnumInstallerDeploymentMetadataProperties(This,dwFlags,pReference,pAppidDeployment,riid,ppQueryResult)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IStore_Transact_Proxy( 
    IStore * This,
    /* [in] */ SIZE_T cOperation,
    /* [size_is][in] */ const STORE_TXN_OPERATION rgOperations[  ],
    /* [size_is][out] */ ULONG rgDispositions[  ],
    /* [size_is][out] */ HRESULT rgResults[  ]);


void __RPC_STUB IStore_Transact_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStore_BindReferenceToAssembly_Proxy( 
    IStore * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceIdentity *pIReferenceIdentity,
    /* [in] */ ULONG cDeploymentsToIgnore,
    /* [length_is][in] */ IDefinitionIdentity *rgpIDefinitionIdentity_DeploymentsToIgnore[  ],
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppAssembly);


void __RPC_STUB IStore_BindReferenceToAssembly_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStore_CalculateDelimiterOfDeploymentsBasedOnQuota_Proxy( 
    IStore * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ SIZE_T cDeployments,
    /* [length_is][in] */ IDefinitionAppId *rgpIDefinitionAppId_Deployments[  ],
    /* [in] */ PCSTORE_ASSEMBLY_INSTALLATION_REFERENCE pReference,
    /* [in] */ ULONGLONG ulonglongQuote,
    /* [out] */ SIZE_T *Delimiter,
    /* [out] */ ULONGLONG *SizeSharedWithExternalDeployment,
    /* [out] */ ULONGLONG *SizeConsumedByInputDeploymentArray);


void __RPC_STUB IStore_CalculateDelimiterOfDeploymentsBasedOnQuota_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStore_BindDefinitions_Proxy( 
    IStore * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ SIZE_T DefinitionCount,
    /* [length_is][in] */ IDefinitionIdentity *rgpIDefinitionIdentity[  ],
    /* [in] */ ULONG cDeploymentsToIgnore,
    /* [length_is][in] */ IDefinitionIdentity *rgpIDefinitionIdentity_DeploymentsToIgnore[  ],
    /* [size_is][out] */ ISTORE_BINDING_RESULT rgBindingResults[  ]);


void __RPC_STUB IStore_BindDefinitions_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStore_GetAssemblyInformation_Proxy( 
    IStore * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppManifest);


void __RPC_STUB IStore_GetAssemblyInformation_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStore_EnumAssemblies_Proxy( 
    IStore * This,
    /* [in] */ DWORD dwFlags,
    /* [unique][in] */ IReferenceIdentity *pIReferenceIdentity_ToMatch,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppQueryResult);


void __RPC_STUB IStore_EnumAssemblies_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStore_EnumFiles_Proxy( 
    IStore * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pDefinitionIdentity,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppQueryResult);


void __RPC_STUB IStore_EnumFiles_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStore_EnumInstallationReferences_Proxy( 
    IStore * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pDefinitionIdentity,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppQueryResults);


void __RPC_STUB IStore_EnumInstallationReferences_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStore_LockAssemblyPath_Proxy( 
    IStore * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pDefinitionIdentity,
    /* [out] */ LPVOID *ppvCookie,
    /* [out] */ LPWSTR *ppszPayloadRoot);


void __RPC_STUB IStore_LockAssemblyPath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStore_ReleaseAssemblyPath_Proxy( 
    IStore * This,
    /* [in] */ LPVOID pvCookie);


void __RPC_STUB IStore_ReleaseAssemblyPath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStore_QueryChangeID_Proxy( 
    IStore * This,
    /* [in] */ IDefinitionIdentity *pDefinitionIdentity,
    /* [out] */ ULONGLONG *pullChangeId);


void __RPC_STUB IStore_QueryChangeID_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStore_EnumCategories_Proxy( 
    IStore * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceIdentity *pIReferenceIdentity_ToMatch,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppIUnknown);


void __RPC_STUB IStore_EnumCategories_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStore_EnumSubcategories_Proxy( 
    IStore * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pCategoryId,
    /* [in] */ LPCWSTR pszSubcategoryPathPattern,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppIUnknown);


void __RPC_STUB IStore_EnumSubcategories_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStore_EnumCategoryInstances_Proxy( 
    IStore * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pCategoryId,
    /* [in] */ LPCWSTR pszSubcategoryPath,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppUnknown);


void __RPC_STUB IStore_EnumCategoryInstances_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStore_GetDeploymentProperty_Proxy( 
    IStore * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionAppId *pDeploymentInPackage,
    /* [in] */ PCSTORE_ASSEMBLY_INSTALLATION_REFERENCE InstallReference,
    /* [in] */ REFCLSID PropertySet,
    /* [in] */ LPCWSTR pcwszPropertyName,
    /* [retval][out] */ BLOB *PropertyValue);


void __RPC_STUB IStore_GetDeploymentProperty_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStore_LockApplicationPath_Proxy( 
    IStore * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionAppId *pIdentity,
    /* [out] */ LPVOID *ppvCookie,
    /* [out] */ LPWSTR *ppszPayloadRoot);


void __RPC_STUB IStore_LockApplicationPath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStore_ReleaseApplicationPath_Proxy( 
    IStore * This,
    /* [in] */ LPVOID Cookie);


void __RPC_STUB IStore_ReleaseApplicationPath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStore_EnumPrivateFiles_Proxy( 
    IStore * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionAppId *pApplicationId,
    /* [in] */ IDefinitionIdentity *pDefinitionIdentity,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppQueryResult);


void __RPC_STUB IStore_EnumPrivateFiles_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStore_EnumInstallerDeploymentMetadata_Proxy( 
    IStore * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ PCSTORE_ASSEMBLY_INSTALLATION_REFERENCE pReference,
    /* [in] */ IReferenceAppId *pDeploymentFilter,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppQueryResult);


void __RPC_STUB IStore_EnumInstallerDeploymentMetadata_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStore_EnumInstallerDeploymentMetadataProperties_Proxy( 
    IStore * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ PCSTORE_ASSEMBLY_INSTALLATION_REFERENCE pReference,
    /* [in] */ IDefinitionAppId *pAppidDeployment,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppQueryResult);


void __RPC_STUB IStore_EnumInstallerDeploymentMetadataProperties_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IStore_INTERFACE_DEFINED__ */


#ifndef __IMigrateStore_INTERFACE_DEFINED__
#define __IMigrateStore_INTERFACE_DEFINED__

/* interface IMigrateStore */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IMigrateStore;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("a5c6a738-fc6a-4204-b4db-b8629b67e655")
    IMigrateStore : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Migrate( 
            /* [in] */ LPVOID pvReserved) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IMigrateStoreVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IMigrateStore * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IMigrateStore * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IMigrateStore * This);
        
        HRESULT ( STDMETHODCALLTYPE *Migrate )( 
            IMigrateStore * This,
            /* [in] */ LPVOID pvReserved);
        
        END_INTERFACE
    } IMigrateStoreVtbl;

    interface IMigrateStore
    {
        CONST_VTBL struct IMigrateStoreVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IMigrateStore_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IMigrateStore_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IMigrateStore_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IMigrateStore_Migrate(This,pvReserved)	\
    (This)->lpVtbl -> Migrate(This,pvReserved)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IMigrateStore_Migrate_Proxy( 
    IMigrateStore * This,
    /* [in] */ LPVOID pvReserved);


void __RPC_STUB IMigrateStore_Migrate_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IMigrateStore_INTERFACE_DEFINED__ */


#ifndef __IEnumSTORE_DEPLOYMENT_METADATA_INTERFACE_DEFINED__
#define __IEnumSTORE_DEPLOYMENT_METADATA_INTERFACE_DEFINED__

/* interface IEnumSTORE_DEPLOYMENT_METADATA */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IEnumSTORE_DEPLOYMENT_METADATA;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("f9fd4090-93db-45c0-af87-624940f19cff")
    IEnumSTORE_DEPLOYMENT_METADATA : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ IDefinitionAppId *AppIds[  ],
            /* [optional][out] */ ULONG *pceltFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG celt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ IEnumSTORE_DEPLOYMENT_METADATA **ppEnum) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEnumSTORE_DEPLOYMENT_METADATAVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumSTORE_DEPLOYMENT_METADATA * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumSTORE_DEPLOYMENT_METADATA * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumSTORE_DEPLOYMENT_METADATA * This);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumSTORE_DEPLOYMENT_METADATA * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ IDefinitionAppId *AppIds[  ],
            /* [optional][out] */ ULONG *pceltFetched);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumSTORE_DEPLOYMENT_METADATA * This,
            /* [in] */ ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumSTORE_DEPLOYMENT_METADATA * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumSTORE_DEPLOYMENT_METADATA * This,
            /* [out] */ IEnumSTORE_DEPLOYMENT_METADATA **ppEnum);
        
        END_INTERFACE
    } IEnumSTORE_DEPLOYMENT_METADATAVtbl;

    interface IEnumSTORE_DEPLOYMENT_METADATA
    {
        CONST_VTBL struct IEnumSTORE_DEPLOYMENT_METADATAVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumSTORE_DEPLOYMENT_METADATA_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEnumSTORE_DEPLOYMENT_METADATA_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEnumSTORE_DEPLOYMENT_METADATA_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEnumSTORE_DEPLOYMENT_METADATA_Next(This,celt,AppIds,pceltFetched)	\
    (This)->lpVtbl -> Next(This,celt,AppIds,pceltFetched)

#define IEnumSTORE_DEPLOYMENT_METADATA_Skip(This,celt)	\
    (This)->lpVtbl -> Skip(This,celt)

#define IEnumSTORE_DEPLOYMENT_METADATA_Reset(This)	\
    (This)->lpVtbl -> Reset(This)

#define IEnumSTORE_DEPLOYMENT_METADATA_Clone(This,ppEnum)	\
    (This)->lpVtbl -> Clone(This,ppEnum)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IEnumSTORE_DEPLOYMENT_METADATA_Next_Proxy( 
    IEnumSTORE_DEPLOYMENT_METADATA * This,
    /* [in] */ ULONG celt,
    /* [length_is][size_is][out] */ IDefinitionAppId *AppIds[  ],
    /* [optional][out] */ ULONG *pceltFetched);


void __RPC_STUB IEnumSTORE_DEPLOYMENT_METADATA_Next_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_DEPLOYMENT_METADATA_Skip_Proxy( 
    IEnumSTORE_DEPLOYMENT_METADATA * This,
    /* [in] */ ULONG celt);


void __RPC_STUB IEnumSTORE_DEPLOYMENT_METADATA_Skip_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_DEPLOYMENT_METADATA_Reset_Proxy( 
    IEnumSTORE_DEPLOYMENT_METADATA * This);


void __RPC_STUB IEnumSTORE_DEPLOYMENT_METADATA_Reset_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_DEPLOYMENT_METADATA_Clone_Proxy( 
    IEnumSTORE_DEPLOYMENT_METADATA * This,
    /* [out] */ IEnumSTORE_DEPLOYMENT_METADATA **ppEnum);


void __RPC_STUB IEnumSTORE_DEPLOYMENT_METADATA_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumSTORE_DEPLOYMENT_METADATA_INTERFACE_DEFINED__ */


#ifndef __IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_INTERFACE_DEFINED__
#define __IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_INTERFACE_DEFINED__

/* interface IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY */
/* [local][unique][uuid][object] */ 

typedef struct _STORE_DEPLOYMENT_METADATA_PROPERTY
    {
    GUID guidPropertySet;
    LPCWSTR pszName;
    SIZE_T cbValue;
    /* [length_is][size_is] */ const BYTE *prgbValue;
    } 	STORE_DEPLOYMENT_METADATA_PROPERTY;

typedef struct _STORE_DEPLOYMENT_METADATA_PROPERTY *PSTORE_DEPLOYMENT_METADATA_PROPERTY;


EXTERN_C const IID IID_IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("5fa4f590-a416-4b22-ac79-7c3f0d31f303")
    IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ STORE_DEPLOYMENT_METADATA_PROPERTY AppIds[  ],
            /* [optional][out] */ ULONG *pceltFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG celt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY **ppEnum) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEnumSTORE_DEPLOYMENT_METADATA_PROPERTYVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY * This);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ STORE_DEPLOYMENT_METADATA_PROPERTY AppIds[  ],
            /* [optional][out] */ ULONG *pceltFetched);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY * This,
            /* [in] */ ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY * This,
            /* [out] */ IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY **ppEnum);
        
        END_INTERFACE
    } IEnumSTORE_DEPLOYMENT_METADATA_PROPERTYVtbl;

    interface IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY
    {
        CONST_VTBL struct IEnumSTORE_DEPLOYMENT_METADATA_PROPERTYVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_Next(This,celt,AppIds,pceltFetched)	\
    (This)->lpVtbl -> Next(This,celt,AppIds,pceltFetched)

#define IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_Skip(This,celt)	\
    (This)->lpVtbl -> Skip(This,celt)

#define IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_Reset(This)	\
    (This)->lpVtbl -> Reset(This)

#define IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_Clone(This,ppEnum)	\
    (This)->lpVtbl -> Clone(This,ppEnum)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_Next_Proxy( 
    IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY * This,
    /* [in] */ ULONG celt,
    /* [length_is][size_is][out] */ STORE_DEPLOYMENT_METADATA_PROPERTY AppIds[  ],
    /* [optional][out] */ ULONG *pceltFetched);


void __RPC_STUB IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_Next_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_Skip_Proxy( 
    IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY * This,
    /* [in] */ ULONG celt);


void __RPC_STUB IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_Skip_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_Reset_Proxy( 
    IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY * This);


void __RPC_STUB IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_Reset_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_Clone_Proxy( 
    IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY * This,
    /* [out] */ IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY **ppEnum);


void __RPC_STUB IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY_INTERFACE_DEFINED__ */


#ifndef __IEnumSTORE_ASSEMBLY_INTERFACE_DEFINED__
#define __IEnumSTORE_ASSEMBLY_INTERFACE_DEFINED__

/* interface IEnumSTORE_ASSEMBLY */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IEnumSTORE_ASSEMBLY;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("a5c637bf-6eaa-4e5f-b535-55299657e33e")
    IEnumSTORE_ASSEMBLY : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ STORE_ASSEMBLY rgelt[  ],
            /* [optional][out] */ ULONG *pceltFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG celt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ IEnumSTORE_ASSEMBLY **ppEnum) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEnumSTORE_ASSEMBLYVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumSTORE_ASSEMBLY * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumSTORE_ASSEMBLY * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumSTORE_ASSEMBLY * This);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumSTORE_ASSEMBLY * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ STORE_ASSEMBLY rgelt[  ],
            /* [optional][out] */ ULONG *pceltFetched);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumSTORE_ASSEMBLY * This,
            /* [in] */ ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumSTORE_ASSEMBLY * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumSTORE_ASSEMBLY * This,
            /* [out] */ IEnumSTORE_ASSEMBLY **ppEnum);
        
        END_INTERFACE
    } IEnumSTORE_ASSEMBLYVtbl;

    interface IEnumSTORE_ASSEMBLY
    {
        CONST_VTBL struct IEnumSTORE_ASSEMBLYVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumSTORE_ASSEMBLY_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEnumSTORE_ASSEMBLY_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEnumSTORE_ASSEMBLY_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEnumSTORE_ASSEMBLY_Next(This,celt,rgelt,pceltFetched)	\
    (This)->lpVtbl -> Next(This,celt,rgelt,pceltFetched)

#define IEnumSTORE_ASSEMBLY_Skip(This,celt)	\
    (This)->lpVtbl -> Skip(This,celt)

#define IEnumSTORE_ASSEMBLY_Reset(This)	\
    (This)->lpVtbl -> Reset(This)

#define IEnumSTORE_ASSEMBLY_Clone(This,ppEnum)	\
    (This)->lpVtbl -> Clone(This,ppEnum)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IEnumSTORE_ASSEMBLY_Next_Proxy( 
    IEnumSTORE_ASSEMBLY * This,
    /* [in] */ ULONG celt,
    /* [length_is][size_is][out] */ STORE_ASSEMBLY rgelt[  ],
    /* [optional][out] */ ULONG *pceltFetched);


void __RPC_STUB IEnumSTORE_ASSEMBLY_Next_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_ASSEMBLY_Skip_Proxy( 
    IEnumSTORE_ASSEMBLY * This,
    /* [in] */ ULONG celt);


void __RPC_STUB IEnumSTORE_ASSEMBLY_Skip_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_ASSEMBLY_Reset_Proxy( 
    IEnumSTORE_ASSEMBLY * This);


void __RPC_STUB IEnumSTORE_ASSEMBLY_Reset_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_ASSEMBLY_Clone_Proxy( 
    IEnumSTORE_ASSEMBLY * This,
    /* [out] */ IEnumSTORE_ASSEMBLY **ppEnum);


void __RPC_STUB IEnumSTORE_ASSEMBLY_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumSTORE_ASSEMBLY_INTERFACE_DEFINED__ */


#ifndef __IEnumSTORE_ASSEMBLY_FILE_INTERFACE_DEFINED__
#define __IEnumSTORE_ASSEMBLY_FILE_INTERFACE_DEFINED__

/* interface IEnumSTORE_ASSEMBLY_FILE */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IEnumSTORE_ASSEMBLY_FILE;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("a5c6aaa3-03e4-478d-b9f5-2e45908d5e4f")
    IEnumSTORE_ASSEMBLY_FILE : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ STORE_ASSEMBLY_FILE rgelt[  ],
            /* [optional][out] */ ULONG *pceltFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG celt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ IEnumSTORE_ASSEMBLY_FILE **ppEnum) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEnumSTORE_ASSEMBLY_FILEVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumSTORE_ASSEMBLY_FILE * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumSTORE_ASSEMBLY_FILE * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumSTORE_ASSEMBLY_FILE * This);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumSTORE_ASSEMBLY_FILE * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ STORE_ASSEMBLY_FILE rgelt[  ],
            /* [optional][out] */ ULONG *pceltFetched);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumSTORE_ASSEMBLY_FILE * This,
            /* [in] */ ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumSTORE_ASSEMBLY_FILE * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumSTORE_ASSEMBLY_FILE * This,
            /* [out] */ IEnumSTORE_ASSEMBLY_FILE **ppEnum);
        
        END_INTERFACE
    } IEnumSTORE_ASSEMBLY_FILEVtbl;

    interface IEnumSTORE_ASSEMBLY_FILE
    {
        CONST_VTBL struct IEnumSTORE_ASSEMBLY_FILEVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumSTORE_ASSEMBLY_FILE_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEnumSTORE_ASSEMBLY_FILE_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEnumSTORE_ASSEMBLY_FILE_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEnumSTORE_ASSEMBLY_FILE_Next(This,celt,rgelt,pceltFetched)	\
    (This)->lpVtbl -> Next(This,celt,rgelt,pceltFetched)

#define IEnumSTORE_ASSEMBLY_FILE_Skip(This,celt)	\
    (This)->lpVtbl -> Skip(This,celt)

#define IEnumSTORE_ASSEMBLY_FILE_Reset(This)	\
    (This)->lpVtbl -> Reset(This)

#define IEnumSTORE_ASSEMBLY_FILE_Clone(This,ppEnum)	\
    (This)->lpVtbl -> Clone(This,ppEnum)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IEnumSTORE_ASSEMBLY_FILE_Next_Proxy( 
    IEnumSTORE_ASSEMBLY_FILE * This,
    /* [in] */ ULONG celt,
    /* [length_is][size_is][out] */ STORE_ASSEMBLY_FILE rgelt[  ],
    /* [optional][out] */ ULONG *pceltFetched);


void __RPC_STUB IEnumSTORE_ASSEMBLY_FILE_Next_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_ASSEMBLY_FILE_Skip_Proxy( 
    IEnumSTORE_ASSEMBLY_FILE * This,
    /* [in] */ ULONG celt);


void __RPC_STUB IEnumSTORE_ASSEMBLY_FILE_Skip_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_ASSEMBLY_FILE_Reset_Proxy( 
    IEnumSTORE_ASSEMBLY_FILE * This);


void __RPC_STUB IEnumSTORE_ASSEMBLY_FILE_Reset_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_ASSEMBLY_FILE_Clone_Proxy( 
    IEnumSTORE_ASSEMBLY_FILE * This,
    /* [out] */ IEnumSTORE_ASSEMBLY_FILE **ppEnum);


void __RPC_STUB IEnumSTORE_ASSEMBLY_FILE_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumSTORE_ASSEMBLY_FILE_INTERFACE_DEFINED__ */


#ifndef __IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_INTERFACE_DEFINED__
#define __IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_INTERFACE_DEFINED__

/* interface IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("d8b1aacb-5142-4abb-bcc1-e9dc9052a89e")
    IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ STORE_ASSEMBLY_INSTALLATION_REFERENCE rgelt[  ],
            /* [optional][out] */ ULONG *pceltFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG celt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE **ppIEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCEVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE * This);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ STORE_ASSEMBLY_INSTALLATION_REFERENCE rgelt[  ],
            /* [optional][out] */ ULONG *pceltFetched);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE * This,
            /* [in] */ ULONG celt);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE * This,
            /* [out] */ IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE **ppIEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE);
        
        END_INTERFACE
    } IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCEVtbl;

    interface IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE
    {
        CONST_VTBL struct IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCEVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_Next(This,celt,rgelt,pceltFetched)	\
    (This)->lpVtbl -> Next(This,celt,rgelt,pceltFetched)

#define IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_Skip(This,celt)	\
    (This)->lpVtbl -> Skip(This,celt)

#define IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_Reset(This)	\
    (This)->lpVtbl -> Reset(This)

#define IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_Clone(This,ppIEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE)	\
    (This)->lpVtbl -> Clone(This,ppIEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_Next_Proxy( 
    IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE * This,
    /* [in] */ ULONG celt,
    /* [length_is][size_is][out] */ STORE_ASSEMBLY_INSTALLATION_REFERENCE rgelt[  ],
    /* [optional][out] */ ULONG *pceltFetched);


void __RPC_STUB IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_Next_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_Skip_Proxy( 
    IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE * This,
    /* [in] */ ULONG celt);


void __RPC_STUB IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_Skip_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_Reset_Proxy( 
    IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE * This);


void __RPC_STUB IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_Reset_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_Clone_Proxy( 
    IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE * This,
    /* [out] */ IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE **ppIEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE);


void __RPC_STUB IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE_INTERFACE_DEFINED__ */


#ifndef __IEnumCATEGORY_INTERFACE_DEFINED__
#define __IEnumCATEGORY_INTERFACE_DEFINED__

/* interface IEnumCATEGORY */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IEnumCATEGORY;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("02249bf3-e0ef-4396-b8b7-8882e981175f")
    IEnumCATEGORY : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ CATEGORY rgElements[  ],
            /* [out] */ ULONG *pulFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG ulElements) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ IEnumCATEGORY **ppIEnumCATEGORY) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEnumCATEGORYVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumCATEGORY * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumCATEGORY * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumCATEGORY * This);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumCATEGORY * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ CATEGORY rgElements[  ],
            /* [out] */ ULONG *pulFetched);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumCATEGORY * This,
            /* [in] */ ULONG ulElements);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumCATEGORY * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumCATEGORY * This,
            /* [out] */ IEnumCATEGORY **ppIEnumCATEGORY);
        
        END_INTERFACE
    } IEnumCATEGORYVtbl;

    interface IEnumCATEGORY
    {
        CONST_VTBL struct IEnumCATEGORYVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumCATEGORY_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEnumCATEGORY_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEnumCATEGORY_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEnumCATEGORY_Next(This,celt,rgElements,pulFetched)	\
    (This)->lpVtbl -> Next(This,celt,rgElements,pulFetched)

#define IEnumCATEGORY_Skip(This,ulElements)	\
    (This)->lpVtbl -> Skip(This,ulElements)

#define IEnumCATEGORY_Reset(This)	\
    (This)->lpVtbl -> Reset(This)

#define IEnumCATEGORY_Clone(This,ppIEnumCATEGORY)	\
    (This)->lpVtbl -> Clone(This,ppIEnumCATEGORY)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IEnumCATEGORY_Next_Proxy( 
    IEnumCATEGORY * This,
    /* [in] */ ULONG celt,
    /* [length_is][size_is][out] */ CATEGORY rgElements[  ],
    /* [out] */ ULONG *pulFetched);


void __RPC_STUB IEnumCATEGORY_Next_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumCATEGORY_Skip_Proxy( 
    IEnumCATEGORY * This,
    /* [in] */ ULONG ulElements);


void __RPC_STUB IEnumCATEGORY_Skip_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumCATEGORY_Reset_Proxy( 
    IEnumCATEGORY * This);


void __RPC_STUB IEnumCATEGORY_Reset_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumCATEGORY_Clone_Proxy( 
    IEnumCATEGORY * This,
    /* [out] */ IEnumCATEGORY **ppIEnumCATEGORY);


void __RPC_STUB IEnumCATEGORY_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumCATEGORY_INTERFACE_DEFINED__ */


#ifndef __IEnumCATEGORY_SUBCATEGORY_INTERFACE_DEFINED__
#define __IEnumCATEGORY_SUBCATEGORY_INTERFACE_DEFINED__

/* interface IEnumCATEGORY_SUBCATEGORY */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IEnumCATEGORY_SUBCATEGORY;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("5f9fdbe5-57e1-49f6-bb9d-28c1a1503818")
    IEnumCATEGORY_SUBCATEGORY : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ CATEGORY_SUBCATEGORY rgElements[  ],
            /* [out] */ ULONG *pulFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG ulElements) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ IEnumCATEGORY_SUBCATEGORY **ppIEnumCATEGORY_SUBCATEGORY) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEnumCATEGORY_SUBCATEGORYVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumCATEGORY_SUBCATEGORY * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumCATEGORY_SUBCATEGORY * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumCATEGORY_SUBCATEGORY * This);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumCATEGORY_SUBCATEGORY * This,
            /* [in] */ ULONG celt,
            /* [length_is][size_is][out] */ CATEGORY_SUBCATEGORY rgElements[  ],
            /* [out] */ ULONG *pulFetched);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumCATEGORY_SUBCATEGORY * This,
            /* [in] */ ULONG ulElements);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumCATEGORY_SUBCATEGORY * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumCATEGORY_SUBCATEGORY * This,
            /* [out] */ IEnumCATEGORY_SUBCATEGORY **ppIEnumCATEGORY_SUBCATEGORY);
        
        END_INTERFACE
    } IEnumCATEGORY_SUBCATEGORYVtbl;

    interface IEnumCATEGORY_SUBCATEGORY
    {
        CONST_VTBL struct IEnumCATEGORY_SUBCATEGORYVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumCATEGORY_SUBCATEGORY_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEnumCATEGORY_SUBCATEGORY_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEnumCATEGORY_SUBCATEGORY_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEnumCATEGORY_SUBCATEGORY_Next(This,celt,rgElements,pulFetched)	\
    (This)->lpVtbl -> Next(This,celt,rgElements,pulFetched)

#define IEnumCATEGORY_SUBCATEGORY_Skip(This,ulElements)	\
    (This)->lpVtbl -> Skip(This,ulElements)

#define IEnumCATEGORY_SUBCATEGORY_Reset(This)	\
    (This)->lpVtbl -> Reset(This)

#define IEnumCATEGORY_SUBCATEGORY_Clone(This,ppIEnumCATEGORY_SUBCATEGORY)	\
    (This)->lpVtbl -> Clone(This,ppIEnumCATEGORY_SUBCATEGORY)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IEnumCATEGORY_SUBCATEGORY_Next_Proxy( 
    IEnumCATEGORY_SUBCATEGORY * This,
    /* [in] */ ULONG celt,
    /* [length_is][size_is][out] */ CATEGORY_SUBCATEGORY rgElements[  ],
    /* [out] */ ULONG *pulFetched);


void __RPC_STUB IEnumCATEGORY_SUBCATEGORY_Next_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumCATEGORY_SUBCATEGORY_Skip_Proxy( 
    IEnumCATEGORY_SUBCATEGORY * This,
    /* [in] */ ULONG ulElements);


void __RPC_STUB IEnumCATEGORY_SUBCATEGORY_Skip_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumCATEGORY_SUBCATEGORY_Reset_Proxy( 
    IEnumCATEGORY_SUBCATEGORY * This);


void __RPC_STUB IEnumCATEGORY_SUBCATEGORY_Reset_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumCATEGORY_SUBCATEGORY_Clone_Proxy( 
    IEnumCATEGORY_SUBCATEGORY * This,
    /* [out] */ IEnumCATEGORY_SUBCATEGORY **ppIEnumCATEGORY_SUBCATEGORY);


void __RPC_STUB IEnumCATEGORY_SUBCATEGORY_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumCATEGORY_SUBCATEGORY_INTERFACE_DEFINED__ */


#ifndef __IEnumCATEGORY_INSTANCE_INTERFACE_DEFINED__
#define __IEnumCATEGORY_INSTANCE_INTERFACE_DEFINED__

/* interface IEnumCATEGORY_INSTANCE */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IEnumCATEGORY_INSTANCE;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("8d8842d8-e031-4a7e-8571-dc0b03385807")
    IEnumCATEGORY_INSTANCE : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Next( 
            /* [in] */ ULONG ulElements,
            /* [length_is][size_is][out] */ CATEGORY_INSTANCE rgInstances[  ],
            /* [out] */ ULONG *pulFetched) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Skip( 
            /* [in] */ ULONG ulElements) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clone( 
            /* [out] */ IEnumCATEGORY_INSTANCE **ppIEnumCATEGORY_INSTANCE) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IEnumCATEGORY_INSTANCEVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IEnumCATEGORY_INSTANCE * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IEnumCATEGORY_INSTANCE * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IEnumCATEGORY_INSTANCE * This);
        
        HRESULT ( STDMETHODCALLTYPE *Next )( 
            IEnumCATEGORY_INSTANCE * This,
            /* [in] */ ULONG ulElements,
            /* [length_is][size_is][out] */ CATEGORY_INSTANCE rgInstances[  ],
            /* [out] */ ULONG *pulFetched);
        
        HRESULT ( STDMETHODCALLTYPE *Skip )( 
            IEnumCATEGORY_INSTANCE * This,
            /* [in] */ ULONG ulElements);
        
        HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IEnumCATEGORY_INSTANCE * This);
        
        HRESULT ( STDMETHODCALLTYPE *Clone )( 
            IEnumCATEGORY_INSTANCE * This,
            /* [out] */ IEnumCATEGORY_INSTANCE **ppIEnumCATEGORY_INSTANCE);
        
        END_INTERFACE
    } IEnumCATEGORY_INSTANCEVtbl;

    interface IEnumCATEGORY_INSTANCE
    {
        CONST_VTBL struct IEnumCATEGORY_INSTANCEVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IEnumCATEGORY_INSTANCE_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEnumCATEGORY_INSTANCE_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IEnumCATEGORY_INSTANCE_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IEnumCATEGORY_INSTANCE_Next(This,ulElements,rgInstances,pulFetched)	\
    (This)->lpVtbl -> Next(This,ulElements,rgInstances,pulFetched)

#define IEnumCATEGORY_INSTANCE_Skip(This,ulElements)	\
    (This)->lpVtbl -> Skip(This,ulElements)

#define IEnumCATEGORY_INSTANCE_Reset(This)	\
    (This)->lpVtbl -> Reset(This)

#define IEnumCATEGORY_INSTANCE_Clone(This,ppIEnumCATEGORY_INSTANCE)	\
    (This)->lpVtbl -> Clone(This,ppIEnumCATEGORY_INSTANCE)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IEnumCATEGORY_INSTANCE_Next_Proxy( 
    IEnumCATEGORY_INSTANCE * This,
    /* [in] */ ULONG ulElements,
    /* [length_is][size_is][out] */ CATEGORY_INSTANCE rgInstances[  ],
    /* [out] */ ULONG *pulFetched);


void __RPC_STUB IEnumCATEGORY_INSTANCE_Next_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumCATEGORY_INSTANCE_Skip_Proxy( 
    IEnumCATEGORY_INSTANCE * This,
    /* [in] */ ULONG ulElements);


void __RPC_STUB IEnumCATEGORY_INSTANCE_Skip_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumCATEGORY_INSTANCE_Reset_Proxy( 
    IEnumCATEGORY_INSTANCE * This);


void __RPC_STUB IEnumCATEGORY_INSTANCE_Reset_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IEnumCATEGORY_INSTANCE_Clone_Proxy( 
    IEnumCATEGORY_INSTANCE * This,
    /* [out] */ IEnumCATEGORY_INSTANCE **ppIEnumCATEGORY_INSTANCE);


void __RPC_STUB IEnumCATEGORY_INSTANCE_Clone_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IEnumCATEGORY_INSTANCE_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0343 */
/* [local] */ 

typedef /* [v1_enum] */ 
enum _STATE_AXIS
    {	STATE_AXIS_INVALID	= 0,
	STATE_AXIS_USER	= 1,
	STATE_AXIS_APPLICATION	= 2,
	STATE_AXIS_COMPONENT	= 3
    } 	STATE_AXIS;

typedef /* [v1_enum] */ enum _STATE_AXIS *PSTATE_AXIS;

typedef const STATE_AXIS *PCSTATE_AXIS;

typedef 
enum _STATE_COORDINATE_VALUE_USER
    {	STATE_COORDINATE_VALUE_USER_INVALID	= 0,
	STATE_COORDINATE_VALUE_USER_NEUTRAL	= 1,
	STATE_COORDINATE_VALUE_USER_LOCAL_MACHINE	= 2,
	STATE_COORDINATE_VALUE_USER_GLOBAL	= 3
    } 	STATE_COORDINATE_VALUE_USER;

typedef 
enum _STATE_COORDINATE_VALUE_APPLICATION
    {	STATE_COORDINATE_VALUE_APPLICATION_INVALID	= 0,
	STATE_COORDINATE_VALUE_APPLICATION_NEUTRAL	= 1,
	STATE_COORDINATE_VALUE_APPLICATION_VERSION_INDEPENDENT	= 2,
	STATE_COORDINATE_VALUE_APPLICATION_VERSION_FUNCTIONALITY	= 3,
	STATE_COORDINATE_VALUE_APPLICATION_VERSIONED	= 4
    } 	STATE_COORDINATE_VALUE_APPLICATION;

typedef enum _STATE_COORDINATE_VALUE_APPLICATION *PSTATE_COORDINATE_VALUE_APPLICATION;

typedef const STATE_COORDINATE_VALUE_APPLICATION *PCSTATE_COORDINATE_VALUE_APPLICATION;

typedef 
enum _STATE_COORDINATE_VALUE_COMPONENT
    {	STATE_COORDINATE_VALUE_COMPONENT_INVALID	= 0,
	STATE_COORDINATE_VALUE_COMPONENT_NEUTRAL	= 1,
	STATE_COORDINATE_VALUE_COMPONENT_VERSION_INDEPENDENT	= 2,
	STATE_COORDINATE_VALUE_COMPONENT_VERSION_FUNCTIONALITY	= 3,
	STATE_COORDINATE_VALUE_COMPONENT_VERSIONED	= 4
    } 	STATE_COORDINATE_VALUE_COMPONENT;

typedef enum _STATE_COORDINATE_VALUE_COMPONENT *PSTATE_COORDINATE_VALUE_COMPONENT;

typedef const STATE_COORDINATE_VALUE_COMPONENT *PCSTATE_COORDINATE_VALUE_COMPONENT;

typedef /* [switch_type] */ union _STATE_COORDINATE_VALUE
    {
    /* [case()] */ STATE_COORDINATE_VALUE_USER User;
    /* [case()] */ STATE_COORDINATE_VALUE_APPLICATION Application;
    /* [case()] */ STATE_COORDINATE_VALUE_COMPONENT Component;
    } 	STATE_COORDINATE_VALUE;

typedef /* [switch_type] */ union _STATE_COORDINATE_VALUE *PSTATE_COORDINATE_VALUE;

typedef const STATE_COORDINATE_VALUE *PCSTATE_COORDINATE_VALUE;

typedef struct _STATE_COORDINATE
    {
    STATE_AXIS Axis;
    STATE_COORDINATE_VALUE Value;
    } 	STATE_COORDINATE;

typedef struct _STATE_COORDINATE *PSTATE_COORDINATE;

typedef const STATE_COORDINATE *PCSTATE_COORDINATE;

typedef struct _STATE_COORDINATE_LIST
    {
    SIZE_T Count;
    PCSTATE_COORDINATE List;
    } 	STATE_COORDINATE_LIST;

typedef struct _STATE_COORDINATE_LIST *PSTATE_COORDINATE_LIST;

typedef const STATE_COORDINATE_LIST *PCSTATE_COORDINATE_LIST;



extern RPC_IF_HANDLE __MIDL_itf_isolation_0343_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0343_v0_0_s_ifspec;

#ifndef __IManifestInformation_INTERFACE_DEFINED__
#define __IManifestInformation_INTERFACE_DEFINED__

/* interface IManifestInformation */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IManifestInformation;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("81c85208-fe61-4c15-b5bb-ff5ea66baad9")
    IManifestInformation : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FullPath( 
            /* [retval][out] */ LPWSTR *ManifestPath) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IManifestInformationVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IManifestInformation * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IManifestInformation * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IManifestInformation * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FullPath )( 
            IManifestInformation * This,
            /* [retval][out] */ LPWSTR *ManifestPath);
        
        END_INTERFACE
    } IManifestInformationVtbl;

    interface IManifestInformation
    {
        CONST_VTBL struct IManifestInformationVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IManifestInformation_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IManifestInformation_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IManifestInformation_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IManifestInformation_get_FullPath(This,ManifestPath)	\
    (This)->lpVtbl -> get_FullPath(This,ManifestPath)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IManifestInformation_get_FullPath_Proxy( 
    IManifestInformation * This,
    /* [retval][out] */ LPWSTR *ManifestPath);


void __RPC_STUB IManifestInformation_get_FullPath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IManifestInformation_INTERFACE_DEFINED__ */


#ifndef __IActContext_INTERFACE_DEFINED__
#define __IActContext_INTERFACE_DEFINED__

/* interface IActContext */
/* [local][unique][uuid][object] */ 

/* [v1_enum] */ 
enum _IAPP_CONTEXT_REPLACE_STRING_MACROS_FLAGS
    {	IAPP_CONTEXT_REPLACE_STRING_MACROS_FLAG_NO_COMPONENT	= 0x1
    } ;
typedef struct _IAPP_CONTEXT_PREPARE_FOR_EXECUTION_INPUTS
    {
    ULONG ulSize;
    DWORD dwFlags;
    } 	IAPP_CONTEXT_PREPARE_FOR_EXECUTION_INPUTS;

typedef struct _IAPP_CONTEXT_PREPARE_FOR_EXECUTION_INPUTS *PIAPP_CONTEXT_PREPARE_FOR_EXECUTION_INPUTS;

typedef const IAPP_CONTEXT_PREPARE_FOR_EXECUTION_INPUTS *PCIAPP_CONTEXT_PREPARE_FOR_EXECUTION_INPUTS;

/* [v1_enum] */ 
enum _IAPP_CONTEXT_PREPARE_FOR_EXECUTION_OUTPUTS_FLAGS
    {	IAPP_CONTEXT_PREPARE_FOR_EXECUTION_OUTPUTS_FLAG_OVERALL_DISPOSITION_VALID	= 0x1
    } ;
typedef struct _IAPP_CONTEXT_PREPARE_FOR_EXECUTION_OUTPUTS
    {
    ULONG ulSize;
    DWORD dwFlags;
    DWORD dwOverallDisposition;
    } 	IAPP_CONTEXT_PREPARE_FOR_EXECUTION_OUTPUTS;

typedef struct _IAPP_CONTEXT_PREPARE_FOR_EXECUTION_OUTPUTS *PIAPP_CONTEXT_PREPARE_FOR_EXECUTION_OUTPUTS;

typedef const IAPP_CONTEXT_PREPARE_FOR_EXECUTION_OUTPUTS *PCIAPP_CONTEXT_PREPARE_FOR_EXECUTION_OUTPUTS;

/* [v1_enum] */ 
enum _IAPP_CONTEXT_SET_APPLICATION_RUNNING_STATES
    {	IAPP_CONTEXT_SET_APPLICATION_RUNNING_STATE_UNDEFINED	= 0,
	IAPP_CONTEXT_SET_APPLICATION_RUNNING_STATE_STARTING	= 1,
	IAPP_CONTEXT_SET_APPLICATION_RUNNING_STATE_RUNNING	= 2
    } ;
/* [v1_enum] */ 
enum _IAPP_CONTEXT_SET_APPLICATION_RUNNING_STATE_DISPOSITIONS
    {	IAPP_CONTEXT_SET_APPLICATION_RUNNING_STATE_DISPOSITION_UNDEFINED	= 0,
	IAPP_CONTEXT_SET_APPLICATION_RUNNING_STATE_DISPOSITION_STARTING	= 1,
	IAPP_CONTEXT_SET_APPLICATION_RUNNING_STATE_DISPOSITION_STARTING_MIGRATED	= 1 << 16,
	IAPP_CONTEXT_SET_APPLICATION_RUNNING_STATE_DISPOSITION_RUNNING	= 2,
	IAPP_CONTEXT_SET_APPLICATION_RUNNING_STATE_DISPOSITION_RUNNING_FIRST_RUN	= 1 << 17
    } ;
/* [v1_enum] */ 
enum IAPP_CONTEXT_GET_APPLICATION_STATE_FILESYSTEM_LOCATION_FLAGS
    {	IAPP_CONTEXT_GET_APPLICATION_STATE_FILESYSTEM_LOCATION_FLAG_NO_COMPONENT	= 0x1
    } ;
/* [v1_enum] */ 
enum _IAPP_CONTEXT_FIND_COMPONENTS_BY_DEFINITION_DISPOSITION_STATES
    {	IAPP_CONTEXT_FIND_COMPONENTS_BY_DEFINITION_DISPOSITION_STATE_UNDEFINED	= 0,
	IAPP_CONTEXT_FIND_COMPONENTS_BY_DEFINITION_DISPOSITION_STATE_NOT_LOOKED_AT	= 1,
	IAPP_CONTEXT_FIND_COMPONENTS_BY_DEFINITION_DISPOSITION_STATE_FOUND	= 2,
	IAPP_CONTEXT_FIND_COMPONENTS_BY_DEFINITION_DISPOSITION_STATE_NOT_FOUND	= 3
    } ;
/* [v1_enum] */ 
enum _IAPP_CONTEXT_FIND_COMPONENTS_BY_REFERENCE_DISPOSITION_STATES
    {	IAPP_CONTEXT_FIND_COMPONENTS_BY_REFERENCE_DISPOSITION_STATE_UNDEFINED	= 0,
	IAPP_CONTEXT_FIND_COMPONENTS_BY_REFERENCE_DISPOSITION_STATE_NOT_LOOKED_AT	= 1,
	IAPP_CONTEXT_FIND_COMPONENTS_BY_REFERENCE_DISPOSITION_STATE_FOUND	= 2,
	IAPP_CONTEXT_FIND_COMPONENTS_BY_REFERENCE_DISPOSITION_STATE_NOT_FOUND	= 3
    } ;
/* [v1_enum] */ 
enum _IAPP_CONTEXT_FIND_COMPONENTS_BY_REFERENCE_FLAGS
    {	IAPP_CONTEXT_FIND_COMPONENTS_BY_REFERENCE_FLAG_REQUIRE_EXACT_MATCH	= 0x1
    } ;

EXTERN_C const IID IID_IActContext;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0af57545-a72a-4fbe-813c-8554ed7d4528")
    IActContext : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AppId( 
            /* [retval][out] */ IDefinitionAppId **ppAppId) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumCategories( 
            /* [in] */ DWORD dwFlags,
            /* [unique][in] */ IReferenceIdentity *pReferenceIdentity_ToMatch,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppIUnknown) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumSubcategories( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [in] */ LPCWSTR pszSubcategoryPathPattern,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppIUnknown) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumCategoryInstances( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity_Category,
            /* [in] */ LPCWSTR pszSubcategoryPath,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppIUnknown) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReplaceMacrosInStrings( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ SIZE_T iComponentIndex,
            /* [in] */ SIZE_T cStrings,
            /* [size_is][in] */ const LPCWSTR rgpszSourceStrings[  ],
            /* [size_is][out] */ LPWSTR rgpszDestinationStrings[  ],
            /* [in] */ PCCULTURE_FALLBACK_LIST pCultureFallbackList) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetComponentStringTableStrings( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ SIZE_T iComponentIndex,
            /* [in] */ SIZE_T cStrings,
            /* [size_is][in] */ const LPCWSTR rgpszSourceStrings[  ],
            /* [size_is][out] */ LPWSTR rgpszDestinationStrings[  ],
            /* [in] */ PCCULTURE_FALLBACK_LIST pCultureFallbackList) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetApplicationProperties( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ SIZE_T cProperties,
            /* [size_is][in] */ const LPCWSTR rgpszPropertyNames[  ],
            /* [size_is][out] */ LPWSTR rgpszPropertyValues[  ],
            /* [size_is][out] */ SIZE_T rgiComponentIndices[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ApplicationBasePath( 
            /* [in] */ DWORD dwFlags,
            /* [retval][out] */ LPWSTR *ApplicationPath) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetComponentManifest( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *Component,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppIUnknown) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetComponentPayloadPath( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *Component,
            /* [retval][out] */ LPWSTR *ppwszComponentPayloadPath) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FindReferenceInContext( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *Reference,
            /* [retval][out] */ IDefinitionIdentity **MatchedDefinition) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateActContextFromCategoryInstance( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ PCCATEGORY_INSTANCE CategoryInstance,
            /* [retval][out] */ IActContext **ppCreatedAppContext) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EnumComponents( 
            /* [in] */ DWORD dwFlags,
            /* [retval][out] */ IEnumDefinitionIdentity **ppIdentityEnum) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PrepareForExecution( 
            /* [in] */ PCIAPP_CONTEXT_PREPARE_FOR_EXECUTION_INPUTS pInputs,
            /* [out][in] */ PIAPP_CONTEXT_PREPARE_FOR_EXECUTION_OUTPUTS pOutputs) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetApplicationRunningState( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ ULONG ulState,
            /* [retval][out] */ ULONG *Disposition) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetApplicationStateFilesystemLocation( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ SIZE_T iComponentIndex,
            /* [in] */ PCSTATE_COORDINATE_LIST pCoordinateList,
            /* [out] */ LPWSTR *ppszPath) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FindComponentsByDefinition( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ SIZE_T cComponents,
            /* [size_is][in] */ IDefinitionIdentity *pIDefinitionIdentities[  ],
            /* [size_is][out] */ SIZE_T rgiComponentIndices[  ],
            /* [size_is][out] */ ULONG rgulDispositions[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FindComponentsByReference( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ SIZE_T cComponents,
            /* [size_is][in] */ IReferenceIdentity *pIReferenceIdentities[  ],
            /* [size_is][out] */ SIZE_T rgComponentIndices[  ],
            /* [size_is][out] */ ULONG rgulDispositions[  ]) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IActContextVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IActContext * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IActContext * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IActContext * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AppId )( 
            IActContext * This,
            /* [retval][out] */ IDefinitionAppId **ppAppId);
        
        HRESULT ( STDMETHODCALLTYPE *EnumCategories )( 
            IActContext * This,
            /* [in] */ DWORD dwFlags,
            /* [unique][in] */ IReferenceIdentity *pReferenceIdentity_ToMatch,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppIUnknown);
        
        HRESULT ( STDMETHODCALLTYPE *EnumSubcategories )( 
            IActContext * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
            /* [in] */ LPCWSTR pszSubcategoryPathPattern,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppIUnknown);
        
        HRESULT ( STDMETHODCALLTYPE *EnumCategoryInstances )( 
            IActContext * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity_Category,
            /* [in] */ LPCWSTR pszSubcategoryPath,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppIUnknown);
        
        HRESULT ( STDMETHODCALLTYPE *ReplaceMacrosInStrings )( 
            IActContext * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ SIZE_T iComponentIndex,
            /* [in] */ SIZE_T cStrings,
            /* [size_is][in] */ const LPCWSTR rgpszSourceStrings[  ],
            /* [size_is][out] */ LPWSTR rgpszDestinationStrings[  ],
            /* [in] */ PCCULTURE_FALLBACK_LIST pCultureFallbackList);
        
        HRESULT ( STDMETHODCALLTYPE *GetComponentStringTableStrings )( 
            IActContext * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ SIZE_T iComponentIndex,
            /* [in] */ SIZE_T cStrings,
            /* [size_is][in] */ const LPCWSTR rgpszSourceStrings[  ],
            /* [size_is][out] */ LPWSTR rgpszDestinationStrings[  ],
            /* [in] */ PCCULTURE_FALLBACK_LIST pCultureFallbackList);
        
        HRESULT ( STDMETHODCALLTYPE *GetApplicationProperties )( 
            IActContext * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ SIZE_T cProperties,
            /* [size_is][in] */ const LPCWSTR rgpszPropertyNames[  ],
            /* [size_is][out] */ LPWSTR rgpszPropertyValues[  ],
            /* [size_is][out] */ SIZE_T rgiComponentIndices[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *ApplicationBasePath )( 
            IActContext * This,
            /* [in] */ DWORD dwFlags,
            /* [retval][out] */ LPWSTR *ApplicationPath);
        
        HRESULT ( STDMETHODCALLTYPE *GetComponentManifest )( 
            IActContext * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *Component,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ IUnknown **ppIUnknown);
        
        HRESULT ( STDMETHODCALLTYPE *GetComponentPayloadPath )( 
            IActContext * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionIdentity *Component,
            /* [retval][out] */ LPWSTR *ppwszComponentPayloadPath);
        
        HRESULT ( STDMETHODCALLTYPE *FindReferenceInContext )( 
            IActContext * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IReferenceIdentity *Reference,
            /* [retval][out] */ IDefinitionIdentity **MatchedDefinition);
        
        HRESULT ( STDMETHODCALLTYPE *CreateActContextFromCategoryInstance )( 
            IActContext * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ PCCATEGORY_INSTANCE CategoryInstance,
            /* [retval][out] */ IActContext **ppCreatedAppContext);
        
        HRESULT ( STDMETHODCALLTYPE *EnumComponents )( 
            IActContext * This,
            /* [in] */ DWORD dwFlags,
            /* [retval][out] */ IEnumDefinitionIdentity **ppIdentityEnum);
        
        HRESULT ( STDMETHODCALLTYPE *PrepareForExecution )( 
            IActContext * This,
            /* [in] */ PCIAPP_CONTEXT_PREPARE_FOR_EXECUTION_INPUTS pInputs,
            /* [out][in] */ PIAPP_CONTEXT_PREPARE_FOR_EXECUTION_OUTPUTS pOutputs);
        
        HRESULT ( STDMETHODCALLTYPE *SetApplicationRunningState )( 
            IActContext * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ ULONG ulState,
            /* [retval][out] */ ULONG *Disposition);
        
        HRESULT ( STDMETHODCALLTYPE *GetApplicationStateFilesystemLocation )( 
            IActContext * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ SIZE_T iComponentIndex,
            /* [in] */ PCSTATE_COORDINATE_LIST pCoordinateList,
            /* [out] */ LPWSTR *ppszPath);
        
        HRESULT ( STDMETHODCALLTYPE *FindComponentsByDefinition )( 
            IActContext * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ SIZE_T cComponents,
            /* [size_is][in] */ IDefinitionIdentity *pIDefinitionIdentities[  ],
            /* [size_is][out] */ SIZE_T rgiComponentIndices[  ],
            /* [size_is][out] */ ULONG rgulDispositions[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *FindComponentsByReference )( 
            IActContext * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ SIZE_T cComponents,
            /* [size_is][in] */ IReferenceIdentity *pIReferenceIdentities[  ],
            /* [size_is][out] */ SIZE_T rgComponentIndices[  ],
            /* [size_is][out] */ ULONG rgulDispositions[  ]);
        
        END_INTERFACE
    } IActContextVtbl;

    interface IActContext
    {
        CONST_VTBL struct IActContextVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IActContext_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IActContext_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IActContext_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IActContext_get_AppId(This,ppAppId)	\
    (This)->lpVtbl -> get_AppId(This,ppAppId)

#define IActContext_EnumCategories(This,dwFlags,pReferenceIdentity_ToMatch,riid,ppIUnknown)	\
    (This)->lpVtbl -> EnumCategories(This,dwFlags,pReferenceIdentity_ToMatch,riid,ppIUnknown)

#define IActContext_EnumSubcategories(This,dwFlags,pIDefinitionIdentity,pszSubcategoryPathPattern,riid,ppIUnknown)	\
    (This)->lpVtbl -> EnumSubcategories(This,dwFlags,pIDefinitionIdentity,pszSubcategoryPathPattern,riid,ppIUnknown)

#define IActContext_EnumCategoryInstances(This,dwFlags,pIDefinitionIdentity_Category,pszSubcategoryPath,riid,ppIUnknown)	\
    (This)->lpVtbl -> EnumCategoryInstances(This,dwFlags,pIDefinitionIdentity_Category,pszSubcategoryPath,riid,ppIUnknown)

#define IActContext_ReplaceMacrosInStrings(This,dwFlags,iComponentIndex,cStrings,rgpszSourceStrings,rgpszDestinationStrings,pCultureFallbackList)	\
    (This)->lpVtbl -> ReplaceMacrosInStrings(This,dwFlags,iComponentIndex,cStrings,rgpszSourceStrings,rgpszDestinationStrings,pCultureFallbackList)

#define IActContext_GetComponentStringTableStrings(This,dwFlags,iComponentIndex,cStrings,rgpszSourceStrings,rgpszDestinationStrings,pCultureFallbackList)	\
    (This)->lpVtbl -> GetComponentStringTableStrings(This,dwFlags,iComponentIndex,cStrings,rgpszSourceStrings,rgpszDestinationStrings,pCultureFallbackList)

#define IActContext_GetApplicationProperties(This,dwFlags,cProperties,rgpszPropertyNames,rgpszPropertyValues,rgiComponentIndices)	\
    (This)->lpVtbl -> GetApplicationProperties(This,dwFlags,cProperties,rgpszPropertyNames,rgpszPropertyValues,rgiComponentIndices)

#define IActContext_ApplicationBasePath(This,dwFlags,ApplicationPath)	\
    (This)->lpVtbl -> ApplicationBasePath(This,dwFlags,ApplicationPath)

#define IActContext_GetComponentManifest(This,dwFlags,Component,riid,ppIUnknown)	\
    (This)->lpVtbl -> GetComponentManifest(This,dwFlags,Component,riid,ppIUnknown)

#define IActContext_GetComponentPayloadPath(This,dwFlags,Component,ppwszComponentPayloadPath)	\
    (This)->lpVtbl -> GetComponentPayloadPath(This,dwFlags,Component,ppwszComponentPayloadPath)

#define IActContext_FindReferenceInContext(This,dwFlags,Reference,MatchedDefinition)	\
    (This)->lpVtbl -> FindReferenceInContext(This,dwFlags,Reference,MatchedDefinition)

#define IActContext_CreateActContextFromCategoryInstance(This,dwFlags,CategoryInstance,ppCreatedAppContext)	\
    (This)->lpVtbl -> CreateActContextFromCategoryInstance(This,dwFlags,CategoryInstance,ppCreatedAppContext)

#define IActContext_EnumComponents(This,dwFlags,ppIdentityEnum)	\
    (This)->lpVtbl -> EnumComponents(This,dwFlags,ppIdentityEnum)

#define IActContext_PrepareForExecution(This,pInputs,pOutputs)	\
    (This)->lpVtbl -> PrepareForExecution(This,pInputs,pOutputs)

#define IActContext_SetApplicationRunningState(This,dwFlags,ulState,Disposition)	\
    (This)->lpVtbl -> SetApplicationRunningState(This,dwFlags,ulState,Disposition)

#define IActContext_GetApplicationStateFilesystemLocation(This,dwFlags,iComponentIndex,pCoordinateList,ppszPath)	\
    (This)->lpVtbl -> GetApplicationStateFilesystemLocation(This,dwFlags,iComponentIndex,pCoordinateList,ppszPath)

#define IActContext_FindComponentsByDefinition(This,dwFlags,cComponents,pIDefinitionIdentities,rgiComponentIndices,rgulDispositions)	\
    (This)->lpVtbl -> FindComponentsByDefinition(This,dwFlags,cComponents,pIDefinitionIdentities,rgiComponentIndices,rgulDispositions)

#define IActContext_FindComponentsByReference(This,dwFlags,cComponents,pIReferenceIdentities,rgComponentIndices,rgulDispositions)	\
    (This)->lpVtbl -> FindComponentsByReference(This,dwFlags,cComponents,pIReferenceIdentities,rgComponentIndices,rgulDispositions)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IActContext_get_AppId_Proxy( 
    IActContext * This,
    /* [retval][out] */ IDefinitionAppId **ppAppId);


void __RPC_STUB IActContext_get_AppId_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActContext_EnumCategories_Proxy( 
    IActContext * This,
    /* [in] */ DWORD dwFlags,
    /* [unique][in] */ IReferenceIdentity *pReferenceIdentity_ToMatch,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppIUnknown);


void __RPC_STUB IActContext_EnumCategories_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActContext_EnumSubcategories_Proxy( 
    IActContext * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pIDefinitionIdentity,
    /* [in] */ LPCWSTR pszSubcategoryPathPattern,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppIUnknown);


void __RPC_STUB IActContext_EnumSubcategories_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActContext_EnumCategoryInstances_Proxy( 
    IActContext * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *pIDefinitionIdentity_Category,
    /* [in] */ LPCWSTR pszSubcategoryPath,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppIUnknown);


void __RPC_STUB IActContext_EnumCategoryInstances_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActContext_ReplaceMacrosInStrings_Proxy( 
    IActContext * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ SIZE_T iComponentIndex,
    /* [in] */ SIZE_T cStrings,
    /* [size_is][in] */ const LPCWSTR rgpszSourceStrings[  ],
    /* [size_is][out] */ LPWSTR rgpszDestinationStrings[  ],
    /* [in] */ PCCULTURE_FALLBACK_LIST pCultureFallbackList);


void __RPC_STUB IActContext_ReplaceMacrosInStrings_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActContext_GetComponentStringTableStrings_Proxy( 
    IActContext * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ SIZE_T iComponentIndex,
    /* [in] */ SIZE_T cStrings,
    /* [size_is][in] */ const LPCWSTR rgpszSourceStrings[  ],
    /* [size_is][out] */ LPWSTR rgpszDestinationStrings[  ],
    /* [in] */ PCCULTURE_FALLBACK_LIST pCultureFallbackList);


void __RPC_STUB IActContext_GetComponentStringTableStrings_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActContext_GetApplicationProperties_Proxy( 
    IActContext * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ SIZE_T cProperties,
    /* [size_is][in] */ const LPCWSTR rgpszPropertyNames[  ],
    /* [size_is][out] */ LPWSTR rgpszPropertyValues[  ],
    /* [size_is][out] */ SIZE_T rgiComponentIndices[  ]);


void __RPC_STUB IActContext_GetApplicationProperties_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActContext_ApplicationBasePath_Proxy( 
    IActContext * This,
    /* [in] */ DWORD dwFlags,
    /* [retval][out] */ LPWSTR *ApplicationPath);


void __RPC_STUB IActContext_ApplicationBasePath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActContext_GetComponentManifest_Proxy( 
    IActContext * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *Component,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppIUnknown);


void __RPC_STUB IActContext_GetComponentManifest_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActContext_GetComponentPayloadPath_Proxy( 
    IActContext * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionIdentity *Component,
    /* [retval][out] */ LPWSTR *ppwszComponentPayloadPath);


void __RPC_STUB IActContext_GetComponentPayloadPath_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActContext_FindReferenceInContext_Proxy( 
    IActContext * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IReferenceIdentity *Reference,
    /* [retval][out] */ IDefinitionIdentity **MatchedDefinition);


void __RPC_STUB IActContext_FindReferenceInContext_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActContext_CreateActContextFromCategoryInstance_Proxy( 
    IActContext * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ PCCATEGORY_INSTANCE CategoryInstance,
    /* [retval][out] */ IActContext **ppCreatedAppContext);


void __RPC_STUB IActContext_CreateActContextFromCategoryInstance_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActContext_EnumComponents_Proxy( 
    IActContext * This,
    /* [in] */ DWORD dwFlags,
    /* [retval][out] */ IEnumDefinitionIdentity **ppIdentityEnum);


void __RPC_STUB IActContext_EnumComponents_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActContext_PrepareForExecution_Proxy( 
    IActContext * This,
    /* [in] */ PCIAPP_CONTEXT_PREPARE_FOR_EXECUTION_INPUTS pInputs,
    /* [out][in] */ PIAPP_CONTEXT_PREPARE_FOR_EXECUTION_OUTPUTS pOutputs);


void __RPC_STUB IActContext_PrepareForExecution_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActContext_SetApplicationRunningState_Proxy( 
    IActContext * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ ULONG ulState,
    /* [retval][out] */ ULONG *Disposition);


void __RPC_STUB IActContext_SetApplicationRunningState_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActContext_GetApplicationStateFilesystemLocation_Proxy( 
    IActContext * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ SIZE_T iComponentIndex,
    /* [in] */ PCSTATE_COORDINATE_LIST pCoordinateList,
    /* [out] */ LPWSTR *ppszPath);


void __RPC_STUB IActContext_GetApplicationStateFilesystemLocation_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActContext_FindComponentsByDefinition_Proxy( 
    IActContext * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ SIZE_T cComponents,
    /* [size_is][in] */ IDefinitionIdentity *pIDefinitionIdentities[  ],
    /* [size_is][out] */ SIZE_T rgiComponentIndices[  ],
    /* [size_is][out] */ ULONG rgulDispositions[  ]);


void __RPC_STUB IActContext_FindComponentsByDefinition_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IActContext_FindComponentsByReference_Proxy( 
    IActContext * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ SIZE_T cComponents,
    /* [size_is][in] */ IReferenceIdentity *pIReferenceIdentities[  ],
    /* [size_is][out] */ SIZE_T rgComponentIndices[  ],
    /* [size_is][out] */ ULONG rgulDispositions[  ]);


void __RPC_STUB IActContext_FindComponentsByReference_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IActContext_INTERFACE_DEFINED__ */


#ifndef __IStateManager_INTERFACE_DEFINED__
#define __IStateManager_INTERFACE_DEFINED__

/* interface IStateManager */
/* [local][unique][uuid][object] */ 

/* [v1_enum] */ 
enum _ISTATE_MANAGER_PREPARE_APPLICATION_STATE_INPUTS_FLAGS
    {	ISTATE_MANAGER_PREPARE_APPLICATION_STATE_INPUTS_FLAG_APPLICATION_TO_PREPARE_VALID	= 0x1
    } ;
typedef struct _ISTATE_MANAGER_PREPARE_APPLICATION_STATE_INPUTS
    {
    ULONG ulSize;
    DWORD dwFlags;
    IActContext *pApplicationToPrepare;
    } 	ISTATE_MANAGER_PREPARE_APPLICATION_STATE_INPUTS;

typedef struct _ISTATE_MANAGER_PREPARE_APPLICATION_STATE_INPUTS *PISTATE_MANAGER_PREPARE_APPLICATION_STATE_INPUTS;

typedef const ISTATE_MANAGER_PREPARE_APPLICATION_STATE_INPUTS *PCISTATE_MANAGER_PREPARE_APPLICATION_STATE_INPUTS;

/* [v1_enum] */ 
enum _ISTATE_MANAGER_PREPARE_APPLICATION_STATE_OUTPUTS_FLAGS
    {	ISTATE_MANAGER_PREPARE_APPLICATION_STATE_OUTPUTS_FLAG_OVERALL_DISPOSITION_VALID	= 0x1
    } ;
typedef struct _ISTATE_MANAGER_PREPARE_APPLICATION_STATE_OUTPUTS
    {
    ULONG ulSize;
    DWORD dwFlags;
    DWORD dwOverallDisposition;
    } 	ISTATE_MANAGER_PREPARE_APPLICATION_STATE_OUTPUTS;

typedef struct _ISTATE_MANAGER_PREPARE_APPLICATION_STATE_OUTPUTS *PISTATE_MANAGER_PREPARE_APPLICATION_STATE_OUTPUTS;

typedef const ISTATE_MANAGER_PREPARE_APPLICATION_STATE_OUTPUTS *PCISTATE_MANAGER_PREPARE_APPLICATION_STATE_OUTPUTS;

/* [v1_enum] */ 
enum _ISTATE_MANAGER_SET_APPLICATION_RUNNING_STATES
    {	ISTATE_MANAGER_SET_APPLICATION_RUNNING_STATE_UNDEFINED	= 0,
	ISTATE_MANAGER_SET_APPLICATION_RUNNING_STATE_STARTING	= 1,
	ISTATE_MANAGER_SET_APPLICATION_RUNNING_STATE_RUNNING	= 2
    } ;
/* [v1_enum] */ 
enum _ISTATE_MANAGER_SET_APPLICATION_RUNNING_STATE_DISPOSITIONS
    {	ISTATE_MANAGER_SET_APPLICATION_RUNNING_STATE_DISPOSITION_UNDEFINED	= 0,
	ISTATE_MANAGER_SET_APPLICATION_RUNNING_STATE_DISPOSITION_STARTING	= 1,
	ISTATE_MANAGER_SET_APPLICATION_RUNNING_STATE_DISPOSITION_STARTING_MIGRATED	= 1 << 16,
	ISTATE_MANAGER_SET_APPLICATION_RUNNING_STATE_DISPOSITION_RUNNING	= 2,
	ISTATE_MANAGER_SET_APPLICATION_RUNNING_STATE_DISPOSITION_RUNNING_FIRST_RUN	= 1 << 17
    } ;

EXTERN_C const IID IID_IStateManager;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("07662534-750b-4ed5-9cfb-1c5bc5acfd07")
    IStateManager : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE PrepareApplicationState( 
            /* [in] */ PCISTATE_MANAGER_PREPARE_APPLICATION_STATE_INPUTS pInputs,
            /* [out][in] */ PISTATE_MANAGER_PREPARE_APPLICATION_STATE_OUTPUTS pOutputs) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetApplicationRunningState( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IActContext *pIActContext,
            /* [in] */ ULONG ulState,
            /* [in] */ ULONG *pulDisposition) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetApplicationStateFilesystemLocation( 
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIDefinitionAppId_Application,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity_Component,
            /* [in] */ PCSTATE_COORDINATE_LIST pCoordinateList,
            /* [out] */ LPWSTR *ppszPath) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Scavenge( 
            /* [in] */ DWORD dwFlags,
            /* [out] */ DWORD *pdwDisposition) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IStateManagerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IStateManager * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IStateManager * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IStateManager * This);
        
        HRESULT ( STDMETHODCALLTYPE *PrepareApplicationState )( 
            IStateManager * This,
            /* [in] */ PCISTATE_MANAGER_PREPARE_APPLICATION_STATE_INPUTS pInputs,
            /* [out][in] */ PISTATE_MANAGER_PREPARE_APPLICATION_STATE_OUTPUTS pOutputs);
        
        HRESULT ( STDMETHODCALLTYPE *SetApplicationRunningState )( 
            IStateManager * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IActContext *pIActContext,
            /* [in] */ ULONG ulState,
            /* [in] */ ULONG *pulDisposition);
        
        HRESULT ( STDMETHODCALLTYPE *GetApplicationStateFilesystemLocation )( 
            IStateManager * This,
            /* [in] */ DWORD dwFlags,
            /* [in] */ IDefinitionAppId *pIDefinitionAppId_Application,
            /* [in] */ IDefinitionIdentity *pIDefinitionIdentity_Component,
            /* [in] */ PCSTATE_COORDINATE_LIST pCoordinateList,
            /* [out] */ LPWSTR *ppszPath);
        
        HRESULT ( STDMETHODCALLTYPE *Scavenge )( 
            IStateManager * This,
            /* [in] */ DWORD dwFlags,
            /* [out] */ DWORD *pdwDisposition);
        
        END_INTERFACE
    } IStateManagerVtbl;

    interface IStateManager
    {
        CONST_VTBL struct IStateManagerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IStateManager_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IStateManager_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IStateManager_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IStateManager_PrepareApplicationState(This,pInputs,pOutputs)	\
    (This)->lpVtbl -> PrepareApplicationState(This,pInputs,pOutputs)

#define IStateManager_SetApplicationRunningState(This,dwFlags,pIActContext,ulState,pulDisposition)	\
    (This)->lpVtbl -> SetApplicationRunningState(This,dwFlags,pIActContext,ulState,pulDisposition)

#define IStateManager_GetApplicationStateFilesystemLocation(This,dwFlags,pIDefinitionAppId_Application,pIDefinitionIdentity_Component,pCoordinateList,ppszPath)	\
    (This)->lpVtbl -> GetApplicationStateFilesystemLocation(This,dwFlags,pIDefinitionAppId_Application,pIDefinitionIdentity_Component,pCoordinateList,ppszPath)

#define IStateManager_Scavenge(This,dwFlags,pdwDisposition)	\
    (This)->lpVtbl -> Scavenge(This,dwFlags,pdwDisposition)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IStateManager_PrepareApplicationState_Proxy( 
    IStateManager * This,
    /* [in] */ PCISTATE_MANAGER_PREPARE_APPLICATION_STATE_INPUTS pInputs,
    /* [out][in] */ PISTATE_MANAGER_PREPARE_APPLICATION_STATE_OUTPUTS pOutputs);


void __RPC_STUB IStateManager_PrepareApplicationState_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStateManager_SetApplicationRunningState_Proxy( 
    IStateManager * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IActContext *pIActContext,
    /* [in] */ ULONG ulState,
    /* [in] */ ULONG *pulDisposition);


void __RPC_STUB IStateManager_SetApplicationRunningState_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStateManager_GetApplicationStateFilesystemLocation_Proxy( 
    IStateManager * This,
    /* [in] */ DWORD dwFlags,
    /* [in] */ IDefinitionAppId *pIDefinitionAppId_Application,
    /* [in] */ IDefinitionIdentity *pIDefinitionIdentity_Component,
    /* [in] */ PCSTATE_COORDINATE_LIST pCoordinateList,
    /* [out] */ LPWSTR *ppszPath);


void __RPC_STUB IStateManager_GetApplicationStateFilesystemLocation_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IStateManager_Scavenge_Proxy( 
    IStateManager * This,
    /* [in] */ DWORD dwFlags,
    /* [out] */ DWORD *pdwDisposition);


void __RPC_STUB IStateManager_Scavenge_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IStateManager_INTERFACE_DEFINED__ */


#ifndef __IManifestParseErrorCallback_INTERFACE_DEFINED__
#define __IManifestParseErrorCallback_INTERFACE_DEFINED__

/* interface IManifestParseErrorCallback */
/* [local][unique][uuid][object] */ 


EXTERN_C const IID IID_IManifestParseErrorCallback;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("ace1b703-1aac-4956-ab87-90cac8b93ce6")
    IManifestParseErrorCallback : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE OnError( 
            /* [in] */ ULONG nStartLine,
            /* [in] */ ULONG nStartColumn,
            /* [in] */ ULONG cCharacterCount,
            /* [in] */ HRESULT hr,
            /* [in] */ LPCWSTR pszErrorStatusHostFile,
            /* [in] */ ULONG cParameterCount,
            /* [size_is][in] */ LPCWSTR *prgpszParameters) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IManifestParseErrorCallbackVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IManifestParseErrorCallback * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IManifestParseErrorCallback * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IManifestParseErrorCallback * This);
        
        HRESULT ( STDMETHODCALLTYPE *OnError )( 
            IManifestParseErrorCallback * This,
            /* [in] */ ULONG nStartLine,
            /* [in] */ ULONG nStartColumn,
            /* [in] */ ULONG cCharacterCount,
            /* [in] */ HRESULT hr,
            /* [in] */ LPCWSTR pszErrorStatusHostFile,
            /* [in] */ ULONG cParameterCount,
            /* [size_is][in] */ LPCWSTR *prgpszParameters);
        
        END_INTERFACE
    } IManifestParseErrorCallbackVtbl;

    interface IManifestParseErrorCallback
    {
        CONST_VTBL struct IManifestParseErrorCallbackVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IManifestParseErrorCallback_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IManifestParseErrorCallback_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IManifestParseErrorCallback_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IManifestParseErrorCallback_OnError(This,nStartLine,nStartColumn,cCharacterCount,hr,pszErrorStatusHostFile,cParameterCount,prgpszParameters)	\
    (This)->lpVtbl -> OnError(This,nStartLine,nStartColumn,cCharacterCount,hr,pszErrorStatusHostFile,cParameterCount,prgpszParameters)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IManifestParseErrorCallback_OnError_Proxy( 
    IManifestParseErrorCallback * This,
    /* [in] */ ULONG nStartLine,
    /* [in] */ ULONG nStartColumn,
    /* [in] */ ULONG cCharacterCount,
    /* [in] */ HRESULT hr,
    /* [in] */ LPCWSTR pszErrorStatusHostFile,
    /* [in] */ ULONG cParameterCount,
    /* [size_is][in] */ LPCWSTR *prgpszParameters);


void __RPC_STUB IManifestParseErrorCallback_OnError_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IManifestParseErrorCallback_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_isolation_0347 */
/* [local] */ 

/* [local] */ HRESULT __stdcall GetAppIdAuthority( 
    /* [out] */ IAppIdAuthority **ppIAppIdAuthority);

/* [local] */ HRESULT __stdcall GetIdentityAuthority( 
    /* [out] */ IIdentityAuthority **ppIIdentityAuthority);

/* [local] */ HRESULT __stdcall SetIsolationIMalloc( 
    /* [in] */ IMalloc *pIMalloc);

/* [local] */ HRESULT __stdcall GetSystemStore( 
    /* [in] */ DWORD dwFlags,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppIStore);

/* [local] */ HRESULT __stdcall GetUserStore( 
    /* [in] */ DWORD dwFlags,
    /* [in] */ HANDLE hToken,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppIStore);

/* [local] */ HRESULT __stdcall GetUserStateManager( 
    /* [in] */ DWORD Flags,
    /* [in] */ HANDLE hToken,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppManager);

/* [local] */ HRESULT __stdcall ParseManifest( 
    /* [in] */ LPCWSTR pszManifestPath,
    /* [unique][in] */ IManifestParseErrorCallback *pIManifestParseErrorCallback,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppManifest);

/* [local] */ HRESULT __stdcall CreateCMSFromXml( 
    /* [in] */ void *Data,
    /* [in] */ DWORD DataSize,
    /* [unique][in] */ IManifestParseErrorCallback *pIManifestParseErrorCallback,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ IUnknown **ppManifest);

/* [local] */ HRESULT __stdcall GetCurrentActContext( 
    /* [out] */ IActContext **ppIActContext);

/* [local] */ HRESULT __stdcall CreateActContext( 
    /* [in] */ PCCREATE_APP_CONTEXT_DATA Data,
    /* [out] */ IActContext **ppIActContext);



extern RPC_IF_HANDLE __MIDL_itf_isolation_0347_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_isolation_0347_v0_0_s_ifspec;


#ifndef __Isolation_LIBRARY_DEFINED__
#define __Isolation_LIBRARY_DEFINED__

/* library Isolation */
/* [version][helpstring][uuid] */ 


EXTERN_C const IID LIBID_Isolation;
#endif /* __Isolation_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

unsigned long             __RPC_USER  BSTR_UserSize(     unsigned long *, unsigned long            , BSTR * ); 
unsigned char * __RPC_USER  BSTR_UserMarshal(  unsigned long *, unsigned char *, BSTR * ); 
unsigned char * __RPC_USER  BSTR_UserUnmarshal(unsigned long *, unsigned char *, BSTR * ); 
void                      __RPC_USER  BSTR_UserFree(     unsigned long *, BSTR * ); 

unsigned long             __RPC_USER  LPSAFEARRAY_UserSize(     unsigned long *, unsigned long            , LPSAFEARRAY * ); 
unsigned char * __RPC_USER  LPSAFEARRAY_UserMarshal(  unsigned long *, unsigned char *, LPSAFEARRAY * ); 
unsigned char * __RPC_USER  LPSAFEARRAY_UserUnmarshal(unsigned long *, unsigned char *, LPSAFEARRAY * ); 
void                      __RPC_USER  LPSAFEARRAY_UserFree(     unsigned long *, LPSAFEARRAY * ); 

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


