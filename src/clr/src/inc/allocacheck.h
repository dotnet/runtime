//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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

	struct AllocaSentinal {
		int check;
		AllocaSentinal* next;
	};

public:
	/***************************************************/
	AllocaCheck() { 
		sentinals = 0; 
	}

	~AllocaCheck() { 
		AllocaSentinal* ptr = sentinals;
		while (ptr != 0) {
			if (ptr->check != (int)CheckBytes)
				_ASSERTE(!"alloca buffer overrun");
			ptr = ptr->next;
		}
	}

	void* add(void* allocaBuff, unsigned size) {
		AllocaSentinal* newSentinal = (AllocaSentinal*) ((char*) allocaBuff + size);
		newSentinal->check = CheckBytes;
		newSentinal->next = sentinals;
		sentinals = newSentinal;
        memset(allocaBuff, 0xDD, size);
		return allocaBuff;
	}

private:
	AllocaSentinal* sentinals;
};

#define ALLOCA_CHECK() AllocaCheck __allocaChecker
#define ALLOCA(size)  __allocaChecker.add(_alloca(size+sizeof(AllocaCheck::AllocaSentinal)), size);

#else

#define ALLOCA_CHECK() 
#define ALLOCA(size)  _alloca(size)

#endif
	
#endif
