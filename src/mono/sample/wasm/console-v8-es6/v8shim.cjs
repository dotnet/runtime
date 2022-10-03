import('./main.mjs').catch(err => {
    console.log(err);
    console.log(err.stack);
});