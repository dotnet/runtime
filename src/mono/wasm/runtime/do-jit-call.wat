(module
  (import "i" "memory" (memory 0))
  (func $jit_call_cb (import "i" "jit_call_cb") (param i32))
  (func $do_jit_call_indirect (export "do_jit_call_indirect") (param $unused i32) (param $cb_data i32) (param $thrown i32)
    try
        local.get $cb_data
        call $jit_call_cb
    catch_all
        local.get $thrown
        i32.const 1
        i32.store
    end
  )
)
