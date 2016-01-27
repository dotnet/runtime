// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifdef _MSC_VER
#pragma warning( disable: 4049 )  /* more than 64k source lines */
#endif

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 6.00.0328 */
/* Compiler settings for xmlparser.idl:
    Oicf (OptLev=i2), W1, Zp8, env=Win32 (32b run)
    protocol : dce , ms_ext, c_ext
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
//@@MIDL_FILE_HEADING(  )


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 440
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __xmlparser_h__
#define __xmlparser_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __IXMLNodeSource_FWD_DEFINED__
#define __IXMLNodeSource_FWD_DEFINED__
typedef interface IXMLNodeSource IXMLNodeSource;
#endif 	/* __IXMLNodeSource_FWD_DEFINED__ */


#ifndef __IXMLParser_FWD_DEFINED__
#define __IXMLParser_FWD_DEFINED__
typedef interface IXMLParser IXMLParser;
#endif 	/* __IXMLParser_FWD_DEFINED__ */


#ifndef __IXMLNodeFactory_FWD_DEFINED__
#define __IXMLNodeFactory_FWD_DEFINED__
typedef interface IXMLNodeFactory IXMLNodeFactory;
#endif 	/* __IXMLNodeFactory_FWD_DEFINED__ */


#ifndef __XMLParser_FWD_DEFINED__
#define __XMLParser_FWD_DEFINED__

#ifdef __cplusplus
typedef class XMLParser XMLParser;
#else
typedef struct XMLParser XMLParser;
#endif /* __cplusplus */

#endif 	/* __XMLParser_FWD_DEFINED__ */


/* header files for imported files */
#include "unknwn.h"
#include "objidl.h"
#include "oaidl.h"

#ifdef __cplusplus
extern "C"{
#endif 

void __RPC_FAR * __RPC_USER MIDL_user_allocate(size_t);
void __RPC_USER MIDL_user_free( void __RPC_FAR * ); 

/* interface __MIDL_itf_xmlparser_0000 */
/* [local] */ 

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xmlparser_0000_0001
    {	XML_ELEMENT	= 1,
	XML_ATTRIBUTE	= XML_ELEMENT + 1,
	XML_PI	= XML_ATTRIBUTE + 1,
	XML_XMLDECL	= XML_PI + 1,
	XML_DOCTYPE	= XML_XMLDECL + 1,
	XML_DTDATTRIBUTE	= XML_DOCTYPE + 1,
	XML_ENTITYDECL	= XML_DTDATTRIBUTE + 1,
	XML_ELEMENTDECL	= XML_ENTITYDECL + 1,
	XML_ATTLISTDECL	= XML_ELEMENTDECL + 1,
	XML_NOTATION	= XML_ATTLISTDECL + 1,
	XML_GROUP	= XML_NOTATION + 1,
	XML_INCLUDESECT	= XML_GROUP + 1,
	XML_PCDATA	= XML_INCLUDESECT + 1,
	XML_CDATA	= XML_PCDATA + 1,
	XML_IGNORESECT	= XML_CDATA + 1,
	XML_COMMENT	= XML_IGNORESECT + 1,
	XML_ENTITYREF	= XML_COMMENT + 1,
	XML_WHITESPACE	= XML_ENTITYREF + 1,
	XML_NAME	= XML_WHITESPACE + 1,
	XML_NMTOKEN	= XML_NAME + 1,
	XML_STRING	= XML_NMTOKEN + 1,
	XML_PEREF	= XML_STRING + 1,
	XML_MODEL	= XML_PEREF + 1,
	XML_ATTDEF	= XML_MODEL + 1,
	XML_ATTTYPE	= XML_ATTDEF + 1,
	XML_ATTPRESENCE	= XML_ATTTYPE + 1,
	XML_DTDSUBSET	= XML_ATTPRESENCE + 1,
	XML_LASTNODETYPE	= XML_DTDSUBSET + 1
    } 	XML_NODE_TYPE;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xmlparser_0000_0002
    {	XML_VERSION	= XML_LASTNODETYPE,
	XML_ENCODING	= XML_VERSION + 1,
	XML_STANDALONE	= XML_ENCODING + 1,
	XML_NS	= XML_STANDALONE + 1,
	XML_XMLSPACE	= XML_NS + 1,
	XML_XMLLANG	= XML_XMLSPACE + 1,
	XML_SYSTEM	= XML_XMLLANG + 1,
	XML_PUBLIC	= XML_SYSTEM + 1,
	XML_NDATA	= XML_PUBLIC + 1,
	XML_AT_CDATA	= XML_NDATA + 1,
	XML_AT_ID	= XML_AT_CDATA + 1,
	XML_AT_IDREF	= XML_AT_ID + 1,
	XML_AT_IDREFS	= XML_AT_IDREF + 1,
	XML_AT_ENTITY	= XML_AT_IDREFS + 1,
	XML_AT_ENTITIES	= XML_AT_ENTITY + 1,
	XML_AT_NMTOKEN	= XML_AT_ENTITIES + 1,
	XML_AT_NMTOKENS	= XML_AT_NMTOKEN + 1,
	XML_AT_NOTATION	= XML_AT_NMTOKENS + 1,
	XML_AT_REQUIRED	= XML_AT_NOTATION + 1,
	XML_AT_IMPLIED	= XML_AT_REQUIRED + 1,
	XML_AT_FIXED	= XML_AT_IMPLIED + 1,
	XML_PENTITYDECL	= XML_AT_FIXED + 1,
	XML_EMPTY	= XML_PENTITYDECL + 1,
	XML_ANY	= XML_EMPTY + 1,
	XML_MIXED	= XML_ANY + 1,
	XML_SEQUENCE	= XML_MIXED + 1,
	XML_CHOICE	= XML_SEQUENCE + 1,
	XML_STAR	= XML_CHOICE + 1,
	XML_PLUS	= XML_STAR + 1,
	XML_QUESTIONMARK	= XML_PLUS + 1,
	XML_LASTSUBNODETYPE	= XML_QUESTIONMARK + 1
    } 	XML_NODE_SUBTYPE;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xmlparser_0000_0003
    {	XML_E_PARSEERRORBASE	= 0xc00ce500L,
	XML_E_ENDOFINPUT	= XML_E_PARSEERRORBASE,
	XML_E_MISSINGEQUALS	= XML_E_ENDOFINPUT + 1,
	XML_E_MISSINGQUOTE	= XML_E_MISSINGEQUALS + 1,
	XML_E_COMMENTSYNTAX	= XML_E_MISSINGQUOTE + 1,
	XML_E_BADSTARTNAMECHAR	= XML_E_COMMENTSYNTAX + 1,
	XML_E_BADNAMECHAR	= XML_E_BADSTARTNAMECHAR + 1,
	XML_E_BADCHARINSTRING	= XML_E_BADNAMECHAR + 1,
	XML_E_XMLDECLSYNTAX	= XML_E_BADCHARINSTRING + 1,
	XML_E_BADCHARDATA	= XML_E_XMLDECLSYNTAX + 1,
	XML_E_MISSINGWHITESPACE	= XML_E_BADCHARDATA + 1,
	XML_E_EXPECTINGTAGEND	= XML_E_MISSINGWHITESPACE + 1,
	XML_E_BADCHARINDTD	= XML_E_EXPECTINGTAGEND + 1,
	XML_E_BADCHARINDECL	= XML_E_BADCHARINDTD + 1,
	XML_E_MISSINGSEMICOLON	= XML_E_BADCHARINDECL + 1,
	XML_E_BADCHARINENTREF	= XML_E_MISSINGSEMICOLON + 1,
	XML_E_UNBALANCEDPAREN	= XML_E_BADCHARINENTREF + 1,
	XML_E_EXPECTINGOPENBRACKET	= XML_E_UNBALANCEDPAREN + 1,
	XML_E_BADENDCONDSECT	= XML_E_EXPECTINGOPENBRACKET + 1,
	XML_E_INTERNALERROR	= XML_E_BADENDCONDSECT + 1,
	XML_E_UNEXPECTED_WHITESPACE	= XML_E_INTERNALERROR + 1,
	XML_E_INCOMPLETE_ENCODING	= XML_E_UNEXPECTED_WHITESPACE + 1,
	XML_E_BADCHARINMIXEDMODEL	= XML_E_INCOMPLETE_ENCODING + 1,
	XML_E_MISSING_STAR	= XML_E_BADCHARINMIXEDMODEL + 1,
	XML_E_BADCHARINMODEL	= XML_E_MISSING_STAR + 1,
	XML_E_MISSING_PAREN	= XML_E_BADCHARINMODEL + 1,
	XML_E_BADCHARINENUMERATION	= XML_E_MISSING_PAREN + 1,
	XML_E_PIDECLSYNTAX	= XML_E_BADCHARINENUMERATION + 1,
	XML_E_EXPECTINGCLOSEQUOTE	= XML_E_PIDECLSYNTAX + 1,
	XML_E_MULTIPLE_COLONS	= XML_E_EXPECTINGCLOSEQUOTE + 1,
	XML_E_INVALID_DECIMAL	= XML_E_MULTIPLE_COLONS + 1,
	XML_E_INVALID_HEXIDECIMAL	= XML_E_INVALID_DECIMAL + 1,
	XML_E_INVALID_UNICODE	= XML_E_INVALID_HEXIDECIMAL + 1,
	XML_E_WHITESPACEORQUESTIONMARK	= XML_E_INVALID_UNICODE + 1,
	XML_E_TOKEN_ERROR	= XML_E_PARSEERRORBASE + 0x50,
	XML_E_SUSPENDED	= XML_E_TOKEN_ERROR,
	XML_E_STOPPED	= XML_E_SUSPENDED + 1,
	XML_E_UNEXPECTEDENDTAG	= XML_E_STOPPED + 1,
	XML_E_UNCLOSEDTAG	= XML_E_UNEXPECTEDENDTAG + 1,
	XML_E_DUPLICATEATTRIBUTE	= XML_E_UNCLOSEDTAG + 1,
	XML_E_MULTIPLEROOTS	= XML_E_DUPLICATEATTRIBUTE + 1,
	XML_E_INVALIDATROOTLEVEL	= XML_E_MULTIPLEROOTS + 1,
	XML_E_BADXMLDECL	= XML_E_INVALIDATROOTLEVEL + 1,
	XML_E_MISSINGROOT	= XML_E_BADXMLDECL + 1,
	XML_E_UNEXPECTEDEOF	= XML_E_MISSINGROOT + 1,
	XML_E_BADPEREFINSUBSET	= XML_E_UNEXPECTEDEOF + 1,
	XML_E_PE_NESTING	= XML_E_BADPEREFINSUBSET + 1,
	XML_E_INVALID_CDATACLOSINGTAG	= XML_E_PE_NESTING + 1,
	XML_E_UNCLOSEDPI	= XML_E_INVALID_CDATACLOSINGTAG + 1,
	XML_E_UNCLOSEDSTARTTAG	= XML_E_UNCLOSEDPI + 1,
	XML_E_UNCLOSEDENDTAG	= XML_E_UNCLOSEDSTARTTAG + 1,
	XML_E_UNCLOSEDSTRING	= XML_E_UNCLOSEDENDTAG + 1,
	XML_E_UNCLOSEDCOMMENT	= XML_E_UNCLOSEDSTRING + 1,
	XML_E_UNCLOSEDDECL	= XML_E_UNCLOSEDCOMMENT + 1,
	XML_E_UNCLOSEDMARKUPDECL	= XML_E_UNCLOSEDDECL + 1,
	XML_E_UNCLOSEDCDATA	= XML_E_UNCLOSEDMARKUPDECL + 1,
	XML_E_BADDECLNAME	= XML_E_UNCLOSEDCDATA + 1,
	XML_E_BADEXTERNALID	= XML_E_BADDECLNAME + 1,
	XML_E_BADELEMENTINDTD	= XML_E_BADEXTERNALID + 1,
	XML_E_RESERVEDNAMESPACE	= XML_E_BADELEMENTINDTD + 1,
	XML_E_EXPECTING_VERSION	= XML_E_RESERVEDNAMESPACE + 1,
	XML_E_EXPECTING_ENCODING	= XML_E_EXPECTING_VERSION + 1,
	XML_E_EXPECTING_NAME	= XML_E_EXPECTING_ENCODING + 1,
	XML_E_UNEXPECTED_ATTRIBUTE	= XML_E_EXPECTING_NAME + 1,
	XML_E_ENDTAGMISMATCH	= XML_E_UNEXPECTED_ATTRIBUTE + 1,
	XML_E_INVALIDENCODING	= XML_E_ENDTAGMISMATCH + 1,
	XML_E_INVALIDSWITCH	= XML_E_INVALIDENCODING + 1,
	XML_E_EXPECTING_NDATA	= XML_E_INVALIDSWITCH + 1,
	XML_E_INVALID_MODEL	= XML_E_EXPECTING_NDATA + 1,
	XML_E_INVALID_TYPE	= XML_E_INVALID_MODEL + 1,
	XML_E_INVALIDXMLSPACE	= XML_E_INVALID_TYPE + 1,
	XML_E_MULTI_ATTR_VALUE	= XML_E_INVALIDXMLSPACE + 1,
	XML_E_INVALID_PRESENCE	= XML_E_MULTI_ATTR_VALUE + 1,
	XML_E_BADXMLCASE	= XML_E_INVALID_PRESENCE + 1,
	XML_E_CONDSECTINSUBSET	= XML_E_BADXMLCASE + 1,
	XML_E_CDATAINVALID	= XML_E_CONDSECTINSUBSET + 1,
	XML_E_INVALID_STANDALONE	= XML_E_CDATAINVALID + 1,
	XML_E_UNEXPECTED_STANDALONE	= XML_E_INVALID_STANDALONE + 1,
	XML_E_DOCTYPE_IN_DTD	= XML_E_UNEXPECTED_STANDALONE + 1,
	XML_E_MISSING_ENTITY	= XML_E_DOCTYPE_IN_DTD + 1,
	XML_E_ENTITYREF_INNAME	= XML_E_MISSING_ENTITY + 1,
	XML_E_DOCTYPE_OUTSIDE_PROLOG	= XML_E_ENTITYREF_INNAME + 1,
	XML_E_INVALID_VERSION	= XML_E_DOCTYPE_OUTSIDE_PROLOG + 1,
	XML_E_DTDELEMENT_OUTSIDE_DTD	= XML_E_INVALID_VERSION + 1,
	XML_E_DUPLICATEDOCTYPE	= XML_E_DTDELEMENT_OUTSIDE_DTD + 1,
	XML_E_RESOURCE	= XML_E_DUPLICATEDOCTYPE + 1,
	XML_E_LASTERROR	= XML_E_RESOURCE + 1
    } 	XML_ERROR_CODE;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xmlparser_0000_0004
    {	XMLPARSER_IDLE	= 0,
	XMLPARSER_WAITING	= XMLPARSER_IDLE + 1,
	XMLPARSER_BUSY	= XMLPARSER_WAITING + 1,
	XMLPARSER_ERROR	= XMLPARSER_BUSY + 1,
	XMLPARSER_STOPPED	= XMLPARSER_ERROR + 1,
	XMLPARSER_SUSPENDED	= XMLPARSER_STOPPED + 1
    } 	XML_PARSER_STATE;

typedef /* [public] */ 
enum __MIDL___MIDL_itf_xmlparser_0000_0005
    {	XMLFLAG_FLOATINGAMP	= 1,
	XMLFLAG_SHORTENDTAGS	= 2,
	XMLFLAG_CASEINSENSITIVE	= 4,
	XMLFLAG_NONAMESPACES	= 8,
	XMLFLAG_NOWHITESPACE	= 16,
	XMLFLAG_IE4QUIRKS	= 32,
	XMLFLAG_NODTDNODES	= 64,
	XMLFLAG_IE4COMPATIBILITY	= 255
    } 	XML_PARSER_FLAGS;

typedef /* [public][public] */ 
enum __MIDL___MIDL_itf_xmlparser_0000_0006
    {	XMLNF_STARTDOCUMENT	= 0,
	XMLNF_STARTDTD	= XMLNF_STARTDOCUMENT + 1,
	XMLNF_ENDDTD	= XMLNF_STARTDTD + 1,
	XMLNF_STARTDTDSUBSET	= XMLNF_ENDDTD + 1,
	XMLNF_ENDDTDSUBSET	= XMLNF_STARTDTDSUBSET + 1,
	XMLNF_ENDPROLOG	= XMLNF_ENDDTDSUBSET + 1,
	XMLNF_STARTENTITY	= XMLNF_ENDPROLOG + 1,
	XMLNF_ENDENTITY	= XMLNF_STARTENTITY + 1,
	XMLNF_ENDDOCUMENT	= XMLNF_ENDENTITY + 1,
	XMLNF_DATAAVAILABLE	= XMLNF_ENDDOCUMENT + 1,
	XMLNF_LASTEVENT	= XMLNF_DATAAVAILABLE
    } 	XML_NODEFACTORY_EVENT;

typedef struct _XML_NODE_INFO
    {
    DWORD dwSize;
    DWORD dwType;
    DWORD dwSubType;
    BOOL fTerminal;
    const WCHAR __RPC_FAR *pwcText;
    ULONG ulLen;
    ULONG ulNsPrefixLen;
    PVOID pNode;
    PVOID pReserved;
    } 	XML_NODE_INFO;



extern RPC_IF_HANDLE __MIDL_itf_xmlparser_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_xmlparser_0000_v0_0_s_ifspec;


#ifndef __XMLPSR_LIBRARY_DEFINED__
#define __XMLPSR_LIBRARY_DEFINED__

/* library XMLPSR */
/* [version][lcid][helpstring][uuid] */ 


EXTERN_C const IID LIBID_XMLPSR;

#ifndef __IXMLNodeSource_INTERFACE_DEFINED__
#define __IXMLNodeSource_INTERFACE_DEFINED__

/* interface IXMLNodeSource */
/* [unique][helpstring][uuid][local][object] */ 


EXTERN_C const IID IID_IXMLNodeSource;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("d242361d-51a0-11d2-9caf-0060b0ec3d39")
    IXMLNodeSource : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetFactory( 
            /* [in] */ IXMLNodeFactory __RPC_FAR *pNodeFactory) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFactory( 
            /* [out] */ IXMLNodeFactory __RPC_FAR *__RPC_FAR *ppNodeFactory) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Abort( 
            /* [in] */ BSTR bstrErrorInfo) = 0;
        
        virtual ULONG STDMETHODCALLTYPE GetLineNumber( void) = 0;
        
        virtual ULONG STDMETHODCALLTYPE GetLinePosition( void) = 0;
        
        virtual ULONG STDMETHODCALLTYPE GetAbsolutePosition( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLineBuffer( 
            /* [out] */ const WCHAR __RPC_FAR *__RPC_FAR *ppwcBuf,
            /* [out] */ ULONG __RPC_FAR *pulLen,
            /* [out] */ ULONG __RPC_FAR *pulStartPos) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLastError( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetErrorInfo( 
            /* [out] */ BSTR __RPC_FAR *pbstrErrorInfo) = 0;
        
        virtual ULONG STDMETHODCALLTYPE GetFlags( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetURL( 
            /* [out] */ const WCHAR __RPC_FAR *__RPC_FAR *ppwcBuf) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IXMLNodeSourceVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IXMLNodeSource __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IXMLNodeSource __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IXMLNodeSource __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *SetFactory )( 
            IXMLNodeSource __RPC_FAR * This,
            /* [in] */ IXMLNodeFactory __RPC_FAR *pNodeFactory);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetFactory )( 
            IXMLNodeSource __RPC_FAR * This,
            /* [out] */ IXMLNodeFactory __RPC_FAR *__RPC_FAR *ppNodeFactory);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Abort )( 
            IXMLNodeSource __RPC_FAR * This,
            /* [in] */ BSTR bstrErrorInfo);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *GetLineNumber )( 
            IXMLNodeSource __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *GetLinePosition )( 
            IXMLNodeSource __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *GetAbsolutePosition )( 
            IXMLNodeSource __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetLineBuffer )( 
            IXMLNodeSource __RPC_FAR * This,
            /* [out] */ const WCHAR __RPC_FAR *__RPC_FAR *ppwcBuf,
            /* [out] */ ULONG __RPC_FAR *pulLen,
            /* [out] */ ULONG __RPC_FAR *pulStartPos);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetLastError )( 
            IXMLNodeSource __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetErrorInfo )( 
            IXMLNodeSource __RPC_FAR * This,
            /* [out] */ BSTR __RPC_FAR *pbstrErrorInfo);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *GetFlags )( 
            IXMLNodeSource __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetURL )( 
            IXMLNodeSource __RPC_FAR * This,
            /* [out] */ const WCHAR __RPC_FAR *__RPC_FAR *ppwcBuf);
        
        END_INTERFACE
    } IXMLNodeSourceVtbl;

    interface IXMLNodeSource
    {
        CONST_VTBL struct IXMLNodeSourceVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXMLNodeSource_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IXMLNodeSource_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IXMLNodeSource_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IXMLNodeSource_SetFactory(This,pNodeFactory)	\
    (This)->lpVtbl -> SetFactory(This,pNodeFactory)

#define IXMLNodeSource_GetFactory(This,ppNodeFactory)	\
    (This)->lpVtbl -> GetFactory(This,ppNodeFactory)

#define IXMLNodeSource_Abort(This,bstrErrorInfo)	\
    (This)->lpVtbl -> Abort(This,bstrErrorInfo)

#define IXMLNodeSource_GetLineNumber(This)	\
    (This)->lpVtbl -> GetLineNumber(This)

#define IXMLNodeSource_GetLinePosition(This)	\
    (This)->lpVtbl -> GetLinePosition(This)

#define IXMLNodeSource_GetAbsolutePosition(This)	\
    (This)->lpVtbl -> GetAbsolutePosition(This)

#define IXMLNodeSource_GetLineBuffer(This,ppwcBuf,pulLen,pulStartPos)	\
    (This)->lpVtbl -> GetLineBuffer(This,ppwcBuf,pulLen,pulStartPos)

#define IXMLNodeSource_GetLastError(This)	\
    (This)->lpVtbl -> GetLastError(This)

#define IXMLNodeSource_GetErrorInfo(This,pbstrErrorInfo)	\
    (This)->lpVtbl -> GetErrorInfo(This,pbstrErrorInfo)

#define IXMLNodeSource_GetFlags(This)	\
    (This)->lpVtbl -> GetFlags(This)

#define IXMLNodeSource_GetURL(This,ppwcBuf)	\
    (This)->lpVtbl -> GetURL(This,ppwcBuf)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IXMLNodeSource_SetFactory_Proxy( 
    IXMLNodeSource __RPC_FAR * This,
    /* [in] */ IXMLNodeFactory __RPC_FAR *pNodeFactory);


void __RPC_STUB IXMLNodeSource_SetFactory_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLNodeSource_GetFactory_Proxy( 
    IXMLNodeSource __RPC_FAR * This,
    /* [out] */ IXMLNodeFactory __RPC_FAR *__RPC_FAR *ppNodeFactory);


void __RPC_STUB IXMLNodeSource_GetFactory_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLNodeSource_Abort_Proxy( 
    IXMLNodeSource __RPC_FAR * This,
    /* [in] */ BSTR bstrErrorInfo);


void __RPC_STUB IXMLNodeSource_Abort_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


ULONG STDMETHODCALLTYPE IXMLNodeSource_GetLineNumber_Proxy( 
    IXMLNodeSource __RPC_FAR * This);


void __RPC_STUB IXMLNodeSource_GetLineNumber_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


ULONG STDMETHODCALLTYPE IXMLNodeSource_GetLinePosition_Proxy( 
    IXMLNodeSource __RPC_FAR * This);


void __RPC_STUB IXMLNodeSource_GetLinePosition_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


ULONG STDMETHODCALLTYPE IXMLNodeSource_GetAbsolutePosition_Proxy( 
    IXMLNodeSource __RPC_FAR * This);


void __RPC_STUB IXMLNodeSource_GetAbsolutePosition_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLNodeSource_GetLineBuffer_Proxy( 
    IXMLNodeSource __RPC_FAR * This,
    /* [out] */ const WCHAR __RPC_FAR *__RPC_FAR *ppwcBuf,
    /* [out] */ ULONG __RPC_FAR *pulLen,
    /* [out] */ ULONG __RPC_FAR *pulStartPos);


void __RPC_STUB IXMLNodeSource_GetLineBuffer_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLNodeSource_GetLastError_Proxy( 
    IXMLNodeSource __RPC_FAR * This);


void __RPC_STUB IXMLNodeSource_GetLastError_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLNodeSource_GetErrorInfo_Proxy( 
    IXMLNodeSource __RPC_FAR * This,
    /* [out] */ BSTR __RPC_FAR *pbstrErrorInfo);


void __RPC_STUB IXMLNodeSource_GetErrorInfo_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


ULONG STDMETHODCALLTYPE IXMLNodeSource_GetFlags_Proxy( 
    IXMLNodeSource __RPC_FAR * This);


void __RPC_STUB IXMLNodeSource_GetFlags_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLNodeSource_GetURL_Proxy( 
    IXMLNodeSource __RPC_FAR * This,
    /* [out] */ const WCHAR __RPC_FAR *__RPC_FAR *ppwcBuf);


void __RPC_STUB IXMLNodeSource_GetURL_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IXMLNodeSource_INTERFACE_DEFINED__ */


#ifndef __IXMLParser_INTERFACE_DEFINED__
#define __IXMLParser_INTERFACE_DEFINED__

/* interface IXMLParser */
/* [unique][helpstring][uuid][local][object] */ 


EXTERN_C const IID IID_IXMLParser;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("d242361e-51a0-11d2-9caf-0060b0ec3d39")
    IXMLParser : public IXMLNodeSource
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetURL( 
            /* [in] */ const WCHAR __RPC_FAR *pszBaseUrl,
            /* [in] */ const WCHAR __RPC_FAR *pszRelativeUrl,
            /* [in] */ BOOL fAsync) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Load( 
            /* [in] */ BOOL fFullyAvailable,
            /* [in] */ IMoniker __RPC_FAR *pimkName,
            /* [in] */ LPBC pibc,
            /* [in] */ DWORD grfMode) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetInput( 
            /* [in] */ IUnknown __RPC_FAR *pStm) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PushData( 
            /* [in] */ const char __RPC_FAR *pData,
            /* [in] */ ULONG ulChars,
            /* [in] */ BOOL fLastBuffer) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LoadDTD( 
            /* [in] */ const WCHAR __RPC_FAR *pszBaseUrl,
            /* [in] */ const WCHAR __RPC_FAR *pszRelativeUrl) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LoadEntity( 
            /* [in] */ const WCHAR __RPC_FAR *pszBaseUrl,
            /* [in] */ const WCHAR __RPC_FAR *pszRelativeUrl,
            /* [in] */ BOOL fpe) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ParseEntity( 
            /* [in] */ const WCHAR __RPC_FAR *pwcText,
            /* [in] */ ULONG ulLen,
            /* [in] */ BOOL fpe) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExpandEntity( 
            /* [in] */ const WCHAR __RPC_FAR *pwcText,
            /* [in] */ ULONG ulLen) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetRoot( 
            /* [in] */ PVOID pRoot) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRoot( 
            /* [in] */ PVOID __RPC_FAR *ppRoot) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Run( 
            /* [in] */ long lChars) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetParserState( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Suspend( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetFlags( 
            /* [in] */ ULONG iFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetSecureBaseURL( 
            /* [in] */ const WCHAR __RPC_FAR *pszBaseUrl) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSecureBaseURL( 
            /* [out] */ const WCHAR __RPC_FAR *__RPC_FAR *ppwcBuf) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IXMLParserVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IXMLParser __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IXMLParser __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IXMLParser __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *SetFactory )( 
            IXMLParser __RPC_FAR * This,
            /* [in] */ IXMLNodeFactory __RPC_FAR *pNodeFactory);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetFactory )( 
            IXMLParser __RPC_FAR * This,
            /* [out] */ IXMLNodeFactory __RPC_FAR *__RPC_FAR *ppNodeFactory);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Abort )( 
            IXMLParser __RPC_FAR * This,
            /* [in] */ BSTR bstrErrorInfo);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *GetLineNumber )( 
            IXMLParser __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *GetLinePosition )( 
            IXMLParser __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *GetAbsolutePosition )( 
            IXMLParser __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetLineBuffer )( 
            IXMLParser __RPC_FAR * This,
            /* [out] */ const WCHAR __RPC_FAR *__RPC_FAR *ppwcBuf,
            /* [out] */ ULONG __RPC_FAR *pulLen,
            /* [out] */ ULONG __RPC_FAR *pulStartPos);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetLastError )( 
            IXMLParser __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetErrorInfo )( 
            IXMLParser __RPC_FAR * This,
            /* [out] */ BSTR __RPC_FAR *pbstrErrorInfo);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *GetFlags )( 
            IXMLParser __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetURL )( 
            IXMLParser __RPC_FAR * This,
            /* [out] */ const WCHAR __RPC_FAR *__RPC_FAR *ppwcBuf);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *SetURL )( 
            IXMLParser __RPC_FAR * This,
            /* [in] */ const WCHAR __RPC_FAR *pszBaseUrl,
            /* [in] */ const WCHAR __RPC_FAR *pszRelativeUrl,
            /* [in] */ BOOL fAsync);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Load )( 
            IXMLParser __RPC_FAR * This,
            /* [in] */ BOOL fFullyAvailable,
            /* [in] */ IMoniker __RPC_FAR *pimkName,
            /* [in] */ LPBC pibc,
            /* [in] */ DWORD grfMode);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *SetInput )( 
            IXMLParser __RPC_FAR * This,
            /* [in] */ IUnknown __RPC_FAR *pStm);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *PushData )( 
            IXMLParser __RPC_FAR * This,
            /* [in] */ const char __RPC_FAR *pData,
            /* [in] */ ULONG ulChars,
            /* [in] */ BOOL fLastBuffer);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *LoadDTD )( 
            IXMLParser __RPC_FAR * This,
            /* [in] */ const WCHAR __RPC_FAR *pszBaseUrl,
            /* [in] */ const WCHAR __RPC_FAR *pszRelativeUrl);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *LoadEntity )( 
            IXMLParser __RPC_FAR * This,
            /* [in] */ const WCHAR __RPC_FAR *pszBaseUrl,
            /* [in] */ const WCHAR __RPC_FAR *pszRelativeUrl,
            /* [in] */ BOOL fpe);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *ParseEntity )( 
            IXMLParser __RPC_FAR * This,
            /* [in] */ const WCHAR __RPC_FAR *pwcText,
            /* [in] */ ULONG ulLen,
            /* [in] */ BOOL fpe);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *ExpandEntity )( 
            IXMLParser __RPC_FAR * This,
            /* [in] */ const WCHAR __RPC_FAR *pwcText,
            /* [in] */ ULONG ulLen);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *SetRoot )( 
            IXMLParser __RPC_FAR * This,
            /* [in] */ PVOID pRoot);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetRoot )( 
            IXMLParser __RPC_FAR * This,
            /* [in] */ PVOID __RPC_FAR *ppRoot);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Run )( 
            IXMLParser __RPC_FAR * This,
            /* [in] */ long lChars);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetParserState )( 
            IXMLParser __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Suspend )( 
            IXMLParser __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Reset )( 
            IXMLParser __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *SetFlags )( 
            IXMLParser __RPC_FAR * This,
            /* [in] */ ULONG iFlags);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *SetSecureBaseURL )( 
            IXMLParser __RPC_FAR * This,
            /* [in] */ const WCHAR __RPC_FAR *pszBaseUrl);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *GetSecureBaseURL )( 
            IXMLParser __RPC_FAR * This,
            /* [out] */ const WCHAR __RPC_FAR *__RPC_FAR *ppwcBuf);
        
        END_INTERFACE
    } IXMLParserVtbl;

    interface IXMLParser
    {
        CONST_VTBL struct IXMLParserVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXMLParser_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IXMLParser_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IXMLParser_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IXMLParser_SetFactory(This,pNodeFactory)	\
    (This)->lpVtbl -> SetFactory(This,pNodeFactory)

#define IXMLParser_GetFactory(This,ppNodeFactory)	\
    (This)->lpVtbl -> GetFactory(This,ppNodeFactory)

#define IXMLParser_Abort(This,bstrErrorInfo)	\
    (This)->lpVtbl -> Abort(This,bstrErrorInfo)

#define IXMLParser_GetLineNumber(This)	\
    (This)->lpVtbl -> GetLineNumber(This)

#define IXMLParser_GetLinePosition(This)	\
    (This)->lpVtbl -> GetLinePosition(This)

#define IXMLParser_GetAbsolutePosition(This)	\
    (This)->lpVtbl -> GetAbsolutePosition(This)

#define IXMLParser_GetLineBuffer(This,ppwcBuf,pulLen,pulStartPos)	\
    (This)->lpVtbl -> GetLineBuffer(This,ppwcBuf,pulLen,pulStartPos)

#define IXMLParser_GetLastError(This)	\
    (This)->lpVtbl -> GetLastError(This)

#define IXMLParser_GetErrorInfo(This,pbstrErrorInfo)	\
    (This)->lpVtbl -> GetErrorInfo(This,pbstrErrorInfo)

#define IXMLParser_GetFlags(This)	\
    (This)->lpVtbl -> GetFlags(This)

#define IXMLParser_GetURL(This,ppwcBuf)	\
    (This)->lpVtbl -> GetURL(This,ppwcBuf)


#define IXMLParser_SetURL(This,pszBaseUrl,pszRelativeUrl,fAsync)	\
    (This)->lpVtbl -> SetURL(This,pszBaseUrl,pszRelativeUrl,fAsync)

#define IXMLParser_Load(This,fFullyAvailable,pimkName,pibc,grfMode)	\
    (This)->lpVtbl -> Load(This,fFullyAvailable,pimkName,pibc,grfMode)

#define IXMLParser_SetInput(This,pStm)	\
    (This)->lpVtbl -> SetInput(This,pStm)

#define IXMLParser_PushData(This,pData,ulChars,fLastBuffer)	\
    (This)->lpVtbl -> PushData(This,pData,ulChars,fLastBuffer)

#define IXMLParser_LoadDTD(This,pszBaseUrl,pszRelativeUrl)	\
    (This)->lpVtbl -> LoadDTD(This,pszBaseUrl,pszRelativeUrl)

#define IXMLParser_LoadEntity(This,pszBaseUrl,pszRelativeUrl,fpe)	\
    (This)->lpVtbl -> LoadEntity(This,pszBaseUrl,pszRelativeUrl,fpe)

#define IXMLParser_ParseEntity(This,pwcText,ulLen,fpe)	\
    (This)->lpVtbl -> ParseEntity(This,pwcText,ulLen,fpe)

#define IXMLParser_ExpandEntity(This,pwcText,ulLen)	\
    (This)->lpVtbl -> ExpandEntity(This,pwcText,ulLen)

#define IXMLParser_SetRoot(This,pRoot)	\
    (This)->lpVtbl -> SetRoot(This,pRoot)

#define IXMLParser_GetRoot(This,ppRoot)	\
    (This)->lpVtbl -> GetRoot(This,ppRoot)

#define IXMLParser_Run(This,lChars)	\
    (This)->lpVtbl -> Run(This,lChars)

#define IXMLParser_GetParserState(This)	\
    (This)->lpVtbl -> GetParserState(This)

#define IXMLParser_Suspend(This)	\
    (This)->lpVtbl -> Suspend(This)

#define IXMLParser_Reset(This)	\
    (This)->lpVtbl -> Reset(This)

#define IXMLParser_SetFlags(This,iFlags)	\
    (This)->lpVtbl -> SetFlags(This,iFlags)

#define IXMLParser_SetSecureBaseURL(This,pszBaseUrl)	\
    (This)->lpVtbl -> SetSecureBaseURL(This,pszBaseUrl)

#define IXMLParser_GetSecureBaseURL(This,ppwcBuf)	\
    (This)->lpVtbl -> GetSecureBaseURL(This,ppwcBuf)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IXMLParser_SetURL_Proxy( 
    IXMLParser __RPC_FAR * This,
    /* [in] */ const WCHAR __RPC_FAR *pszBaseUrl,
    /* [in] */ const WCHAR __RPC_FAR *pszRelativeUrl,
    /* [in] */ BOOL fAsync);


void __RPC_STUB IXMLParser_SetURL_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLParser_Load_Proxy( 
    IXMLParser __RPC_FAR * This,
    /* [in] */ BOOL fFullyAvailable,
    /* [in] */ IMoniker __RPC_FAR *pimkName,
    /* [in] */ LPBC pibc,
    /* [in] */ DWORD grfMode);


void __RPC_STUB IXMLParser_Load_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLParser_SetInput_Proxy( 
    IXMLParser __RPC_FAR * This,
    /* [in] */ IUnknown __RPC_FAR *pStm);


void __RPC_STUB IXMLParser_SetInput_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLParser_PushData_Proxy( 
    IXMLParser __RPC_FAR * This,
    /* [in] */ const char __RPC_FAR *pData,
    /* [in] */ ULONG ulChars,
    /* [in] */ BOOL fLastBuffer);


void __RPC_STUB IXMLParser_PushData_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLParser_LoadDTD_Proxy( 
    IXMLParser __RPC_FAR * This,
    /* [in] */ const WCHAR __RPC_FAR *pszBaseUrl,
    /* [in] */ const WCHAR __RPC_FAR *pszRelativeUrl);


void __RPC_STUB IXMLParser_LoadDTD_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLParser_LoadEntity_Proxy( 
    IXMLParser __RPC_FAR * This,
    /* [in] */ const WCHAR __RPC_FAR *pszBaseUrl,
    /* [in] */ const WCHAR __RPC_FAR *pszRelativeUrl,
    /* [in] */ BOOL fpe);


void __RPC_STUB IXMLParser_LoadEntity_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLParser_ParseEntity_Proxy( 
    IXMLParser __RPC_FAR * This,
    /* [in] */ const WCHAR __RPC_FAR *pwcText,
    /* [in] */ ULONG ulLen,
    /* [in] */ BOOL fpe);


void __RPC_STUB IXMLParser_ParseEntity_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLParser_ExpandEntity_Proxy( 
    IXMLParser __RPC_FAR * This,
    /* [in] */ const WCHAR __RPC_FAR *pwcText,
    /* [in] */ ULONG ulLen);


void __RPC_STUB IXMLParser_ExpandEntity_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLParser_SetRoot_Proxy( 
    IXMLParser __RPC_FAR * This,
    /* [in] */ PVOID pRoot);


void __RPC_STUB IXMLParser_SetRoot_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLParser_GetRoot_Proxy( 
    IXMLParser __RPC_FAR * This,
    /* [in] */ PVOID __RPC_FAR *ppRoot);


void __RPC_STUB IXMLParser_GetRoot_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLParser_Run_Proxy( 
    IXMLParser __RPC_FAR * This,
    /* [in] */ long lChars);


void __RPC_STUB IXMLParser_Run_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLParser_GetParserState_Proxy( 
    IXMLParser __RPC_FAR * This);


void __RPC_STUB IXMLParser_GetParserState_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLParser_Suspend_Proxy( 
    IXMLParser __RPC_FAR * This);


void __RPC_STUB IXMLParser_Suspend_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLParser_Reset_Proxy( 
    IXMLParser __RPC_FAR * This);


void __RPC_STUB IXMLParser_Reset_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLParser_SetFlags_Proxy( 
    IXMLParser __RPC_FAR * This,
    /* [in] */ ULONG iFlags);


void __RPC_STUB IXMLParser_SetFlags_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLParser_SetSecureBaseURL_Proxy( 
    IXMLParser __RPC_FAR * This,
    /* [in] */ const WCHAR __RPC_FAR *pszBaseUrl);


void __RPC_STUB IXMLParser_SetSecureBaseURL_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLParser_GetSecureBaseURL_Proxy( 
    IXMLParser __RPC_FAR * This,
    /* [out] */ const WCHAR __RPC_FAR *__RPC_FAR *ppwcBuf);


void __RPC_STUB IXMLParser_GetSecureBaseURL_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IXMLParser_INTERFACE_DEFINED__ */


#ifndef __IXMLNodeFactory_INTERFACE_DEFINED__
#define __IXMLNodeFactory_INTERFACE_DEFINED__

/* interface IXMLNodeFactory */
/* [unique][helpstring][uuid][local][object] */ 


EXTERN_C const IID IID_IXMLNodeFactory;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("d242361f-51a0-11d2-9caf-0060b0ec3d39")
    IXMLNodeFactory : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE NotifyEvent( 
            /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
            /* [in] */ XML_NODEFACTORY_EVENT iEvt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE BeginChildren( 
            /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
            /* [in] */ XML_NODE_INFO __RPC_FAR *pNodeInfo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndChildren( 
            /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
            /* [in] */ BOOL fEmpty,
            /* [in] */ XML_NODE_INFO __RPC_FAR *pNodeInfo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Error( 
            /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
            /* [in] */ HRESULT hrErrorCode,
            /* [in] */ USHORT cNumRecs,
            /* [in] */ XML_NODE_INFO __RPC_FAR *__RPC_FAR *apNodeInfo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateNode( 
            /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
            /* [in] */ PVOID pNodeParent,
            /* [in] */ USHORT cNumRecs,
            /* [in] */ XML_NODE_INFO __RPC_FAR *__RPC_FAR *apNodeInfo) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IXMLNodeFactoryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *QueryInterface )( 
            IXMLNodeFactory __RPC_FAR * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void __RPC_FAR *__RPC_FAR *ppvObject);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *AddRef )( 
            IXMLNodeFactory __RPC_FAR * This);
        
        ULONG ( STDMETHODCALLTYPE __RPC_FAR *Release )( 
            IXMLNodeFactory __RPC_FAR * This);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *NotifyEvent )( 
            IXMLNodeFactory __RPC_FAR * This,
            /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
            /* [in] */ XML_NODEFACTORY_EVENT iEvt);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *BeginChildren )( 
            IXMLNodeFactory __RPC_FAR * This,
            /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
            /* [in] */ XML_NODE_INFO __RPC_FAR *pNodeInfo);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *EndChildren )( 
            IXMLNodeFactory __RPC_FAR * This,
            /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
            /* [in] */ BOOL fEmpty,
            /* [in] */ XML_NODE_INFO __RPC_FAR *pNodeInfo);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *Error )( 
            IXMLNodeFactory __RPC_FAR * This,
            /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
            /* [in] */ HRESULT hrErrorCode,
            /* [in] */ USHORT cNumRecs,
            /* [in] */ XML_NODE_INFO __RPC_FAR *__RPC_FAR *apNodeInfo);
        
        HRESULT ( STDMETHODCALLTYPE __RPC_FAR *CreateNode )( 
            IXMLNodeFactory __RPC_FAR * This,
            /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
            /* [in] */ PVOID pNodeParent,
            /* [in] */ USHORT cNumRecs,
            /* [in] */ XML_NODE_INFO __RPC_FAR *__RPC_FAR *apNodeInfo);
        
        END_INTERFACE
    } IXMLNodeFactoryVtbl;

    interface IXMLNodeFactory
    {
        CONST_VTBL struct IXMLNodeFactoryVtbl __RPC_FAR *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IXMLNodeFactory_QueryInterface(This,riid,ppvObject)	\
    (This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IXMLNodeFactory_AddRef(This)	\
    (This)->lpVtbl -> AddRef(This)

#define IXMLNodeFactory_Release(This)	\
    (This)->lpVtbl -> Release(This)


#define IXMLNodeFactory_NotifyEvent(This,pSource,iEvt)	\
    (This)->lpVtbl -> NotifyEvent(This,pSource,iEvt)

#define IXMLNodeFactory_BeginChildren(This,pSource,pNodeInfo)	\
    (This)->lpVtbl -> BeginChildren(This,pSource,pNodeInfo)

#define IXMLNodeFactory_EndChildren(This,pSource,fEmpty,pNodeInfo)	\
    (This)->lpVtbl -> EndChildren(This,pSource,fEmpty,pNodeInfo)

#define IXMLNodeFactory_Error(This,pSource,hrErrorCode,cNumRecs,apNodeInfo)	\
    (This)->lpVtbl -> Error(This,pSource,hrErrorCode,cNumRecs,apNodeInfo)

#define IXMLNodeFactory_CreateNode(This,pSource,pNodeParent,cNumRecs,apNodeInfo)	\
    (This)->lpVtbl -> CreateNode(This,pSource,pNodeParent,cNumRecs,apNodeInfo)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IXMLNodeFactory_NotifyEvent_Proxy( 
    IXMLNodeFactory __RPC_FAR * This,
    /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
    /* [in] */ XML_NODEFACTORY_EVENT iEvt);


void __RPC_STUB IXMLNodeFactory_NotifyEvent_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLNodeFactory_BeginChildren_Proxy( 
    IXMLNodeFactory __RPC_FAR * This,
    /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
    /* [in] */ XML_NODE_INFO __RPC_FAR *pNodeInfo);


void __RPC_STUB IXMLNodeFactory_BeginChildren_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLNodeFactory_EndChildren_Proxy( 
    IXMLNodeFactory __RPC_FAR * This,
    /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
    /* [in] */ BOOL fEmpty,
    /* [in] */ XML_NODE_INFO __RPC_FAR *pNodeInfo);


void __RPC_STUB IXMLNodeFactory_EndChildren_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLNodeFactory_Error_Proxy( 
    IXMLNodeFactory __RPC_FAR * This,
    /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
    /* [in] */ HRESULT hrErrorCode,
    /* [in] */ USHORT cNumRecs,
    /* [in] */ XML_NODE_INFO __RPC_FAR *__RPC_FAR *apNodeInfo);


void __RPC_STUB IXMLNodeFactory_Error_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IXMLNodeFactory_CreateNode_Proxy( 
    IXMLNodeFactory __RPC_FAR * This,
    /* [in] */ IXMLNodeSource __RPC_FAR *pSource,
    /* [in] */ PVOID pNodeParent,
    /* [in] */ USHORT cNumRecs,
    /* [in] */ XML_NODE_INFO __RPC_FAR *__RPC_FAR *apNodeInfo);


void __RPC_STUB IXMLNodeFactory_CreateNode_Stub(
    IRpcStubBuffer *This,
    IRpcChannelBuffer *_pRpcChannelBuffer,
    PRPC_MESSAGE _pRpcMessage,
    DWORD *_pdwStubPhase);



#endif 	/* __IXMLNodeFactory_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_XMLParser;

#ifdef __cplusplus

class DECLSPEC_UUID("d2423620-51a0-11d2-9caf-0060b0ec3d39")
XMLParser;
#endif
#endif /* __XMLPSR_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


