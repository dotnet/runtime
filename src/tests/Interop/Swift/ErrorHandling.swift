enum MyError: Error {
    case runtimeError(String)
}

public func someFuncThatMightThrow () throws -> Int {
    print("Hello from Swift");
    throw MyError.runtimeError ("Catch me if you can");
    return 42;
}