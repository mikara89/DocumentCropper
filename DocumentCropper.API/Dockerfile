#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["DocumentCropper.API/DocumentCropper.API.csproj", "DocumentCropper.API/"]
COPY ["DocumentCropper.Lib/DocumentCropper.Lib.csproj", "DocumentCropper.Lib/"]
RUN dotnet restore "DocumentCropper.API/DocumentCropper.API.csproj"
COPY . .
WORKDIR "/src/DocumentCropper.API"
RUN dotnet build "DocumentCropper.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DocumentCropper.API.csproj" -c Release -o /app/publish

FROM base AS final

RUN apt-get update && apt-get install -y \
    apt-utils \
    libgdiplus \
    libc6-dev \
    libgtk2.0-dev \
    libtbb-dev \
    libatlas-base-dev \
    libvorbis-dev \
    libxvidcore-dev \
    libopencore-amrnb-dev \
    libopencore-amrwb-dev \
    libavresample-dev \
    x264 \
    v4l-utils \
    libwebp-dev \
    tesseract-ocr \
    libtesseract-dev \
    libleptonica-dev \
    libtiff-dev \
    libavcodec-dev \
    libavformat-dev \
    libswscale-dev \
    libdc1394-22-dev \
    libxine2-dev \
    libv4l-dev
RUN apt-get clean && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocumentCropper.API.dll"]