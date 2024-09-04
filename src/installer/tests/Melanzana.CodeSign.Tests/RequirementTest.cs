using Xunit;
using System.IO;
using System.Linq;
using Melanzana.CodeSign.Requirements;

namespace Melanzana.CodeSign.Tests
{
    public class RequirementTest
    {
        [Fact]
        public void DefaultRequirement()
        {
            var defaultReq = RequirementSet.CreateDefault("BUNDLE ID", "FRIENDLY NAME")[RequirementType.Designated];
            var stringExpr = "identifier \"BUNDLE ID\" and anchor apple generic and certificate leaf[subject.CN] = \"FRIENDLY NAME\" and certificate 1[field.1.2.840.113635.100.6.2.1] /* exists */";

            // Check string representation
            Assert.Equal(stringExpr, defaultReq.ToString());

            // Check binary round-trip
            var expr = new byte[defaultReq.Expression.Size];
            defaultReq.Expression.Write(expr, out var exprBytesWritten);
            Assert.Equal(expr.Length, exprBytesWritten);
            var readExpression = Expression.FromBlob(expr);
            Assert.Equal(stringExpr, readExpression.ToString());
        }
    }
}
