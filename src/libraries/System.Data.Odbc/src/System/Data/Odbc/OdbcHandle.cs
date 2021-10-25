// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable CA1419 // TODO https://github.com/dotnet/roslyn-analyzers/issues/5232: not intended for use with P/Invoke

namespace System.Data.Odbc
{
    internal abstract class OdbcHandle : SafeHandle
    {
        private readonly ODBC32.SQL_HANDLE _handleType;
        private OdbcHandle? _parentHandle;

        protected OdbcHandle(ODBC32.SQL_HANDLE handleType, OdbcHandle? parentHandle) : base(IntPtr.Zero, true)
        {
            _handleType = handleType;

            bool mustRelease = false;
            ODBC32.RetCode retcode = ODBC32.RetCode.SUCCESS;

            try
            {
                // validate handleType
                switch (handleType)
                {
                    case ODBC32.SQL_HANDLE.ENV:
                        Debug.Assert(null == parentHandle, "did not expect a parent handle");
                        retcode = Interop.Odbc.SQLAllocHandle(handleType, IntPtr.Zero, out base.handle);
                        break;
                    case ODBC32.SQL_HANDLE.DBC:
                    case ODBC32.SQL_HANDLE.STMT:
                        // must addref before calling native so it won't be released just after
                        Debug.Assert(null != parentHandle, "expected a parent handle"); // safehandle can't be null
                        parentHandle.DangerousAddRef(ref mustRelease);

                        retcode = Interop.Odbc.SQLAllocHandle(handleType, parentHandle, out base.handle);
                        break;
                    //              case ODBC32.SQL_HANDLE.DESC:
                    default:
                        Debug.Fail("unexpected handleType");
                        break;
                }
            }
            finally
            {
                if (mustRelease)
                {
                    switch (handleType)
                    {
                        case ODBC32.SQL_HANDLE.DBC:
                        case ODBC32.SQL_HANDLE.STMT:
                            if (IntPtr.Zero != base.handle)
                            {
                                // must assign _parentHandle after a handle is actually created
                                // since ReleaseHandle will only call DangerousRelease if a handle exists
                                _parentHandle = parentHandle;
                            }
                            else
                            {
                                // without a handle, ReleaseHandle may not be called
                                parentHandle!.DangerousRelease();
                            }
                            break;
                    }
                }
            }

            if ((ADP.PtrZero == base.handle) || (ODBC32.RetCode.SUCCESS != retcode))
            {
                //
                throw ODBC.CantAllocateEnvironmentHandle(retcode);
            }
        }

        internal OdbcHandle(OdbcStatementHandle parentHandle, ODBC32.SQL_ATTR attribute) : base(IntPtr.Zero, true)
        {
            Debug.Assert((ODBC32.SQL_ATTR.APP_PARAM_DESC == attribute) || (ODBC32.SQL_ATTR.APP_ROW_DESC == attribute), "invalid attribute");
            _handleType = ODBC32.SQL_HANDLE.DESC;

            int cbActual;
            ODBC32.RetCode retcode;
            bool mustRelease = false;

            try
            {
                // must addref before calling native so it won't be released just after
                parentHandle.DangerousAddRef(ref mustRelease);

                retcode = parentHandle.GetStatementAttribute(attribute, out base.handle, out cbActual);
            }
            finally
            {
                if (mustRelease)
                {
                    if (IntPtr.Zero != base.handle)
                    {
                        // must call DangerousAddRef after a handle is actually created
                        // since ReleaseHandle will only call DangerousRelease if a handle exists
                        _parentHandle = parentHandle;
                    }
                    else
                    {
                        // without a handle, ReleaseHandle may not be called
                        parentHandle.DangerousRelease();
                    }
                }
            }
            if (ADP.PtrZero == base.handle)
            {
                throw ODBC.FailedToGetDescriptorHandle(retcode);
            }
            // no info-message handle on getting a descriptor handle
        }

        internal ODBC32.SQL_HANDLE HandleType
        {
            get
            {
                return _handleType;
            }
        }

        public override bool IsInvalid
        {
            get
            {
                // we should not have a parent if we do not have a handle
                return (IntPtr.Zero == base.handle);
            }
        }

        protected override bool ReleaseHandle()
        {
            // NOTE: The SafeHandle class guarantees this will be called exactly once and is non-interrutible.
            IntPtr handle = base.handle;
            base.handle = IntPtr.Zero;

            if (IntPtr.Zero != handle)
            {
                ODBC32.SQL_HANDLE handleType = HandleType;

                switch (handleType)
                {
                    case ODBC32.SQL_HANDLE.DBC:
                    // Disconnect happens in OdbcConnectionHandle.ReleaseHandle
                    case ODBC32.SQL_HANDLE.ENV:
                    case ODBC32.SQL_HANDLE.STMT:
                        Interop.Odbc.SQLFreeHandle(handleType, handle);
                        break;

                    case ODBC32.SQL_HANDLE.DESC:
                        // nothing to free on the handle
                        break;

                    // case 0: ThreadAbortException setting handle before HandleType
                    default:
                        Debug.Assert(ADP.PtrZero == handle, "unknown handle type");
                        break;
                }
            }

            // If we ended up getting released, then we have to release
            // our reference on our parent.
            OdbcHandle? parentHandle = _parentHandle;
            _parentHandle = null;
            if (null != parentHandle)
            {
                parentHandle.DangerousRelease();
                parentHandle = null;
            }
            return true;
        }

        internal ODBC32.RetCode GetDiagnosticField(out string sqlState)
        {
            short cbActual;
            // ODBC (MSDN) documents it expects a buffer large enough to hold 5(+L'\0') unicode characters
            StringBuilder sb = new StringBuilder(6);
            ODBC32.RetCode retcode = Interop.Odbc.SQLGetDiagFieldW(
                HandleType,
                this,
                (short)1,
                ODBC32.SQL_DIAG_SQLSTATE,
                sb,
                checked((short)(2 * sb.Capacity)), // expects number of bytes, see \\kbinternal\kb\articles\294\1\69.HTM
                out cbActual);
            ODBC.TraceODBC(3, "SQLGetDiagFieldW", retcode);
            if ((retcode == ODBC32.RetCode.SUCCESS) || (retcode == ODBC32.RetCode.SUCCESS_WITH_INFO))
            {
                sqlState = sb.ToString();
            }
            else
            {
                sqlState = string.Empty;
            }
            return retcode;
        }

        internal ODBC32.RetCode GetDiagnosticRecord(short record, out string sqlState, StringBuilder message, out int nativeError, out short cchActual)
        {
            // ODBC (MSDN) documents it expects a buffer large enough to hold 4(+L'\0') unicode characters
            StringBuilder sb = new StringBuilder(5);
            ODBC32.RetCode retcode = Interop.Odbc.SQLGetDiagRecW(HandleType, this, record, sb, out nativeError, message, checked((short)message.Capacity), out cchActual);
            ODBC.TraceODBC(3, "SQLGetDiagRecW", retcode);

            if ((retcode == ODBC32.RetCode.SUCCESS) || (retcode == ODBC32.RetCode.SUCCESS_WITH_INFO))
            {
                sqlState = sb.ToString();
            }
            else
            {
                sqlState = string.Empty;
            }
            return retcode;
        }
    }

    internal sealed class OdbcDescriptorHandle : OdbcHandle
    {
        internal OdbcDescriptorHandle(OdbcStatementHandle statementHandle, ODBC32.SQL_ATTR attribute) : base(statementHandle, attribute)
        {
        }

        internal ODBC32.RetCode GetDescriptionField(int i, ODBC32.SQL_DESC attribute, CNativeBuffer buffer, out int numericAttribute)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLGetDescFieldW(this, checked((short)i), attribute, buffer, buffer.ShortLength, out numericAttribute);
            ODBC.TraceODBC(3, "SQLGetDescFieldW", retcode);
            return retcode;
        }

        internal ODBC32.RetCode SetDescriptionField1(short ordinal, ODBC32.SQL_DESC type, IntPtr value)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLSetDescFieldW(this, ordinal, type, value, 0);
            ODBC.TraceODBC(3, "SQLSetDescFieldW", retcode);
            return retcode;
        }

        internal ODBC32.RetCode SetDescriptionField2(short ordinal, ODBC32.SQL_DESC type, HandleRef value)
        {
            ODBC32.RetCode retcode = Interop.Odbc.SQLSetDescFieldW(this, ordinal, type, value, 0);
            ODBC.TraceODBC(3, "SQLSetDescFieldW", retcode);
            return retcode;
        }
    }
}
