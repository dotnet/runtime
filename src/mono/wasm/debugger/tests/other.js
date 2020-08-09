// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

function big_array_js_test (len) {
	var big = new Array(len);
	for (let i=0; i < len; i ++) {
		big[i]=i + 1000;
	}
	console.log('break here');
};

function object_js_test () {
	var obj = {
		a_obj: { aa: 5, ab: 'foo' },
		b_arr: [ 10, 12 ]
	};

	return obj;
};

function getters_js_test () {
	var ptd = {
		get Int () { return 5; },
		get String () { return "foobar"; },
		get DT () { return "dt"; },
		get IntArray () { return [1,2,3]; },
		get DTArray () { return ["dt0", "dt1"]; },
		DTAutoProperty: "dt",
		StringField: "string field value"
	};
	console.log (`break here`);
	return ptd;
}

function exception_caught_test () {
	try {
		throw new TypeError ('exception caught');
	} catch (e) {
		console.log(e);
	}
}

function exception_uncaught_test () {
	console.log('uncaught test');
	throw new RangeError ('exception uncaught');
}

function exceptions_test () {
	exception_caught_test ();
	exception_uncaught_test ();
}

function negative_cfo_test (str_value = null) {
	var ptd = {
		get Int () { return 5; },
		get String () { return "foobar"; },
		get DT () { return "dt"; },
		get IntArray () { return [1,2,3]; },
		get DTArray () { return ["dt0", "dt1"]; },
		DTAutoProperty: "dt",
		StringField: str_value
	};
	console.log (`break here`);
	return ptd;
}
