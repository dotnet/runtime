public protocol MathLibraryDelegate: AnyObject {
    func mathLibraryDidInitialize()
    func mathLibraryDidDeinitialize()
}

public class MathLibrary {
    // Global/Static functions
    public static func add(_ a: Double, _ b: Double) -> Double {
        return a + b
    }

    public static func subtract(_ a: Double, _ b: Double) -> Double {
        return a - b
    }

    // Function with default params
    public static func multiply(_ a: Double, _ b: Double = 1.0) -> Double {
        return a * b
    }

    // Function with varargs
    public static func average(numbers: Double...) -> Double {
        let sum = numbers.reduce(0, +)
        return sum / Double(numbers.count)
    }

    // Function with closures
    public static func applyOperation(a: Double, b: Double, operation: (Double, Double) -> Double) -> Double {
        return operation(a, b)
    }

    // Computed property
    public static var circleArea: (Double) -> Double = { radius in
        return Double.pi * radius * radius
    }

    public weak var delegate: MathLibraryDelegate?

    // Initializers/Deinitializers
    public init(_internalValue: Int) {
        self._internalValue = _internalValue
        print("MathLibrary initialized.")

        // Notify the delegate that the library was initialized
        delegate?.mathLibraryDidInitialize()

    }

    deinit {
        print("MathLibrary deinitialized.")

        // Notify the delegate that the library was deinitialized
        delegate?.mathLibraryDidDeinitialize()
    }

    // Getters/Setters
    private var _internalValue: Int = 0
    
    public var value: Int {
        get {
            return _internalValue
        }
        set {
            _internalValue = newValue
        }
    }

    public static let shared = MathLibrary()

    // Instance function
    public func factorial() -> Int {
        guard _internalValue >= 0 else {
            fatalError("Factorial is undefined for negative numbers")
        }
        return _internalValue == 0 ? 1 : _internalValue * MathLibrary(_internalValue: _internalValue - 1).factorial()
    }

    public static func singleton () -> MathLibrary {
        
    } 
}
