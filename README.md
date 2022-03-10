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