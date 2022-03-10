# docker.test
Configure docker to run complete environnement with :
- Web API

# Create Web API
Creates directory `docker.test` and go inside it.

```cmd
mkdir docker.test
cd docker.test
```

Initialize git and add remote.

```cmd
git init
git remote add origin https://github.com/diplomegalo/docker.test.git
git fetch
git checkout develop
git pull
```

Creates new webapi project and add to solution.

```cmd
dotnet new webapi -o docker.test.webapi
dotnet new sln
dotnet sln add .\docker.test.webapi\
```

Test application by running it and navigate to the swagger ui `https://localhost:7061/swagger/index.html`

```cmd
dotnet build
dotnet run --project .\docker.test.webapi\
```

# Dockerize the WebAPI
Open solution in your IDE

```cmd
.\docker.test.sln
```
Add docker file using docker support for linux environment. This creates the `Dockerfile` with every step to build container and the `.dockerignore` file.

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
>- `aspnet:6.0` is the host environment 
>- `sdk:6.0` is used to build and publish the .NET application.

Build image

```cmd
docker build -t docker.test.webapi -f .\docker.test.webapi\Dockerfile .
```