// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "strike.h"
#include "util.h"

#ifndef SOS_Assert
#ifdef _DEBUG
#define SOS_Assert(x) do { if (!(x)) sos::Throw<sos::Exception>("SOS Assert Failure: %s\n", #x); } while(0)
#else
#define SOS_Assert(x) (void)0
#endif
#endif

#ifdef throw
#undef throw
#endif

#ifdef try
#undef try
#endif

#ifdef catch
#undef catch
#endif

class LinearReadCache;
class CGCDesc;
class CGCDescSeries;

namespace sos
{
    class GCHeap;

    /* The base SOS Exception.  Note that most commands should not attempt to be
     * resilient to exceptions thrown by most functions here.  Instead a top level
     * try/catch at the beginning of the command which prints out the exception's
     * message should be sufficient.
     * Note you should not throw these directly, instead use the sos::Throw function.
     */
    class Exception
    {
    public:
        Exception(const char *format, va_list args)
        {
            vsprintf_s(mMsg, _countof(mMsg), format, args);

            va_end(args);
        }

        inline virtual ~Exception() {}

        // from std::exception
        virtual const char *what() const
        {
            return mMsg;
        }

        const char *GetMesssage() const
        {
            return mMsg;
        }

    protected:
        char mMsg[1024];
    };

    /* Thrown when we could not read data we expected out of the target process.
     * This can be due to heap corruption, or it could just be an invalid pointer.
     */
    class DataRead : public Exception
    {
    public:
        DataRead(const char *format, va_list args)
            : Exception(format, args)
        {
        }
    };

    /* This is thrown when we detect heap corruption in the process.
     */
    class HeapCorruption : public Exception
    {
    public:
        HeapCorruption(const char *format, va_list args)
            : Exception(format, args)
        {
        }
    };

    // Internal helper method.  Use SOS_Throw macros instead.
    template <class T>
    void Throw(const char *format, ...)
    {
        va_list args;
        va_start(args, format);

        throw T(format, args);
    }

    /* Checks to see if the user hit control-c.  Throws an exception to escape SOS
     * if so.
     */
    inline void CheckInterrupt()
    {
        if (g_ExtControl->GetInterrupt() == S_OK)
            Throw<Exception>("User interrupt.");
    }

    /* ThinLock struct.  Use Object::GetThinLock to fill the struct.
     */
    struct ThinLockInfo
    {
        int ThreadId;
        TADDR ThreadPtr;
        int Recursion;

        ThinLockInfo()
            : ThreadId(0), ThreadPtr(0), Recursion(0)
        {
        }
    };

    /* The MethodTable for an Object.  The general pattern should be:
     *   MethodTable mt = someObject.GetMT();
     */
    class MethodTable
    {
    public:
        /* Returns whether an object is from an AppDomain that has been unloaded.
         * If so, we cannot validate the object's members.
         * Params:
         *   mt - The address of the MethodTable to test for.
         */
        static bool IsZombie(TADDR mt);
        
        /* Returns the method table for arrays.
         */
        inline static TADDR GetArrayMT()
        {
            return TO_TADDR(g_special_usefulGlobals.ArrayMethodTable);
        }

        /* Returns the method table for String objects.
         */
        inline static TADDR GetStringMT()
        {
            return TO_TADDR(g_special_usefulGlobals.StringMethodTable);
        }

        /* Returns the method table for Free objects.
         */
        inline static TADDR GetFreeMT()
        {
            return TO_TADDR(g_special_usefulGlobals.FreeMethodTable);
        }

        /* Returns true if the given method table is that of a Free object.
         */
        inline static bool IsFreeMT(TADDR mt)
        {
            return GetFreeMT() == mt;
        }
        
        /* Returns true if the given method table is that of an Array.
         */
        inline static bool IsArrayMT(TADDR mt)
        {
            return GetArrayMT() == mt;
        }

        /* Returns true if the given method table is that of a System.String object.
         */
        inline static bool IsStringMT(TADDR mt)
        {
            return GetStringMT() == mt;
        }
        
        inline static bool IsValid(TADDR mt)
        {
            DacpMethodTableData data;
            return data.Request(g_sos, TO_CDADDR(mt)) == S_OK;
        }

    public:
        MethodTable(TADDR mt)
            : mMT(mt), mName(0)
        {
        }
        
        MethodTable(const MethodTable &mt)
            : mMT(mt.mMT), mName(mt.mName)
        {
            // Acquire the calculated mName field.  Since we are making a copy, we will likely use
            // the copy instead of the original.
            mt.mName = NULL;
        }
        
        const MethodTable &operator=(const MethodTable &mt)
        {
            Clear();

            // Acquire the calculated mName field.  Since we are making a copy, we will likely use
            // the copy instead of the original.
            mMT = mt.mMT;
            mName = mt.mName;
            mt.mName = NULL;
            
            return *this;
        }
        
        ~MethodTable()
        {
            Clear();
        }

        /* Returns the class name of this MethodTable.  The pointer returned is
         * valid through the lifetime of the MethodTable object and should not be
         * freed.
         */
        const WCHAR *GetName() const;

    private:
        void Clear();

    private:
        TADDR mMT;
        mutable WCHAR *mName;
    };

    /* This represents an object on the GC heap in the target process.  This class
     * represents a single object, and is immutable after construction.  All
     * information about this class is lazily evaluated, so it is entirely possible
     * to get exceptions when calling any member function.  If this is a concern,
     * call validate before attempting to call any other method on this object.
     */
    class Object
    {
    public:
        /* Attempts to determine if the target address points to a valid object.
         * Note that this is a heuristic based check, so false positives could
         * be possible.
         * Params:
         *   address - The address of the object to inspect.
         *   verifyFields - Whether or not to validate that the fields the object
         *                  points to are also valid.  (If the object contains a
         *                  corrupted pointer, passing true to this parameter will
         *                  cause IsValid to return false.)  In general passing
         *                  true will make IsValid return less false positives.
         */
        static bool IsValid(TADDR address, bool verifyFields=false);
        
        static int GetStringDataOffset()
        {
#ifndef _TARGET_WIN64_
            return 8;
#else
            return 0xc;
#endif
        }

    public:
        /* Constructor.  Use Object(TADDR, TADDR) instead if you know the method table.
         * Parameters:
         *   addr - an address to an object on the managed heap
         * Throws:
         *   Exception - if addr is misaligned.
         */
        Object(TADDR addr);

        /* Constructor.  Use this constructor if you already know the method table for
         * the object in question.  This will save a read if the method table is needed.
         * Parameters:
         *   addr - an address to an object on the managed heap
         * Throws:
         *   Exception - if addr is misaligned.
         */
        Object(TADDR addr, TADDR mt);
        
        Object(const Object &rhs);

        inline ~Object()
        {
            if (mMTData)
                delete mMTData;
                
            if (mTypeName)
                delete mTypeName;
        }

        const Object &operator=(TADDR addr);

        // Comparison operators.  These compare the underlying address of
        // the object to the parameter.
        inline bool operator<=(TADDR addr) { return mAddress <= addr; }
        inline bool operator>=(TADDR addr) { return mAddress >= addr; }
        inline bool operator<(TADDR addr)  { return mAddress < addr; }
        inline bool operator>(TADDR addr)  { return mAddress > addr; }
        inline bool operator==(TADDR addr)  { return mAddress == addr; }

        /* Returns the target address of the object this represents.
         */
        inline TADDR GetAddress() const
        {
            return mAddress;
        }

        /* Returns the target address of the object this represents.
         */
        inline operator TADDR() const
        {
            return GetAddress();
        }

        /* Returns the object header for this object.
         * Throws:
         *   DataRead - we failed to read the object header.
         */
        ULONG GetHeader() const;
        
        /* Gets the header for the current object, does not throw any exception.
         * Params:
         *   outHeader - filled with the header if this function was successful.
         * Returns:
         *   True if we successfully read the object header, false otherwise.
         */
        bool TryGetHeader(ULONG &outHeader) const;
        
        /* Returns the method table of the object this represents.
         * Throws:
         *   DataRead - If we failed to read the method table from the address.
         *              This is usually indicative of heap corruption.
         *   HeapCorruption - If we successfully read the target method table
         *                    but it is invalid.  (We do not do a very deep
         *                    verification here.)
         */
        TADDR GetMT() const;
        
        /* Returns the component method table of the object.  For example, if
         * this object is an array, the method table will be the general array
         * MT.  Calling this function tells you what type of objects can be
         * placed in the array.
         * Throws:
         *   DataRead - If we failed to read the method table from the address.
         *              This is usually indicative of heap corruption.
         *   HeapCorruption - If we successfully read the target method table
         *                    but it is invalid.  (We do not do a very deep
         *                    verification here.)
         */
        TADDR GetComponentMT() const;

        /* Returns the size of the object this represents.  Note that this size
         * may not be pointer aligned.
         * Throws:
         *   DataRead - If we failed to read the method table data (which contains
         *              the size of the object).
         */
        size_t GetSize() const;

        /* Returns true if this object contains pointers to other objects.
         * Throws:
         *   DataRead - if we failed to read out of the object's method table.
         */
        bool HasPointers() const;

        /* Gets the thinlock information for this object.
         * Params:
         *   out - The ThinLockInfo to be filled.
         * Returns:
         *   True if the object has a thinlock, false otherwise.  If this function
         *   returns false, then out will be untouched.
         * Throws:
         *   DataRead - If we could not read the object header from the object.
         */
        bool GetThinLock(ThinLockInfo &out) const;

        /* Returns true if this object is a Free object (meaning it points to free
         * space in the GC heap.
         * Throws:
         *   The same as GetMT().
         */
        inline bool IsFree() const
        {
            return GetMT() == MethodTable::GetFreeMT();
        }

        /* Returns true if this object is a string.
         * Throws:
         *   The same as GetMT().
         */
        inline bool IsString() const
        {
            return GetMT() == MethodTable::GetStringMT();
        }

        /* Returns the length of the String, if this is a string object.  This
         * function assumes that you have called IsString first to ensure that
         * the object is indeed a string.
         * Throws:
         *    DataRead if we could not read the contents of the object.
         */
        size_t GetStringLength() const;

        /* Fills the given buffer with the contents of the String.  This
         * function assumes you have called IsString first to ensure that this
         * object is actually a System.String.  This function does not throw,
         * but the results are undefined if this object is not a string.
         * Params:
         *   buffer - The buffer to fill with the string contents.
         *   size - The total size of the buffer.
         * Returns:
         *   True if the string data was successfully requested and placed in
         *   buffer, false otherwise.
         */
        bool GetStringData(__out_ecount(size) WCHAR *buffer, size_t size) const;

        /* Returns the name of the type of this object.  E.g. System.String.
         * Throws:
         *    DataRead if we could not read the contents of the object.
         * Returns:
         *    A string containing the type of the object.
         */
        const WCHAR *GetTypeName() const;

    private:
        void FillMTData() const;
        void CalculateSizeAndPointers() const;
        static bool VerifyMemberFields(TADDR pMT, TADDR obj);
        static bool VerifyMemberFields(TADDR pMT, TADDR obj, WORD &numInstanceFields);

    protected:
        // Conceptually, this class is never modified after you pass in the the object address.
        // That is, there can't be anything the user does to point this object to a different
        // object after construction.  Since we lazy evaluate *everything*, we must be able to
        // modify these variables.  Hence they are mutable.
        TADDR mAddress;
        mutable TADDR mMT;
        mutable size_t mSize;
        mutable bool mPointers;
        mutable DacpMethodTableData *mMTData;
        mutable WCHAR *mTypeName;
    };

    /* Enumerates all the GC references (objects) contained in an object.  This uses the GCDesc
     * map exactly as the GC does.
     */
    class RefIterator
    {
    public:
        RefIterator(TADDR obj, LinearReadCache *cache = NULL);
        RefIterator(TADDR obj, CGCDesc *desc, bool arrayOfVC, LinearReadCache *cache = NULL);
        ~RefIterator();
    
        /* Moves to the next reference in the object.
         */
        const RefIterator &operator++();
        
        /* Returns the address of the current reference.
         */
        TADDR operator*() const;
        
        /* Gets the offset into the object where the current reference comes from.
         */
        TADDR GetOffset() const;
        
        /* Returns true if there are more objects in the iteration, false otherwise.
         * Used as:
         *     if (itr)
         *        ...
         */
        inline operator void *() const
        {
            return (void*)!mDone;
        }

        bool IsLoaderAllocator() const
        {
            return mLoaderAllocatorObjectHandle == mCurr;
        }
        
    private:
        void Init();
        inline TADDR ReadPointer(TADDR addr) const
        {
            if (mCache)
            {
                if (!mCache->Read(addr, &addr, false))
                    Throw<DataRead>("Could not read address %p.", addr);
            }
            else
            {
                MOVE(addr, addr);
            }
            
            return addr;
        }
        
    private:
        LinearReadCache *mCache;
        CGCDesc *mGCDesc;
        bool mArrayOfVC, mDone;
        
        TADDR *mBuffer;
        CGCDescSeries *mCurrSeries;
        
        TADDR mLoaderAllocatorObjectHandle;

        int i, mCount;
        
        TADDR mCurr, mStop, mObject;
        size_t mObjSize;
    };


    /* The Iterator used to walk the managed objects on the GC heap.
     * The general usage pattern for this class is:
     *   for (ObjectIterator itr = gcheap.WalkHeap(); itr; ++itr)
     *     itr->SomeObjectMethod();
     */
    class ObjectIterator
    {
        friend class GCHeap;
    public:

        /* Returns the next object in the GCHeap.  Note that you must ensure
         * that there are more objects to walk before calling this function by
         * checking "if (iterator)".  If this function throws an exception,
         * the the iterator is invalid, and should no longer be used to walk
         * the heap.  This should generally only happen if we cannot read the
         * MethodTable of the object to move to the next object.
         * Throws:
         *   DataRead
         */
        const ObjectIterator &operator++();

        /* Dereference operator.  This allows you to take a reference to the
         * current object.  Note the lifetime of this reference is valid for
         * either the lifetime of the iterator or until you call operator++,
         * whichever is shorter.  For example.
         *   void Foo(const Object &param);
         *   void Bar(const ObjectIterator &itr)
         *   {
         *      Foo(*itr);
         *   }
         */
        const Object &operator*() const;

        /* Returns a pointer to the current Object to call members on it.
         * The usage pattern for the iterator is to simply use operator->
         * to call methods on the Object it points to without taking a
         * direct reference to the underlying Object if at all possible.
         */
        const Object *operator->() const;

        /* Returns false when the iterator has reached the end of the managed
         * heap.
         */
        inline operator void *() const
        {
            return (void*)(SIZE_T)(mCurrHeap == mNumHeaps ? 0 : 1);
        }

        /* Do not use.
         * TODO: Replace this functionality with int Object::GetGeneration().
         */
        bool IsCurrObjectOnLOH() const
        {
            SOS_Assert(*this);
            return bLarge;
        }

        /* Verifies the current object.  Returns true if the current object is valid.
         * Returns false and fills 'buffer' with the reason the object is corrupted.
         * This is a deeper validation than Object::IsValid as it checks the card
         * table entires for the object in addition to the rest of the references.
         * This function does not throw exceptions.
         * Params:
         *   buffer - out buffer that is filled if and only if this function returns
         *            false.
         *   size - the total size of the buffer
         * Returns:
         *   True if the object is valid, false otherwise.
         */
        bool Verify(__out_ecount(size) char *buffer, size_t size) const;

        /* The same as Verify(char*, size_t), except it does not write out the failure
         * reason to a provided buffer.
         * See:
         *   ObjectIterator::Verify(char *, size_t)
         */
        bool Verify() const;

        /* Attempts to move to the next object (similar to ObjectIterator++), but
         * attempts to recover from any heap corruption by skipping to the next
         * segment.  If Verify returns false, meaning it detected heap corruption
         * at the current object, you can use MoveToNextObjectCarefully instead of
         * ObjectIterator++ to attempt to keep reading from the heap.  If possible,
         * this function attempts to move to the next object in the same segment,
         * but if that's not possible then it skips to the next segment and
         * continues from there.
         * Note:
         *   This function can throw, and if it does then the iterator is no longer
         *   in a valid state.  No further attempts to move to the next object will
         *   be possible.
         * Throws:
         *   DataRead - if the heap is corrupted and it's not possible to continue
         *              walking the heap
         */
        void MoveToNextObjectCarefully();

    private:
        ObjectIterator(const DacpGcHeapDetails *heap, int numHeaps, TADDR start, TADDR stop);

        bool VerifyObjectMembers(__out_ecount(size) char *buffer, size_t size) const;
        void BuildError(__out_ecount(count) char *out, size_t count, const char *format, ...) const;

        void AssertSanity() const;
        bool NextSegment();
        bool CheckSegmentRange();
        void MoveToNextObject();

    private:
        DacpHeapSegmentData mSegment;
        bool bLarge;
        Object mCurrObj;
        TADDR mLastObj, mStart, mEnd, mSegmentEnd;
        AllocInfo mAllocInfo;
        const DacpGcHeapDetails *mHeaps;
        int mNumHeaps;
        int mCurrHeap;
    };

    /* Reprensents an entry in the sync block table.
     */
    class SyncBlk
    {
        friend class SyncBlkIterator;
    public:
        /* Constructor.
         * Params:
         *   index - the index of the syncblk entry you wish to inspect.
         *           This should be in range [1, MaxEntries], but in general
         *           you should always use the SyncBlk iterator off of GCHeap
         *           and not construct these directly.
         * Throws:
         *   DataRead - if we could not read the syncblk entry for the given index.
         */
        explicit SyncBlk(int index);
        
        /* Returns whether or not the current entry is a "Free" SyncBlk table entry
         * or not.  This should be called *before* any other function here.
         */
        bool IsFree() const;

        /* Returns the address of this syncblk entry (generally for display purposes).
         */
        TADDR GetAddress() const;

        /* Returns the address of the object which this is syncblk is pointing to.
         */
        TADDR GetObject() const;

        /* Returns the index of this entry.
         */
        int GetIndex() const;

        /* Returns the COMFlags for the SyncBlk object.  The return value of this
         * function is undefined if FEATURE_COMINTEROP is not defined, so you should
         * #ifdef the calling region yourself.
         */
        DWORD GetCOMFlags() const;

        unsigned int GetMonitorHeldCount() const;
        unsigned int GetRecursion() const;
        unsigned int GetAdditionalThreadCount() const;

        /* Returns the thread which holds this monitor (this is the clr!Thread object).
         */
        TADDR GetHoldingThread() const;
        TADDR GetAppDomain() const;

    private:
        /* Copy constructor unimplemented due to how expensive this is.  Use references
         * instead.
         */
        SyncBlk(const SyncBlk &rhs);
        SyncBlk();
        void Init();
        const SyncBlk &operator=(int index);

    private:
        int mIndex;
        DacpSyncBlockData mData;
    };

    /* An iterator over syncblks.  The common usage for this class is:
     *   for (SyncBlkIterator itr; itr; ++itr)
     *       itr->SomeSyncBlkFunction();
     */
    class SyncBlkIterator
    {
    public:
        SyncBlkIterator();

        /* Moves to the next SyncBlk in the table.
         */
        inline const SyncBlkIterator &operator++()
        {
            SOS_Assert(mCurr <= mTotal);
            mSyncBlk = ++mCurr;

            return *this;
        }

        inline const SyncBlk &operator*() const
        {
            SOS_Assert(mCurr <= mTotal);
            return mSyncBlk;
        }

        inline const SyncBlk *operator->() const
        {
            SOS_Assert(mCurr <= mTotal);
            return &mSyncBlk;
        }

        inline operator void *() const
        {
            return (void*)(SIZE_T)(mCurr <= mTotal ? 1 : 0);
        }

    private:
        int mCurr, mTotal;
        SyncBlk mSyncBlk;
    };
    
    /* An class which contains information about the GCHeap.
     */
    class GCHeap
    {
    public:
        static const TADDR HeapStart;  // A constant signifying the start of the GC heap.
        static const TADDR HeapEnd;    // A constant signifying the end of the GC heap.

    public:
        /* Constructor.
         * Throws:
         *   DataRead
         */
        GCHeap();

        /* Returns an ObjectIterator which allows you to walk the objects on the managed heap.
         * This ObjectIterator is valid for the duration of the GCHeap's lifetime.  Note that
         * if you specify an address at which you wish to start walking the heap it need
         * not point directly to a managed object.  However, if it does not, WalkHeap
         * will need to walk the segment that address resides in to find the first object
         * after that address, and if it encounters any heap corruption along the way,
         * it may be impossible to walk the heap from the address specified.
         *
         * Params:
         *   start - The starting address at which you want to start walking the heap.
         *           This need not point directly to an object on the heap.
         *   end - The ending address at which you want to stop walking the heap.  This
         *         need not point directly to an object on the heap.
         *   validate - Whether or not you wish to validate the GC heap as you walk it.
         * Throws:
         *   DataRead
         */
        ObjectIterator WalkHeap(TADDR start = HeapStart, TADDR stop = HeapEnd) const;

        /* Returns true if the GC Heap structures are in a valid state for traversal.
         * Returns false if not (e.g. if we are in the middle of a relocation).
         */
        bool AreGCStructuresValid() const;

    private:
        DacpGcHeapDetails *mHeaps;
        DacpGcHeapData mHeapData;
        int mNumHeaps;
    };

    // convenience functions
    /* A temporary wrapper function for Object::IsValid.  There are too many locations
     * in SOS which need to use IsObject but have a wide variety of internal
     * representations for an object address.  Until it can all be unified as TADDR,
     * this is what they will use.
     */
    template <class T>
    bool IsObject(T addr, bool verifyFields=false)
    {
        return Object::IsValid(TO_TADDR(addr), verifyFields);
    }
    
    
    void BuildTypeWithExtraInfo(TADDR addr, unsigned int size, __inout_ecount(size) WCHAR *buffer);
}
