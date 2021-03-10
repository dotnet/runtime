// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_x509chain.h"

#include <assert.h>
#include <stdbool.h>
#include <string.h>

#define INIT_LOCALS(name, ...) \
    enum { __VA_ARGS__, count_##name }; \
    jobject name[count_##name] = { 0 }; \

#define RELEASE_LOCALS(name, env) \
{ \
    for (int i_##name = 0; i_##name < count_##name; ++i_##name) \
    { \
        jobject local = name[i_##name]; \
        if (local != NULL) \
            (*env)->DeleteLocalRef(env, local); \
    } \
} \

struct X509ChainContext_t
{
    jobject /*PKIXBuilderParameters*/ params;
    jobject /*CertPath*/ certPath;
    jobject /*TrustAnchor*/ trustAnchor;

    jobject /*ArrayList<Throwable>*/ errorList;
};

enum
{
    PAL_X509ChainNoError = 0,
    PAL_X509ChainNotTimeValid = 0x00000001,
    PAL_X509ChainNotTimeNested = 0x00000002,
    PAL_X509ChainRevoked = 0x00000004,
    PAL_X509ChainNotSignatureValid = 0x00000008,
    PAL_X509ChainNotValidForUsage = 0x00000010,
    PAL_X509ChainUntrustedRoot = 0x00000020,
    PAL_X509ChainRevocationStatusUnknown = 0x00000040,
    PAL_X509ChainCyclic = 0x00000080,
    PAL_X509ChainInvalidExtension = 0x00000100,
    PAL_X509ChainInvalidPolicyConstraints = 0x00000200,
    PAL_X509ChainInvalidBasicConstraints = 0x00000400,
    PAL_X509ChainInvalidNameConstraints = 0x00000800,
    PAL_X509ChainHasNotSupportedNameConstraint = 0x00001000,
    PAL_X509ChainHasNotDefinedNameConstraint = 0x00002000,
    PAL_X509ChainHasNotPermittedNameConstraint = 0x00004000,
    PAL_X509ChainHasExcludedNameConstraint = 0x00008000,
    PAL_X509ChainPartialChain = 0x00010000,
    PAL_X509ChainCtlNotTimeValid = 0x00020000,
    PAL_X509ChainCtlNotSignatureValid = 0x00040000,
    PAL_X509ChainCtlNotValidForUsage = 0x00080000,
    PAL_X509ChainOfflineRevocation = 0x01000000,
    PAL_X509ChainNoIssuanceChainPolicy = 0x02000000,
    PAL_X509ChainExplicitDistrust = 0x04000000,
    PAL_X509ChainHasNotSupportedCriticalExtension = 0x08000000,
    PAL_X509ChainHasWeakSignature = 0x00100000,
};
typedef uint32_t PAL_X509ChainStatusFlags;

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
    INIT_LOCALS(loc, keyStoreType, keyStore, targetSel, params, certList, certStoreType, certStoreParams, certStore)

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
    RELEASE_LOCALS(loc, env)
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
    INIT_LOCALS(loc, date, builderType, builder, result, ex, certPath, trustAnchor)

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
    if (TryGetJNIException(env, &loc[ex], true /*printException*/))
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
    RELEASE_LOCALS(loc, env)
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
    INIT_LOCALS(loc, certPathList, iter)

    // List<Certificate> certPathList = certPath.getCertificates();
    loc[certPathList] = (*env)->CallObjectMethod(env, ctx->certPath, g_CertPathGetCertificates);
    int certCount = (int)(*env)->CallIntMethod(env, loc[certPathList], g_CollectionSize);

    if (certsLen < certCount + 1)
        goto cleanup;

    // int i = 0;
    // Iterator<Certificate> iter = certs.iterator();
    // while (iter.hasNext()) {
    //     Certificate cert = iter.next();
    //     out[i] = cert;
    //     i++;
    // }
    int32_t i = 0;
    loc[iter] = (*env)->CallObjectMethod(env, loc[certPathList], g_CollectionIterator);
    jboolean hasNext = (*env)->CallBooleanMethod(env, loc[iter], g_IteratorHasNext);
    while (hasNext)
    {
        jobject cert = (*env)->CallObjectMethod(env, loc[iter], g_IteratorNext);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        certs[i] = ToGRef(env, cert);
        i++;

        hasNext = (*env)->CallBooleanMethod(env, loc[iter], g_IteratorHasNext);
    }

    // Certificate trustedCert = trustAnchor.getTrustedCert();
    // certs[i] = trustedCert;
    jobject trustedCert = (*env)->CallObjectMethod(env, ctx->trustAnchor, g_TrustAnchorGetTrustedCert);
    certs[i] = ToGRef(env, trustedCert);

    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env)
    return ret;
}

int32_t AndroidCryptoNative_X509ChainGetErrorCount(X509ChainContext* ctx)
{
    assert(ctx != NULL);
    JNIEnv* env = GetJNIEnv();
    int32_t count = (*env)->CallIntMethod(env, ctx->errorList, g_CollectionSize);
    return count;
}

static PAL_X509ChainStatusFlags ChainStatusFromValidatorExceptionReason(jobject reason)
{
    // TODO: [AndroidCrypto] Convert reason to chain status
    return PAL_X509ChainNoError;
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
            chainStatus = ChainStatusFromValidatorExceptionReason(reason);
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

int32_t AndroidCryptoNative_X509ChainGetErrors(X509ChainContext* ctx,
                                                ValidationError* errors,
                                                int32_t errorsLen)
{
    assert(ctx != NULL);
    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;

    // int i = 0;
    // Iterator<Throwable> iter = errorList.iterator();
    // while (iter.hasNext()) {
    //     Throwable error = iter.next();
    //     << populate errors[i] >>
    //     i++;
    // }
    int32_t i = 0;
    jobject iter = (*env)->CallObjectMethod(env, ctx->errorList, g_CollectionIterator);
    jboolean hasNext = (*env)->CallBooleanMethod(env, iter, g_IteratorHasNext);
    while (hasNext)
    {
        jobject error = (*env)->CallObjectMethod(env, iter, g_IteratorNext);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        PopulateValidationError(env, error, &errors[i]);
        i++;

        hasNext = (*env)->CallBooleanMethod(env, iter, g_IteratorHasNext);
        (*env)->DeleteLocalRef(env, error);
    }

    ret = SUCCESS;

cleanup:
    (*env)->DeleteLocalRef(env, iter);
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

static jobject /*CertPath*/
GetCertPathFromBuilderResult(JNIEnv* env, jobject /*CertPath*/ certPath, jobject /*TrustAnchor*/ trustAnchor)
{
    jobject ret = NULL;
    INIT_LOCALS(loc, certPathList, certList, trustedCert, certFactoryType, certFactory)

    // List<Certificate> certPathList = certPath.getCertificates();
    loc[certPathList] = (*env)->CallObjectMethod(env, certPath, g_CertPathGetCertificates);

    // The result cert path does not include the trust anchor. Create a list combining the path and anchor.
    // ArrayList<Certificate> certList = new ArrayList<Certificate>(certPathList);
    loc[certList] = (*env)->NewObject(env, g_ArrayListClass, g_ArrayListCtorWithCollection, loc[certPathList]);

    // Certificate trustedCert = trustAnchor.getTrustedCert();
    // certList.add(trustedCert);
    loc[trustedCert] = (*env)->CallObjectMethod(env, trustAnchor, g_TrustAnchorGetTrustedCert);
    (*env)->CallBooleanMethod(env, loc[certList], g_ArrayListAdd, loc[trustedCert]);

    // CertificateFactory certFactory = CertificateFactory.getInstance("X.509");
    loc[certFactoryType] = JSTRING("X.509");
    loc[certFactory] =
        (*env)->CallStaticObjectMethod(env, g_CertFactoryClass, g_CertFactoryGetInstance, loc[certFactoryType]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // CertPath certPathWithAnchor = certFactory.generateCertPath(certList);
    jobject certPathWithAnchor =
        (*env)->CallObjectMethod(env, loc[certFactory], g_CertFactoryGenerateCertPathFromList, loc[certList]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    ret = certPathWithAnchor;

cleanup:
    RELEASE_LOCALS(loc, env)
    return ret;
}

int32_t AndroidCryptoNative_X509ChainValidate(X509ChainContext* ctx,
                                              PAL_X509RevocationMode revocationMode,
                                              PAL_X509RevocationFlag revocationFlag)
{
    assert(ctx != NULL);
    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;
    INIT_LOCALS(loc, validatorType, validator, result, ex)

    bool checkRevocation = revocationMode != X509RevocationMode_NoCheck;
    if (checkRevocation)
    {
        // bool isOnline = revocationMode == X509RevocationMode_Online;
        if (AndroidCryptoNative_X509ChainSupportsRevocationOptions())
        {
            // TODO: [AndroidCrypto] Deal with revocation options
            goto cleanup;
        }
        else
        {
            if (revocationFlag == X509RevocationFlag_EndCertificateOnly)
            {
                goto cleanup;
            }

            // Security.setProperty("oscp.enable", isOnline);
        }
    }

    // bool entireChain = checkRevocation && revocationFlag == X509RevocationFlag_EntireChain;

    jobject params = ctx->params;
    jobject certPath = ctx->certPath;

    // params.setRevocationEnabled(checkRevocation);
    (*env)->CallVoidMethod(env, params, g_PKIXBuilderParametersSetRevocationEnabled, checkRevocation);

    // String validatorType = "PKIX";
    // CertPathValidator validator = CertPathValidator.getInstance(validatorType);
    // PKIXCertPathValidatorResult result = (PKIXCertPathValidatorResult)validator.validate(certPath, params);
    loc[validatorType] = JSTRING("PKIX");
    loc[validator] = (*env)->CallStaticObjectMethod(
        env, g_CertPathValidatorClass, g_CertPathValidatorGetInstance, loc[validatorType]);
    loc[result] = (*env)->CallObjectMethod(env, loc[validator], g_CertPathValidatorValidate, certPath, params);
    if (TryGetJNIException(env, &loc[ex], true /*printException*/))
    {
        (*env)->CallBooleanMethod(env, ctx->errorList, g_ArrayListAdd, loc[ex]);
        goto cleanup;
    }

    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env)
    return ret;
}
