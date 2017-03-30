// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Collections;
    using System.Globalization;

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

            s_resultCodeHash.Add(ResultCode.Success, String.Format(CultureInfo.CurrentCulture, SR.LDAP_SUCCESS));
            s_resultCodeHash.Add(ResultCode.OperationsError, String.Format(CultureInfo.CurrentCulture, SR.LDAP_OPERATIONS_ERROR));
            s_resultCodeHash.Add(ResultCode.ProtocolError, String.Format(CultureInfo.CurrentCulture, SR.LDAP_PROTOCOL_ERROR));
            s_resultCodeHash.Add(ResultCode.TimeLimitExceeded, String.Format(CultureInfo.CurrentCulture, SR.LDAP_TIMELIMIT_EXCEEDED));
            s_resultCodeHash.Add(ResultCode.SizeLimitExceeded, String.Format(CultureInfo.CurrentCulture, SR.LDAP_SIZELIMIT_EXCEEDED));
            s_resultCodeHash.Add(ResultCode.CompareFalse, String.Format(CultureInfo.CurrentCulture, SR.LDAP_COMPARE_FALSE));
            s_resultCodeHash.Add(ResultCode.CompareTrue, String.Format(CultureInfo.CurrentCulture, SR.LDAP_COMPARE_TRUE));
            s_resultCodeHash.Add(ResultCode.AuthMethodNotSupported, String.Format(CultureInfo.CurrentCulture, SR.LDAP_AUTH_METHOD_NOT_SUPPORTED));
            s_resultCodeHash.Add(ResultCode.StrongAuthRequired, String.Format(CultureInfo.CurrentCulture, SR.LDAP_STRONG_AUTH_REQUIRED));
            s_resultCodeHash.Add(ResultCode.ReferralV2, String.Format(CultureInfo.CurrentCulture, SR.LDAP_PARTIAL_RESULTS));
            s_resultCodeHash.Add(ResultCode.Referral, String.Format(CultureInfo.CurrentCulture, SR.LDAP_REFERRAL));
            s_resultCodeHash.Add(ResultCode.AdminLimitExceeded, String.Format(CultureInfo.CurrentCulture, SR.LDAP_ADMIN_LIMIT_EXCEEDED));
            s_resultCodeHash.Add(ResultCode.UnavailableCriticalExtension, String.Format(CultureInfo.CurrentCulture, SR.LDAP_UNAVAILABLE_CRIT_EXTENSION));
            s_resultCodeHash.Add(ResultCode.ConfidentialityRequired, String.Format(CultureInfo.CurrentCulture, SR.LDAP_CONFIDENTIALITY_REQUIRED));
            s_resultCodeHash.Add(ResultCode.SaslBindInProgress, String.Format(CultureInfo.CurrentCulture, SR.LDAP_SASL_BIND_IN_PROGRESS));
            s_resultCodeHash.Add(ResultCode.NoSuchAttribute, String.Format(CultureInfo.CurrentCulture, SR.LDAP_NO_SUCH_ATTRIBUTE));
            s_resultCodeHash.Add(ResultCode.UndefinedAttributeType, String.Format(CultureInfo.CurrentCulture, SR.LDAP_UNDEFINED_TYPE));
            s_resultCodeHash.Add(ResultCode.InappropriateMatching, String.Format(CultureInfo.CurrentCulture, SR.LDAP_INAPPROPRIATE_MATCHING));
            s_resultCodeHash.Add(ResultCode.ConstraintViolation, String.Format(CultureInfo.CurrentCulture, SR.LDAP_CONSTRAINT_VIOLATION));
            s_resultCodeHash.Add(ResultCode.AttributeOrValueExists, String.Format(CultureInfo.CurrentCulture, SR.LDAP_ATTRIBUTE_OR_VALUE_EXISTS));
            s_resultCodeHash.Add(ResultCode.InvalidAttributeSyntax, String.Format(CultureInfo.CurrentCulture, SR.LDAP_INVALID_SYNTAX));
            s_resultCodeHash.Add(ResultCode.NoSuchObject, String.Format(CultureInfo.CurrentCulture, SR.LDAP_NO_SUCH_OBJECT));
            s_resultCodeHash.Add(ResultCode.AliasProblem, String.Format(CultureInfo.CurrentCulture, SR.LDAP_ALIAS_PROBLEM));
            s_resultCodeHash.Add(ResultCode.InvalidDNSyntax, String.Format(CultureInfo.CurrentCulture, SR.LDAP_INVALID_DN_SYNTAX));
            s_resultCodeHash.Add(ResultCode.AliasDereferencingProblem, String.Format(CultureInfo.CurrentCulture, SR.LDAP_ALIAS_DEREF_PROBLEM));
            s_resultCodeHash.Add(ResultCode.InappropriateAuthentication, String.Format(CultureInfo.CurrentCulture, SR.LDAP_INAPPROPRIATE_AUTH));
            s_resultCodeHash.Add(ResultCode.InsufficientAccessRights, String.Format(CultureInfo.CurrentCulture, SR.LDAP_INSUFFICIENT_RIGHTS));
            s_resultCodeHash.Add(ResultCode.Busy, String.Format(CultureInfo.CurrentCulture, SR.LDAP_BUSY));
            s_resultCodeHash.Add(ResultCode.Unavailable, String.Format(CultureInfo.CurrentCulture, SR.LDAP_UNAVAILABLE));
            s_resultCodeHash.Add(ResultCode.UnwillingToPerform, String.Format(CultureInfo.CurrentCulture, SR.LDAP_UNWILLING_TO_PERFORM));
            s_resultCodeHash.Add(ResultCode.LoopDetect, String.Format(CultureInfo.CurrentCulture, SR.LDAP_LOOP_DETECT));
            s_resultCodeHash.Add(ResultCode.SortControlMissing, String.Format(CultureInfo.CurrentCulture, SR.LDAP_SORT_CONTROL_MISSING));
            s_resultCodeHash.Add(ResultCode.OffsetRangeError, String.Format(CultureInfo.CurrentCulture, SR.LDAP_OFFSET_RANGE_ERROR));
            s_resultCodeHash.Add(ResultCode.NamingViolation, String.Format(CultureInfo.CurrentCulture, SR.LDAP_NAMING_VIOLATION));
            s_resultCodeHash.Add(ResultCode.ObjectClassViolation, String.Format(CultureInfo.CurrentCulture, SR.LDAP_OBJECT_CLASS_VIOLATION));
            s_resultCodeHash.Add(ResultCode.NotAllowedOnNonLeaf, String.Format(CultureInfo.CurrentCulture, SR.LDAP_NOT_ALLOWED_ON_NONLEAF));
            s_resultCodeHash.Add(ResultCode.NotAllowedOnRdn, String.Format(CultureInfo.CurrentCulture, SR.LDAP_NOT_ALLOWED_ON_RDN));
            s_resultCodeHash.Add(ResultCode.EntryAlreadyExists, String.Format(CultureInfo.CurrentCulture, SR.LDAP_ALREADY_EXISTS));
            s_resultCodeHash.Add(ResultCode.ObjectClassModificationsProhibited, String.Format(CultureInfo.CurrentCulture, SR.LDAP_NO_OBJECT_CLASS_MODS));
            s_resultCodeHash.Add(ResultCode.ResultsTooLarge, String.Format(CultureInfo.CurrentCulture, SR.LDAP_RESULTS_TOO_LARGE));
            s_resultCodeHash.Add(ResultCode.AffectsMultipleDsas, String.Format(CultureInfo.CurrentCulture, SR.LDAP_AFFECTS_MULTIPLE_DSAS));
            s_resultCodeHash.Add(ResultCode.VirtualListViewError, String.Format(CultureInfo.CurrentCulture, SR.LDAP_VIRTUAL_LIST_VIEW_ERROR));
            s_resultCodeHash.Add(ResultCode.Other, String.Format(CultureInfo.CurrentCulture, SR.LDAP_OTHER));
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
