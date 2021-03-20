// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_x509chain.h"
#include <pal_x509_types.h>

#include <assert.h>
#include <stdbool.h>
#include <string.h>

struct X509ChainContext_t
{
    jobject /*PKIXBuilderParameters*/ params;
    jobject /*CertPath*/ certPath;
    jobject /*TrustAnchor*/ trustAnchor;

    jobject /*ArrayList<Throwable>*/ errorList;
};

struct ValidationError_t
{
    uint16_t* message;
    int index;
    PAL_X509ChainStatusFlags chainStatus;
};

X509ChainContext* AndroidCryptoNative_X509ChainCreateContext(jobject /*X509Certificate*/ cert,
                                                             jobject* /*X509Certificate[]*/ extraStore,
                                                             int32_t extraStoreLen)
{
    assert(cert != NULL);
    assert(extraStore != NULL || extraStoreLen == 0);
    JNIEnv* env = GetJNIEnv();

    X509ChainContext* ret = NULL;
    INIT_LOCALS(loc, keyStoreType, keyStore, targetSel, params, certList, certStoreType, certStoreParams, certStore);

    // String keyStoreType = "AndroidCAStore";
    // KeyStore keyStore = KeyStore.getInstance(keyStoreType);
    // keyStore.load(null, null);
    loc[keyStoreType] = JSTRING("AndroidCAStore");
    loc[keyStore] = (*env)->CallStaticObjectMethod(env, g_KeyStoreClass, g_KeyStoreGetInstance, loc[keyStoreType]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallVoidMethod(env, loc[keyStore], g_KeyStoreLoad, NULL, NULL);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // X509CertSelector targetSel = new X509CertSelector();
    // targetSel.setCertificate(cert);
    loc[targetSel] = (*env)->NewObject(env, g_X509CertSelectorClass, g_X509CertSelectorCtor);
    (*env)->CallVoidMethod(env, loc[targetSel], g_X509CertSelectorSetCertificate, cert);

    // PKIXBuilderParameters params = new PKIXBuilderParameters(keyStore, targetSelector);
    loc[params] = (*env)->NewObject(
        env, g_PKIXBuilderParametersClass, g_PKIXBuilderParametersCtor, loc[keyStore], loc[targetSel]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // ArrayList<Certificate> certList = new ArrayList<Certificate>();
    // certList.add(cert);
    // for (int i = 0; i < extraStoreLen; i++) {
    //     certList.add(extraStore[i]);
    // }
    loc[certList] = (*env)->NewObject(env, g_ArrayListClass, g_ArrayListCtorWithCapacity, extraStoreLen);
    (*env)->CallBooleanMethod(env, loc[certList], g_ArrayListAdd, cert);
    for (int i = 0; i < extraStoreLen; ++i)
    {
        (*env)->CallBooleanMethod(env, loc[certList], g_ArrayListAdd, extraStore[i]);
    }

    // String certStoreType = "Collection";
    // CollectionCertStoreParameters certStoreParams = new CollectionCertStoreParameters(certList);
    // CertStore certStore = CertStore.getInstance(certStoreType, certStoreParams);
    loc[certStoreType] = JSTRING("Collection");
    loc[certStoreParams] = (*env)->NewObject(
        env, g_CollectionCertStoreParametersClass, g_CollectionCertStoreParametersCtor, loc[certList]);
    loc[certStore] = (*env)->CallStaticObjectMethod(
        env, g_CertStoreClass, g_CertStoreGetInstance, loc[certStoreType], loc[certStoreParams]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // params.addCertStore(certStore);
    (*env)->CallVoidMethod(env, loc[params], g_PKIXBuilderParametersAddCertStore, loc[certStore]);

    ret = malloc(sizeof(X509ChainContext));
    memset(ret, 0, sizeof(X509ChainContext));
    ret->params = AddGRef(env, loc[params]);
    ret->errorList = ToGRef(env, (*env)->NewObject(env, g_ArrayListClass, g_ArrayListCtor));

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

void AndroidCryptoNative_X509ChainDestroyContext(X509ChainContext* ctx)
{
    if (ctx == NULL)
        return;

    JNIEnv* env = GetJNIEnv();
    ReleaseGRef(env, ctx->params);
    ReleaseGRef(env, ctx->certPath);
    ReleaseGRef(env, ctx->trustAnchor);
    ReleaseGRef(env, ctx->errorList);
    free(ctx);
}

int32_t AndroidCryptoNative_X509ChainBuild(X509ChainContext* ctx, int64_t timeInMsFromUnixEpoch)
{
    assert(ctx != NULL);
    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;
    INIT_LOCALS(loc, date, builderType, builder, result, ex, certPath, trustAnchor);

    jobject params = ctx->params;

    // Date date = new Date(timeInMsFromUnixEpoch);
    // params.setDate(date);
    loc[date] = (*env)->NewObject(env, g_DateClass, g_DateCtor, timeInMsFromUnixEpoch);
    (*env)->CallVoidMethod(env, params, g_PKIXBuilderParametersSetDate, loc[date]);

    // Disable revocation checking when building the cert path. It will be handled in a validation pass if the path is
    // successfully built.
    // params.setRevocationEnabled(false);
    (*env)->CallVoidMethod(env, params, g_PKIXBuilderParametersSetRevocationEnabled, false);

    // String builderType = "PKIX";
    // CertPathBuilder builder = CertPathBuilder.getInstance(builderType);
    // PKIXCertPathBuilderResult result = (PKIXCertPathBuilderResult)builder.build(params);
    loc[builderType] = JSTRING("PKIX");
    loc[builder] =
        (*env)->CallStaticObjectMethod(env, g_CertPathBuilderClass, g_CertPathBuilderGetInstance, loc[builderType]);
    loc[result] = (*env)->CallObjectMethod(env, loc[builder], g_CertPathBuilderBuild, params);
    if (TryGetJNIException(env, &loc[ex], false /*printException*/))
    {
        (*env)->CallBooleanMethod(env, ctx->errorList, g_ArrayListAdd, loc[ex]);
        goto cleanup;
    }

    loc[certPath] = (*env)->CallObjectMethod(env, loc[result], g_PKIXCertPathBuilderResultGetCertPath);
    loc[trustAnchor] = (*env)->CallObjectMethod(env, loc[result], g_PKIXCertPathBuilderResultGetTrustAnchor);

    ctx->certPath = AddGRef(env, loc[certPath]);
    ctx->trustAnchor = AddGRef(env, loc[trustAnchor]);
    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

int32_t AndroidCryptoNative_X509ChainGetCertificateCount(X509ChainContext* ctx)
{
    assert(ctx != NULL);
    JNIEnv* env = GetJNIEnv();

    // List<Certificate> certPathList = certPath.getCertificates();
    jobject certPathList = (*env)->CallObjectMethod(env, ctx->certPath, g_CertPathGetCertificates);
    int certCount = (int)(*env)->CallIntMethod(env, certPathList, g_CollectionSize);

    (*env)->DeleteLocalRef(env, certPathList);
    return certCount + 1; // +1 for the trust anchor
}

int32_t AndroidCryptoNative_X509ChainGetCertificates(X509ChainContext* ctx,
                                                     jobject* /*X509Certificate[]*/ certs,
                                                     int32_t certsLen)
{
    assert(ctx != NULL);
    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;

    // List<Certificate> certPathList = certPath.getCertificates();
    jobject certPathList = (*env)->CallObjectMethod(env, ctx->certPath, g_CertPathGetCertificates);
    int certCount = (int)(*env)->CallIntMethod(env, certPathList, g_CollectionSize);
    if (certsLen < certCount + 1)
        goto cleanup;

    // for (int i = 0; i < certPathList.size(); ++i) {
    //     Certificate cert = certPathList.get(i);
    //     certs[i] = cert;
    // }
    int32_t i;
    for (i = 0; i < certCount; ++i)
    {
        jobject cert = (*env)->CallObjectMethod(env, certPathList, g_ListGet, i);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
        certs[i] = ToGRef(env, cert);
    }

    // Certificate trustedCert = trustAnchor.getTrustedCert();
    // certs[i] = trustedCert;
    jobject trustedCert = (*env)->CallObjectMethod(env, ctx->trustAnchor, g_TrustAnchorGetTrustedCert);
    certs[i] = ToGRef(env, trustedCert);

    ret = SUCCESS;

cleanup:
    (*env)->DeleteLocalRef(env, certPathList);
    return ret;
}

int32_t AndroidCryptoNative_X509ChainGetErrorCount(X509ChainContext* ctx)
{
    assert(ctx != NULL);
    JNIEnv* env = GetJNIEnv();
    int32_t count = (*env)->CallIntMethod(env, ctx->errorList, g_CollectionSize);
    return count;
}

enum
{
    PKIXREASON_NAME_CHAINING,
    PKIXREASON_INVALID_KEY_USAGE,
    PKIXREASON_INVALID_POLICY,
    PKIXREASON_NO_TRUST_ANCHOR,
    PKIXREASON_UNRECOGNIZED_CRIT_EXT,
    PKIXREASON_NOT_CA_CERT,
    PKIXREASON_PATH_TOO_LONG,
    PKIXREASON_INVALID_NAME,
};

enum
{
    BASICREASON_UNSPECIFIED,
    BASICREASON_EXPIRED,
    BASICREASON_NOT_YET_VALID,
    BASICREASON_REVOKED,
    BASICREASON_UNDETERMINED_REVOCATION_STATUS,
    BASICREASON_INVALID_SIGNATURE,
    BASICREASON_ALGORITHM_CONSTRAINED,
};

static PAL_X509ChainStatusFlags ChainStatusFromValidatorExceptionReason(JNIEnv* env, jobject reason)
{
    int value = (*env)->CallIntMethod(env, reason, g_EnumOrdinal);
    if (g_CertPathExceptionBasicReasonClass != NULL
        && (*env)->IsInstanceOf(env, reason, g_CertPathExceptionBasicReasonClass))
    {
        switch (value)
        {
            case BASICREASON_UNSPECIFIED:
                return PAL_X509ChainPartialChain;
            case BASICREASON_EXPIRED:
            case BASICREASON_NOT_YET_VALID:
                return PAL_X509ChainNotTimeValid;
            case BASICREASON_REVOKED:
                return PAL_X509ChainRevoked;
            case BASICREASON_UNDETERMINED_REVOCATION_STATUS:
                return PAL_X509ChainRevocationStatusUnknown;
            case BASICREASON_INVALID_SIGNATURE:
                return PAL_X509ChainCtlNotSignatureValid;
            case BASICREASON_ALGORITHM_CONSTRAINED:
                return PAL_X509ChainPartialChain;
        }
    }
    else if (g_PKIXReasonClass != NULL && (*env)->IsInstanceOf(env, reason, g_PKIXReasonClass))
    {
        switch (value)
        {
            case PKIXREASON_NAME_CHAINING:
                return PAL_X509ChainPartialChain;
            case PKIXREASON_INVALID_KEY_USAGE:
                return PAL_X509ChainNotValidForUsage;
            case PKIXREASON_INVALID_POLICY:
                return PAL_X509ChainInvalidPolicyConstraints;
            case PKIXREASON_NO_TRUST_ANCHOR:
                return PAL_X509ChainPartialChain;
            case PKIXREASON_UNRECOGNIZED_CRIT_EXT:
                return PAL_X509ChainHasNotSupportedCriticalExtension;
            case PKIXREASON_NOT_CA_CERT:
                return PAL_X509ChainUntrustedRoot;
            case PKIXREASON_PATH_TOO_LONG:
                return PAL_X509ChainInvalidBasicConstraints;
            case PKIXREASON_INVALID_NAME:
                return PAL_X509ChainInvalidNameConstraints;
        }
    }

    return PAL_X509ChainPartialChain;
}

static void PopulateValidationError(JNIEnv* env, jobject error, ValidationError* out)
{
    int index = -1;
    PAL_X509ChainStatusFlags chainStatus = PAL_X509ChainNoError;
    if ((*env)->IsInstanceOf(env, error, g_CertPathValidatorExceptionClass))
    {
        index = (*env)->CallIntMethod(env, error, g_CertPathValidatorExceptionGetIndex);

        // Get the reason (if the API is available) and convert it to a chain status flag
        if (g_CertPathValidatorExceptionGetReason != NULL)
        {
            jobject reason = (*env)->CallObjectMethod(env, error, g_CertPathValidatorExceptionGetReason);
            chainStatus = ChainStatusFromValidatorExceptionReason(env, reason);
            (*env)->DeleteLocalRef(env, reason);
        }
    }

    jobject message = (*env)->CallObjectMethod(env, error, g_ThrowableGetMessage);
    jsize messageLen = (*env)->GetStringLength(env, message);

    // +1 for null terminator
    uint16_t* messagePtr = malloc(sizeof(uint16_t) * (size_t)(messageLen + 1));
    messagePtr[messageLen] = '\0';
    (*env)->GetStringRegion(env, message, 0, messageLen, (jchar*)messagePtr);

    out->message = messagePtr;
    out->index = index;
    out->chainStatus = chainStatus;

    (*env)->DeleteLocalRef(env, message);
}

int32_t AndroidCryptoNative_X509ChainGetErrors(X509ChainContext* ctx, ValidationError* errors, int32_t errorsLen)
{
    assert(ctx != NULL);
    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;

    int32_t count = (*env)->CallIntMethod(env, ctx->errorList, g_CollectionSize);
    if (errorsLen < count)
        goto exit;

    // for (int i = 0; i < erroList.size(); ++i) {
    //     Throwable error = erroList.get(i);
    //     << populate errors[i] >>
    // }
    for (int32_t i = 0; i < count; ++i)
    {
        jobject error = (*env)->CallObjectMethod(env, ctx->errorList, g_ListGet, i);
        ON_EXCEPTION_PRINT_AND_GOTO(exit);
        PopulateValidationError(env, error, &errors[i]);
        (*env)->DeleteLocalRef(env, error);
    }

    ret = SUCCESS;

exit:
    return ret;
}

int32_t AndroidCryptoNative_X509ChainSetCustomTrustStore(X509ChainContext* ctx,
                                                         jobject* /*X509Certificate*/ customTrustStore,
                                                         int32_t customTrustStoreLen)
{
    assert(ctx != NULL);
    JNIEnv* env = GetJNIEnv();

    // HashSet<TrustAnchor> anchors = new HashSet<TrustAnchor>(customTrustStoreLen);
    // for (Certificate cert : customTrustStore) {
    //     TrustAnchor anchor = new TrustAnchor(cert, null);
    //     anchors.Add(anchor);
    // }
    jobject anchors = (*env)->NewObject(env, g_HashSetClass, g_HashSetCtorWithCapacity, customTrustStoreLen);
    for (int i = 0; i < customTrustStoreLen; ++i)
    {
        jobject anchor = (*env)->NewObject(env, g_TrustAnchorClass, g_TrustAnchorCtor, customTrustStore[i], NULL);
        (*env)->CallBooleanMethod(env, anchors, g_HashSetAdd, anchor);
        (*env)->DeleteLocalRef(env, anchor);
    }

    // params.setTrustAnchors(anchors);
    (*env)->CallVoidMethod(env, ctx->params, g_PKIXBuilderParametersSetTrustAnchors, anchors);

    (*env)->DeleteLocalRef(env, anchors);
    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

bool AndroidCryptoNative_X509ChainSupportsRevocationOptions(void)
{
    return g_CertPathValidatorGetRevocationChecker != NULL && g_PKIXRevocationCheckerClass != NULL;
}

static jobject /*HashSet<PKIXRevocationChecker.Option>*/
GetRevocationCheckerOptions(JNIEnv* env, PAL_X509RevocationMode revocationMode, PAL_X509RevocationFlag revocationFlag)
{
    assert(AndroidCryptoNative_X509ChainSupportsRevocationOptions());

    // HashSet<PKIXRevocationChecker.Option> options = new HashSet<PKIXRevocationChecker.Option>(3);
    jobject options = (*env)->NewObject(env, g_HashSetClass, g_HashSetCtorWithCapacity, 3);

    if (revocationMode == X509RevocationMode_Offline)
    {
        // options.add(PKIXRevocationChecker.Option.PREFER_CRLS);
        jobject preferCrls = (*env)->GetStaticObjectField(
            env, g_PKIXRevocationCheckerOptionClass, g_PKIXRevocationCheckerOptionPreferCrls);
        (*env)->CallBooleanMethod(env, options, g_HashSetAdd, preferCrls);
        (*env)->DeleteLocalRef(env, preferCrls);

        // options.add(PKIXRevocationChecker.Option.NO_FALLBACK);
        jobject noFallback = (*env)->GetStaticObjectField(
            env, g_PKIXRevocationCheckerOptionClass, g_PKIXRevocationCheckerOptionNoFallback);
        (*env)->CallBooleanMethod(env, options, g_HashSetAdd, noFallback);
        (*env)->DeleteLocalRef(env, noFallback);
    }

    if (revocationFlag == X509RevocationFlag_EndCertificateOnly)
    {
        // options.add(PKIXRevocationChecker.Option.ONLY_END_ENTITY);
        jobject endOnly = (*env)->GetStaticObjectField(
            env, g_PKIXRevocationCheckerOptionClass, g_PKIXRevocationCheckerOptionOnlyEndEntity);
        (*env)->CallBooleanMethod(env, options, g_HashSetAdd, endOnly);
        (*env)->DeleteLocalRef(env, endOnly);
    }

    return options;
}

int32_t AndroidCryptoNative_X509ChainValidate(X509ChainContext* ctx,
                                              PAL_X509RevocationMode revocationMode,
                                              PAL_X509RevocationFlag revocationFlag)
{
    assert(ctx != NULL);
    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;
    INIT_LOCALS(loc, validatorType, validator, checker, result, ex);

    jobject params = ctx->params;
    jobject certPath = ctx->certPath;

    // String validatorType = "PKIX";
    // CertPathValidator validator = CertPathValidator.getInstance(validatorType);
    // PKIXCertPathValidatorResult result = (PKIXCertPathValidatorResult)validator.validate(certPath, params);
    loc[validatorType] = JSTRING("PKIX");
    loc[validator] = (*env)->CallStaticObjectMethod(
        env, g_CertPathValidatorClass, g_CertPathValidatorGetInstance, loc[validatorType]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    bool checkRevocation = revocationMode != X509RevocationMode_NoCheck;

    // params.setRevocationEnabled(checkRevocation);
    (*env)->CallVoidMethod(env, params, g_PKIXBuilderParametersSetRevocationEnabled, checkRevocation);
    if (checkRevocation)
    {
        if (revocationFlag == X509RevocationFlag_EntireChain)
        {
            LOG_INFO("Treating revocation flag 'EntireChain' as 'ExcludeRoot'. Revocation will not be checked for the root certificate.");
        }

        if (AndroidCryptoNative_X509ChainSupportsRevocationOptions())
        {
            // PKIXRevocationChecker checker = validator.getRevocationChecker();
            jobject checker = (*env)->CallObjectMethod(env, loc[validator], g_CertPathValidatorGetRevocationChecker);
            ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

            // checker.setOptions(options);
            // params.addCertPathChecker(checker);
            jobject options = GetRevocationCheckerOptions(env, revocationMode, revocationFlag);
            (*env)->CallVoidMethod(env, checker, g_PKIXRevocationCheckerSetOptions, options);
            (*env)->CallVoidMethod(env, params, g_PKIXBuilderParametersAddCertPathChecker, checker);

            (*env)->DeleteLocalRef(env, options);
            (*env)->DeleteLocalRef(env, checker);
        }
        else
        {
            if (revocationFlag == X509RevocationFlag_EndCertificateOnly)
            {
                LOG_INFO("Treating revocation flag 'EndCertificateOnly' as 'ExcludeRoot'. Revocation will be checked for non-end certificates.");
            }
        }
    }

    loc[result] = (*env)->CallObjectMethod(env, loc[validator], g_CertPathValidatorValidate, certPath, params);
    if (TryGetJNIException(env, &loc[ex], false /*printException*/))
    {
        (*env)->CallBooleanMethod(env, ctx->errorList, g_ArrayListAdd, loc[ex]);
    }

    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}
