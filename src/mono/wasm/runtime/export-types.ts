import { DotNetExports } from "./exports";
import { EmscriptenModuleConfig } from "./types";

// this is the only public export from the dotnet.js module
declare function createDotnetRuntime(moduleFactory: (api: DotNetExports) => EmscriptenModuleConfig): Promise<DotNetExports>;
export default createDotnetRuntime;

