# Local Fabric Network

This directory contains the commands and configuration to run a local Hyperledger Fabric network (Proof of Concept).

## Prerequisites

- Docker Desktop
- Hyperledger Fabric Binaries (cryptogen, configtxgen) or run via Docker.

## Setup Steps

### 1. Generate Crypto Material & Artifacts

Since you do not have local binaries, use the `hyperledger/fabric-tools` Docker image to generate the files.

**PowerShell**:
```powershell
# Generate Crypto
docker run --rm -v ${PWD}:/work -w /work hyperledger/fabric-tools:latest cryptogen generate --config=./crypto-config.yaml

# Generate Channel Configuration Block
docker run --rm -v ${PWD}:/work -w /work hyperledger/fabric-tools:latest configtxgen -configPath . -profile TwoOrgsChannel -outputBlock ./channel-artifacts/mychannel.block -channelID mychannel
```

**Command Prompt (cmd.exe)**:
```cmd
# Generate Crypto
docker run --rm -v "%cd%":/work -w /work hyperledger/fabric-tools:latest cryptogen generate --config=./crypto-config.yaml

# Generate Channel Configuration Block
docker run --rm -v "%cd%":/work -w /work hyperledger/fabric-tools:latest configtxgen -configPath . -profile TwoOrgsChannel -outputBlock ./channel-artifacts/mychannel.block -channelID mychannel
```

### 2. Start the Network

```bash
docker-compose up -d
```

### 3. Create and Join Channel

Enter the CLI container:
```bash
docker exec -it cli bash
```

Inside the CLI container:

**A. Join Orderer (Using osnadmin)**
The orderer must be joined via the Admin API (port 9443) using TLS.

```bash
# 1. Export Admin TLS Variables
export ORDERER_CA=/opt/gopath/src/github.com/hyperledger/fabric/peer/crypto/ordererOrganizations/example.com/orderers/orderer.example.com/msp/tlscacerts/tlsca.example.com-cert.pem
export ORDERER_ADMIN_TLS_SIGN_CERT=/opt/gopath/src/github.com/hyperledger/fabric/peer/crypto/ordererOrganizations/example.com/orderers/orderer.example.com/tls/server.crt
export ORDERER_ADMIN_TLS_PRIVATE_KEY=/opt/gopath/src/github.com/hyperledger/fabric/peer/crypto/ordererOrganizations/example.com/orderers/orderer.example.com/tls/server.key

# 2. Join the Channel
osnadmin channel join --channelID mychannel \
  --config-block ./channel-artifacts/mychannel.block \
  -o orderer.example.com:9443 \
  --ca-file $ORDERER_CA \
  --client-cert $ORDERER_ADMIN_TLS_SIGN_CERT \
  --client-key $ORDERER_ADMIN_TLS_PRIVATE_KEY

# 3. List Channels (Verify)
osnadmin channel list -o orderer.example.com:9443 --ca-file $ORDERER_CA --client-cert $ORDERER_ADMIN_TLS_SIGN_CERT --client-key $ORDERER_ADMIN_TLS_PRIVATE_KEY
```

**B. Join Peer**
```bash
peer channel join -b ./channel-artifacts/mychannel.block
```

### 4. Deploy .NET Chaincode

**Method A: Chaincode as a Service (Recommended for .NET)**
You need to configure the peer to connect to your local .NET application.

**Method B: Standard Lifecycle (Requires custom builder or supported image)**
Since default `fabric-ccenv` supports Go/Java, using .NET is slightly more involved in the standalone peer.
We recommend using the standard `test-network` scripts which handle some of this complexity, or running the chaincode locally and setting the peer to dev mode.

To run chaincode in DevMode against this network:
1. Ensure Peer is in DevMode (Already set `CORE_PEER_CHAINCODE_LISTENADDRESS`).
2. Run your .NET Key:
   ```bash
   cd ../chaincode/claims_contract_dotnet
   dotnet run -- --peer.address 127.0.0.1:7052 --chaincode-id-name claims_cc:1.0
   ```
3. Approve/Commit the definition in the CLI (using `peer lifecycle chaincode approveformyorg` etc) but with `--package-id claims_cc:1.0`.

## Alternative: Use Test Network
The root `README.md` refers to using the standard Fabric `test-network`. That is often easier as it handles certificate generation automatically.
