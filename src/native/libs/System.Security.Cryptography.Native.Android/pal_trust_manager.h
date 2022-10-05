#include "pal_jni.h"

typedef bool (*ValidationCallback)(intptr_t, int32_t, int32_t*, uint8_t**, bool);

PALEXPORT void AndroidCryptoNative_RegisterTrustManagerValidationCallback(ValidationCallback callback);

jobjectArray initTrustManagersWithCustomValidatorProxy(
    JNIEnv* env,
    intptr_t dotnetRemoteCertificateValidatorHandle,
    char* targetHostName);

JNIEXPORT jboolean JNICALL
Java_net_dot_android_crypto_RemoteCertificateValidationCallbackProxy_validateRemoteCertificate(
    JNIEnv *env,
    jobject RemoteCertificateValidationCallbackProxy,
    intptr_t dotnetRemoteCertificateValidatorHandle,
    jobjectArray certificates,
    int32_t errors);
