// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "fx_ver.h"
#include "pal.h"

#define TEST_ASSERT(a) \
  if (!(a)) \
  { \
    fprintf(stderr, "TEST_ASSERT failed '%s' at %d\n", #a, __LINE__); \
    exit(1); \
  }

struct TestCase
{
    pal::string_t str;
    struct
    {
        int major;
        int minor;
        int patch;
        pal::string_t pre;
        pal::string_t build;
    } ver;
    bool same;
};

const TestCase orderedCases[] =
{
    { _X("1.0.0-0.3.7"),                { 1, 0, 0,  _X("-0.3.7"),             _X("") }                 , false },
    { _X("1.0.0-alpha"),                { 1, 0, 0,  _X("-alpha"),             _X("") }                 , false },
    { _X("1.0.0-alpha+001"),            { 1, 0, 0,  _X("-alpha"),             _X("+001") }             , true  },
    { _X("1.0.0-alpha.1"),              { 1, 0, 0,  _X("-alpha.1"),           _X("") }                 , false },
    { _X("1.0.0-alpha.beta"),           { 1, 0, 0,  _X("-alpha.beta"),        _X("") }                 , false },
    { _X("1.0.0-beta"),                 { 1, 0, 0,  _X("-beta"),              _X("") }                 , false },
    { _X("1.0.0-beta+exp.sha.5114f85"), { 1, 0, 0,  _X("-beta"),              _X("+exp.sha.5114f85") } , true  },
    { _X("1.0.0-beta.2"),               { 1, 0, 0,  _X("-beta.2"),            _X("") }                 , false },
    { _X("1.0.0-beta.11"),              { 1, 0, 0,  _X("-beta.11"),           _X("") }                 , false },
    { _X("1.0.0-rc.1"),                 { 1, 0, 0,  _X("-rc.1"),              _X("") }                 , false },
    { _X("1.0.0-x.7.z.92"),             { 1, 0, 0,  _X("-x.7.z.92"),          _X("") }                 , false },
    { _X("1.0.0"),                      { 1, 0, 0,  _X(""),                   _X("") }                 , false },
    { _X("1.0.0+20130313144700"),       { 1, 0, 0,  _X(""),                   _X("+20130313144700") }  , true  },
    { _X("1.9.0-9"),                    { 1, 9, 0,  _X("-9"),                 _X("") }                 , false },
    { _X("1.9.0-10"),                   { 1, 9, 0,  _X("-10"),                _X("") }                 , false },
    { _X("1.9.0-1A"),                   { 1, 9, 0,  _X("-1A"),                _X("") }                 , false },
    { _X("1.9.0"),                      { 1, 9, 0,  _X(""),                   _X("") }                 , false },
    { _X("1.10.0"),                     { 1, 10, 0, _X(""),                   _X("") }                 , false },
    { _X("1.11.0"),                     { 1, 11, 0, _X(""),                   _X("") }                 , false },
    { _X("2.0.0"),                      { 2, 0, 0,  _X(""),                   _X("") }                 , false },
    { _X("2.1.0"),                      { 2, 1, 0,  _X(""),                   _X("") }                 , false },
    { _X("2.1.1"),                      { 2, 1, 1,  _X(""),                   _X("") }                 , false },
    { _X("4.6.0-preview.19064.1"),      { 4, 6, 0,  _X("-preview.19064.1"),   _X("") }                 , false },
    { _X("4.6.0-preview1-27018-01"),    { 4, 6, 0,  _X("-preview1-27018-01"), _X("") }                 , false },
};

const size_t cases = sizeof(orderedCases)/sizeof(TestCase);

void checkPrecedence(size_t iVal, const fx_ver_t &iVer, size_t jVal, const fx_ver_t &jVer)
{
    if (iVal == jVal)
    {
        TEST_ASSERT( (iVer == jVer));
        TEST_ASSERT(!(iVer <  jVer));
        TEST_ASSERT(!(iVer >  jVer));
        TEST_ASSERT( (iVer <= jVer));
        TEST_ASSERT( (iVer >= jVer));
        TEST_ASSERT(!(iVer != jVer));
    }
    else if (iVal < jVal)
    {
        TEST_ASSERT(!(iVer == jVer));
        TEST_ASSERT( (iVer <  jVer));
        TEST_ASSERT(!(iVer >  jVer));
        TEST_ASSERT( (iVer <= jVer));
        TEST_ASSERT(!(iVer >= jVer));
        TEST_ASSERT( (iVer != jVer));
    }
    else
    {
        TEST_ASSERT(iVal > jVal);

        TEST_ASSERT(!(iVer == jVer));
        TEST_ASSERT(!(iVer <  jVer));
        TEST_ASSERT( (iVer >  jVer));
        TEST_ASSERT(!(iVer <= jVer));
        TEST_ASSERT( (iVer >= jVer));
        TEST_ASSERT( (iVer != jVer));
    }
}

void checkPrecedence()
{
    size_t isame = 0;

    for (size_t i = 0; i < cases; ++i)
    {
        fx_ver_t iver;
        bool ivalid = fx_ver_t::parse(orderedCases[i].str, &iver);

        TEST_ASSERT(ivalid);

        if (orderedCases[i].same) isame++;

        size_t jsame = 0;

        for (size_t j = 0; j < cases; ++j)
        {
            fx_ver_t jver;
            bool jvalid = fx_ver_t::parse(orderedCases[j].str, &jver);

            TEST_ASSERT(jvalid);

            if (orderedCases[j].same) jsame++;

            checkPrecedence(i - isame, iver, j - jsame, jver);
        }
    }
}

void checkParsing(const TestCase &myCase)
{
    fx_ver_t ver;
    bool valid = fx_ver_t::parse(myCase.str, &ver);

    TEST_ASSERT(valid);
    TEST_ASSERT(ver.get_major() == myCase.ver.major);
    TEST_ASSERT(ver.get_minor() == myCase.ver.minor);
    TEST_ASSERT(ver.get_patch() == myCase.ver.patch);
    TEST_ASSERT(ver.is_prerelease() == !myCase.ver.pre.empty());
    TEST_ASSERT(ver.as_str() == myCase.str);
}

void checkParsing()
{
    for (size_t i = 0; i < cases; ++i)
    {
        checkParsing(orderedCases[i]);
    }
}

void checkInvalidVersions()
{
    pal::string_t invalidVersions[] =
    {
        _X(""),
        _X("1"),
        _X("1.1"),
        _X("A.1.1"),
        _X("1.A.1"),
        _X("1.1.A"),
        _X("1A.1.1"),
        _X("1.1A.1"),
        _X("1.1.1A"),
        _X("1.1.1-"),
        _X("1.1.1-."),
        _X("1.1.1-A."),
        _X("1.1.1-A.B."),
        _X("1.1.1-.+id"),
        _X("1.1.1-A.+id"),
        _X("1.1.1-A.B.+id"),
        _X("1.1.1-A.B+id."),
        _X("01.1.1"),
        _X("1.01.1"),
        _X("1.1.01"),
        _X("1.1.1-01.B"),
        _X("1.1.1-A.01"),
        _X("00.1.1"),
        _X("1.00.1"),
        _X("1.1.00"),
        _X("1.1.00-A"),
        _X("1.1.1-00.B"),
        _X("1.1.1-A.00"),
        _X("1.1.1+"),
        _X("1.1.1-A+"),
        _X("1.1.1-A*B"),
        _X("1.1.1-A/B"),
        _X("1.1.1-A:B"),
        _X("1.1.1-A^B"),
        _X("1.1.1-A|B"),
    };

    const size_t cases = sizeof(invalidVersions)/sizeof(pal::string_t);

    for (size_t i = 0; i < cases; ++i)
    {
        fx_ver_t ver;
        bool valid = fx_ver_t::parse(invalidVersions[i], &ver);

        TEST_ASSERT(!valid);
    }
}

#if defined(_WIN32)
int __cdecl wmain(const int argc, const pal::char_t* argv[])
#else
int main(const int argc, const pal::char_t* argv[])
#endif
{
    checkInvalidVersions();
    checkParsing();
    checkPrecedence();
}
