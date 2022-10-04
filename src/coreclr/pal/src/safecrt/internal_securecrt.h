// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*internal_securecrt.h - contains declarations of internal routines and variables for securecrt
*

*
*Purpose:
*       Declares routines and variables used internally in the SecureCRT implementation.
*       In this include file we define the macros needed to implement the secure functions
*       inlined in the *.inl files like tcscpy_s.inl, etc.
*       Note that this file is used for the CRT implementation, while internal_safecrt is used
*       to build the downlevel library safecrt.lib.
*
*       [Internal]
*
****/

#pragma once

#ifndef _INC_INTERNAL_SECURECRT
#define _INC_INTERNAL_SECURECRT

/* more VS specific goodness */
#define _In_
#define _In_z_
#define _In_opt_
#define _In_opt_z_
#define _Out_
#define _Out_opt_
#define _Out_writes_(size)
#define _Out_writes_opt_(size)
#define _Out_writes_bytes_(size)
#define _Out_writes_bytes_opt_(size)
#define _Out_writes_z_(size)
#define _Out_writes_opt_z_(size)

/*
 * The original SafeCRT implementation allows runtime control over buffer checking.
 * For now we'll key this off the debug flag.
 */
#ifdef _DEBUG
    #define _CrtGetCheckCount()                 ((int)1)
#else
    #define _CrtGetCheckCount()                 ((int)0)
#endif

/* Assert message and Invalid parameter */
#ifdef _DEBUG
    #define _ASSERT_EXPR( val, exp )                                            \
        {                                                                       \
            if ( ( val ) == 0 )                                                 \
            {                                                                   \
                if ( sMBUSafeCRTAssertFunc != NULL )                            \
                {                                                               \
                    ( *sMBUSafeCRTAssertFunc )( #exp, "SafeCRT assert failed", __FILE__, __LINE__ );    \
                }                                                               \
            }                                                                   \
        }
    #define _INVALID_PARAMETER( exp )   _ASSERT_EXPR( 0, exp )
    #define _ASSERTE( exp ) _ASSERT_EXPR( exp, exp )
#else
    #define _ASSERT_EXPR( val, expr )
    #define _INVALID_PARAMETER( exp )
    #define _ASSERTE( exp )
#endif

/* _TRUNCATE */
#if !defined (_TRUNCATE)
#define _TRUNCATE ((size_t)-1)
#endif  /* !defined (_TRUNCATE) */

/* #include <internal.h> */

#define _VALIDATE_RETURN_VOID( expr, errorcode )                               \
    {                                                                          \
        int _Expr_val=!!(expr);                                                \
        _ASSERT_EXPR( ( _Expr_val ), #expr );                       \
        if ( !( _Expr_val ) )                                                  \
        {                                                                      \
            errno = errorcode;                                                 \
            _INVALID_PARAMETER(#expr);                              \
            return;                                                            \
        }                                                                      \
    }

/*
 * Assert in debug builds.
 * set errno and return value
 */

#ifndef _VALIDATE_RETURN
#define _VALIDATE_RETURN( expr, errorcode, retexpr )                           \
    {                                                                          \
        int _Expr_val=!!(expr);                                                \
        _ASSERT_EXPR( ( _Expr_val ), #expr );                       \
        if ( !( _Expr_val ) )                                                  \
        {                                                                      \
            errno = errorcode;                                                 \
            _INVALID_PARAMETER(#expr );                             \
            return ( retexpr );                                                \
        }                                                                      \
    }
#endif  /* _VALIDATE_RETURN */

#ifndef _VALIDATE_RETURN_NOEXC
#define _VALIDATE_RETURN_NOEXC( expr, errorcode, retexpr )                     \
    {                                                                          \
        if ( !(expr) )                                                         \
        {                                                                      \
            errno = errorcode;                                                 \
            return ( retexpr );                                                \
        }                                                                      \
    }
#endif  /* _VALIDATE_RETURN_NOEXC */

/*
 * Assert in debug builds.
 * set errno and return errorcode
 */

#define _VALIDATE_RETURN_ERRCODE( expr, errorcode )                            \
    {                                                                          \
        int _Expr_val=!!(expr);                                                \
        _ASSERT_EXPR( ( _Expr_val ), _CRT_WIDE(#expr) );                       \
        if ( !( _Expr_val ) )                                                  \
        {                                                                      \
            errno = errorcode;                                                 \
            _INVALID_PARAMETER(_CRT_WIDE(#expr));                              \
            return ( errorcode );                                              \
        }                                                                      \
    }

/* We completely fill the buffer only in debug (see _SECURECRT__FILL_STRING
 * and _SECURECRT__FILL_BYTE macros).
 */
#if !defined (_SECURECRT_FILL_BUFFER)
#ifdef _DEBUG
#define _SECURECRT_FILL_BUFFER 1
#else  /* _DEBUG */
#define _SECURECRT_FILL_BUFFER 0
#endif  /* _DEBUG */
#endif  /* !defined (_SECURECRT_FILL_BUFFER) */

/* _SECURECRT_FILL_BUFFER_PATTERN is the same as _bNoMansLandFill */
#define _SECURECRT_FILL_BUFFER_PATTERN 0xFD

#if !defined (_SECURECRT_FILL_BUFFER_THRESHOLD)
#ifdef _DEBUG
#define _SECURECRT_FILL_BUFFER_THRESHOLD ((size_t)8)
#else  /* _DEBUG */
#define _SECURECRT_FILL_BUFFER_THRESHOLD ((size_t)0)
#endif  /* _DEBUG */
#endif  /* !defined (_SECURECRT_FILL_BUFFER_THRESHOLD) */

#if _SECURECRT_FILL_BUFFER
#define _SECURECRT__FILL_STRING(_String, _Size, _Offset)                            \
    if ((_Size) != ((size_t)-1) && (_Size) != INT_MAX &&                            \
        ((size_t)(_Offset)) < (_Size))                                              \
    {                                                                               \
        memset((_String) + (_Offset),                                               \
            _SECURECRT_FILL_BUFFER_PATTERN,                                         \
            (_SECURECRT_FILL_BUFFER_THRESHOLD < ((size_t)((_Size) - (_Offset))) ?   \
                _SECURECRT_FILL_BUFFER_THRESHOLD :                                  \
                ((_Size) - (_Offset))) * sizeof(*(_String)));                       \
    }
#else  /* _SECURECRT_FILL_BUFFER */
#define _SECURECRT__FILL_STRING(_String, _Size, _Offset)
#endif  /* _SECURECRT_FILL_BUFFER */

#if _SECURECRT_FILL_BUFFER
#define _SECURECRT__FILL_BYTE(_Position)                \
    if (_SECURECRT_FILL_BUFFER_THRESHOLD > 0)           \
    {                                                   \
        (_Position) = _SECURECRT_FILL_BUFFER_PATTERN;   \
    }
#else  /* _SECURECRT_FILL_BUFFER */
#define _SECURECRT__FILL_BYTE(_Position)
#endif  /* _SECURECRT_FILL_BUFFER */

/* string resetting */
#define _FILL_STRING _SECURECRT__FILL_STRING

#define _FILL_BYTE _SECURECRT__FILL_BYTE

#define _RESET_STRING(_String, _Size)           \
    {                                           \
        *(_String) = 0;                         \
        _FILL_STRING((_String), (_Size), 1);    \
    }

/* validations */
#define _VALIDATE_STRING_ERROR(_String, _Size, _Ret) \
    _VALIDATE_RETURN((_String) != NULL && (_Size) > 0, EINVAL, (_Ret))

#define _VALIDATE_STRING(_String, _Size) \
    _VALIDATE_STRING_ERROR((_String), (_Size), EINVAL)

#define _VALIDATE_POINTER_ERROR_RETURN(_Pointer, _ErrorCode, _Ret) \
    _VALIDATE_RETURN((_Pointer) != NULL, (_ErrorCode), (_Ret))

#define _VALIDATE_POINTER_ERROR(_Pointer, _Ret) \
    _VALIDATE_POINTER_ERROR_RETURN((_Pointer), EINVAL, (_Ret))

#define _VALIDATE_POINTER(_Pointer) \
    _VALIDATE_POINTER_ERROR((_Pointer), EINVAL)

#define _VALIDATE_CONDITION_ERROR_RETURN(_Condition, _ErrorCode, _Ret) \
    _VALIDATE_RETURN((_Condition), (_ErrorCode), (_Ret))

#define _VALIDATE_CONDITION_ERROR(_Condition, _Ret) \
    _VALIDATE_CONDITION_ERROR_RETURN((_Condition), EINVAL, (_Ret))

#define _VALIDATE_POINTER_RESET_STRING_ERROR(_Pointer, _String, _Size, _Ret) \
    if ((_Pointer) == NULL) \
    { \
        _RESET_STRING((_String), (_Size)); \
        _VALIDATE_POINTER_ERROR_RETURN((_Pointer), EINVAL, (_Ret)) \
    }

#define _VALIDATE_POINTER_RESET_STRING(_Pointer, _String, _Size) \
    _VALIDATE_POINTER_RESET_STRING_ERROR((_Pointer), (_String), (_Size), EINVAL)

#define _RETURN_BUFFER_TOO_SMALL_ERROR(_String, _Size, _Ret) \
    _VALIDATE_RETURN(("Buffer is too small" && 0), ERANGE, _Ret)

#define _RETURN_BUFFER_TOO_SMALL(_String, _Size) \
    _RETURN_BUFFER_TOO_SMALL_ERROR((_String), (_Size), ERANGE)

#define _RETURN_DEST_NOT_NULL_TERMINATED(_String, _Size) \
    _VALIDATE_RETURN(("String is not null terminated" && 0), EINVAL, EINVAL)

#define _RETURN_EINVAL \
    _VALIDATE_RETURN(("Invalid parameter" && 0), EINVAL, EINVAL)

#define _RETURN_ERROR(_Msg, _Ret) \
    _VALIDATE_RETURN(((_Msg), 0), EINVAL, _Ret)

/* returns without calling _invalid_parameter */
#define _RETURN_NO_ERROR \
    return 0

/* Note that _RETURN_TRUNCATE does not set errno */
#define _RETURN_TRUNCATE \
    return STRUNCATE

/* locale dependent */
#define _LOCALE_ARG \
    _LocInfo

#define _LOCALE_ARG_DECL \
    _locale_t _LOCALE_ARG

#define _LOCALE_UPDATE \
    _LocaleUpdate _LocUpdate(_LOCALE_ARG)

#define _LOCALE_SHORTCUT_TEST \
    _LocUpdate.GetLocaleT()->mbcinfo->ismbcodepage == 0

#define _USE_LOCALE_ARG 1

/* misc */
#define _ASSIGN_IF_NOT_NULL(_Pointer, _Value) \
    if ((_Pointer) != NULL) \
    { \
        *(_Pointer) = (_Value); \
    }

#endif  /* _INC_INTERNAL_SECURECRT */
