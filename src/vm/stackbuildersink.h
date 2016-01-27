// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: StackBuilderSink.h
//

//
// Purpose: Native implementation for System.Runtime.Remoting.Messaging.StackBuilderSink
//


#ifndef __STACKBUILDERSINK_H__
#define __STACKBUILDERSINK_H__

#ifndef FEATURE_REMOTING
#error FEATURE_REMOTING is not set, please do not include stackbuildersink.h
#endif

void CallDescrWithObjectArray(OBJECTREF& pServer, MethodDesc *pMD, //ReflectMethod *pMD,
                  PCODE pTarget, MetaSig* sig, VASigCookie *pCookie,
                  PTRARRAYREF& pArguments,
                  OBJECTREF* pVarRet, PTRARRAYREF* ppVarOutParams);

//+----------------------------------------------------------
//
//  Class:      CStackBuilderSink
// 
//  Synopsis:   EE counterpart to 
//              Microsoft.Runtime.StackBuilderSink
//              Code helper to build a stack of parameter 
//              arguments and make a function call on an 
//              object.
// 
//
//------------------------------------------------------------
class CStackBuilderSink
{
public:    
   
    static FCDECL5(Object*, PrivateProcessMessage, 
                                Object* pSBSinkUNSAFE, 
                                MethodDesc* pMD, 
                                PTRArray* pArgsUNSAFE, 
                                Object* pServerUNSAFE, 
                                PTRARRAYREF* ppVarOutParams);
};

#endif  // __STACKBUILDERSINK_H__
