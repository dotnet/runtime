// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.ComTypes
{
    /// <summary>
    ///     The IDataObject interface specifies methods that enable data transfer
    ///     and notification of changes in data. Data transfer methods specify
    ///     the format of the transferred data along with the medium through
    ///     which the data is to be transferred. Optionally, the data can be
    ///     rendered for a specific target device. In addition to methods for
    ///     retrieving and storing data, the IDataObject interface specifies
    ///     methods for enumerating available formats and managing connections
    ///     to advisory sinks for handling change notifications.
    /// </summary>
    [CLSCompliant(false)]
    [ComImport()]
    [Guid("0000010E-0000-0000-C000-000000000046")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDataObject {

        /// <summary>
        ///     Called by a data consumer to obtain data from a source data object.
        ///     The GetData method renders the data described in the specified FORMATETC
        ///     structure and transfers it through the specified STGMEDIUM structure.
        ///     The caller then assumes responsibility for releasing the STGMEDIUM structure.
        /// </summary>
        void GetData([In] ref FORMATETC format, out STGMEDIUM medium);

        /// <summary>
        ///     Called by a data consumer to obtain data from a source data object.
        ///     This method differs from the GetData method in that the caller must
        ///     allocate and free the specified storage medium.
        /// </summary>
        void GetDataHere([In] ref FORMATETC format, ref STGMEDIUM medium);

        /// <summary>
        ///     Determines whether the data object is capable of rendering the data
        ///     described in the FORMATETC structure. Objects attempting a paste or
        ///     drop operation can call this method before calling IDataObject::GetData
        ///     to get an indication of whether the operation may be successful.
        /// </summary>
        [PreserveSig]
        int QueryGetData([In] ref FORMATETC format);

        /// <summary>
        ///     Provides a standard FORMATETC structure that is logically equivalent to one that is more
        ///     complex. You use this method to determine whether two different
        ///     FORMATETC structures would return the same data, removing the need
        ///     for duplicate rendering.
        /// </summary>
        [PreserveSig]
        int GetCanonicalFormatEtc([In] ref FORMATETC formatIn, out FORMATETC formatOut);

        /// <summary>
        ///     Called by an object containing a data source to transfer data to
        ///     the object that implements this method.
        /// </summary>
        void SetData([In] ref FORMATETC formatIn, [In] ref STGMEDIUM medium, [MarshalAs(UnmanagedType.Bool)] bool release);

        /// <summary>
        ///     Creates an object for enumerating the FORMATETC structures for a
        ///     data object. These structures are used in calls to IDataObject::GetData
        ///     or IDataObject::SetData.
        /// </summary>
        IEnumFORMATETC EnumFormatEtc(DATADIR direction);

        /// <summary>
        ///     Called by an object supporting an advise sink to create a connection between
        ///     a data object and the advise sink. This enables the advise sink to be
        ///     notified of changes in the data of the object.
        /// </summary>
        [PreserveSig]
        int DAdvise([In] ref FORMATETC pFormatetc, ADVF advf, IAdviseSink adviseSink, out int connection);

        /// <summary>
        ///     Destroys a notification connection that had been previously set up.
        /// </summary>
        void DUnadvise(int connection);

        /// <summary>
        ///     Creates an object that can be used to enumerate the current advisory connections.
        /// </summary>
        [PreserveSig]
        int EnumDAdvise(out IEnumSTATDATA enumAdvise);
    }
}
