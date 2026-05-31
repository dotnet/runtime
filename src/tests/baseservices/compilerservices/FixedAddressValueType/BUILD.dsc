// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/**
 * BuildXL spec for src/tests/baseservices/compilerservices/FixedAddressValueType
 */


@@public
export const fixedAddressValueType = coreclr_test({
    name: "FixedAddressValueType",
    srcs: ["FixedAddressValueType.cs"],
    optimize: true,
    allowUnsafe: true
});
