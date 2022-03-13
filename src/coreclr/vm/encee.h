// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// EnCee.h
//

//
// Defines the core VM data structures and methods for support EditAndContinue
//
// ======================================================================================


#ifndef EnC_H
#define EnC_H

#include "ceeload.h"
#include "field.h"
#include "class.h"

#ifdef EnC_SUPPORTED

class FieldDesc;
struct EnCAddedField;
struct EnCAddedStaticField;
class EnCFieldDesc;
class EnCEEClassData;

typedef DPTR(EnCAddedField) PTR_EnCAddedField;
typedef DPTR(EnCAddedStaticField) PTR_EnCAddedStaticField;
typedef DPTR(EnCFieldDesc) PTR_EnCFieldDesc;
typedef DPTR(EnCEEClassData) PTR_EnCEEClassData;

//---------------------------------------------------------------------------------------
//
// EnCFieldDesc - A field descriptor for fields added by EnC
//
// Notes: We need to track some additional data for added fields, since they can't
// simply be glued onto existing object instances like any other field.
//
// For each field added, there is a single instance of this object tied to the type where
// the field was added.
//
class EnCFieldDesc : public FieldDesc
{
public:
    // Initialize just the bare minimum necessary now.
    // We'll do a proper FieldDesc initialization later when Fixup is called.
    void Init( mdFieldDef token, BOOL fIsStatic);

    // Compute the address of this field for a specific object
    void *GetAddress( void *o);

    // Returns true if Fixup still needs to be called
    BOOL NeedsFixup()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_bNeedsFixup;
    }

    // Used to properly configure the FieldDesc after it has been added
    // This may do things like load classes (which can trigger a GC), and so can only be
    // done after the process has resumed execution.
    VOID Fixup(mdFieldDef token)
    {
        WRAPPER_NO_CONTRACT;
        EEClass::FixupFieldDescForEnC(GetEnclosingMethodTable(), this, token);
        m_bNeedsFixup = FALSE;
    }

    // Gets a pointer to the field's contents (assuming this is a static field) if it's
    // available or NULL otherwise
    EnCAddedStaticField *GetStaticFieldData();

    // Gets a pointer to the field's contents (assuming this is a static field) if it's
    // available or allocates space for it and returns the address to the allocated field
    // Returns a valid address or throws OOM
    EnCAddedStaticField * GetOrAllocateStaticFieldData();


private:
    // True if Fixup() has been called on this instance
    BOOL m_bNeedsFixup;

    // For static fields, pointer to where the field value is held
    PTR_EnCAddedStaticField m_pStaticFieldData;
};

// EnCAddedFieldElement
// A node in the linked list representing fields added to a class with EnC
typedef DPTR(struct EnCAddedFieldElement) PTR_EnCAddedFieldElement;
struct EnCAddedFieldElement
{
    // Pointer to the next element in the list
    PTR_EnCAddedFieldElement m_next;

    // Details about this field
    EnCFieldDesc m_fieldDesc;

    // Initialize this entry.
    // Basically just sets a couple fields to default values.
    // We'll have to go back later and call Fixup on the fieldDesc.
    void Init(mdFieldDef token, BOOL fIsStatic)
    {
        WRAPPER_NO_CONTRACT;
        m_next = NULL;
        m_fieldDesc.Init(token, fIsStatic);
    }
};

//---------------------------------------------------------------------------------------
//
// EnCEEClassData - EnC specific information about this class
//
class EnCEEClassData
{
public:
#ifndef DACCESS_COMPILE
    // Initialize all the members
    //  pClass - the EEClass we're tracking EnC data for
    void Init(MethodTable * pMT)
    {
        LIMITED_METHOD_CONTRACT;
        m_pMT = pMT;
        m_dwNumAddedInstanceFields = 0;
        m_dwNumAddedStaticFields = 0;
        m_pAddedInstanceFields = NULL;
        m_pAddedStaticFields = NULL;
    }
#endif

    // Adds the provided new field to the appropriate linked list and updates the appropriate count
    void AddField(EnCAddedFieldElement *pAddedField);

    // Get the number of instance fields that have been added to this class.
    // Since we can only add private fields, these fields can't be seen from any other class but this one.
    int GetAddedInstanceFields()
    {
        SUPPORTS_DAC;
        return m_dwNumAddedInstanceFields;
    }

    // Get the number of static fields that have been added to this class.
    int GetAddedStaticFields()
    {
        SUPPORTS_DAC;
        return m_dwNumAddedStaticFields;
    }

    // Get the methodtable that this EnC data refers to
    MethodTable * GetMethodTable()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pMT;
    }

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

private:
    friend class EEClass;
    friend class EncApproxFieldDescIterator;

    // The class that this EnC data refers to
    PTR_MethodTable       m_pMT;

    // The number of instance fields that have been added to this class
    int                   m_dwNumAddedInstanceFields;

    // The number of static fields that have been added to this class
    int                   m_dwNumAddedStaticFields;

    // Linked list of EnCFieldDescs for all the added instance fields
    PTR_EnCAddedFieldElement m_pAddedInstanceFields;

    // Linked list of EnCFieldDescs for all the added static fields
    PTR_EnCAddedFieldElement m_pAddedStaticFields;
};

//---------------------------------------------------------------------------------------
//
// EditAndContinueModule - specialization of the Module class which adds EnC support
//
// Assumptions:
//
// Notes:
//
class EditAndContinueModule : public Module
{
    VPTR_VTABLE_CLASS(EditAndContinueModule, Module)

    // keep track of the number of changes - this is used to apply a version number
    // to an updated function. The version number for a function is the overall edit count,
    // ie the number of times ApplyChanges has been called, not the number of times that
    // function itself has been edited.
    int m_applyChangesCount;

    // Holds a table of EnCEEClassData object for classes in this module that have been modified
    CUnorderedArray<EnCEEClassData*, 5> m_ClassList;

#ifndef DACCESS_COMPILE
    // Return the minimum permissable address for new IL to be stored at
    // This can't be less than the current load address because then we'd
    // have negative RVAs.
    BYTE *GetEnCBase() { return (BYTE *) GetPEAssembly()->GetManagedFileContents(); }
#endif // DACCESS_COMPILE

private:
    // Constructor is invoked only by Module::Create
    friend Module *Module::Create(Assembly *pAssembly, mdToken moduleRef, PEAssembly *pPEAssembly, AllocMemTracker *pamTracker);
    EditAndContinueModule(Assembly *pAssembly, mdToken moduleRef, PEAssembly *pPEAssembly);

protected:
#ifndef DACCESS_COMPILE
    // Initialize the module
    virtual void Initialize(AllocMemTracker *pamTracker, LPCWSTR szName = NULL);
#endif

public:
#ifndef DACCESS_COMPILE
    // Destruct the module when it's finished being unloaded
    // Note that due to the loader's allocation mechanism, C++ consturctors and destructors
    // wouldn't be called.
    virtual void Destruct();
#endif

    virtual BOOL IsEditAndContinueCapable() const { return TRUE; }

    // Apply an EnC edit
    HRESULT ApplyEditAndContinue(DWORD cbMetadata,
                            BYTE *pMetadata,
                            DWORD cbIL,
                            BYTE *pIL);

    // Called when a method has been modified (new IL)
    HRESULT UpdateMethod(MethodDesc *pMethod);

    // Called when a new method has been added to the module's metadata
    HRESULT AddMethod(mdMethodDef token);

    // Called when a new field has been added to the module's metadata
    HRESULT AddField(mdFieldDef token);

    // JIT the new version of a function for EnC
    PCODE JitUpdatedFunction(MethodDesc *pMD, T_CONTEXT *pContext);

    // Remap execution to the latest version of an edited method
    HRESULT ResumeInUpdatedFunction(MethodDesc *pMD,
                                    void *oldDebuggerFuncHandle,
                                    SIZE_T newILOffset,
                                    T_CONTEXT *pContext);

    // Modify the thread context for EnC remap and resume execution
    void FixContextAndResume(MethodDesc *pMD,
                             void *oldDebuggerFuncHandle,
                             T_CONTEXT *pContext,
                             EECodeInfo *pOldCodeInfo,
                             EECodeInfo *pNewCodeInfo);

    // Get a pointer to the value of a field added by EnC or return NULL if it doesn't exist
    PTR_CBYTE ResolveField(OBJECTREF thisPointer,
                           EnCFieldDesc *pFD);

    // Get a pointer to the value of a field added by EnC. Allocates if it doesn't exist, so we'll
    // return a valid address or throw OOM
    PTR_CBYTE ResolveOrAllocateField(OBJECTREF      thisPointer,
                                     EnCFieldDesc * pFD);


    // Get class-specific EnC data for a class in this module
    // Note: For DAC build, getOnly must be TRUE
    PTR_EnCEEClassData GetEnCEEClassData(MethodTable * pMT, BOOL getOnly = FALSE);

    // Get the number of times edits have been applied to this module
    int GetApplyChangesCount()
    {
        return m_applyChangesCount;
    }

#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags,
                                   bool enumThis);
#endif
};

// Information about an instance field value added by EnC
// When an instance field is added to an object, we will lazily create an EnCAddedField
// for EACH instance of that object, but there will be a single EnCFieldDesc.
//
// Note that if we were concerned about the overhead when there are lots of instances of
// an object, we could slim this down to just the m_FieldData field by storing a pointer
// to a growable array of these in the EnCSyncBlockInfo, instead of using a linked list, and
// have the EnCFieldDesc specify a field index number.
//
struct EnCAddedField
{
    // This field data hangs off the SyncBlock in a linked list.
    // This is the pointer to the next field in the list.
    PTR_EnCAddedField m_pNext;

    // Pointer to the fieldDesc describing which field this refers to
    PTR_EnCFieldDesc m_pFieldDesc;

    // A dependent handle whose primary object points to the object instance which has been modified,
    // and whose secondary object points to an EnC helper object containing a reference to the field value.
    OBJECTHANDLE m_FieldData;

    // Allocates a new EnCAddedField and hook it up to the object
    static EnCAddedField *Allocate(OBJECTREF thisPointer, EnCFieldDesc *pFD);
};

// Information about a static field value added by EnC
// We can't change the MethodTable, so these are hung off the FieldDesc
// Note that the actual size of this type is variable.
struct EnCAddedStaticField
{
    // Pointer back to the fieldDesc describing which field this refers to
    // This isn't strictly necessary since our callers always know it, but the overhead
    // in minimal (per type, not per instance) and this is cleaner and permits an extra sanity check.
    PTR_EnCFieldDesc m_pFieldDesc;

    // For primitive types, this is the beginning of the actual value.
    // For reference types and user-defined value types, it's the beginning of a pointer
    // to the object.
    // Note that this is intentionally the last field of this structure as it is variably-sized.
    // NOTE: It looks like we did the same thing for instance fields in EnCAddedField but then simplified
    // it by always storing just an OBJREF which may point to a boxed value type.  I suggest we do the
    // same here unless we can demonstrate that the extra indirection makes a noticable perf difference
    // in scenarios which are important for EnC.
    BYTE m_FieldData;

    // Get a pointer to the contents of this field
    PTR_CBYTE GetFieldData();

    // Allocate a new instance appropriate for the specified field
    static EnCAddedStaticField *Allocate(EnCFieldDesc *pFD);
};

// EnCSyncBlockInfo lives off an object's SyncBlock and contains a lazily-created linked
// list of the values of all the fields added to the object by EnC
//
// Note that much of the logic here would probably belong better in EnCAddedField since it is
// specific to the implementation there.  Perhaps this should ideally just be a container
// that holds a bunch of EnCAddedFields and can iterate over them and map from EnCFieldDesc
// to them.
class EnCSyncBlockInfo
{
public:
    // Initialize the list
    EnCSyncBlockInfo() :
        m_pList(PTR_NULL)
    {
    }

    // Get a pointer to the data in a specific field on this object or return NULL if it
    // doesn't exist
    PTR_CBYTE ResolveField(OBJECTREF      thisPointer,
                           EnCFieldDesc * pFieldDesc);

    // Get a pointer to the data in a specific field on this object. We'll allocate if it doesn't already
    // exist, so we'll only fail on OOM
    PTR_CBYTE ResolveOrAllocateField(OBJECTREF thisPointer, EnCFieldDesc *pFD);


    // Free the data used by this field value.  Called after the object instance the
    // fields belong to is collected.
    void Cleanup();

private:
    // Gets the address of an EnC field accounting for its type: valuetype, class or primitive
    PTR_CBYTE GetEnCFieldAddrFromHelperFieldDesc(FieldDesc *    pHelperFieldDesc,
                                                 OBJECTREF      pHelper,
                                                 EnCFieldDesc * pFD);

    // Pointer to the head of the list
    PTR_EnCAddedField m_pList;
};

// The DPTR is actually defined in syncblk.h to make it visible to SyncBlock
// typedef DPTR(EnCSyncBlockInfo) PTR_EnCSyncBlockInfo;

#endif // !EnC_SUPPORTED


//---------------------------------------------------------------------------------------
//
// EncApproxFieldDescIterator - Iterates through all fields of a class including ones
//  added by EnC
//
// Notes:
//    This is just like ApproxFieldDescIterator, but it also includes EnC fields if
//    EnC is supported.
//    This does not include inherited fields.
//    The order the fields returned here is unspecified.
//
//    We don't bother maintaining an accurate total and remaining field count like
//    ApproxFieldDescIterator because none of our clients need it.  But it would
//    be easy to add this using the data from m_classData
//
class EncApproxFieldDescIterator
{
public:
#ifdef EnC_SUPPORTED
    // Create and initialize the iterator
    EncApproxFieldDescIterator(MethodTable *pMT, int iteratorType, BOOL fixupEnC);

    // Get the next fieldDesc (either EnC or non-EnC)
    PTR_FieldDesc Next();

    int Count();
#else
    // Non-EnC version - simple wrapper
    EncApproxFieldDescIterator(MethodTable *pMT, int iteratorType, BOOL fixupEnC) :
      m_nonEnCIter( pMT, iteratorType ) {}

    PTR_FieldDesc Next() { WRAPPER_NO_CONTRACT; return m_nonEnCIter.Next(); }

    int Count() { WRAPPER_NO_CONTRACT; return m_nonEnCIter.Count(); }
#endif // EnC_SUPPORTED

    int GetIteratorType()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_nonEnCIter.GetIteratorType();
    }

private:
    // The iterator for the non-EnC fields.
    // We delegate to this for alll non-EnC specific stuff
    ApproxFieldDescIterator m_nonEnCIter;

#ifdef EnC_SUPPORTED
    // Return the next available EnC FieldDesc or NULL when done
    PTR_EnCFieldDesc NextEnC();

    // True if our client wants us to fixup any EnC fieldDescs before handing them back
    BOOL m_fixupEnC;

    // A count of how many EnC fields have been returned so far
    int m_encFieldsReturned;

    // The current pointer into one of the EnC field lists when enumerating EnC fields
    PTR_EnCAddedFieldElement m_pCurrListElem;

    // EnC specific data for the class of interest.
    // NULL if EnC is disabled or this class doesn't have any EnC data
    PTR_EnCEEClassData m_encClassData;
#endif
};

#endif // #ifndef EnC_H
