import Foundation

public enum MyError: Error {
    case runtimeError(message: String)
}

public func someFuncThatMightThrow (willThrow: Bool, dummy: UnsafeRawPointer) throws -> Int {
    if willThrow { throw MyError.runtimeError (message: "Catch me if you can!"); }
    else { return 42; }
}

public func handleError(from pointer: UnsafePointer<MyError>) {
    let pointerValue = UInt(bitPattern: pointer)
    let offsetPointerValue = pointerValue + 0x48 // 0x20 on amd64
    let offsetPointer = UnsafeRawPointer(bitPattern: offsetPointerValue)
    
    if let offsetErrorPointer = offsetPointer?.assumingMemoryBound(to: MyError.self) {
        let errorInstance = offsetErrorPointer.pointee
        switch errorInstance {
        case .runtimeError(let message):
            print(message)
        }
    } else {
        print("Pointer does not point to MyError.")
    }
    
}
