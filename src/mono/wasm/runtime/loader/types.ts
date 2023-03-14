import type { APIType, DotnetModuleConfig, EarlyExports, EarlyImports, MonoConfig } from "../types";
import type { EmscriptenModule } from "../types/emscripten";

export interface DotnetHostBuilder {
    withConfig(config: MonoConfig): DotnetHostBuilder
    withConfigSrc(configSrc: string): DotnetHostBuilder
    withApplicationArguments(...args: string[]): DotnetHostBuilder
    withEnvironmentVariable(name: string, value: string): DotnetHostBuilder
    withEnvironmentVariables(variables: { [i: string]: string; }): DotnetHostBuilder
    withVirtualWorkingDirectory(vfsPath: string): DotnetHostBuilder
    withDiagnosticTracing(enabled: boolean): DotnetHostBuilder
    withDebugging(level: number): DotnetHostBuilder
    withMainAssembly(mainAssemblyName: string): DotnetHostBuilder
    withApplicationArgumentsFromQuery(): DotnetHostBuilder
    create(): Promise<RuntimeAPI>
    run(): Promise<number>
}

// this represents visibility in the javascript
// like https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web.JS/src/Platform/Mono/MonoTypes.ts
export type RuntimeAPI = {
    /**
     * @deprecated Please use API object instead. See also MONOType in dotnet-legacy.d.ts
     */
    MONO: any,
    /**
     * @deprecated Please use API object instead. See also BINDINGType in dotnet-legacy.d.ts
     */
    BINDING: any,
    INTERNAL: any,
    Module: EmscriptenModule,
    runtimeId: number,
    runtimeBuildInfo: {
        productVersion: string,
        gitHash: string,
        buildConfiguration: string,
    }
} & APIType

declare function initializeImportsAndExports(
    imports: EarlyImports,
    exports: EarlyExports,
): RuntimeAPI;

export type RuntimeModuleAPI = {
    initializeImportsAndExports: typeof initializeImportsAndExports;
}
export type EmscriptenModuleAPI = (moduleConfig: DotnetModuleConfig) => Promise<void>
