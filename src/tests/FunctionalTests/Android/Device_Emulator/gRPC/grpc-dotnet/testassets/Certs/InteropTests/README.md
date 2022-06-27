Keys taken from https://github.com/grpc/grpc/tree/master/src/core/tsi/test_creds
so that interop server in this project is compatible with interop clients
implemented in other gRPC languages.

The server1.pem and server1.key were combined into server1.pfx. The password is 1111. These certs are not secure, do not use in production.
```
openssl pkcs12 -export -out server1.pfx -inkey server1.key -in server1.pem -certfile ca.pem
```
