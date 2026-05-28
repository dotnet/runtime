// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/**
 * BuildXL spec for src/tests/baseservices/multidimmarray
 */


@@public
export const enumTest = coreclr_test({
    name: "MultiDimmArray_Enum",
    srcs: ["enum.cs"],
    optimize: true,
    allowUnsafe: true
});
