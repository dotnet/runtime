// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef _NIDUMP_INL_
#define _NIDUMP_INL_
template<typename T>
TADDR NativeImageDumper::DPtrToPreferredAddr( T ptr )
{
    TADDR tptr = PTR_TO_TADDR(ptr);
    return DataPtrToDisplay(tptr);
}

inline TADDR NativeImageDumper::RvaToDisplay( SIZE_T rva )
{
    return DataPtrToDisplay(m_decoder.GetRvaData((RVA)rva));
}
inline TADDR NativeImageDumper::DataPtrToDisplay(TADDR ptr)
{
    if( ptr == NULL || ptr == (TADDR)-1
        || CheckOptions(CLRNATIVEIMAGE_DISABLE_REBASING) )
        return TO_TADDR(ptr);

    if( isInRange(ptr) || m_dependencies == NULL )
    {
        //fast path in case the dependencies aren't loaded.
        RVA rva = m_decoder.GetDataRva(ptr);
        if (CheckOptions(CLRNATIVEIMAGE_FILE_OFFSET))
            return (TADDR) m_decoder.RvaToOffset(rva);
        else
            return rva + (INT_PTR)m_decoder.GetNativePreferredBase();
    }
    if( m_mscorwksBase <= ptr && ptr < (m_mscorwksBase + m_mscorwksSize) )
    {
        return ptr - m_mscorwksBase + m_mscorwksPreferred;
    }
    for( COUNT_T i = 0; i < m_numDependencies; ++i )
    {
        const Dependency * dependency = &m_dependencies[i];
        if( dependency->pPreferredBase == NULL )
            continue;
        if( dependency->pLoadedAddress <= ptr
            && ((dependency->pLoadedAddress + dependency->size) > ptr) )
        {
            //found the right target
            return ptr - (INT_PTR)dependency->pLoadedAddress
                + (INT_PTR)dependency->pPreferredBase;
        }
    }
    return ptr;
}
inline int NativeImageDumper::CheckOptions( CLRNativeImageDumpOptions opt )
{
    //if( opt == ((CLRNativeImageDumpOptions)~0) )
        //return 1;
    return (m_dumpOptions & opt) != 0;
}


#if 0
PTR_CCOR_SIGNATURE
NativeImageDumper::metadataToHostDAC( PCCOR_SIGNATURE pSig,
                                      IMetaDataImport2 * import)
{
    TADDR tsig = TO_TADDR(pSig);
    if( m_MetadataSize == 0 ) //assume target
        return PTR_CCOR_SIGNATURE(tsig);

    //find the dependency for this import
    const Dependency * dependency = NULL;
    for( COUNT_T i = 0; i < m_numDependencies; ++i )
    {
        if( m_dependencies[i].pImport == import )
        {
            dependency = &m_dependencies[i];
            break;
        }
    }
    if( dependency != NULL && dependency->pMetadataStartHost <= tsig
        && tsig < (dependency->pMetadataStartHost
                   + dependency->MetadataSize)  )
    {
        //host metadata pointer
        return PTR_CCOR_SIGNATURE((tsig
                                   - dependency->pMetadataStartHost)
                                  + dependency->pMetadataStartTarget );
    }
    return PTR_CCOR_SIGNATURE(tsig);
}
#endif
template<typename T>
DPTR(T)
NativeImageDumper::metadataToHostDAC( T * pSig,
                                      IMetaDataImport2 * import)
{
    TADDR tsig = TO_TADDR(pSig);
    if( m_MetadataSize == 0 ) //assume target
        return DPTR(T)(tsig);

    //find the dependency for this import
    const Dependency * dependency = NULL;
    for( COUNT_T i = 0; i < m_numDependencies; ++i )
    {
        if( m_dependencies[i].pImport == import )
        {
            dependency = &m_dependencies[i];
            break;
        }
    }
    if( dependency != NULL && dependency->pMetadataStartHost <= tsig
        && tsig < (dependency->pMetadataStartHost
                   + dependency->MetadataSize)  )
    {
        //host metadata pointer
        return DPTR(T)((tsig - dependency->pMetadataStartHost)
                       + dependency->pMetadataStartTarget );
    }
    return DPTR(T)(tsig);
}

inline SIZE_T NativeImageDumper::GetSectionAlignment() const {
    _ASSERTE( m_sectionAlignment > 0 );
    return m_sectionAlignment;
}
#ifdef MANUAL_RELOCS
template< typename T >
inline T NativeImageDumper::RemapPointerForReloc( T ptr )
{
    return T(RemapTAddrForReloc(PTR_TO_TADDR(ptr)));
}
inline TADDR NativeImageDumper::RemapTAddrForReloc( TADDR ptr )
{
#if 0
    if( NULL == ptr )
        return ptr;
    if( isInRange(ptr) )
        return ptr;
    const NativeImageDumper::Dependency * dependency =
        GetDependencyForPointer(ptr);
    _ASSERTE(dependency);
    return RemapTAddrForReloc( dependency, ptr );
#else
    return ptr;
#endif

}
template< typename T >
inline T
NativeImageDumper::RemapPointerForReloc(const NativeImageDumper::Dependency* dependency,
                                        T ptr )
{
    return T(RemapTAddrForReloc( dependency, PTR_TO_TADDR(ptr) ));
}
inline TADDR
NativeImageDumper::RemapTAddrForReloc( const NativeImageDumper::Dependency * d,
                                       TADDR ptr )
{
#if 0
    if( d->pPreferredBase == d->pLoadedAddress )
        return ptr;
    else
        return (ptr - d->pPreferredBase) + d->pLoadedAddress;
#else
    return ptr;
#endif
}
#endif //MANUAL_RELOCS
#endif //!_NIDUMP_INL_
