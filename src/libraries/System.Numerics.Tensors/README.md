# System.Numerics.Tensors

Provides APIs for performing primitive operations over tensors represented by spans of memory.

Some shape and storage behavior intentionally differs from NumPy:

- Creating a tensor from an empty shape (`[]`) produces shape `[0]`, with no elements. In NumPy,
  shape `()` has rank zero but contains one scalar element; `np.empty(())` leaves that element
  uninitialized ("empty" refers to initialization, not the number of elements).
- Squeezing shape `[1, 1]` produces shape `[1]`, rather than NumPy's rank-zero shape `()`.
  Both contain one element and can broadcast as a scalar; code that inspects rank, selects
  axes, or indexes the result must account for the retained dimension.
- Strides must be nonnegative. NumPy can represent a reversed view with a negative stride
  (for example, `array[::-1]`); use `Tensor.ReverseDimension` with dimension `0` to
  reverse the first axis instead. This produces a new tensor, not a reversed view.
- When growing a tensor, `Tensor.Resize` and `Tensor.ResizeTo` fill new elements with
  `default(T)`. Resizing `[1, 2]` to five `int` elements yields `[1, 2, 0, 0, 0]`,
  whereas `np.resize` repeats the input and yields `[1, 2, 1, 2, 1]`.
- `Tensor.ResizeTo` and concatenation into an existing destination reject a zero-stride
  dimension with more than one logical element: multiple output indexes would refer to the
  same storage and could not hold distinct values. Zero strides in singleton dimensions
  and empty destinations do not have this conflict.

When strides are omitted, a shape containing a zero-length dimension has zero strides
in every dimension. Its element count and storage requirement are zero regardless of
the other dimension lengths. Negative lengths remain invalid, and explicitly supplied
strides must still satisfy the normal layout validation.

Overlapping sources and destinations are supported for equal-length dense copies, which
use the same overlap-safe behavior as `Span<T>.CopyTo`, and for elementwise operations on identical
non-broadcast views. An in-place reversal of a dense tensor also needs no temporary
storage. Other overlapping tensor layouts throw `ArgumentException` before writing:
copying them correctly could require buffering an amount of data proportional to the
tensor's size. Nonoverlapping strided copies do not create such a buffer.
