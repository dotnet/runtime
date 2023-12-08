// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import Foundation

public enum MyError: Error {
    case runtimeError(message: String)
}

var errorMessage: String = ""

public func setMyErrorMessage(bytes: UnsafePointer<CChar>) {
    errorMessage = String(validatingUTF8: bytes)!
}

public func conditionallyThrowError(willThrow: Bool) throws -> Int {
    if willThrow {
        throw MyError.runtimeError(message: errorMessage)
    } else {
        return 42
    }
}

public func getMyErrorMessage(from error: Error) -> UnsafePointer<UInt8>? {
    if let myError = error as? MyError {
        switch myError {
        case .runtimeError(let message):
            let messageBytes: [UInt8] = Array(message.utf8)
            let buffer = UnsafeMutableBufferPointer<UInt8>.allocate(capacity: messageBytes.count)
            _ = buffer.initialize(from: messageBytes)
            return UnsafePointer(buffer.baseAddress!)
        }
    }
    return nil
}
