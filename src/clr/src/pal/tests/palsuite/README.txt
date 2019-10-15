; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

===========================================================================


1.   ENVIRONMENT SETUP

2.   RUNNING THE SUITES

3.   ENVIRONMENT VARIABLES AND AUTOMATED TESTING SPECIFICS
3.1  PAL_DISABLE_MESSAGEBOX
3.2  Other Notes

4.   ADDITIONAL NOTES ON TESTING/SPECIFIC TEST CASE ISSUES
     C_runtime: _fdopen testing issues
     File_IO: getfilesize/test1, setfilepointer/test(5,6,7)
     File_IO: gettempfilename(a,w)/test2
     File_IO: setfileattributesa/test(1,4), setfileattributesw/test(1,4)
     Miscellaneous: messageboxw/test(1,2)
     Pal_specific:: pal_get_stdin/test1, pal_get_stdout/test1, pal_get_stderr/test1
     Threading: setconsolectrlhandler/test(3,4)


===========================================================================

1.  ENVIRONMENT SETUP
~~~~~~~~~~~~~~~~~~~~~

Within a Rotor build window (env.sh/env.csh/env.bat), no additional
configuration needs to be done.


2. RUNNING THE SUITES
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Run %ROTOR_DIR%\tests\pvtrun.pl to launch the test suites.  It will
display information about each test as it runs, then report a
summary of the results upon completion.

The results are logged to %ROTOR_DIR%\tests\pvtResults.log.


3. ENVIRONMENT VARIABLES AND AUTOMATED TESTING SPECIFICS
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    
3.1 PAL_DISABLE_MESSAGEBOX

For automated testing on the WIN32 PAL, the following environment
variable is useful to prevent pop-up message boxes from interupting the
execution of the MessageBoxW test cases:

set PAL_DISABLE_MESSAGEBOX=1  


3.2 Other Notes

See notes in section 4 on the following test cases if running automated tests:

     Miscellaneous: messageboxw/test(1,2)
     Threading: setconsolectrlhandler/test(3,4)


4. ADDITIONAL NOTES ON TESTING/SPECIFIC TEST CASE ISSUES
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

C Runtime: _fdopen testing issues

There is a very specific manner in which _fdopen has been documented to work,
this will determine how the function will be tested.

_fdopen takes a parameter of a c run-time file handle, to open a stream to the
file. This file handle can only be returned from the _open_osfhandle function.
The Rotor documentation states that _open_osfhandle will only return a 
READ-ONLY file handle, from an operating-system file handle returned from
CreatePipe().

With these restrictions _fdopen will only be tested with a mode of read(r).
The other modes are not possible to test. _open_osfhandle returns an error
when attempting to open a write pipe handle in a mode of read-only. As well,
it is not possible to read and write to the same handle that is returned from
CreatePipe().

The modes that will not be tested are as follows:

    "w" -  Opens an empty file for writing. 
    "a" -  Opens for writing at the end of the file (appending).
    "r+" - Opens for both reading and writing.
    "w+" - Opens an empty file for both reading and writing.
    "a+" - Opens for reading and appending.



File_IO: getfilesize/test1, getfilesizeex/test1 setfilepointer/test(5,6,7)

These tests cases create a large number of temporary files which require  
ample disk space.  On systems with less than 6Gb free disk space expect 
these test cases to fail.


File_IO: gettempfilename(a,w)/test2

These test cases take longer than 60 seconds to run.  Currently, the Test 
Harness will timeout any test case that exceeds 60 seconds.


File_IO: setfileattributesa/test(1,4), SetFileAttributesW/test(1,4)

These test cases ensure restricted file permissions are respected.  Administrators 
or super users (root) are not affected by file permissions and, as a result, these
test cases will fail for such users.


Miscellaneous: messageboxw/test(1,2)

Setting PAL_MESSAGEBOX_DISABLE=1 for these test cases prevents message box pop
ups that occur during the tests' execution on Windows.  For automated testing
where user interaction is not desired/possible, setting this environment 
variable will prevent a pause in the automated test run.


ic: pal_get_stdin/test1, pal_get_stdout/test1, pal_get_stderr/test1

These test cases should be manually inspected to ensure the information being returned
is correct.  The pal_get_stdin test case requires user input.  The pal_get_stdout and 
pal_get_stderr test cases do not require user input, but their output should be inspected 
to verify that correct messages are being displayed.


Threading: setconsolectrlhandler/test(3,4)

These test cases require user response in order to produce a meaningful results.
For automated testing, this test case is disabled.
