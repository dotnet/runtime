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