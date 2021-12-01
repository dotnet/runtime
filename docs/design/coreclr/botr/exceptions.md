What Every Dev needs to Know About Exceptions in the Runtime
============================================================

Date: 2005

When talking about "exceptions" in the CLR, there is an important distinction to keep in mind. There are managed exceptions, which are exposed to applications through mechanisms like C#'s try/catch/finally, with all of the runtime machinery to implement them. And then there is the use of exceptions inside the runtime itself. Most runtime developers seldom need to think about how to build and expose the managed exception model. But every runtime developer needs to understand how exceptions are used in the implementation of the runtime. When there is a need to keep the distinction clear, this document will refer to _managed exceptions_ that a managed application may throw or catch, and will refer to the _CLR's internal exceptions_ that are used by the runtime for its own error handling. Mostly, though, this document is about the CLR's internal exceptions.

Where do exceptions matter?
===========================

Exceptions matter almost everywhere. They matter the most in functions that throw or catch exceptions, because that code must be written explicitly to throw the exception, or to catch and properly handle an exception. Even if a particular function doesn't itself throw an exception, it may well call one that does, and so that particular function must be written to behave correctly when an exception is thrown through it. The judicious use of _holders_ can greatly ease writing such code correctly.

Why are CLR internal exceptions different?
==========================================

The CLR's internal exceptions are much like C++ exceptions, but not exactly. CoreCLR can be built for Mac OSX, for Linux, for BSD, and for Windows. The OS and compiler differences dictate that we can't just use standard C++ try/catch. In addition, the CLR internal exceptions provide features similar to the managed "finally" and "fault".

With the help of some macros, it is possible to write exception handling code that is almost as easy to write and to read as standard C++.

Catching an Exception
=====================

EX_TRY
------

The basic macros are, of course, EX_TRY / EX_CATCH / EX_END_CATCH, and in use they look like this:

    EX_TRY
      // Call some function.  Maybe it will throw an exception.
      Bar();
    EX_CATCH
      // If we're here, something failed.
      m_finalDisposition = terminallyHopeless;
    EX_END_CATCH(RethrowTransientExceptions)

The EX_TRY macro simply introduces the try block, and is much like the C++ "try", except that it also includes an opening brace, "{".

EX_CATCH
--------

The EX_CATCH macro ends the try block, including the closing brace, "}", and begins the catch block. Like the EX_TRY, it also starts the catch block with an opening brace.

And here is the big difference from C++ exceptions: the CLR developer doesn't get to specify what to catch. In fact, this set of macros catches everything, including non-C++ exceptions like AV or a managed exception. If a bit of code needs to catch just one exception, or a subset, then it will need to catch, examine the exception, and rethrow anything that isn't relevant.

It bears repeating that the EX_CATCH macro catches everything. This behaviour is frequently not what a function needs. The next two sections discuss more about how to deal with exceptions that shouldn't have been caught.

GET_EXCEPTION() & GET_THROWABLE()
---------------------------------

How, then, does a CLR developer discover just what has been caught, and determine what to do? There are several options, depending on just what the requirement is.

First, whatever the (C++) exception that is caught, it will be delivered as an instance of some class derived from the global Exception class. Some of these derived classes are pretty obvious, like OutOfMemoryException. Some are somewhat domain specific, like EETypeLoadException. And some of these are just wrapper classes around another system's exceptions, like CLRException (has an OBJECTHANDLE to reference any managed exception) or HRException (wraps an HRESULT). If the original exception was not derived from Exception, the macros will wrap it up in something that is.  (Note that all of these exceptions are system-provided and well known. _New exception classes shouldn't be added without involving the Core Execution Engine Team!_)

Next, there is always an HRESULT associated with a CLR internal exception. Sometimes, as with HRException, the value came from some COM source, but internal errors and Win32 api failures also have HRESULTS.

Finally, because almost any exception inside the CLR could possibly be delivered back to managed code, there is a mapping from the internal exceptions back to the corresponding managed exceptions. The managed exception won't necessarily be created, but there is always the possibility of obtaining it.

So, given these features, how does the CLR developer categorize an exception?

Frequently, all that is needed to categorize is the HRESULT that corresponds to the exception, and this is extremely easy to get:

    HRESULT hr = GET_EXCEPTION()->GetHR();

More information is often most conveniently available through the managed exception object. And if the exception will be delivered back to managed code, whether immediately, or cached for later, the managed object is, of course, required. And the exception object is just as easy to get. Of course, it is a managed objectref, so all the usual rules apply:

    OBJECTREF throwable = NULL;
    GCPROTECT_BEGIN(throwable);
    // . . .
    EX_TRY
        // . . . do something that might throw
    EX_CATCH
        throwable = GET_THROWABLE();
    EX_END_CATCH(RethrowTransientExceptions)
    // . . . do something with throwable
    GCPROTECT_END()

Sometimes, there is no avoiding a need for the C++ exception object, though this is mostly inside the exception implementation. If it is important exactly what the C++ exception type is, there is a set of lightweight RTTI-like functions that help categorize exceptions. For instance,

    Exception *pEx = GET_EXCEPTION();
    if (pEx->IsType(CLRException::GetType())) {/* ... */}

would tell whether the exception is (or derives from) CLRException.

EX_END_CATCH(RethrowTransientExceptions)
----------------------------------------

In the example above, "RethrowTransientExceptions" is an argument to the EX_END_CATCH macro; it is one of three pre-defined macros that can be thought of "exception disposition". Here are the macros, and their meanings:

- _SwallowAllExceptions_: This is aptly named, and very simple. As the name suggests, it swallows everything. While simple and appealing, this is often not the right thing to do.
- _RethrowTerminalExceptions_. A better name would be "RethrowThreadAbort", which is what this macro does.
- _RethrowTransientExceptions_. The best definition of a "transient" exception is one that might not occur if tried again, possibly in a different context. These are the transient exceptions:
  - COR_E_THREADABORTED
  - COR_E_THREADINTERRUPTED
  - COR_E_THREADSTOP
  - COR_E_APPDOMAINUNLOADED
  - E_OUTOFMEMORY
  - HRESULT_FROM_WIN32(ERROR_COMMITMENT_LIMIT)
  - HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY)
  - (HRESULT)STATUS_NO_MEMORY
  - COR_E_STACKOVERFLOW
  - MSEE_E_ASSEMBLYLOADINPROGRESS

The CLR developer with doubts about which macro to use should probably pick _RethrowTransientExceptions_.

In every case, however, the developer writing an EX_END_CATCH needs to think hard about which exception should be caught, and should catch only those exceptions. And, because the macros catch everything anyway, the only way to not catch an exception is to rethrow it.

If an EX_CATCH / EX_END_CATCH block has properly categorized its exceptions, and has rethrown wherever necessary, then SwallowAllExceptions is the way to tell the macros that no further rethrowing is necessary.

## EX_CATCH_HRESULT

Sometimes all that is needed is the HRESULT corresponding to an exception, particularly when the code is in an interface from COM. For these cases, EX_CATCH_HRESULT is simpler than writing a while EX_CATCH block. A typical case would look like this:

    HRESULT hr;
    EX_TRY
      // code
    EX_CATCH_HRESULT (hr)

    return hr;

_However, while very tempting, it is not always correct_. The EX_CATCH_HRESULT catches all exceptions, saves the HRESULT, and swallows the exception. So, unless that exception swallowing is what the function really needs, EX_CATCH_HRESULT is not appropriate.

EX_RETHROW
----------

As noted above, the exception macros catch all exceptions; the only way to catch a specific exception is to catch all, and rethrow all but the one(s) of interest. So, if, after an exception is caught, examined, possibly logged, and so forth, it shouldn't be caught, it may be re-thrown. EX_RETHROW will re-raise the same exception.

Not catching an exception
=========================

It's frequently the case that a bit of code doesn't need to catch an exception, but does need to perform some sort of cleanup or compensating action, Holders are frequently just the thing for this scenario, but not always. For the times that holders aren't adequate, the CLR has two variations on a "finally" block.

EX_TRY_FOR_FINALLY
------------------

When there is a need for some sort of compensating action as code exits, a finally may be appropriate. There is a set of macros to implement a try/finally in the CLR:

    EX_TRY_FOR_FINALLY
      // code
    EX_FINALLY
      // exit and/or backout code
    EX_END_FINALLY

**Important** : The EX_TRY_FOR_FINALLY macros are built with SEH, rather than C++ EH, and the C++ compiler doesn't allow SEH and C++ EH to be mixed in the same function. Locals with auto-destructors require C++ EH for their destructor to run. Therefore, any function with EX_TRY_FOR_FINALLY can't have EX_TRY, and can't have any local variable with an auto-destructor.

EX_HOOK
-------

Frequently there is a need for compensating code, but only when an exception is thrown. For these cases, EX_HOOK is similar to EX_FINALLY, but the "hook" clause only runs when there is an exception. The exception is automatically rethrown at the end of the "hook" clause.

    EX_TRY
      // code
    EX_HOOK
      // code to run when an exception escapes the "code" block.
    EX_END_HOOK

This construct is somewhat better than simply EX_CATCH with EX_RETHROW, because it will rethrow a non-stack-overflow, but will catch a stack overflow exception (and unwind the stack) and then throw a new stack overflow exception.

Throwing an Exception
=====================

Throwing an Exception in the CLR is generally a matter of calling

    COMPlusThrow ( < args > )

There are a number of overloads, but the idea is to pass the "kind" of the exception to COMPlusThrow. The list of "kinds" is generated by a set of macros operating on [rexcep.h](https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/rexcep.h), and the various "kinds" are kAmbiguousMatchException, kApplicationException, and so forth. Additional arguments (for the overloads) specify resources and substitution text. Generally, the right "kind" is selected by looking for other code that reports a similar error.

There are some pre-defined convenience variations:

COMPlusThrowOOM();
------------------

Defers to ThrowOutOfMemory(), which throws the C++ OOM exception. This will throw a pre-allocated exception, to avoid the problem of being out of memory trying to throw an out of memory exception!

When getting the managed exception object for this exception, the runtime will first try to allocate a new managed object <sup>[1]</sup>, and if that fails, will return a pre-allocated, shared, global out of memory exception object.

[1] After all, if it was a request for a 2gb array that failed, a simple object may be fine.

COMPlusThrowHR(HRESULT theBadHR);
---------------------------------

There are a number of overloads, in case you have an IErrorInfo, etc. There is some surprisingly complicated code to figure out what kind of exception corresponds to a particular HRESULT.

COMPlusThrowWin32(); / COMPlusThrowWin32(hr);
---------------------------------------------

Basically throws an HRESULT_FROM_WIN32(GetLastError())

COMPlusThrowSO();
-----------------

Throws a Stack Overflow (SO) Exception. Note that this is not a hard SO, but rather an exception we throw when proceeding might lead to a hard SO.

Like OOM, this throws a pre-allocated C++ SO exception object. Unlike OOM, when retrieving the managed object, the runtime always returns the pre-allocated, shared, global stack overflow exception object.

COMPlusThrowArgumentNull()
--------------------------

A helper for throwing an "argument foo must not be null" exception.

COMPlusThrowArgumentOutOfRange()
--------------------------------

As it sounds.

COMPlusThrowArgumentException()
-------------------------------

Yet another flavor of invalid argument exception.

COMPlusThrowInvalidCastException(thFrom, thTo)
----------------------------------------------

Given type handles to from and to types of the attempted cast, the helper creates a nicely formatted exception message.

EX_THROW
--------

This is a low-level throw construct that is not generally needed in normal code. Many of the COMPlusThrowXXX functions use EX_THROW internally, as do other specialized ThrowXXX functions.  It is best to minimize direct use of EX_THROW, simply to keep the nitty-gritty details of the exception mechanism as well encapsulated as possible. But when none of the higher-level Throw functions work, it is fine to use EX_THROW.

The macro takes two arguments, the type of exception to be thrown (some sub-type of the C++ Exception class), and a parenthesized list of arguments to the exception type's constructor.

Using SEH directly
==================

There are a few situations where it is appropriate to use SEH directly. In particular, SEH is the only option if some processing is needed on the first pass, that is, before the stack is unwound. The filter code in an SEH __try/__except can do anything, in addition to deciding whether to handle an exception. Debugger notifications is an area that sometimes needs first pass handling.

Filter code needs to be written very carefully. In general, the filter code must be prepared for any random, and likely inconsistent, state. Because the filter runs on the first pass, and dtors run on the second pass, holders won't have run yet, and will not have restored their state.

PAL_TRY / PAL_EXCEPT, PAL_EXCEPT_FILTER, PAL_FINALLY / PAL_ENDTRY
-----------------------------------------------------------------

When a filter is needed, the PAL_TRY family is the portable way to write one in the CLR. Because the filter uses SEH directly, it is incompatible with C++ EH in the same function, and so there can't be any holders in the function.

Again, these should be rare.

__try / __except, __finally
---------------------------

There isn't a good reason to use these directly in the CLR.

Exceptions and GC mode
======================

Throwing an exception with COMPlusThrowXXX() doesn't affect the GC mode, and is safe in any mode. As the exception unwinds back to the EX_CATCH, any holders that were on the stack will be unwound, releasing their resources and resetting their state. By the time that execution resumes in the EX_CATCH, the holder-protected state will have been restored to what it was at the time of the EX_TRY.

Transitions
===========

Considering managed code, the CLR, COM servers, and other native code, there are many possible transitions between calling conventions, memory management, and, of course, exception handling mechanisms. Regarding exceptions, it is fortunate for the CLR developer that most of these transitions are either completely outside of the runtime, or are handled automatically.  There are three transitions that are a daily concern for a CLR developer. Anything else is an advanced topic, and those who need to know about them, are well aware that they need to know!

Managed code into the runtime
-----------------------------

This is the "fcall", "jit helper", and so forth. The typical way that the runtime reports errors back to managed code is through a managed exception. So, if an fcall function, directly or indirectly, raises a managed exception, that's perfectly fine. The normal CLR managed exception implementation will "do the right thing" and look for an appropriate managed handler.

On the other hand, if an fcall function can do anything that might throw a CLR internal exception (one of the C++ exceptions), that exception must not be allowed to leak back out to managed code. To handle this case, the CLR has the UnwindAndContinueHandler (UACH), which is a set of code to catch the C++ EH exceptions, and re-raise them as managed exceptions.

Any runtime function that is called from managed code, and might throw a C++ EH exception, must wrap the throwing code in INSTALL_UNWIND_AND_CONTINUE_HANDLER / UNINSTALL_UNWIND_AND_CONTINUE_HANDLER.  Installing a HELPER_METHOD_FRAME will automatically install the UACH. There is a non-trivial amount of overhead to installing a UACH, so they shouldn't be used everywhere. One technique that is used in performance critical code is to run without a UACH, and install one just before throwing an exception.

When a C++ exception is thrown, and there is a missing UACH, the typical failure will be a Contract Violation of "GC_TRIGGERS called in a GC_NOTRIGGER region" in CPFH_RealFirstPassHandler. To fix these, look for managed to runtime transitions, and check for INSTALL_UNWIND_AND_CONTINUE_HANDLER or HELPER_METHOD_FRAME_BEGIN_XXX.

Runtime code into managed code
------------------------------

The transition from the runtime into managed code has highly platform-dependent requirements. On 32-bit Windows platforms, the CLR's managed exception code requires that "COMPlusFrameHandler" is installed just before entering managed code. These transitions are handled by highly specialized helper functions, which take care of the appropriate exception handlers. It is very unlikely that any typical new calls into managed would use any other way in. In the event that the COMPlusFrameHander were missing, the most likely effect would be that exception handling code in the target managed code simply wouldn't be executed – no finally blocks, and no catch blocks.

Runtime code into external native code
--------------------------------------

Calls from the runtime into other native code (the OS, the CRT, and other DLLs) may need particular attention. The cases that matter are those in which the external code might cause an exception. The reason that this is a problem comes from the implementation of the EX_TRY macros, and in particular how they translate or wrap non-Exceptions into Exceptions. With C++ EH, it is possible to catch any and all exceptions (via "catch(...)"), but only by giving up all information about what has been caught. When catching an Exception*, the macros have the exception object to examine, but when catching anything else, there is nothing to examine, and the macros must guess what the actual exception is. And when the exception comes from outside of the runtime, the macros will always guess wrong.

The current solution is to wrap the call to external code in a "callout filter". The filter will catch the external exception, and translate it into SEHException, one of the runtime's internal exceptions. This filter is predefined, and is simple to use. However, using a filter means using SEH, which of course precludes using C++ EH in the same function. To add a callout filter to a function that uses C++ EH will require splitting a function in two.

To use the callout filter, instead of this:

    length = SysStringLen(pBSTR);

write this:

    BOOL OneShot = TRUE;
    struct Param {
        BSTR*  pBSTR;
        int length;
    };
    struct Param param;
    param.pBSTR = pBSTR;

    PAL_TRY(Param*, pParam, &param)
    {
      pParam->length = SysStringLen(pParam->pBSTR);
    }
    PAL_EXCEPT_FILTER(CallOutFilter, &OneShot)
    {
      _ASSERTE(!"CallOutFilter returned EXECUTE_HANDLER.");
    }
    PAL_ENDTRY;

A missing callout filter on a call that raises an exception will always result in the wrong exception being reported in the runtime. The type that is incorrectly reported isn't even always deterministic; if there is already some managed exception "in flight", then that managed exception is what will be reported. If there is no current exception, then OOM will be reported. On a checked build there are asserts that usually fire for a missing callout filter. These assert messages will include the text "The runtime may have lost track of the type of an exception".

Miscellaneous
=============

There are actually a lot of macros involved in EX_TRY. Most of them should never, ever, be used outside of the macro implementations.
