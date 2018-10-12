# Instructions on how to setup database

## In Fedora 24 container:
- `docker ps` shows _id of existing Fedora 24 container
- `docker exec -it _id /bin/sh`
- `dnf install findutils` need to install findutils for building corefx to add missing xargs
- `find / -name libsqlite3odbc.so` to be used in odbcinst.ini
- `odbcinst -j` to show version and location of ini files
- `ldconfig -p | grep sqlite`
- `ldconfig -p | grep odbc`
- `dnf list | grep unixODBC`
- `dnf install unixODBC.x86_64`
- `dnf install unixODBC-devel.x86_64`
- `dnf install sqliteodbc.x86_64`

## Notes on commands used in Debian 8.2
- followed [dockerfile](https://github.com/dotnet/dotnet-buildtools-prereqs-docker/blob/master/src/debian/8.2/Dockerfile) instructions for debian 8.2
- dependencies: libkrb5-dev, cmake

Get the tag name from https://hub.docker.com/r/microsoft/dotnet-buildtools-prereqs/tags/ and use in docker run below
- `docker run -it microsoft/dotnet-buildtools-prereqs:debian-8.2-SHA-YMD.. /bin/sh`
- `docker images` shows _id for Debian 8.2 to use in command below
- `docker exec -it _id /bin/sh`

## Notes on commands used in Linux 14.04
This section describes the process to install unixODBC libraries and SQLite/MSSQL driver for Ubuntu 14.04.

- `sudo su` 
- `curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add -`
- `curl https://packages.microsoft.com/config/ubuntu/14.04/prod.list > /etc/apt/sources.list.d/mssql-release.list`
- `sudo apt-get update`
- `sudo apt-get install unixodbc`

SQLite Driver
- `wget "http://www.ch-werner.de/sqliteodbc/sqliteodbc-0.9995.tar.gz"` download and install SQLite ODBC Driver
- `gunzip sqliteodbc-0.9995.tar.gz`
- `tar xvf sqliteodbc-0.9995.tar`
- `cd sqliteodbc-0.9995/`
- `./configure`
- `make`
- `sudo make install`
- `sudo nano /etc/odbcinst.ini`

```
[SQLite3 ODBC Driver]
Description=SQLite ODBC Driver
Driver=/usr/local/lib/libsqlite3odbc.so
Setup=/usr/local/lib/libsqlite3odbc.so
Threading=4
```

Microsoft SQL Driver
- `sudo ACCEPT_EULA=Y apt-get install msodbcsql17`
- `sudo nano /etc/odbcinst.ini`

```
[ODBC Driver 17 for SQL Server]
Description=Microsoft ODBC Driver 17 for SQL Server
Driver=/opt/microsoft/msodbcsql17/lib64/libmsodbcsql-17.2.so.0.1
UsageCount=1
```

To use the Microsoft driver, here is sample `odbc.ini`
```
[SQLTest]
Driver=ODBC Driver 17 for SQL Server
Server=tcp:server,port
Database=
```

## Notes on commands used in Linux 16.04
- `wget "ftp://ftp.unixodbc.org/pub/unixODBC/unixODBC-2.3.4.tar.gz"` download and install unixODBC
- `gunzip unixODBC-2.3.4.tar.gz`
- `tar xvf unixODBC-2.3.4.tar`
- `cd unixODBC-2.3.4/`
- `./configure`
- `make`
- `sudo make install`
- `cd ..`
- `wget "http://www.ch-werner.de/sqliteodbc/sqliteodbc-0.9995.tar.gz"` download and install SQLite ODBC Driver
- `gunzip sqliteodbc-0.9995.tar.gz`
- `tar xvf sqliteodbc-0.9995.tar`
- `cd sqliteodbc-0.9995/`
- `./configure`
- `make`
- `sudo make install`
- `sudo nano /usr/local/etc/odbcinst.ini`

```
[ODBC Drivers]
SQLite3 ODBC Driver=Installed

[SQLite3 ODBC Driver]
Description=SQLite3 ODBC Driver
Driver=/usr/local/lib/libsqlite3odbc.so
Setup=/usr/local/lib/libsqlite3odbc.so
```

- `sudo nano /etc/odbcinst.ini`

```
[SQLite3 ODBC Driver]
Description=SQLite ODBC Driver
Driver=/usr/local/lib/libsqlite3odbc.so
Setup=/usr/local/lib/libsqlite3odbc.so
Threading=4
```

## Notes on commands used in Mac 
- `gunzip unixODBC-2.3.4.tar.gz` download unixodbc
- `tar xvf unixODBC-2.3.4.tar`
- `cd unix...`
- `./configure`
- `make`
- `make install`
- `/usr/local/bin/odbcinst` try out odbcinst using the command below
- `sudo nano /etc/odbcinst.ini`

```
 [SQLite3 ODBC Driver]
 Description=SQLite ODBC Driver
 Driver=/usr/local/lib/libsqlite3odbc.so
 Setup=/usr/local/lib/libsqlite3odbc.so
 Threading=4
```

- `sudo nano find / -name odbcinst.ini`
- `sudo nano /usr/local/etc/odbcinst.ini`

```
 [ODBC Drivers]
 SQLite3 ODBC Driver=Installed

 [SQLite3 ODBC Driver]
 Driver=/usr/local/lib/libsqlite3odbc.dylib
 Setup=/usr/local/lib/libsqlite3odbc.dylib
```

## Notes on commands used in Windows7-10, 64 bit machine
- odbc32.dll already available, just needs to install sqlite64.exe

