
Object.defineProperty(Module, '__esModule', { value: true });
}());

if (typeof exports === 'object' && typeof module === 'object')
    module.exports = Module;
else if (typeof define === 'function' && define['amd'])
    define([], function () { return Module; });
else if (typeof exports === 'object')
    exports["Module"] = Module;
