FROM mcr.microsoft.com/mssql/server 

ARG PROJECT_DIR=/tmp/globomantics
RUN mkdir -p $PROJECT_DIR
WORKDIR $PROJECT_DIR
COPY sql/InitializeGlobomanticsDbAndUser.sql ./
COPY sql/entrypoint.sh ./
COPY sql/setup.sh ./

CMD ["/bin/bash", "entrypoint.sh"]