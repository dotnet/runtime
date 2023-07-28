// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/*********************************************************************/
/*                           AllocaCheck                             */
/*********************************************************************/

/* check for alloca overruns (which otherwise are hard to track down
   and often only repro on optimized builds).

   USAGE:

		void foo() {
			ALLOCA_CHECK();				// Declare at function level scope

			....
			void* mem = ALLOCA(size);	// does an alloca,

		}	// destructor of ALLOCA_CHECK for buffer overruns.
*/

/*   */
/*********************************************************************/

#ifndef AllocaCheck_h
#define AllocaCheck_h
#include <malloc.h>			// for alloca itself

#if defined(assert) && !defined(_ASSERTE)
#define _ASSERTE assert
#endif

#if defined(_DEBUG) || defined(DEBUG)

/*********************************************************************/
class AllocaCheck {
public:
	enum { CheckBytes = 0xCCCDCECF,
		 };

	struct AllocaSentinel {
		int check;
		AllocaSentinel* next;
	};

public:
	/***************************************************/
	AllocaCheck() {
		sentinels = 0;
	}

	~AllocaCheck() {
		AllocaSentinel* ptr = sentinels;
		while (ptr != 0) {
			if (ptr->check != (int)CheckBytes)
				_ASSERTE(!"alloca buffer overrun");
			ptr = ptr->next;
		}
	}

	void* add(void* allocaBuff, unsigned size) {
		AllocaSentinel* newSentinel = (AllocaSentinel*) ((char*) allocaBuff + size);
		newSentinel->check = CheckBytes;
		newSentinel->next = sentinels;
		sentinels = newSentinel;
        memset(allocaBuff, 0xDD, size);
		return allocaBuff;
	}

private:
	AllocaSentinel* sentinels;
};

#define ALLOCA_CHECK() AllocaCheck __allocaChecker
#define ALLOCA(size)  __allocaChecker.add(_alloca(size+sizeof(AllocaCheck::AllocaSentinel)), size);

#else

#define ALLOCA_CHECK()
#define ALLOCA(size)  _alloca(size)

#endif

#endif
