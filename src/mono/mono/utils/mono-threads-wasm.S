.globl get_wasm_stack_low
.globl get_wasm_stack_high
.globl wasm_atomic_fence

.globaltype __stack_pointer, i32

get_wasm_stack_low:
  .functype get_wasm_stack_low () -> (i32)
  global.get __stack_low@GOT
  // align up to 16 bytes
  i32.const 0xF
  i32.add
  i32.const -0x10
  i32.and
  end_function

get_wasm_stack_high:
  .functype get_wasm_stack_high () -> (i32)
  global.get __stack_high@GOT
  end_function

# simulate atomic.fence
wasm_atomic_fence:
  .functype wasm_atomic_fence () -> ()
  global.get __stack_pointer
  i32.const 0
  i32.atomic.rmw.or 0
  drop
  end_function
