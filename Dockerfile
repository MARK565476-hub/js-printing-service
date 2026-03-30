# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build
COPY ["JS Printing Service.csproj", "./"]
RUN dotnet restore "JS Printing Service.csproj"
COPY . .
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /build/out .

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:${PORT:-10000}

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=1 \
    CMD curl -f http://localhost:${PORT:-10000}/ || exit 1

# Start application
ENTRYPOINT ["dotnet", "JS_Printing_Service.dll"]
