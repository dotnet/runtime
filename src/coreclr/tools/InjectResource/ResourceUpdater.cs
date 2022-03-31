// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

class ResourceUpdater : IDisposable
{
    private class UpdateResourceHandle : SafeHandle
    {
        public UpdateResourceHandle()
            :base(IntPtr.Zero, true)
        {

        }

        protected override bool ReleaseHandle()
        {
            return EndUpdateResource(handle, false);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }

    [DllImport("kernel32", EntryPoint = "BeginUpdateResourceA", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern UpdateResourceHandle BeginUpdateResource(string pFileName, bool bDeleteExistingResources);

    [DllImport("kernel32", EntryPoint = "UpdateResourceA", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern bool UpdateResource(
        UpdateResourceHandle hUpdate,
        nint lpType,
        string lpName,
        ushort wLanguage,
        byte[] lpData,
        int cb);

    [DllImport("kernel32", EntryPoint = "EndUpdateResourceA", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);

    private UpdateResourceHandle handle;

    public ResourceUpdater(FileInfo peFile)
    {
        handle = BeginUpdateResource(peFile.FullName, false);
        if (handle.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError(), peFile.FullName);
        }
    }

    private static readonly nint RT_RCDATA = 10;

    private const ushort LANG_NEUTRAL = 0;

    public void AddBinaryResource(string name, byte[] data)
    {
        bool success = UpdateResource(handle, RT_RCDATA, name, LANG_NEUTRAL, data, data.Length);
        if (!success)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError(), name);
        }
    }

    public void Dispose()
    {
        handle.Dispose();
    }
}