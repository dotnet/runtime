// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*sal.h - markers for documenting the semantics of APIs
*

*
*Purpose:
*       sal.h provides a set of annotations to describe how a function uses its
*       parameters - the assumptions it makes about them, and the guarantees it makes
*       upon finishing.
****/
#pragma once

/*==========================================================================

   The comments in this file are intended to give basic understanding of
   the usage of SAL, the Microsoft Source Code Annotation Language.
   For more details, please see http://go.microsoft.com/fwlink/?LinkID=242134

   The macros are defined in 3 layers, plus the structural set:

   _In_/_Out_/_Ret_ Layer:
   ----------------------
   This layer provides the highest abstraction and its macros should be used
   in most cases. These macros typically start with:
      _In_     : input parameter to a function, unmodified by called function
      _Out_    : output parameter, written to by called function, pointed-to
                 location not expected to be initialized prior to call
      _Outptr_ : like _Out_ when returned variable is a pointer type
                 (so param is pointer-to-pointer type). Called function
                 provides/allocated space.
      _Outref_ : like _Outptr_, except param is reference-to-pointer type.
      _Inout_  : inout parameter, read from and potentially modified by
                 called function.
      _Ret_    : for return values
      _Field_  : class/struct field invariants
   For common usage, this class of SAL provides the most concise annotations.
   Note that _In_/_Out_/_Inout_/_Outptr_ annotations are designed to be used
   with a parameter target. Using them with _At_ to specify non-parameter
   targets may yield unexpected results.

   This layer also includes a number of other properties that can be specified
   to extend the ability of code analysis, most notably:
      -- Designating parameters as format strings for printf/scanf/scanf_s
      -- Requesting stricter type checking for C enum parameters

   _Pre_/_Post_ Layer:
   ------------------
   The macros of this layer only should be used when there is no suitable macro
   in the _In_/_Out_ layer. Its macros start with _Pre_ or _Post_.
   This layer provides the most flexibility for annotations.

   Implementation Abstraction Layer:
   --------------------------------
   Macros from this layer should never be used directly. The layer only exists
   to hide the implementation of the annotation macros.

   Structural Layer:
   ----------------
   These annotations, like _At_ and _When_, are used with annotations from
   any of the other layers as modifiers, indicating exactly when and where
   the annotations apply.


   Common syntactic conventions:
   ----------------------------

   Usage:
   -----
   _In_, _Out_, _Inout_, _Pre_, _Post_, are for formal parameters.
   _Ret_, _Deref_ret_ must be used for return values.

   Nullness:
   --------
   If the parameter can be NULL as a precondition to the function, the
   annotation contains _opt. If the macro does not contain '_opt' the
   parameter cannot be NULL.

   If an out/inout parameter returns a null pointer as a postcondition, this is
   indicated by _Ret_maybenull_ or _result_maybenull_. If the macro is not
   of this form, then the result will not be NULL as a postcondition.
     _Outptr_ - output value is not NULL
     _Outptr_result_maybenull_ - output value might be NULL

   String Type:
   -----------
   _z: NullTerminated string
   for _In_ parameters the buffer must have the specified stringtype before the call
   for _Out_ parameters the buffer must have the specified stringtype after the call
   for _Inout_ parameters both conditions apply

   Extent Syntax:
   -------------
   Buffer sizes are expressed as element counts, unless the macro explicitly
   contains _byte_ or _bytes_. Some annotations specify two buffer sizes, in
   which case the second is used to indicate how much of the buffer is valid
   as a postcondition. This table outlines the precondition buffer allocation
   size, precondition number of valid elements, postcondition allocation size,
   and postcondition number of valid elements for representative buffer size
   annotations:
                                     Pre    |  Pre    |  Post   |  Post
                                     alloc  |  valid  |  alloc  |  valid
      Annotation                     elems  |  elems  |  elems  |  elems
      ----------                     ------------------------------------
      _In_reads_(s)                    s    |   s     |   s     |   s
      _Inout_updates_(s)               s    |   s     |   s     |   s
      _Inout_updates_to_(s,c)          s    |   s     |   s     |   c
      _Out_writes_(s)                  s    |   0     |   s     |   s
      _Out_writes_to_(s,c)             s    |   0     |   s     |   c
      _Outptr_result_buffer_(s)        ?    |   ?     |   s     |   s
      _Outptr_result_buffer_to_(s,c)   ?    |   ?     |   s     |   c

   For the _Outptr_ annotations, the buffer in question is at one level of
   dereference. The called function is responsible for supplying the buffer.

   Success and failure:
   -------------------
   The SAL concept of success allows functions to define expressions that can
   be tested by the caller, which if it evaluates to non-zero, indicates the
   function succeeded, which means that its postconditions are guaranteed to
   hold.  Otherwise, if the expression evaluates to zero, the function is
   considered to have failed, and the postconditions are not guaranteed.

   The success criteria can be specified with the _Success_(expr) annotation:
     _Success_(return != FALSE) BOOL
     PathCanonicalizeA(_Out_writes_(MAX_PATH) LPSTR pszBuf, LPCSTR pszPath) :
        pszBuf is only guaranteed to be NULL-terminated when TRUE is returned,
        and FALSE indicates failure. In common practice, callers check for zero
        vs. non-zero returns, so it is preferable to express the success
        criteria in terms of zero/non-zero, not checked for exactly TRUE.

   Functions can specify that some postconditions will still hold, even when
   the function fails, using _On_failure_(anno-list), or postconditions that
   hold regardless of success or failure using _Always_(anno-list).

   The annotation _Return_type_success_(expr) may be used with a typedef to
   give a default _Success_ criteria to all functions returning that type.
   This is the case for common Windows API status types, including
   HRESULT and NTSTATUS.  This may be overridden on a per-function basis by
   specifying a _Success_ annotation locally.

============================================================================*/

#define __ATTR_SAL

#ifndef _SAL_VERSION /*IFSTRIP=IGN*/
#define _SAL_VERSION 20
#endif

#ifdef _PREFAST_ // [

// choose attribute or __declspec implementation
#ifndef _USE_DECLSPECS_FOR_SAL // [
#define _USE_DECLSPECS_FOR_SAL 1
#endif // ]

#if _USE_DECLSPECS_FOR_SAL // [
#undef _USE_ATTRIBUTES_FOR_SAL
#define _USE_ATTRIBUTES_FOR_SAL 0
#elif !defined(_USE_ATTRIBUTES_FOR_SAL) // ][
#if _MSC_VER >= 1400 /*IFSTRIP=IGN*/ // [
#define _USE_ATTRIBUTES_FOR_SAL 1
#else // ][
#define _USE_ATTRIBUTES_FOR_SAL 0
#endif // ]
#endif // ]


#if !_USE_DECLSPECS_FOR_SAL // [
#if !_USE_ATTRIBUTES_FOR_SAL // [
#if _MSC_VER >= 1400 /*IFSTRIP=IGN*/ // [
#undef _USE_ATTRIBUTES_FOR_SAL
#define _USE_ATTRIBUTES_FOR_SAL 1
#else // ][
#undef _USE_DECLSPECS_FOR_SAL
#define _USE_DECLSPECS_FOR_SAL  1
#endif // ]
#endif // ]
#endif // ]

#else

// Disable expansion of SAL macros in non-Prefast mode to
// improve compiler throughput.
#ifndef _USE_DECLSPECS_FOR_SAL // [
#define _USE_DECLSPECS_FOR_SAL 0
#endif // ]
#ifndef _USE_ATTRIBUTES_FOR_SAL // [
#define _USE_ATTRIBUTES_FOR_SAL 0
#endif // ]

#endif // ]

// safeguard for MIDL and RC builds
#if _USE_DECLSPECS_FOR_SAL && ( defined( MIDL_PASS ) || defined(__midl) || defined(RC_INVOKED) || !defined(_PREFAST_) ) /*IFSTRIP=IGN*/ // [
#undef _USE_DECLSPECS_FOR_SAL
#define _USE_DECLSPECS_FOR_SAL 0
#endif // ]
#if _USE_ATTRIBUTES_FOR_SAL && ( !defined(_MSC_EXTENSIONS) || defined( MIDL_PASS ) || defined(__midl) || defined(RC_INVOKED) ) /*IFSTRIP=IGN*/ // [
#undef _USE_ATTRIBUTES_FOR_SAL
#define _USE_ATTRIBUTES_FOR_SAL 0
#endif // ]

#if _USE_DECLSPECS_FOR_SAL || _USE_ATTRIBUTES_FOR_SAL

// Special enum type for Y/N/M
enum __SAL_YesNo {_SAL_notpresent, _SAL_no, _SAL_maybe, _SAL_yes, _SAL_default};

#endif

#if defined(BUILD_WINDOWS) && !_USE_ATTRIBUTES_FOR_SAL /*IFSTRIP=IGN*/
#define _SAL1_Source_(Name, args, annotes) _SA_annotes3(SAL_name, #Name, "", "1") _GrouP_(annotes _SAL_nop_impl_)
#define _SAL1_1_Source_(Name, args, annotes) _SA_annotes3(SAL_name, #Name, "", "1.1") _GrouP_(annotes _SAL_nop_impl_)
#define _SAL1_2_Source_(Name, args, annotes) _SA_annotes3(SAL_name, #Name, "", "1.2") _GrouP_(annotes _SAL_nop_impl_)
#define _SAL2_Source_(Name, args, annotes) _SA_annotes3(SAL_name, #Name, "", "2") _GrouP_(annotes _SAL_nop_impl_)
#else
#define _SAL1_Source_(Name, args, annotes) _SA_annotes3(SAL_name, #Name, "", "1") _Group_(annotes _SAL_nop_impl_)
#define _SAL1_1_Source_(Name, args, annotes) _SA_annotes3(SAL_name, #Name, "", "1.1") _Group_(annotes _SAL_nop_impl_)
#define _SAL1_2_Source_(Name, args, annotes) _SA_annotes3(SAL_name, #Name, "", "1.2") _Group_(annotes _SAL_nop_impl_)
#define _SAL2_Source_(Name, args, annotes) _SA_annotes3(SAL_name, #Name, "", "2") _Group_(annotes _SAL_nop_impl_)
#endif

//============================================================================
//   Structural SAL:
//     These annotations modify the use of other annotations.  They may
//     express the annotation target (i.e. what parameter/field the annotation
//     applies to) or the condition under which the annotation is applicable.
//============================================================================

// _At_(target, annos) specifies that the annotations listed in 'annos' is to
// be applied to 'target' rather than to the identifier which is the current
// lexical target.
#define _At_(target, annos)            _At_impl_(target, annos _SAL_nop_impl_)

// _At_buffer_(target, iter, bound, annos) is similar to _At_, except that
// target names a buffer, and each annotation in annos is applied to each
// element of target up to bound, with the variable named in iter usable
// by the annotations to refer to relevant offsets within target.
#define _At_buffer_(target, iter, bound, annos)  _At_buffer_impl_(target, iter, bound, annos _SAL_nop_impl_)

// _When_(expr, annos) specifies that the annotations listed in 'annos' only
// apply when 'expr' evaluates to non-zero.
#define _When_(expr, annos)            _When_impl_(expr, annos _SAL_nop_impl_)
#define _Group_(annos)                 _Group_impl_(annos _SAL_nop_impl_)
#define _GrouP_(annos)                 _GrouP_impl_(annos _SAL_nop_impl_)

// <expr> indicates whether normal post conditions apply to a function
#define _Success_(expr)                  _SAL2_Source_(_Success_, (expr), _Success_impl_(expr))

// <expr> indicates whether post conditions apply to a function returning
// the type that this annotation is applied to
#define _Return_type_success_(expr)      _SAL2_Source_(_Return_type_success_, (expr), _Success_impl_(expr))

// Establish postconditions that apply only if the function does not succeed
#define _On_failure_(annos)              _On_failure_impl_(annos _SAL_nop_impl_)

// Establish postconditions that apply in both success and failure cases.
// Only applicable with functions that have  _Success_ or _Return_type_succss_.
#define _Always_(annos)                  _Always_impl_(annos _SAL_nop_impl_)

// Usable on a function definition. Asserts that a function declaration is
// in scope, and its annotations are to be used. There are no other annotations
// allowed on the function definition.
#define _Use_decl_annotations_         _Use_decl_anno_impl_

// _Notref_ may precede a _Deref_ or "real" annotation, and removes one
// level of dereference if the parameter is a C++ reference (&).  If the
// net deref on a "real" annotation is negative, it is simply discarded.
#define _Notref_                       _Notref_impl_

// Annotations for defensive programming styles.
#define _Pre_defensive_             _SA_annotes0(SAL_pre_defensive)
#define _Post_defensive_            _SA_annotes0(SAL_post_defensive)

#define _In_defensive_(annotes)     _Pre_defensive_ _Group_(annotes)
#define _Out_defensive_(annotes)    _Post_defensive_ _Group_(annotes)
#define _Inout_defensive_(annotes)  _Pre_defensive_ _Post_defensive_ _Group_(annotes)

//============================================================================
//   _In_\_Out_ Layer:
//============================================================================

// Reserved pointer parameters, must always be NULL.
#define _Reserved_                      _SAL2_Source_(_Reserved_, (), _Pre1_impl_(__null_impl))

// _Const_ allows specification that any namable memory location is considered
// readonly for a given call.
#define _Const_                         _SAL2_Source_(_Const_, (), _Pre1_impl_(__readaccess_impl_notref))


// Input parameters --------------------------

//   _In_ - Annotations for parameters where data is passed into the function, but not modified.
//          _In_ by itself can be used with non-pointer types (although it is redundant).

// e.g. void SetPoint( _In_ const POINT* pPT );
#define _In_                            _SAL2_Source_(_In_, (), _Pre1_impl_(__notnull_impl_notref) _Pre_valid_impl_ _Deref_pre1_impl_(__readaccess_impl_notref))
#define _In_opt_                        _SAL2_Source_(_In_opt_, (), _Pre1_impl_(__maybenull_impl_notref) _Pre_valid_impl_ _Deref_pre_readonly_)

// nullterminated 'in' parameters.
// e.g. void CopyStr( _In_z_ const char* szFrom, _Out_z_cap_(cchTo) char* szTo, size_t cchTo );
#define _In_z_                          _SAL2_Source_(_In_z_, (),     _In_     _Pre1_impl_(__zterm_impl))
#define _In_opt_z_                      _SAL2_Source_(_In_opt_z_, (), _In_opt_ _Pre1_impl_(__zterm_impl))


// 'input' buffers with given size

#define _In_reads_(size)               _SAL2_Source_(_In_reads_, (size), _Pre_count_(size)          _Deref_pre_readonly_)
#define _In_reads_opt_(size)           _SAL2_Source_(_In_reads_opt_, (size), _Pre_opt_count_(size)      _Deref_pre_readonly_)
#define _In_reads_bytes_(size)         _SAL2_Source_(_In_reads_bytes_, (size), _Pre_bytecount_(size)      _Deref_pre_readonly_)
#define _In_reads_bytes_opt_(size)     _SAL2_Source_(_In_reads_bytes_opt_, (size), _Pre_opt_bytecount_(size)  _Deref_pre_readonly_)
#define _In_reads_z_(size)             _SAL2_Source_(_In_reads_z_, (size), _In_reads_(size)     _Pre_z_)
#define _In_reads_opt_z_(size)         _SAL2_Source_(_In_reads_opt_z_, (size), _Pre_opt_count_(size)      _Deref_pre_readonly_     _Pre_opt_z_)
#define _In_reads_or_z_(size)          _SAL2_Source_(_In_reads_or_z_, (size), _In_ _When_(_String_length_(_Curr_) < (size), _Pre_z_) _When_(_String_length_(_Curr_) >= (size), _Pre1_impl_(__count_impl(size))))
#define _In_reads_or_z_opt_(size)      _SAL2_Source_(_In_reads_or_z_opt_, (size), _In_opt_ _When_(_String_length_(_Curr_) < (size), _Pre_z_) _When_(_String_length_(_Curr_) >= (size), _Pre1_impl_(__count_impl(size))))


// 'input' buffers valid to the given end pointer

#define _In_reads_to_ptr_(ptr)         _SAL2_Source_(_In_reads_to_ptr_, (ptr), _Pre_ptrdiff_count_(ptr)     _Deref_pre_readonly_)
#define _In_reads_to_ptr_opt_(ptr)     _SAL2_Source_(_In_reads_to_ptr_opt_, (ptr), _Pre_opt_ptrdiff_count_(ptr) _Deref_pre_readonly_)
#define _In_reads_to_ptr_z_(ptr)       _SAL2_Source_(_In_reads_to_ptr_z_, (ptr), _In_reads_to_ptr_(ptr) _Pre_z_)
#define _In_reads_to_ptr_opt_z_(ptr)   _SAL2_Source_(_In_reads_to_ptr_opt_z_, (ptr), _Pre_opt_ptrdiff_count_(ptr) _Deref_pre_readonly_  _Pre_opt_z_)



// Output parameters --------------------------

//   _Out_ - Annotations for pointer or reference parameters where data passed back to the caller.
//           These are mostly used where the pointer/reference is to a non-pointer type.
//           _Outptr_/_Outref) (see below) are typically used to return pointers via parameters.

// e.g. void GetPoint( _Out_ POINT* pPT );
#define _Out_                                  _SAL2_Source_(_Out_, (),     _Out_impl_)
#define _Out_opt_                              _SAL2_Source_(_Out_opt_, (), _Out_opt_impl_)

#define _Out_writes_(size)                     _SAL2_Source_(_Out_writes_, (size), _Pre_cap_(size)            _Post_valid_impl_)
#define _Out_writes_opt_(size)                 _SAL2_Source_(_Out_writes_opt_, (size), _Pre_opt_cap_(size)        _Post_valid_impl_)
#define _Out_writes_bytes_(size)               _SAL2_Source_(_Out_writes_bytes_, (size), _Pre_bytecap_(size)        _Post_valid_impl_)
#define _Out_writes_bytes_opt_(size)           _SAL2_Source_(_Out_writes_bytes_opt_, (size), _Pre_opt_bytecap_(size)    _Post_valid_impl_)
#define _Out_writes_z_(size)                   _SAL2_Source_(_Out_writes_z_, (size), _Pre_cap_(size)            _Post_valid_impl_ _Post_z_)
#define _Out_writes_opt_z_(size)               _SAL2_Source_(_Out_writes_opt_z_, (size), _Pre_opt_cap_(size)        _Post_valid_impl_ _Post_z_)

#define _Out_writes_to_(size,count)            _SAL2_Source_(_Out_writes_to_, (size,count), _Pre_cap_(size)            _Post_valid_impl_ _Post_count_(count))
#define _Out_writes_to_opt_(size,count)        _SAL2_Source_(_Out_writes_to_opt_, (size,count), _Pre_opt_cap_(size)        _Post_valid_impl_ _Post_count_(count))
#define _Out_writes_all_(size)                 _SAL2_Source_(_Out_writes_all_, (size), _Out_writes_to_(_Old_(size), _Old_(size)))
#define _Out_writes_all_opt_(size)             _SAL2_Source_(_Out_writes_all_opt_, (size), _Out_writes_to_opt_(_Old_(size), _Old_(size)))

#define _Out_writes_bytes_to_(size,count)      _SAL2_Source_(_Out_writes_bytes_to_, (size,count), _Pre_bytecap_(size)        _Post_valid_impl_ _Post_bytecount_(count))
#define _Out_writes_bytes_to_opt_(size,count)  _SAL2_Source_(_Out_writes_bytes_to_opt_, (size,count), _Pre_opt_bytecap_(size) _Post_valid_impl_ _Post_bytecount_(count))
#define _Out_writes_bytes_all_(size)           _SAL2_Source_(_Out_writes_bytes_all_, (size), _Out_writes_bytes_to_(_Old_(size), _Old_(size)))
#define _Out_writes_bytes_all_opt_(size)       _SAL2_Source_(_Out_writes_bytes_all_opt_, (size), _Out_writes_bytes_to_opt_(_Old_(size), _Old_(size)))

#define _Out_writes_to_ptr_(ptr)               _SAL2_Source_(_Out_writes_to_ptr_, (ptr), _Pre_ptrdiff_cap_(ptr)     _Post_valid_impl_)
#define _Out_writes_to_ptr_opt_(ptr)           _SAL2_Source_(_Out_writes_to_ptr_opt_, (ptr), _Pre_opt_ptrdiff_cap_(ptr) _Post_valid_impl_)
#define _Out_writes_to_ptr_z_(ptr)             _SAL2_Source_(_Out_writes_to_ptr_z_, (ptr), _Pre_ptrdiff_cap_(ptr)     _Post_valid_impl_ Post_z_)
#define _Out_writes_to_ptr_opt_z_(ptr)         _SAL2_Source_(_Out_writes_to_ptr_opt_z_, (ptr), _Pre_opt_ptrdiff_cap_(ptr) _Post_valid_impl_ Post_z_)


// Inout parameters ----------------------------

//   _Inout_ - Annotations for pointer or reference parameters where data is passed in and
//        potentially modified.
//          void ModifyPoint( _Inout_ POINT* pPT );
//          void ModifyPointByRef( _Inout_ POINT& pPT );

#define _Inout_                                _SAL2_Source_(_Inout_, (), _Prepost_valid_)
#define _Inout_opt_                            _SAL2_Source_(_Inout_opt_, (), _Prepost_opt_valid_)

// For modifying string buffers
//   void toupper( _Inout_z_ char* sz );
#define _Inout_z_                              _SAL2_Source_(_Inout_z_, (), _Prepost_z_)
#define _Inout_opt_z_                          _SAL2_Source_(_Inout_opt_z_, (), _Prepost_opt_z_)

// For modifying buffers with explicit element size
#define _Inout_updates_(size)                  _SAL2_Source_(_Inout_updates_, (size), _Pre_cap_(size)         _Pre_valid_impl_ _Post_valid_impl_)
#define _Inout_updates_opt_(size)              _SAL2_Source_(_Inout_updates_opt_, (size), _Pre_opt_cap_(size)     _Pre_valid_impl_ _Post_valid_impl_)
#define _Inout_updates_z_(size)                _SAL2_Source_(_Inout_updates_z_, (size), _Pre_cap_(size)         _Pre_valid_impl_ _Post_valid_impl_ _Pre1_impl_(__zterm_impl) _Post1_impl_(__zterm_impl))
#define _Inout_updates_opt_z_(size)            _SAL2_Source_(_Inout_updates_opt_z_, (size), _Pre_opt_cap_(size)     _Pre_valid_impl_ _Post_valid_impl_ _Pre1_impl_(__zterm_impl) _Post1_impl_(__zterm_impl))

#define _Inout_updates_to_(size,count)         _SAL2_Source_(_Inout_updates_to_, (size,count), _Out_writes_to_(size,count) _Pre_valid_impl_ _Pre1_impl_(__count_impl(count)))
#define _Inout_updates_to_opt_(size,count)     _SAL2_Source_(_Inout_updates_to_opt_, (size,count), _Out_writes_to_opt_(size,count) _Pre_valid_impl_ _Pre1_impl_(__count_impl(count)))

#define _Inout_updates_all_(size)              _SAL2_Source_(_Inout_updates_all_, (size), _Inout_updates_to_(_Old_(size), _Old_(size)))
#define _Inout_updates_all_opt_(size)          _SAL2_Source_(_Inout_updates_all_opt_, (size), _Inout_updates_to_opt_(_Old_(size), _Old_(size)))

// For modifying buffers with explicit byte size
#define _Inout_updates_bytes_(size)            _SAL2_Source_(_Inout_updates_bytes_, (size), _Pre_bytecap_(size)     _Pre_valid_impl_ _Post_valid_impl_)
#define _Inout_updates_bytes_opt_(size)        _SAL2_Source_(_Inout_updates_bytes_opt_, (size), _Pre_opt_bytecap_(size) _Pre_valid_impl_ _Post_valid_impl_)

#define _Inout_updates_bytes_to_(size,count)       _SAL2_Source_(_Inout_updates_bytes_to_, (size,count), _Out_writes_bytes_to_(size,count) _Pre_valid_impl_ _Pre1_impl_(__bytecount_impl(count)))
#define _Inout_updates_bytes_to_opt_(size,count)   _SAL2_Source_(_Inout_updates_bytes_to_opt_, (size,count), _Out_writes_bytes_to_opt_(size,count) _Pre_valid_impl_ _Pre1_impl_(__bytecount_impl(count)))

#define _Inout_updates_bytes_all_(size)        _SAL2_Source_(_Inout_updates_bytes_all_, (size), _Inout_updates_bytes_to_(_Old_(size), _Old_(size)))
#define _Inout_updates_bytes_all_opt_(size)    _SAL2_Source_(_Inout_updates_bytes_all_opt_, (size), _Inout_updates_bytes_to_opt_(_Old_(size), _Old_(size)))


// Pointer to pointer parameters -------------------------

//   _Outptr_ - Annotations for output params returning pointers
//      These describe parameters where the called function provides the buffer:
//        HRESULT SHStrDupW(_In_ LPCWSTR psz, _Outptr_ LPWSTR *ppwsz);
//      The caller passes the address of an LPWSTR variable as ppwsz, and SHStrDupW allocates
//      and initializes memory and returns the pointer to the new LPWSTR in *ppwsz.
//
//    _Outptr_opt_ - describes parameters that are allowed to be NULL.
//    _Outptr_*_result_maybenull_ - describes parameters where the called function might return NULL to the caller.
//
//    Example:
//       void MyFunc(_Outptr_opt_ int **ppData1, _Outptr_result_maybenull_ int **ppData2);
//    Callers:
//       MyFunc(NULL, NULL);           // error: parameter 2, ppData2, should not be NULL
//       MyFunc(&pData1, &pData2);     // ok: both non-NULL
//       if (*pData1 == *pData2) ...   // error: pData2 might be NULL after call

#define _Outptr_                         _SAL2_Source_(_Outptr_, (),                      _Out_impl_     _Deref_post2_impl_(__notnull_impl_notref,   __count_impl(1)))
#define _Outptr_result_maybenull_        _SAL2_Source_(_Outptr_result_maybenull_, (),     _Out_impl_     _Deref_post2_impl_(__maybenull_impl_notref, __count_impl(1)))
#define _Outptr_opt_                     _SAL2_Source_(_Outptr_opt_, (),                  _Out_opt_impl_ _Deref_post2_impl_(__notnull_impl_notref,   __count_impl(1)))
#define _Outptr_opt_result_maybenull_    _SAL2_Source_(_Outptr_opt_result_maybenull_, (), _Out_opt_impl_ _Deref_post2_impl_(__maybenull_impl_notref, __count_impl(1)))

// Annotations for _Outptr_ parameters returning pointers to null terminated strings.

#define _Outptr_result_z_                _SAL2_Source_(_Outptr_result_z_, (),               _Out_impl_     _Deref_post_z_)
#define _Outptr_opt_result_z_            _SAL2_Source_(_Outptr_opt_result_z_, (),           _Out_opt_impl_ _Deref_post_z_)
#define _Outptr_result_maybenull_z_      _SAL2_Source_(_Outptr_result_maybenull_z_, (),     _Out_impl_     _Deref_post_opt_z_)
#define _Outptr_opt_result_maybenull_z_  _SAL2_Source_(_Outptr_opt_result_maybenull_z_, (), _Out_opt_impl_ _Deref_post_opt_z_)

// Annotations for _Outptr_ parameters where the output pointer is set to NULL if the function fails.

#define _Outptr_result_nullonfailure_       _SAL2_Source_(_Outptr_result_nullonfailure_, (),     _Outptr_      _On_failure_(_Deref_post_null_))
#define _Outptr_opt_result_nullonfailure_   _SAL2_Source_(_Outptr_opt_result_nullonfailure_, (), _Outptr_opt_  _On_failure_(_Deref_post_null_))

// Annotations for _Outptr_ parameters which return a pointer to a ref-counted COM object,
// following the COM convention of setting the output to NULL on failure.
// The current implementation is identical to _Outptr_result_nullonfailure_.
// For pointers to types that are not COM objects, _Outptr_result_nullonfailure_ is preferred.

#define _COM_Outptr_                        _SAL2_Source_(_COM_Outptr_, (),                      _Outptr_                      _On_failure_(_Deref_post_null_))
#define _COM_Outptr_result_maybenull_       _SAL2_Source_(_COM_Outptr_result_maybenull_, (),     _Outptr_result_maybenull_     _On_failure_(_Deref_post_null_))
#define _COM_Outptr_opt_                    _SAL2_Source_(_COM_Outptr_opt_, (),                  _Outptr_opt_                  _On_failure_(_Deref_post_null_))
#define _COM_Outptr_opt_result_maybenull_   _SAL2_Source_(_COM_Outptr_opt_result_maybenull_, (), _Outptr_opt_result_maybenull_ _On_failure_(_Deref_post_null_))

// Annotations for _Outptr_ parameters returning a pointer to buffer with a specified number of elements/bytes

#define _Outptr_result_buffer_(size)                      _SAL2_Source_(_Outptr_result_buffer_, (size),               _Out_impl_     _Deref_post2_impl_(__notnull_impl_notref, __cap_impl(size)))
#define _Outptr_opt_result_buffer_(size)                  _SAL2_Source_(_Outptr_opt_result_buffer_, (size),           _Out_opt_impl_ _Deref_post2_impl_(__notnull_impl_notref, __cap_impl(size)))
#define _Outptr_result_buffer_to_(size, count)            _SAL2_Source_(_Outptr_result_buffer_to_, (size, count),     _Out_impl_     _Deref_post3_impl_(__notnull_impl_notref, __cap_impl(size), __count_impl(count)))
#define _Outptr_opt_result_buffer_to_(size, count)        _SAL2_Source_(_Outptr_opt_result_buffer_to_, (size, count), _Out_opt_impl_ _Deref_post3_impl_(__notnull_impl_notref, __cap_impl(size), __count_impl(count)))

#define _Outptr_result_buffer_all_(size)                  _SAL2_Source_(_Outptr_result_buffer_all_, (size),           _Out_impl_     _Deref_post2_impl_(__notnull_impl_notref, __count_impl(size)))
#define _Outptr_opt_result_buffer_all_(size)              _SAL2_Source_(_Outptr_opt_result_buffer_all_, (size),       _Out_opt_impl_ _Deref_post2_impl_(__notnull_impl_notref, __count_impl(size)))

#define _Outptr_result_buffer_maybenull_(size)               _SAL2_Source_(_Outptr_result_buffer_maybenull_, (size),               _Out_impl_     _Deref_post2_impl_(__maybenull_impl_notref, __cap_impl(size)))
#define _Outptr_opt_result_buffer_maybenull_(size)           _SAL2_Source_(_Outptr_opt_result_buffer_maybenull_, (size),           _Out_opt_impl_ _Deref_post2_impl_(__maybenull_impl_notref, __cap_impl(size)))
#define _Outptr_result_buffer_to_maybenull_(size, count)     _SAL2_Source_(_Outptr_result_buffer_to_maybenull_, (size, count),     _Out_impl_     _Deref_post3_impl_(__maybenull_impl_notref, __cap_impl(size), __count_impl(count)))
#define _Outptr_opt_result_buffer_to_maybenull_(size, count) _SAL2_Source_(_Outptr_opt_result_buffer_to_maybenull_, (size, count), _Out_opt_impl_ _Deref_post3_impl_(__maybenull_impl_notref, __cap_impl(size), __count_impl(count)))

#define _Outptr_result_buffer_all_maybenull_(size)           _SAL2_Source_(_Outptr_result_buffer_all_maybenull_, (size),           _Out_impl_     _Deref_post2_impl_(__maybenull_impl_notref, __count_impl(size)))
#define _Outptr_opt_result_buffer_all_maybenull_(size)       _SAL2_Source_(_Outptr_opt_result_buffer_all_maybenull_, (size),       _Out_opt_impl_ _Deref_post2_impl_(__maybenull_impl_notref, __count_impl(size)))

#define _Outptr_result_bytebuffer_(size)                     _SAL2_Source_(_Outptr_result_bytebuffer_, (size),                     _Out_impl_     _Deref_post2_impl_(__notnull_impl_notref, __bytecap_impl(size)))
#define _Outptr_opt_result_bytebuffer_(size)                 _SAL2_Source_(_Outptr_opt_result_bytebuffer_, (size),                 _Out_opt_impl_ _Deref_post2_impl_(__notnull_impl_notref, __bytecap_impl(size)))
#define _Outptr_result_bytebuffer_to_(size, count)           _SAL2_Source_(_Outptr_result_bytebuffer_to_, (size, count),           _Out_impl_     _Deref_post3_impl_(__notnull_impl_notref, __bytecap_impl(size), __bytecount_impl(count)))
#define _Outptr_opt_result_bytebuffer_to_(size, count)       _SAL2_Source_(_Outptr_opt_result_bytebuffer_to_, (size, count),       _Out_opt_impl_ _Deref_post3_impl_(__notnull_impl_notref, __bytecap_impl(size), __bytecount_impl(count)))

#define _Outptr_result_bytebuffer_all_(size)                 _SAL2_Source_(_Outptr_result_bytebuffer_all_, (size),                 _Out_impl_     _Deref_post2_impl_(__notnull_impl_notref, __bytecount_impl(size)))
#define _Outptr_opt_result_bytebuffer_all_(size)             _SAL2_Source_(_Outptr_opt_result_bytebuffer_all_, (size),             _Out_opt_impl_ _Deref_post2_impl_(__notnull_impl_notref, __bytecount_impl(size)))

#define _Outptr_result_bytebuffer_maybenull_(size)                 _SAL2_Source_(_Outptr_result_bytebuffer_maybenull_, (size),               _Out_impl_     _Deref_post2_impl_(__maybenull_impl_notref, __bytecap_impl(size)))
#define _Outptr_opt_result_bytebuffer_maybenull_(size)             _SAL2_Source_(_Outptr_opt_result_bytebuffer_maybenull_, (size),           _Out_opt_impl_ _Deref_post2_impl_(__maybenull_impl_notref, __bytecap_impl(size)))
#define _Outptr_result_bytebuffer_to_maybenull_(size, count)       _SAL2_Source_(_Outptr_result_bytebuffer_to_maybenull_, (size, count),     _Out_impl_     _Deref_post3_impl_(__maybenull_impl_notref, __bytecap_impl(size), __bytecount_impl(count)))
#define _Outptr_opt_result_bytebuffer_to_maybenull_(size, count)   _SAL2_Source_(_Outptr_opt_result_bytebuffer_to_maybenull_, (size, count), _Out_opt_impl_ _Deref_post3_impl_(__maybenull_impl_notref, __bytecap_impl(size), __bytecount_impl(count)))

#define _Outptr_result_bytebuffer_all_maybenull_(size)         _SAL2_Source_(_Outptr_result_bytebuffer_all_maybenull_, (size),               _Out_impl_     _Deref_post2_impl_(__maybenull_impl_notref, __bytecount_impl(size)))
#define _Outptr_opt_result_bytebuffer_all_maybenull_(size)     _SAL2_Source_(_Outptr_opt_result_bytebuffer_all_maybenull_, (size),           _Out_opt_impl_ _Deref_post2_impl_(__maybenull_impl_notref, __bytecount_impl(size)))

// Annotations for output reference to pointer parameters.

#define _Outref_                                               _SAL2_Source_(_Outref_, (),                  _Out_impl_ _Post_notnull_)
#define _Outref_result_maybenull_                              _SAL2_Source_(_Outref_result_maybenull_, (), _Pre2_impl_(__notnull_impl_notref, __cap_c_one_notref_impl) _Post_maybenull_ _Post_valid_impl_)

#define _Outref_result_buffer_(size)                           _SAL2_Source_(_Outref_result_buffer_, (size),                         _Outref_ _Post1_impl_(__cap_impl(size)))
#define _Outref_result_bytebuffer_(size)                       _SAL2_Source_(_Outref_result_bytebuffer_, (size),                     _Outref_ _Post1_impl_(__bytecap_impl(size)))
#define _Outref_result_buffer_to_(size, count)                 _SAL2_Source_(_Outref_result_buffer_to_, (size, count),               _Outref_result_buffer_(size) _Post1_impl_(__count_impl(count)))
#define _Outref_result_bytebuffer_to_(size, count)             _SAL2_Source_(_Outref_result_bytebuffer_to_, (size, count),           _Outref_result_bytebuffer_(size) _Post1_impl_(__bytecount_impl(count)))
#define _Outref_result_buffer_all_(size)                       _SAL2_Source_(_Outref_result_buffer_all_, (size),                     _Outref_result_buffer_to_(size, _Old_(size)))
#define _Outref_result_bytebuffer_all_(size)                   _SAL2_Source_(_Outref_result_bytebuffer_all_, (size),                 _Outref_result_bytebuffer_to_(size, _Old_(size)))

#define _Outref_result_buffer_maybenull_(size)                 _SAL2_Source_(_Outref_result_buffer_maybenull_, (size),               _Outref_result_maybenull_ _Post1_impl_(__cap_impl(size)))
#define _Outref_result_bytebuffer_maybenull_(size)             _SAL2_Source_(_Outref_result_bytebuffer_maybenull_, (size),           _Outref_result_maybenull_ _Post1_impl_(__bytecap_impl(size)))
#define _Outref_result_buffer_to_maybenull_(size, count)       _SAL2_Source_(_Outref_result_buffer_to_maybenull_, (size, count),     _Outref_result_buffer_maybenull_(size) _Post1_impl_(__count_impl(count)))
#define _Outref_result_bytebuffer_to_maybenull_(size, count)   _SAL2_Source_(_Outref_result_bytebuffer_to_maybenull_, (size, count), _Outref_result_bytebuffer_maybenull_(size) _Post1_impl_(__bytecount_impl(count)))
#define _Outref_result_buffer_all_maybenull_(size)             _SAL2_Source_(_Outref_result_buffer_all_maybenull_, (size),           _Outref_result_buffer_to_maybenull_(size, _Old_(size)))
#define _Outref_result_bytebuffer_all_maybenull_(size)         _SAL2_Source_(_Outref_result_bytebuffer_all_maybenull_, (size),       _Outref_result_bytebuffer_to_maybenull_(size, _Old_(size)))

// Annotations for output reference to pointer parameters that guarantee
// that the pointer is set to NULL on failure.
#define _Outref_result_nullonfailure_                          _SAL2_Source_(_Outref_result_nullonfailure_, (), _Outref_    _On_failure_(_Post_null_))

// Generic annotations to set output value of a by-pointer or by-reference parameter to null/zero on failure.
#define _Result_nullonfailure_                                 _SAL2_Source_(_Result_nullonfailure_, (), _On_failure_(_Notref_impl_ _Deref_impl_ _Post_null_))
#define _Result_zeroonfailure_                                 _SAL2_Source_(_Result_zeroonfailure_, (), _On_failure_(_Notref_impl_ _Deref_impl_ _Out_range_(==, 0)))


// return values -------------------------------

//
// _Ret_ annotations
//
// describing conditions that hold for return values after the call

// e.g. _Ret_z_ CString::operator const WCHAR*() const throw();
#define _Ret_z_                             _SAL2_Source_(_Ret_z_, (), _Ret2_impl_(__notnull_impl,  __zterm_impl) _Ret_valid_impl_)
#define _Ret_maybenull_z_                   _SAL2_Source_(_Ret_maybenull_z_, (), _Ret2_impl_(__maybenull_impl,__zterm_impl) _Ret_valid_impl_)

// used with allocated but not yet initialized objects
#define _Ret_notnull_                       _SAL2_Source_(_Ret_notnull_, (), _Ret1_impl_(__notnull_impl))
#define _Ret_maybenull_                     _SAL2_Source_(_Ret_maybenull_, (), _Ret1_impl_(__maybenull_impl))
#define _Ret_null_                          _SAL2_Source_(_Ret_null_, (), _Ret1_impl_(__null_impl))

// used with allocated and initialized objects
//    returns single valid object
#define _Ret_valid_                         _SAL2_Source_(_Ret_valid_, (), _Ret1_impl_(__notnull_impl_notref)   _Ret_valid_impl_)

//    returns pointer to initialized buffer of specified size
#define _Ret_writes_(size)                  _SAL2_Source_(_Ret_writes_, (size), _Ret2_impl_(__notnull_impl,  __count_impl(size))          _Ret_valid_impl_)
#define _Ret_writes_z_(size)                _SAL2_Source_(_Ret_writes_z_, (size), _Ret3_impl_(__notnull_impl,  __count_impl(size), __zterm_impl) _Ret_valid_impl_)
#define _Ret_writes_bytes_(size)            _SAL2_Source_(_Ret_writes_bytes_, (size), _Ret2_impl_(__notnull_impl,  __bytecount_impl(size))      _Ret_valid_impl_)
#define _Ret_writes_maybenull_(size)        _SAL2_Source_(_Ret_writes_maybenull_, (size), _Ret2_impl_(__maybenull_impl,__count_impl(size))          _Ret_valid_impl_)
#define _Ret_writes_maybenull_z_(size)      _SAL2_Source_(_Ret_writes_maybenull_z_, (size), _Ret3_impl_(__maybenull_impl,__count_impl(size),__zterm_impl)  _Ret_valid_impl_)
#define _Ret_writes_bytes_maybenull_(size)  _SAL2_Source_(_Ret_writes_bytes_maybenull_, (size), _Ret2_impl_(__maybenull_impl,__bytecount_impl(size))      _Ret_valid_impl_)

//    returns pointer to partially initialized buffer, with total size 'size' and initialized size 'count'
#define _Ret_writes_to_(size,count)                   _SAL2_Source_(_Ret_writes_to_, (size,count), _Ret3_impl_(__notnull_impl,  __cap_impl(size),     __count_impl(count))     _Ret_valid_impl_)
#define _Ret_writes_bytes_to_(size,count)             _SAL2_Source_(_Ret_writes_bytes_to_, (size,count), _Ret3_impl_(__notnull_impl,  __bytecap_impl(size), __bytecount_impl(count)) _Ret_valid_impl_)
#define _Ret_writes_to_maybenull_(size,count)         _SAL2_Source_(_Ret_writes_to_maybenull_, (size,count), _Ret3_impl_(__maybenull_impl,  __cap_impl(size),     __count_impl(count))     _Ret_valid_impl_)
#define _Ret_writes_bytes_to_maybenull_(size,count)   _SAL2_Source_(_Ret_writes_bytes_to_maybenull_, (size,count), _Ret3_impl_(__maybenull_impl,  __bytecap_impl(size), __bytecount_impl(count)) _Ret_valid_impl_)


// Annotations for strict type checking
#define _Points_to_data_        _SAL2_Source_(_Points_to_data_, (), _Pre_ _Points_to_data_impl_)
#define _Literal_               _SAL2_Source_(_Literal_, (), _Pre_ _Literal_impl_)
#define _Notliteral_            _SAL2_Source_(_Notliteral_, (), _Pre_ _Notliteral_impl_)

// Check the return value of a function e.g. _Check_return_ ErrorCode Foo();
#define _Check_return_           _SAL2_Source_(_Check_return_, (), _Check_return_impl_)
#define _Must_inspect_result_    _SAL2_Source_(_Must_inspect_result_, (), _Must_inspect_impl_ _Check_return_impl_)

// e.g. MyPrintF( _Printf_format_string_ const WCHAR* wzFormat, ... );
#define _Printf_format_string_  _SAL2_Source_(_Printf_format_string_, (), _Printf_format_string_impl_)
#define _Scanf_format_string_   _SAL2_Source_(_Scanf_format_string_, (), _Scanf_format_string_impl_)
#define _Scanf_s_format_string_  _SAL2_Source_(_Scanf_s_format_string_, (), _Scanf_s_format_string_impl_)

#define _Format_string_impl_(kind,where)  _SA_annotes2(SAL_IsFormatString2, kind, where)
#define _Printf_format_string_params_(x)  _SAL2_Source_(_Printf_format_string_params_, (x), _Format_string_impl_("printf", x))
#define _Scanf_format_string_params_(x)   _SAL2_Source_(_Scanf_format_string_params_, (x), _Format_string_impl_("scanf", x))
#define _Scanf_s_format_string_params_(x) _SAL2_Source_(_Scanf_s_format_string_params_, (x), _Format_string_impl_("scanf_s", x))

// annotations to express value of integral or pointer parameter
#define _In_range_(lb,ub)           _SAL2_Source_(_In_range_, (lb,ub), _In_range_impl_(lb,ub))
#define _Out_range_(lb,ub)          _SAL2_Source_(_Out_range_, (lb,ub), _Out_range_impl_(lb,ub))
#define _Ret_range_(lb,ub)          _SAL2_Source_(_Ret_range_, (lb,ub), _Ret_range_impl_(lb,ub))
#define _Deref_in_range_(lb,ub)     _SAL2_Source_(_Deref_in_range_, (lb,ub), _Deref_in_range_impl_(lb,ub))
#define _Deref_out_range_(lb,ub)    _SAL2_Source_(_Deref_out_range_, (lb,ub), _Deref_out_range_impl_(lb,ub))
#define _Deref_ret_range_(lb,ub)    _SAL2_Source_(_Deref_ret_range_, (lb,ub), _Deref_ret_range_impl_(lb,ub))
#define _Pre_equal_to_(expr)        _SAL2_Source_(_Pre_equal_to_, (expr), _In_range_(==, expr))
#define _Post_equal_to_(expr)       _SAL2_Source_(_Post_equal_to_, (expr), _Out_range_(==, expr))

// annotation to express that a value (usually a field of a mutable class)
// is not changed by a function call
#define _Unchanged_(e)              _SAL2_Source_(_Unchanged_, (e), _At_(e, _Post_equal_to_(_Old_(e)) _Const_))

// Annotations to allow expressing generalized pre and post conditions.
// 'cond' may be any valid SAL expression that is considered to be true as a precondition
// or postcondition (respsectively).
#define _Pre_satisfies_(cond)       _SAL2_Source_(_Pre_satisfies_, (cond), _Pre_satisfies_impl_(cond))
#define _Post_satisfies_(cond)      _SAL2_Source_(_Post_satisfies_, (cond), _Post_satisfies_impl_(cond))

// Annotations to express struct, class and field invariants
#define _Struct_size_bytes_(size)                  _SAL2_Source_(_Struct_size_bytes_, (size), _Writable_bytes_(size))

#define _Field_size_(size)                         _SAL2_Source_(_Field_size_, (size), _Notnull_   _Writable_elements_(size))
#define _Field_size_opt_(size)                     _SAL2_Source_(_Field_size_opt_, (size), _Maybenull_ _Writable_elements_(size))
#define _Field_size_part_(size, count)             _SAL2_Source_(_Field_size_part_, (size, count), _Notnull_   _Writable_elements_(size) _Readable_elements_(count))
#define _Field_size_part_opt_(size, count)         _SAL2_Source_(_Field_size_part_opt_, (size, count), _Maybenull_ _Writable_elements_(size) _Readable_elements_(count))
#define _Field_size_full_(size)                    _SAL2_Source_(_Field_size_full_, (size), _Field_size_part_(size, size))
#define _Field_size_full_opt_(size)                _SAL2_Source_(_Field_size_full_opt_, (size), _Field_size_part_opt_(size, size))

#define _Field_size_bytes_(size)                   _SAL2_Source_(_Field_size_bytes_, (size), _Notnull_   _Writable_bytes_(size))
#define _Field_size_bytes_opt_(size)               _SAL2_Source_(_Field_size_bytes_opt_, (size), _Maybenull_ _Writable_bytes_(size))
#define _Field_size_bytes_part_(size, count)       _SAL2_Source_(_Field_size_bytes_part_, (size, count), _Notnull_   _Writable_bytes_(size) _Readable_bytes_(count))
#define _Field_size_bytes_part_opt_(size, count)   _SAL2_Source_(_Field_size_bytes_part_opt_, (size, count), _Maybenull_ _Writable_bytes_(size) _Readable_bytes_(count))
#define _Field_size_bytes_full_(size)              _SAL2_Source_(_Field_size_bytes_full_, (size), _Field_size_bytes_part_(size, size))
#define _Field_size_bytes_full_opt_(size)          _SAL2_Source_(_Field_size_bytes_full_opt_, (size), _Field_size_bytes_part_opt_(size, size))

#define _Field_z_                                  _SAL2_Source_(_Field_z_, (), _Null_terminated_)

#define _Field_range_(min,max)                     _SAL2_Source_(_Field_range_, (min,max), _Field_range_impl_(min,max))

//============================================================================
//   _Pre_\_Post_ Layer:
//============================================================================

//
// Raw Pre/Post for declaring custom pre/post conditions
//

#define _Pre_                             _Pre_impl_
#define _Post_                            _Post_impl_

//
// Validity property
//

#define _Valid_                           _Valid_impl_
#define _Notvalid_                        _Notvalid_impl_
#define _Maybevalid_                      _Maybevalid_impl_

//
// Buffer size properties
//

// Expressing buffer sizes without specifying pre or post condition
#define _Readable_bytes_(size)            _SAL2_Source_(_Readable_bytes_, (size), _Readable_bytes_impl_(size))
#define _Readable_elements_(size)         _SAL2_Source_(_Readable_elements_, (size), _Readable_elements_impl_(size))
#define _Writable_bytes_(size)            _SAL2_Source_(_Writable_bytes_, (size), _Writable_bytes_impl_(size))
#define _Writable_elements_(size)         _SAL2_Source_(_Writable_elements_, (size), _Writable_elements_impl_(size))

#define _Null_terminated_                 _SAL2_Source_(_Null_terminated_, (), _Null_terminated_impl_)
#define _NullNull_terminated_             _SAL2_Source_(_NullNull_terminated_, (), _NullNull_terminated_impl_)

// Expressing buffer size as pre or post condition
#define _Pre_readable_size_(size)         _SAL2_Source_(_Pre_readable_size_, (size), _Pre1_impl_(__count_impl(size))      _Pre_valid_impl_)
#define _Pre_writable_size_(size)         _SAL2_Source_(_Pre_writable_size_, (size), _Pre1_impl_(__cap_impl(size)))
#define _Pre_readable_byte_size_(size)    _SAL2_Source_(_Pre_readable_byte_size_, (size), _Pre1_impl_(__bytecount_impl(size))  _Pre_valid_impl_)
#define _Pre_writable_byte_size_(size)    _SAL2_Source_(_Pre_writable_byte_size_, (size), _Pre1_impl_(__bytecap_impl(size)))

#define _Post_readable_size_(size)        _SAL2_Source_(_Post_readable_size_, (size), _Post1_impl_(__count_impl(size))     _Post_valid_impl_)
#define _Post_writable_size_(size)        _SAL2_Source_(_Post_writable_size_, (size), _Post1_impl_(__cap_impl(size)))
#define _Post_readable_byte_size_(size)   _SAL2_Source_(_Post_readable_byte_size_, (size), _Post1_impl_(__bytecount_impl(size)) _Post_valid_impl_)
#define _Post_writable_byte_size_(size)   _SAL2_Source_(_Post_writable_byte_size_, (size), _Post1_impl_(__bytecap_impl(size)))

//
// Pointer null-ness properties
//
#define _Null_                            _Null_impl_
#define _Notnull_                         _Notnull_impl_
#define _Maybenull_                       _Maybenull_impl_

//
// _Pre_ annotations ---
//
// describing conditions that must be met before the call of the function

// e.g. int strlen( _Pre_z_ const char* sz );
// buffer is a zero terminated string
#define _Pre_z_                           _SAL2_Source_(_Pre_z_, (), _Pre1_impl_(__zterm_impl) _Pre_valid_impl_)

// valid size unknown or indicated by type (e.g.:LPSTR)
#define _Pre_valid_                       _SAL2_Source_(_Pre_valid_, (), _Pre1_impl_(__notnull_impl_notref)   _Pre_valid_impl_)
#define _Pre_opt_valid_                   _SAL2_Source_(_Pre_opt_valid_, (), _Pre1_impl_(__maybenull_impl_notref) _Pre_valid_impl_)

#define _Pre_invalid_                     _SAL2_Source_(_Pre_invalid_, (), _Deref_pre1_impl_(__notvalid_impl))

// Overrides recursive valid when some field is not yet initialized when using _Inout_
#define _Pre_unknown_                     _SAL2_Source_(_Pre_unknown_, (), _Pre1_impl_(__maybevalid_impl))

// used with allocated but not yet initialized objects
#define _Pre_notnull_                     _SAL2_Source_(_Pre_notnull_, (), _Pre1_impl_(__notnull_impl_notref))
#define _Pre_maybenull_                   _SAL2_Source_(_Pre_maybenull_, (), _Pre1_impl_(__maybenull_impl_notref))
#define _Pre_null_                        _SAL2_Source_(_Pre_null_, (), _Pre1_impl_(__null_impl_notref))

//
// _Post_ annotations ---
//
// describing conditions that hold after the function call

// void CopyStr( _In_z_ const char* szFrom, _Pre_cap_(cch) _Post_z_ char* szFrom, size_t cchFrom );
// buffer will be a zero-terminated string after the call
#define _Post_z_                         _SAL2_Source_(_Post_z_, (), _Post1_impl_(__zterm_impl) _Post_valid_impl_)

// e.g. HRESULT InitStruct( _Post_valid_ Struct* pobj );
#define _Post_valid_                     _SAL2_Source_(_Post_valid_, (), _Post_valid_impl_)
#define _Post_invalid_                   _SAL2_Source_(_Post_invalid_, (), _Deref_post1_impl_(__notvalid_impl))

// e.g. void free( _Post_ptr_invalid_ void* pv );
#define _Post_ptr_invalid_               _SAL2_Source_(_Post_ptr_invalid_, (), _Post1_impl_(__notvalid_impl))

// e.g. void ThrowExceptionIfNull( _Post_notnull_ const void* pv );
#define _Post_notnull_                   _SAL2_Source_(_Post_notnull_, (), _Post1_impl_(__notnull_impl))

// e.g. HRESULT GetObject(_Outptr_ _On_failure_(_At_(*p, _Post_null_)) T **p);
#define _Post_null_                      _SAL2_Source_(_Post_null_, (), _Post1_impl_(__null_impl))

#define _Post_maybenull_                 _SAL2_Source_(_Post_maybenull_, (), _Post1_impl_(__maybenull_impl))

#define _Prepost_z_                      _SAL2_Source_(_Prepost_z_, (), _Pre_z_      _Post_z_)


// #pragma region Input Buffer SAL 1 compatibility macros

/*==========================================================================

   This section contains definitions for macros defined for VS2010 and earlier.
   Usage of these macros is still supported, but the SAL 2 macros defined above
   are recommended instead.  This comment block is retained to assist in
   understanding SAL that still uses the older syntax.

   The macros are defined in 3 layers:

   _In_\_Out_ Layer:
   ----------------
   This layer provides the highest abstraction and its macros should be used
   in most cases. Its macros start with _In_, _Out_ or _Inout_. For the
   typical case they provide the most concise annotations.

   _Pre_\_Post_ Layer:
   ------------------
   The macros of this layer only should be used when there is no suitable macro
   in the _In_\_Out_ layer. Its macros start with _Pre_, _Post_, _Ret_,
   _Deref_pre_ _Deref_post_ and _Deref_ret_. This layer provides the most
   flexibility for annotations.

   Implementation Abstraction Layer:
   --------------------------------
   Macros from this layer should never be used directly. The layer only exists
   to hide the implementation of the annotation macros.


   Annotation Syntax:
   |--------------|----------|----------------|-----------------------------|
   |   Usage      | Nullness | ZeroTerminated |  Extent                     |
   |--------------|----------|----------------|-----------------------------|
   | _In_         | <>       | <>             | <>                          |
   | _Out_        | opt_     | z_             | [byte]cap_[c_|x_]( size )   |
   | _Inout_      |          |                | [byte]count_[c_|x_]( size ) |
   | _Deref_out_  |          |                | ptrdiff_cap_( ptr )         |
   |--------------|          |                | ptrdiff_count_( ptr )       |
   | _Ret_        |          |                |                             |
   | _Deref_ret_  |          |                |                             |
   |--------------|          |                |                             |
   | _Pre_        |          |                |                             |
   | _Post_       |          |                |                             |
   | _Deref_pre_  |          |                |                             |
   | _Deref_post_ |          |                |                             |
   |--------------|----------|----------------|-----------------------------|

   Usage:
   -----
   _In_, _Out_, _Inout_, _Pre_, _Post_, _Deref_pre_, _Deref_post_ are for
   formal parameters.
   _Ret_, _Deref_ret_ must be used for return values.

   Nullness:
   --------
   If the pointer can be NULL the annotation contains _opt. If the macro
   does not contain '_opt' the pointer may not be NULL.

   String Type:
   -----------
   _z: NullTerminated string
   for _In_ parameters the buffer must have the specified stringtype before the call
   for _Out_ parameters the buffer must have the specified stringtype after the call
   for _Inout_ parameters both conditions apply

   Extent Syntax:
   |------|---------------|---------------|
   | Unit | Writ\Readable | Argument Type |
   |------|---------------|---------------|
   |  <>  | cap_          | <>            |
   | byte | count_        | c_            |
   |      |               | x_            |
   |------|---------------|---------------|

   'cap' (capacity) describes the writable size of the buffer and is typically used
   with _Out_. The default unit is elements. Use 'bytecap' if the size is given in bytes
   'count' describes the readable size of the buffer and is typically used with _In_.
   The default unit is elements. Use 'bytecount' if the size is given in bytes.

   Argument syntax for cap_, bytecap_, count_, bytecount_:
   (<parameter>|return)[+n]  e.g. cch, return, cb+2

   If the buffer size is a constant expression use the c_ postfix.
   E.g. cap_c_(20), count_c_(MAX_PATH), bytecount_c_(16)

   If the buffer size is given by a limiting pointer use the ptrdiff_ versions
   of the macros.

   If the buffer size is neither a parameter nor a constant expression use the x_
   postfix. e.g. bytecount_x_(num*size) x_ annotations accept any arbitrary string.
   No analysis can be done for x_ annotations but they at least tell the tool that
   the buffer has some sort of extent description. x_ annotations might be supported
   by future compiler versions.

============================================================================*/

// e.g. void SetCharRange( _In_count_(cch) const char* rgch, size_t cch )
// valid buffer extent described by another parameter
#define _In_count_(size)               _SAL1_1_Source_(_In_count_, (size), _Pre_count_(size)         _Deref_pre_readonly_)
#define _In_opt_count_(size)           _SAL1_1_Source_(_In_opt_count_, (size), _Pre_opt_count_(size)     _Deref_pre_readonly_)
#define _In_bytecount_(size)           _SAL1_1_Source_(_In_bytecount_, (size), _Pre_bytecount_(size)     _Deref_pre_readonly_)
#define _In_opt_bytecount_(size)       _SAL1_1_Source_(_In_opt_bytecount_, (size), _Pre_opt_bytecount_(size) _Deref_pre_readonly_)

// valid buffer extent described by a constant extression
#define _In_count_c_(size)             _SAL1_1_Source_(_In_count_c_, (size), _Pre_count_c_(size)         _Deref_pre_readonly_)
#define _In_opt_count_c_(size)         _SAL1_1_Source_(_In_opt_count_c_, (size), _Pre_opt_count_c_(size)     _Deref_pre_readonly_)
#define _In_bytecount_c_(size)         _SAL1_1_Source_(_In_bytecount_c_, (size), _Pre_bytecount_c_(size)     _Deref_pre_readonly_)
#define _In_opt_bytecount_c_(size)     _SAL1_1_Source_(_In_opt_bytecount_c_, (size), _Pre_opt_bytecount_c_(size) _Deref_pre_readonly_)

// nullterminated  'input' buffers with given size

// e.g. void SetCharRange( _In_count_(cch) const char* rgch, size_t cch )
// nullterminated valid buffer extent described by another parameter
#define _In_z_count_(size)               _SAL1_1_Source_(_In_z_count_, (size), _Pre_z_ _Pre_count_(size)         _Deref_pre_readonly_)
#define _In_opt_z_count_(size)           _SAL1_1_Source_(_In_opt_z_count_, (size), _Pre_opt_z_ _Pre_opt_count_(size)     _Deref_pre_readonly_)
#define _In_z_bytecount_(size)           _SAL1_1_Source_(_In_z_bytecount_, (size), _Pre_z_ _Pre_bytecount_(size)     _Deref_pre_readonly_)
#define _In_opt_z_bytecount_(size)       _SAL1_1_Source_(_In_opt_z_bytecount_, (size), _Pre_opt_z_ _Pre_opt_bytecount_(size) _Deref_pre_readonly_)

// nullterminated valid buffer extent described by a constant extression
#define _In_z_count_c_(size)             _SAL1_1_Source_(_In_z_count_c_, (size), _Pre_z_ _Pre_count_c_(size)         _Deref_pre_readonly_)
#define _In_opt_z_count_c_(size)         _SAL1_1_Source_(_In_opt_z_count_c_, (size), _Pre_opt_z_ _Pre_opt_count_c_(size)     _Deref_pre_readonly_)
#define _In_z_bytecount_c_(size)         _SAL1_1_Source_(_In_z_bytecount_c_, (size), _Pre_z_ _Pre_bytecount_c_(size)     _Deref_pre_readonly_)
#define _In_opt_z_bytecount_c_(size)     _SAL1_1_Source_(_In_opt_z_bytecount_c_, (size), _Pre_opt_z_ _Pre_opt_bytecount_c_(size) _Deref_pre_readonly_)

// buffer capacity is described by another pointer
// e.g. void Foo( _In_ptrdiff_count_(pchMax) const char* pch, const char* pchMax ) { while pch < pchMax ) pch++; }
#define _In_ptrdiff_count_(size)       _SAL1_1_Source_(_In_ptrdiff_count_, (size), _Pre_ptrdiff_count_(size)     _Deref_pre_readonly_)
#define _In_opt_ptrdiff_count_(size)   _SAL1_1_Source_(_In_opt_ptrdiff_count_, (size), _Pre_opt_ptrdiff_count_(size) _Deref_pre_readonly_)

// 'x' version for complex expressions that are not supported by the current compiler version
// e.g. void Set3ColMatrix( _In_count_x_(3*cRows) const Elem* matrix, int cRows );
#define _In_count_x_(size)             _SAL1_1_Source_(_In_count_x_, (size), _Pre_count_x_(size)         _Deref_pre_readonly_)
#define _In_opt_count_x_(size)         _SAL1_1_Source_(_In_opt_count_x_, (size), _Pre_opt_count_x_(size)     _Deref_pre_readonly_)
#define _In_bytecount_x_(size)         _SAL1_1_Source_(_In_bytecount_x_, (size), _Pre_bytecount_x_(size)     _Deref_pre_readonly_)
#define _In_opt_bytecount_x_(size)     _SAL1_1_Source_(_In_opt_bytecount_x_, (size), _Pre_opt_bytecount_x_(size) _Deref_pre_readonly_)


// 'out' with buffer size
// e.g. void GetIndices( _Out_cap_(cIndices) int* rgIndices, size_t cIndices );
// buffer capacity is described by another parameter
#define _Out_cap_(size)                   _SAL1_1_Source_(_Out_cap_, (size), _Pre_cap_(size)           _Post_valid_impl_)
#define _Out_opt_cap_(size)               _SAL1_1_Source_(_Out_opt_cap_, (size), _Pre_opt_cap_(size)       _Post_valid_impl_)
#define _Out_bytecap_(size)               _SAL1_1_Source_(_Out_bytecap_, (size), _Pre_bytecap_(size)       _Post_valid_impl_)
#define _Out_opt_bytecap_(size)           _SAL1_1_Source_(_Out_opt_bytecap_, (size), _Pre_opt_bytecap_(size)   _Post_valid_impl_)

// buffer capacity is described by a constant expression
#define _Out_cap_c_(size)                 _SAL1_1_Source_(_Out_cap_c_, (size), _Pre_cap_c_(size)         _Post_valid_impl_)
#define _Out_opt_cap_c_(size)             _SAL1_1_Source_(_Out_opt_cap_c_, (size), _Pre_opt_cap_c_(size)     _Post_valid_impl_)
#define _Out_bytecap_c_(size)             _SAL1_1_Source_(_Out_bytecap_c_, (size), _Pre_bytecap_c_(size)     _Post_valid_impl_)
#define _Out_opt_bytecap_c_(size)         _SAL1_1_Source_(_Out_opt_bytecap_c_, (size), _Pre_opt_bytecap_c_(size) _Post_valid_impl_)

// buffer capacity is described by another parameter multiplied by a constant expression
#define _Out_cap_m_(mult,size)            _SAL1_1_Source_(_Out_cap_m_, (mult,size), _Pre_cap_m_(mult,size)     _Post_valid_impl_)
#define _Out_opt_cap_m_(mult,size)        _SAL1_1_Source_(_Out_opt_cap_m_, (mult,size), _Pre_opt_cap_m_(mult,size) _Post_valid_impl_)
#define _Out_z_cap_m_(mult,size)          _SAL1_1_Source_(_Out_z_cap_m_, (mult,size), _Pre_cap_m_(mult,size)     _Post_valid_impl_ _Post_z_)
#define _Out_opt_z_cap_m_(mult,size)      _SAL1_1_Source_(_Out_opt_z_cap_m_, (mult,size), _Pre_opt_cap_m_(mult,size) _Post_valid_impl_ _Post_z_)

// buffer capacity is described by another pointer
// e.g. void Foo( _Out_ptrdiff_cap_(pchMax) char* pch, const char* pchMax ) { while pch < pchMax ) pch++; }
#define _Out_ptrdiff_cap_(size)           _SAL1_1_Source_(_Out_ptrdiff_cap_, (size), _Pre_ptrdiff_cap_(size)     _Post_valid_impl_)
#define _Out_opt_ptrdiff_cap_(size)       _SAL1_1_Source_(_Out_opt_ptrdiff_cap_, (size), _Pre_opt_ptrdiff_cap_(size) _Post_valid_impl_)

// buffer capacity is described by a complex expression
#define _Out_cap_x_(size)                 _SAL1_1_Source_(_Out_cap_x_, (size), _Pre_cap_x_(size)         _Post_valid_impl_)
#define _Out_opt_cap_x_(size)             _SAL1_1_Source_(_Out_opt_cap_x_, (size), _Pre_opt_cap_x_(size)     _Post_valid_impl_)
#define _Out_bytecap_x_(size)             _SAL1_1_Source_(_Out_bytecap_x_, (size), _Pre_bytecap_x_(size)     _Post_valid_impl_)
#define _Out_opt_bytecap_x_(size)         _SAL1_1_Source_(_Out_opt_bytecap_x_, (size), _Pre_opt_bytecap_x_(size) _Post_valid_impl_)

// a zero terminated string is filled into a buffer of given capacity
// e.g. void CopyStr( _In_z_ const char* szFrom, _Out_z_cap_(cchTo) char* szTo, size_t cchTo );
// buffer capacity is described by another parameter
#define _Out_z_cap_(size)                 _SAL1_1_Source_(_Out_z_cap_, (size), _Pre_cap_(size)           _Post_valid_impl_ _Post_z_)
#define _Out_opt_z_cap_(size)             _SAL1_1_Source_(_Out_opt_z_cap_, (size), _Pre_opt_cap_(size)       _Post_valid_impl_ _Post_z_)
#define _Out_z_bytecap_(size)             _SAL1_1_Source_(_Out_z_bytecap_, (size), _Pre_bytecap_(size)       _Post_valid_impl_ _Post_z_)
#define _Out_opt_z_bytecap_(size)         _SAL1_1_Source_(_Out_opt_z_bytecap_, (size), _Pre_opt_bytecap_(size)   _Post_valid_impl_ _Post_z_)

// buffer capacity is described by a constant expression
#define _Out_z_cap_c_(size)               _SAL1_1_Source_(_Out_z_cap_c_, (size), _Pre_cap_c_(size)         _Post_valid_impl_ _Post_z_)
#define _Out_opt_z_cap_c_(size)           _SAL1_1_Source_(_Out_opt_z_cap_c_, (size), _Pre_opt_cap_c_(size)     _Post_valid_impl_ _Post_z_)
#define _Out_z_bytecap_c_(size)           _SAL1_1_Source_(_Out_z_bytecap_c_, (size), _Pre_bytecap_c_(size)     _Post_valid_impl_ _Post_z_)
#define _Out_opt_z_bytecap_c_(size)       _SAL1_1_Source_(_Out_opt_z_bytecap_c_, (size), _Pre_opt_bytecap_c_(size) _Post_valid_impl_ _Post_z_)

// buffer capacity is described by a complex expression
#define _Out_z_cap_x_(size)               _SAL1_1_Source_(_Out_z_cap_x_, (size), _Pre_cap_x_(size)         _Post_valid_impl_ _Post_z_)
#define _Out_opt_z_cap_x_(size)           _SAL1_1_Source_(_Out_opt_z_cap_x_, (size), _Pre_opt_cap_x_(size)     _Post_valid_impl_ _Post_z_)
#define _Out_z_bytecap_x_(size)           _SAL1_1_Source_(_Out_z_bytecap_x_, (size), _Pre_bytecap_x_(size)     _Post_valid_impl_ _Post_z_)
#define _Out_opt_z_bytecap_x_(size)       _SAL1_1_Source_(_Out_opt_z_bytecap_x_, (size), _Pre_opt_bytecap_x_(size) _Post_valid_impl_ _Post_z_)

// a zero terminated string is filled into a buffer of given capacity
// e.g. size_t CopyCharRange( _In_count_(cchFrom) const char* rgchFrom, size_t cchFrom, _Out_cap_post_count_(cchTo,return)) char* rgchTo, size_t cchTo );
#define _Out_cap_post_count_(cap,count)                _SAL1_1_Source_(_Out_cap_post_count_, (cap,count), _Pre_cap_(cap)         _Post_valid_impl_ _Post_count_(count))
#define _Out_opt_cap_post_count_(cap,count)            _SAL1_1_Source_(_Out_opt_cap_post_count_, (cap,count), _Pre_opt_cap_(cap)     _Post_valid_impl_ _Post_count_(count))
#define _Out_bytecap_post_bytecount_(cap,count)        _SAL1_1_Source_(_Out_bytecap_post_bytecount_, (cap,count), _Pre_bytecap_(cap)     _Post_valid_impl_ _Post_bytecount_(count))
#define _Out_opt_bytecap_post_bytecount_(cap,count)    _SAL1_1_Source_(_Out_opt_bytecap_post_bytecount_, (cap,count), _Pre_opt_bytecap_(cap) _Post_valid_impl_ _Post_bytecount_(count))

// a zero terminated string is filled into a buffer of given capacity
// e.g. size_t CopyStr( _In_z_ const char* szFrom, _Out_z_cap_post_count_(cchTo,return+1) char* szTo, size_t cchTo );
#define _Out_z_cap_post_count_(cap,count)               _SAL1_1_Source_(_Out_z_cap_post_count_, (cap,count), _Pre_cap_(cap)         _Post_valid_impl_ _Post_z_count_(count))
#define _Out_opt_z_cap_post_count_(cap,count)           _SAL1_1_Source_(_Out_opt_z_cap_post_count_, (cap,count), _Pre_opt_cap_(cap)     _Post_valid_impl_ _Post_z_count_(count))
#define _Out_z_bytecap_post_bytecount_(cap,count)       _SAL1_1_Source_(_Out_z_bytecap_post_bytecount_, (cap,count), _Pre_bytecap_(cap)     _Post_valid_impl_ _Post_z_bytecount_(count))
#define _Out_opt_z_bytecap_post_bytecount_(cap,count)   _SAL1_1_Source_(_Out_opt_z_bytecap_post_bytecount_, (cap,count), _Pre_opt_bytecap_(cap) _Post_valid_impl_ _Post_z_bytecount_(count))

// only use with dereferenced arguments e.g. '*pcch'
#define _Out_capcount_(capcount)             _SAL1_1_Source_(_Out_capcount_, (capcount), _Pre_cap_(capcount)         _Post_valid_impl_ _Post_count_(capcount))
#define _Out_opt_capcount_(capcount)         _SAL1_1_Source_(_Out_opt_capcount_, (capcount), _Pre_opt_cap_(capcount)     _Post_valid_impl_ _Post_count_(capcount))
#define _Out_bytecapcount_(capcount)         _SAL1_1_Source_(_Out_bytecapcount_, (capcount), _Pre_bytecap_(capcount)     _Post_valid_impl_ _Post_bytecount_(capcount))
#define _Out_opt_bytecapcount_(capcount)     _SAL1_1_Source_(_Out_opt_bytecapcount_, (capcount), _Pre_opt_bytecap_(capcount) _Post_valid_impl_ _Post_bytecount_(capcount))

#define _Out_capcount_x_(capcount)           _SAL1_1_Source_(_Out_capcount_x_, (capcount), _Pre_cap_x_(capcount)         _Post_valid_impl_ _Post_count_x_(capcount))
#define _Out_opt_capcount_x_(capcount)       _SAL1_1_Source_(_Out_opt_capcount_x_, (capcount), _Pre_opt_cap_x_(capcount)     _Post_valid_impl_ _Post_count_x_(capcount))
#define _Out_bytecapcount_x_(capcount)       _SAL1_1_Source_(_Out_bytecapcount_x_, (capcount), _Pre_bytecap_x_(capcount)     _Post_valid_impl_ _Post_bytecount_x_(capcount))
#define _Out_opt_bytecapcount_x_(capcount)   _SAL1_1_Source_(_Out_opt_bytecapcount_x_, (capcount), _Pre_opt_bytecap_x_(capcount) _Post_valid_impl_ _Post_bytecount_x_(capcount))

// e.g. GetString( _Out_z_capcount_(*pLen+1) char* sz, size_t* pLen );
#define _Out_z_capcount_(capcount)           _SAL1_1_Source_(_Out_z_capcount_, (capcount), _Pre_cap_(capcount)         _Post_valid_impl_ _Post_z_count_(capcount))
#define _Out_opt_z_capcount_(capcount)       _SAL1_1_Source_(_Out_opt_z_capcount_, (capcount), _Pre_opt_cap_(capcount)     _Post_valid_impl_ _Post_z_count_(capcount))
#define _Out_z_bytecapcount_(capcount)       _SAL1_1_Source_(_Out_z_bytecapcount_, (capcount), _Pre_bytecap_(capcount)     _Post_valid_impl_ _Post_z_bytecount_(capcount))
#define _Out_opt_z_bytecapcount_(capcount)   _SAL1_1_Source_(_Out_opt_z_bytecapcount_, (capcount), _Pre_opt_bytecap_(capcount) _Post_valid_impl_ _Post_z_bytecount_(capcount))


// 'inout' buffers with initialized elements before and after the call
// e.g. void ModifyIndices( _Inout_count_(cIndices) int* rgIndices, size_t cIndices );
#define _Inout_count_(size)               _SAL1_1_Source_(_Inout_count_, (size), _Prepost_count_(size))
#define _Inout_opt_count_(size)           _SAL1_1_Source_(_Inout_opt_count_, (size), _Prepost_opt_count_(size))
#define _Inout_bytecount_(size)           _SAL1_1_Source_(_Inout_bytecount_, (size), _Prepost_bytecount_(size))
#define _Inout_opt_bytecount_(size)       _SAL1_1_Source_(_Inout_opt_bytecount_, (size), _Prepost_opt_bytecount_(size))

#define _Inout_count_c_(size)             _SAL1_1_Source_(_Inout_count_c_, (size), _Prepost_count_c_(size))
#define _Inout_opt_count_c_(size)         _SAL1_1_Source_(_Inout_opt_count_c_, (size), _Prepost_opt_count_c_(size))
#define _Inout_bytecount_c_(size)         _SAL1_1_Source_(_Inout_bytecount_c_, (size), _Prepost_bytecount_c_(size))
#define _Inout_opt_bytecount_c_(size)     _SAL1_1_Source_(_Inout_opt_bytecount_c_, (size), _Prepost_opt_bytecount_c_(size))

// nullterminated 'inout' buffers with initialized elements before and after the call
// e.g. void ModifyIndices( _Inout_count_(cIndices) int* rgIndices, size_t cIndices );
#define _Inout_z_count_(size)               _SAL1_1_Source_(_Inout_z_count_, (size), _Prepost_z_ _Prepost_count_(size))
#define _Inout_opt_z_count_(size)           _SAL1_1_Source_(_Inout_opt_z_count_, (size), _Prepost_z_ _Prepost_opt_count_(size))
#define _Inout_z_bytecount_(size)           _SAL1_1_Source_(_Inout_z_bytecount_, (size), _Prepost_z_ _Prepost_bytecount_(size))
#define _Inout_opt_z_bytecount_(size)       _SAL1_1_Source_(_Inout_opt_z_bytecount_, (size), _Prepost_z_ _Prepost_opt_bytecount_(size))

#define _Inout_z_count_c_(size)             _SAL1_1_Source_(_Inout_z_count_c_, (size), _Prepost_z_ _Prepost_count_c_(size))
#define _Inout_opt_z_count_c_(size)         _SAL1_1_Source_(_Inout_opt_z_count_c_, (size), _Prepost_z_ _Prepost_opt_count_c_(size))
#define _Inout_z_bytecount_c_(size)         _SAL1_1_Source_(_Inout_z_bytecount_c_, (size), _Prepost_z_ _Prepost_bytecount_c_(size))
#define _Inout_opt_z_bytecount_c_(size)     _SAL1_1_Source_(_Inout_opt_z_bytecount_c_, (size), _Prepost_z_ _Prepost_opt_bytecount_c_(size))

#define _Inout_ptrdiff_count_(size)       _SAL1_1_Source_(_Inout_ptrdiff_count_, (size), _Pre_ptrdiff_count_(size))
#define _Inout_opt_ptrdiff_count_(size)   _SAL1_1_Source_(_Inout_opt_ptrdiff_count_, (size), _Pre_opt_ptrdiff_count_(size))

#define _Inout_count_x_(size)             _SAL1_1_Source_(_Inout_count_x_, (size), _Prepost_count_x_(size))
#define _Inout_opt_count_x_(size)         _SAL1_1_Source_(_Inout_opt_count_x_, (size), _Prepost_opt_count_x_(size))
#define _Inout_bytecount_x_(size)         _SAL1_1_Source_(_Inout_bytecount_x_, (size), _Prepost_bytecount_x_(size))
#define _Inout_opt_bytecount_x_(size)     _SAL1_1_Source_(_Inout_opt_bytecount_x_, (size), _Prepost_opt_bytecount_x_(size))

// e.g. void AppendToLPSTR( _In_ LPCSTR szFrom, _Inout_cap_(cchTo) LPSTR* szTo, size_t cchTo );
#define _Inout_cap_(size)                 _SAL1_1_Source_(_Inout_cap_, (size), _Pre_valid_cap_(size)           _Post_valid_)
#define _Inout_opt_cap_(size)             _SAL1_1_Source_(_Inout_opt_cap_, (size), _Pre_opt_valid_cap_(size)       _Post_valid_)
#define _Inout_bytecap_(size)             _SAL1_1_Source_(_Inout_bytecap_, (size), _Pre_valid_bytecap_(size)       _Post_valid_)
#define _Inout_opt_bytecap_(size)         _SAL1_1_Source_(_Inout_opt_bytecap_, (size), _Pre_opt_valid_bytecap_(size)   _Post_valid_)

#define _Inout_cap_c_(size)               _SAL1_1_Source_(_Inout_cap_c_, (size), _Pre_valid_cap_c_(size)         _Post_valid_)
#define _Inout_opt_cap_c_(size)           _SAL1_1_Source_(_Inout_opt_cap_c_, (size), _Pre_opt_valid_cap_c_(size)     _Post_valid_)
#define _Inout_bytecap_c_(size)           _SAL1_1_Source_(_Inout_bytecap_c_, (size), _Pre_valid_bytecap_c_(size)     _Post_valid_)
#define _Inout_opt_bytecap_c_(size)       _SAL1_1_Source_(_Inout_opt_bytecap_c_, (size), _Pre_opt_valid_bytecap_c_(size) _Post_valid_)

#define _Inout_cap_x_(size)               _SAL1_1_Source_(_Inout_cap_x_, (size), _Pre_valid_cap_x_(size)         _Post_valid_)
#define _Inout_opt_cap_x_(size)           _SAL1_1_Source_(_Inout_opt_cap_x_, (size), _Pre_opt_valid_cap_x_(size)     _Post_valid_)
#define _Inout_bytecap_x_(size)           _SAL1_1_Source_(_Inout_bytecap_x_, (size), _Pre_valid_bytecap_x_(size)     _Post_valid_)
#define _Inout_opt_bytecap_x_(size)       _SAL1_1_Source_(_Inout_opt_bytecap_x_, (size), _Pre_opt_valid_bytecap_x_(size) _Post_valid_)

// inout string buffers with writable size
// e.g. void AppendStr( _In_z_ const char* szFrom, _Inout_z_cap_(cchTo) char* szTo, size_t cchTo );
#define _Inout_z_cap_(size)                  _SAL1_1_Source_(_Inout_z_cap_, (size), _Pre_z_cap_(size)            _Post_z_)
#define _Inout_opt_z_cap_(size)              _SAL1_1_Source_(_Inout_opt_z_cap_, (size), _Pre_opt_z_cap_(size)        _Post_z_)
#define _Inout_z_bytecap_(size)              _SAL1_1_Source_(_Inout_z_bytecap_, (size), _Pre_z_bytecap_(size)        _Post_z_)
#define _Inout_opt_z_bytecap_(size)          _SAL1_1_Source_(_Inout_opt_z_bytecap_, (size), _Pre_opt_z_bytecap_(size)    _Post_z_)

#define _Inout_z_cap_c_(size)                _SAL1_1_Source_(_Inout_z_cap_c_, (size), _Pre_z_cap_c_(size)          _Post_z_)
#define _Inout_opt_z_cap_c_(size)            _SAL1_1_Source_(_Inout_opt_z_cap_c_, (size), _Pre_opt_z_cap_c_(size)      _Post_z_)
#define _Inout_z_bytecap_c_(size)            _SAL1_1_Source_(_Inout_z_bytecap_c_, (size), _Pre_z_bytecap_c_(size)      _Post_z_)
#define _Inout_opt_z_bytecap_c_(size)        _SAL1_1_Source_(_Inout_opt_z_bytecap_c_, (size), _Pre_opt_z_bytecap_c_(size)  _Post_z_)

#define _Inout_z_cap_x_(size)                _SAL1_1_Source_(_Inout_z_cap_x_, (size), _Pre_z_cap_x_(size)          _Post_z_)
#define _Inout_opt_z_cap_x_(size)            _SAL1_1_Source_(_Inout_opt_z_cap_x_, (size), _Pre_opt_z_cap_x_(size)      _Post_z_)
#define _Inout_z_bytecap_x_(size)            _SAL1_1_Source_(_Inout_z_bytecap_x_, (size), _Pre_z_bytecap_x_(size)      _Post_z_)
#define _Inout_opt_z_bytecap_x_(size)        _SAL1_1_Source_(_Inout_opt_z_bytecap_x_, (size), _Pre_opt_z_bytecap_x_(size)  _Post_z_)


// returning pointers to valid objects
#define _Ret_                   _SAL1_1_Source_(_Ret_, (), _Ret_valid_)
#define _Ret_opt_               _SAL1_1_Source_(_Ret_opt_, (), _Ret_opt_valid_)

// annotations to express 'boundedness' of integral value parameter
#define _In_bound_           _SAL1_1_Source_(_In_bound_, (), _In_bound_impl_)
#define _Out_bound_          _SAL1_1_Source_(_Out_bound_, (), _Out_bound_impl_)
#define _Ret_bound_          _SAL1_1_Source_(_Ret_bound_, (), _Ret_bound_impl_)
#define _Deref_in_bound_     _SAL1_1_Source_(_Deref_in_bound_, (), _Deref_in_bound_impl_)
#define _Deref_out_bound_    _SAL1_1_Source_(_Deref_out_bound_, (), _Deref_out_bound_impl_)
#define _Deref_inout_bound_  _SAL1_1_Source_(_Deref_inout_bound_, (), _Deref_in_bound_ _Deref_out_bound_)
#define _Deref_ret_bound_    _SAL1_1_Source_(_Deref_ret_bound_, (), _Deref_ret_bound_impl_)

// e.g.  HRESULT HrCreatePoint( _Deref_out_opt_ POINT** ppPT );
#define _Deref_out_             _SAL1_1_Source_(_Deref_out_, (), _Out_ _Deref_post_valid_)
#define _Deref_out_opt_         _SAL1_1_Source_(_Deref_out_opt_, (), _Out_ _Deref_post_opt_valid_)
#define _Deref_opt_out_         _SAL1_1_Source_(_Deref_opt_out_, (), _Out_opt_ _Deref_post_valid_)
#define _Deref_opt_out_opt_     _SAL1_1_Source_(_Deref_opt_out_opt_, (), _Out_opt_ _Deref_post_opt_valid_)

// e.g.  void CloneString( _In_z_ const WCHAR* wzFrom, _Deref_out_z_ WCHAR** pWzTo );
#define _Deref_out_z_           _SAL1_1_Source_(_Deref_out_z_, (), _Out_ _Deref_post_z_)
#define _Deref_out_opt_z_       _SAL1_1_Source_(_Deref_out_opt_z_, (), _Out_ _Deref_post_opt_z_)
#define _Deref_opt_out_z_       _SAL1_1_Source_(_Deref_opt_out_z_, (), _Out_opt_ _Deref_post_z_)
#define _Deref_opt_out_opt_z_   _SAL1_1_Source_(_Deref_opt_out_opt_z_, (), _Out_opt_ _Deref_post_opt_z_)

//
// _Deref_pre_ ---
//
// describing conditions for array elements of dereferenced pointer parameters that must be met before the call

// e.g. void SaveStringArray( _In_count_(cStrings) _Deref_pre_z_ const WCHAR* const rgpwch[] );
#define _Deref_pre_z_                           _SAL1_1_Source_(_Deref_pre_z_, (), _Deref_pre1_impl_(__notnull_impl_notref) _Deref_pre1_impl_(__zterm_impl) _Pre_valid_impl_)
#define _Deref_pre_opt_z_                       _SAL1_1_Source_(_Deref_pre_opt_z_, (), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre1_impl_(__zterm_impl) _Pre_valid_impl_)

// e.g. void FillInArrayOfStr32( _In_count_(cStrings) _Deref_pre_cap_c_(32) _Deref_post_z_ WCHAR* const rgpwch[] );
// buffer capacity is described by another parameter
#define _Deref_pre_cap_(size)                   _SAL1_1_Source_(_Deref_pre_cap_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre1_impl_(__cap_impl(size)))
#define _Deref_pre_opt_cap_(size)               _SAL1_1_Source_(_Deref_pre_opt_cap_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre1_impl_(__cap_impl(size)))
#define _Deref_pre_bytecap_(size)               _SAL1_1_Source_(_Deref_pre_bytecap_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre1_impl_(__bytecap_impl(size)))
#define _Deref_pre_opt_bytecap_(size)           _SAL1_1_Source_(_Deref_pre_opt_bytecap_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre1_impl_(__bytecap_impl(size)))

// buffer capacity is described by a constant expression
#define _Deref_pre_cap_c_(size)                 _SAL1_1_Source_(_Deref_pre_cap_c_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre1_impl_(__cap_c_impl(size)))
#define _Deref_pre_opt_cap_c_(size)             _SAL1_1_Source_(_Deref_pre_opt_cap_c_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre1_impl_(__cap_c_impl(size)))
#define _Deref_pre_bytecap_c_(size)             _SAL1_1_Source_(_Deref_pre_bytecap_c_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre1_impl_(__bytecap_c_impl(size)))
#define _Deref_pre_opt_bytecap_c_(size)         _SAL1_1_Source_(_Deref_pre_opt_bytecap_c_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre1_impl_(__bytecap_c_impl(size)))

// buffer capacity is described by a complex condition
#define _Deref_pre_cap_x_(size)                 _SAL1_1_Source_(_Deref_pre_cap_x_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre1_impl_(__cap_x_impl(size)))
#define _Deref_pre_opt_cap_x_(size)             _SAL1_1_Source_(_Deref_pre_opt_cap_x_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre1_impl_(__cap_x_impl(size)))
#define _Deref_pre_bytecap_x_(size)             _SAL1_1_Source_(_Deref_pre_bytecap_x_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre1_impl_(__bytecap_x_impl(size)))
#define _Deref_pre_opt_bytecap_x_(size)         _SAL1_1_Source_(_Deref_pre_opt_bytecap_x_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre1_impl_(__bytecap_x_impl(size)))

// convenience macros for nullterminated buffers with given capacity
#define _Deref_pre_z_cap_(size)                 _SAL1_1_Source_(_Deref_pre_z_cap_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre2_impl_(__zterm_impl,__cap_impl(size))     _Pre_valid_impl_)
#define _Deref_pre_opt_z_cap_(size)             _SAL1_1_Source_(_Deref_pre_opt_z_cap_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre2_impl_(__zterm_impl,__cap_impl(size))     _Pre_valid_impl_)
#define _Deref_pre_z_bytecap_(size)             _SAL1_1_Source_(_Deref_pre_z_bytecap_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre2_impl_(__zterm_impl,__bytecap_impl(size)) _Pre_valid_impl_)
#define _Deref_pre_opt_z_bytecap_(size)         _SAL1_1_Source_(_Deref_pre_opt_z_bytecap_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre2_impl_(__zterm_impl,__bytecap_impl(size)) _Pre_valid_impl_)

#define _Deref_pre_z_cap_c_(size)               _SAL1_1_Source_(_Deref_pre_z_cap_c_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre2_impl_(__zterm_impl,__cap_c_impl(size))     _Pre_valid_impl_)
#define _Deref_pre_opt_z_cap_c_(size)           _SAL1_1_Source_(_Deref_pre_opt_z_cap_c_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre2_impl_(__zterm_impl,__cap_c_impl(size))     _Pre_valid_impl_)
#define _Deref_pre_z_bytecap_c_(size)           _SAL1_1_Source_(_Deref_pre_z_bytecap_c_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre2_impl_(__zterm_impl,__bytecap_c_impl(size)) _Pre_valid_impl_)
#define _Deref_pre_opt_z_bytecap_c_(size)       _SAL1_1_Source_(_Deref_pre_opt_z_bytecap_c_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre2_impl_(__zterm_impl,__bytecap_c_impl(size)) _Pre_valid_impl_)

#define _Deref_pre_z_cap_x_(size)               _SAL1_1_Source_(_Deref_pre_z_cap_x_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre2_impl_(__zterm_impl,__cap_x_impl(size))     _Pre_valid_impl_)
#define _Deref_pre_opt_z_cap_x_(size)           _SAL1_1_Source_(_Deref_pre_opt_z_cap_x_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre2_impl_(__zterm_impl,__cap_x_impl(size))     _Pre_valid_impl_)
#define _Deref_pre_z_bytecap_x_(size)           _SAL1_1_Source_(_Deref_pre_z_bytecap_x_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre2_impl_(__zterm_impl,__bytecap_x_impl(size)) _Pre_valid_impl_)
#define _Deref_pre_opt_z_bytecap_x_(size)       _SAL1_1_Source_(_Deref_pre_opt_z_bytecap_x_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre2_impl_(__zterm_impl,__bytecap_x_impl(size)) _Pre_valid_impl_)

// known capacity and valid but unknown readable extent
#define _Deref_pre_valid_cap_(size)             _SAL1_1_Source_(_Deref_pre_valid_cap_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre1_impl_(__cap_impl(size))     _Pre_valid_impl_)
#define _Deref_pre_opt_valid_cap_(size)         _SAL1_1_Source_(_Deref_pre_opt_valid_cap_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre1_impl_(__cap_impl(size))     _Pre_valid_impl_)
#define _Deref_pre_valid_bytecap_(size)         _SAL1_1_Source_(_Deref_pre_valid_bytecap_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre1_impl_(__bytecap_impl(size)) _Pre_valid_impl_)
#define _Deref_pre_opt_valid_bytecap_(size)     _SAL1_1_Source_(_Deref_pre_opt_valid_bytecap_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre1_impl_(__bytecap_impl(size)) _Pre_valid_impl_)

#define _Deref_pre_valid_cap_c_(size)           _SAL1_1_Source_(_Deref_pre_valid_cap_c_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre1_impl_(__cap_c_impl(size))     _Pre_valid_impl_)
#define _Deref_pre_opt_valid_cap_c_(size)       _SAL1_1_Source_(_Deref_pre_opt_valid_cap_c_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre1_impl_(__cap_c_impl(size))     _Pre_valid_impl_)
#define _Deref_pre_valid_bytecap_c_(size)       _SAL1_1_Source_(_Deref_pre_valid_bytecap_c_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre1_impl_(__bytecap_c_impl(size)) _Pre_valid_impl_)
#define _Deref_pre_opt_valid_bytecap_c_(size)   _SAL1_1_Source_(_Deref_pre_opt_valid_bytecap_c_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre1_impl_(__bytecap_c_impl(size)) _Pre_valid_impl_)

#define _Deref_pre_valid_cap_x_(size)           _SAL1_1_Source_(_Deref_pre_valid_cap_x_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre1_impl_(__cap_x_impl(size))     _Pre_valid_impl_)
#define _Deref_pre_opt_valid_cap_x_(size)       _SAL1_1_Source_(_Deref_pre_opt_valid_cap_x_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre1_impl_(__cap_x_impl(size))     _Pre_valid_impl_)
#define _Deref_pre_valid_bytecap_x_(size)       _SAL1_1_Source_(_Deref_pre_valid_bytecap_x_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre1_impl_(__bytecap_x_impl(size)) _Pre_valid_impl_)
#define _Deref_pre_opt_valid_bytecap_x_(size)   _SAL1_1_Source_(_Deref_pre_opt_valid_bytecap_x_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre1_impl_(__bytecap_x_impl(size)) _Pre_valid_impl_)

// e.g. void SaveMatrix( _In_count_(n) _Deref_pre_count_(n) const Elem** matrix, size_t n );
// valid buffer extent is described by another parameter
#define _Deref_pre_count_(size)                 _SAL1_1_Source_(_Deref_pre_count_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre1_impl_(__count_impl(size))     _Pre_valid_impl_)
#define _Deref_pre_opt_count_(size)             _SAL1_1_Source_(_Deref_pre_opt_count_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre1_impl_(__count_impl(size))     _Pre_valid_impl_)
#define _Deref_pre_bytecount_(size)             _SAL1_1_Source_(_Deref_pre_bytecount_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre1_impl_(__bytecount_impl(size)) _Pre_valid_impl_)
#define _Deref_pre_opt_bytecount_(size)         _SAL1_1_Source_(_Deref_pre_opt_bytecount_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre1_impl_(__bytecount_impl(size)) _Pre_valid_impl_)

// valid buffer extent is described by a constant expression
#define _Deref_pre_count_c_(size)               _SAL1_1_Source_(_Deref_pre_count_c_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre1_impl_(__count_c_impl(size))     _Pre_valid_impl_)
#define _Deref_pre_opt_count_c_(size)           _SAL1_1_Source_(_Deref_pre_opt_count_c_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre1_impl_(__count_c_impl(size))     _Pre_valid_impl_)
#define _Deref_pre_bytecount_c_(size)           _SAL1_1_Source_(_Deref_pre_bytecount_c_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre1_impl_(__bytecount_c_impl(size)) _Pre_valid_impl_)
#define _Deref_pre_opt_bytecount_c_(size)       _SAL1_1_Source_(_Deref_pre_opt_bytecount_c_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre1_impl_(__bytecount_c_impl(size)) _Pre_valid_impl_)

// valid buffer extent is described by a complex expression
#define _Deref_pre_count_x_(size)               _SAL1_1_Source_(_Deref_pre_count_x_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre1_impl_(__count_x_impl(size))     _Pre_valid_impl_)
#define _Deref_pre_opt_count_x_(size)           _SAL1_1_Source_(_Deref_pre_opt_count_x_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre1_impl_(__count_x_impl(size))     _Pre_valid_impl_)
#define _Deref_pre_bytecount_x_(size)           _SAL1_1_Source_(_Deref_pre_bytecount_x_, (size), _Deref_pre1_impl_(__notnull_impl_notref)   _Deref_pre1_impl_(__bytecount_x_impl(size)) _Pre_valid_impl_)
#define _Deref_pre_opt_bytecount_x_(size)       _SAL1_1_Source_(_Deref_pre_opt_bytecount_x_, (size), _Deref_pre1_impl_(__maybenull_impl_notref) _Deref_pre1_impl_(__bytecount_x_impl(size)) _Pre_valid_impl_)

// e.g. void PrintStringArray( _In_count_(cElems) _Deref_pre_valid_ LPCSTR rgStr[], size_t cElems );
#define _Deref_pre_valid_                       _SAL1_1_Source_(_Deref_pre_valid_, (), _Deref_pre1_impl_(__notnull_impl_notref)   _Pre_valid_impl_)
#define _Deref_pre_opt_valid_                   _SAL1_1_Source_(_Deref_pre_opt_valid_, (), _Deref_pre1_impl_(__maybenull_impl_notref) _Pre_valid_impl_)
#define _Deref_pre_invalid_                     _SAL1_1_Source_(_Deref_pre_invalid_, (), _Deref_pre1_impl_(__notvalid_impl))

#define _Deref_pre_notnull_                     _SAL1_1_Source_(_Deref_pre_notnull_, (), _Deref_pre1_impl_(__notnull_impl_notref))
#define _Deref_pre_maybenull_                   _SAL1_1_Source_(_Deref_pre_maybenull_, (), _Deref_pre1_impl_(__maybenull_impl_notref))
#define _Deref_pre_null_                        _SAL1_1_Source_(_Deref_pre_null_, (), _Deref_pre1_impl_(__null_impl_notref))

// restrict access rights
#define _Deref_pre_readonly_                    _SAL1_1_Source_(_Deref_pre_readonly_, (), _Deref_pre1_impl_(__readaccess_impl_notref))
#define _Deref_pre_writeonly_                   _SAL1_1_Source_(_Deref_pre_writeonly_, (), _Deref_pre1_impl_(__writeaccess_impl_notref))

//
// _Deref_post_ ---
//
// describing conditions for array elements or dereferenced pointer parameters that hold after the call

// e.g. void CloneString( _In_z_ const Wchar_t* wzIn _Out_ _Deref_post_z_ WCHAR** pWzOut );
#define _Deref_post_z_                           _SAL1_1_Source_(_Deref_post_z_, (), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post1_impl_(__zterm_impl) _Post_valid_impl_)
#define _Deref_post_opt_z_                       _SAL1_1_Source_(_Deref_post_opt_z_, (), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post1_impl_(__zterm_impl) _Post_valid_impl_)

// e.g. HRESULT HrAllocateMemory( size_t cb, _Out_ _Deref_post_bytecap_(cb) void** ppv );
// buffer capacity is described by another parameter
#define _Deref_post_cap_(size)                   _SAL1_1_Source_(_Deref_post_cap_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post1_impl_(__cap_impl(size)))
#define _Deref_post_opt_cap_(size)               _SAL1_1_Source_(_Deref_post_opt_cap_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post1_impl_(__cap_impl(size)))
#define _Deref_post_bytecap_(size)               _SAL1_1_Source_(_Deref_post_bytecap_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post1_impl_(__bytecap_impl(size)))
#define _Deref_post_opt_bytecap_(size)           _SAL1_1_Source_(_Deref_post_opt_bytecap_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post1_impl_(__bytecap_impl(size)))

// buffer capacity is described by a constant expression
#define _Deref_post_cap_c_(size)                 _SAL1_1_Source_(_Deref_post_cap_c_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post1_impl_(__cap_c_impl(size)))
#define _Deref_post_opt_cap_c_(size)             _SAL1_1_Source_(_Deref_post_opt_cap_c_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post1_impl_(__cap_c_impl(size)))
#define _Deref_post_bytecap_c_(size)             _SAL1_1_Source_(_Deref_post_bytecap_c_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post1_impl_(__bytecap_c_impl(size)))
#define _Deref_post_opt_bytecap_c_(size)         _SAL1_1_Source_(_Deref_post_opt_bytecap_c_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post1_impl_(__bytecap_c_impl(size)))

// buffer capacity is described by a complex expression
#define _Deref_post_cap_x_(size)                 _SAL1_1_Source_(_Deref_post_cap_x_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post1_impl_(__cap_x_impl(size)))
#define _Deref_post_opt_cap_x_(size)             _SAL1_1_Source_(_Deref_post_opt_cap_x_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post1_impl_(__cap_x_impl(size)))
#define _Deref_post_bytecap_x_(size)             _SAL1_1_Source_(_Deref_post_bytecap_x_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post1_impl_(__bytecap_x_impl(size)))
#define _Deref_post_opt_bytecap_x_(size)         _SAL1_1_Source_(_Deref_post_opt_bytecap_x_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post1_impl_(__bytecap_x_impl(size)))

// convenience macros for nullterminated buffers with given capacity
#define _Deref_post_z_cap_(size)                 _SAL1_1_Source_(_Deref_post_z_cap_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post2_impl_(__zterm_impl,__cap_impl(size))       _Post_valid_impl_)
#define _Deref_post_opt_z_cap_(size)             _SAL1_1_Source_(_Deref_post_opt_z_cap_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post2_impl_(__zterm_impl,__cap_impl(size))       _Post_valid_impl_)
#define _Deref_post_z_bytecap_(size)             _SAL1_1_Source_(_Deref_post_z_bytecap_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post2_impl_(__zterm_impl,__bytecap_impl(size))   _Post_valid_impl_)
#define _Deref_post_opt_z_bytecap_(size)         _SAL1_1_Source_(_Deref_post_opt_z_bytecap_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post2_impl_(__zterm_impl,__bytecap_impl(size))   _Post_valid_impl_)

#define _Deref_post_z_cap_c_(size)               _SAL1_1_Source_(_Deref_post_z_cap_c_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post2_impl_(__zterm_impl,__cap_c_impl(size))     _Post_valid_impl_)
#define _Deref_post_opt_z_cap_c_(size)           _SAL1_1_Source_(_Deref_post_opt_z_cap_c_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post2_impl_(__zterm_impl,__cap_c_impl(size))     _Post_valid_impl_)
#define _Deref_post_z_bytecap_c_(size)           _SAL1_1_Source_(_Deref_post_z_bytecap_c_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post2_impl_(__zterm_impl,__bytecap_c_impl(size)) _Post_valid_impl_)
#define _Deref_post_opt_z_bytecap_c_(size)       _SAL1_1_Source_(_Deref_post_opt_z_bytecap_c_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post2_impl_(__zterm_impl,__bytecap_c_impl(size)) _Post_valid_impl_)

#define _Deref_post_z_cap_x_(size)               _SAL1_1_Source_(_Deref_post_z_cap_x_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post2_impl_(__zterm_impl,__cap_x_impl(size))     _Post_valid_impl_)
#define _Deref_post_opt_z_cap_x_(size)           _SAL1_1_Source_(_Deref_post_opt_z_cap_x_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post2_impl_(__zterm_impl,__cap_x_impl(size))     _Post_valid_impl_)
#define _Deref_post_z_bytecap_x_(size)           _SAL1_1_Source_(_Deref_post_z_bytecap_x_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post2_impl_(__zterm_impl,__bytecap_x_impl(size)) _Post_valid_impl_)
#define _Deref_post_opt_z_bytecap_x_(size)       _SAL1_1_Source_(_Deref_post_opt_z_bytecap_x_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post2_impl_(__zterm_impl,__bytecap_x_impl(size)) _Post_valid_impl_)

// known capacity and valid but unknown readable extent
#define _Deref_post_valid_cap_(size)             _SAL1_1_Source_(_Deref_post_valid_cap_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post1_impl_(__cap_impl(size))       _Post_valid_impl_)
#define _Deref_post_opt_valid_cap_(size)         _SAL1_1_Source_(_Deref_post_opt_valid_cap_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post1_impl_(__cap_impl(size))       _Post_valid_impl_)
#define _Deref_post_valid_bytecap_(size)         _SAL1_1_Source_(_Deref_post_valid_bytecap_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post1_impl_(__bytecap_impl(size))   _Post_valid_impl_)
#define _Deref_post_opt_valid_bytecap_(size)     _SAL1_1_Source_(_Deref_post_opt_valid_bytecap_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post1_impl_(__bytecap_impl(size))   _Post_valid_impl_)

#define _Deref_post_valid_cap_c_(size)           _SAL1_1_Source_(_Deref_post_valid_cap_c_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post1_impl_(__cap_c_impl(size))     _Post_valid_impl_)
#define _Deref_post_opt_valid_cap_c_(size)       _SAL1_1_Source_(_Deref_post_opt_valid_cap_c_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post1_impl_(__cap_c_impl(size))     _Post_valid_impl_)
#define _Deref_post_valid_bytecap_c_(size)       _SAL1_1_Source_(_Deref_post_valid_bytecap_c_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post1_impl_(__bytecap_c_impl(size)) _Post_valid_impl_)
#define _Deref_post_opt_valid_bytecap_c_(size)   _SAL1_1_Source_(_Deref_post_opt_valid_bytecap_c_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post1_impl_(__bytecap_c_impl(size)) _Post_valid_impl_)

#define _Deref_post_valid_cap_x_(size)           _SAL1_1_Source_(_Deref_post_valid_cap_x_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post1_impl_(__cap_x_impl(size))     _Post_valid_impl_)
#define _Deref_post_opt_valid_cap_x_(size)       _SAL1_1_Source_(_Deref_post_opt_valid_cap_x_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post1_impl_(__cap_x_impl(size))     _Post_valid_impl_)
#define _Deref_post_valid_bytecap_x_(size)       _SAL1_1_Source_(_Deref_post_valid_bytecap_x_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post1_impl_(__bytecap_x_impl(size)) _Post_valid_impl_)
#define _Deref_post_opt_valid_bytecap_x_(size)   _SAL1_1_Source_(_Deref_post_opt_valid_bytecap_x_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post1_impl_(__bytecap_x_impl(size)) _Post_valid_impl_)

// e.g. HRESULT HrAllocateZeroInitializedMemory( size_t cb, _Out_ _Deref_post_bytecount_(cb) void** ppv );
// valid buffer extent is described by another parameter
#define _Deref_post_count_(size)                 _SAL1_1_Source_(_Deref_post_count_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post1_impl_(__count_impl(size))       _Post_valid_impl_)
#define _Deref_post_opt_count_(size)             _SAL1_1_Source_(_Deref_post_opt_count_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post1_impl_(__count_impl(size))       _Post_valid_impl_)
#define _Deref_post_bytecount_(size)             _SAL1_1_Source_(_Deref_post_bytecount_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post1_impl_(__bytecount_impl(size))   _Post_valid_impl_)
#define _Deref_post_opt_bytecount_(size)         _SAL1_1_Source_(_Deref_post_opt_bytecount_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post1_impl_(__bytecount_impl(size))   _Post_valid_impl_)

// buffer capacity is described by a constant expression
#define _Deref_post_count_c_(size)               _SAL1_1_Source_(_Deref_post_count_c_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post1_impl_(__count_c_impl(size))     _Post_valid_impl_)
#define _Deref_post_opt_count_c_(size)           _SAL1_1_Source_(_Deref_post_opt_count_c_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post1_impl_(__count_c_impl(size))     _Post_valid_impl_)
#define _Deref_post_bytecount_c_(size)           _SAL1_1_Source_(_Deref_post_bytecount_c_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post1_impl_(__bytecount_c_impl(size)) _Post_valid_impl_)
#define _Deref_post_opt_bytecount_c_(size)       _SAL1_1_Source_(_Deref_post_opt_bytecount_c_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post1_impl_(__bytecount_c_impl(size)) _Post_valid_impl_)

// buffer capacity is described by a complex expression
#define _Deref_post_count_x_(size)               _SAL1_1_Source_(_Deref_post_count_x_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post1_impl_(__count_x_impl(size))     _Post_valid_impl_)
#define _Deref_post_opt_count_x_(size)           _SAL1_1_Source_(_Deref_post_opt_count_x_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post1_impl_(__count_x_impl(size))     _Post_valid_impl_)
#define _Deref_post_bytecount_x_(size)           _SAL1_1_Source_(_Deref_post_bytecount_x_, (size), _Deref_post1_impl_(__notnull_impl_notref) _Deref_post1_impl_(__bytecount_x_impl(size)) _Post_valid_impl_)
#define _Deref_post_opt_bytecount_x_(size)       _SAL1_1_Source_(_Deref_post_opt_bytecount_x_, (size), _Deref_post1_impl_(__maybenull_impl_notref) _Deref_post1_impl_(__bytecount_x_impl(size)) _Post_valid_impl_)

// e.g. void GetStrings( _Out_count_(cElems) _Deref_post_valid_ LPSTR const rgStr[], size_t cElems );
#define _Deref_post_valid_                       _SAL1_1_Source_(_Deref_post_valid_, (), _Deref_post1_impl_(__notnull_impl_notref)   _Post_valid_impl_)
#define _Deref_post_opt_valid_                   _SAL1_1_Source_(_Deref_post_opt_valid_, (), _Deref_post1_impl_(__maybenull_impl_notref) _Post_valid_impl_)

#define _Deref_post_notnull_                     _SAL1_1_Source_(_Deref_post_notnull_, (), _Deref_post1_impl_(__notnull_impl_notref))
#define _Deref_post_maybenull_                   _SAL1_1_Source_(_Deref_post_maybenull_, (), _Deref_post1_impl_(__maybenull_impl_notref))
#define _Deref_post_null_                        _SAL1_1_Source_(_Deref_post_null_, (), _Deref_post1_impl_(__null_impl_notref))

//
// _Deref_ret_ ---
//

#define _Deref_ret_z_                            _SAL1_1_Source_(_Deref_ret_z_, (), _Deref_ret1_impl_(__notnull_impl_notref) _Deref_ret1_impl_(__zterm_impl))
#define _Deref_ret_opt_z_                        _SAL1_1_Source_(_Deref_ret_opt_z_, (), _Deref_ret1_impl_(__maybenull_impl_notref) _Ret1_impl_(__zterm_impl))

//
// special _Deref_ ---
//
#define _Deref2_pre_readonly_                    _SAL1_1_Source_(_Deref2_pre_readonly_, (), _Deref2_pre1_impl_(__readaccess_impl_notref))

//
// _Ret_ ---
//

// e.g. _Ret_opt_valid_ LPSTR void* CloneSTR( _Pre_valid_ LPSTR src );
#define _Ret_opt_valid_                   _SAL1_1_Source_(_Ret_opt_valid_, (), _Ret1_impl_(__maybenull_impl_notref) _Ret_valid_impl_)
#define _Ret_opt_z_                       _SAL1_1_Source_(_Ret_opt_z_, (), _Ret2_impl_(__maybenull_impl,__zterm_impl) _Ret_valid_impl_)

// e.g. _Ret_opt_bytecap_(cb) void* AllocateMemory( size_t cb );
// Buffer capacity is described by another parameter
#define _Ret_cap_(size)                   _SAL1_1_Source_(_Ret_cap_, (size), _Ret1_impl_(__notnull_impl_notref) _Ret1_impl_(__cap_impl(size)))
#define _Ret_opt_cap_(size)               _SAL1_1_Source_(_Ret_opt_cap_, (size), _Ret1_impl_(__maybenull_impl_notref) _Ret1_impl_(__cap_impl(size)))
#define _Ret_bytecap_(size)               _SAL1_1_Source_(_Ret_bytecap_, (size), _Ret1_impl_(__notnull_impl_notref) _Ret1_impl_(__bytecap_impl(size)))
#define _Ret_opt_bytecap_(size)           _SAL1_1_Source_(_Ret_opt_bytecap_, (size), _Ret1_impl_(__maybenull_impl_notref) _Ret1_impl_(__bytecap_impl(size)))

// Buffer capacity is described by a constant expression
#define _Ret_cap_c_(size)                 _SAL1_1_Source_(_Ret_cap_c_, (size), _Ret1_impl_(__notnull_impl_notref) _Ret1_impl_(__cap_c_impl(size)))
#define _Ret_opt_cap_c_(size)             _SAL1_1_Source_(_Ret_opt_cap_c_, (size), _Ret1_impl_(__maybenull_impl_notref) _Ret1_impl_(__cap_c_impl(size)))
#define _Ret_bytecap_c_(size)             _SAL1_1_Source_(_Ret_bytecap_c_, (size), _Ret1_impl_(__notnull_impl_notref) _Ret1_impl_(__bytecap_c_impl(size)))
#define _Ret_opt_bytecap_c_(size)         _SAL1_1_Source_(_Ret_opt_bytecap_c_, (size), _Ret1_impl_(__maybenull_impl_notref) _Ret1_impl_(__bytecap_c_impl(size)))

// Buffer capacity is described by a complex condition
#define _Ret_cap_x_(size)                 _SAL1_1_Source_(_Ret_cap_x_, (size), _Ret1_impl_(__notnull_impl_notref) _Ret1_impl_(__cap_x_impl(size)))
#define _Ret_opt_cap_x_(size)             _SAL1_1_Source_(_Ret_opt_cap_x_, (size), _Ret1_impl_(__maybenull_impl_notref) _Ret1_impl_(__cap_x_impl(size)))
#define _Ret_bytecap_x_(size)             _SAL1_1_Source_(_Ret_bytecap_x_, (size), _Ret1_impl_(__notnull_impl_notref) _Ret1_impl_(__bytecap_x_impl(size)))
#define _Ret_opt_bytecap_x_(size)         _SAL1_1_Source_(_Ret_opt_bytecap_x_, (size), _Ret1_impl_(__maybenull_impl_notref) _Ret1_impl_(__bytecap_x_impl(size)))

// return value is nullterminated and capacity is given by another parameter
#define _Ret_z_cap_(size)                 _SAL1_1_Source_(_Ret_z_cap_, (size), _Ret1_impl_(__notnull_impl_notref) _Ret2_impl_(__zterm_impl,__cap_impl(size))     _Ret_valid_impl_)
#define _Ret_opt_z_cap_(size)             _SAL1_1_Source_(_Ret_opt_z_cap_, (size), _Ret1_impl_(__maybenull_impl_notref) _Ret2_impl_(__zterm_impl,__cap_impl(size))     _Ret_valid_impl_)
#define _Ret_z_bytecap_(size)             _SAL1_1_Source_(_Ret_z_bytecap_, (size), _Ret1_impl_(__notnull_impl_notref) _Ret2_impl_(__zterm_impl,__bytecap_impl(size)) _Ret_valid_impl_)
#define _Ret_opt_z_bytecap_(size)         _SAL1_1_Source_(_Ret_opt_z_bytecap_, (size), _Ret1_impl_(__maybenull_impl_notref) _Ret2_impl_(__zterm_impl,__bytecap_impl(size)) _Ret_valid_impl_)

// e.g. _Ret_opt_bytecount_(cb) void* AllocateZeroInitializedMemory( size_t cb );
// Valid Buffer extent is described by another parameter
#define _Ret_count_(size)                 _SAL1_1_Source_(_Ret_count_, (size), _Ret1_impl_(__notnull_impl_notref) _Ret1_impl_(__count_impl(size))     _Ret_valid_impl_)
#define _Ret_opt_count_(size)             _SAL1_1_Source_(_Ret_opt_count_, (size), _Ret1_impl_(__maybenull_impl_notref) _Ret1_impl_(__count_impl(size))     _Ret_valid_impl_)
#define _Ret_bytecount_(size)             _SAL1_1_Source_(_Ret_bytecount_, (size), _Ret1_impl_(__notnull_impl_notref) _Ret1_impl_(__bytecount_impl(size)) _Ret_valid_impl_)
#define _Ret_opt_bytecount_(size)         _SAL1_1_Source_(_Ret_opt_bytecount_, (size), _Ret1_impl_(__maybenull_impl_notref) _Ret1_impl_(__bytecount_impl(size)) _Ret_valid_impl_)

// Valid Buffer extent is described by a constant expression
#define _Ret_count_c_(size)               _SAL1_1_Source_(_Ret_count_c_, (size), _Ret1_impl_(__notnull_impl_notref) _Ret1_impl_(__count_c_impl(size))     _Ret_valid_impl_)
#define _Ret_opt_count_c_(size)           _SAL1_1_Source_(_Ret_opt_count_c_, (size), _Ret1_impl_(__maybenull_impl_notref) _Ret1_impl_(__count_c_impl(size))     _Ret_valid_impl_)
#define _Ret_bytecount_c_(size)           _SAL1_1_Source_(_Ret_bytecount_c_, (size), _Ret1_impl_(__notnull_impl_notref) _Ret1_impl_(__bytecount_c_impl(size)) _Ret_valid_impl_)
#define _Ret_opt_bytecount_c_(size)       _SAL1_1_Source_(_Ret_opt_bytecount_c_, (size), _Ret1_impl_(__maybenull_impl_notref) _Ret1_impl_(__bytecount_c_impl(size)) _Ret_valid_impl_)

// Valid Buffer extent is described by a complex expression
#define _Ret_count_x_(size)               _SAL1_1_Source_(_Ret_count_x_, (size), _Ret1_impl_(__notnull_impl_notref) _Ret1_impl_(__count_x_impl(size))     _Ret_valid_impl_)
#define _Ret_opt_count_x_(size)           _SAL1_1_Source_(_Ret_opt_count_x_, (size), _Ret1_impl_(__maybenull_impl_notref) _Ret1_impl_(__count_x_impl(size))     _Ret_valid_impl_)
#define _Ret_bytecount_x_(size)           _SAL1_1_Source_(_Ret_bytecount_x_, (size), _Ret1_impl_(__notnull_impl_notref) _Ret1_impl_(__bytecount_x_impl(size)) _Ret_valid_impl_)
#define _Ret_opt_bytecount_x_(size)       _SAL1_1_Source_(_Ret_opt_bytecount_x_, (size), _Ret1_impl_(__maybenull_impl_notref) _Ret1_impl_(__bytecount_x_impl(size)) _Ret_valid_impl_)

// return value is nullterminated and length is given by another parameter
#define _Ret_z_count_(size)               _SAL1_1_Source_(_Ret_z_count_, (size), _Ret1_impl_(__notnull_impl_notref) _Ret2_impl_(__zterm_impl,__count_impl(size))     _Ret_valid_impl_)
#define _Ret_opt_z_count_(size)           _SAL1_1_Source_(_Ret_opt_z_count_, (size), _Ret1_impl_(__maybenull_impl_notref) _Ret2_impl_(__zterm_impl,__count_impl(size))     _Ret_valid_impl_)
#define _Ret_z_bytecount_(size)           _SAL1_1_Source_(_Ret_z_bytecount_, (size), _Ret1_impl_(__notnull_impl_notref) _Ret2_impl_(__zterm_impl,__bytecount_impl(size)) _Ret_valid_impl_)
#define _Ret_opt_z_bytecount_(size)       _SAL1_1_Source_(_Ret_opt_z_bytecount_, (size), _Ret1_impl_(__maybenull_impl_notref) _Ret2_impl_(__zterm_impl,__bytecount_impl(size)) _Ret_valid_impl_)


// _Pre_ annotations ---
#define _Pre_opt_z_                       _SAL1_1_Source_(_Pre_opt_z_, (), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__zterm_impl) _Pre_valid_impl_)

// restrict access rights
#define _Pre_readonly_                    _SAL1_1_Source_(_Pre_readonly_, (), _Pre1_impl_(__readaccess_impl_notref))
#define _Pre_writeonly_                   _SAL1_1_Source_(_Pre_writeonly_, (), _Pre1_impl_(__writeaccess_impl_notref))

// e.g. void FreeMemory( _Pre_bytecap_(cb) _Post_ptr_invalid_ void* pv, size_t cb );
// buffer capacity described by another parameter
#define _Pre_cap_(size)                   _SAL1_1_Source_(_Pre_cap_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__cap_impl(size)))
#define _Pre_opt_cap_(size)               _SAL1_1_Source_(_Pre_opt_cap_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__cap_impl(size)))
#define _Pre_bytecap_(size)               _SAL1_1_Source_(_Pre_bytecap_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__bytecap_impl(size)))
#define _Pre_opt_bytecap_(size)           _SAL1_1_Source_(_Pre_opt_bytecap_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__bytecap_impl(size)))

// buffer capacity described by a constant expression
#define _Pre_cap_c_(size)                 _SAL1_1_Source_(_Pre_cap_c_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__cap_c_impl(size)))
#define _Pre_opt_cap_c_(size)             _SAL1_1_Source_(_Pre_opt_cap_c_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__cap_c_impl(size)))
#define _Pre_bytecap_c_(size)             _SAL1_1_Source_(_Pre_bytecap_c_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__bytecap_c_impl(size)))
#define _Pre_opt_bytecap_c_(size)         _SAL1_1_Source_(_Pre_opt_bytecap_c_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__bytecap_c_impl(size)))
#define _Pre_cap_c_one_                   _SAL1_1_Source_(_Pre_cap_c_one_, (), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__cap_c_one_notref_impl))
#define _Pre_opt_cap_c_one_               _SAL1_1_Source_(_Pre_opt_cap_c_one_, (), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__cap_c_one_notref_impl))

// buffer capacity is described by another parameter multiplied by a constant expression
#define _Pre_cap_m_(mult,size)            _SAL1_1_Source_(_Pre_cap_m_, (mult,size), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__mult_impl(mult,size)))
#define _Pre_opt_cap_m_(mult,size)        _SAL1_1_Source_(_Pre_opt_cap_m_, (mult,size), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__mult_impl(mult,size)))

// buffer capacity described by size of other buffer, only used by dangerous legacy APIs
// e.g. int strcpy(_Pre_cap_for_(src) char* dst, const char* src);
#define _Pre_cap_for_(param)              _SAL1_1_Source_(_Pre_cap_for_, (param), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__cap_for_impl(param)))
#define _Pre_opt_cap_for_(param)          _SAL1_1_Source_(_Pre_opt_cap_for_, (param), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__cap_for_impl(param)))

// buffer capacity described by a complex condition
#define _Pre_cap_x_(size)                 _SAL1_1_Source_(_Pre_cap_x_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__cap_x_impl(size)))
#define _Pre_opt_cap_x_(size)             _SAL1_1_Source_(_Pre_opt_cap_x_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__cap_x_impl(size)))
#define _Pre_bytecap_x_(size)             _SAL1_1_Source_(_Pre_bytecap_x_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__bytecap_x_impl(size)))
#define _Pre_opt_bytecap_x_(size)         _SAL1_1_Source_(_Pre_opt_bytecap_x_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__bytecap_x_impl(size)))

// buffer capacity described by the difference to another pointer parameter
#define _Pre_ptrdiff_cap_(ptr)            _SAL1_1_Source_(_Pre_ptrdiff_cap_, (ptr), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__cap_x_impl(__ptrdiff(ptr))))
#define _Pre_opt_ptrdiff_cap_(ptr)        _SAL1_1_Source_(_Pre_opt_ptrdiff_cap_, (ptr), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__cap_x_impl(__ptrdiff(ptr))))

// e.g. void AppendStr( _Pre_z_ const char* szFrom, _Pre_z_cap_(cchTo) _Post_z_ char* szTo, size_t cchTo );
#define _Pre_z_cap_(size)                 _SAL1_1_Source_(_Pre_z_cap_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre2_impl_(__zterm_impl,__cap_impl(size))       _Pre_valid_impl_)
#define _Pre_opt_z_cap_(size)             _SAL1_1_Source_(_Pre_opt_z_cap_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre2_impl_(__zterm_impl,__cap_impl(size))       _Pre_valid_impl_)
#define _Pre_z_bytecap_(size)             _SAL1_1_Source_(_Pre_z_bytecap_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre2_impl_(__zterm_impl,__bytecap_impl(size))   _Pre_valid_impl_)
#define _Pre_opt_z_bytecap_(size)         _SAL1_1_Source_(_Pre_opt_z_bytecap_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre2_impl_(__zterm_impl,__bytecap_impl(size))   _Pre_valid_impl_)

#define _Pre_z_cap_c_(size)               _SAL1_1_Source_(_Pre_z_cap_c_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre2_impl_(__zterm_impl,__cap_c_impl(size))     _Pre_valid_impl_)
#define _Pre_opt_z_cap_c_(size)           _SAL1_1_Source_(_Pre_opt_z_cap_c_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre2_impl_(__zterm_impl,__cap_c_impl(size))     _Pre_valid_impl_)
#define _Pre_z_bytecap_c_(size)           _SAL1_1_Source_(_Pre_z_bytecap_c_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre2_impl_(__zterm_impl,__bytecap_c_impl(size)) _Pre_valid_impl_)
#define _Pre_opt_z_bytecap_c_(size)       _SAL1_1_Source_(_Pre_opt_z_bytecap_c_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre2_impl_(__zterm_impl,__bytecap_c_impl(size)) _Pre_valid_impl_)

#define _Pre_z_cap_x_(size)               _SAL1_1_Source_(_Pre_z_cap_x_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre2_impl_(__zterm_impl,__cap_x_impl(size))     _Pre_valid_impl_)
#define _Pre_opt_z_cap_x_(size)           _SAL1_1_Source_(_Pre_opt_z_cap_x_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre2_impl_(__zterm_impl,__cap_x_impl(size))     _Pre_valid_impl_)
#define _Pre_z_bytecap_x_(size)           _SAL1_1_Source_(_Pre_z_bytecap_x_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre2_impl_(__zterm_impl,__bytecap_x_impl(size)) _Pre_valid_impl_)
#define _Pre_opt_z_bytecap_x_(size)       _SAL1_1_Source_(_Pre_opt_z_bytecap_x_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre2_impl_(__zterm_impl,__bytecap_x_impl(size)) _Pre_valid_impl_)

// known capacity and valid but unknown readable extent
#define _Pre_valid_cap_(size)             _SAL1_1_Source_(_Pre_valid_cap_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__cap_impl(size))       _Pre_valid_impl_)
#define _Pre_opt_valid_cap_(size)         _SAL1_1_Source_(_Pre_opt_valid_cap_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__cap_impl(size))       _Pre_valid_impl_)
#define _Pre_valid_bytecap_(size)         _SAL1_1_Source_(_Pre_valid_bytecap_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__bytecap_impl(size))   _Pre_valid_impl_)
#define _Pre_opt_valid_bytecap_(size)     _SAL1_1_Source_(_Pre_opt_valid_bytecap_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__bytecap_impl(size))   _Pre_valid_impl_)

#define _Pre_valid_cap_c_(size)           _SAL1_1_Source_(_Pre_valid_cap_c_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__cap_c_impl(size))     _Pre_valid_impl_)
#define _Pre_opt_valid_cap_c_(size)       _SAL1_1_Source_(_Pre_opt_valid_cap_c_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__cap_c_impl(size))     _Pre_valid_impl_)
#define _Pre_valid_bytecap_c_(size)       _SAL1_1_Source_(_Pre_valid_bytecap_c_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__bytecap_c_impl(size)) _Pre_valid_impl_)
#define _Pre_opt_valid_bytecap_c_(size)   _SAL1_1_Source_(_Pre_opt_valid_bytecap_c_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__bytecap_c_impl(size)) _Pre_valid_impl_)

#define _Pre_valid_cap_x_(size)           _SAL1_1_Source_(_Pre_valid_cap_x_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__cap_x_impl(size))     _Pre_valid_impl_)
#define _Pre_opt_valid_cap_x_(size)       _SAL1_1_Source_(_Pre_opt_valid_cap_x_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__cap_x_impl(size))     _Pre_valid_impl_)
#define _Pre_valid_bytecap_x_(size)       _SAL1_1_Source_(_Pre_valid_bytecap_x_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__bytecap_x_impl(size)) _Pre_valid_impl_)
#define _Pre_opt_valid_bytecap_x_(size)   _SAL1_1_Source_(_Pre_opt_valid_bytecap_x_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__bytecap_x_impl(size)) _Pre_valid_impl_)

// e.g. void AppendCharRange( _Pre_count_(cchFrom) const char* rgFrom, size_t cchFrom, _Out_z_cap_(cchTo) char* szTo, size_t cchTo );
// Valid buffer extent described by another parameter
#define _Pre_count_(size)                 _SAL1_1_Source_(_Pre_count_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__count_impl(size))       _Pre_valid_impl_)
#define _Pre_opt_count_(size)             _SAL1_1_Source_(_Pre_opt_count_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__count_impl(size))       _Pre_valid_impl_)
#define _Pre_bytecount_(size)             _SAL1_1_Source_(_Pre_bytecount_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__bytecount_impl(size))   _Pre_valid_impl_)
#define _Pre_opt_bytecount_(size)         _SAL1_1_Source_(_Pre_opt_bytecount_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__bytecount_impl(size))   _Pre_valid_impl_)

// Valid buffer extent described by a constant expression
#define _Pre_count_c_(size)               _SAL1_1_Source_(_Pre_count_c_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__count_c_impl(size))     _Pre_valid_impl_)
#define _Pre_opt_count_c_(size)           _SAL1_1_Source_(_Pre_opt_count_c_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__count_c_impl(size))     _Pre_valid_impl_)
#define _Pre_bytecount_c_(size)           _SAL1_1_Source_(_Pre_bytecount_c_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__bytecount_c_impl(size)) _Pre_valid_impl_)
#define _Pre_opt_bytecount_c_(size)       _SAL1_1_Source_(_Pre_opt_bytecount_c_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__bytecount_c_impl(size)) _Pre_valid_impl_)

// Valid buffer extent described by a complex expression
#define _Pre_count_x_(size)               _SAL1_1_Source_(_Pre_count_x_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__count_x_impl(size))     _Pre_valid_impl_)
#define _Pre_opt_count_x_(size)           _SAL1_1_Source_(_Pre_opt_count_x_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__count_x_impl(size))     _Pre_valid_impl_)
#define _Pre_bytecount_x_(size)           _SAL1_1_Source_(_Pre_bytecount_x_, (size), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__bytecount_x_impl(size)) _Pre_valid_impl_)
#define _Pre_opt_bytecount_x_(size)       _SAL1_1_Source_(_Pre_opt_bytecount_x_, (size), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__bytecount_x_impl(size)) _Pre_valid_impl_)

// Valid buffer extent described by the difference to another pointer parameter
#define _Pre_ptrdiff_count_(ptr)          _SAL1_1_Source_(_Pre_ptrdiff_count_, (ptr), _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__count_x_impl(__ptrdiff(ptr))) _Pre_valid_impl_)
#define _Pre_opt_ptrdiff_count_(ptr)      _SAL1_1_Source_(_Pre_opt_ptrdiff_count_, (ptr), _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__count_x_impl(__ptrdiff(ptr))) _Pre_valid_impl_)


// char * strncpy(_Out_cap_(_Count) _Post_maybez_ char * _Dest, _In_z_ const char * _Source, _In_ size_t _Count)
// buffer maybe zero-terminated after the call
#define _Post_maybez_                    _SAL1_1_Source_(_Post_maybez_, (), _Post1_impl_(__maybezterm_impl))

// e.g. SIZE_T HeapSize( _In_ HANDLE hHeap, DWORD dwFlags, _Pre_notnull_ _Post_bytecap_(return) LPCVOID lpMem );
#define _Post_cap_(size)                 _SAL1_1_Source_(_Post_cap_, (size), _Post1_impl_(__cap_impl(size)))
#define _Post_bytecap_(size)             _SAL1_1_Source_(_Post_bytecap_, (size), _Post1_impl_(__bytecap_impl(size)))

// e.g. int strlen( _In_z_ _Post_count_(return+1) const char* sz );
#define _Post_count_(size)               _SAL1_1_Source_(_Post_count_, (size), _Post1_impl_(__count_impl(size))       _Post_valid_impl_)
#define _Post_bytecount_(size)           _SAL1_1_Source_(_Post_bytecount_, (size), _Post1_impl_(__bytecount_impl(size))   _Post_valid_impl_)
#define _Post_count_c_(size)             _SAL1_1_Source_(_Post_count_c_, (size), _Post1_impl_(__count_c_impl(size))     _Post_valid_impl_)
#define _Post_bytecount_c_(size)         _SAL1_1_Source_(_Post_bytecount_c_, (size), _Post1_impl_(__bytecount_c_impl(size)) _Post_valid_impl_)
#define _Post_count_x_(size)             _SAL1_1_Source_(_Post_count_x_, (size), _Post1_impl_(__count_x_impl(size))     _Post_valid_impl_)
#define _Post_bytecount_x_(size)         _SAL1_1_Source_(_Post_bytecount_x_, (size), _Post1_impl_(__bytecount_x_impl(size)) _Post_valid_impl_)

// e.g. size_t CopyStr( _In_z_ const char* szFrom, _Pre_cap_(cch) _Post_z_count_(return+1) char* szFrom, size_t cchFrom );
#define _Post_z_count_(size)             _SAL1_1_Source_(_Post_z_count_, (size), _Post2_impl_(__zterm_impl,__count_impl(size))       _Post_valid_impl_)
#define _Post_z_bytecount_(size)         _SAL1_1_Source_(_Post_z_bytecount_, (size), _Post2_impl_(__zterm_impl,__bytecount_impl(size))   _Post_valid_impl_)
#define _Post_z_count_c_(size)           _SAL1_1_Source_(_Post_z_count_c_, (size), _Post2_impl_(__zterm_impl,__count_c_impl(size))     _Post_valid_impl_)
#define _Post_z_bytecount_c_(size)       _SAL1_1_Source_(_Post_z_bytecount_c_, (size), _Post2_impl_(__zterm_impl,__bytecount_c_impl(size)) _Post_valid_impl_)
#define _Post_z_count_x_(size)           _SAL1_1_Source_(_Post_z_count_x_, (size), _Post2_impl_(__zterm_impl,__count_x_impl(size))     _Post_valid_impl_)
#define _Post_z_bytecount_x_(size)       _SAL1_1_Source_(_Post_z_bytecount_x_, (size), _Post2_impl_(__zterm_impl,__bytecount_x_impl(size)) _Post_valid_impl_)

//
// _Prepost_ ---
//
// describing conditions that hold before and after the function call

#define _Prepost_opt_z_                  _SAL1_1_Source_(_Prepost_opt_z_, (), _Pre_opt_z_  _Post_z_)

#define _Prepost_count_(size)            _SAL1_1_Source_(_Prepost_count_, (size), _Pre_count_(size)           _Post_count_(size))
#define _Prepost_opt_count_(size)        _SAL1_1_Source_(_Prepost_opt_count_, (size), _Pre_opt_count_(size)       _Post_count_(size))
#define _Prepost_bytecount_(size)        _SAL1_1_Source_(_Prepost_bytecount_, (size), _Pre_bytecount_(size)       _Post_bytecount_(size))
#define _Prepost_opt_bytecount_(size)    _SAL1_1_Source_(_Prepost_opt_bytecount_, (size), _Pre_opt_bytecount_(size)   _Post_bytecount_(size))
#define _Prepost_count_c_(size)          _SAL1_1_Source_(_Prepost_count_c_, (size), _Pre_count_c_(size)         _Post_count_c_(size))
#define _Prepost_opt_count_c_(size)      _SAL1_1_Source_(_Prepost_opt_count_c_, (size), _Pre_opt_count_c_(size)     _Post_count_c_(size))
#define _Prepost_bytecount_c_(size)      _SAL1_1_Source_(_Prepost_bytecount_c_, (size), _Pre_bytecount_c_(size)     _Post_bytecount_c_(size))
#define _Prepost_opt_bytecount_c_(size)  _SAL1_1_Source_(_Prepost_opt_bytecount_c_, (size), _Pre_opt_bytecount_c_(size) _Post_bytecount_c_(size))
#define _Prepost_count_x_(size)          _SAL1_1_Source_(_Prepost_count_x_, (size), _Pre_count_x_(size)         _Post_count_x_(size))
#define _Prepost_opt_count_x_(size)      _SAL1_1_Source_(_Prepost_opt_count_x_, (size), _Pre_opt_count_x_(size)     _Post_count_x_(size))
#define _Prepost_bytecount_x_(size)      _SAL1_1_Source_(_Prepost_bytecount_x_, (size), _Pre_bytecount_x_(size)     _Post_bytecount_x_(size))
#define _Prepost_opt_bytecount_x_(size)  _SAL1_1_Source_(_Prepost_opt_bytecount_x_, (size), _Pre_opt_bytecount_x_(size) _Post_bytecount_x_(size))

#define _Prepost_valid_                   _SAL1_1_Source_(_Prepost_valid_, (), _Pre_valid_     _Post_valid_)
#define _Prepost_opt_valid_               _SAL1_1_Source_(_Prepost_opt_valid_, (), _Pre_opt_valid_ _Post_valid_)

//
// _Deref_<both> ---
//
// short version for _Deref_pre_<ann> _Deref_post_<ann>
// describing conditions for array elements or dereferenced pointer parameters that hold before and after the call

#define _Deref_prepost_z_                         _SAL1_1_Source_(_Deref_prepost_z_, (), _Deref_pre_z_      _Deref_post_z_)
#define _Deref_prepost_opt_z_                     _SAL1_1_Source_(_Deref_prepost_opt_z_, (), _Deref_pre_opt_z_  _Deref_post_opt_z_)

#define _Deref_prepost_cap_(size)                 _SAL1_1_Source_(_Deref_prepost_cap_, (size), _Deref_pre_cap_(size)                _Deref_post_cap_(size))
#define _Deref_prepost_opt_cap_(size)             _SAL1_1_Source_(_Deref_prepost_opt_cap_, (size), _Deref_pre_opt_cap_(size)            _Deref_post_opt_cap_(size))
#define _Deref_prepost_bytecap_(size)             _SAL1_1_Source_(_Deref_prepost_bytecap_, (size), _Deref_pre_bytecap_(size)            _Deref_post_bytecap_(size))
#define _Deref_prepost_opt_bytecap_(size)         _SAL1_1_Source_(_Deref_prepost_opt_bytecap_, (size), _Deref_pre_opt_bytecap_(size)        _Deref_post_opt_bytecap_(size))

#define _Deref_prepost_cap_x_(size)               _SAL1_1_Source_(_Deref_prepost_cap_x_, (size), _Deref_pre_cap_x_(size)              _Deref_post_cap_x_(size))
#define _Deref_prepost_opt_cap_x_(size)           _SAL1_1_Source_(_Deref_prepost_opt_cap_x_, (size), _Deref_pre_opt_cap_x_(size)          _Deref_post_opt_cap_x_(size))
#define _Deref_prepost_bytecap_x_(size)           _SAL1_1_Source_(_Deref_prepost_bytecap_x_, (size), _Deref_pre_bytecap_x_(size)          _Deref_post_bytecap_x_(size))
#define _Deref_prepost_opt_bytecap_x_(size)       _SAL1_1_Source_(_Deref_prepost_opt_bytecap_x_, (size), _Deref_pre_opt_bytecap_x_(size)      _Deref_post_opt_bytecap_x_(size))

#define _Deref_prepost_z_cap_(size)               _SAL1_1_Source_(_Deref_prepost_z_cap_, (size), _Deref_pre_z_cap_(size)              _Deref_post_z_cap_(size))
#define _Deref_prepost_opt_z_cap_(size)           _SAL1_1_Source_(_Deref_prepost_opt_z_cap_, (size), _Deref_pre_opt_z_cap_(size)          _Deref_post_opt_z_cap_(size))
#define _Deref_prepost_z_bytecap_(size)           _SAL1_1_Source_(_Deref_prepost_z_bytecap_, (size), _Deref_pre_z_bytecap_(size)          _Deref_post_z_bytecap_(size))
#define _Deref_prepost_opt_z_bytecap_(size)       _SAL1_1_Source_(_Deref_prepost_opt_z_bytecap_, (size), _Deref_pre_opt_z_bytecap_(size)      _Deref_post_opt_z_bytecap_(size))

#define _Deref_prepost_valid_cap_(size)           _SAL1_1_Source_(_Deref_prepost_valid_cap_, (size), _Deref_pre_valid_cap_(size)          _Deref_post_valid_cap_(size))
#define _Deref_prepost_opt_valid_cap_(size)       _SAL1_1_Source_(_Deref_prepost_opt_valid_cap_, (size), _Deref_pre_opt_valid_cap_(size)      _Deref_post_opt_valid_cap_(size))
#define _Deref_prepost_valid_bytecap_(size)       _SAL1_1_Source_(_Deref_prepost_valid_bytecap_, (size), _Deref_pre_valid_bytecap_(size)      _Deref_post_valid_bytecap_(size))
#define _Deref_prepost_opt_valid_bytecap_(size)   _SAL1_1_Source_(_Deref_prepost_opt_valid_bytecap_, (size), _Deref_pre_opt_valid_bytecap_(size)  _Deref_post_opt_valid_bytecap_(size))

#define _Deref_prepost_valid_cap_x_(size)           _SAL1_1_Source_(_Deref_prepost_valid_cap_x_, (size), _Deref_pre_valid_cap_x_(size)          _Deref_post_valid_cap_x_(size))
#define _Deref_prepost_opt_valid_cap_x_(size)       _SAL1_1_Source_(_Deref_prepost_opt_valid_cap_x_, (size), _Deref_pre_opt_valid_cap_x_(size)      _Deref_post_opt_valid_cap_x_(size))
#define _Deref_prepost_valid_bytecap_x_(size)       _SAL1_1_Source_(_Deref_prepost_valid_bytecap_x_, (size), _Deref_pre_valid_bytecap_x_(size)      _Deref_post_valid_bytecap_x_(size))
#define _Deref_prepost_opt_valid_bytecap_x_(size)   _SAL1_1_Source_(_Deref_prepost_opt_valid_bytecap_x_, (size), _Deref_pre_opt_valid_bytecap_x_(size)  _Deref_post_opt_valid_bytecap_x_(size))

#define _Deref_prepost_count_(size)             _SAL1_1_Source_(_Deref_prepost_count_, (size), _Deref_pre_count_(size)            _Deref_post_count_(size))
#define _Deref_prepost_opt_count_(size)         _SAL1_1_Source_(_Deref_prepost_opt_count_, (size), _Deref_pre_opt_count_(size)        _Deref_post_opt_count_(size))
#define _Deref_prepost_bytecount_(size)         _SAL1_1_Source_(_Deref_prepost_bytecount_, (size), _Deref_pre_bytecount_(size)        _Deref_post_bytecount_(size))
#define _Deref_prepost_opt_bytecount_(size)     _SAL1_1_Source_(_Deref_prepost_opt_bytecount_, (size), _Deref_pre_opt_bytecount_(size)    _Deref_post_opt_bytecount_(size))

#define _Deref_prepost_count_x_(size)           _SAL1_1_Source_(_Deref_prepost_count_x_, (size), _Deref_pre_count_x_(size)          _Deref_post_count_x_(size))
#define _Deref_prepost_opt_count_x_(size)       _SAL1_1_Source_(_Deref_prepost_opt_count_x_, (size), _Deref_pre_opt_count_x_(size)      _Deref_post_opt_count_x_(size))
#define _Deref_prepost_bytecount_x_(size)       _SAL1_1_Source_(_Deref_prepost_bytecount_x_, (size), _Deref_pre_bytecount_x_(size)      _Deref_post_bytecount_x_(size))
#define _Deref_prepost_opt_bytecount_x_(size)   _SAL1_1_Source_(_Deref_prepost_opt_bytecount_x_, (size), _Deref_pre_opt_bytecount_x_(size)  _Deref_post_opt_bytecount_x_(size))

#define _Deref_prepost_valid_                    _SAL1_1_Source_(_Deref_prepost_valid_, (), _Deref_pre_valid_     _Deref_post_valid_)
#define _Deref_prepost_opt_valid_                _SAL1_1_Source_(_Deref_prepost_opt_valid_, (), _Deref_pre_opt_valid_ _Deref_post_opt_valid_)

//
// _Deref_<miscellaneous>
//
// used with references to arrays

#define _Deref_out_z_cap_c_(size)  _SAL1_1_Source_(_Deref_out_z_cap_c_, (size), _Deref_pre_cap_c_(size) _Deref_post_z_)
#define _Deref_inout_z_cap_c_(size)  _SAL1_1_Source_(_Deref_inout_z_cap_c_, (size), _Deref_pre_z_cap_c_(size) _Deref_post_z_)
#define _Deref_out_z_bytecap_c_(size)  _SAL1_1_Source_(_Deref_out_z_bytecap_c_, (size), _Deref_pre_bytecap_c_(size) _Deref_post_z_)
#define _Deref_inout_z_bytecap_c_(size)  _SAL1_1_Source_(_Deref_inout_z_bytecap_c_, (size), _Deref_pre_z_bytecap_c_(size) _Deref_post_z_)
#define _Deref_inout_z_  _SAL1_1_Source_(_Deref_inout_z_, (), _Deref_prepost_z_)

// #pragma endregion Input Buffer SAL 1 compatibility macros


//============================================================================
//   Implementation Layer:
//============================================================================


// Naming conventions:
// A symbol the begins with _SA_ is for the machinery of creating any
// annotations; many of those come from sourceannotations.h in the case
// of attributes.

// A symbol that ends with _impl is the very lowest level macro.  It is
// not required to be a legal standalone annotation, and in the case
// of attribute annotations, usually is not.  (In the case of some declspec
// annotations, it might be, but it should not be assumed so.)  Those
// symols will be used in the _PreN..., _PostN... and _RetN... annotations
// to build up more complete annotations.

// A symbol ending in _impl_ is reserved to the implementation as well,
// but it does form a complete annotation; usually they are used to build
// up even higher level annotations.


#if _USE_ATTRIBUTES_FOR_SAL || _USE_DECLSPECS_FOR_SAL // [
// Sharable "_impl" macros: these can be shared between the various annotation
// forms but are part of the implementation of the macros.  These are collected
// here to assure that only necessary differences in the annotations
// exist.

#define _Always_impl_(annos)            _Group_(annos _SAL_nop_impl_) _On_failure_impl_(annos _SAL_nop_impl_)
#define _Bound_impl_                    _SA_annotes0(SAL_bound)
#define _Field_range_impl_(min,max)     _Range_impl_(min,max)
#define _Literal_impl_                  _SA_annotes1(SAL_constant, __yes)
#define _Maybenull_impl_                _SA_annotes1(SAL_null, __maybe)
#define _Maybevalid_impl_               _SA_annotes1(SAL_valid, __maybe)
#define _Must_inspect_impl_ _Post_impl_ _SA_annotes0(SAL_mustInspect)
#define _Notliteral_impl_               _SA_annotes1(SAL_constant, __no)
#define _Notnull_impl_                  _SA_annotes1(SAL_null, __no)
#define _Notvalid_impl_                 _SA_annotes1(SAL_valid, __no)
#define _NullNull_terminated_impl_      _Group_(_SA_annotes1(SAL_nullTerminated, __yes) _SA_annotes1(SAL_readableTo,inexpressibleCount("NullNull terminated string")))
#define _Null_impl_                     _SA_annotes1(SAL_null, __yes)
#define _Null_terminated_impl_          _SA_annotes1(SAL_nullTerminated, __yes)
#define _Out_impl_                      _Pre1_impl_(__notnull_impl_notref) _Pre1_impl_(__cap_c_one_notref_impl) _Post_valid_impl_
#define _Out_opt_impl_                  _Pre1_impl_(__maybenull_impl_notref) _Pre1_impl_(__cap_c_one_notref_impl) _Post_valid_impl_
#define _Points_to_data_impl_           _At_(*_Curr_, _SA_annotes1(SAL_mayBePointer, __no))
#define _Post_satisfies_impl_(cond)     _Post_impl_ _Satisfies_impl_(cond)
#define _Post_valid_impl_               _Post1_impl_(__valid_impl)
#define _Pre_satisfies_impl_(cond)      _Pre_impl_ _Satisfies_impl_(cond)
#define _Pre_valid_impl_                _Pre1_impl_(__valid_impl)
#define _Range_impl_(min,max)           _SA_annotes2(SAL_range, min, max)
#define _Readable_bytes_impl_(size)     _SA_annotes1(SAL_readableTo, byteCount(size))
#define _Readable_elements_impl_(size)  _SA_annotes1(SAL_readableTo, elementCount(size))
#define _Ret_valid_impl_                _Ret1_impl_(__valid_impl)
#define _Satisfies_impl_(cond)          _SA_annotes1(SAL_satisfies, cond)
#define _Valid_impl_                    _SA_annotes1(SAL_valid, __yes)
#define _Writable_bytes_impl_(size)     _SA_annotes1(SAL_writableTo, byteCount(size))
#define _Writable_elements_impl_(size)  _SA_annotes1(SAL_writableTo, elementCount(size))

#define _In_range_impl_(min,max)        _Pre_impl_ _Range_impl_(min,max)
#define _Out_range_impl_(min,max)       _Post_impl_ _Range_impl_(min,max)
#define _Ret_range_impl_(min,max)       _Post_impl_ _Range_impl_(min,max)
#define _Deref_in_range_impl_(min,max)  _Deref_pre_impl_ _Range_impl_(min,max)
#define _Deref_out_range_impl_(min,max) _Deref_post_impl_ _Range_impl_(min,max)
#define _Deref_ret_range_impl_(min,max) _Deref_post_impl_ _Range_impl_(min,max)

#define _Deref_pre_impl_                _Pre_impl_  _Notref_impl_ _Deref_impl_
#define _Deref_post_impl_               _Post_impl_ _Notref_impl_ _Deref_impl_

// The following are for the implementation machinery, and are not
// suitable for annotating general code.
// We're tying to phase this out, someday.  The parser quotes the param.
#define __AuToQuOtE                     _SA_annotes0(SAL_AuToQuOtE)

// Normally the parser does some simple type checking of annotation params,
// defer that check to the plugin.
#define __deferTypecheck                _SA_annotes0(SAL_deferTypecheck)

#define _SA_SPECSTRIZE( x ) #x
#define _SAL_nop_impl_       /* nothing */
#define __nop_impl(x)            x
#endif


#if _USE_ATTRIBUTES_FOR_SAL // [

// Using attributes for sal

#include "codeanalysis\sourceannotations.h"


#define _SA_annotes0(n)                [SAL_annotes(Name=#n)]
#define _SA_annotes1(n,pp1)            [SAL_annotes(Name=#n, p1=_SA_SPECSTRIZE(pp1))]
#define _SA_annotes2(n,pp1,pp2)        [SAL_annotes(Name=#n, p1=_SA_SPECSTRIZE(pp1), p2=_SA_SPECSTRIZE(pp2))]
#define _SA_annotes3(n,pp1,pp2,pp3)    [SAL_annotes(Name=#n, p1=_SA_SPECSTRIZE(pp1), p2=_SA_SPECSTRIZE(pp2), p3=_SA_SPECSTRIZE(pp3))]

#define _Pre_impl_                     [SAL_pre]
#define _Post_impl_                    [SAL_post]
#define _Deref_impl_                   [SAL_deref]
#define _Notref_impl_                  [SAL_notref]


// Declare a function to be an annotation or primop (respectively).
// Done this way so that they don't appear in the regular compiler's
// namespace.
#define __ANNOTATION(fun)              _SA_annotes0(SAL_annotation)  void __SA_##fun;
#define __PRIMOP(type, fun)            _SA_annotes0(SAL_primop)  type __SA_##fun;
#define __QUALIFIER(fun)               _SA_annotes0(SAL_qualifier)  void __SA_##fun;

// Benign declspec needed here for WindowsPREfast
#define __In_impl_ [SA_Pre(Valid=SA_Yes)] [SA_Pre(Deref=1, Notref=1, Access=SA_Read)] __declspec("SAL_pre SAL_valid")

#elif _USE_DECLSPECS_FOR_SAL // ][

// Using declspecs for sal

#define _SA_annotes0(n)                __declspec(#n)
#define _SA_annotes1(n,pp1)            __declspec(#n "(" _SA_SPECSTRIZE(pp1) ")" )
#define _SA_annotes2(n,pp1,pp2)        __declspec(#n "(" _SA_SPECSTRIZE(pp1) "," _SA_SPECSTRIZE(pp2) ")")
#define _SA_annotes3(n,pp1,pp2,pp3)    __declspec(#n "(" _SA_SPECSTRIZE(pp1) "," _SA_SPECSTRIZE(pp2) "," _SA_SPECSTRIZE(pp3) ")")

#define _Pre_impl_                     _SA_annotes0(SAL_pre)
#define _Post_impl_                    _SA_annotes0(SAL_post)
#define _Deref_impl_                   _SA_annotes0(SAL_deref)
#define _Notref_impl_                  _SA_annotes0(SAL_notref)

// Declare a function to be an annotation or primop (respectively).
// Done this way so that they don't appear in the regular compiler's
// namespace.
#define __ANNOTATION(fun)              _SA_annotes0(SAL_annotation) void __SA_##fun

#define __PRIMOP(type, fun)            _SA_annotes0(SAL_primop) type __SA_##fun

#define __QUALIFIER(fun)               _SA_annotes0(SAL_qualifier)  void __SA_##fun;

#define __In_impl_ _Pre_impl_ _SA_annotes0(SAL_valid) _Pre_impl_ _Deref_impl_ _Notref_impl_ _SA_annotes0(SAL_readonly)

#else // ][

// Using "nothing" for sal

#define _SA_annotes0(n)
#define _SA_annotes1(n,pp1)
#define _SA_annotes2(n,pp1,pp2)
#define _SA_annotes3(n,pp1,pp2,pp3)

#define __ANNOTATION(fun)
#define __PRIMOP(type, fun)
#define __QUALIFIER(type, fun)

#endif // ]

#if _USE_ATTRIBUTES_FOR_SAL || _USE_DECLSPECS_FOR_SAL // [

// Declare annotations that need to be declared.
__ANNOTATION(SAL_useHeader(void));
__ANNOTATION(SAL_bound(void));
__ANNOTATION(SAL_allocator(void));   //??? resolve with PFD
__ANNOTATION(SAL_file_parser(__AuToQuOtE __In_impl_ char *, __In_impl_ char *));
__ANNOTATION(SAL_source_code_content(__In_impl_ char *));
__ANNOTATION(SAL_analysisHint(__AuToQuOtE __In_impl_ char *));
__ANNOTATION(SAL_untrusted_data_source(__AuToQuOtE __In_impl_ char *));
__ANNOTATION(SAL_untrusted_data_source_this(__AuToQuOtE __In_impl_ char *));
__ANNOTATION(SAL_validated(__AuToQuOtE __In_impl_ char *));
__ANNOTATION(SAL_validated_this(__AuToQuOtE __In_impl_ char *));
__ANNOTATION(SAL_encoded(void));
__ANNOTATION(SAL_adt(__AuToQuOtE __In_impl_ char *, __AuToQuOtE __In_impl_ char *));
__ANNOTATION(SAL_add_adt_property(__AuToQuOtE __In_impl_ char *, __AuToQuOtE __In_impl_ char *));
__ANNOTATION(SAL_remove_adt_property(__AuToQuOtE __In_impl_ char *, __AuToQuOtE __In_impl_ char *));
__ANNOTATION(SAL_transfer_adt_property_from(__AuToQuOtE __In_impl_ char *));
__ANNOTATION(SAL_post_type(__AuToQuOtE __In_impl_ char *));
__ANNOTATION(SAL_volatile(void));
__ANNOTATION(SAL_nonvolatile(void));
__ANNOTATION(SAL_entrypoint(__AuToQuOtE __In_impl_ char *, __AuToQuOtE __In_impl_ char *));
__ANNOTATION(SAL_blocksOn(__In_impl_ void*));
__ANNOTATION(SAL_mustInspect(void));

// Only appears in model files, but needs to be declared.
__ANNOTATION(SAL_TypeName(__AuToQuOtE __In_impl_ char *));

// To be declared well-known soon.
__ANNOTATION(SAL_interlocked(void);)

#pragma warning (suppress: 28227 28241)
__ANNOTATION(SAL_name(__In_impl_ char *, __In_impl_ char *, __In_impl_ char *);)

__PRIMOP(char *, _Macro_value_(__In_impl_ char *));
__PRIMOP(int, _Macro_defined_(__In_impl_ char *));
__PRIMOP(char *, _Strstr_(__In_impl_ char *, __In_impl_ char *));

#endif // ]

#if _USE_ATTRIBUTES_FOR_SAL // [

#define _Check_return_impl_           [SA_Post(MustCheck=SA_Yes)]

#define _Success_impl_(expr)          [SA_Success(Condition=#expr)]
#define _On_failure_impl_(annos)      [SAL_context(p1="SAL_failed")] _Group_(_Post_impl_ _Group_(annos _SAL_nop_impl_))

#define _Printf_format_string_impl_   [SA_FormatString(Style="printf")]
#define _Scanf_format_string_impl_    [SA_FormatString(Style="scanf")]
#define _Scanf_s_format_string_impl_  [SA_FormatString(Style="scanf_s")]

#define _In_bound_impl_               [SA_PreBound(Deref=0)]
#define _Out_bound_impl_              [SA_PostBound(Deref=0)]
#define _Ret_bound_impl_              [SA_PostBound(Deref=0)]
#define _Deref_in_bound_impl_         [SA_PreBound(Deref=1)]
#define _Deref_out_bound_impl_        [SA_PostBound(Deref=1)]
#define _Deref_ret_bound_impl_        [SA_PostBound(Deref=1)]

#define __valid_impl                  Valid=SA_Yes
#define __maybevalid_impl             Valid=SA_Maybe
#define __notvalid_impl               Valid=SA_No

#define __null_impl                   Null=SA_Yes
#define __maybenull_impl              Null=SA_Maybe
#define __notnull_impl                Null=SA_No

#define __null_impl_notref        Null=SA_Yes,Notref=1
#define __maybenull_impl_notref   Null=SA_Maybe,Notref=1
#define __notnull_impl_notref     Null=SA_No,Notref=1

#define __zterm_impl              NullTerminated=SA_Yes
#define __maybezterm_impl         NullTerminated=SA_Maybe
#define __maybzterm_impl          NullTerminated=SA_Maybe
#define __notzterm_impl           NullTerminated=SA_No

#define __readaccess_impl         Access=SA_Read
#define __writeaccess_impl        Access=SA_Write
#define __allaccess_impl          Access=SA_ReadWrite

#define __readaccess_impl_notref  Access=SA_Read,Notref=1
#define __writeaccess_impl_notref Access=SA_Write,Notref=1
#define __allaccess_impl_notref   Access=SA_ReadWrite,Notref=1

#if _MSC_VER >= 1610 /*IFSTRIP=IGN*/ // [

// For SAL2, we need to expect general expressions.

#define __cap_impl(size)          WritableElements="\n"#size
#define __bytecap_impl(size)      WritableBytes="\n"#size
#define __bytecount_impl(size)    ValidBytes="\n"#size
#define __count_impl(size)        ValidElements="\n"#size

#else // ][

#define __cap_impl(size)          WritableElements=#size
#define __bytecap_impl(size)      WritableBytes=#size
#define __bytecount_impl(size)    ValidBytes=#size
#define __count_impl(size)        ValidElements=#size

#endif // ]

#define __cap_c_impl(size)        WritableElementsConst=size
#define __cap_c_one_notref_impl   WritableElementsConst=1,Notref=1
#define __cap_for_impl(param)     WritableElementsLength=#param
#define __cap_x_impl(size)        WritableElements="\n@"#size

#define __bytecap_c_impl(size)    WritableBytesConst=size
#define __bytecap_x_impl(size)    WritableBytes="\n@"#size

#define __mult_impl(mult,size)    __cap_impl((mult)*(size))

#define __count_c_impl(size)      ValidElementsConst=size
#define __count_x_impl(size)      ValidElements="\n@"#size

#define __bytecount_c_impl(size)  ValidBytesConst=size
#define __bytecount_x_impl(size)  ValidBytes="\n@"#size


#define _At_impl_(target, annos)       [SAL_at(p1=#target)] _Group_(annos)
#define _At_buffer_impl_(target, iter, bound, annos)  [SAL_at_buffer(p1=#target, p2=#iter, p3=#bound)] _Group_(annos)
#define _When_impl_(expr, annos)       [SAL_when(p1=#expr)] _Group_(annos)

#define _Group_impl_(annos)            [SAL_begin] annos [SAL_end]
#define _GrouP_impl_(annos)            [SAL_BEGIN] annos [SAL_END]

#define _Use_decl_anno_impl_               _SA_annotes0(SAL_useHeader) // this is a special case!

#define _Pre1_impl_(p1)                    [SA_Pre(p1)]
#define _Pre2_impl_(p1,p2)                 [SA_Pre(p1,p2)]
#define _Pre3_impl_(p1,p2,p3)              [SA_Pre(p1,p2,p3)]

#define _Post1_impl_(p1)                   [SA_Post(p1)]
#define _Post2_impl_(p1,p2)                [SA_Post(p1,p2)]
#define _Post3_impl_(p1,p2,p3)             [SA_Post(p1,p2,p3)]

#define _Ret1_impl_(p1)                    [SA_Post(p1)]
#define _Ret2_impl_(p1,p2)                 [SA_Post(p1,p2)]
#define _Ret3_impl_(p1,p2,p3)              [SA_Post(p1,p2,p3)]

#define _Deref_pre1_impl_(p1)              [SA_Pre(Deref=1,p1)]
#define _Deref_pre2_impl_(p1,p2)           [SA_Pre(Deref=1,p1,p2)]
#define _Deref_pre3_impl_(p1,p2,p3)        [SA_Pre(Deref=1,p1,p2,p3)]


#define _Deref_post1_impl_(p1)             [SA_Post(Deref=1,p1)]
#define _Deref_post2_impl_(p1,p2)          [SA_Post(Deref=1,p1,p2)]
#define _Deref_post3_impl_(p1,p2,p3)       [SA_Post(Deref=1,p1,p2,p3)]

#define _Deref_ret1_impl_(p1)              [SA_Post(Deref=1,p1)]
#define _Deref_ret2_impl_(p1,p2)           [SA_Post(Deref=1,p1,p2)]
#define _Deref_ret3_impl_(p1,p2,p3)        [SA_Post(Deref=1,p1,p2,p3)]

#define _Deref2_pre1_impl_(p1)             [SA_Pre(Deref=2,Notref=1,p1)]
#define _Deref2_post1_impl_(p1)            [SA_Post(Deref=2,Notref=1,p1)]
#define _Deref2_ret1_impl_(p1)             [SA_Post(Deref=2,Notref=1,p1)]

// Obsolete -- may be needed for transition to attributes.
#define __inner_typefix(ctype)             [SAL_typefix(p1=_SA_SPECSTRIZE(ctype))]
#define __inner_exceptthat                 [SAL_except]


#elif _USE_DECLSPECS_FOR_SAL // ][

#define _Check_return_impl_ __post      _SA_annotes0(SAL_checkReturn)

#define _Success_impl_(expr)            _SA_annotes1(SAL_success, expr)
#define _On_failure_impl_(annos)        _SA_annotes1(SAL_context, SAL_failed) _Group_(_Post_impl_ _Group_(_SAL_nop_impl_ annos))

#define _Printf_format_string_impl_     _SA_annotes1(SAL_IsFormatString, "printf")
#define _Scanf_format_string_impl_      _SA_annotes1(SAL_IsFormatString, "scanf")
#define _Scanf_s_format_string_impl_    _SA_annotes1(SAL_IsFormatString, "scanf_s")

#define _In_bound_impl_                 _Pre_impl_ _Bound_impl_
#define _Out_bound_impl_                _Post_impl_ _Bound_impl_
#define _Ret_bound_impl_                _Post_impl_ _Bound_impl_
#define _Deref_in_bound_impl_           _Deref_pre_impl_ _Bound_impl_
#define _Deref_out_bound_impl_          _Deref_post_impl_ _Bound_impl_
#define _Deref_ret_bound_impl_          _Deref_post_impl_ _Bound_impl_


#define __null_impl              _SA_annotes0(SAL_null) // _SA_annotes1(SAL_null, __yes)
#define __notnull_impl           _SA_annotes0(SAL_notnull) // _SA_annotes1(SAL_null, __no)
#define __maybenull_impl         _SA_annotes0(SAL_maybenull) // _SA_annotes1(SAL_null, __maybe)

#define __valid_impl             _SA_annotes0(SAL_valid) // _SA_annotes1(SAL_valid, __yes)
#define __notvalid_impl          _SA_annotes0(SAL_notvalid) // _SA_annotes1(SAL_valid, __no)
#define __maybevalid_impl        _SA_annotes0(SAL_maybevalid) // _SA_annotes1(SAL_valid, __maybe)

#define __null_impl_notref       _Notref_ _Null_impl_
#define __maybenull_impl_notref  _Notref_ _Maybenull_impl_
#define __notnull_impl_notref    _Notref_ _Notnull_impl_

#define __zterm_impl             _SA_annotes1(SAL_nullTerminated, __yes)
#define __maybezterm_impl        _SA_annotes1(SAL_nullTerminated, __maybe)
#define __maybzterm_impl         _SA_annotes1(SAL_nullTerminated, __maybe)
#define __notzterm_impl          _SA_annotes1(SAL_nullTerminated, __no)

#define __readaccess_impl        _SA_annotes1(SAL_access, 0x1)
#define __writeaccess_impl       _SA_annotes1(SAL_access, 0x2)
#define __allaccess_impl         _SA_annotes1(SAL_access, 0x3)

#define __readaccess_impl_notref  _Notref_ _SA_annotes1(SAL_access, 0x1)
#define __writeaccess_impl_notref _Notref_ _SA_annotes1(SAL_access, 0x2)
#define __allaccess_impl_notref   _Notref_ _SA_annotes1(SAL_access, 0x3)

#define __cap_impl(size)         _SA_annotes1(SAL_writableTo,elementCount(size))
#define __cap_c_impl(size)       _SA_annotes1(SAL_writableTo,elementCount(size))
#define __cap_c_one_notref_impl  _Notref_ _SA_annotes1(SAL_writableTo,elementCount(1))
#define __cap_for_impl(param)    _SA_annotes1(SAL_writableTo,inexpressibleCount(sizeof(param)))
#define __cap_x_impl(size)       _SA_annotes1(SAL_writableTo,inexpressibleCount(#size))

#define __bytecap_impl(size)     _SA_annotes1(SAL_writableTo,byteCount(size))
#define __bytecap_c_impl(size)   _SA_annotes1(SAL_writableTo,byteCount(size))
#define __bytecap_x_impl(size)   _SA_annotes1(SAL_writableTo,inexpressibleCount(#size))

#define __mult_impl(mult,size)   _SA_annotes1(SAL_writableTo,(mult)*(size))

#define __count_impl(size)       _SA_annotes1(SAL_readableTo,elementCount(size))
#define __count_c_impl(size)     _SA_annotes1(SAL_readableTo,elementCount(size))
#define __count_x_impl(size)     _SA_annotes1(SAL_readableTo,inexpressibleCount(#size))

#define __bytecount_impl(size)   _SA_annotes1(SAL_readableTo,byteCount(size))
#define __bytecount_c_impl(size) _SA_annotes1(SAL_readableTo,byteCount(size))
#define __bytecount_x_impl(size) _SA_annotes1(SAL_readableTo,inexpressibleCount(#size))

#define _At_impl_(target, annos)     _SA_annotes0(SAL_at(target)) _Group_(annos)
#define _At_buffer_impl_(target, iter, bound, annos)  _SA_annotes3(SAL_at_buffer, target, iter, bound) _Group_(annos)
#define _Group_impl_(annos)          _SA_annotes0(SAL_begin) annos _SA_annotes0(SAL_end)
#define _GrouP_impl_(annos)          _SA_annotes0(SAL_BEGIN) annos _SA_annotes0(SAL_END)
#define _When_impl_(expr, annos)     _SA_annotes0(SAL_when(expr)) _Group_(annos)

#define _Use_decl_anno_impl_         __declspec("SAL_useHeader()") // this is a special case!

#define _Pre1_impl_(p1)              _Pre_impl_ p1
#define _Pre2_impl_(p1,p2)           _Pre_impl_ p1 _Pre_impl_ p2
#define _Pre3_impl_(p1,p2,p3)        _Pre_impl_ p1 _Pre_impl_ p2 _Pre_impl_ p3

#define _Post1_impl_(p1)             _Post_impl_ p1
#define _Post2_impl_(p1,p2)          _Post_impl_ p1 _Post_impl_ p2
#define _Post3_impl_(p1,p2,p3)       _Post_impl_ p1 _Post_impl_ p2 _Post_impl_ p3

#define _Ret1_impl_(p1)              _Post_impl_ p1
#define _Ret2_impl_(p1,p2)           _Post_impl_ p1 _Post_impl_ p2
#define _Ret3_impl_(p1,p2,p3)        _Post_impl_ p1 _Post_impl_ p2 _Post_impl_ p3

#define _Deref_pre1_impl_(p1)        _Deref_pre_impl_ p1
#define _Deref_pre2_impl_(p1,p2)     _Deref_pre_impl_ p1 _Deref_pre_impl_ p2
#define _Deref_pre3_impl_(p1,p2,p3)  _Deref_pre_impl_ p1 _Deref_pre_impl_ p2 _Deref_pre_impl_ p3

#define _Deref_post1_impl_(p1)       _Deref_post_impl_ p1
#define _Deref_post2_impl_(p1,p2)    _Deref_post_impl_ p1 _Deref_post_impl_ p2
#define _Deref_post3_impl_(p1,p2,p3) _Deref_post_impl_ p1 _Deref_post_impl_ p2 _Deref_post_impl_ p3

#define _Deref_ret1_impl_(p1)        _Deref_post_impl_ p1
#define _Deref_ret2_impl_(p1,p2)     _Deref_post_impl_ p1 _Deref_post_impl_ p2
#define _Deref_ret3_impl_(p1,p2,p3)  _Deref_post_impl_ p1 _Deref_post_impl_ p2 _Deref_post_impl_ p3

#define _Deref2_pre1_impl_(p1)       _Deref_pre_impl_ _Notref_impl_ _Deref_impl_ p1
#define _Deref2_post1_impl_(p1)      _Deref_post_impl_ _Notref_impl_ _Deref_impl_ p1
#define _Deref2_ret1_impl_(p1)       _Deref_post_impl_ _Notref_impl_ _Deref_impl_ p1

#define __inner_typefix(ctype)             _SA_annotes1(SAL_typefix, ctype)
#define __inner_exceptthat                 _SA_annotes0(SAL_except)

#elif defined(_MSC_EXTENSIONS) && !defined( MIDL_PASS ) && !defined(__midl) && !defined(RC_INVOKED) && defined(_PFT_VER) && _MSC_VER >= 1400 /*IFSTRIP=IGN*/ // ][

// minimum attribute expansion for foreground build

#pragma push_macro( "SA" )
#pragma push_macro( "REPEATABLE" )

#ifdef __cplusplus // [
#define SA( id ) id
#define REPEATABLE [repeatable]
#else  // !__cplusplus // ][
#define SA( id ) SA_##id
#define REPEATABLE
#endif  // !__cplusplus // ]

REPEATABLE
[source_annotation_attribute( SA( Parameter ) )]
struct __P_impl
{
#ifdef __cplusplus // [
    __P_impl();
#endif // ]
   int __d_;
};
typedef struct __P_impl __P_impl;

REPEATABLE
[source_annotation_attribute( SA( ReturnValue ) )]
struct __R_impl
{
#ifdef __cplusplus // [
    __R_impl();
#endif // ]
   int __d_;
};
typedef struct __R_impl __R_impl;

[source_annotation_attribute( SA( Method ) )]
struct __M_
{
#ifdef __cplusplus // [
    __M_();
#endif // ]
   int __d_;
};
typedef struct __M_ __M_;

[source_annotation_attribute( SA( All ) )]
struct __A_
{
#ifdef __cplusplus // [
    __A_();
#endif // ]
   int __d_;
};
typedef struct __A_ __A_;

[source_annotation_attribute( SA( Field ) )]
struct __F_
{
#ifdef __cplusplus // [
    __F_();
#endif // ]
   int __d_;
};
typedef struct __F_ __F_;

#pragma pop_macro( "REPEATABLE" )
#pragma pop_macro( "SA" )


#define _SAL_nop_impl_

#define _At_impl_(target, annos)        [__A_(__d_=0)]
#define _At_buffer_impl_(target, iter, bound, annos)  [__A_(__d_=0)]
#define _When_impl_(expr, annos)        annos
#define _Group_impl_(annos)             annos
#define _GrouP_impl_(annos)             annos
#define _Use_decl_anno_impl_            [__M_(__d_=0)]

#define _Points_to_data_impl_           [__P_impl(__d_=0)]
#define _Literal_impl_                  [__P_impl(__d_=0)]
#define _Notliteral_impl_               [__P_impl(__d_=0)]

#define _Pre_valid_impl_                [__P_impl(__d_=0)]
#define _Post_valid_impl_               [__P_impl(__d_=0)]
#define _Ret_valid_impl_                [__R_impl(__d_=0)]

#define _Check_return_impl_             [__R_impl(__d_=0)]
#define _Must_inspect_impl_             [__R_impl(__d_=0)]

#define _Success_impl_(expr)            [__M_(__d_=0)]
#define _On_failure_impl_(expr)         [__M_(__d_=0)]
#define _Always_impl_(expr)             [__M_(__d_=0)]

#define _Printf_format_string_impl_     [__P_impl(__d_=0)]
#define _Scanf_format_string_impl_      [__P_impl(__d_=0)]
#define _Scanf_s_format_string_impl_    [__P_impl(__d_=0)]

#define _Raises_SEH_exception_impl_         [__M_(__d_=0)]
#define _Maybe_raises_SEH_exception_impl_   [__M_(__d_=0)]

#define _In_bound_impl_                 [__P_impl(__d_=0)]
#define _Out_bound_impl_                [__P_impl(__d_=0)]
#define _Ret_bound_impl_                [__R_impl(__d_=0)]
#define _Deref_in_bound_impl_           [__P_impl(__d_=0)]
#define _Deref_out_bound_impl_          [__P_impl(__d_=0)]
#define _Deref_ret_bound_impl_          [__R_impl(__d_=0)]

#define _Range_impl_(min,max)           [__P_impl(__d_=0)]
#define _In_range_impl_(min,max)        [__P_impl(__d_=0)]
#define _Out_range_impl_(min,max)       [__P_impl(__d_=0)]
#define _Ret_range_impl_(min,max)       [__R_impl(__d_=0)]
#define _Deref_in_range_impl_(min,max)  [__P_impl(__d_=0)]
#define _Deref_out_range_impl_(min,max) [__P_impl(__d_=0)]
#define _Deref_ret_range_impl_(min,max) [__R_impl(__d_=0)]

#define _Field_range_impl_(min,max)     [__F_(__d_=0)]

#define _Pre_satisfies_impl_(cond)      [__A_(__d_=0)]
#define _Post_satisfies_impl_(cond)     [__A_(__d_=0)]
#define _Satisfies_impl_(cond)          [__A_(__d_=0)]

#define _Null_impl_                     [__A_(__d_=0)]
#define _Notnull_impl_                  [__A_(__d_=0)]
#define _Maybenull_impl_                [__A_(__d_=0)]

#define _Valid_impl_                    [__A_(__d_=0)]
#define _Notvalid_impl_                 [__A_(__d_=0)]
#define _Maybevalid_impl_               [__A_(__d_=0)]

#define _Readable_bytes_impl_(size)     [__A_(__d_=0)]
#define _Readable_elements_impl_(size)  [__A_(__d_=0)]
#define _Writable_bytes_impl_(size)     [__A_(__d_=0)]
#define _Writable_elements_impl_(size)  [__A_(__d_=0)]

#define _Null_terminated_impl_          [__A_(__d_=0)]
#define _NullNull_terminated_impl_      [__A_(__d_=0)]

#define _Pre_impl_                      [__P_impl(__d_=0)]
#define _Pre1_impl_(p1)                 [__P_impl(__d_=0)]
#define _Pre2_impl_(p1,p2)              [__P_impl(__d_=0)]
#define _Pre3_impl_(p1,p2,p3)           [__P_impl(__d_=0)]

#define _Post_impl_                     [__P_impl(__d_=0)]
#define _Post1_impl_(p1)                [__P_impl(__d_=0)]
#define _Post2_impl_(p1,p2)             [__P_impl(__d_=0)]
#define _Post3_impl_(p1,p2,p3)          [__P_impl(__d_=0)]

#define _Ret1_impl_(p1)                 [__R_impl(__d_=0)]
#define _Ret2_impl_(p1,p2)              [__R_impl(__d_=0)]
#define _Ret3_impl_(p1,p2,p3)           [__R_impl(__d_=0)]

#define _Deref_pre1_impl_(p1)           [__P_impl(__d_=0)]
#define _Deref_pre2_impl_(p1,p2)        [__P_impl(__d_=0)]
#define _Deref_pre3_impl_(p1,p2,p3)     [__P_impl(__d_=0)]

#define _Deref_post1_impl_(p1)          [__P_impl(__d_=0)]
#define _Deref_post2_impl_(p1,p2)       [__P_impl(__d_=0)]
#define _Deref_post3_impl_(p1,p2,p3)    [__P_impl(__d_=0)]

#define _Deref_ret1_impl_(p1)           [__R_impl(__d_=0)]
#define _Deref_ret2_impl_(p1,p2)        [__R_impl(__d_=0)]
#define _Deref_ret3_impl_(p1,p2,p3)     [__R_impl(__d_=0)]

#define _Deref2_pre1_impl_(p1)          //[__P_impl(__d_=0)]
#define _Deref2_post1_impl_(p1)         //[__P_impl(__d_=0)]
#define _Deref2_ret1_impl_(p1)          //[__P_impl(__d_=0)]

#else // ][


#define _SAL_nop_impl_ X

#define _At_impl_(target, annos)
#define _When_impl_(expr, annos)
#define _Group_impl_(annos)
#define _GrouP_impl_(annos)
#define _At_buffer_impl_(target, iter, bound, annos)
#define _Use_decl_anno_impl_
#define _Points_to_data_impl_
#define _Literal_impl_
#define _Notliteral_impl_
#define _Notref_impl_

#define _Pre_valid_impl_
#define _Post_valid_impl_
#define _Ret_valid_impl_

#define _Check_return_impl_
#define _Must_inspect_impl_

#define _Success_impl_(expr)
#define _On_failure_impl_(annos)
#define _Always_impl_(annos)

#define _Printf_format_string_impl_
#define _Scanf_format_string_impl_
#define _Scanf_s_format_string_impl_

#define _In_bound_impl_
#define _Out_bound_impl_
#define _Ret_bound_impl_
#define _Deref_in_bound_impl_
#define _Deref_out_bound_impl_
#define _Deref_ret_bound_impl_

#define _Range_impl_(min,max)
#define _In_range_impl_(min,max)
#define _Out_range_impl_(min,max)
#define _Ret_range_impl_(min,max)
#define _Deref_in_range_impl_(min,max)
#define _Deref_out_range_impl_(min,max)
#define _Deref_ret_range_impl_(min,max)

#define _Satisfies_impl_(expr)
#define _Pre_satisfies_impl_(expr)
#define _Post_satisfies_impl_(expr)

#define _Null_impl_
#define _Notnull_impl_
#define _Maybenull_impl_

#define _Valid_impl_
#define _Notvalid_impl_
#define _Maybevalid_impl_

#define _Field_range_impl_(min,max)

#define _Pre_impl_
#define _Pre1_impl_(p1)
#define _Pre2_impl_(p1,p2)
#define _Pre3_impl_(p1,p2,p3)

#define _Post_impl_
#define _Post1_impl_(p1)
#define _Post2_impl_(p1,p2)
#define _Post3_impl_(p1,p2,p3)

#define _Ret1_impl_(p1)
#define _Ret2_impl_(p1,p2)
#define _Ret3_impl_(p1,p2,p3)

#define _Deref_pre1_impl_(p1)
#define _Deref_pre2_impl_(p1,p2)
#define _Deref_pre3_impl_(p1,p2,p3)

#define _Deref_post1_impl_(p1)
#define _Deref_post2_impl_(p1,p2)
#define _Deref_post3_impl_(p1,p2,p3)

#define _Deref_ret1_impl_(p1)
#define _Deref_ret2_impl_(p1,p2)
#define _Deref_ret3_impl_(p1,p2,p3)

#define _Deref2_pre1_impl_(p1)
#define _Deref2_post1_impl_(p1)
#define _Deref2_ret1_impl_(p1)

#define _Readable_bytes_impl_(size)
#define _Readable_elements_impl_(size)
#define _Writable_bytes_impl_(size)
#define _Writable_elements_impl_(size)

#define _Null_terminated_impl_
#define _NullNull_terminated_impl_

// Obsolete -- may be needed for transition to attributes.
#define __inner_typefix(ctype)
#define __inner_exceptthat

#endif // ]

// This section contains the deprecated annotations

/*
 -------------------------------------------------------------------------------
 Introduction

 sal.h provides a set of annotations to describe how a function uses its
 parameters - the assumptions it makes about them, and the guarantees it makes
 upon finishing.

 Annotations may be placed before either a function parameter's type or its return
 type, and describe the function's behavior regarding the parameter or return value.
 There are two classes of annotations: buffer annotations and advanced annotations.
 Buffer annotations describe how functions use their pointer parameters, and
 advanced annotations either describe complex/unusual buffer behavior, or provide
 additional information about a parameter that is not otherwise expressible.

 -------------------------------------------------------------------------------
 Buffer Annotations

 The most important annotations in sal.h provide a consistent way to annotate
 buffer parameters or return values for a function. Each of these annotations describes
 a single buffer (which could be a string, a fixed-length or variable-length array,
 or just a pointer) that the function interacts with: where it is, how large it is,
 how much is initialized, and what the function does with it.

 The appropriate macro for a given buffer can be constructed using the table below.
 Just pick the appropriate values from each category, and combine them together
 with a leading underscore. Some combinations of values do not make sense as buffer
 annotations. Only meaningful annotations can be added to your code; for a list of
 these, see the buffer annotation definitions section.

 Only a single buffer annotation should be used for each parameter.

 |------------|------------|---------|--------|----------|----------|---------------|
 |   Level    |   Usage    |  Size   | Output | NullTerm | Optional |  Parameters   |
 |------------|------------|---------|--------|----------|----------|---------------|
 | <>         | <>         | <>      | <>     | _z       | <>       | <>            |
 | _deref     | _in        | _ecount | _full  | _nz      | _opt     | (size)        |
 | _deref_opt | _out       | _bcount | _part  |          |          | (size,length) |
 |            | _inout     |         |        |          |          |               |
 |            |            |         |        |          |          |               |
 |------------|------------|---------|--------|----------|----------|---------------|

 Level: Describes the buffer pointer's level of indirection from the parameter or
          return value 'p'.

 <>         : p is the buffer pointer.
 _deref     : *p is the buffer pointer. p must not be NULL.
 _deref_opt : *p may be the buffer pointer. p may be NULL, in which case the rest of
                the annotation is ignored.

 Usage: Describes how the function uses the buffer.

 <>     : The buffer is not accessed. If used on the return value or with _deref, the
            function will provide the buffer, and it will be uninitialized at exit.
            Otherwise, the caller must provide the buffer. This should only be used
            for alloc and free functions.
 _in    : The function will only read from the buffer. The caller must provide the
            buffer and initialize it. Cannot be used with _deref.
 _out   : The function will only write to the buffer. If used on the return value or
            with _deref, the function will provide the buffer and initialize it.
            Otherwise, the caller must provide the buffer, and the function will
            initialize it.
 _inout : The function may freely read from and write to the buffer. The caller must
            provide the buffer and initialize it. If used with _deref, the buffer may
            be reallocated by the function.

 Size: Describes the total size of the buffer. This may be less than the space actually
         allocated for the buffer, in which case it describes the accessible amount.

 <>      : No buffer size is given. If the type specifies the buffer size (such as
             with LPSTR and LPWSTR), that amount is used. Otherwise, the buffer is one
             element long. Must be used with _in, _out, or _inout.
 _ecount : The buffer size is an explicit element count.
 _bcount : The buffer size is an explicit byte count.

 Output: Describes how much of the buffer will be initialized by the function. For
           _inout buffers, this also describes how much is initialized at entry. Omit this
           category for _in buffers; they must be fully initialized by the caller.

 <>    : The type specifies how much is initialized. For instance, a function initializing
           an LPWSTR must NULL-terminate the string.
 _full : The function initializes the entire buffer.
 _part : The function initializes part of the buffer, and explicitly indicates how much.

 NullTerm: States if the present of a '\0' marks the end of valid elements in the buffer.
 _z    : A '\0' indicated the end of the buffer
 _nz     : The buffer may not be null terminated and a '\0' does not indicate the end of the
          buffer.
 Optional: Describes if the buffer itself is optional.

 <>   : The pointer to the buffer must not be NULL.
 _opt : The pointer to the buffer might be NULL. It will be checked before being dereferenced.

 Parameters: Gives explicit counts for the size and length of the buffer.

 <>            : There is no explicit count. Use when neither _ecount nor _bcount is used.
 (size)        : Only the buffer's total size is given. Use with _ecount or _bcount but not _part.
 (size,length) : The buffer's total size and initialized length are given. Use with _ecount_part
                   and _bcount_part.

 -------------------------------------------------------------------------------
 Buffer Annotation Examples

 LWSTDAPI_(BOOL) StrToIntExA(
     __in LPCSTR pszString,
     DWORD dwFlags,
     __out int *piRet                     -- A pointer whose dereference will be filled in.
 );

 void MyPaintingFunction(
     __in HWND hwndControl,               -- An initialized read-only parameter.
     __in_opt HDC hdcOptional,            -- An initialized read-only parameter that might be NULL.
     __inout IPropertyStore *ppsStore     -- An initialized parameter that may be freely used
                                          --   and modified.
 );

 LWSTDAPI_(BOOL) PathCompactPathExA(
     __out_ecount(cchMax) LPSTR pszOut,   -- A string buffer with cch elements that will
                                          --   be NULL terminated on exit.
     __in LPCSTR pszSrc,
     UINT cchMax,
     DWORD dwFlags
 );

 HRESULT SHLocalAllocBytes(
     size_t cb,
     __deref_bcount(cb) T **ppv           -- A pointer whose dereference will be set to an
                                          --   uninitialized buffer with cb bytes.
 );

 __inout_bcount_full(cb) : A buffer with cb elements that is fully initialized at
     entry and exit, and may be written to by this function.

 __out_ecount_part(count, *countOut) : A buffer with count elements that will be
     partially initialized by this function. The function indicates how much it
     initialized by setting *countOut.

 -------------------------------------------------------------------------------
 Advanced Annotations

 Advanced annotations describe behavior that is not expressible with the regular
 buffer macros. These may be used either to annotate buffer parameters that involve
 complex or conditional behavior, or to enrich existing annotations with additional
 information.

 __success(expr) f :
     <expr> indicates whether function f succeeded or not. If <expr> is true at exit,
     all the function's guarantees (as given by other annotations) must hold. If <expr>
     is false at exit, the caller should not expect any of the function's guarantees
     to hold. If not used, the function must always satisfy its guarantees. Added
     automatically to functions that indicate success in standard ways, such as by
     returning an HRESULT.

 __nullterminated p :
     Pointer p is a buffer that may be read or written up to and including the first
     NULL character or pointer. May be used on typedefs, which marks valid (properly
     initialized) instances of that type as being NULL-terminated.

 __nullnullterminated p :
     Pointer p is a buffer that may be read or written up to and including the first
     sequence of two NULL characters or pointers. May be used on typedefs, which marks
     valid instances of that type as being double-NULL terminated.

 __reserved v :
     Value v must be 0/NULL, reserved for future use.

 __checkReturn v :
     Return value v must not be ignored by callers of this function.

 __typefix(ctype) v :
     Value v should be treated as an instance of ctype, rather than its declared type.

 __override f :
     Specify C#-style 'override' behaviour for overriding virtual methods.

 __callback f :
     Function f can be used as a function pointer.

 __format_string p :
     Pointer p is a string that contains % markers in the style of printf.

 __blocksOn(resource) f :
     Function f blocks on the resource 'resource'.

 FALLTHROUGH :
     Annotates switch statement labels where fall-through is desired, to distinguish
     from forgotten break statements.

 -------------------------------------------------------------------------------
 Advanced Annotation Examples

 __success(return != FALSE) LWSTDAPI_(BOOL)
 PathCanonicalizeA(__out_ecount(MAX_PATH) LPSTR pszBuf, LPCSTR pszPath) :
    pszBuf is only guaranteed to be NULL-terminated when TRUE is returned.

 typedef __nullterminated WCHAR* LPWSTR : Initialized LPWSTRs are NULL-terminated strings.

 __out_ecount(cch) __typefix(LPWSTR) void *psz : psz is a buffer parameter which will be
     a NULL-terminated WCHAR string at exit, and which initially contains cch WCHARs.

 -------------------------------------------------------------------------------
*/

#define __specstrings

#ifdef  __cplusplus // [
#ifndef __nothrow // [
# define __nothrow NOTHROW_DECL
#endif // ]
extern "C" {
#else // ][
#ifndef __nothrow // [
# define __nothrow
#endif // ]
#endif  /* #ifdef __cplusplus */ // ]


/*
 -------------------------------------------------------------------------------
 Helper Macro Definitions

 These express behavior common to many of the high-level annotations.
 DO NOT USE THESE IN YOUR CODE.
 -------------------------------------------------------------------------------
*/

/*
    The helper annotations are only understood by the compiler version used by
    various defect detection tools. When the regular compiler is running, they
    are defined into nothing, and do not affect the compiled code.
*/

#if !defined(__midl) && defined(_PREFAST_) // [

    /*
     In the primitive "SAL_*" annotations "SAL" stands for Standard
     Annotation Language.  These "SAL_*" annotations are the
     primitives the compiler understands and high-level MACROs
     will decompose into these primivates.
    */

    #define _SA_SPECSTRIZE( x ) #x

    /*
     __null p
     __notnull p
     __maybenull p

     Annotates a pointer p. States that pointer p is null. Commonly used
     in the negated form __notnull or the possibly null form __maybenull.
    */

#ifndef PAL_STDCPP_COMPAT
    #define __null                  _Null_impl_
    #define __notnull               _Notnull_impl_
    #define __maybenull             _Maybenull_impl_
#endif // !PAL_STDCPP_COMPAT

    /*
     __readonly l
     __notreadonly l
     __maybereadonly l

     Annotates a location l. States that location l is not modified after
     this point.  If the annotation is placed on the precondition state of
     a function, the restriction only applies until the postcondition state
     of the function.  __maybereadonly states that the annotated location
     may be modified, whereas __notreadonly states that a location must be
     modified.
    */

    #define __readonly              _Pre1_impl_(__readaccess_impl)
    #define __notreadonly           _Pre1_impl_(__allaccess_impl)
    #define __maybereadonly         _Pre1_impl_(__readaccess_impl)

    /*
     __valid v
     __notvalid v
     __maybevalid v

     Annotates any value v. States that the value satisfies all properties of
     valid values of its type. For example, for a string buffer, valid means
     that the buffer pointer is either NULL or points to a NULL-terminated string.
    */

    #define __valid                 _Valid_impl_
    #define __notvalid              _Notvalid_impl_
    #define __maybevalid            _Maybevalid_impl_

    /*
     __readableTo(extent) p

     Annotates a buffer pointer p.  If the buffer can be read, extent describes
     how much of the buffer is readable. For a reader of the buffer, this is
     an explicit permission to read up to that amount, rather than a restriction to
     read only up to it.
    */

    #define __readableTo(extent)    _SA_annotes1(SAL_readableTo, extent)

    /*

     __elem_readableTo(size)

     Annotates a buffer pointer p as being readable to size elements.
    */

    #define __elem_readableTo(size)   _SA_annotes1(SAL_readableTo, elementCount( size ))

    /*
     __byte_readableTo(size)

     Annotates a buffer pointer p as being readable to size bytes.
    */
    #define __byte_readableTo(size)   _SA_annotes1(SAL_readableTo, byteCount(size))

    /*
     __writableTo(extent) p

     Annotates a buffer pointer p. If the buffer can be modified, extent
     describes how much of the buffer is writable (usually the allocation
     size). For a writer of the buffer, this is an explicit permission to
     write up to that amount, rather than a restriction to write only up to it.
    */
    #define __writableTo(size)   _SA_annotes1(SAL_writableTo, size)

    /*
     __elem_writableTo(size)

     Annotates a buffer pointer p as being writable to size elements.
    */
    #define __elem_writableTo(size)   _SA_annotes1(SAL_writableTo, elementCount( size ))

    /*
     __byte_writableTo(size)

     Annotates a buffer pointer p as being writable to size bytes.
    */
    #define __byte_writableTo(size)   _SA_annotes1(SAL_writableTo, byteCount( size))

    /*
     __deref p

     Annotates a pointer p. The next annotation applies one dereference down
     in the type. If readableTo(p, size) then the next annotation applies to
     all elements *(p+i) for which i satisfies the size. If p is a pointer
     to a struct, the next annotation applies to all fields of the struct.
    */
    #define __deref                 _Deref_impl_

    /*
     __pre __next_annotation

     The next annotation applies in the precondition state
    */
    #define __pre                   _Pre_impl_

    /*
     __post __next_annotation

     The next annotation applies in the postcondition state
    */
    #define __post                  _Post_impl_

    /*
     __precond(<expr>)

     When <expr> is true, the next annotation applies in the precondition state
     (currently not enabled)
    */
    #define __precond(expr)         __pre

    /*
     __postcond(<expr>)

     When <expr> is true, the next annotation applies in the postcondition state
     (currently not enabled)
    */
    #define __postcond(expr)        __post

    /*
     __exceptthat

     Given a set of annotations Q containing __exceptthat maybeP, the effect of
     the except clause is to erase any P or notP annotations (explicit or
     implied) within Q at the same level of dereferencing that the except
     clause appears, and to replace it with maybeP.

      Example 1: __valid __pre_except_maybenull on a pointer p means that the
                 pointer may be null, and is otherwise valid, thus overriding
                 the implicit notnull annotation implied by __valid on
                 pointers.

      Example 2: __valid __deref __pre_except_maybenull on an int **p means
                 that p is not null (implied by valid), but the elements
                 pointed to by p could be null, and are otherwise valid.
    */
    #define __exceptthat                __inner_exceptthat

    /*
     _refparam

     Added to all out parameter macros to indicate that they are all reference
     parameters.
    */
    #define __refparam                  _Notref_ __deref __notreadonly

    /*
     __inner_*

     Helper macros that directly correspond to certain high-level annotations.

    */

    /*
     Macros to classify the entrypoints and indicate their category.

     Pre-defined control point categories include: RPC, LPC, DeviceDriver, UserToKernel, ISAPI, COM.

    */
    #define __inner_control_entrypoint(category) _SA_annotes2(SAL_entrypoint, controlEntry, category)


    /*
     Pre-defined data entry point categories include: Registry, File, Network.
    */
    #define __inner_data_entrypoint(category)    _SA_annotes2(SAL_entrypoint, dataEntry, category)

    #define __inner_override                    _SA_annotes0(__override)
    #define __inner_callback                    _SA_annotes0(__callback)
    #define __inner_blocksOn(resource)          _SA_annotes1(SAL_blocksOn, resource)

    #define __post_except_maybenull     __post __inner_exceptthat _Maybenull_impl_
    #define __pre_except_maybenull      __pre  __inner_exceptthat _Maybenull_impl_

    #define __post_deref_except_maybenull       __post __deref __inner_exceptthat _Maybenull_impl_
    #define __pre_deref_except_maybenull    __pre  __deref __inner_exceptthat _Maybenull_impl_

    #define __inexpressible_readableTo(size)  _Readable_elements_impl_(_Inexpressible_(size))
    #define __inexpressible_writableTo(size)  _Writable_elements_impl_(_Inexpressible_(size))


#else // ][
#ifndef PAL_STDCPP_COMPAT
    #define __null
    #define __notnull
    #define __deref
#endif // !PAL_STDCPP_COMPAT
    #define __maybenull
    #define __readonly
    #define __notreadonly
    #define __maybereadonly
    #define __valid
    #define __notvalid
    #define __maybevalid
    #define __readableTo(extent)
    #define __elem_readableTo(size)
    #define __byte_readableTo(size)
    #define __writableTo(size)
    #define __elem_writableTo(size)
    #define __byte_writableTo(size)
    #define __pre
    #define __post
    #define __precond(expr)
    #define __postcond(expr)
    #define __exceptthat
    #define __inner_override
    #define __inner_callback
    #define __inner_blocksOn(resource)
    #define __refparam
    #define __inner_control_entrypoint(category)
    #define __inner_data_entrypoint(category)

    #define __post_except_maybenull
    #define __pre_except_maybenull
    #define __post_deref_except_maybenull
    #define __pre_deref_except_maybenull

    #define __inexpressible_readableTo(size)
    #define __inexpressible_writableTo(size)

#endif /* #if !defined(__midl) && defined(_PREFAST_) */ // ]

/*
-------------------------------------------------------------------------------
Buffer Annotation Definitions

Any of these may be used to directly annotate functions, but only one should
be used for each parameter. To determine which annotation to use for a given
buffer, use the table in the buffer annotations section.
-------------------------------------------------------------------------------
*/

#define __ecount(size)                                           _SAL1_Source_(__ecount, (size), __notnull __elem_writableTo(size))
#define __bcount(size)                                           _SAL1_Source_(__bcount, (size), __notnull __byte_writableTo(size))
#define __in_ecount(size)                                        _SAL1_Source_(__in_ecount, (size), _In_reads_(size))
#define __in_bcount(size)                                        _SAL1_Source_(__in_bcount, (size), _In_reads_bytes_(size))
#define __in_z                                                   _SAL1_Source_(__in_z, (), _In_z_)
#define __in_ecount_z(size)                                      _SAL1_Source_(__in_ecount_z, (size), _In_reads_z_(size))
#define __in_bcount_z(size)                                      _SAL1_Source_(__in_bcount_z, (size), __in_bcount(size) __pre __nullterminated)
#define __in_nz                                                  _SAL1_Source_(__in_nz, (), __in)
#define __in_ecount_nz(size)                                     _SAL1_Source_(__in_ecount_nz, (size), __in_ecount(size))
#define __in_bcount_nz(size)                                     _SAL1_Source_(__in_bcount_nz, (size), __in_bcount(size))
#define __out_ecount(size)                                       _SAL1_Source_(__out_ecount, (size), _Out_writes_(size))
#define __out_bcount(size)                                       _SAL1_Source_(__out_bcount, (size), _Out_writes_bytes_(size))
#define __out_ecount_part(size,length)                           _SAL1_Source_(__out_ecount_part, (size,length), _Out_writes_to_(size,length))
#define __out_bcount_part(size,length)                           _SAL1_Source_(__out_bcount_part, (size,length), _Out_writes_bytes_to_(size,length))
#define __out_ecount_full(size)                                  _SAL1_Source_(__out_ecount_full, (size), _Out_writes_all_(size))
#define __out_bcount_full(size)                                  _SAL1_Source_(__out_bcount_full, (size), _Out_writes_bytes_all_(size))
#define __out_z                                                  _SAL1_Source_(__out_z, (), __post __valid __refparam __post __nullterminated)
#define __out_z_opt                                              _SAL1_Source_(__out_z_opt, (), __post __valid __refparam __post __nullterminated __pre_except_maybenull)
#define __out_ecount_z(size)                                     _SAL1_Source_(__out_ecount_z, (size), __ecount(size) __post __valid __refparam __post __nullterminated)
#define __out_bcount_z(size)                                     _SAL1_Source_(__out_bcount_z, (size), __bcount(size) __post __valid __refparam __post __nullterminated)
#define __out_ecount_part_z(size,length)                         _SAL1_Source_(__out_ecount_part_z, (size,length), __out_ecount_part(size,length) __post __nullterminated)
#define __out_bcount_part_z(size,length)                         _SAL1_Source_(__out_bcount_part_z, (size,length), __out_bcount_part(size,length) __post __nullterminated)
#define __out_ecount_full_z(size)                                _SAL1_Source_(__out_ecount_full_z, (size), __out_ecount_full(size) __post __nullterminated)
#define __out_bcount_full_z(size)                                _SAL1_Source_(__out_bcount_full_z, (size), __out_bcount_full(size) __post __nullterminated)
#define __out_nz                                                 _SAL1_Source_(__out_nz, (), __post __valid __refparam)
#define __out_nz_opt                                             _SAL1_Source_(__out_nz_opt, (), __post __valid __refparam __post_except_maybenull_)
#define __out_ecount_nz(size)                                    _SAL1_Source_(__out_ecount_nz, (size), __ecount(size) __post __valid __refparam)
#define __out_bcount_nz(size)                                    _SAL1_Source_(__out_bcount_nz, (size), __bcount(size) __post __valid __refparam)
#define __inout                                                  _SAL1_Source_(__inout, (), _Inout_)
#define __inout_ecount(size)                                     _SAL1_Source_(__inout_ecount, (size), _Inout_updates_(size))
#define __inout_bcount(size)                                     _SAL1_Source_(__inout_bcount, (size), _Inout_updates_bytes_(size))
#define __inout_ecount_part(size,length)                         _SAL1_Source_(__inout_ecount_part, (size,length), _Inout_updates_to_(size,length))
#define __inout_bcount_part(size,length)                         _SAL1_Source_(__inout_bcount_part, (size,length), _Inout_updates_bytes_to_(size,length))
#define __inout_ecount_full(size)                                _SAL1_Source_(__inout_ecount_full, (size), _Inout_updates_all_(size))
#define __inout_bcount_full(size)                                _SAL1_Source_(__inout_bcount_full, (size), _Inout_updates_bytes_all_(size))
#define __inout_z                                                _SAL1_Source_(__inout_z, (), _Inout_z_)
#define __inout_ecount_z(size)                                   _SAL1_Source_(__inout_ecount_z, (size), _Inout_updates_z_(size))
#define __inout_bcount_z(size)                                   _SAL1_Source_(__inout_bcount_z, (size), __inout_bcount(size) __pre __nullterminated __post __nullterminated)
#define __inout_nz                                               _SAL1_Source_(__inout_nz, (), __inout)
#define __inout_ecount_nz(size)                                  _SAL1_Source_(__inout_ecount_nz, (size), __inout_ecount(size))
#define __inout_bcount_nz(size)                                  _SAL1_Source_(__inout_bcount_nz, (size), __inout_bcount(size))
#define __ecount_opt(size)                                       _SAL1_Source_(__ecount_opt, (size), __ecount(size)                              __pre_except_maybenull)
#define __bcount_opt(size)                                       _SAL1_Source_(__bcount_opt, (size), __bcount(size)                              __pre_except_maybenull)
#define __in_opt                                                 _SAL1_Source_(__in_opt, (), _In_opt_)
#define __in_ecount_opt(size)                                    _SAL1_Source_(__in_ecount_opt, (size), _In_reads_opt_(size))
#define __in_bcount_opt(size)                                    _SAL1_Source_(__in_bcount_opt, (size), _In_reads_bytes_opt_(size))
#define __in_z_opt                                               _SAL1_Source_(__in_z_opt, (), _In_opt_z_)
#define __in_ecount_z_opt(size)                                  _SAL1_Source_(__in_ecount_z_opt, (size), __in_ecount_opt(size) __pre __nullterminated)
#define __in_bcount_z_opt(size)                                  _SAL1_Source_(__in_bcount_z_opt, (size), __in_bcount_opt(size) __pre __nullterminated)
#define __in_nz_opt                                              _SAL1_Source_(__in_nz_opt, (), __in_opt)
#define __in_ecount_nz_opt(size)                                 _SAL1_Source_(__in_ecount_nz_opt, (size), __in_ecount_opt(size))
#define __in_bcount_nz_opt(size)                                 _SAL1_Source_(__in_bcount_nz_opt, (size), __in_bcount_opt(size))
#define __out_opt                                                _SAL1_Source_(__out_opt, (), _Out_opt_)
#define __out_ecount_opt(size)                                   _SAL1_Source_(__out_ecount_opt, (size), _Out_writes_opt_(size))
#define __out_bcount_opt(size)                                   _SAL1_Source_(__out_bcount_opt, (size), _Out_writes_bytes_opt_(size))
#define __out_ecount_part_opt(size,length)                       _SAL1_Source_(__out_ecount_part_opt, (size,length), __out_ecount_part(size,length)              __pre_except_maybenull)
#define __out_bcount_part_opt(size,length)                       _SAL1_Source_(__out_bcount_part_opt, (size,length), __out_bcount_part(size,length)              __pre_except_maybenull)
#define __out_ecount_full_opt(size)                              _SAL1_Source_(__out_ecount_full_opt, (size), __out_ecount_full(size)                     __pre_except_maybenull)
#define __out_bcount_full_opt(size)                              _SAL1_Source_(__out_bcount_full_opt, (size), __out_bcount_full(size)                     __pre_except_maybenull)
#define __out_ecount_z_opt(size)                                 _SAL1_Source_(__out_ecount_z_opt, (size), __out_ecount_opt(size) __post __nullterminated)
#define __out_bcount_z_opt(size)                                 _SAL1_Source_(__out_bcount_z_opt, (size), __out_bcount_opt(size) __post __nullterminated)
#define __out_ecount_part_z_opt(size,length)                     _SAL1_Source_(__out_ecount_part_z_opt, (size,length), __out_ecount_part_opt(size,length) __post __nullterminated)
#define __out_bcount_part_z_opt(size,length)                     _SAL1_Source_(__out_bcount_part_z_opt, (size,length), __out_bcount_part_opt(size,length) __post __nullterminated)
#define __out_ecount_full_z_opt(size)                            _SAL1_Source_(__out_ecount_full_z_opt, (size), __out_ecount_full_opt(size) __post __nullterminated)
#define __out_bcount_full_z_opt(size)                            _SAL1_Source_(__out_bcount_full_z_opt, (size), __out_bcount_full_opt(size) __post __nullterminated)
#define __out_ecount_nz_opt(size)                                _SAL1_Source_(__out_ecount_nz_opt, (size), __out_ecount_opt(size) __post __nullterminated)
#define __out_bcount_nz_opt(size)                                _SAL1_Source_(__out_bcount_nz_opt, (size), __out_bcount_opt(size) __post __nullterminated)
#define __inout_opt                                              _SAL1_Source_(__inout_opt, (), _Inout_opt_)
#define __inout_ecount_opt(size)                                 _SAL1_Source_(__inout_ecount_opt, (size), __inout_ecount(size)                        __pre_except_maybenull)
#define __inout_bcount_opt(size)                                 _SAL1_Source_(__inout_bcount_opt, (size), __inout_bcount(size)                        __pre_except_maybenull)
#define __inout_ecount_part_opt(size,length)                     _SAL1_Source_(__inout_ecount_part_opt, (size,length), __inout_ecount_part(size,length)            __pre_except_maybenull)
#define __inout_bcount_part_opt(size,length)                     _SAL1_Source_(__inout_bcount_part_opt, (size,length), __inout_bcount_part(size,length)            __pre_except_maybenull)
#define __inout_ecount_full_opt(size)                            _SAL1_Source_(__inout_ecount_full_opt, (size), __inout_ecount_full(size)                   __pre_except_maybenull)
#define __inout_bcount_full_opt(size)                            _SAL1_Source_(__inout_bcount_full_opt, (size), __inout_bcount_full(size)                   __pre_except_maybenull)
#define __inout_z_opt                                            _SAL1_Source_(__inout_z_opt, (), __inout_opt __pre __nullterminated __post __nullterminated)
#define __inout_ecount_z_opt(size)                               _SAL1_Source_(__inout_ecount_z_opt, (size), __inout_ecount_opt(size) __pre __nullterminated __post __nullterminated)
#define __inout_ecount_z_opt(size)                               _SAL1_Source_(__inout_ecount_z_opt, (size), __inout_ecount_opt(size) __pre __nullterminated __post __nullterminated)
#define __inout_bcount_z_opt(size)                               _SAL1_Source_(__inout_bcount_z_opt, (size), __inout_bcount_opt(size))
#define __inout_nz_opt                                           _SAL1_Source_(__inout_nz_opt, (), __inout_opt)
#define __inout_ecount_nz_opt(size)                              _SAL1_Source_(__inout_ecount_nz_opt, (size), __inout_ecount_opt(size))
#define __inout_bcount_nz_opt(size)                              _SAL1_Source_(__inout_bcount_nz_opt, (size), __inout_bcount_opt(size))
#define __deref_ecount(size)                                     _SAL1_Source_(__deref_ecount, (size), _Notref_ __ecount(1) __post _Notref_ __elem_readableTo(1) __post _Notref_ __deref _Notref_ __notnull __post __deref __elem_writableTo(size))
#define __deref_bcount(size)                                     _SAL1_Source_(__deref_bcount, (size), _Notref_ __ecount(1) __post _Notref_ __elem_readableTo(1) __post _Notref_ __deref _Notref_ __notnull __post __deref __byte_writableTo(size))
#define __deref_out                                              _SAL1_Source_(__deref_out, (), _Outptr_)
#define __deref_out_ecount(size)                                 _SAL1_Source_(__deref_out_ecount, (size), _Outptr_result_buffer_(size))
#define __deref_out_bcount(size)                                 _SAL1_Source_(__deref_out_bcount, (size), _Outptr_result_bytebuffer_(size))
#define __deref_out_ecount_part(size,length)                     _SAL1_Source_(__deref_out_ecount_part, (size,length), _Outptr_result_buffer_to_(size,length))
#define __deref_out_bcount_part(size,length)                     _SAL1_Source_(__deref_out_bcount_part, (size,length), _Outptr_result_bytebuffer_to_(size,length))
#define __deref_out_ecount_full(size)                            _SAL1_Source_(__deref_out_ecount_full, (size), __deref_out_ecount_part(size,size))
#define __deref_out_bcount_full(size)                            _SAL1_Source_(__deref_out_bcount_full, (size), __deref_out_bcount_part(size,size))
#define __deref_out_z                                            _SAL1_Source_(__deref_out_z, (), _Outptr_result_z_)
#define __deref_out_ecount_z(size)                               _SAL1_Source_(__deref_out_ecount_z, (size), __deref_out_ecount(size) __post __deref __nullterminated)
#define __deref_out_bcount_z(size)                               _SAL1_Source_(__deref_out_bcount_z, (size), __deref_out_bcount(size) __post __deref __nullterminated)
#define __deref_out_nz                                           _SAL1_Source_(__deref_out_nz, (), __deref_out)
#define __deref_out_ecount_nz(size)                              _SAL1_Source_(__deref_out_ecount_nz, (size), __deref_out_ecount(size))
#define __deref_out_bcount_nz(size)                              _SAL1_Source_(__deref_out_bcount_nz, (size), __deref_out_ecount(size))
#define __deref_inout                                            _SAL1_Source_(__deref_inout, (), _Notref_ __notnull _Notref_ __elem_readableTo(1) __pre __deref __valid __post _Notref_ __deref __valid __refparam)
#define __deref_inout_z                                          _SAL1_Source_(__deref_inout_z, (), __deref_inout __pre __deref __nullterminated __post _Notref_ __deref __nullterminated)
#define __deref_inout_ecount(size)                               _SAL1_Source_(__deref_inout_ecount, (size), __deref_inout __pre __deref __elem_writableTo(size) __post _Notref_ __deref __elem_writableTo(size))
#define __deref_inout_bcount(size)                               _SAL1_Source_(__deref_inout_bcount, (size), __deref_inout __pre __deref __byte_writableTo(size) __post _Notref_ __deref __byte_writableTo(size))
#define __deref_inout_ecount_part(size,length)                   _SAL1_Source_(__deref_inout_ecount_part, (size,length), __deref_inout_ecount(size) __pre __deref __elem_readableTo(length) __post __deref __elem_readableTo(length))
#define __deref_inout_bcount_part(size,length)                   _SAL1_Source_(__deref_inout_bcount_part, (size,length), __deref_inout_bcount(size) __pre __deref __byte_readableTo(length) __post __deref __byte_readableTo(length))
#define __deref_inout_ecount_full(size)                          _SAL1_Source_(__deref_inout_ecount_full, (size), __deref_inout_ecount_part(size,size))
#define __deref_inout_bcount_full(size)                          _SAL1_Source_(__deref_inout_bcount_full, (size), __deref_inout_bcount_part(size,size))
#define __deref_inout_ecount_z(size)                             _SAL1_Source_(__deref_inout_ecount_z, (size), __deref_inout_ecount(size) __pre __deref __nullterminated __post __deref __nullterminated)
#define __deref_inout_bcount_z(size)                             _SAL1_Source_(__deref_inout_bcount_z, (size), __deref_inout_bcount(size) __pre __deref __nullterminated __post __deref __nullterminated)
#define __deref_inout_nz                                         _SAL1_Source_(__deref_inout_nz, (), __deref_inout)
#define __deref_inout_ecount_nz(size)                            _SAL1_Source_(__deref_inout_ecount_nz, (size), __deref_inout_ecount(size))
#define __deref_inout_bcount_nz(size)                            _SAL1_Source_(__deref_inout_bcount_nz, (size), __deref_inout_ecount(size))
#define __deref_ecount_opt(size)                                 _SAL1_Source_(__deref_ecount_opt, (size), __deref_ecount(size)                        __post_deref_except_maybenull)
#define __deref_bcount_opt(size)                                 _SAL1_Source_(__deref_bcount_opt, (size), __deref_bcount(size)                        __post_deref_except_maybenull)
#define __deref_out_opt                                          _SAL1_Source_(__deref_out_opt, (), __deref_out                                 __post_deref_except_maybenull)
#define __deref_out_ecount_opt(size)                             _SAL1_Source_(__deref_out_ecount_opt, (size), __deref_out_ecount(size)                    __post_deref_except_maybenull)
#define __deref_out_bcount_opt(size)                             _SAL1_Source_(__deref_out_bcount_opt, (size), __deref_out_bcount(size)                    __post_deref_except_maybenull)
#define __deref_out_ecount_part_opt(size,length)                 _SAL1_Source_(__deref_out_ecount_part_opt, (size,length), __deref_out_ecount_part(size,length)        __post_deref_except_maybenull)
#define __deref_out_bcount_part_opt(size,length)                 _SAL1_Source_(__deref_out_bcount_part_opt, (size,length), __deref_out_bcount_part(size,length)        __post_deref_except_maybenull)
#define __deref_out_ecount_full_opt(size)                        _SAL1_Source_(__deref_out_ecount_full_opt, (size), __deref_out_ecount_full(size)               __post_deref_except_maybenull)
#define __deref_out_bcount_full_opt(size)                        _SAL1_Source_(__deref_out_bcount_full_opt, (size), __deref_out_bcount_full(size)               __post_deref_except_maybenull)
#define __deref_out_z_opt                                        _SAL1_Source_(__deref_out_z_opt, (), _Outptr_result_maybenull_z_)
#define __deref_out_ecount_z_opt(size)                           _SAL1_Source_(__deref_out_ecount_z_opt, (size), __deref_out_ecount_opt(size) __post __deref __nullterminated)
#define __deref_out_bcount_z_opt(size)                           _SAL1_Source_(__deref_out_bcount_z_opt, (size), __deref_out_bcount_opt(size) __post __deref __nullterminated)
#define __deref_out_nz_opt                                       _SAL1_Source_(__deref_out_nz_opt, (), __deref_out_opt)
#define __deref_out_ecount_nz_opt(size)                          _SAL1_Source_(__deref_out_ecount_nz_opt, (size), __deref_out_ecount_opt(size))
#define __deref_out_bcount_nz_opt(size)                          _SAL1_Source_(__deref_out_bcount_nz_opt, (size), __deref_out_bcount_opt(size))
#define __deref_inout_opt                                        _SAL1_Source_(__deref_inout_opt, (), __deref_inout                               __pre_deref_except_maybenull __post_deref_except_maybenull)
#define __deref_inout_ecount_opt(size)                           _SAL1_Source_(__deref_inout_ecount_opt, (size), __deref_inout_ecount(size)                  __pre_deref_except_maybenull __post_deref_except_maybenull)
#define __deref_inout_bcount_opt(size)                           _SAL1_Source_(__deref_inout_bcount_opt, (size), __deref_inout_bcount(size)                  __pre_deref_except_maybenull __post_deref_except_maybenull)
#define __deref_inout_ecount_part_opt(size,length)               _SAL1_Source_(__deref_inout_ecount_part_opt, (size,length), __deref_inout_ecount_part(size,length)      __pre_deref_except_maybenull __post_deref_except_maybenull)
#define __deref_inout_bcount_part_opt(size,length)               _SAL1_Source_(__deref_inout_bcount_part_opt, (size,length), __deref_inout_bcount_part(size,length)      __pre_deref_except_maybenull __post_deref_except_maybenull)
#define __deref_inout_ecount_full_opt(size)                      _SAL1_Source_(__deref_inout_ecount_full_opt, (size), __deref_inout_ecount_full(size)             __pre_deref_except_maybenull __post_deref_except_maybenull)
#define __deref_inout_bcount_full_opt(size)                      _SAL1_Source_(__deref_inout_bcount_full_opt, (size), __deref_inout_bcount_full(size)             __pre_deref_except_maybenull __post_deref_except_maybenull)
#define __deref_inout_z_opt                                      _SAL1_Source_(__deref_inout_z_opt, (), __deref_inout_opt __pre __deref __nullterminated __post __deref __nullterminated)
#define __deref_inout_ecount_z_opt(size)                         _SAL1_Source_(__deref_inout_ecount_z_opt, (size), __deref_inout_ecount_opt(size) __pre __deref __nullterminated __post __deref __nullterminated)
#define __deref_inout_bcount_z_opt(size)                         _SAL1_Source_(__deref_inout_bcount_z_opt, (size), __deref_inout_bcount_opt(size) __pre __deref __nullterminated __post __deref __nullterminated)
#define __deref_inout_nz_opt                                     _SAL1_Source_(__deref_inout_nz_opt, (), __deref_inout_opt)
#define __deref_inout_ecount_nz_opt(size)                        _SAL1_Source_(__deref_inout_ecount_nz_opt, (size), __deref_inout_ecount_opt(size))
#define __deref_inout_bcount_nz_opt(size)                        _SAL1_Source_(__deref_inout_bcount_nz_opt, (size), __deref_inout_bcount_opt(size))
#define __deref_opt_ecount(size)                                 _SAL1_Source_(__deref_opt_ecount, (size), __deref_ecount(size)                        __pre_except_maybenull)
#define __deref_opt_bcount(size)                                 _SAL1_Source_(__deref_opt_bcount, (size), __deref_bcount(size)                        __pre_except_maybenull)
#define __deref_opt_out                                          _SAL1_Source_(__deref_opt_out, (), _Outptr_opt_)
#define __deref_opt_out_z                                        _SAL1_Source_(__deref_opt_out_z, (), _Outptr_opt_result_z_)
#define __deref_opt_out_ecount(size)                             _SAL1_Source_(__deref_opt_out_ecount, (size), __deref_out_ecount(size)                    __pre_except_maybenull)
#define __deref_opt_out_bcount(size)                             _SAL1_Source_(__deref_opt_out_bcount, (size), __deref_out_bcount(size)                    __pre_except_maybenull)
#define __deref_opt_out_ecount_part(size,length)                 _SAL1_Source_(__deref_opt_out_ecount_part, (size,length), __deref_out_ecount_part(size,length)        __pre_except_maybenull)
#define __deref_opt_out_bcount_part(size,length)                 _SAL1_Source_(__deref_opt_out_bcount_part, (size,length), __deref_out_bcount_part(size,length)        __pre_except_maybenull)
#define __deref_opt_out_ecount_full(size)                        _SAL1_Source_(__deref_opt_out_ecount_full, (size), __deref_out_ecount_full(size)               __pre_except_maybenull)
#define __deref_opt_out_bcount_full(size)                        _SAL1_Source_(__deref_opt_out_bcount_full, (size), __deref_out_bcount_full(size)               __pre_except_maybenull)
#define __deref_opt_inout                                        _SAL1_Source_(__deref_opt_inout, (), _Inout_opt_)
#define __deref_opt_inout_ecount(size)                           _SAL1_Source_(__deref_opt_inout_ecount, (size), __deref_inout_ecount(size)                  __pre_except_maybenull)
#define __deref_opt_inout_bcount(size)                           _SAL1_Source_(__deref_opt_inout_bcount, (size), __deref_inout_bcount(size)                  __pre_except_maybenull)
#define __deref_opt_inout_ecount_part(size,length)               _SAL1_Source_(__deref_opt_inout_ecount_part, (size,length), __deref_inout_ecount_part(size,length)      __pre_except_maybenull)
#define __deref_opt_inout_bcount_part(size,length)               _SAL1_Source_(__deref_opt_inout_bcount_part, (size,length), __deref_inout_bcount_part(size,length)      __pre_except_maybenull)
#define __deref_opt_inout_ecount_full(size)                      _SAL1_Source_(__deref_opt_inout_ecount_full, (size), __deref_inout_ecount_full(size)             __pre_except_maybenull)
#define __deref_opt_inout_bcount_full(size)                      _SAL1_Source_(__deref_opt_inout_bcount_full, (size), __deref_inout_bcount_full(size)             __pre_except_maybenull)
#define __deref_opt_inout_z                                      _SAL1_Source_(__deref_opt_inout_z, (), __deref_opt_inout __pre __deref __nullterminated __post __deref __nullterminated)
#define __deref_opt_inout_ecount_z(size)                         _SAL1_Source_(__deref_opt_inout_ecount_z, (size), __deref_opt_inout_ecount(size) __pre __deref __nullterminated __post __deref __nullterminated)
#define __deref_opt_inout_bcount_z(size)                         _SAL1_Source_(__deref_opt_inout_bcount_z, (size), __deref_opt_inout_bcount(size) __pre __deref __nullterminated __post __deref __nullterminated)
#define __deref_opt_inout_nz                                     _SAL1_Source_(__deref_opt_inout_nz, (), __deref_opt_inout)
#define __deref_opt_inout_ecount_nz(size)                        _SAL1_Source_(__deref_opt_inout_ecount_nz, (size), __deref_opt_inout_ecount(size))
#define __deref_opt_inout_bcount_nz(size)                        _SAL1_Source_(__deref_opt_inout_bcount_nz, (size), __deref_opt_inout_bcount(size))
#define __deref_opt_ecount_opt(size)                             _SAL1_Source_(__deref_opt_ecount_opt, (size), __deref_ecount_opt(size)                    __pre_except_maybenull)
#define __deref_opt_bcount_opt(size)                             _SAL1_Source_(__deref_opt_bcount_opt, (size), __deref_bcount_opt(size)                    __pre_except_maybenull)
#define __deref_opt_out_opt                                      _SAL1_Source_(__deref_opt_out_opt, (), _Outptr_opt_result_maybenull_)
#define __deref_opt_out_ecount_opt(size)                         _SAL1_Source_(__deref_opt_out_ecount_opt, (size), __deref_out_ecount_opt(size)                __pre_except_maybenull)
#define __deref_opt_out_bcount_opt(size)                         _SAL1_Source_(__deref_opt_out_bcount_opt, (size), __deref_out_bcount_opt(size)                __pre_except_maybenull)
#define __deref_opt_out_ecount_part_opt(size,length)             _SAL1_Source_(__deref_opt_out_ecount_part_opt, (size,length), __deref_out_ecount_part_opt(size,length)    __pre_except_maybenull)
#define __deref_opt_out_bcount_part_opt(size,length)             _SAL1_Source_(__deref_opt_out_bcount_part_opt, (size,length), __deref_out_bcount_part_opt(size,length)    __pre_except_maybenull)
#define __deref_opt_out_ecount_full_opt(size)                    _SAL1_Source_(__deref_opt_out_ecount_full_opt, (size), __deref_out_ecount_full_opt(size)           __pre_except_maybenull)
#define __deref_opt_out_bcount_full_opt(size)                    _SAL1_Source_(__deref_opt_out_bcount_full_opt, (size), __deref_out_bcount_full_opt(size)           __pre_except_maybenull)
#define __deref_opt_out_z_opt                                    _SAL1_Source_(__deref_opt_out_z_opt, (), __post __deref __valid __refparam __pre_except_maybenull __pre_deref_except_maybenull __post_deref_except_maybenull __post __deref __nullterminated)
#define __deref_opt_out_ecount_z_opt(size)                       _SAL1_Source_(__deref_opt_out_ecount_z_opt, (size), __deref_opt_out_ecount_opt(size) __post __deref __nullterminated)
#define __deref_opt_out_bcount_z_opt(size)                       _SAL1_Source_(__deref_opt_out_bcount_z_opt, (size), __deref_opt_out_bcount_opt(size) __post __deref __nullterminated)
#define __deref_opt_out_nz_opt                                   _SAL1_Source_(__deref_opt_out_nz_opt, (), __deref_opt_out_opt)
#define __deref_opt_out_ecount_nz_opt(size)                      _SAL1_Source_(__deref_opt_out_ecount_nz_opt, (size), __deref_opt_out_ecount_opt(size))
#define __deref_opt_out_bcount_nz_opt(size)                      _SAL1_Source_(__deref_opt_out_bcount_nz_opt, (size), __deref_opt_out_bcount_opt(size))
#define __deref_opt_inout_opt                                    _SAL1_Source_(__deref_opt_inout_opt, (), __deref_inout_opt                           __pre_except_maybenull)
#define __deref_opt_inout_ecount_opt(size)                       _SAL1_Source_(__deref_opt_inout_ecount_opt, (size), __deref_inout_ecount_opt(size)              __pre_except_maybenull)
#define __deref_opt_inout_bcount_opt(size)                       _SAL1_Source_(__deref_opt_inout_bcount_opt, (size), __deref_inout_bcount_opt(size)              __pre_except_maybenull)
#define __deref_opt_inout_ecount_part_opt(size,length)           _SAL1_Source_(__deref_opt_inout_ecount_part_opt, (size,length), __deref_inout_ecount_part_opt(size,length)  __pre_except_maybenull)
#define __deref_opt_inout_bcount_part_opt(size,length)           _SAL1_Source_(__deref_opt_inout_bcount_part_opt, (size,length), __deref_inout_bcount_part_opt(size,length)  __pre_except_maybenull)
#define __deref_opt_inout_ecount_full_opt(size)                  _SAL1_Source_(__deref_opt_inout_ecount_full_opt, (size), __deref_inout_ecount_full_opt(size)         __pre_except_maybenull)
#define __deref_opt_inout_bcount_full_opt(size)                  _SAL1_Source_(__deref_opt_inout_bcount_full_opt, (size), __deref_inout_bcount_full_opt(size)         __pre_except_maybenull)
#define __deref_opt_inout_z_opt                                  _SAL1_Source_(__deref_opt_inout_z_opt, (), __deref_opt_inout_opt  __pre __deref __nullterminated __post __deref __nullterminated)
#define __deref_opt_inout_ecount_z_opt(size)                     _SAL1_Source_(__deref_opt_inout_ecount_z_opt, (size), __deref_opt_inout_ecount_opt(size)  __pre __deref __nullterminated __post __deref __nullterminated)
#define __deref_opt_inout_bcount_z_opt(size)                     _SAL1_Source_(__deref_opt_inout_bcount_z_opt, (size), __deref_opt_inout_bcount_opt(size)  __pre __deref __nullterminated __post __deref __nullterminated)
#define __deref_opt_inout_nz_opt                                 _SAL1_Source_(__deref_opt_inout_nz_opt, (), __deref_opt_inout_opt)
#define __deref_opt_inout_ecount_nz_opt(size)                    _SAL1_Source_(__deref_opt_inout_ecount_nz_opt, (size), __deref_opt_inout_ecount_opt(size))
#define __deref_opt_inout_bcount_nz_opt(size)                    _SAL1_Source_(__deref_opt_inout_bcount_nz_opt, (size), __deref_opt_inout_bcount_opt(size))

/*
-------------------------------------------------------------------------------
Advanced Annotation Definitions

Any of these may be used to directly annotate functions, and may be used in
combination with each other or with regular buffer macros. For an explanation
of each annotation, see the advanced annotations section.
-------------------------------------------------------------------------------
*/

#define __success(expr)                      _Success_(expr)
#define __nullterminated                     _Null_terminated_
#define __nullnullterminated
#define __clr_reserved                       _SAL1_Source_(__reserved, (), _Reserved_)
#define __checkReturn                        _SAL1_Source_(__checkReturn, (), _Check_return_)
#define __typefix(ctype)                     _SAL1_Source_(__typefix, (ctype), __inner_typefix(ctype))
#define __override                           __inner_override
#define __callback                           __inner_callback
#define __format_string                      _Printf_format_string_
#define __blocksOn(resource)                 __inner_blocksOn(resource)
#define __control_entrypoint(category)       __inner_control_entrypoint(category)
#define __data_entrypoint(category)          __inner_data_entrypoint(category)
#define __useHeader                          _Use_decl_anno_impl_
#define __on_failure(annotes)                _On_failure_impl_(annotes _SAL_nop_impl_)

#ifndef __has_cpp_attribute
#define __has_cpp_attribute(x) (0)
#endif

#ifndef __fallthrough // [
#if __has_cpp_attribute(fallthrough)
#define __fallthrough [[fallthrough]]
#else
#define __fallthrough
#endif
#endif // ]

#ifndef __analysis_assume // [
#ifdef _PREFAST_ // [
#define __analysis_assume(expr) __assume(expr)
#else // ][
#define __analysis_assume(expr)
#endif // ]
#endif // ]

#ifndef _Analysis_assume_ // [
#ifdef _PREFAST_ // [
#define _Analysis_assume_(expr) __assume(expr)
#else // ][
#define _Analysis_assume_(expr)
#endif // ]
#endif // ]

#define _Analysis_noreturn_    _SAL2_Source_(_Analysis_noreturn_, (), _SA_annotes0(SAL_terminates))

#ifdef _PREFAST_ // [
__inline __nothrow
void __AnalysisAssumeNullterminated(_Post_ __nullterminated void *p);

#define _Analysis_assume_nullterminated_(x) __AnalysisAssumeNullterminated(x)
#else // ][
#define _Analysis_assume_nullterminated_(x)
#endif // ]

//
// Set the analysis mode (global flags to analysis).
// They take effect at the point of declaration; use at global scope
// as a declaration.
//

// Synthesize a unique symbol.
#define ___MKID(x, y) x ## y
#define __MKID(x, y) ___MKID(x, y)
#define __GENSYM(x) __MKID(x, __COUNTER__)

__ANNOTATION(SAL_analysisMode(__AuToQuOtE __In_impl_ char *mode);)

#define _Analysis_mode_impl_(mode) _SA_annotes1(SAL_analysisMode, #mode)

#define _Analysis_mode_(mode)                                                 \
    typedef _Analysis_mode_impl_(mode) int                                    \
        __GENSYM(__prefast_analysis_mode_flag);

// The following are predefined:
//  _Analysis_operator_new_throw_   (operator new throws)
//  _Analysis_operator_new_null_        (operator new returns null)
//  _Analysis_operator_new_never_fails_ (operator new never fails)
//

// Function class annotations.
__ANNOTATION(SAL_functionClassNew(__In_impl_ char*);)
__PRIMOP(int, _In_function_class_(__In_impl_ char*);)
#define _In_function_class_(x)  _In_function_class_(#x)

#define _Function_class_(x)  _SA_annotes1(SAL_functionClassNew, #x)

/*
 * interlocked operand used in interlocked instructions
 */
//#define _Interlocked_operand_ _Pre_ _SA_annotes0(SAL_interlocked)

#define _Enum_is_bitflag_    _SA_annotes0(SAL_enumIsBitflag)
#define _Strict_type_match_  _SA_annotes0(SAL_strictType2)

#define _Maybe_raises_SEH_exception_   _Pre_ _SA_annotes1(SAL_inTry,__yes)
#define _Raises_SEH_exception_         _Group_(_Maybe_raises_SEH_exception_ _Analysis_noreturn_)

#ifdef  __cplusplus // [
}
#endif // ]
