# Infrastructure & Connectivity Specifications

## 1. Executive Summary
This document details the infrastructure requirements for the **Inter-company Claim Settlement dApp**. The architecture mimics a production-grade **Hyperledger Fabric** deployment suitable for a consortium of 3 organizations (Insurer A, Insurer B, Auditor).

## 2. Infrastructure Estimates (OnPrem)

### Node Inventory & Hardware Recommendations
For a High Availability (HA) production setup:

| Node Type | Role | Count | vCPU | RAM | Storage | Rationale |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Orderer** | Consensus Service (Raft) | 3 (min) | 4 | 8GB | 100GB SSD | Min 3 for Raft crash fault tolerance. |
| **Peer** | Maintain Ledger & Chaincode | 2 per Org | 4 | 16GB | 200GB SSD | 1 Anchor, 1 Regular. High RAM for CouchDB. |
| **CA Server** | Identity Management | 1 per Org | 2 | 4GB | 20GB | Lightweight. Handles PKI. |
| **CLI / Tools** | Admin Operations | 1 Shared | 2 | 4GB | 20GB | Scripts, Binaries. |

**Total Resource Pool (3 Orgs + Orderers):**
*   **vCPUs**: ~40 core
*   **RAM**: ~120 GB
*   **Storage**: ~1.5 TB SSD

### Cost Estimate (Approximate Monthly - OnPrem/Cloud Equivalent)
*   **Compute**: ~$800 - $1,200/mo (depending on enterprise hardware amortization or cloud instance types like AWS t3.xlarge).
*   **Storage**: ~$200/mo (High I/O SSDs are critical for LevelDB/CouchDB performance).
*   **Network**: Variable (intra-cluster traffic is free usually, egress depends on usage).

## 3. Connectivity Requirements

All communication in Hyperledger Fabric is secured via **mTLS (Mutual TLS)**.

### Network Firewall & Port Matrix

| Source | Destination | Protocol | Port | Purpose |
| :--- | :--- | :--- | :--- | :--- |
| **Client App** | **Peer** | gRPC | 7051 | Endorsement (Proposal Submission), Event Listening. |
| **Client App** | **Orderer** | gRPC | 7050 | Transaction Broadcast (submit signed RW sets). |
| **Peer** | **Orderer** | gRPC | 7050 | **Deliver Service**: Peers pull blocks from Orderers. |
| **Peer (Org1)** | **Peer (Org1)** | gRPC | 7051 | **Gossip (Intra-Org)**: State sync, Private Data dissemination. |
| **Peer (Org1)** | **Peer (Org2)** | gRPC | 7051 | **Gossip (Inter-Org)**: Via **Anchor Peers** only. Service discovery. |
| **Orderer** | **Orderer** | gRPC | 7050 | **Raft Consensus**: Leader election and log replication. |
| **Admin/Client** | **CA** | HTTPS | 7054 | Enrollment, Certificate Issuance. |
| **Peer** | **CouchDB** | TCP | 5984 | World State Database interface (localhost usually). |

### Critical Connectivity Flows

1.  **Peer-to-Orderer (The "Deliver" Service)**:
    *   Peers MUST have outbound connectivity to the Orderering Service.
    *   Peers actively *pull* blocks from the Orderers; Orderers do not push.
    *   *Requirement*: Firewall open from Peer Subnet -> Orderer Subnet on port 7050.

2.  **Gossip Protocol (Peer-to-Peer)**:
    *   Used for data dissemination (block propagation to peers who missed them) and Private Data.
    *   **Anchor Peers** must be reachable by peers in other organizations.
    *   *Requirement*: Public/VPN IP reachability between Organization Peer subnets on port 7051.

3.  **Client Application**:
    *   Needs visibility to **both** Peers (for endorsement) and Orderers (for ordering).
    *   If using the **Gateway SDK** (Fabric v2.4+), the client only needs to connect to one Peer (the Gateway), which proxies other requests. This simplifies firewall rules significantly.

### Latency Requirements
*   **Orderer-to-Orderer**: Low latency (<500ms) required for Raft leader heartbeats to prevent frequent re-elections.
*   **Peer-to-Orderer**: Moderate latency acceptable, but affects block finality time.
