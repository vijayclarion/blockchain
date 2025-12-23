# Claims API (.NET)

This is the .NET implementation of the backend API for the Claim Settlement dApp.

## Overview
This API exposes REST endpoints that interact with the Hyperledger Fabric network via the `peer` CLI. It facilitates the creation, approval, and settlement of claims.

## Prerequisites
- **.NET 6 SDK** installed.
- **Peer CLI** (`peer`) binary accessible in the system PATH or configured in `appsettings.json`.
- Valid Crypto Material (certificates) accessible to this application.

## Configuration
Update `appsettings.json` with your environment values:

```json
"Fabric": {
  "PeerBinaryPath": "peer", // or absolute path like /bin/peer
  "ChannelName": "mychannel",
  "ChaincodeName": "claims_cc",
  "MspId": "Org1MSP",
  "MspConfigPath": "d:/VijayN/Work/Repo/BlockChain/network/crypto-config/peerOrganizations/org1.example.com/users/Admin@org1.example.com/msp",
  "OrdererAddress": "localhost:7050",
  "PeerAddress": "localhost:7051",
  "TlsEnabled": "false"
}
```

*Note: The `MspConfigPath` must point to a valid User or Admin MSP folder containing `signcerts` and `keystore`.*

## Running the API

1. Restore dependencies:
   ```bash
   dotnet restore
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. Access Swagger UI:
   Open `http://localhost:5000/swagger` in your browser.

## Endpoints

- `GET /api/Claims`: List all claims.
- `GET /api/Claims/{id}`: Get a claim by ID.
- `POST /api/Claims`: Create a new claim.
- `POST /api/Claims/{id}/approve`: Approve a claim.
- `POST /api/Claims/{id}/settle`: Settle a claim.
- `POST /api/Claims/init`: Initialize the ledger with dummy data.
