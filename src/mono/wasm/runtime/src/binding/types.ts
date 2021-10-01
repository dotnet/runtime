/* TODO this is from ASP
declare interface BINDING {
    mono_obj_array_new(length: number): System_Array<System_Object>;
    mono_obj_array_set(array: System_Array<System_Object>, index: Number, value: System_Object): void;
    js_string_to_mono_string(jsString: string): System_String;
    js_typed_array_to_array(array: Uint8Array): System_Object;
    js_to_mono_obj(jsObject: any) : System_Object;
    mono_array_to_js_array<TInput, TOutput>(array: System_Array<TInput>) : Array<TOutput>;
    conv_string(dotnetString: System_String | null): string | null;
    bind_static_method(fqn: string, signature?: string): Function;
    call_assembly_entry_point(assemblyName: string, args: any[], signature: any): Promise<any>;
    unbox_mono_obj(object: System_Object): any;
  }
  */