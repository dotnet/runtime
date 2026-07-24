using System;
using Xunit;
using Moq;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using System.Reflection;

namespace Microsoft.Diagnostics.DataContractReader.Tests
{
    public class BugRepro
    {
        [Fact]
        public unsafe void TestNullHandle()
        {
            var arch = new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true };
            var builder = new TestPlaceholderTarget.Builder(arch);
            var mockTarget = builder.Build();
            var mockLegacy = new Mock<IXCLRDataProcess>();
            
            IXCLRDataProcess process = new SOSDacImpl(mockTarget, mockLegacy.Object);
            
            // Check if _legacyProcess is not null
            var legacyField = process.GetType().GetField("_legacyProcess", BindingFlags.Instance | BindingFlags.NonPublic);
            System.Console.WriteLine($"_legacyProcess is not null: {legacyField.GetValue(process) != null}");
            
            try {
                int hr = process.StartEnumMethodDefinitionsByAddress(0x1000, null);
                System.Console.WriteLine($"hr = {hr}");
            } catch (Exception ex) {
                System.Console.WriteLine($"Exception: {ex}");
            }
        }
    }
}
