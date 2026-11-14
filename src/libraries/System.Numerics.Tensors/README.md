# System.Numerics.Tensors

Provides APIs for performing primitive operations over tensors represented by spans of memory.

Some shape and storage behavior intentionally differs from NumPy:

- Creating a tensor from an empty shape (`[]`) produces shape `[0]`, with no elements. In NumPy,
  shape `()` has rank zero but contains one scalar element; `np.empty(())` leaves that element
  uninitialized ("empty" refers to initialization, not the number of elements).
- `Tensor<T>.Empty` and default tensor spans retain rank-zero metadata but contain no
  elements, unlike NumPy's scalar shape `()`. Tensor computations treat them as empty
  vectors with effective shape `[0]`, corresponding to NumPy's `(0,)`. This includes
  equality, broadcasting, reshaping, stacking, concatenation, splitting, slicing, and
  dimension operations. Explicitly ranked empty shapes retain their axes.
- Squeezing shape `[1, 1]` produces shape `[1]`, rather than NumPy's rank-zero shape `()`.
  Both contain one element and can broadcast as a scalar; code that inspects rank, selects
  axes, or indexes the result must account for the retained dimension.
- Directional broadcasting can discard excess leading singleton dimensions. For example,
  a source with shape `[1, 1, 3]` can broadcast into destination shape `[3]`, whereas
  NumPy's `broadcast_to` rejects a target with fewer dimensions than the source.
  This also applies to tensor copies and elementwise operations with supplied destinations.
  Only leading singleton dimensions are redundant: `[2, 1]` and `[1, 2]` are distinct,
  and zero-length dimensions cannot be discarded. Source metadata and explicitly requested
  destination shapes are preserved.
- Shape equality also ignores leading singleton padding, without broadcasting other
  dimensions. Stacking requires equivalent input shapes; concatenation requires matching
  aligned dimensions except along its selected axis. These operations use the first
  input's effective shape to interpret the axis and determine the result's rank. Other
  inputs and supplied destinations can add or omit leading singleton padding, but cannot
  omit significant axes. For example, stacking `[2]` with `[1, 2]` at axis `0` produces
  `[2, 2]`, while stacking `[1, 2]` with `[2]` at axis `0` produces `[2, 1, 2]`: inserting
  the new axis makes the first input's retained singleton axis non-leading. Existing
  axes are not renumbered or removed from the first input.
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
Empty slices and empty dimension views retain their source's storage origin: endpoint
ranges do not move an empty view beyond its backing storage.

Native-backed tensor spans can have more than `int.MaxValue` logical elements. Operations
retain native-sized lengths and offsets, using bounded spans or indexed iteration rather
than narrowing the total element count. Dense copies preserve overlap-safe ordering.
Index-of-min/max reductions use `TensorPrimitives` over dense spans and dense suffixes.
Other layouts retain the flattened primitive path for span-sized inputs and use bounded
gathered blocks for native-width inputs. Chunk results retain logical native-sized indexes,
including first-NaN and tie handling.

Overlapping sources and destinations are supported for equal-length dense copies, which
use the same overlap-safe behavior as `Span<T>.CopyTo`, and for elementwise operations on identical
non-broadcast views. An in-place reversal of a dense tensor also needs no temporary
storage. Other overlapping tensor layouts throw `ArgumentException` before writing:
copying them correctly could require buffering an amount of data proportional to the
tensor's size. Nonoverlapping strided copies do not create such a buffer.
