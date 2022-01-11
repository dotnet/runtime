if (typeof globalThis.URL === 'undefined') {
    globalThis.URL = class URL {
        constructor(url) {
            this.url = url;
        }
        toString() {
            return this.url;
        }
    };
}

import('./main.mjs').catch(err => {
    console.log(err);
    console.log(err.stack);
});