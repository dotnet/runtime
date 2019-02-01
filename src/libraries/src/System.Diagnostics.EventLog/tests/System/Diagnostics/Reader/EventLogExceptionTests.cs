// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Eventing.Reader;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogExceptionTests
    {
        [Fact]
        public void EventLogNotFoundException_Ctor()
        {
            Assert.ThrowsAsync<EventLogNotFoundException>(() => throw new EventLogNotFoundException());
            Assert.ThrowsAsync<EventLogNotFoundException>(() => throw new EventLogNotFoundException("message"));
            Assert.ThrowsAsync<EventLogNotFoundException>(() => throw new EventLogNotFoundException("message", new Exception("inner exception")));
        }

        [Fact]
        public void EventLogReadingException_Ctor()
        {
            Assert.ThrowsAsync<EventLogReadingException>(() => throw new EventLogReadingException());
            Assert.ThrowsAsync<EventLogReadingException>(() => throw new EventLogReadingException("message"));
            Assert.ThrowsAsync<EventLogReadingException>(() => throw new EventLogReadingException("message", new Exception("inner exception")));
        }

        [Fact]
        public void EventLogProviderDisabledException_Ctor()
        {
            Assert.ThrowsAsync<EventLogProviderDisabledException>(() => throw new EventLogProviderDisabledException());
            Assert.ThrowsAsync<EventLogProviderDisabledException>(() => throw new EventLogProviderDisabledException("message"));
            Assert.ThrowsAsync<EventLogProviderDisabledException>(() => throw new EventLogProviderDisabledException("message", new Exception("inner exception")));
        }

        [Fact]
        public void EventLogInvalidDataException_Ctor()
        {
            Assert.ThrowsAsync<EventLogInvalidDataException>(() => throw new EventLogInvalidDataException());
            Assert.ThrowsAsync<EventLogInvalidDataException>(() => throw new EventLogInvalidDataException("message"));
            Assert.ThrowsAsync<EventLogInvalidDataException>(() => throw new EventLogInvalidDataException("message", new Exception("inner exception")));
        }
        
    }
}