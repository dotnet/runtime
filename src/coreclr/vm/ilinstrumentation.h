// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: ILInstrumentation.h
//
// ===========================================================================



#ifndef IL_INSTRUMENTATION_H
#define IL_INSTRUMENTATION_H

#include "cdacdata.h"

// declare an array type of COR_IL_MAP entries
typedef ArrayDPTR(COR_IL_MAP) ARRAY_PTR_COR_IL_MAP;

//---------------------------------------------------------------------------------------
//
// A profiler may instrument a method by changing the IL.  This is typically done when the profiler receives
// a JITCompilationStarted notification.  The profiler also has the option to provide the runtime with
// a mapping between original IL offsets and instrumented IL offsets.  This struct is a simple container
// for storing the mapping information.  We store the mapping information on the Module class, where it can
// be accessed by the debugger from out-of-process.
//

class InstrumentedILOffsetMapping
{
public:
    InstrumentedILOffsetMapping();

    // Check whether there is any mapping information stored in this object.
    BOOL IsNull() const;

#if !defined(DACCESS_COMPILE)
    // Release the memory used by the array of COR_IL_MAPs.
    void Clear();

    void SetMappingInfo(UINT cMap, COR_IL_MAP * rgMap);
#endif // !DACCESS_COMPILE

    UINT                 GetCount()   const;
    ARRAY_PTR_COR_IL_MAP GetOffsets() const;

private:
    UINT                 m_cMap;        // the number of elements in m_rgMap
    ARRAY_PTR_COR_IL_MAP m_rgMap;       // an array of COR_IL_MAPs

    friend struct ::cdac_data<InstrumentedILOffsetMapping>;
};

template<>
struct cdac_data<InstrumentedILOffsetMapping>
{
    static constexpr size_t Count = offsetof(InstrumentedILOffsetMapping, m_cMap);
    static constexpr size_t Map = offsetof(InstrumentedILOffsetMapping, m_rgMap);
};

#if !defined(DACCESS_COMPILE)
//---------------------------------------------------------------------------------------
//
// Owns a mapping returned by Module::GetInstrumentedILOffsetMapping and frees its array when
// it leaves scope. Only for the Module table's map: the ReJIT per-version map lives on the
// loader heap and must not be freed.
//
class InstrumentedILOffsetMappingHolder
{
public:
    InstrumentedILOffsetMappingHolder() = default;

    explicit InstrumentedILOffsetMappingHolder(const InstrumentedILOffsetMapping & mapping)
        : m_mapping(mapping)
    {
        LIMITED_METHOD_CONTRACT;
    }

    ~InstrumentedILOffsetMappingHolder()
    {
        LIMITED_METHOD_CONTRACT;
        m_mapping.Clear();
    }

    InstrumentedILOffsetMappingHolder(const InstrumentedILOffsetMappingHolder &) = delete;
    InstrumentedILOffsetMappingHolder & operator=(const InstrumentedILOffsetMappingHolder &) = delete;

    InstrumentedILOffsetMappingHolder(InstrumentedILOffsetMappingHolder && other)
        : m_mapping(other.m_mapping)
    {
        LIMITED_METHOD_CONTRACT;
        other.m_mapping = InstrumentedILOffsetMapping();
    }

    InstrumentedILOffsetMappingHolder & operator=(InstrumentedILOffsetMappingHolder && other)
    {
        LIMITED_METHOD_CONTRACT;
        if (this != &other)
        {
            m_mapping.Clear();
            m_mapping = other.m_mapping;
            other.m_mapping = InstrumentedILOffsetMapping();
        }
        return *this;
    }

    // Takes ownership of a fresh mapping from the getter, freeing any array already held.
    InstrumentedILOffsetMappingHolder & operator=(const InstrumentedILOffsetMapping & mapping)
    {
        LIMITED_METHOD_CONTRACT;
        if (&mapping != &m_mapping)
        {
            m_mapping.Clear();
            m_mapping = mapping;
        }
        return *this;
    }

    const InstrumentedILOffsetMapping & Get() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_mapping;
    }

private:
    InstrumentedILOffsetMapping m_mapping;
};
#endif // !DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
// Hash table entry for storing InstrumentedILOffsetMapping.  This is keyed by the MethodDef token.
//

struct ILOffsetMappingEntry
{
    ILOffsetMappingEntry()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        m_methodToken = mdMethodDefNil;
        // No need to initialize m_mapping.  The default ctor of InstrumentedILOffsetMapping does the job.
    }

    ILOffsetMappingEntry(mdMethodDef token, InstrumentedILOffsetMapping mapping)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        m_methodToken = token;
        m_mapping = mapping;
    }

    mdMethodDef                 m_methodToken;
    InstrumentedILOffsetMapping m_mapping;
};

//---------------------------------------------------------------------------------------
//
// This class is used to create the hash table for the instrumented IL offset mapping.
// It encapsulates the desired behaviour of the templated hash table and implements
// the various functions needed by the hash table.
//

class ILOffsetMappingTraits : public NoRemoveSHashTraits<DefaultSHashTraits<ILOffsetMappingEntry> >
{
public:
    typedef mdMethodDef key_t;

    static key_t GetKey(element_t e)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return e.m_methodToken;
    }
    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (k1 == k2);
    }
    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (count_t)(size_t)k;
    }
    static const element_t Null()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        ILOffsetMappingEntry e;
        return e;
    }
    static bool IsNull(const element_t &e) { LIMITED_METHOD_DAC_CONTRACT; return e.m_methodToken == mdMethodDefNil; }
};

// Hash table of profiler-provided instrumented IL offset mapping, keyed by the MethodDef token
typedef SHash<ILOffsetMappingTraits> ILOffsetMappingTable;
typedef DPTR(ILOffsetMappingTable) PTR_ILOffsetMappingTable;

#endif // IL_INSTRUMENTATION_H
