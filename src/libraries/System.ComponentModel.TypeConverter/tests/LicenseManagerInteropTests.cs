// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information. 

using Xunit;
using System.Runtime.InteropServices;

namespace System.ComponentModel.Tests
{
    public class LicenseManagerInteropTests
    {
        [ComImport]
        [CoClass(typeof(SpellCheckerFactoryCoClass))]
        [Guid("8E018A9D-2415-4677-BF08-794EA61F94BB")]
        internal interface SpellCheckerFactoryClass
        {
        }

        [ComImport]
        [Guid("7AB36653-1796-484B-BDFA-E74F1DB7C1DC")]
        [TypeLibType(TypeLibTypeFlags.FCanCreate)]
        [ClassInterface(ClassInterfaceType.None)]
        internal class SpellCheckerFactoryCoClass
        {
        }


        // HasWindowsShell test eliminates IoT, Server Core, and Nano Server
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasWindowsShell), nameof(PlatformDetection.IsWindows))]
        public void CanCreateSpellChecker_Regression_core_1994()
        {
            // regression test for https://github.com/dotnet/core/issues/1994
            // ensure that we can create this object which implements IClassFactory2
            // we're mainly concerned that we don't AV
            try
            {
                var spellCheck = new SpellCheckerFactoryClass();
            }
            catch (InvalidCastException)
            { }
        }
    }
}
