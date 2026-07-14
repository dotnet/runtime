// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import Foundation

public enum MyError: Error {
    case runtimeError(message: String)
}

var errorMessage: String = ""

public func setMyErrorMessage(message: UnsafePointer<unichar>, length: Int32) {
    errorMessage = NSString(characters: message, length: Int(length)) as String
}

public func conditionallyThrowError(willThrow: Int32) throws -> Int32 {
    if willThrow != 0 {
        throw MyError.runtimeError(message: errorMessage)
    } else {
        return 42
    }
}

public func getMyErrorMessage(from error: Error, messageLength: inout Int32) -> UnsafePointer<unichar>? {
    if let myError = error as? MyError {
        switch myError {
        case .runtimeError(let message):
            let nsMessage = message as NSString
            let buffer = UnsafeMutableBufferPointer<unichar>.allocate(capacity: nsMessage.length)
            nsMessage.getCharacters(buffer.baseAddress!, range: NSRange(location: 0, length: nsMessage.length))
            messageLength = Int32(nsMessage.length)
            return UnsafePointer(buffer.baseAddress!)
        }
    }
    messageLength = 0
    return nil
}

public func freeStringBuffer(buffer: UnsafeMutablePointer<unichar>) {
    buffer.deallocate()
}

public func nativeFunctionWithCallback(setError: Int32, _ callback: (Int32) -> Void) {
    callback(setError)
}

public func nativeFunctionWithCallback(value: Int32, setError: Int32, _ callback: (Int32, Int32) -> Int32) -> Int32 {
    return callback(value, setError)
}
