// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_x509_name.h"

extern "C" int32_t GetX509NameStackFieldCount(X509NameStack* sk)
{
    return sk_X509_NAME_num(sk);
}

extern "C" X509_NAME* GetX509NameStackField(X509NameStack* sk, int32_t loc)
{
    return sk_X509_NAME_value(sk, loc);
}

extern "C" X509_NAME* DecodeX509Name(const unsigned char* buf, int32_t len)
{
    if (!buf || !len)
    {
        return nullptr;
    }

    return d2i_X509_NAME(nullptr, &buf, len);
}

extern "C" void X509NameDestroy(X509_NAME* a)
{
    if (a != nullptr)
    {
        X509_NAME_free(a);
    }
}

static int32_t X509NamePrint(BIO* out, X509_NAME* nm, unsigned long flags)
{
    return X509_NAME_print_ex(out, nm, /*indent*/ 0, flags);
}

extern "C" int32_t X509NamePrintForFind(BIO* out, X509_NAME* nm)
{
    const unsigned long findFormat = XN_FLAG_FN_NONE | XN_FLAG_SEP_CPLUS_SPC;

    return X509NamePrint(out, nm, findFormat);
}

/*
Converts from X500DistinguishedNameFlags to the correct flags used in X509_NAME_print_ex.
*/
static unsigned long ConvertFormatFlags(X500DistinguishedNameFlags inFlags)
{
    unsigned long outFlags = 0;

    if ((inFlags & X500DistinguishedNameFlags::Reversed) != 0)
    {
        outFlags |= XN_FLAG_DN_REV;
    }

    if ((inFlags & X500DistinguishedNameFlags::UseSemicolons) != 0)
    {
        outFlags |= XN_FLAG_SEP_SPLUS_SPC;
    }
    else if ((inFlags & X500DistinguishedNameFlags::UseNewLines) != 0)
    {
        outFlags |= XN_FLAG_SEP_MULTILINE;
    }
    else
    {
        outFlags |= XN_FLAG_SEP_CPLUS_SPC;
    }

    if ((inFlags & X500DistinguishedNameFlags::DoNotUseQuotes) != 0)
    {
        // TODO: Handle this.
    }

    if ((inFlags & X500DistinguishedNameFlags::ForceUTF8Encoding) != 0)
    {
        // TODO: Handle this.
    }

    if ((inFlags & X500DistinguishedNameFlags::UseUTF8Encoding) != 0)
    {
        // TODO: Handle this.
    }
    else if ((inFlags & X500DistinguishedNameFlags::UseT61Encoding) != 0)
    {
        // TODO: Handle this.
    }

    return outFlags;
}

extern "C" int32_t X509NamePrintEx(BIO* out, X509_NAME* nm, X500DistinguishedNameFlags flags)
{
    unsigned long format = ConvertFormatFlags(flags);
    return X509NamePrint(out, nm, format);
}

extern "C" STACK_OF(X509_NAME)* NewX509NameStack()
{
    return sk_X509_NAME_new_null();
}

extern "C" int32_t PushX509NameStackField(STACK_OF(X509_NAME)* stack, X509_NAME* x509Name)
{
    if (!stack)
    {
        return 0;
    }

    return sk_X509_NAME_push(stack, x509Name);
}

extern "C" void RecursiveFreeX509NameStack(STACK_OF(X509_NAME)* stack)
{
    sk_X509_NAME_pop_free(stack, X509_NAME_free);
}

extern "C" X509_NAME* DuplicateX509Name(X509_NAME* x509Name)
{
    return X509_NAME_dup(x509Name);
}
