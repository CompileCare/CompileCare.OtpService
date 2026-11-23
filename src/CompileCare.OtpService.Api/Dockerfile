FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy just the csproj file from current directory
COPY ["CompileCare.OtpService.Api.csproj", "CompileCare.OtpService.Api/"]
RUN dotnet restore "CompileCare.OtpService.Api/CompileCare.OtpService.Api.csproj"

# Copy all files from current directory
COPY . CompileCare.OtpService.Api/
WORKDIR "/src/CompileCare.OtpService.Api"
RUN dotnet build "CompileCare.OtpService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "CompileCare.OtpService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CompileCare.OtpService.Api.dll"]