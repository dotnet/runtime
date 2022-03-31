class JSData {
    constructor(name) {
        this.name = name;
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


class JavaScriptTestHelper {
    createData(name) {
        //console.log(`createData(name:"${name ? name : '<null>'}")`)
        return new JSData(name);
    }

    createException(name) {
        //console.log(`createException(name:"${name ? name : '<null>'}")`)
        return new JSTestError(name);
    }

    echo1(arg1) {
        // console.log(`echo1(arg1:${arg1 !== null ? JSON.stringify(arg1): '<null>'})`)
        return arg1;
    }

    store1(arg1) {
        // console.log(`store1(arg1:${arg1 !== null ? arg1 : '<null>'})`)
        globalThis.javaScriptTestHelper.store1val = arg1;
    }

    retrieve1() {
        const val = globalThis.javaScriptTestHelper.store1val;
        // console.log(`retrieve1(arg1:${val !== null ? val : '<null>'})`)
        return val;
    }

    throw0() {
        //console.log(`throw0()`)
        throw new Error('throw-0-msg');
    }

    throw1(arg1) {
        //console.log(`throw1(arg1:${arg1 !== null ? arg1 : '<null>'})`)
        throw new Error('throw1-msg ' + arg1);
    }

    throwretrieve1() {
        const val = globalThis.javaScriptTestHelper.store1val;
        //console.log(`retrieve1(arg1:${val !== null ? val : '<null>'})`)
        throw new Error('throwretrieve1 ' + val);
    }

    identity1(arg1) {
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

    getType1() {
        const val = globalThis.javaScriptTestHelper.store1val;
        const vtype = typeof (val);
        // console.log(`getType1(arg1:${vtype !== null ? vtype : '<null>'})`)
        return vtype;
    }

    getClass1() {
        const val = globalThis.javaScriptTestHelper.store1val;
        const cname = val.constructor.name;
        // console.log(`getClass1(arg1:${cname !== null ? cname : '<null>'})`)
        return cname;
    }

    invoke1(arg1, name) {
        // console.log(`invoke1: ${name}(arg1:${arg1 !== null ? arg1 : '<null>'})`)
        const fn = globalThis.JavaScriptTestHelper[name];

        // console.log("invoke1:" + fn.toString());
        const res = fn(arg1);
        // console.log(`invoke1: res ${res !== null ? res : '<null>'})`)
        return res;
    }

    async awaitvoid(arg1) {
        //console.log("awaitvoid:" + typeof arg1);
        await arg1;
        //console.log("awaitvoid done");
    }

    async await1(arg1) {
        console.log("await1:" + typeof arg1);
        const value = await arg1;
        return value;
    }
}

globalThis.javaScriptTestHelper = new JavaScriptTestHelper();
// console.log('JavaScriptTestHelper:' Object.keys(globalThis.JavaScriptTestHelper));
