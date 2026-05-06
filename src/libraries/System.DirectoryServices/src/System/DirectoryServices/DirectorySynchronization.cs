// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.DirectoryServices
{
    public class DirectorySynchronization
    {
        private DirectorySynchronizationOptions _option = DirectorySynchronizationOptions.None;
        private byte[] _cookie = Array.Empty<byte>();

        public DirectorySynchronization()
        {
        }

        public DirectorySynchronization(DirectorySynchronizationOptions option)
        {
            Option = option;
        }

        public DirectorySynchronization(DirectorySynchronization? sync)
        {
            if (sync != null)
            {
                Option = sync.Option;
                ResetDirectorySynchronizationCookie(sync.GetDirectorySynchronizationCookie());
            }
        }

        public DirectorySynchronization(byte[]? cookie) => ResetDirectorySynchronizationCookie(cookie);

        public DirectorySynchronization(DirectorySynchronizationOptions option, byte[]? cookie)
        {
            Option = option;
            ResetDirectorySynchronizationCookie(cookie);
        }

        [DefaultValue(DirectorySynchronizationOptions.None)]
        public DirectorySynchronizationOptions Option
        {
            get => _option;
            set
            {
                long val = (long)(value & (~(DirectorySynchronizationOptions.None | DirectorySynchronizationOptions.ObjectSecurity |
                                            DirectorySynchronizationOptions.ParentsFirst | DirectorySynchronizationOptions.PublicDataOnly |
                                            DirectorySynchronizationOptions.IncrementalValues)));
                if (val != 0)
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(DirectorySynchronizationOptions));
                }

                _option = value;
            }
        }

        public byte[] GetDirectorySynchronizationCookie()
        {
            byte[] tempcookie = new byte[_cookie.Length];
            for (int i = 0; i < _cookie.Length; i++)
            {
                tempcookie[i] = _cookie[i];
            }

            return tempcookie;
        }

        public void ResetDirectorySynchronizationCookie() => _cookie = Array.Empty<byte>();

        public void ResetDirectorySynchronizationCookie(byte[]? cookie)
        {
            if (cookie == null)
            {
                _cookie = Array.Empty<byte>();
            }
            else
            {
                _cookie = new byte[cookie.Length];
                for (int i = 0; i < cookie.Length; i++)
                {
                    _cookie[i] = cookie[i];
                }
            }
        }

        public DirectorySynchronization Copy() => new DirectorySynchronization(_option, _cookie);
    }
}
