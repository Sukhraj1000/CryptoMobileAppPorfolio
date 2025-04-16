#!/bin/bash

# Log function for better debugging
log() {
  echo "[SkiaSharp Fix] $1"
}

log "Starting framework cleanup..."

# Look for any build directories containing iOS frameworks
SEARCH_DIR="/Users/sukhrajkalon/Desktop/Final Year/6002/CryptoApp copy"
log "Searching for frameworks in $SEARCH_DIR"

# Clean extended attributes from all frameworks in the directory tree
find "$SEARCH_DIR" -name "*.framework" -type d | while read -r framework; do
  log "Cleaning extended attributes from $framework"
  xattr -cr "$framework" 2>/dev/null
done

log "Framework cleanup complete!"
exit 0 