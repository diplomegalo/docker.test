# docker.test

Configure docker to run complete environnement with :

- the default asp.net web API project,
- nginx as secure reverse proxy,
- seq as centralized structured logs,

# Create Web API

Creates directory `docker.test` and go inside it.

```shell
mkdir docker.test
cd docker.test
```

Initialize git and add remote.

```shell
git init
git remote add origin https://github.com/diplomegalo/docker.test.git
git fetch
git checkout develop
git pull
```

Creates new webapi project and add to solution.

```shell
dotnet new webapi -o docker.test.webapi
dotnet new sln
dotnet sln add .\docker.test.webapi\
```

Test application by running it and navigate to the swagger ui `https://localhost:7061/swagger/index.html`

```shell
dotnet build
dotnet run --project .\docker.test.webapi\
```

# Dockerize the WebAPI

Open solution in your IDE

```shell
.\docker.test.sln
```

Add docker file using docker support for linux environment. This creates the `Dockerfile` with every step to build
container and the `.dockerignore` file.

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["docker.test.webapi/docker.test.webapi.csproj", "docker.test.webapi/"]
RUN dotnet restore "docker.test.webapi/docker.test.webapi.csproj"
COPY . .
WORKDIR "/src/docker.test.webapi"
RUN dotnet build "docker.test.webapi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "docker.test.webapi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "docker.test.webapi.dll"]
```

> - `aspnet:6.0` is the host environment
>- `sdk:6.0` is used to build and publish the .NET application.

Build image from the `/docker.test` directory.

```shell
docker build -t docker.test.webapi -f .\docker.test.webapi\Dockerfile .
```

Check if image has been build.

```shell
docker images
```

The `docker.test.webapi` image should be listed.

Run container from image just build.

```shell
docker run -tid --rm --name test_webapi -p 8080:80 -e "ASPNETCORE_ENVIRONMENT=Development" docker.test.webapi
```

> Environment variable `ASPNETCORE_ENVIRONMENT` is set to `Development` to allow access to the swagger.

Navigate to url `http://localhost:8080/swagger/index.html` to see whether the webapi is up and running.

# Nginx reverse proxy

Create directory to store nginx associated files.

````shell
mkdir "nginx"
````

## Docker compose with nginx

### Dockerfile

In the `/nginx` directory, create the `Dockerfile`.

```dockerfile
FROM nginx

COPY nginx/nginx.local.conf etc/nginx/nginx.conf
```

### Nginx configuration

In the `/nginx` directory, create the config file `nginx.local.conf` and add the data below.

```config
worker_processes 1;

        events {
        worker_connections  1024;
        }

        http {
        sendfile on;

        upstream webapi  {
        server docker.test.webapi:80;
        }

        server {
        listen 44344;
        server_name localhost;

        location / {
        proxy_pass http://webapi;
        }
        }
        }
```

> Note that the nginx will listen on port 44344.
> The `docker.test.webapi` value must be the same as the service describe in the docker compose file (see below).

### Docker compose

In the root directory, create the `dockercompose.yml` file.

```yaml
version: '3.4'

services:
  reverseproxy:
    image: dockertest_reversproxy
    build:
      context: .
      dockerfile: nginx/Dockerfile
    depends_on:
      - docker.test.webapi
    ports:
      - "44344:44344"
  docker.test.webapi:
    build:
      context: .
      dockerfile: docker.test.webapi/Dockerfile
    ports:
      - "8080:80"
    environment:
      - "ASPNETCORE_ENVIRONMENT=Development"
```

Run this first version by running the docker compose :

```shell
docker-compose up
```

Test whether the complete environment and applications are up and running by navigating to
the `http://localhost:44344/swagger/index.html`

## Secure nginx

First create a self-signed certificate with openssl.
> You can download openssl for windows with [chocolatey](https://community.chocolatey.org/packages/openssl) : `choco install openssl`

### Certificate configuration

In the `/nginx` directory add a new `local.conf` file with the data below:

```config
[req]
        default_bits       = 2048
        default_keyfile    = localhost.key
        distinguished_name = req_distinguished_name
        req_extensions     = req_ext
        x509_extensions    = v3_ca

        [req_distinguished_name]
        countryName                 = Country Name (2 letter code)
        countryName_default         = BE
        stateOrProvinceName         = State or Province Name (full name)
        stateOrProvinceName_default = Hainaut
        localityName                = Locality Name (eg, city)
        localityName_default        = Tournai
        organizationName            = Organization Name (eg, company)
        organizationName_default    = Delsoft
        organizationalUnitName      = organizationalunit
        organizationalUnitName_default = IT
        commonName                  = Common Name (e.g. server FQDN or YOUR name)
        commonName_default          = localhost
        commonName_max              = 64

        [req_ext]
        subjectAltName = @alt_names

        [v3_ca]
        subjectAltName = @alt_names

        [alt_names]
        DNS.1   = localhost
        DNS.2   = 127.0.0.1
```

Then execute the followning command to create private key and the self-signed certificate.

```shell
openssl req -x509 -sha256 -nodes -days 365 -newkey rsa:2048 -keyout local.key -out local.crt -config local.conf -passin pass:strongPassword
```

Then the following command to package the certificate:

```shell
openssl pkcs12 -export -out local.pfx -inkey local.key -in local.crt
```

Finally, install the .pfx in the certificate store on `Local Machine > Trusted Root Certification Authority`

### Update nginx Dockerfile

Add `COPY` instructions in the Nginx Dockerfile :

```dockerfile
COPY nginx/local.crt /etc/ssl/certs/local.crt
COPY nginx/local.key /etc/ssl/private/local.key
```

### Update nginx.local.conf

Update the configuration of the Nginx reverse proxy to listen through 443 port number with ssl.

```config
  server {
        listen 443 ssl;
        server_name docker.test.webapi;
        ssl_certificate /etc/ssl/certs/local.crt;
        ssl_certificate_key /etc/ssl/private/local.key;


        location / {
        proxy_pass http://webapi;
        }
        }
```

> The `server_name` value must be the same as the name of the webapi in the `docker-compose.yml` file.

### Update docker-compose.yml

Update ports to map the port 443 to the external port.

```yaml
  reverseproxy:
    image: dockertest_reversproxy
    build:
      context: .
      dockerfile: nginx/Dockerfile
    ports:
      - "44344:443"
    restart: always
    depends_on:
      - docker.test.webapi

```

### Rebuild image

Rebuild image then recreate and start containers.

```shell 
docker-compose build
docker-compose up
```

# Add seq as a service

## Add service to docker-compose

Update the `docker-compose.yml` file with : 

```yaml
  seqservice:
    image: datalust/seq
    restart: unless-stopped
    ports:
      - "5341:80"
    environment:
      - "ACCEPT_EULA=Y"
```

Run the `docker-compose up` command and try to navigate to `http://localhost:5341` to check whether the application is up and running.

> At this time you can configure the API Key by creating it through the `Admin > API Key` menu of the application. Then copy and paste the generated key in the Seq section of your config file.

## Update project to use seq

In the `docker.test\docker.test.webapi` folder execute the following command to install extensions methods allowing the application to send logs to Seq : 

```shell
dotnet add package Seq.Extensions.Logging
```

In your `program.cs` file, call `AddSeq()` on the `loggingBuilder` provided by `AddLogging()`.

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddLogging(loggingBuilder => 
    {
        loggingBuilder.AddSeq();
    });
// ...
```

In the `appsettings.json`, add the following section where the `ApiKey` value must be set with the key generate through the `API Key` menu of Seq,

```json
  "Seq": {
    "ServerUrl": "http://seqservice",
    "ApiKey": "123456",
    "MinimumLevel": "Trace",
    "LevelOverride": {
      "Microsoft": "Warning"
    }
  },
``` 

> Note that the server url must be equal to the name of the service declared in the `docker-compose.yml` file.