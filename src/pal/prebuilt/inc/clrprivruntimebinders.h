// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.00.0603 */
/* @@MIDL_FILE_HEADING(  ) */

#pragma warning( disable: 4049 )  /* more than 64k source lines */


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 475
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif // __RPCNDR_H_VERSION__


#ifndef __CLRPrivRuntimeBinders_h__
#define __CLRPrivRuntimeBinders_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __CLRPrivAppXBinder_FWD_DEFINED__
#define __CLRPrivAppXBinder_FWD_DEFINED__

#ifdef __cplusplus
typedef class CLRPrivAppXBinder CLRPrivAppXBinder;
#else
typedef struct CLRPrivAppXBinder CLRPrivAppXBinder;
#endif /* __cplusplus */

#endif 	/* __CLRPrivAppXBinder_FWD_DEFINED__ */


#ifndef __CLRPrivFusionBinder_FWD_DEFINED__
#define __CLRPrivFusionBinder_FWD_DEFINED__

#ifdef __cplusplus
typedef class CLRPrivFusionBinder CLRPrivFusionBinder;
#else
typedef struct CLRPrivFusionBinder CLRPrivFusionBinder;
#endif /* __cplusplus */

#endif 	/* __CLRPrivFusionBinder_FWD_DEFINED__ */


/* header files for imported files */
#include "clrprivbinding.h"

#ifdef __cplusplus
extern "C"{
#endif 



#ifndef __CLRPrivRuntimeBinders_LIBRARY_DEFINED__
#define __CLRPrivRuntimeBinders_LIBRARY_DEFINED__

/* library CLRPrivRuntimeBinders */
/* [uuid] */ 


EXTERN_C const IID LIBID_CLRPrivRuntimeBinders;

EXTERN_C const CLSID CLSID_CLRPrivAppXBinder;

#ifdef __cplusplus

class DECLSPEC_UUID("E990F732-2D0A-48AC-87FC-EF12B618981A")
CLRPrivAppXBinder;
#endif

EXTERN_C const CLSID CLSID_CLRPrivFusionBinder;

#ifdef __cplusplus

class DECLSPEC_UUID("E990F732-2D0A-48AC-87FC-EF12B618981C")
CLRPrivFusionBinder;
#endif
#endif /* __CLRPrivRuntimeBinders_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


