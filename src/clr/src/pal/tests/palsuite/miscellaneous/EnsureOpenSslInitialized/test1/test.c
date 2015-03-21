//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source:  
**
** Source : test1.c
**
** Purpose: Test for EnsureOpenSslInitialized() function
**
**
**=========================================================*/

#include <palsuite.h>
#include <pal_corefx.h>
#include <dlfcn.h>

typedef void* (*CRYPTO_get_locking_callback)();
typedef int (*RAND_pseudo_bytes)(unsigned char*,int);

int __cdecl main(int argc, char *argv[]) {

    BOOL ret = PASS;

    DWORD ensureResult;
    void* libcrypto;

    CRYPTO_get_locking_callback getCallbackFunc;
    void* lockingCallback;
    RAND_pseudo_bytes randFunc;

    const int bufLength = 5;
    unsigned char buf[bufLength];
    int rv;

    /* Initialize the PAL and return FAILURE if this fails */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Load libcrypto */

    libcrypto = dlopen("libcrypto"
#if __APPLE__
        ".dlsym"
#else
        ".so"
#endif
        , RTLD_NOW);
    if (libcrypto == NULL)
    {
        Trace("libcrypto could not be opened: \"%s\" ", dlerror());
    }

    /* Try to initialize OpenSSL threading using EnsureOpenSslInitialized */

    ensureResult = EnsureOpenSslInitialized();
    if (libcrypto == NULL)
    {
        if (ensureResult == 0)
        {
            Trace("libcrypto isn't available, but EnsureOpenSslInitialized succeeded");
            ret = FAIL;
        }
        goto done;
    }
    else if (ensureResult != 0)
    {
        Trace("EnsureOpenSslInitialized failed: %d ", ensureResult);
        ret = FAIL;
        goto done;
    }

    /* Get the CRYPTO_get_locking_callback function, and ensure its result
     * is non-null, indicating that EnsureOpenSslInitialized did install
     * a callback.
     */

    getCallbackFunc = (CRYPTO_get_locking_callback) dlsym(libcrypto, "CRYPTO_get_locking_callback");
    if (getCallbackFunc == NULL)
    {
        Trace("Loading CRYPTO_get_locking_callback failed: \"%s\" ", dlerror());
        ret = FAIL;
        goto done;
    }
    lockingCallback = getCallbackFunc();
    if (lockingCallback == NULL)
    {
        Trace("Locking callback was not set by EnsureOpenSslInitialized");
        ret = FAIL;
        goto done;
    }    
    
    /* Now get a function from libcrypto that uses the locking callback,
     * and invoke that function to exercise the locking callback and
     * at least verify its invocation doesn't hang or crash.  This doesn't
     * validate that all locking is done correctly.
     */

    randFunc = (RAND_pseudo_bytes) dlsym(libcrypto, "RAND_pseudo_bytes");
    if (randFunc == NULL)
    {
        Trace("Loading RAND_pseudo_bytes failed: \"%s\" ", dlerror());
        ret = FAIL;
        goto done;
    }
    rv = randFunc(buf, bufLength);
    if (rv < 0)
    {
        Trace("RAND_pseudo_bytes failed: %d ", rv);
        ret = FAIL;
        goto done;
    }
    
done:
    if (libcrypto != NULL)
    {
        if (dlclose(libcrypto) != 0)
        {
            Trace("Closing libcrypto failed: \"%s\" ", dlerror());
            ret = FAIL;
        }
    }

    PAL_Terminate();
    return ret;
}

