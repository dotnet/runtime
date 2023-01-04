/**
 * @fileoverview https://github.com/google/closure-compiler/wiki/Annotating-JavaScript-for-the-Closure-Compiler
 * @externs
 */


const MONO = {}, BINDING = {}, INTERNAL = {}, IMPORTS = {};

const __dotnet_runtime = {};

/** @interface */
function EarlyImports() { }
EarlyImports.prototype.isGlobal = false;
EarlyImports.prototype.isNode = false;
EarlyImports.prototype.isWorker = false;
EarlyImports.prototype.isShell = false;
EarlyImports.prototype.isPThread = false;
EarlyImports.prototype.quit_ = function () { };
EarlyImports.prototype.ExitStatus = {};
EarlyImports.prototype.requirePromise = {};


/** @interface */
function EarlyExports() { }
EarlyExports.prototype.mono = {};
EarlyExports.prototype.binding = {};
EarlyExports.prototype.internal = {};
EarlyExports.prototype.module = {};
EarlyExports.prototype.marshaled_imports = {};

/** @interface */
function EarlyReplacements() { }
EarlyReplacements.prototype.fetch = function () { };
EarlyReplacements.prototype.require = function () { };
EarlyReplacements.prototype.requirePromise = {};
EarlyReplacements.prototype.noExitRuntime = false;
EarlyReplacements.prototype.updateGlobalBufferAndViews = function () { };
EarlyReplacements.prototype.pthreadReplacements = {};
EarlyReplacements.prototype.scriptDirectory = "";
EarlyReplacements.prototype.scriptUrl = "";


/**
 * @param {EarlyImports} imports
 * @param {EarlyExports} exports
 * @param {EarlyReplacements} replacements
 * @param {Object} callbackAPI: any
 */
const __initializeImportsAndExports = function (
    imports,
    exports,
    replacements,
    callbackAPI) { };
const __requirePromise = {};