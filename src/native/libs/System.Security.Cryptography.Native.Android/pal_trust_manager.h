#include "pal_jni.h"

typedef bool (*ValidationCallback)(intptr_t, uint8_t**, int32_t*, int32_t, int32_t);

PALEXPORT void AndroidCryptoNative_RegisterTrustManagerValidationCallback(ValidationCallback callback);

jobjectArray init_trust_managers_with_custom_validator(JNIEnv* env, intptr_t csharpObjectHandle);

JNIEXPORT jboolean JNICALL
Java_net_dot_android_crypto_TrustManagerProxy_validateRemoteCertificate(
    JNIEnv *env,
    jobject trustManagerProxy,
    intptr_t csharpObjectHandle,
    jobjectArray certificates,
    int errors);
