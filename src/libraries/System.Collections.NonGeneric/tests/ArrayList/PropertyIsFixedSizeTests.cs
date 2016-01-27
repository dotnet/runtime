// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Tests
{
    public class ArrayList_IsFixedSizeTests
    {
        [Fact]
        public void TestGetIsFixedSizeBasic()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList list = null;
            ArrayList fixedList = null;
            IList ilist = null;
            //
            // []vanila - should not be fixed size
            //
            list = new ArrayList();
            Assert.False(list.IsFixedSize);

            //[]just to make sure that we can add values here
            for (int i = 0; i < 100; i++)
            {
                list.Add(i);
            }

            //[]we woill get a fixed size one and then see
            list = new ArrayList();

            fixedList = ArrayList.FixedSize(list);
            Assert.True(fixedList.IsFixedSize);

            //[]just to make sure that we can not add values here
            Assert.Throws<NotSupportedException>(() => fixedList.Add(100));

            //[]we will get one from adapater
            list = ArrayList.Adapter((IList)new ArrayList());

            Assert.False(list.IsFixedSize);

            //[]we will get one from Synchronized for an ArrayList
            list = ArrayList.Synchronized(new ArrayList());

            Assert.False(list.IsFixedSize);

            //[]we will get one from Synchronized for an IList
            ilist = ArrayList.Synchronized((IList)new ArrayList());

            Assert.False(ilist.IsFixedSize);

            //[]we will get one from FixedSize for an IList
            ilist = ArrayList.FixedSize((IList)new ArrayList());
            Assert.True(ilist.IsFixedSize);

            //[]we will get one from ReadOnly for an IList
            ilist = ArrayList.ReadOnly((IList)new ArrayList());
            Assert.True(ilist.IsFixedSize);

            //[]we will get one from ReadOnly for an ArrayList
            list = ArrayList.ReadOnly(new ArrayList());
            Assert.True(list.IsFixedSize);

            //[]we will get one from Range for an ArrayList
            list = (new ArrayList()).GetRange(0, 0);
            Assert.False(list.IsFixedSize);
        }
    }
}
