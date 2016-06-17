using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class CheckConnStrSetupFactAttribute : FactAttribute
    {
        public CheckConnStrSetupFactAttribute()
        {
            if(!DataTestUtility.AreConnStringsSetup())
            {
                Skip = "Connection Strings Not Setup";
            }
        }
    }
}
