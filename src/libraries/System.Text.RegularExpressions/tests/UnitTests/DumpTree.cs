using System.Globalization;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class DumpTreeTest
    {
        [Fact]
        public void DumpTreeForBugPattern()
        {
            // This parses and optimizes (FinalOptimize is called inside Parse)
            var tree = RegexParser.Parse(@"a(b.*?c)?d", RegexOptions.None, CultureInfo.InvariantCulture);
            string treeStr = tree.Root.ToString();
            
            // Also check what RegexWriter produces
            var code = RegexWriter.Write(tree);
            
            // Format the opcodes
            string opcodes = string.Join(",", code.Codes);
            
            // Throw to see the output
            throw new Exception($"Tree:\n{treeStr}\nOpcodes: [{opcodes}]");
        }
    }
}
