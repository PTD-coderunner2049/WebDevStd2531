#!/usr/bin/env bash
set -euo pipefail

DB_NAME="${DB_NAME:-WebDevStd2531}"
BACKUP_FILE="${BACKUP_FILE:-/var/opt/mssql/backup/WebDevStd2531.bak}"
SA_PASSWORD="${MSSQL_SA_PASSWORD:-${SA_PASSWORD:-}}"

if [ -z "$SA_PASSWORD" ]; then
  echo "MSSQL_SA_PASSWORD is required."
  exit 1
fi

SQLCMD_BIN="/opt/mssql-tools18/bin/sqlcmd"
if [ ! -x "$SQLCMD_BIN" ]; then
  SQLCMD_BIN="/opt/mssql-tools/bin/sqlcmd"
fi

DATA_FILE="/var/opt/mssql/data/${DB_NAME}.mdf"
LOG_FILE="/var/opt/mssql/data/${DB_NAME}_log.ldf"

restore_database() {
  echo "Restoring ${DB_NAME} from ${BACKUP_FILE}..."

  local file_list
  file_list="$("$SQLCMD_BIN" -C -S localhost -U sa -P "$SA_PASSWORD" -h -1 -W -s "|" -Q "RESTORE FILELISTONLY FROM DISK = N'${BACKUP_FILE}';" | tr -d '\r')"

  local data_logical
  local log_logical

  data_logical="$(printf '%s\n' "$file_list" | awk -F'|' '$3=="D" { gsub(/^[ \t]+|[ \t]+$/, "", $1); print $1; exit }')"
  log_logical="$(printf '%s\n' "$file_list" | awk -F'|' '$3=="L" { gsub(/^[ \t]+|[ \t]+$/, "", $1); print $1; exit }')"

  if [ -z "$data_logical" ] || [ -z "$log_logical" ]; then
    echo "Could not determine logical file names from backup."
    exit 1
  fi

  "$SQLCMD_BIN" -C -S localhost -U sa -P "$SA_PASSWORD" -Q "RESTORE DATABASE [${DB_NAME}] FROM DISK = N'${BACKUP_FILE}' WITH MOVE N'${data_logical}' TO N'${DATA_FILE}', MOVE N'${log_logical}' TO N'${LOG_FILE}', REPLACE, RECOVERY, STATS = 10;"
}

cleanup() {
  if [ -n "${sqlservr_pid:-}" ] && kill -0 "$sqlservr_pid" 2>/dev/null; then
    kill -TERM "$sqlservr_pid" 2>/dev/null || true
    wait "$sqlservr_pid" 2>/dev/null || true
  fi
}

trap cleanup EXIT

/opt/mssql/bin/sqlservr &
sqlservr_pid="$!"

echo "Waiting for SQL Server to become available..."
until "$SQLCMD_BIN" -C -S localhost -U sa -P "$SA_PASSWORD" -Q "SELECT 1" >/dev/null 2>&1; do
  sleep 1
done

db_exists="$("$SQLCMD_BIN" -C -S localhost -U sa -P "$SA_PASSWORD" -h -1 -W -Q "SET NOCOUNT ON; IF DB_ID(N'${DB_NAME}') IS NULL SELECT 0 ELSE SELECT 1;" | tr -d '\r' | tail -n 1)"

if [ "$db_exists" = "0" ]; then
  restore_database
else
  echo "Database ${DB_NAME} already exists; skipping restore."
fi

wait "$sqlservr_pid"
