//------------------------------------------------------------------------------
// <copyright file="ResultCode.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

namespace System.DirectoryServices.Protocols {
    using System;
    using System.Collections;
    
    public enum ResultCode {
        Success                             = 0,
        OperationsError                     = 1,
        ProtocolError                       = 2,
        TimeLimitExceeded                   = 3,
        SizeLimitExceeded                   = 4,
        CompareFalse                        = 5,
        CompareTrue                         = 6,
        AuthMethodNotSupported              = 7,
        StrongAuthRequired                  = 8,
        ReferralV2                          = 9,
        Referral                            = 10,
        AdminLimitExceeded                  = 11,
        UnavailableCriticalExtension        = 12,
        ConfidentialityRequired             = 13,
        SaslBindInProgress                  = 14,
        NoSuchAttribute                     = 16,
        UndefinedAttributeType              = 17,
        InappropriateMatching               = 18,
        ConstraintViolation                 = 19,
        AttributeOrValueExists              = 20,
        InvalidAttributeSyntax              = 21,
        NoSuchObject                        = 32,
        AliasProblem                        = 33,
        InvalidDNSyntax                     = 34,
        AliasDereferencingProblem           = 36,
        InappropriateAuthentication         = 48,        
        InsufficientAccessRights            = 50,
        Busy                                = 51,
        Unavailable                         = 52,
        UnwillingToPerform                  = 53,
        LoopDetect                          = 54,
        SortControlMissing                  = 60,
        OffsetRangeError                    = 61,
        NamingViolation                     = 64,
        ObjectClassViolation                = 65,
        NotAllowedOnNonLeaf                 = 66,
        NotAllowedOnRdn                     = 67,
        EntryAlreadyExists                  = 68,
        ObjectClassModificationsProhibited  = 69,
        ResultsTooLarge                     = 70, 
        AffectsMultipleDsas                 = 71,
        VirtualListViewError                = 76,
        Other                               = 80
        
    }

    internal class OperationErrorMappings
    {
        static Hashtable ResultCodeHash = null;
        
        static OperationErrorMappings()
        {
            ResultCodeHash = new Hashtable();

            ResultCodeHash.Add(ResultCode.Success, Res.GetString(Res.LDAP_SUCCESS));
            ResultCodeHash.Add(ResultCode.OperationsError, Res.GetString(Res.LDAP_OPERATIONS_ERROR));
            ResultCodeHash.Add(ResultCode.ProtocolError, Res.GetString(Res.LDAP_PROTOCOL_ERROR));
            ResultCodeHash.Add(ResultCode.TimeLimitExceeded, Res.GetString(Res.LDAP_TIMELIMIT_EXCEEDED));
            ResultCodeHash.Add(ResultCode.SizeLimitExceeded, Res.GetString(Res.LDAP_SIZELIMIT_EXCEEDED));
            ResultCodeHash.Add(ResultCode.CompareFalse, Res.GetString(Res.LDAP_COMPARE_FALSE));
            ResultCodeHash.Add(ResultCode.CompareTrue, Res.GetString(Res.LDAP_COMPARE_TRUE));
            ResultCodeHash.Add(ResultCode.AuthMethodNotSupported, Res.GetString(Res.LDAP_AUTH_METHOD_NOT_SUPPORTED));
            ResultCodeHash.Add(ResultCode.StrongAuthRequired, Res.GetString(Res.LDAP_STRONG_AUTH_REQUIRED));
            ResultCodeHash.Add(ResultCode.ReferralV2, Res.GetString(Res.LDAP_PARTIAL_RESULTS));
            ResultCodeHash.Add(ResultCode.Referral, Res.GetString(Res.LDAP_REFERRAL));
            ResultCodeHash.Add(ResultCode.AdminLimitExceeded , Res.GetString(Res.LDAP_ADMIN_LIMIT_EXCEEDED));
            ResultCodeHash.Add(ResultCode.UnavailableCriticalExtension, Res.GetString(Res.LDAP_UNAVAILABLE_CRIT_EXTENSION));
            ResultCodeHash.Add(ResultCode.ConfidentialityRequired, Res.GetString(Res.LDAP_CONFIDENTIALITY_REQUIRED));
            ResultCodeHash.Add(ResultCode.SaslBindInProgress, Res.GetString(Res.LDAP_SASL_BIND_IN_PROGRESS));
            ResultCodeHash.Add(ResultCode.NoSuchAttribute, Res.GetString(Res.LDAP_NO_SUCH_ATTRIBUTE));
            ResultCodeHash.Add(ResultCode.UndefinedAttributeType, Res.GetString(Res.LDAP_UNDEFINED_TYPE));
            ResultCodeHash.Add(ResultCode.InappropriateMatching, Res.GetString(Res.LDAP_INAPPROPRIATE_MATCHING));
            ResultCodeHash.Add(ResultCode.ConstraintViolation, Res.GetString(Res.LDAP_CONSTRAINT_VIOLATION));
            ResultCodeHash.Add(ResultCode.AttributeOrValueExists, Res.GetString(Res.LDAP_ATTRIBUTE_OR_VALUE_EXISTS));
            ResultCodeHash.Add(ResultCode.InvalidAttributeSyntax, Res.GetString(Res.LDAP_INVALID_SYNTAX));
            ResultCodeHash.Add(ResultCode.NoSuchObject , Res.GetString(Res.LDAP_NO_SUCH_OBJECT));
            ResultCodeHash.Add(ResultCode.AliasProblem, Res.GetString(Res.LDAP_ALIAS_PROBLEM));
            ResultCodeHash.Add(ResultCode.InvalidDNSyntax, Res.GetString(Res.LDAP_INVALID_DN_SYNTAX));
            ResultCodeHash.Add(ResultCode.AliasDereferencingProblem, Res.GetString(Res.LDAP_ALIAS_DEREF_PROBLEM));
            ResultCodeHash.Add(ResultCode.InappropriateAuthentication, Res.GetString(Res.LDAP_INAPPROPRIATE_AUTH));
            ResultCodeHash.Add(ResultCode.InsufficientAccessRights, Res.GetString(Res.LDAP_INSUFFICIENT_RIGHTS));
            ResultCodeHash.Add(ResultCode.Busy, Res.GetString(Res.LDAP_BUSY));
            ResultCodeHash.Add(ResultCode.Unavailable, Res.GetString(Res.LDAP_UNAVAILABLE));
            ResultCodeHash.Add(ResultCode.UnwillingToPerform, Res.GetString(Res.LDAP_UNWILLING_TO_PERFORM));
            ResultCodeHash.Add(ResultCode.LoopDetect, Res.GetString(Res.LDAP_LOOP_DETECT));
            ResultCodeHash.Add(ResultCode.SortControlMissing, Res.GetString(Res.LDAP_SORT_CONTROL_MISSING));
            ResultCodeHash.Add(ResultCode.OffsetRangeError, Res.GetString(Res.LDAP_OFFSET_RANGE_ERROR));
            ResultCodeHash.Add(ResultCode.NamingViolation, Res.GetString(Res.LDAP_NAMING_VIOLATION));
            ResultCodeHash.Add(ResultCode.ObjectClassViolation, Res.GetString(Res.LDAP_OBJECT_CLASS_VIOLATION));
            ResultCodeHash.Add(ResultCode.NotAllowedOnNonLeaf, Res.GetString(Res.LDAP_NOT_ALLOWED_ON_NONLEAF));
            ResultCodeHash.Add(ResultCode.NotAllowedOnRdn, Res.GetString(Res.LDAP_NOT_ALLOWED_ON_RDN));
            ResultCodeHash.Add(ResultCode.EntryAlreadyExists, Res.GetString(Res.LDAP_ALREADY_EXISTS));
            ResultCodeHash.Add(ResultCode.ObjectClassModificationsProhibited, Res.GetString(Res.LDAP_NO_OBJECT_CLASS_MODS));
            ResultCodeHash.Add(ResultCode.ResultsTooLarge, Res.GetString(Res.LDAP_RESULTS_TOO_LARGE));
            ResultCodeHash.Add(ResultCode.AffectsMultipleDsas, Res.GetString(Res.LDAP_AFFECTS_MULTIPLE_DSAS));
            ResultCodeHash.Add(ResultCode.VirtualListViewError, Res.GetString(Res.LDAP_VIRTUAL_LIST_VIEW_ERROR));
            ResultCodeHash.Add(ResultCode.Other, Res.GetString(Res.LDAP_OTHER));
            
        }

        static public string MapResultCode(int errorCode)
		{
            return (string) ResultCodeHash[(ResultCode)errorCode];
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
