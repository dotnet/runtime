// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Security;
using System.Collections;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{

    // Local definition of Windows.Foundation.IClosable
    [ComImport]
    [Guid("30d5a829-7fa4-4026-83bb-d75bae4ea99e")]
    [WindowsRuntimeImport]
    internal interface IClosable
    {
        void Close();
    }

    // Adapter class - converts IClosable.Close calls to Disposable.Dispose
    internal sealed class IDisposableToIClosableAdapter
    {
        private IDisposableToIClosableAdapter()
        {
            Contract.Assert(false, "This class is never instantiated");
        }

        [SecurityCritical]
        public void Close()
        {
            IDisposable _this = JitHelpers.UnsafeCast<IDisposable>(this);
            _this.Dispose();
        }
    }

    // Adapter class which converts IDisposable.Dispose calls into IClosable.Close
    [SecurityCritical]
    internal sealed class IClosableToIDisposableAdapter
    {
        private IClosableToIDisposableAdapter()
        {
            Contract.Assert(false, "This class is never instantiated");
        }

        [SecurityCritical]
        private void Dispose()
        {
            IClosable _this = JitHelpers.UnsafeCast<IClosable>(this);
            _this.Close();
        }
    }
}
