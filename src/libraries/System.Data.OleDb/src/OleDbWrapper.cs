// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CA1419 // TODO https://github.com/dotnet/roslyn-analyzers/issues/5232: not intended for use with P/Invoke

namespace System.Data.OleDb
{
    // SafeHandle wrapper around 'DataLinks' object which pools the native OLE DB providers.
    // expect 1 per app-domain
    internal sealed class OleDbServicesWrapper : WrappedIUnknown
    {
        // we expect to store IDataInitialize instance pointer in base.handle

        // since we only have one DataLinks object, caching the delegate here is valid as long we
        // maintain an AddRef which is also the lifetime of this class instance
        private readonly UnsafeNativeMethods.IDataInitializeGetDataSource? DangerousIDataInitializeGetDataSource;

        // DataLinks (the unknown parameter) is created via Activator.CreateInstance outside of the SafeHandle
        internal OleDbServicesWrapper(object? unknown) : base()
        {
            if (null != unknown)
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                { }
                finally
                {
                    // store the QI result for IID_IDataInitialize
                    base.handle = Marshal.GetComInterfaceForObject(unknown, typeof(UnsafeNativeMethods.IDataInitialize));
                }
                // native COM rules are the QI result is the 'this' pointer
                // the pointer stored at that location is the vtable
                // since IDataInitialize is a public,shipped COM interface, its layout will not change (ever)
                IntPtr vtable = Marshal.ReadIntPtr(base.handle, 0);
                IntPtr method = Marshal.ReadIntPtr(vtable, 3 * IntPtr.Size); // GetDataSource is the 4'th vtable entry
                DangerousIDataInitializeGetDataSource = (UnsafeNativeMethods.IDataInitializeGetDataSource)Marshal.GetDelegateForFunctionPointer(method, typeof(UnsafeNativeMethods.IDataInitializeGetDataSource));
            }
        }

        internal unsafe void GetDataSource(OleDbConnectionString constr, ref DataSourceWrapper datasrcWrapper)
        {
            OleDbHResult hr;
            UnsafeNativeMethods.IDataInitializeGetDataSource GetDataSource = DangerousIDataInitializeGetDataSource!;
            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                // this is the string that DataLinks / OLE DB Services will use to create the provider
                string connectionString = constr.ActualConnectionString!;

                // base.handle is the 'this' pointer for making the COM call to GetDataSource
                // the datasrcWrapper will store the IID_IDBInitialize pointer
                // call IDataInitiailze::GetDataSource via the delegate
                fixed (char* connectionStringPtr = connectionString)
                fixed (Guid* riid = &ODB.IID_IDBInitialize)
                {
                    bool addRefd = false;
                    DataSourceWrapper newWrapper = new DataSourceWrapper();
                    datasrcWrapper.DangerousAddRef(ref addRefd);
                    IntPtr originalHandle = datasrcWrapper.DangerousGetHandle();
                    IntPtr handle = originalHandle;
                    try
                    {
                        hr = GetDataSource(base.handle, IntPtr.Zero, ODB.CLSCTX_ALL, connectionStringPtr, riid, &handle);
                    }
                    finally
                    {
                        if (addRefd)
                        {
                            datasrcWrapper.DangerousRelease();
                        }
                        if (handle != originalHandle)
                        {
                            Marshal.InitHandle(newWrapper, handle);
                            datasrcWrapper = newWrapper;
                        }
                    }
                }
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
            if (hr < 0)
            { // ignore infomsg
                if (OleDbHResult.REGDB_E_CLASSNOTREG == hr)
                {
                    throw ODB.ProviderUnavailable(constr.Provider, null);
                }
                Exception? e = OleDbConnection.ProcessResults(hr, null, null);
                Debug.Assert(null != e, "CreateProviderError");
                throw e;
            }
            else if (datasrcWrapper.IsInvalid)
            {
                SafeNativeMethods.Wrapper.ClearErrorInfo();
                throw ODB.ProviderUnavailable(constr.Provider, null);
            }
            Debug.Assert(!datasrcWrapper.IsInvalid, "bad DataSource");
        }
    }

    // SafeHandle wrapper around 'Data Source' object which represents the connection
    // expect 1 per OleDbConnectionInternal
    internal sealed class DataSourceWrapper : WrappedIUnknown
    {
        // we expect to store IDBInitialize instance pointer in base.handle

        // construct a DataSourceWrapper and used as a ref parameter to GetDataSource
        public DataSourceWrapper() : base()
        {
        }

        internal unsafe OleDbHResult InitializeAndCreateSession(OleDbConnectionString constr, ref SessionWrapper sessionWrapper)
        {
            OleDbHResult hr;
            bool mustRelease = false;
            IntPtr idbCreateSession = IntPtr.Zero;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                // native COM rules are the QI result is the 'this' pointer
                // the pointer stored at that location is the vtable
                // since IUnknown is a public,shipped COM interface, its layout will not change (ever)
                IntPtr vtable = Marshal.ReadIntPtr(base.handle, 0);
                IntPtr method = Marshal.ReadIntPtr(vtable, 0);

                // we cache the QueryInterface delegate to prevent recreating it on every call
                UnsafeNativeMethods.IUnknownQueryInterface? QueryInterface = constr.DangerousDataSourceIUnknownQueryInterface;

                // since the delegate lifetime is longer than the original instance used to create it
                // we double check before each usage to verify the delegates function pointer
                if ((null == QueryInterface) || (method != Marshal.GetFunctionPointerForDelegate(QueryInterface)))
                {
                    QueryInterface = (UnsafeNativeMethods.IUnknownQueryInterface)Marshal.GetDelegateForFunctionPointer(method, typeof(UnsafeNativeMethods.IUnknownQueryInterface));
                    constr.DangerousDataSourceIUnknownQueryInterface = QueryInterface;
                }

                // native COM rules are the QI result is the 'this' pointer
                // the pointer stored at that location is the vtable
                // since IDBInitialize is a public,shipped COM interface, its layout will not change (ever)
                vtable = Marshal.ReadIntPtr(base.handle, 0);
                method = Marshal.ReadIntPtr(vtable, 3 * IntPtr.Size);  // Initialize is the 4'th vtable entry

                // we cache the Initialize delegate to prevent recreating it on every call
                UnsafeNativeMethods.IDBInitializeInitialize? Initialize = constr.DangerousIDBInitializeInitialize;

                // since the delegate lifetime is longer than the original instance used to create it
                // we double check before each usage to verify the delegates function pointer
                if ((null == Initialize) || (method != Marshal.GetFunctionPointerForDelegate(Initialize)))
                {
                    Initialize = (UnsafeNativeMethods.IDBInitializeInitialize)Marshal.GetDelegateForFunctionPointer(method, typeof(UnsafeNativeMethods.IDBInitializeInitialize));
                    constr.DangerousIDBInitializeInitialize = Initialize;
                }

                // call IDBInitialize::Initialize via the delegate
                hr = Initialize(base.handle);

                // we don't ever expect DB_E_ALREADYINITIALIZED, but since we checked in V1.0 - its propagated along
                if ((0 <= hr) || (OleDbHResult.DB_E_ALREADYINITIALIZED == hr))
                {
                    // call IUnknown::QueryInterface via the delegate
                    fixed (Guid* riid = &ODB.IID_IDBCreateSession)
                    {
                        hr = (OleDbHResult)QueryInterface(base.handle, riid, &idbCreateSession);
                    }
                    if ((0 <= hr) && (IntPtr.Zero != idbCreateSession))
                    {
                        // native COM rules are the QI result is the 'this' pointer
                        // the pointer stored at that location is the vtable
                        // since IDBCreateSession is a public,shipped COM interface, its layout will not change (ever)
                        vtable = Marshal.ReadIntPtr(idbCreateSession, 0);
                        method = Marshal.ReadIntPtr(vtable, 3 * IntPtr.Size);  // CreateSession is the 4'th vtable entry

                        UnsafeNativeMethods.IDBCreateSessionCreateSession? CreateSession = constr.DangerousIDBCreateSessionCreateSession;

                        // since the delegate lifetime is longer than the original instance used to create it
                        // we double check before each usage to verify the delegates function pointer
                        if ((null == CreateSession) || (method != Marshal.GetFunctionPointerForDelegate(CreateSession)))
                        {
                            CreateSession = (UnsafeNativeMethods.IDBCreateSessionCreateSession)Marshal.GetDelegateForFunctionPointer(method, typeof(UnsafeNativeMethods.IDBCreateSessionCreateSession));
                            constr.DangerousIDBCreateSessionCreateSession = CreateSession;
                        }

                        // if I have a delegate for CreateCommand directly ask for IDBCreateCommand
                        if (null != constr.DangerousIDBCreateCommandCreateCommand)
                        {
                            // call IDBCreateSession::CreateSession via the delegate directly for IDBCreateCommand
                            fixed (Guid* riid = &ODB.IID_IDBCreateCommand)
                            {
                                bool addRefd = false;
                                SessionWrapper newWrapper = new SessionWrapper();
                                sessionWrapper.DangerousAddRef(ref addRefd);
                                IntPtr originalHandle = sessionWrapper.DangerousGetHandle();
                                IntPtr handle = originalHandle;
                                try
                                {
                                    hr = CreateSession(idbCreateSession, IntPtr.Zero, riid, &handle);
                                }
                                finally
                                {
                                    if (addRefd)
                                    {
                                        sessionWrapper.DangerousRelease();
                                    }
                                    if (handle != originalHandle)
                                    {
                                        Marshal.InitHandle(newWrapper, handle);
                                        sessionWrapper = newWrapper;
                                    }
                                }
                            }
                            if ((0 <= hr) && !sessionWrapper.IsInvalid)
                            {
                                // double check the cached delegate is correct
                                sessionWrapper.VerifyIDBCreateCommand(constr);
                            }
                        }
                        else
                        {
                            // otherwise ask for IUnknown (it may be first time usage or IDBCreateCommand not supported)
                            fixed (Guid* riid = &ODB.IID_IUnknown)
                            {
                                bool addRefd = false;
                                SessionWrapper newWrapper = new SessionWrapper();
                                sessionWrapper.DangerousAddRef(ref addRefd);
                                IntPtr originalHandle = sessionWrapper.DangerousGetHandle();
                                IntPtr handle = originalHandle;
                                try
                                {
                                    hr = CreateSession(idbCreateSession, IntPtr.Zero, riid, &handle);
                                }
                                finally
                                {
                                    if (addRefd)
                                    {
                                        sessionWrapper.DangerousRelease();
                                    }
                                    if (handle != originalHandle)
                                    {
                                        Marshal.InitHandle(newWrapper, handle);
                                        sessionWrapper = newWrapper;
                                    }
                                }
                            }
                            if ((0 <= hr) && !sessionWrapper.IsInvalid)
                            {
                                // and check support for IDBCreateCommand and create delegate for CreateCommand
                                sessionWrapper.QueryInterfaceIDBCreateCommand(constr);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (IntPtr.Zero != idbCreateSession)
                {
                    // release the QI for IDBCreateSession
                    Marshal.Release(idbCreateSession);
                }
                if (mustRelease)
                {
                    // release the AddRef on DataLinks
                    DangerousRelease();
                }
            }
            return hr;
        }

        internal IDBInfoWrapper IDBInfo(OleDbConnectionInternal connection)
        {
            return new IDBInfoWrapper(ComWrapper());
        }
        internal IDBPropertiesWrapper IDBProperties(OleDbConnectionInternal connection)
        {
            return new IDBPropertiesWrapper(ComWrapper());
        }
    }

    // SafeHandle wrapper around 'Session' object which represents the session on the connection
    // expect 1 per OleDbConnectionInternal
    internal sealed class SessionWrapper : WrappedIUnknown
    {
        // base.handle will either reference the IUnknown interface or IDBCreateCommand interface
        // if OleDbConnectionString.DangerousIDBCreateCommandCreateCommand exists
        // the CreateSession call will ask directly for the optional IDBCreateCommand
        // otherwise it is the first call or known that IDBCreateCommand isn't supported

        // we cache the DangerousIDBCreateCommandCreateCommand (expecting base.handle to be IDBCreateCommand)
        // since we maintain an AddRef on IDBCreateCommand it is safe to use the delegate without rechecking its function pointer
        private UnsafeNativeMethods.IDBCreateCommandCreateCommand? DangerousIDBCreateCommandCreateCommand;

        public SessionWrapper() : base()
        {
        }

        // if OleDbConnectionString.DangerousIDBCreateCommandCreateCommand does not exist
        // this method will be called to query for IDBCreateCommand (and cache that interface pointer)
        // or it will be known that IDBCreateCommand is not supported
        internal unsafe void QueryInterfaceIDBCreateCommand(OleDbConnectionString constr)
        {
            // DangerousAddRef/DangerousRelease are not neccessary here in the current implementation
            // only used from within OleDbConnectionInternal.ctor->DataSourceWrapper.InitializeAndCreateSession

            // caching the fact if we have queried for IDBCreateCommand or not
            // the command object is not supported by all providers, they would use IOpenRowset
            // added extra if condition
            // If constr.HaveQueriedForCreateCommand is false, this is the first time through this method and we need to set up the cache for sure.
            // If two threads try to set the cache at the same time, everything should be okay. There can be multiple delegates that point to the same unmanaged function.
            // If constr.HaveQueriedForCreateCommand is true, we have already tried to query for IDBCreateCommand on a previous call to this method, but based on that alone,
            //     we don't know if another thread set the flag, or if the provider really doesn't support commands.
            // If constr.HaveQueriedForCreateCommand is true and constr.DangerousIDBCreateCommandCreateCommand is not null, that means that another thread has set it after we
            //     determined we needed to call QueryInterfaceIDBCreateCommand -- otherwise we would have called VerifyIDBCreateCommand instead
            // In that case, we still need to set our local DangerousIDBCreateCommandCreateCommand, so we want to go through the if block even though the cache has been set on constr already
            if (!constr.HaveQueriedForCreateCommand || (null != constr.DangerousIDBCreateCommandCreateCommand))
            {
                IntPtr idbCreateCommand = IntPtr.Zero;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    // native COM rules are the QI result is the 'this' pointer
                    // the pointer stored at that location is the vtable
                    // since IUnknown is a public,shipped COM interface, its layout will not change (ever)
                    IntPtr vtable = Marshal.ReadIntPtr(base.handle, 0);
                    IntPtr method = Marshal.ReadIntPtr(vtable, 0);
                    UnsafeNativeMethods.IUnknownQueryInterface QueryInterface = (UnsafeNativeMethods.IUnknownQueryInterface)Marshal.GetDelegateForFunctionPointer(method, typeof(UnsafeNativeMethods.IUnknownQueryInterface));

                    int hresult;
                    fixed (Guid* riid = &ODB.IID_IDBCreateCommand)
                    {
                        hresult = QueryInterface(base.handle, riid, &idbCreateCommand);
                    }
                    if ((0 <= hresult) && (IntPtr.Zero != idbCreateCommand))
                    {
                        vtable = Marshal.ReadIntPtr(idbCreateCommand, 0);
                        method = Marshal.ReadIntPtr(vtable, 3 * IntPtr.Size);

                        DangerousIDBCreateCommandCreateCommand = (UnsafeNativeMethods.IDBCreateCommandCreateCommand)Marshal.GetDelegateForFunctionPointer(method, typeof(UnsafeNativeMethods.IDBCreateCommandCreateCommand));
                        constr.DangerousIDBCreateCommandCreateCommand = DangerousIDBCreateCommandCreateCommand;
                    }

                    // caching the fact that we have queried for IDBCreateCommand
                    constr.HaveQueriedForCreateCommand = true;
                }
                finally
                {
                    if (IntPtr.Zero != idbCreateCommand)
                    {
                        IntPtr ptr = base.handle;
                        base.handle = idbCreateCommand;
                        Marshal.Release(ptr);
                    }
                }
            }
            //else if constr.HaveQueriedForCreateCommand is true and constr.DangerousIDBCreateCommandCreateCommand is still null, it means that this provider doesn't support commands
        }

        internal void VerifyIDBCreateCommand(OleDbConnectionString constr)
        {
            // DangerousAddRef/DangerousRelease are not neccessary here in the current implementation
            // only used from within OleDbConnectionInternal.ctor->DataSourceWrapper.InitializeAndCreateSession

            Debug.Assert(constr.HaveQueriedForCreateCommand, "expected HaveQueriedForCreateCommand");
            Debug.Assert(null != constr.DangerousIDBCreateCommandCreateCommand, "expected DangerousIDBCreateCommandCreateCommand");

            // native COM rules are the QI result is the 'this' pointer
            // the pointer stored at that location is the vtable
            // since IDBCreateCommand is a public,shipped COM interface, its layout will not change (ever)
            IntPtr vtable = Marshal.ReadIntPtr(base.handle, 0);
            IntPtr method = Marshal.ReadIntPtr(vtable, 3 * IntPtr.Size);

            // obtain the cached delegate to be cached on this instance
            UnsafeNativeMethods.IDBCreateCommandCreateCommand CreateCommand = constr.DangerousIDBCreateCommandCreateCommand;

            // since the delegate lifetime is longer than the original instance used to create it
            // we double check before each usage to verify the delegates function pointer
            if ((null == CreateCommand) || (method != Marshal.GetFunctionPointerForDelegate(CreateCommand)))
            {
                CreateCommand = (UnsafeNativeMethods.IDBCreateCommandCreateCommand)Marshal.GetDelegateForFunctionPointer(method, typeof(UnsafeNativeMethods.IDBCreateCommandCreateCommand));
                constr.DangerousIDBCreateCommandCreateCommand = CreateCommand;
            }
            // since this instance can be used to create multiple commands
            // cache it on the class so that the function pointer doesn't have to be validated every time
            DangerousIDBCreateCommandCreateCommand = CreateCommand;
        }

        internal unsafe OleDbHResult CreateCommand(ref object? icommandText)
        {
            // if (null == CreateCommand), the IDBCreateCommand isn't supported - aka E_NOINTERFACE
            OleDbHResult hr = OleDbHResult.E_NOINTERFACE;
            UnsafeNativeMethods.IDBCreateCommandCreateCommand? CreateCommand = DangerousIDBCreateCommandCreateCommand;
            if (null != CreateCommand)
            {
                IntPtr nativeICommandText = IntPtr.Zero;
                bool mustRelease = false;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    DangerousAddRef(ref mustRelease);

                    if (icommandText is not null)
                    {
                        nativeICommandText = Marshal.GetIUnknownForObject(icommandText);
                    }

                    fixed (Guid* riid = &ODB.IID_ICommandText)
                    {
                        // call IDBCreateCommand::CreateCommand via the delegate directly for IDBCreateCommand
                        hr = CreateCommand(base.handle, IntPtr.Zero, riid, &nativeICommandText);
                    }

                    icommandText = Marshal.GetObjectForIUnknown(nativeICommandText);
                }
                finally
                {
                    if (mustRelease)
                    {
                        DangerousRelease();
                    }
                    Marshal.Release(nativeICommandText);
                }
            }
            return hr;
        }

        internal IDBSchemaRowsetWrapper IDBSchemaRowset(OleDbConnectionInternal connection)
        {
            return new IDBSchemaRowsetWrapper(ComWrapper());
        }

        internal IOpenRowsetWrapper IOpenRowset(OleDbConnectionInternal connection)
        {
            return new IOpenRowsetWrapper(ComWrapper());
        }

        internal ITransactionJoinWrapper ITransactionJoin(OleDbConnectionInternal connection)
        {
            return new ITransactionJoinWrapper(ComWrapper());
        }
    }

    // unable to use generics because (unknown as T) doesn't compile
    internal struct IDBInfoWrapper : IDisposable
    {
        // _unknown must be tracked because the _value may not exist,
        // yet _unknown must still be released
        private object _unknown;
        private UnsafeNativeMethods.IDBInfo _value;

        internal IDBInfoWrapper(object unknown)
        {
            _unknown = unknown;
            _value = (unknown as UnsafeNativeMethods.IDBInfo)!;
        }

        internal UnsafeNativeMethods.IDBInfo Value
        {
            get
            {
                return _value;
            }
        }

        public void Dispose()
        {
            object unknown = _unknown;
            _unknown = null!;
            _value = null!;
            if (null != unknown)
            {
                Marshal.ReleaseComObject(unknown);
            }
        }
    }

    internal struct IDBPropertiesWrapper : IDisposable
    {
        private object _unknown;
        private UnsafeNativeMethods.IDBProperties _value;

        internal IDBPropertiesWrapper(object unknown)
        {
            _unknown = unknown;
            _value = (unknown as UnsafeNativeMethods.IDBProperties)!;
            Debug.Assert(null != _value, "null IDBProperties");
        }

        internal UnsafeNativeMethods.IDBProperties Value
        {
            get
            {
                Debug.Assert(null != _value, "null IDBProperties");
                return _value;
            }
        }

        public void Dispose()
        {
            object unknown = _unknown;
            _unknown = null!;
            _value = null!;
            if (null != unknown)
            {
                Marshal.ReleaseComObject(unknown);
            }
        }
    }

    internal struct IDBSchemaRowsetWrapper : IDisposable
    {
        private object _unknown;
        private UnsafeNativeMethods.IDBSchemaRowset _value;

        internal IDBSchemaRowsetWrapper(object unknown)
        {
            _unknown = unknown;
            _value = (unknown as UnsafeNativeMethods.IDBSchemaRowset)!;
        }

        internal UnsafeNativeMethods.IDBSchemaRowset Value
        {
            get
            {
                return _value;
            }
        }

        public void Dispose()
        {
            object unknown = _unknown;
            _unknown = null!;
            _value = null!;
            if (null != unknown)
            {
                Marshal.ReleaseComObject(unknown);
            }
        }
    }

    internal struct IOpenRowsetWrapper : IDisposable
    {
        private object _unknown;
        private UnsafeNativeMethods.IOpenRowset _value;

        internal IOpenRowsetWrapper(object unknown)
        {
            _unknown = unknown;
            _value = (unknown as UnsafeNativeMethods.IOpenRowset)!;
            Debug.Assert(null != _value, "null IOpenRowsetWrapper");
        }

        internal UnsafeNativeMethods.IOpenRowset Value
        {
            get
            {
                Debug.Assert(null != _value, "null IDBProperties");
                return _value;
            }
        }

        public void Dispose()
        {
            object unknown = _unknown;
            _unknown = null!;
            _value = null!;
            if (null != unknown)
            {
                Marshal.ReleaseComObject(unknown);
            }
        }
    }

    internal struct ITransactionJoinWrapper : IDisposable
    {
        private object _unknown;
        private NativeMethods.ITransactionJoin _value;

        internal ITransactionJoinWrapper(object unknown)
        {
            _unknown = unknown;
            _value = (unknown as NativeMethods.ITransactionJoin)!;
        }

        internal NativeMethods.ITransactionJoin Value
        {
            get
            {
                return _value;
            }
        }

        public void Dispose()
        {
            object unknown = _unknown;
            _unknown = null!;
            _value = null!;
            if (null != unknown)
            {
                Marshal.ReleaseComObject(unknown);
            }
        }
    }
}
