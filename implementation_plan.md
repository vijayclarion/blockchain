# Implementation Plan - Inter-company Claim Settlement dApp POC

This plan outlines the steps to build a "Happy Path" Proof of Concept (POC) for the Inter-company Claim Settlement dApp using Hyperledger Fabric.

## 1. Project Structure Setup
- Create directory structure for the project.
- `chaincode/`: Smart contracts (Go).
- `application/`: Backend API and Client dApp (Node.js/Express + Basic UI).
- `network/`: Scripts to launch a local Fabric test network.

## 2. Chaincode Development (Smart Contract)
- **Language**: Go
- **Asset**: `Claim`
- **Fields**: ID, PolicyID, Claimant, Amount, Status (SUBMITTED, VALIDATED, SETTLED), SubmitterOrg, PayerOrg.
- **Functions**:
    - `CreateClaim`: Submit a new claim.
    - `ValidateClaim`: Endorse/Approve a claim (by the counterparty).
    - `SettleClaim`: Mark as settled.
    - `GetClaim`: Read a single claim.
    - `GetAllClaims`: List all claims.

## 3. Network Setup (Local Dev)
- Utilize the standard Hyperledger Fabric `test-network` (Minifabric or Fabric Samples scripts) assumption or provide specific Docker Compose files for a minimal setup (1 Orderer, 1 Peer per Org).
- *Note*: For this POC, we will simulate the network interaction using the `fabric-network` SDK against a mocked or standard local test network environment.

## 4. Application (API & UI)
- **Backend**: Node.js with `fabric-network` SDK.
    - Endpoint to Initialize Ledger.
    - Endpoints for `Create`, `Validate`, `Settle`.
- **Frontend**: Simple HTML/JS to interact with the API.

## 5. Deployment Instructions
- Guide on how to package the chaincode.
- Guide on how to run the application.
