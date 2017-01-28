using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class KeyInfoTests
    {
        [Fact]
        public void Constructor()
        {
            KeyInfo keyInfo = new KeyInfo();

            Assert.Equal(0, keyInfo.Count);
            Assert.Equal(null, keyInfo.Id);
        }


    }
}
