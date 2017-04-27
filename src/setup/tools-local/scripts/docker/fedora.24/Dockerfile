FROM fedora:24

RUN dnf install -y bash git cmake wget which python clang-3.8.0-1.fc24.x86_64 llvm-devel-3.8.0-1.fc24.x86_64 make libicu-devel lldb-devel.x86_64 \
                   libunwind-devel.x86_64 lttng-ust-devel.x86_64 uuid-devel libuuid-devel tar glibc-locale-source zlib-devel libcurl-devel \
                   krb5-devel openssl-devel autoconf libtool hostname

RUN dnf upgrade -y nss

RUN dnf clean all

# Set a different rid to publish buildtools for, until we update to a version which
# natively supports fedora.24-x64
ENV __PUBLISH_RID=fedora.23-x64

# Setup User to match Host User, and give superuser permissions 
ARG USER_ID=0 
RUN useradd -m code_executor -u ${USER_ID} -g wheel
RUN echo 'code_executor ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers 
 
# With the User Change, we need to change permissions on these directories 
RUN chmod -R a+rwx /usr/local 
RUN chmod -R a+rwx /home
 
# Set user to the one we just created 
USER ${USER_ID} 
 
# Set working directory 
WORKDIR /opt/code
