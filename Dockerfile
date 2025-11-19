# ========= Build stage =========
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY CafeLocatorApp.csproj ./
RUN dotnet restore

# Copy all source files and publish release build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# ========= Runtime stage =========
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published app from build stage
COPY --from=build /app/publish ./

# Allow ASP.NET Core to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Run the application
ENTRYPOINT ["dotnet", "CafeLocatorApp.dll"]
