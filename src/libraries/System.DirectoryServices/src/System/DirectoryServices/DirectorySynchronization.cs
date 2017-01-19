// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices
{
    using System.ComponentModel;

    /// <include file='doc\DirectorySynchronization.uex' path='docs/doc[@for="DirectorySynchronization"]/*' />
    /// <devdoc>
    ///    <para>Specifies how to do directory synchronization search.</para>
    /// </devdoc>
    public class DirectorySynchronization
    {
        private DirectorySynchronizationOptions _flag = DirectorySynchronizationOptions.None;
        private byte[] _cookie = new byte[0];

        /// <include file='doc\DirectorySynchronization.uex' path='docs/doc[@for="DirectorySynchronization.DirectorySynchronization"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
    	public DirectorySynchronization()
        {
        }

        /// <include file='doc\DirectorySynchronization.uex' path='docs/doc[@for="DirectorySynchronization.DirectorySynchronization1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
    	public DirectorySynchronization(DirectorySynchronizationOptions option)
        {
            Option = option;
        }

        /// <include file='doc\DirectorySynchronization.uex' path='docs/doc[@for="DirectorySynchronization.DirectorySynchronization2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
    	public DirectorySynchronization(DirectorySynchronization sync)
        {
            if (sync != null)
            {
                Option = sync.Option;
                ResetDirectorySynchronizationCookie(sync.GetDirectorySynchronizationCookie());
            }
        }

        /// <include file='doc\DirectorySynchronization.uex' path='docs/doc[@for="DirectorySynchronization.DirectorySynchronization3"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
    	public DirectorySynchronization(byte[] cookie)
        {
            ResetDirectorySynchronizationCookie(cookie);
        }

        /// <include file='doc\DirectorySynchronization.uex' path='docs/doc[@for="DirectorySynchronization.DirectorySynchronization4"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
    	public DirectorySynchronization(DirectorySynchronizationOptions option, byte[] cookie)
        {
            Option = option;
            ResetDirectorySynchronizationCookie(cookie);
        }

        /// <include file='doc\DirectorySynchronization.uex' path='docs/doc[@for="DirectorySynchronization.Flag"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets the flag to do directory synchronization search.</para>
        /// </devdoc>
    	[
            DefaultValue(DirectorySynchronizationOptions.None),
        ]
        public DirectorySynchronizationOptions Option
        {
            get
            {
                return _flag;
            }

            set
            {
                long val = (long)(value & (~(DirectorySynchronizationOptions.None | DirectorySynchronizationOptions.ObjectSecurity |
                                         DirectorySynchronizationOptions.ParentsFirst | DirectorySynchronizationOptions.PublicDataOnly |
                                         DirectorySynchronizationOptions.IncrementalValues)));
                if (val != 0)
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(DirectorySynchronizationOptions));

                _flag = value;
            }
        }

        /// <include file='doc\DirectorySynchronization.uex' path='docs/doc[@for="DirectorySynchronization.GetDirectorySynchronizationCookie"]/*' />
        /// <devdoc>
        ///    <para>Gets the directory synchronization search cookie.</para>
        /// </devdoc>
        public byte[] GetDirectorySynchronizationCookie()
        {
            byte[] tempcookie = new byte[_cookie.Length];
            for (int i = 0; i < _cookie.Length; i++)
            {
                tempcookie[i] = _cookie[i];
            }

            return tempcookie;
        }

        /// <include file='doc\DirectorySynchronization.uex' path='docs/doc[@for="DirectorySynchronization.GetDirectorySynchronizationCookie1"]/*' />
        /// <devdoc>
        ///    <para>Gets the directory synchronization search cookie.</para>
        /// </devdoc>
        public void ResetDirectorySynchronizationCookie()
        {
            _cookie = new byte[0];
        }

        /// <include file='doc\DirectorySynchronization.uex' path='docs/doc[@for="DirectorySynchronization.ResetDirectorySynchronizationCookie"]/*' />
        /// <devdoc>
        ///    <para>Resets the directory synchronization search cookie.</para>
        /// </devdoc>
        public void ResetDirectorySynchronizationCookie(byte[] cookie)
        {
            if (cookie == null)
                _cookie = new byte[0];
            else
            {
                _cookie = new byte[cookie.Length];
                for (int i = 0; i < cookie.Length; i++)
                {
                    _cookie[i] = cookie[i];
                }
            }
        }

        /// <include file='doc\DirectorySynchronization.uex' path='docs/doc[@for="DirectorySynchronization.Copy"]/*' />
        /// <devdoc>
        ///    <para>Returns a copy of the current DirectorySynchronization instance.</para>
        /// </devdoc>
        public DirectorySynchronization Copy()
        {
            return new DirectorySynchronization(_flag, _cookie);
        }
    }
}
