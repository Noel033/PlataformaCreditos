# Usa la imagen base oficial de ASP.NET Core para runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Usa la imagen del SDK para compilar la aplicación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia el archivo csproj y restaura dependencias
COPY ["PlataformaCreditos.csproj", "./"]
RUN dotnet restore "PlataformaCreditos.csproj"

# Copia el resto de los archivos y compila
COPY . .
WORKDIR "/src/"
RUN dotnet build "PlataformaCreditos.csproj" -c Release -o /app/build

# Publica la aplicación
FROM build AS publish
RUN dotnet publish "PlataformaCreditos.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa final: copia los binarios publicados a la imagen base
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PlataformaCreditos.dll"]
