// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.DirectoryServices.Protocols
{
    public enum ResultCode
    {
        Success = 0,
        OperationsError = 1,
        ProtocolError = 2,
        TimeLimitExceeded = 3,
        SizeLimitExceeded = 4,
        CompareFalse = 5,
        CompareTrue = 6,
        AuthMethodNotSupported = 7,
        StrongAuthRequired = 8,
        ReferralV2 = 9,
        Referral = 10,
        AdminLimitExceeded = 11,
        UnavailableCriticalExtension = 12,
        ConfidentialityRequired = 13,
        SaslBindInProgress = 14,
        NoSuchAttribute = 16,
        UndefinedAttributeType = 17,
        InappropriateMatching = 18,
        ConstraintViolation = 19,
        AttributeOrValueExists = 20,
        InvalidAttributeSyntax = 21,
        NoSuchObject = 32,
        AliasProblem = 33,
        InvalidDNSyntax = 34,
        AliasDereferencingProblem = 36,
        InappropriateAuthentication = 48,
        InsufficientAccessRights = 50,
        Busy = 51,
        Unavailable = 52,
        UnwillingToPerform = 53,
        LoopDetect = 54,
        SortControlMissing = 60,
        OffsetRangeError = 61,
        NamingViolation = 64,
        ObjectClassViolation = 65,
        NotAllowedOnNonLeaf = 66,
        NotAllowedOnRdn = 67,
        EntryAlreadyExists = 68,
        ObjectClassModificationsProhibited = 69,
        ResultsTooLarge = 70,
        AffectsMultipleDsas = 71,
        VirtualListViewError = 76,
        Other = 80
    }

    internal class OperationErrorMappings
    {
        private static readonly Dictionary<ResultCode, string> s_resultCodeMapping = new Dictionary<ResultCode, string>(capacity: 43)
        {
            { ResultCode.Success, SR.LDAP_SUCCESS },
            { ResultCode.OperationsError, SR.LDAP_OPERATIONS_ERROR },
            { ResultCode.ProtocolError, SR.LDAP_PROTOCOL_ERROR },
            { ResultCode.TimeLimitExceeded, SR.LDAP_TIMELIMIT_EXCEEDED },
            { ResultCode.SizeLimitExceeded, SR.LDAP_SIZELIMIT_EXCEEDED },
            { ResultCode.CompareFalse, SR.LDAP_COMPARE_FALSE },
            { ResultCode.CompareTrue, SR.LDAP_COMPARE_TRUE },
            { ResultCode.AuthMethodNotSupported, SR.LDAP_AUTH_METHOD_NOT_SUPPORTED },
            { ResultCode.StrongAuthRequired, SR.LDAP_STRONG_AUTH_REQUIRED },
            { ResultCode.ReferralV2, SR.LDAP_PARTIAL_RESULTS },
            { ResultCode.Referral, SR.LDAP_REFERRAL },
            { ResultCode.AdminLimitExceeded, SR.LDAP_ADMIN_LIMIT_EXCEEDED },
            { ResultCode.UnavailableCriticalExtension, SR.LDAP_UNAVAILABLE_CRIT_EXTENSION },
            { ResultCode.ConfidentialityRequired, SR.LDAP_CONFIDENTIALITY_REQUIRED },
            { ResultCode.SaslBindInProgress, SR.LDAP_SASL_BIND_IN_PROGRESS },
            { ResultCode.NoSuchAttribute, SR.LDAP_NO_SUCH_ATTRIBUTE },
            { ResultCode.UndefinedAttributeType, SR.LDAP_UNDEFINED_TYPE },
            { ResultCode.InappropriateMatching, SR.LDAP_INAPPROPRIATE_MATCHING },
            { ResultCode.ConstraintViolation, SR.LDAP_CONSTRAINT_VIOLATION },
            { ResultCode.AttributeOrValueExists, SR.LDAP_ATTRIBUTE_OR_VALUE_EXISTS },
            { ResultCode.InvalidAttributeSyntax, SR.LDAP_INVALID_SYNTAX },
            { ResultCode.NoSuchObject, SR.LDAP_NO_SUCH_OBJECT },
            { ResultCode.AliasProblem, SR.LDAP_ALIAS_PROBLEM },
            { ResultCode.InvalidDNSyntax, SR.LDAP_INVALID_DN_SYNTAX },
            { ResultCode.AliasDereferencingProblem, SR.LDAP_ALIAS_DEREF_PROBLEM },
            { ResultCode.InappropriateAuthentication, SR.LDAP_INAPPROPRIATE_AUTH },
            { ResultCode.InsufficientAccessRights, SR.LDAP_INSUFFICIENT_RIGHTS },
            { ResultCode.Busy, SR.LDAP_BUSY },
            { ResultCode.Unavailable, SR.LDAP_UNAVAILABLE },
            { ResultCode.UnwillingToPerform, SR.LDAP_UNWILLING_TO_PERFORM },
            { ResultCode.LoopDetect, SR.LDAP_LOOP_DETECT },
            { ResultCode.SortControlMissing, SR.LDAP_SORT_CONTROL_MISSING },
            { ResultCode.OffsetRangeError, SR.LDAP_OFFSET_RANGE_ERROR },
            { ResultCode.NamingViolation, SR.LDAP_NAMING_VIOLATION },
            { ResultCode.ObjectClassViolation, SR.LDAP_OBJECT_CLASS_VIOLATION },
            { ResultCode.NotAllowedOnNonLeaf, SR.LDAP_NOT_ALLOWED_ON_NONLEAF },
            { ResultCode.NotAllowedOnRdn, SR.LDAP_NOT_ALLOWED_ON_RDN },
            { ResultCode.EntryAlreadyExists, SR.LDAP_ALREADY_EXISTS },
            { ResultCode.ObjectClassModificationsProhibited, SR.LDAP_NO_OBJECT_CLASS_MODS },
            { ResultCode.ResultsTooLarge, SR.LDAP_RESULTS_TOO_LARGE },
            { ResultCode.AffectsMultipleDsas, SR.LDAP_AFFECTS_MULTIPLE_DSAS },
            { ResultCode.VirtualListViewError, SR.LDAP_VIRTUAL_LIST_VIEW_ERROR },
            { ResultCode.Other, SR.LDAP_OTHER }
        };

        public static string MapResultCode(int errorCode)
        {
            s_resultCodeMapping.TryGetValue((ResultCode)errorCode, out string errorMessage);
            return errorMessage;
        }
    }

    internal enum LdapOperation
    {
        LdapAdd = 0,
        LdapModify = 1,
        LdapSearch = 2,
        LdapDelete = 3,
        LdapModifyDn = 4,
        LdapCompare = 5,
        LdapExtendedRequest = 6
    }
}
