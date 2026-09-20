FROM ubuntu:22.04

ENV DEBIAN_FRONTEND=noninteractive TZ=UTC

RUN apt-get update \
 && apt-get install -y --no-install-recommends \
      ca-certificates libstdc++6 libsdl2-2.0-0 libpulse0 libvorbisfile3 \
 && rm -rf /var/lib/apt/lists/*

COPY server/ /valheim/
RUN chmod +x /valheim/valheim_server.x86_64 /valheim/*.sh || true

EXPOSE 2456-2457/udp

ENV HOME=/config/steamhome
ENTRYPOINT ["/valheim/entrypoint.sh"]
