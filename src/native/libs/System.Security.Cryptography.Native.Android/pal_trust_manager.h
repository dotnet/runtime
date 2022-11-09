#include "pal_jni.h"

typedef bool (*RemoteCertificateValidationCallback)(intptr_t, int32_t, jobject*);

PALEXPORT void AndroidCryptoNative_RegisterTrustManagerCallback(RemoteCertificateValidationCallback callback);

jobjectArray InitTrustManagersWithCustomValidatorProxy(JNIEnv* env, intptr_t dotnetHandle);

JNIEXPORT jboolean JNICALL Java_net_dot_android_crypto_RemoteCertificateVerificationProxyTrustManager_verifyRemoteCertificate(
    JNIEnv *env, jobject thisHandle, intptr_t dotnetHandle, jobjectArray certificates);
