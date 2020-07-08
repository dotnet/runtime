// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Media
{
    public class SystemSound
    {
        private readonly int _soundType;

        internal SystemSound(int soundType)
        {
            _soundType = soundType;
        }

        public void Play()
        {
            Interop.User32.MessageBeep(_soundType);
        }
    }
}
