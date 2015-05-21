// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Test
{
    internal class S
    {
        private T _nvalue;
        public S(T t) { _nvalue = t; }
    }

    internal struct T
    {
        private static T s_stat;
        private S _gcref;

        public T(T t) { t.DoMethod(); _gcref = new S(t); }
        public T(S s) { _gcref = s; }

        private void DoMethod() { }

        private static int Main()
        {
            s_stat =
                new T(new S(new T(new S(new T(new S(new T(new S(new T(new S(
                new T(new S(new T(new S(new T(new S(new T(new S(new T(new S(
                new T(new S(new T(new S(new T(new S(new T(new S(new T(new S(
                new T(new S(new T(new S(new T(new S(new T(new S(new T(new S(
                new T(new S(new T(new S(new T(new S(new T(new S(new T(new S(
                new T(new S(new T(new S(new T(new S(new T(new S(new T(new S(
                        s_stat
                ))))))))))
                ))))))))))
                ))))))))))
                ))))))))))
                ))))))))))
                ))))))))))
                ;
            return 100;
        }
    }
}
