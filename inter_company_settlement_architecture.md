# Inter-Company Claim Settlement System - Technical Architecture Guide

## 1. Network Topology

### Organization Structure
The network consists of three distinct organizations, each with its own Membership Service Provider (MSP).

*   **Insurer A (OrgA)**: A primary participant initiating or receiving claims.
*   **Insurer B (OrgB)**: A primary participant initiating or receiving claims.
*   **Neutral Org (NeutralOrg)**: The network administrator, arbiter, and host of shared infrastructure (Ordering Service).

### Node Types & Distribution
| Organization | Peer Nodes | Node Type | Purpose |
| :--- | :--- | :--- | :--- |
| **Insurer A** | `peer0.orgA` | Endorsing Peer | Holds ledger, executes chaincode, signs proposals. |
| | `peer1.orgA` | Committing Peer | Redundant node for HA, syncs ledger, no endorsement (optional). |
| **Insurer B** | `peer0.orgB` | Endorsing Peer | Holds ledger, executes chaincode, signs proposals. |
| | `peer1.orgB` | Committing Peer | Redundant node for HA, syncs ledger. |
| **Neutral Org** | `peer0.neutral` | Committing Peer | Audits transactions, holds ledger for dispute resolution. |
| | `orderer[1..3]` | Ordering Nodes | Raft Cluster for different strategies (see Section 2). |

### Channel Strategy
We will use a **Single Channel** (`settlement-channel`) approach combined with Private Data Collections (PDC) for simplicity and scalability, rather than creating bilateral channels for every pair of insurers.

*   **Channel Members**: OrgA, OrgB, NeutralOrg.
*   **Visibility**: All peers store the specific block data, but sensitive details are masked via PDCs (see Section 3).

---

## 2. Ordering Service Strategy

### Strategy A: Neutral Org Hosted (Centralized Raft)
*   **Description**: The Neutral Org hosts a Raft cluster (3-5 nodes).
*   **Pros**:
    *   Simplified maintenance for Insurers A and B (they don't need to manage orderers).
    *   Neutral Org acts as the trusted "sequencer" of truth.
*   **Cons**:
    *   **Single Point of Failure (Institutional)**: If Neutral Org goes down or acts maliciously (censorship), the network halts.
    *   Less decentralization.

### Strategy B: Distributed Raft Cluster
*   **Description**: The consenters set includes nodes from all orgs (e.g., 1 node from A, 1 node from B, 1 node from Neutral).
*   **Pros**:
    *   **High Decentralization**: No single organization controls block generation.
    *   **Resilience**: Network survives if one organization goes offline (assuming 2/3 quorum).
*   **Cons**:
    *   Complex configuration (TLS cert rot, connectivity meshes).
    *   Insurers must manage ordering infrastructure.

**Recommendation**: **Strategy A (Neutral Hosted)** is recommended for Production if ensuring `<100ms` stable latency between organizations is difficult (e.g., geographically dispersed public internet).
*   **Why**: Raft is extremely sensitive to latency. A cluster split across unstable links will suffer from frequent leader elections, stalling the network. Hosting the cluster within the Neutral Org's high-speed LAN ensures block production stability, while A and B only need "good enough" connectivity to *receive* blocks, not participate in the consensus loop.

### Network Connectivity (Firewall & Peering)

Regardless of the strategy, **Peer-to-Peer (Gossip)** connectivity is required between OrgA and OrgB to exchange Private Data (PDC). Data in PDCs is *not* in blocks, so it must travel via peer gossip.

#### Connectivity for Strategy A (Neutral Hosted)
*   **Peer Gossip (A ↔ B)**: Ports `7051` (Exchange PDCs & Block announcements).
*   **Orderer Access (A/B → Neutral)**: OrgA and OrgB Peers/Clients must reach Neutral Org Orderers on port `7050` (or `9443` if separate).
*   **Consensus**: Internal only to Neutral Org (no cross-org Raft traffic).

#### Connectivity for Strategy B (Distributed Raft)
*   **Peer Gossip (A ↔ B)**: Same as Strategy A (Port `7051`).
*   **Orderer Mesh (Raft Cluster)**: Critical. Orderer nodes from A, B, and Neutral must have a fully connected mesh on port `7050` (or dedicated cluster port).
    *   `OrdererA` ↔ `OrdererB`
    *   `OrdererB` ↔ `OrdererNeutral`
    *   `OrdererA` ↔ `OrdererNeutral`
*   **Client Submission**: Clients submit to their *local* orderer (Internal traffic only), keeping the "Submit" traffice local.

### Physical Network Infrastructure

#### Connectivity Layer
*   **Private Connectivity (Recommended)**: Site-to-Site VPN (IPsec) or Direct Connect (MPLS/ExpressRoute) between OrgA, OrgB, and Neutral Org data centers.
    *   *Why*: Simulates a LAN. Avoids exposing gRPC ports to the public internet.
*   **Public Internet (Alternative)**: Feasible but requires rigorous security.
    *   **Public IP**: Each exposed node (Peer/Orderer) needs a static Public IP or a Load Balancer/Ingress Controller with a Public IP.
    *   **Security**: Mutual TLS (mTLS) is **mandatory** and sufficient for encryption, but IP allow-listing is highly recommended to prevent DDoS.

#### Bandwidth & Latency
*   **Latency**:
    *   **Strategy A**: `< 200ms` acceptable from A/B to Neutral (Data synchronization only).
    *   **Strategy B**: `< 100ms` **STRICT** requirement between all Orderer nodes.
*   **Performance Targets (TPS)**:
    *   **Baseline**: 50-100 TPS (Standard optimization). Sufficient for most insurance claim workloads (e.g., 50 claims/second = 4.3M claims/day).
    *   **High Performance**: 1000+ TPS. Requires:
        *   **Batching**: `MaxMessageCount` = 50-100 in `configtx.yaml`.
        *   **Validation**: Parallel validation enabled (VSCC) on Peers.
        *   **Chaincode**: Optimized read/write sets (avoiding "Hot Keys").
        *   **Infrastructure**: 16+ vCPU Peers, extensive caching, and 10Gbps+ network.
    *   **Throughput Constraint**: Usually limited by crypto-verification (CPU) and disk I/O (Committing), not the Ordering Service itself.
*   **Throughput**:
    *   **Minimum**: 50 Mbps dedicated symmetric uplink.
    *   **Recommended**: 1 Gbps+ for high throughput (100+ TPS) or large blocks (2MB+).
    *   *Note*: Block propagation is "bursty". If PDCs involve large files (e.g., PDF Claim documents), bandwidth requirements scale linearly with document size/frequency. Consider off-chain storage (IPFS/S3) for files > 2MB.

---

## 3. Privacy: Private Data Collections (PDC)

To satisfy the requirement that *sensitive claim data stays between A and B*, while Neutral Org only sees the hash:

### Collection Definition
We define a collection `collectionMaroon` (for A & B):

```json
[
  {
    "name": "collectionClaimsAB",
    "policy": "OR('OrgAMSP.member', 'OrgBMSP.member')",
    "requiredPeerCount": 1,
    "maxPeerCount": 2,
    "blockToLive": 0,
    "memberOnlyRead": true
  }
]
```

### Data Visibility
*   **OrgA & OrgB**: Store actual claim details (Policy#, Amount, PII) in their local SideDB.
*   **Neutral Org**: Stores only the **Private Data Hash** on the public ledger. They can verify *that* a transaction occurred and its immutability, but cannot read the content unless explicitly shared (e.g., during a dispute via an ephemeral Key exchange or off-chain channel).

---

## 4. Infrastructure Requirements

### Hardware / VM Sizing (Per Node)
| Node Type | CPU | RAM | Storage | Disk Type |
| :--- | :--- | :--- | :--- | :--- |
| **Peer Node** | 4 vCPU | 8-16 GB | 500 GB+ | SSD (NVMe preferred for LevelDB) |
| **Orderer** | 2 vCPU | 4 GB | 100 GB | SSD |
| **CA / Tools**| 1 vCPU | 2 GB | 20 GB | Standard HDD/SSD |

### Software Stack
*   **Container Runtime**: Docker Enterprise or Containerd.
*   **Orchestration**: Kubernetes (K8s) is recommended for production (using Fabric Operator or Helm charts).
*   **Database**: CouchDB (if rich queries on metadata are needed) or GoLevelDB (for pure performance).
*   **KVASER**: (Assuming this refers to specific internal tooling or vault, otherwise HashiCorp Vault is standard for key management).

---

## 5. Data Flow: Claim Transaction Lifecycle

1.  **Proposal**:
    *   **OrgA Application** creates a "Submit Claim" transaction proposal containing private data (transient field).
    *   Sends proposal to **OrgA Peer** and **OrgB Peer** (Endorsers).
2.  **Execution & Privacy**:
    *   Peers execute chaincode.
    *   Private data is stored in `transient` store, not written to the read-write set directly.
    *   Hash of private data is calculated and added to the read-write set.
    *   **OrgA** and **OrgB** peers distribute the private data to each other via Gossip protocol (SideDB).
3.  **Endorsement**:
    *   OrgA and OrgB peers sign the RW set (hashes only) and return to the Application.
4.  **Submission**:
    *   Application assembles the signed endorsements into a transaction.
    *   Submits transaction to **Ordering Service**.
5.  **Ordering**:
    *   Orderer packages the transaction into a Block.
6.  **Validation & Commit**:
    *   Block is broadcast to **All Peers** (A, B, Neutral).
    *   **Neutral Peer** validates endorsement policy but only sees hashes. Commits block.
    *   **OrgA/B Peers** validate block AND verify they have the matching private data in SideDB. Commits block and moves private data from temporary to permanent storage.

---

## 6. Rollback Transaction Flow

Hyperledger Fabric is an **immutable ledger**; you cannot "delete" or "rollback" a committed block. Strategies for handling "bugs" or bad data:

### Scenario: Production Bug resulting in Bad Data
1.  **Identify**: Query ledger to find the incorrect transaction ID.
2.  **Compensating Transaction (The "Fix")**:
    *   Issue a *new* transaction that reverses the effect of the bad one.
    *   Example: If Claim #123 was settled for $10,000 in error, issue a `ReverseClaim` transaction for -$10,000 or `UpdateClaim` to set status to `CORRECTED`.
3.  **Chaincode Logic (Preventative)**:
    *   Implement `SchemaVersion` in the state. If a bug was due to schema interpretation, deploy Chaincode v2.0 that handles both legacy v1 data and new v2 logic.
4.  **Ledger Reset (Catastrophic Case Only)**:
    *   If the bug is critical (e.g., corrupted world state non-recoverably), the only "rollback" is to restore peers/orderers from a **Backup Snapshot** taken before the bad transaction.
    *   **Warning**: This requires halting the entire network and coordinating a reset across ALL organizations. This is a disaster recovery procedure, not a standard flow.
