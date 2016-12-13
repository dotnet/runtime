// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Collections;

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
        private static Hashtable s_resultCodeHash = null;

        static OperationErrorMappings()
        {
            s_resultCodeHash = new Hashtable();

            s_resultCodeHash.Add(ResultCode.Success, Res.GetString(Res.LDAP_SUCCESS));
            s_resultCodeHash.Add(ResultCode.OperationsError, Res.GetString(Res.LDAP_OPERATIONS_ERROR));
            s_resultCodeHash.Add(ResultCode.ProtocolError, Res.GetString(Res.LDAP_PROTOCOL_ERROR));
            s_resultCodeHash.Add(ResultCode.TimeLimitExceeded, Res.GetString(Res.LDAP_TIMELIMIT_EXCEEDED));
            s_resultCodeHash.Add(ResultCode.SizeLimitExceeded, Res.GetString(Res.LDAP_SIZELIMIT_EXCEEDED));
            s_resultCodeHash.Add(ResultCode.CompareFalse, Res.GetString(Res.LDAP_COMPARE_FALSE));
            s_resultCodeHash.Add(ResultCode.CompareTrue, Res.GetString(Res.LDAP_COMPARE_TRUE));
            s_resultCodeHash.Add(ResultCode.AuthMethodNotSupported, Res.GetString(Res.LDAP_AUTH_METHOD_NOT_SUPPORTED));
            s_resultCodeHash.Add(ResultCode.StrongAuthRequired, Res.GetString(Res.LDAP_STRONG_AUTH_REQUIRED));
            s_resultCodeHash.Add(ResultCode.ReferralV2, Res.GetString(Res.LDAP_PARTIAL_RESULTS));
            s_resultCodeHash.Add(ResultCode.Referral, Res.GetString(Res.LDAP_REFERRAL));
            s_resultCodeHash.Add(ResultCode.AdminLimitExceeded, Res.GetString(Res.LDAP_ADMIN_LIMIT_EXCEEDED));
            s_resultCodeHash.Add(ResultCode.UnavailableCriticalExtension, Res.GetString(Res.LDAP_UNAVAILABLE_CRIT_EXTENSION));
            s_resultCodeHash.Add(ResultCode.ConfidentialityRequired, Res.GetString(Res.LDAP_CONFIDENTIALITY_REQUIRED));
            s_resultCodeHash.Add(ResultCode.SaslBindInProgress, Res.GetString(Res.LDAP_SASL_BIND_IN_PROGRESS));
            s_resultCodeHash.Add(ResultCode.NoSuchAttribute, Res.GetString(Res.LDAP_NO_SUCH_ATTRIBUTE));
            s_resultCodeHash.Add(ResultCode.UndefinedAttributeType, Res.GetString(Res.LDAP_UNDEFINED_TYPE));
            s_resultCodeHash.Add(ResultCode.InappropriateMatching, Res.GetString(Res.LDAP_INAPPROPRIATE_MATCHING));
            s_resultCodeHash.Add(ResultCode.ConstraintViolation, Res.GetString(Res.LDAP_CONSTRAINT_VIOLATION));
            s_resultCodeHash.Add(ResultCode.AttributeOrValueExists, Res.GetString(Res.LDAP_ATTRIBUTE_OR_VALUE_EXISTS));
            s_resultCodeHash.Add(ResultCode.InvalidAttributeSyntax, Res.GetString(Res.LDAP_INVALID_SYNTAX));
            s_resultCodeHash.Add(ResultCode.NoSuchObject, Res.GetString(Res.LDAP_NO_SUCH_OBJECT));
            s_resultCodeHash.Add(ResultCode.AliasProblem, Res.GetString(Res.LDAP_ALIAS_PROBLEM));
            s_resultCodeHash.Add(ResultCode.InvalidDNSyntax, Res.GetString(Res.LDAP_INVALID_DN_SYNTAX));
            s_resultCodeHash.Add(ResultCode.AliasDereferencingProblem, Res.GetString(Res.LDAP_ALIAS_DEREF_PROBLEM));
            s_resultCodeHash.Add(ResultCode.InappropriateAuthentication, Res.GetString(Res.LDAP_INAPPROPRIATE_AUTH));
            s_resultCodeHash.Add(ResultCode.InsufficientAccessRights, Res.GetString(Res.LDAP_INSUFFICIENT_RIGHTS));
            s_resultCodeHash.Add(ResultCode.Busy, Res.GetString(Res.LDAP_BUSY));
            s_resultCodeHash.Add(ResultCode.Unavailable, Res.GetString(Res.LDAP_UNAVAILABLE));
            s_resultCodeHash.Add(ResultCode.UnwillingToPerform, Res.GetString(Res.LDAP_UNWILLING_TO_PERFORM));
            s_resultCodeHash.Add(ResultCode.LoopDetect, Res.GetString(Res.LDAP_LOOP_DETECT));
            s_resultCodeHash.Add(ResultCode.SortControlMissing, Res.GetString(Res.LDAP_SORT_CONTROL_MISSING));
            s_resultCodeHash.Add(ResultCode.OffsetRangeError, Res.GetString(Res.LDAP_OFFSET_RANGE_ERROR));
            s_resultCodeHash.Add(ResultCode.NamingViolation, Res.GetString(Res.LDAP_NAMING_VIOLATION));
            s_resultCodeHash.Add(ResultCode.ObjectClassViolation, Res.GetString(Res.LDAP_OBJECT_CLASS_VIOLATION));
            s_resultCodeHash.Add(ResultCode.NotAllowedOnNonLeaf, Res.GetString(Res.LDAP_NOT_ALLOWED_ON_NONLEAF));
            s_resultCodeHash.Add(ResultCode.NotAllowedOnRdn, Res.GetString(Res.LDAP_NOT_ALLOWED_ON_RDN));
            s_resultCodeHash.Add(ResultCode.EntryAlreadyExists, Res.GetString(Res.LDAP_ALREADY_EXISTS));
            s_resultCodeHash.Add(ResultCode.ObjectClassModificationsProhibited, Res.GetString(Res.LDAP_NO_OBJECT_CLASS_MODS));
            s_resultCodeHash.Add(ResultCode.ResultsTooLarge, Res.GetString(Res.LDAP_RESULTS_TOO_LARGE));
            s_resultCodeHash.Add(ResultCode.AffectsMultipleDsas, Res.GetString(Res.LDAP_AFFECTS_MULTIPLE_DSAS));
            s_resultCodeHash.Add(ResultCode.VirtualListViewError, Res.GetString(Res.LDAP_VIRTUAL_LIST_VIEW_ERROR));
            s_resultCodeHash.Add(ResultCode.Other, Res.GetString(Res.LDAP_OTHER));
        }

        static public string MapResultCode(int errorCode)
        {
            return (string)s_resultCodeHash[(ResultCode)errorCode];
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
