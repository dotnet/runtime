.globl get_wasm_stack_low
.globl get_wasm_stack_high

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
