import Foundation

public enum MyError: Error {
    case runtimeError(String)
}

public func someFuncThatMightThrow (dummy: UnsafeRawPointer, willThrow: Bool) throws -> Int {
    if willThrow { throw MyError.runtimeError ("Catch me if you can!"); }
    else { return 42; }
}

public func handleError(from pointer: UnsafePointer<MyError>) {
    let errorInstance = pointer.pointee
    
    switch errorInstance {
    case .runtimeError(let message):
        print (message);
    default:
        print ("Unhandled error!")
    }
}
