import Foundation

public enum MyError: Error {
    case runtimeError(message: String, info: String)
}

public func someFuncThatMightThrow (willThrow: Bool, dummy: UnsafeRawPointer) throws -> Int {
    if willThrow { throw MyError.runtimeError (message: "Catch me if you can!", info: "abcd abcd"); }
    else { return 42; }
}

public func handleError(from pointer: UnsafePointer<MyError>) {
    let pointerValue = UInt(bitPattern: pointer)
    let offsetPointerValue = pointerValue + 0x48
    let offsetPointer = UnsafeRawPointer(bitPattern: offsetPointerValue)
    
    if let offsetErrorPointer = offsetPointer?.assumingMemoryBound(to: MyError.self) {
        let errorInstance = offsetErrorPointer.pointee
        switch errorInstance {
        case .runtimeError(let message, _):
            print(message)
        }
    } else {
        print("Pointer does not point to MyError.")
    }
    
}
