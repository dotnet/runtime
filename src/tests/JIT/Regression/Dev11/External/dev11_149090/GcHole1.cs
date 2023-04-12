// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace GcHole1
{
    public class Sequence : IEnumerable<string>
    {
        IEnumerator IEnumerable.GetEnumerator() { return new Enumerator(); }
        IEnumerator<string> IEnumerable<string>.GetEnumerator() { return new Enumerator(); }

        public class Enumerator : IEnumerator<string>
        {
            private static string[] s_strings = {
                "Index0",
                "Index1"
            };

            private int _indexInSequence = -1;
            private string CurrentString { get { return Enumerator.s_strings[_indexInSequence]; } }

            void IDisposable.Dispose() { return; }
            void IEnumerator.Reset() { throw new NotSupportedException(); }

            bool IEnumerator.MoveNext()
            {
                GC.Collect();

                _indexInSequence++;

                return ((_indexInSequence <= 1) ? true : false);
            }

            object IEnumerator.Current { get { return this.CurrentString; } }
            string IEnumerator<string>.Current { get { return this.CurrentString; } }
        }
    }


    public static class App
    {
        private static bool CheckString(string element)
        {
            Console.WriteLine("ELEMENT: `{0}'", element);
            return ((element == "Index0") ? true : false);
        }


        [Fact]
        public static int TestEntryPoint()
        {
            string result;
            IEnumerable<string> sequence;

            sequence = new Sequence();
            result = sequence.SingleOrDefault(App.CheckString);
            Console.WriteLine("RESULT: `{0}'", result);

            return 100;  //assume if run to completion, the test passes
        }
    }
}
