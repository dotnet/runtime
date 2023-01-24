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
    jobject /*ArrayList<Throwable>*/ revocationErrorList;
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
    abort_if_invalid_pointer_argument (cert);

    if (extraStore == NULL && extraStoreLen != 0) {
        LOG_WARN ("No extra store pointer provided, but extra store length is %d", extraStoreLen);
        extraStoreLen = 0;
    }

    JNIEnv* env = GetJNIEnv();

    X509ChainContext* ret = NULL;
    INIT_LOCALS(loc, keyStoreType, keyStore, targetSel, params, certList, certStoreType, certStoreParams, certStore);

    // String keyStoreType = "AndroidCAStore";
    // KeyStore keyStore = KeyStore.getInstance(keyStoreType);
    // keyStore.load(null, null);
    loc[keyStoreType] = make_java_string(env, "AndroidCAStore");
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
    loc[certStoreType] = make_java_string(env, "Collection");
    loc[certStoreParams] = (*env)->NewObject(
        env, g_CollectionCertStoreParametersClass, g_CollectionCertStoreParametersCtor, loc[certList]);
    loc[certStore] = (*env)->CallStaticObjectMethod(
        env, g_CertStoreClass, g_CertStoreGetInstance, loc[certStoreType], loc[certStoreParams]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // params.addCertStore(certStore);
    (*env)->CallVoidMethod(env, loc[params], g_PKIXBuilderParametersAddCertStore, loc[certStore]);

    ret = xcalloc(1, sizeof(X509ChainContext));
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
    ReleaseGRef(env, ctx->revocationErrorList);
    free(ctx);
}

int32_t AndroidCryptoNative_X509ChainBuild(X509ChainContext* ctx, int64_t timeInMsFromUnixEpoch)
{
    abort_if_invalid_pointer_argument (ctx);
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
    loc[builderType] = make_java_string(env, "PKIX");
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
    abort_if_invalid_pointer_argument (ctx);
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
    abort_if_invalid_pointer_argument (ctx);
    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;

    // List<Certificate> certPathList = certPath.getCertificates();
    jobject certPathList = (*env)->CallObjectMethod(env, ctx->certPath, g_CertPathGetCertificates);
    int certCount = (int)(*env)->CallIntMethod(env, certPathList, g_CollectionSize);
    if (certsLen < certCount + 1)
        goto cleanup;

    abort_if_invalid_pointer_argument (certs);

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
    if (i == 0 || !(*env)->IsSameObject(env, certs[i-1], trustedCert))
    {
        certs[i] = ToGRef(env, trustedCert);
        ret = i + 1;
    }
    else
    {
        ret = i;
        certs[i] = NULL;
    }

cleanup:
    (*env)->DeleteLocalRef(env, certPathList);
    return ret;
}

int32_t AndroidCryptoNative_X509ChainGetErrorCount(X509ChainContext* ctx)
{
    abort_if_invalid_pointer_argument(ctx);
    abort_unless(ctx->errorList != NULL, "errorList is NULL in X509ChainContext");

    JNIEnv* env = GetJNIEnv();
    int32_t count = (*env)->CallIntMethod(env, ctx->errorList, g_CollectionSize);
    if (ctx->revocationErrorList != NULL)
    {
        count += (*env)->CallIntMethod(env, ctx->revocationErrorList, g_CollectionSize);
    }

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
    if (g_CertPathExceptionBasicReasonClass != NULL &&
        (*env)->IsInstanceOf(env, reason, g_CertPathExceptionBasicReasonClass))
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
                return PAL_X509ChainNotSignatureValid;
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

static void PopulateValidationError(JNIEnv* env, jobject error, bool isRevocationError, ValidationError* out)
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
    else
    {
        chainStatus = isRevocationError ? PAL_X509ChainRevocationStatusUnknown : PAL_X509ChainPartialChain;
    }

    jobject message = (*env)->CallObjectMethod(env, error, g_ThrowableGetMessage);
    uint16_t* messagePtr = NULL;
    if (message != NULL)
    {
        jsize messageLen = message == NULL ? 0 : (*env)->GetStringLength(env, message);

        // +1 for null terminator
        messagePtr = xmalloc(sizeof(uint16_t) * (size_t)(messageLen + 1));
        messagePtr[messageLen] = '\0';
        (*env)->GetStringRegion(env, message, 0, messageLen, (jchar*)messagePtr);
    }

    // If the error is known to be from revocation checking, but couldn't be mapped to a revocation status,
    // report it as RevocationStatusUnknown
    if (isRevocationError && chainStatus != PAL_X509ChainRevocationStatusUnknown && chainStatus != PAL_X509ChainRevoked)
    {
        chainStatus = PAL_X509ChainRevocationStatusUnknown;
    }

    out->message = messagePtr;
    out->index = index;
    out->chainStatus = chainStatus;

    (*env)->DeleteLocalRef(env, message);
}

int32_t AndroidCryptoNative_X509ChainGetErrors(X509ChainContext* ctx, ValidationError* errors, int32_t errorsLen)
{
    abort_if_invalid_pointer_argument (ctx);
    abort_unless(ctx->errorList != NULL, "errorList is NULL in X509ChainContext");

    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;

    int32_t errorCount = (*env)->CallIntMethod(env, ctx->errorList, g_CollectionSize);
    int32_t revocationErrorCount =
        ctx->revocationErrorList == NULL ? 0 : (*env)->CallIntMethod(env, ctx->revocationErrorList, g_CollectionSize);

    if (errorsLen < errorCount + revocationErrorCount)
        goto exit;

    abort_if_invalid_pointer_argument (errors);

    // for (int i = 0; i < errorList.size(); ++i) {
    //     Throwable error = errorList.get(i);
    //     << populate errors[i] >>
    // }
    for (int32_t i = 0; i < errorCount; ++i)
    {
        jobject error = (*env)->CallObjectMethod(env, ctx->errorList, g_ListGet, i);
        ON_EXCEPTION_PRINT_AND_GOTO(exit);
        PopulateValidationError(env, error, false /*isRevocationError*/, &errors[i]);
        (*env)->DeleteLocalRef(env, error);
    }

    // for (int i = 0; i < revocationErrorList.size(); ++i) {
    //     Throwable error = revocationErrorList.get(i);
    //     << populate errors[i] >>
    // }
    if(ctx->revocationErrorList != NULL) { // double check, don't just trust the count to protect us from a segfault
        for (int32_t i = 0; i < revocationErrorCount; ++i)
        {
            jobject error = (*env)->CallObjectMethod(env, ctx->revocationErrorList, g_ListGet, i);
            ON_EXCEPTION_PRINT_AND_GOTO(exit);
            PopulateValidationError(env, error, true /*isRevocationError*/, &errors[errorCount + i]);
            (*env)->DeleteLocalRef(env, error);
        }
    }

    ret = SUCCESS;

exit:
    return ret;
}

int32_t AndroidCryptoNative_X509ChainSetCustomTrustStore(X509ChainContext* ctx,
                                                         jobject* /*X509Certificate*/ customTrustStore,
                                                         int32_t customTrustStoreLen)
{
    abort_if_invalid_pointer_argument (ctx);
    if (customTrustStoreLen > 0) {
        abort_if_invalid_pointer_argument (customTrustStore);
    }
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

static bool X509ChainSupportsRevocationOptions(void)
{
    return g_CertPathValidatorGetRevocationChecker != NULL && g_PKIXRevocationCheckerClass != NULL;
}

static jobject /*CertPath*/ CreateCertPathFromAnchor(JNIEnv* env, jobject /*TrustAnchor*/ trustAnchor)
{
    jobject ret = NULL;
    INIT_LOCALS(loc, certList, trustedCert, certFactoryType, certFactory);

    // ArrayList<Certificate> certList = new ArrayList<Certificate>(1);
    loc[certList] = (*env)->NewObject(env, g_ArrayListClass, g_ArrayListCtorWithCapacity, 1);

    // Certificate trustedCert = trustAnchor.getTrustedCert();
    // certList.add(trustedCert);
    loc[trustedCert] = (*env)->CallObjectMethod(env, trustAnchor, g_TrustAnchorGetTrustedCert);
    (*env)->CallBooleanMethod(env, loc[certList], g_ArrayListAdd, loc[trustedCert]);

    // CertificateFactory certFactory = CertificateFactory.getInstance("X.509");
    loc[certFactoryType] = make_java_string(env, "X.509");
    loc[certFactory] =
        (*env)->CallStaticObjectMethod(env, g_CertFactoryClass, g_CertFactoryGetInstance, loc[certFactoryType]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // CertPath certPath = certFactory.generateCertPath(certList);
    jobject certPath =
        (*env)->CallObjectMethod(env, loc[certFactory], g_CertFactoryGenerateCertPathFromList, loc[certList]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    ret = certPath;

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

static int32_t ValidateWithRevocation(JNIEnv* env,
                                      X509ChainContext* ctx,
                                      jobject /*CertPathValidator*/ validator,
                                      PAL_X509RevocationMode revocationMode,
                                      PAL_X509RevocationFlag revocationFlag)
{
    abort_if_invalid_pointer_argument (ctx);
    abort_if_invalid_pointer_argument (validator);

    int32_t ret = FAIL;
    INIT_LOCALS(loc, certPathFromAnchor, options, checker, result, ex);

    if (revocationMode == X509RevocationMode_Offline)
    {
        // Android does not supply a way to disable OCSP/CRL fetching
        LOG_INFO("Treating revocation mode 'Offline' as 'Online'.");
    }

    jobject certPathToUse = NULL;
    if (revocationFlag == X509RevocationFlag_EntireChain)
    {
        LOG_INFO("Treating revocation flag 'EntireChain' as 'ExcludeRoot'. "
                 "Revocation will be not be checked for root certificate.");

        certPathToUse = ctx->certPath;
    }
    else if (revocationFlag == X509RevocationFlag_EndCertificateOnly)
    {
        // List<Certificate> certPathList = certPath.getCertificates();
        // int certCount = certPathList.size();
        jobject certPathList = (*env)->CallObjectMethod(env, ctx->certPath, g_CertPathGetCertificates);
        int certCount = (int)(*env)->CallIntMethod(env, certPathList, g_CollectionSize);
        if (certCount == 0)
        {
            // If the chain only consists only of the trust anchor, create a path with just
            // the trust anchor for revocation checking for end certificate only. This should
            // still pass normal (non-revocation) validation when it is the only certificate.
            loc[certPathFromAnchor] = CreateCertPathFromAnchor(env, ctx->trustAnchor);
            certPathToUse = loc[certPathFromAnchor];
        }
        else
        {
            certPathToUse = ctx->certPath;
            if (X509ChainSupportsRevocationOptions())
            {
                // Only add the ONLY_END_ENTITY if we are not just checking the trust anchor. If ONLY_END_ENTITY is
                // specified, revocation checking will skip the trust anchor even if it is the only certificate.

                // HashSet<PKIXRevocationChecker.Option> options = new HashSet<PKIXRevocationChecker.Option>(3);
                // options.add(PKIXRevocationChecker.Option.ONLY_END_ENTITY);
                loc[options] = (*env)->NewObject(env, g_HashSetClass, g_HashSetCtorWithCapacity, 3);
                jobject endOnly = (*env)->GetStaticObjectField(
                    env, g_PKIXRevocationCheckerOptionClass, g_PKIXRevocationCheckerOptionOnlyEndEntity);
                (*env)->CallBooleanMethod(env, loc[options], g_HashSetAdd, endOnly);
                (*env)->DeleteLocalRef(env, endOnly);
            }
            else
            {
                LOG_INFO("Treating revocation flag 'EndCertificateOnly' as 'ExcludeRoot'. "
                         "Revocation will be checked for non-end certificates.");
            }
        }

        (*env)->DeleteLocalRef(env, certPathList);
    }
    else
    {
        abort_unless(revocationFlag == X509RevocationFlag_ExcludeRoot, "revocationFlag must be X509RevocationFlag_ExcludeRoot");
        certPathToUse = ctx->certPath;
    }

    jobject params = ctx->params;
    if (X509ChainSupportsRevocationOptions())
    {
        // PKIXRevocationChecker checker = validator.getRevocationChecker();
        loc[checker] = (*env)->CallObjectMethod(env, validator, g_CertPathValidatorGetRevocationChecker);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        // Set any specific options
        if (loc[options] != NULL)
        {
            // checker.setOptions(options);
            (*env)->CallVoidMethod(env, loc[checker], g_PKIXRevocationCheckerSetOptions, loc[options]);
        }

        // params.addCertPathChecker(checker);
        (*env)->CallVoidMethod(env, params, g_PKIXBuilderParametersAddCertPathChecker, loc[checker]);
    }

    // params.setRevocationEnabled(true);
    // PKIXCertPathValidatorResult result = (PKIXCertPathValidatorResult)validator.validate(certPathToUse, params);
    (*env)->CallVoidMethod(env, params, g_PKIXBuilderParametersSetRevocationEnabled, true);
    loc[result] = (*env)->CallObjectMethod(env, validator, g_CertPathValidatorValidate, certPathToUse, params);
    if (TryGetJNIException(env, &loc[ex], false /*printException*/))
    {
        if (ctx->revocationErrorList == NULL)
        {
            ctx->revocationErrorList = ToGRef(env, (*env)->NewObject(env, g_ArrayListClass, g_ArrayListCtor));
        }

        (*env)->CallBooleanMethod(env, ctx->revocationErrorList, g_ArrayListAdd, loc[ex]);
    }

    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

int32_t AndroidCryptoNative_X509ChainValidate(X509ChainContext* ctx,
                                              PAL_X509RevocationMode revocationMode,
                                              PAL_X509RevocationFlag revocationFlag,
                                              bool* checkedRevocation)
{
    abort_if_invalid_pointer_argument (ctx);
    abort_if_invalid_pointer_argument (checkedRevocation);
    JNIEnv* env = GetJNIEnv();

    *checkedRevocation = false;
    int32_t ret = FAIL;
    INIT_LOCALS(loc, validatorType, validator, result, ex);

    // String validatorType = "PKIX";
    // CertPathValidator validator = CertPathValidator.getInstance(validatorType);
    loc[validatorType] = make_java_string(env, "PKIX");
    loc[validator] = (*env)->CallStaticObjectMethod(
        env, g_CertPathValidatorClass, g_CertPathValidatorGetInstance, loc[validatorType]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // PKIXCertPathValidatorResult result = (PKIXCertPathValidatorResult)validator.validate(certPath, params);
    loc[result] =
        (*env)->CallObjectMethod(env, loc[validator], g_CertPathValidatorValidate, ctx->certPath, ctx->params);
    if (TryGetJNIException(env, &loc[ex], false /*printException*/))
    {
        (*env)->CallBooleanMethod(env, ctx->errorList, g_ArrayListAdd, loc[ex]);
        ret = SUCCESS;
    }
    else if (revocationMode != X509RevocationMode_NoCheck)
    {
        ret = ValidateWithRevocation(env, ctx, loc[validator], revocationMode, revocationFlag);
        *checkedRevocation = true;
    }
    else
    {
        ret = SUCCESS;
    }

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}
