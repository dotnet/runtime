// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading;

internal sealed class LowLevelLock : IDisposable
{
    private bool _isLocked;

#if DEBUG
    public bool IsLocked => _isLocked;
#endif

    public void Dispose() => VerifyIsNotLocked();

    [Conditional("DEBUG")]
    public void VerifyIsLocked() => Debug.Assert(_isLocked);

    [Conditional("DEBUG")]
    public void VerifyIsNotLocked() => Debug.Assert(!_isLocked);

    public bool TryAcquire()
    {
        VerifyIsNotLocked();
        if (_isLocked)
        {
            return false;
        }

        _isLocked = true;
        return true;
    }

    public void Acquire()
    {
        if (!TryAcquire())
        {
            throw new PlatformNotSupportedException();
        }
    }

    public void Release()
    {
        VerifyIsLocked();
        _isLocked = false;
    }
}
