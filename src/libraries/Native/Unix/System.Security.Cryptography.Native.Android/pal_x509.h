#include "pal_jni.h"

// Creation
PALEXPORT jobject /*X509Certificate*/ CryptoNative_DecodeX509(const uint8_t *buf, int32_t len);
PALEXPORT int32_t CryptoNative_EncodeX509(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len);
PALEXPORT void CryptoNative_X509Destroy(jobject /*X509Certificate*/ cert);
PALEXPORT jobject /*X509Certificate*/ CryptoNative_X509UpRef(jobject /*X509Certificate*/ cert);

// Basic properties

PALEXPORT int32_t CryptoNative_GetX509Thumbprint(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len);
PALEXPORT int32_t CryptoNative_GetX509NameRawBytes(jobject /*X500Principal*/ name, uint8_t *pBuf, int32_t cBuf);
PALEXPORT int64_t CryptoNative_GetX509NotBefore(jobject /*X509Certificate*/ cert);
PALEXPORT int64_t CryptoNative_GetX509NotAfter(jobject /*X509Certificate*/ cert);
PALEXPORT int32_t CryptoNative_GetX509PublicKeyAlgorithm(jobject /*X509Certificate*/ cert, char *buf, int32_t len);
PALEXPORT int32_t CryptoNative_GetX509PublicKeyParameterBytes(jobject /*X509Certificate*/ cert, uint8_t *pBuf, int32_t cBuf);
PALEXPORT int32_t CryptoNative_GetX509PublicKeyBytes(jobject /*X509Certificate*/ cert, uint8_t *pBuf, int32_t cBuf);
PALEXPORT int32_t CryptoNative_GetX509SignatureAlgorithm(jobject /*X509Certificate*/ cert, char *buf, int32_t len);
PALEXPORT int32_t CryptoNative_GetX509Version(jobject /*X509Certificate*/ cert);

PALEXPORT jobject /*X500Principal*/ CryptoNative_X509GetIssuerName(jobject /*X509Certificate*/ cert);
PALEXPORT int32_t CryptoNative_X509GetSerialNumber(jobject /*X509Certificate*/ cert, uint8_t* buf, int32_t len);
PALEXPORT jobject /*X500Principal*/ CryptoNative_X509GetSubjectName(jobject /*X509Certificate*/ cert);
PALEXPORT uint64_t CryptoNative_X509IssuerNameHash(jobject /*X509Certificate*/ cert);

typedef void (*EnumX509ExtensionsCallback)(const char *oid, int32_t oid_len, const uint8_t *data, int32_t data_len, bool isCritical, void *context);

/* Enumerate all extensions */
PALEXPORT int32_t AndroidCryptoNative_X509EnumExtensions(jobject /*X509Certificate*/ cert, EnumX509ExtensionsCallback cb, void *context);
PALEXPORT int32_t AndroidCryptoNative_X509FindExtensionData(jobject /*X509Certificate*/ cert, const char *oid, uint8_t *buf, int32_t len);

// X509 CRL
PALEXPORT jobject /*X509CRL*/ CryptoNative_DecodeX509Crl(const uint8_t* buf, int32_t len);
PALEXPORT long CryptoNative_GetX509CrlNextUpdate(jobject /*X509CRL*/ crl);
PALEXPORT void CryptoNative_X509CrlDestroy(jobject /*X509CRL*/ crl);
