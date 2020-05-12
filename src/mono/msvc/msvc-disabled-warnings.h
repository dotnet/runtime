#pragma once

#ifdef _MSC_VER

// FIXME This is all questionable but the logs are flooded and nothing else is fixing them.
#define _CRT_SECURE_NO_WARNINGS 1
#pragma warning(  error:4013) // function undefined; assuming extern returning int
#pragma warning(disable:4018) // signed/unsigned mismatch
#pragma warning(  error:4022) // call and prototype disagree
#pragma warning(  error:4047) // differs in level of indirection
#pragma warning(disable:4090) // const problem
#pragma warning(  error:4098) // void return returns a value
#pragma warning(disable:4101) // unreferenced local variable
#pragma warning(  error:4113) // call and prototype disagree
#pragma warning(disable:4146) // unary minus operator applied to unsigned type, result still unsigned
#pragma warning(  error:4172) // returning address of local variable or temporary
#pragma warning(disable:4189) // local variable is initialized but not referenced
#pragma warning(  error:4197) // top-level volatile in cast is ignored
#pragma warning(disable:4244) // integer conversion, possible loss of data
#pragma warning(disable:4245) // signed/unsigned mismatch
#pragma warning(disable:4267) // integer conversion, possible loss of data
#pragma warning(  error:4273) // inconsistent dll linkage
#pragma warning(  error:4293) // shift count negative or too big, undefined behavior
#pragma warning(disable:4305) // truncation from 'double' to 'float'
#pragma warning(  error:4312) // 'type cast': conversion from 'MonoNativeThreadId' to 'gpointer' of greater size
#pragma warning(disable:4389) // signed/unsigned mismatch
#pragma warning(disable:4456) // declaration of 'j' hides previous local declaration
#pragma warning(disable:4457) // declaration of 'text' hides function parameter
#pragma warning(disable:4702) // unreachable code
#pragma warning(disable:4706) // assignment within conditional expression
#pragma warning(  error:4715) // 'keyword' not all control paths return a value
#pragma warning(disable:4996) // deprecated function GetVersion GetVersionExW fopen inet_addr mktemp sprintf strcat strcpy strtok unlink etc.

#endif
