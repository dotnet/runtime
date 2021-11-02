import { Module } from "./modules";

const _temp_mallocs: Array<Array<VoidPtr> | null> = [];

export function temp_malloc(size: number): VoidPtr {
    if (!_temp_mallocs || !_temp_mallocs.length)
        throw new Error("No temp frames have been created at this point");

    const frame = _temp_mallocs[_temp_mallocs.length - 1] || [];
    const result = Module._malloc(size);
    frame.push(result);
    _temp_mallocs[_temp_mallocs.length - 1] = frame;
    return result;
}

export function _create_temp_frame(): void {
    _temp_mallocs.push(null);
}

export function _release_temp_frame(): void {
    if (!_temp_mallocs.length)
        throw new Error("No temp frames have been created at this point");

    const frame = _temp_mallocs.pop();
    if (!frame)
        return;

    for (let i = 0, l = frame.length; i < l; i++)
        Module._free(frame[i]);
}
