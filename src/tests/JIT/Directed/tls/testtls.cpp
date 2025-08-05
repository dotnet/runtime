// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif // _MSC_VER

thread_local int tls0; 
thread_local int tls1;
thread_local int tls2;
thread_local int tls3;
thread_local int tls4;
thread_local int tls5;
thread_local int tls6;
thread_local int tls7;
thread_local int tls8;
thread_local int tls9;
thread_local int tls10;
thread_local int tls11;
thread_local int tls12;
thread_local int tls13;
thread_local int tls14;
thread_local int tls15;
thread_local int tls16;

extern "C" DLLEXPORT void initializeTLS() {
    tls0=0;
    tls1=0;
    tls2=0;
    tls3=0;
    tls4=0;
    tls5=0;
    tls6=0;
    tls7=0;
    tls8=0;
    tls9=0;
    tls10=0;
    tls11=0;
    tls12=0;
    tls13=0;
    tls14=0;
    tls15=0;
    tls16=0;
}
