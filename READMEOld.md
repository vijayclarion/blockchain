# Inter-company Claim Settlement dApp - Complete Setup Guide

This repository contains a full Proof of Concept (POC) for an Inter-company Claim Settlement application using **Hyperledger Fabric** and **.NET Core**.

## Components
1.  **Network**: A custom Fabric network (Orderer + Peer + CLI) running in Docker.
2.  **Smart Contract (Chaincode)**: Written in C# (.NET 6.0).
3.  **Backend API**: Written in C# (.NET 6.0 Web API).
4.  **Frontend**: Simple HTML/JS Dashboard.

---

## 1. Prerequisites

Ensure you have the following installed:
-   **Docker Desktop** (enabled with Linux containers)
-   **.NET 6.0 SDK**
-   **VS Code** (Optional)
-   **Fabric Binaries** (`peer`, `configtxgen`, `cryptogen`)
    -   *If you do not have these locally, you can use the dockerized fabric-tools image, but having them in your path makes the CLI steps easier.*

---

## 2. Infrastructure Setup (Fabric Network)

We will start a minimal network with 1 Orderer and 1 Peer (Org1).

### Step 2.1: Generate Certificates & Genesis Block
Navigate to the `network` directory.

```bash
cd network
```

Since you do not have Fabric binaries installed locally, we will use the `hyperledger/fabric-tools` Docker image. We must set the working directory `-w /data` so that the generated files are saved to your local folder.

**Run these commands in your terminal (PowerShell or CMD):**

1.  **Generate Crypto Material** (Certificates):
    ```bash
    docker run --rm -v "D:/VijayN/Work/Repo/BlockChain/network:/data" -w /data hyperledger/fabric-tools:latest cryptogen generate --config=crypto-config.yaml
    ```

2.  **Generate Genesis Block**:
    ```bash
    docker run --rm -v "D:/VijayN/Work/Repo/BlockChain/network:/data" -w /data -e FABRIC_CFG_PATH=/data hyperledger/fabric-tools:latest configtxgen -profile TwoOrgsOrdererGenesis -channelID system-channel -outputBlock channel-artifacts/genesis.block
    ```

3.  **Generate Channel Transaction**:
    ```bash
    docker run --rm -v "D:/VijayN/Work/Repo/BlockChain/network:/data" -w /data -e FABRIC_CFG_PATH=/data hyperledger/fabric-tools:latest configtxgen -profile TwoOrgsChannel -outputCreateChannelTx channel-artifacts/channel.tx -channelID mychannel
    ```

*Note: The `-w /data` flag ensures the commands run inside the mounted directory, so the output files are saved to your disk.*

### Step 2.2: Start the Containers
Bring up the network nodes (Orderer, Peer, CLI).

```bash
docker-compose up -d
```

Check if containers are running:
```bash
docker ps
```
You should see `orderer.example.com`, `peer0.org1.example.com`, and `cli`.

### Step 2.3: Create and Join Channel
We will use the `cli` container to execute peer commands within the network.

Enter the CLI container:
```bash
docker exec -it cli bash
```

**Inside the CLI container, run:**

1.  **Create the Channel**:
    ```bash
    peer channel create -o orderer.example.com:7050 -c mychannel -f ./channel-artifacts/channel.tx
    ```

2.  **Join the Peer to the Channel**:
    ```bash
    peer channel join -b mychannel.block
    ```

3.  *Exit the container:*
    ```bash
    exit
    ```

---

## 3. Deploy .NET Chaincode

For this POC, we will run the Chaincode as an external service (or locally in Dev Mode) to simplify the .NET deployment without building complex custom peer images.

### Step 3.1: Start the Chaincode (External Service)
Open a **new terminal** window. Navigate to the chaincode folder and run it. It will connect to the peer.

```bash
cd chaincode/claims_contract_dotnet
```

Run the chaincode pointing to the peer:
```bash
dotnet run -- --peer.address 127.0.0.1:7052 --chaincode-id-name claims_cc:1.0
```
*Leave this terminal open. The chaincode is now running and waiting for the peer.*

### Step 3.2: Approve and Commit Chaincode
Go back to your **original terminal** (or the `cli` container).

Enter the CLI container again:
```bash
docker exec -it cli bash
```

**Inside CLI:**

1.  **Package Chaincode** (Optional step in external mode, we just need the metadata usually, but we will approve the definition directly):
    
    Since we are running externally with ID `claims_cc:1.0`, we approve that specific Label/PackageID.

2.  **Approve the Definition**:
    ```bash
    peer lifecycle chaincode approveformyorg -o orderer.example.com:7050 --channelID mychannel --name claims_cc --version 1.0 --package-id claims_cc:1.0 --sequence 1
    ```

3.  **Commit the Definition**:
    ```bash
    peer lifecycle chaincode commit -o orderer.example.com:7050 --channelID mychannel --name claims_cc --version 1.0 --sequence 1
    ```

4.  **Init Ledger** (Optional Test):
    ```bash
    peer chaincode invoke -o orderer.example.com:7050 -C mychannel -n claims_cc -c '{"Args":["InitLedger"]}'
    ```
    *If successful, you will see a status 200/OK message.*

---

## 4. Start the Application API

Now that the blockchain is running, we start the API to interact with it.

### Step 4.1: Configure the API
Open `application/api_dotnet/appsettings.json`.

Update the `MspConfigPath`. It must be the **absolute path** to the Admin MSP generated in Step 2.1.
Example (Windows):
```json
"MspConfigPath": "D:/VijayN/Work/Repo/BlockChain/network/crypto-config/peerOrganizations/org1.example.com/users/Admin@org1.example.com/msp"
```

### Step 4.2: Run the API
Open a **new terminal**.

```bash
cd application/api_dotnet
dotnet run
```

The API will start at `http://localhost:5000` (or similar port shown in logs).

---

## 5. Settlement Flow (Happy Path)

You can use **Swagger** (`http://localhost:<PORT>/swagger`) to execute these steps.

### 1. Create a Claim
**Endpoint**: `POST /api/Claims`
**Body**:
```json
{
  "id": "CLM001",
  "policyID": "POL-999",
  "claimantName": "Alice wonderland",
  "amount": 1000,
  "description": "Car Accident",
  "approverMSP": "Org1MSP" 
}
```
*(Note: In a multi-org setup, ApproverMSP would be Org2, but here we self-approve for POC).*

### 2. View Claim
**Endpoint**: `GET /api/Claims/CLM001`
*Status should be `SUBMITTED`.*

### 3. Approve Claim
**Endpoint**: `POST /api/Claims/CLM001/approve`
*Status changes to `APPROVED`.*

### 4. Settle Claim
**Endpoint**: `POST /api/Claims/CLM001/settle`
*Status changes to `SETTLED`.*

---

## 6. Cleanup

To stop the network and remove containers:

```bash
cd network
docker-compose down -v
```
