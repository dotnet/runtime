export default class PromiseController<T = void> {
    readonly promise: Promise<T>;
    readonly resolve: (value: T | PromiseLike<T>) => void;
    readonly reject: (reason: any) => void;
    constructor() {
        let rs: (value: T | PromiseLike<T>) => void = undefined as any;
        let rj: (reason: any) => void = undefined as any;
        this.promise = new Promise<T>((resolve, reject) => {
            rs = resolve;
            rj = reject;
        });
        this.resolve = rs;
        this.reject = rj;
    }
}
