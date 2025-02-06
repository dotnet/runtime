.globl wasm_atomic_fence

.globaltype __stack_pointer, i32

# simulate atomic.fence
wasm_atomic_fence:
  .functype wasm_atomic_fence () -> ()
  global.get __stack_pointer
  i32.const 0
  i32.atomic.rmw.or 0
  drop
  end_function
