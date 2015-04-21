//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include <string.h>
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
    if (!asn1)
    {
        return 0;
    }

    if (!pBuf || cBuf < asn1->length)
    {
        return -asn1->length;
    }

    memcpy(pBuf, asn1->data, asn1->length);
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
    if (!x509Name || !x509Name->bytes)
    {
        return 0;
    }

    if (!pBuf || cBuf < x509Name->bytes->length)
    {
        return -x509Name->bytes->length;
    }

    memcpy(pBuf, x509Name->bytes->data, x509Name->bytes->length);
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
