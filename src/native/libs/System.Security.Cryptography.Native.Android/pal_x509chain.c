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
    INIT_LOCALS(loc, keyStoreType, keyStore, targetSel, params, certList, certStoreType, certStoreParams, certStore, errorList);

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
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallVoidMethod(env, loc[targetSel], g_X509CertSelectorSetCertificate, cert);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

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
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallBooleanMethod(env, loc[certList], g_ArrayListAdd, cert);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    for (int i = 0; i < extraStoreLen; ++i)
    {
        (*env)->CallBooleanMethod(env, loc[certList], g_ArrayListAdd, extraStore[i]);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    }

    // String certStoreType = "Collection";
    // CollectionCertStoreParameters certStoreParams = new CollectionCertStoreParameters(certList);
    // CertStore certStore = CertStore.getInstance(certStoreType, certStoreParams);
    loc[certStoreType] = make_java_string(env, "Collection");
    loc[certStoreParams] = (*env)->NewObject(
        env, g_CollectionCertStoreParametersClass, g_CollectionCertStoreParametersCtor, loc[certList]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[certStore] = (*env)->CallStaticObjectMethod(
        env, g_CertStoreClass, g_CertStoreGetInstance, loc[certStoreType], loc[certStoreParams]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // params.addCertStore(certStore);
    (*env)->CallVoidMethod(env, loc[params], g_PKIXBuilderParametersAddCertStore, loc[certStore]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    loc[errorList] = (*env)->NewObject(env, g_ArrayListClass, g_ArrayListCtor);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    ret = xcalloc(1, sizeof(X509ChainContext));
    ret->params = AddGRef(env, loc[params]);
    ret->errorList = AddGRef(env, loc[errorList]);

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
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallVoidMethod(env, params, g_PKIXBuilderParametersSetDate, loc[date]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // Disable revocation checking when building the cert path. It will be handled in a validation pass if the path is
    // successfully built.
    // params.setRevocationEnabled(false);
    (*env)->CallVoidMethod(env, params, g_PKIXBuilderParametersSetRevocationEnabled, false);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // String builderType = "PKIX";
    // CertPathBuilder builder = CertPathBuilder.getInstance(builderType);
    // PKIXCertPathBuilderResult result = (PKIXCertPathBuilderResult)builder.build(params);
    loc[builderType] = make_java_string(env, "PKIX");
    loc[builder] =
        (*env)->CallStaticObjectMethod(env, g_CertPathBuilderClass, g_CertPathBuilderGetInstance, loc[builderType]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[result] = (*env)->CallObjectMethod(env, loc[builder], g_CertPathBuilderBuild, params);
    if (TryGetJNIException(env, &loc[ex], false /*printException*/))
    {
        (*env)->CallBooleanMethod(env, ctx->errorList, g_ArrayListAdd, loc[ex]);
        // Clear any exception raised while recording the build failure so it doesn't leak to managed code.
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
        goto cleanup;
    }

    loc[certPath] = (*env)->CallObjectMethod(env, loc[result], g_PKIXCertPathBuilderResultGetCertPath);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[trustAnchor] = (*env)->CallObjectMethod(env, loc[result], g_PKIXCertPathBuilderResultGetTrustAnchor);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

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

    int32_t ret = 0;
    int certCount = 0;
    INIT_LOCALS(loc, certPathList);

    // List<Certificate> certPathList = certPath.getCertificates();
    loc[certPathList] = (*env)->CallObjectMethod(env, ctx->certPath, g_CertPathGetCertificates);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    certCount = (int)(*env)->CallIntMethod(env, loc[certPathList], g_CollectionSize);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    ret = certCount + 1; // +1 for the trust anchor

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

int32_t AndroidCryptoNative_X509ChainGetCertificates(X509ChainContext* ctx,
                                                     jobject* /*X509Certificate[]*/ certs,
                                                     int32_t certsLen)
{
    abort_if_invalid_pointer_argument (ctx);
    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;
    int certCount = 0;
    int32_t added = 0;
    INIT_LOCALS(loc, certPathList, trustedCert, cert);

    // List<Certificate> certPathList = certPath.getCertificates();
    loc[certPathList] = (*env)->CallObjectMethod(env, ctx->certPath, g_CertPathGetCertificates);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    certCount = (int)(*env)->CallIntMethod(env, loc[certPathList], g_CollectionSize);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    if (certsLen < certCount + 1)
        goto cleanup;

    abort_if_invalid_pointer_argument (certs);

    // Populate `certs`, tracking how many entries we've stored (`added`) so that any failure
    // can roll them back -- the caller must never observe a partially populated array.
    // for (int i = 0; i < certPathList.size(); ++i) {
    //     Certificate cert = certPathList.get(i);
    //     certs[i] = cert;
    // }
    for (int32_t i = 0; i < certCount; ++i)
    {
        loc[cert] = (*env)->CallObjectMethod(env, loc[certPathList], g_ListGet, i);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
        certs[added++] = ToGRef(env, loc[cert]);
        loc[cert] = NULL;
    }

    // Certificate trustedCert = trustAnchor.getTrustedCert();
    // certs[i] = trustedCert;
    loc[trustedCert] = (*env)->CallObjectMethod(env, ctx->trustAnchor, g_TrustAnchorGetTrustedCert);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    if (added == 0 || !(*env)->IsSameObject(env, certs[added - 1], loc[trustedCert]))
    {
        certs[added++] = AddGRef(env, loc[trustedCert]);
    }

    ret = added;

cleanup:
    if (ret == FAIL)
    {
        // Release any global refs we already stored and clear the slots so the caller never
        // observes a partially populated array when we fail partway through.
        for (int32_t i = 0; i < added; ++i)
        {
            ReleaseGRef(env, certs[i]);
            certs[i] = NULL;
        }
    }
    RELEASE_LOCALS(loc, env);
    return ret;
}

int32_t AndroidCryptoNative_X509ChainGetErrorCount(X509ChainContext* ctx)
{
    abort_if_invalid_pointer_argument(ctx);
    abort_unless(ctx->errorList != NULL, "errorList is NULL in X509ChainContext");

    JNIEnv* env = GetJNIEnv();
    int32_t count = -1;
    int32_t errorCount = 0;
    int32_t revocationErrorCount = 0;

    errorCount = (*env)->CallIntMethod(env, ctx->errorList, g_CollectionSize);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    if (ctx->revocationErrorList != NULL)
    {
        revocationErrorCount = (*env)->CallIntMethod(env, ctx->revocationErrorList, g_CollectionSize);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    }

    count = errorCount + revocationErrorCount;

cleanup:
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
    if ((*env)->IsInstanceOf(env, reason, g_CertPathExceptionBasicReasonClass))
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
    else if ((*env)->IsInstanceOf(env, reason, g_PKIXReasonClass))
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

static int32_t PopulateValidationError(JNIEnv* env, jobject error, bool isRevocationError, ValidationError* out)
{
    int32_t ret = FAIL;
    int index = -1;
    PAL_X509ChainStatusFlags chainStatus = PAL_X509ChainNoError;
    uint16_t* messagePtr = NULL;
    INIT_LOCALS(loc, reason, message);

    if ((*env)->IsInstanceOf(env, error, g_CertPathValidatorExceptionClass))
    {
        index = (*env)->CallIntMethod(env, error, g_CertPathValidatorExceptionGetIndex);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        // Get the reason and convert it to a chain status flag.
        loc[reason] = (*env)->CallObjectMethod(env, error, g_CertPathValidatorExceptionGetReason);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
        chainStatus = ChainStatusFromValidatorExceptionReason(env, loc[reason]);
    }
    else
    {
        chainStatus = isRevocationError ? PAL_X509ChainRevocationStatusUnknown : PAL_X509ChainPartialChain;
    }

    loc[message] = (*env)->CallObjectMethod(env, error, g_ThrowableGetMessage);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    if (loc[message] != NULL)
    {
        jsize messageLen = (*env)->GetStringLength(env, loc[message]);

        // +1 for null terminator
        messagePtr = xmalloc(sizeof(uint16_t) * (size_t)(messageLen + 1));
        messagePtr[messageLen] = '\0';
        (*env)->GetStringRegion(env, loc[message], 0, messageLen, (jchar*)messagePtr);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
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
    messagePtr = NULL;
    ret = SUCCESS;

cleanup:
    free(messagePtr);
    RELEASE_LOCALS(loc, env);
    return ret;
}

int32_t AndroidCryptoNative_X509ChainGetErrors(X509ChainContext* ctx, ValidationError* errors, int32_t errorsLen)
{
    abort_if_invalid_pointer_argument (ctx);
    abort_unless(ctx->errorList != NULL, "errorList is NULL in X509ChainContext");

    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;
    int32_t populated = 0;
    int32_t errorCount = 0;
    int32_t revocationErrorCount = 0;

    errorCount = (*env)->CallIntMethod(env, ctx->errorList, g_CollectionSize);
    ON_EXCEPTION_PRINT_AND_GOTO(exit);
    revocationErrorCount =
        ctx->revocationErrorList == NULL ? 0 : (*env)->CallIntMethod(env, ctx->revocationErrorList, g_CollectionSize);
    ON_EXCEPTION_PRINT_AND_GOTO(exit);

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
        int32_t status = PopulateValidationError(env, error, false /*isRevocationError*/, &errors[populated]);
        (*env)->DeleteLocalRef(env, error);
        if (status != SUCCESS)
            goto exit;
        populated++;
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
            int32_t status = PopulateValidationError(env, error, true /*isRevocationError*/, &errors[populated]);
            (*env)->DeleteLocalRef(env, error);
            if (status != SUCCESS)
                goto exit;
            populated++;
        }
    }

    ret = SUCCESS;

exit:
    if (ret != SUCCESS)
    {
        // The managed caller only frees ValidationError.message on the success path, so release any
        // buffers already populated here to avoid leaking them when we fail partway through.
        for (int32_t i = 0; i < populated; ++i)
        {
            free(errors[i].message);
            errors[i].message = NULL;
        }
    }
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

    int32_t ret = FAIL;
    INIT_LOCALS(loc, anchors, anchor);

    // HashSet<TrustAnchor> anchors = new HashSet<TrustAnchor>(customTrustStoreLen);
    // for (Certificate cert : customTrustStore) {
    //     TrustAnchor anchor = new TrustAnchor(cert, null);
    //     anchors.Add(anchor);
    // }
    loc[anchors] = (*env)->NewObject(env, g_HashSetClass, g_HashSetCtorWithCapacity, customTrustStoreLen);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    for (int i = 0; i < customTrustStoreLen; ++i)
    {
        loc[anchor] = (*env)->NewObject(env, g_TrustAnchorClass, g_TrustAnchorCtor, customTrustStore[i], NULL);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
        (*env)->CallBooleanMethod(env, loc[anchors], g_HashSetAdd, loc[anchor]);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
        ReleaseLRef(env, loc[anchor]);
        loc[anchor] = NULL;
    }

    // params.setTrustAnchors(anchors);
    (*env)->CallVoidMethod(env, ctx->params, g_PKIXBuilderParametersSetTrustAnchors, loc[anchors]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

static jobject /*CertPath*/ CreateCertPathFromAnchor(JNIEnv* env, jobject /*TrustAnchor*/ trustAnchor)
{
    jobject ret = NULL;
    INIT_LOCALS(loc, certList, trustedCert, certFactoryType, certFactory);

    // ArrayList<Certificate> certList = new ArrayList<Certificate>(1);
    loc[certList] = (*env)->NewObject(env, g_ArrayListClass, g_ArrayListCtorWithCapacity, 1);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // Certificate trustedCert = trustAnchor.getTrustedCert();
    // certList.add(trustedCert);
    loc[trustedCert] = (*env)->CallObjectMethod(env, trustAnchor, g_TrustAnchorGetTrustedCert);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallBooleanMethod(env, loc[certList], g_ArrayListAdd, loc[trustedCert]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

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
    INIT_LOCALS(loc, certPathList, certPathFromAnchor, options, checker, result, ex, revocationErrorList, endOnly);

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
        loc[certPathList] = (*env)->CallObjectMethod(env, ctx->certPath, g_CertPathGetCertificates);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
        int certCount = (int)(*env)->CallIntMethod(env, loc[certPathList], g_CollectionSize);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
        if (certCount == 0)
        {
            // If the chain only consists only of the trust anchor, create a path with just
            // the trust anchor for revocation checking for end certificate only. This should
            // still pass normal (non-revocation) validation when it is the only certificate.
            loc[certPathFromAnchor] = CreateCertPathFromAnchor(env, ctx->trustAnchor);
            if (loc[certPathFromAnchor] == NULL)
                goto cleanup;
            certPathToUse = loc[certPathFromAnchor];
        }
        else
        {
            certPathToUse = ctx->certPath;

            // Only add the ONLY_END_ENTITY if we are not just checking the trust anchor. If ONLY_END_ENTITY is
            // specified, revocation checking will skip the trust anchor even if it is the only certificate.

            // HashSet<PKIXRevocationChecker.Option> options = new HashSet<PKIXRevocationChecker.Option>(3);
            // options.add(PKIXRevocationChecker.Option.ONLY_END_ENTITY);
            loc[options] = (*env)->NewObject(env, g_HashSetClass, g_HashSetCtorWithCapacity, 3);
            ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
            loc[endOnly] = (*env)->GetStaticObjectField(
                env, g_PKIXRevocationCheckerOptionClass, g_PKIXRevocationCheckerOptionOnlyEndEntity);
            ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
            (*env)->CallBooleanMethod(env, loc[options], g_HashSetAdd, loc[endOnly]);
            ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
        }
    }
    else
    {
        abort_unless(revocationFlag == X509RevocationFlag_ExcludeRoot, "revocationFlag must be X509RevocationFlag_ExcludeRoot");
        certPathToUse = ctx->certPath;
    }

    jobject params = ctx->params;

    // PKIXRevocationChecker checker = validator.getRevocationChecker();
    loc[checker] = (*env)->CallObjectMethod(env, validator, g_CertPathValidatorGetRevocationChecker);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // Set any specific options
    if (loc[options] != NULL)
    {
        // checker.setOptions(options);
        (*env)->CallVoidMethod(env, loc[checker], g_PKIXRevocationCheckerSetOptions, loc[options]);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    }

    // params.addCertPathChecker(checker);
    (*env)->CallVoidMethod(env, params, g_PKIXBuilderParametersAddCertPathChecker, loc[checker]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // params.setRevocationEnabled(true);
    // PKIXCertPathValidatorResult result = (PKIXCertPathValidatorResult)validator.validate(certPathToUse, params);
    (*env)->CallVoidMethod(env, params, g_PKIXBuilderParametersSetRevocationEnabled, true);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[result] = (*env)->CallObjectMethod(env, validator, g_CertPathValidatorValidate, certPathToUse, params);
    if (TryGetJNIException(env, &loc[ex], false /*printException*/))
    {
        if (ctx->revocationErrorList == NULL)
        {
            loc[revocationErrorList] = (*env)->NewObject(env, g_ArrayListClass, g_ArrayListCtor);
            ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
            ctx->revocationErrorList = AddGRef(env, loc[revocationErrorList]);
        }

        (*env)->CallBooleanMethod(env, ctx->revocationErrorList, g_ArrayListAdd, loc[ex]);
        // Clear any exception raised while recording the revocation failure so it doesn't leak to managed code.
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
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
        // Clear any exception raised while recording the validation failure so it doesn't leak to managed code.
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
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
