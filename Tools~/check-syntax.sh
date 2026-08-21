#!/usr/bin/env bash
# Editor/*.cs をスクラッチビルドし、Unity 参照欠如 (CS0246/CS0234) 以外のエラーを検出する

set -euo pipefail

cd "$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT

cp Editor/*.cs "$tmp"/
cat > "$tmp/check.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
  </PropertyGroup>
</Project>
EOF

output=$(cd "$tmp" && dotnet build 2>&1 || true)
real_errors=$(echo "$output" | grep 'error CS' | grep -vE 'CS0246|CS0234' || true)

if [ -n "$real_errors" ]; then
    echo "$real_errors" | sort -u
    echo "NG: Unity 参照欠如以外のコンパイルエラーがあります" >&2
    exit 1
fi

echo "OK: 構文エラーなし (Unity 参照欠如の CS0246/CS0234 は除外)"
