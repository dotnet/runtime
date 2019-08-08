// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Used to cache one exclusive runner reference
    /// </summary>
    internal sealed class ExclusiveReference
    {
        private RegexRunner _ref;
        private RegexRunner _obj;
        private volatile int _locked;

        /// <summary>
        /// Return an object and grab an exclusive lock.
        ///
        /// If the exclusive lock can't be obtained, null is returned;
        /// if the object can't be returned, the lock is released.
        /// </summary>
        public RegexRunner Get()
        {
            // try to obtain the lock

            if (0 == Interlocked.Exchange(ref _locked, 1))
            {
                // grab reference
                RegexRunner obj = _ref;

                // release the lock and return null if no reference
                if (obj == null)
                {
                    _locked = 0;

                    return null;
                }

                // remember the reference and keep the lock
                _obj = obj;

                return obj;
            }

            return null;
        }

        /// <summary>
        /// Release an object back to the cache.
        ///
        /// If the object is the one that's under lock, the lock is released.
        /// If there is no cached object, then the lock is obtained and the object is placed in the cache.
        /// </summary>
        public void Release(RegexRunner obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            // if this reference owns the lock, release it
            if (_obj == obj)
            {
                _obj = null;
                _locked = 0;

                return;
            }

            // if no reference owns the lock, try to cache this reference
            if (_obj == null)
            {
                // try to obtain the lock
                if (0 == Interlocked.Exchange(ref _locked, 1))
                {
                    // if there's really no reference, cache this reference
                    if (_ref == null)
                        _ref = obj;

                    // release the lock
                    _locked = 0;

                    return;
                }
            }
        }
    }
}
