# Self-Hosted IBM Z Github Actions Runner.

# Temporary image: amd64 dependencies.
FROM amd64/ubuntu:20.04 as ld-prefix
ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update && apt-get -y install ca-certificates libicu66 libssl1.1

# Main image.
FROM s390x/ubuntu:20.04

# Packages for zlib-ng testing.
ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update && apt-get -y install \
        clang-11 \
        cmake \
        curl \
        gcc \
        git \
        jq \
        libxml2-dev \
        libxslt-dev \
        llvm-11-tools \
        ninja-build \
        python-is-python3 \
        python3 \
        python3-dev \
        python3-pip

# amd64 dependencies.
COPY --from=ld-prefix / /usr/x86_64-linux-gnu/
RUN ln -fs ../lib/x86_64-linux-gnu/ld-linux-x86-64.so.2 /usr/x86_64-linux-gnu/lib64/
RUN ln -fs /etc/resolv.conf /usr/x86_64-linux-gnu/etc/
ENV QEMU_LD_PREFIX=/usr/x86_64-linux-gnu

# amd64 Github Actions Runner.
RUN useradd -m actions-runner
USER actions-runner
WORKDIR /home/actions-runner
RUN curl -L https://github.com/actions/runner/releases/download/v2.287.1/actions-runner-linux-x64-2.287.1.tar.gz | tar -xz
VOLUME /home/actions-runner

# Scripts.
COPY fs/ /
ENTRYPOINT ["/usr/bin/entrypoint"]
CMD ["/usr/bin/actions-runner"]
