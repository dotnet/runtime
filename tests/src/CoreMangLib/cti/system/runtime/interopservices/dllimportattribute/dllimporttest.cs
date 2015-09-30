using System;
using System.Runtime.InteropServices;

/// <summary>
/// This namespace and class were created for the DllImport CTI tests
/// </summary>
namespace DllImportTest
{
    /// <summary>
    /// Simple static class containing a DllImport to be used in CoreMangLib\CTI\System\Runtime\InteropServices\DllImportAttribute tests
    /// Since the tests aren't actually calling anything referenced by the DllImport, the imported dll and the function can be fake.
    /// </summary>
    public static class DllImportTestClass
    {

        [DllImport("DllFakeImport.dll")]
        public static extern int BogusFunction();

    }
}