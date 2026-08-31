// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Eventing.Reader;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogExceptionTests
    {
        [Fact]
        public void EventLogNotFoundException_Ctor()
        {
            Assert.Throws<EventLogNotFoundException>(new Action(() => { throw new EventLogNotFoundException(); }));
            Assert.Throws<EventLogNotFoundException>(new Action(() => { throw new EventLogNotFoundException("message"); }));
            Assert.Throws<EventLogNotFoundException>(new Action(() => { throw new EventLogNotFoundException("message", new Exception("inner exception")); }));
        }

        [Fact]
        public void EventLogReadingException_Ctor()
        {
            Assert.Throws<EventLogReadingException>(new Action(() => { throw new EventLogReadingException(); }));
            Assert.Throws<EventLogReadingException>(new Action(() => { throw new EventLogReadingException("message"); }));
            Assert.Throws<EventLogReadingException>(new Action(() => { throw new EventLogReadingException("message", new Exception("inner exception")); }));
        }

        [Fact]
        public void EventLogProviderDisabledException_Ctor()
        {
            Assert.Throws<EventLogProviderDisabledException>(new Action(() => { throw new EventLogProviderDisabledException(); }));
            Assert.Throws<EventLogProviderDisabledException>(new Action(() => { throw new EventLogProviderDisabledException("message"); }));
            Assert.Throws<EventLogProviderDisabledException>(new Action(() => { throw new EventLogProviderDisabledException("message", new Exception("inner exception")); }));
        }

        [Fact]
        public void EventLogInvalidDataException_Ctor()
        {
            Assert.Throws<EventLogInvalidDataException>(new Action(() => throw new EventLogInvalidDataException()));
            Assert.Throws<EventLogInvalidDataException>(new Action(() => throw new EventLogInvalidDataException("message")));
            Assert.Throws<EventLogInvalidDataException>(new Action(() => throw new EventLogInvalidDataException("message", new Exception("inner exception"))));
        }

    }
}
