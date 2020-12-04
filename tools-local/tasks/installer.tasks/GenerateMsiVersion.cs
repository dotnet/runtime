// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;

namespace Microsoft.DotNet.Build.Tasks
{
    // MSI versioning
    // Encode the CLI version to fit into the MSI versioning scheme - https://msdn.microsoft.com/en-us/library/windows/desktop/aa370859(v=vs.85).aspx
    // MSI versions are 3 part
    //                           major.minor.build
    // Size(bits) of each part     8     8    16
    // So we have 32 bits to encode the CLI version

    // For a CLI version based on commit count:
    //   Starting with most significant bit this how the CLI version is going to be encoded as MSI Version
    //   CLI major  -> 6 bits
    //   CLI minor  -> 6 bits
    //   CLI patch  -> 6 bits
    //   CLI commitcount -> 14 bits
    //
    // For a CLI version based on BuildTools versioning
    //   CLI major  -> 5 bits
    //   CLI minor  -> 5 bits
    //   CLI patch  -> 4 bits
    //   BuildNumber major -> 14 bits
    //   BuildNumber minor -> 4 bits
    public class GenerateMsiVersion : BuildTask
    {
        [Required]
        public string Major { get; set; }
        [Required]
        public string Minor { get; set; }
        [Required]
        public string Patch { get; set; }
        public string BuildNumber { get; set; }
        public string BuildNumberMajor { get; set; }
        public string BuildNumberMinor { get; set; }
        [Output]
        public string MsiVersion { get; set; }

        public override bool Execute()
        {
            if(BuildNumber == null && BuildNumberMajor == null)
            {
                Log.LogError("Either BuildNumber or BuildNumberMajor and BuildNumberMinor are required parameters.");
                return false;
            }
            if(BuildNumber != null && BuildNumberMajor != null)
            {
                Log.LogError("You must specify either BuildNumber or BuildNumberMajor and BuildNumberMinor, you cannot specify both parameters.");
                return false;
            }
            if (BuildNumberMajor != null && BuildNumberMinor == null)
            {
                Log.LogError("If you specify a BuildNumberMajor, you must also specify the BuildNumberMinor.");
                return false;
            }
            if(BuildNumber != null)
            {
                ParseBuildNumber();
            }
            else
            {
                ParseBuildNumberMajorMinor();
            }
            return true;
        }
        private void ParseBuildNumber()
        {
            var major = int.Parse(Major) << 26;
            var minor = int.Parse(Minor) << 20;
            var patch = int.Parse(Patch) << 14;
            var msiVersionNumber = major | minor | patch | int.Parse(BuildNumber);

            var msiMajor = (msiVersionNumber >> 24) & 0xFF;
            var msiMinor = (msiVersionNumber >> 16) & 0xFF;
            var msiBuild = msiVersionNumber & 0xFFFF;

            MsiVersion = $"{msiMajor}.{msiMinor}.{msiBuild}";
        }
        private void ParseBuildNumberMajorMinor()
        {
            var major = int.Parse(Major) << 27;
            var minor = int.Parse(Minor) << 22;
            var patch = int.Parse(Patch) << 18;

            var buildNumberMajor = int.Parse(BuildNumberMajor) & 0x3FFF << 4;
            var buildNumberMinor = int.Parse(BuildNumberMinor) & 0xF;

            var msiVersionNumber = major | minor | patch | buildNumberMajor | buildNumberMinor;

            var msiMajor = (msiVersionNumber >> 24) & 0xFF;
            var msiMinor = (msiVersionNumber >> 16) & 0xFF;
            var msiBuild = msiVersionNumber & 0xFFFF;

            MsiVersion = $"{msiMajor}.{msiMinor}.{msiBuild}";
        }

    }
}
