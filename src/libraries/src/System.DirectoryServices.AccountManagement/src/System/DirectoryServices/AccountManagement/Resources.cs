// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Resources;
using System.Reflection;
using System.Globalization;

// disable csharp compiler warning #0414: field assigned unused value
#pragma warning disable 0414

namespace System.DirectoryServices.AccountManagement
{
    ///<summary>
    /// This class loads all the localizable string resources.
    ///</summary>
    internal class StringResources
    {
        static StringResources()
        {
            Type t = typeof(StringResources);

            ResourceManager rm = new ResourceManager(t);
            MemberInfo[] infos = t.GetMembers(BindingFlags.Public | BindingFlags.Static);
            foreach (MemberInfo m in infos)
            {
                t.InvokeMember(m.Name, BindingFlags.SetField, null, null,
                               new Object[] { rm.GetString(m.Name, CultureInfo.CurrentUICulture) },
                               CultureInfo.CurrentCulture);
            }
        }

        public static string ContextNoWellKnownObjects = null;
        public static string ContextNoContainerForMachineCtx = null;
        public static string ContextBadUserPwdCombo = null;

        public static string StoreNotSupportMethod = null;

        public static string PrincipalCantSetContext = null;
        public static string PrincipalUnsupportedProperty = null;
        public static string PrincipalUnsupportPropertyForPlatform = null;
        public static string PrincipalUnsupportPropertyForType = null;
        public static string PrincipalMustSetContextForSave = null;
        public static string PrincipalMustSetContextForNative = null;
        public static string PrincipalMustSetContextForProperty = null;
        public static string PrincipalCantDeleteUnpersisted = null;
        public static string PrincipalDeleted = null;
        public static string PrincipalNotSupportedOnFakePrincipal = null;
        public static string PrincipalMustPersistFirst = null;
        public static string PrincipalIdentityTypeNotAllowed = null;
        public static string PrincipalIdentityTypeNotRemovable = null;
        public static string PrincipalCantChangeSamNameOnPersistedSAM = null;

        public static string EmptyIdentityType = null;

        public static string PrincipalSearcherPersistedPrincipal = null;
        public static string PrincipalSearcherMustSetContext = null;
        public static string PrincipalSearcherMustSetContextForUnderlying = null;
        public static string PrincipalSearcherNoUnderlying = null;
        public static string PrincipalSearcherNonReferentialProps = null;

        public static string FindResultEnumInvalidPos = null;

        public static string TrackedCollectionNotOneDimensional = null;
        public static string TrackedCollectionIndexNotInArray = null;
        public static string TrackedCollectionArrayTooSmall = null;

        public static string TrackedCollectionEnumHasChanged = null;
        public static string TrackedCollectionEnumInvalidPos = null;

        public static string MultipleMatchesExceptionText = null;
        public static string MultipleMatchingPrincipals = null;
        public static string NoMatchingPrincipalExceptionText = null;
        public static string NoMatchingGroupExceptionText = null;
        public static string PrincipalExistsExceptionText = null;

        public static string IdentityClaimCollectionNullFields = null;

        public static string PrincipalCollectionNotOneDimensional = null;
        public static string PrincipalCollectionIndexNotInArray = null;
        public static string PrincipalCollectionArrayTooSmall = null;

        public static string PrincipalCollectionEnumHasChanged = null;
        public static string PrincipalCollectionEnumInvalidPos = null;
        public static string PrincipalCollectionAlreadyMember = null;

        public static string AuthenticablePrincipalMustBeSubtypeOfAuthPrinc = null;

        public static string PasswordInfoChangePwdOnUnpersistedPrinc = null;

        public static string UserMustSetContextForMethod = null;
        public static string UserDomainNotSupportedOnPlatform = null;
        public static string UserLocalNotSupportedOnPlatform = null;
        public static string UserCouldNotFindCurrent = null;

        public static string UnableToRetrieveDomainInfo = null;
        public static string UnableToOpenToken = null;
        public static string UnableToRetrieveTokenInfo = null;
        public static string UnableToRetrievePolicy = null;
        public static string UnableToImpersonateCredentials = null;

        public static string StoreCtxUnsupportedPrincipalTypeForSave = null;
        public static string StoreCtxUnsupportedPrincipalTypeForGroupInsert = null;
        public static string StoreCtxUnsupportedPrincipalTypeForQuery = null;
        public static string StoreCtxUnsupportedPropertyForQuery = null;
        public static string StoreCtxUnsupportedIdentityClaimForQuery = null;
        public static string StoreCtxIdentityClaimMustHaveScheme = null;
        public static string StoreCtxSecurityIdentityClaimBadFormat = null;
        public static string StoreCtxGuidIdentityClaimBadFormat = null;
        public static string StoreCtxNT4IdentityClaimWrongForm = null;
        public static string StoreCtxCantSetTimeLimitOnIdentityClaim = null;
        public static string StoreCtxGroupHasUnpersistedInsertedPrincipal = null;
        public static string StoreCtxNeedValueSecurityIdentityClaimToQuery = null;
        public static string StoreCtxExceptionUpdatingGroup = null;
        public static string StoreCtxExceptionCommittingChanges = null;

        public static string ADStoreCtxUnsupportedPrincipalContextForGroupInsert = null;
        public static string ADStoreCtxCouldntGetSIDForGroupMember = null;
        public static string ADStoreCtxMustBeContainer = null;
        public static string ADStoreCtxCantRetrieveObjectSidForCrossStore = null;
        public static string ADStoreCtxCantResolveSidForCrossStore = null;
        public static string ADStoreCtxFailedFindCrossStoreTarget = null;
        public static string ADStoreCtxCantClearGroup = null;
        public static string ADStoreCtxCantRemoveMemberFromGroup = null;

        public static string ADStoreCtxUnableToReadExistingAccountControlFlagsToEnable = null;
        public static string ADStoreCtxUnableToReadExistingAccountControlFlagsForUpdate = null;
        public static string ADStoreCtxUnableToReadExistingGroupTypeFlagsForUpdate = null;
        public static string ADStoreCtxNoComputerPasswordChange = null;

        public static string SAMStoreCtxUnableToRetrieveVersion = null;
        public static string SAMStoreCtxUnableToRetrieveMachineName = null;
        public static string SAMStoreCtxUnableToRetrieveFlatMachineName = null;

        public static string SAMStoreCtxNoComputerPasswordSet = null;
        public static string SAMStoreCtxNoComputerPasswordExpire = null;
        public static string SAMStoreCtxIdentityClaimsImmutable = null;
        public static string SAMStoreCtxCouldntGetSIDForGroupMember = null;
        public static string SAMStoreCtxFailedToClearGroup = null;
        public static string SAMStoreCtxCantRetrieveObjectSidForCrossStore = null;
        public static string SAMStoreCtxCantResolveSidForCrossStore = null;
        public static string SAMStoreCtxFailedFindCrossStoreTarget = null;
        public static string SAMStoreCtxErrorEnumeratingGroup = null;
        public static string SAMStoreCtxLocalGroupsOnly = null;

        public static string AuthZFailedToRetrieveGroupList = null;
        public static string AuthZNotSupported = null;
        public static string AuthZErrorEnumeratingGroups = null;
        public static string AuthZCantFindGroup = null;

        public static string ConfigHandlerConfigSectionsUnique = null;
        public static string ConfigHandlerInvalidBoolAttribute = null;
        public static string ConfigHandlerInvalidEnumAttribute = null;
        public static string ConfigHandlerInvalidStringAttribute = null;
        public static string ConfigHandlerUnknownConfigSection = null;

        public static string PrincipalPermWrongType = null;
        public static string PrincipalPermXmlNotPermission = null;
        public static string PrincipalPermXmlBadVersion = null;
        public static string PrincipalPermXmlBadContents = null;
        public static string ExtensionInvalidClassAttributes = null;
        public static string ExtensionInvalidClassDefinitionConstructor = null;

        public static string AdsiNotInstalled = null;
        public static string DSUnknown = null;
        public static string ContextOptionsNotValidForMachineStore = null;
        public static string ContextNoContainerForApplicationDirectoryCtx = null;
        public static string PassedContextTypeDoesNotMatchDetectedType = null;
        public static string NullArguments = null;
        public static string InvalidStringValueForStore = null;
        public static string InvalidNullArgument = null;
        public static string ServerDown = null;
        public static string PrincipalSearcherMustSetFilter = null;
        public static string InvalidPropertyForStore = null;
        public static string InvalidOperationForStore = null;
        public static string NameMustBeSetToPersistPrincipal = null;
        public static string SaveToMustHaveSamecontextType = null;
        public static string SaveToNotSupportedAgainstMachineStore = null;
        public static string ComputerInvalidForAppDirectoryStore = null;

        public static string InvalidContextOptionsForMachine = null;
        public static string InvalidContextOptionsForAD = null;

        public static string InvalidExtensionCollectionType = null;
        public static string ADAMStoreUnableToPopulateSchemaList = null;
        public static string StoreCtxMultipleFiltersForPropertyUnsupported = null;

        //public static string PrincipalPermEmptyName = null;
        //      PrincipalPermEmptyName=Name cannot be an empty string.
        //public static string PrincipalPermInvalidContextType = null;
        //      PrincipalPermInvalidContextType=The value '{0}' is not a valid value for the ContextType.        
    }
}

#pragma warning restore 0414
