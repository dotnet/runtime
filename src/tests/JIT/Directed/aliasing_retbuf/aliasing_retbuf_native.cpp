// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(_MSC_VER)
#define EXPORT_API extern "C" __declspec(dllexport)
#else
#define EXPORT_API extern "C" __attribute__((visibility("default")))
#endif

struct Foo
{
    void* A;
    void* B;
    void* C;
};

EXPORT_API
Foo TransposeRetBuf(Foo* fi)
{
    Foo f;
    f.A = fi->B;
    f.B = fi->C;
    f.C = fi->A;
    return f;
}