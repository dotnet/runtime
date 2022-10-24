#include "pal_jni.h"

typedef bool (*ValidationCallback)(intptr_t, int32_t, int32_t*, uint8_t**);

PALEXPORT void AndroidCryptoNative_RegisterTrustManagerValidationCallback(ValidationCallback callback);

jobjectArray initTrustManagersWithCustomValidatorProxy(JNIEnv* env, intptr_t dotnetValidatorHandle);

JNIEXPORT jboolean JNICALL Java_net_dot_android_crypto_DotnetProxyTrustManager_validateRemoteCertificate(
    JNIEnv *env,
    jobject handle,
    intptr_t dotnetValidatorHandle,
    jobjectArray certificates);
