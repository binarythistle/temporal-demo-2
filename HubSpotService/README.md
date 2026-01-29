

## Set up

### HubSpot

- Create an account
- Create a Legacy App
- Get the Token
- Test that you can create a Company using cURL:

```bash
curl -XPOST -H 'authorization: Bearer <INSERT YOUR TOKEN>' -H "Content-type: application/json" -d '{
  "properties": {
    "name": "Acme Corporation",
    "domain": "acme.com",
    "industry": "RETAIL",
    "phone": "555-123-4567"
  }
}' 'https://api.hubapi.com/crm/v3/objects/companies'
```

### HubSpotService (this app)

- Create a User-secret to hold your HubSpot token:

```
dotnet user-secrets set "HubSpotToken" "<INSERT YOUR TOKEN>"
```

You should see:

```
Successfully saved HubSpotToken to the secret store.
```

The other configuration elements are stored in `appsettings.Development.json`, there should be no need to change these.