// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <summary>
/// Errors codes the application may return.
/// The codes are divided up into buckets at the Phone team's behest so that they can easily
/// triage errors.
/// </summary>
enum ErrorCodes
{
    Success = 0,

    //
    // Bucket 1: Deployment / Usage
    //           Errors that the Marketplace team should address in their deployment
    //

    // 1000+: tool usage errors
    EmptyFileList = 1000,
    IncorrectInputFolder = 1001,
    IncorrectOutputFolder = 1002,
    IncorrectConfigFile = 1003,
    InvalidArgument = 1004,
    CrossGenNotOnPath = 1005,
    InvalidAppManifest = 1006,
    IncorrectPlatformFolder = 1007,
    IncorrectFrameworkFolder = 1008,
    IncorrectMakePriPath = 1009,
    IncorrectRcPath = 1009, 

    // 1200+: Errors getting crossgen off the ground and running
    UnknownCrossgenError = 1200,
    CLRInitError = 1201,
    InvalidCrossgenArguments = 1202,
    AssemblyNotFound = 1203,

    // 1300+: Errors that occur while processing the app
    InputFileReadError = 1300,
    InputPackageFolderStructureTooComplex = 1301,
    SecurityException = 1302,
    UnauthorizedAccessException = 1303,
    PathTooLongException = 1304,
    NotSupported = 1305,
    CoreRuntimeLinkageError = 1306,
    InternalWireUpCoreRuntimeError = 1307,

    // 1400+: Errors from the OS that were unexpected (ie, not specifically caused by user input or config)
    FailedCreatingJobObject = 1400,

    //
    // Bucket 2: Invalid IL
    //           Expected errors that occur because the user's application was malformed
    //

    CompilationFailedBadInputIL = 2001,
    ExceededMaximumMethodSize =   2002,

    //
    // Bucket 3: Transient Errors
    //           Errors most likely caused by machine state that could pass given a second chance
    //
    CrossgenTimeout = 3000,
    InsufficientMemory = 3001,
    InsufficientDiskSpace = 3003,
    SharingViolation = 3004,
    
    //
    // Bucket 4: Unexpected Errors
    //           Errors that most likely indicate a bug in the runtime and need investigation by the CLR team
    //
    CorEExecutionEngine = 4000,
    CorESecurity = 4001,
    CldbEInternalError = 4002,
    AccessViolation = 4004,
    AppCompileInternalFailure = 4005

}
