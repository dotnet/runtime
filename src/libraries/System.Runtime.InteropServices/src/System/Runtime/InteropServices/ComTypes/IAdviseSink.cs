// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.ComTypes
{
    /// <summary>
    /// The IAdviseSink interface enables containers and other objects to 
    /// receive notifications of data changes, view changes, and compound-document 
    /// changes occurring in objects of interest. Container applications, for 
    /// example, require such notifications to keep cached presentations of their 
    /// linked and embedded objects up-to-date. Calls to IAdviseSink methods are 
    /// asynchronous, so the call is sent and then the next instruction is executed 
    /// without waiting for the call's return.
    /// </summary>
    [ComImport]
    [Guid("0000010F-0000-0000-C000-000000000046")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAdviseSink
    {
        /// <summary>
        /// Called by the server to notify a data object's currently registered 
        /// advise sinks that data in the object has changed.
        /// </summary>
        [PreserveSig]
        void OnDataChange([In] ref FORMATETC format, [In] ref STGMEDIUM stgmedium);

        /// <summary>
        /// Notifies an object's registered advise sinks that its view has changed.
        /// </summary>
        [PreserveSig]
        void OnViewChange(int aspect, int index);

        /// <summary>
        /// Called by the server to notify all registered advisory sinks that 
        /// the object has been renamed.
        /// </summary>
        [PreserveSig]
        void OnRename(IMoniker moniker);

        /// <summary>
        /// Called by the server to notify all registered advisory sinks that 
        /// the object has been saved.
        /// </summary>
        [PreserveSig]
        void OnSave();

        /// <summary>
        /// Called by the server to notify all registered advisory sinks that the 
        /// object has changed from the running to the loaded state.
        /// </summary>
        [PreserveSig]
        void OnClose();
    }
}
