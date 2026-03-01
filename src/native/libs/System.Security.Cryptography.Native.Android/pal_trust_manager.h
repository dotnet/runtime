#include "pal_jni.h"

typedef bool (*RemoteCertificateValidationCallback)(intptr_t, int32_t);

PALEXPORT void AndroidCryptoNative_RegisterRemoteCertificateValidationCallback(RemoteCertificateValidationCallback callback);

jobjectArray GetTrustManagers(JNIEnv* env, intptr_t sslStreamProxyHandle, const char* targetHost, jobject* trustCerts, int32_t trustCertsLen);

JNIEXPORT jboolean JNICALL Java_net_dot_android_crypto_DotnetProxyTrustManager_verifyRemoteCertificate(
    JNIEnv *env, jobject thisHandle, jlong sslStreamProxyHandle, jboolean chainTrustedByPlatform);

PALEXPORT int32_t AndroidCryptoNative_IsCleartextTrafficPermitted(const char* hostname);
PALEXPORT int32_t AndroidCryptoNative_IsCertificateTrustedForHost(const uint8_t* certDer, int32_t certDerLen, const char* hostname);
