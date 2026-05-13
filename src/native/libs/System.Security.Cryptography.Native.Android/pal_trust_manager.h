#include "pal_jni.h"

typedef bool (*RemoteCertificateValidationCallback)(intptr_t, int32_t);

PALEXPORT void AndroidCryptoNative_RegisterRemoteCertificateValidationCallback(RemoteCertificateValidationCallback callback);

jobjectArray GetTrustManagers(JNIEnv* env, intptr_t sslStreamProxyHandle, const char* targetHost, jobject* trustCerts, int32_t trustCertsLen);

jboolean DotnetProxyTrustManager_VerifyRemoteCertificate(
    JNIEnv *env, jobject thisHandle, jlong sslStreamProxyHandle, jboolean chainTrustedByPlatform);
