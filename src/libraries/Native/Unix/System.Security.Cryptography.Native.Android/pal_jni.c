// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_jni.h"

JavaVM* gJvm;

// java/io/ByteArrayInputStream
jclass    g_ByteArrayInputStreamClass;
jmethodID g_ByteArrayInputStreamCtor;
jmethodID g_ByteArrayInputStreamReset;

// java/lang/Enum
jclass    g_Enum;
jmethodID g_EnumOrdinal;

// java/security/Key
jclass    g_KeyClass;
jmethodID g_KeyGetAlgorithm;
jmethodID g_KeyGetEncoded;

// java/security/SecureRandom
jclass    g_randClass;
jmethodID g_randCtor;
jmethodID g_randNextBytesMethod;

// java/security/MessageDigest
jclass    g_mdClass;
jmethodID g_mdGetInstanceMethod;
jmethodID g_mdDigestMethod;
jmethodID g_mdDigestCurrentMethodId;
jmethodID g_mdResetMethod;
jmethodID g_mdUpdateMethod;

// javax/crypto/Mac
jclass    g_macClass;
jmethodID g_macGetInstanceMethod;
jmethodID g_macDoFinalMethod;
jmethodID g_macUpdateMethod;
jmethodID g_macInitMethod;
jmethodID g_macResetMethod;

// javax/crypto/spec/SecretKeySpec
jclass    g_sksClass;
jmethodID g_sksCtor;

// javax/crypto/Cipher
jclass    g_cipherClass;
jmethodID g_cipherGetInstanceMethod;
jmethodID g_cipherDoFinalMethod;
jmethodID g_cipherDoFinal2Method;
jmethodID g_cipherUpdateMethod;
jmethodID g_cipherUpdateAADMethod;
jmethodID g_cipherInitMethod;
jmethodID g_cipherInit2Method;
jmethodID g_getBlockSizeMethod;

// javax/crypto/spec/IvParameterSpec
jclass    g_ivPsClass;
jmethodID g_ivPsCtor;

// java/math/BigInteger
jclass    g_bigNumClass;
jmethodID g_bigNumCtor;
jmethodID g_bigNumCtorWithSign;
jmethodID g_toByteArrayMethod;
jmethodID g_valueOfMethod;
jmethodID g_intValueMethod;
jmethodID g_compareToMethod;
jmethodID g_bitLengthMethod;
jmethodID g_sigNumMethod;

// javax/net/ssl/SSLParameters
jclass    g_sslParamsClass;
jmethodID g_sslParamsGetProtocolsMethod;

// javax/net/ssl/SSLContext
jclass    g_sslCtxClass;
jmethodID g_sslCtxGetDefaultMethod;
jmethodID g_sslCtxGetDefaultSslParamsMethod;

// javax/crypto/spec/GCMParameterSpec
jclass    g_GCMParameterSpecClass;
jmethodID g_GCMParameterSpecCtor;

// java/security/interfaces/RSAKey
jclass    g_RSAKeyClass;
jmethodID g_RSAKeyGetModulus;

// java/security/interfaces/RSAPublicKey
jclass    g_RSAPublicKeyClass;
jmethodID g_RSAPublicKeyGetPubExpMethod;

// java/security/KeyPair
jclass    g_keyPairClass;
jmethodID g_keyPairCtor;
jmethodID g_keyPairGetPrivateMethod;
jmethodID g_keyPairGetPublicMethod;

// java/security/KeyPairGenerator
jclass    g_keyPairGenClass;
jmethodID g_keyPairGenGetInstanceMethod;
jmethodID g_keyPairGenInitializeWithParamsMethod;
jmethodID g_keyPairGenInitializeMethod;
jmethodID g_keyPairGenGenKeyPairMethod;

// java/security/Signature
jclass    g_SignatureClass;
jmethodID g_SignatureGetInstance;
jmethodID g_SignatureInitSign;
jmethodID g_SignatureInitVerify;
jmethodID g_SignatureUpdate;
jmethodID g_SignatureSign;
jmethodID g_SignatureVerify;
// java/security/cert/CertificateFactory
jclass    g_CertFactoryClass;
jmethodID g_CertFactoryGetInstance;
jmethodID g_CertFactoryGenerateCertificate;
jmethodID g_CertFactoryGenerateCertificates;
jmethodID g_CertFactoryGenerateCertPath;
jmethodID g_CertFactoryGenerateCRL;

// java/security/cert/X509Certificate
jclass    g_X509CertClass;
jmethodID g_X509CertGetEncoded;
jmethodID g_X509CertGetIssuerX500Principal;
jmethodID g_X509CertGetNotAfter;
jmethodID g_X509CertGetNotBefore;
jmethodID g_X509CertGetPublicKey;
jmethodID g_X509CertGetSerialNumber;
jmethodID g_X509CertGetSigAlgOID;
jmethodID g_X509CertGetSubjectX500Principal;
jmethodID g_X509CertGetVersion;

// java/security/cert/X509Certificate implements java/security/cert/X509Extension
jmethodID g_X509CertGetCriticalExtensionOIDs;
jmethodID g_X509CertGetExtensionValue;
jmethodID g_X509CertGetNonCriticalExtensionOIDs;

// java/security/cert/X509CRL
jclass    g_X509CRLClass;
jmethodID g_X509CRLGetNextUpdate;

// java/security/interfaces/RSAPrivateCrtKey
jclass    g_RSAPrivateCrtKeyClass;
jmethodID g_RSAPrivateCrtKeyPubExpField;
jmethodID g_RSAPrivateCrtKeyPrimePField;
jmethodID g_RSAPrivateCrtKeyPrimeQField;
jmethodID g_RSAPrivateCrtKeyPrimeExpPField;
jmethodID g_RSAPrivateCrtKeyPrimeExpQField;
jmethodID g_RSAPrivateCrtKeyCrtCoefField;
jmethodID g_RSAPrivateCrtKeyModulusField;
jmethodID g_RSAPrivateCrtKeyPrivExpField;

// java/security/spec/RSAPrivateCrtKeySpec
jclass    g_RSAPrivateCrtKeySpecClass;
jmethodID g_RSAPrivateCrtKeySpecCtor;

// java/security/spec/RSAPublicKeySpec
jclass    g_RSAPublicCrtKeySpecClass;
jmethodID g_RSAPublicCrtKeySpecCtor;

// java/security/KeyFactory
jclass    g_KeyFactoryClass;
jmethodID g_KeyFactoryGetInstanceMethod;
jmethodID g_KeyFactoryGenPrivateMethod;
jmethodID g_KeyFactoryGenPublicMethod;
jmethodID g_KeyFactoryGetKeySpecMethod;

// java/security/spec/ECParameterSpec
jclass    g_ECParameterSpecClass;
jmethodID g_ECParameterSpecCtor;
jmethodID g_ECParameterSpecGetCurve;
jmethodID g_ECParameterSpecGetGenerator;
jmethodID g_ECParameterSpecGetCofactor;
jmethodID g_ECParameterSpecGetOrder;
jmethodID g_ECParameterSpecGetCurveName;

// java/security/spec/ECField
jclass    g_ECFieldClass;
jmethodID g_ECFieldGetFieldSize;

// java/security/spec/ECFieldFp
jclass    g_ECFieldFpClass;
jmethodID g_ECFieldFpCtor;
jmethodID g_ECFieldFpGetP;

// java/security/spec/ECFieldF2m
jclass    g_ECFieldF2mClass;
jmethodID g_ECFieldF2mCtorWithCoefficientBigInteger;
jmethodID g_ECFieldF2mGetReductionPolynomial;

// java/security/spec/ECGenParameterSpecClass
jclass    g_ECGenParameterSpecClass;
jmethodID g_ECGenParameterSpecCtor;

// java/security/spec/ECPoint
jclass    g_ECPointClass;
jmethodID g_ECPointCtor;
jmethodID g_ECPointGetAffineX;
jmethodID g_ECPointGetAffineY;

// java/security/interfaces/ECPrivateKey
jclass    g_ECPrivateKeyClass;
jmethodID g_ECPrivateKeyGetS;

// java/security/spec/ECPrivateKeySpec
jclass    g_ECPrivateKeySpecClass;
jmethodID g_ECPrivateKeySpecCtor;

// java/security/interfaces/ECPublicKey
jclass    g_ECPublicKeyClass;
jmethodID g_ECPublicKeyGetW;

// java/security/spec/ECPublicKeySpec
jclass    g_ECPublicKeySpecClass;
jmethodID g_ECPublicKeySpecCtor;
jmethodID g_ECPublicKeySpecGetParams;

// java/security/spec/EllipticCurve
jclass    g_EllipticCurveClass;
jmethodID g_EllipticCurveCtor;
jmethodID g_EllipticCurveCtorWithSeed;
jmethodID g_EllipticCurveGetA;
jmethodID g_EllipticCurveGetB;
jmethodID g_EllipticCurveGetField;
jmethodID g_EllipticCurveGetSeed;

// java/security/spec/X509EncodedKeySpec
jclass    g_X509EncodedKeySpecClass;
jmethodID g_X509EncodedKeySpecCtor;

// javax/security/auth
jclass    g_DestroyableClass;
jmethodID g_destroy;

// java/util/Collection
jclass    g_CollectionClass;
jmethodID g_CollectionIterator;
jmethodID g_CollectionSize;

// java/util/Date
jclass    g_DateClass;
jmethodID g_DateGetTime;

// java/util/Iterator
jclass    g_IteratorClass;
jmethodID g_IteratorHasNext;
jmethodID g_IteratorNext;

// java/util/Set
jclass    g_SetClass;
jmethodID g_SetIterator;

// com/android/org/conscrypt/NativeCrypto
jclass    g_NativeCryptoClass;

// javax/net/ssl/SSLEngine
jclass    g_SSLEngine;
jmethodID g_SSLEngineSetUseClientModeMethod;
jmethodID g_SSLEngineGetSessionMethod;
jmethodID g_SSLEngineBeginHandshakeMethod;
jmethodID g_SSLEngineWrapMethod;
jmethodID g_SSLEngineUnwrapMethod;
jmethodID g_SSLEngineCloseInboundMethod;
jmethodID g_SSLEngineCloseOutboundMethod;
jmethodID g_SSLEngineGetHandshakeStatusMethod;

// java/nio/ByteBuffer
jclass    g_ByteBuffer;
jmethodID g_ByteBufferAllocateMethod;
jmethodID g_ByteBufferPutMethod;
jmethodID g_ByteBufferPut2Method;
jmethodID g_ByteBufferPut3Method;
jmethodID g_ByteBufferFlipMethod;
jmethodID g_ByteBufferGetMethod;
jmethodID g_ByteBufferPutBufferMethod;
jmethodID g_ByteBufferLimitMethod;
jmethodID g_ByteBufferRemainingMethod;
jmethodID g_ByteBufferCompactMethod;
jmethodID g_ByteBufferPositionMethod;

// javax/net/ssl/SSLContext
jclass    g_SSLContext;
jmethodID g_SSLContextGetInstanceMethod;
jmethodID g_SSLContextInitMethod;
jmethodID g_SSLContextCreateSSLEngineMethod;

// javax/net/ssl/SSLSession
jclass    g_SSLSession;
jmethodID g_SSLSessionGetApplicationBufferSizeMethod;
jmethodID g_SSLSessionGetPacketBufferSizeMethod;

// javax/net/ssl/SSLEngineResult
jclass    g_SSLEngineResult;
jmethodID g_SSLEngineResultGetStatusMethod;
jmethodID g_SSLEngineResultGetHandshakeStatusMethod;

// javax/net/ssl/TrustManager
jclass    g_TrustManager;

// javax/security/auth/x500/X500Principal
jclass    g_X500PrincipalClass;
jmethodID g_X500PrincipalGetEncoded;
jmethodID g_X500PrincipalHashCode;

// javax/crypto/KeyAgreement
jclass    g_KeyAgreementClass;
jmethodID g_KeyAgreementGetInstance;
jmethodID g_KeyAgreementInit;
jmethodID g_KeyAgreementDoPhase;
jmethodID g_KeyAgreementGenerateSecret;

jobject ToGRef(JNIEnv *env, jobject lref)
{
    if (lref)
    {
        jobject gref = (*env)->NewGlobalRef(env, lref);
        (*env)->DeleteLocalRef(env, lref);
        return gref;
    }
    return lref;
}

jobject AddGRef(JNIEnv *env, jobject gref)
{
    if (!gref)
        return NULL;
    return (*env)->NewGlobalRef(env, gref);
}

void ReleaseGRef(JNIEnv *env, jobject gref)
{
    if (gref)
        (*env)->DeleteGlobalRef(env, gref);
}

void ReleaseLRef(JNIEnv *env, jobject lref)
{
    if (lref)
        (*env)->DeleteLocalRef(env, lref);
}

jclass GetClassGRef(JNIEnv *env, const char* name)
{
    LOG_DEBUG("Finding %s class", name);
    jclass klass = ToGRef(env, (*env)->FindClass (env, name));
    if (!klass) {
        LOG_ERROR("class %s was not found", name);
        assert(klass);
    }
    return klass;
}

bool CheckJNIExceptions(JNIEnv* env)
{
    if ((*env)->ExceptionCheck(env))
    {
        (*env)->ExceptionDescribe(env);
        (*env)->ExceptionClear(env);
        return true;
    }
    return false;
}

void AssertOnJNIExceptions(JNIEnv* env)
{
    assert(!CheckJNIExceptions(env));
}

void SaveTo(uint8_t* src, uint8_t** dst, size_t len, bool overwrite)
{
    assert(overwrite || !(*dst));
    if (overwrite)
    {
        free(*dst);
    }
    *dst = (uint8_t*)malloc(len * sizeof(uint8_t));
    memcpy(*dst, src, len);
}

jmethodID GetMethod(JNIEnv *env, bool isStatic, jclass klass, const char* name, const char* sig)
{
    LOG_DEBUG("Finding %s method", name);
    jmethodID mid = isStatic ? (*env)->GetStaticMethodID(env, klass, name, sig) : (*env)->GetMethodID(env, klass, name, sig);
    if (!mid) {
        LOG_ERROR("method %s %s was not found", name, sig);
        assert(mid);
    }
    return mid;
}

jmethodID GetOptionalMethod(JNIEnv *env, bool isStatic, jclass klass, const char* name, const char* sig)
{
    LOG_DEBUG("Finding %s method", name);
    jmethodID mid = isStatic ? (*env)->GetStaticMethodID(env, klass, name, sig) : (*env)->GetMethodID(env, klass, name, sig);
    if (!mid) {
        LOG_INFO("optional method %s %s was not found", name, sig);
    }
    return mid;
}

jfieldID GetField(JNIEnv *env, bool isStatic, jclass klass, const char* name, const char* sig)
{
    LOG_DEBUG("Finding %s field", name);
    jfieldID fid = isStatic ? (*env)->GetStaticFieldID(env, klass, name, sig) : (*env)->GetFieldID(env, klass, name, sig);
    if (!fid) {
        LOG_ERROR("field %s %s was not found", name, sig);
        assert(fid);
    }
    return fid;
}

JNIEnv* GetJNIEnv()
{
    JNIEnv *env;
    (*gJvm)->GetEnv(gJvm, (void**)&env, JNI_VERSION_1_6);
    if (env)
        return env;
    jint ret = (*gJvm)->AttachCurrentThreadAsDaemon(gJvm, &env, NULL);
    assert(ret == JNI_OK && "Unable to attach thread to JVM");
    (void)ret;
    return env;
}

int GetEnumAsInt(JNIEnv *env, jobject enumObj)
{
    int value = (*env)->CallIntMethod(env, enumObj, g_EnumOrdinal);
    (*env)->DeleteLocalRef(env, enumObj);
    return value;
}

JNIEXPORT jint JNICALL
JNI_OnLoad(JavaVM *vm, void *reserved)
{
    (void)reserved;
    LOG_INFO("JNI_OnLoad in pal_jni.c");
    gJvm = vm;

    JNIEnv* env = GetJNIEnv();

    // cache some classes and methods while we're in the thread-safe JNI_OnLoad
    g_ByteArrayInputStreamClass =   GetClassGRef(env, "java/io/ByteArrayInputStream");
    g_ByteArrayInputStreamCtor =    GetMethod(env, false, g_ByteArrayInputStreamClass, "<init>", "([B)V");
    g_ByteArrayInputStreamReset =    GetMethod(env, false, g_ByteArrayInputStreamClass, "reset", "()V");

    g_Enum =                    GetClassGRef(env, "java/lang/Enum");
    g_EnumOrdinal =             GetMethod(env, false, g_Enum, "ordinal", "()I");

    g_KeyClass =        GetClassGRef(env, "java/security/Key");
    g_KeyGetAlgorithm = GetMethod(env, false, g_KeyClass, "getAlgorithm", "()Ljava/lang/String;");
    g_KeyGetEncoded =   GetMethod(env, false, g_KeyClass, "getEncoded", "()[B");

    g_randClass =               GetClassGRef(env, "java/security/SecureRandom");
    g_randCtor =                GetMethod(env, false, g_randClass, "<init>", "()V");
    g_randNextBytesMethod =     GetMethod(env, false, g_randClass, "nextBytes", "([B)V");

    g_mdClass =                 GetClassGRef(env, "java/security/MessageDigest");
    g_mdGetInstanceMethod =     GetMethod(env, true,  g_mdClass, "getInstance", "(Ljava/lang/String;)Ljava/security/MessageDigest;");
    g_mdResetMethod =           GetMethod(env, false, g_mdClass, "reset", "()V");
    g_mdDigestMethod =          GetMethod(env, false, g_mdClass, "digest", "([B)[B");
    g_mdDigestCurrentMethodId = GetMethod(env, false, g_mdClass, "digest", "()[B");
    g_mdUpdateMethod =          GetMethod(env, false, g_mdClass, "update", "([B)V");

    g_macClass =                GetClassGRef(env, "javax/crypto/Mac");
    g_macGetInstanceMethod =    GetMethod(env, true,  g_macClass, "getInstance", "(Ljava/lang/String;)Ljavax/crypto/Mac;");
    g_macDoFinalMethod =        GetMethod(env, false, g_macClass, "doFinal", "()[B");
    g_macUpdateMethod =         GetMethod(env, false, g_macClass, "update", "([B)V");
    g_macInitMethod =           GetMethod(env, false, g_macClass, "init", "(Ljava/security/Key;)V");
    g_macResetMethod =          GetMethod(env, false, g_macClass, "reset", "()V");

    g_sksClass =                GetClassGRef(env, "javax/crypto/spec/SecretKeySpec");
    g_sksCtor =                 GetMethod(env, false, g_sksClass, "<init>", "([BLjava/lang/String;)V");

    g_cipherClass =             GetClassGRef(env, "javax/crypto/Cipher");
    g_cipherGetInstanceMethod = GetMethod(env, true,  g_cipherClass, "getInstance", "(Ljava/lang/String;)Ljavax/crypto/Cipher;");
    g_getBlockSizeMethod =      GetMethod(env, false, g_cipherClass, "getBlockSize", "()I");
    g_cipherDoFinalMethod =     GetMethod(env, false, g_cipherClass, "doFinal", "()[B");
    g_cipherDoFinal2Method =    GetMethod(env, false, g_cipherClass, "doFinal", "([B)[B");
    g_cipherUpdateMethod =      GetMethod(env, false, g_cipherClass, "update", "([B)[B");
    g_cipherUpdateAADMethod =   GetMethod(env, false, g_cipherClass, "updateAAD", "([B)V");
    g_cipherInitMethod =        GetMethod(env, false, g_cipherClass, "init", "(ILjava/security/Key;Ljava/security/spec/AlgorithmParameterSpec;)V");
    g_cipherInit2Method =       GetMethod(env, false, g_cipherClass, "init", "(ILjava/security/Key;)V");

    g_ivPsClass =               GetClassGRef(env, "javax/crypto/spec/IvParameterSpec");
    g_ivPsCtor =                GetMethod(env, false, g_ivPsClass, "<init>", "([B)V");

    g_GCMParameterSpecClass =   GetClassGRef(env, "javax/crypto/spec/GCMParameterSpec");
    g_GCMParameterSpecCtor =    GetMethod(env, false, g_GCMParameterSpecClass, "<init>", "(I[B)V");

    g_bigNumClass =             GetClassGRef(env, "java/math/BigInteger");
    g_bigNumCtor =              GetMethod(env, false, g_bigNumClass, "<init>", "([B)V");
    g_bigNumCtorWithSign =      GetMethod(env, false, g_bigNumClass, "<init>", "(I[B)V");
    g_toByteArrayMethod =       GetMethod(env, false, g_bigNumClass, "toByteArray", "()[B");
    g_valueOfMethod =           GetMethod(env, true, g_bigNumClass, "valueOf", "(J)Ljava/math/BigInteger;");
    g_intValueMethod =          GetMethod(env, false, g_bigNumClass, "intValue", "()I");
    g_compareToMethod =         GetMethod(env, false, g_bigNumClass, "compareTo", "(Ljava/math/BigInteger;)I");
    g_bitLengthMethod =         GetMethod(env, false, g_bigNumClass, "bitLength", "()I");
    g_sigNumMethod =            GetMethod(env, false, g_bigNumClass, "signum", "()I");

    g_sslParamsClass =              GetClassGRef(env, "javax/net/ssl/SSLParameters");
    g_sslParamsGetProtocolsMethod = GetMethod(env, false,  g_sslParamsClass, "getProtocols", "()[Ljava/lang/String;");

    g_sslCtxClass =                     GetClassGRef(env, "javax/net/ssl/SSLContext");
    g_sslCtxGetDefaultMethod =          GetMethod(env, true,  g_sslCtxClass, "getDefault", "()Ljavax/net/ssl/SSLContext;");
    g_sslCtxGetDefaultSslParamsMethod = GetMethod(env, false, g_sslCtxClass, "getDefaultSSLParameters", "()Ljavax/net/ssl/SSLParameters;");

    g_CertFactoryClass =                GetClassGRef(env, "java/security/cert/CertificateFactory");
    g_CertFactoryGetInstance =          GetMethod(env, true, g_CertFactoryClass, "getInstance", "(Ljava/lang/String;)Ljava/security/cert/CertificateFactory;");
    g_CertFactoryGenerateCertificate =  GetMethod(env, false, g_CertFactoryClass, "generateCertificate", "(Ljava/io/InputStream;)Ljava/security/cert/Certificate;");
    g_CertFactoryGenerateCertificates = GetMethod(env, false, g_CertFactoryClass, "generateCertificates", "(Ljava/io/InputStream;)Ljava/util/Collection;");
    g_CertFactoryGenerateCertPath =     GetMethod(env, false, g_CertFactoryClass, "generateCertPath", "(Ljava/io/InputStream;Ljava/lang/String;)Ljava/security/cert/CertPath;");
    g_CertFactoryGenerateCRL =          GetMethod(env, false, g_CertFactoryClass, "generateCRL", "(Ljava/io/InputStream;)Ljava/security/cert/CRL;");

    g_X509CertClass =                       GetClassGRef(env, "java/security/cert/X509Certificate");
    g_X509CertGetEncoded =                  GetMethod(env, false, g_X509CertClass, "getEncoded", "()[B");
    g_X509CertGetIssuerX500Principal =      GetMethod(env, false, g_X509CertClass, "getIssuerX500Principal", "()Ljavax/security/auth/x500/X500Principal;");
    g_X509CertGetNotAfter =                 GetMethod(env, false, g_X509CertClass, "getNotAfter", "()Ljava/util/Date;");
    g_X509CertGetNotBefore =                GetMethod(env, false, g_X509CertClass, "getNotBefore", "()Ljava/util/Date;");
    g_X509CertGetPublicKey =                GetMethod(env, false, g_X509CertClass, "getPublicKey", "()Ljava/security/PublicKey;");
    g_X509CertGetSerialNumber =             GetMethod(env, false, g_X509CertClass, "getSerialNumber", "()Ljava/math/BigInteger;");
    g_X509CertGetSigAlgOID =                GetMethod(env, false, g_X509CertClass, "getSigAlgOID", "()Ljava/lang/String;");
    g_X509CertGetSubjectX500Principal =     GetMethod(env, false, g_X509CertClass, "getSubjectX500Principal", "()Ljavax/security/auth/x500/X500Principal;");
    g_X509CertGetVersion =                  GetMethod(env, false, g_X509CertClass, "getVersion", "()I");

    g_X509CertGetCriticalExtensionOIDs =    GetMethod(env, false, g_X509CertClass, "getCriticalExtensionOIDs", "()Ljava/util/Set;");
    g_X509CertGetExtensionValue =           GetMethod(env, false, g_X509CertClass, "getExtensionValue", "(Ljava/lang/String;)[B");
    g_X509CertGetNonCriticalExtensionOIDs = GetMethod(env, false, g_X509CertClass, "getNonCriticalExtensionOIDs", "()Ljava/util/Set;");

    g_X509CRLClass          = GetClassGRef(env, "java/security/cert/X509CRL");
    g_X509CRLGetNextUpdate  = GetMethod(env, false, g_X509CRLClass, "getNextUpdate", "()Ljava/util/Date;");

    g_RSAKeyClass =                    GetClassGRef(env, "java/security/interfaces/RSAKey");
    g_RSAKeyGetModulus =               GetMethod(env, false, g_RSAKeyClass, "getModulus", "()Ljava/math/BigInteger;");

    g_RSAPublicKeyClass =              GetClassGRef(env, "java/security/interfaces/RSAPublicKey");
    g_RSAPublicKeyGetPubExpMethod =    GetMethod(env, false, g_RSAPublicKeyClass, "getPublicExponent", "()Ljava/math/BigInteger;");

    g_keyPairClass =                   GetClassGRef(env, "java/security/KeyPair");
    g_keyPairCtor =                    GetMethod(env, false, g_keyPairClass, "<init>", "(Ljava/security/PublicKey;Ljava/security/PrivateKey;)V");
    g_keyPairGetPrivateMethod =        GetMethod(env, false, g_keyPairClass, "getPrivate", "()Ljava/security/PrivateKey;");
    g_keyPairGetPublicMethod =         GetMethod(env, false, g_keyPairClass, "getPublic", "()Ljava/security/PublicKey;");

    g_keyPairGenClass =                      GetClassGRef(env, "java/security/KeyPairGenerator");
    g_keyPairGenGetInstanceMethod =          GetMethod(env, true,  g_keyPairGenClass, "getInstance", "(Ljava/lang/String;)Ljava/security/KeyPairGenerator;");
    g_keyPairGenInitializeMethod =           GetMethod(env, false, g_keyPairGenClass, "initialize", "(I)V");
    g_keyPairGenInitializeWithParamsMethod = GetMethod(env, false, g_keyPairGenClass, "initialize", "(Ljava/security/spec/AlgorithmParameterSpec;)V");
    g_keyPairGenGenKeyPairMethod =           GetMethod(env, false, g_keyPairGenClass, "genKeyPair", "()Ljava/security/KeyPair;");

    g_SignatureClass =                 GetClassGRef(env, "java/security/Signature");
    g_SignatureGetInstance =           GetMethod(env, true, g_SignatureClass, "getInstance", "(Ljava/lang/String;)Ljava/security/Signature;");
    g_SignatureInitSign =              GetMethod(env, false, g_SignatureClass, "initSign", "(Ljava/security/PrivateKey;)V");
    g_SignatureInitVerify =            GetMethod(env, false, g_SignatureClass, "initVerify", "(Ljava/security/PublicKey;)V");
    g_SignatureUpdate =                GetMethod(env, false, g_SignatureClass, "update", "([B)V");
    g_SignatureSign =                  GetMethod(env, false, g_SignatureClass, "sign", "()[B");
    g_SignatureVerify =                GetMethod(env, false, g_SignatureClass, "verify", "([B)Z");

    g_RSAPrivateCrtKeyClass =          GetClassGRef(env, "java/security/interfaces/RSAPrivateCrtKey");
    g_RSAPrivateCrtKeyPubExpField =    GetMethod(env, false, g_RSAPrivateCrtKeyClass, "getPublicExponent", "()Ljava/math/BigInteger;");
    g_RSAPrivateCrtKeyPrimePField =    GetMethod(env, false, g_RSAPrivateCrtKeyClass, "getPrimeP", "()Ljava/math/BigInteger;");
    g_RSAPrivateCrtKeyPrimeQField =    GetMethod(env, false, g_RSAPrivateCrtKeyClass, "getPrimeQ", "()Ljava/math/BigInteger;");
    g_RSAPrivateCrtKeyPrimeExpPField = GetMethod(env, false, g_RSAPrivateCrtKeyClass, "getPrimeExponentP", "()Ljava/math/BigInteger;");
    g_RSAPrivateCrtKeyPrimeExpQField = GetMethod(env, false, g_RSAPrivateCrtKeyClass, "getPrimeExponentQ", "()Ljava/math/BigInteger;");
    g_RSAPrivateCrtKeyCrtCoefField =   GetMethod(env, false, g_RSAPrivateCrtKeyClass, "getCrtCoefficient", "()Ljava/math/BigInteger;");
    g_RSAPrivateCrtKeyModulusField =   GetMethod(env, false, g_RSAPrivateCrtKeyClass, "getModulus", "()Ljava/math/BigInteger;");
    g_RSAPrivateCrtKeyPrivExpField =   GetMethod(env, false, g_RSAPrivateCrtKeyClass, "getPrivateExponent", "()Ljava/math/BigInteger;");

    g_RSAPrivateCrtKeySpecClass =      GetClassGRef(env, "java/security/spec/RSAPrivateCrtKeySpec");
    g_RSAPrivateCrtKeySpecCtor =       GetMethod(env, false, g_RSAPrivateCrtKeySpecClass, "<init>", "(Ljava/math/BigInteger;Ljava/math/BigInteger;Ljava/math/BigInteger;Ljava/math/BigInteger;Ljava/math/BigInteger;Ljava/math/BigInteger;Ljava/math/BigInteger;Ljava/math/BigInteger;)V");

    g_RSAPublicCrtKeySpecClass =       GetClassGRef(env, "java/security/spec/RSAPublicKeySpec");
    g_RSAPublicCrtKeySpecCtor =        GetMethod(env, false, g_RSAPublicCrtKeySpecClass, "<init>", "(Ljava/math/BigInteger;Ljava/math/BigInteger;)V");

    g_KeyFactoryClass =                GetClassGRef(env, "java/security/KeyFactory");
    g_KeyFactoryGetInstanceMethod =    GetMethod(env, true, g_KeyFactoryClass, "getInstance", "(Ljava/lang/String;)Ljava/security/KeyFactory;");
    g_KeyFactoryGenPrivateMethod =     GetMethod(env, false, g_KeyFactoryClass, "generatePrivate", "(Ljava/security/spec/KeySpec;)Ljava/security/PrivateKey;");
    g_KeyFactoryGenPublicMethod =      GetMethod(env, false, g_KeyFactoryClass, "generatePublic", "(Ljava/security/spec/KeySpec;)Ljava/security/PublicKey;");
    g_KeyFactoryGetKeySpecMethod =     GetMethod(env, false, g_KeyFactoryClass, "getKeySpec", "(Ljava/security/Key;Ljava/lang/Class;)Ljava/security/spec/KeySpec;");

    g_ECGenParameterSpecClass =        GetClassGRef(env, "java/security/spec/ECGenParameterSpec");
    g_ECGenParameterSpecCtor =         GetMethod(env, false, g_ECGenParameterSpecClass, "<init>", "(Ljava/lang/String;)V");

    g_ECFieldClass =                   GetClassGRef(env, "java/security/spec/ECField");
    g_ECFieldGetFieldSize =            GetMethod(env, false, g_ECFieldClass, "getFieldSize", "()I");

    g_ECFieldFpClass =                 GetClassGRef(env, "java/security/spec/ECFieldFp");
    g_ECFieldFpCtor =                  GetMethod(env, false, g_ECFieldFpClass, "<init>", "(Ljava/math/BigInteger;)V");
    g_ECFieldFpGetP =                  GetMethod(env, false, g_ECFieldFpClass, "getP", "()Ljava/math/BigInteger;");

    g_ECFieldF2mClass =                         GetClassGRef(env, "java/security/spec/ECFieldF2m");
    g_ECFieldF2mCtorWithCoefficientBigInteger = GetMethod(env, false, g_ECFieldF2mClass, "<init>", "(ILjava/math/BigInteger;)V");
    g_ECFieldF2mGetReductionPolynomial =        GetMethod(env, false, g_ECFieldF2mClass, "getReductionPolynomial", "()Ljava/math/BigInteger;");

    g_ECParameterSpecClass =           GetClassGRef(env, "java/security/spec/ECParameterSpec");
    g_ECParameterSpecCtor =            GetMethod(env, false, g_ECParameterSpecClass, "<init>", "(Ljava/security/spec/EllipticCurve;Ljava/security/spec/ECPoint;Ljava/math/BigInteger;I)V");
    g_ECParameterSpecGetCurve =        GetMethod(env, false, g_ECParameterSpecClass, "getCurve", "()Ljava/security/spec/EllipticCurve;");
    g_ECParameterSpecGetGenerator =    GetMethod(env, false, g_ECParameterSpecClass, "getGenerator", "()Ljava/security/spec/ECPoint;");
    g_ECParameterSpecGetCofactor =     GetMethod(env, false, g_ECParameterSpecClass, "getCofactor", "()I");
    g_ECParameterSpecGetOrder =        GetMethod(env, false, g_ECParameterSpecClass, "getOrder", "()Ljava/math/BigInteger;");
    g_ECParameterSpecGetCurveName =    GetOptionalMethod(env, false, g_ECParameterSpecClass, "getCurveName", "()Ljava/lang/String;");

    g_ECPointClass =                   GetClassGRef(env, "java/security/spec/ECPoint");
    g_ECPointCtor =                    GetMethod(env, false, g_ECPointClass, "<init>", "(Ljava/math/BigInteger;Ljava/math/BigInteger;)V");
    g_ECPointGetAffineX =              GetMethod(env, false, g_ECPointClass, "getAffineX", "()Ljava/math/BigInteger;");
    g_ECPointGetAffineY =              GetMethod(env, false, g_ECPointClass, "getAffineY", "()Ljava/math/BigInteger;");

    g_ECPrivateKeyClass =              GetClassGRef(env, "java/security/interfaces/ECPrivateKey");
    g_ECPrivateKeyGetS =               GetMethod(env, false, g_ECPrivateKeyClass, "getS", "()Ljava/math/BigInteger;");

    g_ECPrivateKeySpecClass =          GetClassGRef(env, "java/security/spec/ECPrivateKeySpec");
    g_ECPrivateKeySpecCtor =           GetMethod(env, false, g_ECPrivateKeySpecClass, "<init>", "(Ljava/math/BigInteger;Ljava/security/spec/ECParameterSpec;)V");

    g_ECPublicKeyClass =               GetClassGRef(env, "java/security/interfaces/ECPublicKey");
    g_ECPublicKeyGetW =                GetMethod(env, false, g_ECPublicKeyClass, "getW", "()Ljava/security/spec/ECPoint;");

    g_ECPublicKeySpecClass =           GetClassGRef(env, "java/security/spec/ECPublicKeySpec");
    g_ECPublicKeySpecCtor =            GetMethod(env, false, g_ECPublicKeySpecClass, "<init>", "(Ljava/security/spec/ECPoint;Ljava/security/spec/ECParameterSpec;)V");
    g_ECPublicKeySpecGetParams =       GetMethod(env, false, g_ECPublicKeySpecClass, "getParams", "()Ljava/security/spec/ECParameterSpec;");

    g_EllipticCurveClass =             GetClassGRef(env, "java/security/spec/EllipticCurve");
    g_EllipticCurveCtor =              GetMethod(env, false, g_EllipticCurveClass, "<init>", "(Ljava/security/spec/ECField;Ljava/math/BigInteger;Ljava/math/BigInteger;)V");
    g_EllipticCurveCtorWithSeed =      GetMethod(env, false, g_EllipticCurveClass, "<init>", "(Ljava/security/spec/ECField;Ljava/math/BigInteger;Ljava/math/BigInteger;[B)V");
    g_EllipticCurveGetA =              GetMethod(env, false, g_EllipticCurveClass, "getA", "()Ljava/math/BigInteger;");
    g_EllipticCurveGetB =              GetMethod(env, false, g_EllipticCurveClass, "getB", "()Ljava/math/BigInteger;");
    g_EllipticCurveGetField =          GetMethod(env, false, g_EllipticCurveClass, "getField", "()Ljava/security/spec/ECField;");
    g_EllipticCurveGetSeed =           GetMethod(env, false, g_EllipticCurveClass, "getSeed", "()[B");

    g_X509EncodedKeySpecClass =        GetClassGRef(env, "java/security/spec/X509EncodedKeySpec");
    g_X509EncodedKeySpecCtor =         GetMethod(env, false, g_X509EncodedKeySpecClass, "<init>", "([B)V");

    g_DestroyableClass =               GetClassGRef(env, "javax/security/auth/Destroyable");
    g_destroy =                        GetMethod(env, false, g_DestroyableClass, "destroy", "()V");

    g_CollectionClass =     GetClassGRef(env, "java/util/Collection");
    g_CollectionIterator =  GetMethod(env, false, g_CollectionClass, "iterator", "()Ljava/util/Iterator;");
    g_CollectionSize =      GetMethod(env, false, g_CollectionClass, "size", "()I");

    g_DateClass =   GetClassGRef(env, "java/util/Date");
    g_DateGetTime = GetMethod(env, false, g_DateClass, "getTime", "()J");

    g_IteratorClass =   GetClassGRef(env, "java/util/Iterator");
    g_IteratorHasNext = GetMethod(env, false, g_IteratorClass, "hasNext", "()Z");
    g_IteratorNext =    GetMethod(env, false, g_IteratorClass, "next", "()Ljava/lang/Object;");

    g_SetClass =    GetClassGRef(env, "java/util/Set");
    g_SetIterator = GetMethod(env, false, g_SetClass, "iterator", "()Ljava/util/Iterator;");

    g_NativeCryptoClass =              GetClassGRef(env, "com/android/org/conscrypt/NativeCrypto");

    g_SSLEngine =                         GetClassGRef(env, "javax/net/ssl/SSLEngine");
    g_SSLEngineSetUseClientModeMethod =   GetMethod(env, false, g_SSLEngine, "setUseClientMode", "(Z)V");
    g_SSLEngineGetSessionMethod =         GetMethod(env, false, g_SSLEngine, "getSession", "()Ljavax/net/ssl/SSLSession;");
    g_SSLEngineBeginHandshakeMethod =     GetMethod(env, false, g_SSLEngine, "beginHandshake", "()V");
    g_SSLEngineWrapMethod =               GetMethod(env, false, g_SSLEngine, "wrap", "(Ljava/nio/ByteBuffer;Ljava/nio/ByteBuffer;)Ljavax/net/ssl/SSLEngineResult;");
    g_SSLEngineUnwrapMethod =             GetMethod(env, false, g_SSLEngine, "unwrap", "(Ljava/nio/ByteBuffer;Ljava/nio/ByteBuffer;)Ljavax/net/ssl/SSLEngineResult;");
    g_SSLEngineGetHandshakeStatusMethod = GetMethod(env, false, g_SSLEngine, "getHandshakeStatus", "()Ljavax/net/ssl/SSLEngineResult$HandshakeStatus;");
    g_SSLEngineCloseInboundMethod =       GetMethod(env, false, g_SSLEngine, "closeInbound", "()V");
    g_SSLEngineCloseOutboundMethod =      GetMethod(env, false, g_SSLEngine, "closeOutbound", "()V");

    g_ByteBuffer =                        GetClassGRef(env, "java/nio/ByteBuffer");
    g_ByteBufferAllocateMethod =          GetMethod(env, true,  g_ByteBuffer, "allocate", "(I)Ljava/nio/ByteBuffer;");
    g_ByteBufferPutMethod =               GetMethod(env, false, g_ByteBuffer, "put", "(Ljava/nio/ByteBuffer;)Ljava/nio/ByteBuffer;");
    g_ByteBufferPut2Method =              GetMethod(env, false, g_ByteBuffer, "put", "([B)Ljava/nio/ByteBuffer;");
    g_ByteBufferPut3Method =              GetMethod(env, false, g_ByteBuffer, "put", "([BII)Ljava/nio/ByteBuffer;");
    g_ByteBufferFlipMethod =              GetMethod(env, false, g_ByteBuffer, "flip", "()Ljava/nio/Buffer;");
    g_ByteBufferLimitMethod =             GetMethod(env, false, g_ByteBuffer, "limit", "()I");
    g_ByteBufferGetMethod =               GetMethod(env, false, g_ByteBuffer, "get", "([B)Ljava/nio/ByteBuffer;");
    g_ByteBufferPutBufferMethod =         GetMethod(env, false, g_ByteBuffer, "put", "(Ljava/nio/ByteBuffer;)Ljava/nio/ByteBuffer;");
    g_ByteBufferRemainingMethod =         GetMethod(env, false, g_ByteBuffer, "remaining", "()I");
    g_ByteBufferCompactMethod =           GetMethod(env, false, g_ByteBuffer, "compact", "()Ljava/nio/ByteBuffer;");
    g_ByteBufferPositionMethod =          GetMethod(env, false, g_ByteBuffer, "position", "()I");

    g_SSLContext =                        GetClassGRef(env, "javax/net/ssl/SSLContext");
    g_SSLContextGetInstanceMethod =       GetMethod(env, true,  g_SSLContext, "getInstance", "(Ljava/lang/String;)Ljavax/net/ssl/SSLContext;");
    g_SSLContextInitMethod =              GetMethod(env, false, g_SSLContext, "init", "([Ljavax/net/ssl/KeyManager;[Ljavax/net/ssl/TrustManager;Ljava/security/SecureRandom;)V");
    g_SSLContextCreateSSLEngineMethod =   GetMethod(env, false, g_SSLContext, "createSSLEngine", "()Ljavax/net/ssl/SSLEngine;");

    g_SSLSession =                               GetClassGRef(env, "javax/net/ssl/SSLSession");
    g_SSLSessionGetApplicationBufferSizeMethod = GetMethod(env, false, g_SSLSession, "getApplicationBufferSize", "()I");
    g_SSLSessionGetPacketBufferSizeMethod =      GetMethod(env, false, g_SSLSession, "getPacketBufferSize", "()I");

    g_SSLEngineResult =                          GetClassGRef(env, "javax/net/ssl/SSLEngineResult");
    g_SSLEngineResultGetStatusMethod =           GetMethod(env, false, g_SSLEngineResult, "getStatus", "()Ljavax/net/ssl/SSLEngineResult$Status;");
    g_SSLEngineResultGetHandshakeStatusMethod =  GetMethod(env, false, g_SSLEngineResult, "getHandshakeStatus", "()Ljavax/net/ssl/SSLEngineResult$HandshakeStatus;");

    g_TrustManager =                             GetClassGRef(env, "javax/net/ssl/TrustManager");

    g_X500PrincipalClass =      GetClassGRef(env, "javax/security/auth/x500/X500Principal");
    g_X500PrincipalGetEncoded = GetMethod(env, false, g_X500PrincipalClass, "getEncoded", "()[B");
    g_X500PrincipalHashCode =   GetMethod(env, false, g_X500PrincipalClass, "hashCode", "()I");

    g_KeyAgreementClass          = GetClassGRef(env, "javax/crypto/KeyAgreement");
    g_KeyAgreementGetInstance    = GetMethod(env, true, g_KeyAgreementClass, "getInstance", "(Ljava/lang/String;)Ljavax/crypto/KeyAgreement;");
    g_KeyAgreementInit           = GetMethod(env, false, g_KeyAgreementClass, "init", "(Ljava/security/Key;)V");
    g_KeyAgreementDoPhase        = GetMethod(env, false, g_KeyAgreementClass, "doPhase", "(Ljava/security/Key;Z)Ljava/security/Key;");
    g_KeyAgreementGenerateSecret = GetMethod(env, false, g_KeyAgreementClass, "generateSecret", "()[B");

    return JNI_VERSION_1_6;
}
