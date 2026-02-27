// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#if !defined( __STREAMUTIL_H__ )
#define __STREAMUTIL_H__

namespace StreamUtil
{

//  Write data to stream and advance the totalBytes counter
//
inline
HRESULT WriteToStream( IStream * strm, void const * data, UINT32 sizeInBytes, UINT32 * totalBytes = NULL )
{
    HRESULT hr = strm->Write( data, sizeInBytes, NULL );
    if ( SUCCEEDED( hr ) && totalBytes != NULL )
        *totalBytes += sizeInBytes;
    return hr;
}


//  Write a POD to stream
//
template < typename T >
HRESULT WritePODToStream( IStream * strm, T val, UINT32 * totalBytes )
{
    return WriteToStream( strm, & val, sizeof( val ), totalBytes );
}


//  Write concrete data types to stream
//  Add additional overloads as needed
//

inline
HRESULT WriteToStream( IStream * strm, int val, UINT32 * totalBytes = NULL )
{
    return WritePODToStream( strm, val, totalBytes );
}


inline
HRESULT WriteToStream( IStream * strm, DWORD val, UINT32 * totalBytes = NULL )
{
    return WritePODToStream( strm, val, totalBytes );
}


inline
HRESULT WriteToStream( IStream * strm, WORD val, UINT32 * totalBytes = NULL )
{
    return WritePODToStream( strm, val, totalBytes );
}


inline
HRESULT WriteToStream( IStream * strm, BYTE val, UINT32 * totalBytes = NULL )
{
    return WritePODToStream( strm, val, totalBytes );
}


//  Align to DWORD boundary
//
inline
HRESULT AlignDWORD( IStream * strm, UINT32 * totalBytes )
{
    HRESULT hr = S_OK;

    UINT32 aligned = (*totalBytes + 3) & ~3;
    if (aligned > *totalBytes)
    {   // The *totalBytes were not aligned to DWORD, we need to add padding
        DWORD data = 0;
        hr = WriteToStream( strm, & data, aligned - *totalBytes, totalBytes );
    }
    else if (aligned < *totalBytes)
    {   // We got an integer overflow in 'aligned' expression above
        hr = COR_E_OVERFLOW;
    }

    return hr;
}


//  Get stream position
//
inline
HRESULT GetPos( IStream * strm, UINT32 * pos )
{
    LARGE_INTEGER temp = { {0} };
    ULARGE_INTEGER ul_pos = { {0} };
    HRESULT hr = strm->Seek( temp, STREAM_SEEK_CUR, & ul_pos );
    * pos = ul_pos.u.LowPart;
    return hr;
}


};  // namespace StreamUtil

#endif
