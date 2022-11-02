# This image will be based on the ubuntu image.  Build it using the
# command in the comment below.  You may omit the ubuntu pull if you already
# did it.  Before running the build, change into the directory containing
# this Dockerfile.  (Remember, the filename is "Dockerfile" with no
# extension.)
#
# docker pull ubuntu
# docker build --tag SpreadsheetFinal .
# 
# (Don't forget the trailing . to specify that this directory is the context.)

FROM ubuntu

SHELL ["/bin/bash", "-c"]

ARG DEBIAN_FRONTEND=noninteractive
ENV TZ="America/Salt Lake City"

RUN apt -y update && apt -y install \
    g++ \
    make \
    nlohmann-json3-dev \
    libsfml-dev \
    libboost-all-dev \
    libtinyxml2-dev

COPY ./SpreadsheetTester /SpreadsheetTester 

RUN make -C /SpreadsheetTester/ && \
    mv /SpreadsheetTester/build/spreadsheet_tester /

