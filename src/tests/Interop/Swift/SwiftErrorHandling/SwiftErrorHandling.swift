// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import Foundation

public enum MyError: Error {
    case runtimeError(message: NSString)
}

var errorMessage: NSString = ""

public func setMyErrorMessage(message: UnsafePointer<unichar>, length: Int32) {
    errorMessage = NSString(characters: message, length: Int(length))
}

public func conditionallyThrowError(willThrow: Bool) throws -> Int {
    if willThrow {
        throw MyError.runtimeError(message: errorMessage)
    } else {
        return 42
    }
}

public func getMyErrorMessage(from error: Error) -> UnsafePointer<unichar>? {
    if let myError = error as? MyError {
        switch myError {
        case .runtimeError(let message):
            let buffer = UnsafeMutableBufferPointer<unichar>.allocate(capacity: message.length)
            message.getCharacters(buffer.baseAddress!, range: NSRange(location: 0, length: message.length))
            return UnsafePointer(buffer.baseAddress!)
        }
    }
    return nil
}
