//Licensed to the .NET Foundation under one or more agreements. 
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "unixasmmacros.inc"

NESTED_ENTRY FuncEvalHijack, _TEXT, FuncEvalHijackPersonalityRoutine

// NOTE: FuncEvalHijackPersonalityRoutine is dependent on the stack layout so if 
// you change the prolog you will also need to update the personality routine.

// push arg to the stack so our personality routine can find it 
// push lr to get good stacktrace in debugger    

PROLOG_SAVE_REG_PAIR  fp, lr, #-32

	str x0, [sp, #16]
	// FuncEvalHijackWorker returns the address we should jump to.    
	bl FuncEvalHijackWorker
	
	EPILOG_STACK_FREE 32
	EPILOG_BRANCH_REG x0
NESTED_END FuncEvalHijack, _TEXT

//NESTED_ENTRY ExceptionHijack,,ExceptionHijackPersonalityRoutine