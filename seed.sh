#!/bin/bash
# Seed sample contacts via API
BASE="http://localhost:5100/api"
echo "Registering admin..."
curl -s -X POST "$BASE/auth/register" -H "Content-Type: application/json" \
  -d '{"username":"hartunoo.soud@hmo.gov.bn","password":"SuperAdmin@123"}' > /dev/null

echo "Logging in..."
COOKIE=$(mktemp)
curl -s -c "$COOKIE" -X POST "$BASE/auth/login" -H "Content-Type: application/json" \
  -d '{"username":"hartunoo.soud@hmo.gov.bn","password":"SuperAdmin@123"}' > /dev/null

echo "Seeding contacts..."
count=0
while IFS= read -r contact; do
  curl -s -b "$COOKIE" -X POST "$BASE/contacts" -H "Content-Type: application/json" -d "$contact" > /dev/null
  ((count++))
  echo "  Added contact $count"
done < <(python3 -c "import json; [print(json.dumps(c)) for c in json.load(open('seed-data.json'))]")

echo "Done! $count contacts seeded."
rm -f "$COOKIE"
