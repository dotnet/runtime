// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography.X509Certificates;

using Xunit;

namespace System.Net.Http.WinHttpHandlerUnitTests
{
    public class ClientCertificateHelper
    {
        private readonly X509Certificate2 _cert_KeyUsageIncludesDigitalSignature_EKUIncludesClientAuth_PrivateKey =
            new X509Certificate2(
                Convert.FromBase64String(
                  // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Dummy certificate for testing.")]
                  @"MIIKTgIBAzCCCgoGCSqGSIb3DQEHAaCCCfsEggn3MIIJ8zCCBgwGCSqGSIb3DQEHAaCCBf0EggX5
                    MIIF9TCCBfEGCyqGSIb3DQEMCgECoIIE/jCCBPowHAYKKoZIhvcNAQwBAzAOBAiHDatvDr8QBQIC
                    B9AEggTYv1r4ckwt7o6f6DCMHlb/zv4t7rPju+PP0PjoJ8kzPfj419aSeyPuE+65YH9WFDqafJed
                    uzBSDaTXIabKapWN5NLnNYsyjCM3xHuP2N6QVa9irHmXE0BsB+3pfJW/aqIeDpHok/qc6xKxQhx0
                    jvfSNGkm6dqGpLX67o5ADGRTInmQc8EuLk11I75gJhRgZfsuaHEOaIj8+xbBQprmkDTEtjO/COVu
                    +FSksIBbQzi8Uyf778zNkvOZNdyOvLBLC4rOnJH1taCTr/9Rd03cf5U2F03GMTppJeoexMQZn/Ue
                    9F8yeZR20RLXXrgq+jD7a4IIpHhVenuF1+GQjwST3UQiS2QJRs0c4VH45Cmv7diWPtJ5qb/66XP7
                    g1gKpSc+bM24MvawUOm54dvnLjTqCGYKMVsSMv7UFFmMQIfbRU03XyAvC2uQXD85xx3iBLa5HNX9
                    sZ30p+Bi5nc38DtQhvefhG0sA62fUTJNinSqp6gZ0DeXLRGlKlrRAc/MmLA1vHx6ntHf5R4KluJH
                    uh8i02ibTGdtNL/IQ3VmE1c7MXxfGVx22P8CxyDCKaI9ko/2+gbTdmbvBJDqhiWvMOUqC1eczH5a
                    AgtOdmxozjt0Ep+t3901BFjvbQcBkrHHnjPleUXGfDmKsbxZ3BzFwUQAlcU59iD+5lqxGXcOOBFG
                    lsFRhD1RK5RJ+35uwS/7Wao5L4j5BeNJQHPb8tj7+t/LxW5FJhjX2hrcmQdCzEuuVsdRf+VVgW5G
                    2h++cfvwUOqpdy0g1JT5s70H4AGk21Q2kIVpkEqOnByQUThg9AEnyEB7Tc08+xCw5KsG9t/YryNE
                    qN6kVCpngpOwMYYW5Xu7zv7XTKiEjqs4TfNer3QUon1Ks3kfln1kK7IP5YzvnyIXS2bnVIqGffb2
                    P495QGTMnCy/8OykvsgO7vHjn0aw603/5xGIoxYDYFgDDmqOz0HLCbGg8VLM1KhaMVT7areTiKmx
                    RUticaGZ0xvxeXSUEVPohlxzOy0chb+yifdLbhaQZWeweVJzaYxk7PXjIU/rpONKgd8EKnCsqyFk
                    Mvpm334X7WIlAhwWhHrYDnEdENFAhqTe948/GNf0aRkB7/JlbRijAQkoVrmrWHODvq0IiC2SD/8C
                    cZhwEGb4YP9Ua8XL0EMAe4nMPCDSFqNKsdSQ4TB0YWyfGW8sk+hf6hfjEwKnjqItH4NWjwV5GoxP
                    wMvXJa/41+7RGtZwY5pMcjR771r93MBSkZmrKKpUcoD5RNr/kQyVD547kT/NTt5wIEGkwnxFlmiC
                    ctDAuBfIbS+gOv5MlOjZt+44Lz4+GkgZhHNFj8+e0zaZnkzSTnjJa5+2VNHhH6k7+xid0VDvyPhu
                    iCRQo2REApSsyCAwX/oE5xJ/BN/g1gr2UQ57iNyDG3SNsxqdATYZ6u79iR8ZHu8wRtNVLSYQ3bof
                    hOax/Ti4vTProc/hxVbO0qB6KApMhR2AJmDRmY2WnWofc4ZsM8pXihgasYkzJdplU0tepKDmy4cu
                    Jrafx/lexIhpmeIoewQue10Hm+hrk170uwOsfcx5RLw1RrsJKJUJqAIqNWUeAttYkOvIsRtxeoVl
                    W4mp2rnUET/ictDRAEZzUm13lvdK+tiDpNxLzPp0YifaJYGPNAJuy3wRKM+U0WtLNDGB3zATBgkq
                    hkiG9w0BCRUxBgQEAQAAADBdBgkqhkiG9w0BCRQxUB5OAGwAcAAtADgAYgAwADgAZQBhAGIANAAt
                    AGMAMQBmADIALQA0ADcAOABkAC0AOQAxAGIAYQAtADEAZQBjAGYAMwBlAGMAOQAwADQAYgAwMGkG
                    CSsGAQQBgjcRATFcHloATQBpAGMAcgBvAHMAbwBmAHQAIABSAFMAQQAgAFMAQwBoAGEAbgBuAGUA
                    bAAgAEMAcgB5AHAAdABvAGcAcgBhAHAAaABpAGMAIABQAHIAbwB2AGkAZABlAHIwggPfBgkqhkiG
                    9w0BBwagggPQMIIDzAIBADCCA8UGCSqGSIb3DQEHATAcBgoqhkiG9w0BDAEGMA4ECFVqFR0l+H6e
                    AgIH0ICCA5iU8LSe69nRL14pZKgOrnzXZXdUrC7McluguhjVbFCLXKWL1p87vR8fTqPBv8xapXGa
                    zmU++6gWhPEl8xWbAd090E2IcsOOlnYclm0Zqs2cW2Jpo0LM1f4MTDI67hC9IOCAJr/YYZzex/I1
                    r0Eqi3pz2ioFvcy62ShNeEFKTJMzPBZ/3dLRfPpG8ILgArPoFw/K5Z2Yis7k/mOIs9YBAB8L5w+T
                    sCqD1nfVVGijjdAeVNTbWystIMKtg9npaMYd6aKbnP7otyviDBTeRnCc34n5QxHontxNAfVdNgE8
                    prRWMZgEjE1sqmmDG34u0R0dscL3yTBz5v4pyEgqjzDXtjjLGvQ2G2awuw6ylcle9iXMOw3AUer/
                    ev0BPfkltG45j/FPe6MLXC78ZctOddlMxcfby6d+bCh+tplk/BbkAgzNk6z4uVWPpaNzcwEBhS+g
                    ARlFuGtfZes2Gky/ZpfxCG8GCuCilqcyV8T1ak0L97Q8JMmYSNcr+DElQdkl9kVgMqiXuHvx1SAQ
                    7sKxk/eITj7cXon036m30jh7T7CyeIC7S8e++2GMzV4UFQU3OqCkJKOsnvY9Gn/8yrTHGz62oZtY
                    1DaDg3m47jD9McR1NMhWOSNVcVfZcCzpJ+8yFX58qg2/uiAx9oigEY4Lms5kYCp4AO2VXtrRrp2B
                    pu7IEDmlF8eqrCUK6t5FqUY50xwgMVqPdW/cbn8YEoBU2TeSjEGXAFapuYDmAgDGUQTseHFXsd+D
                    wnKlj1xHeDYqBWFKkKt7GDSgq1pPvQVDIDvJbQCeVmYWMzplqPd5NHUojEwwc4jaln0g1M3YRN+w
                    vEVCG8mH4YtYp4fXCpEsapIn7tfWHPRAYARCwEEMsOuTruE4yPyhLEntVMw7UZlFgt5OuKDmHB6Q
                    phyJUN/UCnC+hfdTtvfGBzceKB/CAVPPLYmTYIAGfeJiZ+ED43rq1Vru6bIE3H6Bugxen+ZMvBXA
                    8uCGAHlVTHkmfS3egT8KUIeFLGSj2NBlUuE+lTfSmJcYyE8ptknKpE0PFNoVhtM+7Rc6C+hObBt2
                    S3POmJhVbUFXX0MoXQU7CfLZgRDNtFuj67izmcnqdfHcLViRPxqYl5uk9KY8UBgo5Gg5qjdv8Csx
                    3lKowEBJ0mf5/SO8e0jrsngnnBMnzhbH63DpvbMkew9JzYniO/qqbTYTb5Se9xGNaZMeCq9s0Ktn
                    eGzaLVeL3KuDXTcykeSrMzA7MB8wBwYFKw4DAhoEFFyWxw+sBYjg+u+FZOpcB22jW37XBBTa1n71
                    1STYVQ5YhCJvstItqyExlAICB9A="),
                "password");

        private readonly X509Certificate2 _cert_KeyUsageMissingDigitalSignature_EKUIncludesClientAuth_PrivateKey =
            new X509Certificate2(
                Convert.FromBase64String(
                  // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Dummy certificate for testing.")]
                  @"MIIKTgIBAzCCCgoGCSqGSIb3DQEHAaCCCfsEggn3MIIJ8zCCBgwGCSqGSIb3DQEHAaCCBf0EggX5
                    MIIF9TCCBfEGCyqGSIb3DQEMCgECoIIE/jCCBPowHAYKKoZIhvcNAQwBAzAOBAiSNi65ZF5ZTQIC
                    B9AEggTYRTivDtzHOWRR+MobtGFEUu6d1PiIlF1Ic84FWvmFCcJShkBmg3cBqDilqtamAkDkga4h
                    A1nwCrlx/lzkpI03hUbM7+hJrC9mB2pQrPi33ZHacIe2aunvFnMAoBcHv7xEn8XSRTx+TgIDAKwS
                    UqCIsgRSUYPQvR0kxgBYAC9XG7JeuxTrbV9F/VNOMbP4NumER0Ym4R668x8KpJkMjNuVH5/+CAxt
                    TKpLBav1wSj1qkrRtJRyPB+x8Pz7L0qP/3HZ1Ett05z1uUjBT8GSloO7OvwshFHeXlqG1I8CYRXE
                    f/NaXYw+Qm/sYWetkOhX4RWHFlzrKXi+nQRJLYnNdoK6dmWcj8ZnHfz0zAimTp16CL+qg8KegRUV
                    J8vz+CrZcsIKIvj+/Ys6m30b9htbwvdubb1hf1sF/xD5sfJHN0lR44ALzzH5AGIyVXFIhvGIHKWa
                    UywBJcnCHQHl/5IGsKQJBXHAfxCsmpxfTts1iTDCglNnxkkdNzST/ndcBFRPr1o88lPXNUl1dlMY
                    yGuSSUbHSzyodd7TZaQYcoDWZIjOQYkdtPGXizeWv0zDY6Qbf8g0rYIAg+iBBb1pHeqkBTdLG92z
                    QmRk8KSxxQkUivzQQZH2xIz0BEGMBWTl7uK7J+tnPq4U8FLRU8e5/eRQbfKsQJHheWkmCJseZhSW
                    puG32l8Bpr3KOE5bJI+A+In/XHNvbgWrQUZNh8a5Z0W+x3+I8EJisBqzl1AI/MTuM6BqEgQPtJhQ
                    X81PilHGNTTK1+VDJWJB+Uun0Luw0kcl45d4nK/WCVfZ6BC9CYxgexodMJfDZmuxG00w+HI6pldV
                    yXUlTW4cPQQyNRPZL8BUvW/wGNNzS+iMEqjQ9OolfIyx9hx31qZPvixfpZlUH8xUwNPHjmC0DKSn
                    RUhyOQ2ukxyvkvwcNkn3Uq9jqByvVBXeBOs8XyMjQkGpzh7snSh5Tx9xI0uKQwoFPmK6bNj2P2Sj
                    j1TstdWNWVIogLd+vrwuNPSfQP5V1L2LjvgkDNhWK6tVo82dDi0eBlQpIFMNtFLYk2GhEdkjBwt/
                    +ajFF17fqyS+DHwTUrYxtA2CTwjpeFWpyN/KV63nUUbVtCdWgvMIGuycgvZ4IjvfQbkLewPe+YtZ
                    EiyyJYoE6hU/XyN0GmV9wjLz6qutVL0g3qEhmv1M9B6CfyDhwQMywJsYt0fT2PEkPZsy/84Nmj/M
                    tYzYeQEsQ+uHSKA/H6x4ogah1irLwStOmoQhcAQNi0B4gU/dGekGKOmAV9ch82HpH7EMZWSGN/kd
                    6hutGtHdFG/h4pUGR1MbikoT+C+UDmM+qilXvtFLxZFUkR3zgVzXPTW4Cl4SX5dkh72j5D7nX01o
                    Q8xTkDGF0d85zsjqK7jMlBSZZGyz4qQXwWICaCAWI3KugB4nuyD33BpTRPWQbCFx6Xu+qqbKYffR
                    QPI4psNr1tjounYRXyO+b0dy6P0JaayfA3A4A+nTi6bQTA/g2G6WdpzUkl9NsPYeRLqie0LrQt7A
                    M96K7oZqqr0Kwr/EHJOunbL7hf0MQyF4B5rNS1SC/BkUPVm05VTsPoQ/fZRnh4joR5/Pf2hJiiZ9
                    cj3FHJAgxJa4AybONWYHZndXxAmz4qXd/0lEGcDQolID8wAhzHkrxtoDkhGFe8wvpjGB3zATBgkq
                    hkiG9w0BCRUxBgQEAQAAADBdBgkqhkiG9w0BCRQxUB5OAGwAcAAtAGYANgBlAGMAYgAzAGUAMQAt
                    ADAAMQAwADQALQA0ADAANABjAC0AYQAxAGYAMAAtADgAMAA1ADgAZAA5ADIAYgBkADYANAA4MGkG
                    CSsGAQQBgjcRATFcHloATQBpAGMAcgBvAHMAbwBmAHQAIABSAFMAQQAgAFMAQwBoAGEAbgBuAGUA
                    bAAgAEMAcgB5AHAAdABvAGcAcgBhAHAAaABpAGMAIABQAHIAbwB2AGkAZABlAHIwggPfBgkqhkiG
                    9w0BBwagggPQMIIDzAIBADCCA8UGCSqGSIb3DQEHATAcBgoqhkiG9w0BDAEGMA4ECFm0XVHjzmaN
                    AgIH0ICCA5hT8Uv6jXhh/mHFb2p4XctoZH3gU3uf+MXFkzO9rzRPjgpKqVt1CYDbGHMqI+PypZL+
                    Ppy9cHnw7ChKy/qGJ02q9Tjn2m5IQI024Qo1GRBh0P5ibNWeb1ILxuJi5Xb/LZUc+buMXQhNZPuh
                    KvImzd/6Uigr8RrZCdBdbuAu8qtkWwq+WUBBLt/gY1arVV6Tkbquz+LbJQhxa8nIL2hntCZlayL1
                    CtQe87NZ+Kuh8jxjxRyPIlQvYTyksqPxZbP4iaFa3C45QksELuzg3HpqIPocMTP9SETaj5KX7ZPm
                    ziK8B3aa3LcUH5wonbU9zpVa9Zkr3IJJz5rlnoStdIrkK+4c8pCGJGvYTg6rypKcBi037b9U16p5
                    pmn4nh9nz2Ys0+bh6FNAL9rt5naRd3TlpjmPQHhx+m5jwUvLVMcvtJq2Tq2bSqHJd7owNPheEqQY
                    9DQ6NKuj9lTRgKvoL5ok8zjZYcgUW0Ev/kjZ27Qin1AS90eoLCC3gELqxIlZbADt1H/cyIr96M6Q
                    bkDS+CzP9RXgDJ9xdGx2wwIscNCPCncTTe8mabpBocERcjN0i42ylrlD6P3aHWt2Ngx9rYNlvbwQ
                    8RrDwW5b9WF/2BLETgKly3DXp0ClhRdWrmSbso1w0y99AZPQEM1aCLdqTmgY48+7zTrKttfeD/F6
                    ugPmhxpyA+Wu/DlxZP7JqMrH0v9XoN0VJ5L/UuQOEowVx2P/8RgqtMB1MJ3WRaUnd/BC8w/sFKHl
                    xpeH0aoheSqeW5LEHSYB/XLb7PfBwLVqgttNqid+dkPfrLfpb5ZRfm8z9icf7gUl6CbIj+KbdeUV
                    x+9KT52Oym3+A9z/Fz01w3Z63WkG1YBx70tV8qj8x9SfHaG7S1pS9PeCgbdzwZLeY9XNaR5349Pq
                    CX7rArkC3kHe+v+jdQ0MP/91InaQpJLoHeIL+Otp2bLLLIgGLoZvkavOQjNay/HLNujohtHx3gns
                    7HiMJCQiWCCwRpe3j4lK1/jT+xAAfdhP/cJj9MMdO1/ANkytSAD+0SS5zs4aBu3cXrXCH7ZN4X7Q
                    QZWC9/2TJnPvzTMtTKrgu9azNjEQw8T2d6uHtEV+qyKm6QR4fs2l25OqH167/StAlpTohOahwzO1
                    6ZzUabYI5B+4pji7A1fn+aQa8u0kX3qyYNCeEpFoX0FVI6G7VfeFus+91a/sVVa1EkGQrdFE/xBA
                    gOxmB9mCYydeLzMbYUa7KTA7MB8wBwYFKw4DAhoEFP7GyT09gJP2TcfZRvC4S5rTPsRjBBSWQf3f
                    BXH9IG0V641MICnWmHeUhQICB9A="),
                "password");

        private readonly X509Certificate2 _cert_KeyUsageIncludesDigitalSignature_EKUMissingClientAuth_PrivateKey =
            new X509Certificate2(
                Convert.FromBase64String(
                  // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Dummy certificate for testing.")]
                  @"MIIKRgIBAzCCCgIGCSqGSIb3DQEHAaCCCfMEggnvMIIJ6zCCBgQGCSqGSIb3DQEHAaCCBfUEggXx
                    MIIF7TCCBekGCyqGSIb3DQEMCgECoIIE9jCCBPIwHAYKKoZIhvcNAQwBAzAOBAhCUuNQ0RqfZQIC
                    B9AEggTQHCQRSiCiNI7egTvUaI1Z3tfeLwFWvG7B/za5v9fb97MExoyVQSDmUyUDTlVEcg3gVqJZ
                    MKGD6U1pmJsnTB+tH53Ho8L9GIrmZKq1L4Y9Wxu/VfIgub4I54UK8LZylpcIkb+OXBn46HnhDBu/
                    fknsBWLWUbtuOGp7PDHQLvoEu5rvmvTDWTWQr4S7S2cqrR0RIXThKObgKCjJ+/y9YCBZfX8jbZvq
                    r7vedpoFS0P0mDOk8aILefNADdoALfHzrrxYl0hp31sHrnDHJ0I/j6xFtsKBFmDigtPhHb9+c9PC
                    Bi/QJ5vmfECiHsw+dwBFH7m50ZbRFllDY6vIOjXRCeixMkq+18GZirX9l042RCijChCIbiaQI9O8
                    Eejnvkf41+KiVQAoaC42s1yQtmmDDIqJZD6N5X9wx7NgnfjxpuUvq91O/iBN0yzVzep5nQ4CDGmF
                    ljFc50beI5uOcSNOiO7Zn40h6MkPcecIMlI7sUFlo3IglzQGLvGL3OqsQ+dYqBGLD21x1ItmQOyH
                    zRIe+u5zytQVux6L47caPVR05k7ge7py0P+trh0UZimh7udh2l1a6GahNtKhU28cbJa3GFj+AlWa
                    tSD6LzPdsMhoDooSMKpWcz8YogO4gueZkTL+ZI/WE6uGIOfbg70aSPc+k1xMNu8jnkdeOOUHzy7l
                    V9raeUuSCLsh9mOEhSD9sVRVgpPz9bU72vWO4nYuGoENDuMVpiOQZ+2+LOv54TJVeJB+niSx/z6+
                    YGcGTHa8rSY25hv0wHF6Ws1B/M68ScmJ1YKAL5yWptiOZPqoPcHaqZcPRNKJ5sK8s77K8r6uU00F
                    49i6QWGVcLN+4oTis2HEA4YZQXWxeM08iTpzA7LqRrPuhfeqpb4mfWcnfkZUiVFGp245bP0MtCLm
                    i7yBcztQ9NXexCEhszYHBkPJub76h6Du/98xSmDfqYXA0u2b242TrF0pK2kkYbDFIzBu49G75uL/
                    22jOwrB5XgqxISmSHG0aVoYnh6XLN7Mec0dg9guwCeN58/0LoDfoR5OSlVwd94Yx/R89gAbFVPp5
                    aWNVwHio0TrlBuyQSlIARlkO37yXJq85JOjnaa1EdyH/zwGrVE6j6xG3P/z4Lz2QybpfKWyaGjZ9
                    nML8YPCVRMrCln9M1Fe+hw66KDDnw3N9YD4fLzIk67AeEjf+uXHIas14aKZLaFHvBax5GPfoZ59X
                    nDajL2+f85aDWyG/yt4Zx9cdURSbhDna7MArn+fz/4y+KLYoFvdiU0mf9xp1CVormfp+iIJrmsEy
                    Yb3TDDFimQbI1LhR6c+D+UGxjAbbqEbCdaY+3sLRpbWyEEyEA317A3v3gFWQQQjAUxrFQb1hOyit
                    SmrcFg13XKmO5sEgfZi5zf4i3UOOron6NskmgvrSptXv/6Dndk4tEnsYPk/L7LvGUwfBninVsP96
                    8ifZ8It3H3A26fOhSpbv7DWJih5wXS1Cq0UehciuhMaYjNoXEKcyd4PMghuYKBJpoZMGCUUCunZy
                    upL3uOt/GvThtjv+ZyGnEZ8aQHO1iXlDg7gqmvmfr/RM3jwuBU4cthm4jzzSukj4rJltSVKPnOKb
                    YG0QBKKYB7PUA9puYAVLvXCDyCN6Rc6EuNlX023d+TPQXXfkRPwbw9Axgd8wEwYJKoZIhvcNAQkV
                    MQYEBAEAAAAwXQYJKoZIhvcNAQkUMVAeTgBsAHAALQBhADQANwA3ADYAZQBhAGQALQAxADUANwAz
                    AC0ANABiAGEAYwAtAGIANQA5ADQALQAxADIAYgBkADYAYQAxAGYAMAA4ADYAZTBpBgkrBgEEAYI3
                    EQExXB5aAE0AaQBjAHIAbwBzAG8AZgB0ACAAUgBTAEEAIABTAEMAaABhAG4AbgBlAGwAIABDAHIA
                    eQBwAHQAbwBnAHIAYQBwAGgAaQBjACAAUAByAG8AdgBpAGQAZQByMIID3wYJKoZIhvcNAQcGoIID
                    0DCCA8wCAQAwggPFBgkqhkiG9w0BBwEwHAYKKoZIhvcNAQwBBjAOBAgvllSbyRotdwICB9CAggOY
                    V1o4hQ6elS5118RFmTFOCR0+m6htp9L5oljwV2ULh8bloOHZVt91WT9jfI/E8SQ12dyCKC3YsZoQ
                    BQI4g8FUk6AcAhSKVXJim/9WG7dcVxWv0TClcl2TtNwFs97nkBlurIdUIx49frWdfmhBj+O8P8vq
                    3b3GxmiJgEhcg2KmhsRB+mzXUK9frJqF9hMqB/Pb5Txa15vmHl5vh+oIYK89aDKeQ+Waqqm4ioY9
                    MsrlNP4BJLsMA1IccQpl9DNq7N31QvkD855sw4BARkDAMu+MmhxS2IXGWmioHLvOzvyU36bXykh9
                    g1XeH1h3L6PKQaeOcDBDcwwy6Rh/CBRAe0jWLMaSObLxnupSp6Wv5ft/hd9sAoZ6mMOosOwtbfeP
                    2ZpVtpYikU0Zt0EYjC+lPHqhsc1GThLLJjOvPtLMJV9TDJDpjVlE/b4xYbjkA2uTVWH3PkObME3P
                    fXcYvl6KxrHfWMw4GQPQPRnG63QlxWnXpNbmhcr5BdYg0kLaVgzV9z1KeDH7q7z0Qvd8Zl+4Grsz
                    sUJz6UJzo4CCqhtLQ6IbOyqtBTWSomW9dSD0nI4q+ZlIgL6sS8sfpPekWAqcsud/TUIQ3FbX5Ble
                    LCkAcU5dUwH9vzAXKE/pUFaeJUpYXg26jhgonQa7ODi8L+cBu4tCjef7NQqe+4I5SOrCDlo04d3S
                    V38Crywjz7YyC1wSYmxCzMZSAXZZ0K6wWsDnUjpxcuKSjZ98B3SIyNWIitQvlrzv0eo29PYlp4Rz
                    WPLrPG5y09o8MCtonHfwoKjqjQ5k/4qorwxfHoLwNdQpEEyldaiA14VRcpQukdMr4Y6cDKPU92Tm
                    iENC3eecBBTPgzaftHPt4A5v8TUiH7NhK1TJTn6qjzLpSONUKe0SSs1me6qqvdOcen+koey7XMTi
                    +CwJ2pH7QFK3ImxJ4jcTrXIUb0nzRabeAy1fMQv4jBMMqEpBQLpy+NOosME7NhHn9ijM0i0TPImT
                    Iw3MD5cLB1f2jHlyrviC3gGtIjACb61WLp1g2nkOyXgoq61aBOYQCuE0Ej8tcqbBMwOSV5BdGuAX
                    4+4wKQm5gN/A61E53rXWbC94KYInPE+cOr520WYXa5A2EBLeD2zFJmPLKRWtcYQ68TpO5CXwv3Vf
                    PJSfxLpKGbEzJwTVym+e8/wV6rIMenN2wDc3lPUD0BYeQNSthGyb8Lxx7QGIh9n2tv69xCjGX0Q2
                    XsK7XDeFDEowOzAfMAcGBSsOAwIaBBSRhbklb9VyYh3HUjX0soZDQ4LQYQQUUNfB0Xe9MmRurnYX
                    KMyIoz+dMXUCAgfQ"),
                "password");

        private readonly X509Certificate2 _cert_KeyUsageIncludesDigitalSignature_NoEKU_PrivateKey =
            new X509Certificate2(
                Convert.FromBase64String(
                  // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Dummy certificate for testing.")]
                  @"MIIKPgIBAzCCCfoGCSqGSIb3DQEHAaCCCesEggnnMIIJ4zCCBgwGCSqGSIb3DQEHAaCCBf0EggX5
                    MIIF9TCCBfEGCyqGSIb3DQEMCgECoIIE/jCCBPowHAYKKoZIhvcNAQwBAzAOBAijQh1kbOZOYQIC
                    B9AEggTY+wDp3V31Lh7f8YrsqEsyGZ+GlYvFhLWvDASjisYJi5NlQ0ONbf0KOXHVSvBj3tVyuHm4
                    5j6PlwF8nLiANmvnNyr+tmnLLx8Fa8XGmi4ggs3YGPJEw6u41qTnPGlT7goQaylT+XudRTMgB1lQ
                    tAGW12P2kQX2laJFqK/KF1YGaUC7dTxPnRQg+qzfP3+omlx6kqt38YvVjoc1toYGo/Jc1GuEUQ++
                    HrarLzVUJvAzD22Q8fX0Tjp5EVezYhb/aSiqd7d7VLVHoukaYJxKJW3JKTVHI76+pyNv+HnTwlHC
                    gfY8DI6NekwtXEHf9W1XPaTMyFYyamWAsH5FeM1EyLh/bTmvoCNZtVx2UiUD1MbSnYO/KNGHcl74
                    6A92sFXhzSXdkxLCMEiHTD5LZ8SFJCh7b3LeTHsdRb6C3SlkPsji5mCbacy6femW9Q1RyPO08Td3
                    vZtPB4fambMXLTaVaSnT/+F8Vd/seGrGsfON1okSIz34M6kH9GzHtbeQV3BuO6YxIJqljAlM+I1u
                    ItcXKGwv5vtzmGFIRVBxmgkErtO+dWeocee/du3VPA8MyuIEumCKVTeiM5OOPPHDxdOxieKYqC01
                    T8TvLFuTSqQg008s2BcGCW3dsbOc8jyKg4tp8J7XnaCYv7toyB4A8fzc3fx+mquBmc1ehMQKJHN1
                    Cx3nVV//gEEbq2ZSNrhuEKw2D85rA1XZX1zwhHy1T5bGNgC4sAwmRszUSeCrUAlGMLxXv+Cu4G1j
                    U+kwvG+MuKuK4Z22lMAwm7mNEK1vi7wmuoFPWOolPVCoxvCIGGDT0eLjL3YmePCkifwYrbDgWmWB
                    OElG1E7LtpCYDqTgsBwo/Vp47l/RQFYRAcxishKjn5Bi4AURagaFdVrFI+7XyjG5ZYijy39uKWJN
                    lquP5yHg9wjMsYeBjDIfZhkPFMPUou2DDuI3VimnW6SETXkitY6knjPl8T9kVYEHiDj4n2hZxymj
                    sXCPjO673zK4IB887KoOUpmzaGkfA5Gqw1JkE/HK/ghEJQpnkBs+SMWSwY200+UJWWSCeVI0ZY0T
                    sihWT7cd/o3LdFDNNKok9qA6lpREOv3+5l23McBM7y6sxtjXL/+GwbN3XiTGNY5yjJ0+bVUob2E6
                    L9JRc2+3Jlcg9xAV9YCvdjd1LkPo0aRm+oZKFWCv4mgoATBlJGImkIp/HcukEeaiuCQplDLapk+a
                    6ZwV4YfpZluoSoMaXzGZEr+qFUAzhEJ/WXLBQI9qEkf5Lf9Kdh6iKSqnV8wordvu24rGynYkM3TO
                    Ni/8IjeZRCE2CqcQ9coAzXgSJdM1vC+1AJm0mpsvlHocHnJoF305OtFUALTFCHkrZMxqVGMq2DlX
                    cXw6KEEheVZGZs7QD5eYf47YcSFCGsSEhcP+syt0UgAi2p5Y8Ym8AFotTMT8opwJ9LwjaCwBMQkH
                    xKPwcSg7Q9SXb4NNTAL1nGxOU5ZNW0QRcwbJQzVfVTMwQ7nRtSjc/Qg3ST1fVuIiqsTSu2AL3bSn
                    24I3Zi8idaf69c2MNhc03UTgMNCh9T4QNVf7bSXznPl8hd9G3cekPuQY1b5YzB8DOU5cyD+pLuOa
                    43oQ6V0WVceUHe+Lw0aKelCI+6dYa7C8RerOTgOaDyuBxG+qouBk5LvxCNWLh7nMyTGB3zATBgkq
                    hkiG9w0BCRUxBgQEAQAAADBdBgkqhkiG9w0BCRQxUB5OAGwAcAAtADYANwBkAGYAZgA0ADIAOAAt
                    ADEAZQAxADEALQA0ADYAYwA5AC0AOABlADkAMgAtAGIAZQBmAGIANwAzAGUANAA4ADIAOAA1MGkG
                    CSsGAQQBgjcRATFcHloATQBpAGMAcgBvAHMAbwBmAHQAIABSAFMAQQAgAFMAQwBoAGEAbgBuAGUA
                    bAAgAEMAcgB5AHAAdABvAGcAcgBhAHAAaABpAGMAIABQAHIAbwB2AGkAZABlAHIwggPPBgkqhkiG
                    9w0BBwagggPAMIIDvAIBADCCA7UGCSqGSIb3DQEHATAcBgoqhkiG9w0BDAEGMA4ECKd3W1PCnIYL
                    AgIH0ICCA4g+xQnikaqVknam03UPBunLcbc4vM5elTihZjvuQHjcQWOr/GeLDWSkIqJAf7f/6jRM
                    D8nlgx/YM1z6aZfYeU/kfY7T58yS5glTscFEY0sitH4Dt8bN6jGz9B5MG6afKYsIT3IcgM52EbzJ
                    1RiHU6KHSagaBmAvSv75npvg/gV+UpqSMmWyUm3Wq1vJcmm58dzYrxSMdvPtnDeFIvSK6GH1Okpz
                    8B63JDjPPUFCv/4cdZyRpDmz4RIlfM1fH89koQ0sX5tZHxFSZcy7RPlRfCAxo65AF78WGWPAHxIC
                    11OesbIlv6O/ZECZIxmRC02LdTUr4DAF2vVZy3x3Fn24d2KHAotykvn8ENpSvs9DTedGAjlKvEFO
                    hP5DJHqbK2WPacD8hrCQatxyWBRmMhC5/fvm6ACb/HSL+EDZgZ5Zr294RUH9QXJd+IPUJI5AQaqj
                    Br2u699hv0rlaf4j+NAbneDLn8M5M3wJHGD2rG3Q1xpNC30s9/v68rtKJFVKndtVXmzQi33GnC4P
                    EQU/FyL/Jwal+NnJO68aHQ2D9Ai3DMqsRvKNznpxXp9kiUuSgKWsdSbMoRzs/BfdbeCOIyzxV1BZ
                    UvWCzSZu4YE8UYGVxOIfrSILp7NFQD2rQSpdI831OPLeE9+QJHULiV8mzf6svCyTn+s0m0dIBIO0
                    K9oqUpdWcjDdbSHANOPRYlUWgZHwJ6Sh4ZCpKmvU3FeS4yL5en+jW/1JsvBNq1mWVQTIIM5q8onG
                    FloYvQpRxZb6QJ4sITLbk1rdlRMxDwzcUZYQeFZhQbFk8MSuiZKGfdSpij0UEIUbLjO4HDFcdw4j
                    FzKe3k4gNiwtN5KKR4fT2DaHJehXuOrzHWmkBhXbsSMItPUmaHbbILYrhNYS8lDgEBtzgCJo/kZh
                    jUdMfnL5SdHsHV05mWuDhvDjhzaSFIkPlPJ4xxNhuC6ecUemm51846uw6O/iFHl1WHE5kaxoLNfY
                    fU7xHeYkvovsZwKrwFKKFiVnlstG+XqCgul1v7jhPcAvc9nDmHVoPwXwZEhPXhx46j61/TSmZboU
                    35iV7s5brC67ChbRIJk2cq/odioWyxVoKjAIZmH+e08QYc6mZRRgce6VVbk8R9Lh9/wkd2u9IIbd
                    NP5hynCdo85eTjJ4RaF8LGJwK45Jkw3jIghcKePkLzQIN03OGKm2+YjQV18M3UtlB7cti4JwZJCL
                    MDswHzAHBgUrDgMCGgQUvUM7Kw/8NN+1PlObSrj4zZwINasEFNL9LO5HLwrmwm/xDlNMw1KASQOL
                    AgIH0A=="),
                "password");

        private readonly X509Certificate2 _cert_KeyUsageIncludesDigitalSignature_EKUIncludesClientAuth_NoPrivateKey =
            new X509Certificate2(
                Convert.FromBase64String(
                  // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Dummy certificate for testing.")]
                  @"MIIDFjCCAf6gAwIBAgIQTm8+EF94L4FJ0nBFl5LICzANBgkqhkiG9w0BAQsFADAb
                    MRkwFwYDVQQDDBB1c2VyQGV4YW1wbGUuY29tMCAXDTE1MTAwNTEwMDMwMFoYDzIx
                    MTUxMDA1MTAwMzAwWjAbMRkwFwYDVQQDDBB1c2VyQGV4YW1wbGUuY29tMIIBIjAN
                    BgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA2Cl/rbhWRnypVnNyS48iWanKR0em
                    y0t8aDLe6OF2PUabmDdP/Xu7rREM19mB9c2eYC3U21lbCdNlksZS8pWKizcXpCtY
                    KpcBuyudFcSGRDcPSMPaXouBpow9WzxiOQ30qY72pAj6HZZXn5W+t6oJnzFOzD8u
                    vcptvc6sAEMRrdq7NTfSuW5pfaZMfkXM5lbBDdgbZrBptaNErYoHl/hIKCGiHauE
                    pWpUP1tpyGPxt2r8xjht2BHV6gFK582Wn8IuIGXag/9oBvFEavjpqbbYOrC6qraa
                    vTGoaWgraQHyGvWKbROFCpX46Wxjn+PMFhtwsFF/F93iNrzs6A2B2WPz0QIDAQAB
                    o1QwUjAOBgNVHQ8BAf8EBAMCBLAwEwYDVR0lBAwwCgYIKwYBBQUHAwIwDAYDVR0T
                    AQH/BAIwADAdBgNVHQ4EFgQUMgZRdUW5s+sgh7EC2pHpvOGQzUgwDQYJKoZIhvcN
                    AQELBQADggEBAJDi3pmzVvjJdTQCwFJEZvem9y64jHN+9MxM2rpwem0ZdQj+Cqst
                    iTRJZ7zW4alBec76qA0/BXN7kI2zm5+AtoDxUIUi18jUK8Y6b3qq7lInykE2al9e
                    xSgnRhaEJUxN2V5kkmhbdXBip70jazQZZbxQgYAZXhsX2rC39spt5Jz6NG24q4Eg
                    egBBHhV8p9mvaYUXLJUnL98ZSz2Zplw8YSR3LxJsko6uKYnJl0WRnt+O3eNodj7Y
                    3TKeUdX+KE01n8/QjJCiDd6QyyqDxtIfCVdvrlMbBGGNBY4TZ39RIpGunbe/zuLC
                    5hx0nLLS6LB2x2UaYdSkKnIlM5BCDnCt/38="));

        public ClientCertificateHelper()
        {
        }

        public object[][] ValidClientCertificates
        {
            get
            {
                return new object[][]
                    {
                        new object[] { _cert_KeyUsageIncludesDigitalSignature_EKUIncludesClientAuth_PrivateKey },
                        new object[] { _cert_KeyUsageIncludesDigitalSignature_NoEKU_PrivateKey }
                    };
            }
        }

        public X509Certificate2Collection ValidClientCertificateCollection
        {
            get
            {
                X509Certificate2Collection certs = new X509Certificate2Collection();
                certs.Add(_cert_KeyUsageIncludesDigitalSignature_EKUIncludesClientAuth_PrivateKey);
                certs.Add(_cert_KeyUsageIncludesDigitalSignature_NoEKU_PrivateKey);

                return certs;
            }
        }

        public object[][] InvalidClientCertificates
        {
            get
            {
                return new object[][]
                    {
                        new object[] { _cert_KeyUsageIncludesDigitalSignature_EKUIncludesClientAuth_NoPrivateKey },
                        new object[] { _cert_KeyUsageMissingDigitalSignature_EKUIncludesClientAuth_PrivateKey },
                        new object[] { _cert_KeyUsageIncludesDigitalSignature_EKUMissingClientAuth_PrivateKey }
                    };
            }
        }

        public X509Certificate2Collection InvalidClientCertificateCollection
        {
            get
            {
                X509Certificate2Collection certs = new X509Certificate2Collection();
                certs.Add(_cert_KeyUsageIncludesDigitalSignature_EKUIncludesClientAuth_NoPrivateKey);
                certs.Add(_cert_KeyUsageMissingDigitalSignature_EKUIncludesClientAuth_PrivateKey);
                certs.Add(_cert_KeyUsageIncludesDigitalSignature_EKUMissingClientAuth_PrivateKey);

                return certs;
            }
        }

        public X509Certificate2Collection ValidAndInvalidClientCertificateCollection
        {
            get
            {
                X509Certificate2Collection certs = new X509Certificate2Collection();
                certs.Add(_cert_KeyUsageIncludesDigitalSignature_EKUIncludesClientAuth_NoPrivateKey);
                certs.Add(_cert_KeyUsageMissingDigitalSignature_EKUIncludesClientAuth_PrivateKey);
                certs.Add(_cert_KeyUsageIncludesDigitalSignature_EKUIncludesClientAuth_PrivateKey);
                certs.Add(_cert_KeyUsageIncludesDigitalSignature_EKUMissingClientAuth_PrivateKey);

                return certs;
            }
        }
    }
}
