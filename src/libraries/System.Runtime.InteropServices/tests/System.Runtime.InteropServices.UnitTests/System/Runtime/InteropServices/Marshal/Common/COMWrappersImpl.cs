// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Runtime.InteropServices.Tests.Common
{
    public class ComWrappersImpl : ComWrappers
    {
        // Doesn't represent a real interface. The value is only used to support a call to QueryInterface for testing.
        public const string IID_TestQueryInterface = "1F906666-B388-4729-B78C-826BC5FD4245";

        protected unsafe override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            Assert.Equal(CreateComInterfaceFlags.None, flags);

            IntPtr fpQueryInterface = default;
            IntPtr fpAddRef = default;
            IntPtr fpRelease = default;
            ComWrappers.GetIUnknownImpl(out fpQueryInterface, out fpAddRef, out fpRelease);

            var vtblRaw = (IntPtr*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ComWrappersImpl), IntPtr.Size * 3);
            vtblRaw[0] = fpQueryInterface;
            vtblRaw[1] = fpAddRef;
            vtblRaw[2] = fpRelease;

            var entryRaw = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ComWrappersImpl), sizeof(ComInterfaceEntry));
            entryRaw->IID = new Guid(IID_TestQueryInterface);
            entryRaw->Vtable = (IntPtr)vtblRaw;

            count = 1;
            return entryRaw;
        }

        protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flag)
            => throw new NotImplementedException();

        protected override void ReleaseObjects(IEnumerable objects)
            => throw new NotImplementedException();
    }
}
