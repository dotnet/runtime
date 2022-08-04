// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

class JSData {
    constructor(name) {
        this.name = name;
    }
    echoMemberMethod(arg1) {
        return arg1 + "-w-i-t-h-" + this.name;
    }
    toString() {
        return `JSData("${this.name}")`;
    }
}

class JSTestError extends Error {
    constructor(message) {
        super(message)
    }
}

export function createData(name) {
    //console.log(`createData(name:"${name ? name : '<null>'}")`)
    return new JSData(name);
}

export function createException(name) {
    //console.log(`createException(name:"${name ? name : '<null>'}")`)
    return new JSTestError(name);
}

export function echo1(arg1) {
    if (globalThis.gc) {
        // console.log('globalThis.gc');
        globalThis.gc();
    }

    //console.log(`echo1(arg1:${arg1 !== null ? JSON.stringify(arg1): '<null>'})`)
    return arg1;
}

export function echo1view(arg1, edit) {
    if (globalThis.gc) {
        // console.log('globalThis.gc');
        globalThis.gc();
    }

    // console.log(`echo1view(arg1:${arg1 !== null ? arg1 : '<null>'})`)
    // console.log(`echo1view(arg1:${arg1 !== null ? typeof arg1 : '<null>'})`)
    const cpy = arg1.slice();
    if (edit) {
        cpy[1] = cpy[0]
        arg1.set(cpy);
    }
    return arg1;
}

export function echo1array(arg1, edit) {
    if (globalThis.gc) {
        // console.log('globalThis.gc');
        globalThis.gc();
    }

    // console.log(`echo1view(arg1:${arg1 !== null ? arg1 : '<null>'})`)
    // console.log(`echo1view(arg1:${arg1 !== null ? typeof arg1 : '<null>'})`)
    if (edit) {
        arg1[1] = arg1[0]
    }
    return arg1;
}

export function echo1large(arg1) {
    try {
        arg1._large = new Uint8Array(10000000);
    }
    catch (ex) {
        console.log("echo1large " + ex)
    }
    if (globalThis.gc) {
        // console.log('globalThis.gc');
        globalThis.gc();
    }

    //console.log(`echo1large(arg1:${arg1 !== null ? typeof arg1: '<null>'})`)
    return () => {
        console.log("don't call me");
    };
}

export function store1(arg1) {
    //console.log(`store1(arg1:${arg1 !== null ? arg1 : '<null>'})`)
    globalThis.javaScriptTestHelper.store1val = arg1;
}

export function storeAt(arg1, arg2) {
    //console.log(`storeAt(arg1:${arg1 !== null ? arg1 : '<null>'})`)
    //console.log(`storeAt(arg2:${arg2 !== null ? arg2 : '<null>'})`)
    globalThis.javaScriptTestHelper.store1val = arg1[arg2];
    return arg1[arg2];
}

export function retrieve1() {
    const val = globalThis.javaScriptTestHelper.store1val;
    //console.log(`retrieve1(arg1:${val !== null ? val : '<null>'})`)
    return val;
}

export function throw0() {
    //console.log(`throw0()`)
    throw new Error('throw-0-msg');
}

export function throw1(arg1) {
    //console.log(`throw1(arg1:${arg1 !== null ? arg1 : '<null>'})`)
    throw new Error('throw1-msg ' + arg1);
}

export function throwretrieve1() {
    const val = globalThis.javaScriptTestHelper.store1val;
    //console.log(`retrieve1(arg1:${val !== null ? val : '<null>'})`)
    throw new Error('throwretrieve1 ' + val);
}

export function identity1(arg1) {
    const val = globalThis.javaScriptTestHelper.store1val;
    //console.log(`compare1(arg1:${arg1 !== null ? arg1 : '<null>'}) with ${val !== null ? val : '<null>'}`)
    if (val instanceof Date) {
        return arg1.valueOf() == val.valueOf();
    }
    if (Number.isNaN(val)) {
        return Number.isNaN(arg1);
    }
    return arg1 === val;
}

export function getType1() {
    const val = globalThis.javaScriptTestHelper.store1val;
    const vtype = typeof (val);
    // console.log(`getType1(arg1:${vtype !== null ? vtype : '<null>'})`)
    return vtype;
}

export function getClass1() {
    const val = globalThis.javaScriptTestHelper.store1val;
    const cname = val.constructor.name;
    // console.log(`getClass1(arg1:${cname !== null ? cname : '<null>'})`)
    return cname;
}
let dllExports;
export function invoke1(arg1, name) {
    if (globalThis.gc) {
        // console.log('globalThis.gc');
        globalThis.gc();
    }
    // console.log(`invoke1: ${name}(arg1:${arg1 !== null ? typeof arg1 : '<null>'})`)
    const JavaScriptTestHelper = dllExports.System.Runtime.InteropServices.JavaScript.Tests.JavaScriptTestHelper;
    const fn = JavaScriptTestHelper[name];

    // console.log("invoke1:" + typeof fn);
    // console.log("invoke1:" + fn.toString());
    const res = fn(arg1);
    // console.log(`invoke1: res ${res !== null ? typeof res : '<null>'}`)
    return res;
}

export function invoke2(arg1, name) {
    const fn = dllExports.JavaScriptTestHelperNoNamespace[name];
    //console.log("invoke1:" + fn.toString());
    const res = fn(arg1);
    // console.log(`invoke1: res ${res !== null ? typeof res : '<null>'}`)
    return res;
}

export async function awaitvoid(arg1) {
    // console.log("awaitvoid:" + typeof arg1);
    await arg1;
    // console.log("awaitvoid done");
}

export function thenvoid(arg1) {
    //console.log("thenvoid:" + typeof arg1);
    arg1.then(() => {
        // console.log("thenvoid then done");
    });
    //console.log("thenvoid done");
}

export async function await1(arg1) {
    try {
        // console.log("await1:" + typeof arg1);
        const value = await arg1;
        // console.log("await1 value:" + value);
        return value;
    } catch (ex) {
        // console.log("await1 ex:" + ex);
        throw ex;
    }
}

export async function await2(arg1) {
    //console.log("await2-1:" + typeof arg1);
    await arg1;
    //console.log("await2-2:" + typeof arg1);
}

export async function sleep(ms) {
    if (globalThis.gc) {
        // console.log('globalThis.gc');
        globalThis.gc();
    }
    // console.log("sleep:" + ms);
    await new Promise(resolve => setTimeout(resolve, ms));
    // console.log("sleep2:" + ms);
    return ms;
}

export async function forever() {
    if (globalThis.gc) {
        // console.log('globalThis.gc');
        globalThis.gc();
    }
    // console.log("forever:" + ms);
    await new Promise(() => { });
}

export function back3(arg1, arg2, arg3) {
    if (globalThis.gc) {
        // console.log('globalThis.gc');
        globalThis.gc();
    }
    try {
        if (!(arg1 instanceof Function)) throw new Error('expecting Function!')
        //console.log(`back3(arg1:${arg1 !== null ? typeof (arg1) : '<null>'})`)
        //console.log(`back3(arg2:${arg2 !== null ? arg2 : '<null>'})`)
        //console.log(`back3(arg3:${arg3 !== null ? arg3 : '<null>'})`)

        // call it twice, to make sure it's persistent
        arg1(arg2, arg3);
        //console.log(`back3(arg2:${arg2 !== null ? arg2 : '<null>'})`)
        //console.log(`back3(arg3:${arg3 !== null ? arg3 : '<null>'})`)

        return arg1(arg2, arg3);
    }
    catch (ex) {
        // console.log(`back1 - catch`)
        throw ex;
    }
}

export function backback(arg1, arg2, arg3) {
    if (globalThis.gc) {
        // console.log('globalThis.gc');
        globalThis.gc();
    }
    // console.log('backback A')
    return (brg1, brg2) => {
        // console.log('backback B')
        return arg1(brg1 + arg2, brg2 + arg3);
    }
}

export const instance = {}

globalThis.javaScriptTestHelper = instance;
globalThis.data = new JSData("i-n-s-t-a-n-c-e");
globalThis.rebound = {
    // our JSImport will try to bind it to `globalThis.rebound`
    // but it would stay bound to globalThis.data
    // because once the function is bound, it would stay bound to the first object and can't be re-bound subsequently
    // this line is actually the first binding, not the fact it's part of the class JSData
    echoMemberMethod: globalThis.data.echoMemberMethod.bind(globalThis.data)
}

export async function setup() {
    dllExports = await App.runtime.getAssemblyExports("System.Runtime.InteropServices.JavaScript.Tests.dll");
}

// console.log('JavaScriptTestHelper:' Object.keys(globalThis.JavaScriptTestHelper));
