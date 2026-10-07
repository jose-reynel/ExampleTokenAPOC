#!/usr/bin/env bash
# Construye las imágenes DENTRO de OpenShift (build binario con Dockerfile) y las deja en ImageStreams.
# Uso: ./openshift/build.sh [proyecto]
set -euo pipefail

PROJECT="${1:-decoupled-payments}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"

oc get project "$PROJECT" >/dev/null 2>&1 || oc new-project "$PROJECT"

build() {            # build <nombre> <carpeta>
  local name="$1" dir="$2"
  oc get bc/"$name" -n "$PROJECT" >/dev/null 2>&1 || \
    oc new-build --name "$name" --binary --strategy=docker -n "$PROJECT"
  oc start-build "$name" --from-dir="$ROOT/$dir" --follow -n "$PROJECT"
}

build dp-backend  backend
build dp-frontend frontend
echo "Imágenes listas: dp-backend:latest y dp-frontend:latest en el proyecto $PROJECT"
