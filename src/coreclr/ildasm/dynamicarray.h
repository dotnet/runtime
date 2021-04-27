// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef DYNAMICARRAY_H

#define DYNAMICARRAY_H

#include "memory.h"

const int START_SIZE = 24 ;
const int MIN_SIZE = 8 ;

template <class T>
class DynamicArray
{
	public:
		DynamicArray(int iSize = START_SIZE) ;
		~DynamicArray() ;
		T& operator[](int i) ;
		bool Error() ;
	private:
		T* m_pArray ;
		int m_iMemSize ;
		int m_iArraySize ;
		bool m_bError ;
};

/************************************************************************
 *																		*
 *	Default constructor. User has option to pass in the size of the		*
 *	initial array.														*
 *																		*
 ************************************************************************/
template<class T> DynamicArray<T>::DynamicArray(int iSize)
{
	if( iSize < MIN_SIZE )
	{
		iSize = MIN_SIZE ;
	}
	m_pArray = new T[iSize] ;
	m_iMemSize = iSize ;
	m_iArraySize = 0 ;
	m_bError = false ;
}

/************************************************************************
 *																		*
 *	Destructor. All it really has to do is delete the array.			*
 *																		*
 ************************************************************************/
template<class T> DynamicArray<T>::~DynamicArray()
{
	if( m_pArray )
	{
		delete [] m_pArray ;
	}
}

/************************************************************************
 *																		*
 *	operator [] to work on the left or right side of the equation.		*
 *																		*
 ************************************************************************/
template<class T> T& DynamicArray<T>::operator [](int iIndex)
{
	if( iIndex < 0 )
	{
		// Error, set error value to true and return the first element of the array
		m_bError = true ;
		return m_pArray[0] ;
	}
	else if ( iIndex >= m_iArraySize )
	{
		if( iIndex >= m_iMemSize )
		{
			int iNewSize ;
			if( iIndex >= m_iMemSize * 2 )
			{
				iNewSize = iIndex + 1 ;
			}
			else
			{
				iNewSize = m_iMemSize * 2 ;
			}

			// We need to allocate more memory
			T* pTmp = new T[iNewSize] ;
			memcpy(pTmp, m_pArray, m_iMemSize * sizeof(T)) ;
			delete [] m_pArray ;
			m_pArray = pTmp ;
			// Record the new memory size
			m_iMemSize = iNewSize ;
		}

		//ZeroMemory(&m_pArray[iIndex], sizeof(T)) ;

		++m_iArraySize ;
	}

	return m_pArray[iIndex] ;
}

template<class T> bool DynamicArray<T>::Error()
{
	return m_bError ;
}

#endif
