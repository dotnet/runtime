// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Sharpliner;
using Sharpliner.AzureDevOps;

namespace Pipelines;

public record Platform(
    string Name,
    string OsGroup,
    string Architecture,
    string? TargetRid = null,
    string? PlatformId = null,
    string? OsSubGroup = null,
    string? HostedOs = null,
    object? Container = null,
    string? RuntimeFlavor = null,
    bool CrossBuild = false,
    string? CrossRootFsDir = null,
    IEnumerable<string>? RunForPlatforms = null,
    TemplateParameters? AdditionalJobParams = null);

public class PlatformMatrix : PlatformMatrixBase
{
    // These fields can be used to restrict which build legs run in your PR.
    //
    // Set this to only dis/allow platforms that contain given substrings.
    // E.g. add "linux" if you only want to filter platforms containing "linux" in their name.
    // Then "dotnet build" this project and commit the YAML.
    //
    // First allowed platforms are filtered, then disallowed filter is applied.
    //
    // Example "Run all linux non-arm legs":
    //   - allowed  = [ "linux" ]
    //   - disallowed = [ "arm" ]
    protected override List<string> AllowedPlatforms { get; } = new() { };
    protected override List<string> DisallowedPlatforms { get; } = new() { };

    public override TargetPathType TargetPathType => TargetPathType.RelativeToGitRoot;

    public override string TargetFile => "eng/pipelines/common/platform-matrix.yml";

    // TODO: We can extract container names somewhere in one place
    private const string AndroidContainer = "ubuntu-18.04-android-20220808192756-8fcaabc";

    protected override List<Platform> Platforms => new()
    {
        new("Linux_arm", "Linux", "arm",
            Container: "ubuntu-18.04-cross-arm-20220907130538-70ed2e8",
            CrossBuild: true,
            CrossRootFsDir: "/crossrootfs/arm",
            RunForPlatforms: new[] { "all", "gcstress" }),

        new("Linux_armv6", "Linux", "armv6",
            Container: "ubuntu-20.04-cross-armv6-raspbian-10-20211208135931-e6e3ac4",
            CrossBuild: true,
            CrossRootFsDir: "/crossrootfs/armv6"),

        new("Linux_arm64", "Linux", "arm64",
            Container:
                new TemplateParameters
                {
                    { If.Equal("parameters.container", "''"), new TemplateParameters
                    {
                        { "image", "ubuntu-18.04-cross-arm64-20220907130538-70ed2e8" }
                    }},
                    { If.NotEqual("parameters.container", "''"), new TemplateParameters
                    {
                        { "image", parameters["container"] }
                    }},
                    { "registry", "mcr" }
                },
            CrossBuild: true,
            CrossRootFsDir: "/crossrootfs/arm64",
            RunForPlatforms: new[] { "all", "gcstress" }),

        new("Linux_musl_x64", "Linux", "x64",
            OsSubGroup: "_musl",
            Container: "alpine-3.13-WithNode-20210910135845-c401c85",
            RunForPlatforms: new[] { "all" }),

        new("Linux_musl_arm", "Linux", "arm",
            OsSubGroup: "_musl",
            Container: "ubuntu-16.04-cross-arm-alpine-20210923140502-78f7860",
            CrossBuild: true,
            CrossRootFsDir: "/crossrootfs/arm",
            RunForPlatforms: new[] { "all" }),

        new("Linux_musl_arm64", "Linux", "arm64",
            OsSubGroup: "_musl",
            Container: "ubuntu-16.04-cross-arm64-alpine-20210923140502-78f7860",
            CrossBuild: true,
            CrossRootFsDir: "/crossrootfs/arm64",
            RunForPlatforms: new[] { "all" }),

        new("Linux_bionic_arm64", "Linux", "arm64",
            OsSubGroup: "_bionic",
            Container: "ubuntu-18.04-android-20220808192756-8fcaabc",
            RuntimeFlavor: "mono",
            AdditionalJobParams: new() { { "runScriptWindowsCmd", true } }),

        new("Linux_bionic_arm64", "Linux", "x64",
            OsSubGroup: "_bionic",
            Container: "ubuntu-18.04-android-20220808192756-8fcaabc",
            RuntimeFlavor: "mono"),

        new("Linux_x64", "Linux", "x64", "linux-x64",
            Container:
                new TemplateParameters
                {
                    { If.Equal("parameters.container", "''"), new TemplateParameters
                    {
                        { "image", "centos-7-20210714125435-9b5bbc2" }
                    }},
                    { If.NotEqual("parameters.container", "''"), new TemplateParameters
                    {
                        { "image", parameters["container"] }
                    }},
                    { "registry", "mcr" }
                },
            RunForPlatforms: new[] { "all" }),

        new("Linux_x86", "Linux", "x86",
            Container: "ubuntu-18.04-cross-x86-linux-20211022152824-f853169",
            CrossBuild: true,
            CrossRootFsDir: "/crossrootfs/x86",
            AdditionalJobParams: new()
            {
                { "disableClrTest", true }
            }),

        new("SourceBuild_Linux_x64", "Linux", "x64",
            Container: "centos-7-source-build-20210714125450-5d87b80",
            AdditionalJobParams: new()
            {
                { "buildingOnSourceBuildImage", true }
            }),

        new("Linux_s390x", "Linux", "s390x",
            Container: "ubuntu-18.04-cross-s390x-20201102145728-d6e0352",
            CrossBuild: true,
            CrossRootFsDir: "/crossrootfs/s390x"),

        new("Linux_ppc64le", "Linux", "ppc64le",
            Container: "ubuntu-18.04-cross-ppc64le-20220531132048-b9de666",
            CrossBuild: true,
            CrossRootFsDir: "/crossrootfs/ppc64le"),

        new("Browser_wasm", "Browser", "wasm",
            Container: "ubuntu-18.04-webassembly-20220531132048-00a561c",
            HostedOs: "Linux"),

        new("Browser_wasm_firefox", "Browser", "wasm",
            PlatformId: "Browser_wasm_win",
            HostedOs: "Linux",
            Container: "ubuntu-18.04-webassembly-20220531132048-00a561c"),

        new("Browser_wasm_win", "Browser", "wasm",
            PlatformId: "Browser_wasm_win",
            HostedOs: "windows"),

        new("FreeBSD_x64", "FreeBSD", "x64",
            Container: "ubuntu-18.04-cross-freebsd-12-20210917001307-f13d79e"),

        new("Android_x64", "Android", "x64",
            Container: AndroidContainer,
            RuntimeFlavor: "mono"),

        new("Android_x86", "Android", "x86",
            Container: AndroidContainer,
            RuntimeFlavor: "mono"),

        new("Android_arm", "Android", "arm",
            Container: AndroidContainer,
            RuntimeFlavor: "mono"),

        new("Android_arm64", "Android", "arm64",
            Container: AndroidContainer,
            RuntimeFlavor: "mono"),

        new("MacCatalyst_x64", "MacCatalyst", "x64",
            RuntimeFlavor: "mono"),

        new("MacCatalyst_arm64", "MacCatalyst", "arm64",
            RuntimeFlavor: "mono"),

        new("tvOS_arm64", "tvOS", "arm64",
            RuntimeFlavor: "mono"),

        new("tvOSSimulator_x64", "tvOSSimulator", "x64",
            RuntimeFlavor: "mono"),

        new("tvOSSimulator_arm64", "tvOSSimulator", "arm64",
            RuntimeFlavor: "mono"),

        new("iOS_arm", "iOS", "arm",
            RuntimeFlavor: "mono"),

        new("iOS_arm64", "iOS", "arm64",
            RuntimeFlavor: "mono"),

        new("iOSSimulator_x64", "iOSSimulator", "x64",
            RuntimeFlavor: "mono"),

        new("iOSSimulator_x86", "iOSSimulator", "x86",
            RuntimeFlavor: "mono",
            AdditionalJobParams: new()
            {
                { "managedTestBuildOsGroup", "OSX" }
            }),

        new("iOSSimulator_arm64", "iOSSimulator", "arm64",
            RuntimeFlavor: "mono"),

        new("OSX_arm64", "OSX", "arm64",
            CrossBuild: true),

        new("OSX_x64", "OSX", "x64",
            RunForPlatforms: new[] { "all" }),

        new("Tizen_armel", "Tizen", "armel",
            Container: "ubuntu-18.04-cross-armel-tizen-20210719212651-8b02f56",
            CrossBuild: true,
            CrossRootFsDir: "/crossrootfs/armel",
            AdditionalJobParams: new()
            {
                { "disableClrTest", true }
            }),

        new("windows_x64", "windows", "x64",
            TargetRid: "win-x64",
            RunForPlatforms: new[] { "all", "gcstress" }),

        new("windows_x86", "windows", "x86",
            TargetRid: "win-x86",
            RunForPlatforms: new[] { "all", "gcstress" }),

        new("windows_arm", "windows", "arm",
            TargetRid: "win-arm",
            RunForPlatforms: new[] { "all" }),

        new("windows_arm64", "windows", "arm64",
            TargetRid: "win-arm64",
            RunForPlatforms: new[] { "all", "gcstress" }),
    };
}
