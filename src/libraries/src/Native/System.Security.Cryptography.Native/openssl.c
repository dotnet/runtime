//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include <assert.h>
#include <limits.h>
#include <pthread.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <unistd.h>
#include <openssl/asn1.h>
#include <openssl/bio.h>
#include <openssl/evp.h>
#include <openssl/x509.h>
#include <openssl/x509v3.h>

// See X509NameType.SimpleName
#define NAME_TYPE_SIMPLE 0
// See X509NameType.EmailName
#define NAME_TYPE_EMAIL 1
// See X509NameType.UpnName
#define NAME_TYPE_UPN 2
// See X509NameType.DnsName
#define NAME_TYPE_DNS 3
// See X509NameType.DnsFromAlternateName
#define NAME_TYPE_DNSALT 4
// See X509NameType.UrlName
#define NAME_TYPE_URL 5

/*
Function:
MakeTimeT

Used to convert the constituent elements of a struct tm into a time_t. As time_t does not have
a guaranteed blitting size, this function is static and cannot be p/invoked. It is here merely
as a utility.

Return values:
A time_t representation of the input date. See also man mktime(3).
*/
static
time_t
MakeTimeT(
    int year,
    int month,
    int day,
    int hour,
    int minute,
    int second,
    int isDst)
{
    struct tm currentTm;
    currentTm.tm_year = year - 1900;
    currentTm.tm_mon = month - 1;
    currentTm.tm_mday = day;
    currentTm.tm_hour = hour;
    currentTm.tm_min = minute;
    currentTm.tm_sec = second;
    currentTm.tm_isdst = isDst;
    return mktime(&currentTm);
}

/*
Function:
GetX509Thumbprint

Used by System.Security.Cryptography.X509Certificates' OpenSslX509CertificateReader to copy the SHA1
digest of the certificate (the thumbprint) into a managed buffer.

Return values:
0: Invalid X509 pointer
1: Data was copied
Any negative value: The input buffer size was reported as insufficient. A buffer of size ABS(return) is required.
*/
int
GetX509Thumbprint(
    X509* x509,
    unsigned char* pBuf,
    int cBuf)
{
    if (!x509)
    {
        return 0;
    }

    if (cBuf < SHA_DIGEST_LENGTH)
    {
        return -SHA_DIGEST_LENGTH;
    }

    memcpy(pBuf, x509->sha1_hash, SHA_DIGEST_LENGTH);
    return 1;
}

/*
Function:
GetX509NotBefore

Used by System.Security.Cryptography.X509Certificates' OpenSslX509CertificateReader to identify the
beginning of the validity period of the certificate in question.

Return values:
NULL if the validity cannot be determined, a pointer to the ASN1_TIME structure for the NotBefore value
otherwise.
*/
ASN1_TIME*
GetX509NotBefore(
    X509* x509)
{
    if (x509 && x509->cert_info && x509->cert_info->validity)
    {
        return x509->cert_info->validity->notBefore;
    }

    return NULL;
}

/*
Function:
GetX509NotAfter

Used by System.Security.Cryptography.X509Certificates' OpenSslX509CertificateReader to identify the
end of the validity period of the certificate in question.

Return values:
NULL if the validity cannot be determined, a pointer to the ASN1_TIME structure for the NotAfter value
otherwise.
*/
ASN1_TIME*
GetX509NotAfter(
    X509* x509)
{
    if (x509 && x509->cert_info && x509->cert_info->validity)
    {
        return x509->cert_info->validity->notAfter;
    }

    return NULL;
}

/*
Function:
GetX509CrlNextUpdate

Used by System.Security.Cryptography.X509Certificates' CrlCache to identify the
end of the validity period of the certificate revocation list in question.

Return values:
NULL if the validity cannot be determined, a pointer to the ASN1_TIME structure for the NextUpdate value
otherwise.
*/
ASN1_TIME*
GetX509CrlNextUpdate(
    X509_CRL* crl)
{
    if (crl)
    {
        return X509_CRL_get_nextUpdate(crl);
    }

    return NULL;
}

/*
Function:
GetX509Version

Used by System.Security.Cryptography.X509Certificates' OpenSslX509CertificateReader to identify the
X509 data format version for this certificate.

Return values:
-1 if the value cannot be determined
The encoded value of the version, otherwise:
  0: X509v1
  1: X509v2
  2: X509v3
*/
int
GetX509Version(
    X509* x509)
{
    if (x509 && x509->cert_info)
    {
        long ver = ASN1_INTEGER_get(x509->cert_info->version);
        return (int)ver;
    }

    return -1;
}

/*
Function:
GetX509PublicKeyAlgorithm

Used by System.Security.Cryptography.X509Certificates' OpenSslX509CertificateReader to identify the
algorithm the public key is associated with.

Return values:
NULL if the algorithm cannot be determined, otherwise a pointer to the OpenSSL ASN1_OBJECT structure
describing the object type.
*/
ASN1_OBJECT*
GetX509PublicKeyAlgorithm(
    X509* x509)
{
    if (x509 && x509->cert_info && x509->cert_info->key && x509->cert_info->key->algor)
    {
        return x509->cert_info->key->algor->algorithm;
    }

    return NULL;
}

/*
Function:
GetX509SignatureAlgorithm

Used by System.Security.Cryptography.X509Certificates' OpenSslX509CertificateReader to identify the
algorithm used by the Certificate Authority for signing the certificate.

Return values:
NULL if the algorithm cannot be determined, otherwise a pointer to the OpenSSL ASN1_OBJECT structure
describing the object type.
*/
ASN1_OBJECT*
GetX509SignatureAlgorithm(
    X509* x509)
{
    if (x509 && x509->sig_alg && x509->sig_alg->algorithm)
    {
        return x509->sig_alg->algorithm;
    }

    return NULL;
}

/*
Function:
GetX509PublicKeyParameterBytes

Used by System.Security.Cryptography.X509Certificates' OpenSslX509CertificateReader to copy out the
parameters to the algorithm used by the certificate public key

Return values:
0: Invalid X509 pointer
1: Data was copied
Any negative value: The input buffer size was reported as insufficient. A buffer of size ABS(return) is required.
*/
int
GetX509PublicKeyParameterBytes(
    X509* x509,
    unsigned char* pBuf,
    int cBuf)
{
    if (!x509 || !x509->cert_info || !x509->cert_info->key || !x509->cert_info->key->algor)
    {
        return 0;
    }

    ASN1_TYPE* parameter = x509->cert_info->key->algor->parameter;
    int len = i2d_ASN1_TYPE(parameter, NULL);

    if (cBuf < len)
    {
        return -len;
    }

    unsigned char* pBuf2 = pBuf;
    len = i2d_ASN1_TYPE(parameter, &pBuf2);

    if (len > 0)
    {
        return 1;
    }

    return 0;
}

/*
Function:
GetX509PublicKeyBytes

Used by System.Security.Cryptography.X509Certificates' OpenSslX509CertificateReader to obtain the
raw bytes of the public key.

Return values:
NULL if the public key cannot be determined, a pointer to the ASN1_BIT_STRING structure representing
the public key.
*/
ASN1_BIT_STRING*
GetX509PublicKeyBytes(
    X509* x509)
{
    if (x509 && x509->cert_info && x509->cert_info->key)
    {
        return x509->cert_info->key->public_key;
    }

    return NULL;
}

/*
Function:
GetAsn1StringBytes

Used by the NativeCrypto shim type to extract byte[] data from OpenSSL ASN1_* types whenever a byte[] is called
for in managed code.

Return values:
0: Invalid X509 pointer
1: Data was copied
Any negative value: The input buffer size was reported as insufficient. A buffer of size ABS(return) is required.

Remarks:
 Many ASN1 types are actually the same type in OpenSSL:
   STRING
   INTEGER
   ENUMERATED
   BIT_STRING
   OCTET_STRING
   PRINTABLESTRING
   T61STRING
   IA5STRING
   GENERALSTRING
   UNIVERSALSTRING
   BMPSTRING
   UTCTIME
   TIME
   GENERALIZEDTIME
   VISIBLEStRING
   UTF8STRING

 So this function will really work on all of them.
*/
int
GetAsn1StringBytes(
    ASN1_STRING* asn1,
    unsigned char* pBuf,
    int cBuf)
{
    if (!asn1 || cBuf < 0)
    {
        return 0;
    }

    int length = asn1->length;
    assert(length >= 0);
    if (length < 0)
    {
        return 0;
    }

    if (!pBuf || cBuf < length)
    {
        return -length;
    }

    memcpy(pBuf, asn1->data, (unsigned int)length);
    return 1;
}

/*
Function:
GetX509NameRawBytes

Used by System.Security.Cryptography.X509Certificates' OpenSslX509CertificateReader to obtain the
DER encoded value of an X500DistinguishedName.

Return values:
0: Invalid X509 pointer
1: Data was copied
Any negative value: The input buffer size was reported as insufficient. A buffer of size ABS(return) is required.
*/
int
GetX509NameRawBytes(
    X509_NAME* x509Name,
    unsigned char* pBuf,
    int cBuf)
{
    if (!x509Name || !x509Name->bytes || cBuf < 0)
    {
        return 0;
    }

    /* 
     * length is size_t on some platforms and int on others, so the comparisons
     * are not tautological everywhere. We can let the compiler optimize away
     * any part of the check that is. We split the size checks into two checks
     * so we can get around the warnings on Linux where the Length is unsigned
     * whereas Length is signed on OS X. The first check makes sure the variable 
     * value is less than INT_MAX in it's native format; once we know it is not
     * too large, we can safely cast to an int to make sure it is not negative
     */
    if (x509Name->bytes->length > INT_MAX)
    {
        assert(0 && "Huge length X509_NAME");
        return 0;
    }

    int length = (int)(x509Name->bytes->length);

    if (length < 0)
    {
        assert(0 && "Negative length X509_NAME");
        return 0;
    }

    if (!pBuf || cBuf < length)
    {
        return -length;
    }
    
    memcpy(pBuf, x509Name->bytes->data, (unsigned int)length);
    return 1;
}

/*
Function:
GetX509EkuFieldCount

Used by System.Security.Cryptography.X509Certificates' OpenSslX509Encoder to identify the
number of Extended Key Usage OIDs present in the EXTENDED_KEY_USAGE structure.

Return values:
0 if the field count cannot be determined, or the count of OIDs present in the EKU.
Note that 0 does not always indicate an error, merely that GetX509EkuField should not be called.
*/
int
GetX509EkuFieldCount(
    EXTENDED_KEY_USAGE* eku)
{
    return sk_ASN1_OBJECT_num(eku);
}

/*
Function:
GetX509EkuField

Used by System.Security.Cryptography.X509Certificates' OpenSslX509Encoder to get a pointer to the
ASN1_OBJECT structure which represents the OID in a particular spot in the EKU.

Return values:
NULL if eku is NULL or loc is out of bounds, otherwise a pointer to the ASN1_OBJECT structure encoding
that particular OID.
*/
ASN1_OBJECT*
GetX509EkuField(
    EXTENDED_KEY_USAGE* eku,
    int loc)
{
    return sk_ASN1_OBJECT_value(eku, loc);
}

/*
Function:
GetX509NameInfo

Used by System.Security.Cryptography.X509Certificates' OpenSslX509CertificateReader as the entire
implementation of X509Certificate2.GetNameInfo.

Return values:
NULL if the certificate is invalid or no name information could be found, otherwise a pointer to a
memory-backed BIO structure which contains the answer to the GetNameInfo query
*/
BIO*
GetX509NameInfo(
    X509* x509,
    int nameType,
    int forIssuer)
{
    static const char szOidUpn[] = "1.3.6.1.4.1.311.20.2.3";

    if (!x509 || !x509->cert_info || nameType < NAME_TYPE_SIMPLE || nameType > NAME_TYPE_URL)
    {
        return NULL;
    }

    // Algorithm behaviors (pseudocode).  When forIssuer is true, replace "Subject" with "Issuer" and
    // SAN (Subject Alternative Names) with IAN (Issuer Alternative Names).
    //
    // SimpleName: Subject[CN] ?? Subject[OU] ?? Subject[O] ?? Subject[E] ?? Subject.Rdns.FirstOrDefault() ?? SAN.Entries.FirstOrDefault(type == GEN_EMAIL);
    // EmailName: SAN.Entries.FirstOrDefault(type == GEN_EMAIL) ?? Subject[E];
    // UpnName: SAN.Entries.FirsOrDefaultt(type == GEN_OTHER && entry.AsOther().OID == szOidUpn).AsOther().Value;
    // DnsName: SAN.Entries.FirstOrDefault(type == GEN_DNS) ?? Subject[CN];
    // DnsFromAlternativeName: SAN.Entries.FirstOrDefault(type == GEN_DNS);
    // UrlName: SAN.Entries.FirstOrDefault(type == GEN_URI);
    if (nameType == NAME_TYPE_SIMPLE)
    {
        X509_NAME* name = forIssuer ? x509->cert_info->issuer : x509->cert_info->subject;

        if (name)
        {
            ASN1_STRING* cn = NULL;
            ASN1_STRING* ou = NULL;
            ASN1_STRING* o = NULL;
            ASN1_STRING* e = NULL;
            ASN1_STRING* firstRdn = NULL;

            // Walk the list backwards because it is stored in stack order
            for (int i = X509_NAME_entry_count(name) - 1; i >= 0; --i)
            {
                X509_NAME_ENTRY* entry = X509_NAME_get_entry(name, i);

                if (!entry)
                {
                    continue;
                }

                ASN1_OBJECT* oid = X509_NAME_ENTRY_get_object(entry);
                ASN1_STRING* str = X509_NAME_ENTRY_get_data(entry);

                if (!oid || !str)
                {
                    continue;
                }

                int nid = OBJ_obj2nid(oid);

                if (nid == NID_commonName)
                {
                    // CN wins, so no need to keep looking.
                    cn = str;
                    break;
                }
                else if (nid == NID_organizationalUnitName)
                {
                    ou = str;
                }
                else if (nid == NID_organizationName)
                {
                    o = str;
                }
                else if (nid == NID_pkcs9_emailAddress)
                {
                    e = str;
                }
                else if (!firstRdn)
                {
                    firstRdn = str;
                }
            }

            ASN1_STRING* answer = cn;

            // If there was no CN, but there was something, then perform fallbacks.
            if (!answer && firstRdn)
            {
                answer = ou;

                if (!answer)
                {
                    answer = o;
                }

                if (!answer)
                {
                    answer = e;
                }

                if (!answer)
                {
                    answer = firstRdn;
                }
            }

            if (answer)
            {
                BIO* b = BIO_new(BIO_s_mem());
                ASN1_STRING_print_ex(b, answer, 0);
                return b;
            }
        }
    }

    if (nameType == NAME_TYPE_SIMPLE ||
        nameType == NAME_TYPE_DNS ||
        nameType == NAME_TYPE_DNSALT ||
        nameType == NAME_TYPE_EMAIL ||
        nameType == NAME_TYPE_UPN ||
        nameType == NAME_TYPE_URL)
    {
        int expectedType = -1;

        switch (nameType)
        {
            case NAME_TYPE_DNS:
            case NAME_TYPE_DNSALT:
                expectedType = GEN_DNS;
                break;
            case NAME_TYPE_SIMPLE:
            case NAME_TYPE_EMAIL:
                expectedType = GEN_EMAIL;
                break;
            case NAME_TYPE_UPN:
                expectedType = GEN_OTHERNAME;
                break;
            case NAME_TYPE_URL:
                expectedType = GEN_URI;
                break;
        }

        STACK_OF(GENERAL_NAME)* altNames =
            X509_get_ext_d2i(x509, forIssuer ? NID_issuer_alt_name : NID_subject_alt_name, NULL, NULL);

        if (altNames)
        {
            int i;

            for (i = 0; i < sk_GENERAL_NAME_num(altNames); ++i)
            {
                GENERAL_NAME* altName = sk_GENERAL_NAME_value(altNames, i);

                if (altName && altName->type == expectedType)
                {
                    ASN1_STRING* str = NULL;

                    switch (nameType)
                    {
                        case NAME_TYPE_DNS:
                        case NAME_TYPE_DNSALT:
                            str = altName->d.dNSName;
                            break;
                        case NAME_TYPE_SIMPLE:
                        case NAME_TYPE_EMAIL:
                            str = altName->d.rfc822Name;
                            break;
                        case NAME_TYPE_URL:
                            str = altName->d.uniformResourceIdentifier;
                            break;
                        case NAME_TYPE_UPN:
                        {
                            OTHERNAME* value = altName->d.otherName;

                            if (value)
                            {
                                // Enough more padding than szOidUpn that a \0 won't accidentally align
                                char localOid[sizeof(szOidUpn) + 3];
                                int cchLocalOid = 1 + OBJ_obj2txt(localOid, sizeof(localOid), value->type_id, 1);

                                if (sizeof(szOidUpn) == cchLocalOid &&
                                    0 == strncmp(localOid, szOidUpn, sizeof(szOidUpn)))
                                {
                                    //OTHERNAME->ASN1_TYPE->union.field
                                    str = value->value->value.asn1_string;
                                }
                            }

                            break;
                        }
                    }

                    if (str)
                    {
                        BIO* b = BIO_new(BIO_s_mem());
                        ASN1_STRING_print_ex(b, str, 0);
                        sk_GENERAL_NAME_free(altNames);
                        return b;
                    }
                }
            }

            sk_GENERAL_NAME_free(altNames);
        }
    }

    if (nameType == NAME_TYPE_EMAIL ||
        nameType == NAME_TYPE_DNS)
    {
        X509_NAME* name = forIssuer ? x509->cert_info->issuer : x509->cert_info->subject;
        int expectedNid = NID_undef;

        switch (nameType)
        {
            case NAME_TYPE_EMAIL:
                expectedNid = NID_pkcs9_emailAddress;
                break;
            case NAME_TYPE_DNS:
                expectedNid = NID_commonName;
                break;
        }

        if (name)
        {
            // Walk the list backwards because it is stored in stack order
            for (int i = X509_NAME_entry_count(name) - 1; i >= 0; --i)
            {
                X509_NAME_ENTRY* entry = X509_NAME_get_entry(name, i);

                if (!entry)
                {
                    continue;
                }

                ASN1_OBJECT* oid = X509_NAME_ENTRY_get_object(entry);
                ASN1_STRING* str = X509_NAME_ENTRY_get_data(entry);

                if (!oid || !str)
                {
                    continue;
                }

                int nid = OBJ_obj2nid(oid);

                if (nid == expectedNid)
                {
                    BIO* b = BIO_new(BIO_s_mem());
                    ASN1_STRING_print_ex(b, str, 0);
                    return b;
                }
            }
        }
    }

    return NULL;
}

/*
Function:
GetX509StackFieldCount

Used by System.Security.Cryptography.X509Certificates' OpenSslX509ChainProcessor to identify the
number of certificates returned in the built chain.

Return values:
0 if the field count cannot be determined, or the count of certificates in STACK_OF(X509)
Note that 0 does not always indicate an error, merely that GetX509StackField should not be called.
*/
int
GetX509StackFieldCount(
    STACK_OF(X509)* stack)
{
    return sk_X509_num(stack);
}

/*
Function:
GetX509StackField

Used by System.Security.Cryptography.X509Certificates' OpenSslX509ChainProcessor to get a pointer to
the indexed member of a chain.

Return values:
NULL if stack is NULL or loc is out of bounds, otherwise a pointer to the X509 structure encoding
that particular element.
*/
X509*
GetX509StackField(
    STACK_OF(X509)* stack,
    int loc)
{
    return sk_X509_value(stack, loc);
}

/*
Function:
RecursiveFreeX509Stack

Used by System.Security.Cryptography.X509Certificates' OpenSslX509ChainProcessor to free a stack
when done with it.
*/
void
RecursiveFreeX509Stack(
    STACK_OF(X509)* stack)
{
    sk_X509_pop_free(stack, X509_free);
}

/*
Function:
SetX509ChainVerifyTime

Used by System.Security.Cryptography.X509Certificates' OpenSslX509ChainProcessor to assign the
verification time to the chain building.  The input is in LOCAL time, not UTC.

Return values:
0 if ctx is NULL, if ctx has no X509_VERIFY_PARAM, or the date inputs don't produce a valid time_t;
1 on success.
*/
int
SetX509ChainVerifyTime(
    X509_STORE_CTX* ctx,
    int year,
    int month,
    int day,
    int hour,
    int minute,
    int second,
    int isDst)
{
    if (!ctx)
    {
        return 0;
    }

    time_t verifyTime = MakeTimeT(year, month, day, hour, minute, second, isDst);

    if (verifyTime == (time_t)-1)
    {
        return 0;
    }

    X509_VERIFY_PARAM* verifyParams = X509_STORE_CTX_get0_param(ctx);

    if (!verifyParams)
    {
        return 0;
    }

    X509_VERIFY_PARAM_set_time(verifyParams, verifyTime);
    return 1;
}

/*
Function:
GetX509RootStorePath

Used by System.Security.Cryptography.X509Certificates' Unix StorePal to determine the path to use
for the LocalMachine\Root X509 store.

Return values:
The directory which would be applied for X509_LOOKUP_add_dir(ctx, NULL). That is, the value of the
SSL_CERT_DIR environment variable, or the value of the X509_CERT_DIR compile-time constant.
*/
const char*
GetX509RootStorePath()
{
    const char* dir = getenv(X509_get_default_cert_dir_env());

    if (!dir)
    {
        dir = X509_get_default_cert_dir();
    }

    return dir;
}

/*
Function:
ReadX509AsDerFromBio

Used by System.Security.Cryptography.X509Certificates' OpenSslX509CertificateReader when attempting
to turn the contents of a file into an ICertificatePal object.

Return values:
If bio containns a valid DER-encoded X509 object, a pointer to that X509 structure that was deserialized,
otherwise NULL.
*/
X509*
ReadX509AsDerFromBio(
    BIO* bio)
{
    return d2i_X509_bio(bio, NULL);
}

/*
Function:
BioTell

Used by System.Security.Cryptography.X509Certificates' OpenSslX509CertificateReader when attempting
to turn the contents of a file into an ICertificatePal object to allow seeking back to the start point
in the event of a deserialization failure.

Return values:
The current seek position of the BIO if it is a file-related BIO, -1 on NULL inputs, and has unspecified
behavior on non-file, non-null BIO objects.

See also:
OpenSSL's BIO_tell
*/
int BioTell(
    BIO* bio)
{
    if (!bio)
    {
        return -1;
    }

    return BIO_tell(bio);
}

/*
Function:
BioTell

Used by System.Security.Cryptography.X509Certificates' OpenSslX509CertificateReader when attempting
to turn the contents of a file into an ICertificatePal object to seek back to the start point
in the event of a deserialization failure.

Return values:
-1 if bio is NULL
-1 if bio is a file-related BIO and seek fails
0 if bio is a file-related BIO and seek succeeds
otherwise unspecified

See also:
OpenSSL's BIO_seek
*/
int BioSeek(
    BIO* bio,
    int ofs)
{
    if (!bio)
    {
        return -1;
    }

    return BIO_seek(bio, ofs);
}

/*
Function:
NewX509Stack

Used by System.Security.Cryptography.X509Certificates when needing to pass a collection
of X509* to OpenSSL.

Return values:
A STACK_OF(X509*) with no comparator.
*/
STACK_OF(X509)*
NewX509Stack()
{
    return sk_X509_new_null();
}

/*
Function:
PushX509StackField

Used by System.Security.Cryptography.X509Certificates when needing to pass a collection
of X509* to OpenSSL.

Return values:
1 on success
0 on a NULL stack, or an error within sk_X509_push
*/
int
PushX509StackField(
    STACK_OF(X509)* stack,
    X509* x509)
{
    if (!stack)
    {
        return 0;
    }

    return sk_X509_push(stack, x509);
}

/*
Function:
UpRefEvpPkey

Used by System.Security.Cryptography.X509Certificates' OpenSslX509CertificateReader when
duplicating a private key context as part of duplicating the Pal object

Return values:
The number (as of this call) of references to the EVP_PKEY. Anything less than
2 is an error, because the key is already in the process of being freed.
*/
int
UpRefEvpPkey(
    EVP_PKEY* pkey)
{
    if (!pkey)
    {
        return 0;
    }

    return CRYPTO_add(&pkey->references, 1, CRYPTO_LOCK_EVP_PKEY);
}

/*
Function:
GetPkcs7Certificates

Used by System.Security.Cryptography.X509Certificates' CertificatePal when
reading the contents of a PKCS#7 file or blob.

Return values:
0 on NULL inputs, or a PKCS#7 file whose layout is not understood
1 when the file format is understood, and *certs is assigned to the
certificate contents of the structure.
*/
int
GetPkcs7Certificates(
    PKCS7* p7,
    STACK_OF(X509)** certs)
{
    if (!p7 || !certs)
    {
        return 0;
    }

    switch (OBJ_obj2nid(p7->type))
    {
        case NID_pkcs7_signed:
            *certs = p7->d.sign->cert;
            return 1;
        case NID_pkcs7_signedAndEnveloped:
            *certs = p7->d.signed_and_enveloped->cert;
            return 1;
    }

    return 0;
}

// Lock used to make sure EnsureopenSslInitialized itself is thread safe
static pthread_mutex_t g_initLock = PTHREAD_MUTEX_INITIALIZER;

// Set of locks initialized for OpenSSL
static pthread_mutex_t* g_locks = NULL;

/*
Function:
LockingCallback

Called back by OpenSSL to lock or unlock.
*/
static
void
LockingCallback(int mode, int n, const char* file, int line)
{
    (void)file, (void)line; // deliberately unused parameters

    int result;
    if (mode & CRYPTO_LOCK)
    {
        result = pthread_mutex_lock(&g_locks[n]);
    }
    else
    {
        result = pthread_mutex_unlock(&g_locks[n]);
    }

    if (result != 0)
    {
        assert(0 && "LockingCallback failed.");
    }
}

/*
Function:
EnsureOpenSslInitialized

Initializes OpenSSL with a locking callback to ensure thread safety.

Return values:
0 on success
non-zero on failure
*/
int
EnsureOpenSslInitialized()
{
    int ret = 0;
    int numLocks = 0;
    int locksInitialized = 0;

    pthread_mutex_lock(&g_initLock);

    if (g_locks != NULL)
    {
        // Already initialized; nothing more to do.
        goto done;
    }

    // Determine how many locks are needed
    numLocks = CRYPTO_num_locks();
    if (numLocks <= 0)
    {
        assert(0 && "CRYPTO_num_locks returned invalid value.");
        ret = 1;
        goto done;
    }

    // Create the locks array
    g_locks = (pthread_mutex_t*)malloc(sizeof(pthread_mutex_t) * (unsigned int)numLocks);
    if (g_locks == NULL)
    {
        assert(0 && "malloc failed.");
        ret = 2;
        goto done;
    }

    // Initialize each of the locks
    for (locksInitialized = 0; locksInitialized < numLocks; locksInitialized++)
    {
        if (pthread_mutex_init(&g_locks[locksInitialized], NULL) != 0)
        {
            assert(0 && "pthread_mutex_init failed.");
            ret = 3;
            goto done;
        }
    }

    // Initialize the callback
    CRYPTO_set_locking_callback(LockingCallback);

done:
    if (ret != 0)
    {
        // Cleanup on failure
        if (g_locks != NULL)
        {
            for (int i = locksInitialized - 1; i >= 0; i--)
            {
                if (pthread_mutex_destroy(&g_locks[i]) != 0)
                {
                    assert(0 && "Unable to pthread_mutex_destroy while cleaning up.");
                }
            }
            free(g_locks);
            g_locks = NULL;
        }
    }

    pthread_mutex_unlock(&g_initLock);
    return ret;
}
