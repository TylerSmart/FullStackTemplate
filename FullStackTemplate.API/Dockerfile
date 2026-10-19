# Stage 1: Build Client
FROM node:22-alpine AS client-build
WORKDIR /src/FullStackTemplate.Client
COPY FullStackTemplate.Client/package*.json ./
RUN npm ci --ignore-scripts
COPY FullStackTemplate.Client/ .
RUN npm run build:production

# Stage 2: Build API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src
COPY FullStackTemplate.API/FullStackTemplate.API.csproj FullStackTemplate.API/
COPY FullStackTemplate.Core/FullStackTemplate.Core.csproj FullStackTemplate.Core/
COPY FullStackTemplate.Data/FullStackTemplate.Data.csproj FullStackTemplate.Data/
COPY FullStackTemplate.Infrastructure/FullStackTemplate.Infrastructure.csproj FullStackTemplate.Infrastructure/

RUN dotnet restore FullStackTemplate.API/FullStackTemplate.API.csproj

COPY . .
WORKDIR /src/FullStackTemplate.API
RUN dotnet publish -c Release -o /app/publish

# Stage 3: Final Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=api-build /app/publish .
# Copy client build to wwwroot
COPY --from=client-build /src/FullStackTemplate.Client/dist/FullStackTemplate.Client/browser ./wwwroot

EXPOSE 8080
ENTRYPOINT ["dotnet", "FullStackTemplate.API.dll"]
