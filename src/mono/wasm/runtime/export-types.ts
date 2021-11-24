import { DotNetPublicAPI } from "./exports";
import { EmscriptenModuleConfig } from "./types";

// this is the only public export from the dotnet.js module
declare function createDotnetRuntime(moduleFactory: (api: DotNetPublicAPI) => EmscriptenModuleConfig): Promise<DotNetPublicAPI>;
export default createDotnetRuntime;

