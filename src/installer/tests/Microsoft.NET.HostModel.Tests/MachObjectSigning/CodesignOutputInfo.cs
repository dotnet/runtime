

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.NET.HostModel.MachO;
using Xunit;

namespace Microsoft.NET.HostModel.Tests;

/// <summary>
/// Info related to the code signature that can be extracted from the output of the `codesign` command.
/// </summary>
internal sealed record CodesignOutputInfo
{
    public string Identifier { get; init; }
    public CodeDirectoryFlags CodeDirectoryFlags { get; init; }
    public CodeDirectoryVersion CodeDirectoryVersion { get; init; }
    public ulong ExecutableSegmentBase { get; init; }
    public ulong ExecutableSegmentLimit { get; init; }
    public ExecutableSegmentFlags ExecutableSegmentFlags { get; init; }
    public byte[][] SpecialSlotHashes { get; init; }
    public byte[][] CodeHashes { get; init; }

    public bool Equals(CodesignOutputInfo? obj)
    {
        if (obj is not CodesignOutputInfo other)
            return false;

        return Identifier == other.Identifier &&
            CodeDirectoryFlags == other.CodeDirectoryFlags &&
            CodeDirectoryVersion == other.CodeDirectoryVersion &&
            ExecutableSegmentBase == other.ExecutableSegmentBase &&
            ExecutableSegmentLimit == other.ExecutableSegmentLimit &&
            ExecutableSegmentFlags == other.ExecutableSegmentFlags &&
            SpecialSlotHashes.Length == other.SpecialSlotHashes.Length &&
            CodeHashes.Length == other.CodeHashes.Length &&
            SpecialSlotHashes.Zip(other.SpecialSlotHashes, static (a, b) => a.SequenceEqual(b)).All(static x => x) &&
            CodeHashes.Zip(other.CodeHashes, static (a, b) => a.SequenceEqual(b)).All(static x => x);
    }

    public override string ToString()
    {
        return $$"""
                Identifier: {{Identifier}},
                CodeDirectoryFlags: {{CodeDirectoryFlags}},
                CodeDirectoryVersion: {{CodeDirectoryVersion}},
                ExecutableSegmentBase: {{ExecutableSegmentBase}},
                ExecutableSegmentLimit: {{ExecutableSegmentLimit}},
                ExecutableSegmentFlags: {{ExecutableSegmentFlags}},
                SpecialSlotHashes: [{{string.Join(", ", SpecialSlotHashes.Select(h => BitConverter.ToString(h).Replace("-", "")))}}],
                CodeHashes: [{{string.Join(", ", CodeHashes.Select(h => BitConverter.ToString(h).Replace("-", "")))}}]
                """;
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(Identifier, CodeDirectoryFlags, CodeDirectoryVersion, ExecutableSegmentLimit, ExecutableSegmentFlags, SpecialSlotHashes, CodeHashes);
    }

    /// <summary>
    /// Parses the output of the `codesign` command to extract codesign information.
    /// </summary>
    public static CodesignOutputInfo ParseFromCodeSignOutput(string output)
    {
        var splitOptions = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
        var lines = output.Split(new[] { '\n', '\r' }, splitOptions);
        var Identifier = lines[1].Split('=', splitOptions)[1];
        var cdInfo = lines[3].Split(' ');
        var CodeDirectoryVersion = (CodeDirectoryVersion)Convert.ToUInt32(cdInfo[1].Split('=', splitOptions)[1], 16);
        var CodeDirectoryFlags = (CodeDirectoryFlags)Convert.ToUInt32(cdInfo[3].Split(['=', '('], splitOptions)[1].TrimStart("0x").ToString(), 16);
        Assert.True(lines[13].StartsWith("Executable Segment base="), "Expected 'Executable Segment base=' at line 13");
        Assert.True(lines[14].StartsWith("Executable Segment limit="), "Expected 'Executable Segment limit=' at line 14");
        Assert.True(lines[15].StartsWith("Executable Segment flags="), "Expected 'Executable Segment flags=' at line 15");
        var ExecutableSegmentBase = ulong.Parse(lines[13].Split('=', splitOptions)[1]);
        var ExecutableSegmentLimit = ulong.Parse(lines[14].Split('=', splitOptions)[1]);
        var ExecutableSegmentFlags = (ExecutableSegmentFlags)Convert.ToUInt64(lines[15].Split('=', splitOptions)[1].TrimStart("0x").ToString(), 16);
        Assert.True(lines[16].StartsWith("Page size=4096"), "Expected 'Page size=4096' at line 16");
        var (SpecialSlotHashes, CodeHashes) = ExtractHashes(lines.Skip(17));

        return new CodesignOutputInfo
        {
            Identifier = Identifier,
            CodeDirectoryFlags = CodeDirectoryFlags,
            CodeDirectoryVersion = CodeDirectoryVersion,
            ExecutableSegmentBase = ExecutableSegmentBase,
            ExecutableSegmentLimit = ExecutableSegmentLimit,
            ExecutableSegmentFlags = ExecutableSegmentFlags,
            SpecialSlotHashes = SpecialSlotHashes,
            CodeHashes = CodeHashes,
        };

        static (byte[][] SpecialSlotHashes, byte[][] CodeHashes) ExtractHashes(IEnumerable<string> lines)
        {
            List<byte[]> specialSlotHashes = [];
            List<byte[]> codeHashes = [];
            foreach (var line in lines)
            {
                if (line[0] is not ('-' or '0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9'))
                    break;
                var hash = line.Split('=')[1].Trim();
                var index = int.Parse(line.Split('=')[0].Trim());
                if (index < 0)
                {
                    // specialSlot
                    specialSlotHashes.Add(ParseByteArray(hash));
                }
                else
                {
                    // codeHashes
                    codeHashes.Add(ParseByteArray(hash));
                }
            }
            return (specialSlotHashes.ToArray(), codeHashes.ToArray());
        }
        static byte[] ParseByteArray(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have an even length.");
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }
    }

    public const string SampleCodesignOutput = """
    Executable=/Users/jacksonschuster/source/runtime3/artifacts/bin/osx-x64.Debug/corehost/singlefilehost
    Identifier=singlefilehost-5555494409d4df688bf436b291061028f736b11c
    Format=Mach-O thin (x86_64)
    CodeDirectory v=20400 size=89264 flags=0x2(adhoc) hashes=2778+7 location=embedded
    VersionPlatform=1
    VersionMin=786432
    VersionSDK=984064
    Hash type=sha256 size=32
    CandidateCDHash sha256=6fee638e9fe544a66b0acf9489ebc59e073b3e39
    CandidateCDHashFull sha256=6fee638e9fe544a66b0acf9489ebc59e073b3e39082903247c5413f555ec2857
    Hash choices=sha256
    CMSDigest=6fee638e9fe544a66b0acf9489ebc59e073b3e39082903247c5413f555ec2857
    CMSDigestType=2
    Executable Segment base=0
    Executable Segment limit=8949760
    Executable Segment flags=0x1
    Page size=4096
        -7=4d8d4b9e4116e8edd996176b5553463acb64287bb635e7f141155529e20457bc
        -6=0000000000000000000000000000000000000000000000000000000000000000
        -5=cca8afe72425463c13b813da9ae468ae3b5fe20fe5fe1d3f34302ba2f15722f2
        -4=0000000000000000000000000000000000000000000000000000000000000000
        -3=0000000000000000000000000000000000000000000000000000000000000000
        -2=987920904eab650e75788c054aa0b0524e6a80bfc71aa32df8d237a61743f986
        -1=0000000000000000000000000000000000000000000000000000000000000000
        0=20042993665611bf5d01d35a46092c2d43a07883f31247a03b5600c301f5c039
        1=a97fad07cc9d6eabad27a77e32b69c3da59372fa7987a13c2b8d23f378380476
        2=ad7facb2586fc6e966c004d7d1d16b024f5805ff7cb47c7a85dabd8b48892ca7
        3=ad7facb2586fc6e966c004d7d1d16b024f5805ff7cb47c7a85dabd8b48892ca7
        4=ad7facb2586fc6e966c004d7d1d16b024f5805ff7cb47c7a85dabd8b48892ca7
        5=b3d230340aa5ed09c788c39081c207a7430b83d22c9489d84d4ede3ed320f47b
        6=825b7aa16170a9b739a4689ba8878391bcae87efd63e3b174738c382020031c1
        7=e360159ee0adaeba5ac5f562c45ec551dbe8b73fbc858beca298610312df33b3
        8=20585ef0bc0287c5b7a9b54f2669704cdc31cea7d7b1702b336fcf93a9f01ca2
        9=414ae6563e5881b215a08bb33fc539fb0c90c3a5532f6e15a726ed6cdc255550
        10=b672b667eb31b48d027bd5f1cf75bad5a8552b4d6b649cbdae35699152fb8a1b
    CDHash=6fee638e9fe544a66b0acf9489ebc59e073b3e39
    Signature=adhoc
    Info.plist=not bound
    TeamIdentifier=not set
    Sealed Resources=none
    Internal requirements count=0 size=12
    """;
}
