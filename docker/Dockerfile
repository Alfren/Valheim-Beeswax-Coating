FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

RUN apt-get update \
 && apt-get install -y --no-install-recommends \
      lib32gcc-s1 lib32stdc++6 lib32z1 ca-certificates curl zip unzip \
 && rm -rf /var/lib/apt/lists/* \
 && mkdir -p /opt/steamcmd \
 && curl -fsSL https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz \
      | tar xz -C /opt/steamcmd \
 && chmod -R +x /opt/steamcmd \
 && (/opt/steamcmd/steamcmd.sh +quit || true) \
 && chmod -R a+rwX /opt/steamcmd \
 && dotnet tool install --tool-path /usr/local/dotnet-tools ilspycmd --version 9.1.0.7988 \
 && chmod -R a+rX /usr/local/dotnet-tools \
 && ln -s /usr/local/dotnet-tools/ilspycmd /usr/local/bin/ilspycmd

WORKDIR /src
ENTRYPOINT ["/bin/bash", "-lc"]
