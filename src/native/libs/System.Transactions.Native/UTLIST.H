//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

//  Copyright (C) 1995-1999 Microsoft Corporation.  All rights reserved.
#ifndef __UTLIST_H__
#define __UTLIST_H__
/******************************************************************************
Microsoft D.T.C. (Distributed Transaction Coordinator)


@doc

@module UTList.h  |

*******************************************************************************/


//---------- Forward Declarations -------------------------------------------
template <class T> class UTLink;
template <class T> class UTList;
template <class T> class UTStaticList;

//-----**************************************************************-----
// @class Template class
//      The UTLink class is the backbone of a linked list. It holds the
//      actual data of type T (which is the list's data type) and points
//      to the next and previous elements in the list.
//      The class is entirely private to hide all functions and data from
//      everyone but it's friends - which are the UTList and UTListIterator.
//
// @tcarg class | T | data type to store in the link
//-----**************************************************************-----
template <class T> class UTLink
{
// @access Public members
public:

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // constructors/destructor
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // @cmember Constructor
    UTLink (void);
    // @cmember Constructor
    UTLink (const T& LinkValue,UTLink< T > * Prev = NULL,UTLink< T > * Next = NULL);
    // @cmember Copy Constructor
    UTLink (const UTLink< T >&  CopyLink);
    // @cmember Destructor
    virtual ~UTLink (void);

    void Init (const T& LinkValue,UTLink< T > * Prev = NULL,UTLink< T > * Next = NULL);

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // operators
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // @cmember Assignment operator
    virtual UTLink< T >& operator = (const UTLink< T >& AssignLink);

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // action protocol
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    BOOL        IsLinked();

public:
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // friends
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    friend class UTStaticList < T >;

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // data members
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // @cmember Value held in the link of type T
    T               m_Value;
    // @cmember Pointer to next link (in list)
    UTLink< T > *   m_pNextLink;
    // @cmember Pointer to previous link (in list)
    UTLink< T > *   m_pPrevLink;

    // remember if we are currently queued in a list
    BOOL            m_fQueued;


    // remember the queue where we are currently stored

    UTStaticList< T > * m_pQueuedList;


};


//-----**************************************************************-----
// @class Template class
//      The UTStaticList class. This is similar to the UTList class except 
//      that it in this the links are all static. Links are provided, to
//      the various methods, they are never created or destroyed as they
//      have been preallocated.
//
// @tcarg class | T | data type to store in the list
//-----**************************************************************-----
template <class T> class UTStaticList
{
// @access Public members
public:

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // constructors/destructor
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // @cmember Consructor
    UTStaticList    (void);
    // @cmember Copy Constructor
//  UTStaticList    (const UTStaticList< T >& CopyList);
    // @cmember Destructor
    virtual ~UTStaticList (void);

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // operators
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // @cmember Assignment operator
//  virtual UTStaticList< T >&      operator =  (const UTStaticList< T >& AssignList);

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // action protocol
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // @cmember Insert new link at the end of the list
    virtual void            InsertLast  (UTLink <T> * pLink);
    // @cmember Remove first element from the list
    virtual BOOL            RemoveFirst (UTLink <T> ** ppLink);

    virtual void    Init (void);

// @access Protected members
public:

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // data members
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    // @cmember Count of elements in the list
    ULONG           m_ulCount;
    // @cmember Pointer to the first link in the list
    UTLink< T > *   m_pFirstLink;
    // @cmember Pointer to the last link in the list
    UTLink< T > *   m_pLastLink;
}; //End class UTStaticList






//---------- Inline Functions -----------------------------------------------

//---------------------------------------------------------------------------
// @mfunc   Constructors
//
// @tcarg None.
//
//
// @rdesc None.
//
//---------------------------------------------------------------------------

template <class T> UTLink< T >::UTLink
    (
        void
    )
    {
    // do nothing
    m_pPrevLink = NULL;
    m_pNextLink = NULL;

    m_fQueued = 0;
    m_pQueuedList = NULL;

    }



//---------------------------------------------------------------------------
// @mfunc   Constructors
//
// @tcarg class | T | data type to store in the link
//
//
// @rdesc None.
//
//---------------------------------------------------------------------------

template <class T> UTLink< T >::UTLink(
        const T&        LinkValue,  // @parm [in] Value to be stored with link
        UTLink< T > *   Prev,       // @parm [in] pointer to previous link
        UTLink< T > *   Next        // @parm [in] pointer to next link
        )
: m_Value(LinkValue),
    m_pPrevLink(Prev),
    m_pNextLink(Next)
    {
    m_fQueued = 0;
    m_pQueuedList = NULL;

    // do nothing
    }

//---------------------------------------------------------------------------
// @tcarg class | T | data type to store in the link
//
// @rdesc None.
//
//---------------------------------------------------------------------------

template <class T> UTLink< T >::UTLink(
        const UTLink< T >&  CopyLink    // @parm [in] value to copy into this object
        )
    {
    m_Value     = CopyLink.m_Value;
    m_pPrevLink = CopyLink.m_pPrevLink;
    m_pNextLink = CopyLink.m_pNextLink;
    }

//---------------------------------------------------------------------------
// @mfunc   This is the destructor. Currently, it does nothing.
//
// @tcarg class | T | data type to store in the link
//
// @rdesc None.
//
//---------------------------------------------------------------------------

template <class T> UTLink< T >::~UTLink(
        void
        )
    {
    // do nothing
    }


//---------------------------------------------------------------------------
// @mfunc   This is the assignment operator for <c UTLink>.
//
// @tcarg class | T | data type to store in the link
//
// @rdesc   Returns a reference to the newly assigned object
//
//---------------------------------------------------------------------------
template <class T> void UTLink<T>::Init
     (
        const T& LinkValue,UTLink< T > * Prev, UTLink< T > * Next
     )
{
    m_Value     = LinkValue;
    m_pPrevLink = Prev;
    m_pNextLink = Next;
}



//---------------------------------------------------------------------------
// @mfunc   This is the assignment operator for <c UTLink>.
//
// @tcarg class | T | data type to store in the link
//
// @rdesc   Returns a reference to the newly assigned object
//
//---------------------------------------------------------------------------

template <class T> UTLink< T >& UTLink< T >::operator =(
        const UTLink< T >&  AssignLink  // @parm [in] value to assign into this object
        )
    {
    m_Value     = AssignLink.m_Value;
    m_pPrevLink = AssignLink.m_pPrevLink;
    m_pNextLink = AssignLink.m_pNextLink;
    return *this;
    }


template <class T> BOOL UTLink<T>::IsLinked()
{
    if ( m_fQueued )
    {
        return TRUE;
    }
    return FALSE;
}



//---------------------------------------------------------------------------
// @mfunc   Constructors
//
// @tcarg class | T | data type to store in the list
//
// @syntax  UTStaticList< T >::UTStaticList()
//
// @rdesc   None.
//
//---------------------------------------------------------------------------

template <class T> UTStaticList< T >::UTStaticList(
        void
        )
: m_ulCount(0), m_pFirstLink(NULL), m_pLastLink(NULL)
    {
    // do nothing
    }


//---------------------------------------------------------------------------
// @mfunc   This destructor does nothing
//
// @tcarg class | T | data type to store in the list
//
//---------------------------------------------------------------------------

template <class T> UTStaticList< T >::~UTStaticList(
        void
        )
    {
        //Do nothing
    }

//---------------------------------------------------------------------------
// @mfunc   Reinitializes the list
//
// @tcarg class | T | data type to store in the list
//
//---------------------------------------------------------------------------
template <class T> void UTStaticList< T >::Init (void)
{
    m_ulCount       = 0x0;    
    m_pFirstLink    = 0x0; 
    m_pLastLink     = 0x0;    
}


//---------------------------------------------------------------------------
// @mfunc   This method inserts the new link at the end of the list.
//
// @tcarg class | T | data type to store in the list
//
// @rdesc   None.
//---------------------------------------------------------------------------

template <class T> void UTStaticList< T >::InsertLast(
         UTLink <T> * pLink // @parm [in] link to be inserted at end of list
        )
{

    if (m_pLastLink)    // list is not empty
    {
        pLink->m_pNextLink          = (UTLink<T> *) 0;
        pLink->m_pPrevLink          = m_pLastLink;
        m_pLastLink->m_pNextLink    = pLink;
        m_pLastLink                 = pLink;
    }
    else        // list is empty
    {
        m_pFirstLink = pLink;
        m_pLastLink = pLink;

        pLink->m_pNextLink          = NULL;
        pLink->m_pPrevLink          = NULL;
    }
    
    pLink->m_fQueued = TRUE;
    pLink->m_pQueuedList = this;

    //Increment the count
    m_ulCount++;

} //End UTStaticList::InsertLast





//---------------------------------------------------------------------------
// @mfunc   This method removes the first element from the list and fixes
//          the pointer to the first element according to what remains
//          of the list. The former first element is returned through a
//          CALLER allocated variable.
//
// @tcarg class | T | data type to store in the list
//
// @rdesc   Returns a BOOL
// @flag TRUE   | if first element exists and is deleted
// @flag FALSE  | otherwise
//---------------------------------------------------------------------------

template <class T> BOOL UTStaticList< T >::RemoveFirst(
        UTLink<T> **    ppLink  // @parm [out] location to put first element if available
        )
{
    BOOL            bReturn = FALSE;

    //Assign the out param the first link
    *ppLink     = m_pFirstLink;

    if (m_pFirstLink)
    {
        // Reset the first link
        m_pFirstLink = m_pFirstLink->m_pNextLink;

        if (m_pFirstLink)
            m_pFirstLink->m_pPrevLink = NULL;

        if (m_pLastLink == *ppLink)
            m_pLastLink = NULL;

        bReturn = TRUE;

        m_ulCount--;
    }

    //Remember to clear previous and the next pointers in the link
    if (*ppLink)
    {
    
        (*ppLink)->m_fQueued = FALSE;
        (*ppLink)->m_pQueuedList = NULL;


        (*ppLink)->m_pNextLink = NULL;
        (*ppLink)->m_pPrevLink = NULL;
    }

    return bReturn;
} //End UTStaticList::RemoveFirst



#endif  // __UTLIST_H__
